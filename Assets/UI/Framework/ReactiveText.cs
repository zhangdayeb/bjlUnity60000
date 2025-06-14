// Assets/UI/Framework/ReactiveText.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Architecture;

namespace UI.Framework
{
    /// <summary>
    /// 响应式文本组件
    /// 自动绑定ReactiveData并在数据变化时更新文本显示
    /// 支持Text、TextMeshPro、格式化、本地化等功能
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class ReactiveText : MonoBehaviour, IReactiveComponent
    {
        [Header("绑定配置")]
        [SerializeField] private ReactiveTextBinding textBinding = new ReactiveTextBinding();
        [SerializeField] private bool autoFindTextComponent = true;
        [SerializeField] private bool updateOnStart = true;

        [Header("格式化配置")]
        [SerializeField] private string formatString = "{0}";
        [SerializeField] private ReactiveTextFormatter formatter = ReactiveTextFormatter.None;
        [SerializeField] private string customFormat = "";
        [SerializeField] private bool useLocalization = false;
        [SerializeField] private string localizationKey = "";

        [Header("动画配置")]
        [SerializeField] private bool enableAnimation = false;
        [SerializeField] private ReactiveTextAnimation animationType = ReactiveTextAnimation.None;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("样式配置")]
        [SerializeField] private bool enableStyleBinding = false;
        [SerializeField] private ReactiveTextStyle styleConfig = new ReactiveTextStyle();

        [Header("调试信息")]
        [SerializeField] private bool enableDebugLog = false;
        [SerializeField] private string componentName = "";
        [SerializeField] private int updateCount = 0;
        [SerializeField] private float lastUpdateTime = 0f;

        #region 私有字段

        // UI组件引用
        private Text _textComponent;
        private TextMeshProUGUI _tmpComponent;
        private bool _isTextMeshPro;

        // 响应式数据绑定
        private readonly List<System.Action> _dataUnbinders = new List<System.Action>();
        private readonly Dictionary<string, object> _lastValues = new Dictionary<string, object>();

        // 动画控制
        private Coroutine _animationCoroutine;
        private string _targetText = "";
        private string _currentDisplayText = "";

        // 缓存和性能
        private readonly System.Text.StringBuilder _stringBuilder = new System.Text.StringBuilder();
        private bool _isInitialized = false;

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前显示的文本
        /// </summary>
        public string CurrentText
        {
            get => _currentDisplayText;
            private set
            {
                _currentDisplayText = value;
                ApplyTextToComponent(value);
            }
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 更新次数
        /// </summary>
        public int UpdateCount => updateCount;

        /// <summary>
        /// 文本绑定配置
        /// </summary>
        public ReactiveTextBinding TextBinding => textBinding;

        /// <summary>
        /// 格式化字符串
        /// </summary>
        public string FormatString
        {
            get => formatString;
            set
            {
                formatString = value;
                RefreshText();
            }
        }

        #endregion

        #region 事件

        /// <summary>
        /// 文本更新前事件
        /// </summary>
        public event System.Action<string, string> OnTextChanging;

        /// <summary>
        /// 文本更新后事件
        /// </summary>
        public event System.Action<string> OnTextChanged;

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
        }

