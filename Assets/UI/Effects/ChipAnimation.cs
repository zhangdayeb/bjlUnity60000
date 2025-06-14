using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

namespace SlotMachine.Effects
{
    /// <summary>
    /// 筹码动画组件 - 处理筹码的各种动画效果，包括投注、获奖、收集等
    /// </summary>
    public class ChipAnimation : MonoBehaviour
    {
        [Header("Chip Prefabs")]
        [SerializeField] private GameObject chipPrefab;
        [SerializeField] private Transform chipContainer;
        [SerializeField] private Sprite[] chipSprites; // 不同面值的筹码图片
        [SerializeField] private Color[] chipColors;   // 不同面值的筹码颜色
        
        [Header("Animation Positions")]
        [SerializeField] private Transform startPosition;      // 起始位置
        [SerializeField] private Transform betAreaPosition;    // 投注区域位置
        [SerializeField] private Transform winAreaPosition;    // 获奖区域位置
        [SerializeField] private Transform collectPosition;    // 收集位置
        
        [Header("Animation Settings")]
        [SerializeField] private float moveDuration = 1f;
        [SerializeField] private float stackDelay = 0.1f;
        [SerializeField] private int maxVisibleChips = 10;
        [SerializeField] private Vector3 stackOffset = new Vector3(0, 0.02f, 0);
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Chip Effects")]
        [SerializeField] private bool enableRotation = true;
        [SerializeField] private bool enableScale = true;
        [SerializeField] private bool enableBounce = true;
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private Vector3 scaleMultiplier = Vector3.one * 1.2f;
        [SerializeField] private float bounceHeight = 50f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip chipPlaceSound;
        [SerializeField] private AudioClip chipStackSound;
        [SerializeField] private AudioClip chipCollectSound;
        [SerializeField] private AudioClip chipWinSound;
        
        [Header("Particle Effects")]
        [SerializeField] private ParticleSystem chipTrailEffect;
        [SerializeField] private ParticleSystem chipImpactEffect;
        [SerializeField] private ParticleSystem chipGlowEffect;
        
        // 私有变量
        private List<GameObject> activeChips = new List<GameObject>();
        private Queue<GameObject> chipPool = new Queue<GameObject>();
        private Sequence currentAnimation;
        private int currentChipCount = 0;
        
        // 事件
        public System.Action<float> OnBetPlaced;
        public System.Action<float> OnWinCollected;
        public System.Action OnAnimationComplete;
        
        /// <summary>
        /// 筹码面值枚举
        /// </summary>
        public enum ChipValue
        {
            One = 1,
            Five = 5,
            Ten = 10,
            TwentyFive = 25,
            Fifty = 50,
            Hundred = 100,
            FiveHundred = 500,
            Thousand = 1000
        }
        
        /// <summary>
        /// 动画类型枚举
        /// </summary>
        public enum AnimationType
        {
            PlaceBet,       // 下注动画
            CollectWin,     // 收集奖金动画
            StackChips,     // 堆叠筹码动画
            ScatterChips,   // 散开筹码动画
            ReturnChips     // 返还筹码动画
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
            InitializeChipPool();
        }
        
        private void Start()
        {
            SetupPositions();
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
            if (chipContainer == null)
            {
                chipContainer = transform;
            }
            
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            
            // 验证必要组件
            if (chipPrefab == null)
            {
                Debug.LogError("ChipAnimation: 缺少筹码预制体引用!");
            }
        }
        
        /// <summary>
        /// 初始化筹码对象池
        /// </summary>
        private void InitializeChipPool()
        {
            for (int i = 0; i < maxVisibleChips * 2; i++)
            {
                CreatePooledChip();
            }
        }
        
