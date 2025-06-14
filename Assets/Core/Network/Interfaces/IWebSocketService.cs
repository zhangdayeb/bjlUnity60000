// Assets/_Core/Network/Interfaces/IWebSocketService.cs
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Core.Network.Interfaces
{
    /// <summary>
    /// WebSocket服务接口
    /// 对应JavaScript中的optimizedSocket，提供实时通信功能
    /// </summary>
    public interface IWebSocketService
    {
        #region 连接管理

        /// <summary>
        /// 连接状态
        /// </summary>
        WSConnectionStatus ConnectionStatus { get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接延迟（毫秒）
        /// </summary>
        int Latency { get; }

        /// <summary>
        /// 初始化WebSocket连接
        /// </summary>
        /// <param name="config">连接配置</param>
        /// <returns>初始化结果</returns>
        Task<WebSocketInitResult> InitializeAsync(WebSocketConfig config);

        /// <summary>
        /// 连接到WebSocket服务器
        /// </summary>
        /// <returns>连接结果</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// 断开WebSocket连接
        /// </summary>
        /// <param name="reason">断开原因</param>
        /// <returns>断开结果</returns>
        Task<bool> DisconnectAsync(string reason = "User requested");

        /// <summary>
        /// 重新连接
        /// </summary>
        /// <returns>重连结果</returns>
        Task<bool> ReconnectAsync();

        #endregion

        #region 消息处理

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <returns>发送结果</returns>
        Task<bool> SendMessageAsync(WebSocketMessage message);

        /// <summary>
        /// 发送文本消息
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <returns>发送结果</returns>
        Task<bool> SendTextAsync(string text);

        /// <summary>
        /// 发送JSON消息
        /// </summary>
        /// <param name="data">数据对象</param>
        /// <returns>发送结果</returns>
        Task<bool> SendJsonAsync(object data);

        /// <summary>
        /// 注册消息类型处理器
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="handler">处理器</param>
        void RegisterMessageHandler(string messageType, Action<WebSocketMessage> handler);

        /// <summary>
        /// 注销消息处理器
        /// </summary>
        /// <param name="messageType">消息类型</param>
        void UnregisterMessageHandler(string messageType);

        /// <summary>
        /// 清除所有消息处理器
        /// </summary>
        void ClearAllHandlers();

        #endregion

        #region 事件订阅

        /// <summary>
        /// 连接建立事件
        /// </summary>
        event Action OnConnected;

        /// <summary>
        /// 连接断开事件
        /// </summary>
        event Action<string> OnDisconnected;

        /// <summary>
        /// 连接错误事件
        /// </summary>
        event Action<WebSocketError> OnError;

        /// <summary>
        /// 收到消息事件
        /// </summary>
        event Action<WebSocketMessage> OnMessageReceived;

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        event Action<WSConnectionStatus> OnConnectionStatusChanged;

        /// <summary>
        /// 重连尝试事件
        /// </summary>
        event Action<int> OnReconnectAttempt;

        #endregion

        #region 心跳和监控

        /// <summary>
        /// 启动心跳检测
        /// </summary>
        /// <param name="interval">心跳间隔（毫秒）</param>
        void StartHeartbeat(int interval = 30000);

        /// <summary>
        /// 停止心跳检测
        /// </summary>
        void StopHeartbeat();

        /// <summary>
        /// 发送Ping消息
        /// </summary>
        /// <returns>延迟时间（毫秒）</returns>
        Task<int> PingAsync();

        /// <summary>
        /// 获取连接统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        WebSocketStatistics GetStatistics();

        #endregion

        #region 自动重连

        /// <summary>
        /// 启用自动重连
        /// </summary>
        /// <param name="config">重连配置</param>
        void EnableAutoReconnect(ReconnectConfig config = null);

        /// <summary>
        /// 禁用自动重连
        /// </summary>
        void DisableAutoReconnect();

        /// <summary>
        /// 是否启用自动重连
        /// </summary>
        bool IsAutoReconnectEnabled { get; }

        #endregion

        #region 消息队列

        /// <summary>
        /// 启用消息队列（离线时缓存消息）
        /// </summary>
        /// <param name="maxQueueSize">最大队列大小</param>
        void EnableMessageQueue(int maxQueueSize = 100);

        /// <summary>
        /// 禁用消息队列
        /// </summary>
        void DisableMessageQueue();

        /// <summary>
        /// 获取队列中的消息数量
        /// </summary>
        int GetQueuedMessageCount();

        /// <summary>
        /// 清空消息队列
        /// </summary>
        void ClearMessageQueue();

        #endregion
    }

    #region WebSocket数据类型

    /// <summary>
    /// WebSocket连接状态
    /// </summary>
    public enum WSConnectionStatus
    {
        Disconnected,   // 已断开
        Connecting,     // 连接中
        Connected,      // 已连接
        Reconnecting,   // 重连中
        Error,          // 错误状态
        Closing,        // 关闭中
        Maintenance     // 维护模式
    }

    /// <summary>
    /// WebSocket初始化结果
    /// </summary>
    [System.Serializable]
    public class WebSocketInitResult
    {
        public bool success;
        public string message;
        public WSConnectionStatus status;
        public DateTime timestamp;
        public string serverInfo;
        public Dictionary<string, object> metadata;
    }

    /// <summary>
    /// WebSocket配置
    /// </summary>
    [System.Serializable]
    public class WebSocketConfig
    {
        [UnityEngine.Header("连接设置")]
        public string url;                    // WebSocket URL
        public string[] protocols;            // 支持的协议
        public Dictionary<string, string> headers; // 请求头
        
        [UnityEngine.Header("超时设置")]
        public int connectionTimeout = 10000;  // 连接超时（毫秒）
        public int messageTimeout = 5000;      // 消息超时（毫秒）
        public int keepAliveInterval = 30000;  // 心跳间隔（毫秒）
        
        [UnityEngine.Header("重连设置")]
        public bool autoReconnect = true;      // 自动重连
        public int maxReconnectAttempts = 5;   // 最大重连次数
        public int reconnectDelay = 1000;      // 重连延迟（毫秒）
        public float reconnectBackoffFactor = 1.5f; // 重连退避因子
        
        [UnityEngine.Header("消息设置")]
        public bool enableMessageQueue = true; // 启用消息队列
        public int maxQueueSize = 100;         // 最大队列大小
        public bool compressMessages = false;  // 压缩消息
        
        [UnityEngine.Header("安全设置")]
        public bool validateCertificates = true; // 验证SSL证书
        public string authToken;               // 认证令牌
        public string gameType;                // 游戏类型
        public string tableId;                 // 桌台ID
        public string userId;                  // 用户ID
    }

    /// <summary>
    /// WebSocket消息
    /// </summary>
    [System.Serializable]
    public class WebSocketMessage
    {
        [UnityEngine.Header("消息基础信息")]
        public string id;                     // 消息ID
        public string type;                   // 消息类型
        public string action;                 // 动作类型
        public DateTime timestamp;            // 时间戳
        
        [UnityEngine.Header("消息内容")]
        public string content;                // 消息内容（JSON字符串）
        public Dictionary<string, object> data; // 消息数据
        public byte[] binaryData;             // 二进制数据
        
        [UnityEngine.Header("消息元数据")]
        public string source;                 // 消息来源
        public string target;                 // 目标接收者
        public int priority;                  // 优先级（0-9）
        public bool requiresAck;              // 是否需要确认
        public string correlationId;          // 关联ID
        
        /// <summary>
        /// 创建文本消息
        /// </summary>
        public static WebSocketMessage CreateTextMessage(string type, string content, string action = null)
        {
            return new WebSocketMessage
            {
                id = Guid.NewGuid().ToString(),
                type = type,
                action = action,
                content = content,
                timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// 创建JSON消息
        /// </summary>
        public static WebSocketMessage CreateJsonMessage(string type, object data, string action = null)
        {
            return new WebSocketMessage
            {
                id = Guid.NewGuid().ToString(),
                type = type,
                action = action,
                content = UnityEngine.JsonUtility.ToJson(data),
                data = data as Dictionary<string, object>,
                timestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// 解析为指定类型
        /// </summary>
        public T ParseContent<T>() where T : class
        {
            if (string.IsNullOrEmpty(content))
                return null;
                
            return UnityEngine.JsonUtility.FromJson<T>(content);
        }
    }

    /// <summary>
    /// WebSocket错误
    /// </summary>
    [System.Serializable]
    public class WebSocketError
    {
        [UnityEngine.Header("错误信息")]
        public string code;                   // 错误代码
        public string message;                // 错误消息
        public WebSocketErrorType type;       // 错误类型
        public DateTime timestamp;            // 发生时间
        
        [UnityEngine.Header("错误详情")]
        public string stackTrace;             // 堆栈跟踪
        public Dictionary<string, object> context; // 错误上下文
        public bool isRecoverable;            // 是否可恢复
        public string suggestion;             // 修复建议
    }

    /// <summary>
    /// WebSocket错误类型
    /// </summary>
    public enum WebSocketErrorType
    {
        ConnectionFailed,    // 连接失败
        AuthenticationFailed, // 认证失败
        MessageSendFailed,   // 消息发送失败
        MessageReceiveFailed, // 消息接收失败
        ProtocolError,       // 协议错误
        TimeoutError,        // 超时错误
        NetworkError,        // 网络错误
        ServerError,         // 服务器错误
        UnknownError         // 未知错误
    }

    /// <summary>
    /// 重连配置
    /// </summary>
    [System.Serializable]
    public class ReconnectConfig
    {
        [UnityEngine.Header("重连设置")]
        public int maxAttempts = 5;           // 最大重连次数
        public int baseDelay = 1000;          // 基础延迟（毫秒）
        public float backoffFactor = 1.5f;    // 退避因子
        public int maxDelay = 30000;          // 最大延迟（毫秒）
        
        [UnityEngine.Header("触发条件")]
        public bool onConnectionLost = true;   // 连接丢失时重连
        public bool onNetworkError = true;     // 网络错误时重连
        public bool onServerError = false;     // 服务器错误时重连
        
        [UnityEngine.Header("重连策略")]
        public ReconnectStrategy strategy = ReconnectStrategy.ExponentialBackoff;
        public bool resetOnSuccess = true;     // 成功连接后重置计数
        public bool jitterEnabled = true;      // 启用抖动
    }

    /// <summary>
    /// 重连策略
    /// </summary>
    public enum ReconnectStrategy
    {
        FixedDelay,         // 固定延迟
        LinearBackoff,      // 线性退避
        ExponentialBackoff, // 指数退避
        CustomDelay         // 自定义延迟
    }

    /// <summary>
    /// WebSocket统计信息
    /// </summary>
    [System.Serializable]
    public class WebSocketStatistics
    {
        [UnityEngine.Header("连接统计")]
        public DateTime connectionStartTime;   // 连接开始时间
        public TimeSpan totalConnectionTime;   // 总连接时间
        public int connectionAttempts;         // 连接尝试次数
        public int successfulConnections;      // 成功连接次数
        public int reconnectionCount;          // 重连次数
        
        [UnityEngine.Header("消息统计")]
        public long messagesSent;              // 发送消息数
        public long messagesReceived;          // 接收消息数
        public long bytesSent;                 // 发送字节数
        public long bytesReceived;             // 接收字节数
        public long messagesQueued;            // 队列消息数
        public long messagesDropped;           // 丢弃消息数
        
        [UnityEngine.Header("性能统计")]
        public int averageLatency;             // 平均延迟（毫秒）
        public int minLatency;                 // 最小延迟（毫秒）
        public int maxLatency;                 // 最大延迟（毫秒）
        public float messageRate;              // 消息频率（消息/秒）
        public DateTime lastMessageTime;       // 最后消息时间
        
        [UnityEngine.Header("错误统计")]
        public int totalErrors;                // 总错误数
        public int connectionErrors;           // 连接错误数
        public int messageErrors;              // 消息错误数
        public int timeoutErrors;              // 超时错误数
        public DateTime lastErrorTime;         // 最后错误时间
    }

    #endregion

    #region 百家乐专用WebSocket消息类型

    /// <summary>
    /// 百家乐倒计时消息
    /// 对应JavaScript中的CountdownData
    /// </summary>
    [System.Serializable]
    public class BaccaratCountdownMessage
    {
        public string type = "countdown";
        public string action;
        public string table_id;
        public string game_number;
        public int countdown;
        public int total_time;
        public BaccaratGamePhase phase;
        public bool betting_enabled;
        public DateTime server_time;
    }

    /// <summary>
    /// 百家乐游戏结果消息
    /// 对应JavaScript中的GameResultData
    /// </summary>
    [System.Serializable]
    public class BaccaratGameResultMessage
    {
        public string type = "game_result";
        public string action = "game_end";
        public string table_id;
        public string game_number;
        public BaccaratGameResult result;
        public List<BaccaratCard> banker_cards;
        public List<BaccaratCard> player_cards;
        public BaccaratWinner winner;
        public List<string> winning_areas;
        public DateTime result_time;
    }

    /// <summary>
    /// 百家乐中奖消息
    /// 对应JavaScript中的WinData
    /// </summary>
    [System.Serializable]
    public class BaccaratWinMessage
    {
        public string type = "win_data";
        public string action = "payout";
        public string user_id;
        public string game_number;
        public List<WinBet> winning_bets;
        public float total_win_amount;
        public float total_bet_amount;
        public float net_profit;
        public float new_balance;
        public DateTime payout_time;
    }

    /// <summary>
    /// 中奖投注详情
    /// </summary>
    [System.Serializable]
    public class WinBet
    {
        public string bet_type;
        public float bet_amount;
        public float win_amount;
        public float odds;
        public string bet_id;
    }

    /// <summary>
    /// 百家乐游戏状态消息
    /// </summary>
    [System.Serializable]
    public class BaccaratGameStatusMessage
    {
        public string type = "game_status";
        public string action;
        public string table_id;
        public string game_number;
        public BaccaratGamePhase phase;
        public int countdown;
        public bool betting_open;
        public string dealer_name;
        public PopularBets popular_bets;
        public DateTime status_time;
    }

    /// <summary>
    /// 百家乐余额更新消息
    /// </summary>
    [System.Serializable]
    public class BaccaratBalanceUpdateMessage
    {
        public string type = "balance_update";
        public string action = "balance_change";
        public string user_id;
        public float new_balance;
        public float old_balance;
        public float change_amount;
        public string change_reason;
        public string game_number;
        public DateTime update_time;
    }

    /// <summary>
    /// 百家乐路纸更新消息
    /// </summary>
    [System.Serializable]
    public class BaccaratRoadmapUpdateMessage
    {
        public string type = "roadmap_update";
        public string action = "new_result";
        public string table_id;
        public RoadmapBead new_bead;
        public BaccaratStatistics updated_stats;
        public DateTime update_time;
    }

    /// <summary>
    /// 百家乐系统消息
    /// </summary>
    [System.Serializable]
    public class BaccaratSystemMessage
    {
        public string type = "system_message";
        public string action;
        public string message_type; // "info", "warning", "error", "maintenance"
        public string title;
        public string content;
        public bool is_popup;
        public bool is_dismissible;
        public DateTime timestamp;
        public int duration; // 显示时长（毫秒）
    }

    #endregion
}