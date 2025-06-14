// Assets/_Core/Network/Interfaces/IBaccaratGameService.cs
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Core.Network.Interfaces
{
    /// <summary>
    /// 百家乐专用游戏服务接口
    /// 对应JavaScript中的bjlService，提供百家乐特有的API操作
    /// </summary>
    public interface IBaccaratGameService : IGameApiService
    {
        #region 百家乐专用投注
        
        /// <summary>
        /// 百家乐投注 - 对应 /bjl/bet/order
        /// </summary>
        /// <param name="bets">投注列表</param>
        /// <param name="isExempt">是否免佣（0=否，1=是）</param>
        /// <returns>投注结果</returns>
        Task<BaccaratBetResponse> PlaceBaccaratBetsAsync(List<BaccaratBetRequest> bets, int isExempt = 0);
        
        /// <summary>
        /// 获取百家乐投注限额
        /// </summary>
        /// <returns>投注限额信息</returns>
        Task<BaccaratBetLimits> GetBetLimitsAsync();
        
        /// <summary>
        /// 获取免佣设置
        /// </summary>
        /// <returns>免佣配置</returns>
        Task<ExemptSettings> GetExemptSettingsAsync();
        
        #endregion

        #region 百家乐游戏状态
        
        /// <summary>
        /// 获取当前游戏状态
        /// </summary>
        /// <returns>百家乐游戏状态</returns>
        Task<BaccaratGameState> GetGameStateAsync();
        
        /// <summary>
        /// 获取当前局号和倒计时
        /// </summary>
        /// <returns>游戏时间信息</returns>
        Task<BaccaratGameTiming> GetGameTimingAsync();
        
        /// <summary>
        /// 获取开牌结果
        /// </summary>
        /// <param name="gameNumber">局号</param>
        /// <returns>开牌结果</returns>
        Task<BaccaratGameResult> GetGameResultAsync(string gameNumber);
        
        #endregion

        #region 百家乐路纸和统计
        
        /// <summary>
        /// 获取百家乐路纸 - 大路、大眼仔、小路、蟑螂路
        /// </summary>
        /// <returns>完整路纸数据</returns>
        Task<BaccaratRoadmaps> GetBaccaratRoadmapsAsync();
        
        /// <summary>
        /// 获取历史开奖结果
        /// </summary>
        /// <param name="count">获取数量，默认50局</param>
        /// <returns>历史结果</returns>
        Task<List<BaccaratHistoryResult>> GetHistoryResultsAsync(int count = 50);
        
        /// <summary>
        /// 获取庄闲统计
        /// </summary>
        /// <returns>庄闲统计数据</returns>
        Task<BaccaratStatistics> GetStatisticsAsync();
        
        #endregion

        #region 百家乐特殊功能
        
        /// <summary>
        /// 获取预测数据（如果支持）
        /// </summary>
        /// <returns>预测信息</returns>
        Task<BaccaratPrediction> GetPredictionAsync();
        
        /// <summary>
        /// 获取热门投注区域
        /// </summary>
        /// <returns>热门投注统计</returns>
        Task<PopularBets> GetPopularBetsAsync();
        
        /// <summary>
        /// 获取赔率信息
        /// </summary>
        /// <returns>当前赔率</returns>
        Task<BaccaratOdds> GetOddsAsync();
        
        #endregion
    }

    #region 百家乐专用数据类型

    /// <summary>
    /// 百家乐投注请求
    /// </summary>
    [System.Serializable]
    public class BaccaratBetRequest
    {
        [UnityEngine.Header("投注信息")]
        public float money;           // 投注金额
        public int rate_id;          // 投注类型ID（1=庄,2=闲,3=和,4=庄对,5=闲对,6=大,7=小）
        public string betType;       // 投注类型名称（用于显示）
        
        [UnityEngine.Header("扩展信息")]
        public bool isMainBet;       // 是否主要投注
        public float originalAmount; // 原始金额（用于计算）
        public System.DateTime timestamp; // 投注时间
    }

    /// <summary>
    /// 百家乐投注响应
    /// </summary>
    [System.Serializable]
    public class BaccaratBetResponse
    {
        [UnityEngine.Header("投注结果")]
        public bool success;
        public string message;
        public float money_balance;   // 剩余余额
        public float money_spend;     // 本次花费
        public List<BaccaratBetRequest> bets; // 确认的投注
        
        [UnityEngine.Header("游戏信息")]
        public string game_number;    // 当前局号
        public string bet_id;         // 投注ID
        public System.DateTime bet_time; // 投注时间
        
        [UnityEngine.Header("免佣信息")]
        public bool is_exempt_applied; // 是否应用了免佣
        public float exempt_saving;   // 免佣节省金额
    }

    /// <summary>
    /// 百家乐投注限额
    /// </summary>
    [System.Serializable]
    public class BaccaratBetLimits
    {
        [UnityEngine.Header("主要投注限额")]
        public float bankerMin;       // 庄家最小投注
        public float bankerMax;       // 庄家最大投注
        public float playerMin;       // 闲家最小投注
        public float playerMax;       // 闲家最大投注
        public float tieMin;          // 和局最小投注
        public float tieMax;          // 和局最大投注
        
        [UnityEngine.Header("对子投注限额")]
        public float pairMin;         // 对子最小投注
        public float pairMax;         // 对子最大投注
        
        [UnityEngine.Header("大小投注限额")]
        public float bigSmallMin;     // 大小最小投注
        public float bigSmallMax;     // 大小最大投注
        
        [UnityEngine.Header("表格限额")]
        public float tableMin;        // 台桌最小投注
        public float tableMax;        // 台桌最大投注
    }

    /// <summary>
    /// 免佣设置
    /// </summary>
    [System.Serializable]
    public class ExemptSettings
    {
        [UnityEngine.Header("免佣配置")]
        public bool isAvailable;      // 是否提供免佣
        public bool isEnabled;        // 用户是否启用免佣
        public float exemptRate;      // 免佣费率（通常为5%）
        public float breakEvenPoint;  // 盈亏平衡点
        
        [UnityEngine.Header("免佣规则")]
        public bool onlyForBanker;    // 是否仅限庄家投注
        public float minBetAmount;    // 最小投注金额要求
        public string description;    // 免佣说明
    }

    /// <summary>
    /// 百家乐游戏状态
    /// </summary>
    [System.Serializable]
    public class BaccaratGameState
    {
        [UnityEngine.Header("基本状态")]
        public string game_number;    // 当前局号
        public BaccaratGamePhase phase; // 游戏阶段
        public int countdown;         // 倒计时（秒）
        public bool betting_open;     // 是否可以投注
        
        [UnityEngine.Header("桌台信息")]
        public string dealer_name;    // 荷官名称
        public string table_name;     // 桌台名称
        public int round_number;      // 轮次号
        
        [UnityEngine.Header("视频信息")]
        public string video_url_near; // 近景视频URL
        public string video_url_far;  // 远景视频URL
        public bool video_active;     // 视频是否活跃
    }

    /// <summary>
    /// 百家乐游戏阶段
    /// </summary>
    public enum BaccaratGamePhase
    {
        Waiting,     // 等待开始
        Betting,     // 投注阶段
        Dealing,     // 发牌阶段
        Revealing,   // 开牌阶段
        Result,      // 结果阶段
        Settlement   // 结算阶段
    }

    /// <summary>
    /// 百家乐游戏计时
    /// </summary>
    [System.Serializable]
    public class BaccaratGameTiming
    {
        [UnityEngine.Header("时间信息")]
        public string game_number;      // 局号
        public int betting_countdown;   // 投注倒计时
        public int total_countdown;     // 总倒计时
        public System.DateTime start_time; // 开始时间
        public System.DateTime end_time;   // 结束时间
        
        [UnityEngine.Header("阶段时间")]
        public int betting_duration;   // 投注时长
        public int dealing_duration;   // 发牌时长
        public int result_duration;    // 结果显示时长
    }

    /// <summary>
    /// 百家乐游戏结果
    /// </summary>
    [System.Serializable]
    public class BaccaratGameResult
    {
        [UnityEngine.Header("基本结果")]
        public string game_number;    // 局号
        public BaccaratWinner winner; // 获胜方
        public int banker_points;     // 庄家点数
        public int player_points;     // 闲家点数
        
        [UnityEngine.Header("牌面信息")]
        public List<BaccaratCard> banker_cards; // 庄家牌
        public List<BaccaratCard> player_cards; // 闲家牌
        
        [UnityEngine.Header("特殊结果")]
        public bool banker_pair;      // 庄对
        public bool player_pair;      // 闲对
        public bool is_big;          // 大（总牌数>=5）
        public bool is_small;        // 小（总牌数==4）
        
        [UnityEngine.Header("中奖信息")]
        public List<string> winning_bets; // 中奖投注类型
        public float total_payout;    // 总派彩
        public System.DateTime result_time; // 结果时间
    }

    /// <summary>
    /// 百家乐获胜方
    /// </summary>
    public enum BaccaratWinner
    {
        Banker,  // 庄胜
        Player,  // 闲胜
        Tie      // 和局
    }

    /// <summary>
    /// 百家乐牌面
    /// </summary>
    [System.Serializable]
    public class BaccaratCard
    {
        [UnityEngine.Header("牌面信息")]
        public int suit;             // 花色（1=♠,2=♥,3=♣,4=♦）
        public int rank;             // 点数（1-13）
        public string display_name;   // 显示名称（如"♠A"）
        public int baccarat_value;   // 百家乐点数（0-9）
        
        [UnityEngine.Header("显示信息")]
        public string image_url;     // 牌面图片URL
        public string back_image_url; // 背面图片URL
        public bool is_revealed;     // 是否已翻开
    }

    /// <summary>
    /// 百家乐路纸集合
    /// </summary>
    [System.Serializable]
    public class BaccaratRoadmaps
    {
        [UnityEngine.Header("路纸数据")]
        public List<RoadmapBead> main_road;     // 大路
        public List<RoadmapBead> big_eye_road;  // 大眼仔路
        public List<RoadmapBead> small_road;    // 小路
        public List<RoadmapBead> cockroach_road; // 蟑螂路
        
        [UnityEngine.Header("统计信息")]
        public int total_games;       // 总局数
        public int banker_wins;       // 庄胜局数
        public int player_wins;       // 闲胜局数
        public int ties;             // 和局数
        public System.DateTime last_updated; // 最后更新时间
    }

    /// <summary>
    /// 路纸珠子
    /// </summary>
    [System.Serializable]
    public class RoadmapBead
    {
        public BaccaratWinner result; // 结果
        public bool banker_pair;      // 庄对
        public bool player_pair;      // 闲对
        public int position_x;        // X坐标
        public int position_y;        // Y坐标
        public string game_number;    // 局号
    }

    /// <summary>
    /// 百家乐历史结果
    /// </summary>
    [System.Serializable]
    public class BaccaratHistoryResult
    {
        public string game_number;    // 局号
        public BaccaratWinner winner; // 结果
        public bool banker_pair;      // 庄对
        public bool player_pair;      // 闲对
        public bool is_big;          // 大
        public int banker_points;     // 庄点数
        public int player_points;     // 闲点数
        public System.DateTime game_time; // 游戏时间
    }

    /// <summary>
    /// 百家乐统计数据
    /// </summary>
    [System.Serializable]
    public class BaccaratStatistics
    {
        [UnityEngine.Header("基础统计")]
        public int total_games;       // 总局数
        public int banker_wins;       // 庄胜次数
        public int player_wins;       // 闲胜次数
        public int ties;             // 和局次数
        
        [UnityEngine.Header("胜率统计")]
        public float banker_win_rate; // 庄胜率
        public float player_win_rate; // 闲胜率
        public float tie_rate;        // 和局率
        
        [UnityEngine.Header("连胜统计")]
        public int banker_max_streak; // 庄最大连胜
        public int player_max_streak; // 闲最大连胜
        public int current_streak;    // 当前连胜
        public BaccaratWinner streak_side; // 连胜方
        
        [UnityEngine.Header("对子统计")]
        public int banker_pair_count; // 庄对次数
        public int player_pair_count; // 闲对次数
        public float banker_pair_rate; // 庄对率
        public float player_pair_rate; // 闲对率
    }

    /// <summary>
    /// 百家乐预测（如支持）
    /// </summary>
    [System.Serializable]
    public class BaccaratPrediction
    {
        [UnityEngine.Header("预测信息")]
        public BaccaratWinner predicted_winner; // 预测结果
        public float confidence;      // 置信度（0-1）
        public string prediction_method; // 预测方法
        public List<string> reasoning; // 预测理由
        
        [UnityEngine.Header("趋势分析")]
        public string trend_analysis; // 趋势分析
        public bool is_streak_likely; // 是否可能连胜
        public float banker_probability; // 庄胜概率
        public float player_probability; // 闲胜概率
        public float tie_probability;    // 和局概率
    }

    /// <summary>
    /// 热门投注统计
    /// </summary>
    [System.Serializable]
    public class PopularBets
    {
        [UnityEngine.Header("投注热度")]
        public float banker_popularity;  // 庄家热度
        public float player_popularity;  // 闲家热度
        public float tie_popularity;     // 和局热度
        public float pair_popularity;    // 对子热度
        
        [UnityEngine.Header("投注金额")]
        public float banker_total_amount; // 庄家总投注
        public float player_total_amount; // 闲家总投注
        public float tie_total_amount;    // 和局总投注
        
        [UnityEngine.Header("投注人数")]
        public int banker_bet_count;     // 庄家投注人数
        public int player_bet_count;     // 闲家投注人数
        public int tie_bet_count;        // 和局投注人数
    }

    /// <summary>
    /// 百家乐赔率
    /// </summary>
    [System.Serializable]
    public class BaccaratOdds
    {
        [UnityEngine.Header("主要投注赔率")]
        public float banker_odds;        // 庄家赔率（通常0.95）
        public float player_odds;        // 闲家赔率（通常1.0）
        public float tie_odds;           // 和局赔率（通常8.0）
        
        [UnityEngine.Header("对子投注赔率")]
        public float banker_pair_odds;   // 庄对赔率（通常11.0）
        public float player_pair_odds;   // 闲对赔率（通常11.0）
        
        [UnityEngine.Header("大小投注赔率")]
        public float big_odds;           // 大赔率（通常0.54）
        public float small_odds;         // 小赔率（通常1.5）
        
        [UnityEngine.Header("免佣赔率")]
        public float banker_no_commission_odds; // 免佣庄赔率
        public bool has_special_rules;   // 是否有特殊规则
        public string special_rules_desc; // 特殊规则描述
    }

    #endregion
}