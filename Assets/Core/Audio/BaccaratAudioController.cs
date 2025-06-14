// ================================================================================================
// 百家乐音频控制器 - BaccaratAudioController.cs
// 用途：百家乐游戏专用的音频控制器，对应JavaScript项目中的useAudio业务逻辑
// ================================================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaccaratGame.Config;
using BaccaratGame.Data.Types;

namespace BaccaratGame.Audio
{
    /// <summary>
    /// 游戏音效序列类型
    /// </summary>
    public enum GameAudioSequence
    {
        BetPlaced,          // 下注成功
        BetSuccess,         // 投注确认
        BetPeriodStart,     // 下注期开始
        BetPeriodEnd,       // 下注期结束
        CardOpening,        // 开牌音效序列
        WelcomeSequence,    // 欢迎音效序列
        WinningSmall,       // 小额中奖
        WinningMedium,      // 中等中奖
        WinningBig,         // 大额中奖
        WinningJackpot,     // 超级中奖
        WinningByAmount     // 根据金额播放中奖音效
    }

    /// <summary>
    /// 游戏结果类型（用于音效）
    /// </summary>
    public enum GameResultType
    {
        BankerWin = 1,      // 庄赢/龙赢
        PlayerWin = 2,      // 闲赢/虎赢
        TieWin = 3          // 和牌
    }

    /// <summary>
    /// 百家乐音频控制器 - 管理百家乐游戏的专用音频逻辑
    /// 对应JavaScript项目中useAudio的游戏相关功能
    /// </summary>
    public class BaccaratAudioController : MonoBehaviour
    {
        [Header("🎮 Game Audio Settings")]
        [Tooltip("游戏类型（2=龙虎，3=百家乐）")]
        public int gameType = 3;
        
        [Tooltip("是否启用游戏音效")]
        public bool enableGameAudio = true;
        
        [Tooltip("是否启用中奖音效")]
        public bool enableWinningAudio = true;
        
        [Tooltip("是否启用开牌音效")]
        public bool enableOpenCardAudio = true;

        [Header("🔊 Audio Volume Settings")]
        [Tooltip("游戏音效音量")]
        [Range(0f, 1f)]
        public float gameAudioVolume = 0.8f;
        
        [Tooltip("中奖音效音量")]
        [Range(0f, 1f)]
        public float winningAudioVolume = 1f;
        
        [Tooltip("开牌音效音量")]
        [Range(0f, 1f)]
        public float openCardAudioVolume = 0.9f;

        [Header("📋 Audio File Mapping")]
        [Tooltip("音效文件映射配置")]
        public AudioFileMapping audioMapping;

        // 音频管理器引用
        private AudioManager audioManager;
        
        // 音频状态
        private bool isInitialized = false;
        private bool userSettingsLoaded = false;
        
        // 中奖音效控制
        private string lastPlayedWinningKey = "";
        private float lastWinningAmount = 0f;
        
        // 开牌音效控制
        private bool isPlayingOpenCardSequence = false;
        private Coroutine openCardSequenceCoroutine;

        /// <summary>
        /// 音效文件映射配置
        /// </summary>
        [System.Serializable]
        public class AudioFileMapping
        {
            [Header("🎯 Betting Audio")]
            public string betSound = "betSound.mp3";
            public string betSuccessSound = "betsuccess.mp3";
            public string cancelSound = "cancel.wav";
            public string confirmSound = "confirm.wav";

            [Header("⏰ Game Phase Audio")]
            public string startBetSound = "bet.wav";
            public string stopBetSound = "stop.wav";
            public string tipSound = "tip.wav";
            public string errorSound = "error.wav";

            [Header("🃏 Card Audio")]
            public string openCardSound = "OPENCARD.mp3";
            public string dealCardSound = "kai.mp3";

            [Header("🎉 Winning Audio")]
            public string coinSound = "coin.wav";
            public string bigWinSound = "bigwin.wav";
            public string celebrationSound = "celebration.wav";
            public string jackpotSound = "jackpot.wav";

