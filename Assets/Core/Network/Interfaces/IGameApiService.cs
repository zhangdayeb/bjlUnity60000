// Assets/_Core/Network/Interfaces/IGameApiService.cs
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Core.Network.Interfaces
{
    /// <summary>
    /// 通用游戏API服务接口
    /// 定义所有游戏类型通用的API操作
    /// </summary>
    public interface IGameApiService
    {
        #region 基础配置
        
        /// <summary>
        /// 初始化API服务
        /// </summary>
        /// <param name="gameParams">游戏参数</param>
        /// <returns>初始化结果</returns>
        Task<ApiInitResult> InitializeAsync(GameParams gameParams);
        
        /// <summary>
        /// 更新游戏参数
        /// </summary>
        /// <param name="newParams">新的游戏参数</param>
        void UpdateGameParams(GameParams newParams);
        
        /// <summary>
        /// 获取当前游戏参数
        /// </summary>
        /// <returns>游戏参数</returns>
        GameParams GetGameParams();
        
        #endregion

        #region 用户相关
        
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns>用户信息</returns>
        Task<UserInfo> GetUserInfoAsync();
        
        /// <summary>
        /// 更新用户余额
        /// </summary>
        /// <returns>最新余额信息</returns>
        Task<UserBalance> RefreshBalanceAsync();
        
        #endregion

        #region 台桌相关
        
        /// <summary>
        /// 获取台桌信息
        /// </summary>
        /// <returns>台桌信息</returns>
        Task<TableInfo> GetTableInfoAsync();
        
        /// <summary>
        /// 获取台桌状态
        /// </summary>
        /// <returns>台桌运行状态</returns>
        Task<TableRunInfo> GetTableStatusAsync();
        
        #endregion

        #region 投注相关
        
        /// <summary>
        /// 提交投注
        /// </summary>
        /// <param name="bets">投注请求列表</param>
        /// <returns>投注结果</returns>
        Task<BetResponse> PlaceBetsAsync(List<BetRequest> bets);
        
        /// <summary>
        /// 获取当前投注记录
        /// </summary>
        /// <returns>当前局投注记录</returns>
        Task<CurrentBetsResponse> GetCurrentBetsAsync();
        
        /// <summary>
        /// 取消未确认的投注
        /// </summary>
        /// <returns>取消结果</returns>
        Task<CancelBetResponse> CancelPendingBetsAsync();
        
        #endregion

        #region 历史记录
        
        /// <summary>
        /// 获取投注历史
        /// </summary>
        /// <param name="params">查询参数</param>
        /// <returns>投注历史</returns>
        Task<BettingHistoryResponse> GetBettingHistoryAsync(BettingHistoryParams @params);
        
        /// <summary>
        /// 获取投注详情
        /// </summary>
        /// <param name="recordId">记录ID</param>
        /// <returns>投注详情</returns>
        Task<BettingDetailResponse> GetBettingDetailAsync(string recordId);
        
        /// <summary>
        /// 获取路纸数据
        /// </summary>
        /// <returns>路纸数据</returns>
        Task<RoadmapData> GetRoadmapDataAsync();
        
        #endregion

        #region 错误处理
        
        /// <summary>
        /// 设置错误处理回调
        /// </summary>
        /// <param name="onError">错误处理函数</param>
        void SetErrorHandler(System.Action<ApiError> onError);
        
        /// <summary>
        /// 设置认证失败回调
        /// </summary>
        /// <param name="onAuthFailed">认证失败处理函数</param>
        void SetAuthFailedHandler(System.Action onAuthFailed);
        
        #endregion

        #region 网络状态
        
        /// <summary>
        /// 检查网络连接状态
        /// </summary>
        /// <returns>连接状态</returns>
        Task<NetworkStatus> CheckConnectionAsync();
        
        /// <summary>
        /// 获取API服务状态
        /// </summary>
        /// <returns>服务状态</returns>
        ApiServiceStatus GetServiceStatus();
        
        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// API初始化结果
    /// </summary>
    [System.Serializable]
    public class ApiInitResult
    {
        public bool success;
        public string message;
        public UserInfo userInfo;
        public TableInfo tableInfo;
        public System.DateTime timestamp;
    }

    /// <summary>
    /// 用户余额信息
    /// </summary>
    [System.Serializable]
    public class UserBalance
    {
        public float balance;
        public float moneyBalance;
        public string currency;
        public System.DateTime lastUpdated;
    }

    /// <summary>
    /// 当前投注响应
    /// </summary>
    [System.Serializable]
    public class CurrentBetsResponse
    {
        public List<BetRecord> currentBets;
        public float totalBetAmount;
        public string gameNumber;
        public System.DateTime timestamp;
    }

    /// <summary>
    /// 取消投注响应
    /// </summary>
    [System.Serializable]
    public class CancelBetResponse
    {
        public bool success;
        public string message;
        public List<string> cancelledBetIds;
        public float refundAmount;
    }

    /// <summary>
    /// 路纸数据
    /// </summary>
    [System.Serializable]
    public class RoadmapData
    {
        public List<GameResultRecord> results;
        public RoadmapStatistics statistics;
        public System.DateTime lastUpdated;
    }

    /// <summary>
    /// 路纸统计
    /// </summary>
    [System.Serializable]
    public class RoadmapStatistics
    {
        public int totalGames;
        public int bankerWins;
        public int playerWins;
        public int ties;
        public float bankerWinRate;
        public float playerWinRate;
        public float tieRate;
    }

    /// <summary>
    /// API错误信息
    /// </summary>
    [System.Serializable]
    public class ApiError
    {
        public int code;
        public string message;
        public string type;
        public System.DateTime timestamp;
        public bool recoverable;
        public System.Collections.Generic.Dictionary<string, object> context;
    }

    /// <summary>
    /// 网络状态
    /// </summary>
    [System.Serializable]
    public class NetworkStatus
    {
        public bool isConnected;
        public float latency;
        public string quality; // "excellent", "good", "poor", "offline"
        public System.DateTime lastCheck;
    }

    /// <summary>
    /// API服务状态
    /// </summary>
    public enum ApiServiceStatus
    {
        Uninitialized,  // 未初始化
        Initializing,   // 初始化中
        Ready,          // 就绪
        Error,          // 错误状态
        Reconnecting,   // 重连中
        Maintenance     // 维护中
    }

    #endregion
}