// Assets/UI/Components/BaccaratBetButton.cs
// 百家乐投注按钮组件 - 专用于百家乐投注区域的交互式按钮
// 支持投注金额显示、闪烁效果、状态管理等功能

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UI.Framework;
using UI.Effects;
using Core.Data.Types;
using Core.Architecture;
using System.Collections;

namespace UI.Components
{
    /// <summary>
    /// 百家乐投注按钮组件
    /// 专门用于百家乐投注区域，集成投注逻辑、视觉反馈、状态管理
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BaccaratBetButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, 
        IPointerDownHandler, IPointerUpHandler, IReactiveDataObserver<float>
    {
        #region 组件配置

        [Header("投注区域配置")]
        [Tooltip("投注类型")]
        [SerializeField] private BaccaratBetType _betType = BaccaratBetType.Banker;
        
        [Tooltip("投注区域标签")]
        [SerializeField] private string _betLabel = "庄";
        
        [Tooltip("投注赔率")]
        [SerializeField] private float _betOdds = 1.0f;

        [Header("组件引用")]
        [Tooltip("按钮组件")]
        [SerializeField] private Button _button;
        
        [Tooltip("背景图片")]
        [SerializeField] private Image _backgroundImage;
        
        [Tooltip("标签文本")]
        [SerializeField] private Text _labelText;
        
        [Tooltip("金额显示文本")]
        [SerializeField] private Text _amountText;
        
        [Tooltip("赔率显示文本")]
        [SerializeField] private Text _oddsText;

        [Header("视觉配置")]
        [Tooltip("正常状态颜色")]
        [SerializeField] private Color _normalColor = Color.white;
        
        [Tooltip("悬停状态颜色")]
        [SerializeField] private Color _hoverColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        
        [Tooltip("按下状态颜色")]
        [SerializeField] private Color _pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        
        [Tooltip("禁用状态颜色")]
        [SerializeField] private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [Header("动画配置")]
        [Tooltip("状态切换动画时间")]
        [SerializeField] private float _transitionDuration = 0.2f;
        
        [Tooltip("点击反馈缩放")]
        [SerializeField] private float _clickScale = 0.95f;
        
        [Tooltip("悬停反馈缩放")]
        [SerializeField] private float _hoverScale = 1.05f;

        [Header("交互配置")]
        [Tooltip("是否启用悬停效果")]
        [SerializeField] private bool _enableHoverEffects = true;
        
        [Tooltip("是否启用点击反馈")]
        [SerializeField] private bool _enableClickFeedback = true;
        
        [Tooltip("是否启用音效")]
        [SerializeField] private bool _enableSoundEffects = true;
        
        [Tooltip("是否启用触觉反馈")]
        [SerializeField] private bool _enableHapticFeedback = true;

        [Header("投注限制")]
        [Tooltip("最小投注金额")]
        [SerializeField] private float _minBetAmount = 10f;
        
        [Tooltip("最大投注金额")]
        [SerializeField] private float _maxBetAmount = 50000f;

        [Header("调试信息")]
        [Tooltip("是否启用调试模式")]
        [SerializeField] private bool _enableDebugMode = false;
        
        [SerializeField] private float _currentBetAmount = 0f;
        [SerializeField] private bool _isInteractable = true;
        [SerializeField] private bool _isFlashing = false;

        #endregion

        #region 私有字段

        // 投注数据
        private BaccaratBetTarget _betTarget;
        private List<ChipDisplayData> _chipStack = new List<ChipDisplayData>();
        
        // 状态管理
        private bool _isHovered = false;
        private bool _isPressed = false;
        private bool _isBettingEnabled = true;
        
        // 动画和效果
        private Coroutine _colorTransitionCoroutine;
        private Coroutine _scaleTransitionCoroutine;
        private Coroutine _flashCoroutine;
        
        // 响应式数据绑定
        private ReactiveData<float> _betAmountData;
        private ReactiveData<bool> _interactableData;
        private System.Action _unbindBetAmount;
        private System.Action _unbindInteractable;
        
        // 闪烁效果
        private FlashEffect _flashEffect;
        private Color _originalColor;
        private Vector3 _originalScale;

        // 事件处理
        private float _lastClickTime = 0f;
        private const float CLICK_DEBOUNCE_TIME = 0.3f;

        #endregion

        #region 公共属性

        /// <summary>
        /// 投注类型
        /// </summary>
        public BaccaratBetType BetType 
        { 
            get => _betType; 
            set => _betType = value; 
        }

        /// <summary>
        /// 投注标签
        /// </summary>
        public string BetLabel 
        { 
            get => _betLabel; 
            set 
            { 
                _betLabel = value;
                UpdateLabelText();
            } 
        }

        /// <summary>
        /// 当前投注金额
        /// </summary>
        public float CurrentBetAmount 
        { 
            get => _currentBetAmount; 
            private set
            {
                if (_currentBetAmount != value)
                {
                    _currentBetAmount = value;
                    UpdateAmountDisplay();
                    OnBetAmountChanged?.Invoke(_betType, value);
                }
            }
        }

        /// <summary>
        /// 是否可交互
        /// </summary>
        public bool IsInteractable 
        { 
            get => _isInteractable; 
            set
            {
                if (_isInteractable != value)
                {
                    _isInteractable = value;
                    UpdateInteractableState();
                }
            }
        }

        /// <summary>
        /// 是否正在闪烁
        /// </summary>
        public bool IsFlashing 
        { 
            get => _isFlashing; 
            private set => _isFlashing = value; 
        }

        /// <summary>
        /// 投注目标数据
        /// </summary>
        public BaccaratBetTarget BetTarget => _betTarget;

        #endregion

        #region 事件定义

        /// <summary>
        /// 投注按钮点击事件
        /// </summary>
        public event System.Action<BaccaratBetType, float> OnBetButtonClicked;

        /// <summary>
        /// 投注金额变化事件
        /// </summary>
        public event System.Action<BaccaratBetType, float> OnBetAmountChanged;

        /// <summary>
        /// 按钮悬停事件
        /// </summary>
        public event System.Action<BaccaratBetType, bool> OnBetButtonHovered;

        /// <summary>
        /// 闪烁状态变化事件
        /// </summary>
        public event System.Action<BaccaratBetType, bool> OnFlashStateChanged;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            InitializeComponents();
            InitializeBetTarget();
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
            
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();
            
            ValidateConfiguration();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 获取或添加必要组件
            if (_button == null)
                _button = GetComponent<Button>();
            
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();

            // 查找子组件
            if (_labelText == null)
                _labelText = GetComponentInChildren<Text>();

            // 如果没有找到文本组件，创建它们
            CreateMissingTextComponents();

            // 添加闪烁效果组件
            _flashEffect = GetComponent<FlashEffect>();
            if (_flashEffect == null)
                _flashEffect = gameObject.AddComponent<FlashEffect>();
        }

