// Assets/_Core/Network/WebSocket/WebSocketManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Network.Interfaces;

namespace Core.Network.WebSocket
{
    /// <summary>
    /// WebSocket连接管理器 - 对应JavaScript中的OptimizedSocketManager
    /// 负责WebSocket连接的生命周期管理、自动重连、心跳检测等核心功能
    /// </summary>
    public class WebSocketManager : MonoBehaviour, IWebSocketService
    {
        #region 属性实现

        public WSConnectionStatus ConnectionStatus { get; private set; } = WSConnectionStatus.Disconnected;
        public bool IsConnected => ConnectionStatus == WSConnectionStatus.Connected;
        public int Latency { get; private set; } = 0;
        public bool IsAutoReconnectEnabled { get; private set; } = false;

        #endregion

        #region Inspector配置

        [Header("WebSocket配置")]
        [SerializeField] private string _websocketUrl = "wss://ws.yourgame.com";
        [SerializeField] private int _connectionTimeout = 10000; // 连接超时（毫秒）
        [SerializeField] private int _heartbeatInterval = 30000; // 心跳间隔（毫秒）
        [SerializeField] private bool _enableLogging = true; // 启用日志

        [Header("重连配置")]
        [SerializeField] private bool _autoReconnectEnabled = true;
        [SerializeField] private int _maxReconnectAttempts = 5;
        [SerializeField] private int _reconnectBaseDelay = 1000; // 基础重连延迟（毫秒）
        [SerializeField] private float _reconnectBackoffFactor = 1.5f;

        [Header("消息队列配置")]
        [SerializeField] private bool _enableMessageQueue = true;
        [SerializeField] private int _maxQueueSize = 100;

        #endregion

        #region 私有字段

        // WebSocket核心
        private NativeWebSocket.WebSocket _webSocket;
        private WebSocketConfig _config;
        
        // 消息处理
        private Dictionary<string, Action<WebSocketMessage>> _messageHandlers;
        private Queue<WebSocketMessage> _messageQueue;
        private Queue<WebSocketMessage> _outgoingQueue;
        
        // 重连机制
        private ReconnectConfig _reconnectConfig;
        private int _currentReconnectAttempt = 0;
        private Coroutine _reconnectCoroutine;
        
        // 心跳机制
        private Coroutine _heartbeatCoroutine;
        private DateTime _lastHeartbeatTime;
        private DateTime _lastPongTime;
        
        // 统计信息
        private WebSocketStatistics _statistics;
        
        // 协程管理
        private Coroutine _messageProcessorCoroutine;
        private Coroutine _queueProcessorCoroutine;
        
        // 状态管理
        private bool _isInitialized = false;
        private bool _isDisconnecting = false;
        private string _disconnectReason = "";

        #endregion

        #region 事件

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<WebSocketError> OnError;
        public event Action<WebSocketMessage> OnMessageReceived;
        public event Action<WSConnectionStatus> OnConnectionStatusChanged;
        public event Action<int> OnReconnectAttempt;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            // 确保WebSocket在主线程中运行
            if (_enableLogging)
            {
                Debug.Log("[WebSocketManager] WebSocket管理器已启动");
            }
        }