            [Header("🎵 Game Result Audio")]
            public string bankerWinSound = "bankerWin.wav";
            public string playerWinSound = "playerWin.wav";
            public string dragonWinSound = "dragonWin.wav";
            public string tigerWinSound = "tigerWin.wav";
            public string tieSound = "tie.wav";

            [Header("🏠 Welcome Audio")]
            public string welcomeSound = "welcome.wav";
            public string backgroundMusic = "bgm.mp3";
        }

        #region Unity Lifecycle

        private void Awake()
        {
            // 获取AudioManager实例
            audioManager = AudioManager.Instance;
            
            // 初始化默认音效映射
            if (audioMapping == null)
            {
                InitializeDefaultAudioMapping();
            }
        }

        private void Start()
        {
            InitializeAudioController();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化音频控制器
        /// </summary>
        public void InitializeAudioController()
        {
            if (audioManager == null)
            {
                Debug.LogError("[BaccaratAudioController] AudioManager未找到");
                return;
            }

            // 加载用户音频设置
            LoadUserAudioSettings();
            
            isInitialized = true;
            
            Debug.Log($"[BaccaratAudioController] 初始化完成 - 游戏类型: {gameType}");
        }

        /// <summary>
        /// 初始化默认音效映射
        /// </summary>
        private void InitializeDefaultAudioMapping()
        {
            audioMapping = new AudioFileMapping();
            Debug.Log("[BaccaratAudioController] 使用默认音效映射");
        }

        /// <summary>
        /// 加载用户音频设置
        /// </summary>
        private void LoadUserAudioSettings()
        {
            try
            {
                // 从本地存储或配置文件加载用户音频偏好
                enableGameAudio = PlayerPrefs.GetInt("EnableGameAudio", 1) == 1;
                enableWinningAudio = PlayerPrefs.GetInt("EnableWinningAudio", 1) == 1;
                enableOpenCardAudio = PlayerPrefs.GetInt("EnableOpenCardAudio", 1) == 1;
                
                gameAudioVolume = PlayerPrefs.GetFloat("GameAudioVolume", 0.8f);
                winningAudioVolume = PlayerPrefs.GetFloat("WinningAudioVolume", 1f);
                openCardAudioVolume = PlayerPrefs.GetFloat("OpenCardAudioVolume", 0.9f);
                
                userSettingsLoaded = true;
                
                Debug.Log("[BaccaratAudioController] 用户音频设置加载完成");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BaccaratAudioController] 加载用户音频设置失败: {ex.Message}");
                SetDefaultAudioSettings();
            }
        }

        /// <summary>
        /// 设置默认音频设置
        /// </summary>
        private void SetDefaultAudioSettings()
        {
            enableGameAudio = true;
            enableWinningAudio = true;
            enableOpenCardAudio = true;
            gameAudioVolume = 0.8f;
            winningAudioVolume = 1f;
            openCardAudioVolume = 0.9f;
            userSettingsLoaded = true;
            
            Debug.Log("[BaccaratAudioController] 使用默认音频设置");
        }

        #endregion

        #region Public Game Audio Methods

        /// <summary>
        /// 播放下注音效
        /// </summary>
        public bool PlayBetSound()
        {
            if (!enableGameAudio || !isInitialized) return false;
            
            return audioManager.PlaySoundEffect(audioMapping.betSound, gameAudioVolume);
        }

        /// <summary>
        /// 播放下注成功音效
        /// </summary>
        public bool PlayBetSuccessSound()
        {
            if (!enableGameAudio || !isInitialized) return false;
            
            return audioManager.PlaySoundEffect(audioMapping.betSuccessSound, gameAudioVolume);
        }

        /// <summary>
        /// 播放取消音效
        /// </summary>
        public bool PlayCancelSound()
        {
            if (!enableGameAudio || !isInitialized) return false;
            
            return audioManager.PlaySoundEffect(audioMapping.cancelSound, gameAudioVolume);
        }

