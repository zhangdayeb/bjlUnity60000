// Assets/UI/Framework/UIUpdateManager.cs
// UI更新管理器 - 统一管理UI组件的数据绑定和更新
// 类似Vue的响应式系统，统一管理所有UI组件的数据绑定

using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Architecture;
using System.Linq;

namespace UI.Framework
{
    /// <summary>
    /// UI更新管理器 - 单例模式
    /// 统一管理所有响应式UI组件和数据绑定
    /// </summary>
    public class UIUpdateManager : MonoBehaviour
    {
        #region 单例模式

        private static UIUpdateManager _instance;
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static UIUpdateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIUpdateManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIUpdateManager");
                        _instance = go.AddComponent<UIUpdateManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        [Header("管理器配置")]
        [Tooltip("是否启用调试模式")]
        [SerializeField] private bool _enableDebugMode = false;
        
        [Tooltip("更新频率（FPS）")]
        [SerializeField] private float _updateFrequency = 60f;
        
        [Tooltip("是否自动清理无效组件")]
        [SerializeField] private bool _autoCleanupInvalidComponents = true;

        [Header("性能监控")]
        [Tooltip("最大每帧更新组件数")]
        [SerializeField] private int _maxUpdatesPerFrame = 10;
        
        [Tooltip("是否启用性能监控")]
        [SerializeField] private bool _enablePerformanceMonitoring = true;
        
        [SerializeField] private int _totalRegisteredComponents = 0;
        [SerializeField] private int _activeBindingCount = 0;
        [SerializeField] private float _lastUpdateTime = 0f;

        // 响应式数据存储
        private Dictionary<string, object> _reactiveDataStore = new Dictionary<string, object>();
        
        // 已注册的响应式组件
        private HashSet<MonoBehaviour> _registeredComponents = new HashSet<MonoBehaviour>();
        
        // 数据绑定映射
        private Dictionary<string, List<IDataBinding>> _dataBindings = new Dictionary<string, List<IDataBinding>>();
        
        // 更新队列
        private Queue<IDataBinding> _updateQueue = new Queue<IDataBinding>();
        
        // 性能统计
        private Dictionary<string, PerformanceStats> _performanceStats = new Dictionary<string, PerformanceStats>();
        
        // 清理队列
        private List<MonoBehaviour> _cleanupQueue = new List<MonoBehaviour>();

        // 更新计时器
        private float _updateTimer = 0f;
        private float _cleanupTimer = 0f;
        private const float CLEANUP_INTERVAL = 5f; // 5秒清理一次

        #region Unity生命周期

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            _updateTimer += Time.deltaTime;
            _cleanupTimer += Time.deltaTime;

            // 处理更新队列
            if (_updateTimer >= 1f / _updateFrequency)
            {
                ProcessUpdateQueue();
                _updateTimer = 0f;
                _lastUpdateTime = Time.time;
            }

            // 定期清理无效组件
            if (_autoCleanupInvalidComponents && _cleanupTimer >= CLEANUP_INTERVAL)
            {
                PerformCleanup();
                _cleanupTimer = 0f;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                CleanupAllBindings();
                _instance = null;
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化管理器
        /// </summary>
        private void InitializeManager()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[UIUpdateManager] 初始化UI更新管理器");
            }

            // 创建性能统计
            _performanceStats["TotalUpdates"] = new PerformanceStats();
            _performanceStats["DataBindings"] = new PerformanceStats();
            _performanceStats["ComponentRegistrations"] = new PerformanceStats();
        }

        #endregion

        #region 响应式数据管理

