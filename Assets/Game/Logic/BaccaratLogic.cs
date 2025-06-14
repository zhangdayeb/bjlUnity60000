// Assets/Game/Logic/BaccaratLogic.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Data.Types;

namespace Game.Logic
{
    /// <summary>
    /// 百家乐核心逻辑类
    /// 负责处理百家乐游戏的所有业务逻辑，包括牌型计算、胜负判断、赔率计算等
    /// </summary>
    public static class BaccaratLogic
    {
        #region 常量定义

        /// <summary>
        /// 百家乐赔率表 - 对应实际游戏规则
        /// </summary>
        public static readonly Dictionary<BaccaratBetType, float> BetOdds = new Dictionary<BaccaratBetType, float>
        {
            { BaccaratBetType.Banker, 0.95f },      // 庄家 1:0.95 (扣5%佣金)
            { BaccaratBetType.Player, 1.0f },       // 闲家 1:1
            { BaccaratBetType.Tie, 8.0f },          // 和局 1:8
            { BaccaratBetType.BankerPair, 11.0f },  // 庄对 1:11
            { BaccaratBetType.PlayerPair, 11.0f },  // 闲对 1:11
            { BaccaratBetType.BigBig, 0.54f },      // 大 1:0.54
            { BaccaratBetType.SmallSmall, 1.5f }    // 小 1:1.5
        };

        /// <summary>
        /// 免佣庄家赔率
        /// </summary>
        public const float BankerNoCommissionOdds = 1.0f;

        /// <summary>
        /// 超级六的赔率（庄家以6点获胜时）
        /// </summary>
        public const float Super6Odds = 0.5f;

        #endregion

        #region 核心计算方法

        /// <summary>
        /// 计算牌面点数 - 百家乐核心算法
        /// </summary>
        /// <param name="cards">牌面列表</param>
        /// <returns>百家乐点数（0-9）</returns>
        public static int CalculatePoints(List<BaccaratCard> cards)
        {
            if (cards == null || cards.Count == 0)
                return 0;

            int totalPoints = 0;
            foreach (var card in cards)
            {
                totalPoints += GetCardBaccaratValue(card);
            }

            // 百家乐规则：总点数取个位数
            return totalPoints % 10;
        }

        /// <summary>
        /// 获取单张牌的百家乐点数
        /// </summary>
        /// <param name="card">牌面</param>
        /// <returns>百家乐点数</returns>
        public static int GetCardBaccaratValue(BaccaratCard card)
        {
            if (card.rank == 1) return 1;      // A = 1点
            if (card.rank >= 10) return 0;     // 10, J, Q, K = 0点
            return card.rank;                   // 2-9 = 面值
        }

        /// <summary>
        /// 判断游戏胜负 - 百家乐胜负判断核心逻辑
        /// </summary>
        /// <param name="bankerCards">庄家牌</param>
        /// <param name="playerCards">闲家牌</param>
        /// <returns>游戏结果</returns>
        public static BaccaratGameResult DetermineGameResult(List<BaccaratCard> bankerCards, List<BaccaratCard> playerCards)
        {
            var result = new BaccaratGameResult
            {
                banker_cards = new List<BaccaratCard>(bankerCards),
                player_cards = new List<BaccaratCard>(playerCards),
                game_number = GenerateGameNumber(),
                result_time = DateTime.UtcNow
            };

            // 1. 计算双方点数
            result.banker_points = CalculatePoints(bankerCards);
            result.player_points = CalculatePoints(playerCards);

            // 2. 判断胜负
            result.winner = DetermineWinner(result.banker_points, result.player_points);

            // 3. 检查对子
            result.banker_pair = CheckPair(bankerCards);
            result.player_pair = CheckPair(playerCards);

            // 4. 检查大小
            int totalCards = bankerCards.Count + playerCards.Count;
            result.is_big = totalCards >= 5;    // 5张或6张牌为大
            
            // 5. 生成中奖投注类型
            result.winning_bets = GenerateWinningBets(result);

            // 6. 生成闪烁区域
            result.flash_areas = GenerateFlashAreas(result);

            return result;
        }

        /// <summary>
        /// 判断获胜方
        /// </summary>
        /// <param name="bankerPoints">庄家点数</param>
        /// <param name="playerPoints">闲家点数</param>
        /// <returns>获胜方</returns>
        public static BaccaratWinner DetermineWinner(int bankerPoints, int playerPoints)
        {
            if (bankerPoints > playerPoints)
                return BaccaratWinner.Banker;
            else if (playerPoints > bankerPoints)
                return BaccaratWinner.Player;
            else
                return BaccaratWinner.Tie;
        }

