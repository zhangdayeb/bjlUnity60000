// Assets/_Core/Network/Utils/ConnectionMonitor.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Network.Interfaces;

namespace Core.Network.Utils
{
    /// <summary>
    /// 连接监控器
    /// 实时监控网络连接状态、质量和性能，提供连接健康度评估和预警机制
    /// </summary>
    public class ConnectionMonitor : MonoBehaviour
    {
        #region 监控事件

        public event Action<ConnectionStatus> OnConnectionStatusChanged;
        public event Action<ConnectionQuality> OnConnectionQualityChanged;
        public event Action<NetworkPerformanceMetrics> OnPerformanceMetricsUpdated;
        public event Action<ConnectionAlert> OnConnectionAlert;
        public event Action<ConnectivityReport> OnConnectivityReportGenerated;

        #endregion

        #region Inspector配置

        [Header("监控配置")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _monitoringInterval = 5f; // 监控间隔（秒）
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _enablePerformanceTracking = true;

        [Header("连接测试配置")]
        [SerializeField] private string[] _testUrls = {
            "https://www.google.com",
            "https://www.baidu.com",
            "https://httpbin.org/get"
        };
        [SerializeField] private int _connectionTimeout = 5000; // 超时时间（毫秒）
        [SerializeField] private int _maxConcurrentTests = 3;

        [Header("质量评估配置")]
        [SerializeField] private float _excellentLatencyThreshold = 50f; // 优秀延迟阈值（毫秒）
        [SerializeField] private float _goodLatencyThreshold = 150f; // 良好延迟阈值
        [SerializeField] private float _poorLatencyThreshold = 500f; // 较差延迟阈值
        [SerializeField] private float _minSuccessRateThreshold = 0.8f; // 最低成功率阈值

        [Header("告警配置")]
        [SerializeField] private bool _enableAlerts = true;
        [SerializeField] private int _consecutiveFailureThreshold = 3; // 连续失败告警阈值
        [SerializeField] private float _latencyDegradationThreshold = 2.0f; // 延迟恶化倍数阈值
        [SerializeField] private float _successRateDropThreshold = 0.5f; // 成功率下降阈值

        [Header("历史数据配置")]
        [SerializeField] private int _maxHistorySize = 100;
        [SerializeField] private bool _enableTrendAnalysis = true;

        #endregion

        #region 私有字段

        // 监控状态
        private bool _isMonitoring = false;
        private Coroutine _monitoringCoroutine;
        
        // 连接状态
        private ConnectionStatus _currentStatus = ConnectionStatus.Unknown;
        private ConnectionQuality _currentQuality = ConnectionQuality.Unknown;
        private NetworkReachability _lastReachability = NetworkReachability.NotReachable;
        
        // 性能指标
        private NetworkPerformanceMetrics _currentMetrics;
        private Queue<NetworkPerformanceMetrics> _metricsHistory;
        private Queue<ConnectionTestResult> _testHistory;
        
        // 告警管理
        private int _consecutiveFailures = 0;
        private DateTime _lastSuccessfulTest = DateTime.MinValue;
        private List<ConnectionAlert> _activeAlerts;
        
        // 趋势分析
        private TrendAnalyzer _trendAnalyzer;
        
        // 测试管理
        private Dictionary<string, ConnectionTestSession> _activeSessions;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            if (_enableMonitoring)
            {
                StartMonitoring();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseMonitoring();
            }
            else
            {
                ResumeMonitoring();
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
            _currentMetrics = new NetworkPerformanceMetrics();
            _metricsHistory = new Queue<NetworkPerformanceMetrics>();
            _testHistory = new Queue<ConnectionTestResult>();
            _activeAlerts = new List<ConnectionAlert>();
            _activeSessions = new Dictionary<string, ConnectionTestSession>();
            
            if (_enableTrendAnalysis)
            {
                _trendAnalyzer = new TrendAnalyzer();
            }
            
            // 初始化连接状态
            UpdateReachabilityStatus();
            
            if (_enableLogging)
            {
                Debug.Log("[ConnectionMonitor] 连接监控器已初始化");
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 开始监控
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
            {
                if (_enableLogging)
                {
                    Debug.LogWarning("[ConnectionMonitor] 监控已在运行中");
                }
                return;
            }

            _isMonitoring = true;
            _monitoringCoroutine = StartCoroutine(MonitoringCoroutine());
            
            if (_enableLogging)
            {
                Debug.Log("[ConnectionMonitor] 连接监控已启动");
            }
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
            {
                return;
            }

            _isMonitoring = false;
            
            if (_monitoringCoroutine != null)
            {
                StopCoroutine(_monitoringCoroutine);
                _monitoringCoroutine = null;
            }
            
            if (_enableLogging)
            {
                Debug.Log("[ConnectionMonitor] 连接监控已停止");
            }
        }

        /// <summary>
        /// 暂停监控
        /// </summary>
        public void PauseMonitoring()
        {
            if (_isMonitoring && _monitoringCoroutine != null)
            {
                StopCoroutine(_monitoringCoroutine);
                _monitoringCoroutine = null;
                
                if (_enableLogging)
                {
                    Debug.Log("[ConnectionMonitor] 连接监控已暂停");
                }
            }
        }

        /// <summary>
        /// 恢复监控
        /// </summary>
        public void ResumeMonitoring()
        {
            if (_isMonitoring && _monitoringCoroutine == null)
            {
                _monitoringCoroutine = StartCoroutine(MonitoringCoroutine());
                
                if (_enableLogging)
                {
                    Debug.Log("[ConnectionMonitor] 连接监控已恢复");
                }
            }
        }

        /// <summary>
        /// 手动测试连接
        /// </summary>
        public async Task<ConnectionTestResult> TestConnectionAsync(string testUrl = null)
        {
            var url = testUrl ?? (_testUrls.Length > 0 ? _testUrls[0] : "https://www.google.com");
            return await PerformConnectionTest(url);
        }

        /// <summary>
        /// 批量测试连接
        /// </summary>
        public async Task<ConnectionTestSummary> TestMultipleConnectionsAsync()
        {
            var results = new List<ConnectionTestResult>();
            var tasks = new List<Task<ConnectionTestResult>>();
            
            // 限制并发数量
            var semaphore = new System.Threading.SemaphoreSlim(_maxConcurrentTests);
            
            foreach (var url in _testUrls)
            {
                tasks.Add(TestWithSemaphore(url, semaphore));
            }
            
            try
            {
                var allResults = await Task.WhenAll(tasks);
                results.AddRange(allResults);
            }
            finally
            {
                semaphore.Dispose();
            }
            
            return new ConnectionTestSummary
            {
                Results = results,
                TestTime = DateTime.UtcNow,
                SuccessfulTests = results.FindAll(r => r.IsSuccessful).Count,
                TotalTests = results.Count,
                AverageLatency = results.Where(r => r.IsSuccessful).Average(r => r.LatencyMs),
                OverallQuality = CalculateOverallQuality(results)
            };
        }

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        public ConnectionStatus GetCurrentStatus()
        {
            return _currentStatus;
        }

        /// <summary>
        /// 获取当前连接质量
        /// </summary>
        public ConnectionQuality GetCurrentQuality()
        {
            return _currentQuality;
        }

        /// <summary>
        /// 获取当前性能指标
        /// </summary>
        public NetworkPerformanceMetrics GetCurrentMetrics()
        {
            return _currentMetrics;
        }

        /// <summary>
        /// 获取性能历史
        /// </summary>
        public NetworkPerformanceMetrics[] GetMetricsHistory()
        {
            return _metricsHistory.ToArray();
        }

        /// <summary>
        /// 获取测试历史
        /// </summary>
        public ConnectionTestResult[] GetTestHistory()
        {
            return _testHistory.ToArray();
        }

        /// <summary>
        /// 获取活跃告警
        /// </summary>
        public ConnectionAlert[] GetActiveAlerts()
        {
            return _activeAlerts.ToArray();
        }

        /// <summary>
        /// 清除告警
        /// </summary>
        public void ClearAlert(string alertId)
        {
            _activeAlerts.RemoveAll(a => a.Id == alertId);
            
            if (_enableLogging)
            {
                Debug.Log($"[ConnectionMonitor] 告警已清除: {alertId}");
            }
        }

        /// <summary>
        /// 清除所有告警
        /// </summary>
        public void ClearAllAlerts()
        {
            _activeAlerts.Clear();
            
            if (_enableLogging)
            {
                Debug.Log("[ConnectionMonitor] 所有告警已清除");
            }
        }

        /// <summary>
        /// 生成连接报告
        /// </summary>
        public ConnectivityReport GenerateReport()
        {
            var report = new ConnectivityReport
            {
                GeneratedAt = DateTime.UtcNow,
                CurrentStatus = _currentStatus,
                CurrentQuality = _currentQuality,
                CurrentMetrics = _currentMetrics,
                MetricsHistory = _metricsHistory.ToArray(),
                TestHistory = _testHistory.ToArray(),
                ActiveAlerts = _activeAlerts.ToArray(),
                Summary = GenerateReportSummary()
            };
            
            if (_enableTrendAnalysis && _trendAnalyzer != null)
            {
                report.TrendAnalysis = _trendAnalyzer.AnalyzeTrends(_metricsHistory.ToArray());
            }
            
            OnConnectivityReportGenerated?.Invoke(report);
            
            return report;
        }

        /// <summary>
        /// 设置监控配置
        /// </summary>
        public void SetMonitoringConfig(ConnectionMonitorConfig config)
        {
            _enableMonitoring = config.enableMonitoring;
            _monitoringInterval = config.monitoringInterval;
            _connectionTimeout = config.connectionTimeout;
            _enableAlerts = config.enableAlerts;
            _consecutiveFailureThreshold = config.consecutiveFailureThreshold;
            _testUrls = config.testUrls ?? _testUrls;
            
            if (_enableLogging)
            {
                Debug.Log("[ConnectionMonitor] 监控配置已更新");
            }
        }

        #endregion

        #region 监控协程

        private IEnumerator MonitoringCoroutine()
        {
            while (_isMonitoring)
            {
                try
                {
                    // 更新网络可达性状态
                    UpdateReachabilityStatus();
                    
                    // 执行连接测试
                    yield return StartCoroutine(PerformMonitoringTests());
                    
                    // 更新性能指标
                    UpdatePerformanceMetrics();
                    
                    // 检查告警条件
                    CheckAlertConditions();
                    
                    // 趋势分析
                    if (_enableTrendAnalysis)
                    {
                        PerformTrendAnalysis();
                    }
                }
                catch (Exception ex)
                {
                    if (_enableLogging)
                    {
                        Debug.LogError($"[ConnectionMonitor] 监控过程中发生错误: {ex.Message}");
                    }
                }
                
                yield return new WaitForSeconds(_monitoringInterval);
            }
        }

        private IEnumerator PerformMonitoringTests()
        {
            if (_testUrls == null || _testUrls.Length == 0)
            {
                yield break;
            }
            
            var results = new List<ConnectionTestResult>();
            var testTasks = new List<Task<ConnectionTestResult>>();
            
            // 执行连接测试
            foreach (var url in _testUrls)
            {
                var task = PerformConnectionTest(url);
                testTasks.Add(task);
                
                // 限制并发数量
                if (testTasks.Count >= _maxConcurrentTests)
                {
                    yield return new WaitUntil(() => testTasks.TrueForAll(t => t.IsCompleted));
                    
                    foreach (var completedTask in testTasks)
                    {
                        if (completedTask.IsCompletedSuccessfully)
                        {
                            results.Add(completedTask.Result);
                        }
                    }
                    
                    testTasks.Clear();
                }
            }
            
            // 等待剩余任务完成
            if (testTasks.Count > 0)
            {
                yield return new WaitUntil(() => testTasks.TrueForAll(t => t.IsCompleted));
                
                foreach (var completedTask in testTasks)
                {
                    if (completedTask.IsCompletedSuccessfully)
                    {
                        results.Add(completedTask.Result);
                    }
                }
            }
            
            // 处理测试结果
            ProcessTestResults(results);
        }

        #endregion

        #region 连接测试

        private async Task<ConnectionTestResult> PerformConnectionTest(string url)
        {
            var testId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;
            
            try
            {
                var session = new ConnectionTestSession
                {
                    Id = testId,
                    Url = url,
                    StartTime = startTime
                };
                
                _activeSessions[testId] = session;
                
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(_connectionTimeout);
                    
                    var response = await client.GetAsync(url);
                    var endTime = DateTime.UtcNow;
                    var latency = (int)(endTime - startTime).TotalMilliseconds;
                    
                    var result = new ConnectionTestResult
                    {
                        Id = testId,
                        Url = url,
                        IsSuccessful = response.IsSuccessStatusCode,
                        LatencyMs = latency,
                        StatusCode = (int)response.StatusCode,
                        TestTime = startTime,
                        Duration = endTime - startTime,
                        ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}"
                    };
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                var endTime = DateTime.UtcNow;
                
                return new ConnectionTestResult
                {
                    Id = testId,
                    Url = url,
                    IsSuccessful = false,
                    LatencyMs = -1,
                    StatusCode = 0,
                    TestTime = startTime,
                    Duration = endTime - startTime,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                _activeSessions.Remove(testId);
            }
        }

        private async Task<ConnectionTestResult> TestWithSemaphore(string url, System.Threading.SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                return await PerformConnectionTest(url);
            }
            finally
            {
                semaphore.Release();
            }
        }

        #endregion

        #region 状态更新

        private void UpdateReachabilityStatus()
        {
            var currentReachability = Application.internetReachability;
            
            if (currentReachability != _lastReachability)
            {
                _lastReachability = currentReachability;
                
                var newStatus = ConvertReachabilityToStatus(currentReachability);
                UpdateConnectionStatus(newStatus);
                
                if (_enableLogging)
                {
                    Debug.Log($"[ConnectionMonitor] 网络可达性变更: {currentReachability} -> {newStatus}");
                }
            }
        }

        private ConnectionStatus ConvertReachabilityToStatus(NetworkReachability reachability)
        {
            switch (reachability)
            {
                case NetworkReachability.NotReachable:
                    return ConnectionStatus.Disconnected;
                case NetworkReachability.ReachableViaCarrierDataNetwork:
                    return ConnectionStatus.ConnectedMobile;
                case NetworkReachability.ReachableViaLocalAreaNetwork:
                    return ConnectionStatus.ConnectedWifi;
                default:
                    return ConnectionStatus.Unknown;
            }
        }

        private void UpdateConnectionStatus(ConnectionStatus newStatus)
        {
            if (_currentStatus != newStatus)
            {
                var oldStatus = _currentStatus;
                _currentStatus = newStatus;
                
                OnConnectionStatusChanged?.Invoke(newStatus);
                
                if (_enableLogging)
                {
                    Debug.Log($"[ConnectionMonitor] 连接状态变更: {oldStatus} -> {newStatus}");
                }
            }
        }

        private void UpdateConnectionQuality(ConnectionQuality newQuality)
        {
            if (_currentQuality != newQuality)
            {
                var oldQuality = _currentQuality;
                _currentQuality = newQuality;
                
                OnConnectionQualityChanged?.Invoke(newQuality);
                
                if (_enableLogging)
                {
                    Debug.Log($"[ConnectionMonitor] 连接质量变更: {oldQuality} -> {newQuality}");
                }
            }
        }

        #endregion

        #region 性能指标

        private void UpdatePerformanceMetrics()
        {
            var metrics = new NetworkPerformanceMetrics
            {
                Timestamp = DateTime.UtcNow,
                ConnectionStatus = _currentStatus,
                ConnectionQuality = _currentQuality
            };
            
            // 从测试历史计算指标
            var recentTests = GetRecentTests(TimeSpan.FromMinutes(5));
            
            if (recentTests.Length > 0)
            {
                var successfulTests = recentTests.Where(t => t.IsSuccessful).ToArray();
                
                metrics.SuccessRate = (float)successfulTests.Length / recentTests.Length;
                metrics.AverageLatency = successfulTests.Length > 0 ? successfulTests.Average(t => t.LatencyMs) : -1;
                metrics.MinLatency = successfulTests.Length > 0 ? successfulTests.Min(t => t.LatencyMs) : -1;
                metrics.MaxLatency = successfulTests.Length > 0 ? successfulTests.Max(t => t.LatencyMs) : -1;
                metrics.TestCount = recentTests.Length;
                metrics.FailureCount = recentTests.Length - successfulTests.Length;
            }
            
            _currentMetrics = metrics;
            
            // 添加到历史记录
            _metricsHistory.Enqueue(metrics);
            while (_metricsHistory.Count > _maxHistorySize)
            {
                _metricsHistory.Dequeue();
            }
            
            // 更新连接质量
            var quality = CalculateConnectionQuality(metrics);
            UpdateConnectionQuality(quality);
            
            if (_enablePerformanceTracking)
            {
                OnPerformanceMetricsUpdated?.Invoke(metrics);
            }
        }

        private ConnectionQuality CalculateConnectionQuality(NetworkPerformanceMetrics metrics)
        {
            if (metrics.SuccessRate < _minSuccessRateThreshold)
            {
                return ConnectionQuality.Poor;
            }
            
            if (metrics.AverageLatency < 0) // 没有成功的测试
            {
                return ConnectionQuality.Poor;
            }
            
            if (metrics.AverageLatency <= _excellentLatencyThreshold)
            {
                return ConnectionQuality.Excellent;
            }
            else if (metrics.AverageLatency <= _goodLatencyThreshold)
            {
                return ConnectionQuality.Good;
            }
            else if (metrics.AverageLatency <= _poorLatencyThreshold)
            {
                return ConnectionQuality.Fair;
            }
            else
            {
                return ConnectionQuality.Poor;
            }
        }

        private ConnectionQuality CalculateOverallQuality(List<ConnectionTestResult> results)
        {
            if (results == null || results.Count == 0)
            {
                return ConnectionQuality.Unknown;
            }
            
            var successfulResults = results.Where(r => r.IsSuccessful).ToList();
            var successRate = (float)successfulResults.Count / results.Count;
            
            if (successRate < _minSuccessRateThreshold)
            {
                return ConnectionQuality.Poor;
            }
            
            if (successfulResults.Count == 0)
            {
                return ConnectionQuality.Poor;
            }
            
            var averageLatency = successfulResults.Average(r => r.LatencyMs);
            
            if (averageLatency <= _excellentLatencyThreshold)
            {
                return ConnectionQuality.Excellent;
            }
            else if (averageLatency <= _goodLatencyThreshold)
            {
                return ConnectionQuality.Good;
            }
            else if (averageLatency <= _poorLatencyThreshold)
            {
                return ConnectionQuality.Fair;
            }
            else
            {
                return ConnectionQuality.Poor;
            }
        }

        #endregion

        #region 测试结果处理

        private void ProcessTestResults(List<ConnectionTestResult> results)
        {
            foreach (var result in results)
            {
                // 添加到历史记录
                _testHistory.Enqueue(result);
                while (_testHistory.Count > _maxHistorySize)
                {
                    _testHistory.Dequeue();
                }
                
                // 更新连续失败计数
                if (result.IsSuccessful)
                {
                    _consecutiveFailures = 0;
                    _lastSuccessfulTest = result.TestTime;
                }
                else
                {
                    _consecutiveFailures++;
                }
            }
        }

        private ConnectionTestResult[] GetRecentTests(TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.UtcNow - timeSpan;
            return _testHistory.Where(t => t.TestTime >= cutoffTime).ToArray();
        }

        #endregion

        #region 告警检查

        private void CheckAlertConditions()
        {
            if (!_enableAlerts)
            {
                return;
            }
            
            // 检查连续失败
            CheckConsecutiveFailures();
            
            // 检查延迟恶化
            CheckLatencyDegradation();
            
            // 检查成功率下降
            CheckSuccessRateDrop();
            
            // 检查连接中断
            CheckConnectionInterruption();
        }

        private void CheckConsecutiveFailures()
        {
            if (_consecutiveFailures >= _consecutiveFailureThreshold)
            {
                var alertId = "consecutive_failures";
                
                if (!HasActiveAlert(alertId))
                {
                    var alert = new ConnectionAlert
                    {
                        Id = alertId,
                        Type = ConnectionAlertType.ConsecutiveFailures,
                        Severity = AlertSeverity.High,
                        Title = "连续连接失败",
                        Message = $"连续 {_consecutiveFailures} 次连接测试失败",
                        Timestamp = DateTime.UtcNow,
                        Data = new Dictionary<string, object>
                        {
                            ["consecutiveFailures"] = _consecutiveFailures,
                            ["threshold"] = _consecutiveFailureThreshold
                        }
                    };
                    
                    AddAlert(alert);
                }
            }
        }

        private void CheckLatencyDegradation()
        {
            if (_metricsHistory.Count < 2)
            {
                return;
            }
            
            var recent = _metricsHistory.ToArray();
            var currentLatency = recent[recent.Length - 1].AverageLatency;
            var previousLatency = recent[recent.Length - 2].AverageLatency;
            
            if (currentLatency > 0 && previousLatency > 0)
            {
                var degradationFactor = currentLatency / previousLatency;
                
                if (degradationFactor >= _latencyDegradationThreshold)
                {
                    var alertId = "latency_degradation";
                    
                    if (!HasActiveAlert(alertId))
                    {
                        var alert = new ConnectionAlert
                        {
                            Id = alertId,
                            Type = ConnectionAlertType.LatencyDegradation,
                            Severity = AlertSeverity.Medium,
                            Title = "延迟显著增加",
                            Message = $"延迟从 {previousLatency:F0}ms 增加到 {currentLatency:F0}ms",
                            Timestamp = DateTime.UtcNow,
                            Data = new Dictionary<string, object>
                            {
                                ["currentLatency"] = currentLatency,
                                ["previousLatency"] = previousLatency,
                                ["degradationFactor"] = degradationFactor
                            }
                        };
                        
                        AddAlert(alert);
                    }
                }
            }
        }

        private void CheckSuccessRateDrop()
        {
            if (_metricsHistory.Count < 2)
            {
                return;
            }
            
            var recent = _metricsHistory.ToArray();
            var currentSuccessRate = recent[recent.Length - 1].SuccessRate;
            var previousSuccessRate = recent[recent.Length - 2].SuccessRate;
            
            var drop = previousSuccessRate - currentSuccessRate;
            
            if (drop >= _successRateDropThreshold)
            {
                var alertId = "success_rate_drop";
                
                if (!HasActiveAlert(alertId))
                {
                    var alert = new ConnectionAlert
                    {
                        Id = alertId,
                        Type = ConnectionAlertType.SuccessRateDrop,
                        Severity = AlertSeverity.High,
                        Title = "成功率急剧下降",
                        Message = $"成功率从 {previousSuccessRate:P} 下降到 {currentSuccessRate:P}",
                        Timestamp = DateTime.UtcNow,
                        Data = new Dictionary<string, object>
                        {
                            ["currentSuccessRate"] = currentSuccessRate,
                            ["previousSuccessRate"] = previousSuccessRate,
                            ["drop"] = drop
                        }
                    };
                    
                    AddAlert(alert);
                }
            }
        }

        private void CheckConnectionInterruption()
        {
            if (_currentStatus == ConnectionStatus.Disconnected)
            {
                var alertId = "connection_interrupted";
                
                if (!HasActiveAlert(alertId))
                {
                    var alert = new ConnectionAlert
                    {
                        Id = alertId,
                        Type = ConnectionAlertType.ConnectionLost,
                        Severity = AlertSeverity.Critical,
                        Title = "网络连接中断",
                        Message = "检测到网络连接完全中断",
                        Timestamp = DateTime.UtcNow,
                        Data = new Dictionary<string, object>
                        {
                            ["lastSuccessfulTest"] = _lastSuccessfulTest,
                            ["networkReachability"] = Application.internetReachability.ToString()
                        }
                    };
                    
                    AddAlert(alert);
                }
            }
            else
            {
                // 连接恢复，清除连接中断告警
                ClearAlert("connection_interrupted");
            }
        }

        private bool HasActiveAlert(string alertId)
        {
            return _activeAlerts.Any(a => a.Id == alertId);
        }

        private void AddAlert(ConnectionAlert alert)
        {
            _activeAlerts.Add(alert);
            OnConnectionAlert?.Invoke(alert);
            
            if (_enableLogging)
            {
                Debug.LogWarning($"[ConnectionMonitor] 新告警: {alert.Title} - {alert.Message}");
            }
        }

        #endregion

        #region 趋势分析

        private void PerformTrendAnalysis()
        {
            if (_trendAnalyzer == null || _metricsHistory.Count < 5)
            {
                return;
            }
            
            try
            {
                var analysis = _trendAnalyzer.AnalyzeTrends(_metricsHistory.ToArray());
                
                // 根据趋势分析结果生成预警
                if (analysis.LatencyTrend == TrendDirection.Increasing && analysis.LatencyTrendStrength > 0.7f)
                {
                    var alertId = "latency_trend_warning";
                    
                    if (!HasActiveAlert(alertId))
                    {
                        var alert = new ConnectionAlert
                        {
                            Id = alertId,
                            Type = ConnectionAlertType.TrendWarning,
                            Severity = AlertSeverity.Low,
                            Title = "延迟上升趋势",
                            Message = $"检测到延迟持续上升趋势，趋势强度: {analysis.LatencyTrendStrength:P}",
                            Timestamp = DateTime.UtcNow,
                            Data = new Dictionary<string, object>
                            {
                                ["trendStrength"] = analysis.LatencyTrendStrength,
                                ["trendDirection"] = analysis.LatencyTrend.ToString()
                            }
                        };
                        
                        AddAlert(alert);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    Debug.LogError($"[ConnectionMonitor] 趋势分析失败: {ex.Message}");
                }
            }
        }

        #endregion

        #region 报告生成

        private ConnectivityReportSummary GenerateReportSummary()
        {
            var recentTests = GetRecentTests(TimeSpan.FromHours(1));
            var recentMetrics = _metricsHistory.Where(m => DateTime.UtcNow - m.Timestamp < TimeSpan.FromHours(1)).ToArray();
            
            return new ConnectivityReportSummary
            {
                OverallHealth = CalculateOverallHealth(),
                TotalTests = recentTests.Length,
                SuccessfulTests = recentTests.Count(t => t.IsSuccessful),
                AverageLatency = recentTests.Where(t => t.IsSuccessful).DefaultIfEmpty().Average(t => t?.LatencyMs ?? 0),
                WorstLatency = recentTests.Where(t => t.IsSuccessful).DefaultIfEmpty().Max(t => t?.LatencyMs ?? 0),
                BestLatency = recentTests.Where(t => t.IsSuccessful).DefaultIfEmpty().Min(t => t?.LatencyMs ?? 0),
                UptimePercentage = recentMetrics.Length > 0 ? recentMetrics.Average(m => m.SuccessRate) : 0f,
                ActiveAlertsCount = _activeAlerts.Count,
                MonitoringDuration = DateTime.UtcNow - (_metricsHistory.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow)
            };
        }

        private HealthStatus CalculateOverallHealth()
        {
            if (_activeAlerts.Any(a => a.Severity == AlertSeverity.Critical))
            {
                return HealthStatus.Critical;
            }
            
            if (_activeAlerts.Any(a => a.Severity == AlertSeverity.High))
            {
                return HealthStatus.Poor;
            }
            
            if (_currentQuality == ConnectionQuality.Poor)
            {
                return HealthStatus.Poor;
            }
            
            if (_currentQuality == ConnectionQuality.Fair || _activeAlerts.Any(a => a.Severity == AlertSeverity.Medium))
            {
                return HealthStatus.Fair;
            }
            
            if (_currentQuality == ConnectionQuality.Good)
            {
                return HealthStatus.Good;
            }
            
            return HealthStatus.Excellent;
        }

        #endregion

        #region 清理

        private void Cleanup()
        {
            StopMonitoring();
            
            _metricsHistory?.Clear();
            _testHistory?.Clear();
            _activeAlerts?.Clear();
            _activeSessions?.Clear();
            
            if (_enableLogging)
            {
                Debug.Log("[ConnectionMonitor] 资源清理完成");
            }
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectionStatus
    {
        Unknown,
        Disconnected,
        Connecting,
        ConnectedWifi,
        ConnectedMobile,
        ConnectedEthernet
    }

    /// <summary>
    /// 连接质量
    /// </summary>
    public enum ConnectionQuality
    {
        Unknown,
        Poor,      // 较差
        Fair,      // 一般
        Good,      // 良好
        Excellent  // 优秀
    }

    /// <summary>
    /// 健康状态
    /// </summary>
    public enum HealthStatus
    {
        Critical,  // 严重
        Poor,      // 较差
        Fair,      // 一般
        Good,      // 良好
        Excellent  // 优秀
    }

    /// <summary>
    /// 网络性能指标
    /// </summary>
    [System.Serializable]
    public class NetworkPerformanceMetrics
    {
        public DateTime Timestamp { get; set; }
        public ConnectionStatus ConnectionStatus { get; set; }
        public ConnectionQuality ConnectionQuality { get; set; }
        public float SuccessRate { get; set; }       // 成功率 (0-1)
        public float AverageLatency { get; set; }    // 平均延迟(ms)
        public float MinLatency { get; set; }        // 最小延迟(ms)
        public float MaxLatency { get; set; }        // 最大延迟(ms)
        public int TestCount { get; set; }           // 测试次数
        public int FailureCount { get; set; }        // 失败次数
    }

    /// <summary>
    /// 连接测试结果
    /// </summary>
    [System.Serializable]
    public class ConnectionTestResult
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public bool IsSuccessful { get; set; }
        public int LatencyMs { get; set; }
        public int StatusCode { get; set; }
        public DateTime TestTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 连接测试会话
    /// </summary>
    public class ConnectionTestSession
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public DateTime StartTime { get; set; }
    }

    /// <summary>
    /// 连接测试摘要
    /// </summary>
    [System.Serializable]
    public class ConnectionTestSummary
    {
        public List<ConnectionTestResult> Results { get; set; }
        public DateTime TestTime { get; set; }
        public int SuccessfulTests { get; set; }
        public int TotalTests { get; set; }
        public double AverageLatency { get; set; }
        public ConnectionQuality OverallQuality { get; set; }
    }

    /// <summary>
    /// 连接告警
    /// </summary>
    [System.Serializable]
    public class ConnectionAlert
    {
        public string Id { get; set; }
        public ConnectionAlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    /// <summary>
    /// 连接告警类型
    /// </summary>
    public enum ConnectionAlertType
    {
        ConsecutiveFailures,   // 连续失败
        LatencyDegradation,    // 延迟恶化
        SuccessRateDrop,       // 成功率下降
        ConnectionLost,        // 连接丢失
        QualityDegradation,    // 质量下降
        TrendWarning          // 趋势预警
    }

    /// <summary>
    /// 告警严重程度
    /// </summary>
    public enum AlertSeverity
    {
        Low,       // 低
        Medium,    // 中
        High,      // 高
        Critical   // 严重
    }

    /// <summary>
    /// 连接报告
    /// </summary>
    [System.Serializable]
    public class ConnectivityReport
    {
        public DateTime GeneratedAt { get; set; }
        public ConnectionStatus CurrentStatus { get; set; }
        public ConnectionQuality CurrentQuality { get; set; }
        public NetworkPerformanceMetrics CurrentMetrics { get; set; }
        public NetworkPerformanceMetrics[] MetricsHistory { get; set; }
        public ConnectionTestResult[] TestHistory { get; set; }
        public ConnectionAlert[] ActiveAlerts { get; set; }
        public ConnectivityReportSummary Summary { get; set; }
        public TrendAnalysisResult TrendAnalysis { get; set; }
    }

    /// <summary>
    /// 连接报告摘要
    /// </summary>
    [System.Serializable]
    public class ConnectivityReportSummary
    {
        public HealthStatus OverallHealth { get; set; }
        public int TotalTests { get; set; }
        public int SuccessfulTests { get; set; }
        public double AverageLatency { get; set; }
        public double WorstLatency { get; set; }
        public double BestLatency { get; set; }
        public float UptimePercentage { get; set; }
        public int ActiveAlertsCount { get; set; }
        public TimeSpan MonitoringDuration { get; set; }
    }

    /// <summary>
    /// 连接监控配置
    /// </summary>
    [System.Serializable]
    public class ConnectionMonitorConfig
    {
        public bool enableMonitoring = true;
        public float monitoringInterval = 5f;
        public int connectionTimeout = 5000;
        public bool enableAlerts = true;
        public int consecutiveFailureThreshold = 3;
        public string[] testUrls;
    }

    #endregion

    #region 趋势分析

    /// <summary>
    /// 趋势分析器
    /// </summary>
    public class TrendAnalyzer
    {
        public TrendAnalysisResult AnalyzeTrends(NetworkPerformanceMetrics[] metrics)
        {
            if (metrics == null || metrics.Length < 3)
            {
                return new TrendAnalysisResult();
            }

            var result = new TrendAnalysisResult();

            // 分析延迟趋势
            var latencies = metrics.Where(m => m.AverageLatency > 0).Select(m => m.AverageLatency).ToArray();
            if (latencies.Length >= 3)
            {
                result.LatencyTrend = CalculateTrend(latencies);
                result.LatencyTrendStrength = CalculateTrendStrength(latencies);
            }

            // 分析成功率趋势
            var successRates = metrics.Select(m => m.SuccessRate).ToArray();
            result.SuccessRateTrend = CalculateTrend(successRates);
            result.SuccessRateTrendStrength = CalculateTrendStrength(successRates);

            // 预测
            result.Prediction = GeneratePrediction(metrics);

            return result;
        }

        private TrendDirection CalculateTrend(float[] values)
        {
            if (values.Length < 2)
                return TrendDirection.Stable;

            var increases = 0;
            var decreases = 0;

            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > values[i - 1])
                    increases++;
                else if (values[i] < values[i - 1])
                    decreases++;
            }

            if (increases > decreases)
                return TrendDirection.Increasing;
            else if (decreases > increases)
                return TrendDirection.Decreasing;
            else
                return TrendDirection.Stable;
        }

        private float CalculateTrendStrength(float[] values)
        {
            if (values.Length < 2)
                return 0f;

            var totalChange = 0f;
            var maxPossibleChange = 0f;

            for (int i = 1; i < values.Length; i++)
            {
                totalChange += Math.Abs(values[i] - values[i - 1]);
                maxPossibleChange += Math.Max(values[i], values[i - 1]);
            }

            return maxPossibleChange > 0 ? totalChange / maxPossibleChange : 0f;
        }

        private string GeneratePrediction(NetworkPerformanceMetrics[] metrics)
        {
            var recentMetrics = metrics.TakeLast(5).ToArray();
            
            if (recentMetrics.Length < 3)
                return "数据不足，无法预测";

            var avgSuccessRate = recentMetrics.Average(m => m.SuccessRate);
            var avgLatency = recentMetrics.Where(m => m.AverageLatency > 0).Average(m => m.AverageLatency);

            if (avgSuccessRate < 0.7)
                return "连接质量可能继续恶化，建议检查网络环境";
            else if (avgLatency > 300)
                return "延迟较高，可能影响用户体验";
            else if (avgSuccessRate > 0.9 && avgLatency < 100)
                return "连接状态良好，预计保持稳定";
            else
                return "连接状态正常，建议继续监控";
        }
    }

    /// <summary>
    /// 趋势分析结果
    /// </summary>
    [System.Serializable]
    public class TrendAnalysisResult
    {
        public TrendDirection LatencyTrend { get; set; } = TrendDirection.Stable;
        public float LatencyTrendStrength { get; set; } = 0f;
        public TrendDirection SuccessRateTrend { get; set; } = TrendDirection.Stable;
        public float SuccessRateTrendStrength { get; set; } = 0f;
        public string Prediction { get; set; } = "";
    }

    /// <summary>
    /// 趋势方向
    /// </summary>
    public enum TrendDirection
    {
        Decreasing,  // 下降
        Stable,      // 稳定
        Increasing   // 上升
    }

    #endregion
}