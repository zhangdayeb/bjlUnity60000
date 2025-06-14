// Assets/Game/Managers/ExemptManager.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Architecture;
using Core.Network.Interfaces;
using Core.Data.Types;

namespace Game.Managers
{
    /// <summary>
    /// 免佣管理器 - 对应JavaScript项目中的useExempt.js
    /// 负责处理免佣设置的初始化、切换和业务逻辑
    /// </summary>
    public class ExemptManager : MonoBehaviour
    {
        [Header("免佣配置")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private float exemptThresholdAmount = 1000f; // 免佣最低金额阈值
        
        [Header("存储配置")]
        [SerializeField] private string exemptStorageKey = "exempt_setting";
        [SerializeField] private bool autoSaveSettings = true;

        #region 私有字段

        private IBaccaratGameService _gameService;
        
        // 响应式数据 - 对应Vue的ref
        private ReactiveData<bool> _isExemptEnabled = new ReactiveData<bool>(false);
        private ReactiveData<ExemptSettings> _exemptSettings = new ReactiveData<ExemptSettings>(null);
        private ReactiveData<bool> _isLoading = new ReactiveData<bool>(false);
        private ReactiveData<string> _lastError = new ReactiveData<string>("");
        
        // 免佣计算缓存
        private Dictionary<float, float> _exemptCalculationCache = new Dictionary<float, float>();
        
        // 免佣统计
        private ExemptStatistics _exemptStatistics = new ExemptStatistics();

        #endregion

        #region 公共属性 - 对应JavaScript中的computed

        /// <summary>
        /// 是否启用免佣
        /// </summary>
        public bool IsExemptEnabled => _isExemptEnabled.Value;
        
        /// <summary>
        /// 免佣设置
        /// </summary>
        public ExemptSettings ExemptSettings => _exemptSettings.Value;
        
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading => _isLoading.Value;
        
        /// <summary>
        /// 最后的错误信息
        /// </summary>
        public string LastError => _lastError.Value;
        
        /// <summary>
        /// 免佣是否可用（基于设置和用户状态）
        /// </summary>
        public bool IsExemptAvailable
        {
            get
            {
                if (_exemptSettings.Value == null) return false;
                
                var userInfo = GameDataStore.Instance.UserInfo.Value;
                if (userInfo == null) return false;

                return _exemptSettings.Value.isAvailable &&
                       userInfo.balance >= _exemptSettings.Value.minBetAmount;
            }
        }
        
        /// <summary>
        /// 当前免佣率
        /// </summary>
        public float CurrentExemptRate => _exemptSettings.Value?.exemptRate ?? 0f;

        #endregion

        #region 事件 - 对应JavaScript中的emit

        public event Action<bool> OnExemptToggled;
        public event Action<ExemptSettings> OnExemptSettingsUpdated;
        public event Action<float> OnExemptSavingCalculated;
        public event Action<string> OnExemptError;
        public event Action<ExemptStatistics> OnExemptStatisticsUpdated;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            SetupReactiveBindings();
        }

        private void Start()
        {
            // 从服务定位器获取依赖
            _gameService = ServiceLocator.GetService<IBaccaratGameService>();
            
            if (_gameService == null)
            {
                Debug.LogError("ExemptManager: 未找到IBaccaratGameService服务");
                return;
            }

            // 初始化免佣设置
            _ = InitExemptSetting();
        }

