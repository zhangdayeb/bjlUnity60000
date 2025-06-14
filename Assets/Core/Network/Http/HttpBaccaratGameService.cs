// Assets/_Core/Network/Http/HttpBaccaratGameService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Network.Interfaces;
using Core.Network.Http;

namespace Core.Network.Http
{
    /// <summary>
    /// HTTP百家乐游戏服务 - 对接真实后端API
    /// 实现IBaccaratGameService接口，提供与JavaScript bjlService完全一致的功能
    /// </summary>
    public class HttpBaccaratGameService : IBaccaratGameService
    {
        #region 私有字段

        private HttpClient _httpClient;
        private GameParams _gameParams;
        private ApiServiceStatus _serviceStatus = ApiServiceStatus.Uninitialized;
        private System.Action<ApiError> _onError;
        private System.Action _onAuthFailed;

        #endregion

        #region 构造函数

        public HttpBaccaratGameService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            // 设置错误处理器
            _httpClient.SetGlobalErrorHandler(OnHttpError);
            _httpClient.SetAuthFailedHandler(OnAuthFailed);
        }

        #endregion

        #region IGameApiService 实现

        public async Task<ApiInitResult> InitializeAsync(GameParams gameParams)
        {
            try
            {
                _gameParams = gameParams ?? throw new ArgumentNullException(nameof(gameParams));
                _serviceStatus = ApiServiceStatus.Initializing;
                
                // 设置认证Token
                _httpClient.SetAuthToken(gameParams.token);
                
                // 初始化时获取用户和台桌信息
                var userInfoTask = GetUserInfoAsync();
                var tableInfoTask = GetTableInfoAsync();
                
                await Task.WhenAll(userInfoTask, tableInfoTask);
                
                var userInfo = await userInfoTask;
                var tableInfo = await tableInfoTask;
                
                _serviceStatus = ApiServiceStatus.Ready;
                
                Debug.Log($"[HttpBaccaratGameService] 初始化成功 - 用户: {userInfo.user_id}, 台桌: {tableInfo.id}");
                
                return new ApiInitResult
                {
                    success = true,
                    message = "服务初始化成功",
                    userInfo = userInfo,
                    tableInfo = tableInfo,
                    timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _serviceStatus = ApiServiceStatus.Error;
                Debug.LogError($"[HttpBaccaratGameService] 初始化失败: {ex.Message}");
                
                return new ApiInitResult
                {
                    success = false,
                    message = $"初始化失败: {ex.Message}",
                    timestamp = DateTime.UtcNow
                };
            }
        }

        public void UpdateGameParams(GameParams newParams)
        {
            _gameParams = newParams;
            _httpClient.SetAuthToken(newParams.token);
            Debug.Log($"[HttpBaccaratGameService] 游戏参数已更新");
        }

        public GameParams GetGameParams()
        {
            return _gameParams;
        }

        public async Task<UserInfo> GetUserInfoAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _gameParams.user_id
                };
                
