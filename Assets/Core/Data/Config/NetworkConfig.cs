// ================================================================================================
// 网络配置管理 - NetworkConfig.cs  
// 用途：管理HTTP和WebSocket的网络参数，对应JavaScript项目的axios和socket配置
// ================================================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaccaratGame.Config
{
    /// <summary>
    /// 网络配置类 - 管理HTTP和WebSocket连接参数
    /// 对应JavaScript项目中的httpClient和optimizedSocket配置
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "Baccarat/Network Config")]
    public class NetworkConfig : ScriptableObject
    {
        [Header("🌐 HTTP Configuration")]
        [Tooltip("HTTP请求超时时间（秒）")]
        [Range(5, 60)]
        public int httpTimeout = 30;

        [Tooltip("HTTP重试次数")]
        [Range(1, 5)]
        public int httpRetryCount = 3;

        [Tooltip("重试间隔（毫秒）")]
        [Range(500, 5000)]
        public int retryDelayMs = 1000;

        [Tooltip("是否启用HTTP请求缓存")]
        public bool enableHttpCache = false;

        [Tooltip("HTTP缓存时间（秒）")]
        [Range(30, 3600)]
        public int httpCacheTimeSeconds = 300;

        [Header("📡 WebSocket Configuration")]
        [Tooltip("WebSocket连接超时（秒）")]
        [Range(5, 30)]
        public int wsConnectionTimeout = 15;

        [Tooltip("心跳检测间隔（秒）")]
        [Range(10, 60)]
        public int heartbeatInterval = 30;

        [Tooltip("心跳超时时间（秒）")]
        [Range(5, 30)]
        public int heartbeatTimeout = 10;

        [Tooltip("自动重连最大次数")]
        [Range(1, 10)]
        public int maxReconnectAttempts = 5;

        [Tooltip("重连间隔（秒）")]
        [Range(1, 10)]
        public int reconnectDelaySeconds = 3;

        [Tooltip("WebSocket消息队列最大长度")]
        [Range(50, 500)]
        public int wsMessageQueueSize = 200;

        [Header("🔄 Auto Reconnect Settings")]
        [Tooltip("是否启用自动重连")]
        public bool enableAutoReconnect = true;

        [Tooltip("重连指数退避因子")]
        [Range(1.0f, 3.0f)]
        public float reconnectBackoffFactor = 1.5f;

        [Tooltip("最大重连间隔（秒）")]
        [Range(10, 300)]
        public int maxReconnectDelaySeconds = 60;

        [Tooltip("网络状态检测间隔（秒）")]
        [Range(5, 30)]
        public int networkStatusCheckInterval = 10;

        [Header("📊 Performance Settings")]
        [Tooltip("消息队列最大长度")]
        [Range(100, 1000)]
        public int messageQueueMaxSize = 500;

        [Tooltip("批量发送消息阈值")]
        [Range(1, 50)]
        public int batchSendThreshold = 10;

        [Tooltip("批量发送间隔（毫秒）")]
        [Range(50, 1000)]
        public int batchSendIntervalMs = 100;

        [Tooltip("缓存清理间隔（分钟）")]
        [Range(5, 60)]
        public int cacheCleanupIntervalMinutes = 15;

        [Header("🛡️ Security Settings")]
        [Tooltip("是否验证SSL证书")]
        public bool validateSSLCertificate = true;

        [Tooltip("允许的SSL协议版本")]
        public SSLProtocolType sslProtocol = SSLProtocolType.TLS12;

        [Tooltip("API请求签名密钥")]
        [SerializeField] private string apiSignatureKey = "";

        [Tooltip("是否启用请求签名")]
        public bool enableRequestSigning = false;

        [Header("🔧 Debug Settings")]
        [Tooltip("是否记录网络请求日志")]
        public bool logNetworkRequests = true;

        [Tooltip("是否记录WebSocket消息")]
        public bool logWebSocketMessages = true;

        [Tooltip("是否显示网络状态UI")]
        public bool showNetworkStatusUI = true;

        [Tooltip("网络延迟警告阈值（毫秒）")]
        [Range(100, 5000)]
        public int latencyWarningThresholdMs = 1000;

        [Header("🎮 Game Specific Settings")]
        [Tooltip("投注请求超时（秒）")]
        [Range(5, 30)]
        public int betRequestTimeout = 15;

        [Tooltip("游戏状态同步间隔（秒）")]
        [Range(1, 10)]
        public int gameStateSyncInterval = 3;

        [Tooltip("牌桌信息刷新间隔（秒）")]
        [Range(5, 60)]
        public int tableInfoRefreshInterval = 30;

        /// <summary>
        /// SSL协议类型枚举
        /// </summary>
        public enum SSLProtocolType
        {
            TLS10,
            TLS11,
            TLS12,
            TLS13
        }

        /// <summary>
        /// 获取HTTP请求头配置
        /// </summary>
        public Dictionary<string, string> GetDefaultHeaders()
        {
            return new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Accept", "application/json" },
                { "User-Agent", $"BaccaratUnity/{Application.version}" },
                { "X-Client-Platform", "Unity-WebGL" },
                { "X-Client-Version", Application.version },
                { "X-Request-Time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
            };
        }

        /// <summary>
        /// 获取WebSocket连接头配置
        /// </summary>
        public Dictionary<string, string> GetWebSocketHeaders()
        {
            return new Dictionary<string, string>
            {
                { "User-Agent", $"BaccaratUnity/{Application.version}" },
                { "X-Client-Platform", "Unity-WebGL" },
                { "X-Client-Version", Application.version },
                { "Origin", Application.absoluteURL }
            };
        }

        /// <summary>
        /// 获取重连延迟时间（支持指数退避）
        /// </summary>
        public float GetReconnectDelay(int attemptCount)
        {
            float delay = reconnectDelaySeconds * Mathf.Pow(reconnectBackoffFactor, attemptCount - 1);
            return Mathf.Min(delay, maxReconnectDelaySeconds);
        }

        /// <summary>
        /// 获取HTTP重试延迟时间
        /// </summary>
        public float GetHttpRetryDelay(int attemptCount)
        {
            return retryDelayMs * attemptCount * 0.001f; // 转换为秒
        }

        /// <summary>
        /// 验证配置参数
        /// </summary>
        public bool ValidateConfig()
        {
            bool isValid = true;

            if (httpTimeout <= 0 || wsConnectionTimeout <= 0)
            {
                Debug.LogError("[NetworkConfig] 超时配置无效");
                isValid = false;
            }

            if (heartbeatInterval <= 0 || messageQueueMaxSize <= 0)
            {
                Debug.LogError("[NetworkConfig] 性能配置无效");
                isValid = false;
            }

            if (maxReconnectAttempts <= 0 || reconnectDelaySeconds <= 0)
            {
                Debug.LogError("[NetworkConfig] 重连配置无效");
                isValid = false;
            }

            if (httpRetryCount <= 0 || retryDelayMs <= 0)
            {
                Debug.LogError("[NetworkConfig] HTTP重试配置无效");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// 获取API签名密钥
        /// </summary>
        public string GetApiSignatureKey() => apiSignatureKey;

        /// <summary>
        /// 设置API签名密钥
        /// </summary>
        public void SetApiSignatureKey(string key)
        {
            apiSignatureKey = key;
        }

        /// <summary>
        /// 获取网络质量配置
        /// </summary>
        public NetworkQualityConfig GetNetworkQualityConfig()
        {
            return new NetworkQualityConfig
            {
                LatencyWarningThreshold = latencyWarningThresholdMs,
                ConnectionTimeout = wsConnectionTimeout,
                HeartbeatInterval = heartbeatInterval,
                MaxReconnectAttempts = maxReconnectAttempts
            };
        }

        /// <summary>
        /// 获取游戏专用网络配置
        /// </summary>
        public GameNetworkConfig GetGameNetworkConfig()
        {
            return new GameNetworkConfig
            {
                BetRequestTimeout = betRequestTimeout,
                GameStateSyncInterval = gameStateSyncInterval,
                TableInfoRefreshInterval = tableInfoRefreshInterval,
                EnableAutoReconnect = enableAutoReconnect
            };
        }

        /// <summary>
        /// 应用WebGL优化设置
        /// </summary>
        public void ApplyWebGLOptimizations()
        {
            // 在WebGL环境下优化网络参数
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                // 减少超时时间以提高响应性
                httpTimeout = Mathf.Min(httpTimeout, 20);
                wsConnectionTimeout = Mathf.Min(wsConnectionTimeout, 10);
                
                // 减少重试次数以避免过度重试
                httpRetryCount = Mathf.Min(httpRetryCount, 2);
                maxReconnectAttempts = Mathf.Min(maxReconnectAttempts, 3);
                
                // 调整队列大小以节省内存
                messageQueueMaxSize = Mathf.Min(messageQueueMaxSize, 200);
                wsMessageQueueSize = Mathf.Min(wsMessageQueueSize, 100);

                Debug.Log("[NetworkConfig] WebGL网络优化设置已应用");
            }
        }

        /// <summary>
        /// 应用Safari浏览器优化
        /// </summary>
        public void ApplySafariOptimizations()
        {
            // Safari特殊优化
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                // Safari对WebSocket连接更严格，延长超时时间
                wsConnectionTimeout += 5;
                heartbeatInterval = Mathf.Max(heartbeatInterval, 30);
                
                // 减少并发连接数
                batchSendThreshold = Mathf.Min(batchSendThreshold, 5);
                
                Debug.Log("[NetworkConfig] Safari浏览器优化设置已应用");
            }
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void ResetToDefaults()
        {
            httpTimeout = 30;
            httpRetryCount = 3;
            retryDelayMs = 1000;
            wsConnectionTimeout = 15;
            heartbeatInterval = 30;
            maxReconnectAttempts = 5;
            reconnectDelaySeconds = 3;
            enableAutoReconnect = true;
            
            Debug.Log("[NetworkConfig] 已重置为默认配置");
        }

        /// <summary>
        /// 从JSON字符串加载配置
        /// </summary>
        public void LoadFromJson(string jsonData)
        {
            try
            {
                JsonUtility.FromJsonOverwrite(jsonData, this);
                Debug.Log("[NetworkConfig] 从JSON加载配置成功");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NetworkConfig] JSON加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出为JSON字符串
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// 获取当前网络配置摘要
        /// </summary>
        public string GetConfigSummary()
        {
            return $"HTTP超时:{httpTimeout}s, WS超时:{wsConnectionTimeout}s, 心跳:{heartbeatInterval}s, 最大重连:{maxReconnectAttempts}次";
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中的验证
        /// </summary>
        private void OnValidate()
        {
            // 确保超时时间合理
            if (heartbeatTimeout >= heartbeatInterval)
            {
                Debug.LogWarning("[NetworkConfig] 心跳超时时间应小于心跳间隔");
            }

            // 确保重连参数合理
            if (maxReconnectDelaySeconds < reconnectDelaySeconds)
            {
                Debug.LogWarning("[NetworkConfig] 最大重连间隔应大于等于基础重连间隔");
            }

            // 确保队列大小合理
            if (wsMessageQueueSize > messageQueueMaxSize)
            {
                Debug.LogWarning("[NetworkConfig] WebSocket消息队列大小不应超过总消息队列大小");
            }
        }
#endif
    }

    /// <summary>
    /// 网络质量配置数据结构
    /// </summary>
    [System.Serializable]
    public class NetworkQualityConfig
    {
        public int LatencyWarningThreshold;
        public int ConnectionTimeout;
        public int HeartbeatInterval;
        public int MaxReconnectAttempts;
    }

    /// <summary>
    /// 游戏专用网络配置数据结构
    /// </summary>
    [System.Serializable]
    public class GameNetworkConfig
    {
        public int BetRequestTimeout;
        public int GameStateSyncInterval;
        public int TableInfoRefreshInterval;
        public bool EnableAutoReconnect;
    }
}