        /// <summary>
        /// 创建或获取响应式数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键名</param>
        /// <param name="initialValue">初始值</param>
        /// <returns>响应式数据</returns>
        public ReactiveData<T> GetOrCreateReactiveData<T>(string key, T initialValue = default(T))
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[UIUpdateManager] 数据键名不能为空");
                return null;
            }

            if (!_reactiveDataStore.ContainsKey(key))
            {
                var data = new ReactiveData<T>(initialValue, key);
                _reactiveDataStore[key] = data;
                
                if (_enableDebugMode)
                {
                    Debug.Log($"[UIUpdateManager] 创建响应式数据: {key} = {initialValue}");
                }
            }

            return _reactiveDataStore[key] as ReactiveData<T>;
        }

        /// <summary>
        /// 获取响应式数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键名</param>
        /// <returns>响应式数据</returns>
        public ReactiveData<T> GetReactiveData<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            _reactiveDataStore.TryGetValue(key, out object data);
            return data as ReactiveData<T>;
        }

        /// <summary>
        /// 设置响应式数据值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键名</param>
        /// <param name="value">新值</param>
        public void SetReactiveValue<T>(string key, T value)
        {
            var data = GetReactiveData<T>(key);
            if (data != null)
            {
                data.Value = value;
                
                if (_enableDebugMode)
                {
                    Debug.Log($"[UIUpdateManager] 更新响应式数据: {key} = {value}");
                }
            }
            else
            {
                // 如果不存在，创建新的
                data = GetOrCreateReactiveData(key, value);
            }
        }

        /// <summary>
        /// 获取响应式数据值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>数据值</returns>
        public T GetReactiveValue<T>(string key, T defaultValue = default(T))
        {
            var data = GetReactiveData<T>(key);
            return data != null && data.HasValue ? data.Value : defaultValue;
        }

        /// <summary>
        /// 移除响应式数据
        /// </summary>
        /// <param name="key">数据键名</param>
        public void RemoveReactiveData(string key)
        {
            if (_reactiveDataStore.ContainsKey(key))
            {
                _reactiveDataStore.Remove(key);
                
                // 清理相关绑定
                if (_dataBindings.ContainsKey(key))
                {
                    _dataBindings.Remove(key);
                }
                
                if (_enableDebugMode)
                {
                    Debug.Log($"[UIUpdateManager] 移除响应式数据: {key}");
                }
            }
        }

        #endregion

        #region 组件管理

        /// <summary>
        /// 注册响应式组件
        /// </summary>
        /// <param name="component">组件</param>
        public void RegisterReactiveComponent(MonoBehaviour component)
        {
            if (component == null) return;

            if (_registeredComponents.Add(component))
            {
                _totalRegisteredComponents = _registeredComponents.Count;
                
                if (_enableDebugMode)
                {
                    Debug.Log($"[UIUpdateManager] 注册响应式组件: {component.GetType().Name} ({component.gameObject.name})");
                }

                // 记录性能统计
                if (_enablePerformanceMonitoring)
                {
                    _performanceStats["ComponentRegistrations"].RecordOperation();
                }
            }
        }

        /// <summary>
        /// 注销响应式组件
        /// </summary>
        /// <param name="component">组件</param>
        public void UnregisterReactiveComponent(MonoBehaviour component)
        {
            if (component == null) return;

            if (_registeredComponents.Remove(component))
            {
                _totalRegisteredComponents = _registeredComponents.Count;
                
                // 清理相关绑定
                CleanupComponentBindings(component);
                
                if (_enableDebugMode)
                {
                    Debug.Log($"[UIUpdateManager] 注销响应式组件: {component.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// 清理组件绑定
        /// </summary>
        /// <param name="component">组件</param>
        private void CleanupComponentBindings(MonoBehaviour component)
        {
            foreach (var bindingList in _dataBindings.Values)
            {
                bindingList.RemoveAll(binding => binding.Component == component);
            }
            
            _activeBindingCount = _dataBindings.Values.Sum(list => list.Count);
        }

        #endregion

        #region 数据绑定管理

        /// <summary>
        /// 创建数据绑定
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="dataKey">数据键名</param>
        /// <param name="component">绑定的组件</param>
        /// <param name="updateCallback">更新回调</param>
        /// <returns>绑定ID</returns>
        public string CreateDataBinding<T>(string dataKey, MonoBehaviour component, Action<T> updateCallback)
        {
            if (string.IsNullOrEmpty(dataKey) || component == null || updateCallback == null)
                return null;

            var binding = new DataBinding<T>
            {
                DataKey = dataKey,
                Component = component,
                UpdateCallback = updateCallback,
                BindingId = Guid.NewGuid().ToString()
            };

            if (!_dataBindings.ContainsKey(dataKey))
            {
                _dataBindings[dataKey] = new List<IDataBinding>();
            }

            _dataBindings[dataKey].Add(binding);
            _activeBindingCount = _dataBindings.Values.Sum(list => list.Count);

            // 立即更新一次
            var data = GetReactiveData<T>(dataKey);
            if (data != null && data.HasValue)
            {
                updateCallback.Invoke(data.Value);
            }

            if (_enableDebugMode)
            {
                Debug.Log($"[UIUpdateManager] 创建数据绑定: {dataKey} -> {component.GetType().Name}");
            }

            // 记录性能统计
            if (_enablePerformanceMonitoring)
            {
                _performanceStats["DataBindings"].RecordOperation();
            }

            return binding.BindingId;
        }

        /// <summary>
        /// 移除数据绑定
        /// </summary>
        /// <param name="bindingId">绑定ID</param>
        public void RemoveDataBinding(string bindingId)
        {
            if (string.IsNullOrEmpty(bindingId)) return;

            foreach (var bindingList in _dataBindings.Values)
            {
                bindingList.RemoveAll(binding => binding.BindingId == bindingId);
            }
            
            _activeBindingCount = _dataBindings.Values.Sum(list => list.Count);
        }

        #endregion

        #region 更新处理

        /// <summary>
        /// 添加到更新队列
        /// </summary>
        /// <param name="binding">数据绑定</param>
        private void AddToUpdateQueue(IDataBinding binding)
        {
            _updateQueue.Enqueue(binding);
        }

        /// <summary>
        /// 处理更新队列
        /// </summary>
        private void ProcessUpdateQueue()
        {
            int processedCount = 0;
            
            while (_updateQueue.Count > 0 && processedCount < _maxUpdatesPerFrame)
            {
                var binding = _updateQueue.Dequeue();
                
                try
                {
                    binding.UpdateFromData();
                    processedCount++;
                    
                    // 记录性能统计
                    if (_enablePerformanceMonitoring)
                    {
                        _performanceStats["TotalUpdates"].RecordOperation();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UIUpdateManager] 处理数据绑定更新时发生错误: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 强制更新所有绑定
        /// </summary>
        public void ForceUpdateAllBindings()
        {
            foreach (var bindingList in _dataBindings.Values)
            {
                foreach (var binding in bindingList)
                {
                    AddToUpdateQueue(binding);
                }
            }
        }

        /// <summary>
        /// 强制更新指定数据的绑定
        /// </summary>
        /// <param name="dataKey">数据键名</param>
        public void ForceUpdateDataBindings(string dataKey)
        {
            if (_dataBindings.ContainsKey(dataKey))
            {
                foreach (var binding in _dataBindings[dataKey])
                {
                    AddToUpdateQueue(binding);
                }
            }
        }

        #endregion

        #region 清理和维护

        /// <summary>
        /// 执行清理操作
        /// </summary>
        private void PerformCleanup()
        {
            _cleanupQueue.Clear();
            
            // 检查无效组件
            foreach (var component in _registeredComponents)
            {
                if (component == null)
                {
                    _cleanupQueue.Add(component);
                }
            }
            
            // 移除无效组件
            foreach (var invalidComponent in _cleanupQueue)
            {
                _registeredComponents.Remove(invalidComponent);
            }
            
            // 清理无效绑定
            foreach (var bindingList in _dataBindings.Values)
            {
                bindingList.RemoveAll(binding => binding.Component == null);
            }
            
            _totalRegisteredComponents = _registeredComponents.Count;
            _activeBindingCount = _dataBindings.Values.Sum(list => list.Count);
            
            if (_enableDebugMode && _cleanupQueue.Count > 0)
            {
                Debug.Log($"[UIUpdateManager] 清理了 {_cleanupQueue.Count} 个无效组件");
            }
        }

        /// <summary>
        /// 清理所有绑定
        /// </summary>
        private void CleanupAllBindings()
        {
            _dataBindings.Clear();
            _registeredComponents.Clear();
            _updateQueue.Clear();
            _reactiveDataStore.Clear();
            
            _totalRegisteredComponents = 0;
            _activeBindingCount = 0;
        }

        #endregion

        #region 调试和监控

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        /// <returns>性能统计</returns>
        public Dictionary<string, PerformanceStats> GetPerformanceStats()
        {
            return new Dictionary<string, PerformanceStats>(_performanceStats);
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        /// <returns>调试信息</returns>
        public string GetDebugInfo()
        {
            var info = $"=== UI更新管理器调试信息 ===\n";
            info += $"已注册组件数: {_totalRegisteredComponents}\n";
            info += $"活跃绑定数: {_activeBindingCount}\n";
            info += $"响应式数据数: {_reactiveDataStore.Count}\n";
            info += $"更新队列长度: {_updateQueue.Count}\n";
            info += $"最后更新时间: {_lastUpdateTime:F2}\n";
            
            if (_enablePerformanceMonitoring)
            {
                info += "\n=== 性能统计 ===\n";
                foreach (var stat in _performanceStats)
                {
                    info += $"{stat.Key}: {stat.Value}\n";
                }
            }
            
            return info;
        }

        /// <summary>
        /// 打印调试信息
        /// </summary>
        [ContextMenu("打印调试信息")]
        public void PrintDebugInfo()
        {
            Debug.Log(GetDebugInfo());
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 数据绑定接口
        /// </summary>
        private interface IDataBinding
        {
            string DataKey { get; }
            string BindingId { get; }
            MonoBehaviour Component { get; }
            void UpdateFromData();
        }

        /// <summary>
        /// 泛型数据绑定实现
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        private class DataBinding<T> : IDataBinding
        {
            public string DataKey { get; set; }
            public string BindingId { get; set; }
            public MonoBehaviour Component { get; set; }
            public Action<T> UpdateCallback { get; set; }

            public void UpdateFromData()
            {
                if (Component == null || UpdateCallback == null) return;

                var data = Instance.GetReactiveData<T>(DataKey);
                if (data != null && data.HasValue)
                {
                    UpdateCallback.Invoke(data.Value);
                }
            }
        }

        /// <summary>
        /// 性能统计
        /// </summary>
        [System.Serializable]
        public class PerformanceStats
        {
            public int operationCount = 0;
            public float totalTime = 0f;
            public float averageTime => operationCount > 0 ? totalTime / operationCount : 0f;
            public float lastOperationTime = 0f;

            public void RecordOperation()
            {
                operationCount++;
                lastOperationTime = Time.time;
                // 这里可以添加更详细的性能记录
            }

            public override string ToString()
            {
                return $"操作次数: {operationCount}, 平均时间: {averageTime:F4}s, 最后操作: {lastOperationTime:F2}s";
            }
        }

        #endregion
    }
}