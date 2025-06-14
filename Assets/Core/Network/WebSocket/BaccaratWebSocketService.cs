// Assets/_Core/Network/WebSocket/BaccaratWebSocketService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Network.Interfaces;
using Core.Network.WebSocket;

namespace Core.Network.WebSocket
{
    /// <summary>
    /// 百家乐WebSocket服务 - 对应JavaScript中的useSocket.js
    /// 处理百家乐游戏专用的WebSocket消息，包括倒计时、游戏结果、中奖信息等
    /// </summary>
    public class BaccaratWebSocketService : MonoBehaviour
    {
        #region 事件定义

        // 游戏消息事件
        public event Action<BaccaratCountdownMessage> OnCountdownReceived;
        public event Action<BaccaratGameResultMessage> OnGameResultReceived;
        public event Action<BaccaratWinMessage> OnWinDataReceived;
        public event Action<BaccaratGameStatusMessage> OnGameStatusReceived;
        public event Action<BaccaratBalanceUpdateMessage> OnBalanceUpdateReceived;
        public event Action<BaccaratRoadmapUpdateMessage> OnRoadmapUpdateReceived;
        public event Action<BaccaratSystemMessage> OnSystemMessageReceived;
        
        // 连接事件
        public event Action OnSocketConnected;
        public event Action<string> OnSocketDisconnected;
        public event Action<WebSocketError> OnSocketError;
        public event Action<WSConnectionStatus> OnConnectionStatusChanged;

        #endregion

        #region Inspector配置

        [Header("百家乐WebSocket配置")]
        [SerializeField] private bool _autoConnect = true;
        [SerializeField] private bool _autoReconnect = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _heartbeatInterval = 30; // 秒

        [Header("消息过滤配置")]
        [SerializeField] private bool _filterByTableId = true;
        [SerializeField] private bool _filterByUserId = true;
        [SerializeField] private bool _enableMessageCache = true;
        [SerializeField] private int _maxCacheSize = 50;

        #endregion

        #region 私有字段

        private IWebSocketService _webSocketService;
        private GameMessageDispatcher _messageDispatcher;
        private GameParams _gameParams;
        
        // 消息缓存
        private Queue<BaccaratCountdownMessage> _countdownCache;
        private Queue<BaccaratGameResultMessage> _gameResultCache;
        private BaccaratGameStatusMessage _lastGameStatus;
        private BaccaratBalanceUpdateMessage _lastBalanceUpdate;
        
        // 状态管理
        private bool _isInitialized = false;
        private bool _isConnecting = false;
        private string _currentGameNumber = "";
        private DateTime _lastMessageTime;
        
        // 消息统计
        private Dictionary<string, int> _messageStats;
        private int _totalMessagesReceived = 0;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            InitializeService();
        }