        /// <summary>
        /// 检查是否为对子
        /// </summary>
        /// <param name="cards">牌面列表</param>
        /// <returns>是否为对子</returns>
        public static bool CheckPair(List<BaccaratCard> cards)
        {
            if (cards == null || cards.Count < 2)
                return false;

            // 前两张牌点数相同即为对子
            return cards[0].rank == cards[1].rank;
        }

        /// <summary>
        /// 检查是否为天牌（前两张牌点数为8或9）
        /// </summary>
        /// <param name="cards">牌面列表</param>
        /// <returns>是否为天牌</returns>
        public static bool IsNatural(List<BaccaratCard> cards)
        {
            if (cards == null || cards.Count < 2)
                return false;

            int points = CalculatePoints(cards.Take(2).ToList());
            return points == 8 || points == 9;
        }

        /// <summary>
        /// 检查是否需要补牌 - 根据百家乐标准规则
        /// </summary>
        /// <param name="bankerCards">庄家牌</param>
        /// <param name="playerCards">闲家牌</param>
        /// <returns>补牌信息</returns>
        public static DrawCardInfo CheckDrawCard(List<BaccaratCard> bankerCards, List<BaccaratCard> playerCards)
        {
            var drawInfo = new DrawCardInfo();

            // 如果任一方有天牌，则不补牌
            if (IsNatural(bankerCards) || IsNatural(playerCards))
            {
                drawInfo.shouldDraw = false;
                drawInfo.reason = "有天牌，不需补牌";
                return drawInfo;
            }

            int bankerPoints = CalculatePoints(bankerCards.Take(2).ToList());
            int playerPoints = CalculatePoints(playerCards.Take(2).ToList());

            // 闲家补牌规则
            if (playerPoints <= 5)
            {
                drawInfo.playerShouldDraw = true;
                drawInfo.reason += "闲家点数≤5，需要补牌; ";
            }

            // 庄家补牌规则（复杂规则）
            if (playerCards.Count == 2) // 闲家没有补牌
            {
                if (bankerPoints <= 5)
                {
                    drawInfo.bankerShouldDraw = true;
                    drawInfo.reason += "闲家未补牌，庄家点数≤5，需要补牌";
                }
            }
            else if (playerCards.Count == 3) // 闲家已补牌
            {
                int playerThirdCard = GetCardBaccaratValue(playerCards[2]);
                drawInfo.bankerShouldDraw = ShouldBankerDrawWithPlayerThirdCard(bankerPoints, playerThirdCard);
                
                if (drawInfo.bankerShouldDraw)
                {
                    drawInfo.reason += $"闲家第三张牌为{playerThirdCard}，庄家需要补牌";
                }
            }

            drawInfo.shouldDraw = drawInfo.playerShouldDraw || drawInfo.bankerShouldDraw;
            return drawInfo;
        }

        #endregion

        #region 投注计算方法

        /// <summary>
        /// 计算投注赔付 - 核心赔付计算算法
        /// </summary>
        /// <param name="bets">投注列表</param>
        /// <param name="gameResult">游戏结果</param>
        /// <param name="isExemptEnabled">是否启用免佣</param>
        /// <returns>赔付计算结果</returns>
        public static PayoutCalculationResult CalculatePayout(List<BaccaratBetRequest> bets, BaccaratGameResult gameResult, bool isExemptEnabled = false)
        {
            var result = new PayoutCalculationResult
            {
                totalBetAmount = 0f,
                totalPayout = 0f,
                netProfit = 0f,
                betResults = new List<BetPayoutResult>()
            };

            foreach (var bet in bets)
            {
                var betResult = CalculateSingleBetPayout(bet, gameResult, isExemptEnabled);
                result.betResults.Add(betResult);
                result.totalBetAmount += bet.money;
                result.totalPayout += betResult.payoutAmount;
            }

            result.netProfit = result.totalPayout - result.totalBetAmount;
            return result;
        }

        /// <summary>
        /// 计算单个投注的赔付
        /// </summary>
        /// <param name="bet">投注</param>
        /// <param name="gameResult">游戏结果</param>
        /// <param name="isExemptEnabled">是否免佣</param>
        /// <returns>单个投注赔付结果</returns>
        public static BetPayoutResult CalculateSingleBetPayout(BaccaratBetRequest bet, BaccaratGameResult gameResult, bool isExemptEnabled = false)
        {
            var result = new BetPayoutResult
            {
                betType = (BaccaratBetType)bet.rate_id,
                betAmount = bet.money,
                isWin = false,
                payoutAmount = 0f,
                odds = 0f
            };

            bool isWin = IsBetWinning(bet.rate_id, gameResult);
            result.isWin = isWin;

            if (isWin)
            {
                result.odds = GetBetOdds((BaccaratBetType)bet.rate_id, gameResult, isExemptEnabled);
                result.payoutAmount = bet.money * (1 + result.odds);
                result.description = $"中奖！获得 {result.payoutAmount:F2} 元";
            }
            else
            {
                result.payoutAmount = 0f;
                result.description = "未中奖";
            }

            return result;
        }

