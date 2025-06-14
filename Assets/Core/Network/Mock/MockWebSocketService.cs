// Assets/_Core/Network/Mock/MockWebSocketService.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Network.Interfaces;

namespace Core.Network.Mock
{
    /// <summary>
    /// Mock WebSocket服务 - 模拟optimizedSocket
    /// 提供完整的WebSocket模拟功能，支持前端独立开发
    /// </summary>
    public class MockWebSocketService : MonoBehaviour, IWebSocketService
    {
        #region 属性实现

        public WSConnectionStatus ConnectionStatus { get; private set; } = WSConnectionStatus.Disconnected;
        public bool IsConnected => ConnectionStatus == WSConnectionStatus.Connected;
        public int Latency { get; private set; } = 50;
        public bool IsAutoReconnectEnabled { get; private set; } = false;

        #endregion

        #region 私有字段

        private WebSocketConfig _config;
        private Dictionary<string, Action<WebSocketMessage>> _messageHandlers = new Dictionary<string, Action<WebSocketMessage>>();
        private WebSocketStatistics _statistics = new WebSocketStatistics();
        private ReconnectConfig _reconnectConfig;
        private Queue<WebSocketMessage> _messageQueue = new Queue<WebSocketMessage>();
        private bool _isMessageQueueEnabled = false;
        
        // 模拟相关
        private Coroutine _heartbeatCoroutine;
        private Coroutine _messageSimulatorCoroutine;
        private Coroutine _countdownCoroutine;
        private MockDataGenerator _dataGenerator;
        
        // 游戏状态模拟
        private string _currentGameNumber;
        private BaccaratGamePhase _currentPhase = BaccaratGamePhase.Betting;
        private int _countdown = 30;
        private bool _isSimulationRunning = false;

        #endregion

        #region 事件

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<WebSocketError> OnError;
        public event Action<WebSocketMessage> OnMessageReceived;
        public event Action<WSConnectionStatus> OnConnectionStatusChanged;
        public event Action<int> OnReconnectAttempt;

        #endregion

        #region 初始化

        private void Awake()
        {
            _dataGenerator = new MockDataGenerator();
            _currentGameNumber = GenerateGameNumber();
            InitializeStatistics();
        }

        private void InitializeStatistics()
        {
            _statistics.connectionStartTime = DateTime.UtcNow;
            _statistics.minLatency = 30;
            _statistics.maxLatency = 100;
            _statistics.averageLatency = 50;
        }

        #endregion

        #region IWebSocketService 实现

        public async Task<WebSocketInitResult> InitializeAsync(WebSocketConfig config)
        {
            _config = config;
            
            // 模拟初始化延迟
            await Task.Delay(UnityEngine.Random.Range(500, 1000));
            
            Debug.Log($"[MockWebSocketService] 初始化完成 - URL: {config.url}");
            
            return new WebSocketInitResult
            {
                success = true,
                message = "Mock WebSocket service initialized",
                status = WSConnectionStatus.Disconnected,
                timestamp = DateTime.UtcNow,
                serverInfo = "Mock Server v1.0",
                metadata = new Dictionary<string, object>
                {
                    {"mock", true},
                    {"version", "1.0.0"},
                    {"features", new[] {"countdown", "game_result", "win_data"}}
                }
            };
        }

        public async Task<bool> ConnectAsync()
        {
            if (IsConnected)
            {
                Debug.LogWarning("[MockWebSocketService] 已经连接");
                return true;
            }
            
            SetConnectionStatus(WSConnectionStatus.Connecting);
            
            // 模拟连接延迟
            await Task.Delay(UnityEngine.Random.Range(1000, 2000));
            
            // 模拟5%的连接失败概率
            if (UnityEngine.Random.Range(0f, 1f) < 0.05f)
            {
                SetConnectionStatus(WSConnectionStatus.Error);
                var error = new WebSocketError
                {
                    code = "CONNECTION_FAILED",
                    message = "模拟连接失败",
                    type = WebSocketErrorType.ConnectionFailed,
                    timestamp = DateTime.UtcNow,
                    isRecoverable = true,
                    suggestion = "请检查网络连接并重试"
                };
                OnError?.Invoke(error);
                return false;
            }
            
            SetConnectionStatus(WSConnectionStatus.Connected);
            
            // 更新统计
            _statistics.connectionAttempts++;
            _statistics.successfulConnections++;
            _statistics.connectionStartTime = DateTime.UtcNow;
            
            // 开始模拟
            StartSimulation();
            
            OnConnected?.Invoke();
            Debug.Log("[MockWebSocketService] 连接成功");
            
            return true;
        }