        private void Start()
        {
            if (_autoConnect)
            {
                // 延迟自动连接，确保其他组件已初始化
                Invoke(nameof(AttemptAutoConnect), 1f);
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region 初始化

        private void InitializeService()
        {
            // 查找WebSocket管理器
            _webSocketService = FindObjectOfType<WebSocketManager>();
            if (_webSocketService == null)
            {
                Debug.LogError("[BaccaratWebSocketService] 未找到WebSocketManager组件");
                return;
            }
            
            // 创建消息分发器
            _messageDispatcher = new GameMessageDispatcher();
            _messageDispatcher.OnBaccaratCountdownMessage += HandleCountdownMessage;
            _messageDispatcher.OnBaccaratGameResultMessage += HandleGameResultMessage;
            _messageDispatcher.OnBaccaratWinMessage += HandleWinMessage;
            _messageDispatcher.OnBaccaratGameStatusMessage += HandleGameStatusMessage;
            _messageDispatcher.OnBaccaratBalanceUpdateMessage += HandleBalanceUpdateMessage;
            _messageDispatcher.OnBaccaratRoadmapUpdateMessage += HandleRoadmapUpdateMessage;
            _messageDispatcher.OnBaccaratSystemMessage += HandleSystemMessage;
            
            // 初始化缓存
            _countdownCache = new Queue<BaccaratCountdownMessage>();
            _gameResultCache = new Queue<BaccaratGameResultMessage>();
            _messageStats = new Dictionary<string, int>();
            
            // 注册WebSocket事件
            _webSocketService.OnConnected += OnWebSocketConnected;
            _webSocketService.OnDisconnected += OnWebSocketDisconnected;
            _webSocketService.OnError += OnWebSocketError;
            _webSocketService.OnConnectionStatusChanged += OnWebSocketStatusChanged;
            _webSocketService.OnMessageReceived += OnWebSocketMessageReceived;
            
            _isInitialized = true;
            
            if (_enableLogging)
            {
                Debug.Log("[BaccaratWebSocketService] 百家乐WebSocket服务已初始化");
            }
        }

        private void AttemptAutoConnect()
        {
            if (_isInitialized && !_webSocketService.IsConnected)
            {
                _ = ConnectAsync();
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 初始化并连接WebSocket
        /// </summary>
        public async Task<bool> InitializeAndConnectAsync(GameParams gameParams)
        {
            _gameParams = gameParams;
            
            if (!_isInitialized)
            {
                Debug.LogError("[BaccaratWebSocketService] 服务未初始化");
                return false;
            }
            
            return await ConnectAsync();
        }

        /// <summary>
        /// 连接WebSocket
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            if (_isConnecting)
            {
                Debug.LogWarning("[BaccaratWebSocketService] 正在连接中，忽略重复请求");
                return false;
            }
            
            if (_webSocketService.IsConnected)
            {
                Debug.LogWarning("[BaccaratWebSocketService] 已经连接");
                return true;
            }
            
            try
            {
                _isConnecting = true;
                
                // 构建WebSocket配置
                var config = CreateWebSocketConfig();
                
                // 初始化WebSocket服务
                var initResult = await _webSocketService.InitializeAsync(config);
                if (!initResult.success)
                {
                    Debug.LogError($"[BaccaratWebSocketService] WebSocket初始化失败: {initResult.message}");
                    return false;
                }
                
                // 连接WebSocket
                var connectSuccess = await _webSocketService.ConnectAsync();
                if (!connectSuccess)
                {
                    Debug.LogError("[BaccaratWebSocketService] WebSocket连接失败");
                    return false;
                }
                
                // 注册消息处理器
                RegisterMessageHandlers();
                
                // 启用自动重连
                if (_autoReconnect)
                {
                    _webSocketService.EnableAutoReconnect();
                }
                
                // 启动心跳
                _webSocketService.StartHeartbeat(_heartbeatInterval * 1000);
                
                if (_enableLogging)
                {
                    Debug.Log("[BaccaratWebSocketService] WebSocket连接成功");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 连接过程中发生错误: {ex.Message}");
                return false;
            }
            finally
            {
                _isConnecting = false;
            }
        }

        /// <summary>
        /// 断开WebSocket连接
        /// </summary>
        public async Task<bool> DisconnectAsync(string reason = "用户主动断开")
        {
            try
            {
                var success = await _webSocketService.DisconnectAsync(reason);
                
                if (success && _enableLogging)
                {
                    Debug.Log($"[BaccaratWebSocketService] WebSocket已断开: {reason}");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 断开连接时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送加入台桌消息
        /// </summary>
        public async Task<bool> JoinTableAsync(string tableId, string userId)
        {
            var joinMessage = new
            {
                type = "join_table",
                action = "join",
                table_id = tableId,
                user_id = userId,
                timestamp = DateTime.UtcNow.Ticks
            };
            
            return await _webSocketService.SendJsonAsync(joinMessage);
        }

        /// <summary>
        /// 发送离开台桌消息
        /// </summary>
        public async Task<bool> LeaveTableAsync(string tableId, string userId)
        {
            var leaveMessage = new
            {
                type = "leave_table",
                action = "leave",
                table_id = tableId,
                user_id = userId,
                timestamp = DateTime.UtcNow.Ticks
            };
            
            return await _webSocketService.SendJsonAsync(leaveMessage);
        }

        /// <summary>
        /// 发送投注确认消息
        /// </summary>
        public async Task<bool> ConfirmBetAsync(string gameNumber, List<BaccaratBetRequest> bets)
        {
            var confirmMessage = new
            {
                type = "bet_confirm",
                action = "confirm",
                game_number = gameNumber,
                bets = bets,
                timestamp = DateTime.UtcNow.Ticks
            };
            
            return await _webSocketService.SendJsonAsync(confirmMessage);
        }

        /// <summary>
        /// 请求游戏状态
        /// </summary>
        public async Task<bool> RequestGameStateAsync(string tableId)
        {
            var requestMessage = new
            {
                type = "game_state_request",
                action = "get_state",
                table_id = tableId,
                timestamp = DateTime.UtcNow.Ticks
            };
            
            return await _webSocketService.SendJsonAsync(requestMessage);
        }

        /// <summary>
        /// 获取连接状态
        /// </summary>
        public WSConnectionStatus GetConnectionStatus()
        {
            return _webSocketService.ConnectionStatus;
        }

        /// <summary>
        /// 获取连接延迟
        /// </summary>
        public int GetLatency()
        {
            return _webSocketService.Latency;
        }

        /// <summary>
        /// 获取最后的游戏状态
        /// </summary>
        public BaccaratGameStatusMessage GetLastGameStatus()
        {
            return _lastGameStatus;
        }

        /// <summary>
        /// 获取最后的余额更新
        /// </summary>
        public BaccaratBalanceUpdateMessage GetLastBalanceUpdate()
        {
            return _lastBalanceUpdate;
        }

        /// <summary>
        /// 获取缓存的倒计时消息
        /// </summary>
        public Queue<BaccaratCountdownMessage> GetCountdownCache()
        {
            return new Queue<BaccaratCountdownMessage>(_countdownCache);
        }

        /// <summary>
        /// 获取缓存的游戏结果
        /// </summary>
        public Queue<BaccaratGameResultMessage> GetGameResultCache()
        {
            return new Queue<BaccaratGameResultMessage>(_gameResultCache);
        }

        /// <summary>
        /// 获取消息统计
        /// </summary>
        public Dictionary<string, int> GetMessageStatistics()
        {
            var stats = new Dictionary<string, int>(_messageStats);
            stats["total_messages"] = _totalMessagesReceived;
            stats["cache_size"] = _countdownCache.Count + _gameResultCache.Count;
            return stats;
        }

        /// <summary>
        /// 清除消息缓存
        /// </summary>
        public void ClearCache()
        {
            _countdownCache.Clear();
            _gameResultCache.Clear();
            _lastGameStatus = null;
            _lastBalanceUpdate = null;
            
            if (_enableLogging)
            {
                Debug.Log("[BaccaratWebSocketService] 消息缓存已清除");
            }
        }

        #endregion

        #region WebSocket事件处理

        private void OnWebSocketConnected()
        {
            if (_enableLogging)
            {
                Debug.Log("[BaccaratWebSocketService] WebSocket连接已建立");
            }
            
            // 自动加入台桌
            if (_gameParams != null)
            {
                _ = JoinTableAsync(_gameParams.table_id, _gameParams.user_id);
            }
            
            OnSocketConnected?.Invoke();
        }

        private void OnWebSocketDisconnected(string reason)
        {
            if (_enableLogging)
            {
                Debug.Log($"[BaccaratWebSocketService] WebSocket连接已断开: {reason}");
            }
            
            OnSocketDisconnected?.Invoke(reason);
        }

        private void OnWebSocketError(WebSocketError error)
        {
            if (_enableLogging)
            {
                Debug.LogError($"[BaccaratWebSocketService] WebSocket错误: {error.message}");
            }
            
            OnSocketError?.Invoke(error);
        }

        private void OnWebSocketStatusChanged(WSConnectionStatus status)
        {
            if (_enableLogging)
            {
                Debug.Log($"[BaccaratWebSocketService] 连接状态变更: {status}");
            }
            
            OnConnectionStatusChanged?.Invoke(status);
        }

        private void OnWebSocketMessageReceived(WebSocketMessage message)
        {
            try
            {
                _lastMessageTime = DateTime.UtcNow;
                _totalMessagesReceived++;
                
                // 更新消息统计
                if (_messageStats.ContainsKey(message.type))
                {
                    _messageStats[message.type]++;
                }
                else
                {
                    _messageStats[message.type] = 1;
                }
                
                // 过滤消息
                if (!ShouldProcessMessage(message))
                {
                    return;
                }
                
                // 分发消息到具体处理器
                _messageDispatcher.DispatchMessage(message);
                
                if (_enableLogging)
                {
                    Debug.Log($"[BaccaratWebSocketService] 处理消息: {message.type} - {message.action}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 处理消息时发生错误: {ex.Message}");
            }
        }

        #endregion

        #region 消息处理器

        private void HandleCountdownMessage(BaccaratCountdownMessage message)
        {
            // 更新当前游戏号
            if (!string.IsNullOrEmpty(message.game_number))
            {
                _currentGameNumber = message.game_number;
            }
            
            // 缓存消息
            if (_enableMessageCache)
            {
                _countdownCache.Enqueue(message);
                if (_countdownCache.Count > _maxCacheSize)
                {
                    _countdownCache.Dequeue();
                }
            }
            
            // 触发事件
            OnCountdownReceived?.Invoke(message);
            
            if (_enableLogging)
            {
                Debug.Log($"[BaccaratWebSocketService] 倒计时消息: {message.game_number} - {message.countdown}秒");
            }
        }

        private void HandleGameResultMessage(BaccaratGameResultMessage message)
        {
            // 缓存消息
            if (_enableMessageCache)
            {
                _gameResultCache.Enqueue(message);
                if (_gameResultCache.Count > _maxCacheSize)
                {
                    _gameResultCache.Dequeue();
                }
            }
            
            // 触发事件
            OnGameResultReceived?.Invoke(message);
            
            if (_enableLogging)
            {
                Debug.Log($"[BaccaratWebSocketService] 游戏结果: {message.game_number} - {message.winner}");
            }
        }

        private void HandleWinMessage(BaccaratWinMessage message)
        {
            // 触发事件
            OnWinDataReceived?.Invoke(message);
            
            if (_enableLogging)
            {
                Debug.Log($"[BaccaratWebSocketService] 中奖消息: {message.game_number} - 净收益: {message.net_profit}");
            }
        }

        private void HandleGameStatusMessage(BaccaratGameStatusMessage message)
        {
            _lastGameStatus = message;
            
            // 触发事件
            OnGameStatusReceived?.Invoke(message);
            
            if (_enableLogging)
            {
                Debug.Log($"[BaccaratWebSocketService] 游戏状态: {message.game_number} - {message.phase}");
            }
        }

        private void HandleBalanceUpdateMessage(BaccaratBalanceUpdateMessage message)
        {
            _lastBalanceUpdate = message;
            
            // 触发事件
            OnBalanceUpdateReceived?.Invoke(message);
            
            if (_enableLogging)
            {
                Debug.Log($"[BaccaratWebSocketService] 余额更新: {message.new_balance} (变化: {message.change_amount})");
            }
        }

        private void HandleRoadmapUpdateMessage(BaccaratRoadmapUpdateMessage message)
        {
            // 触发事件
            OnRoadmapUpdateReceived?.Invoke(message);
            
            if (_enableLogging)
            {
                Debug.Log($"[BaccaratWebSocketService] 路纸更新: {message.table_id}");
            }
        }

        private void HandleSystemMessage(BaccaratSystemMessage message)
        {
            // 触发事件
            OnSystemMessageReceived?.Invoke(message);
            
            if (_enableLogging)
            {
                Debug.Log($"[BaccaratWebSocketService] 系统消息: {message.title} - {message.content}");
            }
        }

        #endregion

        #region 辅助方法

        private WebSocketConfig CreateWebSocketConfig()
        {
            var config = new WebSocketConfig
            {
                url = BuildWebSocketUrl(),
                connectionTimeout = 10000,
                messageTimeout = 5000,
                keepAliveInterval = _heartbeatInterval * 1000,
                autoReconnect = _autoReconnect,
                maxReconnectAttempts = 5,
                reconnectDelay = 1000,
                reconnectBackoffFactor = 1.5f,
                enableMessageQueue = true,
                maxQueueSize = 100,
                compressMessages = false,
                validateCertificates = true
            };
            
            // 添加游戏参数
            if (_gameParams != null)
            {
                config.authToken = _gameParams.token;
                config.gameType = _gameParams.game_type;
                config.tableId = _gameParams.table_id;
                config.userId = _gameParams.user_id;
                
                // 添加自定义头部
                config.headers = new Dictionary<string, string>
                {
                    ["X-Game-Type"] = _gameParams.game_type,
                    ["X-Table-Id"] = _gameParams.table_id,
                    ["X-User-Id"] = _gameParams.user_id
                };
            }
            
            return config;
        }

        private string BuildWebSocketUrl()
        {
            var baseUrl = "wss://ws.yourgame.com"; // 这应该来自配置
            
            if (_gameParams != null)
            {
                var url = $"{baseUrl}/baccarat/{_gameParams.table_id}";
                url += $"?user_id={_gameParams.user_id}";
                url += $"&game_type={_gameParams.game_type}";
                url += $"&token={_gameParams.token}";
                return url;
            }
            
            return baseUrl;
        }

        private void RegisterMessageHandlers()
        {
            // 注册百家乐专用消息处理器
            _webSocketService.RegisterMessageHandler("countdown", OnCountdownMessage);
            _webSocketService.RegisterMessageHandler("game_result", OnGameResultMessage);
            _webSocketService.RegisterMessageHandler("win_data", OnWinDataMessage);
            _webSocketService.RegisterMessageHandler("game_status", OnGameStatusMessage);
            _webSocketService.RegisterMessageHandler("balance_update", OnBalanceUpdateMessage);
            _webSocketService.RegisterMessageHandler("roadmap_update", OnRoadmapUpdateMessage);
            _webSocketService.RegisterMessageHandler("system_message", OnSystemMessage);
            _webSocketService.RegisterMessageHandler("popular_bets_update", OnPopularBetsMessage);
            _webSocketService.RegisterMessageHandler("table_info_update", OnTableInfoMessage);
            
            if (_enableLogging)
            {
                Debug.Log("[BaccaratWebSocketService] 消息处理器已注册");
            }
        }

        private bool ShouldProcessMessage(WebSocketMessage message)
        {
            // 基础过滤
            if (message == null || string.IsNullOrEmpty(message.type))
            {
                return false;
            }
            
            // 跳过心跳消息
            if (message.type == "heartbeat" || message.type == "ping" || message.type == "pong")
            {
                return false;
            }
            
            // 台桌ID过滤
            if (_filterByTableId && _gameParams != null)
            {
                var messageData = JsonUtility.FromJson<Dictionary<string, object>>(message.content ?? "{}");
                if (messageData != null && messageData.ContainsKey("table_id"))
                {
                    var tableId = messageData["table_id"].ToString();
                    if (tableId != _gameParams.table_id)
                    {
                        return false;
                    }
                }
            }
            
            // 用户ID过滤（对于个人消息）
            if (_filterByUserId && _gameParams != null)
            {
                var personalMessageTypes = new HashSet<string> { "win_data", "balance_update", "personal_message" };
                if (personalMessageTypes.Contains(message.type))
                {
                    var messageData = JsonUtility.FromJson<Dictionary<string, object>>(message.content ?? "{}");
                    if (messageData != null && messageData.ContainsKey("user_id"))
                    {
                        var userId = messageData["user_id"].ToString();
                        if (userId != _gameParams.user_id)
                        {
                            return false;
                        }
                    }
                }
            }
            
            return true;
        }

        #endregion

        #region 具体消息处理方法

        private void OnCountdownMessage(WebSocketMessage message)
        {
            try
            {
                var countdownMessage = JsonUtility.FromJson<BaccaratCountdownMessage>(message.content);
                if (countdownMessage != null)
                {
                    _messageDispatcher.DispatchBaccaratCountdownMessage(countdownMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 解析倒计时消息失败: {ex.Message}");
            }
        }

        private void OnGameResultMessage(WebSocketMessage message)
        {
            try
            {
                var resultMessage = JsonUtility.FromJson<BaccaratGameResultMessage>(message.content);
                if (resultMessage != null)
                {
                    _messageDispatcher.DispatchBaccaratGameResultMessage(resultMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 解析游戏结果消息失败: {ex.Message}");
            }
        }

        private void OnWinDataMessage(WebSocketMessage message)
        {
            try
            {
                var winMessage = JsonUtility.FromJson<BaccaratWinMessage>(message.content);
                if (winMessage != null)
                {
                    _messageDispatcher.DispatchBaccaratWinMessage(winMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 解析中奖消息失败: {ex.Message}");
            }
        }

        private void OnGameStatusMessage(WebSocketMessage message)
        {
            try
            {
                var statusMessage = JsonUtility.FromJson<BaccaratGameStatusMessage>(message.content);
                if (statusMessage != null)
                {
                    _messageDispatcher.DispatchBaccaratGameStatusMessage(statusMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 解析游戏状态消息失败: {ex.Message}");
            }
        }

        private void OnBalanceUpdateMessage(WebSocketMessage message)
        {
            try
            {
                var balanceMessage = JsonUtility.FromJson<BaccaratBalanceUpdateMessage>(message.content);
                if (balanceMessage != null)
                {
                    _messageDispatcher.DispatchBaccaratBalanceUpdateMessage(balanceMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 解析余额更新消息失败: {ex.Message}");
            }
        }

        private void OnRoadmapUpdateMessage(WebSocketMessage message)
        {
            try
            {
                var roadmapMessage = JsonUtility.FromJson<BaccaratRoadmapUpdateMessage>(message.content);
                if (roadmapMessage != null)
                {
                    _messageDispatcher.DispatchBaccaratRoadmapUpdateMessage(roadmapMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 解析路纸更新消息失败: {ex.Message}");
            }
        }

        private void OnSystemMessage(WebSocketMessage message)
        {
            try
            {
                var systemMessage = JsonUtility.FromJson<BaccaratSystemMessage>(message.content);
                if (systemMessage != null)
                {
                    _messageDispatcher.DispatchBaccaratSystemMessage(systemMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 解析系统消息失败: {ex.Message}");
            }
        }

        private void OnPopularBetsMessage(WebSocketMessage message)
        {
            try
            {
                // 处理热门投注更新消息
                if (_enableLogging)
                {
                    Debug.Log($"[BaccaratWebSocketService] 收到热门投注更新消息");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 解析热门投注消息失败: {ex.Message}");
            }
        }

        private void OnTableInfoMessage(WebSocketMessage message)
        {
            try
            {
                // 处理台桌信息更新消息
                if (_enableLogging)
                {
                    Debug.Log($"[BaccaratWebSocketService] 收到台桌信息更新消息");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 解析台桌信息消息失败: {ex.Message}");
            }
        }

        #endregion

        #region 清理方法

        private void Cleanup()
        {
            try
            {
                // 断开WebSocket连接
                if (_webSocketService != null && _webSocketService.IsConnected)
                {
                    _ = _webSocketService.DisconnectAsync("服务关闭");
                }

                // 注销事件处理器
                if (_webSocketService != null)
                {
                    _webSocketService.OnConnected -= OnWebSocketConnected;
                    _webSocketService.OnDisconnected -= OnWebSocketDisconnected;
                    _webSocketService.OnError -= OnWebSocketError;
                    _webSocketService.OnConnectionStatusChanged -= OnWebSocketStatusChanged;
                    _webSocketService.OnMessageReceived -= OnWebSocketMessageReceived;
                }

                // 清理消息分发器
                if (_messageDispatcher != null)
                {
                    _messageDispatcher.OnBaccaratCountdownMessage -= HandleCountdownMessage;
                    _messageDispatcher.OnBaccaratGameResultMessage -= HandleGameResultMessage;
                    _messageDispatcher.OnBaccaratWinMessage -= HandleWinMessage;
                    _messageDispatcher.OnBaccaratGameStatusMessage -= HandleGameStatusMessage;
                    _messageDispatcher.OnBaccaratBalanceUpdateMessage -= HandleBalanceUpdateMessage;
                    _messageDispatcher.OnBaccaratRoadmapUpdateMessage -= HandleRoadmapUpdateMessage;
                    _messageDispatcher.OnBaccaratSystemMessage -= HandleSystemMessage;
                    _messageDispatcher = null;
                }

                // 清理缓存
                _countdownCache?.Clear();
                _gameResultCache?.Clear();
                _messageStats?.Clear();

                if (_enableLogging)
                {
                    Debug.Log("[BaccaratWebSocketService] 服务清理完成");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaccaratWebSocketService] 清理过程中发生错误: {ex.Message}");
            }
        }

        #endregion
    }
}