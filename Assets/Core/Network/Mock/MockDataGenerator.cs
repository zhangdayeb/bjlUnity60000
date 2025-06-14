// Assets/_Core/Network/Mock/MockDataGenerator.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Network.Interfaces;

namespace Core.Network.Mock
{
    /// <summary>
    /// Mock数据生成器
    /// 生成符合真实API格式的模拟数据，支持各种测试场景
    /// </summary>
    public class MockDataGenerator
    {
        #region 私有字段

        private readonly string[] _dealerNames = { "李小姐", "王先生", "张小姐", "刘先生", "陈小姐", "赵先生" };
        private readonly string[] _tableNames = { "百家乐A桌", "百家乐B桌", "VIP百家乐桌", "高限百家乐桌" };
        private readonly string[] _betTypeNames = { "庄家", "闲家", "和局", "庄对", "闲对", "大", "小" };
        
        // 用于生成一致性数据的种子
        private int _seed = 0;
        private System.Random _random;

        #endregion

        #region 构造函数

        public MockDataGenerator(int seed = 0)
        {
            _seed = seed == 0 ? (int)DateTime.UtcNow.Ticks : seed;
            _random = new System.Random(_seed);
        }

        #endregion

        #region 游戏结果生成

        /// <summary>
        /// 生成百家乐游戏结果
        /// </summary>
        public BaccaratGameResult GenerateGameResult(string gameNumber)
        {
            // 生成牌面
            var bankerCards = GenerateCards(2, 3);
            var playerCards = GenerateCards(2, 3);
            
            // 计算点数
            int bankerPoints = CalculateBaccaratPoints(bankerCards);
            int playerPoints = CalculateBaccaratPoints(playerCards);
            
            // 确定胜负
            BaccaratWinner winner;
            if (bankerPoints > playerPoints)
                winner = BaccaratWinner.Banker;
            else if (playerPoints > bankerPoints)
                winner = BaccaratWinner.Player;
            else
                winner = BaccaratWinner.Tie;
            
            // 检查对子
            bool bankerPair = bankerCards.Count >= 2 && bankerCards[0].rank == bankerCards[1].rank;
            bool playerPair = playerCards.Count >= 2 && playerCards[0].rank == playerCards[1].rank;
            
            // 检查大小（总牌数）
            bool isBig = (bankerCards.Count + playerCards.Count) >= 5;
            
            // 生成中奖区域
            var winningBets = new List<string>();
            winningBets.Add(winner.ToString());
            if (bankerPair) winningBets.Add("BankerPair");
            if (playerPair) winningBets.Add("PlayerPair");
            if (isBig) winningBets.Add("Big");
            else winningBets.Add("Small");
            
            return new BaccaratGameResult
            {
                game_number = gameNumber,
                winner = winner,
                banker_points = bankerPoints,
                player_points = playerPoints,
                banker_cards = bankerCards,
                player_cards = playerCards,
                banker_pair = bankerPair,
                player_pair = playerPair,
                is_big = isBig,
                winning_bets = winningBets,
                total_payout = _random.Next(1000, 10000),
                result_time = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 生成历史结果列表
        /// </summary>
        public List<BaccaratHistoryResult> GenerateHistoryResults(int count)
        {
            var results = new List<BaccaratHistoryResult>();
            var baseTime = DateTime.UtcNow.AddHours(-count);
            
            for (int i = 0; i < count; i++)
            {
                var gameNumber = baseTime.AddMinutes(i * 2).ToString("yyyyMMddHHmm") + _random.Next(100, 999);
                var gameResult = GenerateGameResult(gameNumber);
                
                results.Add(new BaccaratHistoryResult
                {
                    game_number = gameResult.game_number,
                    winner = gameResult.winner,
                    banker_pair = gameResult.banker_pair,
                    player_pair = gameResult.player_pair,
                    is_big = gameResult.is_big,
                    banker_points = gameResult.banker_points,
                    player_points = gameResult.player_points,
                    game_time = baseTime.AddMinutes(i * 2)
                });
            }
            
            return results;
        }

        #endregion

        #region 路纸生成

        /// <summary>
        /// 生成完整路纸数据
        /// </summary>
        public BaccaratRoadmaps GenerateRoadmaps(List<BaccaratHistoryResult> historyResults)
        {
            var roadmaps = new BaccaratRoadmaps
            {
                main_road = new List<RoadmapBead>(),
                big_eye_road = new List<RoadmapBead>(),
                small_road = new List<RoadmapBead>(),
                cockroach_road = new List<RoadmapBead>(),
                total_games = historyResults.Count,
                banker_wins = 0,
                player_wins = 0,
                ties = 0,
                last_updated = DateTime.UtcNow
            };
            
            // 生成大路
            int x = 0, y = 0;
            BaccaratWinner? lastWinner = null;
            
            foreach (var result in historyResults)
            {
                if (result.winner == BaccaratWinner.Tie)
                {
                    roadmaps.ties++;
                    continue; // 和局不计入大路主体，只做标记
                }
                
                // 统计胜负
                if (result.winner == BaccaratWinner.Banker)
                    roadmaps.banker_wins++;
                else if (result.winner == BaccaratWinner.Player)
                    roadmaps.player_wins++;
                
                // 大路逻辑
                if (lastWinner == null || lastWinner != result.winner)
                {
                    // 换边，新的一列
                    x++;
                    y = 0;
                }
                else
                {
                    // 同边，向下
                    y++;
                }
                
                var bead = new RoadmapBead
                {
                    result = result.winner,
                    banker_pair = result.banker_pair,
                    player_pair = result.player_pair,
                    position_x = x,
                    position_y = y,
                    game_number = result.game_number
                };
                
                roadmaps.main_road.Add(bead);
                lastWinner = result.winner;
            }
            
            // 生成下路（简化版本，实际应用更复杂的算法）
            roadmaps.big_eye_road = GenerateDerivativeRoad(roadmaps.main_road, 1);
            roadmaps.small_road = GenerateDerivativeRoad(roadmaps.main_road, 2);
            roadmaps.cockroach_road = GenerateDerivativeRoad(roadmaps.main_road, 3);
            
            return roadmaps;
        }

        /// <summary>
        /// 生成派生路纸（大眼仔、小路、蟑螂路）
        /// </summary>
        private List<RoadmapBead> GenerateDerivativeRoad(List<RoadmapBead> mainRoad, int type)
        {
            var derivativeRoad = new List<RoadmapBead>();
            
            // 简化的派生路逻辑（实际需要更复杂的算法）
            for (int i = 0; i < mainRoad.Count / 2; i++)
            {
                var bead = new RoadmapBead
                {
                    result = _random.NextDouble() > 0.5 ? BaccaratWinner.Banker : BaccaratWinner.Player,
                    banker_pair = false,
                    player_pair = false,
                    position_x = i % 6,
                    position_y = i / 6,
                    game_number = mainRoad[i * 2].game_number
                };
                
                derivativeRoad.Add(bead);
            }
            
            return derivativeRoad;
        }

        #endregion

        #region 投注记录生成

        /// <summary>
        /// 生成投注历史记录
        /// </summary>
        public List<BettingRecord> GenerateBettingHistory(int count)
        {
            var records = new List<BettingRecord>();
            var baseTime = DateTime.UtcNow.AddDays(-30);
            
            for (int i = 0; i < count; i++)
            {
                var record = new BettingRecord
                {
                    bet_id = Guid.NewGuid().ToString(),
                    game_number = GenerateGameNumber(baseTime.AddHours(i)),
                    table_id = "1",
                    user_id = "mock_user_123",
                    bet_time = baseTime.AddHours(i),
                    game_time = baseTime.AddHours(i).AddMinutes(2),
                    bets = GenerateBetDetails(),
                    total_bet_amount = 0, // 将在生成投注详情后计算
                    total_win_amount = 0,
                    net_profit = 0,
                    game_result = GenerateRandomGameResult(),
                    status = "settled"
                };
                
                // 计算总投注金额和盈亏
                foreach (var bet in record.bets)
                {
                    record.total_bet_amount += bet.bet_amount;
                    record.total_win_amount += bet.win_amount;
                }
                record.net_profit = record.total_win_amount - record.total_bet_amount;
                
                records.Add(record);
            }
            
            return records;
        }

        /// <summary>
        /// 生成投注详情
        /// </summary>
        public BettingRecord GenerateBettingDetail(string recordId)
        {
            var record = new BettingRecord
            {
                bet_id = recordId,
                game_number = GenerateGameNumber(),
                table_id = "1",
                user_id = "mock_user_123",
                bet_time = DateTime.UtcNow.AddMinutes(-5),
                game_time = DateTime.UtcNow.AddMinutes(-3),
                bets = GenerateBetDetails(),
                game_result = GenerateRandomGameResult(),
                status = "settled"
            };
            
            // 计算金额
            foreach (var bet in record.bets)
            {
                record.total_bet_amount += bet.bet_amount;
                record.total_win_amount += bet.win_amount;
            }
            record.net_profit = record.total_win_amount - record.total_bet_amount;
            
            return record;
        }

        /// <summary>
        /// 生成投注详情列表
        /// </summary>
        private List<BetDetail> GenerateBetDetails()
        {
            var details = new List<BetDetail>();
            int betCount = _random.Next(1, 4); // 1-3个投注
            
            for (int i = 0; i < betCount; i++)
            {
                var betType = _betTypeNames[_random.Next(_betTypeNames.Length)];
                var betAmount = _random.Next(1, 11) * 10f; // 10-100的倍数
                var isWin = _random.NextDouble() > 0.6; // 40%中奖率
                var odds = GetOddsForBetType(betType);
                
                var detail = new BetDetail
                {
                    bet_type = betType,
                    bet_amount = betAmount,
                    odds = odds,
                    is_win = isWin,
                    win_amount = isWin ? betAmount * (1 + odds) : 0,
                    bet_time = DateTime.UtcNow.AddMinutes(-_random.Next(1, 10))
                };
                
                details.Add(detail);
            }
            
            return details;
        }

        #endregion

        #region 牌面生成

        /// <summary>
        /// 生成牌面
        /// </summary>
        private List<BaccaratCard> GenerateCards(int minCount, int maxCount)
        {
            var cards = new List<BaccaratCard>();
            int cardCount = _random.Next(minCount, maxCount + 1);
            
            for (int i = 0; i < cardCount; i++)
            {
                var card = new BaccaratCard
                {
                    suit = _random.Next(1, 5), // 1-4: ♠♥♣♦
                    rank = _random.Next(1, 14), // 1-13: A-K
                    is_revealed = true
                };
                
                // 计算百家乐点数
                if (card.rank >= 10)
                    card.baccarat_value = 0;
                else if (card.rank == 1)
                    card.baccarat_value = 1;
                else
                    card.baccarat_value = card.rank;
                
                // 生成显示名称
                card.display_name = GetCardDisplayName(card.suit, card.rank);
                
                // 生成图片URL
                card.image_url = $"https://mock-cards.com/{card.suit}_{card.rank}.png";
                card.back_image_url = "https://mock-cards.com/back.png";
                
                cards.Add(card);
            }
            
            return cards;
        }

        /// <summary>
        /// 计算百家乐点数
        /// </summary>
        private int CalculateBaccaratPoints(List<BaccaratCard> cards)
        {
            int total = 0;
            foreach (var card in cards)
            {
                total += card.baccarat_value;
            }
            return total % 10;
        }

        /// <summary>
        /// 获取牌面显示名称
        /// </summary>
        private string GetCardDisplayName(int suit, int rank)
        {
            string suitChar;
            switch (suit)
            {
                case 1: suitChar = "♠"; break;
                case 2: suitChar = "♥"; break;
                case 3: suitChar = "♣"; break;
                case 4: suitChar = "♦"; break;
                default: suitChar = "?"; break;
            }
            
            string rankChar;
            switch (rank)
            {
                case 1: rankChar = "A"; break;
                case 11: rankChar = "J"; break;
                case 12: rankChar = "Q"; break;
                case 13: rankChar = "K"; break;
                default: rankChar = rank.ToString(); break;
            }
            
            return suitChar + rankChar;
        }

        #endregion

        #region 用户和台桌数据

        /// <summary>
        /// 生成用户信息
        /// </summary>
        public UserInfo GenerateUserInfo(string userId = null)
        {
            return new UserInfo
            {
                user_id = userId ?? "mock_user_" + _random.Next(100, 999),
                balance = _random.Next(5000, 20000),
                money_balance = _random.Next(5000, 20000),
                currency = "CNY",
                nickname = "玩家" + _random.Next(1000, 9999),
                avatar = $"https://mock-avatars.com/avatar_{_random.Next(1, 100)}.jpg",
                level = _random.Next(1, 50),
                vip_level = _random.Next(0, 8)
            };
        }

        /// <summary>
        /// 生成台桌信息
        /// </summary>
        public TableInfo GenerateTableInfo(string tableId = null)
        {
            return new TableInfo
            {
                id = tableId != null ? int.Parse(tableId) : _random.Next(1, 10),
                lu_zhu_name = _tableNames[_random.Next(_tableNames.Length)],
                num_pu = _random.Next(30, 100),
                num_xue = _random.Next(10, 50),
                video_near = $"https://mock-video.com/table_{tableId}/near.m3u8",
                video_far = $"https://mock-video.com/table_{tableId}/far.m3u8",
                time_start = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds,
                right_money_banker_player = _random.Next(1000, 10000),
                right_money_tie = _random.Next(500, 2000),
                table_title = _tableNames[_random.Next(_tableNames.Length)],
                bureau_number = $"T{_random.Next(100, 999):D3}"
            };
        }

        #endregion

        #region 统计和分析数据

        /// <summary>
        /// 生成游戏统计数据
        /// </summary>
        public BaccaratStatistics GenerateStatistics(List<BaccaratHistoryResult> historyResults = null)
        {
            var totalGames = historyResults?.Count ?? _random.Next(100, 500);
            var bankerWins = _random.Next(totalGames * 45 / 100, totalGames * 55 / 100);
            var playerWins = _random.Next(totalGames * 40 / 100, totalGames * 50 / 100);
            var ties = totalGames - bankerWins - playerWins;
            
            return new BaccaratStatistics
            {
                total_games = totalGames,
                banker_wins = bankerWins,
                player_wins = playerWins,
                ties = ties,
                banker_win_rate = (float)bankerWins / totalGames,
                player_win_rate = (float)playerWins / totalGames,
                tie_rate = (float)ties / totalGames,
                banker_max_streak = _random.Next(3, 12),
                player_max_streak = _random.Next(3, 10),
                current_streak = _random.Next(1, 6),
                streak_side = _random.NextDouble() > 0.5 ? BaccaratWinner.Banker : BaccaratWinner.Player,
                banker_pair_count = _random.Next(totalGames * 5 / 100, totalGames * 15 / 100),
                player_pair_count = _random.Next(totalGames * 5 / 100, totalGames * 15 / 100),
                banker_pair_rate = _random.Next(5, 15) / 100f,
                player_pair_rate = _random.Next(5, 15) / 100f
            };
        }

        /// <summary>
        /// 生成热门投注数据
        /// </summary>
        public PopularBets GeneratePopularBets()
        {
            return new PopularBets
            {
                banker_popularity = _random.Next(40, 80) / 100f,
                player_popularity = _random.Next(30, 70) / 100f,
                tie_popularity = _random.Next(5, 20) / 100f,
                pair_popularity = _random.Next(10, 30) / 100f,
                banker_total_amount = _random.Next(10000, 100000),
                player_total_amount = _random.Next(8000, 80000),
                tie_total_amount = _random.Next(1000, 10000),
                banker_bet_count = _random.Next(20, 100),
                player_bet_count = _random.Next(15, 90),
                tie_bet_count = _random.Next(2, 20)
            };
        }

        /// <summary>
        /// 生成预测数据
        /// </summary>
        public BaccaratPrediction GeneratePrediction()
        {
            var predictions = new[] { BaccaratWinner.Banker, BaccaratWinner.Player };
            var predictedWinner = predictions[_random.Next(predictions.Length)];
            
            return new BaccaratPrediction
            {
                predicted_winner = predictedWinner,
                confidence = _random.Next(45, 85) / 100f,
                prediction_method = "AI智能分析",
                reasoning = new List<string>
                {
                    "基于历史数据分析",
                    "考虑当前趋势",
                    "概率统计计算",
                    "路纸模式识别"
                },
                trend_analysis = GenerateTrendAnalysis(),
                is_streak_likely = _random.NextDouble() > 0.7,
                banker_probability = _random.Next(40, 60) / 100f,
                player_probability = _random.Next(35, 55) / 100f,
                tie_probability = _random.Next(8, 15) / 100f
            };
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成游戏局号
        /// </summary>
        private string GenerateGameNumber(DateTime? time = null)
        {
            var gameTime = time ?? DateTime.UtcNow;
            return gameTime.ToString("yyyyMMddHHmm") + _random.Next(100, 999);
        }

        /// <summary>
        /// 生成随机游戏结果
        /// </summary>
        private GameResultSummary GenerateRandomGameResult()
        {
            var winners = new[] { "Banker", "Player", "Tie" };
            var winner = winners[_random.Next(winners.Length)];
            
            return new GameResultSummary
            {
                winner = winner,
                banker_points = _random.Next(0, 10),
                player_points = _random.Next(0, 10),
                banker_pair = _random.NextDouble() > 0.85,
                player_pair = _random.NextDouble() > 0.85,
                is_big = _random.NextDouble() > 0.5
            };
        }

        /// <summary>
        /// 获取投注类型对应的赔率
        /// </summary>
        private float GetOddsForBetType(string betType)
        {
            switch (betType)
            {
                case "庄家": return 0.95f;
                case "闲家": return 1.0f;
                case "和局": return 8.0f;
                case "庄对":
                case "闲对": return 11.0f;
                case "大": return 0.54f;
                case "小": return 1.5f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// 生成趋势分析文本
        /// </summary>
        private string GenerateTrendAnalysis()
        {
            var analyses = new[]
            {
                "当前庄家连胜趋势明显，建议关注反转机会",
                "闲家近期表现稳定，可考虑跟投",
                "路纸显示交替出现模式，建议谨慎投注",
                "大路呈现长龙走势，注意风险控制",
                "最近10局庄闲相对均衡，可等待明确信号"
            };
            
            return analyses[_random.Next(analyses.Length)];
        }

        /// <summary>
        /// 生成特殊测试场景数据
        /// </summary>
        public void SetTestScenario(TestScenario scenario)
        {
            switch (scenario)
            {
                case TestScenario.BankerStreak:
                    // 设置庄家连胜场景
                    _seed = 12345;
                    break;
                    
                case TestScenario.PlayerStreak:
                    // 设置闲家连胜场景
                    _seed = 54321;
                    break;
                    
                case TestScenario.AlternatingWins:
                    // 设置交替胜利场景
                    _seed = 11111;
                    break;
                    
                case TestScenario.HighTieRate:
                    // 设置高和局率场景
                    _seed = 99999;
                    break;
                    
                case TestScenario.LowBalance:
                    // 设置低余额场景
                    _seed = 10101;
                    break;
            }
            
            _random = new System.Random(_seed);
        }

        /// <summary>
        /// 生成网络错误模拟
        /// </summary>
        public WebSocketError GenerateNetworkError()
        {
            var errorTypes = new[]
            {
                WebSocketErrorType.ConnectionFailed,
                WebSocketErrorType.NetworkError,
                WebSocketErrorType.TimeoutError,
                WebSocketErrorType.ServerError
            };
            
            var messages = new[]
            {
                "网络连接超时",
                "服务器暂时不可用",
                "连接已断开，正在重连",
                "数据传输错误",
                "认证信息已过期"
            };
            
            return new WebSocketError
            {
                code = $"ERR_{_random.Next(1000, 9999)}",
                message = messages[_random.Next(messages.Length)],
                type = errorTypes[_random.Next(errorTypes.Length)],
                timestamp = DateTime.UtcNow,
                isRecoverable = _random.NextDouble() > 0.3,
                suggestion = "请检查网络连接或稍后重试",
                context = new Dictionary<string, object>
                {
                    {"error_id", Guid.NewGuid().ToString()},
                    {"retry_count", _random.Next(0, 5)},
                    {"last_success_time", DateTime.UtcNow.AddMinutes(-_random.Next(1, 30))}
                }
            };
        }

        #endregion
    }

    #region 测试场景枚举

    /// <summary>
    /// 测试场景类型
    /// </summary>
    public enum TestScenario
    {
        Normal,            // 正常随机
        BankerStreak,      // 庄家连胜
        PlayerStreak,      // 闲家连胜
        AlternatingWins,   // 交替胜利
        HighTieRate,       // 高和局率
        LowBalance,        // 低余额
        NetworkError,      // 网络错误
        ServerMaintenance  // 服务器维护
    }

    #endregion

    #region 辅助数据类型

    /// <summary>
    /// 游戏结果摘要（用于历史记录）
    /// </summary>
    [System.Serializable]
    public class GameResultSummary
    {
        public string winner;
        public int banker_points;
        public int player_points;
        public bool banker_pair;
        public bool player_pair;
        public bool is_big;
    }

    /// <summary>
    /// 投注记录
    /// </summary>
    [System.Serializable]
    public class BettingRecord
    {
        public string bet_id;
        public string game_number;
        public string table_id;
        public string user_id;
        public DateTime bet_time;
        public DateTime game_time;
        public List<BetDetail> bets;
        public float total_bet_amount;
        public float total_win_amount;
        public float net_profit;
        public GameResultSummary game_result;
        public string status; // "pending", "settled", "cancelled"
    }

    /// <summary>
    /// 投注详情
    /// </summary>
    [System.Serializable]
    public class BetDetail
    {
        public string bet_type;
        public float bet_amount;
        public float odds;
        public bool is_win;
        public float win_amount;
        public DateTime bet_time;
    }

    /// <summary>
    /// 表格运行信息
    /// </summary>
    [System.Serializable]
    public class TableRunInfo
    {
        public string table_id;
        public string game_number;
        public string run_status;
        public DateTime end_time;
        public bool betting_open;
    }

    /// <summary>
    /// 投注历史响应
    /// </summary>
    [System.Serializable]
    public class BettingHistoryResponse
    {
        public List<BettingRecord> records;
        public int total;
        public int page;
        public int pageSize;
        public int totalPages;
    }

    /// <summary>
    /// 投注历史查询参数
    /// </summary>
    [System.Serializable]
    public class BettingHistoryParams
    {
        public int page = 1;
        public int pageSize = 20;
        public string start_date;
        public string end_date;
    }

    #endregion
}