        /// <summary>
        /// 播放确认音效
        /// </summary>
        public bool PlayConfirmSound()
        {
            if (!enableGameAudio || !isInitialized) return false;
            
            return audioManager.PlaySoundEffect(audioMapping.confirmSound, gameAudioVolume);
        }

        /// <summary>
        /// 播放提示音效
        /// </summary>
        public bool PlayTipSound()
        {
            if (!enableGameAudio || !isInitialized) return false;
            
            return audioManager.PlaySoundEffect(audioMapping.tipSound, gameAudioVolume);
        }

        /// <summary>
        /// 播放错误音效
        /// </summary>
        public bool PlayErrorSound()
        {
            if (!enableGameAudio || !isInitialized) return false;
            
            return audioManager.PlaySoundEffect(audioMapping.errorSound, gameAudioVolume);
        }

        /// <summary>
        /// 播放下注期开始音效
        /// </summary>
        public bool PlayStartBetSound()
        {
            if (!enableGameAudio || !isInitialized) return false;
            
            return audioManager.PlaySoundEffect(audioMapping.startBetSound, gameAudioVolume);
        }

        /// <summary>
        /// 播放下注期结束音效
        /// </summary>
        public bool PlayStopBetSound()
        {
            if (!enableGameAudio || !isInitialized) return false;
            
            return audioManager.PlaySoundEffect(audioMapping.stopBetSound, gameAudioVolume);
        }

        /// <summary>
        /// 播放开牌音效
        /// </summary>
        public bool PlayOpenCardSound()
        {
            if (!enableOpenCardAudio || !isInitialized) return false;
            
            return audioManager.PlaySoundEffect(audioMapping.openCardSound, openCardAudioVolume);
        }

        /// <summary>
        /// 播放欢迎音效
        /// </summary>
        public bool PlayWelcomeSound()
        {
            if (!enableGameAudio || !isInitialized) return false;
            
            return audioManager.PlaySoundEffect(audioMapping.welcomeSound, gameAudioVolume);
        }

        #endregion

        #region Winning Audio Methods

        /// <summary>
        /// 根据中奖金额播放中奖音效
        /// </summary>
        public bool PlayWinSoundByAmount(float amount)
        {
            if (!enableWinningAudio || !isInitialized || amount <= 0) return false;

            // 防止重复播放相同金额的中奖音效
            string winningKey = $"win_{amount}_{Time.time:F1}";
            if (winningKey == lastPlayedWinningKey && Math.Abs(amount - lastWinningAmount) < 0.01f)
            {
                Debug.Log($"[BaccaratAudioController] 跳过重复的中奖音效: {amount}");
                return false;
            }

            lastPlayedWinningKey = winningKey;
            lastWinningAmount = amount;

            // 使用AudioManager的中奖音效方法
            return audioManager.PlayWinSoundByAmount(amount);
        }

        /// <summary>
        /// 播放基础中奖音效
        /// </summary>
        public bool PlayWinningSound()
        {
            if (!enableWinningAudio || !isInitialized) return false;
            
            return audioManager.PlayWinningSound(audioMapping.betSuccessSound, winningAudioVolume);
        }

        /// <summary>
        /// 播放大奖音效
        /// </summary>
        public bool PlayBigWinSound()
        {
            if (!enableWinningAudio || !isInitialized) return false;
            
            return audioManager.PlayWinningSound(audioMapping.bigWinSound, winningAudioVolume);
        }

        /// <summary>
        /// 播放金币音效
        /// </summary>
        public bool PlayCoinSound()
        {
            if (!enableWinningAudio || !isInitialized) return false;
            
            return audioManager.PlayWinningSound(audioMapping.coinSound, winningAudioVolume);
        }

        /// <summary>
        /// 播放庆祝音效
        /// </summary>
        public bool PlayCelebrationSound()
        {
            if (!enableWinningAudio || !isInitialized) return false;
            
            return audioManager.PlayWinningSound(audioMapping.celebrationSound, winningAudioVolume);
        }

