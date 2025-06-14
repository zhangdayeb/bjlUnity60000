// Assets/UI/Components/ChipButton.cs
// 筹码按钮组件 - 专用于筹码选择的交互式按钮
// 支持筹码选择、状态管理、动画效果、响应式绑定等功能

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UI.Framework;
using Core.Data.Types;
using Core.Architecture;

namespace UI.Components
{
    /// <summary>
    /// 筹码按钮组件
    /// 专门用于筹码选择界面，支持筹码数据绑定、状态管理、动画效果
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ChipButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, 
        IPointerDownHandler, IPointerUpHandler, IReactiveDataObserver<ChipData>
    {
        #region 组件配置

        [Header("筹码数据")]
        [Tooltip("筹码数据")]
        [SerializeField] private ChipData _chipData;
        
        [Tooltip("是否为当前选中的筹码")]
        [SerializeField] private bool _isSelected = false;

        [Header("组件引用")]
        [Tooltip("按钮组件")]
        [SerializeField] private Button _button;
        
        [Tooltip("筹码图片")]
        [SerializeField] private Image _chipImage;
        
        [Tooltip("筹码值文本")]
        [SerializeField] private Text _valueText;
        
        [Tooltip("选中状态边框")]
        [SerializeField] private Image _selectionBorder;
        
        [Tooltip("禁用遮罩")]
        [SerializeField] private Image _disabledOverlay;

        [Header("视觉配置")]
        [Tooltip("正常状态透明度")]
        [SerializeField] private float _normalAlpha = 1f;
        
        [Tooltip("悬停状态透明度")]
        [SerializeField] private float _hoverAlpha = 1.2f;
        
        [Tooltip("按下状态透明度")]
        [SerializeField] private float _pressedAlpha = 0.8f;
        
        [Tooltip("禁用状态透明度")]
        [SerializeField] private float _disabledAlpha = 0.3f;
        
        [Tooltip("选中状态边框颜色")]
        [SerializeField] private Color _selectedBorderColor = Color.yellow;
        
        [Tooltip("未选中状态边框颜色")]
        [SerializeField] private Color _unselectedBorderColor = Color.clear;

        [Header("动画配置")]
        [Tooltip("状态切换动画时间")]
        [SerializeField] private float _transitionDuration = 0.15f;
        
        [Tooltip("选中时的缩放倍数")]
        [SerializeField] private float _selectedScale = 1.1f;
        
        [Tooltip("悬停时的缩放倍数")]
        [SerializeField] private float _hoverScale = 1.05f;
        
        [Tooltip("按下时的缩放倍数")]
        [SerializeField] private float _pressedScale = 0.95f;
        
        [Tooltip("弹跳动画强度")]
        [SerializeField] private float _bounceStrength = 0.1f;
        
        [Tooltip("动画曲线")]
        [SerializeField] private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("交互配置")]
        [Tooltip("是否启用悬停效果")]
        [SerializeField] private bool _enableHoverEffects = true;
        
        [Tooltip("是否启用点击反馈")]
        [SerializeField] private bool _enableClickFeedback = true;
        
        [Tooltip("是否启用选中动画")]
        [SerializeField] private bool _enableSelectionAnimation = true;
        
        [Tooltip("是否启用音效")]
        [SerializeField] private bool _enableSoundEffects = true;
        
        [Tooltip("是否启用触觉反馈")]
        [SerializeField] private bool _enableHapticFeedback = true;

        [Header("用户限制")]
        [Tooltip("最低用户等级要求")]
        [SerializeField] private int _requiredUserLevel = 1;
        
        [Tooltip("是否检查用户余额")]
        [SerializeField] private bool _checkUserBalance = true;

        [Header("调试信息")]
        [Tooltip("是否启用调试模式")]
        [SerializeField] private bool _enableDebugMode = false;
        
        [SerializeField] private bool _isInteractable = true;
        [SerializeField] private bool _isHovered = false;
        [SerializeField] private bool _isPressed = false;

        #endregion

        #region 私有字段

        // 状态管理
        private bool _isAvailable = true;
        private bool _meetsRequirements = true;
        
