// Assets/UI/Framework/ReactiveImage.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Core.Architecture;

namespace UI.Framework
{
    /// <summary>
    /// 响应式图像组件
    /// 自动绑定ReactiveData并在数据变化时更新图像显示
    /// 支持Sprite切换、颜色变化、动画过渡、资源加载等功能
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ReactiveImage : MonoBehaviour, IReactiveComponent
    {
        [Header("绑定配置")]
        [SerializeField] private ReactiveImageBinding imageBinding = new ReactiveImageBinding();
        [SerializeField] private bool autoFindImageComponent = true;
        [SerializeField] private bool updateOnStart = true;

        [Header("资源配置")]
        [SerializeField] private ReactiveImageResourceMode resourceMode = ReactiveImageResourceMode.Direct;
        [SerializeField] private string resourceBasePath = "Images/";
        [SerializeField] private string resourceExtension = "";
        [SerializeField] private bool useAsyncLoading = true;

        [Header("过渡动画")]
        [SerializeField] private bool enableTransition = true;
        [SerializeField] private ReactiveImageTransition transitionType = ReactiveImageTransition.Fade;
        [SerializeField] private float transitionDuration = 0.3f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("样式配置")]
        [SerializeField] private bool enableColorBinding = false;
        [SerializeField] private bool enableAlphaBinding = false;
        [SerializeField] private bool enableFillAmountBinding = false;
        [SerializeField] private ReactiveImageStyle styleConfig = new ReactiveImageStyle();

        [Header("缓存配置")]
        [SerializeField] private bool enableSpriteCache = true;
        [SerializeField] private int maxCacheSize = 50;
        [SerializeField] private bool preloadSprites = false;
        [SerializeField] private List<string> preloadList = new List<string>();

        [Header("调试信息")]
        [SerializeField] private bool enableDebugLog = false;
        [SerializeField] private string componentName = "";
        [SerializeField] private int updateCount = 0;
        [SerializeField] private float lastUpdateTime = 0f;

        #region 私有字段

        // UI组件引用
        private Image _imageComponent;
        
        // 响应式数据绑定
        private readonly List<System.Action> _dataUnbinders = new List<System.Action>();
        private readonly Dictionary<string, object> _lastValues = new Dictionary<string, object>();

        // 过渡动画控制
        private Coroutine _transitionCoroutine;
        private Sprite _targetSprite;
        private Color _targetColor;
        private float _targetAlpha;
        private float _targetFillAmount;

        // 资源管理
        private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private readonly Queue<string> _cacheOrder = new Queue<string>();
        private readonly Dictionary<string, Coroutine> _loadingCoroutines = new Dictionary<string, Coroutine>();

        // 状态管理
        private bool _isInitialized = false;
        private bool _isTransitioning = false;
        private Sprite _currentSprite;
        private Color _currentColor;

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前显示的Sprite
        /// </summary>
        public Sprite CurrentSprite
        {
            get => _currentSprite;
            private set
            {
                _currentSprite = value;
                if (_imageComponent != null && !_isTransitioning)
                {
                    _imageComponent.sprite = value;
                }
            }
        }

        /// <summary>
        /// 当前颜色
        /// </summary>
        public Color CurrentColor
        {
            get => _currentColor;
            private set
            {
                _currentColor = value;
                if (_imageComponent != null && !_isTransitioning)
                {
                    _imageComponent.color = value;
                }
            }
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 是否正在过渡
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// 更新次数
        /// </summary>
        public int UpdateCount => updateCount;

        /// <summary>
        /// 图像绑定配置
        /// </summary>
        public ReactiveImageBinding ImageBinding => imageBinding;

        /// <summary>
        /// 缓存的Sprite数量
        /// </summary>
        public int CachedSpriteCount => _spriteCache.Count;

        #endregion

        #region 事件

        /// <summary>
        /// 图像更新前事件
        /// </summary>
        public event System.Action<Sprite, Sprite> OnImageChanging;

        /// <summary>
        /// 图像更新后事件
        /// </summary>
        public event System.Action<Sprite> OnImageChanged;

        /// <summary>
        /// 过渡开始事件
        /// </summary>
        public event System.Action<ReactiveImageTransition> OnTransitionStarted;

        /// <summary>
        /// 过渡完成事件
        /// </summary>
        public event System.Action<ReactiveImageTransition> OnTransitionCompleted;

        /// <summary>
        /// 资源加载完成事件
        /// </summary>
        public event System.Action<string, Sprite> OnResourceLoaded;

        /// <summary>
        /// 绑定建立事件
        /// </summary>
        public event System.Action OnBindingEstablished;

        /// <summary>
        /// 绑定断开事件
        /// </summary>
        public event System.Action OnBindingLost;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            InitializeComponents();
            InitializeSpriteCache();
        }

