// Assets/UI/Components/TimerDisplay.cs
// 倒计时显示组件 - 专用于显示游戏倒计时
// 支持响应式绑定、动画效果、多种显示格式、音效提醒等功能

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UI.Framework;
using Core.Data.Types;
using Core.Architecture;

namespace UI.Components
{
    /// <summary>
    /// 倒计时显示组件
    /// 专门用于显示游戏倒计时，支持多种格式、动画效果、声音提醒
    /// </summary>
    public class TimerDisplay : MonoBehaviour, IReactiveDataObserver<int>
    {
        #region 组件配置

        [Header("组件引用")]
        [Tooltip("倒计时文本")]
        [SerializeField] private Text _timerText;
        
        [Tooltip("阶段标签文本")]
        [SerializeField] private Text _phaseText;
        
        [Tooltip("背景图片")]
        [SerializeField] private Image _backgroundImage;
        
        [Tooltip("进度条")]
        [SerializeField] private Image _progressBar;
        
        [Tooltip("警告图标")]
        [SerializeField] private Image _warningIcon;

        [Header("显示配置")]
        [Tooltip("时间显示格式")]
        [SerializeField] private TimeDisplayFormat _displayFormat = TimeDisplayFormat.MinuteSecond;
        
        [Tooltip("是否显示阶段标签")]
        [SerializeField] private bool _showPhaseLabel = true;
        
        [Tooltip("是否显示进度条")]
        [SerializeField] private bool _showProgressBar = true;
        
        [Tooltip("是否显示毫秒")]
        [SerializeField] private bool _showMilliseconds = false;

        [Header("颜色配置")]
        [Tooltip("正常状态颜色")]
        [SerializeField] private Color _normalColor = Color.white;
        
        [Tooltip("警告状态颜色")]
        [SerializeField] private Color _warningColor = Color.yellow;
        
        [Tooltip("紧急状态颜色")]
        [SerializeField] private Color _urgentColor = Color.red;
        
        [Tooltip("进度条正常颜色")]
        [SerializeField] private Color _progressNormalColor = Color.green;
        
        [Tooltip("进度条警告颜色")]
        [SerializeField] private Color _progressWarningColor = Color.yellow;
        
        [Tooltip("进度条紧急颜色")]
        [SerializeField] private Color _progressUrgentColor = Color.red;

        [Header("动画配置")]
        [Tooltip("是否启用脉冲动画")]
        [SerializeField] private bool _enablePulseAnimation = true;
        
        [Tooltip("是否启用数字跳动")]
        [SerializeField] private bool _enableNumberJump = true;
        
        [Tooltip("脉冲动画强度")]
        [SerializeField] private float _pulseStrength = 0.1f;
        
        [Tooltip("脉冲动画速度")]
        [SerializeField] private float _pulseSpeed = 2f;
        
        [Tooltip("数字变化动画时间")]
        [SerializeField] private float _numberTransitionDuration = 0.3f;

        [Header("警告配置")]
        [Tooltip("警告阈值（秒）")]
        [SerializeField] private int _warningThreshold = 10;
        
        [Tooltip("紧急阈值（秒）")]
        [SerializeField] private int _urgentThreshold = 5;
        
        [Tooltip("是否启用音效提醒")]
        [SerializeField] private bool _enableSoundAlerts = true;
        
        [Tooltip("是否启用震动提醒")]
        [SerializeField] private bool _enableHapticFeedback = true;

        [Header("字体配置")]
        [Tooltip("正常字体大小")]
        [SerializeField] private int _normalFontSize = 24;
        
        [Tooltip("强调字体大小")]
        [SerializeField] private int _emphasizedFontSize = 32;
        
        [Tooltip("字体样式")]
        [SerializeField] private FontStyle _fontStyle = FontStyle.Bold;

        [Header("调试信息")]
        [Tooltip("是否启用调试模式")]
        [SerializeField] private bool _enableDebugMode = false;
        
        [SerializeField] private int _currentTime = 0;
        [SerializeField] private GamePhase _currentPhase = GamePhase.Waiting;
        [SerializeField] private TimerState _currentState = TimerState.Normal;

        #endregion

        #region 私有字段

        // 计时器状态
        private int _targetTime = 0;
        private int _maxTime = 30;
        private bool _isActive = false;
        private bool _isPaused = false;
        