        /// <summary>
        /// 播放超级大奖音效
        /// </summary>
        public bool PlayJackpotSound()
        {
            if (!enableWinningAudio || !isInitialized) return false;
            
            return audioManager.PlayWinningSound(audioMapping.jackpotSound, winningAudioVolume);
        }

        #endregion

        #region Game Result Audio

        /// <summary>
        /// 播放游戏结果音效
        /// </summary>
        public bool PlayResultSound(GameResultType result)
        {
            if (!enableGameAudio || !isInitialized) return false;

            string soundFile = GetResultAudioFile(result);
            if (string.IsNullOrEmpty(soundFile)) return false;

            return audioManager.PlaySoundEffect(soundFile, gameAudioVolume);
        }

        /// <summary>
        /// 播放游戏结果音效（使用数字结果）
        /// </summary>
        public bool PlayResultSound(int result)
        {
            GameResultType resultType = (GameResultType)result;
            return PlayResultSound(resultType);
        }

        /// <summary>
        /// 获取结果音效文件名
        /// </summary>
        private string GetResultAudioFile(GameResultType result)
        {
            switch (result)
            {
                case GameResultType.BankerWin:
                    return gameType == 3 ? audioMapping.bankerWinSound : audioMapping.dragonWinSound;
                case GameResultType.PlayerWin:
                    return gameType == 3 ? audioMapping.playerWinSound : audioMapping.tigerWinSound;
                case GameResultType.TieWin:
                    return audioMapping.tieSound;
                default:
                    Debug.LogWarning($"[BaccaratAudioController] 未知的游戏结果: {result}");
                    return "";
            }
        }

        #endregion

        #region Open Card Audio Sequence

        /// <summary>
        /// 播放开牌音效序列
        /// </summary>
        public bool PlayOpenCardSequence(List<int> flashArray, object resultInfo = null)
        {
            if (!enableOpenCardAudio || !isInitialized) return false;

            if (flashArray == null || flashArray.Count == 0)
            {
                Debug.LogWarning("[BaccaratAudioController] flashArray无效或为空，跳过开牌音效");
                return false;
            }

            // 停止之前的开牌序列
            if (openCardSequenceCoroutine != null)
            {
                StopCoroutine(openCardSequenceCoroutine);
                openCardSequenceCoroutine = null;
            }

            // 使用AudioManager的开牌音效序列
            bool success = audioManager.PlayOpenCardSequence(flashArray, resultInfo);
            
            if (success)
            {
                Debug.Log($"[BaccaratAudioController] 开牌音效序列启动: {string.Join(",", flashArray)}");
            }

            return success;
        }

        #endregion

        #region Background Music

