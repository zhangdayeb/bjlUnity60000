// ================================================================================================
// 环境配置管理 - EnvironmentConfig.cs
// 用途：管理开发/测试/生产环境的配置切换，对应JavaScript项目的环境变量
// ================================================================================================

using System;
using UnityEngine;

namespace BaccaratGame.Config
{
    /// <summary>
    /// 环境类型枚举
    /// </summary>
    public enum EnvironmentType 
    {
        Development,    // 开发环境（使用Mock数据）
        Testing,        // 测试环境（使用测试服务器）
        Production      // 生产环境（使用正式服务器）
    }

    /// <summary>
    /// 环境配置类 - 管理不同环境下的配置参数
    /// 对应JavaScript项目中的环境变量配置
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "EnvironmentConfig", menuName = "Baccarat/Environment Config")]
    public class EnvironmentConfig : ScriptableObject
    {
        [Header("🌍 Environment Settings")]
        [Tooltip("当前运行环境")]
        public EnvironmentType currentEnvironment = EnvironmentType.Development;

        [Header("🔧 Development Settings")]
        [Tooltip("是否启用调试日志")]
        public bool enableDebugLog = true;
        
        [Tooltip("是否使用Mock数据")]
        public bool useMockData = true;
        
        [Tooltip("是否启用性能监控")]
        public bool enablePerformanceMonitor = true;
        
        [Tooltip("是否显示调试UI")]
        public bool showDebugUI = true;

        [Header("🧪 Testing Settings")]
        [Tooltip("测试环境API地址")]
        public string testingApiBaseUrl = "https://test-api.yourgame.com";
        
        [Tooltip("测试环境WebSocket地址")]
        public string testingWebSocketUrl = "wss://test-ws.yourgame.com";
        
        [Tooltip("测试环境是否启用SSL")]
        public bool testingUseSSL = true;

        [Header("🚀 Production Settings")]
        [Tooltip("生产环境API地址")]
        public string productionApiBaseUrl = "https://api.yourgame.com";
        
        [Tooltip("生产环境WebSocket地址")]
        public string productionWebSocketUrl = "wss://ws.yourgame.com";
        
        [Tooltip("生产环境是否启用SSL")]
        public bool productionUseSSL = true;

        [Header("🛡️ Security Settings")]
        [Tooltip("API密钥（生产环境）")]
        [SerializeField] private string apiKey = "";
        
        [Tooltip("WebSocket认证Token")]
        [SerializeField] private string wsAuthToken = "";
        
        [Tooltip("是否启用Token刷新")]
        public bool enableTokenRefresh = true;
        
        [Tooltip("Token刷新间隔（分钟）")]
        [Range(5, 60)]
        public int tokenRefreshIntervalMinutes = 30;

        [Header("📊 Logging Settings")]
        [Tooltip("日志等级")]
        public LogLevel logLevel = LogLevel.Debug;
        
        [Tooltip("是否输出到控制台")]
        public bool logToConsole = true;
        
        [Tooltip("是否保存到文件")]
        public bool logToFile = false;
        
        [Tooltip("日志文件最大大小（MB）")]
        [Range(1, 100)]
        public int maxLogFileSizeMB = 10;

        [Header("🔄 Auto-reload Settings")]
        [Tooltip("是否启用配置热重载")]
        public bool enableHotReload = true;
        
        [Tooltip("配置检查间隔（秒）")]
        [Range(5, 60)]
        public int configCheckIntervalSeconds = 10;

        /// <summary>
        /// 日志等级枚举
        /// </summary>
        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }

        /// <summary>
        /// 获取当前环境的API基础URL
        /// </summary>
        public string GetApiBaseUrl()
        {
            return currentEnvironment switch
            {
                EnvironmentType.Development => "http://localhost:3000", // 本地开发
                EnvironmentType.Testing => testingApiBaseUrl,
                EnvironmentType.Production => productionApiBaseUrl,
                _ => testingApiBaseUrl
            };
        }

        /// <summary>
        /// 获取当前环境的WebSocket URL
        /// </summary>
        public string GetWebSocketUrl()
        {
            return currentEnvironment switch
            {
                EnvironmentType.Development => "ws://localhost:3001", // 本地开发
                EnvironmentType.Testing => testingWebSocketUrl,
                EnvironmentType.Production => productionWebSocketUrl,
                _ => testingWebSocketUrl
            };
        }

        /// <summary>
        /// 是否为开发环境
        /// </summary>
        public bool IsDevelopment => currentEnvironment == EnvironmentType.Development;

        /// <summary>
        /// 是否为测试环境
        /// </summary>
        public bool IsTesting => currentEnvironment == EnvironmentType.Testing;

        /// <summary>
        /// 是否为生产环境
        /// </summary>
        public bool IsProduction => currentEnvironment == EnvironmentType.Production;

        /// <summary>
        /// 获取API密钥
        /// </summary>
        public string GetApiKey() => apiKey;

        /// <summary>
        /// 获取WebSocket认证Token
        /// </summary>
        public string GetWebSocketAuthToken() => wsAuthToken;

        /// <summary>
        /// 设置API密钥（仅开发环境可用）
        /// </summary>
        public void SetApiKey(string newApiKey)
        {
            if (IsDevelopment)
            {
                apiKey = newApiKey;
                Debug.Log($"[EnvironmentConfig] API密钥已更新");
            }
            else
            {
                Debug.LogWarning("[EnvironmentConfig] 仅开发环境可以动态设置API密钥");
            }
        }

        /// <summary>
        /// 运行时切换环境（用于测试）
        /// </summary>
        public void SwitchEnvironment(EnvironmentType newEnvironment)
        {
            var oldEnvironment = currentEnvironment;
            currentEnvironment = newEnvironment;
            
            Debug.Log($"[EnvironmentConfig] 环境已从 {oldEnvironment} 切换到 {newEnvironment}");
            
            // 触发环境切换事件
            OnEnvironmentChanged?.Invoke(oldEnvironment, newEnvironment);
        }

        /// <summary>
        /// 环境切换事件
        /// </summary>
        public System.Action<EnvironmentType, EnvironmentType> OnEnvironmentChanged;

        /// <summary>
        /// 获取当前环境的完整信息
        /// </summary>
        public EnvironmentInfo GetCurrentEnvironmentInfo()
        {
            return new EnvironmentInfo
            {
                Environment = currentEnvironment,
                ApiBaseUrl = GetApiBaseUrl(),
                WebSocketUrl = GetWebSocketUrl(),
                UseMockData = useMockData,
                EnableDebugLog = enableDebugLog,
                EnablePerformanceMonitor = enablePerformanceMonitor
            };
        }

        /// <summary>
        /// 验证当前配置
        /// </summary>
        public bool ValidateConfiguration()
        {
            bool isValid = true;

            // 检查URL格式
            if (!IsValidUrl(GetApiBaseUrl()))
            {
                Debug.LogError($"[EnvironmentConfig] 无效的API URL: {GetApiBaseUrl()}");
                isValid = false;
            }

            if (!IsValidUrl(GetWebSocketUrl()))
            {
                Debug.LogError($"[EnvironmentConfig] 无效的WebSocket URL: {GetWebSocketUrl()}");
                isValid = false;
            }

            // 生产环境必须有API密钥
            if (IsProduction && string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[EnvironmentConfig] 生产环境必须设置API密钥");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// 检查URL格式是否有效
        /// </summary>
        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void ResetToDefaults()
        {
            currentEnvironment = EnvironmentType.Development;
            enableDebugLog = true;
            useMockData = true;
            enablePerformanceMonitor = true;
            showDebugUI = true;
            
            Debug.Log("[EnvironmentConfig] 已重置为默认配置");
        }

        /// <summary>
        /// 从JSON字符串加载配置
        /// </summary>
        public void LoadFromJson(string jsonData)
        {
            try
            {
                JsonUtility.FromJsonOverwrite(jsonData, this);
                Debug.Log("[EnvironmentConfig] 从JSON加载配置成功");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EnvironmentConfig] JSON加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出为JSON字符串
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中的验证
        /// </summary>
        private void OnValidate()
        {
            // 确保URL格式正确
            if (!string.IsNullOrEmpty(testingApiBaseUrl) && !IsValidUrl(testingApiBaseUrl))
            {
                Debug.LogWarning("[EnvironmentConfig] 测试环境API URL格式可能不正确");
            }

            if (!string.IsNullOrEmpty(productionApiBaseUrl) && !IsValidUrl(productionApiBaseUrl))
            {
                Debug.LogWarning("[EnvironmentConfig] 生产环境API URL格式可能不正确");
            }
        }
#endif
    }

    /// <summary>
    /// 环境信息数据结构
    /// </summary>
    [System.Serializable]
    public class EnvironmentInfo
    {
        public EnvironmentType Environment;
        public string ApiBaseUrl;
        public string WebSocketUrl;
        public bool UseMockData;
        public bool EnableDebugLog;
        public bool EnablePerformanceMonitor;
    }
}