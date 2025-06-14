using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Core.Network;
using Core.Network.Utils;
using Core.Data.Types;
using Core.Data.Config;

namespace Scripts.Testing
{
    /// <summary>
    /// 网络测试器 - 用于测试和调试网络功能
    /// 包含HTTP、WebSocket连接测试，性能分析等功能
    /// </summary>
    public class NetworkTester : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool _enableAutoTesting = false;
        [SerializeField] private float _testInterval = 30f;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _enableUIDisplay = true;

        [Header("测试目标")]
        [SerializeField] private string[] _testUrls = {
            "https://api.example.com/health",
            "https://websocket.example.com",
            "https://cdn.example.com/test.json"
        };

        [Header("性能测试参数")]
        [SerializeField] private int _maxConcurrentRequests = 5;
        [SerializeField] private int _testIterations = 10;
        [SerializeField] private float _timeoutSeconds = 10f;

        [Header("UI组件")]
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _resultsText;
        [SerializeField] private Button _testHttpButton;
        [SerializeField] private Button _testWebSocketButton;
        [SerializeField] private Button _testPerformanceButton;
        [SerializeField] private Button _clearResultsButton;
        [SerializeField] private Slider _progressSlider;

        // 私有成员
        private NetworkManager _networkManager;
        private ConnectionMonitor _connectionMonitor;
        private List<TestResult> _testResults = new List<TestResult>();
        private Coroutine _autoTestingCoroutine;
        private bool _isTesting = false;

        #region Unity生命周期

        private void Start()
        {
            InitializeComponents();
            SetupUI();
            
            if (_enableAutoTesting)
            {
                StartAutoTesting();
            }
        }

        private void OnDestroy()
        {
            StopAutoTesting();
        }

        #endregion

        #region 初始化

        private void InitializeComponents()
        {
            // 获取网络管理器
            _networkManager = FindObjectOfType<NetworkManager>();
            if (_networkManager == null)
            {
                LogError("未找到NetworkManager组件");
                return;
            }

            // 获取连接监控器
            _connectionMonitor = FindObjectOfType<ConnectionMonitor>();
            
            LogInfo("网络测试器初始化完成");
        }

        private void SetupUI()
        {
            if (!_enableUIDisplay) return;

            // 设置按钮事件
            if (_testHttpButton != null)
                _testHttpButton.onClick.AddListener(() => StartCoroutine(TestHttpAsync()));

            if (_testWebSocketButton != null)
                _testWebSocketButton.onClick.AddListener(() => StartCoroutine(TestWebSocketAsync()));

            if (_testPerformanceButton != null)
                _testPerformanceButton.onClick.AddListener(() => StartCoroutine(TestPerformanceAsync()));

            if (_clearResultsButton != null)
                _clearResultsButton.onClick.AddListener(ClearResults);

            // 初始化进度条
            if (_progressSlider != null)
                _progressSlider.value = 0f;

            UpdateStatusUI("准备就绪");
        }

        #endregion

        #region HTTP测试

        public IEnumerator TestHttpAsync()
        {
            if (_isTesting)
            {
                LogWarning("测试正在进行中，请稍候");
                yield break;
            }

            _isTesting = true;
            UpdateStatusUI("正在测试HTTP连接...");
            UpdateProgress(0f);

            var results = new List<HttpTestResult>();

            for (int i = 0; i < _testUrls.Length; i++)
            {
                var url = _testUrls[i];
                LogInfo($"测试URL: {url}");

                var result = new HttpTestResult
                {
                    Url = url,
                    StartTime = DateTime.UtcNow
                };

                try
                {
                    // 创建HTTP请求任务
                    var task = TestSingleHttpRequest(url);
                    
                    // 等待完成或超时
                    float elapsed = 0f;
                    while (!task.IsCompleted && elapsed < _timeoutSeconds)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    if (task.IsCompletedSuccessfully)
                    {
                        var response = task.Result;
                        result.IsSuccessful = response.IsSuccess;
                        result.StatusCode = response.StatusCode;
                        result.ResponseTime = response.ResponseTime;
                        result.ErrorMessage = response.ErrorMessage;
                    }
                    else
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage = "请求超时或失败";
                    }
                }
                catch (Exception ex)
                {
                    result.IsSuccessful = false;
                    result.ErrorMessage = ex.Message;
                }

                result.EndTime = DateTime.UtcNow;
                results.Add(result);

                // 更新进度
                UpdateProgress((float)(i + 1) / _testUrls.Length);
                yield return new WaitForSeconds(0.5f);
            }

            // 保存测试结果
            var testResult = new TestResult
            {
                TestType = TestType.Http,
                TestTime = DateTime.UtcNow,
                HttpResults = results,
                IsSuccessful = results.TrueForAll(r => r.IsSuccessful)
            };

