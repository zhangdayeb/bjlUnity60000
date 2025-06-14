using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SlotMachine.UI
{
    /// <summary>
    /// 余额显示组件 - 负责显示玩家当前余额和余额变化动画
    /// </summary>
    public class BalanceDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI currencySymbol;
        [SerializeField] private Image balanceBackground;
        
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 1f;
        [SerializeField] private AnimationCurve balanceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private Color positiveChangeColor = Color.green;
        [SerializeField] private Color negativeChangeColor = Color.red;
        [SerializeField] private Color normalColor = Color.white;
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem coinEffect;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip coinSound;
        [SerializeField] private AudioClip warningSound;
        
        [Header("Warning Settings")]
        [SerializeField] private float lowBalanceThreshold = 10f;
        [SerializeField] private GameObject lowBalanceWarning;
        
        // 私有变量
        private float currentBalance;
        private float targetBalance;
        private bool isAnimating;
        private Coroutine balanceAnimation;
        
        // 事件
        public System.Action<float> OnBalanceChanged;
        public System.Action OnLowBalance;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            SetupInitialState();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 如果没有设置文本组件，尝试自动查找
            if (balanceText == null)
                balanceText = GetComponentInChildren<TextMeshProUGUI>();
            
            // 如果没有设置音频源，尝试自动查找
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            
            // 验证必要组件
            if (balanceText == null)
            {
                Debug.LogError("BalanceDisplay: 缺少 TextMeshProUGUI 组件!");
            }
        }
        
        /// <summary>
        /// 设置初始状态
        /// </summary>
        private void SetupInitialState()
        {
            currentBalance = 0f;
            targetBalance = 0f;
            isAnimating = false;
            
            // 设置初始颜色
            if (balanceText != null)
                balanceText.color = normalColor;
            
            // 隐藏低余额警告
            if (lowBalanceWarning != null)
                lowBalanceWarning.SetActive(false);
            
            UpdateBalanceText();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 设置余额（带动画）
        /// </summary>
        /// <param name="newBalance">新的余额值</param>
        /// <param name="animate">是否显示动画</param>
        public void SetBalance(float newBalance, bool animate = true)
        {
            targetBalance = newBalance;
            
            if (animate && gameObject.activeInHierarchy)
            {
                AnimateToBalance();
            }
            else
            {
                currentBalance = newBalance;
                UpdateBalanceText();
                CheckLowBalance();
            }
            
            OnBalanceChanged?.Invoke(newBalance);
        }
        
        /// <summary>
        /// 增加余额
        /// </summary>
        /// <param name="amount">增加的金额</param>
        public void AddBalance(float amount)
        {
            SetBalance(currentBalance + amount, true);
            
            // 播放增加余额效果
            if (amount > 0)
            {
                PlayPositiveEffect();
            }
        }
        
        /// <summary>
        /// 减少余额
        /// </summary>
        /// <param name="amount">减少的金额</param>
        public void SubtractBalance(float amount)
        {
            SetBalance(currentBalance - amount, true);
            
            // 播放减少余额效果
            if (amount > 0)
            {
                PlayNegativeEffect();
            }
        }
        
        /// <summary>
        /// 获取当前余额
        /// </summary>
        /// <returns>当前余额</returns>
        public float GetCurrentBalance()
        {
            return currentBalance;
        }
        
        /// <summary>
        /// 检查是否有足够的余额
        /// </summary>
        /// <param name="amount">需要检查的金额</param>
        /// <returns>是否有足够余额</returns>
        public bool HasSufficientBalance(float amount)
        {
            return currentBalance >= amount;
        }
        
        /// <summary>
        /// 设置货币符号
        /// </summary>
        /// <param name="symbol">货币符号</param>
        public void SetCurrencySymbol(string symbol)
        {
            if (currencySymbol != null)
            {
                currencySymbol.text = symbol;
            }
        }
        
        #endregion
        
        #region Animation
        
        /// <summary>
        /// 动画过渡到目标余额
        /// </summary>
        private void AnimateToBalance()
        {
            if (balanceAnimation != null)
            {
                StopCoroutine(balanceAnimation);
            }
            
            balanceAnimation = StartCoroutine(BalanceAnimationCoroutine());
        }
        
        /// <summary>
        /// 余额动画协程
        /// </summary>
        private System.Collections.IEnumerator BalanceAnimationCoroutine()
        {
            isAnimating = true;
            float startBalance = currentBalance;
            float difference = targetBalance - startBalance;
            
            // 设置颜色
            Color targetColor = difference > 0 ? positiveChangeColor : 
                               difference < 0 ? negativeChangeColor : normalColor;
            
            if (balanceText != null)
                balanceText.color = targetColor;
            
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / animationDuration;
                float curveValue = balanceCurve.Evaluate(progress);
                
                currentBalance = startBalance + (difference * curveValue);
                UpdateBalanceText();
                
                yield return null;
            }
            
            // 确保最终值正确
            currentBalance = targetBalance;
            UpdateBalanceText();
            
            // 恢复正常颜色
            if (balanceText != null)
            {
                yield return new WaitForSeconds(0.5f);
                StartCoroutine(FadeToNormalColor());
            }
            
            isAnimating = false;
            CheckLowBalance();
        }
        
        /// <summary>
        /// 渐变回正常颜色
        /// </summary>
        private System.Collections.IEnumerator FadeToNormalColor()
        {
            if (balanceText == null) yield break;
            
            Color startColor = balanceText.color;
            float fadeTime = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeTime;
                balanceText.color = Color.Lerp(startColor, normalColor, progress);
                yield return null;
            }
            
            balanceText.color = normalColor;
        }
        
        #endregion
        
        #region Effects
        
        /// <summary>
        /// 播放正向效果（增加余额）
        /// </summary>
        private void PlayPositiveEffect()
        {
            // 播放粒子效果
            if (coinEffect != null)
            {
                coinEffect.Play();
            }
            
            // 播放音效
            if (audioSource != null && coinSound != null)
            {
                audioSource.PlayOneShot(coinSound);
            }
            
            // 缩放动画
            StartCoroutine(ScaleEffect(1.1f, 0.2f));
        }
        
        /// <summary>
        /// 播放负向效果（减少余额）
        /// </summary>
        private void PlayNegativeEffect()
        {
            // 震动效果
            StartCoroutine(ShakeEffect());
        }
        
        /// <summary>
        /// 缩放效果
        /// </summary>
        private System.Collections.IEnumerator ScaleEffect(float targetScale, float duration)
        {
            Vector3 originalScale = transform.localScale;
            Vector3 targetScaleVector = originalScale * targetScale;
            
            // 放大
            float elapsed = 0f;
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (duration / 2);
                transform.localScale = Vector3.Lerp(originalScale, targetScaleVector, progress);
                yield return null;
            }
            
            // 缩小回原大小
            elapsed = 0f;
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (duration / 2);
                transform.localScale = Vector3.Lerp(targetScaleVector, originalScale, progress);
                yield return null;
            }
            
            transform.localScale = originalScale;
        }
        
        /// <summary>
        /// 震动效果
        /// </summary>
        private System.Collections.IEnumerator ShakeEffect()
        {
            Vector3 originalPosition = transform.localPosition;
            float shakeIntensity = 5f;
            float shakeDuration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / shakeDuration;
                float intensity = shakeIntensity * (1 - progress);
                
                Vector3 randomOffset = new Vector3(
                    Random.Range(-intensity, intensity),
                    Random.Range(-intensity, intensity),
                    0
                );
                
                transform.localPosition = originalPosition + randomOffset;
                yield return null;
            }
            
            transform.localPosition = originalPosition;
        }
        
        #endregion
        
        #region UI Updates
        
        /// <summary>
        /// 更新余额文本显示
        /// </summary>
        private void UpdateBalanceText()
        {
            if (balanceText != null)
            {
                // 格式化余额显示（保留2位小数）
                balanceText.text = $"{currentBalance:F2}";
            }
        }
        
        /// <summary>
        /// 检查低余额警告
        /// </summary>
        private void CheckLowBalance()
        {
            bool isLowBalance = currentBalance <= lowBalanceThreshold;
            
            if (lowBalanceWarning != null)
            {
                lowBalanceWarning.SetActive(isLowBalance);
            }
            
            if (isLowBalance)
            {
                OnLowBalance?.Invoke();
                
                // 播放警告音效
                if (audioSource != null && warningSound != null)
                {
                    audioSource.PlayOneShot(warningSound);
                }
            }
        }
        
        #endregion
        
        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中的测试方法
        /// </summary>
        [ContextMenu("Test Add Balance")]
        private void TestAddBalance()
        {
            AddBalance(100f);
        }
        
        [ContextMenu("Test Subtract Balance")]
        private void TestSubtractBalance()
        {
            SubtractBalance(50f);
        }
        
        [ContextMenu("Test Low Balance")]
        private void TestLowBalance()
        {
            SetBalance(5f);
        }
#endif
        
        #endregion
    }
}