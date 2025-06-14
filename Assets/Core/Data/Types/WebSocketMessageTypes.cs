// Assets/_Core/Data/Types/WebSocketMessageTypes.cs
// WebSocket消息类型 - 对应JavaScript项目的WebSocket消息格式

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Data.Types
{
    /// <summary>
    /// WebSocket连接状态枚举 - 对应JavaScript项目的CONNECTION_STATUS
    /// </summary>
    public enum WSConnectionStatus
    {
        Disconnected = 0,   // 已断开
        Connecting = 1,     // 连接中
        Connected = 2,      // 已连接
        Reconnecting = 3,   // 重连中
        Error = 4           // 连接错误
    }

    /// <summary>
    /// WebSocket消息类型枚举
    /// </summary>
    public enum WSMessageType
    {
        Heartbeat = 0,      // 心跳消息 (ping/pong)
        TableInfo = 1,      // 桌台信息
        GameResult = 2,     // 开牌结果
        WinningData = 3,    // 中奖信息
        BetUpdate = 4,      // 投注更新
        BalanceUpdate = 5,  // 余额更新
        GameStatus = 6,     // 游戏状态
        Error = 7           // 错误消息
    }

    /// <summary>
    /// WebSocket基础消息结构 - 对应JavaScript项目的消息格式
    /// </summary>
    [System.Serializable]
    public class WSBaseMessage
    {
        [Header("基础信息")]
        [Tooltip("响应码")]
        public int code = 0;
        
        [Tooltip("消息内容")]
        public string msg = "";
        
        [Tooltip("消息类型")]
        public WSMessageType messageType = WSMessageType.Heartbeat;
        
        [Tooltip("时间戳")]
        public long timestamp = 0;

        /// <summary>
        /// 是否成功
        /// </summary>
        /// <returns>是否成功</returns>
        public bool IsSuccess()
        {
            return code == 200 || code == 1;
        }

        /// <summary>
        /// 设置时间戳为当前时间
        /// </summary>
        public void SetCurrentTimestamp()
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// 心跳消息 - 对应JavaScript项目的ping/pong机制
    /// </summary>
    [System.Serializable]
    public class WSHeartbeatMessage : WSBaseMessage
    {
        [Header("心跳信息")]
        [Tooltip("是否为ping消息")]
        public bool isPing = false;
        
        [Tooltip("是否为pong消息")]
        public bool isPong = false;
        
        [Tooltip("心跳间隔")]
        public int interval = 30000;

        /// <summary>
        /// 创建Ping消息
        /// </summary>
        /// <returns>Ping消息</returns>
        public static WSHeartbeatMessage CreatePing()
        {
            var message = new WSHeartbeatMessage
            {
                isPing = true,
                isPong = false,
                messageType = WSMessageType.Heartbeat
            };
            message.SetCurrentTimestamp();
            return message;
        }

        /// <summary>
        /// 创建Pong消息
        /// </summary>
        /// <returns>Pong消息</returns>
        public static WSHeartbeatMessage CreatePong()
        {
            var message = new WSHeartbeatMessage
            {
                isPing = false,
                isPong = true,
                messageType = WSMessageType.Heartbeat
            };
            message.SetCurrentTimestamp();
            return message;
        }
    }

    /// <summary>
    /// 桌台信息消息 - 对应JavaScript项目的table_run_info推送
    /// </summary>
    [System.Serializable]
    public class WSTableInfoMessage : WSBaseMessage
    {
        [Header("桌台数据")]
        [Tooltip("桌台运行信息")]
        public WSTableInfoData data;

        [System.Serializable]
        public class WSTableInfoData
        {
            [Tooltip("桌台运行信息")]
            public TableRunInfo table_run_info;
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WSTableInfoMessage()
        {
            messageType = WSMessageType.TableInfo;
        }
    }

    /// <summary>
    /// 游戏结果消息 - 对应JavaScript项目的开牌结果推送
    /// </summary>
    [System.Serializable]
    public class WSGameResultMessage : WSBaseMessage
    {
        [Header("开牌数据")]
        [Tooltip("开牌结果数据")]
        public WSGameResultData data;

        [System.Serializable]
        public class WSGameResultData
        {
            [Header("基础信息")]
            [Tooltip("游戏局号")]
            public string bureau_number = "";

            [Header("结果信息")]
            [Tooltip("开牌结果详情")]
            public WSResultInfo result_info;
        }

        [System.Serializable]
        public class WSResultInfo
        {
            [Header("中奖信息")]
            [Tooltip("中奖金额")]
            public float money = 0f;

            [Header("闪烁效果")]
            [Tooltip("闪烁区域ID数组")]
            public List<int> pai_flash = new List<int>();

            [Header("牌面信息")]
            [Tooltip("牌面详情")]
            public List<WSCardInfo> pai_info = new List<WSCardInfo>();

            [Header("开奖结果")]
            [Tooltip("庄家点数")]
            public int banker_points = 0;
            
            [Tooltip("闲家点数")]
            public int player_points = 0;
            
            [Tooltip("获胜方 (1=庄, 2=闲, 3=和)")]
            public int winner = 0;
            
            [Tooltip("是否庄对")]
            public bool banker_pair = false;
            
            [Tooltip("是否闲对")]
            public bool player_pair = false;
        }

        [System.Serializable]
        public class WSCardInfo
        {
            [Tooltip("花色")]
            public int suit = 1;
            
            [Tooltip("点数")]
            public int rank = 1;
            
            [Tooltip("牌面显示")]
            public string display = "";

            /// <summary>
            /// 转换为百家乐牌面
            /// </summary>
            /// <returns>百家乐牌面</returns>
            public BaccaratCard ToBaccaratCard()
            {
                return new BaccaratCard(suit, rank)
                {
                    display = display
                };
            }
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WSGameResultMessage()
        {
            messageType = WSMessageType.GameResult;
        }

        /// <summary>
        /// 获取中奖金额
        /// </summary>
        /// <returns>中奖金额</returns>
        public float GetWinningAmount()
        {
            return data?.result_info?.money ?? 0f;
        }

        /// <summary>
        /// 获取闪烁区域
        /// </summary>
        /// <returns>闪烁区域ID列表</returns>
        public List<int> GetFlashAreas()
        {
            return data?.result_info?.pai_flash ?? new List<int>();
        }

        /// <summary>
        /// 获取游戏局号
        /// </summary>
        /// <returns>游戏局号</returns>
        public string GetBureauNumber()
        {
            return data?.bureau_number ?? "";
        }
    }

    /// <summary>
    /// 中奖数据消息 - 对应JavaScript项目的中奖信息推送
    /// </summary>
    [System.Serializable]
    public class WSWinningMessage : WSBaseMessage
    {
        [Header("中奖数据")]
        [Tooltip("中奖数据")]
        public WSWinningData data;

        [System.Serializable]
        public class WSWinningData
        {
            [Tooltip("中奖金额")]
            public float win_amount = 0f;
            
            [Tooltip("游戏局号")]
            public string game_number = "";
            
            [Tooltip("中奖投注类型")]
            public List<int> winning_bet_types = new List<int>();
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WSWinningMessage()
        {
            messageType = WSMessageType.WinningData;
        }

        /// <summary>
        /// 获取中奖金额
        /// </summary>
        /// <returns>中奖金额</returns>
        public float GetWinningAmount()
        {
            return data?.win_amount ?? 0f;
        }
    }

    /// <summary>
    /// 投注更新消息 - 对应JavaScript项目的用户下注推送
    /// </summary>
    [System.Serializable]
    public class WSBetUpdateMessage : WSBaseMessage
    {
        [Header("投注数据")]
        [Tooltip("投注更新数据")]
        public WSBetUpdateData data;

        [System.Serializable]
        public class WSBetUpdateData
        {
            [Tooltip("余额")]
            public float money_balance = 0f;
            
            [Tooltip("花费金额")]
            public float money_spend = 0f;
            
            [Tooltip("投注详情")]
            public List<WSBetDetail> bet_details = new List<WSBetDetail>();
        }

        [System.Serializable]
        public class WSBetDetail
        {
            [Tooltip("投注金额")]
            public float amount = 0f;
            
            [Tooltip("投注类型ID")]
            public int rate_id = 0;
            
            [Tooltip("投注类型名称")]
            public string bet_type_name = "";
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WSBetUpdateMessage()
        {
            messageType = WSMessageType.BetUpdate;
        }
    }

    /// <summary>
    /// 余额更新消息 - 对应JavaScript项目的余额变化推送
    /// </summary>
    [System.Serializable]
    public class WSBalanceUpdateMessage : WSBaseMessage
    {
        [Header("余额数据")]
        [Tooltip("余额更新数据")]
        public WSBalanceData data;

        [System.Serializable]
        public class WSBalanceData
        {
            [Tooltip("当前余额")]
            public float balance = 0f;
            
            [Tooltip("变化金额")]
            public float change_amount = 0f;
            
            [Tooltip("变化类型 (1=增加, -1=减少)")]
            public int change_type = 0;
            
            [Tooltip("变化原因")]
            public string reason = "";
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WSBalanceUpdateMessage()
        {
            messageType = WSMessageType.BalanceUpdate;
        }

        /// <summary>
        /// 获取新余额
        /// </summary>
        /// <returns>新余额</returns>
        public float GetNewBalance()
        {
            return data?.balance ?? 0f;
        }
    }

    /// <summary>
    /// 游戏状态消息 - 对应JavaScript项目的游戏状态推送
    /// </summary>
    [System.Serializable]
    public class WSGameStatusMessage : WSBaseMessage
    {
        [Header("状态数据")]
        [Tooltip("游戏状态数据")]
        public WSGameStatusData data;

        [System.Serializable]
        public class WSGameStatusData
        {
            [Tooltip("游戏状态")]
            public string status = "";
            
            [Tooltip("状态描述")]
            public string message = "";
            
            [Tooltip("状态码")]
            public int status_code = 0;
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WSGameStatusMessage()
        {
            messageType = WSMessageType.GameStatus;
        }

        /// <summary>
        /// 是否为维护状态
        /// </summary>
        /// <returns>是否为维护状态</returns>
        public bool IsMaintenance()
        {
            return data?.status == "maintenance" || code == 207;
        }
    }

    /// <summary>
    /// 错误消息 - 对应JavaScript项目的错误处理
    /// </summary>
    [System.Serializable]
    public class WSErrorMessage : WSBaseMessage
    {
        [Header("错误数据")]
        [Tooltip("错误数据")]
        public WSErrorData data;

        [System.Serializable]
        public class WSErrorData
        {
            [Tooltip("错误码")]
            public string error_code = "";
            
            [Tooltip("错误描述")]
            public string error_message = "";
            
            [Tooltip("错误详情")]
            public string error_details = "";
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WSErrorMessage()
        {
            messageType = WSMessageType.Error;
        }

        /// <summary>
        /// 获取错误信息
        /// </summary>
        /// <returns>错误信息</returns>
        public string GetErrorMessage()
        {
            return data?.error_message ?? msg ?? "未知错误";
        }
    }

    /// <summary>
    /// WebSocket连接信息 - 对应JavaScript项目的连接管理
    /// </summary>
    [System.Serializable]
    public class WSConnectionInfo
    {
        [Header("连接状态")]
        [Tooltip("连接状态")]
        public WSConnectionStatus status = WSConnectionStatus.Disconnected;
        
        [Tooltip("连接URL")]
        public string url = "";
        
        [Tooltip("重连次数")]
        public int reconnectAttempts = 0;
        
        [Tooltip("最大重连次数")]
        public int maxReconnectAttempts = 15;

        [Header("连接统计")]
        [Tooltip("连接时间")]
        public float connectionTime = 0f;
        
        [Tooltip("最后心跳时间")]
        public float lastHeartbeatTime = 0f;
        
        [Tooltip("消息发送数量")]
        public int messagesSent = 0;
        
        [Tooltip("消息接收数量")]
        public int messagesReceived = 0;

        [Header("错误信息")]
        [Tooltip("最后错误信息")]
        public string lastError = "";
        
        [Tooltip("错误次数")]
        public int errorCount = 0;

        /// <summary>
        /// 是否已连接
        /// </summary>
        /// <returns>是否已连接</returns>
        public bool IsConnected()
        {
            return status == WSConnectionStatus.Connected;
        }

        /// <summary>
        /// 是否正在连接
        /// </summary>
        /// <returns>是否正在连接</returns>
        public bool IsConnecting()
        {
            return status == WSConnectionStatus.Connecting || status == WSConnectionStatus.Reconnecting;
        }

        /// <summary>
        /// 可否重连
        /// </summary>
        /// <returns>可否重连</returns>
        public bool CanReconnect()
        {
            return reconnectAttempts < maxReconnectAttempts;
        }

        /// <summary>
        /// 增加重连次数
        /// </summary>
        public void IncrementReconnectAttempts()
        {
            reconnectAttempts++;
        }

        /// <summary>
        /// 重置重连次数
        /// </summary>
        public void ResetReconnectAttempts()
        {
            reconnectAttempts = 0;
        }

        /// <summary>
        /// 更新连接状态
        /// </summary>
        /// <param name="newStatus">新状态</param>
        public void UpdateStatus(WSConnectionStatus newStatus)
        {
            status = newStatus;
            
            if (newStatus == WSConnectionStatus.Connected)
            {
                connectionTime = Time.time;
                ResetReconnectAttempts();
            }
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="error">错误信息</param>
        public void RecordError(string error)
        {
            lastError = error;
            errorCount++;
            status = WSConnectionStatus.Error;
        }

        /// <summary>
        /// 更新心跳时间
        /// </summary>
        public void UpdateHeartbeat()
        {
            lastHeartbeatTime = Time.time;
        }

        /// <summary>
        /// 增加发送消息计数
        /// </summary>
        public void IncrementMessagesSent()
        {
            messagesSent++;
        }

        /// <summary>
        /// 增加接收消息计数
        /// </summary>
        public void IncrementMessagesReceived()
        {
            messagesReceived++;
        }

        /// <summary>
        /// 获取连接状态描述
        /// </summary>
        /// <returns>状态描述</returns>
        public string GetStatusDescription()
        {
            switch (status)
            {
                case WSConnectionStatus.Connected:
                    return "已连接";
                case WSConnectionStatus.Connecting:
                    return "连接中...";
                case WSConnectionStatus.Reconnecting:
                    return $"重连中...({reconnectAttempts}/{maxReconnectAttempts})";
                case WSConnectionStatus.Disconnected:
                    return "已断开";
                case WSConnectionStatus.Error:
                    return "连接错误";
                default:
                    return "未知状态";
            }
        }

        /// <summary>
        /// 获取连接健康状态
        /// </summary>
        /// <returns>健康状态信息</returns>
        public WSConnectionHealth GetHealthStatus()
        {
            var health = new WSConnectionHealth
            {
                IsHealthy = IsConnected(),
                Status = status,
                Issues = new List<string>()
            };

            if (!IsConnected())
            {
                health.Issues.Add($"连接状态: {GetStatusDescription()}");
            }

            if (!string.IsNullOrEmpty(lastError))
            {
                health.Issues.Add($"最后错误: {lastError}");
            }

            if (errorCount > 10)
            {
                health.Issues.Add($"错误次数过多: {errorCount}");
            }

            float timeSinceLastHeartbeat = Time.time - lastHeartbeatTime;
            if (IsConnected() && timeSinceLastHeartbeat > 60f)
            {
                health.Issues.Add($"心跳超时: {timeSinceLastHeartbeat:F1}秒");
                health.IsHealthy = false;
            }

            return health;
        }
    }

    /// <summary>
    /// WebSocket连接健康状态
    /// </summary>
    [System.Serializable]
    public class WSConnectionHealth
    {
        [Tooltip("是否健康")]
        public bool IsHealthy = true;
        
        [Tooltip("连接状态")]
        public WSConnectionStatus Status = WSConnectionStatus.Disconnected;
        
        [Tooltip("问题列表")]
        public List<string> Issues = new List<string>();

        /// <summary>
        /// 获取健康状态描述
        /// </summary>
        /// <returns>健康状态描述</returns>
        public string GetHealthDescription()
        {
            if (IsHealthy)
            {
                return "连接健康";
            }
            else
            {
                return $"连接异常: {string.Join("; ", Issues)}";
            }
        }
    }

    /// <summary>
    /// WebSocket消息解析器 - 对应JavaScript项目的消息解析
    /// </summary>
    public static class WSMessageParser
    {
        /// <summary>
        /// 解析原始消息
        /// </summary>
        /// <param name="rawMessage">原始消息</param>
        /// <returns>解析结果</returns>
        public static WSMessageParseResult ParseMessage(string rawMessage)
        {
            var result = new WSMessageParseResult();

            if (string.IsNullOrEmpty(rawMessage))
            {
                result.Success = false;
                result.Error = "消息为空";
                return result;
            }

            // 处理心跳消息
            if (rawMessage == "ping" || rawMessage == "pong")
            {
                result.Success = true;
                result.MessageType = WSMessageType.Heartbeat;
                result.ParsedMessage = rawMessage == "ping" ? 
                    WSHeartbeatMessage.CreatePing() : 
                    WSHeartbeatMessage.CreatePong();
                return result;
            }

            try
            {
                // 尝试解析JSON消息
                var baseMessage = JsonUtility.FromJson<WSBaseMessage>(rawMessage);
                
                if (baseMessage == null)
                {
                    result.Success = false;
                    result.Error = "无法解析基础消息";
                    return result;
                }

                result.Success = true;
                result.MessageType = DetermineMessageType(baseMessage, rawMessage);
                result.ParsedMessage = ParseSpecificMessage(result.MessageType, rawMessage);

                return result;
            }
            catch (Exception e)
            {
                result.Success = false;
                result.Error = $"JSON解析失败: {e.Message}";
                return result;
            }
        }

        /// <summary>
        /// 确定消息类型
        /// </summary>
        /// <param name="baseMessage">基础消息</param>
        /// <param name="rawMessage">原始消息</param>
        /// <returns>消息类型</returns>
        private static WSMessageType DetermineMessageType(WSBaseMessage baseMessage, string rawMessage)
        {
            // 根据消息内容和结构判断类型
            if (rawMessage.Contains("table_run_info"))
            {
                return WSMessageType.TableInfo;
            }
            else if (rawMessage.Contains("result_info"))
            {
                return WSMessageType.GameResult;
            }
            else if (rawMessage.Contains("win_amount"))
            {
                return WSMessageType.WinningData;
            }
            else if (rawMessage.Contains("money_balance"))
            {
                return WSMessageType.BalanceUpdate;
            }
            else if (baseMessage.code == 207) // 洗牌状态
            {
                return WSMessageType.GameStatus;
            }
            else if (baseMessage.code == 209) // 用户下注
            {
                return WSMessageType.BetUpdate;
            }
            else if (!baseMessage.IsSuccess())
            {
                return WSMessageType.Error;
            }

            return WSMessageType.Heartbeat;
        }

        /// <summary>
        /// 解析特定类型的消息
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="rawMessage">原始消息</param>
        /// <returns>解析后的消息对象</returns>
        private static object ParseSpecificMessage(WSMessageType messageType, string rawMessage)
        {
            try
            {
                switch (messageType)
                {
                    case WSMessageType.TableInfo:
                        return JsonUtility.FromJson<WSTableInfoMessage>(rawMessage);
                    case WSMessageType.GameResult:
                        return JsonUtility.FromJson<WSGameResultMessage>(rawMessage);
                    case WSMessageType.WinningData:
                        return JsonUtility.FromJson<WSWinningMessage>(rawMessage);
                    case WSMessageType.BetUpdate:
                        return JsonUtility.FromJson<WSBetUpdateMessage>(rawMessage);
                    case WSMessageType.BalanceUpdate:
                        return JsonUtility.FromJson<WSBalanceUpdateMessage>(rawMessage);
                    case WSMessageType.GameStatus:
                        return JsonUtility.FromJson<WSGameStatusMessage>(rawMessage);
                    case WSMessageType.Error:
                        return JsonUtility.FromJson<WSErrorMessage>(rawMessage);
                    default:
                        return JsonUtility.FromJson<WSBaseMessage>(rawMessage);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"解析{messageType}消息失败: {e.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// WebSocket消息解析结果
    /// </summary>
    [System.Serializable]
    public class WSMessageParseResult
    {
        [Tooltip("是否解析成功")]
        public bool Success = false;
        
        [Tooltip("消息类型")]
        public WSMessageType MessageType = WSMessageType.Heartbeat;
        
        [Tooltip("错误信息")]
        public string Error = "";
        
        [Tooltip("解析后的消息对象")]
        [System.NonSerialized]
        public object ParsedMessage = null;

        /// <summary>
        /// 获取指定类型的消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <returns>类型化的消息对象</returns>
        public T GetMessage<T>() where T : class
        {
            return ParsedMessage as T;
        }
    }
}