        // 动画和效果
        private Coroutine _scaleTransitionCoroutine;
        private Coroutine _alphaTransitionCoroutine;
        private Coroutine _colorTransitionCoroutine;
        private Coroutine _selectionAnimationCoroutine;
        private Coroutine _bounceAnimationCoroutine;
        
        // 响应式数据绑定
        private ReactiveData<ChipData> _currentChipData;
        private ReactiveData<float> _userBalanceData;
        private ReactiveData<int> _userLevelData;
        private System.Action _unbindCurrentChip;
        private System.Action _unbindUserBalance;
        private System.Action _unbindUserLevel;
        
        // 原始值
        private Vector3 _originalScale;
        private Color _originalChipColor;
        private Color _originalTextColor;
        
        // 效果管理
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        // 事件处理
        private float _lastClickTime = 0f;
        private const float CLICK_DEBOUNCE_TIME = 0.2f;

        #endregion

        #region 公共属性

        /// <summary>
        /// 筹码数据
        /// </summary>
        public ChipData ChipData 
        { 
            get => _chipData; 
            set 
            { 
                _chipData = value;
                UpdateChipDisplay();
            } 
        }

        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool IsSelected 
        { 
            get => _isSelected; 
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    UpdateSelectionState();
                }
            }
        }

        /// <summary>
        /// 是否可交互
        /// </summary>
        public bool IsInteractable 
        { 
            get => _isInteractable; 
            private set
            {
                if (_isInteractable != value)
                {
                    _isInteractable = value;
                    UpdateInteractableState();
                }
            }
        }

        /// <summary>
        /// 筹码值
        /// </summary>
        public float ChipValue => _chipData?.val ?? 0f;

        /// <summary>
        /// 筹码文本
        /// </summary>
        public string ChipText => _chipData?.text ?? "";

        /// <summary>
        /// 是否可用（满足所有条件）
        /// </summary>
        public bool IsAvailable 
        { 
            get => _isAvailable; 
            private set
            {
                if (_isAvailable != value)
                {
                    _isAvailable = value;
                    UpdateAvailabilityStateVisual();
                }
            }
        }

        #endregion

        #region 事件定义

        /// <summary>
        /// 筹码按钮选中事件
        /// </summary>
        public event System.Action<ChipButton, ChipData> OnChipSelected;

        /// <summary>
        /// 筹码按钮悬停事件
        /// </summary>
        public event System.Action<ChipButton, bool> OnChipHovered;

        /// <summary>
        /// 筹码可用性变化事件
        /// </summary>
        public event System.Action<ChipButton, bool> OnAvailabilityChanged;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            InitializeComponents();
            StoreOriginalValues();
        }

        private void Start()
        {
            SetupEventListeners();
            RegisterToUIManager();
            SetupReactiveBindings();
            UpdateInitialState();
        }

        private void OnDestroy()
        {
            CleanupEventListeners();
            UnregisterFromUIManager();
            CleanupReactiveBindings();
            StopAllCoroutines();
        }

        private void OnValidate()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            
            if (_chipImage == null)
                _chipImage = GetComponent<Image>();
            
            ValidateConfiguration();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 获取基本组件
            if (_button == null)
                _button = GetComponent<Button>();
            
            if (_chipImage == null)
                _chipImage = GetComponent<Image>();

            _rectTransform = GetComponent<RectTransform>();
            
            // 获取或创建CanvasGroup
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 查找或创建子组件
            CreateMissingComponents();
        }

        /// <summary>
        /// 创建缺失的组件
        /// </summary>
        private void CreateMissingComponents()
        {
            // 创建筹码值文本
            if (_valueText == null)
            {
                GameObject textObj = new GameObject("ValueText", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(transform, false);
                _valueText = textObj.GetComponent<Text>();
                
                // 设置文本样式
                _valueText.fontSize = 14;
                _valueText.alignment = TextAnchor.MiddleCenter;
                _valueText.color = Color.white;
                _valueText.fontStyle = FontStyle.Bold;
                _valueText.raycastTarget = false;
                
                // 设置位置（覆盖整个按钮）
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                // 添加文本阴影
                Shadow shadow = textObj.AddComponent<Shadow>();
                shadow.effectColor = Color.black;
                shadow.effectDistance = Vector2.one;
            }

            // 创建选中边框
            if (_selectionBorder == null)
            {
                GameObject borderObj = new GameObject("SelectionBorder", typeof(RectTransform), typeof(Image));
                borderObj.transform.SetParent(transform, false);
                _selectionBorder = borderObj.GetComponent<Image>();
                
                // 设置边框样式
                _selectionBorder.color = _selectedBorderColor;
                _selectionBorder.raycastTarget = false;
                
                // 设置位置（略大于按钮）
                RectTransform borderRect = borderObj.GetComponent<RectTransform>();
                borderRect.anchorMin = Vector2.zero;
                borderRect.anchorMax = Vector2.one;
                borderRect.offsetMin = Vector2.one * -3f;
                borderRect.offsetMax = Vector2.one * 3f;
                
                // 移到最后面
                borderObj.transform.SetAsFirstSibling();
                borderObj.SetActive(false);
            }

            // 创建禁用遮罩
            if (_disabledOverlay == null)
            {
                GameObject overlayObj = new GameObject("DisabledOverlay", typeof(RectTransform), typeof(Image));
                overlayObj.transform.SetParent(transform, false);
                _disabledOverlay = overlayObj.GetComponent<Image>();
                
                // 设置遮罩样式
                _disabledOverlay.color = new Color(0, 0, 0, 0.5f);
                _disabledOverlay.raycastTarget = false;
                
                // 设置位置（覆盖整个按钮）
                RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
                
                overlayObj.SetActive(false);
            }
        }

        /// <summary>
        /// 存储原始值
        /// </summary>
        private void StoreOriginalValues()
        {
            _originalScale = transform.localScale;
            
            if (_chipImage != null)
                _originalChipColor = _chipImage.color;
            
            if (_valueText != null)
                _originalTextColor = _valueText.color;
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        private void ValidateConfiguration()
        {
            if (_transitionDuration <= 0)
                _transitionDuration = 0.15f;
            
            if (_selectedScale <= 0)
                _selectedScale = 1.1f;
            
            if (_requiredUserLevel < 1)
                _requiredUserLevel = 1;
        }

        #endregion

        #region 事件设置

        /// <summary>
        /// 设置事件监听器
        /// </summary>
        private void SetupEventListeners()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// 清理事件监听器
        /// </summary>
        private void CleanupEventListeners()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
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

        #region 响应式绑定

        /// <summary>
        /// 设置响应式数据绑定
        /// </summary>
        private void SetupReactiveBindings()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager == null) return;

            // 绑定当前选中筹码
            _currentChipData = uiManager.GetOrCreateReactiveData<ChipData>("currentChip", null);
            _currentChipData.AddObserver(this);
            _unbindCurrentChip = () => _currentChipData.RemoveObserver(this);

            // 绑定用户余额
            if (_checkUserBalance)
            {
                _userBalanceData = uiManager.GetOrCreateReactiveData<float>("userBalance", 0f);
                _userBalanceData.OnValueChanged += OnUserBalanceChanged;
                _unbindUserBalance = () => _userBalanceData.OnValueChanged -= OnUserBalanceChanged;
            }

            // 绑定用户等级
            _userLevelData = uiManager.GetOrCreateReactiveData<int>("userLevel", 1);
            _userLevelData.OnValueChanged += OnUserLevelChanged;
            _unbindUserLevel = () => _userLevelData.OnValueChanged -= OnUserLevelChanged;
        }

        /// <summary>
        /// 清理响应式绑定
        /// </summary>
        private void CleanupReactiveBindings()
        {
            _unbindCurrentChip?.Invoke();
            _unbindUserBalance?.Invoke();
            _unbindUserLevel?.Invoke();
        }

        /// <summary>
        /// 响应式数据观察者接口实现
        /// </summary>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        public void OnValueChanged(ChipData oldValue, ChipData newValue)
        {
            // 检查是否是当前筹码被选中
            bool wasSelected = _isSelected;
            bool nowSelected = newValue != null && newValue.val == ChipValue;
            
            if (wasSelected != nowSelected)
            {
                IsSelected = nowSelected;
            }
        }

        /// <summary>
        /// 用户余额变化回调
        /// </summary>
        /// <param name="newBalance">新余额</param>
        private void OnUserBalanceChanged(float newBalance)
        {
            UpdateAvailabilityState();
        }

        /// <summary>
        /// 用户等级变化回调
        /// </summary>
        /// <param name="newLevel">新等级</param>
        private void OnUserLevelChanged(int newLevel)
        {
            UpdateAvailabilityState();
        }

        #endregion

        #region 交互事件处理

        /// <summary>
        /// 按钮点击事件
        /// </summary>
        private void OnButtonClicked()
        {
            // 防抖处理
            if (Time.time - _lastClickTime < CLICK_DEBOUNCE_TIME)
            {
                if (_enableDebugMode)
                    Debug.Log($"[ChipButton] 筹码 {ChipValue} 点击过快，已忽略");
                return;
            }
            _lastClickTime = Time.time;

            if (!CanSelectChip())
            {
                ShowCannotSelectFeedback();
                return;
            }

            // 选中这个筹码
            SelectChip();
        }

        /// <summary>
        /// 鼠标进入事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_enableHoverEffects || !_isInteractable) return;

            _isHovered = true;
            UpdateVisualState();
            
            OnChipHovered?.Invoke(this, true);

            if (_enableDebugMode)
                Debug.Log($"[ChipButton] 筹码 {ChipValue} 鼠标进入");
        }

        /// <summary>
        /// 鼠标离开事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_enableHoverEffects) return;

            _isHovered = false;
            UpdateVisualState();
            
            OnChipHovered?.Invoke(this, false);

            if (_enableDebugMode)
                Debug.Log($"[ChipButton] 筹码 {ChipValue} 鼠标离开");
        }

        /// <summary>
        /// 鼠标按下事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_enableClickFeedback || !_isInteractable) return;

            _isPressed = true;
            UpdateVisualState();
            
            // 播放触觉反馈
            if (_enableHapticFeedback)
            {
                PlayHapticFeedback();
            }

            if (_enableDebugMode)
                Debug.Log($"[ChipButton] 筹码 {ChipValue} 鼠标按下");
        }

        /// <summary>
        /// 鼠标抬起事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_enableClickFeedback) return;

            _isPressed = false;
            UpdateVisualState();

            if (_enableDebugMode)
                Debug.Log($"[ChipButton] 筹码 {ChipValue} 鼠标抬起");
        }

        #endregion

        #region 筹码选择逻辑

        /// <summary>
        /// 检查是否可以选择筹码
        /// </summary>
        /// <returns>是否可以选择</returns>
        private bool CanSelectChip()
        {
            if (!_isInteractable || !_isAvailable)
                return false;

            if (_chipData == null || _chipData.val <= 0)
                return false;

            return true;
        }

        /// <summary>
        /// 选中筹码
        /// </summary>
        private void SelectChip()
        {
            // 更新当前选中筹码的响应式数据
            if (_currentChipData != null)
            {
                _currentChipData.Value = _chipData;
            }

            // 播放选中音效
            if (_enableSoundEffects)
            {
                PlayChipSelectedSound();
            }

            // 播放选中动画
            if (_enableSelectionAnimation)
            {
                PlaySelectionAnimation();
            }

            // 触发事件
            OnChipSelected?.Invoke(this, _chipData);

            if (_enableDebugMode)
            {
                Debug.Log($"[ChipButton] 筹码 {ChipValue} 被选中");
            }
        }

        /// <summary>
        /// 更新可用性状态
        /// </summary>
        private void UpdateAvailabilityState()
        {
            bool newAvailability = CheckAvailability();
            if (_isAvailable != newAvailability)
            {
                IsAvailable = newAvailability;
                OnAvailabilityChanged?.Invoke(this, _isAvailable);
            }
        }

        /// <summary>
        /// 检查筹码可用性
        /// </summary>
        /// <returns>是否可用</returns>
        private bool CheckAvailability()
        {
            if (_chipData == null)
                return false;

            // 检查筹码是否启用
            if (!_chipData.enabled)
                return false;

            // 检查用户等级要求
            if (_userLevelData != null)
            {
                int userLevel = _userLevelData.Value;
                if (userLevel < _chipData.minLevel || userLevel < _requiredUserLevel)
                    return false;
            }

            // 检查用户余额
            if (_checkUserBalance && _userBalanceData != null)
            {
                float userBalance = _userBalanceData.Value;
                if (userBalance < _chipData.val)
                    return false;
            }

            return true;
        }

        #endregion

        #region 视觉更新

        /// <summary>
        /// 更新初始状态
        /// </summary>
        private void UpdateInitialState()
        {
            UpdateChipDisplay();
            UpdateAvailabilityState();
            UpdateInteractableState();
            UpdateSelectionState();
            UpdateVisualState();
        }

        /// <summary>
        /// 更新筹码显示
        /// </summary>
        private void UpdateChipDisplay()
        {
            if (_chipData == null) return;

            // 更新筹码图片
            if (_chipImage != null)
            {
                if (_chipData.sprite != null)
                {
                    _chipImage.sprite = _chipData.sprite;
                }
                else
                {
                    _chipImage.color = _chipData.themeColor;
                }
            }

            // 更新筹码值文本
            if (_valueText != null)
            {
                _valueText.text = _chipData.GetFormattedValue();
                _valueText.color = _chipData.textColor;
            }
        }

        /// <summary>
        /// 更新可交互状态
        /// </summary>
        private void UpdateInteractableState()
        {
            if (_button != null)
            {
                _button.interactable = _isInteractable && _isAvailable;
            }
            
            // 更新禁用遮罩
            if (_disabledOverlay != null)
            {
                _disabledOverlay.gameObject.SetActive(!_isInteractable || !_isAvailable);
            }
            
            UpdateVisualState();
        }

        /// <summary>
        /// 更新选中状态
        /// </summary>
        private void UpdateSelectionState()
        {
            // 更新选中边框
            if (_selectionBorder != null)
            {
                _selectionBorder.gameObject.SetActive(_isSelected);
                if (_isSelected)
                {
                    _selectionBorder.color = _selectedBorderColor;
                }
            }
            
            UpdateVisualState();
        }

        /// <summary>
        /// 更新视觉状态
        /// </summary>
        private void UpdateVisualState()
        {
            float targetAlpha = GetTargetAlpha();
            float targetScale = GetTargetScale();
            
            // 透明度过渡
            if (_alphaTransitionCoroutine != null)
                StopCoroutine(_alphaTransitionCoroutine);
            _alphaTransitionCoroutine = StartCoroutine(TransitionToAlpha(targetAlpha));

            // 缩放过渡
            if (_scaleTransitionCoroutine != null)
                StopCoroutine(_scaleTransitionCoroutine);
            _scaleTransitionCoroutine = StartCoroutine(TransitionToScale(targetScale));
        }

        /// <summary>
        /// 更新可用性状态视觉
        /// </summary>
        private void UpdateAvailabilityStateVisual()
        {
            UpdateInteractableState();
        }

        /// <summary>
        /// 获取目标透明度
        /// </summary>
        /// <returns>目标透明度</returns>
        private float GetTargetAlpha()
        {
            if (!_isInteractable || !_isAvailable)
                return _disabledAlpha;
            
            if (_isPressed)
                return _pressedAlpha;
            
            if (_isHovered)
                return _hoverAlpha;
            
            return _normalAlpha;
        }

        /// <summary>
        /// 获取目标缩放
        /// </summary>
        /// <returns>目标缩放</returns>
        private float GetTargetScale()
        {
            float baseScale = 1f;
            
            if (_isSelected)
                baseScale = _selectedScale;
            
            if (_isPressed)
                return baseScale * _pressedScale;
            
            if (_isHovered && _enableHoverEffects)
                return baseScale * _hoverScale;
            
            return baseScale;
        }

        /// <summary>
        /// 透明度过渡协程
        /// </summary>
        /// <param name="targetAlpha">目标透明度</param>
        /// <returns></returns>
        private IEnumerator TransitionToAlpha(float targetAlpha)
        {
            if (_canvasGroup == null) yield break;

            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = _animationCurve.Evaluate(elapsed / _transitionDuration);
                
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }

        /// <summary>
        /// 缩放过渡协程
        /// </summary>
        /// <param name="targetScale">目标缩放</param>
        /// <returns></returns>
        private IEnumerator TransitionToScale(float targetScale)
        {
            Vector3 startScale = transform.localScale;
            Vector3 endScale = _originalScale * targetScale;
            float elapsed = 0f;

            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = _animationCurve.Evaluate(elapsed / _transitionDuration);
                
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                
                yield return null;
            }

            transform.localScale = endScale;
        }

        #endregion

        #region 动画和反馈

        /// <summary>
        /// 播放选中动画
        /// </summary>
        private void PlaySelectionAnimation()
        {
            if (_selectionAnimationCoroutine != null)
                StopCoroutine(_selectionAnimationCoroutine);
            
            _selectionAnimationCoroutine = StartCoroutine(SelectionAnimationCoroutine());
        }

        /// <summary>
        /// 选中动画协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator SelectionAnimationCoroutine()
        {
            // 快速放大后回到正常大小
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = _originalScale * (_selectedScale + _bounceStrength);
            Vector3 finalScale = _originalScale * _selectedScale;
            
            // 放大阶段
            float elapsed = 0f;
            float duration = _transitionDuration * 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }
            
            // 回弹阶段
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(targetScale, finalScale, t);
                yield return null;
            }
            
            transform.localScale = finalScale;
        }

        /// <summary>
        /// 播放弹跳动画
        /// </summary>
        public void PlayBounceAnimation()
        {
            if (_bounceAnimationCoroutine != null)
                StopCoroutine(_bounceAnimationCoroutine);
            
            _bounceAnimationCoroutine = StartCoroutine(BounceAnimationCoroutine());
        }

        /// <summary>
        /// 弹跳动画协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator BounceAnimationCoroutine()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 bounceScale = originalScale * (1f + _bounceStrength);
            
            // 弹起
            float elapsed = 0f;
            float halfDuration = _transitionDuration * 0.3f;
            
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(originalScale, bounceScale, t);
                yield return null;
            }
            
            // 回落
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(bounceScale, originalScale, t);
                yield return null;
            }
            
            transform.localScale = originalScale;
        }

        /// <summary>
        /// 显示无法选择反馈
        /// </summary>
        private void ShowCannotSelectFeedback()
        {
            StartCoroutine(CannotSelectFeedbackCoroutine());
        }

        /// <summary>
        /// 无法选择反馈协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator CannotSelectFeedbackCoroutine()
        {
            // 左右摇摆动画
            Vector3 originalPosition = _rectTransform.anchoredPosition;
            float shakeAmount = 5f;
            float shakeDuration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float x = Mathf.Sin(elapsed * 20f) * shakeAmount * (1f - elapsed / shakeDuration);
                _rectTransform.anchoredPosition = originalPosition + new Vector3(x, 0, 0);
                yield return null;
            }
            
            _rectTransform.anchoredPosition = originalPosition;
        }

        /// <summary>
        /// 播放筹码选中音效
        /// </summary>
        private void PlayChipSelectedSound()
        {
            // 播放筹码选中音效
            // AudioManager.Instance?.PlaySFX("chip_selected");
        }

        /// <summary>
        /// 播放触觉反馈
        /// </summary>
        private void PlayHapticFeedback()
        {
            // 播放触觉反馈
            // HapticManager.Instance?.PlayLightTap();
        }

        #endregion

        #region 外观定制

        /// <summary>
        /// 设置筹码颜色主题
        /// </summary>
        /// <param name="themeColor">主题色</param>
        /// <param name="textColor">文字色</param>
        /// <param name="borderColor">边框色</param>
        public void SetColorTheme(Color themeColor, Color textColor, Color borderColor)
        {
            if (_chipData != null)
            {
                _chipData.themeColor = themeColor;
                _chipData.textColor = textColor;
                _chipData.borderColor = borderColor;
            }

            if (_chipImage != null)
                _chipImage.color = themeColor;
            
            if (_valueText != null)
                _valueText.color = textColor;
            
            _selectedBorderColor = borderColor;
            
            if (_selectionBorder != null && _isSelected)
                _selectionBorder.color = borderColor;
        }

        /// <summary>
        /// 设置筹码图片
        /// </summary>
        /// <param name="chipSprite">筹码精灵</param>
        public void SetChipSprite(Sprite chipSprite)
        {
            if (_chipData != null)
                _chipData.sprite = chipSprite;
            
            if (_chipImage != null && chipSprite != null)
            {
                _chipImage.sprite = chipSprite;
                _chipImage.color = Color.white; // 重置颜色，使用精灵原色
            }
        }

        /// <summary>
        /// 设置筹码大小
        /// </summary>
        /// <param name="size">筹码大小</param>
        public void SetChipSize(Vector2 size)
        {
            if (_rectTransform != null)
            {
                _rectTransform.sizeDelta = size;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化筹码按钮
        /// </summary>
        /// <param name="chipData">筹码数据</param>
        /// <param name="isSelected">是否选中</param>
        public void Initialize(ChipData chipData, bool isSelected = false)
        {
            _chipData = chipData;
            _isSelected = isSelected;
            
            if (Application.isPlaying)
            {
                UpdateInitialState();
            }
        }

        /// <summary>
        /// 强制选中筹码
        /// </summary>
        public void ForceSelect()
        {
            if (CanSelectChip())
            {
                SelectChip();
            }
        }

        /// <summary>
        /// 设置筹码可用性
        /// </summary>
        /// <param name="available">是否可用</param>
        public void SetAvailable(bool available)
        {
            IsAvailable = available;
        }

        /// <summary>
        /// 设置筹码启用状态
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetEnabled(bool enabled)
        {
            if (_chipData != null)
            {
                _chipData.enabled = enabled;
            }
            
            UpdateAvailabilityState();
        }

        /// <summary>
        /// 获取筹码信息文本
        /// </summary>
        /// <returns>筹码信息</returns>
        public string GetChipInfo()
        {
            if (_chipData == null) return "无效筹码";
            
            return $"筹码: {_chipData.text}\n" +
                   $"面值: ¥{_chipData.val}\n" +
                   $"要求等级: {_chipData.minLevel}\n" +
                   $"状态: {(_isAvailable ? "可用" : "不可用")}";
        }

        /// <summary>
        /// 重置筹码状态
        /// </summary>
        public void ResetState()
        {
            _isSelected = false;
            _isHovered = false;
            _isPressed = false;
            
            StopAllCoroutines();
            
            transform.localScale = _originalScale;
            if (_canvasGroup != null)
                _canvasGroup.alpha = _normalAlpha;
            
            UpdateInitialState();
        }

        /// <summary>
        /// 强制更新显示
        /// </summary>
        public void ForceUpdateDisplay()
        {
            UpdateInitialState();
        }

        #endregion

        #region 调试支持

#if UNITY_EDITOR
        [ContextMenu("测试选中")]
        private void TestSelect()
        {
            ForceSelect();
        }

        [ContextMenu("测试弹跳动画")]
        private void TestBounce()
        {
            PlayBounceAnimation();
        }

        [ContextMenu("切换可用性")]
        private void ToggleAvailability()
        {
            SetAvailable(!_isAvailable);
        }

        [ContextMenu("打印筹码信息")]
        private void PrintChipInfo()
        {
            Debug.Log(GetChipInfo());
        }
#endif

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取筹码按钮的屏幕位置
        /// </summary>
        /// <returns>屏幕位置</returns>
        public Vector3 GetScreenPosition()
        {
            if (_rectTransform != null)
            {
                return RectTransformUtility.WorldToScreenPoint(null, _rectTransform.position);
            }
            return Vector3.zero;
        }

        /// <summary>
        /// 检查点是否在筹码按钮内
        /// </summary>
        /// <param name="screenPoint">屏幕点</param>
        /// <returns>是否在内部</returns>
        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (_rectTransform != null)
            {
                Vector2 localPoint;
                return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform, screenPoint, null, out localPoint) &&
                    _rectTransform.rect.Contains(localPoint);
            }
            return false;
        }

        /// <summary>
        /// 获取筹码按钮的世界边界
        /// </summary>
        /// <returns>世界边界</returns>
        public Bounds GetWorldBounds()
        {
            if (_rectTransform != null)
            {
                Vector3[] corners = new Vector3[4];
                _rectTransform.GetWorldCorners(corners);
                
                Vector3 min = corners[0];
                Vector3 max = corners[2];
                
                return new Bounds((min + max) * 0.5f, max - min);
            }
            return new Bounds();
        }

        #endregion
    }
}