            _testResults.Add(testResult);
            UpdateResultsUI(testResult);
            UpdateStatusUI($"HTTP测试完成 - 成功: {results.FindAll(r => r.IsSuccessful).Count}/{results.Count}");

            _isTesting = false;
        }

        private async Task<HttpResponse> TestSingleHttpRequest(string url)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                
                // 这里需要调用实际的HTTP客户端
                // 示例实现，实际应该使用项目中的HttpClient
                using (var www = new UnityEngine.Networking.UnityWebRequest(url))
                {
                    www.method = "GET";
                    www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                    www.timeout = (int)_timeoutSeconds;

                    var operation = www.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    return new HttpResponse
                    {
                        IsSuccess = www.result == UnityEngine.Networking.UnityWebRequest.Result.Success,
                        StatusCode = (int)www.responseCode,
                        ResponseTime = (float)responseTime,
                        ErrorMessage = www.error
                    };
                }
            }
            catch (Exception ex)
            {
                return new HttpResponse
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region WebSocket测试

        public IEnumerator TestWebSocketAsync()
        {
            if (_isTesting)
            {
                LogWarning("测试正在进行中，请稍候");
                yield break;
            }

            _isTesting = true;
            UpdateStatusUI("正在测试WebSocket连接...");
            UpdateProgress(0f);

            var wsTestResult = new WebSocketTestResult
            {
                StartTime = DateTime.UtcNow
            };

            try
            {
                // 测试WebSocket连接
                if (_networkManager != null)
                {
                    UpdateProgress(0.2f);
                    
                    // 测试WebSocket管理器状态
                    var wsManager = _networkManager.GetComponent<WebSocketManager>();
                    if (wsManager != null)
                    {
                        wsTestResult.InitialConnectionState = wsManager.IsConnected;
                        
                        UpdateProgress(0.4f);
                        yield return new WaitForSeconds(1f);

                        // 测试连接建立
                        if (!wsManager.IsConnected)
                        {
                            LogInfo("尝试建立WebSocket连接...");
                            // 这里应该调用连接方法
                            // await wsManager.ConnectAsync();
                        }

                        UpdateProgress(0.6f);
                        yield return new WaitForSeconds(2f);

                        // 测试ping/pong
                        var pingStart = DateTime.UtcNow;
                        // 发送ping消息的逻辑
                        var pingTime = (DateTime.UtcNow - pingStart).TotalMilliseconds;
                        wsTestResult.PingTime = (float)pingTime;

                        UpdateProgress(0.8f);
                        yield return new WaitForSeconds(1f);

                        wsTestResult.FinalConnectionState = wsManager.IsConnected;
                        wsTestResult.IsSuccessful = wsManager.IsConnected;
                    }
                    else
                    {
                        wsTestResult.ErrorMessage = "未找到WebSocketManager组件";
                    }
                }
                else
                {
                    wsTestResult.ErrorMessage = "未找到NetworkManager组件";
                }
            }
            catch (Exception ex)
            {
                wsTestResult.IsSuccessful = false;
                wsTestResult.ErrorMessage = ex.Message;
                LogError($"WebSocket测试异常: {ex.Message}");
            }

            wsTestResult.EndTime = DateTime.UtcNow;
            UpdateProgress(1f);

            // 保存测试结果
            var testResult = new TestResult
            {
                TestType = TestType.WebSocket,
                TestTime = DateTime.UtcNow,
                WebSocketResult = wsTestResult,
                IsSuccessful = wsTestResult.IsSuccessful
            };

            _testResults.Add(testResult);
            UpdateResultsUI(testResult);
            UpdateStatusUI($"WebSocket测试完成 - {(wsTestResult.IsSuccessful ? "成功" : "失败")}");

            _isTesting = false;
        }

        #endregion

        #region 性能测试

        public IEnumerator TestPerformanceAsync()
        {
            if (_isTesting)
            {
                LogWarning("测试正在进行中，请稍候");
                yield break;
            }

            _isTesting = true;
            UpdateStatusUI("正在进行性能测试...");
            UpdateProgress(0f);

            var perfResult = new PerformanceTestResult
            {
                StartTime = DateTime.UtcNow,
                TestIterations = _testIterations,
                ConcurrentRequests = _maxConcurrentRequests
            };

            var responseTimes = new List<float>();
            var successCount = 0;

            try
            {
                for (int iteration = 0; iteration < _testIterations; iteration++)
                {
                    LogInfo($"性能测试迭代 {iteration + 1}/{_testIterations}");

                    var iterationTasks = new List<Task<HttpResponse>>();
                    
                    // 启动并发请求
                    for (int i = 0; i < _maxConcurrentRequests && i < _testUrls.Length; i++)
                    {
                        var task = TestSingleHttpRequest(_testUrls[i]);
                        iterationTasks.Add(task);
                    }

                    // 等待所有请求完成
                    var iterationStart = DateTime.UtcNow;
                    while (iterationTasks.Exists(t => !t.IsCompleted))
                    {
                        yield return null;
                    }
                    var iterationTime = (DateTime.UtcNow - iterationStart).TotalMilliseconds;

                    // 收集结果
                    foreach (var task in iterationTasks)
                    {
                        if (task.IsCompletedSuccessfully && task.Result.IsSuccess)
                        {
                            responseTimes.Add(task.Result.ResponseTime);
                            successCount++;
                        }
                    }

                    perfResult.IterationTimes.Add((float)iterationTime);

                    // 更新进度
                    UpdateProgress((float)(iteration + 1) / _testIterations);
                    yield return new WaitForSeconds(0.1f);
                }

                // 计算统计数据
                if (responseTimes.Count > 0)
                {
                    responseTimes.Sort();
                    perfResult.AverageResponseTime = responseTimes.GetRange(0, responseTimes.Count).Sum() / responseTimes.Count;
                    perfResult.MinResponseTime = responseTimes[0];
                    perfResult.MaxResponseTime = responseTimes[responseTimes.Count - 1];
                    perfResult.MedianResponseTime = responseTimes[responseTimes.Count / 2];
                }

                perfResult.SuccessRate = (float)successCount / (_testIterations * _maxConcurrentRequests);
                perfResult.IsSuccessful = perfResult.SuccessRate > 0.8f;
            }
            catch (Exception ex)
            {
                perfResult.IsSuccessful = false;
                perfResult.ErrorMessage = ex.Message;
                LogError($"性能测试异常: {ex.Message}");
            }

            perfResult.EndTime = DateTime.UtcNow;

            // 保存测试结果
            var testResult = new TestResult
            {
                TestType = TestType.Performance,
                TestTime = DateTime.UtcNow,
                PerformanceResult = perfResult,
                IsSuccessful = perfResult.IsSuccessful
            };

            _testResults.Add(testResult);
            UpdateResultsUI(testResult);
            UpdateStatusUI($"性能测试完成 - 成功率: {perfResult.SuccessRate:P2}");

            _isTesting = false;
        }

        #endregion

        #region 自动测试

        private void StartAutoTesting()
        {
            if (_autoTestingCoroutine != null)
            {
                StopCoroutine(_autoTestingCoroutine);
            }

            _autoTestingCoroutine = StartCoroutine(AutoTestingCoroutine());
            LogInfo("自动测试已启动");
        }

        private void StopAutoTesting()
        {
            if (_autoTestingCoroutine != null)
            {
                StopCoroutine(_autoTestingCoroutine);
                _autoTestingCoroutine = null;
            }

            LogInfo("自动测试已停止");
        }

        private IEnumerator AutoTestingCoroutine()
        {
            while (_enableAutoTesting)
            {
                if (!_isTesting)
                {
                    // 轮流执行不同类型的测试
                    var testType = (TestType)(_testResults.Count % 3);
                    
                    switch (testType)
                    {
                        case TestType.Http:
                            yield return StartCoroutine(TestHttpAsync());
                            break;
                        case TestType.WebSocket:
                            yield return StartCoroutine(TestWebSocketAsync());
                            break;
                        case TestType.Performance:
                            yield return StartCoroutine(TestPerformanceAsync());
                            break;
                    }
                }

                yield return new WaitForSeconds(_testInterval);
            }
        }

        #endregion

        #region UI更新

        private void UpdateStatusUI(string status)
        {
            if (!_enableUIDisplay || _statusText == null) return;

            _statusText.text = $"状态: {status}";
            LogInfo(status);
        }

        private void UpdateProgress(float progress)
        {
            if (!_enableUIDisplay || _progressSlider == null) return;

            _progressSlider.value = progress;
        }

        private void UpdateResultsUI(TestResult result)
        {
            if (!_enableUIDisplay || _resultsText == null) return;

            var resultText = FormatTestResult(result);
            _resultsText.text += resultText + "\n\n";

            // 限制显示的结果数量
            var lines = _resultsText.text.Split('\n');
            if (lines.Length > 100)
            {
                var keepLines = lines[^50..]; // 保留最后50行
                _resultsText.text = string.Join("\n", keepLines);
            }
        }

        private string FormatTestResult(TestResult result)
        {
            var text = $"[{result.TestTime:HH:mm:ss}] {result.TestType} 测试 - {(result.IsSuccessful ? "✅成功" : "❌失败")}";

            switch (result.TestType)
            {
                case TestType.Http:
                    if (result.HttpResults != null)
                    {
                        var successful = result.HttpResults.FindAll(r => r.IsSuccessful).Count;
                        text += $"\n  连接测试: {successful}/{result.HttpResults.Count}";
                        
                        foreach (var httpResult in result.HttpResults)
                        {
                            text += $"\n  {httpResult.Url}: {httpResult.StatusCode} ({httpResult.ResponseTime:F0}ms)";
                        }
                    }
                    break;

                case TestType.WebSocket:
                    if (result.WebSocketResult != null)
                    {
                        text += $"\n  连接状态: {result.WebSocketResult.FinalConnectionState}";
                        text += $"\n  Ping时间: {result.WebSocketResult.PingTime:F0}ms";
                    }
                    break;

                case TestType.Performance:
                    if (result.PerformanceResult != null)
                    {
                        text += $"\n  成功率: {result.PerformanceResult.SuccessRate:P2}";
                        text += $"\n  平均响应: {result.PerformanceResult.AverageResponseTime:F0}ms";
                        text += $"\n  响应范围: {result.PerformanceResult.MinResponseTime:F0}-{result.PerformanceResult.MaxResponseTime:F0}ms";
                    }
                    break;
            }

            return text;
        }

        private void ClearResults()
        {
            _testResults.Clear();
            
            if (_resultsText != null)
            {
                _resultsText.text = "";
            }

            UpdateStatusUI("结果已清空");
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 获取测试历史
        /// </summary>
        public List<TestResult> GetTestHistory()
        {
            return new List<TestResult>(_testResults);
        }

        /// <summary>
        /// 获取最近的测试结果
        /// </summary>
        public TestResult GetLatestResult(TestType testType)
        {
            for (int i = _testResults.Count - 1; i >= 0; i--)
            {
                if (_testResults[i].TestType == testType)
                {
                    return _testResults[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 设置测试参数
        /// </summary>
        public void SetTestParameters(string[] urls, int iterations, float timeout)
        {
            _testUrls = urls;
            _testIterations = iterations;
            _timeoutSeconds = timeout;
            
            LogInfo("测试参数已更新");
        }

        /// <summary>
        /// 导出测试报告
        /// </summary>
        public string ExportTestReport()
        {
            var report = $"网络测试报告 - 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            report += $"总测试次数: {_testResults.Count}\n\n";

            foreach (var result in _testResults)
            {
                report += FormatTestResult(result) + "\n\n";
            }

            return report;
        }

        #endregion

        #region 日志方法

        private void LogInfo(string message)
        {
            if (_enableLogging)
            {
                Debug.Log($"[NetworkTester] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (_enableLogging)
            {
                Debug.LogWarning($"[NetworkTester] {message}");
            }
        }

        private void LogError(string message)
        {
            if (_enableLogging)
            {
                Debug.LogError($"[NetworkTester] {message}");
            }
        }

        #endregion
    }

    #region 数据结构定义

    /// <summary>
    /// 测试类型枚举
    /// </summary>
    public enum TestType
    {
        Http = 0,
        WebSocket = 1,
        Performance = 2
    }

    /// <summary>
    /// 测试结果基类
    /// </summary>
    [System.Serializable]
    public class TestResult
    {
        public TestType TestType;
        public DateTime TestTime;
        public bool IsSuccessful;
        public List<HttpTestResult> HttpResults;
        public WebSocketTestResult WebSocketResult;
        public PerformanceTestResult PerformanceResult;
    }

    /// <summary>
    /// HTTP测试结果
    /// </summary>
    [System.Serializable]
    public class HttpTestResult
    {
        public string Url;
        public bool IsSuccessful;
        public int StatusCode;
        public float ResponseTime;
        public string ErrorMessage;
        public DateTime StartTime;
        public DateTime EndTime;
    }

    /// <summary>
    /// WebSocket测试结果
    /// </summary>
    [System.Serializable]
    public class WebSocketTestResult
    {
        public bool IsSuccessful;
        public bool InitialConnectionState;
        public bool FinalConnectionState;
        public float PingTime;
        public string ErrorMessage;
        public DateTime StartTime;
        public DateTime EndTime;
    }

    /// <summary>
    /// 性能测试结果
    /// </summary>
    [System.Serializable]
    public class PerformanceTestResult
    {
        public bool IsSuccessful;
        public int TestIterations;
        public int ConcurrentRequests;
        public float AverageResponseTime;
        public float MinResponseTime;
        public float MaxResponseTime;
        public float MedianResponseTime;
        public float SuccessRate;
        public List<float> IterationTimes = new List<float>();
        public string ErrorMessage;
        public DateTime StartTime;
        public DateTime EndTime;
    }

    /// <summary>
    /// HTTP响应结果
    /// </summary>
    public class HttpResponse
    {
        public bool IsSuccess;
        public int StatusCode;
        public float ResponseTime;
        public string ErrorMessage;
    }

    #endregion
}