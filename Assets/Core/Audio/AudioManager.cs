// ================================================================================================
// 音频管理器 - AudioManager.cs
// 用途：Unity音频系统的核心管理器，对应JavaScript项目的useAudio.js功能
// ================================================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BaccaratGame.Audio
{
    /// <summary>
    /// 音频类型枚举
    /// </summary>
    public enum AudioType
    {
        SoundEffect,    // 音效
        BackgroundMusic, // 背景音乐
        WinningSound,   // 中奖音效（高优先级）
        OpenCardSound   // 开牌音效
    }

    /// <summary>
    /// 音频状态枚举
    /// </summary>
    public enum AudioState
    {
        Stopped,
        Playing,
        Paused,
        Fading
    }

    /// <summary>
    /// 音频源配置
    /// </summary>
    [System.Serializable]
    public class AudioSourceConfig
    {
        [Tooltip("音频源名称")]
        public string sourceName;
        
        [Tooltip("音频类型")]
        public AudioType audioType;
        
        [Tooltip("是否循环播放")]
        public bool loop = false;
        
        [Tooltip("音量")]
        [Range(0f, 1f)]
        public float volume = 1f;
        
        [Tooltip("优先级")]
        [Range(0, 256)]
        public int priority = 128;
        
        [Tooltip("是否3D音效")]
        public bool is3D = false;

        [System.NonSerialized]
        public AudioSource audioSource;
        
        [System.NonSerialized]
        public AudioState currentState = AudioState.Stopped;
    }

    /// <summary>
    /// 音频管理器 - Unity音频系统的核心管理类
    /// 对应JavaScript项目中的AudioHandle和useAudio组合功能
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton Pattern
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        [Header("🎵 Audio Sources Configuration")]
        [SerializeField] private List<AudioSourceConfig> audioSources = new List<AudioSourceConfig>();

        [Header("🔊 Volume Settings")]
        [Tooltip("主音量")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;
        
        [Tooltip("音效音量")]
        [Range(0f, 1f)]
        public float sfxVolume = 0.8f;
        
        [Tooltip("背景音乐音量")]
        [Range(0f, 1f)]
        public float bgmVolume = 0.3f;

        [Header("🎯 Audio Control")]
        [Tooltip("是否启用音效")]
        public bool enableSoundEffects = true;
        
        [Tooltip("是否启用背景音乐")]
        public bool enableBackgroundMusic = false;

        [Header("📁 Audio Resources")]
        [Tooltip("音频资源路径")]
        public string audioResourcePath = "Audio/";

        [Header("🔧 Advanced Settings")]
        [Tooltip("音频淡入淡出时间")]
        [Range(0.1f, 5f)]
        public float fadeTime = 0.5f;
        
        [Tooltip("最大同时播放音效数量")]
        [Range(1, 20)]
        public int maxConcurrentSounds = 10;

        // 音频剪辑缓存
        private Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();
        
        // 中奖音效保护状态
        private bool winningAudioProtected = false;
        private string currentWinningProtectionKey = "";
        private Coroutine winningProtectionCoroutine;
        
        // 音频队列管理
        private Queue<AudioRequest> audioQueue = new Queue<AudioRequest>();
        private bool isProcessingQueue = false;
        
        // 开牌音效序列管理
        private Coroutine openCardSequenceCoroutine;
        private bool isPlayingOpenCardSequence = false;

        /// <summary>
        /// 音频请求数据结构
        /// </summary>
        private class AudioRequest
        {
            public string audioName;
            public AudioType audioType;
            public float volume;
            public bool loop;
            public int priority;
            public System.Action onComplete;
        }

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 应用WebGL特殊设置
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                ApplyWebGLOptimizations();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化音频源
        /// </summary>
        private void InitializeAudioSources()
        {
            // 默认音频源配置
            if (audioSources.Count == 0)
            {
                CreateDefaultAudioSources();
            }

            // 创建AudioSource组件
            foreach (var config in audioSources)
            {
                GameObject sourceGO = new GameObject($"AudioSource_{config.sourceName}");
                sourceGO.transform.SetParent(transform);
                
                config.audioSource = sourceGO.AddComponent<AudioSource>();
                config.audioSource.loop = config.loop;
                config.audioSource.volume = config.volume;
                config.audioSource.priority = config.priority;
                config.audioSource.playOnAwake = false;
                
                if (!config.is3D)
                {
                    config.audioSource.spatialBlend = 0f; // 2D音频
                }
            }

            Debug.Log($"[AudioManager] 初始化完成，共创建 {audioSources.Count} 个音频源");
        }

        /// <summary>
        /// 创建默认音频源配置
        /// </summary>
        private void CreateDefaultAudioSources()
        {
            audioSources = new List<AudioSourceConfig>
            {
                new AudioSourceConfig 
                { 
                    sourceName = "SFX", 
                    audioType = AudioType.SoundEffect, 
                    priority = 128 
                },
                new AudioSourceConfig 
                { 
                    sourceName = "BGM", 
                    audioType = AudioType.BackgroundMusic, 
                    loop = true, 
                    priority = 64 
                },
                new AudioSourceConfig 
                { 
                    sourceName = "Winning", 
                    audioType = AudioType.WinningSound, 
                    priority = 256 
                },
                new AudioSourceConfig 
                { 
                    sourceName = "OpenCard", 
                    audioType = AudioType.OpenCardSound, 
                    priority = 200 
                }
            };
        }

        #endregion

        #region Public Audio Control Methods

        /// <summary>
        /// 播放音效
        /// </summary>
        public bool PlaySoundEffect(string audioName, float volume = 1f)
        {
            if (!enableSoundEffects || winningAudioProtected)
            {
                return false;
            }

            return PlayAudio(audioName, AudioType.SoundEffect, volume * sfxVolume * masterVolume);
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        public bool PlayBackgroundMusic(string audioName, float volume = 1f)
        {
            if (!enableBackgroundMusic)
            {
                return false;
            }

            return PlayAudio(audioName, AudioType.BackgroundMusic, volume * bgmVolume * masterVolume, true);
        }

        /// <summary>
        /// 播放中奖音效（高优先级，不会被打断）
        /// </summary>
        public bool PlayWinningSound(string audioName = "betsuccess.mp3", float volume = 1f)
        {
            // 立即设置保护状态
            SetWinningProtection(audioName, 3f);
            
            return PlayAudio(audioName, AudioType.WinningSound, volume * sfxVolume * masterVolume);
        }

        /// <summary>
        /// 根据中奖金额播放不同的音效序列
        /// </summary>
        public bool PlayWinSoundByAmount(float amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            string audioSequence = DetermineWinningAudioSequence(amount);
            string protectionKey = $"winning_amount_{amount}";
            
            // 设置音效保护期
            SetWinningProtection(protectionKey, 5f);
            
            // 播放音效序列
            StartCoroutine(PlayWinningSequence(audioSequence, amount));
            
            return true;
        }

        /// <summary>
        /// 播放开牌音效序列
        /// </summary>
        public bool PlayOpenCardSequence(List<int> flashArray, object resultInfo = null)
        {
            if (flashArray == null || flashArray.Count == 0)
            {
                Debug.LogWarning("[AudioManager] flashArray无效或为空，跳过开牌音效");
                return false;
            }

            // 停止之前的开牌序列
            if (openCardSequenceCoroutine != null)
            {
                StopCoroutine(openCardSequenceCoroutine);
            }

            // 开始新的开牌音效序列
            openCardSequenceCoroutine = StartCoroutine(PlayOpenCardSequenceCoroutine(flashArray));
            
            return true;
        }

        /// <summary>
        /// 停止所有音效
        /// </summary>
        public void StopAllSounds()
        {
            foreach (var config in audioSources)
            {
                if (config.audioSource != null && config.audioSource.isPlaying)
                {
                    config.audioSource.Stop();
                    config.currentState = AudioState.Stopped;
                }
            }
            
            Debug.Log("[AudioManager] 所有音效已停止");
        }

        /// <summary>
        /// 停止音效（不影响中奖音效）
        /// </summary>
        public void StopSoundEffects()
        {
            var sfxSources = audioSources.Where(config => 
                config.audioType == AudioType.SoundEffect && 
                config.audioType != AudioType.WinningSound);

            foreach (var config in sfxSources)
            {
                if (config.audioSource != null && config.audioSource.isPlaying)
                {
                    config.audioSource.Stop();
                    config.currentState = AudioState.Stopped;
                }
            }
            
            Debug.Log("[AudioManager] 普通音效已停止（保护中奖音效）");
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopBackgroundMusic()
        {
            var bgmSource = GetAudioSource(AudioType.BackgroundMusic);
            if (bgmSource != null && bgmSource.isPlaying)
            {
                StartCoroutine(FadeOutAudio(bgmSource, fadeTime));
            }
        }

        #endregion

        #region Core Audio Methods

        /// <summary>
        /// 核心音频播放方法
        /// </summary>
        private bool PlayAudio(string audioName, AudioType audioType, float volume, bool loop = false)
        {
            if (string.IsNullOrEmpty(audioName))
            {
                Debug.LogWarning("[AudioManager] 音频名称为空");
                return false;
            }

            // 获取音频剪辑
            AudioClip clip = LoadAudioClip(audioName);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] 音频文件未找到: {audioName}");
                return false;
            }

            // 获取对应的音频源
            AudioSource source = GetAudioSource(audioType);
            if (source == null)
            {
                Debug.LogWarning($"[AudioManager] 未找到类型为 {audioType} 的音频源");
                return false;
            }

            // 如果是中奖音效以外的音效，检查保护状态
            if (audioType != AudioType.WinningSound && winningAudioProtected)
            {
                Debug.Log($"[AudioManager] 中奖音效保护期内，跳过播放: {audioName}");
                return false;
            }

            // 播放音频
            source.clip = clip;
            source.volume = volume;
            source.loop = loop;
            source.Play();

            var config = audioSources.FirstOrDefault(c => c.audioSource == source);
            if (config != null)
            {
                config.currentState = AudioState.Playing;
            }

            Debug.Log($"[AudioManager] 播放音频: {audioName} (类型: {audioType}, 音量: {volume:F2})");
            return true;
        }

        /// <summary>
        /// 获取指定类型的音频源
        /// </summary>
        private AudioSource GetAudioSource(AudioType audioType)
        {
            var config = audioSources.FirstOrDefault(c => c.audioType == audioType);
            return config?.audioSource;
        }

        /// <summary>
        /// 加载音频剪辑
        /// </summary>
        private AudioClip LoadAudioClip(string audioName)
        {
            // 检查缓存
            if (audioClipCache.TryGetValue(audioName, out AudioClip cachedClip))
            {
                return cachedClip;
            }

            // 构建资源路径
            string resourcePath = audioResourcePath + audioName;
            
            // 移除文件扩展名（Resources.Load不需要扩展名）
            if (resourcePath.Contains("."))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.LastIndexOf('.'));
            }

            // 加载音频剪辑
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            
            if (clip != null)
            {
                // 添加到缓存
                audioClipCache[audioName] = clip;
                Debug.Log($"[AudioManager] 音频加载成功: {resourcePath}");
            }
            else
            {
                Debug.LogWarning($"[AudioManager] 音频加载失败: {resourcePath}");
            }

            return clip;
        }

        #endregion

        #region Winning Audio Protection

        /// <summary>
        /// 设置中奖音效保护
        /// </summary>
        private void SetWinningProtection(string protectionKey, float duration)
        {
            // 清除之前的保护
            if (winningProtectionCoroutine != null)
            {
                StopCoroutine(winningProtectionCoroutine);
            }

            winningAudioProtected = true;
            currentWinningProtectionKey = protectionKey;
            
            winningProtectionCoroutine = StartCoroutine(ClearWinningProtectionAfterDelay(duration));
            
            Debug.Log($"[AudioManager] 中奖音效保护已启用: {protectionKey} (持续{duration}秒)");
        }

        /// <summary>
        /// 延迟清除中奖音效保护
        /// </summary>
        private IEnumerator ClearWinningProtectionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            winningAudioProtected = false;
            currentWinningProtectionKey = "";
            winningProtectionCoroutine = null;
            
            Debug.Log("[AudioManager] 中奖音效保护已解除");
        }

        /// <summary>
        /// 强制清除中奖音效保护
        /// </summary>
        public void ClearWinningProtection()
        {
            if (winningProtectionCoroutine != null)
            {
                StopCoroutine(winningProtectionCoroutine);
                winningProtectionCoroutine = null;
            }
            
            winningAudioProtected = false;
            currentWinningProtectionKey = "";
            
            Debug.Log("[AudioManager] 🚨 强制清除中奖音效保护");
        }

        #endregion

        #region Audio Sequences

        /// <summary>
        /// 确定中奖音效序列
        /// </summary>
        private string DetermineWinningAudioSequence(float amount)
        {
            if (amount < 500)
                return "winning_small";
            else if (amount < 5000)
                return "winning_medium";
            else if (amount < 20000)
                return "winning_big";
            else
                return "winning_jackpot";
        }

        /// <summary>
        /// 播放中奖音效序列
        /// </summary>
        private IEnumerator PlayWinningSequence(string sequenceType, float amount)
        {
            Debug.Log($"[AudioManager] 🎉 播放中奖音效序列: {sequenceType} (金额: {amount})");

            switch (sequenceType)
            {
                case "winning_small":
                    PlayAudio("coin.wav", AudioType.WinningSound, sfxVolume * masterVolume);
                    break;
                    
                case "winning_medium":
                    PlayAudio("betsuccess.mp3", AudioType.WinningSound, sfxVolume * masterVolume);
                    yield return new WaitForSeconds(0.3f);
                    PlayAudio("coin.wav", AudioType.WinningSound, sfxVolume * masterVolume);
                    break;
                    
                case "winning_big":
                    PlayAudio("bigwin.wav", AudioType.WinningSound, sfxVolume * masterVolume);
                    yield return new WaitForSeconds(0.5f);
                    PlayAudio("celebration.wav", AudioType.WinningSound, sfxVolume * masterVolume);
                    break;
                    
                case "winning_jackpot":
                    PlayAudio("jackpot.wav", AudioType.WinningSound, sfxVolume * masterVolume);
                    yield return new WaitForSeconds(0.8f);
                    PlayAudio("celebration.wav", AudioType.WinningSound, sfxVolume * masterVolume);
                    yield return new WaitForSeconds(1.5f);
                    PlayAudio("coin.wav", AudioType.WinningSound, sfxVolume * masterVolume);
                    break;
            }
        }

        /// <summary>
        /// 播放开牌音效序列
        /// </summary>
        private IEnumerator PlayOpenCardSequenceCoroutine(List<int> flashArray)
        {
            isPlayingOpenCardSequence = true;
            
            // 始终先播放开牌基础音效
            PlayAudio("open/kai.mp3", AudioType.OpenCardSound, sfxVolume * masterVolume);
            yield return new WaitForSeconds(1f);
            
            // 播放对应的数字音效
            foreach (int num in flashArray)
            {
                if (num > 0 && num <= 10)
                {
                    string audioName = $"open/{num}.mp3";
                    PlayAudio(audioName, AudioType.OpenCardSound, sfxVolume * masterVolume);
                    yield return new WaitForSeconds(1f);
                }
            }
            
            isPlayingOpenCardSequence = false;
            openCardSequenceCoroutine = null;
            
            Debug.Log("✅ 开牌音效序列播放完成");
        }

        #endregion

        #region Audio Effects

        /// <summary>
        /// 音频淡出效果
        /// </summary>
        private IEnumerator FadeOutAudio(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            
            var config = audioSources.FirstOrDefault(c => c.audioSource == source);
            if (config != null)
            {
                config.currentState = AudioState.Fading;
            }
            
            while (source.volume > 0)
            {
                source.volume -= startVolume * Time.deltaTime / duration;
                yield return null;
            }
            
            source.Stop();
            source.volume = startVolume;
            
            if (config != null)
            {
                config.currentState = AudioState.Stopped;
            }
        }

        /// <summary>
        /// 音频淡入效果
        /// </summary>
        private IEnumerator FadeInAudio(AudioSource source, float targetVolume, float duration)
        {
            source.volume = 0f;
            source.Play();
            
            var config = audioSources.FirstOrDefault(c => c.audioSource == source);
            if (config != null)
            {
                config.currentState = AudioState.Fading;
            }
            
            while (source.volume < targetVolume)
            {
                source.volume += targetVolume * Time.deltaTime / duration;
                yield return null;
            }
            
            source.volume = targetVolume;
            
            if (config != null)
            {
                config.currentState = AudioState.Playing;
            }
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// 设置主音量
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllAudioVolumes();
            Debug.Log($"[AudioManager] 主音量设置为: {masterVolume:F2}");
        }

        /// <summary>
        /// 设置音效音量
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateAllAudioVolumes();
            Debug.Log($"[AudioManager] 音效音量设置为: {sfxVolume:F2}");
        }

        /// <summary>
        /// 设置背景音乐音量
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            UpdateAllAudioVolumes();
            Debug.Log($"[AudioManager] 背景音乐音量设置为: {bgmVolume:F2}");
        }

        /// <summary>
        /// 设置音效开关
        /// </summary>
        public void SetSoundEffectsEnabled(bool enabled)
        {
            enableSoundEffects = enabled;
            
            if (!enabled)
            {
                StopSoundEffects();
            }
            
            Debug.Log($"[AudioManager] 音效{(enabled ? "开启" : "关闭")}");
        }

        /// <summary>
        /// 设置背景音乐开关
        /// </summary>
        public void SetBackgroundMusicEnabled(bool enabled)
        {
            enableBackgroundMusic = enabled;
            
            if (!enabled)
            {
                StopBackgroundMusic();
            }
            
            Debug.Log($"[AudioManager] 背景音乐{(enabled ? "开启" : "关闭")}");
        }

        /// <summary>
        /// 更新所有音频源的音量
        /// </summary>
        private void UpdateAllAudioVolumes()
        {
            foreach (var config in audioSources)
            {
                if (config.audioSource != null && config.audioSource.isPlaying)
                {
                    float volume = config.volume * masterVolume;
                    
                    switch (config.audioType)
                    {
                        case AudioType.SoundEffect:
                        case AudioType.WinningSound:
                        case AudioType.OpenCardSound:
                            volume *= sfxVolume;
                            break;
                        case AudioType.BackgroundMusic:
                            volume *= bgmVolume;
                            break;
                    }
                    
                    config.audioSource.volume = volume;
                }
            }
        }

        #endregion

        #region WebGL Optimizations

        /// <summary>
        /// 应用WebGL优化设置
        /// </summary>
        private void ApplyWebGLOptimizations()
        {
            // 减少最大并发音效数量
            maxConcurrentSounds = Mathf.Min(maxConcurrentSounds, 5);
            
            // 设置合适的音频设置
            AudioSettings.GetConfiguration(out var config);
            config.dspBufferSize = 1024; // 适合WebGL的缓冲区大小
            AudioSettings.Reset(config);
            
            Debug.Log("[AudioManager] WebGL音频优化设置已应用");
        }

        #endregion

        #region Status and Debug

        /// <summary>
        /// 获取音频状态
        /// </summary>
        public Dictionary<string, object> GetAudioStatus()
        {
            return new Dictionary<string, object>
            {
                { "masterVolume", masterVolume },
                { "sfxVolume", sfxVolume },
                { "bgmVolume", bgmVolume },
                { "enableSoundEffects", enableSoundEffects },
                { "enableBackgroundMusic", enableBackgroundMusic },
                { "winningAudioProtected", winningAudioProtected },
                { "currentWinningProtectionKey", currentWinningProtectionKey },
                { "isPlayingOpenCardSequence", isPlayingOpenCardSequence },
                { "cachedAudioClips", audioClipCache.Count },
                { "activeAudioSources", audioSources.Count(c => c.audioSource != null && c.audioSource.isPlaying) }
            };
        }

        /// <summary>
        /// 清空音频队列
        /// </summary>
        public void ClearAudioQueue()
        {
            audioQueue.Clear();
            isProcessingQueue = false;
            Debug.Log("[AudioManager] 🧹 音频队列已清空");
        }

        /// <summary>
        /// 输出调试信息
        /// </summary>
        public void DebugAudioInfo()
        {
            Debug.Log("=== AudioManager 调试信息 ===");
            Debug.Log($"音频状态: {GetAudioStatus()}");
            Debug.Log($"音频源数量: {audioSources.Count}");
            Debug.Log($"缓存的音频剪辑: {audioClipCache.Count}");
            foreach (var kvp in audioClipCache)
            {
                Debug.Log($"  - {kvp.Key}: {(kvp.Value != null ? "已加载" : "空")}");
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            Debug.Log("[AudioManager] 🧹 清理音频资源");
            
            StopAllSounds();
            ClearWinningProtection();
            ClearAudioQueue();
            
            // 清理协程
            if (openCardSequenceCoroutine != null)
            {
                StopCoroutine(openCardSequenceCoroutine);
                openCardSequenceCoroutine = null;
            }
            
            // 清理音频剪辑缓存
            audioClipCache.Clear();
            
            isPlayingOpenCardSequence = false;
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion
    }
}