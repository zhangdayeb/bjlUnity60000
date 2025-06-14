// ================================================================================================
// 性能监控器 - PerformanceMonitor.cs
// 用途：实时监控游戏性能指标，提供性能分析和优化建议
// ================================================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace BaccaratGame.Utils
{
    /// <summary>
    /// 性能指标类型
    /// </summary>
    public enum PerformanceMetricType
    {
        FrameRate,          // 帧率
        FrameTime,          // 帧时间
        Memory,             // 内存使用
        CPU,                // CPU使用率
        GPU,                // GPU使用率
        DrawCalls,          // 绘制调用
        Triangles,          // 三角形数量
        Vertices,           // 顶点数量
        Batches,            // 批处理数量
        AudioSources,       // 音频源数量
        NetworkLatency,     // 网络延迟
        LoadTime           // 加载时间
    }

    /// <summary>
    /// 性能警告级别
    /// </summary>
    public enum PerformanceWarningLevel
    {
        Normal,     // 正常
        Warning,    // 警告
        Critical,   // 严重
        Emergency   // 紧急
    }

    /// <summary>
    /// 性能指标数据
    /// </summary>
    [System.Serializable]
    public class PerformanceMetric
    {
        public PerformanceMetricType type;
        public float currentValue;
        public float averageValue;
        public float minValue;
        public float maxValue;
        public float warningThreshold;
        public float criticalThreshold;
        public PerformanceWarningLevel warningLevel;
        public List<float> history = new List<float>();
        public DateTime lastUpdated;

        public PerformanceMetric(PerformanceMetricType metricType)
        {
            type = metricType;
            Reset();
        }

        public void Reset()
        {
            currentValue = 0f;
            averageValue = 0f;
            minValue = float.MaxValue;
            maxValue = float.MinValue;
            history.Clear();
            lastUpdated = DateTime.Now;
            warningLevel = PerformanceWarningLevel.Normal;
        }

        public void UpdateValue(float newValue)
        {
            currentValue = newValue;
            
            if (newValue < minValue) minValue = newValue;
            if (newValue > maxValue) maxValue = newValue;

            history.Add(newValue);
            if (history.Count > 100) // 保持最近100个数据点
            {
                history.RemoveAt(0);
            }

            averageValue = history.Average();
            lastUpdated = DateTime.Now;

            // 更新警告级别
            UpdateWarningLevel();
        }

        private void UpdateWarningLevel()
        {
            if (type == PerformanceMetricType.FrameRate)
            {
                // 帧率：值越低越危险
                if (currentValue < criticalThreshold)
                    warningLevel = PerformanceWarningLevel.Critical;
                else if (currentValue < warningThreshold)
                    warningLevel = PerformanceWarningLevel.Warning;
                else
                    warningLevel = PerformanceWarningLevel.Normal;
            }
            else
            {
                // 其他指标：值越高越危险
                if (currentValue > criticalThreshold)
                    warningLevel = PerformanceWarningLevel.Critical;
                else if (currentValue > warningThreshold)
                    warningLevel = PerformanceWarningLevel.Warning;
                else
                    warningLevel = PerformanceWarningLevel.Normal;
            }
        }

        public float GetTrend()
        {
            if (history.Count < 10) return 0f;
            
            var recent = history.TakeLast(10).ToList();
            var older = history.Take(10).ToList();
            
            return recent.Average() - older.Average();
        }
    }

    /// <summary>
    /// 性能报告数据
    /// </summary>
    [System.Serializable]
    public class PerformanceReport
    {
        public DateTime reportTime;
        public float duration;
        public Dictionary<PerformanceMetricType, PerformanceMetric> metrics;
        public List<string> warnings;
        public List<string> recommendations;
        public float overallScore;

        public PerformanceReport()
        {
            reportTime = DateTime.Now;
            metrics = new Dictionary<PerformanceMetricType, PerformanceMetric>();
            warnings = new List<string>();
            recommendations = new List<string>();
        }
    }

    /// <summary>
    /// 性能监控器 - 实时监控和分析游戏性能
    /// 对应JavaScript项目中的性能监控和优化建议功能
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        #region Singleton Pattern
        private static PerformanceMonitor _instance;
        public static PerformanceMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PerformanceMonitor>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PerformanceMonitor");
                        _instance = go.AddComponent<PerformanceMonitor>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        [Header("🔍 Monitoring Settings")]
        [Tooltip("是否启用性能监控")]
        public bool enableMonitoring = true;
        
        [Tooltip("监控更新间隔（秒）")]
        [Range(0.1f, 5f)]
        public float updateInterval = 1f;
        
        [Tooltip("是否显示实时性能信息")]
        public bool showRealTimeInfo = true;

        [Header("⚠️ Warning Thresholds")]
        [Tooltip("帧率警告阈值")]
        [Range(15, 60)]
        public float fpsWarningThreshold = 30f;
        
        [Tooltip("帧率严重阈值")]
        [Range(10, 30)]
        public float fpsCriticalThreshold = 20f;
        
        [Tooltip("内存警告阈值（MB）")]
        [Range(50, 1000)]
        public float memoryWarningThreshold = 200f;
        
        [Tooltip("内存严重阈值（MB）")]
        [Range(100, 2000)]
        public float memoryCriticalThreshold = 500f;

        [Header("📊 Display Settings")]
        [Tooltip("是否启用性能UI显示")]
        public bool enablePerformanceUI = false;
        
        [Tooltip("UI显示位置")]
        public TextAnchor uiAnchor = TextAnchor.UpperLeft;
        
        [Tooltip("UI字体大小")]
        [Range(12, 24)]
        public int uiFontSize = 14;

        [Header("📈 Reporting Settings")]
        [Tooltip("是否启用性能报告")]
        public bool enableReporting = true;
        
        [Tooltip("报告生成间隔（秒）")]
        [Range(30, 300)]
        public float reportInterval = 60f;
        
        [Tooltip("最大保存报告数量")]
        [Range(5, 50)]
        public int maxReports = 20;

        // 性能指标字典
        private Dictionary<PerformanceMetricType, PerformanceMetric> metrics = 
            new Dictionary<PerformanceMetricType, PerformanceMetric>();

        // 私有变量
        private bool isMonitoring = false;
        private Coroutine monitoringCoroutine;
        private Coroutine reportingCoroutine;
        private List<PerformanceReport> reports = new List<PerformanceReport>();
        private float lastFrameTime = 0f;
        private int frameCount = 0;
        private float startTime = 0f;

        // 渲染统计
        private int lastDrawCalls = 0;
        private int lastTriangles = 0;
        private int lastVertices = 0;
        private int lastBatches = 0;

        // UI相关
        private GUIStyle uiStyle;
        private bool showUI = false;

        // 事件回调
        public System.Action<PerformanceMetric> OnMetricUpdated;
        public System.Action<PerformanceWarningLevel, string> OnPerformanceWarning;
        public System.Action<PerformanceReport> OnReportGenerated;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeMetrics();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (enableMonitoring)
            {
                StartMonitoring();
            }
        }

        private void Update()
        {
            if (isMonitoring)
            {
                UpdateFrameMetrics();
            }
        }

        private void OnGUI()
        {
            if (showUI && enablePerformanceUI)
            {
                DrawPerformanceUI();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化性能指标
        /// </summary>
        private void InitializeMetrics()
        {
            // 创建所有性能指标
            foreach (PerformanceMetricType metricType in Enum.GetValues(typeof(PerformanceMetricType)))
            {
                var metric = new PerformanceMetric(metricType);
                SetMetricThresholds(metric);
                metrics[metricType] = metric;
            }

            startTime = Time.realtimeSinceStartup;
            Debug.Log("[PerformanceMonitor] 性能指标初始化完成");
        }

        /// <summary>
        /// 设置指标阈值
        /// </summary>
        private void SetMetricThresholds(PerformanceMetric metric)
        {
            switch (metric.type)
            {
                case PerformanceMetricType.FrameRate:
                    metric.warningThreshold = fpsWarningThreshold;
                    metric.criticalThreshold = fpsCriticalThreshold;
                    break;
                case PerformanceMetricType.Memory:
                    metric.warningThreshold = memoryWarningThreshold;
                    metric.criticalThreshold = memoryCriticalThreshold;
                    break;
                case PerformanceMetricType.FrameTime:
                    metric.warningThreshold = 33.33f; // 30 FPS
                    metric.criticalThreshold = 50f;   // 20 FPS
                    break;
                case PerformanceMetricType.DrawCalls:
                    metric.warningThreshold = 100f;
                    metric.criticalThreshold = 200f;
                    break;
                case PerformanceMetricType.Triangles:
                    metric.warningThreshold = 50000f;
                    metric.criticalThreshold = 100000f;
                    break;
                case PerformanceMetricType.AudioSources:
                    metric.warningThreshold = 10f;
                    metric.criticalThreshold = 20f;
                    break;
                case PerformanceMetricType.NetworkLatency:
                    metric.warningThreshold = 200f; // ms
                    metric.criticalThreshold = 500f;
                    break;
            }
        }

        #endregion

        #region Monitoring Control

        /// <summary>
        /// 开始性能监控
        /// </summary>
        public void StartMonitoring()
        {
            if (isMonitoring)
            {
                Debug.LogWarning("[PerformanceMonitor] 性能监控已在运行");
                return;
            }

            isMonitoring = true;
            showUI = enablePerformanceUI;
            
            // 启动监控协程
            if (monitoringCoroutine != null)
            {
                StopCoroutine(monitoringCoroutine);
            }
            monitoringCoroutine = StartCoroutine(MonitoringLoop());

            // 启动报告协程
            if (enableReporting)
            {
                if (reportingCoroutine != null)
                {
                    StopCoroutine(reportingCoroutine);
                }
                reportingCoroutine = StartCoroutine(ReportingLoop());
            }

            Debug.Log("[PerformanceMonitor] 性能监控已启动");
        }

        /// <summary>
        /// 停止性能监控
        /// </summary>
        public void StopMonitoring()
        {
            isMonitoring = false;
            showUI = false;

            if (monitoringCoroutine != null)
            {
                StopCoroutine(monitoringCoroutine);
                monitoringCoroutine = null;
            }

            if (reportingCoroutine != null)
            {
                StopCoroutine(reportingCoroutine);
                reportingCoroutine = null;
            }

            Debug.Log("[PerformanceMonitor] 性能监控已停止");
        }

        /// <summary>
        /// 切换监控状态
        /// </summary>
        public void ToggleMonitoring()
        {
            if (isMonitoring)
            {
                StopMonitoring();
            }
            else
            {
                StartMonitoring();
            }
        }

        #endregion

        #region Monitoring Loop

        /// <summary>
        /// 监控循环
        /// </summary>
        private IEnumerator MonitoringLoop()
        {
            while (isMonitoring)
            {
                yield return new WaitForSeconds(updateInterval);
                UpdateAllMetrics();
            }
        }

        /// <summary>
        /// 更新所有性能指标
        /// </summary>
        private void UpdateAllMetrics()
        {
            try
            {
                UpdateMemoryMetrics();
                UpdateRenderingMetrics();
                UpdateAudioMetrics();
                UpdateNetworkMetrics();
                CheckWarnings();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerformanceMonitor] 更新性能指标失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新帧相关指标
        /// </summary>
        private void UpdateFrameMetrics()
        {
            frameCount++;
            float currentTime = Time.realtimeSinceStartup;

            // 计算帧率
            if (currentTime - lastFrameTime >= 1f)
            {
                float fps = frameCount / (currentTime - lastFrameTime);
                UpdateMetric(PerformanceMetricType.FrameRate, fps);

                float frameTime = (currentTime - lastFrameTime) / frameCount * 1000f; // ms
                UpdateMetric(PerformanceMetricType.FrameTime, frameTime);

                frameCount = 0;
                lastFrameTime = currentTime;
            }
        }

        /// <summary>
        /// 更新内存指标
        /// </summary>
        private void UpdateMemoryMetrics()
        {
            // 总内存使用量
            long totalMemory = Profiler.GetTotalAllocatedMemory(false) / (1024 * 1024); // MB
            UpdateMetric(PerformanceMetricType.Memory, totalMemory);
        }

        /// <summary>
        /// 更新渲染指标
        /// </summary>
        private void UpdateRenderingMetrics()
        {
            // 由于Unity在运行时无法直接获取渲染统计，这里使用估算值
            // 在实际项目中可以通过Profiler API或自定义统计来获取准确数据
            
            UpdateMetric(PerformanceMetricType.DrawCalls, UnityEngine.Profiling.Profiler.enabled ? lastDrawCalls : EstimateDrawCalls());
            UpdateMetric(PerformanceMetricType.Triangles, EstimateTriangles());
            UpdateMetric(PerformanceMetricType.Vertices, EstimateVertices());
            UpdateMetric(PerformanceMetricType.Batches, EstimateBatches());
        }

        /// <summary>
        /// 更新音频指标
        /// </summary>
        private void UpdateAudioMetrics()
        {
            int activeAudioSources = FindObjectsOfType<AudioSource>().Count(source => source.isPlaying);
            UpdateMetric(PerformanceMetricType.AudioSources, activeAudioSources);
        }

        /// <summary>
        /// 更新网络指标
        /// </summary>
        private void UpdateNetworkMetrics()
        {
            // 这里可以集成网络延迟检测
            // 暂时使用模拟值
            float simulatedLatency = UnityEngine.Random.Range(50f, 150f);
            UpdateMetric(PerformanceMetricType.NetworkLatency, simulatedLatency);
        }

        /// <summary>
        /// 更新单个指标
        /// </summary>
        private void UpdateMetric(PerformanceMetricType type, float value)
        {
            if (metrics.TryGetValue(type, out PerformanceMetric metric))
            {
                metric.UpdateValue(value);
                OnMetricUpdated?.Invoke(metric);
            }
        }

        #endregion

        #region Performance Estimation

        /// <summary>
        /// 估算绘制调用数量
        /// </summary>
        private float EstimateDrawCalls()
        {
            int rendererCount = FindObjectsOfType<Renderer>().Length;
            return rendererCount * 0.8f; // 估算值
        }

        /// <summary>
        /// 估算三角形数量
        /// </summary>
        private float EstimateTriangles()
        {
            var meshFilters = FindObjectsOfType<MeshFilter>();
            int totalTriangles = 0;
            
            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    totalTriangles += meshFilter.sharedMesh.triangles.Length / 3;
                }
            }
            
            return totalTriangles;
        }

        /// <summary>
        /// 估算顶点数量
        /// </summary>
        private float EstimateVertices()
        {
            var meshFilters = FindObjectsOfType<MeshFilter>();
            int totalVertices = 0;
            
            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    totalVertices += meshFilter.sharedMesh.vertexCount;
                }
            }
            
            return totalVertices;
        }

        /// <summary>
        /// 估算批处理数量
        /// </summary>
        private float EstimateBatches()
        {
            return EstimateDrawCalls() * 0.6f; // 估算批处理效率
        }

        #endregion

        #region Warning System

        /// <summary>
        /// 检查性能警告
        /// </summary>
        private void CheckWarnings()
        {
            foreach (var kvp in metrics)
            {
                var metric = kvp.Value;
                
                if (metric.warningLevel == PerformanceWarningLevel.Critical ||
                    metric.warningLevel == PerformanceWarningLevel.Warning)
                {
                    string message = GenerateWarningMessage(metric);
                    OnPerformanceWarning?.Invoke(metric.warningLevel, message);
                }
            }
        }

        /// <summary>
        /// 生成警告消息
        /// </summary>
        private string GenerateWarningMessage(PerformanceMetric metric)
        {
            string levelText = metric.warningLevel == PerformanceWarningLevel.Critical ? "严重" : "警告";
            return $"{levelText}: {metric.type} 当前值 {metric.currentValue:F1} 超过阈值";
        }

        #endregion

        #region Reporting System

        /// <summary>
        /// 报告生成循环
        /// </summary>
        private IEnumerator ReportingLoop()
        {
            while (isMonitoring)
            {
                yield return new WaitForSeconds(reportInterval);
                GeneratePerformanceReport();
            }
        }

        /// <summary>
        /// 生成性能报告
        /// </summary>
        public PerformanceReport GeneratePerformanceReport()
        {
            var report = new PerformanceReport
            {
                reportTime = DateTime.Now,
                duration = Time.realtimeSinceStartup - startTime,
                metrics = new Dictionary<PerformanceMetricType, PerformanceMetric>()
            };

            // 复制当前指标
            foreach (var kvp in metrics)
            {
                report.metrics[kvp.Key] = kvp.Value;
            }

            // 生成警告和建议
            GenerateWarningsAndRecommendations(report);

            // 计算总体评分
            report.overallScore = CalculateOverallScore(report);

            // 保存报告
            reports.Add(report);
            if (reports.Count > maxReports)
            {
                reports.RemoveAt(0);
            }

            OnReportGenerated?.Invoke(report);
            Debug.Log($"[PerformanceMonitor] 性能报告已生成，总体评分: {report.overallScore:F1}");

            return report;
        }

        /// <summary>
        /// 生成警告和建议
        /// </summary>
        private void GenerateWarningsAndRecommendations(PerformanceReport report)
        {
            report.warnings.Clear();
            report.recommendations.Clear();

            foreach (var kvp in report.metrics)
            {
                var metric = kvp.Value;
                
                switch (metric.warningLevel)
                {
                    case PerformanceWarningLevel.Warning:
                        report.warnings.Add($"{metric.type}: {metric.currentValue:F1} (警告)");
                        AddRecommendationForMetric(report, metric);
                        break;
                    case PerformanceWarningLevel.Critical:
                        report.warnings.Add($"{metric.type}: {metric.currentValue:F1} (严重)");
                        AddRecommendationForMetric(report, metric);
                        break;
                }
            }
        }

        /// <summary>
        /// 为指标添加优化建议
        /// </summary>
        private void AddRecommendationForMetric(PerformanceReport report, PerformanceMetric metric)
        {
            switch (metric.type)
            {
                case PerformanceMetricType.FrameRate:
                    report.recommendations.Add("降低渲染质量设置或减少屏幕上的对象数量");
                    break;
                case PerformanceMetricType.Memory:
                    report.recommendations.Add("执行内存清理或减少纹理质量");
                    break;
                case PerformanceMetricType.DrawCalls:
                    report.recommendations.Add("启用批处理或减少材质数量");
                    break;
                case PerformanceMetricType.AudioSources:
                    report.recommendations.Add("停止不必要的音频源或限制并发音频数量");
                    break;
                case PerformanceMetricType.NetworkLatency:
                    report.recommendations.Add("检查网络连接或更换服务器");
                    break;
            }
        }

        /// <summary>
        /// 计算总体评分
        /// </summary>
        private float CalculateOverallScore(PerformanceReport report)
        {
            float totalScore = 0f;
            int metricCount = 0;

            foreach (var kvp in report.metrics)
            {
                var metric = kvp.Value;
                float score = 100f;

                switch (metric.warningLevel)
                {
                    case PerformanceWarningLevel.Normal:
                        score = 100f;
                        break;
                    case PerformanceWarningLevel.Warning:
                        score = 70f;
                        break;
                    case PerformanceWarningLevel.Critical:
                        score = 30f;
                        break;
                    case PerformanceWarningLevel.Emergency:
                        score = 10f;
                        break;
                }

                totalScore += score;
                metricCount++;
            }

            return metricCount > 0 ? totalScore / metricCount : 0f;
        }

        #endregion

        #region UI Display

        /// <summary>
        /// 绘制性能UI
        /// </summary>
        private void DrawPerformanceUI()
        {
            if (uiStyle == null)
            {
                uiStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = uiFontSize,
                    normal = { textColor = Color.white }
                };
            }

            // 计算UI位置
            Rect rect = GetUIRect();
            
            // 绘制背景
            GUI.Box(rect, "", GUI.skin.box);
            
            // 绘制性能信息
            GUILayout.BeginArea(rect);
            GUILayout.Label("=== 性能监控 ===", uiStyle);
            
            foreach (var kvp in metrics)
            {
                var metric = kvp.Value;
                Color color = GetMetricColor(metric.warningLevel);
                
                uiStyle.normal.textColor = color;
                string text = $"{metric.type}: {metric.currentValue:F1}";
                
                if (metric.type == PerformanceMetricType.FrameRate)
                    text += " FPS";
                else if (metric.type == PerformanceMetricType.Memory)
                    text += " MB";
                else if (metric.type == PerformanceMetricType.NetworkLatency)
                    text += " ms";
                
                GUILayout.Label(text, uiStyle);
            }
            
            GUILayout.EndArea();
        }

        /// <summary>
        /// 获取UI矩形
        /// </summary>
        private Rect GetUIRect()
        {
            float width = 200f;
            float height = metrics.Count * 20f + 40f;
            
            switch (uiAnchor)
            {
                case TextAnchor.UpperLeft:
                    return new Rect(10, 10, width, height);
                case TextAnchor.UpperRight:
                    return new Rect(Screen.width - width - 10, 10, width, height);
                case TextAnchor.LowerLeft:
                    return new Rect(10, Screen.height - height - 10, width, height);
                case TextAnchor.LowerRight:
                    return new Rect(Screen.width - width - 10, Screen.height - height - 10, width, height);
                default:
                    return new Rect(10, 10, width, height);
            }
        }

        /// <summary>
        /// 获取指标颜色
        /// </summary>
        private Color GetMetricColor(PerformanceWarningLevel level)
        {
            switch (level)
            {
                case PerformanceWarningLevel.Normal:
                    return Color.green;
                case PerformanceWarningLevel.Warning:
                    return Color.yellow;
                case PerformanceWarningLevel.Critical:
                    return Color.red;
                case PerformanceWarningLevel.Emergency:
                    return Color.magenta;
                default:
                    return Color.white;
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// 获取指定类型的性能指标
        /// </summary>
        public PerformanceMetric GetMetric(PerformanceMetricType type)
        {
            return metrics.TryGetValue(type, out PerformanceMetric metric) ? metric : null;
        }

        /// <summary>
        /// 获取所有性能指标
        /// </summary>
        public Dictionary<PerformanceMetricType, PerformanceMetric> GetAllMetrics()
        {
            return new Dictionary<PerformanceMetricType, PerformanceMetric>(metrics);
        }

        /// <summary>
        /// 获取最新的性能报告
        /// </summary>
        public PerformanceReport GetLatestReport()
        {
            return reports.LastOrDefault();
        }

        /// <summary>
        /// 获取所有性能报告
        /// </summary>
        public List<PerformanceReport> GetAllReports()
        {
            return new List<PerformanceReport>(reports);
        }

        /// <summary>
        /// 切换UI显示
        /// </summary>
        public void ToggleUI()
        {
            showUI = !showUI;
        }

        /// <summary>
        /// 重置所有指标
        /// </summary>
        public void ResetMetrics()
        {
            foreach (var metric in metrics.Values)
            {
                metric.Reset();
            }
            
            reports.Clear();
            Debug.Log("[PerformanceMonitor] 所有性能指标已重置");
        }

        /// <summary>
        /// 获取性能摘要
        /// </summary>
        public string GetPerformanceSummary()
        {
            var fps = GetMetric(PerformanceMetricType.FrameRate);
            var memory = GetMetric(PerformanceMetricType.Memory);
            var drawCalls = GetMetric(PerformanceMetricType.DrawCalls);
            
            return $"FPS: {fps?.currentValue:F1}, Memory: {memory?.currentValue:F1}MB, DrawCalls: {drawCalls?.currentValue:F0}";
        }

        /// <summary>
        /// 导出性能数据为JSON
        /// </summary>
        public string ExportPerformanceData()
        {
            var exportData = new
            {
                exportTime = DateTime.Now,
                metrics = metrics,
                reports = reports
            };
            
            return JsonUtility.ToJson(exportData, true);
        }

        #endregion

        #region Debug and Testing

        /// <summary>
        /// 输出性能调试信息
        /// </summary>
        public void DebugPerformanceInfo()
        {
            Debug.Log("=== 性能监控调试信息 ===");
            Debug.Log($"监控状态: {isMonitoring}");
            Debug.Log($"更新间隔: {updateInterval}s");
            Debug.Log($"指标数量: {metrics.Count}");
            Debug.Log($"报告数量: {reports.Count}");
            Debug.Log($"性能摘要: {GetPerformanceSummary()}");
            
            foreach (var kvp in metrics)
            {
                var metric = kvp.Value;
                Debug.Log($"{metric.type}: {metric.currentValue:F1} (平均: {metric.averageValue:F1}, 级别: {metric.warningLevel})");
            }
        }

        /// <summary>
        /// 模拟性能压力测试
        /// </summary>
        public void SimulatePerformanceStress()
        {
            StartCoroutine(PerformanceStressTest());
        }

        /// <summary>
        /// 性能压力测试协程
        /// </summary>
        private IEnumerator PerformanceStressTest()
        {
            Debug.Log("[PerformanceMonitor] 开始性能压力测试");
            
            // 创建临时对象增加渲染负载
            List<GameObject> testObjects = new List<GameObject>();
            
            for (int i = 0; i < 100; i++)
            {
                GameObject testObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                testObj.transform.position = UnityEngine.Random.insideUnitSphere * 10f;
                testObjects.Add(testObj);
                
                if (i % 10 == 0)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            yield return new WaitForSeconds(5f);
            
            // 清理测试对象
            foreach (var obj in testObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            
            Debug.Log("[PerformanceMonitor] 性能压力测试完成");
        }

        /// <summary>
        /// 测试所有性能监控功能
        /// </summary>
        public void TestAllFeatures()
        {
            StartCoroutine(TestAllFeaturesCoroutine());
        }

        /// <summary>
        /// 测试所有功能的协程
        /// </summary>
        private IEnumerator TestAllFeaturesCoroutine()
        {
            Debug.Log("[PerformanceMonitor] 开始功能测试");
            
            // 测试监控启停
            ToggleMonitoring();
            yield return new WaitForSeconds(2f);
            ToggleMonitoring();
            yield return new WaitForSeconds(2f);
            
            // 测试UI切换
            ToggleUI();
            yield return new WaitForSeconds(2f);
            ToggleUI();
            yield return new WaitForSeconds(2f);
            
            // 测试报告生成
            var report = GeneratePerformanceReport();
            Debug.Log($"测试报告生成: 评分 {report.overallScore:F1}");
            
            // 测试数据重置
            ResetMetrics();
            
            // 测试压力测试
            SimulatePerformanceStress();
            
            yield return new WaitForSeconds(3f);
            Debug.Log("[PerformanceMonitor] 功能测试完成");
        }

        /// <summary>
        /// 获取详细的性能状态报告
        /// </summary>
        public string GetDetailedStatus()
        {
            var status = new System.Text.StringBuilder();
            status.AppendLine("=== 详细性能状态报告 ===");
            status.AppendLine($"监控状态: {(isMonitoring ? "运行中" : "已停止")}");
            status.AppendLine($"监控时长: {(Time.realtimeSinceStartup - startTime):F1}秒");
            status.AppendLine($"更新间隔: {updateInterval}秒");
            status.AppendLine($"UI显示: {(showUI ? "开启" : "关闭")}");
            status.AppendLine($"报告数量: {reports.Count}/{maxReports}");
            status.AppendLine();
            
            status.AppendLine("=== 性能指标详情 ===");
            foreach (var kvp in metrics)
            {
                var metric = kvp.Value;
                status.AppendLine($"{metric.type}:");
                status.AppendLine($"  当前值: {metric.currentValue:F2}");
                status.AppendLine($"  平均值: {metric.averageValue:F2}");
                status.AppendLine($"  最小值: {metric.minValue:F2}");
                status.AppendLine($"  最大值: {metric.maxValue:F2}");
                status.AppendLine($"  警告级别: {metric.warningLevel}");
                status.AppendLine($"  趋势: {metric.GetTrend():F2}");
                status.AppendLine($"  数据点: {metric.history.Count}");
                status.AppendLine();
            }
            
            if (reports.Count > 0)
            {
                var latestReport = reports.Last();
                status.AppendLine("=== 最新报告 ===");
                status.AppendLine($"生成时间: {latestReport.reportTime}");
                status.AppendLine($"总体评分: {latestReport.overallScore:F1}");
                status.AppendLine($"警告数量: {latestReport.warnings.Count}");
                status.AppendLine($"建议数量: {latestReport.recommendations.Count}");
            }
            
            return status.ToString();
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            Debug.Log("[PerformanceMonitor] 🧹 清理性能监控器资源");
            
            StopMonitoring();
            
            // 清理事件回调
            OnMetricUpdated = null;
            OnPerformanceWarning = null;
            OnReportGenerated = null;
            
            // 清理数据
            metrics.Clear();
            reports.Clear();
            
            // 重置状态
            frameCount = 0;
            lastFrameTime = 0f;
            startTime = 0f;
            showUI = false;
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion
    }
}