        /// <summary>
        /// 创建缺失的文本组件
        /// </summary>
        private void CreateMissingTextComponents()
        {
            if (_labelText == null)
            {
                GameObject labelObj = new GameObject("LabelText", typeof(RectTransform), typeof(Text));
                labelObj.transform.SetParent(transform, false);
                _labelText = labelObj.GetComponent<Text>();
                
                // 设置标签文本样式
                _labelText.text = _betLabel;
                _labelText.fontSize = 18;
                _labelText.alignment = TextAnchor.MiddleCenter;
                _labelText.color = Color.white;
                _labelText.raycastTarget = false;
                
                // 设置位置
                RectTransform labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0, 0.6f);
                labelRect.anchorMax = new Vector2(1, 0.9f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            if (_amountText == null)
            {
                GameObject amountObj = new GameObject("AmountText", typeof(RectTransform), typeof(Text));
                amountObj.transform.SetParent(transform, false);
                _amountText = amountObj.GetComponent<Text>();
                
                // 设置金额文本样式
                _amountText.text = "¥0";
                _amountText.fontSize = 14;
                _amountText.alignment = TextAnchor.MiddleCenter;
                _amountText.color = Color.yellow;
                _amountText.fontStyle = FontStyle.Bold;
                _amountText.raycastTarget = false;
                
                // 设置位置
                RectTransform amountRect = amountObj.GetComponent<RectTransform>();
                amountRect.anchorMin = new Vector2(0, 0.3f);
                amountRect.anchorMax = new Vector2(1, 0.6f);
                amountRect.offsetMin = Vector2.zero;
                amountRect.offsetMax = Vector2.zero;
            }

            if (_oddsText == null)
            {
                GameObject oddsObj = new GameObject("OddsText", typeof(RectTransform), typeof(Text));
                oddsObj.transform.SetParent(transform, false);
                _oddsText = oddsObj.GetComponent<Text>();
                
                // 设置赔率文本样式
                _oddsText.text = $"1:{_betOdds}";
                _oddsText.fontSize = 10;
                _oddsText.alignment = TextAnchor.LowerRight;
                _oddsText.color = Color.gray;
                _oddsText.raycastTarget = false;
                
                // 设置位置
                RectTransform oddsRect = oddsObj.GetComponent<RectTransform>();
                oddsRect.anchorMin = new Vector2(0.6f, 0);
                oddsRect.anchorMax = new Vector2(1, 0.3f);
                oddsRect.offsetMin = Vector2.zero;
                oddsRect.offsetMax = Vector2.zero;
            }
        }

        /// <summary>
        /// 初始化投注目标数据
        /// </summary>
        private void InitializeBetTarget()
        {
            _betTarget = new BaccaratBetTarget((int)_betType, _betLabel, "", _betOdds);
        }

        /// <summary>
        /// 存储原始值
        /// </summary>
        private void StoreOriginalValues()
        {
            if (_backgroundImage != null)
                _originalColor = _backgroundImage.color;
            
            _originalScale = transform.localScale;
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        private void ValidateConfiguration()
        {
            if (_minBetAmount <= 0)
                _minBetAmount = 10f;
            
            if (_maxBetAmount <= _minBetAmount)
                _maxBetAmount = _minBetAmount * 1000f;
            
            if (_transitionDuration <= 0)
                _transitionDuration = 0.2f;
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

            // 绑定投注金额
            string amountKey = $"betAmount_{(int)_betType}";
            _betAmountData = uiManager.GetOrCreateReactiveData<float>(amountKey, 0f);
            _betAmountData.AddObserver(this);
            _unbindBetAmount = () => _betAmountData.RemoveObserver(this);

            // 绑定可交互状态
            string interactableKey = $"betInteractable_{(int)_betType}";
            _interactableData = uiManager.GetOrCreateReactiveData<bool>(interactableKey, true);
            _interactableData.OnValueChanged += OnInteractableChanged;
            _unbindInteractable = () => _interactableData.OnValueChanged -= OnInteractableChanged;
        }

        /// <summary>
        /// 清理响应式绑定
        /// </summary>
        private void CleanupReactiveBindings()
        {
            _unbindBetAmount?.Invoke();
            _unbindInteractable?.Invoke();
        }

        /// <summary>
        /// 响应式数据观察者接口实现
        /// </summary>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        public void OnValueChanged(float oldValue, float newValue)
        {
            CurrentBetAmount = newValue;
        }

        /// <summary>
        /// 可交互状态变化回调
        /// </summary>
        /// <param name="isInteractable">是否可交互</param>
        private void OnInteractableChanged(bool isInteractable)
        {
            IsInteractable = isInteractable;
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
                    Debug.Log($"[BaccaratBetButton] {_betType} 点击过快，已忽略");
                return;
            }
            _lastClickTime = Time.time;

            if (!CanPlaceBet())
            {
                ShowCannotBetFeedback();
                return;
            }

            // 获取当前筹码值
            float chipValue = GetCurrentChipValue();
            if (chipValue <= 0)
            {
                if (_enableDebugMode)
                    Debug.LogWarning($"[BaccaratBetButton] {_betType} 当前筹码值无效");
                return;
            }

            // 执行投注
            ExecuteBet(chipValue);
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
            
            OnBetButtonHovered?.Invoke(_betType, true);

            if (_enableDebugMode)
                Debug.Log($"[BaccaratBetButton] {_betType} 鼠标进入");
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
            
            OnBetButtonHovered?.Invoke(_betType, false);

            if (_enableDebugMode)
                Debug.Log($"[BaccaratBetButton] {_betType} 鼠标离开");
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
                Debug.Log($"[BaccaratBetButton] {_betType} 鼠标按下");
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
                Debug.Log($"[BaccaratBetButton] {_betType} 鼠标抬起");
        }

        #endregion

        #region 投注逻辑

        /// <summary>
        /// 检查是否可以投注
        /// </summary>
        /// <returns>是否可以投注</returns>
        private bool CanPlaceBet()
        {
            if (!_isInteractable || !_isBettingEnabled)
                return false;

            // 检查游戏状态
            var uiManager = UIUpdateManager.Instance;
            if (uiManager != null)
            {
                string gamePhase = uiManager.GetReactiveValue<string>("gamePhase", "");
                if (gamePhase != "betting" && gamePhase != "waiting")
                    return false;

                // 检查用户余额
                float balance = uiManager.GetReactiveValue<float>("userBalance", 0f);
                float chipValue = GetCurrentChipValue();
                if (balance < chipValue)
                    return false;

                // 检查投注限额
                float newTotal = _currentBetAmount + chipValue;
                if (newTotal < _minBetAmount || newTotal > _maxBetAmount)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 执行投注
        /// </summary>
        /// <param name="amount">投注金额</param>
        private void ExecuteBet(float amount)
        {
            // 更新投注金额
            if (_betAmountData != null)
            {
                _betAmountData.Value += amount;
            }

            // 添加筹码到堆叠
            var chipData = GetCurrentChipData();
            if (chipData != null)
            {
                _chipStack.Add(chipData.ToDisplayData());
                _betTarget.showChip.Add(chipData.ToDisplayData());
            }

            // 播放投注音效
            if (_enableSoundEffects)
            {
                PlayBetPlacedSound();
            }

            // 显示投注反馈
            PlayBetPlacedAnimation();

            // 触发事件
            OnBetButtonClicked?.Invoke(_betType, amount);

            if (_enableDebugMode)
            {
                Debug.Log($"[BaccaratBetButton] {_betType} 投注成功: {amount}，总额: {_currentBetAmount}");
            }
        }

        /// <summary>
        /// 获取当前筹码值
        /// </summary>
        /// <returns>当前筹码值</returns>
        private float GetCurrentChipValue()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager != null)
            {
                var currentChip = uiManager.GetReactiveValue<ChipData>("currentChip");
                return currentChip?.val ?? 0f;
            }
            return 0f;
        }

        /// <summary>
        /// 获取当前筹码数据
        /// </summary>
        /// <returns>当前筹码数据</returns>
        private ChipData GetCurrentChipData()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager != null)
            {
                return uiManager.GetReactiveValue<ChipData>("currentChip");
            }
            return null;
        }

        #endregion

        #region 视觉更新

        /// <summary>
        /// 更新初始状态
        /// </summary>
        private void UpdateInitialState()
        {
            UpdateLabelText();
            UpdateAmountDisplay();
            UpdateOddsDisplay();
            UpdateInteractableState();
            UpdateVisualState();
        }

        /// <summary>
        /// 更新标签文本
        /// </summary>
        private void UpdateLabelText()
        {
            if (_labelText != null)
            {
                _labelText.text = _betLabel;
            }
        }

        /// <summary>
        /// 更新金额显示
        /// </summary>
        private void UpdateAmountDisplay()
        {
            if (_amountText != null)
            {
                if (_currentBetAmount > 0)
                {
                    _amountText.text = $"¥{_currentBetAmount:F0}";
                    _amountText.gameObject.SetActive(true);
                }
                else
                {
                    _amountText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 更新赔率显示
        /// </summary>
        private void UpdateOddsDisplay()
        {
            if (_oddsText != null)
            {
                _oddsText.text = $"1:{_betOdds}";
            }
        }

        /// <summary>
        /// 更新可交互状态
        /// </summary>
        private void UpdateInteractableState()
        {
            if (_button != null)
            {
                _button.interactable = _isInteractable && _isBettingEnabled;
            }
            
            UpdateVisualState();
        }

        /// <summary>
        /// 更新视觉状态
        /// </summary>
        private void UpdateVisualState()
        {
            Color targetColor = GetTargetColor();
            float targetScale = GetTargetScale();

            // 颜色过渡
            if (_colorTransitionCoroutine != null)
                StopCoroutine(_colorTransitionCoroutine);
            _colorTransitionCoroutine = StartCoroutine(TransitionToColor(targetColor));

            // 缩放过渡
            if (_scaleTransitionCoroutine != null)
                StopCoroutine(_scaleTransitionCoroutine);
            _scaleTransitionCoroutine = StartCoroutine(TransitionToScale(targetScale));
        }

        /// <summary>
        /// 获取目标颜色
        /// </summary>
        /// <returns>目标颜色</returns>
        private Color GetTargetColor()
        {
            if (!_isInteractable || !_isBettingEnabled)
                return _disabledColor;
            
            if (_isPressed)
                return _pressedColor;
            
            if (_isHovered)
                return _hoverColor;
            
            return _normalColor;
        }

        /// <summary>
        /// 获取目标缩放
        /// </summary>
        /// <returns>目标缩放</returns>
        private float GetTargetScale()
        {
            if (_isPressed)
                return _clickScale;
            
            if (_isHovered && _enableHoverEffects)
                return _hoverScale;
            
            return 1f;
        }

        /// <summary>
        /// 颜色过渡协程
        /// </summary>
        /// <param name="targetColor">目标颜色</param>
        /// <returns></returns>
        private IEnumerator TransitionToColor(Color targetColor)
        {
            if (_backgroundImage == null) yield break;

            Color startColor = _backgroundImage.color;
            float elapsed = 0f;

            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _transitionDuration;
                
                _backgroundImage.color = Color.Lerp(startColor, targetColor, t);
                
                yield return null;
            }

            _backgroundImage.color = targetColor;
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
                float t = elapsed / _transitionDuration;
                
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                
                yield return null;
            }

            transform.localScale = endScale;
        }

        #endregion

        #region 动画和反馈

        /// <summary>
        /// 播放投注成功动画
        /// </summary>
        private void PlayBetPlacedAnimation()
        {
            StartCoroutine(BetPlacedAnimationCoroutine());
        }

        /// <summary>
        /// 投注成功动画协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator BetPlacedAnimationCoroutine()
        {
            // 快速放大后缩小
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * 1.1f;
            
            // 放大阶段
            float elapsed = 0f;
            float duration = 0.1f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            // 缩小阶段
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
            
            transform.localScale = originalScale;
        }

        /// <summary>
        /// 显示无法投注反馈
        /// </summary>
        private void ShowCannotBetFeedback()
        {
            StartCoroutine(CannotBetFeedbackCoroutine());
        }

        /// <summary>
        /// 无法投注反馈协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator CannotBetFeedbackCoroutine()
        {
            if (_backgroundImage == null) yield break;

            // 快速闪烁红色
            Color originalColor = _backgroundImage.color;
            Color errorColor = Color.red;
            
            for (int i = 0; i < 3; i++)
            {
                _backgroundImage.color = errorColor;
                yield return new WaitForSeconds(0.1f);
                _backgroundImage.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// 播放投注音效
        /// </summary>
        private void PlayBetPlacedSound()
        {
            // 播放投注音效
            // AudioManager.Instance?.PlaySFX("bet_placed");
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

        #region 闪烁效果

        /// <summary>
        /// 开始闪烁效果
        /// </summary>
        /// <param name="flashColor">闪烁颜色</param>
        /// <param name="duration">闪烁时长</param>
        /// <param name="flashCount">闪烁次数</param>
        public void StartFlash(Color flashColor, float duration = 3f, int flashCount = 6)
        {
            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);
            
            _flashCoroutine = StartCoroutine(FlashCoroutine(flashColor, duration, flashCount));
            IsFlashing = true;
            OnFlashStateChanged?.Invoke(_betType, true);
        }

        /// <summary>
        /// 停止闪烁效果
        /// </summary>
        public void StopFlash()
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
            
            IsFlashing = false;
            OnFlashStateChanged?.Invoke(_betType, false);
            
            // 恢复原始颜色
            UpdateVisualState();
        }

        /// <summary>
        /// 闪烁协程
        /// </summary>
        /// <param name="flashColor">闪烁颜色</param>
        /// <param name="duration">总时长</param>
        /// <param name="flashCount">闪烁次数</param>
        /// <returns></returns>
        private IEnumerator FlashCoroutine(Color flashColor, float duration, int flashCount)
        {
            if (_backgroundImage == null) yield break;

            Color originalColor = _backgroundImage.color;
            float flashInterval = duration / (flashCount * 2);
            
            for (int i = 0; i < flashCount; i++)
            {
                // 闪烁到目标色
                _backgroundImage.color = flashColor;
                yield return new WaitForSeconds(flashInterval);
                
                // 闪烁回原始色
                _backgroundImage.color = originalColor;
                yield return new WaitForSeconds(flashInterval);
            }
            
            IsFlashing = false;
            OnFlashStateChanged?.Invoke(_betType, false);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置投注使能状态
        /// </summary>
        /// <param name="enabled">是否使能投注</param>
        public void SetBettingEnabled(bool enabled)
        {
            _isBettingEnabled = enabled;
            UpdateInteractableState();
        }

        /// <summary>
        /// 清除投注金额
        /// </summary>
        public void ClearBetAmount()
        {
            if (_betAmountData != null)
            {
                _betAmountData.Value = 0f;
            }
            
            _chipStack.Clear();
            _betTarget.ClearBet();
        }

        /// <summary>
        /// 获取投注堆叠信息
        /// </summary>
        /// <returns>筹码堆叠列表</returns>
        public List<ChipDisplayData> GetChipStack()
        {
            return new List<ChipDisplayData>(_chipStack);
        }

        /// <summary>
        /// 设置投注限额
        /// </summary>
        /// <param name="minAmount">最小金额</param>
        /// <param name="maxAmount">最大金额</param>
        public void SetBetLimits(float minAmount, float maxAmount)
        {
            _minBetAmount = minAmount;
            _maxBetAmount = maxAmount;
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
        [ContextMenu("测试投注")]
        private void TestBet()
        {
            ExecuteBet(100f);
        }

        [ContextMenu("测试闪烁")]
        private void TestFlash()
        {
            StartFlash(Color.yellow, 2f, 4);
        }

        [ContextMenu("清除投注")]
        private void TestClearBet()
        {
            ClearBetAmount();
        }
#endif

        #endregion
    }
}