        private void Start()
        {
            InitializeBindings();
            
            if (updateOnStart)
            {
                RefreshText();
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
            if (autoFindTextComponent)
            {
                // 优先查找TextMeshPro组件
                _tmpComponent = GetComponent<TextMeshProUGUI>();
                if (_tmpComponent != null)
                {
                    _isTextMeshPro = true;
                }
                else
                {
                    // 查找标准Text组件
                    _textComponent = GetComponent<Text>();
                    _isTextMeshPro = false;
                }
            }

            // 设置组件名称
            if (string.IsNullOrEmpty(componentName))
            {
                componentName = gameObject.name + "_ReactiveText";
            }

            _isInitialized = true;

            if (enableDebugLog)
            {
                Debug.Log($"ReactiveText '{componentName}' 初始化完成，使用组件: {(_isTextMeshPro ? "TextMeshPro" : "Text")}");
            }
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
                Debug.LogWarning($"ReactiveText '{componentName}' 绑定配置无效");
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
                // 绑定主要文本数据
                if (textBinding.primaryData != null)
                {
                    var unbinder = BindToReactiveData("primary", textBinding.primaryData, RefreshText);
                    if (unbinder != null) _dataUnbinders.Add(unbinder);
                }

                // 绑定辅助数据
                for (int i = 0; i < textBinding.additionalData.Count; i++)
                {
                    var data = textBinding.additionalData[i];
                    if (data.reactiveData != null)
                    {
                        var unbinder = BindToReactiveData($"additional_{i}", data.reactiveData, RefreshText);
                        if (unbinder != null) _dataUnbinders.Add(unbinder);
                    }
                }

                // 绑定样式数据
                if (enableStyleBinding)
                {
                    BindStyleData();
                }

                OnBindingEstablished?.Invoke();

                if (enableDebugLog)
                {
                    Debug.Log($"ReactiveText '{componentName}' 建立了 {_dataUnbinders.Count} 个数据绑定");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ReactiveText '{componentName}' 建立绑定时发生错误: {ex.Message}");
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
            if (styleConfig.colorData != null)
            {
                var unbinder = BindToReactiveData("color", styleConfig.colorData, ApplyStyleChanges);
                if (unbinder != null) _dataUnbinders.Add(unbinder);
            }

            if (styleConfig.fontSizeData != null)
            {
                var unbinder = BindToReactiveData("fontSize", styleConfig.fontSizeData, ApplyStyleChanges);
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

        #region 文本更新方法

        /// <summary>
        /// 刷新文本显示
        /// </summary>
        public void RefreshText()
        {
            if (!_isInitialized) return;

            try
            {
                string newText = GenerateFormattedText();
                UpdateText(newText);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ReactiveText '{componentName}' 刷新文本时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成格式化文本
        /// </summary>
        /// <returns>格式化后的文本</returns>
        private string GenerateFormattedText()
        {
            _stringBuilder.Clear();

            try
            {
                // 收集所有绑定的值
                var values = new List<object>();

                // 添加主要数据
                if (textBinding.primaryData != null && _lastValues.ContainsKey("primary"))
                {
                    values.Add(_lastValues["primary"]);
                }

                // 添加辅助数据
                for (int i = 0; i < textBinding.additionalData.Count; i++)
                {
                    string key = $"additional_{i}";
                    if (_lastValues.ContainsKey(key))
                    {
                        values.Add(_lastValues[key]);
                    }
                }

                // 应用格式化
                string formattedText;
                if (values.Count == 0)
                {
                    formattedText = formatString;
                }
                else if (values.Count == 1)
                {
                    formattedText = ApplyFormatter(values[0]);
                }
                else
                {
                    formattedText = string.Format(formatString, values.ToArray());
                }

                // 应用本地化
                if (useLocalization && !string.IsNullOrEmpty(localizationKey))
                {
                    formattedText = ApplyLocalization(formattedText);
                }

                return formattedText;
            }
            catch (Exception ex)
            {
                Debug.LogError($"生成格式化文本时发生错误: {ex.Message}");
                return "格式化错误";
            }
        }

        /// <summary>
        /// 应用格式化器
        /// </summary>
        /// <param name="value">原始值</param>
        /// <returns>格式化后的文本</returns>
        private string ApplyFormatter(object value)
        {
            if (value == null) return "null";

            try
            {
                switch (formatter)
                {
                    case ReactiveTextFormatter.None:
                        return string.Format(formatString, value);

                    case ReactiveTextFormatter.Currency:
                        if (value is float f) return f.ToString("C2");
                        if (value is double d) return d.ToString("C2");
                        if (value is decimal dec) return dec.ToString("C2");
                        return value.ToString();

                    case ReactiveTextFormatter.Percentage:
                        if (value is float pf) return (pf * 100).ToString("F1") + "%";
                        if (value is double pd) return (pd * 100).ToString("F1") + "%";
                        return value.ToString();

                    case ReactiveTextFormatter.Integer:
                        if (value is float fi) return Mathf.RoundToInt(fi).ToString();
                        if (value is double di) return Math.Round(di).ToString();
                        return value.ToString();

                    case ReactiveTextFormatter.Decimal:
                        if (value is float fd) return fd.ToString("F2");
                        if (value is double dd) return dd.ToString("F2");
                        return value.ToString();

                    case ReactiveTextFormatter.Time:
                        if (value is float ft) return TimeSpan.FromSeconds(ft).ToString(@"mm\:ss");
                        if (value is double dt) return TimeSpan.FromSeconds(dt).ToString(@"mm\:ss");
                        if (value is TimeSpan ts) return ts.ToString(@"mm\:ss");
                        return value.ToString();

                    case ReactiveTextFormatter.Custom:
                        if (!string.IsNullOrEmpty(customFormat))
                        {
                            if (value is IFormattable formattable)
                                return formattable.ToString(customFormat, null);
                        }
                        return value.ToString();

                    default:
                        return string.Format(formatString, value);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"应用格式化器时发生错误: {ex.Message}");
                return value.ToString();
            }
        }

        /// <summary>
        /// 应用本地化
        /// </summary>
        /// <param name="text">原始文本</param>
        /// <returns>本地化后的文本</returns>
        private string ApplyLocalization(string text)
        {
            // 这里可以集成具体的本地化系统
            // 示例实现
            return text; // 暂时直接返回原始文本
        }

        /// <summary>
        /// 更新文本显示
        /// </summary>
        /// <param name="newText">新文本</param>
        private void UpdateText(string newText)
        {
            if (newText == _currentDisplayText) return;

            var oldText = _currentDisplayText;
            OnTextChanging?.Invoke(oldText, newText);

            if (enableAnimation && Application.isPlaying)
            {
                AnimateTextChange(newText);
            }
            else
            {
                CurrentText = newText;
                OnTextChanged?.Invoke(newText);
            }

            // 更新统计信息
            updateCount++;
            lastUpdateTime = Time.time;

            if (enableDebugLog)
            {
                Debug.Log($"ReactiveText '{componentName}' 文本更新: '{oldText}' -> '{newText}'");
            }
        }

        #endregion

        #region 动画方法

        /// <summary>
        /// 动画化文本变化
        /// </summary>
        /// <param name="targetText">目标文本</param>
        private void AnimateTextChange(string targetText)
        {
            _targetText = targetText;

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            _animationCoroutine = StartCoroutine(AnimateTextCoroutine());
        }

        /// <summary>
        /// 文本动画协程
        /// </summary>
        private System.Collections.IEnumerator AnimateTextCoroutine()
        {
            string startText = _currentDisplayText;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / animationDuration;
                float easedProgress = animationCurve.Evaluate(progress);

                string animatedText = InterpolateText(startText, _targetText, easedProgress);
                ApplyTextToComponent(animatedText);

                yield return null;
            }

            // 确保最终文本正确
            CurrentText = _targetText;
            OnTextChanged?.Invoke(_targetText);
            
            _animationCoroutine = null;
        }

        /// <summary>
        /// 插值文本
        /// </summary>
        /// <param name="startText">起始文本</param>
        /// <param name="endText">结束文本</param>
        /// <param name="progress">进度(0-1)</param>
        /// <returns>插值后的文本</returns>
        private string InterpolateText(string startText, string endText, float progress)
        {
            switch (animationType)
            {
                case ReactiveTextAnimation.Fade:
                    // 透明度变化（通过颜色alpha实现）
                    ApplyAlpha(progress);
                    return endText;

                case ReactiveTextAnimation.TypeWriter:
                    // 打字机效果
                    int targetLength = Mathf.RoundToInt(endText.Length * progress);
                    return endText.Substring(0, Mathf.Min(targetLength, endText.Length));

                case ReactiveTextAnimation.CountUp:
                    // 数字递增效果
                    return InterpolateNumericText(startText, endText, progress);

                case ReactiveTextAnimation.Scale:
                    // 缩放效果（通过Transform实现）
                    ApplyScale(progress);
                    return endText;

                default:
                    return endText;
            }
        }

        /// <summary>
        /// 插值数字文本
        /// </summary>
        /// <param name="startText">起始文本</param>
        /// <param name="endText">结束文本</param>
        /// <param name="progress">进度</param>
        /// <returns>插值后的数字文本</returns>
        private string InterpolateNumericText(string startText, string endText, float progress)
        {
            if (float.TryParse(startText, out float startValue) && 
                float.TryParse(endText, out float endValue))
            {
                float currentValue = Mathf.Lerp(startValue, endValue, progress);
                return ApplyFormatter(currentValue);
            }

            return endText;
        }

        /// <summary>
        /// 应用透明度
        /// </summary>
        /// <param name="alpha">透明度</param>
        private void ApplyAlpha(float alpha)
        {
            if (_isTextMeshPro && _tmpComponent != null)
            {
                var color = _tmpComponent.color;
                color.a = alpha;
                _tmpComponent.color = color;
            }
            else if (_textComponent != null)
            {
                var color = _textComponent.color;
                color.a = alpha;
                _textComponent.color = color;
            }
        }

        /// <summary>
        /// 应用缩放
        /// </summary>
        /// <param name="scale">缩放值</param>
        private void ApplyScale(float scale)
        {
            transform.localScale = Vector3.one * scale;
        }

        #endregion

        #region 样式应用方法

        /// <summary>
        /// 应用样式变化
        /// </summary>
        private void ApplyStyleChanges()
        {
            if (!enableStyleBinding) return;

            try
            {
                // 应用颜色
                if (styleConfig.colorData != null && _lastValues.ContainsKey("color"))
                {
                    if (_lastValues["color"] is Color color)
                    {
                        ApplyColor(color);
                    }
                }

                // 应用字体大小
                if (styleConfig.fontSizeData != null && _lastValues.ContainsKey("fontSize"))
                {
                    if (_lastValues["fontSize"] is int fontSize)
                    {
                        ApplyFontSize(fontSize);
                    }
                    else if (_lastValues["fontSize"] is float fontSizeF)
                    {
                        ApplyFontSize(Mathf.RoundToInt(fontSizeF));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"应用样式变化时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用颜色
        /// </summary>
        /// <param name="color">颜色</param>
        private void ApplyColor(Color color)
        {
            if (_isTextMeshPro && _tmpComponent != null)
            {
                _tmpComponent.color = color;
            }
            else if (_textComponent != null)
            {
                _textComponent.color = color;
            }
        }

        /// <summary>
        /// 应用字体大小
        /// </summary>
        /// <param name="fontSize">字体大小</param>
        private void ApplyFontSize(int fontSize)
        {
            if (_isTextMeshPro && _tmpComponent != null)
            {
                _tmpComponent.fontSize = fontSize;
            }
            else if (_textComponent != null)
            {
                _textComponent.fontSize = fontSize;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 应用文本到组件
        /// </summary>
        /// <param name="text">文本内容</param>
        private void ApplyTextToComponent(string text)
        {
            if (_isTextMeshPro && _tmpComponent != null)
            {
                _tmpComponent.text = text;
            }
            else if (_textComponent != null)
            {
                _textComponent.text = text;
            }
        }

        /// <summary>
        /// 验证绑定配置
        /// </summary>
        /// <returns>是否有效</returns>
        private bool ValidateBindingConfiguration()
        {
            // 检查是否有有效的UI组件
            if (!_isTextMeshPro && _textComponent == null && _tmpComponent == null)
            {
                Debug.LogError($"ReactiveText '{componentName}' 未找到有效的Text或TextMeshPro组件");
                return false;
            }

            // 检查是否有绑定数据
            if (textBinding.primaryData == null && textBinding.additionalData.Count == 0)
            {
                Debug.LogWarning($"ReactiveText '{componentName}' 没有绑定任何响应式数据");
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
            
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            _stringBuilder.Clear();
        }

        #endregion

        #region 公共接口方法

        /// <summary>
        /// 手动设置文本（不触发绑定）
        /// </summary>
        /// <param name="text">文本内容</param>
        public void SetTextDirect(string text)
        {
            CurrentText = text;
        }

        /// <summary>
        /// 重新建立绑定
        /// </summary>
        public void RebindData()
        {
            EstablishBindings();
            RefreshText();
        }

        /// <summary>
        /// 获取当前绑定状态
        /// </summary>
        /// <returns>绑定状态信息</returns>
        public string GetBindingStatus()
        {
            return $"绑定数量: {_dataUnbinders.Count}, 更新次数: {updateCount}, 最后更新: {lastUpdateTime:F2}s";
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 响应式文本绑定配置
    /// </summary>
    [System.Serializable]
    public class ReactiveTextBinding
    {
        [Header("主要数据绑定")]
        [Tooltip("主要的响应式数据源")]
        public object primaryData;

        [Header("附加数据绑定")]
        [Tooltip("附加的响应式数据源列表")]
        public List<ReactiveDataBinding> additionalData = new List<ReactiveDataBinding>();
    }

    /// <summary>
    /// 响应式数据绑定项
    /// </summary>
    [System.Serializable]
    public class ReactiveDataBinding
    {
        [Tooltip("数据绑定名称")]
        public string name;
        
        [Tooltip("响应式数据")]
        public object reactiveData;
        
        [Tooltip("数据处理函数")]
        public string processorFunction;
    }

    /// <summary>
    /// 响应式文本样式配置
    /// </summary>
    [System.Serializable]
    public class ReactiveTextStyle
    {
        [Header("颜色绑定")]
        [Tooltip("颜色响应式数据")]
        public object colorData;

        [Header("字体大小绑定")]
        [Tooltip("字体大小响应式数据")]
        public object fontSizeData;

        [Header("字体样式绑定")]
        [Tooltip("是否启用粗体绑定")]
        public bool enableBoldBinding;
        
        [Tooltip("粗体响应式数据")]
        public object boldData;
    }

    /// <summary>
    /// 响应式文本格式化器枚举
    /// </summary>
    public enum ReactiveTextFormatter
    {
        None,           // 无格式化
        Currency,       // 货币格式
        Percentage,     // 百分比格式
        Integer,        // 整数格式
        Decimal,        // 小数格式
        Time,           // 时间格式
        Custom          // 自定义格式
    }

    /// <summary>
    /// 响应式文本动画类型枚举
    /// </summary>
    public enum ReactiveTextAnimation
    {
        None,           // 无动画
        Fade,           // 淡入淡出
        TypeWriter,     // 打字机效果
        CountUp,        // 数字递增
        Scale,          // 缩放效果
        Slide           // 滑动效果
    }

    /// <summary>
    /// 响应式组件接口
    /// </summary>
    public interface IReactiveComponent
    {
        bool IsInitialized { get; }
        void RebindData();
        string GetBindingStatus();
    }

    #endregion
}