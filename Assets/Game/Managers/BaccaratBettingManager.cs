// Assets/Game/Managers/BaccaratBettingManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Core.Architecture;
using Core.Network.Interfaces;
using Core.Data.Types;

namespace Game.Managers
{
    /// <summary>
    /// 百家乐投注管理器 - 对应JavaScript项目中的useBetting.js
    /// 负责处理所有投注相关的业务逻辑，包括下注、确认、取消等操作
    /// </summary>
    public class BaccaratBettingManager : MonoBehaviour
    {
        [Header("投注配置")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private float debounceTimeMs = 300f; // 防抖时间（毫秒）
        [SerializeField] private float confirmTimeoutMs = 1000f; // 确认超时时间
        
        [Header("投注区域配置")]
        [SerializeField] private List<BaccaratBetTarget> betTargets = new List<BaccaratBetTarget>();

        #region 私有字段

        private IBaccaratGameService _gameService;
        private ChipManager _chipManager;
        
        // 响应式数据 - 对应Vue的ref
        private ReactiveData<bool> _isBetting = new ReactiveData<bool>(false);
        private ReactiveData<bool> _isConfirming = new ReactiveData<bool>(false);
        private ReactiveData<float> _totalBetAmount = new ReactiveData<float>(0f);
        private ReactiveData<List<BaccaratBetRequest>> _currentBets = new ReactiveData<List<BaccaratBetRequest>>(new List<BaccaratBetRequest>());
        
        // 防抖控制
        private Dictionary<int, DateTime> _lastClickTimes = new Dictionary<int, DateTime>();
        private Dictionary<int, bool> _isConfirmingBet = new Dictionary<int, bool>();
        
        // 投注历史
        private List<BetHistoryRecord> _betHistory = new List<BetHistoryRecord>();
        
        #endregion

        #region 公共属性 - 对应JavaScript中的computed

        /// <summary>
        /// 是否正在投注中
        /// </summary>
        public bool IsBetting => _isBetting.Value;
        
        /// <summary>
        /// 是否正在确认中
        /// </summary>
        public bool IsConfirming => _isConfirming.Value;
        
        /// <summary>
        /// 当前总投注金额
        /// </summary>
        public float TotalBetAmount => _totalBetAmount.Value;
        
        /// <summary>
        /// 当前投注列表
        /// </summary>
        public List<BaccaratBetRequest> CurrentBets => _currentBets.Value;
        
        /// <summary>
        /// 是否有投注
        /// </summary>
        public bool HasBets => _currentBets.Value.Count > 0;
        
        /// <summary>
        /// 投注区域数据 - 对应JavaScript中的betTargets
        /// </summary>
        public List<BaccaratBetTarget> BetTargets => betTargets;

        #endregion

        #region 事件 - 对应JavaScript中的emit

        public event Action<BaccaratBetRequest> OnBetPlaced;
        public event Action<float> OnTotalAmountChanged;
        public event Action<bool> OnBettingStateChanged;
        public event Action<BaccaratBetResponse> OnBetConfirmed;
        public event Action<string> OnBetError;
        public event Action OnBetsCancelled;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            InitializeBetTargets();
            SetupReactiveBindings();
        }

        private void Start()
        {
            // 从服务定位器获取依赖
            _gameService = ServiceLocator.GetService<IBaccaratGameService>();
            _chipManager = FindObjectOfType<ChipManager>();
            
            if (_gameService == null)
            {
                Debug.LogError("BaccaratBettingManager: 未找到IBaccaratGameService服务");
                return;
            }
            
            if (_chipManager == null)
            {
                Debug.LogError("BaccaratBettingManager: 未找到ChipManager组件");
                return;
            }
            
            LoadBetHistory();
        }

