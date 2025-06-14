// Assets/_Core/Network/WebSocket/GameMessageDispatcher.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Network.Interfaces;

namespace Core.Network.WebSocket
{
    /// <summary>
    /// 游戏消息分发器
    /// 负责解析WebSocket消息并分发到对应的处理器，支持消息路由、过滤和转换
    /// </summary>
    public class GameMessageDispatcher
    {
        #region 百家乐消息事件

        public event Action<BaccaratCountdownMessage> OnBaccaratCountdownMessage;
        public event Action<BaccaratGameResultMessage> OnBaccaratGameResultMessage;
        public event Action<BaccaratWinMessage> OnBaccaratWinMessage;
        public event Action<BaccaratGameStatusMessage> OnBaccaratGameStatusMessage;
        public event Action<BaccaratBalanceUpdateMessage> OnBaccaratBalanceUpdateMessage;
        public event Action<BaccaratRoadmapUpdateMessage> OnBaccaratRoadmapUpdateMessage;
        public event Action<BaccaratSystemMessage> OnBaccaratSystemMessage;

        #endregion

        #region 通用消息事件

        public event Action<WebSocketMessage> OnUnknownMessage;
        public event Action<MessageParseError> OnParseError;

        #endregion

        #region 私有字段

        private Dictionary<string, Func<WebSocketMessage, bool>> _messageHandlers;
        private Dictionary<string, int> _messageStats;
        private bool _enableLogging = true;
        private bool _enableMessageValidation = true;
        private bool _enableMessageTransformation = true;

        #endregion

        #region 构造函数

        public GameMessageDispatcher()
        {
            Initialize();
        }

        #endregion

        #region 初始化

        private void Initialize()
        {
            _messageHandlers = new Dictionary<string, Func<WebSocketMessage, bool>>();
            _messageStats = new Dictionary<string, int>();
            
            // 注册百家乐消息处理器
            RegisterBaccaratMessageHandlers();
            
            Debug.Log("[GameMessageDispatcher] 游戏消息分发器已初始化");
        }

