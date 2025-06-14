// ================================================================================================
// Safari音频兼容管理器 - SafariAudioManager.cs
// 用途：处理Safari浏览器的音频播放限制和兼容性问题，对应JavaScript项目的Safari音频处理
// ================================================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BaccaratGame.Audio
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
        WebKit
    }

    /// <summary>
    /// 音频上下文状态
    /// </summary>
    public enum AudioContextState
    {
        Suspended,      // 暂停状态（需要用户交互才能播放）
        Running,        // 运行状态（可以播放音频）
        Closed,         // 关闭状态
        Interrupted,    // 中断状态
        Unknown         // 未知状态
    }

    /// <summary>
    /// Safari音频兼容管理器 - 专门处理Safari浏览器的音频限制
    /// 对应JavaScript项目中的Safari兼容性处理逻辑
    /// </summary>
    public class SafariAudioManager : MonoBehaviour
    {
        [Header("🌐 Browser Detection")]
        [Tooltip("当前检测到的浏览器类型")]
        [SerializeField] private BrowserType detectedBrowser = BrowserType.Unknown;
        
        [Tooltip("是否为Safari浏览器")]
        [SerializeField] private bool isSafari = false;
        
        [Tooltip("是否为移动设备")]
        [SerializeField] private bool isMobile = false;

        [Header("🔊 Audio Context Management")]
        [Tooltip("音频上下文当前状态")]
        [SerializeField] private AudioContextState audioContextState = AudioContextState.Unknown;
        
        [Tooltip("是否已解锁音频上下文")]
        [SerializeField] private bool audioContextUnlocked = false;
        
        [Tooltip("用户交互次数")]
        [SerializeField] private int userInteractionCount = 0;

        [Header("⚙️ Safari Specific Settings")]
        [Tooltip("Safari音频最大音量")]
        [Range(0f, 1f)]
        public float safariMaxVolume = 0.8f;
        
        [Tooltip("Safari音频预加载延迟（秒）")]
        [Range(0.1f, 2f)]
        public float safariPreloadDelay = 0.5f;
        
        [Tooltip("Safari音频重试次数")]
        [Range(1, 5)]
        public int safariRetryCount = 3;

        [Header("🎯 Interaction Detection")]
        [Tooltip("是否启用自动交互检测")]
        public bool enableAutoInteractionDetection = true;
        
        [Tooltip("交互检测的UI元素列表")]
        public List<GameObject> interactionTargets = new List<GameObject>();

        // 事件和回调
        public System.Action OnAudioContextUnlocked;
        public System.Action<string> OnSafariAudioError;
        public System.Action<BrowserType> OnBrowserDetected;

        // 内部状态
        private AudioManager audioManager;
        private bool isInitialized = false;
        private bool isWebGLBuild = false;
        private List<string> pendingAudioQueue = new List<string>();
        private Coroutine audioContextCheckCoroutine;
        private Coroutine safariOptimizationCoroutine;

        // Safari特殊处理
        private Dictionary<string, float> safariVolumeOverrides = new Dictionary<string, float>();
        private Queue<System.Action> pendingAudioActions = new Queue<System.Action>();
        private bool safariMemoryOptimizationActive = false;

        #region Unity Lifecycle

        private void Awake()
        {
            // 检测平台
            isWebGLBuild = Application.platform == RuntimePlatform.WebGLPlayer;
            
            if (isWebGLBuild)
            {
                DetectBrowser();
                DetectMobile();
                InitializeSafariManager();
            }
        }

        private void Start()
        {
            if (isWebGLBuild && isSafari)
            {
                StartSafariOptimizations();
            }
        }

        private void Update()
        {
            // 检测用户交互（仅在Safari中且音频上下文未解锁时）
            if (isSafari && !audioContextUnlocked && enableAutoInteractionDetection)
            {
                DetectUserInteraction();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化Safari管理器
        /// </summary>
        private void InitializeSafariManager()
        {
            audioManager = AudioManager.Instance;
            
            if (isSafari)
            {
                Debug.Log("[SafariAudioManager] Safari浏览器检测到，启用兼容性模式");
                
                // 设置Safari特殊配置
                ApplySafariAudioSettings();
                
                // 开始音频上下文检查
                if (audioContextCheckCoroutine == null)
                {
                    audioContextCheckCoroutine = StartCoroutine(CheckAudioContextState());
                }
            }
            
            isInitialized = true;
            Debug.Log($"[SafariAudioManager] 初始化完成 - 浏览器: {detectedBrowser}, Safari: {isSafari}");
        }

        /// <summary>
        /// 应用Safari音频设置
        /// </summary>
        private void ApplySafariAudioSettings()
        {
            // Safari音频音量限制
            if (audioManager != null)
            {
                audioManager.SetSFXVolume(Mathf.Min(audioManager.sfxVolume, safariMaxVolume));
                audioManager.SetBGMVolume(Mathf.Min(audioManager.bgmVolume, safariMaxVolume * 0.6f));
            }
            
            // 设置Safari特殊音量覆盖
            safariVolumeOverrides["winning"] = safariMaxVolume * 0.9f;
            safariVolumeOverrides["background"] = safariMaxVolume * 0.4f;
            safariVolumeOverrides["effect"] = safariMaxVolume * 0.7f;
            
            Debug.Log("[SafariAudioManager] Safari音频设置已应用");
        }

        #endregion

        #region Browser Detection

        /// <summary>
        /// 检测浏览器类型
        /// </summary>
        private void DetectBrowser()
        {
            string userAgent = SystemInfo.operatingSystem.ToLower();
            
            // 在WebGL中，通过JavaScript获取更准确的UserAgent
            if (isWebGLBuild)
            {
                userAgent = GetUserAgentFromJS();
            }
            
            detectedBrowser = ParseUserAgent(userAgent);
            isSafari = (detectedBrowser == BrowserType.Safari || detectedBrowser == BrowserType.WebKit);
            
            Debug.Log($"[SafariAudioManager] 检测到浏览器: {detectedBrowser} (UserAgent: {userAgent})");
            
            OnBrowserDetected?.Invoke(detectedBrowser);
        }

        /// <summary>
        /// 解析UserAgent字符串
        /// </summary>
        private BrowserType ParseUserAgent(string userAgent)
        {
            userAgent = userAgent.ToLower();
            
            if (userAgent.Contains("safari") && !userAgent.Contains("chrome") && !userAgent.Contains("chromium"))
            {
                return BrowserType.Safari;
            }
            else if (userAgent.Contains("webkit") && !userAgent.Contains("chrome"))
            {
                return BrowserType.WebKit;
            }
            else if (userAgent.Contains("chrome") || userAgent.Contains("chromium"))
            {
                return BrowserType.Chrome;
            }
            else if (userAgent.Contains("firefox"))
            {
                return BrowserType.Firefox;
            }
            else if (userAgent.Contains("edge"))
            {
                return BrowserType.Edge;
            }
            else if (userAgent.Contains("opera"))
            {
                return BrowserType.Opera;
            }
            else if (userAgent.Contains("msie") || userAgent.Contains("trident"))
            {
                return BrowserType.IE;
            }
            
            return BrowserType.Unknown;
        }

        /// <summary>
        /// 检测移动设备
        /// </summary>
        private void DetectMobile()
        {
            isMobile = Application.isMobilePlatform || 
                      SystemInfo.deviceType == DeviceType.Handheld ||
                      Screen.width <= 768; // 简单的移动设备检测
            
            Debug.Log($"[SafariAudioManager] 移动设备检测: {isMobile}");
        }

        /// <summary>
        /// 从JavaScript获取UserAgent（WebGL专用）
        /// </summary>
        private string GetUserAgentFromJS()
        {
            try
            {
                // 在WebGL环境中，可以通过jslib调用JavaScript获取更准确的UserAgent
                return Application.platform == RuntimePlatform.WebGLPlayer ? 
                       GetUserAgentJS() : SystemInfo.operatingSystem;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SafariAudioManager] 获取UserAgent失败: {ex.Message}");
                return SystemInfo.operatingSystem;
            }
        }

        /// <summary>
        /// JavaScript调用接口（需要在.jslib文件中实现）
        /// </summary>
        private string GetUserAgentJS()
        {
            // 这里应该调用JavaScript函数获取navigator.userAgent
            // 实际实现需要在WebGL插件中完成
            return "webkit safari"; // 临时返回值用于测试
        }

        #endregion

        #region Audio Context Management

        /// <summary>
        /// 检查音频上下文状态
        /// </summary>
        private IEnumerator CheckAudioContextState()
        {
            while (isSafari && !audioContextUnlocked)
            {
                // 检查音频上下文状态
                AudioContextState currentState = GetAudioContextState();
                
                if (currentState != audioContextState)
                {
                    audioContextState = currentState;
                    Debug.Log($"[SafariAudioManager] 音频上下文状态变化: {audioContextState}");
                    
                    if (audioContextState == AudioContextState.Running)
                    {
                        OnAudioContextUnlocked?.Invoke();
                        audioContextUnlocked = true;
                        ProcessPendingAudioActions();
                        break;
                    }
                }
                
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>
        /// 获取音频上下文状态
        /// </summary>
        private AudioContextState GetAudioContextState()
        {
            // 在WebGL环境中，通过JavaScript检查AudioContext状态
            if (isWebGLBuild)
            {
                try
                {
                    return GetAudioContextStateFromJS();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SafariAudioManager] 获取音频上下文状态失败: {ex.Message}");
                    return AudioContextState.Unknown;
                }
            }
            
            // 非WebGL环境默认为运行状态
            return AudioContextState.Running;
        }

        /// <summary>
        /// 从JavaScript获取音频上下文状态
        /// </summary>
        private AudioContextState GetAudioContextStateFromJS()
        {
            // 这里应该调用JavaScript函数检查AudioContext.state
            // 实际实现需要在WebGL插件中完成
            
            // 模拟状态检查
            if (userInteractionCount > 0)
            {
                return AudioContextState.Running;
            }
            
            return AudioContextState.Suspended;
        }

        /// <summary>
        /// 尝试解锁音频上下文
        /// </summary>
        public bool TryUnlockAudioContext()
        {
            if (!isSafari || audioContextUnlocked)
            {
                return true;
            }
            
            try
            {
                Debug.Log("[SafariAudioManager] 尝试解锁音频上下文");
                
                // 通过播放静音音频解锁AudioContext
                if (audioManager != null)
                {
                    // 创建一个极短的静音音频来解锁上下文
                    UnlockAudioContextWithSilentAudio();
                }
                
                userInteractionCount++;
                
                // 检查是否解锁成功
                if (GetAudioContextState() == AudioContextState.Running)
                {
                    audioContextUnlocked = true;
                    OnAudioContextUnlocked?.Invoke();
                    ProcessPendingAudioActions();
                    Debug.Log("✅ [SafariAudioManager] 音频上下文解锁成功");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SafariAudioManager] 解锁音频上下文失败: {ex.Message}");
                OnSafariAudioError?.Invoke($"解锁失败: {ex.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// 使用静音音频解锁上下文
        /// </summary>
        private void UnlockAudioContextWithSilentAudio()
        {
            // 创建一个极短的静音AudioSource来触发音频上下文
            GameObject tempGO = new GameObject("SafariAudioUnlocker");
            AudioSource tempSource = tempGO.AddComponent<AudioSource>();
            
            // 创建极短的静音AudioClip
            AudioClip silentClip = AudioClip.Create("SilentUnlock", 1, 1, 44100, false);
            float[] samples = new float[1];
            samples[0] = 0f;
            silentClip.SetData(samples, 0);
            
            tempSource.clip = silentClip;
            tempSource.volume = 0.001f; // 极低音量
            tempSource.Play();
            
            // 短暂延迟后清理
            StartCoroutine(CleanupUnlockAudio(tempGO, 0.1f));
        }

        /// <summary>
        /// 清理解锁音频
        /// </summary>
        private IEnumerator CleanupUnlockAudio(GameObject tempGO, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (tempGO != null)
            {
                Destroy(tempGO);
            }
        }

        #endregion

        #region User Interaction Detection

        /// <summary>
        /// 检测用户交互
        /// </summary>
        private void DetectUserInteraction()
        {
            // 检测鼠标点击
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                HandleUserInteraction("mouse_click");
            }
            
            // 检测触摸
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                HandleUserInteraction("touch");
            }
            
            // 检测键盘按键
            if (Input.anyKeyDown)
            {
                HandleUserInteraction("keyboard");
            }
        }

        /// <summary>
        /// 处理用户交互
        /// </summary>
        private void HandleUserInteraction(string interactionType)
        {
            if (!audioContextUnlocked)
            {
                Debug.Log($"[SafariAudioManager] 检测到用户交互: {interactionType}");
                TryUnlockAudioContext();
            }
        }

        /// <summary>
        /// 手动触发用户交互（供UI调用）
        /// </summary>
        public void TriggerUserInteraction()
        {
            HandleUserInteraction("manual_trigger");
        }

        #endregion

        #region Safari Optimizations

        /// <summary>
        /// 启动Safari优化
        /// </summary>
        private void StartSafariOptimizations()
        {
            if (safariOptimizationCoroutine == null)
            {
                safariOptimizationCoroutine = StartCoroutine(SafariOptimizationLoop());
            }
            
            // 启用内存优化
            if (isMobile)
            {
                EnableSafariMemoryOptimization();
            }
        }

        /// <summary>
        /// Safari优化循环
        /// </summary>
        private IEnumerator SafariOptimizationLoop()
        {
            while (isSafari)
            {
                // 定期检查和优化内存使用
                if (safariMemoryOptimizationActive)
                {
                    OptimizeMemoryUsage();
                }
                
                // 检查音频性能
                CheckAudioPerformance();
                
                yield return new WaitForSeconds(30f); // 每30秒检查一次
            }
        }

        /// <summary>
        /// 启用Safari内存优化
        /// </summary>
        private void EnableSafariMemoryOptimization()
        {
            safariMemoryOptimizationActive = true;
            
            // 限制音频缓存大小
            if (audioManager != null)
            {
                // 通过反射或公共接口限制缓存大小
                Debug.Log("[SafariAudioManager] Safari内存优化已启用");
            }
        }

        /// <summary>
        /// 优化内存使用
        /// </summary>
        private void OptimizeMemoryUsage()
        {
            // 强制垃圾回收
            System.GC.Collect();
            
            // 清理未使用的音频资源
            Resources.UnloadUnusedAssets();
            
            Debug.Log("[SafariAudioManager] 内存优化完成");
        }

        /// <summary>
        /// 检查音频性能
        /// </summary>
        private void CheckAudioPerformance()
        {
            if (audioManager != null)
            {
                var audioStatus = audioManager.GetAudioStatus();
                
                // 检查是否有音频问题
                if (audioStatus.ContainsKey("activeAudioSources"))
                {
                    int activeSources = (int)audioStatus["activeAudioSources"];
                    
                    if (activeSources > 3) // Safari上限制并发音频数量
                    {
                        Debug.LogWarning($"[SafariAudioManager] 音频源过多 ({activeSources})，可能影响性能");
                        audioManager.StopSoundEffects();
                    }
                }
            }
        }

        #endregion

        #region Audio Action Queue

        /// <summary>
        /// 添加待处理的音频动作
        /// </summary>
        public void QueueAudioAction(System.Action audioAction)
        {
            if (audioContextUnlocked)
            {
                // 直接执行
                audioAction?.Invoke();
            }
            else
            {
                // 加入队列等待解锁
                pendingAudioActions.Enqueue(audioAction);
                Debug.Log("[SafariAudioManager] 音频动作已加入等待队列");
            }
        }

        /// <summary>
        /// 处理待处理的音频动作
        /// </summary>
        private void ProcessPendingAudioActions()
        {
            Debug.Log($"[SafariAudioManager] 处理 {pendingAudioActions.Count} 个待处理音频动作");
            
            while (pendingAudioActions.Count > 0)
            {
                var action = pendingAudioActions.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SafariAudioManager] 执行音频动作失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 清空音频动作队列
        /// </summary>
        public void ClearAudioActionQueue()
        {
            pendingAudioActions.Clear();
            pendingAudioQueue.Clear();
            Debug.Log("[SafariAudioManager] 音频动作队列已清空");
        }

        #endregion

        #region Safari Audio Methods

        /// <summary>
        /// Safari安全音频播放
        /// </summary>
        public bool SafariSafePlayAudio(string audioName, float volume = 1f)
        {
            if (!isSafari)
            {
                // 非Safari浏览器直接播放
                return audioManager?.PlaySoundEffect(audioName, volume) ?? false;
            }
            
            if (!audioContextUnlocked)
            {
                // 加入队列等待
                QueueAudioAction(() => audioManager?.PlaySoundEffect(audioName, GetSafariVolume(volume)));
                return false;
            }
            
            // Safari环境下的安全播放
            float safariVolume = GetSafariVolume(volume);
            return audioManager?.PlaySoundEffect(audioName, safariVolume) ?? false;
        }

        /// <summary>
        /// 获取Safari安全音量
        /// </summary>
        private float GetSafariVolume(float requestedVolume)
        {
            return Mathf.Min(requestedVolume, safariMaxVolume);
        }

        /// <summary>
        /// Safari音频重试播放
        /// </summary>
        public void SafariRetryAudio(string audioName, float volume = 1f)
        {
            StartCoroutine(SafariRetryAudioCoroutine(audioName, volume));
        }

        /// <summary>
        /// Safari音频重试协程
        /// </summary>
        private IEnumerator SafariRetryAudioCoroutine(string audioName, float volume)
        {
            for (int i = 0; i < safariRetryCount; i++)
            {
                bool success = SafariSafePlayAudio(audioName, volume);
                
                if (success)
                {
                    Debug.Log($"[SafariAudioManager] 音频重试成功: {audioName} (第{i + 1}次)");
                    yield break;
                }
                
                yield return new WaitForSeconds(safariPreloadDelay);
            }
            
            Debug.LogWarning($"[SafariAudioManager] 音频重试失败: {audioName} (已重试{safariRetryCount}次)");
            OnSafariAudioError?.Invoke($"音频播放失败: {audioName}");
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// 获取Safari管理器状态
        /// </summary>
        public Dictionary<string, object> GetSafariManagerStatus()
        {
            return new Dictionary<string, object>
            {
                { "isInitialized", isInitialized },
                { "detectedBrowser", detectedBrowser.ToString() },
                { "isSafari", isSafari },
                { "isMobile", isMobile },
                { "isWebGLBuild", isWebGLBuild },
                { "audioContextState", audioContextState.ToString() },
                { "audioContextUnlocked", audioContextUnlocked },
                { "userInteractionCount", userInteractionCount },
                { "pendingActionsCount", pendingAudioActions.Count },
                { "safariMemoryOptimizationActive", safariMemoryOptimizationActive }
            };
        }

        /// <summary>
        /// 强制重新检测浏览器
        /// </summary>
        public void RefreshBrowserDetection()
        {
            DetectBrowser();
            DetectMobile();
            
            if (isSafari && !isInitialized)
            {
                InitializeSafariManager();
            }
        }

        /// <summary>
        /// 检查是否需要用户交互
        /// </summary>
        public bool RequiresUserInteraction()
        {
            return isSafari && !audioContextUnlocked;
        }

        /// <summary>
        /// 获取推荐的Safari设置
        /// </summary>
        public Dictionary<string, object> GetRecommendedSafariSettings()
        {
            return new Dictionary<string, object>
            {
                { "maxVolume", safariMaxVolume },
                { "enableMemoryOptimization", isMobile },
                { "maxConcurrentAudio", isMobile ? 2 : 3 },
                { "useAudioCompression", true },
                { "enableAudioQueue", true }
            };
        }

        #endregion

        #region Debug and Testing

        /// <summary>
        /// 测试Safari音频功能
        /// </summary>
        public void TestSafariAudio()
        {
            StartCoroutine(TestSafariAudioSequence());
        }

        /// <summary>
        /// Safari音频测试序列
        /// </summary>
        private IEnumerator TestSafariAudioSequence()
        {
            Debug.Log("[SafariAudioManager] 开始Safari音频测试");
            
            // 测试用户交互检测
            if (RequiresUserInteraction())
            {
                Debug.Log("等待用户交互...");
                yield return new WaitUntil(() => audioContextUnlocked);
            }
            
            // 测试基础音频播放
            SafariSafePlayAudio("test.wav", 0.5f);
            yield return new WaitForSeconds(1f);
            
            // 测试重试机制
            SafariRetryAudio("test2.wav", 0.3f);
            yield return new WaitForSeconds(2f);
            
            Debug.Log("[SafariAudioManager] Safari音频测试完成");
        }

        /// <summary>
        /// 输出调试信息
        /// </summary>
        public void DebugSafariInfo()
        {
            Debug.Log("=== SafariAudioManager 调试信息 ===");
            var status = GetSafariManagerStatus();
            foreach (var kvp in status)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value}");
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            Debug.Log("[SafariAudioManager] 🧹 清理Safari音频管理器资源");
            
            // 停止协程
            if (audioContextCheckCoroutine != null)
            {
                StopCoroutine(audioContextCheckCoroutine);
                audioContextCheckCoroutine = null;
            }
            
            if (safariOptimizationCoroutine != null)
            {
                StopCoroutine(safariOptimizationCoroutine);
                safariOptimizationCoroutine = null;
            }
            
            // 清理队列
            ClearAudioActionQueue();
            
            // 清理状态
            audioContextUnlocked = false;
            userInteractionCount = 0;
            safariMemoryOptimizationActive = false;
            
            // 清理回调
            OnAudioContextUnlocked = null;
            OnSafariAudioError = null;
            OnBrowserDetected = null;
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion
    }
}