using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

namespace SlotMachine.Effects
{
    /// <summary>
    /// 获奖特效组件 - 负责播放各种获奖时的视觉特效
    /// </summary>
    public class WinningEffect : MonoBehaviour
    {
        [Header("Effect Objects")]
        [SerializeField] private GameObject[] fireworkPrefabs;
        [SerializeField] private GameObject[] explosionPrefabs;
        [SerializeField] private GameObject[] lightRayPrefabs;
        [SerializeField] private Transform effectContainer;
        
        [Header("Screen Effects")]
        [SerializeField] private Image screenFlash;
        [SerializeField] private Image[] colorOverlays;
        [SerializeField] private Camera effectCamera;
        [SerializeField] private PostProcessVolume postProcessVolume; // 需要Post Processing包
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI[] winTexts;
        [SerializeField] private Image[] winIcons;
        [SerializeField] private RectTransform[] animatedElements;
        [SerializeField] private CanvasGroup mainCanvasGroup;
        
        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem confettiSystem;
        [SerializeField] private ParticleSystem goldCoinsSystem;
        [SerializeField] private ParticleSystem starsSystem;
        [SerializeField] private ParticleSystem rainbowSystem;
        [SerializeField] private ParticleSystem smokeSystem;
        
        [Header("Light Effects")]
        [SerializeField] private Light[] spotLights;
        [SerializeField] private Light[] pointLights;
        [SerializeField] private float lightFlashDuration = 0.5f;
        [SerializeField] private AnimationCurve lightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip smallWinEffect;
        [SerializeField] private AudioClip bigWinEffect;
        [SerializeField] private AudioClip megaWinEffect;
        [SerializeField] private AudioClip jackpotEffect;
        [SerializeField] private AudioClip fireworkSound;
        [SerializeField] private AudioClip explosionSound;
        
        [Header("Win Type Settings")]
        [SerializeField] private WinEffectConfig[] winConfigs;
        
        // 私有变量
        private List<GameObject> activeEffects = new List<GameObject>();
        private Sequence currentEffectSequence;
        private bool isPlayingEffect;
        private Queue<GameObject> fireworkPool = new Queue<GameObject>();
        private Queue<GameObject> explosionPool = new Queue<GameObject>();
        
        // 事件
        public System.Action OnEffectStart;
        public System.Action OnEffectComplete;
        
        /// <summary>
        /// 获奖类型枚举
        /// </summary>
        public enum WinType
        {
            SmallWin,
            BigWin,
            MegaWin,
            Jackpot,
            BonusWin,
            FreeSpinWin
        }
        
        /// <summary>
        /// 获奖特效配置
        /// </summary>
        [System.Serializable]
        public class WinEffectConfig
        {
            public WinType winType;
            public Color primaryColor = Color.yellow;
            public Color secondaryColor = Color.white;
            public float effectDuration = 2f;
            public int fireworkCount = 3;
            public int explosionCount = 1;
            public bool useScreenFlash = true;
            public bool useCameraShake = true;
            public bool useLightEffect = true;
            public bool useParticles = true;
            public float screenShakeIntensity = 0.5f;
            public float lightIntensityMultiplier = 2f;
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
            InitializeObjectPools();
        }
        
        private void Start()
        {
            SetupInitialState();
        }
        
        private void OnDestroy()
        {
            StopAllEffects();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            if (effectContainer == null)
                effectContainer = transform;
                
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
                
            if (effectCamera == null)
                effectCamera = Camera.main;
            
            // 验证必要组件
            if (screenFlash == null)
                Debug.LogWarning("WinningEffect: 建议设置屏幕闪烁效果组件");
        }
        
        /// <summary>
        /// 初始化对象池
        /// </summary>
        private void InitializeObjectPools()
        {
            // 初始化烟花对象池
            if (fireworkPrefabs != null)
            {
                foreach (var prefab in fireworkPrefabs)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        CreatePooledFirework(prefab);
                    }
                }
            }
            