        // 动画和效果
        private Coroutine _pulseCoroutine;
        private Coroutine _numberTransitionCoroutine;
        private Coroutine _colorTransitionCoroutine;
        private Coroutine _progressAnimationCoroutine;
        
        // 响应式数据绑定
        private ReactiveData<int> _countdownData;
        private ReactiveData<GamePhase> _gamePhaseData;
        private ReactiveData<string> _gameNumberData;
        private System.Action _unbindCountdown;
        private System.Action _unbindGamePhase;
        private System.Action _unbindGameNumber;
        
        // 原始值存储
        private Vector3 _originalScale;
        private Color _originalTextColor;
        private int _originalFontSize;
        
        // 音效播放标记
        private bool _warningPlayed = false;
        private bool _urgentPlayed = false;
        private bool _finalCountdownPlayed = false;

        // 阶段显示文本
        private readonly Dictionary<GamePhase, string> _phaseTexts = new Dictionary<GamePhase, string>
        {
            { GamePhase.Waiting, "等待开始" },
            { GamePhase.Betting, "投注中" },
            { GamePhase.Dealing, "发牌中" },
            { GamePhase.Result, "结算中" },
            { GamePhase.Pause, "暂停" }
        };

        #endregion

        #region 枚举定义

        /// <summary>
        /// 时间显示格式
        /// </summary>
        public enum TimeDisplayFormat
        {
            SecondsOnly,    // 仅秒数 "30"
            MinuteSecond,   // 分:秒 "00:30"
            Descriptive     // 描述性 "30秒"
        }

        /// <summary>
        /// 计时器状态
        /// </summary>
        public enum TimerState
        {
            Normal,    // 正常状态
            Warning,   // 警告状态
            Urgent,    // 紧急状态
            Stopped    // 停止状态
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前倒计时时间
        /// </summary>
        public int CurrentTime 
        { 
            get => _currentTime; 
            private set
            {
                if (_currentTime != value)
                {
                    int oldValue = _currentTime;
                    _currentTime = value;
                    OnTimeChanged(oldValue, value);
                }
            }
        }

        /// <summary>
        /// 当前游戏阶段
        /// </summary>
        public GamePhase CurrentPhase 
        { 
            get => _currentPhase; 
            private set
            {
                if (_currentPhase != value)
                {
                    _currentPhase = value;
                    UpdatePhaseDisplay();
                }
            }
        }

        /// <summary>
        /// 当前计时器状态
        /// </summary>
        public TimerState CurrentState 
        { 
            get => _currentState; 
            private set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    UpdateTimerState();
                }
            }
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// 是否已暂停
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// 剩余时间百分比
        /// </summary>
        public float TimePercentage => _maxTime > 0 ? Mathf.Clamp01((float)_currentTime / _maxTime) : 0f;

        #endregion

        #region 事件定义

        /// <summary>
        /// 时间变化事件
        /// </summary>
        public event System.Action<int, int> OnTimeValueChanged;

        /// <summary>
        /// 计时器状态变化事件
        /// </summary>
        public event System.Action<TimerState> OnTimerStateChanged;

        /// <summary>
        /// 倒计时结束事件
        /// </summary>
        public event System.Action OnTimerExpired;

        /// <summary>
        /// 警告阈值触发事件
        /// </summary>
        public event System.Action<int> OnWarningTriggered;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            InitializeComponents();
            StoreOriginalValues();
        }

        private void Start()
        {
            RegisterToUIManager();
            SetupReactiveBindings();
            UpdateInitialDisplay();
        }

        private void OnDestroy()
        {
            UnregisterFromUIManager();
            CleanupReactiveBindings();
            StopAllCoroutines();
        }