        /// <summary>
        /// 开始播放背景音乐
        /// </summary>
        public bool StartBackgroundMusic()
        {
            if (!isInitialized) return false;
            
            return audioManager.PlayBackgroundMusic(audioMapping.backgroundMusic, 1f);
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopBackgroundMusic()
        {
            if (!isInitialized) return;
            
            audioManager.StopBackgroundMusic();
        }

        #endregion

        #region Game Audio Sequences

        /// <summary>
        /// 播放游戏音效序列
        /// </summary>
        public bool PlayGameSequence(GameAudioSequence sequence, Dictionary<string, object> parameters = null)
        {
            if (!isInitialized) return false;

            switch (sequence)
            {
                case GameAudioSequence.BetPlaced:
                    return PlayBetSound();

                case GameAudioSequence.BetSuccess:
                    return PlayBetSuccessSound();

                case GameAudioSequence.BetPeriodStart:
                    return PlayStartBetSound();

                case GameAudioSequence.BetPeriodEnd:
                    StartCoroutine(DelayedStopBetSound());
                    return true;

                case GameAudioSequence.CardOpening:
                    if (parameters != null && parameters.ContainsKey("flashArray"))
                    {
                        var flashArray = parameters["flashArray"] as List<int>;
                        var resultInfo = parameters.ContainsKey("resultInfo") ? parameters["resultInfo"] : null;
                        return PlayOpenCardSequence(flashArray, resultInfo);
                    }
                    return false;

                case GameAudioSequence.WelcomeSequence:
                    return PlayWelcomeAudio();

                case GameAudioSequence.WinningSmall:
                    return PlayCoinSound();

                case GameAudioSequence.WinningMedium:
                    return PlayWinningMediumSequence();

                case GameAudioSequence.WinningBig:
                    return PlayWinningBigSequence();

                case GameAudioSequence.WinningJackpot:
                    return PlayWinningJackpotSequence();

                case GameAudioSequence.WinningByAmount:
                    if (parameters != null && parameters.ContainsKey("amount"))
                    {
                        float amount = Convert.ToSingle(parameters["amount"]);
                        return PlayWinSoundByAmount(amount);
                    }
                    return false;

                default:
                    Debug.LogWarning($"[BaccaratAudioController] 未知的音效序列: {sequence}");
                    return false;
            }
        }

        /// <summary>
        /// 延迟播放停止下注音效
        /// </summary>
        private IEnumerator DelayedStopBetSound()
        {
            yield return new WaitForSeconds(1f);
            PlayStopBetSound();
        }

        /// <summary>
        /// 播放欢迎音频序列
        /// </summary>
        private bool PlayWelcomeAudio()
        {
            PlayWelcomeSound();
            StartBackgroundMusic();
            return true;
        }

        /// <summary>
        /// 播放中等中奖音效序列
        /// </summary>
        private bool PlayWinningMediumSequence()
        {
            StartCoroutine(PlayWinningMediumCoroutine());
            return true;
        }

        private IEnumerator PlayWinningMediumCoroutine()
        {
            PlayWinningSound();
            yield return new WaitForSeconds(0.3f);
            PlayCoinSound();
        }

        /// <summary>
        /// 播放大额中奖音效序列
        /// </summary>
        private bool PlayWinningBigSequence()
        {
            StartCoroutine(PlayWinningBigCoroutine());
            return true;
        }

        private IEnumerator PlayWinningBigCoroutine()
        {
            PlayBigWinSound();
            yield return new WaitForSeconds(0.5f);
            PlayCelebrationSound();
        }

        /// <summary>
        /// 播放超级大奖音效序列
        /// </summary>
        private bool PlayWinningJackpotSequence()
        {
            StartCoroutine(PlayWinningJackpotCoroutine());
            return true;
        }

        private IEnumerator PlayWinningJackpotCoroutine()
        {
            PlayJackpotSound();
            yield return new WaitForSeconds(0.8f);
            PlayCelebrationSound();
            yield return new WaitForSeconds(1.5f);
            PlayCoinSound();
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// 设置游戏音效开关
        /// </summary>
        public void SetGameAudioEnabled(bool enabled)
        {
            enableGameAudio = enabled;
            PlayerPrefs.SetInt("EnableGameAudio", enabled ? 1 : 0);
            
            Debug.Log($"[BaccaratAudioController] 游戏音效{(enabled ? "开启" : "关闭")}");
        }

        /// <summary>
        /// 设置中奖音效开关
        /// </summary>
        public void SetWinningAudioEnabled(bool enabled)
        {
            enableWinningAudio = enabled;
            PlayerPrefs.SetInt("EnableWinningAudio", enabled ? 1 : 0);
            
            Debug.Log($"[BaccaratAudioController] 中奖音效{(enabled ? "开启" : "关闭")}");
        }

        /// <summary>
        /// 设置开牌音效开关
        /// </summary>
        public void SetOpenCardAudioEnabled(bool enabled)
        {
            enableOpenCardAudio = enabled;
            PlayerPrefs.SetInt("EnableOpenCardAudio", enabled ? 1 : 0);
            
            Debug.Log($"[BaccaratAudioController] 开牌音效{(enabled ? "开启" : "关闭")}");
        }

        /// <summary>
        /// 设置游戏音效音量
        /// </summary>
        public void SetGameAudioVolume(float volume)
        {
            gameAudioVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("GameAudioVolume", gameAudioVolume);
            
            Debug.Log($"[BaccaratAudioController] 游戏音效音量设置为: {gameAudioVolume:F2}");
        }

        /// <summary>
        /// 设置中奖音效音量
        /// </summary>
        public void SetWinningAudioVolume(float volume)
        {
            winningAudioVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("WinningAudioVolume", winningAudioVolume);
            
            Debug.Log($"[BaccaratAudioController] 中奖音效音量设置为: {winningAudioVolume:F2}");
        }

        /// <summary>
        /// 保存用户音频设置
        /// </summary>
        public void SaveUserAudioSettings()
        {
            PlayerPrefs.SetInt("EnableGameAudio", enableGameAudio ? 1 : 0);
            PlayerPrefs.SetInt("EnableWinningAudio", enableWinningAudio ? 1 : 0);
            PlayerPrefs.SetInt("EnableOpenCardAudio", enableOpenCardAudio ? 1 : 0);
            PlayerPrefs.SetFloat("GameAudioVolume", gameAudioVolume);
            PlayerPrefs.SetFloat("WinningAudioVolume", winningAudioVolume);
            PlayerPrefs.SetFloat("OpenCardAudioVolume", openCardAudioVolume);
            PlayerPrefs.Save();
            
            Debug.Log("[BaccaratAudioController] 用户音频设置已保存");
        }

        #endregion

        #region Status and Debug

        /// <summary>
        /// 获取音频控制器状态
        /// </summary>
        public Dictionary<string, object> GetAudioControllerStatus()
        {
            return new Dictionary<string, object>
            {
                { "isInitialized", isInitialized },
                { "userSettingsLoaded", userSettingsLoaded },
                { "gameType", gameType },
                { "enableGameAudio", enableGameAudio },
                { "enableWinningAudio", enableWinningAudio },
                { "enableOpenCardAudio", enableOpenCardAudio },
                { "gameAudioVolume", gameAudioVolume },
                { "winningAudioVolume", winningAudioVolume },
                { "openCardAudioVolume", openCardAudioVolume },
                { "lastWinningAmount", lastWinningAmount },
                { "isPlayingOpenCardSequence", isPlayingOpenCardSequence }
            };
        }

        /// <summary>
        /// 测试所有音效
        /// </summary>
        public void TestAllAudioEffects()
        {
            StartCoroutine(TestAudioSequence());
        }

        private IEnumerator TestAudioSequence()
        {
            Debug.Log("[BaccaratAudioController] 开始测试所有音效");
            
            yield return new WaitForSeconds(0.5f);
            PlayBetSound();
            
            yield return new WaitForSeconds(1f);
            PlayBetSuccessSound();
            
            yield return new WaitForSeconds(1f);
            PlayConfirmSound();
            
            yield return new WaitForSeconds(1f);
            PlayStartBetSound();
            
            yield return new WaitForSeconds(1f);
            PlayStopBetSound();
            
            yield return new WaitForSeconds(1f);
            PlayOpenCardSound();
            
            yield return new WaitForSeconds(1f);
            PlayWinningSound();
            
            Debug.Log("[BaccaratAudioController] 音效测试完成");
        }

        /// <summary>
        /// 输出调试信息
        /// </summary>
        public void DebugAudioControllerInfo()
        {
            Debug.Log("=== BaccaratAudioController 调试信息 ===");
            Debug.Log($"控制器状态: {GetAudioControllerStatus()}");
            
            if (audioManager != null)
            {
                audioManager.DebugAudioInfo();
            }
            else
            {
                Debug.LogWarning("AudioManager引用为空");
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            Debug.Log("[BaccaratAudioController] 🧹 清理音频控制器资源");
            
            // 停止所有协程
            StopAllCoroutines();
            
            // 清理状态
            isPlayingOpenCardSequence = false;
            openCardSequenceCoroutine = null;
            lastPlayedWinningKey = "";
            lastWinningAmount = 0f;
            
            // 保存用户设置
            if (userSettingsLoaded)
            {
                SaveUserAudioSettings();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion
    }
}