        /// <summary>
        /// 设置位置引用
        /// </summary>
        private void SetupPositions()
        {
            // 如果没有设置位置，使用默认位置
            if (startPosition == null)
                startPosition = transform;
                
            if (betAreaPosition == null)
                betAreaPosition = transform;
                
            if (winAreaPosition == null)
                winAreaPosition = transform;
                
            if (collectPosition == null)
                collectPosition = transform;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 播放下注动画
        /// </summary>
        /// <param name="betAmount">下注金额</param>
        /// <param name="targetPosition">目标位置（可选）</param>
        public void PlayBetAnimation(float betAmount, Transform targetPosition = null)
        {
            StopCurrentAnimation();
            
            Transform target = targetPosition ?? betAreaPosition;
            int chipCount = CalculateChipCount(betAmount);
            
            StartCoroutine(AnimateBetPlacement(chipCount, target, betAmount));
        }
        
        /// <summary>
        /// 播放获奖收集动画
        /// </summary>
        /// <param name="winAmount">获奖金额</param>
        /// <param name="sourcePosition">来源位置（可选）</param>
        public void PlayWinAnimation(float winAmount, Transform sourcePosition = null)
        {
            StopCurrentAnimation();
            
            Transform source = sourcePosition ?? winAreaPosition;
            int chipCount = CalculateChipCount(winAmount);
            
            StartCoroutine(AnimateWinCollection(chipCount, source, winAmount));
        }
        
        /// <summary>
        /// 播放筹码堆叠动画
        /// </summary>
        /// <param name="chipCount">筹码数量</param>
        /// <param name="targetPosition">目标位置</param>
        public void PlayStackAnimation(int chipCount, Transform targetPosition)
        {
            StopCurrentAnimation();
            StartCoroutine(AnimateChipStack(chipCount, targetPosition));
        }
        
        /// <summary>
        /// 播放筹码散开动画
        /// </summary>
        /// <param name="chipCount">筹码数量</param>
        /// <param name="scatterRadius">散开半径</param>
        public void PlayScatterAnimation(int chipCount, float scatterRadius = 2f)
        {
            StopCurrentAnimation();
            StartCoroutine(AnimateChipScatter(chipCount, scatterRadius));
        }
        
        /// <summary>
        /// 清除所有筹码
        /// </summary>
        /// <param name="animate">是否使用动画</param>
        public void ClearAllChips(bool animate = true)
        {
            if (animate)
            {
                StartCoroutine(AnimateClearChips());
            }
            else
            {
                foreach (var chip in activeChips)
                {
                    ReturnChipToPool(chip);
                }
                activeChips.Clear();
                currentChipCount = 0;
            }
        }
        
        /// <summary>
        /// 停止所有动画
        /// </summary>
        public void StopAllAnimations()
        {
            StopCurrentAnimation();
            StopAllCoroutines();
        }
        
        #endregion
        
        #region Animation Coroutines
        
        /// <summary>
        /// 下注放置动画协程
        /// </summary>
        private System.Collections.IEnumerator AnimateBetPlacement(int chipCount, Transform target, float betAmount)
        {
            PlaySound(chipPlaceSound);
            
            for (int i = 0; i < chipCount; i++)
            {
                GameObject chip = GetChipFromPool();
                SetupChip(chip, GetChipValue(betAmount, i, chipCount));
                
                // 设置起始位置
                chip.transform.position = startPosition.position;
                chip.transform.localScale = Vector3.zero;
                
                // 目标位置（带堆叠偏移）
                Vector3 targetPos = target.position + (stackOffset * i);
                
                // 创建移动动画
                var moveSequence = DOTween.Sequence();
                
                // 缩放出现
                moveSequence.Append(chip.transform.DOScale(Vector3.one, 0.2f)
                    .SetEase(Ease.OutBack));
                
                // 移动到目标位置
                moveSequence.Append(chip.transform.DOMove(targetPos, moveDuration)
                    .SetEase(moveCurve));
                
                // 添加旋转效果
                if (enableRotation)
                {
                    moveSequence.Join(chip.transform.DORotate(
                        new Vector3(0, 0, rotationSpeed), moveDuration, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear));
                }
                
                // 添加弹跳效果
                if (enableBounce && i == chipCount - 1)
                {
                    moveSequence.Append(chip.transform.DOMoveY(targetPos.y + bounceHeight, 0.2f)
                        .SetEase(Ease.OutQuad));
                    moveSequence.Append(chip.transform.DOMoveY(targetPos.y, 0.2f)
                        .SetEase(Ease.InQuad));
                }
                
                // 播放堆叠音效
                if (i > 0)
                {
                    moveSequence.AppendCallback(() => PlaySound(chipStackSound));
                }
                
                activeChips.Add(chip);
                currentChipCount++;
                
                // 播放粒子效果
                if (chipTrailEffect != null)
                {
                    PlayParticleEffect(chipTrailEffect, chip.transform.position);
                }
                
                yield return new WaitForSeconds(stackDelay);
            }
            
            OnBetPlaced?.Invoke(betAmount);
            OnAnimationComplete?.Invoke();
        }
        
        /// <summary>
        /// 获奖收集动画协程
        /// </summary>
        private System.Collections.IEnumerator AnimateWinCollection(int chipCount, Transform source, float winAmount)
        {
            PlaySound(chipWinSound);
            
            // 创建获奖筹码
            List<GameObject> winChips = new List<GameObject>();
            
            for (int i = 0; i < chipCount; i++)
            {
                GameObject chip = GetChipFromPool();
                SetupChip(chip, GetChipValue(winAmount, i, chipCount));
                
                // 设置起始位置（从获奖区域开始）
                Vector3 startPos = source.position + (stackOffset * i);
                chip.transform.position = startPos;
                chip.transform.localScale = Vector3.one;
                
                winChips.Add(chip);
                
                // 添加发光效果
                if (chipGlowEffect != null)
                {
                    PlayParticleEffect(chipGlowEffect, chip.transform.position);
                }
                
                yield return new WaitForSeconds(stackDelay * 0.5f);
            }
            
            // 等待一下让玩家看到获奖筹码
            yield return new WaitForSeconds(0.5f);
            
            // 收集动画
            for (int i = 0; i < winChips.Count; i++)
            {
                var chip = winChips[i];
                var collectSequence = DOTween.Sequence();
                
                // 移动到收集位置
                collectSequence.Append(chip.transform.DOMove(collectPosition.position, moveDuration * 0.8f)
                    .SetEase(Ease.InQuad));
                
                // 缩放效果
                if (enableScale)
                {
                    collectSequence.Join(chip.transform.DOScale(scaleMultiplier, moveDuration * 0.4f)
                        .SetEase(Ease.OutQuad));
                    collectSequence.Append(chip.transform.DOScale(Vector3.zero, moveDuration * 0.4f)
                        .SetEase(Ease.InQuad));
                }
                
                // 旋转效果
                if (enableRotation)
                {
                    collectSequence.Join(chip.transform.DORotate(
                        new Vector3(0, 0, -rotationSpeed), moveDuration * 0.8f, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear));
                }
                
                // 动画完成后返回对象池
                collectSequence.OnComplete(() =>
                {
                    ReturnChipToPool(chip);
                    PlaySound(chipCollectSound);
                    
                    // 播放冲击效果
                    if (chipImpactEffect != null)
                    {
                        PlayParticleEffect(chipImpactEffect, collectPosition.position);
                    }
                });
                
                yield return new WaitForSeconds(stackDelay * 0.3f);
            }
            
            OnWinCollected?.Invoke(winAmount);
            OnAnimationComplete?.Invoke();
        }
        
        /// <summary>
        /// 筹码堆叠动画协程
        /// </summary>
        private System.Collections.IEnumerator AnimateChipStack(int chipCount, Transform target)
        {
            for (int i = 0; i < chipCount; i++)
            {
                GameObject chip = GetChipFromPool();
                SetupChip(chip, ChipValue.Ten); // 默认使用10面值筹码
                
                // 随机起始位置
                Vector3 randomStart = target.position + Random.insideUnitSphere * 2f;
                randomStart.y = target.position.y + 5f; // 从上方落下
                chip.transform.position = randomStart;
                
                // 目标位置
                Vector3 targetPos = target.position + (stackOffset * i);
                
                // 落下动画
                var fallSequence = DOTween.Sequence();
                fallSequence.Append(chip.transform.DOMove(targetPos, 0.5f + (i * 0.1f))
                    .SetEase(Ease.OutBounce));
                
                // 旋转效果
                fallSequence.Join(chip.transform.DORotate(
                    new Vector3(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180)),
                    0.5f + (i * 0.1f), RotateMode.FastBeyond360));
                
                fallSequence.OnComplete(() => PlaySound(chipStackSound));
                
                activeChips.Add(chip);
                yield return new WaitForSeconds(0.1f);
            }
            
            OnAnimationComplete?.Invoke();
        }
        