        private void OnDestroy()
        {
            SaveBetHistory();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化投注区域 - 对应JavaScript中的投注区域配置
        /// </summary>
        private void InitializeBetTargets()
        {
            if (betTargets.Count == 0)
            {
                betTargets = new List<BaccaratBetTarget>
                {
                    new BaccaratBetTarget { id = 1, label = "庄", betAmount = 0f, showChip = false },
                    new BaccaratBetTarget { id = 2, label = "闲", betAmount = 0f, showChip = false },
                    new BaccaratBetTarget { id = 3, label = "和", betAmount = 0f, showChip = false },
                    new BaccaratBetTarget { id = 4, label = "庄对", betAmount = 0f, showChip = false },
                    new BaccaratBetTarget { id = 5, label = "闲对", betAmount = 0f, showChip = false },
                    new BaccaratBetTarget { id = 6, label = "大", betAmount = 0f, showChip = false },
                    new BaccaratBetTarget { id = 7, label = "小", betAmount = 0f, showChip = false }
                };
            }
        }

        /// <summary>
        /// 设置响应式数据绑定
        /// </summary>
        private void SetupReactiveBindings()
        {
            _isBetting.OnValueChanged += (value) => OnBettingStateChanged?.Invoke(value);
            _totalBetAmount.OnValueChanged += (value) => OnTotalAmountChanged?.Invoke(value);
            _currentBets.OnValueChanged += (bets) => UpdateBetTargetsDisplay();
        }

        #endregion

        #region 核心投注方法 - 对应JavaScript中的主要函数

        /// <summary>
        /// 执行点击投注 - 对应executeClickBet函数
        /// </summary>
        /// <param name="rateId">投注类型ID（1=庄,2=闲,3=和,4=庄对,5=闲对,6=大,7=小）</param>
        /// <returns>投注是否成功执行</returns>
        public async Task<bool> ExecuteClickBet(int rateId)
        {
            try
            {
                // 1. 防抖检查 - 对应JavaScript中的防抖逻辑
                if (!CanPlaceBet(rateId))
                {
                    if (enableDebugLog) Debug.Log($"投注被防抖拦截: rateId={rateId}");
                    return false;
                }

                // 2. 获取当前筹码金额
                float chipValue = _chipManager.GetCurrentChipValue();
                if (chipValue <= 0)
                {
                    OnBetError?.Invoke("请选择筹码");
                    return false;
                }

                // 3. 检查余额
                var userInfo = GameDataStore.Instance.UserInfo.Value;
                if (userInfo == null || userInfo.balance < chipValue)
                {
                    OnBetError?.Invoke("余额不足");
                    return false;
                }

                // 4. 创建投注请求
                var betRequest = new BaccaratBetRequest
                {
                    money = chipValue,
                    rate_id = rateId,
                    betType = GetBetTypeName(rateId),
                    isMainBet = IsMainBetType(rateId),
                    originalAmount = chipValue,
                    timestamp = DateTime.UtcNow
                };

                // 5. 更新防抖时间
                _lastClickTimes[rateId] = DateTime.UtcNow;

                // 6. 添加到当前投注列表
                var currentBets = new List<BaccaratBetRequest>(_currentBets.Value);
                
                // 检查是否已有相同类型的投注
                var existingBet = currentBets.FirstOrDefault(b => b.rate_id == rateId);
                if (existingBet != null)
                {
                    existingBet.money += chipValue;
                    existingBet.timestamp = DateTime.UtcNow;
                }
                else
                {
                    currentBets.Add(betRequest);
                }

                // 7. 更新响应式数据
                _currentBets.Value = currentBets;
                UpdateTotalBetAmount();

                // 8. 触发事件
                OnBetPlaced?.Invoke(betRequest);

                if (enableDebugLog)
                {
                    Debug.Log($"投注成功: {betRequest.betType} {chipValue}元");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ExecuteClickBet异常: {ex.Message}");
                OnBetError?.Invoke($"投注失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 确认投注 - 对应confirmBet函数
        /// </summary>
        /// <returns>确认结果</returns>
        public async Task<bool> ConfirmBet()
        {
            if (_currentBets.Value.Count == 0)
            {
                OnBetError?.Invoke("没有可确认的投注");
                return false;
            }

            if (_isConfirming.Value)
            {
                if (enableDebugLog) Debug.Log("正在确认中，忽略重复请求");
                return false;
            }

            _isConfirming.Value = true;

            try
            {
                // 1. 发送投注请求到服务器
                var response = await _gameService.PlaceBaccaratBetsAsync(_currentBets.Value, 0);

                if (response.success)
                {
                    // 2. 更新用户余额
                    var userInfo = GameDataStore.Instance.UserInfo.Value;
                    if (userInfo != null)
                    {
                        userInfo.balance = response.money_balance;
                        GameDataStore.Instance.UserInfo.Value = userInfo;
                    }

                    // 3. 保存投注历史
                    SaveBetToHistory(response);

                    // 4. 触发确认事件
                    OnBetConfirmed?.Invoke(response);

                    if (enableDebugLog)
                    {
                        Debug.Log($"投注确认成功: 花费{response.money_spend}元，余额{response.money_balance}元");
                    }

                    return true;
                }
                else
                {
                    OnBetError?.Invoke(response.message ?? "投注失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConfirmBet异常: {ex.Message}");
                OnBetError?.Invoke($"确认投注失败: {ex.Message}");
                return false;
            }
            finally
            {
                _isConfirming.Value = false;
            }
        }

        /// <summary>
        /// 取消投注 - 对应cancelBet函数
        /// </summary>
        /// <param name="rateId">要取消的投注类型ID，如果为-1则取消所有</param>
        public void CancelBet(int rateId = -1)
        {
            try
            {
                var currentBets = new List<BaccaratBetRequest>(_currentBets.Value);

                if (rateId == -1)
                {
                    // 取消所有投注
                    currentBets.Clear();
                    if (enableDebugLog) Debug.Log("已取消所有投注");
                }
                else
                {
                    // 取消指定类型的投注
                    var removed = currentBets.RemoveAll(b => b.rate_id == rateId);
                    if (enableDebugLog && removed > 0)
                    {
                        Debug.Log($"已取消投注: {GetBetTypeName(rateId)}");
                    }
                }

                _currentBets.Value = currentBets;
                UpdateTotalBetAmount();
                OnBetsCancelled?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"CancelBet异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 开牌后清理 - 对应clearAfterGameResult函数
        /// </summary>
        /// <param name="gameResult">游戏结果</param>
        public void ClearAfterGameResult(BaccaratGameResult gameResult = null)
        {
            try
            {
                // 1. 清空当前投注
                _currentBets.Value = new List<BaccaratBetRequest>();
                
                // 2. 重置投注金额
                _totalBetAmount.Value = 0f;
                
                // 3. 重置投注区域显示
                foreach (var target in betTargets)
                {
                    target.betAmount = 0f;
                    target.showChip = false;
                }
                
                // 4. 重置防抖状态
                _lastClickTimes.Clear();
                _isConfirmingBet.Clear();
                
                // 5. 重置投注状态
                _isBetting.Value = false;

                if (enableDebugLog)
                {
                    Debug.Log("开牌后清理完成");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ClearAfterGameResult异常: {ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查是否可以下注（防抖检查）
        /// </summary>
        private bool CanPlaceBet(int rateId)
        {
            if (!_lastClickTimes.ContainsKey(rateId))
                return true;

            var timeSinceLastClick = (DateTime.UtcNow - _lastClickTimes[rateId]).TotalMilliseconds;
            return timeSinceLastClick >= debounceTimeMs;
        }

        /// <summary>
        /// 获取投注类型名称
        /// </summary>
        private string GetBetTypeName(int rateId)
        {
            return rateId switch
            {
                1 => "庄",
                2 => "闲", 
                3 => "和",
                4 => "庄对",
                5 => "闲对",
                6 => "大",
                7 => "小",
                _ => "未知"
            };
        }

        /// <summary>
        /// 判断是否为主要投注类型（庄、闲、和）
        /// </summary>
        private bool IsMainBetType(int rateId)
        {
            return rateId >= 1 && rateId <= 3;
        }

        /// <summary>
        /// 更新总投注金额
        /// </summary>
        private void UpdateTotalBetAmount()
        {
            float total = _currentBets.Value.Sum(b => b.money);
            _totalBetAmount.Value = total;
        }

        /// <summary>
        /// 更新投注区域显示
        /// </summary>
        private void UpdateBetTargetsDisplay()
        {
            foreach (var target in betTargets)
            {
                var bet = _currentBets.Value.FirstOrDefault(b => b.rate_id == target.id);
                if (bet != null)
                {
                    target.betAmount = bet.money;
                    target.showChip = true;
                }
                else
                {
                    target.betAmount = 0f;
                    target.showChip = false;
                }
            }
        }

        /// <summary>
        /// 保存投注到历史记录
        /// </summary>
        private void SaveBetToHistory(BaccaratBetResponse response)
        {
            var historyRecord = new BetHistoryRecord
            {
                id = Guid.NewGuid().ToString(),
                gameNumber = response.game_number,
                bets = new List<BaccaratBetRequest>(_currentBets.Value),
                totalAmount = response.money_spend,
                timestamp = response.bet_time,
                isExemptApplied = response.is_exempt_applied,
                exemptSaving = response.exempt_saving
            };

            _betHistory.Add(historyRecord);

            // 只保留最近100条记录
            if (_betHistory.Count > 100)
            {
                _betHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 加载投注历史
        /// </summary>
        private void LoadBetHistory()
        {
            try
            {
                string historyJson = PlayerPrefs.GetString("BettingHistory", "[]");
                _betHistory = JsonUtility.FromJson<BetHistoryList>(historyJson)?.records ?? new List<BetHistoryRecord>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"加载投注历史失败: {ex.Message}");
                _betHistory = new List<BetHistoryRecord>();
            }
        }

        /// <summary>
        /// 保存投注历史
        /// </summary>
        private void SaveBetHistory()
        {
            try
            {
                var historyList = new BetHistoryList { records = _betHistory };
                string historyJson = JsonUtility.ToJson(historyList);
                PlayerPrefs.SetString("BettingHistory", historyJson);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"保存投注历史失败: {ex.Message}");
            }
        }

        #endregion

        #region 公共查询方法

        /// <summary>
        /// 获取指定类型的投注金额
        /// </summary>
        public float GetBetAmount(int rateId)
        {
            var bet = _currentBets.Value.FirstOrDefault(b => b.rate_id == rateId);
            return bet?.money ?? 0f;
        }

        /// <summary>
        /// 获取投注历史
        /// </summary>
        public List<BetHistoryRecord> GetBetHistory()
        {
            return new List<BetHistoryRecord>(_betHistory);
        }

        /// <summary>
        /// 重置投注管理器状态
        /// </summary>
        public void ResetBettingState()
        {
            ClearAfterGameResult();
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 投注历史记录
    /// </summary>
    [System.Serializable]
    public class BetHistoryRecord
    {
        public string id;
        public string gameNumber;
        public List<BaccaratBetRequest> bets;
        public float totalAmount;
        public DateTime timestamp;
        public bool isExemptApplied;
        public float exemptSaving;
    }

    /// <summary>
    /// 投注历史列表（用于序列化）
    /// </summary>
    [System.Serializable]
    public class BetHistoryList
    {
        public List<BetHistoryRecord> records;
    }

    #endregion
}