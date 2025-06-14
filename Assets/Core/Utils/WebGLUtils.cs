// ================================================================================================
// WebGL工具类 - WebGLUtils.cs
// 用途：WebGL平台专用的工具类，包括URL参数解析、页面通信、浏览器检测等功能
// ================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BaccaratGame.Utils
{
    /// <summary>
    /// 浏览器类型枚举
    /// </summary>
    public enum BrowserType
    {
        Unknown,
        Chrome,
        Firefox,
        Safari,
        Edge,
        IE,
        Opera,
        WebKit,
        Mobile
    }

    /// <summary>
    /// URL参数数据结构
    /// </summary>
    [System.Serializable]
    public class UrlParameters
    {
        public string table_id = "";
        public string game_type = "";
        public string user_id = "";
        public string token = "";
        public string language = "en";
        public string theme = "default";
        public string debug = "false";
        public Dictionary<string, string> customParams = new Dictionary<string, string>();

        /// <summary>
        /// 验证必需参数
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(table_id) && 
                   !string.IsNullOrEmpty(game_type) && 
                   !string.IsNullOrEmpty(user_id) && 
                   !string.IsNullOrEmpty(token);
        }

        /// <summary>
        /// 获取所有参数
        /// </summary>
        public Dictionary<string, string> GetAllParameters()
        {
            var allParams = new Dictionary<string, string>
            {
                { "table_id", table_id },
                { "game_type", game_type },
                { "user_id", user_id },
                { "token", token },
                { "language", language },
                { "theme", theme },
                { "debug", debug }
            };

            foreach (var kvp in customParams)
            {
                allParams[kvp.Key] = kvp.Value;
            }

            return allParams;
        }
    }

    /// <summary>
    /// 浏览器信息
    /// </summary>
    [System.Serializable]
    public class BrowserInfo
    {
        public BrowserType browserType = BrowserType.Unknown;
        public string userAgent = "";
        public string version = "";
        public bool isMobile = false;
        public bool supportsWebGL = false;
        public bool supportsAudio = false;
        public bool supportsLocalStorage = false;
        public int screenWidth = 0;
        public int screenHeight = 0;
        public float devicePixelRatio = 1f;
    }

    /// <summary>
    /// WebGL工具类 - 提供WebGL平台专用的工具方法
    /// 对应JavaScript项目中的WebGL优化和浏览器交互功能
    /// </summary>
    public static class WebGLUtils
    {
        #region JavaScript Bridge Methods

        // WebGL JavaScript接口声明
        [DllImport("__Internal")]
        private static extern string GetCurrentUrl();

        [DllImport("__Internal")]
        private static extern string GetUserAgent();

        [DllImport("__Internal")]
        private static extern void PostMessageToParent(string message);

        [DllImport("__Internal")]
        private static extern string GetLocalStorageItem(string key);

        [DllImport("__Internal")]
        private static extern void SetLocalStorageItem(string key, string value);

        [DllImport("__Internal")]
        private static extern void RemoveLocalStorageItem(string key);

        [DllImport("__Internal")]
        private static extern int GetScreenWidth();

        [DllImport("__Internal")]
        private static extern int GetScreenHeight();

        [DllImport("__Internal")]
        private static extern float GetDevicePixelRatio();

        [DllImport("__Internal")]
        private static extern bool CheckWebGLSupport();

        [DllImport("__Internal")]
        private static extern bool CheckAudioSupport();

        [DllImport("__Internal")]
        private static extern bool CheckLocalStorageSupport();

        [DllImport("__Internal")]
        private static extern void ReloadPage();

        [DllImport("__Internal")]
        private static extern void OpenUrl(string url, string target);

        #endregion

        #region URL Parameter Parsing

        /// <summary>
        /// 解析URL参数
        /// </summary>
        public static UrlParameters ParseUrlParameters()
        {
            var urlParams = new UrlParameters();

            try
            {
                if (Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    Debug.LogWarning("[WebGLUtils] 当前不是WebGL平台，使用默认参数");
                    return GetDefaultUrlParameters();
                }

                string currentUrl = GetCurrentUrl();
                Debug.Log($"[WebGLUtils] 当前URL: {currentUrl}");

                if (string.IsNullOrEmpty(currentUrl))
                {
                    Debug.LogWarning("[WebGLUtils] 无法获取当前URL，使用默认参数");
                    return GetDefaultUrlParameters();
                }

                var parameters = ParseUrlQuery(currentUrl);
                
                // 解析标准参数
                urlParams.table_id = GetParameterValue(parameters, "table_id", "");
                urlParams.game_type = GetParameterValue(parameters, "game_type", "");
                urlParams.user_id = GetParameterValue(parameters, "user_id", "");
                urlParams.token = GetParameterValue(parameters, "token", "");
                urlParams.language = GetParameterValue(parameters, "language", "en");
                urlParams.theme = GetParameterValue(parameters, "theme", "default");
                urlParams.debug = GetParameterValue(parameters, "debug", "false");

                // 解析自定义参数
                var standardKeys = new HashSet<string> 
                { 
                    "table_id", "game_type", "user_id", "token", 
                    "language", "theme", "debug" 
                };

                foreach (var kvp in parameters)
                {
                    if (!standardKeys.Contains(kvp.Key))
                    {
                        urlParams.customParams[kvp.Key] = kvp.Value;
                    }
                }

                Debug.Log($"[WebGLUtils] URL参数解析完成: {urlParams.GetAllParameters().Count}个参数");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLUtils] URL参数解析失败: {ex.Message}");
                return GetDefaultUrlParameters();
            }

            return urlParams;
        }

        /// <summary>
        /// 解析URL查询字符串
        /// </summary>
        private static Dictionary<string, string> ParseUrlQuery(string url)
        {
            var parameters = new Dictionary<string, string>();

            try
            {
                int queryIndex = url.IndexOf('?');
                if (queryIndex == -1) return parameters;

                string queryString = url.Substring(queryIndex + 1);
                
                // 处理hash部分
                int hashIndex = queryString.IndexOf('#');
                if (hashIndex != -1)
                {
                    queryString = queryString.Substring(0, hashIndex);
                }

                string[] pairs = queryString.Split('&');
                
                foreach (string pair in pairs)
                {
                    if (string.IsNullOrEmpty(pair)) continue;

                    string[] keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        string key = Uri.UnescapeDataString(keyValue[0]);
                        string value = Uri.UnescapeDataString(keyValue[1]);
                        parameters[key] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLUtils] 查询字符串解析失败: {ex.Message}");
            }

            return parameters;
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        private static string GetParameterValue(Dictionary<string, string> parameters, string key, string defaultValue)
        {
            return parameters.TryGetValue(key, out string value) ? value : defaultValue;
        }

        /// <summary>
        /// 获取默认URL参数
        /// </summary>
        private static UrlParameters GetDefaultUrlParameters()
        {
            return new UrlParameters
            {
                table_id = "1",
                game_type = "3",
                user_id = "test_user",
                token = "test_token",
                language = "en",
                theme = "default",
                debug = "true"
            };
        }

        #endregion

        #region Browser Detection

        /// <summary>
        /// 检测浏览器信息
        /// </summary>
        public static BrowserInfo DetectBrowser()
        {
            var browserInfo = new BrowserInfo();

            try
            {
                if (Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    return GetDefaultBrowserInfo();
                }

                // 获取用户代理字符串
                browserInfo.userAgent = GetUserAgent();
                
                // 解析浏览器类型和版本
                ParseUserAgent(browserInfo);
                
                // 检测移动设备
                browserInfo.isMobile = DetectMobile(browserInfo.userAgent);
                
                // 获取屏幕信息
                browserInfo.screenWidth = GetScreenWidth();
                browserInfo.screenHeight = GetScreenHeight();
                browserInfo.devicePixelRatio = GetDevicePixelRatio();
                
                // 检测功能支持
                browserInfo.supportsWebGL = CheckWebGLSupport();
                browserInfo.supportsAudio = CheckAudioSupport();
                browserInfo.supportsLocalStorage = CheckLocalStorageSupport();

                Debug.Log($"[WebGLUtils] 浏览器检测完成: {browserInfo.browserType} (移动设备: {browserInfo.isMobile})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLUtils] 浏览器检测失败: {ex.Message}");
                return GetDefaultBrowserInfo();
            }

            return browserInfo;
        }

        /// <summary>
        /// 解析用户代理字符串
        /// </summary>
        private static void ParseUserAgent(BrowserInfo browserInfo)
        {
            string userAgent = browserInfo.userAgent.ToLower();

            if (userAgent.Contains("chrome") && !userAgent.Contains("edg"))
            {
                browserInfo.browserType = BrowserType.Chrome;
                browserInfo.version = ExtractVersion(userAgent, "chrome/");
            }
            else if (userAgent.Contains("safari") && !userAgent.Contains("chrome"))
            {
                browserInfo.browserType = BrowserType.Safari;
                browserInfo.version = ExtractVersion(userAgent, "version/");
            }
            else if (userAgent.Contains("firefox"))
            {
                browserInfo.browserType = BrowserType.Firefox;
                browserInfo.version = ExtractVersion(userAgent, "firefox/");
            }
            else if (userAgent.Contains("edg"))
            {
                browserInfo.browserType = BrowserType.Edge;
                browserInfo.version = ExtractVersion(userAgent, "edg/");
            }
            else if (userAgent.Contains("opera") || userAgent.Contains("opr"))
            {
                browserInfo.browserType = BrowserType.Opera;
                browserInfo.version = ExtractVersion(userAgent, userAgent.Contains("opera") ? "opera/" : "opr/");
            }
            else if (userAgent.Contains("trident") || userAgent.Contains("msie"))
            {
                browserInfo.browserType = BrowserType.IE;
                browserInfo.version = ExtractVersion(userAgent, "msie ");
            }
            else if (userAgent.Contains("webkit"))
            {
                browserInfo.browserType = BrowserType.WebKit;
                browserInfo.version = ExtractVersion(userAgent, "webkit/");
            }
        }

        /// <summary>
        /// 提取版本号
        /// </summary>
        private static string ExtractVersion(string userAgent, string versionPrefix)
        {
            try
            {
                int startIndex = userAgent.IndexOf(versionPrefix);
                if (startIndex == -1) return "Unknown";

                startIndex += versionPrefix.Length;
                int endIndex = userAgent.IndexOfAny(new char[] { ' ', ';', ')', '/' }, startIndex);
                
                if (endIndex == -1) endIndex = userAgent.Length;

                return userAgent.Substring(startIndex, endIndex - startIndex);
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// 检测移动设备
        /// </summary>
        private static bool DetectMobile(string userAgent)
        {
            string[] mobileKeywords = 
            {
                "mobile", "android", "iphone", "ipad", "ipod", 
                "windows phone", "blackberry", "webos"
            };

            return mobileKeywords.Any(keyword => userAgent.ToLower().Contains(keyword)) ||
                   Screen.width <= 768; // 简单的屏幕宽度检测
        }

        /// <summary>
        /// 获取默认浏览器信息
        /// </summary>
        private static BrowserInfo GetDefaultBrowserInfo()
        {
            return new BrowserInfo
            {
                browserType = BrowserType.Unknown,
                userAgent = "Unity Editor",
                version = "Editor",
                isMobile = false,
                supportsWebGL = true,
                supportsAudio = true,
                supportsLocalStorage = true,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                devicePixelRatio = 1f
            };
        }

        #endregion

        #region Page Communication

        /// <summary>
        /// 向父页面发送消息
        /// </summary>
        public static void SendMessageToParent(string message)
        {
            try
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    PostMessageToParent(message);
                    Debug.Log($"[WebGLUtils] 消息已发送到父页面: {message}");
                }
                else
                {
                    Debug.Log($"[WebGLUtils] [模拟] 发送消息到父页面: {message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLUtils] 发送消息到父页面失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送游戏状态消息
        /// </summary>
        public static void SendGameStatusMessage(string status, Dictionary<string, object> data = null)
        {
            var message = new Dictionary<string, object>
            {
                { "type", "game_status" },
                { "status", status },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            };

            if (data != null)
            {
                message["data"] = data;
            }

            string jsonMessage = JsonUtility.ToJson(message);
            SendMessageToParent(jsonMessage);
        }

        /// <summary>
        /// 发送游戏事件消息
        /// </summary>
        public static void SendGameEventMessage(string eventName, Dictionary<string, object> eventData = null)
        {
            var message = new Dictionary<string, object>
            {
                { "type", "game_event" },
                { "event", eventName },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            };

            if (eventData != null)
            {
                message["data"] = eventData;
            }

            string jsonMessage = JsonUtility.ToJson(message);
            SendMessageToParent(jsonMessage);
        }

        /// <summary>
        /// 发送错误消息
        /// </summary>
        public static void SendErrorMessage(string error, string details = "")
        {
            var message = new Dictionary<string, object>
            {
                { "type", "error" },
                { "error", error },
                { "details", details },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
            };

            string jsonMessage = JsonUtility.ToJson(message);
            SendMessageToParent(jsonMessage);
        }

        #endregion

        #region Local Storage

        /// <summary>
        /// 获取本地存储项
        /// </summary>
        public static string GetLocalStorage(string key, string defaultValue = "")
        {
            try
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    string value = GetLocalStorageItem(key);
                    return string.IsNullOrEmpty(value) ? defaultValue : value;
                }
                else
                {
                    // 编辑器中使用PlayerPrefs模拟
                    return PlayerPrefs.GetString($"WebGL_{key}", defaultValue);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLUtils] 获取本地存储失败 ({key}): {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 设置本地存储项
        /// </summary>
        public static void SetLocalStorage(string key, string value)
        {
            try
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    SetLocalStorageItem(key, value);
                }
                else
                {
                    // 编辑器中使用PlayerPrefs模拟
                    PlayerPrefs.SetString($"WebGL_{key}", value);
                }
                
                Debug.Log($"[WebGLUtils] 本地存储已设置: {key}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLUtils] 设置本地存储失败 ({key}): {ex.Message}");
            }
        }

        /// <summary>
        /// 移除本地存储项
        /// </summary>
        public static void RemoveLocalStorage(string key)
        {
            try
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    RemoveLocalStorageItem(key);
                }
                else
                {
                    // 编辑器中使用PlayerPrefs模拟
                    PlayerPrefs.DeleteKey($"WebGL_{key}");
                }
                
                Debug.Log($"[WebGLUtils] 本地存储已移除: {key}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLUtils] 移除本地存储失败 ({key}): {ex.Message}");
            }
        }

        /// <summary>
        /// 获取JSON格式的本地存储
        /// </summary>
        public static T GetLocalStorageJson<T>(string key, T defaultValue = default(T))
        {
            try
            {
                string json = GetLocalStorage(key);
                
                if (string.IsNullOrEmpty(json))
                {
                    return defaultValue;
                }
                
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLUtils] 获取JSON本地存储失败 ({key}): {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 设置JSON格式的本地存储
        /// </summary>
        public static void SetLocalStorageJson<T>(string key, T value)
        {
            try
            {
                string json = JsonUtility.ToJson(value);
                SetLocalStorage(key, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLUtils] 设置JSON本地存储失败 ({key}): {ex.Message}");
            }
        }

        #endregion

        #region Page Control

        /// <summary>
        /// 重新加载页面
        /// </summary>
        public static void ReloadCurrentPage()
        {
            try
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    ReloadPage();
                    Debug.Log("[WebGLUtils] 页面重新加载中...");
                }
                else
                {
                    Debug.Log("[WebGLUtils] [模拟] 页面重新加载");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLUtils] 页面重新加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 打开新URL
        /// </summary>
        public static void OpenNewUrl(string url, string target = "_blank")
        {
            try
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    OpenUrl(url, target);
                    Debug.Log($"[WebGLUtils] 打开URL: {url} (目标: {target})");
                }
                else
                {
                    Debug.Log($"[WebGLUtils] [模拟] 打开URL: {url}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLUtils] 打开URL失败: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 检查是否为WebGL平台
        /// </summary>
        public static bool IsWebGLPlatform()
        {
            return Application.platform == RuntimePlatform.WebGLPlayer;
        }

        /// <summary>
        /// 检查是否为开发环境
        /// </summary>
        public static bool IsDevelopmentEnvironment()
        {
            var urlParams = ParseUrlParameters();
            return urlParams.debug.ToLower() == "true" || Debug.isDebugBuild;
        }

        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        public static long GetCurrentTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// 生成唯一ID
        /// </summary>
        public static string GenerateUniqueId()
        {
            return $"{GetCurrentTimestamp()}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        /// <summary>
        /// URL编码
        /// </summary>
        public static string UrlEncode(string text)
        {
            return Uri.EscapeDataString(text);
        }

        /// <summary>
        /// URL解码
        /// </summary>
        public static string UrlDecode(string text)
        {
            return Uri.UnescapeDataString(text);
        }

        /// <summary>
        /// 检查网络连接状态
        /// </summary>
        public static bool IsNetworkAvailable()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        /// <summary>
        /// 获取网络类型
        /// </summary>
        public static string GetNetworkType()
        {
            switch (Application.internetReachability)
            {
                case NetworkReachability.ReachableViaCarrierDataNetwork:
                    return "cellular";
                case NetworkReachability.ReachableViaLocalAreaNetwork:
                    return "wifi";
                default:
                    return "none";
            }
        }

        #endregion

        #region Debug and Testing

        /// <summary>
        /// 输出WebGL环境信息
        /// </summary>
        public static void DebugWebGLInfo()
        {
            Debug.Log("=== WebGL环境信息 ===");
            Debug.Log($"平台: {Application.platform}");
            Debug.Log($"Unity版本: {Application.unityVersion}");
            Debug.Log($"是否为WebGL: {IsWebGLPlatform()}");
            Debug.Log($"网络状态: {Application.internetReachability}");
            Debug.Log($"屏幕尺寸: {Screen.width}x{Screen.height}");
            
            if (IsWebGLPlatform())
            {
                var urlParams = ParseUrlParameters();
                Debug.Log($"URL参数: {urlParams.GetAllParameters().Count}个");
                
                var browserInfo = DetectBrowser();
                Debug.Log($"浏览器: {browserInfo.browserType} {browserInfo.version}");
                Debug.Log($"移动设备: {browserInfo.isMobile}");
                Debug.Log($"功能支持: WebGL={browserInfo.supportsWebGL}, Audio={browserInfo.supportsAudio}, Storage={browserInfo.supportsLocalStorage}");
            }
        }

        /// <summary>
        /// 测试WebGL功能
        /// </summary>
        public static void TestWebGLFeatures()
        {
            Debug.Log("[WebGLUtils] 开始WebGL功能测试");
            
            // 测试URL参数解析
            var urlParams = ParseUrlParameters();
            Debug.Log($"URL参数测试: {urlParams.IsValid()} (参数数量: {urlParams.GetAllParameters().Count})");
            
            // 测试浏览器检测
            var browserInfo = DetectBrowser();
            Debug.Log($"浏览器检测测试: {browserInfo.browserType}");
            
            // 测试本地存储
            string testKey = "webgl_test";
            string testValue = "test_value_" + GetCurrentTimestamp();
            SetLocalStorage(testKey, testValue);
            string retrievedValue = GetLocalStorage(testKey);
            bool storageTest = retrievedValue == testValue;
            Debug.Log($"本地存储测试: {storageTest}");
            RemoveLocalStorage(testKey);
            
            // 测试页面通信
            SendGameStatusMessage("test", new Dictionary<string, object> { { "test", true } });
            Debug.Log("页面通信测试: 已发送测试消息");
            
            Debug.Log("[WebGLUtils] WebGL功能测试完成");
        }

        #endregion
    }
}