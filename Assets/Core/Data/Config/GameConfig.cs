// ================================================================================================
// 游戏基础配置 - GameConfig.cs
// 用途：管理游戏相关的配置参数，如投注限额、筹码面值等，对应JavaScript项目的游戏规则
// ================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BaccaratGame.Config
{
    /// <summary>
    /// 筹码配置数据
    /// </summary>
    [System.Serializable]
    public class ChipConfigData
    {
        [Tooltip("筹码面值")]
        public int value;
        
        [Tooltip("筹码显示文本")]
        public string displayText;
        
        [Tooltip("筹码图片资源路径")]
        public string imagePath;
        
        [Tooltip("筹码颜色")]
        public Color chipColor = Color.white;
        
        [Tooltip("是否为默认选中")]
        public bool isDefault;
        
        [Tooltip("筹码分类（用于UI分组）")]
        public ChipCategory category = ChipCategory.Regular;

        /// <summary>
        /// 筹码分类枚举
        /// </summary>
        public enum ChipCategory
        {
            Small,      // 小额筹码 (1-25)
            Regular,    // 常规筹码 (50-500)
            Large,      // 大额筹码 (1000+)
            Special     // 特殊筹码
        }
    }

    /// <summary>
    /// 投注区域配置
    /// </summary>
    [System.Serializable]
    public class BettingAreaConfig
    {
        [Tooltip("投注区域ID")]
        public string areaId;
        
        [Tooltip("显示名称")]
        public string displayName;
        
        [Tooltip("多语言Key")]
        public string localizationKey;
        
        [Tooltip("最小投注")]
        public int minBet;
        
        [Tooltip("最大投注")]
        public int maxBet;
        
        [Tooltip("赔率")]
        public float odds;
        
        [Tooltip("是否启用")]
        public bool isEnabled = true;
        
        [Tooltip("投注区域类型")]
        public BettingAreaType areaType = BettingAreaType.Main;
        
        [Tooltip("区域颜色")]
        public Color areaColor = Color.white;

        /// <summary>
        /// 投注区域类型枚举
        /// </summary>
        public enum BettingAreaType
        {
            Main,       // 主要投注区（庄、闲、和）
            Side,       // 边注区（对子）
            Special     // 特殊投注区
        }
    }

    /// <summary>
    /// 游戏阶段配置
    /// </summary>
    [System.Serializable]
    public class GamePhaseConfig
    {
        [Tooltip("阶段名称")]
        public string phaseName;
        
        [Tooltip("阶段持续时间（秒）")]
        public int duration;
        
        [Tooltip("是否允许投注")]
        public bool allowBetting;
        
        [Tooltip("是否显示倒计时")]
        public bool showCountdown;
        
        [Tooltip("阶段音效")]
        public string audioClip;
    }

    /// <summary>
    /// 游戏配置类 - 管理游戏玩法相关的配置参数
    /// 对应JavaScript项目中的游戏规则和限制设置
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Baccarat/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("💰 Betting Configuration")]
        [Tooltip("投注区域配置")]
        public List<BettingAreaConfig> bettingAreas = new List<BettingAreaConfig>
        {
            new BettingAreaConfig 
            { 
                areaId = "banker", 
                displayName = "庄", 
                localizationKey = "bet.banker",
                minBet = 10, 
                maxBet = 10000, 
                odds = 0.95f, 
                areaType = BettingAreaConfig.BettingAreaType.Main,
                areaColor = Color.red
            },
            new BettingAreaConfig 
            { 
                areaId = "player", 
                displayName = "闲", 
                localizationKey = "bet.player",
                minBet = 10, 
                maxBet = 10000, 
                odds = 1.0f, 
                areaType = BettingAreaConfig.BettingAreaType.Main,
                areaColor = Color.blue
            },
            new BettingAreaConfig 
            { 
                areaId = "tie", 
                displayName = "和", 
                localizationKey = "bet.tie",
                minBet = 10, 
                maxBet = 5000, 
                odds = 8.0f, 
                areaType = BettingAreaConfig.BettingAreaType.Main,
                areaColor = Color.green
            },
            new BettingAreaConfig 
            { 
                areaId = "bankerPair", 
                displayName = "庄对", 
                localizationKey = "bet.bankerPair",
                minBet = 5, 
                maxBet = 2000, 
                odds = 11.0f, 
                areaType = BettingAreaConfig.BettingAreaType.Side,
                areaColor = Color.yellow
            },
            new BettingAreaConfig 
            { 
                areaId = "playerPair", 
                displayName = "闲对", 
                localizationKey = "bet.playerPair",
                minBet = 5, 
                maxBet = 2000, 
                odds = 11.0f, 
                areaType = BettingAreaConfig.BettingAreaType.Side,
                areaColor = Color.cyan
            }
        };

        [Header("🎰 Chip Configuration")]
        [Tooltip("筹码配置列表")]
        public List<ChipConfigData> chipConfigs = new List<ChipConfigData>
        {
            new ChipConfigData { value = 1, displayText = "1", imagePath = "Chips/chip_1", chipColor = Color.white, isDefault = true, category = ChipConfigData.ChipCategory.Small },
            new ChipConfigData { value = 5, displayText = "5", imagePath = "Chips/chip_5", chipColor = Color.red, category = ChipConfigData.ChipCategory.Small },
            new ChipConfigData { value = 10, displayText = "10", imagePath = "Chips/chip_10", chipColor = Color.blue, category = ChipConfigData.ChipCategory.Small },
            new ChipConfigData { value = 25, displayText = "25", imagePath = "Chips/chip_25", chipColor = Color.green, category = ChipConfigData.ChipCategory.Small },
            new ChipConfigData { value = 50, displayText = "50", imagePath = "Chips/chip_50", chipColor = Color.yellow, category = ChipConfigData.ChipCategory.Regular },
            new ChipConfigData { value = 100, displayText = "100", imagePath = "Chips/chip_100", chipColor = Color.black, category = ChipConfigData.ChipCategory.Regular },
            new ChipConfigData { value = 500, displayText = "500", imagePath = "Chips/chip_500", chipColor = Color.magenta, category = ChipConfigData.ChipCategory.Regular },
            new ChipConfigData { value = 1000, displayText = "1K", imagePath = "Chips/chip_1000", chipColor = Color.cyan, category = ChipConfigData.ChipCategory.Large }
        };

        [Header("⏱️ Timing Configuration")]
        [Tooltip("游戏阶段配置")]
        public List<GamePhaseConfig> gamePhases = new List<GamePhaseConfig>
        {
            new GamePhaseConfig { phaseName = "Betting", duration = 30, allowBetting = true, showCountdown = true, audioClip = "start_betting" },
            new GamePhaseConfig { phaseName = "Dealing", duration = 15, allowBetting = false, showCountdown = true, audioClip = "dealing_cards" },
            new GamePhaseConfig { phaseName = "Result", duration = 8, allowBetting = false, showCountdown = false, audioClip = "show_result" }
        };

        [Tooltip("投注防抖间隔（毫秒）")]
        [Range(100, 1000)]
        public int betDebounceMs = 300;

        [Tooltip("确认操作防抖间隔（毫秒）")]
        [Range(500, 2000)]
        public int confirmDebounceMs = 1000;

        [Tooltip("自动确认投注延迟（秒）")]
        [Range(3, 15)]
        public int autoConfirmDelaySeconds = 5;

        [Header("🎮 Game Rules")]
        [Tooltip("是否支持免佣")]
        public bool supportCommissionFree = true;

        [Tooltip("庄家抽水比例（正常模式）")]
        [Range(0.01f, 0.1f)]
        public float bankerCommissionRate = 0.05f;

        [Tooltip("免佣模式庄家6点赔率")]
        [Range(0.1f, 1.0f)]
        public float commissionFreeBanker6Odds = 0.5f;

        [Tooltip("最大连续投注次数")]
        [Range(5, 50)]
        public int maxConsecutiveBets = 20;

        [Tooltip("单局最大投注总额")]
        [Range(1000, 100000)]
        public int maxTotalBetPerRound = 50000;

        [Tooltip("最小余额要求")]
        [Range(10, 1000)]
        public int minBalanceRequired = 50;

        [Header("🔄 Game Flow")]
        [Tooltip("是否启用快速模式")]
        public bool enableQuickMode = false;

        [Tooltip("快速模式时间倍率")]
        [Range(0.5f, 2.0f)]
        public float quickModeTimeMultiplier = 0.7f;

        [Tooltip("是否启用自动投注")]
        public bool enableAutoBet = true;

        [Tooltip("自动投注最大轮数")]
        [Range(1, 100)]
        public int maxAutoBetRounds = 10;

        [Header("🎨 UI Settings")]
        [Tooltip("闪烁效果持续时间（秒）")]
        [Range(0.5f, 3.0f)]
        public float flashEffectDuration = 2.0f;

        [Tooltip("中奖特效持续时间（秒）")]
        [Range(1.0f, 5.0f)]
        public float winEffectDuration = 3.0f;

        [Tooltip("筹码动画速度")]
        [Range(0.5f, 3.0f)]
        public float chipAnimationSpeed = 1.5f;

        [Tooltip("筹码堆叠高度限制")]
        [Range(5, 20)]
        public int maxChipStackHeight = 10;

        [Tooltip("投注区域高亮颜色")]
        public Color betAreaHighlightColor = Color.yellow;

        [Header("🔊 Audio Settings")]
        [Tooltip("是否启用音效")]
        public bool enableSoundEffects = true;

        [Tooltip("是否启用背景音乐")]
        public bool enableBackgroundMusic = false;

        [Tooltip("音效音量")]
        [Range(0.0f, 1.0f)]
        public float sfxVolume = 0.8f;

        [Tooltip("背景音乐音量")]
        [Range(0.0f, 1.0f)]
        public float bgmVolume = 0.3f;

        [Tooltip("音效淡入淡出时间")]
        [Range(0.1f, 2.0f)]
        public float audioFadeTime = 0.5f;

        [Header("📊 Statistics & History")]
        [Tooltip("历史记录保存数量")]
        [Range(10, 500)]
        public int maxHistoryRecords = 100;

        [Tooltip("是否启用统计面板")]
        public bool enableStatisticsPanel = true;

        [Tooltip("路珠显示行数")]
        [Range(5, 20)]
        public int roadmapDisplayRows = 6;

        [Tooltip("路珠显示列数")]
        [Range(10, 50)]
        public int roadmapDisplayColumns = 20;

        /// <summary>
        /// 根据投注区域ID获取配置
        /// </summary>
        public BettingAreaConfig GetBettingAreaConfig(string areaId)
        {
            return bettingAreas.Find(area => area.areaId == areaId);
        }

        /// <summary>
        /// 获取默认筹码配置
        /// </summary>
        public ChipConfigData GetDefaultChip()
        {
            return chipConfigs.Find(chip => chip.isDefault) ?? chipConfigs[0];
        }

        /// <summary>
        /// 根据面值获取筹码配置
        /// </summary>
        public ChipConfigData GetChipConfig(int value)
        {
            return chipConfigs.Find(chip => chip.value == value);
        }

        /// <summary>
        /// 根据分类获取筹码配置
        /// </summary>
        public List<ChipConfigData> GetChipsByCategory(ChipConfigData.ChipCategory category)
        {
            return chipConfigs.Where(chip => chip.category == category).ToList();
        }

        /// <summary>
        /// 获取游戏阶段配置
        /// </summary>
        public GamePhaseConfig GetGamePhaseConfig(string phaseName)
        {
            return gamePhases.Find(phase => phase.phaseName == phaseName);
        }

        /// <summary>
        /// 验证投注是否合法
        /// </summary>
        public bool ValidateBet(string areaId, int amount)
        {
            var areaConfig = GetBettingAreaConfig(areaId);
            if (areaConfig == null || !areaConfig.isEnabled)
                return false;

            return amount >= areaConfig.minBet && amount <= areaConfig.maxBet;
        }

        /// <summary>
        /// 验证总投注金额
        /// </summary>
        public bool ValidateTotalBetAmount(int totalAmount)
        {
            return totalAmount <= maxTotalBetPerRound;
        }

        /// <summary>
        /// 验证余额是否足够
        /// </summary>
        public bool ValidateBalance(float currentBalance, int betAmount)
        {
            return currentBalance >= betAmount && (currentBalance - betAmount) >= minBalanceRequired;
        }

        /// <summary>
        /// 获取投注区域的最大可投注金额
        /// </summary>
        public int GetMaxBetAmount(string areaId)
        {
            var areaConfig = GetBettingAreaConfig(areaId);
            return areaConfig?.maxBet ?? 0;
        }

        /// <summary>
        /// 获取投注区域的最小投注金额
        /// </summary>
        public int GetMinBetAmount(string areaId)
        {
            var areaConfig = GetBettingAreaConfig(areaId);
            return areaConfig?.minBet ?? 0;
        }

        /// <summary>
        /// 计算投注赔付金额
        /// </summary>
        public float CalculatePayout(string areaId, int betAmount, bool isCommissionFree = false)
        {
            var areaConfig = GetBettingAreaConfig(areaId);
            if (areaConfig == null) return 0;

            float payout = betAmount * areaConfig.odds;

            // 庄家投注需要考虑抽水
            if (areaId == "banker" && !isCommissionFree)
            {
                payout *= (1 - bankerCommissionRate);
            }

            return payout;
        }

        /// <summary>
        /// 计算免佣模式下的赔付（庄家6点）
        /// </summary>
        public float CalculateCommissionFreePayout(string areaId, int betAmount, bool isBanker6)
        {
            var areaConfig = GetBettingAreaConfig(areaId);
            if (areaConfig == null) return 0;

            if (areaId == "banker" && isBanker6)
            {
                return betAmount * commissionFreeBanker6Odds;
            }

            return betAmount * areaConfig.odds;
        }

        /// <summary>
        /// 获取调整后的游戏阶段时长（考虑快速模式）
        /// </summary>
        public int GetAdjustedPhaseDuration(string phaseName)
        {
            var phaseConfig = GetGamePhaseConfig(phaseName);
            if (phaseConfig == null) return 0;

            float duration = phaseConfig.duration;
            if (enableQuickMode)
            {
                duration *= quickModeTimeMultiplier;
            }

            return Mathf.RoundToInt(duration);
        }

        /// <summary>
        /// 获取可用的筹码面值列表
        /// </summary>
        public List<int> GetAvailableChipValues()
        {
            return chipConfigs.Select(chip => chip.value).ToList();
        }

        /// <summary>
        /// 获取按面值排序的筹码配置
        /// </summary>
        public List<ChipConfigData> GetSortedChipConfigs()
        {
            return chipConfigs.OrderBy(chip => chip.value).ToList();
        }

        /// <summary>
        /// 重置游戏配置为默认值
        /// </summary>
        public void ResetToDefaults()
        {
            betDebounceMs = 300;
            confirmDebounceMs = 1000;
            autoConfirmDelaySeconds = 5;
            supportCommissionFree = true;
            bankerCommissionRate = 0.05f;
            maxConsecutiveBets = 20;
            enableSoundEffects = true;
            sfxVolume = 0.8f;
            bgmVolume = 0.3f;
            
            Debug.Log("[GameConfig] 已重置为默认配置");
        }

        /// <summary>
        /// 从JSON字符串加载配置
        /// </summary>
        public void LoadFromJson(string jsonData)
        {
            try
            {
                JsonUtility.FromJsonOverwrite(jsonData, this);
                Debug.Log("[GameConfig] 从JSON加载配置成功");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameConfig] JSON加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出为JSON字符串
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// 获取配置摘要信息
        /// </summary>
        public string GetConfigSummary()
        {
            return $"投注区域:{bettingAreas.Count}个, 筹码类型:{chipConfigs.Count}种, 游戏阶段:{gamePhases.Count}个, 免佣:{(supportCommissionFree ? "启用" : "禁用")}";
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器中验证配置
        /// </summary>
        private void OnValidate()
        {
            // 确保至少有一个默认筹码
            bool hasDefault = chipConfigs.Exists(chip => chip.isDefault);
            if (!hasDefault && chipConfigs.Count > 0)
            {
                chipConfigs[0].isDefault = true;
            }

            // 验证投注区域配置
            foreach (var area in bettingAreas)
            {
                if (area.minBet > area.maxBet)
                {
                    Debug.LogWarning($"[GameConfig] 投注区域 {area.displayName} 的最小投注大于最大投注");
                }
            }

            // 验证游戏阶段配置
            foreach (var phase in gamePhases)
            {
                if (phase.duration <= 0)
                {
                    Debug.LogWarning($"[GameConfig] 游戏阶段 {phase.phaseName} 的持续时间无效");
                }
            }

            // 验证筹码配置
            var duplicateValues = chipConfigs.GroupBy(c => c.value).Where(g => g.Count() > 1);
            if (duplicateValues.Any())
            {
                Debug.LogWarning("[GameConfig] 存在重复的筹码面值");
            }
        }
#endif
    }
}