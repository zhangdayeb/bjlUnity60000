// Assets/Game/Managers/ChipManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Architecture;
using Core.Data.Types;

namespace Game.Managers
{
    /// <summary>
    /// 筹码管理器 - 对应JavaScript项目中的useChips.js
    /// 负责处理筹码选择、转换、推荐等所有筹码相关的业务逻辑
    /// </summary>
    public class ChipManager : MonoBehaviour
    {
        [Header("筹码配置")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private ChipManagerSettings settings = ChipManagerSettings.Default();
        
        [Header("筹码数据")]
        [SerializeField] private List<ChipData> allChips = new List<ChipData>();
        [SerializeField] private ChipSelection chipSelection = new ChipSelection();

        #region 私有字段

        // 响应式数据 - 对应Vue的ref
        private ReactiveData<List<ChipData>> _availableChips = new ReactiveData<List<ChipData>>(new List<ChipData>());
        private ReactiveData<List<ChipData>> _selectedChips = new ReactiveData<List<ChipData>>(new List<ChipData>());
        private ReactiveData<ChipData> _currentChip = new ReactiveData<ChipData>(null);
        private ReactiveData<bool> _showChipSelector = new ReactiveData<bool>(false);
        
        // 筹码总金额管理
        private Dictionary<int, ChipTotalMoney> _areaChipTotals = new Dictionary<int, ChipTotalMoney>();
        
        // 筹码推荐系统
        private ChipRecommendationEngine _recommendationEngine;

        #endregion

        #region 公共属性 - 对应JavaScript中的computed

        /// <summary>
        /// 可用筹码列表
        /// </summary>
        public List<ChipData> AvailableChips => _availableChips.Value;
        
        /// <summary>
        /// 用户选择的筹码列表
        /// </summary>
        public List<ChipData> SelectedChips => _selectedChips.Value;
        
        /// <summary>
        /// 当前选中的筹码
        /// </summary>
        public ChipData CurrentChip => _currentChip.Value;
        
        /// <summary>
        /// 是否显示筹码选择器
        /// </summary>
        public bool ShowChipSelector => _showChipSelector.Value;
        
        /// <summary>
        /// 当前筹码面值
        /// </summary>
        public float CurrentChipValue => _currentChip.Value?.val ?? 0f;

        #endregion

        #region 事件 - 对应JavaScript中的emit

        public event Action<ChipData> OnChipSelected;
        public event Action<List<ChipData>> OnChipSelectionChanged;
        public event Action<float> OnCurrentChipValueChanged;
        public event Action<bool> OnChipSelectorToggled;
        public event Action<int, float> OnAreaChipTotalChanged;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            InitializeChipData();
            InitializeRecommendationEngine();
            SetupReactiveBindings();
        }

        private void Start()
        {
            LoadUserChipSelection();
            InitializeChipSelection();
        }

