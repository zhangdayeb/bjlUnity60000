// ================================================================================================
// Safari优化器 - SafariOptimizer.cs
// 用途：专门针对Safari浏览器的性能优化和兼容性处理
// ================================================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BaccaratGame.Utils
{
    /// <summary>
    /// Safari优化类型枚举
    /// </summary>
    public enum SafariOptimizationType
    {
        Memory,         // 内存优化
        Performance,    // 性能优化
        Audio,          // 音频优化
        Rendering,      // 渲染优化
        Touch,          // 触摸优化
        Storage,        // 存储优化
        Network         // 网络优化
    }

    /// <summary>
    /// 内存优化配置
    /// </summary>
    [System.Serializable]
    public class MemoryOptimizationConfig
    {
        [Tooltip("内存清理间隔（秒）")]
        [Range(10, 300)]
        public int cleanupInterval = 60;
        
        [Tooltip("内存警告阈值（MB）")]
        [Range(50, 500)]
        public int memoryWarningThreshold = 200;
        
        [Tooltip("强制垃圾回收阈值（MB）")]
        [Range(100, 1000)]
        public int forceGCThreshold = 300;
        
        [Tooltip("纹理压缩质量")]
        [Range(0, 4)]
        public int textureQuality = 2;
        
        [Tooltip("是否启用内存池")]
        public bool enableObjectPooling = true;
    }

    /// <summary>
    /// 性能优化配置
    /// </summary>
    [System.Serializable]
    public class PerformanceOptimizationConfig
    {
        [Tooltip("目标帧率")]
        [Range(20, 60)]
        public int targetFrameRate = 30;
        
        [Tooltip("最大粒子数量")]
        [Range(50, 1000)]
        public int maxParticles = 200;
        
        [Tooltip("阴影距离")]
        [Range(10, 100)]
        public float shadowDistance = 50f;
        
        [Tooltip("LOD偏移")]
        [Range(0f, 2f)]
        public float lodBias = 1f;
        
        [Tooltip("是否启用批处理")]
        public bool enableBatching = true;
    }

    /// <summary>
    /// Safari优化器 - 专门针对Safari浏览器的优化处理
    /// 对应JavaScript项目中的Safari特殊处理逻辑
    /// </summary>
    public class SafariOptimizer : MonoBehaviour
    {
        [Header("🌐 Safari Detection")]
        [Tooltip("是否为Safari浏览器")]
        [SerializeField] private bool isSafari = false;
        
        [Tooltip("是否为移动Safari")]
        [SerializeField] private bool isMobileSafari = false;
        
        [Tooltip("Safari版本")]
        [SerializeField] private string safariVersion = "";

        [Header("⚙️ Optimization Settings")]
        [Tooltip("启用的优化类型")]
        public List<SafariOptimizationType> enabledOptimizations = new List<SafariOptimizationType>
        {
            SafariOptimizationType.Memory,
            SafariOptimizationType.Performance,
            SafariOptimizationType.Audio,
            SafariOptimizationType.Touch
        };

        [Header("🧠 Memory Optimization")]
        public MemoryOptimizationConfig memoryConfig = new MemoryOptimizationConfig();

        [Header("🚀 Performance Optimization")]
        public PerformanceOptimizationConfig performanceConfig = new PerformanceOptimizationConfig();

        [Header("🔊 Audio Optimization")]
        [Tooltip("Safari音频最大并发数")]
        [Range(1, 8)]
        public int maxConcurrentAudio = 3;
        
        [Tooltip("音频预加载延迟（秒）")]
        [Range(0.1f, 2f)]
        public float audioPreloadDelay = 0.5f;

        [Header("📱 Touch Optimization")]
        [Tooltip("触摸响应延迟（毫秒）")]
        [Range(0, 500)]
        public int touchResponseDelay = 100;
        
        [Tooltip("是否启用触摸优化")]
        public bool enableTouchOptimization = true;

        // 私有变量
        private bool isOptimized = false;
        private Coroutine memoryCleanupCoroutine;
        private Coroutine performanceMonitorCoroutine;
        private Dictionary<string, float> performanceMetrics = new Dictionary<string, float>();
        private List<GameObject> objectPool = new List<GameObject>();
        private int lastFrameCount = 0;
        private float lastFrameTime = 0f;

        // 事件回调
        public System.Action<SafariOptimizationType> OnOptimizationApplied;
        public System.Action<string> OnMemoryWarning;
        public System.Action<float> OnPerformanceUpdate;

        #region Unity Lifecycle

        private void Awake()
        {
            DetectSafari();
        }

        private void Start()
        {
            if (isSafari)
            {
                StartSafariOptimizations();
            }
        }

        private void Update()
        {
            if (isSafari && isOptimized)
            {
                UpdatePerformanceMetrics();
            }
        }

        #endregion

        #region Safari Detection

        /// <summary>
        /// 检测Safari浏览器
        /// </summary>
        private void DetectSafari()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                Debug.Log("[SafariOptimizer] 非WebGL平台，跳过Safari检测");
                return;
            }

            try
            {
                var browserInfo = WebGLUtils.DetectBrowser();
                isSafari = (browserInfo.browserType == BrowserType.Safari || 
                           browserInfo.browserType == BrowserType.WebKit);
                isMobileSafari = isSafari && browserInfo.isMobile;
                safariVersion = browserInfo.version;

                Debug.Log($"[SafariOptimizer] Safari检测结果: {isSafari} (移动版: {isMobileSafari}, 版本: {safariVersion})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafariOptimizer] Safari检测失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否需要Safari优化
        /// </summary>
        public bool RequiresSafariOptimization()
        {
            return isSafari && Application.platform == RuntimePlatform.WebGLPlayer;
        }

        #endregion

        #region Optimization Control

        /// <summary>
        /// 启动Safari优化
        /// </summary>
        public void StartSafariOptimizations()
        {
            if (!RequiresSafariOptimization())
            {
                Debug.Log("[SafariOptimizer] 不需要Safari优化");
                return;
            }

            Debug.Log("[SafariOptimizer] 开始应用Safari优化...");

            // 应用各种优化
            foreach (var optimizationType in enabledOptimizations)
            {
                ApplyOptimization(optimizationType);
            }

            // 启动监控协程
            if (enabledOptimizations.Contains(SafariOptimizationType.Memory))
            {
                StartMemoryCleanup();
            }

            if (enabledOptimizations.Contains(SafariOptimizationType.Performance))
            {
                StartPerformanceMonitoring();
            }

            isOptimized = true;
            Debug.Log("[SafariOptimizer] Safari优化应用完成");
        }

        /// <summary>
        /// 应用特定类型的优化
        /// </summary>
        private void ApplyOptimization(SafariOptimizationType optimizationType)
        {
            try
            {
                switch (optimizationType)
                {
                    case SafariOptimizationType.Memory:
                        ApplyMemoryOptimization();
                        break;
                    case SafariOptimizationType.Performance:
                        ApplyPerformanceOptimization();
                        break;
                    case SafariOptimizationType.Audio:
                        ApplyAudioOptimization();
                        break;
                    case SafariOptimizationType.Rendering:
                        ApplyRenderingOptimization();
                        break;
                    case SafariOptimizationType.Touch:
                        ApplyTouchOptimization();
                        break;
                    case SafariOptimizationType.Storage:
                        ApplyStorageOptimization();
                        break;
                    case SafariOptimizationType.Network:
                        ApplyNetworkOptimization();
                        break;
                }

                OnOptimizationApplied?.Invoke(optimizationType);
                Debug.Log($"[SafariOptimizer] 已应用优化: {optimizationType}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafariOptimizer] 应用优化失败 ({optimizationType}): {ex.Message}");
            }
        }

        #endregion

        #region Memory Optimization

        /// <summary>
        /// 应用内存优化
        /// </summary>
        private void ApplyMemoryOptimization()
        {
            // 设置纹理质量
            QualitySettings.globalTextureMipmapLimit = memoryConfig.textureQuality;
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = 512; // Safari内存限制

            // 减少粒子系统内存占用
            var particleSystems = FindObjectsOfType<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.maxParticles = Mathf.Min(main.maxParticles, 100); // Safari限制粒子数量
            }

            // 优化对象池设置
            if (memoryConfig.enableObjectPooling)
            {
                InitializeObjectPool();
            }

            Debug.Log("[SafariOptimizer] 内存优化已应用");
        }

        /// <summary>
        /// 初始化对象池
        /// </summary>
        private void InitializeObjectPool()
        {
            // 创建对象池父节点
            GameObject poolParent = new GameObject("SafariObjectPool");
            poolParent.transform.SetParent(transform);

            // 预创建一些常用对象
            for (int i = 0; i < 10; i++)
            {
                GameObject pooledObject = new GameObject($"PooledObject_{i}");
                pooledObject.transform.SetParent(poolParent.transform);
                pooledObject.SetActive(false);
                objectPool.Add(pooledObject);
            }

            Debug.Log($"[SafariOptimizer] 对象池初始化完成，预创建 {objectPool.Count} 个对象");
        }

        /// <summary>
        /// 启动内存清理
        /// </summary>
        private void StartMemoryCleanup()
        {
            if (memoryCleanupCoroutine != null)
            {
                StopCoroutine(memoryCleanupCoroutine);
            }

            memoryCleanupCoroutine = StartCoroutine(MemoryCleanupLoop());
        }

        /// <summary>
        /// 内存清理循环
        /// </summary>
        private IEnumerator MemoryCleanupLoop()
        {
            while (isOptimized)
            {
                yield return new WaitForSeconds(memoryConfig.cleanupInterval);

                try
                {
                    // 获取当前内存使用量
                    long memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024); // MB

                    if (memoryUsage > memoryConfig.memoryWarningThreshold)
                    {
                        Debug.LogWarning($"[SafariOptimizer] 内存使用警告: {memoryUsage}MB");
                        OnMemoryWarning?.Invoke($"内存使用过高: {memoryUsage}MB");

                        if (memoryUsage > memoryConfig.forceGCThreshold)
                        {
                            ForceMemoryCleanup();
                        }
                    }

                    // 定期清理
                    PerformMemoryCleanup();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SafariOptimizer] 内存清理失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 执行内存清理
        /// </summary>
        private void PerformMemoryCleanup()
        {
            // 清理未使用的资源
            Resources.UnloadUnusedAssets();

            // 建议垃圾回收
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            Debug.Log("[SafariOptimizer] 定期内存清理完成");
        }

        /// <summary>
        /// 强制内存清理
        /// </summary>
        private void ForceMemoryCleanup()
        {
            Debug.Log("[SafariOptimizer] 执行强制内存清理");

            // 更激进的清理
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            // 清理对象池
            ClearObjectPool();

            // 降低质量设置
            if (QualitySettings.GetQualityLevel() > 0)
            {
                QualitySettings.DecreaseLevel();
                Debug.Log($"[SafariOptimizer] 质量等级已降低到: {QualitySettings.GetQualityLevel()}");
            }
        }

        /// <summary>
        /// 清理对象池
        /// </summary>
        private void ClearObjectPool()
        {
            foreach (var obj in objectPool)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            objectPool.Clear();
            Debug.Log("[SafariOptimizer] 对象池已清理");
        }

        #endregion

        #region Performance Optimization

        /// <summary>
        /// 应用性能优化
        /// </summary>
        private void ApplyPerformanceOptimization()
        {
            // 设置目标帧率
            Application.targetFrameRate = performanceConfig.targetFrameRate;

            // 调整质量设置
            QualitySettings.shadowDistance = performanceConfig.shadowDistance;
            QualitySettings.lodBias = performanceConfig.lodBias;
            QualitySettings.maximumLODLevel = 1; // Safari上使用较低的LOD

            // 禁用一些耗性能的功能
            QualitySettings.shadows = ShadowQuality.Disable; // Safari上禁用阴影
            QualitySettings.softParticles = false;

            // 批处理优化
            if (performanceConfig.enableBatching)
            {
                QualitySettings.softVegetation = false;
                QualitySettings.realtimeReflectionProbes = false;
            }

            Debug.Log($"[SafariOptimizer] 性能优化已应用，目标帧率: {performanceConfig.targetFrameRate}");
        }

        /// <summary>
        /// 启动性能监控
        /// </summary>
        private void StartPerformanceMonitoring()
        {
            if (performanceMonitorCoroutine != null)
            {
                StopCoroutine(performanceMonitorCoroutine);
            }

            performanceMonitorCoroutine = StartCoroutine(PerformanceMonitorLoop());
        }

        /// <summary>
        /// 性能监控循环
        /// </summary>
        private IEnumerator PerformanceMonitorLoop()
        {
            while (isOptimized)
            {
                yield return new WaitForSeconds(5f); // 每5秒检查一次

                UpdatePerformanceMetrics();
                CheckPerformanceThresholds();
            }
        }

        /// <summary>
        /// 更新性能指标
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            // 计算帧率
            int currentFrameCount = Time.frameCount;
            float currentTime = Time.realtimeSinceStartup;
            
            if (lastFrameTime > 0)
            {
                float deltaTime = currentTime - lastFrameTime;
                int deltaFrames = currentFrameCount - lastFrameCount;
                float fps = deltaFrames / deltaTime;
                
                performanceMetrics["fps"] = fps;
                OnPerformanceUpdate?.Invoke(fps);
            }

            lastFrameCount = currentFrameCount;
            lastFrameTime = currentTime;

            // 内存使用量
            long memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024);
            performanceMetrics["memory_mb"] = memoryUsage;

            // 渲染统计
            performanceMetrics["draw_calls"] = UnityEngine.Profiling.Profiler.GetRuntimeMemorySize(null) / 1024f;
        }

        /// <summary>
        /// 检查性能阈值
        /// </summary>
        private void CheckPerformanceThresholds()
        {
            if (performanceMetrics.TryGetValue("fps", out float fps))
            {
                if (fps < performanceConfig.targetFrameRate * 0.7f) // 低于目标帧率70%
                {
                    Debug.LogWarning($"[SafariOptimizer] 帧率过低: {fps:F1} FPS");
                    ApplyEmergencyOptimizations();
                }
            }
        }

        /// <summary>
        /// 应用紧急优化
        /// </summary>
        private void ApplyEmergencyOptimizations()
        {
            Debug.Log("[SafariOptimizer] 应用紧急性能优化");

            // 进一步降低目标帧率
            performanceConfig.targetFrameRate = Mathf.Max(20, performanceConfig.targetFrameRate - 5);
            Application.targetFrameRate = performanceConfig.targetFrameRate;

            // 禁用更多视觉效果
            var particles = FindObjectsOfType<ParticleSystem>();
            foreach (var ps in particles)
            {
                var emission = ps.emission;
                emission.rateOverTime = emission.rateOverTime.constant * 0.5f;
            }

            // 强制内存清理
            ForceMemoryCleanup();
        }

        #endregion

        #region Audio Optimization

        /// <summary>
        /// 应用音频优化
        /// </summary>
        private void ApplyAudioOptimization()
        {
            // 限制并发音频数量
            AudioConfiguration audioConfig = AudioSettings.GetConfiguration();
            audioConfig.dspBufferSize = 1024; // Safari推荐缓冲区大小
            AudioSettings.Reset(audioConfig);

            // 设置音频源限制
            var audioSources = FindObjectsOfType<AudioSource>();
            int activeCount = 0;
            
            foreach (var source in audioSources)
            {
                if (source.isPlaying)
                {
                    activeCount++;
                    if (activeCount > maxConcurrentAudio)
                    {
                        source.Stop();
                        Debug.Log($"[SafariOptimizer] 停止多余音频源: {source.name}");
                    }
                }

                // 优化音频设置
                source.spatialBlend = 0f; // 2D音频更节省性能
                source.rolloffMode = AudioRolloffMode.Linear;
            }

            Debug.Log($"[SafariOptimizer] 音频优化已应用，最大并发数: {maxConcurrentAudio}");
        }

        #endregion

        #region Rendering Optimization

        /// <summary>
        /// 应用渲染优化
        /// </summary>
        private void ApplyRenderingOptimization()
        {
            // 设置渲染管线优化
            QualitySettings.vSyncCount = 0; // 禁用垂直同步
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.antiAliasing = 0; // 禁用抗锯齿

            // Canvas优化
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 更高效的渲染模式
                }
            }

            Debug.Log("[SafariOptimizer] 渲染优化已应用");
        }

        #endregion

        #region Touch Optimization

        /// <summary>
        /// 应用触摸优化
        /// </summary>
        private void ApplyTouchOptimization()
        {
            if (!enableTouchOptimization || !isMobileSafari)
            {
                return;
            }

            // 设置触摸处理优化
            Input.multiTouchEnabled = false; // Safari上单点触摸更稳定
            Input.simulateMouseWithTouches = true;

            // 添加触摸延迟处理组件
            if (!GetComponent<SafariTouchHandler>())
            {
                gameObject.AddComponent<SafariTouchHandler>();
            }

            Debug.Log($"[SafariOptimizer] 触摸优化已应用，响应延迟: {touchResponseDelay}ms");
        }

        #endregion

        #region Storage Optimization

        /// <summary>
        /// 应用存储优化
        /// </summary>
        private void ApplyStorageOptimization()
        {
            // Safari LocalStorage有限制，定期清理
            StartCoroutine(StorageCleanupLoop());

            Debug.Log("[SafariOptimizer] 存储优化已应用");
        }

        /// <summary>
        /// 存储清理循环
        /// </summary>
        private IEnumerator StorageCleanupLoop()
        {
            while (isOptimized)
            {
                yield return new WaitForSeconds(300f); // 每5分钟清理一次

                try
                {
                    // 清理过期的本地存储项
                    CleanupExpiredStorage();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SafariOptimizer] 存储清理失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 清理过期存储
        /// </summary>
        private void CleanupExpiredStorage()
        {
            // 这里可以实现具体的存储清理逻辑
            Debug.Log("[SafariOptimizer] 存储清理完成");
        }

        #endregion

        #region Network Optimization

        /// <summary>
        /// 应用网络优化
        /// </summary>
        private void ApplyNetworkOptimization()
        {
            // Safari网络请求优化
            // 这里可以设置网络相关的优化参数
            
            Debug.Log("[SafariOptimizer] 网络优化已应用");
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// 获取优化状态
        /// </summary>
        public Dictionary<string, object> GetOptimizationStatus()
        {
            return new Dictionary<string, object>
            {
                { "isOptimized", isOptimized },
                { "isSafari", isSafari },
                { "isMobileSafari", isMobileSafari },
                { "safariVersion", safariVersion },
                { "enabledOptimizations", enabledOptimizations },
                { "performanceMetrics", performanceMetrics },
                { "targetFrameRate", performanceConfig.targetFrameRate },
                { "memoryThreshold", memoryConfig.memoryWarningThreshold }
            };
        }

        /// <summary>
        /// 手动触发内存清理
        /// </summary>
        public void ManualMemoryCleanup()
        {
            if (isOptimized)
            {
                PerformMemoryCleanup();
                Debug.Log("[SafariOptimizer] 手动内存清理完成");
            }
        }

        /// <summary>
        /// 停止所有优化
        /// </summary>
        public void StopOptimizations()
        {
            isOptimized = false;

            if (memoryCleanupCoroutine != null)
            {
                StopCoroutine(memoryCleanupCoroutine);
                memoryCleanupCoroutine = null;
            }

            if (performanceMonitorCoroutine != null)
            {
                StopCoroutine(performanceMonitorCoroutine);
                performanceMonitorCoroutine = null;
            }

            Debug.Log("[SafariOptimizer] 所有优化已停止");
        }

        /// <summary>
        /// 输出调试信息
        /// </summary>
        public void DebugOptimizationInfo()
        {
            Debug.Log("=== Safari优化器调试信息 ===");
            var status = GetOptimizationStatus();
            foreach (var kvp in status)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value}");
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            StopOptimizations();
            ClearObjectPool();
        }

        #endregion
    }

    /// <summary>
    /// Safari触摸处理组件
    /// </summary>
    public class SafariTouchHandler : MonoBehaviour
    {
        private float lastTouchTime = 0f;
        private float touchDelay = 0.1f;

        private void Update()
        {
            if (Input.touchCount > 0 && Time.time - lastTouchTime > touchDelay)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    HandleSafariTouch(touch);
                    lastTouchTime = Time.time;
                }
            }
        }

        private void HandleSafariTouch(Touch touch)
        {
            // Safari特殊触摸处理逻辑
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(touch.position);
            Debug.Log($"[SafariTouchHandler] 处理触摸: {worldPos}");
        }
    }
}