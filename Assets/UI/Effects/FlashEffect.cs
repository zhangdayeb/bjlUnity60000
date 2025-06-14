using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SlotMachine.Effects
{
    /// <summary>
    /// 闪烁效果组件 - 为UI元素提供各种闪烁和高亮效果
    /// </summary>
    public class FlashEffect : MonoBehaviour
    {
        [Header("Target Components")]
        [SerializeField] private Graphic targetGraphic; // Image, Text等UI图形组件
        [SerializeField] private SpriteRenderer targetSpriteRenderer; // 2D精灵渲染器
        [SerializeField] private Light targetLight; // 光源组件
        [SerializeField] private ParticleSystem targetParticles; // 粒子系统
        
        [Header("Flash Settings")]
        [SerializeField] private FlashType flashType = FlashType.Color;
        [SerializeField] private float flashDuration = 0.5f;
        [SerializeField] private int flashCount = 3;
        [SerializeField] private bool loopFlash = false;
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private bool playOnEnable = false;
        
        [Header("Color Flash")]
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private Color originalColor = Color.white;
        [SerializeField] private bool useOriginalColor = true;
        [SerializeField] private AnimationCurve colorCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Alpha Flash")]
        [SerializeField] private float minAlpha = 0f;
        [SerializeField] private float maxAlpha = 1f;
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Scale Flash")]
        [SerializeField] private Vector3 minScale = Vector3.one * 0.8f;
        [SerializeField] private Vector3 maxScale = Vector3.one * 1.2f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Intensity Flash (for Lights)")]
        [SerializeField] private float minIntensity = 0f;
        [SerializeField] private float maxIntensity = 2f;
        [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Advanced Settings")]
        [SerializeField] private float delayBetweenFlashes = 0.1f;
        [SerializeField] private Ease flashEase = Ease.InOutSine;
        [SerializeField] private bool randomizeFlashTiming = false;
        [SerializeField] private Vector2 randomDelayRange = new Vector2(0.05f, 0.2f);
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip flashSound;
        [SerializeField] private bool playAudioOnEachFlash = false;
        
        // 私有变量
        private Sequence flashSequence;
        private bool isFlashing;
        private Vector3 originalScale;
        private float originalIntensity;
        private float originalParticleEmissionRate;
        
        // 事件
        public System.Action OnFlashStart;
        public System.Action OnFlashComplete;
        public System.Action OnSingleFlashComplete;
        
        /// <summary>
        /// 闪烁类型枚举
        /// </summary>
        public enum FlashType
        {
            Color,          // 颜色闪烁
            Alpha,          // 透明度闪烁
            Scale,          // 缩放闪烁
            Intensity,      // 强度闪烁（光源）
            Combined        // 组合效果
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
            StoreOriginalValues();
        }
        
        private void Start()
        {
            if (playOnStart)
            {
                StartFlash();
            }
        }
        
        private void OnEnable()
        {
            if (playOnEnable && Application.isPlaying)
            {
                StartFlash();
            }
        }
        
        private void OnDisable()
        {
            StopFlash();
        }
        
        private void OnDestroy()
        {
            StopFlash();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 自动获取组件（如果没有手动设置）
            if (targetGraphic == null)
                targetGraphic = GetComponent<Graphic>();
                
            if (targetSpriteRenderer == null)
                targetSpriteRenderer = GetComponent<SpriteRenderer>();
                
            if (targetLight == null)
                targetLight = GetComponent<Light>();
                
            if (targetParticles == null)
                targetParticles = GetComponent<ParticleSystem>();
                
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }
        
        /// <summary>
        /// 存储原始值
        /// </summary>
        private void StoreOriginalValues()
        {
            // 存储原始颜色
            if (useOriginalColor)
            {
                if (targetGraphic != null)
                    originalColor = targetGraphic.color;
                else if (targetSpriteRenderer != null)
                    originalColor = targetSpriteRenderer.color;
            }
            
            // 存储原始缩放
            originalScale = transform.localScale;
            
            // 存储原始光源强度
            if (targetLight != null)
                originalIntensity = targetLight.intensity;
                
            // 存储原始粒子发射率
            if (targetParticles != null)
            {
                var emission = targetParticles.emission;
                originalParticleEmissionRate = emission.rateOverTime.constant;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 开始闪烁效果
        /// </summary>
        public void StartFlash()
        {
            if (isFlashing) return;
            
            StopFlash(); // 确保清理之前的动画
            CreateFlashSequence();
            OnFlashStart?.Invoke();
        }
        
        /// <summary>
        /// 停止闪烁效果
        /// </summary>
        public void StopFlash()
        {
            if (flashSequence != null && flashSequence.IsActive())
            {
                flashSequence.Kill();
            }
            
            isFlashing = false;
            RestoreOriginalValues();
        }
        
        /// <summary>
        /// 单次闪烁
        /// </summary>
        public void FlashOnce()
        {
            if (isFlashing) return;
            
            int originalCount = flashCount;
            flashCount = 1;
            bool originalLoop = loopFlash;
            loopFlash = false;
            
            StartFlash();
            
            // 恢复原始设置
            flashCount = originalCount;
            loopFlash = originalLoop;
        }
        
        /// <summary>
        /// 设置闪烁颜色
        /// </summary>
        /// <param name="color">闪烁颜色</param>
        public void SetFlashColor(Color color)
        {
            flashColor = color;
        }
        
        /// <summary>
        /// 设置闪烁持续时间
        /// </summary>
        /// <param name="duration">持续时间</param>
        public void SetFlashDuration(float duration)
        {
            flashDuration = duration;
        }
        
        /// <summary>
        /// 设置闪烁次数
        /// </summary>
        /// <param name="count">闪烁次数</param>
        public void SetFlashCount(int count)
        {
            flashCount = count;
        }
        
        /// <summary>
        /// 检查是否正在闪烁
        /// </summary>
        /// <returns>是否正在闪烁</returns>
        public bool IsFlashing()
        {
            return isFlashing;
        }
        
        #endregion
        
        #region Flash Animation
        
        /// <summary>
        /// 创建闪烁动画序列
        /// </summary>
        private void CreateFlashSequence()
        {
            isFlashing = true;
            flashSequence = DOTween.Sequence();
            
            // 添加闪烁循环
            for (int i = 0; i < flashCount; i++)
            {
                AddSingleFlash(i);
                
                // 添加闪烁间隔（除了最后一次）
                if (i < flashCount - 1)
                {
                    float delay = randomizeFlashTiming ? 
                        Random.Range(randomDelayRange.x, randomDelayRange.y) : 
                        delayBetweenFlashes;
                    flashSequence.AppendInterval(delay);
                }
            }
            
            // 设置循环
            if (loopFlash)
            {
                flashSequence.SetLoops(-1, LoopType.Restart);
            }
            
            // 完成回调
            flashSequence.OnComplete(() =>
            {
                isFlashing = false;
                RestoreOriginalValues();
                OnFlashComplete?.Invoke();
            });
        }
        
        /// <summary>
        /// 添加单次闪烁动画
        /// </summary>
        /// <param name="flashIndex">闪烁索引</param>
        private void AddSingleFlash(int flashIndex)
        {
            switch (flashType)
            {
                case FlashType.Color:
                    AddColorFlash();
                    break;
                case FlashType.Alpha:
                    AddAlphaFlash();
                    break;
                case FlashType.Scale:
                    AddScaleFlash();
                    break;
                case FlashType.Intensity:
                    AddIntensityFlash();
                    break;
                case FlashType.Combined:
                    AddCombinedFlash();
                    break;
            }
            
            // 播放音效
            if (playAudioOnEachFlash || flashIndex == 0)
            {
                flashSequence.AppendCallback(() => PlayFlashSound());
            }
            
            // 单次闪烁完成回调
            flashSequence.AppendCallback(() => OnSingleFlashComplete?.Invoke());
        }
        
        /// <summary>
        /// 添加颜色闪烁动画
        /// </summary>
        private void AddColorFlash()
        {
            if (targetGraphic != null)
            {
                flashSequence.Append(targetGraphic.DOColor(flashColor, flashDuration / 2)
                    .SetEase(flashEase));
                flashSequence.Append(targetGraphic.DOColor(originalColor, flashDuration / 2)
                    .SetEase(flashEase));
            }
            else if (targetSpriteRenderer != null)
            {
                flashSequence.Append(targetSpriteRenderer.DOColor(flashColor, flashDuration / 2)
                    .SetEase(flashEase));
                flashSequence.Append(targetSpriteRenderer.DOColor(originalColor, flashDuration / 2)
                    .SetEase(flashEase));
            }
        }
        
        /// <summary>
        /// 添加透明度闪烁动画
        /// </summary>
        private void AddAlphaFlash()
        {
            if (targetGraphic != null)
            {
                flashSequence.Append(targetGraphic.DOFade(minAlpha, flashDuration / 2)
                    .SetEase(flashEase));
                flashSequence.Append(targetGraphic.DOFade(maxAlpha, flashDuration / 2)
                    .SetEase(flashEase));
            }
            else if (targetSpriteRenderer != null)
            {
                flashSequence.Append(targetSpriteRenderer.DOFade(minAlpha, flashDuration / 2)
                    .SetEase(flashEase));
                flashSequence.Append(targetSpriteRenderer.DOFade(maxAlpha, flashDuration / 2)
                    .SetEase(flashEase));
            }
        }
        
        /// <summary>
        /// 添加缩放闪烁动画
        /// </summary>
        private void AddScaleFlash()
        {
            flashSequence.Append(transform.DOScale(maxScale, flashDuration / 2)
                .SetEase(flashEase));
            flashSequence.Append(transform.DOScale(minScale, flashDuration / 2)
                .SetEase(flashEase));
        }
        
        /// <summary>
        /// 添加强度闪烁动画（光源）
        /// </summary>
        private void AddIntensityFlash()
        {
            if (targetLight != null)
            {
                flashSequence.Append(targetLight.DOIntensity(maxIntensity, flashDuration / 2)
                    .SetEase(flashEase));
                flashSequence.Append(targetLight.DOIntensity(minIntensity, flashDuration / 2)
                    .SetEase(flashEase));
            }
        }
        
        /// <summary>
        /// 添加组合闪烁动画
        /// </summary>
        private void AddCombinedFlash()
        {
            var combinedTween = DOTween.Sequence();
            
            // 同时执行多种效果
            if (targetGraphic != null)
            {
                combinedTween.Join(targetGraphic.DOColor(flashColor, flashDuration / 2));
            }
            
            combinedTween.Join(transform.DOScale(maxScale, flashDuration / 2));
            
            if (targetLight != null)
            {
                combinedTween.Join(targetLight.DOIntensity(maxIntensity, flashDuration / 2));
            }
            
            flashSequence.Append(combinedTween);
            
            // 恢复动画
            var restoreTween = DOTween.Sequence();
            
            if (targetGraphic != null)
            {
                restoreTween.Join(targetGraphic.DOColor(originalColor, flashDuration / 2));
            }
            
            restoreTween.Join(transform.DOScale(originalScale, flashDuration / 2));
            
            if (targetLight != null)
            {
                restoreTween.Join(targetLight.DOIntensity(originalIntensity, flashDuration / 2));
            }
            
            flashSequence.Append(restoreTween);
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// 恢复原始值
        /// </summary>
        private void RestoreOriginalValues()
        {
            // 恢复颜色
            if (targetGraphic != null)
                targetGraphic.color = originalColor;
            else if (targetSpriteRenderer != null)
                targetSpriteRenderer.color = originalColor;
            
            // 恢复缩放
            transform.localScale = originalScale;
            
            // 恢复光源强度
            if (targetLight != null)
                targetLight.intensity = originalIntensity;
                
            // 恢复粒子发射率
            if (targetParticles != null)
            {
                var emission = targetParticles.emission;
                emission.rateOverTime = originalParticleEmissionRate;
            }
        }
        
        /// <summary>
        /// 播放闪烁音效
        /// </summary>
        private void PlayFlashSound()
        {
            if (audioSource != null && flashSound != null)
            {
                audioSource.PlayOneShot(flashSound);
            }
        }
        
        #endregion
        
        #region Special Effects
        
        /// <summary>
        /// 创建脉冲效果
        /// </summary>
        /// <param name="pulseCount">脉冲次数</param>
        /// <param name="pulseScale">脉冲缩放</param>
        public void CreatePulseEffect(int pulseCount = 3, float pulseScale = 1.2f)
        {
            StopFlash();
            
            var pulseSequence = DOTween.Sequence();
            
            for (int i = 0; i < pulseCount; i++)
            {
                pulseSequence.Append(transform.DOScale(originalScale * pulseScale, 0.2f)
                    .SetEase(Ease.OutQuad));
                pulseSequence.Append(transform.DOScale(originalScale, 0.2f)
                    .SetEase(Ease.InQuad));
                
                if (i < pulseCount - 1)
                    pulseSequence.AppendInterval(0.1f);
            }
            
            pulseSequence.OnComplete(() => isFlashing = false);
            flashSequence = pulseSequence;
            isFlashing = true;
        }
        
        /// <summary>
        /// 创建呼吸效果
        /// </summary>
        /// <param name="breathDuration">呼吸周期</param>
        /// <param name="minAlpha">最小透明度</param>
        /// <param name="maxAlpha">最大透明度</param>
        public void CreateBreathingEffect(float breathDuration = 2f, float minAlpha = 0.3f, float maxAlpha = 1f)
        {
            StopFlash();
            
            if (targetGraphic != null)
            {
                flashSequence = targetGraphic.DOFade(minAlpha, breathDuration / 2)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            else if (targetSpriteRenderer != null)
            {
                flashSequence = targetSpriteRenderer.DOFade(minAlpha, breathDuration / 2)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            
            isFlashing = true;
        }
        
        /// <summary>
        /// 创建彩虹效果
        /// </summary>
        /// <param name="cycleDuration">颜色循环周期</param>
        public void CreateRainbowEffect(float cycleDuration = 3f)
        {
            StopFlash();
            
            if (targetGraphic == null && targetSpriteRenderer == null) return;
            
            var rainbowSequence = DOTween.Sequence();
            Color[] rainbowColors = {
                Color.red, Color.yellow, Color.green, 
                Color.cyan, Color.blue, Color.magenta
            };
            
            float colorDuration = cycleDuration / rainbowColors.Length;
            
            foreach (var color in rainbowColors)
            {
                if (targetGraphic != null)
                {
                    rainbowSequence.Append(targetGraphic.DOColor(color, colorDuration)
                        .SetEase(Ease.InOutSine));
                }
                else if (targetSpriteRenderer != null)
                {
                    rainbowSequence.Append(targetSpriteRenderer.DOColor(color, colorDuration)
                        .SetEase(Ease.InOutSine));
                }
            }
            
            rainbowSequence.SetLoops(-1, LoopType.Restart);
            flashSequence = rainbowSequence;
            isFlashing = true;
        }
        
        #endregion
        
        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 测试闪烁效果
        /// </summary>
        [ContextMenu("Test Flash")]
        private void TestFlash()
        {
            if (Application.isPlaying)
            {
                StartFlash();
            }
        }
        
        /// <summary>
        /// 测试单次闪烁
        /// </summary>
        [ContextMenu("Test Flash Once")]
        private void TestFlashOnce()
        {
            if (Application.isPlaying)
            {
                FlashOnce();
            }
        }
        
        /// <summary>
        /// 测试脉冲效果
        /// </summary>
        [ContextMenu("Test Pulse Effect")]
        private void TestPulseEffect()
        {
            if (Application.isPlaying)
            {
                CreatePulseEffect();
            }
        }
        
        /// <summary>
        /// 测试呼吸效果
        /// </summary>
        [ContextMenu("Test Breathing Effect")]
        private void TestBreathingEffect()
        {
            if (Application.isPlaying)
            {
                CreateBreathingEffect();
            }
        }
        
        /// <summary>
        /// 停止所有效果
        /// </summary>
        [ContextMenu("Stop All Effects")]
        private void TestStopFlash()
        {
            if (Application.isPlaying)
            {
                StopFlash();
            }
        }
#endif
        
        #endregion
    }
}