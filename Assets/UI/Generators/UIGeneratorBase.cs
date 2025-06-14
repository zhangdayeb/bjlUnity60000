// Assets/UI/Generators/UIGeneratorBase.cs
// UI生成器基类 - 为所有UI生成器提供统一的基础功能
// 实现模块化UI生成，支持不同设备适配和动态布局

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI.Framework;
using Core.Architecture;

namespace UI.Generators
{
    /// <summary>
    /// UI生成器基类
    /// 提供UI动态生成的核心功能和模板方法
    /// </summary>
    public abstract class UIGeneratorBase : MonoBehaviour
    {
        #region 保护字段

        [Header("生成器配置")]
        [Tooltip("生成器名称")]
        [SerializeField] protected string _generatorName = "";
        
        [Tooltip("是否启用调试模式")]
        [SerializeField] protected bool _enableDebugMode = false;
        
        [Tooltip("是否自动适配屏幕")]
        [SerializeField] protected bool _autoScreenAdaptation = true;

        [Header("预制体资源")]
        [Tooltip("预制体资源字典")]
        [SerializeField] protected PrefabResourceMap[] _prefabResources = new PrefabResourceMap[0];
        
        [Tooltip("默认预制体文件夹路径")]
        [SerializeField] protected string _defaultPrefabPath = "UI/Prefabs/";

        [Header("布局配置")]
        [Tooltip("根容器")]
        [SerializeField] protected RectTransform _rootContainer;
        
        [Tooltip("布局网格设置")]
        [SerializeField] protected LayoutGridSettings _layoutSettings = new LayoutGridSettings();

        [Header("动画配置")]
        [Tooltip("生成动画时间")]
        [SerializeField] protected float _generationAnimationDuration = 0.5f;
        
        [Tooltip("动画曲线")]
        [SerializeField] protected AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // 内部状态
        protected Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        protected Dictionary<string, List<GameObject>> _generatedObjects = new Dictionary<string, List<GameObject>>();
        protected List<IDisposable> _disposables = new List<IDisposable>();
        protected Canvas _targetCanvas;
        protected CanvasScaler _canvasScaler;
        protected bool _isInitialized = false;
        protected bool _isGenerating = false;

        #endregion

        #region 生命周期

        protected virtual void Awake()
        {
            InitializeGenerator();
        }

        protected virtual void Start()
        {
            PostInitializeSetup();
        }