        private void OnDestroy()
        {
            SaveUserChipSelection();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化筹码数据 - 对应JavaScript中的筹码配置
        /// </summary>
        private void InitializeChipData()
        {
            if (allChips.Count == 0)
            {
                // 默认筹码配置 - 对应JavaScript中的CHIP_CONFIG
                allChips = new List<ChipData>
                {
                    new ChipData { index = 0, val = 1f, text = "1", src = "chip_1", betSrc = "bet_chip_1", enabled = true, minLevel = 1 },
                    new ChipData { index = 1, val = 5f, text = "5", src = "chip_5", betSrc = "bet_chip_5", enabled = true, minLevel = 1 },
                    new ChipData { index = 2, val = 10f, text = "10", src = "chip_10", betSrc = "bet_chip_10", enabled = true, minLevel = 1 },
                    new ChipData { index = 3, val = 50f, text = "50", src = "chip_50", betSrc = "bet_chip_50", enabled = true, minLevel = 1 },
                    new ChipData { index = 4, val = 100f, text = "100", src = "chip_100", betSrc = "bet_chip_100", enabled = true, minLevel = 1 },
                    new ChipData { index = 5, val = 500f, text = "500", src = "chip_500", betSrc = "bet_chip_500", enabled = true, minLevel = 2 },
                    new ChipData { index = 6, val = 1000f, text = "1K", src = "chip_1000", betSrc = "bet_chip_1000", enabled = true, minLevel = 3 },
                    new ChipData { index = 7, val = 5000f, text = "5K", src = "chip_5000", betSrc = "bet_chip_5000", enabled = true, minLevel = 4 },
                    new ChipData { index = 8, val = 10000f, text = "10K", src = "chip_10000", betSrc = "bet_chip_10000", enabled = true, minLevel = 5 },
                    new ChipData { index = 9, val = 50000f, text = "50K", src = "chip_50000", betSrc = "bet_chip_50000", enabled = true, minLevel = 6 },
                    new ChipData { index = 10, val = 100000f, text = "100K", src = "chip_100000", betSrc = "bet_chip_100000", enabled = true, minLevel = 7 }
                };
            }

            UpdateAvailableChips();
        }

        /// <summary>
        /// 初始化推荐引擎
        /// </summary>
        private void InitializeRecommendationEngine()
        {
            _recommendationEngine = new ChipRecommendationEngine();
        }

        /// <summary>
        /// 设置响应式数据绑定
        /// </summary>
        private void SetupReactiveBindings()
        {
            _selectedChips.OnValueChanged += (chips) => OnChipSelectionChanged?.Invoke(chips);
            _currentChip.OnValueChanged += (chip) => OnCurrentChipValueChanged?.Invoke(chip?.val ?? 0f);
            _showChipSelector.OnValueChanged += (show) => OnChipSelectorToggled?.Invoke(show);
        }

        /// <summary>
        /// 初始化筹码选择
        /// </summary>
        private void InitializeChipSelection()
        {
            var userInfo = GameDataStore.Instance.UserInfo.Value;
            if (userInfo != null && settings.useRecommendation)
            {
                // 使用推荐算法选择筹码
                var recommendedChips = _recommendationEngine.GetRecommendedChips(userInfo.balance, allChips);
                chipSelection.Initialize(allChips, recommendedChips);
            }
            else
            {
                // 使用默认选择
                chipSelection.Initialize(allChips);
            }

            _selectedChips.Value = chipSelection.selectedChips;
            _currentChip.Value = chipSelection.currentChip;
        }

        #endregion

        #region 核心筹码方法 - 对应JavaScript中的主要函数

        /// <summary>
        /// 筹码转换算法 - 对应conversionChip函数
        /// 这是一个递归算法，用于将金额转换为最优的筹码组合
        /// </summary>
        /// <param name="amount">要转换的金额</param>
        /// <returns>筹码组合</returns>
        public List<ChipConversionResult> ConversionChip(float amount)
        {
            if (amount <= 0)
                return new List<ChipConversionResult>();

            var result = new List<ChipConversionResult>();
            var availableChips = GetSortedAvailableChips(); // 按面值从大到小排序

            if (enableDebugLog)
                Debug.Log($"开始筹码转换: 金额={amount}");

            // 递归转换算法
            ConvertRecursive(amount, availableChips, 0, result);

            if (enableDebugLog)
            {
                Debug.Log($"筹码转换完成: {string.Join(", ", result.Select(r => $"{r.chipData.text}x{r.count}"))}");
            }

            return result;
        }

        /// <summary>
        /// 递归筹码转换核心算法
        /// </summary>
        private void ConvertRecursive(float remainingAmount, List<ChipData> availableChips, int chipIndex, List<ChipConversionResult> result)
        {
            // 递归终止条件
            if (remainingAmount <= 0 || chipIndex >= availableChips.Count)
                return;

            var currentChip = availableChips[chipIndex];
            
            // 计算当前筹码的使用数量
            int chipCount = Mathf.FloorToInt(remainingAmount / currentChip.val);
            
            if (chipCount > 0)
            {
                // 添加到结果中
                result.Add(new ChipConversionResult
                {
                    chipData = currentChip,
                    count = chipCount,
                    totalValue = chipCount * currentChip.val
                });

                // 更新剩余金额
                remainingAmount -= chipCount * currentChip.val;
            }

            // 递归处理下一个筹码
            ConvertRecursive(remainingAmount, availableChips, chipIndex + 1, result);
        }

        /// <summary>
        /// 寻找最大筹码 - 对应findMaxChip函数（递归算法）
        /// </summary>
        /// <param name="amount">金额</param>
        /// <returns>最大可用筹码</returns>
        public ChipData FindMaxChip(float amount)
        {
            return FindMaxChipRecursive(amount, GetSortedAvailableChips(), 0);
        }

        /// <summary>
        /// 递归寻找最大筹码
        /// </summary>
        private ChipData FindMaxChipRecursive(float amount, List<ChipData> chips, int index)
        {
            // 递归终止条件
            if (index >= chips.Count)
                return null;

            var currentChip = chips[index];
            
            // 如果当前筹码面值小于等于金额，则找到了
            if (currentChip.val <= amount)
                return currentChip;

            // 继续递归查找下一个
            return FindMaxChipRecursive(amount, chips, index + 1);
        }

        /// <summary>
        /// 处理当前筹码选择 - 对应handleCurrentChip函数
        /// </summary>
        /// <param name="chipData">要选择的筹码</param>
        public void HandleCurrentChip(ChipData chipData)
        {
            if (chipData == null)
            {
                Debug.LogWarning("HandleCurrentChip: 筹码数据为空");
                return;
            }

            try
            {
                // 1. 验证筹码是否可用
                if (!chipData.enabled)
                {
                    Debug.LogWarning($"筹码{chipData.text}已禁用，无法选择");
                    return;
                }

                // 2. 检查用户等级要求
                var userInfo = GameDataStore.Instance.UserInfo.Value;
                if (userInfo != null && userInfo.level < chipData.minLevel)
                {
                    Debug.LogWarning($"用户等级不足，无法选择筹码{chipData.text}");
                    return;
                }

                // 3. 检查余额是否足够
                if (userInfo != null && userInfo.balance < chipData.val)
                {
                    Debug.LogWarning($"余额不足，无法选择筹码{chipData.text}");
                    return;
                }

                // 4. 更新当前筹码
                _currentChip.Value = chipData;
                chipSelection.currentChip = chipData;

                // 5. 保存用户选择
                if (settings.autoSaveSelection)
                {
                    SaveUserChipSelection();
                }

                // 6. 触发事件
                OnChipSelected?.Invoke(chipData);

                if (enableDebugLog)
                {
                    Debug.Log($"选择筹码: {chipData.text} ({chipData.val}元)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"HandleCurrentChip异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加筹码到投注区域
        /// </summary>
        /// <param name="areaId">投注区域ID</param>
        /// <param name="chipValue">筹码面值</param>
        /// <param name="count">数量</param>
        public void AddChipToArea(int areaId, float chipValue, int count = 1)
        {
            if (!_areaChipTotals.ContainsKey(areaId))
            {
                _areaChipTotals[areaId] = new ChipTotalMoney();
            }

            _areaChipTotals[areaId].AddChip(chipValue, count);
            OnAreaChipTotalChanged?.Invoke(areaId, _areaChipTotals[areaId].totalAmount);

            if (enableDebugLog)
            {
                Debug.Log($"投注区域{areaId}添加筹码: {chipValue}x{count}, 总额: {_areaChipTotals[areaId].totalAmount}");
            }
        }

        /// <summary>
        /// 从投注区域移除筹码
        /// </summary>
        /// <param name="areaId">投注区域ID</param>
        /// <param name="chipValue">筹码面值</param>
        /// <param name="count">数量</param>
        public bool RemoveChipFromArea(int areaId, float chipValue, int count = 1)
        {
            if (!_areaChipTotals.ContainsKey(areaId))
                return false;

            bool success = _areaChipTotals[areaId].RemoveChip(chipValue, count);
            if (success)
            {
                OnAreaChipTotalChanged?.Invoke(areaId, _areaChipTotals[areaId].totalAmount);
                
                if (enableDebugLog)
                {
                    Debug.Log($"投注区域{areaId}移除筹码: {chipValue}x{count}, 总额: {_areaChipTotals[areaId].totalAmount}");
                }
            }

            return success;
        }

        /// <summary>
        /// 清空投注区域的筹码
        /// </summary>
        /// <param name="areaId">投注区域ID，-1表示清空所有区域</param>
        public void ClearAreaChips(int areaId = -1)
        {
            if (areaId == -1)
            {
                // 清空所有区域
                foreach (var kvp in _areaChipTotals)
                {
                    kvp.Value.Reset();
                    OnAreaChipTotalChanged?.Invoke(kvp.Key, 0f);
                }
                
                if (enableDebugLog)
                    Debug.Log("清空所有投注区域的筹码");
            }
            else
            {
                // 清空指定区域
                if (_areaChipTotals.ContainsKey(areaId))
                {
                    _areaChipTotals[areaId].Reset();
                    OnAreaChipTotalChanged?.Invoke(areaId, 0f);
                    
                    if (enableDebugLog)
                        Debug.Log($"清空投注区域{areaId}的筹码");
                }
            }
        }

        #endregion

        #region 筹码选择管理

        /// <summary>
        /// 显示/隐藏筹码选择器
        /// </summary>
        /// <param name="show">是否显示</param>
        public void ToggleChipSelector(bool? show = null)
        {
            bool newState = show ?? !_showChipSelector.Value;
            _showChipSelector.Value = newState;
            settings.showChipSelector = newState;
        }

        /// <summary>
        /// 更新筹码选择
        /// </summary>
        /// <param name="selectedChips">新的选择列表</param>
        public bool UpdateChipSelection(List<ChipData> selectedChips)
        {
            try
            {
                // 1. 验证选择
                var validation = ValidateChipSelection(selectedChips);
                if (!validation.IsValid)
                {
                    Debug.LogWarning($"筹码选择无效: {string.Join(", ", validation.Errors)}");
                    return false;
                }

                // 2. 更新选择
                chipSelection.selectedChips = new List<ChipData>(selectedChips);
                _selectedChips.Value = chipSelection.selectedChips;

                // 3. 如果当前筹码不在新选择中，重新设置
                if (chipSelection.currentChip == null || 
                    !selectedChips.Any(c => c.index == chipSelection.currentChip.index))
                {
                    chipSelection.currentChip = selectedChips.FirstOrDefault();
                    _currentChip.Value = chipSelection.currentChip;
                }

                // 4. 保存选择
                if (settings.autoSaveSelection)
                {
                    SaveUserChipSelection();
                }

                if (enableDebugLog)
                {
                    Debug.Log($"更新筹码选择: {string.Join(", ", selectedChips.Select(c => c.text))}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UpdateChipSelection异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取推荐的筹码组合
        /// </summary>
        /// <param name="balance">用户余额</param>
        /// <returns>推荐的筹码列表</returns>
        public List<ChipData> GetRecommendedChips(float balance)
        {
            return _recommendationEngine.GetRecommendedChips(balance, allChips);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新可用筹码列表
        /// </summary>
        private void UpdateAvailableChips()
        {
            var userInfo = GameDataStore.Instance.UserInfo.Value;
            int userLevel = userInfo?.level ?? 1;

            var availableChips = allChips.Where(chip => 
                chip.enabled && 
                chip.minLevel <= userLevel
            ).ToList();

            _availableChips.Value = availableChips;
        }

        /// <summary>
        /// 获取按面值排序的可用筹码（从大到小）
        /// </summary>
        private List<ChipData> GetSortedAvailableChips()
        {
            return _selectedChips.Value
                .Where(chip => chip.enabled)
                .OrderByDescending(chip => chip.val)
                .ToList();
        }

        /// <summary>
        /// 验证筹码选择
        /// </summary>
        private ChipSelectionValidation ValidateChipSelection(List<ChipData> selectedChips)
        {
            var validation = new ChipSelectionValidation();

            if (selectedChips == null || selectedChips.Count == 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("至少需要选择一个筹码");
                return validation;
            }

            if (selectedChips.Count > chipSelection.maxSelection)
            {
                validation.IsValid = false;
                validation.Errors.Add($"最多只能选择{chipSelection.maxSelection}个筹码");
            }

            // 检查重复
            var values = selectedChips.Select(c => c.val).ToList();
            var uniqueValues = new HashSet<float>(values);
            if (values.Count != uniqueValues.Count)
            {
                validation.IsValid = false;
                validation.Errors.Add("不能选择相同面值的筹码");
            }

            // 检查是否都是可用筹码
            var availableValues = new HashSet<float>(_availableChips.Value.Select(c => c.val));
            foreach (var chip in selectedChips)
            {
                if (!availableValues.Contains(chip.val))
                {
                    validation.IsValid = false;
                    validation.Errors.Add($"筹码{chip.text}不可用");
                }
            }

            return validation;
        }

        /// <summary>
        /// 加载用户筹码选择
        /// </summary>
        private void LoadUserChipSelection()
        {
            try
            {
                string selectionJson = PlayerPrefs.GetString("UserChipSelection", "");
                if (!string.IsNullOrEmpty(selectionJson))
                {
                    var savedSelection = JsonUtility.FromJson<ChipSelectionData>(selectionJson);
                    if (savedSelection != null && savedSelection.selectedIndexes != null)
                    {
                        var selectedChips = new List<ChipData>();
                        foreach (int index in savedSelection.selectedIndexes)
                        {
                            var chip = allChips.FirstOrDefault(c => c.index == index);
                            if (chip != null)
                            {
                                selectedChips.Add(chip);
                            }
                        }

                        if (selectedChips.Count > 0)
                        {
                            chipSelection.selectedChips = selectedChips;
                            chipSelection.currentChip = selectedChips.FirstOrDefault(c => c.index == savedSelection.currentIndex) 
                                                     ?? selectedChips[0];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"加载用户筹码选择失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存用户筹码选择
        /// </summary>
        private void SaveUserChipSelection()
        {
            try
            {
                var selectionData = new ChipSelectionData
                {
                    selectedIndexes = chipSelection.selectedChips.Select(c => c.index).ToList(),
                    currentIndex = chipSelection.currentChip?.index ?? -1
                };

                string selectionJson = JsonUtility.ToJson(selectionData);
                PlayerPrefs.SetString("UserChipSelection", selectionJson);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"保存用户筹码选择失败: {ex.Message}");
            }
        }

        #endregion

        #region 公共查询方法

        /// <summary>
        /// 获取投注区域的筹码总额
        /// </summary>
        public float GetAreaChipTotal(int areaId)
        {
            return _areaChipTotals.ContainsKey(areaId) ? _areaChipTotals[areaId].totalAmount : 0f;
        }

        /// <summary>
        /// 获取投注区域的筹码详情
        /// </summary>
        public ChipTotalMoney GetAreaChipDetails(int areaId)
        {
            return _areaChipTotals.ContainsKey(areaId) ? _areaChipTotals[areaId] : new ChipTotalMoney();
        }

        /// <summary>
        /// 获取所有投注区域的总金额
        /// </summary>
        public float GetTotalBetAmount()
        {
            return _areaChipTotals.Values.Sum(total => total.totalAmount);
        }

        /// <summary>
        /// 重置筹码管理器状态
        /// </summary>
        public void ResetChipManager()
        {
            ClearAreaChips();
            InitializeChipSelection();
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 筹码转换结果
    /// </summary>
    [System.Serializable]
    public class ChipConversionResult
    {
        public ChipData chipData;
        public int count;
        public float totalValue;
    }

    /// <summary>
    /// 筹码选择数据（用于序列化）
    /// </summary>
    [System.Serializable]
    public class ChipSelectionData
    {
        public List<int> selectedIndexes;
        public int currentIndex;
    }

    /// <summary>
    /// 筹码选择验证结果
    /// </summary>
    public class ChipSelectionValidation
    {
        public bool IsValid = true;
        public List<string> Errors = new List<string>();
    }

    /// <summary>
    /// 筹码推荐引擎
    /// </summary>
    public class ChipRecommendationEngine
    {
        /// <summary>
        /// 根据用户余额推荐筹码组合
        /// </summary>
        public List<ChipData> GetRecommendedChips(float balance, List<ChipData> allChips)
        {
            var recommendedValues = GetRecommendedValues(balance);
            var recommendedChips = new List<ChipData>();

            foreach (float value in recommendedValues)
            {
                var chip = allChips.FirstOrDefault(c => c.val == value && c.enabled);
                if (chip != null)
                {
                    recommendedChips.Add(chip);
                }
            }

            // 如果推荐的筹码少于5个，补充一些合适的筹码
            if (recommendedChips.Count < 5)
            {
                var additionalChips = allChips
                    .Where(c => c.enabled && c.val <= balance && !recommendedChips.Contains(c))
                    .OrderBy(c => c.val)
                    .Take(5 - recommendedChips.Count);
                
                recommendedChips.AddRange(additionalChips);
            }

            return recommendedChips.Take(5).ToList();
        }

        /// <summary>
        /// 根据余额获取推荐的筹码面值
        /// </summary>
        private List<float> GetRecommendedValues(float balance)
        {
            if (balance < 100f)
            {
                return new List<float> { 1f, 5f, 10f, 20f, 50f };
            }
            else if (balance < 1000f)
            {
                return new List<float> { 10f, 50f, 100f, 200f, 500f };
            }
            else if (balance < 10000f)
            {
                return new List<float> { 50f, 100f, 500f, 1000f, 2000f };
            }
            else if (balance < 100000f)
            {
                return new List<float> { 500f, 1000f, 5000f, 10000f, 20000f };
            }
            else
            {
                return new List<float> { 1000f, 5000f, 10000f, 50000f, 100000f };
            }
        }
    }

    #endregion
}