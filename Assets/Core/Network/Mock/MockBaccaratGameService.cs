// Assets/_Core/Network/Mock/MockBaccaratGameService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Network.Interfaces;

namespace Core.Network.Mock
{
    /// <summary>
    /// Mock百家乐游戏服务 - 模拟bjlService
    /// 提供完整的Mock数据，支持前端独立开发和测试
    /// </summary>
    public class MockBaccaratGameService : IBaccaratGameService
    {
        #region 私有字段
        
        private GameParams _gameParams;
        private MockDataGenerator _dataGenerator;
        private ApiServiceStatus _serviceStatus = ApiServiceStatus.Uninitialized;
        private System.Action<ApiError> _onError;
        private System.Action _onAuthFailed;
        
        // Mock数据
        private UserInfo _mockUserInfo;
        private TableInfo _mockTableInfo;
        private BaccaratGameState _mockGameState;
        private List<BaccaratHistoryResult> _mockHistory;
        private BaccaratRoadmaps _mockRoadmaps;
        
        // 投注相关
        private List<BaccaratBetRequest> _currentBets = new List<BaccaratBetRequest>();
        private float _currentBalance = 10000f;
        
        #endregion

        #region 构造函数

        public MockBaccaratGameService()
        {
            _dataGenerator = new MockDataGenerator();
            InitializeMockData();
        }

        #endregion

        #region IGameApiService 实现

        public async Task<ApiInitResult> InitializeAsync(GameParams gameParams)
        {
            _gameParams = gameParams;
            _serviceStatus = ApiServiceStatus.Initializing;
            
            // 模拟初始化延迟
            await Task.Delay(UnityEngine.Random.Range(500, 1500));
            
            _serviceStatus = ApiServiceStatus.Ready;
            
            return new ApiInitResult
            {
                success = true,
                message = "Mock service initialized successfully",
                userInfo = _mockUserInfo,
                tableInfo = _mockTableInfo,
                timestamp = DateTime.UtcNow
            };
        }

        public void UpdateGameParams(GameParams newParams)
        {
            _gameParams = newParams;
            Debug.Log($"[MockBaccaratGameService] Game params updated: Table={newParams.table_id}, User={newParams.user_id}");
        }

        public GameParams GetGameParams()
        {
            return _gameParams;
        }

        public async Task<UserInfo> GetUserInfoAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(100, 300));
            
            // 更新余额（模拟实时变化）
            _mockUserInfo.balance = _currentBalance;
            _mockUserInfo.money_balance = _currentBalance;
            