        public async Task<bool> DisconnectAsync(string reason = "User requested")
        {
            if (!IsConnected)
            {
                return true;
            }
            
            SetConnectionStatus(WSConnectionStatus.Closing);
            
            // 停止所有模拟
            StopSimulation();
            
            // 模拟断开延迟
            await Task.Delay(200);
            
            SetConnectionStatus(WSConnectionStatus.Disconnected);
            
            OnDisconnected?.Invoke(reason);
            Debug.Log($"[MockWebSocketService] 已断开连接: {reason}");
            
            return true;
        }

        public async Task<bool> ReconnectAsync()
        {
            Debug.Log("[MockWebSocketService] 开始重连...");
            
            SetConnectionStatus(WSConnectionStatus.Reconnecting);
            _statistics.reconnectionCount++;
            
            OnReconnectAttempt?.Invoke(_statistics.reconnectionCount);
            
            await DisconnectAsync("Reconnecting");
            await Task.Delay(1000); // 重连延迟
            
            return await ConnectAsync();
        }

        public async Task<bool> SendMessageAsync(WebSocketMessage message)
        {
            if (!IsConnected && !_isMessageQueueEnabled)
            {
                Debug.LogWarning("[MockWebSocketService] 未连接且消息队列未启用");
                return false;
            }
            
            if (!IsConnected && _isMessageQueueEnabled)
            {
                _messageQueue.Enqueue(message);
                _statistics.messagesQueued++;
                Debug.Log($"[MockWebSocketService] 消息已加入队列: {message.type}");
                return true;
            }
            
            // 模拟发送延迟
            await Task.Delay(UnityEngine.Random.Range(10, 50));
            
            // 模拟1%的发送失败概率
            if (UnityEngine.Random.Range(0f, 1f) < 0.01f)
            {
                var error = new WebSocketError
                {
                    code = "SEND_FAILED",
                    message = "模拟发送失败",
                    type = WebSocketErrorType.MessageSendFailed,
                    timestamp = DateTime.UtcNow,
                    isRecoverable = true
                };
                OnError?.Invoke(error);
                _statistics.messageErrors++;
                return false;
            }
            
            // 更新统计
            _statistics.messagesSent++;
            _statistics.bytesSent += System.Text.Encoding.UTF8.GetByteCount(message.content ?? "");
            _statistics.lastMessageTime = DateTime.UtcNow;
            
            Debug.Log($"[MockWebSocketService] 消息已发送: {message.type} - {message.action}");
            
            // 模拟某些消息的自动回复
            HandleAutoResponse(message);
            
            return true;
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
            Debug.Log($"[MockWebSocketService] 已注册消息处理器: {messageType}");
        }

        public void UnregisterMessageHandler(string messageType)
        {
            _messageHandlers.Remove(messageType);
            Debug.Log($"[MockWebSocketService] 已注销消息处理器: {messageType}");
        }

        public void ClearAllHandlers()
        {
            _messageHandlers.Clear();
            Debug.Log("[MockWebSocketService] 已清除所有消息处理器");
        }

        public void StartHeartbeat(int interval = 30000)
        {
            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
            }
            
