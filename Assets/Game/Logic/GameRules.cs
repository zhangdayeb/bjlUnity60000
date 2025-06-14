// Assets/Game/Logic/GameRules.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Data.Types;

namespace Game.Logic
{
    /// <summary>
    /// 百家乐游戏规则管理类
    /// 负责定义和验证所有百家乐游戏规则，包括投注规则、补牌规则、赔率规则等
    /// </summary>
    [CreateAssetMenu(fileName = "GameRules", menuName = "Baccarat/Game Rules")]
    public class GameRules : ScriptableObject
    {
        [Header("基础游戏规则")]
        [SerializeField] private BaccaratRuleSet ruleSet = BaccaratRuleSet.Standard;
        [SerializeField] private bool enableCommission = true;
        [SerializeField] private float commissionRate = 0.05f;  // 5%佣金
        
        [Header("投注限制")]
        [SerializeField] private BettingLimits bettingLimits = new BettingLimits();
        
        [Header("特殊规则")]
        [SerializeField] private bool enableSuper6 = false;    // 超级六
        [SerializeField] private bool enablePairBets = true;   // 对子投注
        [SerializeField] private bool enableBigSmallBets = true; // 大小投注
        [SerializeField] private bool enableLuckyBonusBets = false; // 幸运奖金投注

        [Header("补牌规则配置")]
        [SerializeField] private bool useStandardDrawRules = true;
        [SerializeField] private DrawRuleVariant drawRuleVariant = DrawRuleVariant.Standard;

        #region 常量定义

        /// <summary>
        /// 标准百家乐赔率表
        /// </summary>
        public static readonly Dictionary<BaccaratBetType, float> StandardOdds = new Dictionary<BaccaratBetType, float>
        {
            { BaccaratBetType.Banker, 0.95f },      // 庄 1:0.95 (扣佣金)
            { BaccaratBetType.Player, 1.0f },       // 闲 1:1  
            { BaccaratBetType.Tie, 8.0f },          // 和 1:8
            { BaccaratBetType.BankerPair, 11.0f },  // 庄对 1:11
            { BaccaratBetType.PlayerPair, 11.0f },  // 闲对 1:11
            { BaccaratBetType.BigBig, 0.54f },      // 大 1:0.54
            { BaccaratBetType.SmallSmall, 1.5f }    // 小 1:1.5
        };

        /// <summary>
        /// 免佣百家乐赔率表
        /// </summary>
        public static readonly Dictionary<BaccaratBetType, float> NoCommissionOdds = new Dictionary<BaccaratBetType, float>
        {
            { BaccaratBetType.Banker, 1.0f },       // 庄 1:1 (无佣金)
            { BaccaratBetType.Player, 1.0f },       // 闲 1:1
            { BaccaratBetType.Tie, 8.0f },          // 和 1:8
            { BaccaratBetType.BankerPair, 11.0f },  // 庄对 1:11
            { BaccaratBetType.PlayerPair, 11.0f },  // 闲对 1:11
            { BaccaratBetType.BigBig, 0.54f },      // 大 1:0.54
            { BaccaratBetType.SmallSmall, 1.5f }    // 小 1:1.5
        };

        /// <summary>
        /// 超级六特殊赔率
        /// </summary>
        public const float Super6BankerOdds = 0.5f;  // 庄家以6点获胜时的赔率

        #endregion

        #region 核心规则验证方法

        /// <summary>
        /// 验证投注是否符合规则
        /// </summary>
        /// <param name="bet">投注信息</param>
        /// <param name="userBalance">用户余额</param>
        /// <returns>验证结果</returns>
        public BetValidationResult ValidateBet(BaccaratBetRequest bet, float userBalance)
        {
            var result = new BetValidationResult { IsValid = true };

            try
            {
                // 1. 验证投注类型
                if (!IsValidBetType(bet.rate_id))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"无效的投注类型: {bet.rate_id}";
                    return result;
                }

                // 2. 验证投注金额
                var betType = (BaccaratBetType)bet.rate_id;
                var limits = GetBettingLimits(betType);

                if (bet.money < limits.MinBet)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"{betType}最小投注金额为 {limits.MinBet}";
                    return result;
                }