        protected virtual void OnDestroy()
        {
            CleanupGenerator();
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(_generatorName))
            {
                _generatorName = GetType().Name;
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化生成器
        /// </summary>
        protected virtual void InitializeGenerator()
        {
            if (_isInitialized) return;

            try
            {
                // 查找画布和根容器
                FindCanvasAndContainer();
                
                // 初始化预制体缓存
                InitializePrefabCache();
                
                // 注册到UI管理器
                RegisterToUIManager();
                
                _isInitialized = true;
                
                if (_enableDebugMode)
                {
                    Debug.Log($"[{_generatorName}] 生成器初始化完成");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{_generatorName}] 初始化失败: {e.Message}");
            }
        }

        /// <summary>
        /// 查找画布和容器
        /// </summary>
        private void FindCanvasAndContainer()
        {
            // 查找目标画布
            if (_targetCanvas == null)
            {
                _targetCanvas = GetComponentInParent<Canvas>();
                if (_targetCanvas == null)
                {
                    _targetCanvas = FindObjectOfType<Canvas>();
                }
            }

            if (_targetCanvas != null)
            {
                _canvasScaler = _targetCanvas.GetComponent<CanvasScaler>();
            }

            // 设置根容器
            if (_rootContainer == null)
            {
                _rootContainer = GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// 初始化预制体缓存
        /// </summary>
        private void InitializePrefabCache()
        {
            foreach (var resource in _prefabResources)
            {
                if (!string.IsNullOrEmpty(resource.key) && resource.prefab != null)
                {
                    _prefabCache[resource.key] = resource.prefab;
                }
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
        /// 后初始化设置
        /// </summary>
        protected virtual void PostInitializeSetup()
        {
            // 子类可以重写此方法进行额外设置
        }

        #endregion

        #region 抽象方法 - 子类必须实现

        /// <summary>
        /// 生成UI - 子类必须实现的核心方法
        /// </summary>
        public abstract void GenerateUI();

        /// <summary>
        /// 清理UI - 子类必须实现
        /// </summary>
        public abstract void ClearUI();

        /// <summary>
        /// 获取支持的生成类型 - 子类必须实现
        /// </summary>
        /// <returns>支持的类型列表</returns>
        protected abstract List<string> GetSupportedGenerationTypes();

        #endregion

        #region 公共方法

        /// <summary>
        /// 生成指定类型的UI
        /// </summary>
        /// <param name="generationType">生成类型</param>
        /// <param name="parameters">生成参数</param>
        public virtual void GenerateUI(string generationType, Dictionary<string, object> parameters = null)
        {
            if (_isGenerating)
            {
                Debug.LogWarning($"[{_generatorName}] 正在生成UI，请稍候...");
                return;
            }

            try
            {
                _isGenerating = true;
                
                if (!_isInitialized)
                {
                    InitializeGenerator();
                }

                if (!GetSupportedGenerationTypes().Contains(generationType))
                {
                    Debug.LogError($"[{_generatorName}] 不支持的生成类型: {generationType}");
                    return;
                }

                if (_enableDebugMode)
                {
                    Debug.Log($"[{_generatorName}] 开始生成UI类型: {generationType}");
                }

                // 调用具体的生成方法
                OnGenerateSpecificUI(generationType, parameters);
                
                if (_enableDebugMode)
                {
                    Debug.Log($"[{_generatorName}] UI生成完成: {generationType}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{_generatorName}] 生成UI时发生错误: {e.Message}");
            }
            finally
            {
                _isGenerating = false;
            }
        }

        /// <summary>
        /// 清理指定类型的UI
        /// </summary>
        /// <param name="generationType">要清理的类型</param>
        public virtual void ClearUI(string generationType)
        {
            if (_generatedObjects.ContainsKey(generationType))
            {
                var objects = _generatedObjects[generationType];
                foreach (var obj in objects)
                {
                    if (obj != null)
                    {
                        DestroyImmediate(obj);
                    }
                }
                objects.Clear();
                
                if (_enableDebugMode)
                {
                    Debug.Log($"[{_generatorName}] 已清理UI类型: {generationType}");
                }
            }
        }

        /// <summary>
        /// 重新生成所有UI
        /// </summary>
        public virtual void RegenerateAllUI()
        {
            ClearUI();
            GenerateUI();
        }

        #endregion

        #region 保护方法 - 供子类使用

        /// <summary>
        /// 处理特定类型UI生成 - 子类重写
        /// </summary>
        /// <param name="generationType">生成类型</param>
        /// <param name="parameters">参数</param>
        protected virtual void OnGenerateSpecificUI(string generationType, Dictionary<string, object> parameters)
        {
            // 默认调用无参数的GenerateUI
            GenerateUI();
        }

        /// <summary>
        /// 创建UI对象
        /// </summary>
        /// <param name="prefabKey">预制体键名</param>
        /// <param name="parent">父对象</param>
        /// <param name="category">分类（用于管理）</param>
        /// <returns>创建的对象</returns>
        protected GameObject CreateUIObject(string prefabKey, Transform parent = null, string category = "default")
        {
            GameObject prefab = GetPrefab(prefabKey);
            if (prefab == null)
            {
                Debug.LogError($"[{_generatorName}] 未找到预制体: {prefabKey}");
                return null;
            }

            Transform actualParent = parent ?? _rootContainer;
            GameObject instance = Instantiate(prefab, actualParent);
            
            // 添加到管理列表
            if (!_generatedObjects.ContainsKey(category))
            {
                _generatedObjects[category] = new List<GameObject>();
            }
            _generatedObjects[category].Add(instance);

            return instance;
        }

        /// <summary>
        /// 创建基础UI组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="name">对象名称</param>
        /// <param name="parent">父对象</param>
        /// <param name="category">分类</param>
        /// <returns>创建的组件</returns>
        protected T CreateUIComponent<T>(string name, Transform parent = null, string category = "default") where T : Component
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(T));
            Transform actualParent = parent ?? _rootContainer;
            obj.transform.SetParent(actualParent, false);
            
            // 添加到管理列表
            if (!_generatedObjects.ContainsKey(category))
            {
                _generatedObjects[category] = new List<GameObject>();
            }
            _generatedObjects[category].Add(obj);

            return obj.GetComponent<T>();
        }

        /// <summary>
        /// 获取预制体
        /// </summary>
        /// <param name="key">预制体键名</param>
        /// <returns>预制体对象</returns>
        protected GameObject GetPrefab(string key)
        {
            if (_prefabCache.ContainsKey(key))
            {
                return _prefabCache[key];
            }

            // 尝试从Resources加载
            string path = _defaultPrefabPath + key;
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                _prefabCache[key] = prefab;
                return prefab;
            }

            return null;
        }

        /// <summary>
        /// 设置RectTransform属性
        /// </summary>
        /// <param name="rectTransform">目标RectTransform</param>
        /// <param name="anchor">锚点设置</param>
        /// <param name="position">位置</param>
        /// <param name="size">大小</param>
        protected void SetRectTransform(RectTransform rectTransform, AnchorSettings anchor, Vector2 position, Vector2 size)
        {
            rectTransform.anchorMin = anchor.min;
            rectTransform.anchorMax = anchor.max;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        /// <summary>
        /// 应用布局组
        /// </summary>
        /// <param name="parent">父对象</param>
        /// <param name="layoutType">布局类型</param>
        /// <param name="spacing">间距</param>
        protected void ApplyLayoutGroup(RectTransform parent, LayoutType layoutType, Vector2 spacing)
        {
            switch (layoutType)
            {
                case LayoutType.Horizontal:
                    var hLayout = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
                    hLayout.spacing = spacing.x;
                    hLayout.childControlWidth = true;
                    hLayout.childControlHeight = true;
                    break;
                    
                case LayoutType.Vertical:
                    var vLayout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
                    vLayout.spacing = spacing.y;
                    vLayout.childControlWidth = true;
                    vLayout.childControlHeight = true;
                    break;
                    
                case LayoutType.Grid:
                    var gLayout = parent.gameObject.AddComponent<GridLayoutGroup>();
                    gLayout.spacing = spacing;
                    gLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    gLayout.constraintCount = _layoutSettings.gridColumns;
                    break;
            }
        }

        /// <summary>
        /// 计算屏幕适配尺寸
        /// </summary>
        /// <param name="baseSize">基础尺寸</param>
        /// <returns>适配后的尺寸</returns>
        protected Vector2 CalculateAdaptedSize(Vector2 baseSize)
        {
            if (!_autoScreenAdaptation || _canvasScaler == null) 
                return baseSize;

            float scaleFactor = _canvasScaler.scaleFactor;
            return baseSize * scaleFactor;
        }

        /// <summary>
        /// 添加响应式绑定
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="component">目标组件</param>
        /// <param name="dataKey">数据键</param>
        /// <param name="updateAction">更新动作</param>
        protected void AddReactiveBinding<T>(MonoBehaviour component, string dataKey, Action<T> updateAction)
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager != null)
            {
                string bindingId = uiManager.CreateDataBinding(dataKey, component, updateAction);
                // 这里可以保存bindingId用于清理
            }
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清理生成器
        /// </summary>
        private void CleanupGenerator()
        {
            // 清理所有生成的对象
            foreach (var category in _generatedObjects.Values)
            {
                foreach (var obj in category)
                {
                    if (obj != null)
                    {
                        DestroyImmediate(obj);
                    }
                }
                category.Clear();
            }
            _generatedObjects.Clear();

            // 清理响应式绑定
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();

            // 从UI管理器注销
            var uiManager = UIUpdateManager.Instance;
            if (uiManager != null)
            {
                uiManager.UnregisterReactiveComponent(this);
            }

            if (_enableDebugMode)
            {
                Debug.Log($"[{_generatorName}] 生成器已清理");
            }
        }

        #endregion

        #region 内部数据结构

        /// <summary>
        /// 预制体资源映射
        /// </summary>
        [System.Serializable]
        public class PrefabResourceMap
        {
            public string key;
            public GameObject prefab;
        }

        /// <summary>
        /// 锚点设置
        /// </summary>
        [System.Serializable]
        public class AnchorSettings
        {
            public Vector2 min = new Vector2(0, 0);
            public Vector2 max = new Vector2(1, 1);
        }

        /// <summary>
        /// 布局网格设置
        /// </summary>
        [System.Serializable]
        public class LayoutGridSettings
        {
            [Header("网格设置")]
            public int gridColumns = 3;
            public int gridRows = 3;
            public Vector2 cellSize = new Vector2(100, 100);
            public Vector2 spacing = new Vector2(10, 10);
            
            [Header("边距")]
            public RectOffset padding = new RectOffset(10, 10, 10, 10);
        }

        /// <summary>
        /// 布局类型
        /// </summary>
        public enum LayoutType
        {
            None,
            Horizontal,
            Vertical,
            Grid
        }

        #endregion

        #region 编辑器支持

#if UNITY_EDITOR
        [ContextMenu("重新生成UI")]
        private void EditorRegenerateUI()
        {
            ClearUI();
            GenerateUI();
        }

        [ContextMenu("清理所有UI")]
        private void EditorClearAllUI()
        {
            ClearUI();
        }

        [ContextMenu("打印调试信息")]
        private void EditorPrintDebugInfo()
        {
            Debug.Log($"=== {_generatorName} 调试信息 ===");
            Debug.Log($"已初始化: {_isInitialized}");
            Debug.Log($"正在生成: {_isGenerating}");
            Debug.Log($"预制体缓存数量: {_prefabCache.Count}");
            Debug.Log($"生成对象分类数: {_generatedObjects.Count}");
            
            foreach (var category in _generatedObjects)
            {
                Debug.Log($"分类 '{category.Key}': {category.Value.Count} 个对象");
            }
        }
#endif

        #endregion
    }
}