// Assets/UI/Framework/ReactiveButton.cs
// 响应式按钮组件 - 绑定ReactiveData<bool>控制可点击状态
// 类似Vue的v-bind:disabled，当数据变化时自动更新按钮状态

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Core.Architecture;
using System;

namespace UI.Framework
{
    /// <summary>
    /// 响应式按钮组件
    /// 可以绑定多个ReactiveData来控制按钮的各种状态
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ReactiveButton : MonoBehaviour, IReactiveDataObserver<bool>
    {
        [Header("组件引用")]
        [Tooltip("按钮组件")]
        [SerializeField] private Button _button;
        
        [Tooltip("按钮文本组件")]
        [SerializeField] private Text _buttonText;
        
        [Tooltip("按钮图片组件")]
        [SerializeField] private Image _buttonImage;

        [Header("响应式绑定")]
        [Tooltip("绑定可交互状态的响应式数据")]
        [SerializeField] private string _interactableDataName = "";
        
        [Tooltip("绑定可见性状态的响应式数据")]
        [SerializeField] private string _visibilityDataName = "";
        
        [Tooltip("绑定激活状态的响应式数据")]
        [SerializeField] private string _activeDataName = "";

        [Header("状态配置")]
        [Tooltip("禁用时的透明度")]
        [SerializeField] private float _disabledAlpha = 0.5f;
        
        [Tooltip("启用时的透明度")]
        [SerializeField] private float _enabledAlpha = 1.0f;
        
        [Tooltip("状态变化动画时间")]
        [SerializeField] private float _transitionDuration = 0.2f;

        [Header("颜色配置")]
        [Tooltip("正常状态颜色")]
        [SerializeField] private Color _normalColor = Color.white;
        
        [Tooltip("禁用状态颜色")]
        [SerializeField] private Color _disabledColor = Color.gray;
        
        [Tooltip("高亮状态颜色")]
        [SerializeField] private Color _highlightColor = Color.yellow;

        [Header("调试信息")]
        [Tooltip("是否显示调试日志")]
        [SerializeField] private bool _enableDebugLog = false;
        
        [SerializeField] private bool _isInteractable = true;
        [SerializeField] private bool _isVisible = true;
        [SerializeField] private bool _isActive = true;

        // 响应式数据引用
        private ReactiveData<bool> _interactableData;
        private ReactiveData<bool> _visibilityData;
        private ReactiveData<bool> _activeData;

        // 绑定取消器
        private Action _unbindInteractable;
        private Action _unbindVisibility;
        private Action _unbindActive;

        // 原始颜色
        private Color _originalColor;
        private Color _originalTextColor;

        // 动画协程
        private Coroutine _colorTransitionCoroutine;
        private Coroutine _alphaTransitionCoroutine;

        /// <summary>
        /// 按钮点击事件
        /// </summary>
        public UnityEvent OnClick => _button?.onClick;

        /// <summary>
        /// 当前是否可交互
        /// </summary>
        public bool IsInteractable 
        { 
            get => _isInteractable; 
            private set
            {
                if (_isInteractable != value)
                {
                    _isInteractable = value;
                    UpdateButtonState();
                    
                    if (_enableDebugLog)
                        Debug.Log($"[ReactiveButton] {gameObject.name} 可交互状态: {value}");
                }
            }
        }

        /// <summary>
        /// 当前是否可见
        /// </summary>
        public bool IsVisible 
        { 
            get => _isVisible; 
            private set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    UpdateVisibility();
                    
                    if (_enableDebugLog)
                        Debug.Log($"[ReactiveButton] {gameObject.name} 可见性: {value}");
                }
            }
        }

        /// <summary>
        /// 当前是否激活
        /// </summary>
        public bool IsActive 
        { 
            get => _isActive; 
            private set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    UpdateActiveState();
                    
                    if (_enableDebugLog)
                        Debug.Log($"[ReactiveButton] {gameObject.name} 激活状态: {value}");
                }
            }
        }

        #region Unity生命周期

        private void Awake()
        {
            InitializeComponents();
            StoreOriginalColors();
        }

        private void Start()
        {
            RegisterToUIManager();
            InitializeBindings();
        }

        private void OnDestroy()
        {
            UnbindAllData();
            UnregisterFromUIManager();
        }

        private void OnValidate()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            
            if (_buttonText == null)
                _buttonText = GetComponentInChildren<Text>();
            
            if (_buttonImage == null)
                _buttonImage = GetComponent<Image>();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            
            if (_buttonText == null)
                _buttonText = GetComponentInChildren<Text>();
            
            if (_buttonImage == null)
                _buttonImage = GetComponent<Image>();

            if (_button == null)
            {
                Debug.LogError($"[ReactiveButton] {gameObject.name} 未找到Button组件！");
            }
        }

        /// <summary>
        /// 存储原始颜色
        /// </summary>
        private void StoreOriginalColors()
        {
            if (_buttonImage != null)
                _originalColor = _buttonImage.color;
            
            if (_buttonText != null)
                _originalTextColor = _buttonText.color;
        }

        /// <summary>
        /// 注册到UI管理器
        /// </summary>
        private void RegisterToUIManager()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager != null)
            {
                uiManager.RegisterReactiveComponent(this);
            }
        }

        /// <summary>
        /// 从UI管理器注销
        /// </summary>
        private void UnregisterFromUIManager()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager != null)
            {
                uiManager.UnregisterReactiveComponent(this);
            }
        }

        #endregion

        #region 数据绑定

        /// <summary>
        /// 初始化数据绑定
        /// </summary>
        private void InitializeBindings()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager == null) return;

            // 绑定可交互状态
            if (!string.IsNullOrEmpty(_interactableDataName))
            {
                BindInteractableData(uiManager.GetReactiveData<bool>(_interactableDataName));
            }

            // 绑定可见性状态
            if (!string.IsNullOrEmpty(_visibilityDataName))
            {
                BindVisibilityData(uiManager.GetReactiveData<bool>(_visibilityDataName));
            }

            // 绑定激活状态
            if (!string.IsNullOrEmpty(_activeDataName))
            {
                BindActiveData(uiManager.GetReactiveData<bool>(_activeDataName));
            }
        }

        /// <summary>
        /// 绑定可交互状态数据
        /// </summary>
        /// <param name="data">响应式数据</param>
        public void BindInteractableData(ReactiveData<bool> data)
        {
            // 先解绑旧数据
            _unbindInteractable?.Invoke();
            
            _interactableData = data;
            if (data != null)
            {
                data.AddObserver(this);
                _unbindInteractable = () => data.RemoveObserver(this);
                
                // 立即同步当前值
                if (data.HasValue)
                {
                    IsInteractable = data.Value;
                }
            }
        }

        /// <summary>
        /// 绑定可见性状态数据
        /// </summary>
        /// <param name="data">响应式数据</param>
        public void BindVisibilityData(ReactiveData<bool> data)
        {
            // 先解绑旧数据
            _unbindVisibility?.Invoke();
            
            _visibilityData = data;
            if (data != null)
            {
                data.OnValueChanged += OnVisibilityChanged;
                _unbindVisibility = () => data.OnValueChanged -= OnVisibilityChanged;
                
                // 立即同步当前值
                if (data.HasValue)
                {
                    IsVisible = data.Value;
                }
            }
        }

        /// <summary>
        /// 绑定激活状态数据
        /// </summary>
        /// <param name="data">响应式数据</param>
        public void BindActiveData(ReactiveData<bool> data)
        {
            // 先解绑旧数据
            _unbindActive?.Invoke();
            
            _activeData = data;
            if (data != null)
            {
                data.OnValueChanged += OnActiveChanged;
                _unbindActive = () => data.OnValueChanged -= OnActiveChanged;
                
                // 立即同步当前值
                if (data.HasValue)
                {
                    IsActive = data.Value;
                }
            }
        }

        /// <summary>
        /// 解绑所有数据
        /// </summary>
        private void UnbindAllData()
        {
            _unbindInteractable?.Invoke();
            _unbindVisibility?.Invoke();
            _unbindActive?.Invoke();
        }

        #endregion

        #region 数据变化回调

        /// <summary>
        /// 响应式数据观察者接口实现（用于可交互状态）
        /// </summary>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        public void OnValueChanged(bool oldValue, bool newValue)
        {
            IsInteractable = newValue;
        }

        /// <summary>
        /// 可见性变化回调
        /// </summary>
        /// <param name="visible">是否可见</param>
        private void OnVisibilityChanged(bool visible)
        {
            IsVisible = visible;
        }

        /// <summary>
        /// 激活状态变化回调
        /// </summary>
        /// <param name="active">是否激活</param>
        private void OnActiveChanged(bool active)
        {
            IsActive = active;
        }

        #endregion

        #region 状态更新

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        private void UpdateButtonState()
        {
            if (_button == null) return;

            _button.interactable = _isInteractable;
            
            // 更新颜色
            UpdateButtonColor();
            
            // 更新透明度
            UpdateButtonAlpha();
        }

        /// <summary>
        /// 更新可见性
        /// </summary>
        private void UpdateVisibility()
        {
            gameObject.SetActive(_isVisible);
        }

        /// <summary>
        /// 更新激活状态
        /// </summary>
        private void UpdateActiveState()
        {
            gameObject.SetActive(_isActive);
        }

        /// <summary>
        /// 更新按钮颜色
        /// </summary>
        private void UpdateButtonColor()
        {
            if (_buttonImage == null) return;

            Color targetColor = _isInteractable ? _normalColor : _disabledColor;
            
            if (_colorTransitionCoroutine != null)
            {
                StopCoroutine(_colorTransitionCoroutine);
            }
            
            _colorTransitionCoroutine = StartCoroutine(TransitionToColor(targetColor));
        }

        /// <summary>
        /// 更新按钮透明度
        /// </summary>
        private void UpdateButtonAlpha()
        {
            float targetAlpha = _isInteractable ? _enabledAlpha : _disabledAlpha;
            
            if (_alphaTransitionCoroutine != null)
            {
                StopCoroutine(_alphaTransitionCoroutine);
            }
            
            _alphaTransitionCoroutine = StartCoroutine(TransitionToAlpha(targetAlpha));
        }

        #endregion

        #region 动画效果

        /// <summary>
        /// 颜色过渡协程
        /// </summary>
        /// <param name="targetColor">目标颜色</param>
        /// <returns></returns>
        private System.Collections.IEnumerator TransitionToColor(Color targetColor)
        {
            if (_buttonImage == null) yield break;

            Color startColor = _buttonImage.color;
            float elapsed = 0f;

            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _transitionDuration;
                
                _buttonImage.color = Color.Lerp(startColor, targetColor, t);
                
                yield return null;
            }

            _buttonImage.color = targetColor;
        }

        /// <summary>
        /// 透明度过渡协程
        /// </summary>
        /// <param name="targetAlpha">目标透明度</param>
        /// <returns></returns>
        private System.Collections.IEnumerator TransitionToAlpha(float targetAlpha)
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _transitionDuration;
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置高亮状态
        /// </summary>
        /// <param name="highlight">是否高亮</param>
        public void SetHighlight(bool highlight)
        {
            if (_buttonImage == null) return;

            Color targetColor = highlight ? _highlightColor : 
                              (_isInteractable ? _normalColor : _disabledColor);
            
            if (_colorTransitionCoroutine != null)
            {
                StopCoroutine(_colorTransitionCoroutine);
            }
            
            _colorTransitionCoroutine = StartCoroutine(TransitionToColor(targetColor));
        }

        /// <summary>
        /// 手动更新所有状态
        /// </summary>
        public void ForceUpdateStates()
        {
            UpdateButtonState();
            UpdateVisibility();
            UpdateActiveState();
        }

        /// <summary>
        /// 重置到原始状态
        /// </summary>
        public void ResetToOriginal()
        {
            if (_buttonImage != null)
                _buttonImage.color = _originalColor;
            
            if (_buttonText != null)
                _buttonText.color = _originalTextColor;
            
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        #endregion

        #region 编辑器支持

#if UNITY_EDITOR
        [ContextMenu("测试禁用状态")]
        private void TestDisabledState()
        {
            IsInteractable = false;
        }

        [ContextMenu("测试启用状态")]
        private void TestEnabledState()
        {
            IsInteractable = true;
        }

        [ContextMenu("测试高亮效果")]
        private void TestHighlight()
        {
            SetHighlight(!_buttonImage.color.Equals(_highlightColor));
        }
#endif

        #endregion
    }
}