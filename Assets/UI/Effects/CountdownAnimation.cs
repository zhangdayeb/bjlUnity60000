using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace SlotMachine.Effects
{
    /// <summary>
    /// 倒计时动画组件 - 提供各种倒计时效果和动画
    /// </summary>
    public class CountdownAnimation : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private Image countdownBackground;
        [SerializeField] private Image progressBar;
        [SerializeField] private Image circularProgress;
        [SerializeField] private RectTransform countdownContainer;
        
        [Header("Visual Settings")]
        [SerializeField] private bool showNumbers = true;
        [SerializeField] private bool showProgressBar = true;
        [SerializeField] private bool showCircularProgress = false;
        [SerializeField] private bool useColorTransition = true;
        [SerializeField] private bool enablePulseEffect = true;
        
        [Header("Animation Settings")]
        [SerializeField] private float scalePulseMultiplier = 1.3f;
        [SerializeField] private float pulseDuration = 0.3f;
        [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve progressCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        [Header("Color Scheme")]
        [SerializeField] private Color startColor = Color.green;
        [SerializeField] private Color midColor = Color.yellow;
        [SerializeField] private Color endColor = Color.red;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
        [SerializeField] private Color textColor = Color.white;
        
        [Header("Timing Settings")]
        [SerializeField] private float warningThreshold = 5f; // 警告阈值（秒）
        [SerializeField] private float criticalThreshold = 3f; // 危险阈值（秒）
        [SerializeField] private bool autoHideOnComplete = true;
        [SerializeField] private float hideDelay = 1f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip tickSound;
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private AudioClip completionSound;
        [SerializeField] private bool playTickEverySecond = true;
        [SerializeField] private bool playWarningSound = true;
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem countdownParticles;
        [SerializeField] private GameObject[] warningEffects;
        [SerializeField] private Light countdownLight;
        [SerializeField] private bool enableScreenShake = false;
        [SerializeField] private Camera targetCamera;
        
        // 私有变量
        private float currentTime;
        private float totalTime;
        private bool isCountingDown;
        private bool isPaused;
        private Sequence currentAnimation;
        private Sequence pulseAnimation;
        private int lastSecond = -1;
        private Vector3 originalScale;
        private Color originalTextColor;
        private Color originalBackgroundColor;
        
        // 事件
        public System.Action OnCountdownStart;
        public System.Action OnCountdownComplete;
        public System.Action OnCountdownPaused;
        public System.Action OnCountdownResumed;
        public System.Action<float> OnCountdownTick; // 每秒触发，传递剩余时间
        public System.Action<int> OnSecondChanged; // 秒数变化时触发
        public System.Action OnWarningTriggered; // 达到警告阈值时触发
        public System.Action OnCriticalTriggered; // 达到危险阈值时触发
        
        /// <summary>
        /// 倒计时状态枚举
        /// </summary>
        public enum CountdownState
        {
            Idle,           // 空闲
            Running,        // 运行中
            Paused,         // 暂停
            Warning,        // 警告状态
            Critical,       // 危险状态
            Completed       // 完成
        }
        
        /// <summary>
        /// 动画类型枚举
        /// </summary>
        public enum AnimationType
        {
            Simple,         // 简单数字倒计时
            Pulse,          // 脉冲效果
            Scale,          // 缩放效果
            Rotate,         // 旋转效果
            Shake,          // 震动效果
            Combined        // 组合效果
        }
        
        private CountdownState currentState = CountdownState.Idle;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
            StoreOriginalValues();
        }
        
        private void Start()
        {
            SetupInitialState();
        }
        
        private void Update()
        {
            if (isCountingDown && !isPaused)
            {
                UpdateCountdown();
            }
        }
        
        private void OnDestroy()
        {
            StopAllAnimations();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            if (countdownText == null)
                countdownText = GetComponentInChildren<TextMeshProUGUI>();
                
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
                
            if (targetCamera == null)
                targetCamera = Camera.main;
            
            // 验证必要组件
            if (countdownText == null)
            {
                Debug.LogError("CountdownAnimation: 缺少 TextMeshProUGUI 组件!");
            }
        }
        
        /// <summary>
        /// 存储原始值
        /// </summary>
        private void StoreOriginalValues()
        {
            if (countdownContainer != null)
                originalScale = countdownContainer.localScale;
            else if (countdownText != null)
                originalScale = countdownText.transform.localScale;
            else
                originalScale = transform.localScale;
                
            if (countdownText != null)
                originalTextColor = countdownText.color;
                
            if (countdownBackground != null)
                originalBackgroundColor = countdownBackground.color;
        }
        
        /// <summary>
        /// 设置初始状态
        /// </summary>
        private void SetupInitialState()
        {
            currentState = CountdownState.Idle;
            isCountingDown = false;
            isPaused = false;
            
            // 隐藏倒计时UI
            if (countdownContainer != null)
                countdownContainer.gameObject.SetActive(false);
            else if (countdownText != null)
                countdownText.gameObject.SetActive(false);
                
            // 重置进度条
            ResetProgressBars();
            
            // 隐藏警告效果
            HideWarningEffects();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 开始倒计时
        /// </summary>
        /// <param name="duration">倒计时时长（秒）</param>
        /// <param name="animationType">动画类型</param>
        public void StartCountdown(float duration, AnimationType animationType = AnimationType.Combined)
        {
            if (duration <= 0)
            {
                Debug.LogWarning("CountdownAnimation: 倒计时时长必须大于0!");
                return;
            }
            
            StopCountdown();
            
            totalTime = duration;
            currentTime = duration;
            lastSecond = Mathf.CeilToInt(currentTime);
            isCountingDown = true;
            isPaused = false;
            currentState = CountdownState.Running;
            
            // 显示UI
            ShowCountdownUI();
            
            // 开始动画
            StartCountdownAnimation(animationType);
            
            OnCountdownStart?.Invoke();
        }
        
        /// <summary>
        /// 停止倒计时
        /// </summary>
        public void StopCountdown()
        {
            isCountingDown = false;
            isPaused = false;
            currentState = CountdownState.Idle;
            
            StopAllAnimations();
            HideWarningEffects();
            
            if (autoHideOnComplete)
            {
                HideCountdownUI();
            }
        }
        
        /// <summary>
        /// 暂停倒计时
        /// </summary>
        public void PauseCountdown()
        {
            if (!isCountingDown || isPaused) return;
            
            isPaused = true;
            currentState = CountdownState.Paused;
            
            // 暂停动画
            if (currentAnimation != null && currentAnimation.IsActive())
            {
                currentAnimation.Pause();
            }
            if (pulseAnimation != null && pulseAnimation.IsActive())
            {
                pulseAnimation.Pause();
            }
            
            OnCountdownPaused?.Invoke();
        }
        
        /// <summary>
        /// 恢复倒计时
        /// </summary>
        public void ResumeCountdown()
        {
            if (!isCountingDown || !isPaused) return;
            
            isPaused = false;
            UpdateCountdownState();
            
            // 恢复动画
            if (currentAnimation != null && currentAnimation.IsActive())
            {
                currentAnimation.Play();
            }
            if (pulseAnimation != null && pulseAnimation.IsActive())
            {
                pulseAnimation.Play();
            }
            
            OnCountdownResumed?.Invoke();
        }
        
        /// <summary>
        /// 添加时间
        /// </summary>
        /// <param name="additionalTime">额外时间（秒）</param>
        public void AddTime(float additionalTime)
        {
            if (isCountingDown)
            {
                currentTime += additionalTime;
                totalTime += additionalTime;
                UpdateCountdownState();
                UpdateDisplay();
            }
        }
        
        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="newTime">新的剩余时间（秒）</param>
        public void SetTime(float newTime)
        {
            if (isCountingDown)
            {
                currentTime = Mathf.Max(0, newTime);
                UpdateCountdownState();
                UpdateDisplay();
            }
        }
        
        /// <summary>
        /// 获取当前剩余时间
        /// </summary>
        /// <returns>剩余时间（秒）</returns>
        public float GetRemainingTime()
        {
            return currentTime;
        }
        
        /// <summary>
        /// 获取倒计时进度（0-1）
        /// </summary>
        /// <returns>进度值</returns>
        public float GetProgress()
        {
            if (totalTime <= 0) return 0f;
            return 1f - (currentTime / totalTime);
        }
        
        /// <summary>
        /// 获取当前状态
        /// </summary>
        /// <returns>倒计时状态</returns>
        public CountdownState GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// 检查是否正在倒计时
        /// </summary>
        /// <returns>是否正在倒计时</returns>
        public bool IsCountingDown()
        {
            return isCountingDown && !isPaused;
        }
        
        #endregion
        
        #region Update Logic
        
        /// <summary>
        /// 更新倒计时
        /// </summary>
        private void UpdateCountdown()
        {
            currentTime -= Time.deltaTime;
            
            // 检查是否完成
            if (currentTime <= 0)
            {
                currentTime = 0;
                CompleteCountdown();
                return;
            }
            
            // 更新状态
            UpdateCountdownState();
            
            // 更新显示
            UpdateDisplay();
            
            // 检查秒数变化
            int currentSecond = Mathf.CeilToInt(currentTime);
            if (currentSecond != lastSecond)
            {
                lastSecond = currentSecond;
                OnSecondChanged?.Invoke(currentSecond);
                OnCountdownTick?.Invoke(currentTime);
                
                // 播放滴答声
                if (playTickEverySecond)
                {
                    PlayTickSound();
                }
                
                // 数字脉冲效果
                if (enablePulseEffect && showNumbers)
                {
                    PlayNumberPulse();
                }
            }
        }
        
        /// <summary>
        /// 更新倒计时状态
        /// </summary>
        private void UpdateCountdownState()
        {
            CountdownState newState = CountdownState.Running;
            
            if (currentTime <= criticalThreshold)
            {
                newState = CountdownState.Critical;
            }
            else if (currentTime <= warningThreshold)
            {
                newState = CountdownState.Warning;
            }
            
            // 状态变化处理
            if (newState != currentState)
            {
                HandleStateChange(currentState, newState);
                currentState = newState;
            }
        }
        
        /// <summary>
        /// 处理状态变化
        /// </summary>
        private void HandleStateChange(CountdownState oldState, CountdownState newState)
        {
            switch (newState)
            {
                case CountdownState.Warning:
                    OnWarningTriggered?.Invoke();
                    if (playWarningSound)
                        PlayWarningSound();
                    StartWarningEffects();
                    break;
                    
                case CountdownState.Critical:
                    OnCriticalTriggered?.Invoke();
                    if (playWarningSound)
                        PlayWarningSound();
                    StartCriticalEffects();
                    break;
            }
        }
        
        /// <summary>
        /// 完成倒计时
        /// </summary>
        private void CompleteCountdown()
        {
            isCountingDown = false;
            currentState = CountdownState.Completed;
            
            // 播放完成音效
            PlayCompletionSound();
            
            // 播放完成动画
            PlayCompletionAnimation();
            
            OnCountdownComplete?.Invoke();
            
            // 自动隐藏
            if (autoHideOnComplete)
            {
                StartCoroutine(HideAfterDelay());
            }
        }
        
        #endregion
        
        #region Display Updates
        
        /// <summary>
        /// 更新显示
        /// </summary>
        private void UpdateDisplay()
        {
            // 更新数字显示
            if (showNumbers && countdownText != null)
            {
                UpdateNumberDisplay();
            }
            
            // 更新进度条
            if (showProgressBar && progressBar != null)
            {
                UpdateProgressBar();
            }
            
            // 更新圆形进度
            if (showCircularProgress && circularProgress != null)
            {
                UpdateCircularProgress();
            }
            
            // 更新颜色
            if (useColorTransition)
            {
                UpdateColors();
            }
        }
        
        /// <summary>
        /// 更新数字显示
        /// </summary>
        private void UpdateNumberDisplay()
        {
            if (currentTime >= 1f)
            {
                // 显示整数秒
                countdownText.text = Mathf.CeilToInt(currentTime).ToString();
            }
            else
            {
                // 显示小数
                countdownText.text = currentTime.ToString("F1");
            }
        }
        
        /// <summary>
        /// 更新进度条
        /// </summary>
        private void UpdateProgressBar()
        {
            float progress = GetProgress();
            progressBar.fillAmount = progressCurve.Evaluate(progress);
        }
        
        /// <summary>
        /// 更新圆形进度
        /// </summary>
        private void UpdateCircularProgress()
        {
            float progress = GetProgress();
            circularProgress.fillAmount = 1f - progressCurve.Evaluate(progress);
        }
        
        /// <summary>
        /// 更新颜色
        /// </summary>
        private void UpdateColors()
        {
            Color currentColor = GetCurrentColor();
            
            if (countdownText != null)
            {
                countdownText.color = currentColor;
            }
            
            if (progressBar != null)
            {
                progressBar.color = currentColor;
            }
            
            if (circularProgress != null)
            {
                circularProgress.color = currentColor;
            }
        }
        
        /// <summary>
        /// 获取当前颜色
        /// </summary>
        private Color GetCurrentColor()
        {
            float normalizedTime = currentTime / totalTime;
            
            if (normalizedTime > 0.5f)
            {
                // 从开始颜色到中间颜色
                float t = (normalizedTime - 0.5f) * 2f;
                return Color.Lerp(midColor, startColor, t);
            }
            else
            {
                // 从中间颜色到结束颜色
                float t = normalizedTime * 2f;
                return Color.Lerp(endColor, midColor, t);
            }
        }
        
        #endregion
        
        #region Animations
        
        /// <summary>
        /// 开始倒计时动画
        /// </summary>
        private void StartCountdownAnimation(AnimationType animationType)
        {
            switch (animationType)
            {
                case AnimationType.Simple:
                    // 简单显示，无特殊动画
                    break;
                case AnimationType.Pulse:
                    StartPulseAnimation();
                    break;
                case AnimationType.Scale:
                    StartScaleAnimation();
                    break;
                case AnimationType.Rotate:
                    StartRotateAnimation();
                    break;
                case AnimationType.Shake:
                    StartShakeAnimation();
                    break;
                case AnimationType.Combined:
                    StartCombinedAnimation();
                    break;
            }
        }
        
        /// <summary>
        /// 开始脉冲动画
        /// </summary>
        private void StartPulseAnimation()
        {
            if (countdownContainer != null)
            {
                pulseAnimation = countdownContainer.DOScale(originalScale * scalePulseMultiplier, pulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(pulseCurve);
            }
        }
        
        /// <summary>
        /// 开始缩放动画
        /// </summary>
        private void StartScaleAnimation()
        {
            if (countdownContainer != null)
            {
                currentAnimation = DOTween.Sequence()
                    .Append(countdownContainer.DOScale(originalScale * 1.2f, 0.3f))
                    .Append(countdownContainer.DOScale(originalScale, 0.3f))
                    .SetLoops(-1);
            }
        }
        
        /// <summary>
        /// 开始旋转动画
        /// </summary>
        private void StartRotateAnimation()
        {
            if (countdownContainer != null)
            {
                currentAnimation = countdownContainer.DORotate(new Vector3(0, 0, 360), 2f, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Incremental)
                    .SetEase(Ease.Linear);
            }
        }
        
        /// <summary>
        /// 开始震动动画
        /// </summary>
        private void StartShakeAnimation()
        {
            if (countdownContainer != null)
            {
                currentAnimation = countdownContainer.DOShakePosition(0.5f, 10f, 10, 90, false, true)
                    .SetLoops(-1);
            }
        }
        
        /// <summary>
        /// 开始组合动画
        /// </summary>
        private void StartCombinedAnimation()
        {
            if (countdownContainer != null)
            {
                currentAnimation = DOTween.Sequence()
                    .Append(countdownContainer.DOScale(originalScale * 1.1f, 0.5f))
                    .Join(countdownContainer.DORotate(new Vector3(0, 0, 10), 0.5f))
                    .Append(countdownContainer.DOScale(originalScale, 0.5f))
                    .Join(countdownContainer.DORotate(Vector3.zero, 0.5f))
                    .SetLoops(-1);
            }
        }
        
        /// <summary>
        /// 播放数字脉冲
        /// </summary>
        private void PlayNumberPulse()
        {
            if (countdownText != null)
            {
                countdownText.transform.DOScale(originalScale * 1.2f, 0.1f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        countdownText.transform.DOScale(originalScale, 0.1f)
                            .SetEase(Ease.InQuad);
                    });
            }
        }
        
        /// <summary>
        /// 播放完成动画
        /// </summary>
        private void PlayCompletionAnimation()
        {
            if (countdownContainer != null)
            {
                var completionSequence = DOTween.Sequence();
                
                // 停止当前动画
                StopAllAnimations();
                
                // 闪烁效果
                completionSequence.Append(countdownContainer.DOScale(originalScale * 1.5f, 0.2f))
                    .Join(countdownText.DOColor(Color.white, 0.2f))
                    .Append(countdownContainer.DOScale(originalScale, 0.2f))
                    .Join(countdownText.DOColor(endColor, 0.2f))
                    .SetLoops(3);
                    
                currentAnimation = completionSequence;
            }
        }
        
        #endregion
        
        #region Effects
        
        /// <summary>
        /// 开始警告效果
        /// </summary>
        private void StartWarningEffects()
        {
            // 显示警告特效
            ShowWarningEffects();
            
            // 光源效果
            if (countdownLight != null)
            {
                countdownLight.color = midColor;
                countdownLight.DOIntensity(2f, 0.3f).SetLoops(-1, LoopType.Yoyo);
            }
        }
        
        /// <summary>
        /// 开始危险效果
        /// </summary>
        private void StartCriticalEffects()
        {
            // 屏幕震动
            if (enableScreenShake && targetCamera != null)
            {
                targetCamera.transform.DOShakePosition(0.3f, 0.1f, 10, 90, false, true)
                    .SetLoops(-1);
            }
            
            // 粒子效果
            if (countdownParticles != null)
            {
                countdownParticles.Play();
            }
            
            // 强化光源效果
            if (countdownLight != null)
            {
                countdownLight.color = endColor;
                countdownLight.DOIntensity(3f, 0.1f).SetLoops(-1, LoopType.Yoyo);
            }
        }
        
        /// <summary>
        /// 显示警告效果
        /// </summary>
        private void ShowWarningEffects()
        {
            if (warningEffects != null)
            {
                foreach (var effect in warningEffects)
                {
                    if (effect != null)
                        effect.SetActive(true);
                }
            }
        }
        
        /// <summary>
        /// 隐藏警告效果
        /// </summary>
        private void HideWarningEffects()
        {
            if (warningEffects != null)
            {
                foreach (var effect in warningEffects)
                {
                    if (effect != null)
                        effect.SetActive(false);
                }
            }
            
            // 停止粒子效果
            if (countdownParticles != null && countdownParticles.isPlaying)
            {
                countdownParticles.Stop();
            }
            
            // 重置光源
            if (countdownLight != null)
            {
                countdownLight.DOKill();
                countdownLight.intensity = 0f;
            }
            
            // 停止相机震动
            if (targetCamera != null)
            {
                targetCamera.transform.DOKill();
            }
        }
        
        #endregion
        
        #region UI Management
        
        /// <summary>
        /// 显示倒计时UI
        /// </summary>
        private void ShowCountdownUI()
        {
            if (countdownContainer != null)
            {
                countdownContainer.gameObject.SetActive(true);
                countdownContainer.localScale = Vector3.zero;
                countdownContainer.DOScale(originalScale, 0.3f).SetEase(Ease.OutBack);
            }
            else if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.transform.localScale = Vector3.zero;
                countdownText.transform.DOScale(originalScale, 0.3f).SetEase(Ease.OutBack);
            }
        }
        
        /// <summary>
        /// 隐藏倒计时UI
        /// </summary>
        private void HideCountdownUI()
        {
            if (countdownContainer != null)
            {
                countdownContainer.DOScale(Vector3.zero, 0.3f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => countdownContainer.gameObject.SetActive(false));
            }
            else if (countdownText != null)
            {
                countdownText.transform.DOScale(Vector3.zero, 0.3f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => countdownText.gameObject.SetActive(false));
            }
        }
        
        /// <summary>
        /// 延迟隐藏
        /// </summary>
        private System.Collections.IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(hideDelay);
            HideCountdownUI();
        }
        
        /// <summary>
        /// 重置进度条
        /// </summary>
        private void ResetProgressBars()
        {
            if (progressBar != null)
            {
                progressBar.fillAmount = 0f;
            }
            
            if (circularProgress != null)
            {
                circularProgress.fillAmount = 1f;
            }
        }
        
        #endregion
        
        #region Audio
        
        /// <summary>
        /// 播放滴答声
        /// </summary>
        private void PlayTickSound()
        {
            if (audioSource != null && tickSound != null)
            {
                audioSource.PlayOneShot(tickSound);
            }
        }
        
        /// <summary>
        /// 播放警告声
        /// </summary>
        private void PlayWarningSound()
        {
            if (audioSource != null && warningSound != null)
            {
                audioSource.PlayOneShot(warningSound);
            }
        }
        
        /// <summary>
        /// 播放完成声
        /// </summary>
        private void PlayCompletionSound()
        {
            if (audioSource != null && completionSound != null)
            {
                audioSource.PlayOneShot(completionSound);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// 停止所有动画
        /// </summary>
        private void StopAllAnimations()
        {
            if (currentAnimation != null && currentAnimation.IsActive())
            {
                currentAnimation.Kill();
            }
            
            if (pulseAnimation != null && pulseAnimation.IsActive())
            {
                pulseAnimation.Kill();
            }
            
            // 重置变换
            if (countdownContainer != null)
            {
                countdownContainer.DOKill();
                countdownContainer.localScale = originalScale;
                countdownContainer.rotation = Quaternion.identity;
                countdownContainer.anchoredPosition = Vector2.zero;
            }
            else if (countdownText != null)
            {
                countdownText.transform.DOKill();
                countdownText.transform.localScale = originalScale;
                countdownText.transform.rotation = Quaternion.identity;
            }
        }
        
        /// <summary>
        /// 格式化时间显示
        /// </summary>
        /// <param name="timeInSeconds">时间（秒）</param>
        /// <param name="showDecimals">是否显示小数</param>
        /// <returns>格式化的时间字符串</returns>
        public static string FormatTime(float timeInSeconds, bool showDecimals = false)
        {
            if (timeInSeconds < 0) timeInSeconds = 0;
            
            int hours = Mathf.FloorToInt(timeInSeconds / 3600);
            int minutes = Mathf.FloorToInt((timeInSeconds % 3600) / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);
            float decimals = timeInSeconds % 1;
            
            if (hours > 0)
            {
                return $"{hours:00}:{minutes:00}:{seconds:00}";
            }
            else if (minutes > 0)
            {
                return $"{minutes:00}:{seconds:00}";
            }
            else
            {
                if (showDecimals && timeInSeconds < 10)
                {
                    return $"{seconds}.{Mathf.FloorToInt(decimals * 10)}";
                }
                else
                {
                    return seconds.ToString();
                }
            }
        }
        
        /// <summary>
        /// 设置自定义颜色方案
        /// </summary>
        /// <param name="start">开始颜色</param>
        /// <param name="middle">中间颜色</param>
        /// <param name="end">结束颜色</param>
        public void SetColorScheme(Color start, Color middle, Color end)
        {
            startColor = start;
            midColor = middle;
            endColor = end;
        }
        
        /// <summary>
        /// 设置阈值
        /// </summary>
        /// <param name="warning">警告阈值</param>
        /// <param name="critical">危险阈值</param>
        public void SetThresholds(float warning, float critical)
        {
            warningThreshold = warning;
            criticalThreshold = critical;
        }
        
        /// <summary>
        /// 立即更新显示
        /// </summary>
        public void ForceUpdateDisplay()
        {
            UpdateDisplay();
        }
        
        #endregion
        
        #region Advanced Features
        
        /// <summary>
        /// 创建自定义倒计时序列
        /// </summary>
        /// <param name="durations">时间段数组</param>
        /// <param name="labels">标签数组</param>
        public void StartCustomSequence(float[] durations, string[] labels = null)
        {
            if (durations == null || durations.Length == 0) return;
            
            StartCoroutine(CustomSequenceCoroutine(durations, labels));
        }
        
        /// <summary>
        /// 自定义序列协程
        /// </summary>
        private System.Collections.IEnumerator CustomSequenceCoroutine(float[] durations, string[] labels)
        {
            for (int i = 0; i < durations.Length; i++)
            {
                // 设置标签
                if (labels != null && i < labels.Length && countdownText != null)
                {
                    // 临时显示标签
                    string originalFormat = showNumbers ? "number" : "label";
                    showNumbers = false;
                    countdownText.text = labels[i];
                    
                    yield return new WaitForSeconds(1f);
                    
                    showNumbers = originalFormat == "number";
                }
                
                // 开始这一段的倒计时
                StartCountdown(durations[i]);
                
                // 等待完成
                while (isCountingDown)
                {
                    yield return null;
                }
                
                // 段间停顿
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        /// <summary>
        /// 添加检查点
        /// </summary>
        private System.Collections.Generic.Dictionary<float, System.Action> checkpoints = 
            new System.Collections.Generic.Dictionary<float, System.Action>();
        
        /// <summary>
        /// 设置检查点
        /// </summary>
        /// <param name="timePoint">时间点</param>
        /// <param name="callback">回调函数</param>
        public void SetCheckpoint(float timePoint, System.Action callback)
        {
            if (checkpoints.ContainsKey(timePoint))
            {
                checkpoints[timePoint] = callback;
            }
            else
            {
                checkpoints.Add(timePoint, callback);
            }
        }
        
        /// <summary>
        /// 检查检查点
        /// </summary>
        private void CheckCheckpoints()
        {
            var keysToRemove = new System.Collections.Generic.List<float>();
            
            foreach (var checkpoint in checkpoints)
            {
                if (currentTime <= checkpoint.Key)
                {
                    checkpoint.Value?.Invoke();
                    keysToRemove.Add(checkpoint.Key);
                }
            }
            
            // 移除已触发的检查点
            foreach (var key in keysToRemove)
            {
                checkpoints.Remove(key);
            }
        }
        
        /// <summary>
        /// 创建倒计时预设
        /// </summary>
        [System.Serializable]
        public class CountdownPreset
        {
            public string name;
            public float duration;
            public AnimationType animationType;
            public Color primaryColor;
            public Color secondaryColor;
            public bool useWarning;
            public float warningTime;
            public bool useCritical;
            public float criticalTime;
        }
        
        [Header("Presets")]
        [SerializeField] private CountdownPreset[] countdownPresets;
        
        /// <summary>
        /// 使用预设开始倒计时
        /// </summary>
        /// <param name="presetName">预设名称</param>
        public void StartCountdownWithPreset(string presetName)
        {
            if (countdownPresets == null) return;
            
            foreach (var preset in countdownPresets)
            {
                if (preset.name == presetName)
                {
                    // 应用预设设置
                    SetColorScheme(preset.primaryColor, preset.secondaryColor, endColor);
                    
                    if (preset.useWarning)
                        warningThreshold = preset.warningTime;
                    if (preset.useCritical)
                        criticalThreshold = preset.criticalTime;
                    
                    StartCountdown(preset.duration, preset.animationType);
                    break;
                }
            }
        }
        
        #endregion
        
        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 测试倒计时
        /// </summary>
        [ContextMenu("Test Countdown 10s")]
        private void TestCountdown10()
        {
            if (Application.isPlaying)
            {
                StartCountdown(10f);
            }
        }
        
        /// <summary>
        /// 测试倒计时
        /// </summary>
        [ContextMenu("Test Countdown 5s")]
        private void TestCountdown5()
        {
            if (Application.isPlaying)
            {
                StartCountdown(5f);
            }
        }
        
        /// <summary>
        /// 测试脉冲动画
        /// </summary>
        [ContextMenu("Test Pulse Animation")]
        private void TestPulseAnimation()
        {
            if (Application.isPlaying)
            {
                StartCountdown(8f, AnimationType.Pulse);
            }
        }
        
        /// <summary>
        /// 测试组合动画
        /// </summary>
        [ContextMenu("Test Combined Animation")]
        private void TestCombinedAnimation()
        {
            if (Application.isPlaying)
            {
                StartCountdown(15f, AnimationType.Combined);
            }
        }
        
        /// <summary>
        /// 测试暂停/恢复
        /// </summary>
        [ContextMenu("Test Pause/Resume")]
        private void TestPauseResume()
        {
            if (Application.isPlaying)
            {
                if (isCountingDown && !isPaused)
                {
                    PauseCountdown();
                }
                else if (isCountingDown && isPaused)
                {
                    ResumeCountdown();
                }
                else
                {
                    StartCountdown(10f);
                }
            }
        }
        
        /// <summary>
        /// 停止倒计时
        /// </summary>
        [ContextMenu("Stop Countdown")]
        private void TestStopCountdown()
        {
            if (Application.isPlaying)
            {
                StopCountdown();
            }
        }
        
        /// <summary>
        /// 添加时间
        /// </summary>
        [ContextMenu("Add 5 Seconds")]
        private void TestAddTime()
        {
            if (Application.isPlaying)
            {
                AddTime(5f);
            }
        }
        
        /// <summary>
        /// 设置危险时间
        /// </summary>
        [ContextMenu("Set Critical Time")]
        private void TestSetCriticalTime()
        {
            if (Application.isPlaying)
            {
                SetTime(2f);
            }
        }
#endif
        
        #endregion
    }
}