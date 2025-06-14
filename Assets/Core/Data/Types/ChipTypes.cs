// Assets/_Core/Data/Types/ChipTypes.cs
// 筹码相关类型 - 对应JavaScript项目的筹码管理数据结构

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Data.Types
{
    /// <summary>
    /// 筹码数据 - 对应JavaScript项目的筹码配置
    /// </summary>
    [System.Serializable]
    public class ChipData
    {
        [Header("基础信息")]
        [Tooltip("筹码索引")]
        public int index = 0;
        
        [Tooltip("筹码面值")]
        public float val = 0f;
        
        [Tooltip("筹码显示文本")]
        public string text = "";

        [Header("图片资源")]
        [Tooltip("筹码选择区图片路径")]
        public string src = "";
        
        [Tooltip("投注区显示图片路径")]
        public string betSrc = "";
        
        [Tooltip("筹码选择区精灵（运行时设置）")]
        [System.NonSerialized]
        public Sprite sprite;
        
        [Tooltip("投注区显示精灵（运行时设置）")]
        [System.NonSerialized]
        public Sprite betSprite;

        [Header("颜色和样式")]
        [Tooltip("筹码主题色")]
        public Color themeColor = Color.white;
        
        [Tooltip("筹码边框色")]
        public Color borderColor = Color.black;
        
        [Tooltip("文字颜色")]
        public Color textColor = Color.white;

        [Header("筹码属性")]
        [Tooltip("是否为默认筹码")]
        public bool isDefault = false;
        
        [Tooltip("是否启用")]
        public bool enabled = true;
        
        [Tooltip("最小用户等级要求")]
        public int minLevel = 1;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ChipData()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public ChipData(int chipIndex, float chipValue, string chipText, string chipSrc, string chipBetSrc)
        {
            index = chipIndex;
            val = chipValue;
            text = chipText;
            src = chipSrc;
            betSrc = chipBetSrc;
        }

        /// <summary>
        /// 创建筹码显示数据
        /// </summary>
        /// <returns>筹码显示数据</returns>
        public ChipDisplayData ToDisplayData()
        {
            return new ChipDisplayData(val, text, betSrc)
            {
                sprite = betSprite
            };
        }

        /// <summary>
        /// 获取格式化的面值显示
        /// </summary>
        /// <returns>格式化的面值</returns>
        public string GetFormattedValue()
        {
            if (val >= 10000)
            {
                return $"{val / 10000:F1}万";
            }
            else if (val >= 1000)
            {
                return $"{val / 1000:F1}K";
            }
            else
            {
                return val.ToString("F0");
            }
        }

        /// <summary>
        /// 检查用户是否可以使用此筹码
        /// </summary>
        /// <param name="userLevel">用户等级</param>
        /// <returns>是否可以使用</returns>
        public bool CanUseByUser(int userLevel)
        {
            return enabled && userLevel >= minLevel;
        }

        /// <summary>
        /// 克隆筹码数据
        /// </summary>
        /// <returns>克隆的筹码数据</returns>
        public ChipData Clone()
        {
            return new ChipData
            {
                index = this.index,
                val = this.val,
                text = this.text,
                src = this.src,
                betSrc = this.betSrc,
                sprite = this.sprite,
                betSprite = this.betSprite,
                themeColor = this.themeColor,
                borderColor = this.borderColor,
                textColor = this.textColor,
                isDefault = this.isDefault,
                enabled = this.enabled,
                minLevel = this.minLevel
            };
        }
    }

    /// <summary>
    /// 筹码选择配置 - 对应JavaScript项目的用户筹码选择
    /// </summary>
    [System.Serializable]
    public class ChipSelection
    {
        [Header("选中的筹码")]
        [Tooltip("用户选择的筹码列表（最多5个）")]
        public List<ChipData> selectedChips = new List<ChipData>();
        
        [Tooltip("当前选中的筹码")]
        public ChipData currentChip = null;

        [Header("选择配置")]
        [Tooltip("最大选择数量")]
        public int maxSelection = 5;
        
        [Tooltip("自动选择第一个")]
        public bool autoSelectFirst = true;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ChipSelection()
        {
        }

        /// <summary>
        /// 初始化筹码选择
        /// </summary>
        /// <param name="allChips">所有筹码</param>
        /// <param name="userChips">用户保存的筹码（可选）</param>
        public void Initialize(List<ChipData> allChips, List<ChipData> userChips = null)
        {
            selectedChips.Clear();

            if (userChips != null && userChips.Count > 0)
            {
                // 使用用户保存的筹码选择
                foreach (var userChip in userChips)
                {
                    var chip = allChips.Find(c => c.index == userChip.index);
                    if (chip != null && selectedChips.Count < maxSelection)
                    {
                        selectedChips.Add(chip.Clone());
                    }
                }
            }
            else
            {
                // 使用默认筹码选择（去掉前3个，取5个）
                var defaultChips = allChips.GetRange(3, Math.Min(5, allChips.Count - 3));
                selectedChips.AddRange(defaultChips.ConvertAll(c => c.Clone()));
            }

            // 设置当前选中筹码
            if (autoSelectFirst && selectedChips.Count > 0)
            {
                currentChip = selectedChips[0];
            }
        }

        /// <summary>
        /// 设置当前选中筹码
        /// </summary>
        /// <param name="chip">筹码</param>
        /// <returns>是否成功</returns>
        public bool SetCurrentChip(ChipData chip)
        {
            if (chip != null && selectedChips.Contains(chip))
            {
                currentChip = chip;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 添加筹码到选择列表
        /// </summary>
        /// <param name="chip">筹码</param>
        /// <returns>是否成功</returns>
        public bool AddChip(ChipData chip)
        {
            if (chip == null || selectedChips.Count >= maxSelection)
                return false;

            // 检查是否已存在
            if (selectedChips.Exists(c => c.index == chip.index))
                return false;

            selectedChips.Add(chip.Clone());
            
            // 如果是第一个，自动设为当前选中
            if (selectedChips.Count == 1 && autoSelectFirst)
            {
                currentChip = selectedChips[0];
            }

            return true;
        }

        /// <summary>
        /// 从选择列表移除筹码
        /// </summary>
        /// <param name="chip">筹码</param>
        /// <returns>是否成功</returns>
        public bool RemoveChip(ChipData chip)
        {
            if (chip == null)
                return false;

            var removed = selectedChips.RemoveAll(c => c.index == chip.index) > 0;
            
            // 如果移除的是当前选中筹码，重新选择
            if (removed && currentChip != null && currentChip.index == chip.index)
            {
                currentChip = selectedChips.Count > 0 ? selectedChips[0] : null;
            }

            return removed;
        }

        /// <summary>
        /// 替换筹码选择
        /// </summary>
        /// <param name="newChips">新的筹码列表</param>
        /// <returns>是否成功</returns>
        public bool ReplaceSelection(List<ChipData> newChips)
        {
            if (newChips == null || newChips.Count == 0 || newChips.Count > maxSelection)
                return false;

            selectedChips.Clear();
            selectedChips.AddRange(newChips.ConvertAll(c => c.Clone()));

            // 检查当前选中筹码是否还在新列表中
            if (currentChip != null && !selectedChips.Exists(c => c.index == currentChip.index))
            {
                currentChip = selectedChips.Count > 0 ? selectedChips[0] : null;
            }

            return true;
        }

        /// <summary>
        /// 验证选择是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public ChipSelectionValidation Validate()
        {
            var result = new ChipSelectionValidation();

            if (selectedChips.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add("至少需要选择一个筹码");
            }

            if (selectedChips.Count > maxSelection)
            {
                result.IsValid = false;
                result.Errors.Add($"最多只能选择{maxSelection}个筹码");
            }

            // 检查重复
            var values = selectedChips.ConvertAll(c => c.val);
            var uniqueValues = new HashSet<float>(values);
            if (values.Count != uniqueValues.Count)
            {
                result.IsValid = false;
                result.Errors.Add("不能选择相同面值的筹码");
            }

            return result;
        }

        /// <summary>
        /// 获取当前筹码面值
        /// </summary>
        /// <returns>当前筹码面值</returns>
        public float GetCurrentChipValue()
        {
            return currentChip?.val ?? 0f;
        }

        /// <summary>
        /// 是否有选中的筹码
        /// </summary>
        /// <returns>是否有选中的筹码</returns>
        public bool HasCurrentChip()
        {
            return currentChip != null;
        }
    }

    /// <summary>
    /// 筹码选择验证结果
    /// </summary>
    [System.Serializable]
    public class ChipSelectionValidation
    {
        [Tooltip("是否有效")]
        public bool IsValid = true;
        
        [Tooltip("错误信息列表")]
        public List<string> Errors = new List<string>();

        /// <summary>
        /// 获取错误信息
        /// </summary>
        /// <returns>错误信息字符串</returns>
        public string GetErrorMessage()
        {
            return string.Join("; ", Errors);
        }
    }

    /// <summary>
    /// 筹码转换结果 - 对应JavaScript项目的筹码算法结果
    /// </summary>
    [System.Serializable]
    public class ChipConversionResult
    {
        [Header("转换结果")]
        [Tooltip("筹码组合列表")]
        public List<ChipDisplayData> chips = new List<ChipDisplayData>();
        
        [Tooltip("原始金额")]
        public float originalAmount = 0f;
        
        [Tooltip("转换后总金额")]
        public float convertedAmount = 0f;
        
        [Tooltip("是否转换成功")]
        public bool success = false;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ChipConversionResult()
        {
        }

        /// <summary>
        /// 验证转换结果
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValid()
        {
            return success && Math.Abs(originalAmount - convertedAmount) < 0.01f;
        }

        /// <summary>
        /// 获取筹码数量
        /// </summary>
        /// <returns>筹码总数量</returns>
        public int GetChipCount()
        {
            return chips.Count;
        }

        /// <summary>
        /// 获取筹码类型数量
        /// </summary>
        /// <returns>筹码类型数量</returns>
        public int GetChipTypeCount()
        {
            var uniqueValues = new HashSet<float>();
            foreach (var chip in chips)
            {
                uniqueValues.Add(chip.value);
            }
            return uniqueValues.Count;
        }
    }

    /// <summary>
    /// 筹码推荐配置 - 对应JavaScript项目的推荐算法
    /// </summary>
    [System.Serializable]
    public class ChipRecommendation
    {
        [Header("推荐配置")]
        [Tooltip("用户等级")]
        public int userLevel = 1;
        
        [Tooltip("推荐的筹码面值")]
        public List<float> recommendedValues = new List<float>();

        /// <summary>
        /// 根据用户等级获取推荐筹码
        /// </summary>
        /// <param name="level">用户等级</param>
        /// <returns>推荐的筹码面值列表</returns>
        public static List<float> GetRecommendedChips(int level)
        {
            switch (level)
            {
                case 1:
                    return new List<float> { 10f, 20f, 50f, 100f, 500f };           // 新手
                case 2:
                    return new List<float> { 50f, 100f, 500f, 1000f, 5000f };       // 进阶
                case 3:
                    return new List<float> { 100f, 500f, 1000f, 5000f, 10000f };    // 高级
                case 4:
                    return new List<float> { 500f, 1000f, 5000f, 10000f, 50000f };  // VIP
                case 5:
                    return new List<float> { 1000f, 5000f, 10000f, 50000f, 100000f }; // 超级VIP
                default:
                    return new List<float> { 10f, 20f, 50f, 100f, 500f };
            }
        }

        /// <summary>
        /// 根据余额获取推荐筹码
        /// </summary>
        /// <param name="balance">用户余额</param>
        /// <returns>推荐的筹码面值列表</returns>
        public static List<float> GetRecommendedChipsByBalance(float balance)
        {
            if (balance < 100f)
            {
                return new List<float> { 1f, 5f, 10f, 20f, 50f };
            }
            else if (balance < 1000f)
            {
                return new List<float> { 10f, 20f, 50f, 100f, 200f };
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

    /// <summary>
    /// 筹码管理设置 - 对应JavaScript项目的筹码管理配置
    /// </summary>
    [System.Serializable]
    public class ChipManagerSettings
    {
        [Header("基础设置")]
        [Tooltip("是否显示筹码选择弹窗")]
        public bool showChipSelector = false;
        
        [Tooltip("自动保存用户选择")]
        public bool autoSaveSelection = true;
        
        [Tooltip("使用推荐算法")]
        public bool useRecommendation = true;

        [Header("视觉设置")]
        [Tooltip("筹码动画时长")]
        public float animationDuration = 0.3f;
        
        [Tooltip("筹码缩放比例")]
        public float chipScale = 1.0f;
        
        [Tooltip("启用筹码音效")]
        public bool enableChipSound = true;

        [Header("性能设置")]
        [Tooltip("最大显示筹码数量")]
        public int maxDisplayChips = 20;
        
        [Tooltip("筹码对象池大小")]
        public int chipPoolSize = 50;

        /// <summary>
        /// 默认设置
        /// </summary>
        /// <returns>默认设置</returns>
        public static ChipManagerSettings Default()
        {
            return new ChipManagerSettings
            {
                showChipSelector = false,
                autoSaveSelection = true,
                useRecommendation = true,
                animationDuration = 0.3f,
                chipScale = 1.0f,
                enableChipSound = true,
                maxDisplayChips = 20,
                chipPoolSize = 50
            };
        }
    }

    /// <summary>
    /// 筹码总金额管理 - 对应JavaScript项目的金额统计
    /// </summary>
    [System.Serializable]
    public class ChipTotalMoney
    {
        [Tooltip("总金额")]
        public float totalAmount = 0f;
        
        [Tooltip("筹码计数")]
        public Dictionary<float, int> chipCounts = new Dictionary<float, int>();

        /// <summary>
        /// 添加筹码
        /// </summary>
        /// <param name="chipValue">筹码面值</param>
        /// <param name="count">数量</param>
        public void AddChip(float chipValue, int count = 1)
        {
            if (chipCounts.ContainsKey(chipValue))
            {
                chipCounts[chipValue] += count;
            }
            else
            {
                chipCounts[chipValue] = count;
            }
            
            totalAmount += chipValue * count;
        }

        /// <summary>
        /// 移除筹码
        /// </summary>
        /// <param name="chipValue">筹码面值</param>
        /// <param name="count">数量</param>
        /// <returns>是否成功</returns>
        public bool RemoveChip(float chipValue, int count = 1)
        {
            if (!chipCounts.ContainsKey(chipValue) || chipCounts[chipValue] < count)
                return false;

            chipCounts[chipValue] -= count;
            if (chipCounts[chipValue] <= 0)
            {
                chipCounts.Remove(chipValue);
            }
            
            totalAmount -= chipValue * count;
            totalAmount = Math.Max(0f, totalAmount); // 确保不为负数
            
            return true;
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            totalAmount = 0f;
            chipCounts.Clear();
        }

        /// <summary>
        /// 获取总筹码数量
        /// </summary>
        /// <returns>总筹码数量</returns>
        public int GetTotalChipCount()
        {
            int total = 0;
            foreach (var count in chipCounts.Values)
            {
                total += count;
            }
            return total;
        }
    }
}