        /// <summary>
        /// 筹码散开动画协程
        /// </summary>
        private System.Collections.IEnumerator AnimateChipScatter(int chipCount, float radius)
        {
            Vector3 centerPosition = transform.position;
            
            for (int i = 0; i < chipCount; i++)
            {
                GameObject chip = GetChipFromPool();
                SetupChip(chip, (ChipValue)(Random.Range(1, 8))); // 随机筹码面值
                
                chip.transform.position = centerPosition;
                chip.transform.localScale = Vector3.one;
                
                // 随机散开位置
                Vector2 randomCircle = Random.insideUnitCircle * radius;
                Vector3 scatterPos = centerPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
                
                // 散开动画
                var scatterSequence = DOTween.Sequence();
                
                // 向上弹起然后散开
                scatterSequence.Append(chip.transform.DOMoveY(centerPosition.y + Random.Range(1f, 3f), 0.3f)
                    .SetEase(Ease.OutQuad));
                scatterSequence.Join(chip.transform.DOMove(scatterPos, 0.8f)
                    .SetEase(Ease.OutQuad));
                
                // 旋转效果
                scatterSequence.Join(chip.transform.DORotate(
                    new Vector3(Random.Range(-360, 360), Random.Range(-360, 360), Random.Range(-360, 360)),
                    0.8f, RotateMode.FastBeyond360));
                
                // 最后落地
                scatterSequence.Append(chip.transform.DOMoveY(centerPosition.y, 0.2f)
                    .SetEase(Ease.InQuad));
                
                activeChips.Add(chip);
                yield return new WaitForSeconds(0.05f);
            }
            
            OnAnimationComplete?.Invoke();
        }
        