        private void Start()
        {
            InitializeBindings();
            
            if (preloadSprites)
            {
                StartCoroutine(PreloadSpritesCoroutine());
            }
            
            if (updateOnStart)
            {
                RefreshImage();
            }
        }

        private void OnEnable()
        {
            EstablishBindings();
        }

        private void OnDisable()
        {
            ClearBindings();
        }

        private void OnDestroy()
        {
            CleanupResources();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化UI组件
        /// </summary>
        private void InitializeComponents()
        {
            if (autoFindImageComponent)
            {
                _imageComponent = GetComponent<Image>();
            }

            // 设置组件名称
            if (string.IsNullOrEmpty(componentName))
            {
                componentName = gameObject.name + "_ReactiveImage";
            }

            // 记录初始状态
            if (_imageComponent != null)
            {
                _currentSprite = _imageComponent.sprite;
                _currentColor = _imageComponent.color;
            }

            _isInitialized = true;

            if (enableDebugLog)
            {
                Debug.Log($"ReactiveImage '{componentName}' 初始化完成");
            }
        }

        /// <summary>
        /// 初始化Sprite缓存
        /// </summary>
        private void InitializeSpriteCache()
        {
            if (!enableSpriteCache) return;

            _spriteCache.Clear();
            _cacheOrder.Clear();
        }

        /// <summary>
        /// 初始化数据绑定
        /// </summary>
        private void InitializeBindings()
        {
            if (!_isInitialized) return;

            // 验证绑定配置
            if (!ValidateBindingConfiguration())
            {
                Debug.LogWarning($"ReactiveImage '{componentName}' 绑定配置无效");
                return;
            }

            // 建立绑定
            EstablishBindings();
        }

        #endregion

        #region 绑定管理方法

        /// <summary>
        /// 建立数据绑定
        /// </summary>
        private void EstablishBindings()
        {
            // 清除现有绑定
            ClearBindings();

            try
            {
                // 绑定Sprite数据
                if (imageBinding.spriteData != null)
                {
                    var unbinder = BindToReactiveData("sprite", imageBinding.spriteData, RefreshImage);
                    if (unbinder != null) _dataUnbinders.Add(unbinder);
                }

                // 绑定Sprite路径数据
                if (imageBinding.spritePathData != null)
                {
                    var unbinder = BindToReactiveData("spritePath", imageBinding.spritePathData, RefreshImage);
                    if (unbinder != null) _dataUnbinders.Add(unbinder);
                }

                // 绑定样式数据
                BindStyleData();

                OnBindingEstablished?.Invoke();

                if (enableDebugLog)
                {
                    Debug.Log($"ReactiveImage '{componentName}' 建立了 {_dataUnbinders.Count} 个数据绑定");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ReactiveImage '{componentName}' 建立绑定时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 绑定到响应式数据
        /// </summary>
        /// <param name="key">绑定键</param>
        /// <param name="reactiveData">响应式数据</param>
        /// <param name="callback">回调函数</param>
        /// <returns>取消绑定函数</returns>
        private System.Action BindToReactiveData(string key, object reactiveData, System.Action callback)
        {
            if (reactiveData == null) return null;

            try
            {
                // 使用反射处理不同类型的ReactiveData
                var type = reactiveData.GetType();
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReactiveData<>))
                {
                    var onValueChangedEvent = type.GetEvent("OnValueChanged");
                    if (onValueChangedEvent != null)
                    {
                        var handler = CreateGenericHandler(key, callback);
                        onValueChangedEvent.AddEventHandler(reactiveData, handler);

                        // 立即获取当前值
                        var valueProperty = type.GetProperty("Value");
                        if (valueProperty != null)
                        {
                            var currentValue = valueProperty.GetValue(reactiveData);
                            _lastValues[key] = currentValue;
                        }

                        // 返回取消绑定函数
                        return () => onValueChangedEvent.RemoveEventHandler(reactiveData, handler);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"绑定响应式数据失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 创建泛型事件处理器
        /// </summary>
        /// <param name="key">绑定键</param>
        /// <param name="callback">回调函数</param>
        /// <returns>事件处理器</returns>
        private System.Delegate CreateGenericHandler(string key, System.Action callback)
        {
            System.Action<object> handler = (value) =>
            {
                try
                {
                    _lastValues[key] = value;
                    callback?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"处理响应式数据变化时发生错误: {ex.Message}");
                }
            };

            return handler;
        }

        /// <summary>
        /// 绑定样式数据
        /// </summary>
        private void BindStyleData()
        {
            if (enableColorBinding && styleConfig.colorData != null)
            {
                var unbinder = BindToReactiveData("color", styleConfig.colorData, ApplyStyleChanges);
                if (unbinder != null) _dataUnbinders.Add(unbinder);
            }

            if (enableAlphaBinding && styleConfig.alphaData != null)
            {
                var unbinder = BindToReactiveData("alpha", styleConfig.alphaData, ApplyStyleChanges);
                if (unbinder != null) _dataUnbinders.Add(unbinder);
            }

            if (enableFillAmountBinding && styleConfig.fillAmountData != null)
            {
                var unbinder = BindToReactiveData("fillAmount", styleConfig.fillAmountData, ApplyStyleChanges);
                if (unbinder != null) _dataUnbinders.Add(unbinder);
            }
        }

        /// <summary>
        /// 清除所有绑定
        /// </summary>
        private void ClearBindings()
        {
            foreach (var unbinder in _dataUnbinders)
            {
                try
                {
                    unbinder?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"清除绑定时发生错误: {ex.Message}");
                }
            }

            _dataUnbinders.Clear();
            _lastValues.Clear();

            OnBindingLost?.Invoke();
        }

        #endregion

        #region 图像更新方法

        /// <summary>
        /// 刷新图像显示
        /// </summary>
        public void RefreshImage()
        {
            if (!_isInitialized) return;

            try
            {
                // 获取目标Sprite
                Sprite targetSprite = GetTargetSprite();
                
                if (targetSprite != _currentSprite)
                {
                    UpdateImage(targetSprite);
                }

                // 应用样式变化
                ApplyStyleChanges();
            }
            catch (Exception ex)
            {
                Debug.LogError($"ReactiveImage '{componentName}' 刷新图像时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取目标Sprite
        /// </summary>
        /// <returns>目标Sprite</returns>
        private Sprite GetTargetSprite()
        {
            // 优先使用直接绑定的Sprite
            if (_lastValues.ContainsKey("sprite") && _lastValues["sprite"] is Sprite directSprite)
            {
                return directSprite;
            }

            // 使用路径加载Sprite
            if (_lastValues.ContainsKey("spritePath") && _lastValues["spritePath"] is string spritePath)
            {
                return LoadSpriteFromPath(spritePath);
            }

            return _currentSprite;
        }

        /// <summary>
        /// 从路径加载Sprite
        /// </summary>
        /// <param name="path">Sprite路径</param>
        /// <returns>加载的Sprite</returns>
        private Sprite LoadSpriteFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // 检查缓存
            if (enableSpriteCache && _spriteCache.ContainsKey(path))
            {
                return _spriteCache[path];
            }

            try
            {
                Sprite loadedSprite = null;

                switch (resourceMode)
                {
                    case ReactiveImageResourceMode.Resources:
                        loadedSprite = LoadFromResources(path);
                        break;

                    case ReactiveImageResourceMode.Addressables:
                        // 异步加载（需要实现Addressables支持）
                        if (useAsyncLoading)
                        {
                            StartAsyncLoad(path);
                        }
                        break;

                    case ReactiveImageResourceMode.Direct:
                        // 直接使用传入的Sprite
                        break;

                    case ReactiveImageResourceMode.StreamingAssets:
                        // 从StreamingAssets加载（需要实现）
                        break;
                }

                // 缓存加载的Sprite
                if (loadedSprite != null && enableSpriteCache)
                {
                    CacheSprite(path, loadedSprite);
                }

                return loadedSprite;
            }
            catch (Exception ex)
            {
                Debug.LogError($"从路径加载Sprite失败: {path}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从Resources加载Sprite
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>加载的Sprite</returns>
        private Sprite LoadFromResources(string path)
        {
            string fullPath = resourceBasePath + path + resourceExtension;
            return Resources.Load<Sprite>(fullPath);
        }

        /// <summary>
        /// 开始异步加载
        /// </summary>
        /// <param name="path">资源路径</param>
        private void StartAsyncLoad(string path)
        {
            if (_loadingCoroutines.ContainsKey(path)) return;

            var coroutine = StartCoroutine(AsyncLoadCoroutine(path));
            _loadingCoroutines[path] = coroutine;
        }

        /// <summary>
        /// 异步加载协程
        /// </summary>
        /// <param name="path">资源路径</param>
        private IEnumerator AsyncLoadCoroutine(string path)
        {
            string fullPath = resourceBasePath + path + resourceExtension;
            var request = Resources.LoadAsync<Sprite>(fullPath);
            
            yield return request;

            var loadedSprite = request.asset as Sprite;
            if (loadedSprite != null)
            {
                // 缓存Sprite
                if (enableSpriteCache)
                {
                    CacheSprite(path, loadedSprite);
                }

                // 如果这是当前需要的Sprite，立即应用
                if (_lastValues.ContainsKey("spritePath") && 
                    _lastValues["spritePath"] is string currentPath && 
                    currentPath == path)
                {
                    UpdateImage(loadedSprite);
                }

                OnResourceLoaded?.Invoke(path, loadedSprite);
            }

            _loadingCoroutines.Remove(path);
        }

        /// <summary>
        /// 更新图像显示
        /// </summary>
        /// <param name="newSprite">新的Sprite</param>
        private void UpdateImage(Sprite newSprite)
        {
            var oldSprite = _currentSprite;
            OnImageChanging?.Invoke(oldSprite, newSprite);

            if (enableTransition && Application.isPlaying && gameObject.activeInHierarchy)
            {
                StartTransition(newSprite);
            }
            else
            {
                CurrentSprite = newSprite;
                OnImageChanged?.Invoke(newSprite);
            }

            // 更新统计信息
            updateCount++;
            lastUpdateTime = Time.time;

            if (enableDebugLog)
            {
                Debug.Log($"ReactiveImage '{componentName}' 图像更新: {oldSprite?.name} -> {newSprite?.name}");
            }
        }

        #endregion

        #region 过渡动画方法

        /// <summary>
        /// 开始过渡动画
        /// </summary>
        /// <param name="targetSprite">目标Sprite</param>
        private void StartTransition(Sprite targetSprite)
        {
            _targetSprite = targetSprite;

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            _transitionCoroutine = StartCoroutine(TransitionCoroutine());
        }

        /// <summary>
        /// 过渡动画协程
        /// </summary>
        private IEnumerator TransitionCoroutine()
        {
            _isTransitioning = true;
            OnTransitionStarted?.Invoke(transitionType);

            var startSprite = _currentSprite;
            var startColor = _currentColor;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / transitionDuration;
                float easedProgress = transitionCurve.Evaluate(progress);

                ApplyTransitionProgress(startSprite, _targetSprite, startColor, easedProgress);

                yield return null;
            }

            // 确保最终状态正确
            CurrentSprite = _targetSprite;
            _imageComponent.color = _currentColor;

            _isTransitioning = false;
            OnTransitionCompleted?.Invoke(transitionType);
            OnImageChanged?.Invoke(_targetSprite);

            _transitionCoroutine = null;
        }

        /// <summary>
        /// 应用过渡进度
        /// </summary>
        /// <param name="startSprite">起始Sprite</param>
        /// <param name="endSprite">结束Sprite</param>
        /// <param name="startColor">起始颜色</param>
        /// <param name="progress">进度(0-1)</param>
        private void ApplyTransitionProgress(Sprite startSprite, Sprite endSprite, Color startColor, float progress)
        {
            switch (transitionType)
            {
                case ReactiveImageTransition.Fade:
                    ApplyFadeTransition(startSprite, endSprite, startColor, progress);
                    break;

                case ReactiveImageTransition.Scale:
                    ApplyScaleTransition(endSprite, progress);
                    break;

                case ReactiveImageTransition.Slide:
                    ApplySlideTransition(endSprite, progress);
                    break;

                case ReactiveImageTransition.CrossFade:
                    ApplyCrossFadeTransition(startSprite, endSprite, startColor, progress);
                    break;

                default:
                    _imageComponent.sprite = endSprite;
                    break;
            }
        }

        /// <summary>
        /// 应用淡入淡出过渡
        /// </summary>
        private void ApplyFadeTransition(Sprite startSprite, Sprite endSprite, Color startColor, float progress)
        {
            if (progress < 0.5f)
            {
                // 淡出阶段
                float fadeProgress = progress * 2f;
                var color = startColor;
                color.a = startColor.a * (1f - fadeProgress);
                _imageComponent.color = color;
                _imageComponent.sprite = startSprite;
            }
            else
            {
                // 淡入阶段
                float fadeProgress = (progress - 0.5f) * 2f;
                var color = _currentColor;
                color.a = _currentColor.a * fadeProgress;
                _imageComponent.color = color;
                _imageComponent.sprite = endSprite;
            }
        }

        /// <summary>
        /// 应用缩放过渡
        /// </summary>
        private void ApplyScaleTransition(Sprite endSprite, float progress)
        {
            _imageComponent.sprite = endSprite;
            float scale = Mathf.LerpAngle(0f, 1f, progress);
            transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// 应用滑动过渡
        /// </summary>
        private void ApplySlideTransition(Sprite endSprite, float progress)
        {
            _imageComponent.sprite = endSprite;
            var rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                float offset = Mathf.Lerp(rectTransform.rect.width, 0f, progress);
                rectTransform.anchoredPosition = new Vector2(offset, rectTransform.anchoredPosition.y);
            }
        }

        /// <summary>
        /// 应用交叉淡化过渡
        /// </summary>
        private void ApplyCrossFadeTransition(Sprite startSprite, Sprite endSprite, Color startColor, float progress)
        {
            // 这里可以实现更复杂的交叉淡化效果
            // 可能需要使用两个Image组件或其他技术
            ApplyFadeTransition(startSprite, endSprite, startColor, progress);
        }

        #endregion

        #region 样式应用方法

        /// <summary>
        /// 应用样式变化
        /// </summary>
        private void ApplyStyleChanges()
        {
            if (_imageComponent == null) return;

            try
            {
                Color newColor = _currentColor;
                bool colorChanged = false;

                // 应用颜色变化
                if (enableColorBinding && _lastValues.ContainsKey("color"))
                {
                    if (_lastValues["color"] is Color color)
                    {
                        newColor = color;
                        colorChanged = true;
                    }
                }

                // 应用透明度变化
                if (enableAlphaBinding && _lastValues.ContainsKey("alpha"))
                {
                    if (_lastValues["alpha"] is float alpha)
                    {
                        newColor.a = alpha;
                        colorChanged = true;
                    }
                }

                // 应用颜色变化
                if (colorChanged)
                {
                    if (enableTransition && _isTransitioning)
                    {
                        _targetColor = newColor;
                    }
                    else
                    {
                        CurrentColor = newColor;
                    }
                }

                // 应用填充量变化
                if (enableFillAmountBinding && _lastValues.ContainsKey("fillAmount"))
                {
                    if (_lastValues["fillAmount"] is float fillAmount)
                    {
                        _imageComponent.fillAmount = Mathf.Clamp01(fillAmount);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"应用样式变化时发生错误: {ex.Message}");
            }
        }

        #endregion

        #region 缓存管理方法

        /// <summary>
        /// 缓存Sprite
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="sprite">Sprite</param>
        private void CacheSprite(string path, Sprite sprite)
        {
            if (!enableSpriteCache || sprite == null) return;

            // 检查缓存大小限制
            if (_spriteCache.Count >= maxCacheSize)
            {
                RemoveOldestCacheEntry();
            }

            _spriteCache[path] = sprite;
            _cacheOrder.Enqueue(path);

            if (enableDebugLog)
            {
                Debug.Log($"缓存Sprite: {path} (缓存大小: {_spriteCache.Count})");
            }
        }

        /// <summary>
        /// 移除最旧的缓存条目
        /// </summary>
        private void RemoveOldestCacheEntry()
        {
            if (_cacheOrder.Count > 0)
            {
                string oldestPath = _cacheOrder.Dequeue();
                _spriteCache.Remove(oldestPath);
            }
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            _spriteCache.Clear();
            _cacheOrder.Clear();

            if (enableDebugLog)
            {
                Debug.Log($"ReactiveImage '{componentName}' 缓存已清理");
            }
        }

        /// <summary>
        /// 预加载Sprites协程
        /// </summary>
        private IEnumerator PreloadSpritesCoroutine()
        {
            if (enableDebugLog)
            {
                Debug.Log($"开始预加载 {preloadList.Count} 个Sprites");
            }

            foreach (string path in preloadList)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    LoadSpriteFromPath(path);
                    yield return null; // 分帧加载
                }
            }

            if (enableDebugLog)
            {
                Debug.Log("Sprites预加载完成");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 验证绑定配置
        /// </summary>
        /// <returns>是否有效</returns>
        private bool ValidateBindingConfiguration()
        {
            // 检查是否有有效的Image组件
            if (_imageComponent == null)
            {
                Debug.LogError($"ReactiveImage '{componentName}' 未找到有效的Image组件");
                return false;
            }

            // 检查是否有绑定数据
            if (imageBinding.spriteData == null && imageBinding.spritePathData == null)
            {
                Debug.LogWarning($"ReactiveImage '{componentName}' 没有绑定任何响应式数据");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void CleanupResources()
        {
            ClearBindings();
            
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            // 停止所有加载协程
            foreach (var coroutine in _loadingCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            _loadingCoroutines.Clear();

            if (enableSpriteCache)
            {
                ClearCache();
            }
        }

        #endregion

        #region 公共接口方法

        /// <summary>
        /// 手动设置Sprite（不触发绑定）
        /// </summary>
        /// <param name="sprite">Sprite</param>
        public void SetSpriteDirect(Sprite sprite)
        {
            CurrentSprite = sprite;
        }

        /// <summary>
        /// 手动设置颜色（不触发绑定）
        /// </summary>
        /// <param name="color">颜色</param>
        public void SetColorDirect(Color color)
        {
            CurrentColor = color;
        }

        /// <summary>
        /// 重新建立绑定
        /// </summary>
        public void RebindData()
        {
            EstablishBindings();
            RefreshImage();
        }

        /// <summary>
        /// 获取当前绑定状态
        /// </summary>
        /// <returns>绑定状态信息</returns>
        public string GetBindingStatus()
        {
            return $"绑定数量: {_dataUnbinders.Count}, 更新次数: {updateCount}, " +
                   $"缓存大小: {_spriteCache.Count}, 最后更新: {lastUpdateTime:F2}s";
        }

        /// <summary>
        /// 强制停止所有过渡动画
        /// </summary>
        public void StopAllTransitions()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
                _isTransitioning = false;
            }

            // 重置Transform
            transform.localScale = Vector3.one;
            var rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// 预加载指定路径的Sprite
        /// </summary>
        /// <param name="path">Sprite路径</param>
        public void PreloadSprite(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                LoadSpriteFromPath(path);
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计</returns>
        public string GetCacheStatistics()
        {
            return $"缓存容量: {_spriteCache.Count}/{maxCacheSize}, " +
                   $"加载中: {_loadingCoroutines.Count}, " +
                   $"预加载列表: {preloadList.Count}";
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 响应式图像绑定配置
    /// </summary>
    [System.Serializable]
    public class ReactiveImageBinding
    {
        [Header("Sprite绑定")]
        [Tooltip("直接绑定Sprite的响应式数据")]
        public object spriteData;

        [Header("路径绑定")]
        [Tooltip("绑定Sprite路径的响应式数据")]
        public object spritePathData;

        [Header("纹理绑定")]
        [Tooltip("绑定Texture2D的响应式数据")]
        public object textureData;
    }

    /// <summary>
    /// 响应式图像样式配置
    /// </summary>
    [System.Serializable]
    public class ReactiveImageStyle
    {
        [Header("颜色绑定")]
        [Tooltip("颜色响应式数据")]
        public object colorData;

        [Header("透明度绑定")]
        [Tooltip("透明度响应式数据")]
        public object alphaData;

        [Header("填充量绑定")]
        [Tooltip("填充量响应式数据（用于Image.Type.Filled）")]
        public object fillAmountData;

        [Header("材质绑定")]
        [Tooltip("材质响应式数据")]
        public object materialData;
    }

    /// <summary>
    /// 响应式图像资源模式枚举
    /// </summary>
    public enum ReactiveImageResourceMode
    {
        Direct,             // 直接使用Sprite对象
        Resources,          // 从Resources文件夹加载
        Addressables,       // 使用Addressables系统
        StreamingAssets,    // 从StreamingAssets加载
        AssetBundle         // 从AssetBundle加载
    }

    /// <summary>
    /// 响应式图像过渡类型枚举
    /// </summary>
    public enum ReactiveImageTransition
    {
        None,           // 无过渡
        Fade,           // 淡入淡出
        Scale,          // 缩放
        Slide,          // 滑动
        CrossFade,      // 交叉淡化
        Flip,           // 翻转
        Dissolve,       // 溶解
        Wipe            // 擦除
    }

    /// <summary>
    /// 图像加载状态
    /// </summary>
    public enum ImageLoadingState
    {
        None,           // 无状态
        Loading,        // 加载中
        Loaded,         // 已加载
        Failed,         // 加载失败
        Cached          // 已缓存
    }

    /// <summary>
    /// 图像加载结果
    /// </summary>
    [System.Serializable]
    public class ImageLoadResult
    {
        public bool success;
        public Sprite sprite;
        public string path;
        public string errorMessage;
        public float loadTime;
        public ImageLoadingState state;
    }

    /// <summary>
    /// 响应式图像配置
    /// </summary>
    [System.Serializable]
    public class ReactiveImageConfig
    {
        [Header("性能设置")]
        [Range(1, 100)]
        public int maxCacheSize = 50;
        
        [Range(0.1f, 5f)]
        public float defaultTransitionDuration = 0.3f;
        
        public bool enableAsyncLoading = true;
        public bool enableSpriteCompression = true;

        [Header("质量设置")]
        public FilterMode filterMode = FilterMode.Bilinear;
        public int maxTextureSize = 2048;
        public bool generateMipmaps = false;

        [Header("调试设置")]
        public bool enableDebugMode = false;
        public bool logCacheOperations = false;
        public bool showLoadingIndicator = false;
    }

    /// <summary>
    /// Sprite缓存项
    /// </summary>
    [System.Serializable]
    public class SpriteCacheItem
    {
        public string path;
        public Sprite sprite;
        public DateTime cacheTime;
        public int accessCount;
        public float lastAccessTime;
        public long memorySize;
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    [System.Serializable]
    public class CacheStatistics
    {
        public int totalCachedItems;
        public int totalMemoryUsage;
        public int hitCount;
        public int missCount;
        public float hitRate;
        public DateTime lastCleanupTime;
        public List<SpriteCacheItem> mostUsedItems;
    }

    /// <summary>
    /// 图像效果配置
    /// </summary>
    [System.Serializable]
    public class ImageEffectConfig
    {
        [Header("基础效果")]
        public bool enableGrayscale = false;
        public bool enableSepia = false;
        public bool enableInvert = false;

        [Header("颜色调整")]
        [Range(-1f, 1f)] public float brightness = 0f;
        [Range(0f, 2f)] public float contrast = 1f;
        [Range(0f, 2f)] public float saturation = 1f;
        [Range(-180f, 180f)] public float hue = 0f;

        [Header("模糊效果")]
        public bool enableBlur = false;
        [Range(0f, 10f)] public float blurRadius = 0f;
        public int blurIterations = 1;

        [Header("发光效果")]
        public bool enableGlow = false;
        public Color glowColor = Color.white;
        [Range(0f, 5f)] public float glowIntensity = 1f;
        [Range(0f, 10f)] public float glowRadius = 2f;
    }

    #endregion

    #region 扩展方法

    /// <summary>
    /// ReactiveImage扩展方法
    /// </summary>
    public static class ReactiveImageExtensions
    {
        /// <summary>
        /// 快速设置Sprite绑定
        /// </summary>
        /// <param name="reactiveImage">响应式图像组件</param>
        /// <param name="spriteData">Sprite响应式数据</param>
        public static void BindSprite<T>(this ReactiveImage reactiveImage, ReactiveData<T> spriteData) where T : UnityEngine.Object
        {
            if (reactiveImage != null && spriteData != null)
            {
                reactiveImage.ImageBinding.spriteData = spriteData;
                reactiveImage.RebindData();
            }
        }

        /// <summary>
        /// 快速设置路径绑定
        /// </summary>
        /// <param name="reactiveImage">响应式图像组件</param>
        /// <param name="pathData">路径响应式数据</param>
        public static void BindPath(this ReactiveImage reactiveImage, ReactiveData<string> pathData)
        {
            if (reactiveImage != null && pathData != null)
            {
                reactiveImage.ImageBinding.spritePathData = pathData;
                reactiveImage.RebindData();
            }
        }

        /// <summary>
        /// 设置过渡动画
        /// </summary>
        /// <param name="reactiveImage">响应式图像组件</param>
        /// <param name="transitionType">过渡类型</param>
        /// <param name="duration">持续时间</param>
        public static void SetTransition(this ReactiveImage reactiveImage, ReactiveImageTransition transitionType, float duration = 0.3f)
        {
            if (reactiveImage != null)
            {
                // 通过反射设置私有字段（在实际项目中应该通过公共属性）
                var field = typeof(ReactiveImage).GetField("transitionType", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(reactiveImage, transitionType);

                var durationField = typeof(ReactiveImage).GetField("transitionDuration", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                durationField?.SetValue(reactiveImage, duration);
            }
        }

        /// <summary>
        /// 批量预加载Sprites
        /// </summary>
        /// <param name="reactiveImage">响应式图像组件</param>
        /// <param name="paths">路径列表</param>
        public static void PreloadSprites(this ReactiveImage reactiveImage, IEnumerable<string> paths)
        {
            if (reactiveImage != null && paths != null)
            {
                foreach (string path in paths)
                {
                    reactiveImage.PreloadSprite(path);
                }
            }
        }

        /// <summary>
        /// 创建Sprite到路径的映射
        /// </summary>
        /// <param name="spriteData">Sprite响应式数据</param>
        /// <param name="pathMapping">路径映射函数</param>
        /// <returns>路径响应式数据</returns>
        public static ReactiveData<string> MapToPath(this ReactiveData<Sprite> spriteData, System.Func<Sprite, string> pathMapping)
        {
            var pathData = new ReactiveData<string>();
            
            if (spriteData != null && pathMapping != null)
            {
                spriteData.OnValueChanged += sprite =>
                {
                    string path = pathMapping(sprite);
                    pathData.Value = path;
                };

                // 设置初始值
                if (spriteData.HasValue)
                {
                    pathData.Value = pathMapping(spriteData.Value);
                }
            }

            return pathData;
        }

        /// <summary>
        /// 创建条件图像绑定
        /// </summary>
        /// <param name="conditionData">条件响应式数据</param>
        /// <param name="trueSprite">条件为真时的Sprite</param>
        /// <param name="falseSprite">条件为假时的Sprite</param>
        /// <returns>Sprite响应式数据</returns>
        public static ReactiveData<Sprite> CreateConditionalBinding(ReactiveData<bool> conditionData, Sprite trueSprite, Sprite falseSprite)
        {
            var spriteData = new ReactiveData<Sprite>();
            
            if (conditionData != null)
            {
                conditionData.OnValueChanged += condition =>
                {
                    spriteData.Value = condition ? trueSprite : falseSprite;
                };

                // 设置初始值
                if (conditionData.HasValue)
                {
                    spriteData.Value = conditionData.Value ? trueSprite : falseSprite;
                }
            }

            return spriteData;
        }
    }

    #endregion