        /// <summary>
        /// 判断投注是否中奖
        /// </summary>
        /// <param name="betTypeId">投注类型ID</param>
        /// <param name="gameResult">游戏结果</param>
        /// <returns>是否中奖</returns>
        public static bool IsBetWinning(int betTypeId, BaccaratGameResult gameResult)
        {
            return betTypeId switch
            {
                1 => gameResult.winner == BaccaratWinner.Banker,        // 庄
                2 => gameResult.winner == BaccaratWinner.Player,        // 闲
                3 => gameResult.winner == BaccaratWinner.Tie,           // 和
                4 => gameResult.banker_pair,                            // 庄对
                5 => gameResult.player_pair,                            // 闲对
                6 => gameResult.is_big,                                 // 大
                7 => !gameResult.is_big,                                // 小
                _ => false
            };
        }

        /// <summary>
        /// 获取投注赔率
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <param name="gameResult">游戏结果</param>
        /// <param name="isExemptEnabled">是否免佣</param>
        /// <returns>赔率</returns>
        public static float GetBetOdds(BaccaratBetType betType, BaccaratGameResult gameResult, bool isExemptEnabled = false)
        {
            // 特殊处理免佣庄家
            if (betType == BaccaratBetType.Banker && isExemptEnabled)
            {
                // 免佣庄家：庄家以6点获胜时赔率0.5，其他情况赔率1.0
                if (gameResult.winner == BaccaratWinner.Banker && gameResult.banker_points == 6)
                {
                    return Super6Odds;
                }
                else
                {
                    return BankerNoCommissionOdds;
                }
            }

            // 标准赔率
            return BetOdds.ContainsKey(betType) ? BetOdds[betType] : 0f;
        }

        #endregion

        #region 高级分析方法

        /// <summary>
        /// 分析游戏趋势 - 用于路纸和预测
        /// </summary>
        /// <param name="historyResults">历史结果</param>
        /// <returns>趋势分析</returns>
        public static TrendAnalysis AnalyzeTrend(List<BaccaratGameResult> historyResults)
        {
            var analysis = new TrendAnalysis();

            if (historyResults == null || historyResults.Count == 0)
                return analysis;

            // 统计基础数据
            analysis.totalGames = historyResults.Count;
            analysis.bankerWins = historyResults.Count(r => r.winner == BaccaratWinner.Banker);
            analysis.playerWins = historyResults.Count(r => r.winner == BaccaratWinner.Player);
            analysis.tieWins = historyResults.Count(r => r.winner == BaccaratWinner.Tie);

            // 计算胜率
            analysis.bankerWinRate = (float)analysis.bankerWins / analysis.totalGames;
            analysis.playerWinRate = (float)analysis.playerWins / analysis.totalGames;
            analysis.tieWinRate = (float)analysis.tieWins / analysis.totalGames;

            // 分析连胜情况
            analysis.currentStreak = CalculateCurrentStreak(historyResults);
            analysis.maxBankerStreak = CalculateMaxStreak(historyResults, BaccaratWinner.Banker);
            analysis.maxPlayerStreak = CalculateMaxStreak(historyResults, BaccaratWinner.Player);

            // 对子统计
            analysis.bankerPairCount = historyResults.Count(r => r.banker_pair);
            analysis.playerPairCount = historyResults.Count(r => r.player_pair);
            analysis.bankerPairRate = (float)analysis.bankerPairCount / analysis.totalGames;
            analysis.playerPairRate = (float)analysis.playerPairCount / analysis.totalGames;

            return analysis;
        }

        /// <summary>
        /// 生成路纸珠子
        /// </summary>
        /// <param name="gameResult">游戏结果</param>
        /// <returns>路纸珠子</returns>
        public static RoadmapBead GenerateRoadmapBead(BaccaratGameResult gameResult)
        {
            return new RoadmapBead
            {
                result = gameResult.winner.ToString(),
                banker_pair = gameResult.banker_pair,
                player_pair = gameResult.player_pair,
                banker_points = gameResult.banker_points,
                player_points = gameResult.player_points,
                game_number = gameResult.game_number,
                timestamp = gameResult.result_time
            };
        }

