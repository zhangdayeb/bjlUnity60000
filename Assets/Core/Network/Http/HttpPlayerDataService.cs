// Assets/_Core/Network/Http/HttpPlayerDataService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Network.Http;

namespace Core.Network.Http
{
    /// <summary>
    /// HTTP玩家数据服务
    /// 处理玩家相关的数据操作，包括个人信息、游戏记录、偏好设置等
    /// </summary>
    public class HttpPlayerDataService
    {
        #region 私有字段

        private HttpClient _httpClient;
        private string _userId;
        private PlayerDataCache _cache;

        #endregion

        #region 构造函数

        public HttpPlayerDataService(HttpClient httpClient, string userId)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _userId = userId ?? throw new ArgumentNullException(nameof(userId));
            _cache = new PlayerDataCache();
        }

        #endregion

        #region 玩家基础信息

        /// <summary>
        /// 获取玩家详细信息
        /// </summary>
        public async Task<PlayerProfile> GetPlayerProfileAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _userId
                };

                var response = await _httpClient.GetAsync<PlayerProfileApiResponse>("/player/profile", queryParams);

                var profile = new PlayerProfile
                {
                    user_id = response.user_id,
                    username = response.username,
                    nickname = response.nickname,
                    email = response.email,
                    phone = response.phone,
                    avatar_url = response.avatar_url,
                    level = response.level,
                    vip_level = response.vip_level,
                    experience_points = response.experience_points,
                    registration_date = DateTimeOffset.FromUnixTimeSeconds(response.registration_date).DateTime,
                    last_login_date = DateTimeOffset.FromUnixTimeSeconds(response.last_login_date).DateTime,
                    total_games_played = response.total_games_played,
                    total_win_amount = response.total_win_amount,
                    total_bet_amount = response.total_bet_amount,
                    favorite_game_types = response.favorite_game_types ?? new List<string>(),
                    preferred_language = response.preferred_language,
                    timezone = response.timezone,
                    is_verified = response.is_verified,
                    is_active = response.is_active
                };

                _cache.CachePlayerProfile(profile);
                Debug.Log($"[HttpPlayerDataService] 获取玩家信息成功: {profile.username}");

                return profile;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 获取玩家信息失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 更新玩家信息
        /// </summary>
        public async Task<bool> UpdatePlayerProfileAsync(PlayerProfileUpdateRequest updateRequest)
        {
            try
            {
                var requestData = new
                {
                    user_id = _userId,
                    nickname = updateRequest.nickname,
                    email = updateRequest.email,
                    phone = updateRequest.phone,
                    preferred_language = updateRequest.preferred_language,
                    timezone = updateRequest.timezone,
                    avatar_url = updateRequest.avatar_url
                };

                var response = await _httpClient.PutAsync<UpdateResponse>("/player/profile", requestData);

                if (response.success)
                {
                    _cache.InvalidatePlayerProfile();
                    Debug.Log("[HttpPlayerDataService] 玩家信息更新成功");
                }

                return response.success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 更新玩家信息失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取玩家余额信息
        /// </summary>
        public async Task<PlayerBalance> GetPlayerBalanceAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _userId
                };

                var response = await _httpClient.GetAsync<PlayerBalanceApiResponse>("/player/balance", queryParams);

                var balance = new PlayerBalance
                {
                    user_id = response.user_id,
                    total_balance = response.total_balance,
                    available_balance = response.available_balance,
                    frozen_balance = response.frozen_balance,
                    currency = response.currency,
                    last_updated = DateTimeOffset.FromUnixTimeSeconds(response.last_updated).DateTime,
                    daily_win_limit = response.daily_win_limit,
                    daily_loss_limit = response.daily_loss_limit,
                    daily_win_amount = response.daily_win_amount,
                    daily_loss_amount = response.daily_loss_amount
                };

                _cache.CachePlayerBalance(balance);
                Debug.Log($"[HttpPlayerDataService] 获取余额信息成功: {balance.available_balance}");

                return balance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 获取余额信息失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 游戏历史和统计

        /// <summary>
        /// 获取玩家游戏统计
        /// </summary>
        public async Task<PlayerGameStatistics> GetGameStatisticsAsync(string gameType = null, int days = 30)
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _userId,
                    ["days"] = days
                };

                if (!string.IsNullOrEmpty(gameType))
                    queryParams["game_type"] = gameType;

                var response = await _httpClient.GetAsync<PlayerGameStatisticsApiResponse>("/player/statistics", queryParams);

                var statistics = new PlayerGameStatistics
                {
                    user_id = response.user_id,
                    game_type = response.game_type,
                    period_days = response.period_days,
                    total_games = response.total_games,
                    total_wins = response.total_wins,
                    total_losses = response.total_losses,
                    win_rate = response.win_rate,
                    total_bet_amount = response.total_bet_amount,
                    total_win_amount = response.total_win_amount,
                    net_profit = response.net_profit,
                    biggest_win = response.biggest_win,
                    biggest_loss = response.biggest_loss,
                    average_bet_amount = response.average_bet_amount,
                    longest_win_streak = response.longest_win_streak,
                    longest_loss_streak = response.longest_loss_streak,
                    current_streak = response.current_streak,
                    favorite_bet_types = response.favorite_bet_types ?? new List<string>(),
                    most_profitable_bet_type = response.most_profitable_bet_type,
                    games_by_hour = response.games_by_hour ?? new Dictionary<int, int>(),
                    last_updated = DateTimeOffset.FromUnixTimeSeconds(response.last_updated).DateTime
                };

                Debug.Log($"[HttpPlayerDataService] 获取游戏统计成功: {statistics.total_games}局游戏");

                return statistics;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 获取游戏统计失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取玩家游戏历史记录
        /// </summary>
        public async Task<PlayerGameHistoryResponse> GetGameHistoryAsync(GameHistoryRequest request)
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _userId,
                    ["page"] = request.page,
                    ["page_size"] = request.pageSize
                };

                if (!string.IsNullOrEmpty(request.gameType))
                    queryParams["game_type"] = request.gameType;

                if (!string.IsNullOrEmpty(request.startDate))
                    queryParams["start_date"] = request.startDate;

                if (!string.IsNullOrEmpty(request.endDate))
                    queryParams["end_date"] = request.endDate;

                if (request.minBetAmount.HasValue)
                    queryParams["min_bet_amount"] = request.minBetAmount.Value;

                if (request.maxBetAmount.HasValue)
                    queryParams["max_bet_amount"] = request.maxBetAmount.Value;

                if (!string.IsNullOrEmpty(request.resultFilter))
                    queryParams["result_filter"] = request.resultFilter;

                var response = await _httpClient.GetAsync<PlayerGameHistoryApiResponse>("/player/game_history", queryParams);

                var historyResponse = new PlayerGameHistoryResponse
                {
                    records = new List<PlayerGameRecord>(),
                    total = response.total,
                    page = response.page,
                    pageSize = response.page_size,
                    totalPages = response.total_pages
                };

                foreach (var item in response.records)
                {
                    historyResponse.records.Add(new PlayerGameRecord
                    {
                        record_id = item.record_id,
                        game_number = item.game_number,
                        table_id = item.table_id,
                        game_type = item.game_type,
                        bet_time = DateTimeOffset.FromUnixTimeSeconds(item.bet_time).DateTime,
                        game_time = DateTimeOffset.FromUnixTimeSeconds(item.game_time).DateTime,
                        total_bet_amount = item.total_bet_amount,
                        total_win_amount = item.total_win_amount,
                        net_profit = item.net_profit,
                        game_result = item.game_result,
                        bet_details = item.bet_details ?? new List<PlayerBetDetail>(),
                        status = item.status
                    });
                }

                Debug.Log($"[HttpPlayerDataService] 获取游戏历史成功: {historyResponse.total}条记录");

                return historyResponse;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 获取游戏历史失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取玩家单局游戏详情
        /// </summary>
        public async Task<PlayerGameDetail> GetGameDetailAsync(string recordId)
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _userId
                };

                var response = await _httpClient.GetAsync<PlayerGameDetailApiResponse>($"/player/game_detail/{recordId}", queryParams);

                var detail = new PlayerGameDetail
                {
                    record_id = response.record_id,
                    game_number = response.game_number,
                    table_id = response.table_id,
                    table_name = response.table_name,
                    game_type = response.game_type,
                    dealer_name = response.dealer_name,
                    bet_time = DateTimeOffset.FromUnixTimeSeconds(response.bet_time).DateTime,
                    game_start_time = DateTimeOffset.FromUnixTimeSeconds(response.game_start_time).DateTime,
                    game_end_time = DateTimeOffset.FromUnixTimeSeconds(response.game_end_time).DateTime,
                    total_bet_amount = response.total_bet_amount,
                    total_win_amount = response.total_win_amount,
                    net_profit = response.net_profit,
                    game_result = response.game_result,
                    bet_details = response.bet_details ?? new List<PlayerBetDetail>(),
                    game_cards = response.game_cards ?? new List<GameCard>(),
                    video_replay_url = response.video_replay_url,
                    game_duration = response.game_duration,
                    status = response.status,
                    verification_hash = response.verification_hash
                };

                Debug.Log($"[HttpPlayerDataService] 获取游戏详情成功: {recordId}");

                return detail;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 获取游戏详情失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 偏好设置

        /// <summary>
        /// 获取玩家偏好设置
        /// </summary>
        public async Task<PlayerPreferences> GetPlayerPreferencesAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _userId
                };

                var response = await _httpClient.GetAsync<PlayerPreferencesApiResponse>("/player/preferences", queryParams);

                var preferences = new PlayerPreferences
                {
                    user_id = response.user_id,
                    language = response.language,
                    timezone = response.timezone,
                    currency = response.currency,
                    sound_enabled = response.sound_enabled,
                    music_enabled = response.music_enabled,
                    animation_enabled = response.animation_enabled,
                    auto_confirm_bets = response.auto_confirm_bets,
                    show_statistics = response.show_statistics,
                    show_roadmaps = response.show_roadmaps,
                    notification_settings = response.notification_settings ?? new NotificationSettings(),
                    betting_limits = response.betting_limits ?? new BettingLimits(),
                    favorite_tables = response.favorite_tables ?? new List<string>(),
                    custom_settings = response.custom_settings ?? new Dictionary<string, object>(),
                    last_updated = DateTimeOffset.FromUnixTimeSeconds(response.last_updated).DateTime
                };

                _cache.CachePlayerPreferences(preferences);
                Debug.Log("[HttpPlayerDataService] 获取玩家偏好设置成功");

                return preferences;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 获取玩家偏好设置失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 更新玩家偏好设置
        /// </summary>
        public async Task<bool> UpdatePlayerPreferencesAsync(PlayerPreferences preferences)
        {
            try
            {
                var requestData = new
                {
                    user_id = _userId,
                    language = preferences.language,
                    timezone = preferences.timezone,
                    currency = preferences.currency,
                    sound_enabled = preferences.sound_enabled,
                    music_enabled = preferences.music_enabled,
                    animation_enabled = preferences.animation_enabled,
                    auto_confirm_bets = preferences.auto_confirm_bets,
                    show_statistics = preferences.show_statistics,
                    show_roadmaps = preferences.show_roadmaps,
                    notification_settings = preferences.notification_settings,
                    betting_limits = preferences.betting_limits,
                    favorite_tables = preferences.favorite_tables,
                    custom_settings = preferences.custom_settings
                };

                var response = await _httpClient.PutAsync<UpdateResponse>("/player/preferences", requestData);

                if (response.success)
                {
                    _cache.CachePlayerPreferences(preferences);
                    Debug.Log("[HttpPlayerDataService] 玩家偏好设置更新成功");
                }

                return response.success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 更新玩家偏好设置失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 更新单个偏好设置
        /// </summary>
        public async Task<bool> UpdatePreferenceAsync(string key, object value)
        {
            try
            {
                var requestData = new
                {
                    user_id = _userId,
                    key = key,
                    value = value
                };

                var response = await _httpClient.PutAsync<UpdateResponse>("/player/preference", requestData);

                if (response.success)
                {
                    _cache.InvalidatePlayerPreferences();
                    Debug.Log($"[HttpPlayerDataService] 偏好设置更新成功: {key} = {value}");
                }

                return response.success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 更新偏好设置失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 成就和任务

        /// <summary>
        /// 获取玩家成就列表
        /// </summary>
        public async Task<List<PlayerAchievement>> GetPlayerAchievementsAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _userId
                };

                var response = await _httpClient.GetAsync<List<PlayerAchievementApiResponse>>("/player/achievements", queryParams);

                var achievements = new List<PlayerAchievement>();

                foreach (var item in response)
                {
                    achievements.Add(new PlayerAchievement
                    {
                        achievement_id = item.achievement_id,
                        title = item.title,
                        description = item.description,
                        icon_url = item.icon_url,
                        category = item.category,
                        difficulty = item.difficulty,
                        is_unlocked = item.is_unlocked,
                        unlock_date = item.unlock_date.HasValue ? DateTimeOffset.FromUnixTimeSeconds(item.unlock_date.Value).DateTime : (DateTime?)null,
                        progress = item.progress,
                        max_progress = item.max_progress,
                        reward_type = item.reward_type,
                        reward_amount = item.reward_amount,
                        is_claimed = item.is_claimed
                    });
                }

                Debug.Log($"[HttpPlayerDataService] 获取玩家成就成功: {achievements.Count}个成就");

                return achievements;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 获取玩家成就失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 领取成就奖励
        /// </summary>
        public async Task<ClaimRewardResponse> ClaimAchievementRewardAsync(string achievementId)
        {
            try
            {
                var requestData = new
                {
                    user_id = _userId,
                    achievement_id = achievementId
                };

                var response = await _httpClient.PostAsync<ClaimRewardApiResponse>("/player/claim_achievement", requestData);

                var claimResponse = new ClaimRewardResponse
                {
                    success = response.success,
                    message = response.message,
                    reward_type = response.reward_type,
                    reward_amount = response.reward_amount,
                    new_balance = response.new_balance
                };

                if (response.success)
                {
                    _cache.InvalidatePlayerBalance(); // 余额可能有变化
                    Debug.Log($"[HttpPlayerDataService] 成就奖励领取成功: {achievementId}");
                }

                return claimResponse;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 领取成就奖励失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取每日任务列表
        /// </summary>
        public async Task<List<DailyTask>> GetDailyTasksAsync()
        {
            try
            {
                var queryParams = new Dictionary<string, object>
                {
                    ["user_id"] = _userId
                };

                var response = await _httpClient.GetAsync<List<DailyTaskApiResponse>>("/player/daily_tasks", queryParams);

                var tasks = new List<DailyTask>();

                foreach (var item in response)
                {
                    tasks.Add(new DailyTask
                    {
                        task_id = item.task_id,
                        title = item.title,
                        description = item.description,
                        task_type = item.task_type,
                        target_value = item.target_value,
                        current_progress = item.current_progress,
                        is_completed = item.is_completed,
                        completion_date = item.completion_date.HasValue ? DateTimeOffset.FromUnixTimeSeconds(item.completion_date.Value).DateTime : (DateTime?)null,
                        reward_type = item.reward_type,
                        reward_amount = item.reward_amount,
                        is_claimed = item.is_claimed,
                        expires_at = DateTimeOffset.FromUnixTimeSeconds(item.expires_at).DateTime
                    });
                }

                Debug.Log($"[HttpPlayerDataService] 获取每日任务成功: {tasks.Count}个任务");

                return tasks;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 获取每日任务失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 领取每日任务奖励
        /// </summary>
        public async Task<ClaimRewardResponse> ClaimDailyTaskRewardAsync(string taskId)
        {
            try
            {
                var requestData = new
                {
                    user_id = _userId,
                    task_id = taskId
                };

                var response = await _httpClient.PostAsync<ClaimRewardApiResponse>("/player/claim_daily_task", requestData);

                var claimResponse = new ClaimRewardResponse
                {
                    success = response.success,
                    message = response.message,
                    reward_type = response.reward_type,
                    reward_amount = response.reward_amount,
                    new_balance = response.new_balance
                };

                if (response.success)
                {
                    _cache.InvalidatePlayerBalance();
                    Debug.Log($"[HttpPlayerDataService] 每日任务奖励领取成功: {taskId}");
                }

                return claimResponse;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpPlayerDataService] 领取每日任务奖励失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 获取缓存的玩家信息
        /// </summary>
        public PlayerProfile GetCachedPlayerProfile()
        {
            return _cache.GetCachedPlayerProfile();
        }

        /// <summary>
        /// 获取缓存的余额信息
        /// </summary>
        public PlayerBalance GetCachedPlayerBalance()
        {
            return _cache.GetCachedPlayerBalance();
        }

        /// <summary>
        /// 获取缓存的偏好设置
        /// </summary>
        public PlayerPreferences GetCachedPlayerPreferences()
        {
            return _cache.GetCachedPlayerPreferences();
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearCache()
        {
            _cache.ClearAll();
            Debug.Log("[HttpPlayerDataService] 缓存已清除");
        }

        #endregion
    }

    #region 数据类型定义

    [System.Serializable]
    public class PlayerProfile
    {
        public string user_id;
        public string username;
        public string nickname;
        public string email;
        public string phone;
        public string avatar_url;
        public int level;
        public int vip_level;
        public long experience_points;
        public DateTime registration_date;
        public DateTime last_login_date;
        public int total_games_played;
        public float total_win_amount;
        public float total_bet_amount;
        public List<string> favorite_game_types;
        public string preferred_language;
        public string timezone;
        public bool is_verified;
        public bool is_active;
    }

    [System.Serializable]
    public class PlayerProfileUpdateRequest
    {
        public string nickname;
        public string email;
        public string phone;
        public string preferred_language;
        public string timezone;
        public string avatar_url;
    }

    [System.Serializable]
    public class PlayerBalance
    {
        public string user_id;
        public float total_balance;
        public float available_balance;
        public float frozen_balance;
        public string currency;
        public DateTime last_updated;
        public float daily_win_limit;
        public float daily_loss_limit;
        public float daily_win_amount;
        public float daily_loss_amount;
    }

    [System.Serializable]
    public class PlayerGameStatistics
    {
        public string user_id;
        public string game_type;
        public int period_days;
        public int total_games;
        public int total_wins;
        public int total_losses;
        public float win_rate;
        public float total_bet_amount;
        public float total_win_amount;
        public float net_profit;
        public float biggest_win;
        public float biggest_loss;
        public float average_bet_amount;
        public int longest_win_streak;
        public int longest_loss_streak;
        public int current_streak;
        public List<string> favorite_bet_types;
        public string most_profitable_bet_type;
        public Dictionary<int, int> games_by_hour;
        public DateTime last_updated;
    }

    [System.Serializable]
    public class GameHistoryRequest
    {
        public int page = 1;
        public int pageSize = 20;
        public string gameType;
        public string startDate;
        public string endDate;
        public float? minBetAmount;
        public float? maxBetAmount;
        public string resultFilter; // "win", "loss", "tie"
    }

    [System.Serializable]
    public class PlayerGameHistoryResponse
    {
        public List<PlayerGameRecord> records;
        public int total;
        public int page;
        public int pageSize;
        public int totalPages;
    }

    [System.Serializable]
    public class PlayerGameRecord
    {
        public string record_id;
        public string game_number;
        public string table_id;
        public string game_type;
        public DateTime bet_time;
        public DateTime game_time;
        public float total_bet_amount;
        public float total_win_amount;
        public float net_profit;
        public string game_result;
        public List<PlayerBetDetail> bet_details;
        public string status;
    }

    [System.Serializable]
    public class PlayerBetDetail
    {
        public string bet_type;
        public float bet_amount;
        public float win_amount;
        public float odds;
        public bool is_win;
    }

    [System.Serializable]
    public class PlayerGameDetail
    {
        public string record_id;
        public string game_number;
        public string table_id;
        public string table_name;
        public string game_type;
        public string dealer_name;
        public DateTime bet_time;
        public DateTime game_start_time;
        public DateTime game_end_time;
        public float total_bet_amount;
        public float total_win_amount;
        public float net_profit;
        public string game_result;
        public List<PlayerBetDetail> bet_details;
        public List<GameCard> game_cards;
        public string video_replay_url;
        public int game_duration;
        public string status;
        public string verification_hash;
    }

    [System.Serializable]
    public class GameCard
    {
        public string suit;
        public string rank;
        public string display_name;
        public string image_url;
    }

    [System.Serializable]
    public class PlayerPreferences
    {
        public string user_id;
        public string language;
        public string timezone;
        public string currency;
        public bool sound_enabled;
        public bool music_enabled;
        public bool animation_enabled;
        public bool auto_confirm_bets;
        public bool show_statistics;
        public bool show_roadmaps;
        public NotificationSettings notification_settings;
        public BettingLimits betting_limits;
        public List<string> favorite_tables;
        public Dictionary<string, object> custom_settings;
        public DateTime last_updated;
    }

    [System.Serializable]
    public class NotificationSettings
    {
        public bool game_start = true;
        public bool game_result = true;
        public bool win_notification = true;
        public bool achievement_unlock = true;
        public bool daily_task_complete = true;
        public bool balance_low_warning = true;
        public bool system_maintenance = true;
    }

    [System.Serializable]
    public class BettingLimits
    {
        public float daily_limit = 1000f;
        public float single_bet_limit = 500f;
        public float loss_limit = 500f;
        public bool enable_limits = true;
    }

    [System.Serializable]
    public class PlayerAchievement
    {
        public string achievement_id;
        public string title;
        public string description;
        public string icon_url;
        public string category;
        public string difficulty;
        public bool is_unlocked;
        public DateTime? unlock_date;
        public int progress;
        public int max_progress;
        public string reward_type;
        public float reward_amount;
        public bool is_claimed;
    }

    [System.Serializable]
    public class DailyTask
    {
        public string task_id;
        public string title;
        public string description;
        public string task_type;
        public int target_value;
        public int current_progress;
        public bool is_completed;
        public DateTime? completion_date;
        public string reward_type;
        public float reward_amount;
        public bool is_claimed;
        public DateTime expires_at;
    }

    [System.Serializable]
    public class ClaimRewardResponse
    {
        public bool success;
        public string message;
        public string reward_type;
        public float reward_amount;
        public float new_balance;
    }

    [System.Serializable]
    public class UpdateResponse
    {
        public bool success;
        public string message;
    }

    #endregion

    #region API响应类型

    [System.Serializable]
    public class PlayerProfileApiResponse
    {
        public string user_id;
        public string username;
        public string nickname;
        public string email;
        public string phone;
        public string avatar_url;
        public int level;
        public int vip_level;
        public long experience_points;
        public long registration_date;
        public long last_login_date;
        public int total_games_played;
        public float total_win_amount;
        public float total_bet_amount;
        public List<string> favorite_game_types;
        public string preferred_language;
        public string timezone;
        public bool is_verified;
        public bool is_active;
    }

    [System.Serializable]
    public class PlayerBalanceApiResponse
    {
        public string user_id;
        public float total_balance;
        public float available_balance;
        public float frozen_balance;
        public string currency;
        public long last_updated;
        public float daily_win_limit;
        public float daily_loss_limit;
        public float daily_win_amount;
        public float daily_loss_amount;
    }

    [System.Serializable]
    public class PlayerGameStatisticsApiResponse
    {
        public string user_id;
        public string game_type;
        public int period_days;
        public int total_games;
        public int total_wins;
        public int total_losses;
        public float win_rate;
        public float total_bet_amount;
        public float total_win_amount;
        public float net_profit;
        public float biggest_win;
        public float biggest_loss;
        public float average_bet_amount;
        public int longest_win_streak;
        public int longest_loss_streak;
        public int current_streak;
        public List<string> favorite_bet_types;
        public string most_profitable_bet_type;
        public Dictionary<int, int> games_by_hour;
        public long last_updated;
    }

    [System.Serializable]
    public class PlayerGameHistoryApiResponse
    {
        public List<PlayerGameRecordApiResponse> records;
        public int total;
        public int page;
        public int page_size;
        public int total_pages;
    }

    [System.Serializable]
    public class PlayerGameRecordApiResponse
    {
        public string record_id;
        public string game_number;
        public string table_id;
        public string game_type;
        public long bet_time;
        public long game_time;
        public float total_bet_amount;
        public float total_win_amount;
        public float net_profit;
        public string game_result;
        public List<PlayerBetDetail> bet_details;
        public string status;
    }

    [System.Serializable]
    public class PlayerGameDetailApiResponse
    {
        public string record_id;
        public string game_number;
        public string table_id;
        public string table_name;
        public string game_type;
        public string dealer_name;
        public long bet_time;
        public long game_start_time;
        public long game_end_time;
        public float total_bet_amount;
        public float total_win_amount;
        public float net_profit;
        public string game_result;
        public List<PlayerBetDetail> bet_details;
        public List<GameCard> game_cards;
        public string video_replay_url;
        public int game_duration;
        public string status;
        public string verification_hash;
    }

    [System.Serializable]
    public class PlayerPreferencesApiResponse
    {
        public string user_id;
        public string language;
        public string timezone;
        public string currency;
        public bool sound_enabled;
        public bool music_enabled;
        public bool animation_enabled;
        public bool auto_confirm_bets;
        public bool show_statistics;
        public bool show_roadmaps;
        public NotificationSettings notification_settings;
        public BettingLimits betting_limits;
        public List<string> favorite_tables;
        public Dictionary<string, object> custom_settings;
        public long last_updated;
    }

    [System.Serializable]
    public class PlayerAchievementApiResponse
    {
        public string achievement_id;
        public string title;
        public string description;
        public string icon_url;
        public string category;
        public string difficulty;
        public bool is_unlocked;
        public long? unlock_date;
        public int progress;
        public int max_progress;
        public string reward_type;
        public float reward_amount;
        public bool is_claimed;
    }

    [System.Serializable]
    public class DailyTaskApiResponse
    {
        public string task_id;
        public string title;
        public string description;
        public string task_type;
        public int target_value;
        public int current_progress;
        public bool is_completed;
        public long? completion_date;
        public string reward_type;
        public float reward_amount;
        public bool is_claimed;
        public long expires_at;
    }

    [System.Serializable]
    public class ClaimRewardApiResponse
    {
        public bool success;
        public string message;
        public string reward_type;
        public float reward_amount;
        public float new_balance;
    }

    #endregion

    #region 缓存系统

    public class PlayerDataCache
    {
        private PlayerProfile _cachedProfile;
        private PlayerBalance _cachedBalance;
        private PlayerPreferences _cachedPreferences;
        private DateTime _profileCacheTime;
        private DateTime _balanceCacheTime;
        private DateTime _preferencesCacheTime;
        
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public void CachePlayerProfile(PlayerProfile profile)
        {
            _cachedProfile = profile;
            _profileCacheTime = DateTime.UtcNow;
        }

        public PlayerProfile GetCachedPlayerProfile()
        {
            if (_cachedProfile != null && DateTime.UtcNow - _profileCacheTime < _cacheExpiry)
            {
                return _cachedProfile;
            }
            return null;
        }

        public void InvalidatePlayerProfile()
        {
            _cachedProfile = null;
        }

        public void CachePlayerBalance(PlayerBalance balance)
        {
            _cachedBalance = balance;
            _balanceCacheTime = DateTime.UtcNow;
        }

        public PlayerBalance GetCachedPlayerBalance()
        {
            if (_cachedBalance != null && DateTime.UtcNow - _balanceCacheTime < TimeSpan.FromMinutes(1)) // 余额缓存时间更短
            {
                return _cachedBalance;
            }
            return null;
        }

        public void InvalidatePlayerBalance()
        {
            _cachedBalance = null;
        }

        public void CachePlayerPreferences(PlayerPreferences preferences)
        {
            _cachedPreferences = preferences;
            _preferencesCacheTime = DateTime.UtcNow;
        }

        public PlayerPreferences GetCachedPlayerPreferences()
        {
            if (_cachedPreferences != null && DateTime.UtcNow - _preferencesCacheTime < _cacheExpiry)
            {
                return _cachedPreferences;
            }
            return null;
        }

        public void InvalidatePlayerPreferences()
        {
            _cachedPreferences = null;
        }

        public void ClearAll()
        {
            _cachedProfile = null;
            _cachedBalance = null;
            _cachedPreferences = null;
        }
    }

    #endregion
}