            // 初始化爆炸对象池
            if (explosionPrefabs != null)
            {
                foreach (var prefab in explosionPrefabs)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        CreatePooledExplosion(prefab);
                    }
                }
            }
        }
        
        /// <summary>
        /// 设置初始状态
        /// </summary>
        private void SetupInitialState()
        {
            // 隐藏屏幕闪烁
            if (screenFlash != null)
            {
                Color flashColor = screenFlash.color;
                flashColor.a = 0f;
                screenFlash.color = flashColor;
            }
            
            // 隐藏颜色覆盖层
            if (colorOverlays != null)
            {
                foreach (var overlay in colorOverlays)
                {
                    if (overlay != null)
                    {
                        Color overlayColor = overlay.color;
                        overlayColor.a = 0f;
                        overlay.color = overlayColor;
                    }
                }
            }
            
            // 重置光源
            ResetLights();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 播放获奖特效
        /// </summary>
        /// <param name="winType">获奖类型</param>
        /// <param name="winAmount">获奖金额</param>
        /// <param name="customDuration">自定义持续时间</param>
        public void PlayWinEffect(WinType winType, float winAmount = 0f, float customDuration = -1f)
        {
            if (isPlayingEffect) return;
            
            StopAllEffects();
            
            WinEffectConfig config = GetWinConfig(winType);
            float duration = customDuration > 0 ? customDuration : config.effectDuration;
            
            StartCoroutine(PlayWinEffectSequence(config, winAmount, duration));
        }
        
        /// <summary>
        /// 停止所有特效
        /// </summary>
        public void StopAllEffects()
        {
            if (currentEffectSequence != null && currentEffectSequence.IsActive())
            {
                currentEffectSequence.Kill();
            }
            
            StopAllCoroutines();
            ClearActiveEffects();
            ResetAllVisuals();
            isPlayingEffect = false;
        }
        
        /// <summary>
        /// 播放烟花特效
        /// </summary>
        /// <param name="position">烟花位置</param>
        /// <param name="color">烟花颜色</param>
        public void PlayFirework(Vector3 position, Color color)
        {
            GameObject firework = GetFireworkFromPool();
            if (firework != null)
            {
                firework.transform.position = position;
                SetFireworkColor(firework, color);
                StartCoroutine(AnimateFirework(firework));
                PlaySound(fireworkSound);
            }
        }
        
        /// <summary>
        /// 播放爆炸特效
        /// </summary>
        /// <param name="position">爆炸位置</param>
        /// <param name="intensity">爆炸强度</param>
        public void PlayExplosion(Vector3 position, float intensity = 1f)
        {
            GameObject explosion = GetExplosionFromPool();
            if (explosion != null)
            {
                explosion.transform.position = position;
                explosion.transform.localScale = Vector3.one * intensity;
                StartCoroutine(AnimateExplosion(explosion));
                PlaySound(explosionSound);
            }
        }
        
        /// <summary>
        /// 播放屏幕闪烁
        /// </summary>
        /// <param name="color">闪烁颜色</param>
        /// <param name="duration">持续时间</param>
        /// <param name="intensity">强度</param>
        public void PlayScreenFlash(Color color, float duration = 0.3f, float intensity = 0.7f)
        {
            if (screenFlash != null)
            {
                color.a = intensity;
                screenFlash.color = color;
                screenFlash.DOFade(0f, duration).SetEase(Ease.OutQuad);
            }
        }
        
        /// <summary>
        /// 播放相机震动
        /// </summary>
        /// <param name="intensity">震动强度</param>
        /// <param name="duration">持续时间</param>
        public void PlayCameraShake(float intensity = 0.5f, float duration = 0.5f)
        {
            if (effectCamera != null)
            {
                effectCamera.transform.DOShakePosition(duration, intensity, 10, 90, false, true);
            }
        }
        
        #endregion
        
        #region Effect Sequences
        
        /// <summary>
        /// 播放获奖特效序列
        /// </summary>
        private System.Collections.IEnumerator PlayWinEffectSequence(WinEffectConfig config, float winAmount, float duration)
        {
            isPlayingEffect = true;
            OnEffectStart?.Invoke();
            
            // 播放音效
            PlayWinAudio(config.winType);
            
            // 1. 屏幕闪烁
            if (config.useScreenFlash)
            {
                PlayScreenFlash(config.primaryColor, 0.2f, 0.8f);
            }
            
            // 2. 相机震动
            if (config.useCameraShake)
            {
                PlayCameraShake(config.screenShakeIntensity, 0.3f);
            }
            
            yield return new WaitForSeconds(0.1f);
            
            // 3. 光源效果
            if (config.useLightEffect)
            {
                StartCoroutine(AnimateLights(config));
            }
            
            // 4. 粒子系统
            if (config.useParticles)
            {
                StartCoroutine(AnimateParticles(config));
            }
            
            yield return new WaitForSeconds(0.2f);
            
            // 5. 烟花效果
            for (int i = 0; i < config.fireworkCount; i++)
            {
                Vector3 randomPos = GetRandomScreenPosition();
                PlayFirework(randomPos, config.primaryColor);
                yield return new WaitForSeconds(0.3f);
            }
            
            // 6. 爆炸效果
            for (int i = 0; i < config.explosionCount; i++)
            {
                Vector3 centerPos = effectCamera.transform.position + effectCamera.transform.forward * 5f;
                PlayExplosion(centerPos, 1.5f);
                yield return new WaitForSeconds(0.5f);
            }
            
            // 7. UI文本动画
            StartCoroutine(AnimateWinTexts(config, winAmount));
            
            // 8. 颜色覆盖层动画
            StartCoroutine(AnimateColorOverlays(config));
            
            // 等待效果完成
            yield return new WaitForSeconds(duration - 1.5f);
            
            // 9. 淡出效果
            StartCoroutine(FadeOutEffects());
            
            yield return new WaitForSeconds(1f);
            
            isPlayingEffect = false;
            OnEffectComplete?.Invoke();
        }
        
        /// <summary>
        /// 动画化光源效果
        /// </summary>
        private System.Collections.IEnumerator AnimateLights(WinEffectConfig config)
        {
            // 聚光灯效果
            if (spotLights != null)
            {
                foreach (var light in spotLights)
                {
                    if (light != null)
                    {
                        light.color = config.primaryColor;
                        light.intensity = 0f;
                        light.DOIntensity(config.lightIntensityMultiplier, lightFlashDuration)
                            .SetEase(lightCurve);
                    }
                }
            }
            
            yield return new WaitForSeconds(0.2f);
            
            // 点光源效果
            if (pointLights != null)
            {
                foreach (var light in pointLights)
                {
                    if (light != null)
                    {
                        light.color = config.secondaryColor;
                        light.intensity = 0f;
                        light.DOIntensity(config.lightIntensityMultiplier * 0.8f, lightFlashDuration)
                            .SetEase(lightCurve)
                            .SetDelay(Random.Range(0f, 0.3f));
                    }
                }
            }
            
            // 光源闪烁循环
            float elapsed = 0f;
            while (elapsed < 2f)
            {
                foreach (var light in spotLights)
                {
                    if (light != null)
                    {
                        light.DOIntensity(Random.Range(0.5f, config.lightIntensityMultiplier), 0.1f);
                    }
                }
                
                elapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        /// <summary>
        /// 动画化粒子系统
        /// </summary>
        private System.Collections.IEnumerator AnimateParticles(WinEffectConfig config)
        {
            // 彩带粒子
            if (confettiSystem != null)
            {
                var main = confettiSystem.main;
                main.startColor = config.primaryColor;
                confettiSystem.Play();
            }
            
            yield return new WaitForSeconds(0.3f);
            
            // 金币粒子
            if (goldCoinsSystem != null)
            {
                goldCoinsSystem.Play();
            }
            
            yield return new WaitForSeconds(0.2f);
            
            // 星星粒子
            if (starsSystem != null)
            {
                var main = starsSystem.main;
                main.startColor = config.secondaryColor;
                starsSystem.Play();
            }
            
            // 根据获奖类型播放特殊粒子
            switch (config.winType)
            {
                case WinType.MegaWin:
                case WinType.Jackpot:
                    if (rainbowSystem != null)
                    {
                        yield return new WaitForSeconds(0.5f);
                        rainbowSystem.Play();
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 动画化获奖文本
        /// </summary>
        private System.Collections.IEnumerator AnimateWinTexts(WinEffectConfig config, float winAmount)
        {
            if (winTexts == null) yield break;
            
            foreach (var text in winTexts)
            {
                if (text != null)
                {
                    // 设置文本颜色
                    text.color = config.primaryColor;
                    
                    // 设置文本内容
                    if (winAmount > 0)
                    {
                        text.text = $"+{winAmount:F2}";
                    }
                    else
                    {
                        text.text = GetWinTypeText(config.winType);
                    }
                    
                    // 缩放动画
                    text.transform.localScale = Vector3.zero;
                    text.transform.DOScale(Vector3.one * 1.2f, 0.3f)
                        .SetEase(Ease.OutBack);
                    
                    // 颜色渐变
                    text.DOColor(config.secondaryColor, 1f)
                        .SetDelay(0.5f)
                        .SetEase(Ease.InOutSine);
                        
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        
        /// <summary>
        /// 动画化颜色覆盖层
        /// </summary>
        private System.Collections.IEnumerator AnimateColorOverlays(WinEffectConfig config)
        {
            if (colorOverlays == null) yield break;
            
            for (int i = 0; i < colorOverlays.Length; i++)
            {
                var overlay = colorOverlays[i];
                if (overlay != null)
                {
                    Color targetColor = i % 2 == 0 ? config.primaryColor : config.secondaryColor;
                    targetColor.a = 0.3f;
                    
                    overlay.color = Color.clear;
                    overlay.DOColor(targetColor, 0.5f)
                        .SetEase(Ease.OutQuad)
                        .SetDelay(i * 0.1f);
                }
            }
            
            yield return null;
        }
        
        /// <summary>
        /// 淡出所有效果
        /// </summary>
        private System.Collections.IEnumerator FadeOutEffects()
        {
            // 淡出光源
            if (spotLights != null)
            {
                foreach (var light in spotLights)
                {
                    if (light != null)
                    {
                        light.DOIntensity(0f, 1f);
                    }
                }
            }
            
            if (pointLights != null)
            {
                foreach (var light in pointLights)
                {
                    if (light != null)
                    {
                        light.DOIntensity(0f, 1f);
                    }
                }
            }
            
            // 淡出颜色覆盖层
            if (colorOverlays != null)
            {
                foreach (var overlay in colorOverlays)
                {
                    if (overlay != null)
                    {
                        overlay.DOFade(0f, 1f);
                    }
                }
            }
            
            // 淡出文本
            if (winTexts != null)
            {
                foreach (var text in winTexts)
                {
                    if (text != null)
                    {
                        text.DOFade(0f, 1f);
                        text.transform.DOScale(Vector3.zero, 1f).SetEase(Ease.InBack);
                    }
                }
            }
            
            // 停止粒子系统
            StopAllParticles();
            
            yield return new WaitForSeconds(1f);
            
            // 重置所有视觉效果
            ResetAllVisuals();
        }
        
        #endregion
        
        #region Object Pool Management
        
        /// <summary>
        /// 创建对象池中的烟花
        /// </summary>
        private void CreatePooledFirework(GameObject prefab)
        {
            GameObject firework = Instantiate(prefab, effectContainer);
            firework.SetActive(false);
            fireworkPool.Enqueue(firework);
        }
        
        /// <summary>
        /// 从对象池获取烟花
        /// </summary>
        private GameObject GetFireworkFromPool()
        {
            if (fireworkPrefabs == null || fireworkPrefabs.Length == 0) return null;
            
            GameObject firework;
            if (fireworkPool.Count > 0)
            {
                firework = fireworkPool.Dequeue();
            }
            else
            {
                int randomIndex = Random.Range(0, fireworkPrefabs.Length);
                firework = Instantiate(fireworkPrefabs[randomIndex], effectContainer);
            }
            
            firework.SetActive(true);
            activeEffects.Add(firework);
            return firework;
        }
        
        /// <summary>
        /// 创建对象池中的爆炸
        /// </summary>
        private void CreatePooledExplosion(GameObject prefab)
        {
            GameObject explosion = Instantiate(prefab, effectContainer);
            explosion.SetActive(false);
            explosionPool.Enqueue(explosion);
        }
        
        /// <summary>
        /// 从对象池获取爆炸
        /// </summary>
        private GameObject GetExplosionFromPool()
        {
            if (explosionPrefabs == null || explosionPrefabs.Length == 0) return null;
            
            GameObject explosion;
            if (explosionPool.Count > 0)
            {
                explosion = explosionPool.Dequeue();
            }
            else
            {
                int randomIndex = Random.Range(0, explosionPrefabs.Length);
                explosion = Instantiate(explosionPrefabs[randomIndex], effectContainer);
            }
            
            explosion.SetActive(true);
            activeEffects.Add(explosion);
            return explosion;
        }
        
        /// <summary>
        /// 返回烟花到对象池
        /// </summary>
        private void ReturnFireworkToPool(GameObject firework)
        {
            if (firework != null)
            {
                firework.SetActive(false);
                firework.transform.DOKill();
                fireworkPool.Enqueue(firework);
                activeEffects.Remove(firework);
            }
        }
        
        /// <summary>
        /// 返回爆炸到对象池
        /// </summary>
        private void ReturnExplosionToPool(GameObject explosion)
        {
            if (explosion != null)
            {
                explosion.SetActive(false);
                explosion.transform.DOKill();
                explosionPool.Enqueue(explosion);
                activeEffects.Remove(explosion);
            }
        }
        
        #endregion
        
        #region Animation Helpers
        
        /// <summary>
        /// 动画化烟花
        /// </summary>
        private System.Collections.IEnumerator AnimateFirework(GameObject firework)
        {
            // 初始设置
            firework.transform.localScale = Vector3.zero;
            
            // 出现动画
            firework.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            
            // 旋转动画
            firework.transform.DORotate(new Vector3(0, 0, 360), 2f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental);
            
            // 等待显示时间
            yield return new WaitForSeconds(2f);
            
            // 消失动画
            firework.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
            
            yield return new WaitForSeconds(0.3f);
            
            // 返回对象池
            ReturnFireworkToPool(firework);
        }
        
        /// <summary>
        /// 动画化爆炸
        /// </summary>
        private System.Collections.IEnumerator AnimateExplosion(GameObject explosion)
        {
            // 初始设置
            explosion.transform.localScale = Vector3.zero;
            
            // 爆炸动画
            explosion.transform.DOScale(Vector3.one * 2f, 0.5f).SetEase(Ease.OutQuad);
            
            // 等待显示时间
            yield return new WaitForSeconds(1f);
            
            // 消失动画
            explosion.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InQuad);
            
            yield return new WaitForSeconds(0.5f);
            
            // 返回对象池
            ReturnExplosionToPool(explosion);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// 获取获奖配置
        /// </summary>
        private WinEffectConfig GetWinConfig(WinType winType)
        {
            if (winConfigs != null)
            {
                foreach (var config in winConfigs)
                {
                    if (config.winType == winType)
                        return config;
                }
            }
            
            // 返回默认配置
            return new WinEffectConfig
            {
                winType = winType,
                primaryColor = Color.yellow,
                secondaryColor = Color.white,
                effectDuration = 2f,
                fireworkCount = 3,
                explosionCount = 1
            };
        }
        
        /// <summary>
        /// 获取随机屏幕位置
        /// </summary>
        private Vector3 GetRandomScreenPosition()
        {
            if (effectCamera == null) return Vector3.zero;
            
            float x = Random.Range(0.2f, 0.8f);
            float y = Random.Range(0.3f, 0.7f);
            Vector3 screenPos = new Vector3(x * Screen.width, y * Screen.height, 10f);
            return effectCamera.ScreenToWorldPoint(screenPos);
        }
        
        /// <summary>
        /// 设置烟花颜色
        /// </summary>
        private void SetFireworkColor(GameObject firework, Color color)
        {
            var renderers = firework.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.color = color;
                }
            }
            
            var particles = firework.GetComponentsInChildren<ParticleSystem>();
            foreach (var particle in particles)
            {
                var main = particle.main;
                main.startColor = color;
            }
        }
        
        /// <summary>
        /// 播放获奖音效
        /// </summary>
        private void PlayWinAudio(WinType winType)
        {
            if (audioSource == null) return;
            
            AudioClip clipToPlay = null;
            
            switch (winType)
            {
                case WinType.SmallWin:
                    clipToPlay = smallWinEffect;
                    break;
                case WinType.BigWin:
                    clipToPlay = bigWinEffect;
                    break;
                case WinType.MegaWin:
                    clipToPlay = megaWinEffect;
                    break;
                case WinType.Jackpot:
                    clipToPlay = jackpotEffect;
                    break;
                default:
                    clipToPlay = smallWinEffect;
                    break;
            }
            
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
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
        /// 获取获奖类型文本
        /// </summary>
        private string GetWinTypeText(WinType winType)
        {
            switch (winType)
            {
                case WinType.SmallWin:
                    return "小奖!";
                case WinType.BigWin:
                    return "大奖!";
                case WinType.MegaWin:
                    return "超级大奖!";
                case WinType.Jackpot:
                    return "头奖!";
                case WinType.BonusWin:
                    return "奖励奖金!";
                case WinType.FreeSpinWin:
                    return "免费旋转!";
                default:
                    return "获奖!";
            }
        }
        
        /// <summary>
        /// 清除活跃特效
        /// </summary>
        private void ClearActiveEffects()
        {
            foreach (var effect in activeEffects)
            {
                if (effect != null)
                {
                    effect.transform.DOKill();
                    if (fireworkPool.Contains(effect))
                        ReturnFireworkToPool(effect);
                    else if (explosionPool.Contains(effect))
                        ReturnExplosionToPool(effect);
                    else
                        Destroy(effect);
                }
            }
            activeEffects.Clear();
        }
        
        /// <summary>
        /// 重置所有光源
        /// </summary>
        private void ResetLights()
        {
            if (spotLights != null)
            {
                foreach (var light in spotLights)
                {
                    if (light != null)
                    {
                        light.intensity = 0f;
                    }
                }
            }
            
            if (pointLights != null)
            {
                foreach (var light in pointLights)
                {
                    if (light != null)
                    {
                        light.intensity = 0f;
                    }
                }
            }
        }
        
        /// <summary>
        /// 停止所有粒子系统
        /// </summary>
        private void StopAllParticles()
        {
            var allParticles = new ParticleSystem[] 
            { 
                confettiSystem, goldCoinsSystem, starsSystem, 
                rainbowSystem, smokeSystem 
            };
            
            foreach (var particle in allParticles)
            {
                if (particle != null && particle.isPlaying)
                {
                    particle.Stop();
                }
            }
        }
        
        /// <summary>
        /// 重置所有视觉效果
        /// </summary>
        private void ResetAllVisuals()
        {
            // 重置屏幕闪烁
            if (screenFlash != null)
            {
                Color flashColor = screenFlash.color;
                flashColor.a = 0f;
                screenFlash.color = flashColor;
            }
            
            // 重置颜色覆盖层
            if (colorOverlays != null)
            {
                foreach (var overlay in colorOverlays)
                {
                    if (overlay != null)
                    {
                        Color overlayColor = overlay.color;
                        overlayColor.a = 0f;
                        overlay.color = overlayColor;
                    }
                }
            }
            
            // 重置文本
            if (winTexts != null)
            {
                foreach (var text in winTexts)
                {
                    if (text != null)
                    {
                        text.color = Color.white;
                        text.transform.localScale = Vector3.one;
                        text.text = "";
                    }
                }
            }
            
            // 重置光源
            ResetLights();
        }
        
        #endregion
        
        #region Editor Support
        
#if UNITY_EDITOR
        /// <summary>
        /// 测试小奖特效
        /// </summary>
        [ContextMenu("Test Small Win")]
        private void TestSmallWin()
        {
            if (Application.isPlaying)
            {
                PlayWinEffect(WinType.SmallWin, 50f);
            }
        }
        
        /// <summary>
        /// 测试大奖特效
        /// </summary>
        [ContextMenu("Test Big Win")]
        private void TestBigWin()
        {
            if (Application.isPlaying)
            {
                PlayWinEffect(WinType.BigWin, 250f);
            }
        }
        
        /// <summary>
        /// 测试超级大奖特效
        /// </summary>
        [ContextMenu("Test Mega Win")]
        private void TestMegaWin()
        {
            if (Application.isPlaying)
            {
                PlayWinEffect(WinType.MegaWin, 500f);
            }
        }
        
        /// <summary>
        /// 测试头奖特效
        /// </summary>
        [ContextMenu("Test Jackpot")]
        private void TestJackpot()
        {
            if (Application.isPlaying)
            {
                PlayWinEffect(WinType.Jackpot, 1000f);
            }
        }
        
        /// <summary>
        /// 停止所有特效
        /// </summary>
        [ContextMenu("Stop All Effects")]
        private void TestStopEffects()
        {
            if (Application.isPlaying)
            {
                StopAllEffects();
            }
        }
#endif
        
        #endregion
    }
    
    // 后处理体积组件的简化版本（如果没有Post Processing包）
    public class PostProcessVolume : MonoBehaviour
    {
        // 这是一个占位符类，如果项目中没有Post Processing包可以使用这个
        // 实际项目中应该使用Unity的Post Processing Stack
    }
}