            _heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine(interval));
            Debug.Log($"[MockWebSocketService] 心跳已启动，间隔: {interval}ms");
        }

        public void StopHeartbeat()
        {
            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
                _heartbeatCoroutine = null;
            }
            
            Debug.Log("[MockWebSocketService] 心跳已停止");
        }

        public async Task<int> PingAsync()
        {
            if (!IsConnected)
            {
                return -1;
            }
            
            var startTime = DateTime.UtcNow;
            
            // 模拟Ping延迟
            await Task.Delay(UnityEngine.Random.Range(30, 100));
            
            var latency = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            // 更新延迟统计
            Latency = latency;
            _statistics.minLatency = Math.Min(_statistics.minLatency, latency);
            _statistics.maxLatency = Math.Max(_statistics.maxLatency, latency);
            _statistics.averageLatency = (_statistics.averageLatency + latency) / 2;
            
            Debug.Log($"[MockWebSocketService] Ping: {latency}ms");
            return latency;
        }

        public WebSocketStatistics GetStatistics()
        {
            _statistics.totalConnectionTime = DateTime.UtcNow - _statistics.connectionStartTime;
            _statistics.messageRate = _statistics.messagesReceived / (float)_statistics.totalConnectionTime.TotalSeconds;
            return _statistics;
        }

        public void EnableAutoReconnect(ReconnectConfig config = null)
        {
            _reconnectConfig = config ?? new ReconnectConfig();
            IsAutoReconnectEnabled = true;
            Debug.Log("[MockWebSocketService] 自动重连已启用");
        }

        public void DisableAutoReconnect()
        {
            IsAutoReconnectEnabled = false;
            Debug.Log("[MockWebSocketService] 自动重连已禁用");
        }

        public void EnableMessageQueue(int maxQueueSize = 100)
        {
            _isMessageQueueEnabled = true;
            Debug.Log($"[MockWebSocketService] 消息队列已启用，最大大小: {maxQueueSize}");
        }

        public void DisableMessageQueue()
        {
            _isMessageQueueEnabled = false;
            _messageQueue.Clear();
            Debug.Log("[MockWebSocketService] 消息队列已禁用");
        }

        public int GetQueuedMessageCount()
        {
            return _messageQueue.Count;
        }

        public void ClearMessageQueue()
        {
            _messageQueue.Clear();
            Debug.Log("[MockWebSocketService] 消息队列已清空");
        }

        #endregion

        #region 模拟逻辑

        private void StartSimulation()
        {
            if (_isSimulationRunning) return;
            
            _isSimulationRunning = true;
            
            // 启动各种模拟协程
            _messageSimulatorCoroutine = StartCoroutine(MessageSimulatorCoroutine());
            _countdownCoroutine = StartCoroutine(CountdownCoroutine());
            
            // 启动心跳
            StartHeartbeat();
            
            // 发送队列中的消息
            StartCoroutine(ProcessMessageQueue());
        }

        private void StopSimulation()
        {
            _isSimulationRunning = false;
            
            if (_messageSimulatorCoroutine != null)
            {
                StopCoroutine(_messageSimulatorCoroutine);
                _messageSimulatorCoroutine = null;
            }
            
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
            
            StopHeartbeat();
        }

        private IEnumerator MessageSimulatorCoroutine()
        {
            while (_isSimulationRunning && IsConnected)
            {
                // 模拟随机系统消息
                if (UnityEngine.Random.Range(0f, 1f) < 0.1f) // 10%概率
                {
                    SimulateSystemMessage();
                }
                
                // 模拟热门投注数据
                if (UnityEngine.Random.Range(0f, 1f) < 0.2f) // 20%概率
                {
                    SimulatePopularBetsUpdate();
                }
                
                yield return new WaitForSeconds(UnityEngine.Random.Range(5f, 15f));
            }
        }

        private IEnumerator CountdownCoroutine()
        {
            while (_isSimulationRunning && IsConnected)
            {
                // 发送倒计时消息
                SimulateCountdownMessage();
                
                _countdown--;
                
                // 检查阶段切换
                if (_countdown <= 0)
                {
                    switch (_currentPhase)
                    {
                        case BaccaratGamePhase.Betting:
                            _currentPhase = BaccaratGamePhase.Dealing;
                            _countdown = 15;
                            SimulateGameStatusMessage("停止投注");
                            break;
                            
                        case BaccaratGamePhase.Dealing:
                            _currentPhase = BaccaratGamePhase.Result;
                            _countdown = 10;
                            SimulateGameResult();
                            break;
                            
                        case BaccaratGamePhase.Result:
                            _currentPhase = BaccaratGamePhase.Betting;
                            _countdown = 30;
                            _currentGameNumber = GenerateGameNumber();
                            SimulateGameStatusMessage("开始投注");
                            yield return new WaitForSeconds(2f); // 结算间隔
                            break;
                    }
                }
                
                yield return new WaitForSeconds(1f); // 每秒更新
            }
        }

        private IEnumerator HeartbeatCoroutine(int interval)
        {
            while (_isSimulationRunning && IsConnected)
            {
                yield return new WaitForSeconds(interval / 1000f);
                
                // 发送心跳
                var heartbeat = new WebSocketMessage
                {
                    id = Guid.NewGuid().ToString(),
                    type = "heartbeat",
                    action = "ping",
                    timestamp = DateTime.UtcNow,
                    content = "{\"ping\":\"pong\"}"
                };
                
                await PingAsync(); // 更新延迟
            }
        }

        private IEnumerator ProcessMessageQueue()
        {
            while (_isSimulationRunning)
            {
                if (IsConnected && _messageQueue.Count > 0)
                {
                    var message = _messageQueue.Dequeue();
                    await SendMessageAsync(message);
                    _statistics.messagesQueued--;
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }

        #endregion

        #region 消息模拟

        private void SimulateCountdownMessage()
        {
            var countdownMsg = new BaccaratCountdownMessage
            {
                action = "countdown_update",
                table_id = _config?.tableId ?? "1",
                game_number = _currentGameNumber,
                countdown = _countdown,
                total_time = GetTotalTime(),
                phase = _currentPhase,
                betting_enabled = _currentPhase == BaccaratGamePhase.Betting,
                server_time = DateTime.UtcNow
            };
            
            var message = WebSocketMessage.CreateJsonMessage("countdown", countdownMsg);
            ReceiveMessage(message);
        }

        private void SimulateGameResult()
        {
            var gameResult = _dataGenerator.GenerateGameResult(_currentGameNumber);
            
            var resultMsg = new BaccaratGameResultMessage
            {
                action = "game_end",
                table_id = _config?.tableId ?? "1",
                game_number = _currentGameNumber,
                result = gameResult,
                banker_cards = gameResult.banker_cards,
                player_cards = gameResult.player_cards,
                winner = gameResult.winner,
                winning_areas = gameResult.winning_bets,
                result_time = DateTime.UtcNow
            };
            
            var message = WebSocketMessage.CreateJsonMessage("game_result", resultMsg);
            ReceiveMessage(message);
            
            // 延迟发送中奖消息
            StartCoroutine(DelayedWinMessage(gameResult));
        }

        private IEnumerator DelayedWinMessage(BaccaratGameResult gameResult)
        {
            yield return new WaitForSeconds(2f);
            
            // 模拟中奖消息（假设用户有投注）
            if (UnityEngine.Random.Range(0f, 1f) < 0.3f) // 30%概率中奖
            {
                SimulateWinMessage(gameResult);
            }
            
            // 模拟余额更新
            SimulateBalanceUpdate();
        }

        private void SimulateWinMessage(BaccaratGameResult gameResult)
        {
            var winBets = new List<WinBet>();
            
            // 模拟一些中奖投注
            if (gameResult.winner == BaccaratWinner.Banker)
            {
                winBets.Add(new WinBet
                {
                    bet_type = "庄家",
                    bet_amount = 100f,
                    win_amount = 195f,
                    odds = 0.95f,
                    bet_id = Guid.NewGuid().ToString()
                });
            }
            
            var totalBet = 0f;
            var totalWin = 0f;
            foreach (var bet in winBets)
            {
                totalBet += bet.bet_amount;
                totalWin += bet.win_amount;
            }
            
            var winMsg = new BaccaratWinMessage
            {
                action = "payout",
                user_id = _config?.userId ?? "mock_user",
                game_number = _currentGameNumber,
                winning_bets = winBets,
                total_win_amount = totalWin,
                total_bet_amount = totalBet,
                net_profit = totalWin - totalBet,
                new_balance = 10000f + (totalWin - totalBet),
                payout_time = DateTime.UtcNow
            };
            
            var message = WebSocketMessage.CreateJsonMessage("win_data", winMsg);
            ReceiveMessage(message);
        }

        private void SimulateBalanceUpdate()
        {
            var balanceMsg = new BaccaratBalanceUpdateMessage
            {
                action = "balance_change",
                user_id = _config?.userId ?? "mock_user",
                new_balance = UnityEngine.Random.Range(8000f, 15000f),
                old_balance = UnityEngine.Random.Range(8000f, 15000f),
                change_amount = UnityEngine.Random.Range(-500f, 500f),
                change_reason = "游戏结算",
                game_number = _currentGameNumber,
                update_time = DateTime.UtcNow
            };
            
            var message = WebSocketMessage.CreateJsonMessage("balance_update", balanceMsg);
            ReceiveMessage(message);
        }

        private void SimulateGameStatusMessage(string statusText)
        {
            var statusMsg = new BaccaratGameStatusMessage
            {
                action = "status_change",
                table_id = _config?.tableId ?? "1",
                game_number = _currentGameNumber,
                phase = _currentPhase,
                countdown = _countdown,
                betting_open = _currentPhase == BaccaratGamePhase.Betting,
                dealer_name = "李小姐",
                popular_bets = new PopularBets
                {
                    banker_popularity = UnityEngine.Random.Range(0.4f, 0.7f),
                    player_popularity = UnityEngine.Random.Range(0.3f, 0.6f),
                    tie_popularity = UnityEngine.Random.Range(0.05f, 0.15f)
                },
                status_time = DateTime.UtcNow
            };
            
            var message = WebSocketMessage.CreateJsonMessage("game_status", statusMsg);
            ReceiveMessage(message);
        }

        private void SimulateSystemMessage()
        {
            string[] messages = {
                "欢迎来到百家乐游戏！",
                "系统维护通知：今晚23:00-24:00",
                "新功能上线：免佣投注现已开放",
                "祝您游戏愉快，理性投注！"
            };
            
            var systemMsg = new BaccaratSystemMessage
            {
                action = "system_notify",
                message_type = "info",
                title = "系统消息",
                content = messages[UnityEngine.Random.Range(0, messages.Length)],
                is_popup = false,
                is_dismissible = true,
                timestamp = DateTime.UtcNow,
                duration = 5000
            };
            
            var message = WebSocketMessage.CreateJsonMessage("system_message", systemMsg);
            ReceiveMessage(message);
        }

        private void SimulatePopularBetsUpdate()
        {
            // 模拟热门投注数据更新
            var popularBetsData = new
            {
                type = "popular_bets_update",
                action = "data_refresh",
                table_id = _config?.tableId ?? "1",
                banker_total = UnityEngine.Random.Range(10000f, 50000f),
                player_total = UnityEngine.Random.Range(8000f, 40000f),
                tie_total = UnityEngine.Random.Range(1000f, 5000f),
                update_time = DateTime.UtcNow
            };
            
            var message = WebSocketMessage.CreateJsonMessage("popular_bets_update", popularBetsData);
            ReceiveMessage(message);
        }

        #endregion

        #region 辅助方法

        private void SetConnectionStatus(WSConnectionStatus newStatus)
        {
            if (ConnectionStatus != newStatus)
            {
                ConnectionStatus = newStatus;
                OnConnectionStatusChanged?.Invoke(newStatus);
                Debug.Log($"[MockWebSocketService] 连接状态变更: {newStatus}");
            }
        }

        private void ReceiveMessage(WebSocketMessage message)
        {
            if (!IsConnected) return;
            
            // 更新统计
            _statistics.messagesReceived++;
            _statistics.bytesReceived += System.Text.Encoding.UTF8.GetByteCount(message.content ?? "");
            _statistics.lastMessageTime = DateTime.UtcNow;
            
            // 触发事件
            OnMessageReceived?.Invoke(message);
            
            // 调用注册的处理器
            if (_messageHandlers.ContainsKey(message.type))
            {
                try
                {
                    _messageHandlers[message.type]?.Invoke(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MockWebSocketService] 消息处理器异常: {ex.Message}");
                }
            }
            
            Debug.Log($"[MockWebSocketService] 收到消息: {message.type} - {message.action}");
        }

        private void HandleAutoResponse(WebSocketMessage message)
        {
            // 对某些消息类型自动回复
            switch (message.type)
            {
                case "heartbeat":
                    var pongMessage = WebSocketMessage.CreateTextMessage("heartbeat", "{\"pong\":\"ping\"}");
                    StartCoroutine(DelayedResponse(pongMessage, 0.1f));
                    break;
                    
                case "join_table":
                    var joinResponse = WebSocketMessage.CreateJsonMessage("join_table_response", new
                    {
                        success = true,
                        table_id = _config?.tableId ?? "1",
                        message = "加入桌台成功"
                    });
                    StartCoroutine(DelayedResponse(joinResponse, 0.5f));
                    break;
            }
        }

        private IEnumerator DelayedResponse(WebSocketMessage message, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReceiveMessage(message);
        }

        private string GenerateGameNumber()
        {
            return DateTime.UtcNow.ToString("yyyyMMddHHmm") + UnityEngine.Random.Range(100, 999);
        }

        private int GetTotalTime()
        {
            switch (_currentPhase)
            {
                case BaccaratGamePhase.Betting: return 30;
                case BaccaratGamePhase.Dealing: return 15;
                case BaccaratGamePhase.Result: return 10;
                default: return 30;
            }
        }

        #endregion

        #region Unity生命周期

        private void OnDestroy()
        {
            StopSimulation();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && IsConnected)
            {
                // 应用暂停时断开连接
                _ = DisconnectAsync("Application paused");
            }
            else if (!pauseStatus && IsAutoReconnectEnabled)
            {
                // 应用恢复时重连
                _ = ReconnectAsync();
            }
        }

        #endregion
    }
}