                var userInfo = await _httpClient.GetAsync<UserInfo>("/sicbo/user/info", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取用户信息成功: {userInfo.user_id}");
                return userInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取用户信息失败: {ex.Message}");
                throw;
            }
        }

        public async Task<UserBalance> RefreshBalanceAsync()
        {
            var userInfo = await GetUserInfoAsync();
            return new UserBalance
            {
                balance = userInfo.balance,
                moneyBalance = userInfo.money_balance,
                currency = userInfo.currency,
                lastUpdated = DateTime.UtcNow
            };
        }

        public async Task<TableInfo> GetTableInfoAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["tableId"] = _gameParams.table_id,
                    ["gameType"] = _gameParams.game_type
                };
                
                var tableInfo = await _httpClient.GetAsync<TableInfo>("/sicbo/get_table/table_info", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取台桌信息成功: {tableInfo.id}");
                return tableInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取台桌信息失败: {ex.Message}");
                throw;
            }
        }

        public async Task<TableRunInfo> GetTableStatusAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["table_id"] = _gameParams.table_id
                };
                
                var status = await _httpClient.GetAsync<TableRunInfo>("/sicbo/table/status", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取台桌状态成功");
                return status;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取台桌状态失败: {ex.Message}");
                throw;
            }
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
            try
            {
                var requestData = new
                {
                    id = int.Parse(_gameParams.table_id)
                };
                
                var response = await _httpClient.PostAsync<CurrentBetsApiResponse>("/sicbo/current/record", requestData);
                
                var betRecords = new List<BetRecord>();
                float totalAmount = 0f;
                
                if (response?.bets != null)
                {
                    foreach (var bet in response.bets)
                    {
                        betRecords.Add(new BetRecord
                        {
                            bet_id = bet.bet_id,
                            bet_type = bet.bet_type,
                            bet_amount = bet.money,
                            game_number = response.game_number,
                            timestamp = DateTime.UtcNow
                        });
                        totalAmount += bet.money;
                    }
                }
                
                Debug.Log($"[HttpBaccaratGameService] 获取当前投注成功，共{betRecords.Count}个投注");
                
                return new CurrentBetsResponse
                {
                    currentBets = betRecords,
                    totalBetAmount = totalAmount,
                    gameNumber = response?.game_number ?? "",
                    timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取当前投注失败: {ex.Message}");
                throw;
            }
        }

        public async Task<CancelBetResponse> CancelPendingBetsAsync()
        {
            try
            {
                var requestData = new
                {
                    user_id = _gameParams.user_id,
                    table_id = _gameParams.table_id
                };
                
                var response = await _httpClient.PostAsync<CancelBetApiResponse>("/sicbo/bet/cancel", requestData);
                
                Debug.Log($"[HttpBaccaratGameService] 取消投注成功");
                
                return new CancelBetResponse
                {
                    success = response.success,
                    message = response.message,
                    cancelledBetIds = response.cancelled_bet_ids ?? new List<string>(),
                    refundAmount = response.refund_amount
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 取消投注失败: {ex.Message}");
                throw;
            }
        }

        public async Task<BettingHistoryResponse> GetBettingHistoryAsync(BettingHistoryParams @params)
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _gameParams.user_id,
                    ["table_id"] = _gameParams.table_id,
                    ["game_type"] = _gameParams.game_type,
                    ["page"] = @params.page,
                    ["page_size"] = @params.pageSize
                };
                
                if (!string.IsNullOrEmpty(@params.start_date))
                    queryParams["start_date"] = @params.start_date;
                
                if (!string.IsNullOrEmpty(@params.end_date))
                    queryParams["end_date"] = @params.end_date;
                
                var response = await _httpClient.GetAsync<BettingHistoryResponse>("/sicbo/bet/history", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取投注历史成功，共{response.total}条记录");
                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取投注历史失败: {ex.Message}");
                throw;
            }
        }

        public async Task<BettingDetailResponse> GetBettingDetailAsync(string recordId)
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _gameParams.user_id
                };
                
                var response = await _httpClient.GetAsync<BettingDetailResponse>($"/sicbo/bet/detail/{recordId}", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取投注详情成功: {recordId}");
                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取投注详情失败: {ex.Message}");
                throw;
            }
        }

        public async Task<RoadmapData> GetRoadmapDataAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["tableId"] = _gameParams.table_id,
                    ["xue"] = 1,
                    ["gameType"] = _gameParams.game_type
                };
                
                var response = await _httpClient.GetAsync<RoadmapApiResponse>("/sicbo/get_table/get_data", queryParams);
                
                var roadmapData = new RoadmapData
                {
                    results = response.results ?? new List<GameResultRecord>(),
                    statistics = response.statistics ?? new RoadmapStatistics(),
                    lastUpdated = DateTime.UtcNow
                };
                
                Debug.Log($"[HttpBaccaratGameService] 获取路纸数据成功");
                return roadmapData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取路纸数据失败: {ex.Message}");
                throw;
            }
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
            try
            {
                var startTime = DateTime.UtcNow;
                await _httpClient.GetRawAsync("/api/health");
                var latency = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                
                return new NetworkStatus
                {
                    isConnected = true,
                    latency = latency,
                    quality = GetConnectionQuality(latency),
                    lastCheck = DateTime.UtcNow
                };
            }
            catch (Exception)
            {
                return new NetworkStatus
                {
                    isConnected = false,
                    latency = -1,
                    quality = "offline",
                    lastCheck = DateTime.UtcNow
                };
            }
        }

        public ApiServiceStatus GetServiceStatus()
        {
            return _serviceStatus;
        }

        #endregion

        #region IBaccaratGameService 百家乐专用实现

        public async Task<BaccaratBetResponse> PlaceBaccaratBetsAsync(List<BaccaratBetRequest> bets, int isExempt = 0)
        {
            try
            {
                var requestData = new
                {
                    table_id = int.Parse(_gameParams.table_id),
                    game_type = int.Parse(_gameParams.game_type),
                    is_exempt = isExempt,
                    bet = bets.ConvertAll(b => new { money = b.money, rate_id = b.rate_id })
                };
                
                Debug.Log($"[HttpBaccaratGameService] 提交投注: {bets.Count}个投注，免佣={isExempt}");
                
                var response = await _httpClient.PostAsync<BaccaratBetApiResponse>("/bjl/bet/order", requestData);
                
                Debug.Log($"[HttpBaccaratGameService] 投注成功，余额: {response.money_balance}");
                
                return new BaccaratBetResponse
                {
                    success = true,
                    message = "投注成功",
                    money_balance = response.money_balance,
                    money_spend = response.money_spend,
                    bets = bets,
                    game_number = response.game_number ?? "",
                    bet_id = response.bet_id ?? Guid.NewGuid().ToString(),
                    bet_time = DateTime.UtcNow,
                    is_exempt_applied = isExempt == 1,
                    exempt_saving = response.exempt_saving
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 投注失败: {ex.Message}");
                
                return new BaccaratBetResponse
                {
                    success = false,
                    message = $"投注失败: {ex.Message}",
                    bets = bets,
                    bet_time = DateTime.UtcNow
                };
            }
        }

        public async Task<BaccaratBetLimits> GetBetLimitsAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["table_id"] = _gameParams.table_id,
                    ["game_type"] = _gameParams.game_type
                };
                
                var response = await _httpClient.GetAsync<BaccaratBetLimitsApiResponse>("/bjl/bet/limits", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取投注限额成功");
                
                return new BaccaratBetLimits
                {
                    bankerMin = response.banker_min,
                    bankerMax = response.banker_max,
                    playerMin = response.player_min,
                    playerMax = response.player_max,
                    tieMin = response.tie_min,
                    tieMax = response.tie_max,
                    pairMin = response.pair_min,
                    pairMax = response.pair_max,
                    bigSmallMin = response.big_small_min,
                    bigSmallMax = response.big_small_max,
                    tableMin = response.table_min,
                    tableMax = response.table_max
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取投注限额失败: {ex.Message}");
                throw;
            }
        }

        public async Task<ExemptSettings> GetExemptSettingsAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _gameParams.user_id,
                    ["table_id"] = _gameParams.table_id
                };
                
                var response = await _httpClient.GetAsync<ExemptSettingsApiResponse>("/bjl/exempt/settings", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取免佣设置成功");
                
                return new ExemptSettings
                {
                    isAvailable = response.is_available,
                    isEnabled = response.is_enabled,
                    exemptRate = response.exempt_rate,
                    breakEvenPoint = response.break_even_point,
                    onlyForBanker = response.only_for_banker,
                    minBetAmount = response.min_bet_amount,
                    description = response.description
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取免佣设置失败: {ex.Message}");
                throw;
            }
        }

        public async Task<BaccaratGameState> GetGameStateAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["table_id"] = _gameParams.table_id
                };
                
                var response = await _httpClient.GetAsync<BaccaratGameStateApiResponse>("/bjl/game/state", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取游戏状态成功: {response.game_number}");
                
                return new BaccaratGameState
                {
                    game_number = response.game_number,
                    phase = ParseGamePhase(response.phase),
                    countdown = response.countdown,
                    betting_open = response.betting_open,
                    dealer_name = response.dealer_name,
                    table_name = response.table_name,
                    round_number = response.round_number,
                    video_url_near = response.video_url_near,
                    video_url_far = response.video_url_far,
                    video_active = response.video_active
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取游戏状态失败: {ex.Message}");
                throw;
            }
        }

        public async Task<BaccaratGameTiming> GetGameTimingAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["table_id"] = _gameParams.table_id
                };
                
                var response = await _httpClient.GetAsync<BaccaratGameTimingApiResponse>("/bjl/game/timing", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取游戏计时成功");
                
                return new BaccaratGameTiming
                {
                    game_number = response.game_number,
                    betting_countdown = response.betting_countdown,
                    total_countdown = response.total_countdown,
                    start_time = DateTimeOffset.FromUnixTimeSeconds(response.start_time).DateTime,
                    end_time = DateTimeOffset.FromUnixTimeSeconds(response.end_time).DateTime,
                    betting_duration = response.betting_duration,
                    dealing_duration = response.dealing_duration,
                    result_duration = response.result_duration
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取游戏计时失败: {ex.Message}");
                throw;
            }
        }

        public async Task<BaccaratGameResult> GetGameResultAsync(string gameNumber)
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["game_number"] = gameNumber,
                    ["table_id"] = _gameParams.table_id
                };
                
                var response = await _httpClient.GetAsync<BaccaratGameResultApiResponse>("/bjl/game/result", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取游戏结果成功: {gameNumber}");
                
                return new BaccaratGameResult
                {
                    game_number = response.game_number,
                    winner = ParseWinner(response.winner),
                    banker_points = response.banker_points,
                    player_points = response.player_points,
                    banker_cards = response.banker_cards ?? new List<BaccaratCard>(),
                    player_cards = response.player_cards ?? new List<BaccaratCard>(),
                    banker_pair = response.banker_pair,
                    player_pair = response.player_pair,
                    is_big = response.is_big,
                    winning_bets = response.winning_bets ?? new List<string>(),
                    total_payout = response.total_payout,
                    result_time = DateTimeOffset.FromUnixTimeSeconds(response.result_time).DateTime
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取游戏结果失败: {ex.Message}");
                throw;
            }
        }

        public async Task<BaccaratRoadmaps> GetBaccaratRoadmapsAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["table_id"] = _gameParams.table_id,
                    ["count"] = 100
                };
                
                var response = await _httpClient.GetAsync<BaccaratRoadmapsApiResponse>("/bjl/roadmaps", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取路纸数据成功");
                
                return new BaccaratRoadmaps
                {
                    main_road = response.main_road ?? new List<RoadmapBead>(),
                    big_eye_road = response.big_eye_road ?? new List<RoadmapBead>(),
                    small_road = response.small_road ?? new List<RoadmapBead>(),
                    cockroach_road = response.cockroach_road ?? new List<RoadmapBead>(),
                    total_games = response.total_games,
                    banker_wins = response.banker_wins,
                    player_wins = response.player_wins,
                    ties = response.ties,
                    last_updated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取路纸数据失败: {ex.Message}");
                throw;
            }
        }

        public async Task<List<BaccaratHistoryResult>> GetHistoryResultsAsync(int count = 50)
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["table_id"] = _gameParams.table_id,
                    ["count"] = count
                };
                
                var response = await _httpClient.GetAsync<List<BaccaratHistoryResultApiResponse>>("/bjl/history/results", queryParams);
                
                var results = new List<BaccaratHistoryResult>();
                
                foreach (var item in response)
                {
                    results.Add(new BaccaratHistoryResult
                    {
                        game_number = item.game_number,
                        winner = ParseWinner(item.winner),
                        banker_pair = item.banker_pair,
                        player_pair = item.player_pair,
                        is_big = item.is_big,
                        banker_points = item.banker_points,
                        player_points = item.player_points,
                        game_time = DateTimeOffset.FromUnixTimeSeconds(item.game_time).DateTime
                    });
                }
                
                Debug.Log($"[HttpBaccaratGameService] 获取历史结果成功，共{results.Count}条");
                return results;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取历史结果失败: {ex.Message}");
                throw;
            }
        }

        public async Task<BaccaratStatistics> GetStatisticsAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["table_id"] = _gameParams.table_id
                };
                
                var response = await _httpClient.GetAsync<BaccaratStatisticsApiResponse>("/bjl/statistics", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取统计数据成功");
                
                return new BaccaratStatistics
                {
                    total_games = response.total_games,
                    banker_wins = response.banker_wins,
                    player_wins = response.player_wins,
                    ties = response.ties,
                    banker_win_rate = response.banker_win_rate,
                    player_win_rate = response.player_win_rate,
                    tie_rate = response.tie_rate,
                    banker_max_streak = response.banker_max_streak,
                    player_max_streak = response.player_max_streak,
                    current_streak = response.current_streak,
                    streak_side = ParseWinner(response.streak_side),
                    banker_pair_count = response.banker_pair_count,
                    player_pair_count = response.player_pair_count,
                    banker_pair_rate = response.banker_pair_rate,
                    player_pair_rate = response.player_pair_rate
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取统计数据失败: {ex.Message}");
                throw;
            }
        }

        public async Task<BaccaratPrediction> GetPredictionAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["table_id"] = _gameParams.table_id
                };
                
                var response = await _httpClient.GetAsync<BaccaratPredictionApiResponse>("/bjl/prediction", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取预测数据成功");
                
                return new BaccaratPrediction
                {
                    predicted_winner = ParseWinner(response.predicted_winner),
                    confidence = response.confidence,
                    prediction_method = response.prediction_method,
                    reasoning = response.reasoning ?? new List<string>(),
                    trend_analysis = response.trend_analysis,
                    is_streak_likely = response.is_streak_likely,
                    banker_probability = response.banker_probability,
                    player_probability = response.player_probability,
                    tie_probability = response.tie_probability
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取预测数据失败: {ex.Message}");
                throw;
            }
        }

        public async Task<PopularBets> GetPopularBetsAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["table_id"] = _gameParams.table_id
                };
                
                var response = await _httpClient.GetAsync<PopularBetsApiResponse>("/bjl/popular_bets", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取热门投注成功");
                
                return new PopularBets
                {
                    banker_popularity = response.banker_popularity,
                    player_popularity = response.player_popularity,
                    tie_popularity = response.tie_popularity,
                    pair_popularity = response.pair_popularity,
                    banker_total_amount = response.banker_total_amount,
                    player_total_amount = response.player_total_amount,
                    tie_total_amount = response.tie_total_amount,
                    banker_bet_count = response.banker_bet_count,
                    player_bet_count = response.player_bet_count,
                    tie_bet_count = response.tie_bet_count
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取热门投注失败: {ex.Message}");
                throw;
            }
        }

        public async Task<BaccaratOdds> GetOddsAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["table_id"] = _gameParams.table_id
                };
                
                var response = await _httpClient.GetAsync<BaccaratOddsApiResponse>("/bjl/odds", queryParams);
                
                Debug.Log($"[HttpBaccaratGameService] 获取赔率信息成功");
                
                return new BaccaratOdds
                {
                    banker_odds = response.banker_odds,
                    player_odds = response.player_odds,
                    tie_odds = response.tie_odds,
                    banker_pair_odds = response.banker_pair_odds,
                    player_pair_odds = response.player_pair_odds,
                    big_odds = response.big_odds,
                    small_odds = response.small_odds,
                    banker_no_commission_odds = response.banker_no_commission_odds,
                    has_special_rules = response.has_special_rules,
                    special_rules_desc = response.special_rules_desc
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpBaccaratGameService] 获取赔率信息失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 私有方法

        private void OnHttpError(HttpError error)
        {
            var apiError = new ApiError
            {
                code = error.statusCode,
                message = error.message,
                type = "http",
                timestamp = error.timestamp,
                recoverable = IsRecoverableError(error.statusCode),
                context = new Dictionary<string, object>
                {
                    ["url"] = error.url,
                    ["method"] = error.method
                }
            };
            
            _onError?.Invoke(apiError);
        }

        private void OnAuthFailed()
        {
            Debug.LogWarning("[HttpBaccaratGameService] 认证失败，需要重新登录");
            _onAuthFailed?.Invoke();
        }

        private bool IsRecoverableError(int statusCode)
        {
            // 网络错误、超时、服务器错误等可恢复
            return statusCode == 0 || statusCode >= 500 || statusCode == 408;
        }

        private string GetConnectionQuality(int latency)
        {
            if (latency < 100) return "excellent";
            if (latency < 300) return "good";
            if (latency < 1000) return "poor";
            return "very_poor";
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

        private BaccaratGamePhase ParseGamePhase(string phase)
        {
            switch (phase?.ToLower())
            {
                case "waiting": return BaccaratGamePhase.Waiting;
                case "betting": return BaccaratGamePhase.Betting;
                case "dealing": return BaccaratGamePhase.Dealing;
                case "revealing": return BaccaratGamePhase.Revealing;
                case "result": return BaccaratGamePhase.Result;
                case "settlement": return BaccaratGamePhase.Settlement;
                default: return BaccaratGamePhase.Waiting;
            }
        }

        private BaccaratWinner ParseWinner(string winner)
        {
            switch (winner?.ToLower())
            {
                case "banker": return BaccaratWinner.Banker;
                case "player": return BaccaratWinner.Player;
                case "tie": return BaccaratWinner.Tie;
                default: return BaccaratWinner.Banker;
            }
        }

        #endregion
    }

    #region API响应类型定义

    [System.Serializable]
    public class CurrentBetsApiResponse
    {
        public string game_number;
        public List<CurrentBetItem> bets;
    }

    [System.Serializable]
    public class CurrentBetItem
    {
        public string bet_id;
        public string bet_type;
        public float money;
    }

    [System.Serializable]
    public class CancelBetApiResponse
    {
        public bool success;
        public string message;
        public List<string> cancelled_bet_ids;
        public float refund_amount;
    }

    [System.Serializable]
    public class BaccaratBetApiResponse
    {
        public float money_balance;
        public float money_spend;
        public string game_number;
        public string bet_id;
        public float exempt_saving;
    }

    [System.Serializable]
    public class BaccaratBetLimitsApiResponse
    {
        public float banker_min;
        public float banker_max;
        public float player_min;
        public float player_max;
        public float tie_min;
        public float tie_max;
        public float pair_min;
        public float pair_max;
        public float big_small_min;
        public float big_small_max;
        public float table_min;
        public float table_max;
    }

    [System.Serializable]
    public class ExemptSettingsApiResponse
    {
        public bool is_available;
        public bool is_enabled;
        public float exempt_rate;
        public float break_even_point;
        public bool only_for_banker;
        public float min_bet_amount;
        public string description;
    }

    [System.Serializable]
    public class BaccaratGameStateApiResponse
    {
        public string game_number;
        public string phase;
        public int countdown;
        public bool betting_open;
        public string dealer_name;
        public string table_name;
        public int round_number;
        public string video_url_near;
        public string video_url_far;
        public bool video_active;
    }

    [System.Serializable]
    public class BaccaratGameTimingApiResponse
    {
        public string game_number;
        public int betting_countdown;
        public int total_countdown;
        public long start_time;
        public long end_time;
        public int betting_duration;
        public int dealing_duration;
        public int result_duration;
    }

    [System.Serializable]
    public class BaccaratGameResultApiResponse
    {
        public string game_number;
        public string winner;
        public int banker_points;
        public int player_points;
        public List<BaccaratCard> banker_cards;
        public List<BaccaratCard> player_cards;
        public bool banker_pair;
        public bool player_pair;
        public bool is_big;
        public List<string> winning_bets;
        public float total_payout;
        public long result_time;
    }

    [System.Serializable]
    public class BaccaratRoadmapsApiResponse
    {
        public List<RoadmapBead> main_road;
        public List<RoadmapBead> big_eye_road;
        public List<RoadmapBead> small_road;
        public List<RoadmapBead> cockroach_road;
        public int total_games;
        public int banker_wins;
        public int player_wins;
        public int ties;
    }

    [System.Serializable]
    public class BaccaratHistoryResultApiResponse
    {
        public string game_number;
        public string winner;
        public bool banker_pair;
        public bool player_pair;
        public bool is_big;
        public int banker_points;
        public int player_points;
        public long game_time;
    }

    [System.Serializable]
    public class BaccaratStatisticsApiResponse
    {
        public int total_games;
        public int banker_wins;
        public int player_wins;
        public int ties;
        public float banker_win_rate;
        public float player_win_rate;
        public float tie_rate;
        public int banker_max_streak;
        public int player_max_streak;
        public int current_streak;
        public string streak_side;
        public int banker_pair_count;
        public int player_pair_count;
        public float banker_pair_rate;
        public float player_pair_rate;
    }

    [System.Serializable]
    public class BaccaratPredictionApiResponse
    {
        public string predicted_winner;
        public float confidence;
        public string prediction_method;
        public List<string> reasoning;
        public string trend_analysis;
        public bool is_streak_likely;
        public float banker_probability;
        public float player_probability;
        public float tie_probability;
    }

    [System.Serializable]
    public class PopularBetsApiResponse
    {
        public float banker_popularity;
        public float player_popularity;
        public float tie_popularity;
        public float pair_popularity;
        public float banker_total_amount;
        public float player_total_amount;
        public float tie_total_amount;
        public int banker_bet_count;
        public int player_bet_count;
        public int tie_bet_count;
    }

    [System.Serializable]
    public class BaccaratOddsApiResponse
    {
        public float banker_odds;
        public float player_odds;
        public float tie_odds;
        public float banker_pair_odds;
        public float player_pair_odds;
        public float big_odds;
        public float small_odds;
        public float banker_no_commission_odds;
        public bool has_special_rules;
        public string special_rules_desc;
    }

    [System.Serializable]
    public class RoadmapApiResponse
    {
        public List<GameResultRecord> results;
        public RoadmapStatistics statistics;
    }

    [System.Serializable]
    public class GameResultRecord
    {
        public string game_number;
        public string result;
        public DateTime timestamp;
    }

    #endregion
}