        private void OnDestroy()
        {
            SaveExemptStatistics();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 设置响应式数据绑定
        /// </summary>
        private void SetupReactiveBindings()
        {
            _isExemptEnabled.OnValueChanged += (enabled) => OnExemptToggled?.Invoke(enabled);
            _exemptSettings.OnValueChanged += (settings) => OnExemptSettingsUpdated?.Invoke(settings);
            _lastError.OnValueChanged += (error) => 
            {
                if (!string.IsNullOrEmpty(error))
                    OnExemptError?.Invoke(error);
            };
        }

        #endregion

        #region 核心免佣方法 - 对应JavaScript中的主要函数

        /// <summary>
        /// 初始化免佣设置 - 对应initExemptSetting函数
        /// </summary>
        /// <returns>初始化任务</returns>
        public async Task<bool> InitExemptSetting()
        {
            if (_isLoading.Value)
            {
                if (enableDebugLog) Debug.Log("免佣设置正在初始化中，跳过重复请求");
                return false;
            }

            _isLoading.Value = true;
            _lastError.Value = "";

            try
            {
                if (enableDebugLog) Debug.Log("开始初始化免佣设置...");

                // 1. 从服务器获取免佣设置
                var serverSettings = await _gameService.GetExemptSettingsAsync();
                
                if (serverSettings == null)
                {
                    throw new Exception("服务器返回的免佣设置为空");
                }

                // 2. 更新免佣设置
                _exemptSettings.Value = serverSettings;

                // 3. 从本地存储加载用户的免佣偏好
                bool userPreference = LoadExemptPreference();
                
                // 4. 设置初始免佣状态（服务器设置 && 用户偏好 && 可用性）
                bool initialState = serverSettings.isEnabled && 
                                   userPreference && 
                                   serverSettings.isAvailable;
                
                _isExemptEnabled.Value = initialState;

                // 5. 初始化统计数据
                LoadExemptStatistics();

                if (enableDebugLog)
                {
                    Debug.Log($"免佣设置初始化成功: " +
                             $"可用={serverSettings.isAvailable}, " +
                             $"启用={initialState}, " +
                             $"费率={serverSettings.exemptRate:P2}, " +
                             $"保本点={serverSettings.breakEvenPoint}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _lastError.Value = $"初始化免佣设置失败: {ex.Message}";
                Debug.LogError($"InitExemptSetting异常: {ex.Message}");
                return false;
            }
            finally
            {
                _isLoading.Value = false;
            }
        }

        /// <summary>
        /// 切换免佣状态 - 对应toggleExempt函数
        /// </summary>
        /// <param name="forceState">强制状态（可选）</param>
        /// <returns>切换是否成功</returns>
        public bool ToggleExempt(bool? forceState = null)
        {
            try
            {
                // 1. 检查免佣是否可用
                if (!IsExemptAvailable)
                {
                    _lastError.Value = "免佣功能当前不可用";
                    if (enableDebugLog) Debug.LogWarning("免佣功能不可用，无法切换");
                    return false;
                }

                // 2. 确定新状态
                bool newState = forceState ?? !_isExemptEnabled.Value;

                // 3. 额外的业务逻辑检查
                if (newState && _exemptSettings.Value != null)
                {
                    var userInfo = GameDataStore.Instance.UserInfo.Value;
                    if (userInfo != null && userInfo.balance < _exemptSettings.Value.minBetAmount)
                    {
                        _lastError.Value = $"余额不足，免佣最低要求: {_exemptSettings.Value.minBetAmount}";
                        return false;
                    }

                    // 如果设置了仅庄家免佣，需要额外验证
                    if (_exemptSettings.Value.onlyForBanker)
                    {
                        if (enableDebugLog) Debug.Log("免佣仅适用于庄家投注");
                    }
                }

                // 4. 更新状态
                _isExemptEnabled.Value = newState;

                // 5. 保存用户偏好
                if (autoSaveSettings)
                {
                    SaveExemptPreference(newState);
                }

                // 6. 更新统计
                if (newState)
                {
                    _exemptStatistics.toggleOnCount++;
                }
                else
                {
                    _exemptStatistics.toggleOffCount++;
                }

                if (enableDebugLog)
                {
                    Debug.Log($"免佣状态切换: {(!newState ? "关闭" : "开启")} -> {(newState ? "开启" : "关闭")}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _lastError.Value = $"切换免佣失败: {ex.Message}";
                Debug.LogError($"ToggleExempt异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取投注的免佣信息 - 对应getExemptForBetting函数
        /// </summary>
        /// <param name="betAmount">投注金额</param>
        /// <param name="betType">投注类型</param>
        /// <returns>免佣信息</returns>
        public ExemptForBettingResult GetExemptForBetting(float betAmount, string betType = "banker")
        {
            var result = new ExemptForBettingResult
            {
                isApplicable = false,
                exemptAmount = 0f,
                effectiveAmount = betAmount,
                exemptRate = 0f,
                description = ""
            };

            try
            {
                // 1. 基础检查
                if (!_isExemptEnabled.Value || _exemptSettings.Value == null)
                {
                    result.description = "免佣未启用";
                    return result;
                }

                if (betAmount <= 0)
                {
                    result.description = "投注金额无效";
                    return result;
                }

                // 2. 检查投注类型限制
                if (_exemptSettings.Value.onlyForBanker && betType.ToLower() != "banker")
                {
                    result.description = "免佣仅适用于庄家投注";
                    return result;
                }

                // 3. 检查最低投注金额
                if (betAmount < _exemptSettings.Value.minBetAmount)
                {
                    result.description = $"投注金额低于免佣最低要求: {_exemptSettings.Value.minBetAmount}";
                    return result;
                }

                // 4. 计算免佣金额（使用缓存优化）
                float exemptAmount = CalculateExemptAmount(betAmount);
                
                // 5. 设置结果
                result.isApplicable = true;
                result.exemptAmount = exemptAmount;
                result.effectiveAmount = betAmount - exemptAmount;
                result.exemptRate = _exemptSettings.Value.exemptRate;
                result.description = $"免佣 {exemptAmount:F2}，实际投注 {result.effectiveAmount:F2}";

                // 6. 更新统计
                _exemptStatistics.totalExemptAmount += exemptAmount;
                _exemptStatistics.exemptApplicationCount++;

                // 7. 触发事件
                OnExemptSavingCalculated?.Invoke(exemptAmount);

                if (enableDebugLog)
                {
                    Debug.Log($"免佣计算: 投注={betAmount}, 免佣={exemptAmount:F2}, 实际={result.effectiveAmount:F2}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _lastError.Value = $"计算免佣失败: {ex.Message}";
                Debug.LogError($"GetExemptForBetting异常: {ex.Message}");
                result.description = "免佣计算失败";
                return result;
            }
        }

        /// <summary>
        /// 批量计算免佣（用于多个投注）
        /// </summary>
        /// <param name="bets">投注列表</param>
        /// <returns>批量免佣结果</returns>
        public BatchExemptResult GetBatchExemptForBetting(List<BaccaratBetRequest> bets)
        {
            var result = new BatchExemptResult
            {
                exemptResults = new List<ExemptForBettingResult>(),
                totalOriginalAmount = 0f,
                totalExemptAmount = 0f,
                totalEffectiveAmount = 0f
            };

            try
            {
                foreach (var bet in bets)
                {
                    var exemptResult = GetExemptForBetting(bet.money, bet.betType);
                    result.exemptResults.Add(exemptResult);
                    
                    result.totalOriginalAmount += bet.money;
                    result.totalExemptAmount += exemptResult.exemptAmount;
                    result.totalEffectiveAmount += exemptResult.effectiveAmount;
                }

                if (enableDebugLog)
                {
                    Debug.Log($"批量免佣计算: 原始={result.totalOriginalAmount}, " +
                             $"免佣={result.totalExemptAmount:F2}, " +
                             $"实际={result.totalEffectiveAmount:F2}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _lastError.Value = $"批量免佣计算失败: {ex.Message}";
                Debug.LogError($"GetBatchExemptForBetting异常: {ex.Message}");
                return result;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算免佣金额（带缓存优化）
        /// </summary>
        private float CalculateExemptAmount(float betAmount)
        {
            // 使用缓存优化计算
            if (_exemptCalculationCache.ContainsKey(betAmount))
            {
                return _exemptCalculationCache[betAmount];
            }

            float exemptAmount = betAmount * _exemptSettings.Value.exemptRate;
            
            // 缓存结果（限制缓存大小）
            if (_exemptCalculationCache.Count > 1000)
            {
                _exemptCalculationCache.Clear();
            }
            
            _exemptCalculationCache[betAmount] = exemptAmount;
            return exemptAmount;
        }

        /// <summary>
        /// 加载用户免佣偏好
        /// </summary>
        private bool LoadExemptPreference()
        {
            try
            {
                return PlayerPrefs.GetInt(exemptStorageKey, 0) == 1;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"加载免佣偏好失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存用户免佣偏好
        /// </summary>
        private void SaveExemptPreference(bool enabled)
        {
            try
            {
                PlayerPrefs.SetInt(exemptStorageKey, enabled ? 1 : 0);
                PlayerPrefs.Save();
                
                if (enableDebugLog)
                {
                    Debug.Log($"保存免佣偏好: {enabled}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"保存免佣偏好失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载免佣统计数据
        /// </summary>
        private void LoadExemptStatistics()
        {
            try
            {
                string statsJson = PlayerPrefs.GetString("exempt_statistics", "");
                if (!string.IsNullOrEmpty(statsJson))
                {
                    _exemptStatistics = JsonUtility.FromJson<ExemptStatistics>(statsJson);
                }
                else
                {
                    _exemptStatistics = new ExemptStatistics();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"加载免佣统计失败: {ex.Message}");
                _exemptStatistics = new ExemptStatistics();
            }
        }

        /// <summary>
        /// 保存免佣统计数据
        /// </summary>
        private void SaveExemptStatistics()
        {
            try
            {
                string statsJson = JsonUtility.ToJson(_exemptStatistics);
                PlayerPrefs.SetString("exempt_statistics", statsJson);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"保存免佣统计失败: {ex.Message}");
            }
        }

        #endregion

        #region 公共查询方法

        /// <summary>
        /// 获取免佣统计信息
        /// </summary>
        public ExemptStatistics GetExemptStatistics()
        {
            return _exemptStatistics.Clone();
        }

        /// <summary>
        /// 重置免佣统计
        /// </summary>
        public void ResetExemptStatistics()
        {
            _exemptStatistics = new ExemptStatistics();
            SaveExemptStatistics();
            OnExemptStatisticsUpdated?.Invoke(_exemptStatistics);
        }

        /// <summary>
        /// 检查指定金额是否符合免佣条件
        /// </summary>
        public bool CanApplyExempt(float amount, string betType = "banker")
        {
            if (!IsExemptAvailable || !_isExemptEnabled.Value) return false;
            if (_exemptSettings.Value == null) return false;
            
            return amount >= _exemptSettings.Value.minBetAmount &&
                   (!_exemptSettings.Value.onlyForBanker || betType.ToLower() == "banker");
        }

        /// <summary>
        /// 获取免佣保本点信息
        /// </summary>
        public float GetBreakEvenPoint()
        {
            return _exemptSettings.Value?.breakEvenPoint ?? 0f;
        }

        /// <summary>
        /// 刷新免佣设置（从服务器重新获取）
        /// </summary>
        public async Task<bool> RefreshExemptSettings()
        {
            return await InitExemptSetting();
        }

        /// <summary>
        /// 清理免佣缓存
        /// </summary>
        public void ClearExemptCache()
        {
            _exemptCalculationCache.Clear();
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 免佣投注结果
    /// </summary>
    [System.Serializable]
    public class ExemptForBettingResult
    {
        [Header("免佣信息")]
        public bool isApplicable;       // 是否适用免佣
        public float exemptAmount;      // 免佣金额
        public float effectiveAmount;   // 实际投注金额
        public float exemptRate;        // 免佣率
        public string description;      // 描述信息
    }

    /// <summary>
    /// 批量免佣结果
    /// </summary>
    [System.Serializable]
    public class BatchExemptResult
    {
        [Header("批量免佣结果")]
        public List<ExemptForBettingResult> exemptResults;
        public float totalOriginalAmount;   // 总原始金额
        public float totalExemptAmount;     // 总免佣金额
        public float totalEffectiveAmount;  // 总实际金额
    }

    /// <summary>
    /// 免佣统计数据
    /// </summary>
    [System.Serializable]
    public class ExemptStatistics
    {
        [Header("使用统计")]
        public int toggleOnCount = 0;           // 开启次数
        public int toggleOffCount = 0;          // 关闭次数
        public int exemptApplicationCount = 0;  // 免佣应用次数
        public float totalExemptAmount = 0f;    // 累计免佣金额

        [Header("时间统计")]
        public DateTime firstUseTime = DateTime.MinValue;  // 首次使用时间
        public DateTime lastUseTime = DateTime.MinValue;   // 最后使用时间

        /// <summary>
        /// 克隆统计数据
        /// </summary>
        public ExemptStatistics Clone()
        {
            return new ExemptStatistics
            {
                toggleOnCount = this.toggleOnCount,
                toggleOffCount = this.toggleOffCount,
                exemptApplicationCount = this.exemptApplicationCount,
                totalExemptAmount = this.totalExemptAmount,
                firstUseTime = this.firstUseTime,
                lastUseTime = this.lastUseTime
            };
        }
    }

    #endregion
}