                if (bet.money > limits.MaxBet)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"{betType}最大投注金额为 {limits.MaxBet}";
                    return result;
                }

                // 3. 验证余额
                if (bet.money > userBalance)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "余额不足";
                    return result;
                }

                // 4. 验证特殊规则
                if (!ValidateSpecialRules(bet))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "不符合特殊投注规则";
                    return result;
                }

                result.ValidatedBet = bet;
                result.ApplicableOdds = GetBetOdds(betType);
                
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"投注验证异常: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 验证游戏结果是否符合规则
        /// </summary>
        /// <param name="bankerCards">庄家牌</param>
        /// <param name="playerCards">闲家牌</param>
        /// <returns>验证结果</returns>
        public GameResultValidation ValidateGameResult(List<BaccaratCard> bankerCards, List<BaccaratCard> playerCards)
        {
            var validation = new GameResultValidation { IsValid = true };

            try
            {
                // 1. 验证牌面数量
                if (bankerCards.Count < 2 || bankerCards.Count > 3)
                {
                    validation.IsValid = false;
                    validation.Errors.Add("庄家牌面数量错误");
                }

                if (playerCards.Count < 2 || playerCards.Count > 3)
                {
                    validation.IsValid = false;
                    validation.Errors.Add("闲家牌面数量错误");
                }

                // 2. 验证补牌规则
                if (!ValidateDrawCardRules(bankerCards, playerCards))
                {
                    validation.IsValid = false;
                    validation.Errors.Add("补牌规则违规");
                }

                // 3. 验证点数计算
                int bankerPoints = BaccaratLogic.CalculatePoints(bankerCards);
                int playerPoints = BaccaratLogic.CalculatePoints(playerCards);

                if (bankerPoints < 0 || bankerPoints > 9)
                {
                    validation.IsValid = false;
                    validation.Errors.Add("庄家点数计算错误");
                }

                if (playerPoints < 0 || playerPoints > 9)
                {
                    validation.IsValid = false;
                    validation.Errors.Add("闲家点数计算错误");
                }

                // 4. 验证胜负判断
                var expectedWinner = BaccaratLogic.DetermineWinner(bankerPoints, playerPoints);
                validation.ExpectedWinner = expectedWinner;
                validation.BankerPoints = bankerPoints;
                validation.PlayerPoints = playerPoints;

            }
            catch (Exception ex)
            {
                validation.IsValid = false;
                validation.Errors.Add($"验证异常: {ex.Message}");
            }

            return validation;
        }

        /// <summary>
        /// 验证补牌规则
        /// </summary>
        /// <param name="bankerCards">庄家牌</param>
        /// <param name="playerCards">闲家牌</param>
        /// <returns>是否符合规则</returns>
        public bool ValidateDrawCardRules(List<BaccaratCard> bankerCards, List<BaccaratCard> playerCards)
        {
            if (!useStandardDrawRules) return true;

            try
            {
                // 模拟正确的补牌过程
                var initialBankerCards = bankerCards.Take(2).ToList();
                var initialPlayerCards = playerCards.Take(2).ToList();

                // 检查是否有天牌
                bool bankerNatural = BaccaratLogic.IsNatural(initialBankerCards);
                bool playerNatural = BaccaratLogic.IsNatural(initialPlayerCards);

                if (bankerNatural || playerNatural)
                {
                    // 有天牌时不应该补牌
                    return bankerCards.Count == 2 && playerCards.Count == 2;
                }

                // 验证闲家补牌
                int playerInitialPoints = BaccaratLogic.CalculatePoints(initialPlayerCards);
                bool playerShouldDraw = playerInitialPoints <= 5;
                bool playerActuallyDrew = playerCards.Count == 3;

                if (playerShouldDraw != playerActuallyDrew)
                {
                    return false;
                }

                // 验证庄家补牌
                int bankerInitialPoints = BaccaratLogic.CalculatePoints(initialBankerCards);
                bool bankerShouldDraw = ShouldBankerDraw(bankerInitialPoints, playerCards);
                bool bankerActuallyDrew = bankerCards.Count == 3;

                return bankerShouldDraw == bankerActuallyDrew;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 赔率计算方法

        /// <summary>
        /// 获取投注赔率
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <param name="gameResult">游戏结果（可选）</param>
        /// <returns>赔率</returns>
        public float GetBetOdds(BaccaratBetType betType, BaccaratGameResult gameResult = null)
        {
            // 处理免佣庄家的特殊情况
            if (betType == BaccaratBetType.Banker && !enableCommission)
            {
                // 免佣庄家：庄家以6点获胜时赔率不同
                if (enableSuper6 && gameResult != null && 
                    gameResult.winner == BaccaratWinner.Banker && 
                    gameResult.banker_points == 6)
                {
                    return Super6BankerOdds;
                }
                return NoCommissionOdds[betType];
            }

            // 标准赔率
            var oddsTable = enableCommission ? StandardOdds : NoCommissionOdds;
            return oddsTable.ContainsKey(betType) ? oddsTable[betType] : 0f;
        }

        /// <summary>
        /// 计算实际赔付金额
        /// </summary>
        /// <param name="bet">投注</param>
        /// <param name="gameResult">游戏结果</param>
        /// <returns>赔付信息</returns>
        public PayoutInfo CalculatePayout(BaccaratBetRequest bet, BaccaratGameResult gameResult)
        {
            var payout = new PayoutInfo
            {
                betType = (BaccaratBetType)bet.rate_id,
                betAmount = bet.money,
                isWin = BaccaratLogic.IsBetWinning(bet.rate_id, gameResult)
            };

            if (payout.isWin)
            {
                payout.odds = GetBetOdds(payout.betType, gameResult);
                payout.grossPayout = bet.money * (1 + payout.odds);
                
                // 计算佣金
                if (enableCommission && payout.betType == BaccaratBetType.Banker)
                {
                    payout.commission = bet.money * payout.odds * commissionRate;
                    payout.netPayout = payout.grossPayout - payout.commission;
                }
                else
                {
                    payout.commission = 0f;
                    payout.netPayout = payout.grossPayout;
                }
                
                payout.profit = payout.netPayout - bet.money;
            }
            else
            {
                payout.odds = 0f;
                payout.grossPayout = 0f;
                payout.commission = 0f;
                payout.netPayout = 0f;
                payout.profit = -bet.money;
            }

            return payout;
        }

        #endregion

        #region 游戏流程规则

        /// <summary>
        /// 获取游戏阶段规则
        /// </summary>
        /// <param name="phase">游戏阶段</param>
        /// <returns>阶段规则</returns>
        public PhaseRule GetPhaseRule(BaccaratGamePhase phase)
        {
            return phase switch
            {
                BaccaratGamePhase.Waiting => new PhaseRule
                {
                    allowBetting = false,
                    allowCancelBets = false,
                    duration = 10f,
                    description = "等待开始"
                },
                BaccaratGamePhase.Betting => new PhaseRule
                {
                    allowBetting = true,
                    allowCancelBets = true,
                    duration = 20f,
                    description = "投注阶段"
                },
                BaccaratGamePhase.Dealing => new PhaseRule
                {
                    allowBetting = false,
                    allowCancelBets = false,
                    duration = 15f,
                    description = "发牌阶段"
                },
                BaccaratGamePhase.Result => new PhaseRule
                {
                    allowBetting = false,
                    allowCancelBets = false,
                    duration = 10f,
                    description = "结果显示"
                },
                _ => new PhaseRule { allowBetting = false, allowCancelBets = false }
            };
        }

        /// <summary>
        /// 检查是否可以进入下一阶段
        /// </summary>
        /// <param name="currentPhase">当前阶段</param>
        /// <param name="elapsed">已用时间</param>
        /// <param name="hasActiveBets">是否有活跃投注</param>
        /// <returns>是否可以进入下一阶段</returns>
        public bool CanAdvanceToNextPhase(BaccaratGamePhase currentPhase, float elapsed, bool hasActiveBets)
        {
            var rule = GetPhaseRule(currentPhase);
            
            return currentPhase switch
            {
                BaccaratGamePhase.Waiting => elapsed >= rule.duration,
                BaccaratGamePhase.Betting => elapsed >= rule.duration || !hasActiveBets,
                BaccaratGamePhase.Dealing => elapsed >= rule.duration,
                BaccaratGamePhase.Result => elapsed >= rule.duration,
                _ => true
            };
        }

        #endregion

        #region 特殊规则处理

        /// <summary>
        /// 验证特殊规则
        /// </summary>
        /// <param name="bet">投注</param>
        /// <returns>是否符合特殊规则</returns>
        public bool ValidateSpecialRules(BaccaratBetRequest bet)
        {
            var betType = (BaccaratBetType)bet.rate_id;

            // 检查对子投注是否启用
            if ((betType == BaccaratBetType.BankerPair || betType == BaccaratBetType.PlayerPair) && !enablePairBets)
            {
                return false;
            }

            // 检查大小投注是否启用
            if ((betType == BaccaratBetType.BigBig || betType == BaccaratBetType.SmallSmall) && !enableBigSmallBets)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 处理超级六规则
        /// </summary>
        /// <param name="gameResult">游戏结果</param>
        /// <returns>是否触发超级六</returns>
        public bool ProcessSuper6Rule(BaccaratGameResult gameResult)
        {
            if (!enableSuper6) return false;

            return gameResult.winner == BaccaratWinner.Banker && 
                   gameResult.banker_points == 6;
        }

        /// <summary>
        /// 获取幸运奖金规则
        /// </summary>
        /// <param name="gameResult">游戏结果</param>
        /// <returns>幸运奖金信息</returns>
        public LuckyBonusInfo GetLuckyBonusInfo(BaccaratGameResult gameResult)
        {
            var bonusInfo = new LuckyBonusInfo { hasBonus = false };

            if (!enableLuckyBonusBets) return bonusInfo;

            // 示例：连续对子触发幸运奖金
            if (gameResult.banker_pair && gameResult.player_pair)
            {
                bonusInfo.hasBonus = true;
                bonusInfo.bonusType = "双对奖金";
                bonusInfo.bonusMultiplier = 25f;
                bonusInfo.description = "庄对和闲对同时出现";
            }

            return bonusInfo;
        }

        #endregion

        #region 限制和配置方法

        /// <summary>
        /// 获取投注限制
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <returns>投注限制</returns>
        public BetLimitInfo GetBettingLimits(BaccaratBetType betType)
        {
            return betType switch
            {
                BaccaratBetType.Banker => new BetLimitInfo 
                { 
                    MinBet = bettingLimits.bankerMin, 
                    MaxBet = bettingLimits.bankerMax 
                },
                BaccaratBetType.Player => new BetLimitInfo 
                { 
                    MinBet = bettingLimits.playerMin, 
                    MaxBet = bettingLimits.playerMax 
                },
                BaccaratBetType.Tie => new BetLimitInfo 
                { 
                    MinBet = bettingLimits.tieMin, 
                    MaxBet = bettingLimits.tieMax 
                },
                BaccaratBetType.BankerPair or BaccaratBetType.PlayerPair => new BetLimitInfo 
                { 
                    MinBet = bettingLimits.pairMin, 
                    MaxBet = bettingLimits.pairMax 
                },
                BaccaratBetType.BigBig or BaccaratBetType.SmallSmall => new BetLimitInfo 
                { 
                    MinBet = bettingLimits.bigSmallMin, 
                    MaxBet = bettingLimits.bigSmallMax 
                },
                _ => new BetLimitInfo { MinBet = 1f, MaxBet = 1000f }
            };
        }

        /// <summary>
        /// 设置自定义规则集
        /// </summary>
        /// <param name="customRules">自定义规则</param>
        public void SetCustomRules(CustomRuleSet customRules)
        {
            if (customRules.overrideOdds != null && customRules.overrideOdds.Count > 0)
            {
                // 应用自定义赔率（此处可以扩展实现）
                Debug.Log("应用自定义赔率规则");
            }

            if (customRules.overrideLimits != null)
            {
                bettingLimits = customRules.overrideLimits;
                Debug.Log("应用自定义投注限制");
            }

            enableSuper6 = customRules.enableSuper6;
            enablePairBets = customRules.enablePairBets;
            enableBigSmallBets = customRules.enableBigSmallBets;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查投注类型是否有效
        /// </summary>
        /// <param name="betTypeId">投注类型ID</param>
        /// <returns>是否有效</returns>
        private bool IsValidBetType(int betTypeId)
        {
            return betTypeId >= 1 && betTypeId <= 7 && Enum.IsDefined(typeof(BaccaratBetType), betTypeId);
        }

        /// <summary>
        /// 庄家是否应该补牌
        /// </summary>
        /// <param name="bankerPoints">庄家初始点数</param>
        /// <param name="playerCards">闲家所有牌</param>
        /// <returns>是否应该补牌</returns>
        private bool ShouldBankerDraw(int bankerPoints, List<BaccaratCard> playerCards)
        {
            if (playerCards.Count == 2) // 闲家没有补牌
            {
                return bankerPoints <= 5;
            }
            else if (playerCards.Count == 3) // 闲家已补牌
            {
                int playerThirdCard = BaccaratLogic.GetCardBaccaratValue(playerCards[2]);
                
                return bankerPoints switch
                {
                    0 or 1 or 2 => true,
                    3 => playerThirdCard != 8,
                    4 => playerThirdCard >= 2 && playerThirdCard <= 7,
                    5 => playerThirdCard >= 4 && playerThirdCard <= 7,
                    6 => playerThirdCard == 6 || playerThirdCard == 7,
                    _ => false
                };
            }

            return false;
        }

        /// <summary>
        /// 获取规则集描述
        /// </summary>
        /// <returns>规则描述</returns>
        public string GetRuleSetDescription()
        {
            var description = $"规则集: {ruleSet}\n";
            description += $"佣金: {(enableCommission ? $"{commissionRate:P1}" : "无佣金")}\n";
            description += $"超级六: {(enableSuper6 ? "启用" : "禁用")}\n";
            description += $"对子投注: {(enablePairBets ? "启用" : "禁用")}\n";
            description += $"大小投注: {(enableBigSmallBets ? "启用" : "禁用")}\n";
            description += $"补牌规则: {drawRuleVariant}";
            
            return description;
        }

        #endregion

        #region 验证和诊断方法

        /// <summary>
        /// 验证规则配置
        /// </summary>
        /// <returns>验证结果</returns>
        public RuleValidationResult ValidateRuleConfiguration()
        {
            var result = new RuleValidationResult { IsValid = true };

            // 验证佣金率
            if (enableCommission && (commissionRate < 0 || commissionRate > 0.1f))
            {
                result.IsValid = false;
                result.Warnings.Add("佣金率应在0-10%之间");
            }

            // 验证投注限制
            if (bettingLimits.bankerMin >= bettingLimits.bankerMax)
            {
                result.IsValid = false;
                result.Errors.Add("庄家投注限制配置错误");
            }

            // 验证特殊规则组合
            if (enableSuper6 && enableCommission)
            {
                result.Warnings.Add("超级六通常与免佣庄家搭配使用");
            }

            return result;
        }

        /// <summary>
        /// 获取规则统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public RuleStatistics GetRuleStatistics()
        {
            return new RuleStatistics
            {
                totalBetTypes = Enum.GetValues(typeof(BaccaratBetType)).Length,
                enabledBetTypes = GetEnabledBetTypes().Count,
                averageOdds = CalculateAverageOdds(),
                houseEdge = CalculateHouseEdge(),
                ruleComplexity = CalculateRuleComplexity()
            };
        }

        /// <summary>
        /// 获取启用的投注类型
        /// </summary>
        private List<BaccaratBetType> GetEnabledBetTypes()
        {
            var enabled = new List<BaccaratBetType>
            {
                BaccaratBetType.Banker,
                BaccaratBetType.Player,
                BaccaratBetType.Tie
            };

            if (enablePairBets)
            {
                enabled.Add(BaccaratBetType.BankerPair);
                enabled.Add(BaccaratBetType.PlayerPair);
            }

            if (enableBigSmallBets)
            {
                enabled.Add(BaccaratBetType.BigBig);
                enabled.Add(BaccaratBetType.SmallSmall);
            }

            return enabled;
        }

        /// <summary>
        /// 计算平均赔率
        /// </summary>
        private float CalculateAverageOdds()
        {
            var oddsTable = enableCommission ? StandardOdds : NoCommissionOdds;
            return oddsTable.Values.Average();
        }

        /// <summary>
        /// 计算庄家优势
        /// </summary>
        private float CalculateHouseEdge()
        {
            // 简化计算，实际应该基于概率分布
            return enableCommission ? 0.0106f : 0.0124f; // 庄家投注的庄家优势
        }

        /// <summary>
        /// 计算规则复杂度
        /// </summary>
        private int CalculateRuleComplexity()
        {
            int complexity = 1; // 基础复杂度
            
            if (enableCommission) complexity++;
            if (enableSuper6) complexity++;
            if (enablePairBets) complexity++;
            if (enableBigSmallBets) complexity++;
            if (enableLuckyBonusBets) complexity += 2;
            
            return complexity;
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 百家乐规则集类型
    /// </summary>
    public enum BaccaratRuleSet
    {
        Standard,           // 标准规则
        NoCommission,       // 免佣规则  
        Super6,             // 超级六规则
        Speed,              // 速度百家乐
        Mini,               // 迷你百家乐
        Custom              // 自定义规则
    }

    /// <summary>
    /// 补牌规则变体
    /// </summary>
    public enum DrawRuleVariant
    {
        Standard,           // 标准补牌规则
        Simplified,         // 简化规则
        NoThirdCard,        // 无第三张牌
        AlwaysDraw          // 总是补牌
    }

    /// <summary>
    /// 投注限制配置
    /// </summary>
    [System.Serializable]
    public class BettingLimits
    {
        [Header("主要投注限制")]
        public float bankerMin = 10f;
        public float bankerMax = 50000f;
        public float playerMin = 10f;
        public float playerMax = 50000f;
        public float tieMin = 10f;
        public float tieMax = 10000f;

        [Header("副投注限制")]
        public float pairMin = 5f;
        public float pairMax = 5000f;
        public float bigSmallMin = 10f;
        public float bigSmallMax = 20000f;

        [Header("桌台限制")]
        public float tableMin = 10f;
        public float tableMax = 100000f;
        public float maxTotalBet = 200000f;
    }

    /// <summary>
    /// 投注验证结果
    /// </summary>
    [System.Serializable]
    public class BetValidationResult
    {
        public bool IsValid;
        public string ErrorMessage;
        public BaccaratBetRequest ValidatedBet;
        public float ApplicableOdds;
    }

    /// <summary>
    /// 游戏结果验证
    /// </summary>
    [System.Serializable]
    public class GameResultValidation
    {
        public bool IsValid = true;
        public List<string> Errors = new List<string>();
        public BaccaratWinner ExpectedWinner;
        public int BankerPoints;
        public int PlayerPoints;
    }

    /// <summary>
    /// 赔付信息
    /// </summary>
    [System.Serializable]
    public class PayoutInfo
    {
        public BaccaratBetType betType;
        public float betAmount;
        public bool isWin;
        public float odds;
        public float grossPayout;
        public float commission;
        public float netPayout;
        public float profit;
    }

    /// <summary>
    /// 阶段规则
    /// </summary>
    [System.Serializable]
    public class PhaseRule
    {
        public bool allowBetting;
        public bool allowCancelBets;
        public float duration;
        public string description;
    }

    /// <summary>
    /// 投注限制信息
    /// </summary>
    [System.Serializable]
    public class BetLimitInfo
    {
        public float MinBet;
        public float MaxBet;
    }

    /// <summary>
    /// 幸运奖金信息
    /// </summary>
    [System.Serializable]
    public class LuckyBonusInfo
    {
        public bool hasBonus;
        public string bonusType;
        public float bonusMultiplier;
        public string description;
    }

    /// <summary>
    /// 自定义规则集
    /// </summary>
    [System.Serializable]
    public class CustomRuleSet
    {
        public Dictionary<BaccaratBetType, float> overrideOdds;
        public BettingLimits overrideLimits;
        public bool enableSuper6;
        public bool enablePairBets;
        public bool enableBigSmallBets;
        public bool enableLuckyBonusBets;
    }

    /// <summary>
    /// 规则验证结果
    /// </summary>
    [System.Serializable]
    public class RuleValidationResult
    {
        public bool IsValid = true;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// 规则统计信息
    /// </summary>
    [System.Serializable]
    public class RuleStatistics
    {
        public int totalBetTypes;
        public int enabledBetTypes;
        public float averageOdds;
        public float houseEdge;
        public int ruleComplexity;
    }

    #endregion