        private void Update()
        {
            // 处理WebSocket消息（必须在主线程）
            _webSocket?.DispatchMessageQueue();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // 应用暂停时断开连接
                if (IsConnected)
                {
                    _ = DisconnectAsync("Application paused");
                }
            }
            else
            {
                // 应用恢复时重连
                if (IsAutoReconnectEnabled && ConnectionStatus == WSConnectionStatus.Disconnected)
                {
                    _ = ReconnectAsync();
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && IsConnected)
            {
                // 失去焦点时可能需要断开连接（取决于游戏需求）
                if (_enableLogging)
                {
                    Debug.Log("[WebSocketManager] 应用失去焦点");
                }
            }
            else if (hasFocus && IsAutoReconnectEnabled && !IsConnected)
            {
                // 重新获得焦点时尝试重连
                _ = ReconnectAsync();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region 初始化

        private void Initialize()
        {
            _messageHandlers = new Dictionary<string, Action<WebSocketMessage>>();
            _messageQueue = new Queue<WebSocketMessage>();
            _outgoingQueue = new Queue<WebSocketMessage>();
            _statistics = new WebSocketStatistics();
            
            // 设置默认重连配置
            _reconnectConfig = new ReconnectConfig
            {
                maxAttempts = _maxReconnectAttempts,
                baseDelay = _reconnectBaseDelay,
                backoffFactor = _reconnectBackoffFactor,
                maxDelay = 30000,
                onConnectionLost = true,
                onNetworkError = true,
                strategy = ReconnectStrategy.ExponentialBackoff
            };
            
            IsAutoReconnectEnabled = _autoReconnectEnabled;
            _isInitialized = true;
            
            if (_enableLogging)
            {
                Debug.Log("[WebSocketManager] WebSocket管理器初始化完成");
            }
        }

        #endregion

        #region IWebSocketService实现

        public async Task<WebSocketInitResult> InitializeAsync(WebSocketConfig config)
        {
            try
            {
                _config = config ?? throw new ArgumentNullException(nameof(config));
                
                // 更新配置
                if (!string.IsNullOrEmpty(config.url))
                    _websocketUrl = config.url;
                
                if (config.connectionTimeout > 0)
                    _connectionTimeout = config.connectionTimeout;
                
                if (config.keepAliveInterval > 0)
                    _heartbeatInterval = config.keepAliveInterval;
                
                _maxReconnectAttempts = config.maxReconnectAttempts;
                _reconnectBaseDelay = config.reconnectDelay;
                IsAutoReconnectEnabled = config.autoReconnect;
                _enableMessageQueue = config.enableMessageQueue;
                _maxQueueSize = config.maxQueueSize;
                
                if (_enableLogging)
                {
                    Debug.Log($"[WebSocketManager] 配置已更新 - URL: {_websocketUrl}");
                }
                
                return new WebSocketInitResult
                {
                    success = true,
                    message = "WebSocket服务初始化成功",
                    status = ConnectionStatus,
                    timestamp = DateTime.UtcNow,
                    serverInfo = "Unity WebSocket Manager v1.0",
                    metadata = new Dictionary<string, object>
                    {
                        {"url", _websocketUrl},
                        {"autoReconnect", IsAutoReconnectEnabled},
                        {"heartbeatInterval", _heartbeatInterval}
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocketManager] 初始化失败: {ex.Message}");
                
                return new WebSocketInitResult
                {
                    success = false,
                    message = $"初始化失败: {ex.Message}",
                    status = WSConnectionStatus.Error,
                    timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task<bool> ConnectAsync()
        {
            if (IsConnected)
            {
                if (_enableLogging)
                {
                    Debug.LogWarning("[WebSocketManager] 已经连接，忽略连接请求");
                }
                return true;
            }
            
            if (ConnectionStatus == WSConnectionStatus.Connecting)
            {
                if (_enableLogging)
                {
                    Debug.LogWarning("[WebSocketManager] 正在连接中，忽略连接请求");
                }
                return false;
            }
            
            try
            {
                SetConnectionStatus(WSConnectionStatus.Connecting);
                _isDisconnecting = false;
                
                // 清理之前的连接
                await CleanupWebSocket();
                
                // 创建新的WebSocket连接
                _webSocket = new NativeWebSocket.WebSocket(_websocketUrl);
                
                // 设置事件处理器
                _webSocket.OnOpen += OnWebSocketOpen;
                _webSocket.OnMessage += OnWebSocketMessage;
                _webSocket.OnError += OnWebSocketError;
                _webSocket.OnClose += OnWebSocketClose;
                
                // 发起连接
                await _webSocket.Connect();
                
                // 等待连接结果
                var timeoutTime = DateTime.UtcNow.AddMilliseconds(_connectionTimeout);
                while (ConnectionStatus == WSConnectionStatus.Connecting && DateTime.UtcNow < timeoutTime)
                {
                    await Task.Delay(100);
                }
                
                if (IsConnected)
                {
                    _currentReconnectAttempt = 0; // 重置重连计数
                    StartMessageProcessor();
                    StartQueueProcessor();
                    
                    if (_enableLogging)
                    {
                        Debug.Log("[WebSocketManager] WebSocket连接成功");
                    }
                    
                    return true;
                }
                else
                {
                    throw new Exception("连接超时");
                }
            }
            catch (Exception ex)
            {
                SetConnectionStatus(WSConnectionStatus.Error);
                
                var error = new WebSocketError
                {
                    code = "CONNECTION_FAILED",
                    message = $"连接失败: {ex.Message}",
                    type = WebSocketErrorType.ConnectionFailed,
                    timestamp = DateTime.UtcNow,
                    isRecoverable = true,
                    suggestion = "请检查网络连接和服务器状态"
                };
                
                OnError?.Invoke(error);
                _statistics.connectionErrors++;
                
                if (_enableLogging)
                {
                    Debug.LogError($"[WebSocketManager] 连接失败: {ex.Message}");
                }
                
                // 如果启用自动重连，开始重连
                if (IsAutoReconnectEnabled)
                {
                    StartReconnect();
                }
                
                return false;
            }
        }

        public async Task<bool> DisconnectAsync(string reason = "User requested")
        {
            _isDisconnecting = true;
            _disconnectReason = reason;
            
            try
            {
                SetConnectionStatus(WSConnectionStatus.Closing);
                
                // 停止所有协程
                StopAllCoroutines();
                
                // 关闭WebSocket连接
                if (_webSocket != null)
                {
                    await _webSocket.Close();
                }
                
                await CleanupWebSocket();
                
                SetConnectionStatus(WSConnectionStatus.Disconnected);
                OnDisconnected?.Invoke(reason);
                
                if (_enableLogging)
                {
                    Debug.Log($"[WebSocketManager] WebSocket已断开: {reason}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocketManager] 断开连接时发生错误: {ex.Message}");
                return false;
            }
            finally
            {
                _isDisconnecting = false;
            }
        }

        public async Task<bool> ReconnectAsync()
        {
            if (_enableLogging)
            {
                Debug.Log("[WebSocketManager] 开始重连...");
            }
            
            SetConnectionStatus(WSConnectionStatus.Reconnecting);
            _currentReconnectAttempt++;
            
            OnReconnectAttempt?.Invoke(_currentReconnectAttempt);
            
            // 断开当前连接
            await DisconnectAsync("Reconnecting");
            
            // 计算重连延迟
            var delay = CalculateReconnectDelay(_currentReconnectAttempt - 1);
            await Task.Delay(delay);
            
            // 尝试重新连接
            var success = await ConnectAsync();
            
            if (success)
            {
                _statistics.reconnectionCount++;
                _currentReconnectAttempt = 0; // 重置计数
                
                if (_enableLogging)
                {
                    Debug.Log("[WebSocketManager] 重连成功");
                }
            }
            else if (_currentReconnectAttempt < _reconnectConfig.maxAttempts)
            {
                // 继续重连
                StartReconnect();
            }
            else
            {
                // 重连失败
                SetConnectionStatus(WSConnectionStatus.Error);
                
                var error = new WebSocketError
                {
                    code = "RECONNECT_FAILED",
                    message = $"重连失败，已尝试{_currentReconnectAttempt}次",
                    type = WebSocketErrorType.ConnectionFailed,
                    timestamp = DateTime.UtcNow,
                    isRecoverable = false
                };
                
                OnError?.Invoke(error);
                
                if (_enableLogging)
                {
                    Debug.LogError($"[WebSocketManager] 重连失败，已达到最大重试次数: {_reconnectConfig.maxAttempts}");
                }
            }
            
            return success;
        }

        public async Task<bool> SendMessageAsync(WebSocketMessage message)
        {
            if (message == null)
            {
                Debug.LogWarning("[WebSocketManager] 尝试发送空消息");
                return false;
            }
            
            // 如果未连接且启用了消息队列，加入队列
            if (!IsConnected)
            {
                if (_enableMessageQueue && _outgoingQueue.Count < _maxQueueSize)
                {
                    _outgoingQueue.Enqueue(message);
                    _statistics.messagesQueued++;
                    
                    if (_enableLogging)
                    {
                        Debug.Log($"[WebSocketManager] 消息已加入发送队列: {message.type}");
                    }
                    
                    return true;
                }
                else
                {
                    if (_enableLogging)
                    {
                        Debug.LogWarning("[WebSocketManager] 未连接且消息队列已满或未启用");
                    }
                    return false;
                }
            }
            
            try
            {
                var json = JsonUtility.ToJson(message);
                await _webSocket.SendText(json);
                
                // 更新统计
                _statistics.messagesSent++;
                _statistics.bytesSent += System.Text.Encoding.UTF8.GetByteCount(json);
                _statistics.lastMessageTime = DateTime.UtcNow;
                
                if (_enableLogging)
                {
                    Debug.Log($"[WebSocketManager] 消息已发送: {message.type} - {message.action}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                var error = new WebSocketError
                {
                    code = "SEND_FAILED",
                    message = $"发送消息失败: {ex.Message}",
                    type = WebSocketErrorType.MessageSendFailed,
                    timestamp = DateTime.UtcNow,
                    isRecoverable = true
                };
                
                OnError?.Invoke(error);
                _statistics.messageErrors++;
                
                if (_enableLogging)
                {
                    Debug.LogError($"[WebSocketManager] 发送消息失败: {ex.Message}");
                }
                
                return false;
            }
        }

        public async Task<bool> SendTextAsync(string text)
        {
            var message = WebSocketMessage.CreateTextMessage("text", text);
            return await SendMessageAsync(message);
        }

        public async Task<bool> SendJsonAsync(object data)
        {
            var message = WebSocketMessage.CreateJsonMessage("json", data);
            return await SendMessageAsync(message);
        }

        public void RegisterMessageHandler(string messageType, Action<WebSocketMessage> handler)
        {
            _messageHandlers[messageType] = handler;
            
            if (_enableLogging)
            {
                Debug.Log($"[WebSocketManager] 已注册消息处理器: {messageType}");
            }
        }

        public void UnregisterMessageHandler(string messageType)
        {
            _messageHandlers.Remove(messageType);
            
            if (_enableLogging)
            {
                Debug.Log($"[WebSocketManager] 已注销消息处理器: {messageType}");
            }
        }

        public void ClearAllHandlers()
        {
            _messageHandlers.Clear();
            
            if (_enableLogging)
            {
                Debug.Log("[WebSocketManager] 已清除所有消息处理器");
            }
        }

        public void StartHeartbeat(int interval = 30000)
        {
            _heartbeatInterval = interval;
            
            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
            }
            
            _heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());
            
            if (_enableLogging)
            {
                Debug.Log($"[WebSocketManager] 心跳已启动，间隔: {interval}ms");
            }
        }

        public void StopHeartbeat()
        {
            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
                _heartbeatCoroutine = null;
            }
            
            if (_enableLogging)
            {
                Debug.Log("[WebSocketManager] 心跳已停止");
            }
        }

        public async Task<int> PingAsync()
        {
            if (!IsConnected)
            {
                return -1;
            }
            
            var startTime = DateTime.UtcNow;
            _lastHeartbeatTime = startTime;
            
            var pingMessage = new WebSocketMessage
            {
                id = Guid.NewGuid().ToString(),
                type = "ping",
                action = "heartbeat",
                timestamp = startTime,
                content = JsonUtility.ToJson(new { ping = startTime.Ticks })
            };
            
            var success = await SendMessageAsync(pingMessage);
            
            if (success)
            {
                // 等待pong响应（最多5秒）
                var timeout = DateTime.UtcNow.AddSeconds(5);
                while (_lastPongTime <= startTime && DateTime.UtcNow < timeout)
                {
                    await Task.Delay(50);
                }
                
                if (_lastPongTime > startTime)
                {
                    var latency = (int)(_lastPongTime - startTime).TotalMilliseconds;
                    Latency = latency;
                    
                    // 更新统计
                    _statistics.minLatency = Math.Min(_statistics.minLatency, latency);
                    _statistics.maxLatency = Math.Max(_statistics.maxLatency, latency);
                    _statistics.averageLatency = (_statistics.averageLatency + latency) / 2;
                    
                    return latency;
                }
            }
            
            return -1; // Ping失败
        }

        public WebSocketStatistics GetStatistics()
        {
            _statistics.totalConnectionTime = DateTime.UtcNow - _statistics.connectionStartTime;
            _statistics.messageRate = _statistics.totalConnectionTime.TotalSeconds > 0 
                ? (float)(_statistics.messagesReceived / _statistics.totalConnectionTime.TotalSeconds) 
                : 0f;
            
            return _statistics;
        }

        public void EnableAutoReconnect(ReconnectConfig config = null)
        {
            _reconnectConfig = config ?? _reconnectConfig;
            IsAutoReconnectEnabled = true;
            
            if (_enableLogging)
            {
                Debug.Log("[WebSocketManager] 自动重连已启用");
            }
        }

        public void DisableAutoReconnect()
        {
            IsAutoReconnectEnabled = false;
            
            if (_reconnectCoroutine != null)
            {
                StopCoroutine(_reconnectCoroutine);
                _reconnectCoroutine = null;
            }
            
            if (_enableLogging)
            {
                Debug.Log("[WebSocketManager] 自动重连已禁用");
            }
        }

        public void EnableMessageQueue(int maxQueueSize = 100)
        {
            _enableMessageQueue = true;
            _maxQueueSize = maxQueueSize;
            
            if (_enableLogging)
            {
                Debug.Log($"[WebSocketManager] 消息队列已启用，最大大小: {maxQueueSize}");
            }
        }

        public void DisableMessageQueue()
        {
            _enableMessageQueue = false;
            _messageQueue.Clear();
            _outgoingQueue.Clear();
            
            if (_enableLogging)
            {
                Debug.Log("[WebSocketManager] 消息队列已禁用");
            }
        }

        public int GetQueuedMessageCount()
        {
            return _messageQueue.Count + _outgoingQueue.Count;
        }

        public void ClearMessageQueue()
        {
            _messageQueue.Clear();
            _outgoingQueue.Clear();
            
            if (_enableLogging)
            {
                Debug.Log("[WebSocketManager] 消息队列已清空");
            }
        }

        #endregion

        #region WebSocket事件处理

        private void OnWebSocketOpen()
        {
            SetConnectionStatus(WSConnectionStatus.Connected);
            
            // 更新统计
            _statistics.connectionAttempts++;
            _statistics.successfulConnections++;
            _statistics.connectionStartTime = DateTime.UtcNow;
            
            // 启动心跳
            StartHeartbeat(_heartbeatInterval);
            
            OnConnected?.Invoke();
            
            if (_enableLogging)
            {
                Debug.Log("[WebSocketManager] WebSocket连接已建立");
            }
        }

        private void OnWebSocketMessage(byte[] data)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                var message = JsonUtility.FromJson<WebSocketMessage>(json);
                
                if (message != null)
                {
                    message.timestamp = DateTime.UtcNow;
                    
                    // 处理特殊消息类型
                    if (message.type == "pong")
                    {
                        _lastPongTime = DateTime.UtcNow;
                        return;
                    }
                    
                    // 加入消息队列等待处理
                    if (_enableMessageQueue && _messageQueue.Count < _maxQueueSize)
                    {
                        _messageQueue.Enqueue(message);
                    }
                    else if (!_enableMessageQueue)
                    {
                        // 直接处理消息
                        ProcessMessage(message);
                    }
                    else
                    {
                        _statistics.messagesDropped++;
                        if (_enableLogging)
                        {
                            Debug.LogWarning("[WebSocketManager] 消息队列已满，丢弃消息");
                        }
                    }
                    
                    // 更新统计
                    _statistics.messagesReceived++;
                    _statistics.bytesReceived += data.Length;
                    _statistics.lastMessageTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                var error = new WebSocketError
                {
                    code = "MESSAGE_PARSE_FAILED",
                    message = $"消息解析失败: {ex.Message}",
                    type = WebSocketErrorType.MessageReceiveFailed,
                    timestamp = DateTime.UtcNow,
                    isRecoverable = false
                };
                
                OnError?.Invoke(error);
                _statistics.messageErrors++;
                
                if (_enableLogging)
                {
                    Debug.LogError($"[WebSocketManager] 消息解析失败: {ex.Message}");
                }
            }
        }

        private void OnWebSocketError(string errorMsg)
        {
            var error = new WebSocketError
            {
                code = "WEBSOCKET_ERROR",
                message = errorMsg,
                type = WebSocketErrorType.NetworkError,
                timestamp = DateTime.UtcNow,
                isRecoverable = true
            };
            
            OnError?.Invoke(error);
            _statistics.totalErrors++;
            _statistics.lastErrorTime = DateTime.UtcNow;
            
            if (_enableLogging)
            {
                Debug.LogError($"[WebSocketManager] WebSocket错误: {errorMsg}");
            }
            
            // 如果启用自动重连，开始重连
            if (IsAutoReconnectEnabled && !_isDisconnecting)
            {
                StartReconnect();
            }
        }

        private void OnWebSocketClose(NativeWebSocket.WebSocketCloseCode closeCode)
        {
            if (!_isDisconnecting)
            {
                SetConnectionStatus(WSConnectionStatus.Disconnected);
                
                if (_enableLogging)
                {
                    Debug.LogWarning($"[WebSocketManager] WebSocket连接意外关闭: {closeCode}");
                }
                
                // 如果启用自动重连且不是主动断开，开始重连
                if (IsAutoReconnectEnabled)
                {
                    StartReconnect();
                }
                else
                {
                    OnDisconnected?.Invoke($"Connection closed: {closeCode}");
                }
            }
        }

        #endregion

        #region 消息处理

        private void ProcessMessage(WebSocketMessage message)
        {
            try
            {
                // 触发全局消息事件
                OnMessageReceived?.Invoke(message);
                
                // 调用注册的处理器
                if (_messageHandlers.ContainsKey(message.type))
                {
                    _messageHandlers[message.type]?.Invoke(message);
                }
                
                if (_enableLogging)
                {
                    Debug.Log($"[WebSocketManager] 处理消息: {message.type} - {message.action}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocketManager] 处理消息时发生错误: {ex.Message}");
            }
        }

        private void StartMessageProcessor()
        {
            if (_messageProcessorCoroutine != null)
            {
                StopCoroutine(_messageProcessorCoroutine);
            }
            
            _messageProcessorCoroutine = StartCoroutine(MessageProcessorCoroutine());
        }

        private IEnumerator MessageProcessorCoroutine()
        {
            while (IsConnected || _messageQueue.Count > 0)
            {
                if (_messageQueue.Count > 0)
                {
                    var message = _messageQueue.Dequeue();
                    ProcessMessage(message);
                }
                else
                {
                    yield return new WaitForSeconds(0.01f); // 10ms间隔
                }
            }
        }

        private void StartQueueProcessor()
        {
            if (_queueProcessorCoroutine != null)
            {
                StopCoroutine(_queueProcessorCoroutine);
            }
            
            _queueProcessorCoroutine = StartCoroutine(QueueProcessorCoroutine());
        }

        private IEnumerator QueueProcessorCoroutine()
        {
            while (IsConnected || _outgoingQueue.Count > 0)
            {
                if (IsConnected && _outgoingQueue.Count > 0)
                {
                    var message = _outgoingQueue.Dequeue();
                    _ = SendMessageAsync(message);
                    _statistics.messagesQueued--;
                }
                else
                {
                    yield return new WaitForSeconds(0.1f); // 100ms间隔
                }
            }
        }

        #endregion

        #region 心跳机制

        private IEnumerator HeartbeatCoroutine()
        {
            while (IsConnected)
            {
                yield return new WaitForSeconds(_heartbeatInterval / 1000f);
                
                if (IsConnected)
                {
                    _ = PingAsync();
                }
            }
        }

        #endregion

        #region 重连机制

        private void StartReconnect()
        {
            if (_reconnectCoroutine != null || !IsAutoReconnectEnabled)
            {
                return;
            }
            
            _reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
        }

        private IEnumerator ReconnectCoroutine()
        {
            while (_currentReconnectAttempt < _reconnectConfig.maxAttempts && IsAutoReconnectEnabled)
            {
                var delay = CalculateReconnectDelay(_currentReconnectAttempt);
                
                if (_enableLogging)
                {
                    Debug.Log($"[WebSocketManager] {delay}ms后进行第{_currentReconnectAttempt + 1}次重连尝试");
                }
                
                yield return new WaitForSeconds(delay / 1000f);
                
                var success = false;
                try
                {
                    success = await ReconnectAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebSocketManager] 重连过程中发生错误: {ex.Message}");
                }
                
                if (success)
                {
                    break;
                }
            }
            
            _reconnectCoroutine = null;
        }

        private int CalculateReconnectDelay(int attempt)
        {
            switch (_reconnectConfig.strategy)
            {
                case ReconnectStrategy.FixedDelay:
                    return _reconnectConfig.baseDelay;
                    
                case ReconnectStrategy.LinearBackoff:
                    return Math.Min(_reconnectConfig.baseDelay * (attempt + 1), _reconnectConfig.maxDelay);
                    
                case ReconnectStrategy.ExponentialBackoff:
                    var delay = (int)(_reconnectConfig.baseDelay * Math.Pow(_reconnectConfig.backoffFactor, attempt));
                    return Math.Min(delay, _reconnectConfig.maxDelay);
                    
                default:
                    return _reconnectConfig.baseDelay;
            }
        }

        #endregion

        #region 辅助方法

        private void SetConnectionStatus(WSConnectionStatus newStatus)
        {
            if (ConnectionStatus != newStatus)
            {
                var oldStatus = ConnectionStatus;
                ConnectionStatus = newStatus;
                
                OnConnectionStatusChanged?.Invoke(newStatus);
                
                if (_enableLogging)
                {
                    Debug.Log($"[WebSocketManager] 连接状态变更: {oldStatus} -> {newStatus}");
                }
            }
        }

        private async Task CleanupWebSocket()
        {
            if (_webSocket != null)
            {
                try
                {
                    _webSocket.OnOpen -= OnWebSocketOpen;
                    _webSocket.OnMessage -= OnWebSocketMessage;
                    _webSocket.OnError -= OnWebSocketError;
                    _webSocket.OnClose -= OnWebSocketClose;
                    
                    if (_webSocket.State == NativeWebSocket.WebSocketState.Open)
                    {
                        await _webSocket.Close();
                    }
                }
                catch (Exception ex)
                {
                    if (_enableLogging)
                    {
                        Debug.LogWarning($"[WebSocketManager] 清理WebSocket时发生错误: {ex.Message}");
                    }
                }
                finally
                {
                    _webSocket = null;
                }
            }
        }

        private void Cleanup()
        {
            _isDisconnecting = true;
            
            // 停止所有协程
            StopAllCoroutines();
            
            // 清理WebSocket
            _ = CleanupWebSocket();
            
            // 清理数据
            _messageHandlers?.Clear();
            _messageQueue?.Clear();
            _outgoingQueue?.Clear();
            
            if (_enableLogging)
            {
                Debug.Log("[WebSocketManager] 资源清理完成");
            }
        }

        #endregion
    }
}