        private void RegisterBaccaratMessageHandlers()
        {
            _messageHandlers["countdown"] = HandleCountdownMessage;
            _messageHandlers["game_result"] = HandleGameResultMessage;
            _messageHandlers["win_data"] = HandleWinMessage;
            _messageHandlers["game_status"] = HandleGameStatusMessage;
            _messageHandlers["balance_update"] = HandleBalanceUpdateMessage;
            _messageHandlers["roadmap_update"] = HandleRoadmapUpdateMessage;
            _messageHandlers["system_message"] = HandleSystemMessage;
            
            // 通用消息类型
            _messageHandlers["heartbeat"] = HandleHeartbeatMessage;
            _messageHandlers["ping"] = HandlePingMessage;
            _messageHandlers["pong"] = HandlePongMessage;
            _messageHandlers["error"] = HandleErrorMessage;
            _messageHandlers["join_table_response"] = HandleJoinTableResponse;
            _messageHandlers["leave_table_response"] = HandleLeaveTableResponse;
            
            if (_enableLogging)
            {
                Debug.Log($"[GameMessageDispatcher] 已注册 {_messageHandlers.Count} 个消息处理器");
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 分发WebSocket消息到对应的处理器
        /// </summary>
        public bool DispatchMessage(WebSocketMessage message)
        {
            if (message == null)
            {
                if (_enableLogging)
                {
                    Debug.LogWarning("[GameMessageDispatcher] 尝试分发空消息");
                }
                return false;
            }
            
            try
            {
                // 更新统计
                UpdateMessageStatistics(message.type);
                
                // 消息验证
                if (_enableMessageValidation && !ValidateMessage(message))
                {
                    if (_enableLogging)
                    {
                        Debug.LogWarning($"[GameMessageDispatcher] 消息验证失败: {message.type}");
                    }
                    return false;
                }
                
                // 消息转换
                if (_enableMessageTransformation)
                {
                    message = TransformMessage(message);
                }
                
                // 查找并执行处理器
                if (_messageHandlers.ContainsKey(message.type))
                {
                    var success = _messageHandlers[message.type](message);
                    
                    if (_enableLogging && success)
                    {
                        Debug.Log($"[GameMessageDispatcher] 消息分发成功: {message.type} - {message.action}");
                    }
                    
                    return success;
                }
                else
                {
                    // 未知消息类型
                    OnUnknownMessage?.Invoke(message);
                    
                    if (_enableLogging)
                    {
                        Debug.LogWarning($"[GameMessageDispatcher] 未知消息类型: {message.type}");
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                var parseError = new MessageParseError
                {
                    originalMessage = message,
                    errorMessage = ex.Message,
                    timestamp = DateTime.UtcNow,
                    errorType = "DISPATCH_ERROR"
                };
                
                OnParseError?.Invoke(parseError);
                
                if (_enableLogging)
                {
                    Debug.LogError($"[GameMessageDispatcher] 分发消息时发生错误: {ex.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// 直接分发百家乐倒计时消息
        /// </summary>
        public void DispatchBaccaratCountdownMessage(BaccaratCountdownMessage message)
        {
            try
            {
                OnBaccaratCountdownMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameMessageDispatcher] 分发倒计时消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 直接分发百家乐游戏结果消息
        /// </summary>
        public void DispatchBaccaratGameResultMessage(BaccaratGameResultMessage message)
        {
            try
            {
                OnBaccaratGameResultMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameMessageDispatcher] 分发游戏结果消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 直接分发百家乐中奖消息
        /// </summary>
        public void DispatchBaccaratWinMessage(BaccaratWinMessage message)
        {
            try
            {
                OnBaccaratWinMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameMessageDispatcher] 分发中奖消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 直接分发百家乐游戏状态消息
        /// </summary>
        public void DispatchBaccaratGameStatusMessage(BaccaratGameStatusMessage message)
        {
            try
            {
                OnBaccaratGameStatusMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameMessageDispatcher] 分发游戏状态消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 直接分发百家乐余额更新消息
        /// </summary>
        public void DispatchBaccaratBalanceUpdateMessage(BaccaratBalanceUpdateMessage message)
        {
            try
            {
                OnBaccaratBalanceUpdateMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameMessageDispatcher] 分发余额更新消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 直接分发百家乐路纸更新消息
        /// </summary>
        public void DispatchBaccaratRoadmapUpdateMessage(BaccaratRoadmapUpdateMessage message)
        {
            try
            {
                OnBaccaratRoadmapUpdateMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameMessageDispatcher] 分发路纸更新消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 直接分发百家乐系统消息
        /// </summary>
        public void DispatchBaccaratSystemMessage(BaccaratSystemMessage message)
        {
            try
            {
                OnBaccaratSystemMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameMessageDispatcher] 分发系统消息时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取消息统计信息
        /// </summary>
        public Dictionary<string, int> GetMessageStatistics()
        {
            return new Dictionary<string, int>(_messageStats);
        }

        /// <summary>
        /// 重置消息统计
        /// </summary>
        public void ResetStatistics()
        {
            _messageStats.Clear();
            Debug.Log("[GameMessageDispatcher] 消息统计已重置");
        }

        /// <summary>
        /// 设置日志启用状态
        /// </summary>
        public void SetLoggingEnabled(bool enabled)
        {
            _enableLogging = enabled;
        }

        /// <summary>
        /// 设置消息验证启用状态
        /// </summary>
        public void SetMessageValidationEnabled(bool enabled)
        {
            _enableMessageValidation = enabled;
        }

        /// <summary>
        /// 设置消息转换启用状态
        /// </summary>
        public void SetMessageTransformationEnabled(bool enabled)
        {
            _enableMessageTransformation = enabled;
        }

        #endregion

        #region 消息处理器

        private bool HandleCountdownMessage(WebSocketMessage message)
        {
            try
            {
                var countdownMessage = ParseMessage<BaccaratCountdownMessage>(message);
                if (countdownMessage != null)
                {
                    // 数据验证和补充
                    if (countdownMessage.server_time == default(DateTime))
                    {
                        countdownMessage.server_time = DateTime.UtcNow;
                    }
                    
                    OnBaccaratCountdownMessage?.Invoke(countdownMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogParseError("countdown", ex.Message);
            }
            
            return false;
        }

        private bool HandleGameResultMessage(WebSocketMessage message)
        {
            try
            {
                var resultMessage = ParseMessage<BaccaratGameResultMessage>(message);
                if (resultMessage != null)
                {
                    // 数据验证
                    if (string.IsNullOrEmpty(resultMessage.game_number))
                    {
                        Debug.LogWarning("[GameMessageDispatcher] 游戏结果消息缺少游戏号");
                        return false;
                    }
                    
                    // 补充默认时间
                    if (resultMessage.result_time == default(DateTime))
                    {
                        resultMessage.result_time = DateTime.UtcNow;
                    }
                    
                    OnBaccaratGameResultMessage?.Invoke(resultMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogParseError("game_result", ex.Message);
            }
            
            return false;
        }

        private bool HandleWinMessage(WebSocketMessage message)
        {
            try
            {
                var winMessage = ParseMessage<BaccaratWinMessage>(message);
                if (winMessage != null)
                {
                    // 数据验证
                    if (string.IsNullOrEmpty(winMessage.user_id))
                    {
                        Debug.LogWarning("[GameMessageDispatcher] 中奖消息缺少用户ID");
                        return false;
                    }
                    
                    // 补充默认时间
                    if (winMessage.payout_time == default(DateTime))
                    {
                        winMessage.payout_time = DateTime.UtcNow;
                    }
                    
                    // 初始化空集合
                    if (winMessage.winning_bets == null)
                    {
                        winMessage.winning_bets = new List<WinBet>();
                    }
                    
                    OnBaccaratWinMessage?.Invoke(winMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogParseError("win_data", ex.Message);
            }
            
            return false;
        }

        private bool HandleGameStatusMessage(WebSocketMessage message)
        {
            try
            {
                var statusMessage = ParseMessage<BaccaratGameStatusMessage>(message);
                if (statusMessage != null)
                {
                    // 补充默认时间
                    if (statusMessage.status_time == default(DateTime))
                    {
                        statusMessage.status_time = DateTime.UtcNow;
                    }
                    
                    // 初始化空对象
                    if (statusMessage.popular_bets == null)
                    {
                        statusMessage.popular_bets = new PopularBets();
                    }
                    
                    OnBaccaratGameStatusMessage?.Invoke(statusMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogParseError("game_status", ex.Message);
            }
            
            return false;
        }

        private bool HandleBalanceUpdateMessage(WebSocketMessage message)
        {
            try
            {
                var balanceMessage = ParseMessage<BaccaratBalanceUpdateMessage>(message);
                if (balanceMessage != null)
                {
                    // 补充默认时间
                    if (balanceMessage.update_time == default(DateTime))
                    {
                        balanceMessage.update_time = DateTime.UtcNow;
                    }
                    
                    OnBaccaratBalanceUpdateMessage?.Invoke(balanceMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogParseError("balance_update", ex.Message);
            }
            
            return false;
        }

        private bool HandleRoadmapUpdateMessage(WebSocketMessage message)
        {
            try
            {
                var roadmapMessage = ParseMessage<BaccaratRoadmapUpdateMessage>(message);
                if (roadmapMessage != null)
                {
                    // 补充默认时间
                    if (roadmapMessage.update_time == default(DateTime))
                    {
                        roadmapMessage.update_time = DateTime.UtcNow;
                    }
                    
                    // 初始化空对象
                    if (roadmapMessage.new_bead == null)
                    {
                        roadmapMessage.new_bead = new RoadmapBead();
                    }
                    
                    if (roadmapMessage.updated_stats == null)
                    {
                        roadmapMessage.updated_stats = new BaccaratStatistics();
                    }
                    
                    OnBaccaratRoadmapUpdateMessage?.Invoke(roadmapMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogParseError("roadmap_update", ex.Message);
            }
            
            return false;
        }

        private bool HandleSystemMessage(WebSocketMessage message)
        {
            try
            {
                var systemMessage = ParseMessage<BaccaratSystemMessage>(message);
                if (systemMessage != null)
                {
                    // 补充默认值
                    if (systemMessage.timestamp == default(DateTime))
                    {
                        systemMessage.timestamp = DateTime.UtcNow;
                    }
                    
                    if (systemMessage.duration <= 0)
                    {
                        systemMessage.duration = 5000; // 默认显示5秒
                    }
                    
                    OnBaccaratSystemMessage?.Invoke(systemMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogParseError("system_message", ex.Message);
            }
            
            return false;
        }

        private bool HandleHeartbeatMessage(WebSocketMessage message)
        {
            // 心跳消息通常不需要特殊处理
            return true;
        }

        private bool HandlePingMessage(WebSocketMessage message)
        {
            // Ping消息通常不需要特殊处理
            return true;
        }

        private bool HandlePongMessage(WebSocketMessage message)
        {
            // Pong消息通常不需要特殊处理
            return true;
        }

        private bool HandleErrorMessage(WebSocketMessage message)
        {
            try
            {
                if (_enableLogging)
                {
                    Debug.LogError($"[GameMessageDispatcher] 收到错误消息: {message.content}");
                }
                return true;
            }
            catch (Exception ex)
            {
                LogParseError("error", ex.Message);
                return false;
            }
        }

        private bool HandleJoinTableResponse(WebSocketMessage message)
        {
            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[GameMessageDispatcher] 加入台桌响应: {message.content}");
                }
                return true;
            }
            catch (Exception ex)
            {
                LogParseError("join_table_response", ex.Message);
                return false;
            }
        }

        private bool HandleLeaveTableResponse(WebSocketMessage message)
        {
            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[GameMessageDispatcher] 离开台桌响应: {message.content}");
                }
                return true;
            }
            catch (Exception ex)
            {
                LogParseError("leave_table_response", ex.Message);
                return false;
            }
        }

        #endregion

        #region 辅助方法

        private T ParseMessage<T>(WebSocketMessage message) where T : class
        {
            if (string.IsNullOrEmpty(message.content))
            {
                return null;
            }
            
            try
            {
                return JsonUtility.FromJson<T>(message.content);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameMessageDispatcher] 解析消息失败 - 类型: {typeof(T).Name}, 错误: {ex.Message}");
                return null;
            }
        }

        private bool ValidateMessage(WebSocketMessage message)
        {
            // 基础验证
            if (string.IsNullOrEmpty(message.type))
            {
                return false;
            }
            
            // 时间戳验证
            if (message.timestamp == default(DateTime))
            {
                message.timestamp = DateTime.UtcNow;
            }
            
            // ID验证
            if (string.IsNullOrEmpty(message.id))
            {
                message.id = Guid.NewGuid().ToString();
            }
            
            return true;
        }

        private WebSocketMessage TransformMessage(WebSocketMessage message)
        {
            // 消息转换逻辑，例如格式标准化、字段映射等
            
            // 确保action字段不为空
            if (string.IsNullOrEmpty(message.action))
            {
                switch (message.type)
                {
                    case "countdown":
                        message.action = "countdown_update";
                        break;
                    case "game_result":
                        message.action = "game_end";
                        break;
                    case "win_data":
                        message.action = "payout";
                        break;
                    case "game_status":
                        message.action = "status_change";
                        break;
                    case "balance_update":
                        message.action = "balance_change";
                        break;
                    case "system_message":
                        message.action = "system_notify";
                        break;
                    default:
                        message.action = "unknown";
                        break;
                }
            }
            
            return message;
        }

        private void UpdateMessageStatistics(string messageType)
        {
            if (_messageStats.ContainsKey(messageType))
            {
                _messageStats[messageType]++;
            }
            else
            {
                _messageStats[messageType] = 1;
            }
        }

        private void LogParseError(string messageType, string errorMessage)
        {
            var parseError = new MessageParseError
            {
                originalMessage = null,
                errorMessage = errorMessage,
                timestamp = DateTime.UtcNow,
                errorType = "PARSE_ERROR",
                messageType = messageType
            };
            
            OnParseError?.Invoke(parseError);
            
            if (_enableLogging)
            {
                Debug.LogError($"[GameMessageDispatcher] 解析{messageType}消息失败: {errorMessage}");
            }
        }

        #endregion

        #region 自定义消息处理器注册

        /// <summary>
        /// 注册自定义消息处理器
        /// </summary>
        public void RegisterCustomHandler(string messageType, Func<WebSocketMessage, bool> handler)
        {
            _messageHandlers[messageType] = handler;
            
            if (_enableLogging)
            {
                Debug.Log($"[GameMessageDispatcher] 已注册自定义处理器: {messageType}");
            }
        }

        /// <summary>
        /// 注销消息处理器
        /// </summary>
        public void UnregisterHandler(string messageType)
        {
            _messageHandlers.Remove(messageType);
            
            if (_enableLogging)
            {
                Debug.Log($"[GameMessageDispatcher] 已注销处理器: {messageType}");
            }
        }

        /// <summary>
        /// 获取已注册的处理器列表
        /// </summary>
        public List<string> GetRegisteredHandlers()
        {
            return new List<string>(_messageHandlers.Keys);
        }

        #endregion
    }

    #region 错误处理类型

    /// <summary>
    /// 消息解析错误
    /// </summary>
    [System.Serializable]
    public class MessageParseError
    {
        public WebSocketMessage originalMessage;
        public string errorMessage;
        public DateTime timestamp;
        public string errorType;
        public string messageType;
    }

    #endregion
}