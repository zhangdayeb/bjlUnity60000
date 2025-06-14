// Assets/_Core/Network/Http/HttpClient.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.Network.Http
{
    /// <summary>
    /// 通用HTTP客户端 - 对应JavaScript中的httpClient.ts
    /// 提供统一的HTTP请求功能，支持请求/响应拦截器、错误处理、重试机制
    /// </summary>
    public class HttpClient : MonoBehaviour
    {
        #region 配置和状态

        [Header("HTTP客户端配置")]
        [SerializeField] private string _baseUrl = "https://api.yourgame.com";
        [SerializeField] private int _timeout = 10; // 超时时间（秒）
        [SerializeField] private int _maxRetries = 3; // 最大重试次数
        [SerializeField] private bool _enableLogging = true; // 启用日志
        
        // 认证相关
        private string _authToken;
        private Dictionary<string, string> _defaultHeaders;
        
        // 拦截器
        private List<System.Func<UnityWebRequest, UnityWebRequest>> _requestInterceptors;
        private List<System.Func<HttpResponse, HttpResponse>> _responseInterceptors;
        
        // 错误处理
        private System.Action<HttpError> _globalErrorHandler;
        private System.Action _authFailedHandler;
        
        // 统计信息
        private HttpStatistics _statistics;

        #endregion

        #region 初始化

        private void Awake()
        {
            InitializeClient();
        }

        private void InitializeClient()
        {
            _defaultHeaders = new Dictionary<string, string>
            {
                {"Content-Type", "application/json"},
                {"Accept", "application/json"},
                {"User-Agent", $"Unity/{Application.unityVersion} ({SystemInfo.operatingSystem})"}
            };
            
            _requestInterceptors = new List<System.Func<UnityWebRequest, UnityWebRequest>>();
            _responseInterceptors = new List<System.Func<HttpResponse, HttpResponse>>();
            
            _statistics = new HttpStatistics();
            
            // 添加默认响应拦截器
            AddResponseInterceptor(DefaultResponseInterceptor);
            
            if (_enableLogging)
            {
                Debug.Log("[HttpClient] HTTP客户端已初始化");
            }
        }

        #endregion

        #region 公共配置方法

        /// <summary>
        /// 设置基础URL
        /// </summary>
        public void SetBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            Debug.Log($"[HttpClient] 基础URL已设置: {_baseUrl}");
        }

        /// <summary>
        /// 设置认证Token
        /// </summary>
        public void SetAuthToken(string token)
        {
            _authToken = token;
            if (!string.IsNullOrEmpty(token))
            {
                _defaultHeaders["Authorization"] = $"Bearer {token}";
            }
            else
            {
                _defaultHeaders.Remove("Authorization");
            }
            
            if (_enableLogging)
            {
                Debug.Log($"[HttpClient] 认证Token已{'设置' : '清除'}");
            }
        }

        /// <summary>
        /// 设置超时时间
        /// </summary>
        public void SetTimeout(int timeoutSeconds)
        {
            _timeout = timeoutSeconds;
            Debug.Log($"[HttpClient] 超时时间已设置: {timeoutSeconds}秒");
        }

        /// <summary>
        /// 设置全局错误处理器
        /// </summary>
        public void SetGlobalErrorHandler(System.Action<HttpError> errorHandler)
        {
            _globalErrorHandler = errorHandler;
        }

        /// <summary>
        /// 设置认证失败处理器
        /// </summary>
        public void SetAuthFailedHandler(System.Action authFailedHandler)
        {
            _authFailedHandler = authFailedHandler;
        }

        #endregion

        #region 拦截器管理

        /// <summary>
        /// 添加请求拦截器
        /// </summary>
        public void AddRequestInterceptor(System.Func<UnityWebRequest, UnityWebRequest> interceptor)
        {
            _requestInterceptors.Add(interceptor);
        }

        /// <summary>
        /// 添加响应拦截器
        /// </summary>
        public void AddResponseInterceptor(System.Func<HttpResponse, HttpResponse> interceptor)
        {
            _responseInterceptors.Add(interceptor);
        }

        /// <summary>
        /// 清除所有拦截器
        /// </summary>
        public void ClearInterceptors()
        {
            _requestInterceptors.Clear();
            _responseInterceptors.Clear();
            
            // 重新添加默认响应拦截器
            AddResponseInterceptor(DefaultResponseInterceptor);
        }

        #endregion

        #region HTTP方法

        /// <summary>
        /// GET请求
        /// </summary>
        public async Task<T> GetAsync<T>(string endpoint, object queryParams = null) where T : class
        {
            var response = await SendRequestAsync("GET", endpoint, null, queryParams);
            return ParseResponse<T>(response);
        }

        /// <summary>
        /// POST请求
        /// </summary>
        public async Task<T> PostAsync<T>(string endpoint, object data = null, object queryParams = null) where T : class
        {
            var response = await SendRequestAsync("POST", endpoint, data, queryParams);
            return ParseResponse<T>(response);
        }

        /// <summary>
        /// PUT请求
        /// </summary>
        public async Task<T> PutAsync<T>(string endpoint, object data = null, object queryParams = null) where T : class
        {
            var response = await SendRequestAsync("PUT", endpoint, data, queryParams);
            return ParseResponse<T>(response);
        }

        /// <summary>
        /// DELETE请求
        /// </summary>
        public async Task<T> DeleteAsync<T>(string endpoint, object queryParams = null) where T : class
        {
            var response = await SendRequestAsync("DELETE", endpoint, null, queryParams);
            return ParseResponse<T>(response);
        }

        /// <summary>
        /// 原始响应GET请求
        /// </summary>
        public async Task<HttpResponse> GetRawAsync(string endpoint, object queryParams = null)
        {
            return await SendRequestAsync("GET", endpoint, null, queryParams);
        }

        /// <summary>
        /// 原始响应POST请求
        /// </summary>
        public async Task<HttpResponse> PostRawAsync(string endpoint, object data = null, object queryParams = null)
        {
            return await SendRequestAsync("POST", endpoint, data, queryParams);
        }

        #endregion

        #region 核心请求方法

        /// <summary>
        /// 发送HTTP请求的核心方法
        /// </summary>
        private async Task<HttpResponse> SendRequestAsync(string method, string endpoint, object data = null, object queryParams = null)
        {
            var startTime = DateTime.UtcNow;
            HttpResponse response = null;
            Exception lastException = null;
            
            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    // 构建URL
                    var url = BuildUrl(endpoint, queryParams);
                    
                    // 创建请求
                    var request = CreateRequest(method, url, data);
                    
                    // 应用请求拦截器
                    request = ApplyRequestInterceptors(request);
                    
                    // 发送请求
                    response = await ExecuteRequestAsync(request);
                    
                    // 应用响应拦截器
                    response = ApplyResponseInterceptors(response);
                    
                    // 更新统计信息
                    UpdateStatistics(startTime, response, attempt);
                    
                    // 成功返回
                    return response;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (_enableLogging)
                    {
                        Debug.LogWarning($"[HttpClient] 请求失败 (尝试 {attempt + 1}/{_maxRetries + 1}): {ex.Message}");
                    }
                    
                    // 如果不是最后一次尝试，等待后重试
                    if (attempt < _maxRetries)
                    {
                        var delay = CalculateRetryDelay(attempt);
                        await Task.Delay(delay);
                    }
                }
            }
            
            // 所有重试都失败，抛出异常
            var error = new HttpError
            {
                message = $"请求失败，已重试{_maxRetries}次: {lastException?.Message}",
                statusCode = response?.statusCode ?? 0,
                url = BuildUrl(endpoint, queryParams),
                method = method,
                timestamp = DateTime.UtcNow,
                exception = lastException
            };
            
            _globalErrorHandler?.Invoke(error);
            throw new HttpRequestException(error.message, lastException);
        }

        /// <summary>
        /// 创建UnityWebRequest
        /// </summary>
        private UnityWebRequest CreateRequest(string method, string url, object data)
        {
            UnityWebRequest request;
            
            switch (method.ToUpper())
            {
                case "GET":
                    request = UnityWebRequest.Get(url);
                    break;
                    
                case "POST":
                    if (data != null)
                    {
                        var json = JsonUtility.ToJson(data);
                        var bodyData = Encoding.UTF8.GetBytes(json);
                        request = UnityWebRequest.Put(url, bodyData);
                        request.method = "POST";
                    }
                    else
                    {
                        request = UnityWebRequest.PostWwwForm(url, "");
                    }
                    break;
                    
                case "PUT":
                    if (data != null)
                    {
                        var json = JsonUtility.ToJson(data);
                        var bodyData = Encoding.UTF8.GetBytes(json);
                        request = UnityWebRequest.Put(url, bodyData);
                    }
                    else
                    {
                        request = UnityWebRequest.Put(url, "");
                    }
                    break;
                    
                case "DELETE":
                    request = UnityWebRequest.Delete(url);
                    break;
                    
                default:
                    throw new NotSupportedException($"不支持的HTTP方法: {method}");
            }
            
            // 设置超时
            request.timeout = _timeout;
            
            // 设置默认请求头
            foreach (var header in _defaultHeaders)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }
            
            return request;
        }

        /// <summary>
        /// 执行请求
        /// </summary>
        private async Task<HttpResponse> ExecuteRequestAsync(UnityWebRequest request)
        {
            var taskCompletionSource = new TaskCompletionSource<HttpResponse>();
            
            // 开始协程
            StartCoroutine(ExecuteRequestCoroutine(request, taskCompletionSource));
            
            return await taskCompletionSource.Task;
        }

        /// <summary>
        /// 执行请求的协程
        /// </summary>
        private IEnumerator ExecuteRequestCoroutine(UnityWebRequest request, TaskCompletionSource<HttpResponse> taskSource)
        {
            var startTime = Time.realtimeSinceStartup;
            
            if (_enableLogging)
            {
                Debug.Log($"[HttpClient] 发送请求: {request.method} {request.url}");
            }
            
            yield return request.SendWebRequest();
            
            var duration = Time.realtimeSinceStartup - startTime;
            
            try
            {
                var response = new HttpResponse
                {
                    statusCode = (int)request.responseCode,
                    data = request.downloadHandler?.text ?? "",
                    headers = GetResponseHeaders(request),
                    isSuccess = request.result == UnityWebRequest.Result.Success,
                    error = request.error,
                    url = request.url,
                    method = request.method,
                    duration = duration,
                    timestamp = DateTime.UtcNow
                };
                
                if (_enableLogging)
                {
                    Debug.Log($"[HttpClient] 响应: {response.statusCode} ({duration:F2}s) {request.url}");
                    if (!response.isSuccess)
                    {
                        Debug.LogError($"[HttpClient] 请求错误: {response.error}");
                    }
                }
                
                taskSource.SetResult(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpClient] 处理响应时发生错误: {ex.Message}");
                taskSource.SetException(ex);
            }
            finally
            {
                request.Dispose();
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 构建完整URL
        /// </summary>
        private string BuildUrl(string endpoint, object queryParams = null)
        {
            var url = endpoint.StartsWith("http") ? endpoint : $"{_baseUrl}/{endpoint.TrimStart('/')}";
            
            if (queryParams != null)
            {
                var queryString = BuildQueryString(queryParams);
                if (!string.IsNullOrEmpty(queryString))
                {
                    url += (url.Contains("?") ? "&" : "?") + queryString;
                }
            }
            
            return url;
        }

        /// <summary>
        /// 构建查询字符串
        /// </summary>
        private string BuildQueryString(object queryParams)
        {
            if (queryParams == null) return "";
            
            var pairs = new List<string>();
            
            if (queryParams is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    if (kvp.Value != null)
                    {
                        pairs.Add($"{UnityWebRequest.EscapeURL(kvp.Key)}={UnityWebRequest.EscapeURL(kvp.Value.ToString())}");
                    }
                }
            }
            else
            {
                // 使用反射处理普通对象
                var type = queryParams.GetType();
                var fields = type.GetFields();
                
                foreach (var field in fields)
                {
                    var value = field.GetValue(queryParams);
                    if (value != null)
                    {
                        pairs.Add($"{UnityWebRequest.EscapeURL(field.Name)}={UnityWebRequest.EscapeURL(value.ToString())}");
                    }
                }
            }
            
            return string.Join("&", pairs);
        }

        /// <summary>
        /// 获取响应头
        /// </summary>
        private Dictionary<string, string> GetResponseHeaders(UnityWebRequest request)
        {
            var headers = new Dictionary<string, string>();
            
            if (request.GetResponseHeaders() != null)
            {
                foreach (var header in request.GetResponseHeaders())
                {
                    headers[header.Key] = header.Value;
                }
            }
            
            return headers;
        }

        /// <summary>
        /// 应用请求拦截器
        /// </summary>
        private UnityWebRequest ApplyRequestInterceptors(UnityWebRequest request)
        {
            foreach (var interceptor in _requestInterceptors)
            {
                request = interceptor(request);
            }
            return request;
        }

        /// <summary>
        /// 应用响应拦截器
        /// </summary>
        private HttpResponse ApplyResponseInterceptors(HttpResponse response)
        {
            foreach (var interceptor in _responseInterceptors)
            {
                response = interceptor(response);
            }
            return response;
        }

        /// <summary>
        /// 默认响应拦截器
        /// </summary>
        private HttpResponse DefaultResponseInterceptor(HttpResponse response)
        {
            // 处理认证失败
            if (response.statusCode == 401 || response.statusCode == 403)
            {
                Debug.LogWarning("[HttpClient] 认证失败，Token可能已过期");
                _authFailedHandler?.Invoke();
            }
            
            // 处理服务器错误
            if (response.statusCode >= 500)
            {
                Debug.LogError($"[HttpClient] 服务器错误: {response.statusCode} - {response.error}");
            }
            
            return response;
        }

        /// <summary>
        /// 解析响应数据
        /// </summary>
        private T ParseResponse<T>(HttpResponse response) where T : class
        {
            if (!response.isSuccess)
            {
                throw new HttpRequestException($"HTTP请求失败: {response.statusCode} - {response.error}");
            }
            
            if (string.IsNullOrEmpty(response.data))
            {
                return null;
            }
            
            try
            {
                // 尝试解析为API响应格式
                var apiResponse = JsonUtility.FromJson<ApiResponseWrapper<T>>(response.data);
                if (apiResponse != null && apiResponse.code == 200)
                {
                    return apiResponse.data;
                }
                
                // 直接解析数据
                return JsonUtility.FromJson<T>(response.data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpClient] 解析响应数据失败: {ex.Message}\n数据: {response.data}");
                throw new HttpRequestException($"解析响应数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 计算重试延迟
        /// </summary>
        private int CalculateRetryDelay(int attempt)
        {
            // 指数退避算法：1s, 2s, 4s
            return (int)Math.Pow(2, attempt) * 1000;
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics(DateTime startTime, HttpResponse response, int attempt)
        {
            _statistics.totalRequests++;
            _statistics.totalDuration += response.duration;
            _statistics.averageResponseTime = _statistics.totalDuration / _statistics.totalRequests;
            
            if (response.isSuccess)
            {
                _statistics.successfulRequests++;
            }
            else
            {
                _statistics.failedRequests++;
            }
            
            if (attempt > 0)
            {
                _statistics.retriedRequests++;
            }
            
            _statistics.lastRequestTime = DateTime.UtcNow;
        }

        #endregion

        #region 统计和监控

        /// <summary>
        /// 获取HTTP统计信息
        /// </summary>
        public HttpStatistics GetStatistics()
        {
            _statistics.successRate = _statistics.totalRequests > 0 
                ? (float)_statistics.successfulRequests / _statistics.totalRequests 
                : 0f;
            
            return _statistics;
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            _statistics = new HttpStatistics();
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// HTTP响应
    /// </summary>
    [System.Serializable]
    public class HttpResponse
    {
        public int statusCode;
        public string data;
        public Dictionary<string, string> headers;
        public bool isSuccess;
        public string error;
        public string url;
        public string method;
        public float duration;
        public DateTime timestamp;
    }

    /// <summary>
    /// HTTP错误
    /// </summary>
    [System.Serializable]
    public class HttpError
    {
        public string message;
        public int statusCode;
        public string url;
        public string method;
        public DateTime timestamp;
        public Exception exception;
    }

    /// <summary>
    /// HTTP统计信息
    /// </summary>
    [System.Serializable]
    public class HttpStatistics
    {
        public int totalRequests;
        public int successfulRequests;
        public int failedRequests;
        public int retriedRequests;
        public float totalDuration;
        public float averageResponseTime;
        public float successRate;
        public DateTime lastRequestTime;
    }

    /// <summary>
    /// API响应包装器
    /// </summary>
    [System.Serializable]
    public class ApiResponseWrapper<T>
    {
        public int code;
        public string message;
        public T data;
        public long timestamp;
    }

    /// <summary>
    /// HTTP请求异常
    /// </summary>
    public class HttpRequestException : Exception
    {
        public HttpRequestException(string message) : base(message) { }
        public HttpRequestException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion
}