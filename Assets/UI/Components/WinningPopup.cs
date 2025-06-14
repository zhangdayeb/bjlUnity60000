using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace SlotMachine.UI
{
    /// <summary>
    /// 获奖弹窗组件 - 显示玩家获奖信息和动画效果
    /// </summary>
    public class WinningPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TextMeshProUGUI winAmountText;
        [SerializeField] private TextMeshProUGUI congratulationsText;
        [SerializeField] private TextMeshProUGUI winTypeText;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Image backgroundOverlay;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem celebrationParticles;
        [SerializeField] private ParticleSystem coinRainEffect;
        [SerializeField] private GameObject[] confettiEffects;
        [SerializeField] private Image[] rayEffects;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip smallWinSound;
        [SerializeField] private AudioClip bigWinSound;
        [SerializeField] private AudioClip jackpotSound;
        [SerializeField] private AudioClip collectSound;
        
        [Header("Animation Settings")]
        [SerializeField] private float popupDuration = 0.5f;
        [SerializeField] private float textAnimationDelay = 0.3f;
        [SerializeField] private float particleDelay = 0.5f;
        [SerializeField] private AnimationCurve popupCurve = AnimationCurve.EaseOutBack(0, 0, 1, 1);
        
        [Header("Win Type Thresholds")]
        [SerializeField] private float bigWinThreshold = 100f;
        [SerializeField] private float megaWinThreshold = 500f;
        [SerializeField] private float jackpotThreshold = 1000f;
        
        [Header("Colors")]
        [SerializeField] private Color smallWinColor = Color.yellow;
        [SerializeField] private Color bigWinColor = Color.orange;
        [SerializeField] private Color megaWinColor = Color.red;
        [SerializeField] private Color jackpotColor = Color.magenta;
        
        // 私有变量
        private float currentWinAmount;
        private WinType currentWinType;
        private bool isShowing;
        private Sequence animationSequence;
        
        // 事件
        public System.Action<float> OnWinAmountCollected;
        public System.Action OnPopupClosed;
        
        /// <summary>
        /// 获奖类型枚举
        /// </summary>
        public enum WinType
        {
            SmallWin,
            BigWin,
            MegaWin,
            Jackpot
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
            SetupButtons();
        }
        
        private void Start()
        {
            HidePopup(false);
        }
        
        private void OnDestroy()
        {
            // 清理动画序列
            if (animationSequence != null && animationSequence.IsActive())
            {
                animationSequence.Kill();
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 验证必要组件
            if (popupPanel == null)
            {
                Debug.LogError("WinningPopup: 缺少 popupPanel 引用!");
            }
            
            if (winAmountText == null)
            {
                Debug.LogError("WinningPopup: 缺少 winAmountText 引用!");
            }
            
            // 初始化音频源
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }
        
        /// <summary>
        /// 设置按钮事件
        /// </summary>
        private void SetupButtons()
        {
            if (collectButton != null)
            {
                collectButton.onClick.AddListener(OnCollectButtonClicked);
            }
            
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueButtonClicked);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 显示获奖弹窗
        /// </summary>
        /// <param name="winAmount">获奖金额</param>
        /// <param name="multiplier">倍数（可选）</param>
        public void ShowWinPopup(float winAmount, float multiplier = 1f)
        {
            if (isShowing) return;
            
            currentWinAmount = winAmount;
            currentWinType = DetermineWinType(winAmount);
            
            // 设置文本内容
            SetupWinTexts(winAmount, multiplier);
            
            // 设置颜色主题
            SetupColorTheme();
            
            // 显示弹窗
            ShowPopup();
            
            // 播放音效
            PlayWinSound();
            
            // 开始动画序列
            StartWinAnimation();
        }
        
        /// <summary>
        /// 隐藏弹窗
        /// </summary>
        /// <param name="animate">是否使用动画</param>
        public void HidePopup(bool animate = true)
        {
            if (!isShowing && animate) return;
            
            if (animate)
            {
                StartHideAnimation();
            }
            else
            {
                popupPanel.SetActive(false);
                isShowing = false;
                StopAllEffects();
            }
        }
        
        /// <summary>
        /// 检查弹窗是否正在显示
        /// </summary>
        /// <returns>是否正在显示</returns>
        public bool IsShowing()
        {
            return isShowing;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 确定获奖类型
        /// </summary>
        /// <param name="amount">获奖金额</param>
        /// <returns>获奖类型</returns>
        private WinType DetermineWinType(float amount)
        {
            if (amount >= jackpotThreshold)
                return WinType.Jackpot;
            else if (amount >= megaWinThreshold)
                return WinType.MegaWin;
            else if (amount >= bigWinThreshold)
                return WinType.BigWin;
            else
                return WinType.SmallWin;
        }
        
        /// <summary>
        /// 设置获奖文本
        /// </summary>
        /// <param name="amount">获奖金额</param>
        /// <param name="multiplier">倍数</param>
        private void SetupWinTexts(float amount, float multiplier)
        {
            // 设置获奖金额
            if (winAmountText != null)
            {
                winAmountText.text = $"{amount:F2}";
            }
            
            // 设置祝贺文本
            if (congratulationsText != null)
            {
                congratulationsText.text = GetCongratulationsText();
            }
            
            // 设置获奖类型文本
            if (winTypeText != null)
            {
                string typeText = GetWinTypeText();
                if (multiplier > 1f)
                {
                    typeText += $" x{multiplier:F1}";
                }
                winTypeText.text = typeText;
            }
        }
        
        /// <summary>
        /// 获取祝贺文本
        /// </summary>
        /// <returns>祝贺文本</returns>
        private string GetCongratulationsText()
        {
            switch (currentWinType)
            {
                case WinType.SmallWin:
                    return "恭喜获奖!";
                case WinType.BigWin:
                    return "大奖来了!";
                case WinType.MegaWin:
                    return "超级大奖!";
                case WinType.Jackpot:
                    return "🎉 头奖! 🎉";
                default:
                    return "恭喜!";
            }
        }
        
        /// <summary>
        /// 获取获奖类型文本
        /// </summary>
        /// <returns>获奖类型文本</returns>
        private string GetWinTypeText()
        {
            switch (currentWinType)
            {
                case WinType.SmallWin:
                    return "小奖";
                case WinType.BigWin:
                    return "大奖";
                case WinType.MegaWin:
                    return "超级大奖";
                case WinType.Jackpot:
                    return "头奖";
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// 设置颜色主题
        /// </summary>
        private void SetupColorTheme()
        {
            Color themeColor = GetThemeColor();
            
            // 应用主题颜色到各个UI元素
            if (winAmountText != null)
                winAmountText.color = themeColor;
                
            if (winTypeText != null)
                winTypeText.color = themeColor;
        }
        
        /// <summary>
        /// 获取主题颜色
        /// </summary>
        /// <returns>主题颜色</returns>
        private Color GetThemeColor()
        {
            switch (currentWinType)
            {
                case WinType.SmallWin:
                    return smallWinColor;
                case WinType.BigWin:
                    return bigWinColor;
                case WinType.MegaWin:
                    return megaWinColor;
                case WinType.Jackpot:
                    return jackpotColor;
                default:
                    return Color.white;
            }
        }
        
        #endregion
        
        #region Animation
        
        /// <summary>
        /// 显示弹窗
        /// </summary>
        private void ShowPopup()
        {
            isShowing = true;
            popupPanel.SetActive(true);
            
            // 设置初始状态
            popupPanel.transform.localScale = Vector3.zero;
            if (backgroundOverlay != null)
            {
                Color overlayColor = backgroundOverlay.color;
                overlayColor.a = 0f;
                backgroundOverlay.color = overlayColor;
            }
        }
        
        /// <summary>
        /// 开始获奖动画序列
        /// </summary>
        private void StartWinAnimation()
        {
            // 清理之前的动画
            if (animationSequence != null && animationSequence.IsActive())
            {
                animationSequence.Kill();
            }
            
            animationSequence = DOTween.Sequence();
            
            // 背景淡入
            if (backgroundOverlay != null)
            {
                animationSequence.Join(backgroundOverlay.DOFade(0.7f, popupDuration * 0.5f));
            }
            
            // 弹窗缩放动画
            animationSequence.Join(popupPanel.transform.DOScale(1f, popupDuration).SetEase(popupCurve));
            
            // 文本动画
            animationSequence.AppendCallback(() => StartTextAnimations());
            
            // 粒子效果
            animationSequence.AppendInterval(particleDelay);
            animationSequence.AppendCallback(() => StartParticleEffects());
            
            // 光线效果
            if (rayEffects != null && rayEffects.Length > 0)
            {
                animationSequence.AppendCallback(() => StartRayEffects());
            }
        }
        
        /// <summary>
        /// 开始文本动画
        /// </summary>
        private void StartTextAnimations()
        {
            // 祝贺文本动画
            if (congratulationsText != null)
            {
                congratulationsText.transform.localScale = Vector3.zero;
                congratulationsText.transform.DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(0.1f);
            }
            
            // 获奖金额动画
            if (winAmountText != null)
            {
                winAmountText.transform.localScale = Vector3.zero;
                winAmountText.transform.DOScale(1f, 0.4f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(0.2f);
                    
                // 数字滚动效果
                StartCoroutine(AnimateWinAmount());
            }
            
            // 获奖类型文本动画
            if (winTypeText != null)
            {
                winTypeText.transform.localScale = Vector3.zero;
                winTypeText.transform.DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(0.3f);
            }
        }
        
        /// <summary>
        /// 获奖金额数字滚动动画
        /// </summary>
        private System.Collections.IEnumerator AnimateWinAmount()
        {
            yield return new WaitForSeconds(0.5f);
            
            float duration = 1f;
            float elapsed = 0f;
            float startAmount = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float currentAmount = Mathf.Lerp(startAmount, currentWinAmount, progress);
                
                if (winAmountText != null)
                {
                    winAmountText.text = $"{currentAmount:F2}";
                }
                
                yield return null;
            }
            
            // 确保最终值正确
            if (winAmountText != null)
            {
                winAmountText.text = $"{currentWinAmount:F2}";
            }
        }
        
        /// <summary>
        /// 开始粒子效果
        /// </summary>
        private void StartParticleEffects()
        {
            // 庆祝粒子
            if (celebrationParticles != null)
            {
                celebrationParticles.Play();
            }
            
            // 根据获奖类型播放不同效果
            switch (currentWinType)
            {
                case WinType.BigWin:
                case WinType.MegaWin:
                case WinType.Jackpot:
                    if (coinRainEffect != null)
                        coinRainEffect.Play();
                    break;
            }
            
            // 彩带效果
            if (confettiEffects != null && currentWinType >= WinType.MegaWin)
            {
                foreach (var confetti in confettiEffects)
                {
                    if (confetti != null)
                        confetti.SetActive(true);
                }
            }
        }
        
        /// <summary>
        /// 开始光线效果
        /// </summary>
        private void StartRayEffects()
        {
            if (rayEffects == null) return;
            
            foreach (var ray in rayEffects)
            {
                if (ray != null)
                {
                    ray.gameObject.SetActive(true);
                    ray.transform.DORotate(new Vector3(0, 0, 360), 2f, RotateMode.FastBeyond360)
                        .SetLoops(-1, LoopType.Incremental)
                        .SetEase(Ease.Linear);
                }
            }
        }
        
        /// <summary>
        /// 开始隐藏动画
        /// </summary>
        private void StartHideAnimation()
        {
            var hideSequence = DOTween.Sequence();
            
            // 缩放动画
            hideSequence.Append(popupPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack));
            
            // 背景淡出
            if (backgroundOverlay != null)
            {
                hideSequence.Join(backgroundOverlay.DOFade(0f, 0.3f));
            }
            
            // 完成回调
            hideSequence.OnComplete(() =>
            {
                popupPanel.SetActive(false);
                isShowing = false;
                StopAllEffects();
                OnPopupClosed?.Invoke();
            });
        }
        
        #endregion
        
        #region Audio
        
        /// <summary>
        /// 播放获奖音效
        /// </summary>
        private void PlayWinSound()
        {
            if (audioSource == null) return;
            
            AudioClip soundToPlay = null;
            
            switch (currentWinType)
            {
                case WinType.SmallWin:
                    soundToPlay = smallWinSound;
                    break;
                case WinType.BigWin:
                    soundToPlay = bigWinSound;
                    break;
                case WinType.MegaWin:
                    soundToPlay = bigWinSound;
                    break;
                case WinType.Jackpot:
                    soundToPlay = jackpotSound;
                    break;
            }
            
            if (soundToPlay != null)
            {
                audioSource.PlayOneShot(soundToPlay);
            }
        }
        
        #endregion
        
        #region Effects Control
        
        /// <summary>
        /// 停止所有效果
        /// </summary>
        private void StopAllEffects()
        {
            // 停止粒子效果
            if (celebrationParticles != null && celebrationParticles.isPlaying)
                celebrationParticles.Stop();
                
            if (coinRainEffect != null && coinRainEffect.isPlaying)
                coinRainEffect.Stop();
            
            // 隐藏彩带效果
            if (confettiEffects != null)
            {
                foreach (var confetti in confettiEffects)
                {
                    if (confetti != null)
                        confetti.SetActive(false);
                }
            }
            
            // 停止光线效果
            if (rayEffects != null)
            {
                foreach (var ray in rayEffects)
                {
                    if (ray != null)
                    {
                        ray.transform.DOKill();
                        ray.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        #endregion
        
        #region Button Events
        
        /// <summary>
        /// 收集按钮点击事件
        /// </summary>
        private void OnCollectButtonClicked()
        {
            // 播放收集音效
            if (audioSource != null && collectSound != null)
            {
                audioSource.PlayOneShot(collectSound);
            }
            
            // 触发收集事件
            OnWinAmountCollected?.Invoke(currentWinAmount);
            
            // 隐藏弹窗
            HidePopup();
        }
        
        /// <summary>
        /// 继续按钮点击事件
        /// </summary>
        private void OnContinueButtonClicked()
        {
            // 隐藏弹窗
            HidePopup();
        }
        
        #endregion
        
        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 测试小奖
        /// </summary>
        [ContextMenu("Test Small Win")]
        private void TestSmallWin()
        {
            ShowWinPopup(50f);
        }
        
        /// <summary>
        /// 测试大奖
        /// </summary>
        [ContextMenu("Test Big Win")]
        private void TestBigWin()
        {
            ShowWinPopup(250f);
        }
        
        /// <summary>
        /// 测试超级大奖
        /// </summary>
        [ContextMenu("Test Mega Win")]
        private void TestMegaWin()
        {
            ShowWinPopup(750f);
        }
        
        /// <summary>
        /// 测试头奖
        /// </summary>
        [ContextMenu("Test Jackpot")]
        private void TestJackpot()
        {
            ShowWinPopup(1500f);
        }
#endif
        
        #endregion
    }
}