        /// <summary>
        /// 预测下局结果（基于历史趋势）
        /// </summary>
        /// <param name="historyResults">历史结果</param>
        /// <returns>预测结果</returns>
        public static BaccaratPrediction PredictNextGame(List<BaccaratGameResult> historyResults)
        {
            var prediction = new BaccaratPrediction
            {
                prediction_method = "历史趋势分析",
                reasoning = new List<string>()
            };

            if (historyResults == null || historyResults.Count < 5)
            {
                prediction.predicted_winner = BaccaratWinner.Banker;
                prediction.confidence = 0.5f;
                prediction.reasoning.Add("历史数据不足，使用默认预测");
                return prediction;
            }

            var trend = AnalyzeTrend(historyResults);
            var currentStreak = CalculateCurrentStreak(historyResults);

            // 基于胜率预测
            if (trend.bankerWinRate > trend.playerWinRate)
            {
                prediction.predicted_winner = BaccaratWinner.Banker;
                prediction.reasoning.Add($"庄家胜率较高: {trend.bankerWinRate:P1}");
            }
            else
            {
                prediction.predicted_winner = BaccaratWinner.Player;
                prediction.reasoning.Add($"闲家胜率较高: {trend.playerWinRate:P1}");
            }

            // 连胜反转分析
            if (Math.Abs(currentStreak.count) >= 3)
            {
                var oppositeWinner = currentStreak.winner == BaccaratWinner.Banker ? 
                                   BaccaratWinner.Player : BaccaratWinner.Banker;
                prediction.predicted_winner = oppositeWinner;
                prediction.reasoning.Add($"连胜{Math.Abs(currentStreak.count)}局，可能反转");
                prediction.is_streak_likely = false;
            }
            else
            {
                prediction.is_streak_likely = true;
            }

            // 计算置信度
            float trendStrength = Math.Abs(trend.bankerWinRate - trend.playerWinRate);
            prediction.confidence = Math.Min(0.8f, 0.5f + trendStrength);

            // 设置概率
            prediction.banker_probability = trend.bankerWinRate;
            prediction.player_probability = trend.playerWinRate;
            prediction.tie_probability = trend.tieWinRate;

            prediction.trend_analysis = $"庄胜{trend.bankerWins}局({trend.bankerWinRate:P1}), " +
                                      $"闲胜{trend.playerWins}局({trend.playerWinRate:P1}), " +
                                      $"和{trend.tieWins}局({trend.tieWinRate:P1})";

            return prediction;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 庄家补牌规则（根据闲家第三张牌）
        /// </summary>
        private static bool ShouldBankerDrawWithPlayerThirdCard(int bankerPoints, int playerThirdCard)
        {
            return bankerPoints switch
            {
                0 or 1 or 2 => true,                           // 0-2点必须补牌
                3 => playerThirdCard != 8,                      // 3点时闲家第三张不是8就补牌
                4 => playerThirdCard >= 2 && playerThirdCard <= 7,  // 4点时闲家第三张是2-7就补牌
                5 => playerThirdCard >= 4 && playerThirdCard <= 7,  // 5点时闲家第三张是4-7就补牌
                6 => playerThirdCard == 6 || playerThirdCard == 7,  // 6点时闲家第三张是6-7就补牌
                _ => false                                      // 7-9点不补牌
            };
        }

        /// <summary>
        /// 生成中奖投注类型
        /// </summary>
        private static List<string> GenerateWinningBets(BaccaratGameResult result)
        {
            var winningBets = new List<string>();

            // 主要投注
            switch (result.winner)
            {
                case BaccaratWinner.Banker:
                    winningBets.Add("庄");
                    break;
                case BaccaratWinner.Player:
                    winningBets.Add("闲");
                    break;
                case BaccaratWinner.Tie:
                    winningBets.Add("和");
                    break;
            }

            // 对子投注
            if (result.banker_pair)
                winningBets.Add("庄对");
            if (result.player_pair)
                winningBets.Add("闲对");

            // 大小投注
            if (result.is_big)
                winningBets.Add("大");
            else
                winningBets.Add("小");

            return winningBets;
        }

        /// <summary>
        /// 生成闪烁区域
        /// </summary>
        private static List<int> GenerateFlashAreas(BaccaratGameResult result)
        {
            var flashAreas = new List<int>();

            // 主要区域闪烁
            switch (result.winner)
            {
                case BaccaratWinner.Banker:
                    flashAreas.Add(1);
                    break;
                case BaccaratWinner.Player:
                    flashAreas.Add(2);
                    break;
                case BaccaratWinner.Tie:
                    flashAreas.Add(3);
                    break;
            }

            // 对子区域闪烁
            if (result.banker_pair)
                flashAreas.Add(4);
            if (result.player_pair)
                flashAreas.Add(5);

            // 大小区域闪烁
            if (result.is_big)
                flashAreas.Add(6);
            else
                flashAreas.Add(7);

            return flashAreas;
        }

        /// <summary>
        /// 计算当前连胜
        /// </summary>
        private static StreakInfo CalculateCurrentStreak(List<BaccaratGameResult> results)
        {
            var streak = new StreakInfo();
            
            if (results == null || results.Count == 0)
                return streak;

            var lastResult = results.Last();
            streak.winner = lastResult.winner;
            streak.count = 1;

            for (int i = results.Count - 2; i >= 0; i--)
            {
                if (results[i].winner == streak.winner && results[i].winner != BaccaratWinner.Tie)
                {
                    streak.count++;
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        /// <summary>
        /// 计算最大连胜
        /// </summary>
        private static int CalculateMaxStreak(List<BaccaratGameResult> results, BaccaratWinner winner)
        {
            int maxStreak = 0;
            int currentStreak = 0;

            foreach (var result in results)
            {
                if (result.winner == winner)
                {
                    currentStreak++;
                    maxStreak = Math.Max(maxStreak, currentStreak);
                }
                else
                {
                    currentStreak = 0;
                }
            }

            return maxStreak;
        }

        /// <summary>
        /// 生成游戏局号
        /// </summary>
        private static string GenerateGameNumber()
        {
            var now = DateTime.UtcNow;
            return $"{now:yyyyMMddHHmmss}{UnityEngine.Random.Range(100, 999)}";
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证游戏结果的有效性
        /// </summary>
        /// <param name="result">游戏结果</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateGameResult(BaccaratGameResult result)
        {
            var validation = new ValidationResult { IsValid = true };

            // 检查牌面数量
            if (result.banker_cards == null || result.banker_cards.Count < 2 || result.banker_cards.Count > 3)
            {
                validation.IsValid = false;
                validation.Errors.Add("庄家牌面数量错误");
            }

            if (result.player_cards == null || result.player_cards.Count < 2 || result.player_cards.Count > 3)
            {
                validation.IsValid = false;
                validation.Errors.Add("闲家牌面数量错误");
            }

            // 检查点数计算
            if (result.banker_points != CalculatePoints(result.banker_cards))
            {
                validation.IsValid = false;
                validation.Errors.Add("庄家点数计算错误");
            }

            if (result.player_points != CalculatePoints(result.player_cards))
            {
                validation.IsValid = false;
                validation.Errors.Add("闲家点数计算错误");
            }

            // 检查胜负判断
            var expectedWinner = DetermineWinner(result.banker_points, result.player_points);
            if (result.winner != expectedWinner)
            {
                validation.IsValid = false;
                validation.Errors.Add("胜负判断错误");
            }

            return validation;
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 补牌信息
    /// </summary>
    [System.Serializable]
    public class DrawCardInfo
    {
        public bool shouldDraw = false;
        public bool playerShouldDraw = false;
        public bool bankerShouldDraw = false;
        public string reason = "";
    }

    /// <summary>
    /// 赔付计算结果
    /// </summary>
    [System.Serializable]
    public class PayoutCalculationResult
    {
        public float totalBetAmount;
        public float totalPayout;
        public float netProfit;
        public List<BetPayoutResult> betResults;
    }

    /// <summary>
    /// 单个投注赔付结果
    /// </summary>
    [System.Serializable]
    public class BetPayoutResult
    {
        public BaccaratBetType betType;
        public float betAmount;
        public bool isWin;
        public float payoutAmount;
        public float odds;
        public string description;
    }

    /// <summary>
    /// 趋势分析
    /// </summary>
    [System.Serializable]
    public class TrendAnalysis
    {
        public int totalGames;
        public int bankerWins;
        public int playerWins;
        public int tieWins;
        public float bankerWinRate;
        public float playerWinRate;
        public float tieWinRate;
        public StreakInfo currentStreak;
        public int maxBankerStreak;
        public int maxPlayerStreak;
        public int bankerPairCount;
        public int playerPairCount;
        public float bankerPairRate;
        public float playerPairRate;
    }

    /// <summary>
    /// 连胜信息
    /// </summary>
    [System.Serializable]
    public class StreakInfo
    {
        public BaccaratWinner winner;
        public int count;
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    [System.Serializable]
    public class ValidationResult
    {
        public bool IsValid = true;
        public List<string> Errors = new List<string>();
    }

    #endregion
}