            return _mockUserInfo;
        }

        public async Task<UserBalance> RefreshBalanceAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(100, 200));
            
            return new UserBalance
            {
                balance = _currentBalance,
                moneyBalance = _currentBalance,
                currency = "CNY",
                lastUpdated = DateTime.UtcNow
            };
        }

        public async Task<TableInfo> GetTableInfoAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(100, 300));
            return _mockTableInfo;
        }

        public async Task<TableRunInfo> GetTableStatusAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(50, 150));
            
            return new TableRunInfo
            {
                table_id = _gameParams?.table_id ?? "1",
                game_number = _mockGameState.game_number,
                run_status = _mockGameState.phase.ToString().ToLower(),
                end_time = DateTime.UtcNow.AddSeconds(_mockGameState.countdown),
                betting_open = _mockGameState.betting_open
            };
        }

        public async Task<BetResponse> PlaceBetsAsync(List<BetRequest> bets)
        {
            // 转换为百家乐投注格式
            var baccaratBets = new List<BaccaratBetRequest>();
            foreach (var bet in bets)
            {
                baccaratBets.Add(new BaccaratBetRequest
                {
                    money = bet.money,
                    rate_id = bet.rate_id,
                    betType = GetBetTypeName(bet.rate_id)
                });
            }
            
            var response = await PlaceBaccaratBetsAsync(baccaratBets, 0);
            
            return new BetResponse
            {
                money_balance = response.money_balance,
                money_spend = response.money_spend,
                bets = bets
            };
        }

        public async Task<CurrentBetsResponse> GetCurrentBetsAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(50, 150));
            
            var betRecords = new List<BetRecord>();
            float totalAmount = 0f;
            
            foreach (var bet in _currentBets)
            {
                betRecords.Add(new BetRecord
                {
                    bet_id = Guid.NewGuid().ToString(),
                    bet_type = bet.betType,
                    bet_amount = bet.money,
                    game_number = _mockGameState.game_number,
                    timestamp = bet.timestamp
                });
                totalAmount += bet.money;
            }
            
            return new CurrentBetsResponse
            {
                currentBets = betRecords,
                totalBetAmount = totalAmount,
                gameNumber = _mockGameState.game_number,
                timestamp = DateTime.UtcNow
            };
        }

        public async Task<CancelBetResponse> CancelPendingBetsAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(100, 200));
            
            float refundAmount = 0f;
            var cancelledIds = new List<string>();
            
            foreach (var bet in _currentBets)
            {
                refundAmount += bet.money;
                cancelledIds.Add(Guid.NewGuid().ToString());
            }
            
            // 退还金额
            _currentBalance += refundAmount;
            _currentBets.Clear();
            
            return new CancelBetResponse
            {
                success = true,
                message = $"已取消 {cancelledIds.Count} 个投注",
                cancelledBetIds = cancelledIds,
                refundAmount = refundAmount
            };
        }

        public async Task<BettingHistoryResponse> GetBettingHistoryAsync(BettingHistoryParams @params)
        {
            await Task.Delay(UnityEngine.Random.Range(200, 500));
            
            var mockRecords = _dataGenerator.GenerateBettingHistory(@params.pageSize);
            
            return new BettingHistoryResponse
            {
                records = mockRecords,
                total = 150, // 模拟总记录数
                page = @params.page,
                pageSize = @params.pageSize,
                totalPages = (int)Math.Ceiling(150.0 / @params.pageSize)
            };
        }

        public async Task<BettingDetailResponse> GetBettingDetailAsync(string recordId)
        {
            await Task.Delay(UnityEngine.Random.Range(100, 300));
            
            var mockDetail = _dataGenerator.GenerateBettingDetail(recordId);
            
            return new BettingDetailResponse
            {
                code = 200,
                message = "success",
                data = mockDetail
            };
        }

        public async Task<RoadmapData> GetRoadmapDataAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(100, 300));
            
            var roadmapData = new RoadmapData
            {
                results = _mockHistory.ConvertAll(h => new GameResultRecord
                {
                    game_number = h.game_number,
                    result = h.winner.ToString(),
                    timestamp = h.game_time
                }),
                statistics = new RoadmapStatistics
                {
                    totalGames = _mockHistory.Count,
                    bankerWins = _mockHistory.FindAll(h => h.winner == BaccaratWinner.Banker).Count,
                    playerWins = _mockHistory.FindAll(h => h.winner == BaccaratWinner.Player).Count,
                    ties = _mockHistory.FindAll(h => h.winner == BaccaratWinner.Tie).Count
                },
                lastUpdated = DateTime.UtcNow
            };
            
            // 计算胜率
            roadmapData.statistics.bankerWinRate = (float)roadmapData.statistics.bankerWins / roadmapData.statistics.totalGames;
            roadmapData.statistics.playerWinRate = (float)roadmapData.statistics.playerWins / roadmapData.statistics.totalGames;
            roadmapData.statistics.tieRate = (float)roadmapData.statistics.ties / roadmapData.statistics.totalGames;
            
            return roadmapData;
        }

        public void SetErrorHandler(Action<ApiError> onError)
        {
            _onError = onError;
        }

        public void SetAuthFailedHandler(Action onAuthFailed)
        {
            _onAuthFailed = onAuthFailed;
        }

        public async Task<NetworkStatus> CheckConnectionAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(50, 150));
            
            return new NetworkStatus
            {
                isConnected = true,
                latency = UnityEngine.Random.Range(50, 200),
                quality = "excellent",
                lastCheck = DateTime.UtcNow
            };
        }

        public ApiServiceStatus GetServiceStatus()
        {
            return _serviceStatus;
        }

        #endregion

        #region IBaccaratGameService 专用实现

        public async Task<BaccaratBetResponse> PlaceBaccaratBetsAsync(List<BaccaratBetRequest> bets, int isExempt = 0)
        {
            // 模拟网络延迟
            await Task.Delay(UnityEngine.Random.Range(200, 600));
            
            // 模拟10%的网络错误概率
            if (UnityEngine.Random.Range(0f, 1f) < 0.1f)
            {
                throw new Exception("网络连接超时，请重试");
            }
            
            // 计算总投注金额
            float totalBetAmount = 0f;
            foreach (var bet in bets)
            {
                totalBetAmount += bet.money;
                bet.timestamp = DateTime.UtcNow;
            }
            
            // 检查余额
            if (totalBetAmount > _currentBalance)
            {
                throw new Exception("余额不足");
            }
            
            // 扣除余额
            _currentBalance -= totalBetAmount;
            
            // 保存当前投注
            _currentBets.AddRange(bets);
            
            // 计算免佣节省
            float exemptSaving = 0f;
            if (isExempt == 1)
            {
                foreach (var bet in bets)
                {
                    if (bet.rate_id == 1) // 庄家投注
                    {
                        exemptSaving += bet.money * 0.05f; // 5%免佣
                    }
                }
            }
            
            return new BaccaratBetResponse
            {
                success = true,
                message = "投注成功",
                money_balance = _currentBalance,
                money_spend = totalBetAmount,
                bets = bets,
                game_number = _mockGameState.game_number,
                bet_id = Guid.NewGuid().ToString(),
                bet_time = DateTime.UtcNow,
                is_exempt_applied = isExempt == 1,
                exempt_saving = exemptSaving
            };
        }

        public async Task<BaccaratBetLimits> GetBetLimitsAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(100, 200));
            
            return new BaccaratBetLimits
            {
                bankerMin = 10f,
                bankerMax = 5000f,
                playerMin = 10f,
                playerMax = 5000f,
                tieMin = 10f,
                tieMax = 1000f,
                pairMin = 5f,
                pairMax = 500f,
                bigSmallMin = 5f,
                bigSmallMax = 500f,
                tableMin = 5f,
                tableMax = 5000f
            };
        }

        public async Task<ExemptSettings> GetExemptSettingsAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(50, 150));
            
            return new ExemptSettings
            {
                isAvailable = true,
                isEnabled = PlayerPrefs.GetInt("BaccaratExemptEnabled", 0) == 1,
                exemptRate = 0.05f,
                breakEvenPoint = 0.5f,
                onlyForBanker = true,
                minBetAmount = 100f,
                description = "免佣投注：庄家投注无需支付5%佣金，但庄家以6点获胜时按1:2赔付"
            };
        }

        public async Task<BaccaratGameState> GetGameStateAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(50, 150));
            
            // 模拟游戏状态变化
            UpdateMockGameState();
            
            return _mockGameState;
        }

        public async Task<BaccaratGameTiming> GetGameTimingAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(50, 100));
            
            return new BaccaratGameTiming
            {
                game_number = _mockGameState.game_number,
                betting_countdown = _mockGameState.countdown,
                total_countdown = _mockGameState.countdown,
                start_time = DateTime.UtcNow.AddSeconds(-30),
                end_time = DateTime.UtcNow.AddSeconds(_mockGameState.countdown),
                betting_duration = 30,
                dealing_duration = 15,
                result_duration = 10
            };
        }

        public async Task<BaccaratGameResult> GetGameResultAsync(string gameNumber)
        {
            await Task.Delay(UnityEngine.Random.Range(100, 300));
            
            return _dataGenerator.GenerateGameResult(gameNumber);
        }

        public async Task<BaccaratRoadmaps> GetBaccaratRoadmapsAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(200, 400));
            return _mockRoadmaps;
        }

        public async Task<List<BaccaratHistoryResult>> GetHistoryResultsAsync(int count = 50)
        {
            await Task.Delay(UnityEngine.Random.Range(150, 350));
            
            var results = new List<BaccaratHistoryResult>();
            for (int i = 0; i < Math.Min(count, _mockHistory.Count); i++)
            {
                results.Add(_mockHistory[i]);
            }
            
            return results;
        }

        public async Task<BaccaratStatistics> GetStatisticsAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(100, 250));
            
            var bankerWins = _mockHistory.FindAll(h => h.winner == BaccaratWinner.Banker).Count;
            var playerWins = _mockHistory.FindAll(h => h.winner == BaccaratWinner.Player).Count;
            var ties = _mockHistory.FindAll(h => h.winner == BaccaratWinner.Tie).Count;
            var total = _mockHistory.Count;
            
            return new BaccaratStatistics
            {
                total_games = total,
                banker_wins = bankerWins,
                player_wins = playerWins,
                ties = ties,
                banker_win_rate = (float)bankerWins / total,
                player_win_rate = (float)playerWins / total,
                tie_rate = (float)ties / total,
                banker_max_streak = CalculateMaxStreak(BaccaratWinner.Banker),
                player_max_streak = CalculateMaxStreak(BaccaratWinner.Player),
                current_streak = CalculateCurrentStreak(),
                streak_side = _mockHistory.Count > 0 ? _mockHistory[0].winner : BaccaratWinner.Banker,
                banker_pair_count = _mockHistory.FindAll(h => h.banker_pair).Count,
                player_pair_count = _mockHistory.FindAll(h => h.player_pair).Count,
                banker_pair_rate = (float)_mockHistory.FindAll(h => h.banker_pair).Count / total,
                player_pair_rate = (float)_mockHistory.FindAll(h => h.player_pair).Count / total
            };
        }

        public async Task<BaccaratPrediction> GetPredictionAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(100, 300));
            
            // 简单的预测逻辑：基于历史趋势
            var recentResults = _mockHistory.GetRange(0, Math.Min(10, _mockHistory.Count));
            var bankerCount = recentResults.FindAll(r => r.winner == BaccaratWinner.Banker).Count;
            var playerCount = recentResults.FindAll(r => r.winner == BaccaratWinner.Player).Count;
            
            BaccaratWinner prediction;
            float confidence;
            
            if (bankerCount > playerCount + 2)
            {
                prediction = BaccaratWinner.Player; // 反向预测
                confidence = 0.65f;
            }
            else if (playerCount > bankerCount + 2)
            {
                prediction = BaccaratWinner.Banker; // 反向预测
                confidence = 0.65f;
            }
            else
            {
                prediction = UnityEngine.Random.Range(0, 2) == 0 ? BaccaratWinner.Banker : BaccaratWinner.Player;
                confidence = 0.5f;
            }
            
            return new BaccaratPrediction
            {
                predicted_winner = prediction,
                confidence = confidence,
                prediction_method = "趋势分析",
                reasoning = new List<string> { "基于最近10局结果", "考虑庄闲均衡性", "概率统计分析" },
                trend_analysis = $"最近10局：庄{bankerCount}局，闲{playerCount}局",
                is_streak_likely = Math.Abs(bankerCount - playerCount) >= 3,
                banker_probability = 0.45f + UnityEngine.Random.Range(-0.05f, 0.05f),
                player_probability = 0.44f + UnityEngine.Random.Range(-0.05f, 0.05f),
                tie_probability = 0.11f + UnityEngine.Random.Range(-0.02f, 0.02f)
            };
        }

        public async Task<PopularBets> GetPopularBetsAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(100, 200));
            
            return new PopularBets
            {
                banker_popularity = UnityEngine.Random.Range(0.4f, 0.7f),
                player_popularity = UnityEngine.Random.Range(0.3f, 0.6f),
                tie_popularity = UnityEngine.Random.Range(0.05f, 0.15f),
                pair_popularity = UnityEngine.Random.Range(0.1f, 0.3f),
                banker_total_amount = UnityEngine.Random.Range(10000f, 50000f),
                player_total_amount = UnityEngine.Random.Range(8000f, 40000f),
                tie_total_amount = UnityEngine.Random.Range(1000f, 5000f),
                banker_bet_count = UnityEngine.Random.Range(20, 80),
                player_bet_count = UnityEngine.Random.Range(15, 70),
                tie_bet_count = UnityEngine.Random.Range(2, 15)
            };
        }

        public async Task<BaccaratOdds> GetOddsAsync()
        {
            await Task.Delay(UnityEngine.Random.Range(50, 150));
            
            return new BaccaratOdds
            {
                banker_odds = 0.95f,
                player_odds = 1.0f,
                tie_odds = 8.0f,
                banker_pair_odds = 11.0f,
                player_pair_odds = 11.0f,
                big_odds = 0.54f,
                small_odds = 1.5f,
                banker_no_commission_odds = 1.0f,
                has_special_rules = true,
                special_rules_desc = "免佣庄家6点胜出按1:2赔付"
            };
        }

        #endregion

        #region 私有方法

        private void InitializeMockData()
        {
            // 初始化用户信息
            _mockUserInfo = new UserInfo
            {
                user_id = "mock_user_123",
                balance = _currentBalance,
                money_balance = _currentBalance,
                currency = "CNY",
                nickname = "测试玩家",
                avatar = "https://example.com/avatar.jpg",
                level = 15,
                vip_level = 3
            };
            
            // 初始化台桌信息
            _mockTableInfo = new TableInfo
            {
                id = 1,
                lu_zhu_name = "百家乐A桌",
                num_pu = 50,
                num_xue = 20,
                video_near = "https://mock-video.com/near.m3u8",
                video_far = "https://mock-video.com/far.m3u8",
                time_start = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds,
                right_money_banker_player = 5000,
                right_money_tie = 1000,
                table_title = "VIP百家乐桌",
                bureau_number = "A001"
            };
            
            // 初始化游戏状态
            _mockGameState = new BaccaratGameState
            {
                game_number = GenerateGameNumber(),
                phase = BaccaratGamePhase.Betting,
                countdown = 25,
                betting_open = true,
                dealer_name = "李小姐",
                table_name = "百家乐A桌",
                round_number = 1,
                video_url_near = "https://mock-video.com/near.m3u8",
                video_url_far = "https://mock-video.com/far.m3u8",
                video_active = true
            };
            
            // 生成历史数据
            _mockHistory = _dataGenerator.GenerateHistoryResults(100);
            
            // 生成路纸数据
            _mockRoadmaps = _dataGenerator.GenerateRoadmaps(_mockHistory);
        }

        private void UpdateMockGameState()
        {
            // 模拟倒计时
            _mockGameState.countdown = Mathf.Max(0, _mockGameState.countdown - 1);
            
            // 模拟游戏阶段切换
            if (_mockGameState.countdown <= 0)
            {
                switch (_mockGameState.phase)
                {
                    case BaccaratGamePhase.Betting:
                        _mockGameState.phase = BaccaratGamePhase.Dealing;
                        _mockGameState.countdown = 15;
                        _mockGameState.betting_open = false;
                        break;
                        
                    case BaccaratGamePhase.Dealing:
                        _mockGameState.phase = BaccaratGamePhase.Result;
                        _mockGameState.countdown = 10;
                        ProcessGameResult();
                        break;
                        
                    case BaccaratGamePhase.Result:
                        _mockGameState.phase = BaccaratGamePhase.Betting;
                        _mockGameState.countdown = 30;
                        _mockGameState.betting_open = true;
                        _mockGameState.game_number = GenerateGameNumber();
                        _currentBets.Clear(); // 清空当前投注
                        break;
                }
            }
        }

        private void ProcessGameResult()
        {
            // 生成游戏结果
            var result = _dataGenerator.GenerateGameResult(_mockGameState.game_number);
            
            // 计算中奖金额
            float winAmount = CalculateWinAmount(result);
            _currentBalance += winAmount;
            
            // 添加到历史记录
            _mockHistory.Insert(0, new BaccaratHistoryResult
            {
                game_number = result.game_number,
                winner = result.winner,
                banker_pair = result.banker_pair,
                player_pair = result.player_pair,
                is_big = result.is_big,
                banker_points = result.banker_points,
                player_points = result.player_points,
                game_time = result.result_time
            });
            
            // 保持历史记录数量
            if (_mockHistory.Count > 100)
            {
                _mockHistory = _mockHistory.GetRange(0, 100);
            }
        }

        private float CalculateWinAmount(BaccaratGameResult result)
        {
            float winAmount = 0f;
            
            foreach (var bet in _currentBets)
            {
                bool isWin = false;
                float odds = 1f;
                
                switch (bet.rate_id)
                {
                    case 1: // 庄家
                        isWin = result.winner == BaccaratWinner.Banker;
                        odds = 0.95f;
                        break;
                    case 2: // 闲家
                        isWin = result.winner == BaccaratWinner.Player;
                        odds = 1f;
                        break;
                    case 3: // 和局
                        isWin = result.winner == BaccaratWinner.Tie;
                        odds = 8f;
                        break;
                    case 4: // 庄对
                        isWin = result.banker_pair;
                        odds = 11f;
                        break;
                    case 5: // 闲对
                        isWin = result.player_pair;
                        odds = 11f;
                        break;
                    case 6: // 大
                        isWin = result.is_big;
                        odds = 0.54f;
                        break;
                    case 7: // 小
                        isWin = !result.is_big;
                        odds = 1.5f;
                        break;
                }
                
                if (isWin)
                {
                    winAmount += bet.money * (1 + odds);
                }
            }
            
            return winAmount;
        }

        private string GenerateGameNumber()
        {
            return DateTime.UtcNow.ToString("yyyyMMddHHmm") + UnityEngine.Random.Range(100, 999);
        }

        private string GetBetTypeName(int rateId)
        {
            switch (rateId)
            {
                case 1: return "庄家";
                case 2: return "闲家";
                case 3: return "和局";
                case 4: return "庄对";
                case 5: return "闲对";
                case 6: return "大";
                case 7: return "小";
                default: return "未知";
            }
        }

        private int CalculateMaxStreak(BaccaratWinner winner)
        {
            int maxStreak = 0;
            int currentStreak = 0;
            
            foreach (var result in _mockHistory)
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

        private int CalculateCurrentStreak()
        {
            if (_mockHistory.Count == 0) return 0;
            
            var lastWinner = _mockHistory[0].winner;
            int streak = 1;
            
            for (int i = 1; i < _mockHistory.Count; i++)
            {
                if (_mockHistory[i].winner == lastWinner)
                {
                    streak++;
                }
                else
                {
                    break;
                }
            }
            
            return streak;
        }

        #endregion
    }
}