        private void OnValidate()
        {
            ValidateConfiguration();
            if (Application.isPlaying)
            {
                UpdateDisplay();
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 查找组件引用
            if (_timerText == null)
                _timerText = GetComponentInChildren<Text>();

            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();

            // 创建缺失的组件
            CreateMissingComponents();
        }

        /// <summary>
        /// 创建缺失的组件
        /// </summary>
        private void CreateMissingComponents()
        {
            // 创建计时器文本
            if (_timerText == null)
            {
                GameObject textObj = new GameObject("TimerText", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(transform, false);
                _timerText = textObj.GetComponent<Text>();
                
                // 设置文本样式
                _timerText.text = "00:00";
                _timerText.fontSize = _normalFontSize;
                _timerText.alignment = TextAnchor.MiddleCenter;
                _timerText.color = _normalColor;
                _timerText.fontStyle = _fontStyle;
                _timerText.raycastTarget = false;
                
                // 设置位置
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

            // 创建阶段标签
            if (_phaseText == null && _showPhaseLabel)
            {
                GameObject phaseObj = new GameObject("PhaseText", typeof(RectTransform), typeof(Text));
                phaseObj.transform.SetParent(transform, false);
                _phaseText = phaseObj.GetComponent<Text>();
                
                // 设置阶段文本样式
                _phaseText.text = "等待开始";
                _phaseText.fontSize = _normalFontSize - 6;
                _phaseText.alignment = TextAnchor.UpperCenter;
                _phaseText.color = _normalColor;
                _phaseText.raycastTarget = false;
                
                // 设置位置（在计时器上方）
                RectTransform phaseRect = phaseObj.GetComponent<RectTransform>();
                phaseRect.anchorMin = new Vector2(0, 0.7f);
                phaseRect.anchorMax = new Vector2(1, 1f);
                phaseRect.offsetMin = Vector2.zero;
                phaseRect.offsetMax = Vector2.zero;
            }

            // 创建进度条
            if (_progressBar == null && _showProgressBar)
            {
                GameObject progressObj = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
                progressObj.transform.SetParent(transform, false);
                _progressBar = progressObj.GetComponent<Image>();
                
                // 设置进度条样式
                _progressBar.color = _progressNormalColor;
                _progressBar.type = Image.Type.Filled;
                _progressBar.fillMethod = Image.FillMethod.Horizontal;
                _progressBar.raycastTarget = false;
                
                // 设置位置（在计时器下方）
                RectTransform progressRect = progressObj.GetComponent<RectTransform>();
                progressRect.anchorMin = new Vector2(0, 0);
                progressRect.anchorMax = new Vector2(1, 0.2f);
                progressRect.offsetMin = Vector2.zero;
                progressRect.offsetMax = Vector2.zero;
            }

            // 创建警告图标
            if (_warningIcon == null)
            {
                GameObject warningObj = new GameObject("WarningIcon", typeof(RectTransform), typeof(Image));
                warningObj.transform.SetParent(transform, false);
                _warningIcon = warningObj.GetComponent<Image>();
                
                // 设置警告图标样式
                _warningIcon.color = _urgentColor;
                _warningIcon.raycastTarget = false;
                
                // 设置位置（右上角）
                RectTransform warningRect = warningObj.GetComponent<RectTransform>();
                warningRect.anchorMin = new Vector2(0.8f, 0.8f);
                warningRect.anchorMax = new Vector2(1f, 1f);
                warningRect.offsetMin = Vector2.zero;
                warningRect.offsetMax = Vector2.zero;
                
                // 初始时隐藏
                warningObj.SetActive(false);
            }
        }

        /// <summary>
        /// 存储原始值
        /// </summary>
        private void StoreOriginalValues()
        {
            _originalScale = transform.localScale;
            
            if (_timerText != null)
            {
                _originalTextColor = _timerText.color;
                _originalFontSize = _timerText.fontSize;
            }
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        private void ValidateConfiguration()
        {
            if (_warningThreshold <= 0)
                _warningThreshold = 10;
            
            if (_urgentThreshold <= 0 || _urgentThreshold >= _warningThreshold)
                _urgentThreshold = 5;
            
            if (_normalFontSize <= 0)
                _normalFontSize = 24;
        }

        #endregion

        #region 响应式绑定

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

        /// <summary>
        /// 设置响应式数据绑定
        /// </summary>
        private void SetupReactiveBindings()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager == null) return;

            // 绑定倒计时数据
            _countdownData = uiManager.GetOrCreateReactiveData<int>("countdown", 0);
            _countdownData.AddObserver(this);
            _unbindCountdown = () => _countdownData.RemoveObserver(this);

            // 绑定游戏阶段数据
            _gamePhaseData = uiManager.GetOrCreateReactiveData<GamePhase>("gamePhase", GamePhase.Waiting);
            _gamePhaseData.OnValueChanged += OnGamePhaseChanged;
            _unbindGamePhase = () => _gamePhaseData.OnValueChanged -= OnGamePhaseChanged;

            // 绑定游戏局号（用于重置）
            _gameNumberData = uiManager.GetOrCreateReactiveData<string>("gameNumber", "");
            _gameNumberData.OnValueChanged += OnGameNumberChanged;
            _unbindGameNumber = () => _gameNumberData.OnValueChanged -= OnGameNumberChanged;
        }

        /// <summary>
        /// 清理响应式绑定
        /// </summary>
        private void CleanupReactiveBindings()
        {
            _unbindCountdown?.Invoke();
            _unbindGamePhase?.Invoke();
            _unbindGameNumber?.Invoke();
        }

        /// <summary>
        /// 响应式数据观察者接口实现
        /// </summary>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        public void OnValueChanged(int oldValue, int newValue)
        {
            SetTime(newValue);
        }

        /// <summary>
        /// 游戏阶段变化回调
        /// </summary>
        /// <param name="newPhase">新阶段</param>
        private void OnGamePhaseChanged(GamePhase newPhase)
        {
            CurrentPhase = newPhase;
            
            // 根据阶段设置最大时间
            _maxTime = GetMaxTimeForPhase(newPhase);
            
            // 重置音效标记
            ResetSoundFlags();
        }

        /// <summary>
        /// 游戏局号变化回调
        /// </summary>
        /// <param name="newGameNumber">新局号</param>
        private void OnGameNumberChanged(string newGameNumber)
        {
            // 新局开始，重置所有状态
            ResetTimer();
        }

        #endregion

        #region 计时器控制

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="time">时间（秒）</param>
        public void SetTime(int time)
        {
            time = Mathf.Max(0, time);
            CurrentTime = time;
        }

        /// <summary>
        /// 设置最大时间
        /// </summary>
        /// <param name="maxTime">最大时间</param>
        public void SetMaxTime(int maxTime)
        {
            _maxTime = Mathf.Max(1, maxTime);
            UpdateDisplay();
        }

        /// <summary>
        /// 开始计时器
        /// </summary>
        public void StartTimer()
        {
            _isActive = true;
            _isPaused = false;
            
            if (_enableDebugMode)
            {
                Debug.Log($"[TimerDisplay] 计时器启动: {_currentTime}秒");
            }
        }

        /// <summary>
        /// 停止计时器
        /// </summary>
        public void StopTimer()
        {
            _isActive = false;
            _isPaused = false;
            CurrentState = TimerState.Stopped;
            
            if (_enableDebugMode)
            {
                Debug.Log("[TimerDisplay] 计时器停止");
            }
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        public void PauseTimer()
        {
            _isPaused = true;
            
            if (_enableDebugMode)
            {
                Debug.Log("[TimerDisplay] 计时器暂停");
            }
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        public void ResumeTimer()
        {
            _isPaused = false;
            
            if (_enableDebugMode)
            {
                Debug.Log("[TimerDisplay] 计时器恢复");
            }
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        public void ResetTimer()
        {
            CurrentTime = 0;
            CurrentState = TimerState.Normal;
            _isActive = false;
            _isPaused = false;
            
            ResetSoundFlags();
            UpdateDisplay();
            
            if (_enableDebugMode)
            {
                Debug.Log("[TimerDisplay] 计时器重置");
            }
        }

        #endregion

        #region 显示更新

        /// <summary>
        /// 更新初始显示
        /// </summary>
        private void UpdateInitialDisplay()
        {
            UpdateDisplay();
            UpdatePhaseDisplay();
            UpdateTimerState();
        }

        /// <summary>
        /// 更新显示
        /// </summary>
        private void UpdateDisplay()
        {
            UpdateTimeText();
            UpdateProgressBar();
        }

        /// <summary>
        /// 更新时间文本
        /// </summary>
        private void UpdateTimeText()
        {
            if (_timerText == null) return;

            string timeText = FormatTime(_currentTime);
            
            // 数字跳动动画
            if (_enableNumberJump && _timerText.text != timeText)
            {
                if (_numberTransitionCoroutine != null)
                    StopCoroutine(_numberTransitionCoroutine);
                _numberTransitionCoroutine = StartCoroutine(NumberJumpAnimation(timeText));
            }
            else
            {
                _timerText.text = timeText;
            }
        }

        /// <summary>
        /// 更新阶段显示
        /// </summary>
        private void UpdatePhaseDisplay()
        {
            if (_phaseText == null) return;

            if (_phaseTexts.ContainsKey(_currentPhase))
            {
                _phaseText.text = _phaseTexts[_currentPhase];
            }
        }

        /// <summary>
        /// 更新进度条
        /// </summary>
        private void UpdateProgressBar()
        {
            if (_progressBar == null) return;

            float progress = TimePercentage;
            
            if (_progressAnimationCoroutine != null)
                StopCoroutine(_progressAnimationCoroutine);
            _progressAnimationCoroutine = StartCoroutine(ProgressBarAnimation(progress));
        }

        /// <summary>
        /// 格式化时间显示
        /// </summary>
        /// <param name="seconds">秒数</param>
        /// <returns>格式化的时间字符串</returns>
        private string FormatTime(int seconds)
        {
            switch (_displayFormat)
            {
                case TimeDisplayFormat.SecondsOnly:
                    return seconds.ToString();
                
                case TimeDisplayFormat.MinuteSecond:
                    int minutes = seconds / 60;
                    int secs = seconds % 60;
                    return $"{minutes:D2}:{secs:D2}";
                
                case TimeDisplayFormat.Descriptive:
                    if (seconds >= 60)
                    {
                        int mins = seconds / 60;
                        int remainingSecs = seconds % 60;
                        return remainingSecs > 0 ? $"{mins}分{remainingSecs}秒" : $"{mins}分";
                    }
                    return $"{seconds}秒";
                
                default:
                    return seconds.ToString();
            }
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 时间变化处理
        /// </summary>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        private void OnTimeChanged(int oldValue, int newValue)
        {
            // 更新显示
            UpdateDisplay();
            
            // 检查状态变化
            TimerState newState = DetermineTimerState(newValue);
            if (newState != _currentState)
            {
                CurrentState = newState;
            }
            
            // 检查警告阈值
            CheckWarningThresholds(newValue);
            
            // 检查倒计时结束
            if (newValue <= 0 && oldValue > 0)
            {
                OnTimerExpired?.Invoke();
            }
            
            // 触发事件
            OnTimeValueChanged?.Invoke(oldValue, newValue);
            
            if (_enableDebugMode)
            {
                Debug.Log($"[TimerDisplay] 时间更新: {oldValue} -> {newValue}, 状态: {_currentState}");
            }
        }

        /// <summary>
        /// 确定计时器状态
        /// </summary>
        /// <param name="time">当前时间</param>
        /// <returns>计时器状态</returns>
        private TimerState DetermineTimerState(int time)
        {
            if (!_isActive)
                return TimerState.Stopped;
            
            if (time <= _urgentThreshold)
                return TimerState.Urgent;
            
            if (time <= _warningThreshold)
                return TimerState.Warning;
            
            return TimerState.Normal;
        }

        /// <summary>
        /// 更新计时器状态
        /// </summary>
        private void UpdateTimerState()
        {
            UpdateStateColors();
            UpdateStateAnimations();
            UpdateWarningIcon();
            
            OnTimerStateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// 更新状态颜色
        /// </summary>
        private void UpdateStateColors()
        {
            Color targetTextColor = GetStateColor();
            Color targetProgressColor = GetProgressStateColor();
            
            if (_colorTransitionCoroutine != null)
                StopCoroutine(_colorTransitionCoroutine);
            _colorTransitionCoroutine = StartCoroutine(ColorTransitionAnimation(targetTextColor, targetProgressColor));
        }

        /// <summary>
        /// 更新状态动画
        /// </summary>
        private void UpdateStateAnimations()
        {
            // 停止之前的动画
            if (_pulseCoroutine != null)
                StopCoroutine(_pulseCoroutine);
            
            // 根据状态启动动画
            if (_enablePulseAnimation && (_currentState == TimerState.Warning || _currentState == TimerState.Urgent))
            {
                _pulseCoroutine = StartCoroutine(PulseAnimation());
            }
            
            // 更新字体大小
            UpdateFontSize();
        }

        /// <summary>
        /// 更新警告图标
        /// </summary>
        private void UpdateWarningIcon()
        {
            if (_warningIcon == null) return;
            
            bool shouldShow = _currentState == TimerState.Urgent;
            _warningIcon.gameObject.SetActive(shouldShow);
        }

        /// <summary>
        /// 获取状态颜色
        /// </summary>
        /// <returns>状态对应的颜色</returns>
        private Color GetStateColor()
        {
            return _currentState switch
            {
                TimerState.Warning => _warningColor,
                TimerState.Urgent => _urgentColor,
                _ => _normalColor
            };
        }

        /// <summary>
        /// 获取进度条状态颜色
        /// </summary>
        /// <returns>进度条状态对应的颜色</returns>
        private Color GetProgressStateColor()
        {
            return _currentState switch
            {
                TimerState.Warning => _progressWarningColor,
                TimerState.Urgent => _progressUrgentColor,
                _ => _progressNormalColor
            };
        }

        /// <summary>
        /// 更新字体大小
        /// </summary>
        private void UpdateFontSize()
        {
            if (_timerText == null) return;
            
            int targetSize = _currentState == TimerState.Urgent ? _emphasizedFontSize : _normalFontSize;
            _timerText.fontSize = targetSize;
        }

        #endregion

        #region 动画效果

        /// <summary>
        /// 脉冲动画协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator PulseAnimation()
        {
            while (_currentState == TimerState.Warning || _currentState == TimerState.Urgent)
            {
                float speed = _currentState == TimerState.Urgent ? _pulseSpeed * 1.5f : _pulseSpeed;
                float strength = _currentState == TimerState.Urgent ? _pulseStrength * 1.5f : _pulseStrength;
                
                float scale = 1f + Mathf.Sin(Time.time * speed) * strength;
                transform.localScale = _originalScale * scale;
                
                yield return null;
            }
            
            // 恢复原始大小
            transform.localScale = _originalScale;
        }

        /// <summary>
        /// 数字跳动动画协程
        /// </summary>
        /// <param name="targetText">目标文本</param>
        /// <returns></returns>
        private IEnumerator NumberJumpAnimation(string targetText)
        {
            if (_timerText == null) yield break;

            Vector3 originalScale = _timerText.transform.localScale;
            Vector3 jumpScale = originalScale * 1.2f;
            
            // 跳起
            float elapsed = 0f;
            float halfDuration = _numberTransitionDuration * 0.5f;
            
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                _timerText.transform.localScale = Vector3.Lerp(originalScale, jumpScale, t);
                yield return null;
            }
            
            // 更新文本
            _timerText.text = targetText;
            
            // 落下
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                _timerText.transform.localScale = Vector3.Lerp(jumpScale, originalScale, t);
                yield return null;
            }
            
            _timerText.transform.localScale = originalScale;
        }

        /// <summary>
        /// 颜色过渡动画协程
        /// </summary>
        /// <param name="targetTextColor">目标文本颜色</param>
        /// <param name="targetProgressColor">目标进度条颜色</param>
        /// <returns></returns>
        private IEnumerator ColorTransitionAnimation(Color targetTextColor, Color targetProgressColor)
        {
            Color startTextColor = _timerText != null ? _timerText.color : Color.white;
            Color startProgressColor = _progressBar != null ? _progressBar.color : Color.white;
            
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                if (_timerText != null)
                    _timerText.color = Color.Lerp(startTextColor, targetTextColor, t);
                
                if (_progressBar != null)
                    _progressBar.color = Color.Lerp(startProgressColor, targetProgressColor, t);
                
                yield return null;
            }
            
            if (_timerText != null)
                _timerText.color = targetTextColor;
            
            if (_progressBar != null)
                _progressBar.color = targetProgressColor;
        }

        /// <summary>
        /// 进度条动画协程
        /// </summary>
        /// <param name="targetProgress">目标进度</param>
        /// <returns></returns>
        private IEnumerator ProgressBarAnimation(float targetProgress)
        {
            if (_progressBar == null) yield break;

            float startProgress = _progressBar.fillAmount;
            float elapsed = 0f;
            float duration = 0.2f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _progressBar.fillAmount = Mathf.Lerp(startProgress, targetProgress, t);
                yield return null;
            }
            
            _progressBar.fillAmount = targetProgress;
        }

        #endregion

        #region 警告和音效

        /// <summary>
        /// 检查警告阈值
        /// </summary>
        /// <param name="time">当前时间</param>
        private void CheckWarningThresholds(int time)
        {
            // 警告音效
            if (time <= _warningThreshold && time > _urgentThreshold && !_warningPlayed)
            {
                _warningPlayed = true;
                PlayWarningSound();
                OnWarningTriggered?.Invoke(time);
            }
            
            // 紧急音效
            if (time <= _urgentThreshold && time > 0 && !_urgentPlayed)
            {
                _urgentPlayed = true;
                PlayUrgentSound();
                TriggerHapticFeedback();
            }
            
            // 最后倒计时音效
            if (time <= 3 && time > 0 && !_finalCountdownPlayed)
            {
                _finalCountdownPlayed = true;
                PlayFinalCountdownSound();
            }
        }

        /// <summary>
        /// 播放警告音效
        /// </summary>
        private void PlayWarningSound()
        {
            if (_enableSoundAlerts)
            {
                // AudioManager.Instance?.PlaySFX("timer_warning");
                if (_enableDebugMode)
                    Debug.Log("[TimerDisplay] 播放警告音效");
            }
        }

        /// <summary>
        /// 播放紧急音效
        /// </summary>
        private void PlayUrgentSound()
        {
            if (_enableSoundAlerts)
            {
                // AudioManager.Instance?.PlaySFX("timer_urgent");
                if (_enableDebugMode)
                    Debug.Log("[TimerDisplay] 播放紧急音效");
            }
        }

        /// <summary>
        /// 播放最后倒计时音效
        /// </summary>
        private void PlayFinalCountdownSound()
        {
            if (_enableSoundAlerts)
            {
                // AudioManager.Instance?.PlaySFX("timer_final_countdown");
                if (_enableDebugMode)
                    Debug.Log("[TimerDisplay] 播放最后倒计时音效");
            }
        }

        /// <summary>
        /// 触发触觉反馈
        /// </summary>
        private void TriggerHapticFeedback()
        {
            if (_enableHapticFeedback)
            {
                // HapticManager.Instance?.PlayWarning();
                if (_enableDebugMode)
                    Debug.Log("[TimerDisplay] 触发触觉反馈");
            }
        }

        /// <summary>
        /// 重置音效标记
        /// </summary>
        private void ResetSoundFlags()
        {
            _warningPlayed = false;
            _urgentPlayed = false;
            _finalCountdownPlayed = false;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取阶段的最大时间
        /// </summary>
        /// <param name="phase">游戏阶段</param>
        /// <returns>最大时间（秒）</returns>
        private int GetMaxTimeForPhase(GamePhase phase)
        {
            return phase switch
            {
                GamePhase.Betting => 30,
                GamePhase.Dealing => 10,
                GamePhase.Result => 5,
                _ => 30
            };
        }

        /// <summary>
        /// 设置显示格式
        /// </summary>
        /// <param name="format">显示格式</param>
        public void SetDisplayFormat(TimeDisplayFormat format)
        {
            _displayFormat = format;
            UpdateTimeText();
        }

        /// <summary>
        /// 设置警告阈值
        /// </summary>
        /// <param name="warningThreshold">警告阈值</param>
        /// <param name="urgentThreshold">紧急阈值</param>
        public void SetWarningThresholds(int warningThreshold, int urgentThreshold)
        {
            _warningThreshold = warningThreshold;
            _urgentThreshold = urgentThreshold;
            ValidateConfiguration();
        }

        /// <summary>
        /// 获取格式化的剩余时间
        /// </summary>
        /// <returns>格式化的时间字符串</returns>
        public string GetFormattedTime()
        {
            return FormatTime(_currentTime);
        }

        /// <summary>
        /// 强制更新显示
        /// </summary>
        public void ForceUpdateDisplay()
        {
            UpdateDisplay();
        }

        #endregion

        #region 调试支持

#if UNITY_EDITOR
        [ContextMenu("测试警告状态")]
        private void TestWarningState()
        {
            SetTime(_warningThreshold);
        }

        [ContextMenu("测试紧急状态")]
        private void TestUrgentState()
        {
            SetTime(_urgentThreshold);
        }

        [ContextMenu("测试倒计时")]
        private void TestCountdown()
        {
            StartCoroutine(TestCountdownCoroutine());
        }

        private IEnumerator TestCountdownCoroutine()
        {
            for (int i = 15; i >= 0; i--)
            {
                SetTime(i);
                yield return new WaitForSeconds(1f);
            }
        }
#endif

        #endregion
    }
}