        /// <summary>
        /// 清除筹码动画协程
        /// </summary>
        private System.Collections.IEnumerator AnimateClearChips()
        {
            var chipsToRemove = new List<GameObject>(activeChips);
            
            foreach (var chip in chipsToRemove)
            {
                // 消失动画
                var clearSequence = DOTween.Sequence();
                clearSequence.Append(chip.transform.DOScale(Vector3.zero, 0.3f)
                    .SetEase(Ease.InBack));
                clearSequence.Join(chip.transform.DORotate(
                    new Vector3(0, 0, 360), 0.3f, RotateMode.FastBeyond360));
                
                clearSequence.OnComplete(() => ReturnChipToPool(chip));
                
                yield return new WaitForSeconds(0.05f);
            }
            
            activeChips.Clear();
            currentChipCount = 0;
            OnAnimationComplete?.Invoke();
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// 创建对象池中的筹码
        /// </summary>
        private void CreatePooledChip()
        {
            if (chipPrefab == null) return;
            
            GameObject chip = Instantiate(chipPrefab, chipContainer);
            chip.SetActive(false);
            chipPool.Enqueue(chip);
        }
        
        /// <summary>
        /// 从对象池获取筹码
        /// </summary>
        private GameObject GetChipFromPool()
        {
            GameObject chip;
            
            if (chipPool.Count > 0)
            {
                chip = chipPool.Dequeue();
            }
            else
            {
                chip = Instantiate(chipPrefab, chipContainer);
            }
            
            chip.SetActive(true);
            return chip;
        }
        
        /// <summary>
        /// 将筹码返回对象池
        /// </summary>
        private void ReturnChipToPool(GameObject chip)
        {
            if (chip == null) return;
            
            chip.SetActive(false);
            chip.transform.DOKill(); // 停止所有DOTween动画
            chip.transform.localScale = Vector3.one;
            chip.transform.rotation = Quaternion.identity;
            
            chipPool.Enqueue(chip);
            activeChips.Remove(chip);
        }
        
        /// <summary>
        /// 设置筹码外观
        /// </summary>
        private void SetupChip(GameObject chip, ChipValue value)
        {
            var image = chip.GetComponent<Image>();
            var spriteRenderer = chip.GetComponent<SpriteRenderer>();
            
            int index = Mathf.Clamp((int)Mathf.Log10((float)value), 0, chipSprites.Length - 1);
            
            if (image != null && chipSprites.Length > index)
            {
                image.sprite = chipSprites[index];
                if (chipColors.Length > index)
                    image.color = chipColors[index];
            }
            else if (spriteRenderer != null && chipSprites.Length > index)
            {
                spriteRenderer.sprite = chipSprites[index];
                if (chipColors.Length > index)
                    spriteRenderer.color = chipColors[index];
            }
        }
        
        /// <summary>
        /// 根据金额计算筹码数量
        /// </summary>
        private int CalculateChipCount(float amount)
        {
            // 简化计算，实际项目中可能需要更复杂的逻辑
            int count = Mathf.CeilToInt(amount / 10f);
            return Mathf.Clamp(count, 1, maxVisibleChips);
        }
        
        /// <summary>
        /// 根据金额和索引获取筹码面值
        /// </summary>
        private ChipValue GetChipValue(float totalAmount, int index, int totalCount)
        {
            // 简化逻辑：根据总金额选择合适的筹码面值
            if (totalAmount >= 1000)
                return ChipValue.Hundred;
            else if (totalAmount >= 100)
                return ChipValue.TwentyFive;
            else if (totalAmount >= 50)
                return ChipValue.Ten;
            else
                return ChipValue.Five;
        }
        
        /// <summary>
        /// 停止当前动画
        /// </summary>
        private void StopCurrentAnimation()
        {
            if (currentAnimation != null && currentAnimation.IsActive())
            {
                currentAnimation.Kill();
            }
        }
        
        /// <summary>
        /// 播放音效
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        /// <summary>
        /// 播放粒子效果
        /// </summary>
        private void PlayParticleEffect(ParticleSystem effect, Vector3 position)
        {
            if (effect != null)
            {
                effect.transform.position = position;
                effect.Play();
            }
        }
        
        #endregion
        
        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 测试下注动画
        /// </summary>
        [ContextMenu("Test Bet Animation")]
        private void TestBetAnimation()
        {
            if (Application.isPlaying)
            {
                PlayBetAnimation(50f);
            }
        }
        
        /// <summary>
        /// 测试获奖动画
        /// </summary>
        [ContextMenu("Test Win Animation")]
        private void TestWinAnimation()
        {
            if (Application.isPlaying)
            {
                PlayWinAnimation(100f);
            }
        }
        
        /// <summary>
        /// 测试堆叠动画
        /// </summary>
        [ContextMenu("Test Stack Animation")]
        private void TestStackAnimation()
        {
            if (Application.isPlaying)
            {
                PlayStackAnimation(5, transform);
            }
        }
        
        /// <summary>
        /// 测试散开动画
        /// </summary>
        [ContextMenu("Test Scatter Animation")]
        private void TestScatterAnimation()
        {
            if (Application.isPlaying)
            {
                PlayScatterAnimation(8, 3f);
            }
        }
        
        /// <summary>
        /// 清除所有筹码
        /// </summary>
        [ContextMenu("Clear All Chips")]
        private void TestClearChips()
        {
            if (Application.isPlaying)
            {
                ClearAllChips();
            }
        }
#endif
        
        #endregion
    }
}