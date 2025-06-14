// Assets/_Core/Architecture/ServiceLocator.cs
// 服务定位器 - 管理依赖注入，类似于Vue的provide/inject或Angular的DI系统

using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Data.Types;

namespace Core.Architecture
{
    /// <summary>
    /// 服务定位器 - 管理全局服务的注册、获取和生命周期
    /// 支持接口和实现的解耦，方便Mock和真实服务的切换
    /// </summary>
    public class ServiceLocator : MonoBehaviour
    {
        [Header("服务定位器配置")]
        [Tooltip("是否在场景切换时保持")]
        public bool dontDestroyOnLoad = true;
        
        [Tooltip("是否启用调试日志")]
        public bool enableDebugLogs = true;
        
        [Tooltip("当前环境类型")]
        public EnvironmentType currentEnvironment = EnvironmentType.Development;

        [Header("服务状态")]
        [SerializeField] private int registeredServiceCount = 0;
        [SerializeField] private List<string> serviceNames = new List<string>();

        // 单例实例
        private static ServiceLocator _instance;
        
        // 服务容器
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<object>> _serviceFactories = new Dictionary<Type, Func<object>>();
        private readonly Dictionary<Type, bool> _singletonFlags = new Dictionary<Type, bool>();
        private readonly Dictionary<Type, ServiceLifetime> _serviceLifetimes = new Dictionary<Type, ServiceLifetime>();

        // 初始化状态
        private bool _isInitialized = false;
        private readonly List<Action> _pendingInitializations = new List<Action>();

        /// <summary>
        /// 单例实例
        /// </summary>
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ServiceLocator>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ServiceLocator");
                        _instance = go.AddComponent<ServiceLocator>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 当前注册的服务数量
        /// </summary>
        public int ServiceCount => _services.Count;

        /// <summary>
        /// 当前环境
        /// </summary>
        public EnvironmentType CurrentEnvironment => currentEnvironment;

        #region Unity生命周期

        private void Awake()
        {
            InitializeSingleton();
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                InitializeServices();
            }
        }

        private void OnDestroy()
        {
            DisposeAllServices();
        }

        #endregion

        #region 单例初始化

        /// <summary>
        /// 初始化单例
        /// </summary>
        private void InitializeSingleton()
        {
            if (_instance == null)
            {
                _instance = this;
                
                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
                
                LogDebug("ServiceLocator 单例已初始化");
            }
            else if (_instance != this)
            {
                LogDebug("ServiceLocator 单例已存在，销毁重复实例");
                Destroy(gameObject);
            }
        }

        #endregion

        #region 服务注册

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="instance">服务实例</param>
        public void RegisterSingleton<T>(T instance) where T : class
        {
            RegisterService<T>(instance, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// 注册单例服务工厂
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="factory">服务工厂方法</param>
        public void RegisterSingleton<T>(Func<T> factory) where T : class
        {
            RegisterFactory<T>(factory, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// 注册瞬时服务工厂
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="factory">服务工厂方法</param>
        public void RegisterTransient<T>(Func<T> factory) where T : class
        {
            RegisterFactory<T>(factory, ServiceLifetime.Transient);
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="instance">服务实例</param>
        /// <param name="lifetime">服务生命周期</param>
        public void RegisterService<T>(T instance, ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
        {
            if (instance == null)
            {
                LogError($"尝试注册null服务: {typeof(T).Name}");
                return;
            }

            Type serviceType = typeof(T);
            
            if (_services.ContainsKey(serviceType))
            {
                LogWarning($"服务 {serviceType.Name} 已存在，将被替换");
                DisposeService(serviceType);
            }

            _services[serviceType] = instance;
            _serviceLifetimes[serviceType] = lifetime;
            _singletonFlags[serviceType] = lifetime == ServiceLifetime.Singleton;

            UpdateServiceList();
            LogDebug($"注册服务: {serviceType.Name} ({lifetime})");
        }

        /// <summary>
        /// 注册服务工厂
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="factory">服务工厂方法</param>
        /// <param name="lifetime">服务生命周期</param>
        public void RegisterFactory<T>(Func<T> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
        {
            if (factory == null)
            {
                LogError($"尝试注册null工厂: {typeof(T).Name}");
                return;
            }

            Type serviceType = typeof(T);
            
            _serviceFactories[serviceType] = () => factory();
            _serviceLifetimes[serviceType] = lifetime;
            _singletonFlags[serviceType] = lifetime == ServiceLifetime.Singleton;

            LogDebug($"注册服务工厂: {serviceType.Name} ({lifetime})");
        }

        /// <summary>
        /// 注册接口实现
        /// </summary>
        /// <typeparam name="TInterface">接口类型</typeparam>
        /// <typeparam name="TImplementation">实现类型</typeparam>
        /// <param name="lifetime">服务生命周期</param>
        public void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            RegisterFactory<TInterface>(() => new TImplementation(), lifetime);
        }

        #endregion

        #region 服务获取

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public T GetService<T>() where T : class
        {
            return GetService(typeof(T)) as T;
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <returns>服务实例</returns>
        public object GetService(Type serviceType)
        {
            // 检查是否有直接注册的实例
            if (_services.TryGetValue(serviceType, out object service))
            {
                return service;
            }

            // 检查是否有工厂方法
            if (_serviceFactories.TryGetValue(serviceType, out Func<object> factory))
            {
                try
                {
                    object instance = factory.Invoke();
                    
                    // 如果是单例，缓存实例
                    if (_singletonFlags.TryGetValue(serviceType, out bool isSingleton) && isSingleton)
                    {
                        _services[serviceType] = instance;
                        UpdateServiceList();
                    }
                    
                    return instance;
                }
                catch (Exception e)
                {
                    LogError($"创建服务 {serviceType.Name} 时发生错误: {e.Message}");
                    return null;
                }
            }

            LogWarning($"未找到服务: {serviceType.Name}");
            return null;
        }

        /// <summary>
        /// 尝试获取服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="service">输出的服务实例</param>
        /// <returns>是否成功获取</returns>
        public bool TryGetService<T>(out T service) where T : class
        {
            service = GetService<T>();
            return service != null;
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>是否已注册</returns>
        public bool IsRegistered<T>() where T : class
        {
            return IsRegistered(typeof(T));
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <returns>是否已注册</returns>
        public bool IsRegistered(Type serviceType)
        {
            return _services.ContainsKey(serviceType) || _serviceFactories.ContainsKey(serviceType);
        }

        #endregion

        #region 服务管理

        /// <summary>
        /// 注销服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        public void UnregisterService<T>() where T : class
        {
            UnregisterService(typeof(T));
        }

        /// <summary>
        /// 注销服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        public void UnregisterService(Type serviceType)
        {
            DisposeService(serviceType);
            
            _services.Remove(serviceType);
            _serviceFactories.Remove(serviceType);
            _serviceLifetimes.Remove(serviceType);
            _singletonFlags.Remove(serviceType);
            
            UpdateServiceList();
            LogDebug($"注销服务: {serviceType.Name}");
        }

        /// <summary>
        /// 清除所有服务
        /// </summary>
        public void ClearAllServices()
        {
            DisposeAllServices();
            
            _services.Clear();
            _serviceFactories.Clear();
            _serviceLifetimes.Clear();
            _singletonFlags.Clear();
            
            UpdateServiceList();
            LogDebug("清除所有服务");
        }

        /// <summary>
        /// 释放服务资源
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        private void DisposeService(Type serviceType)
        {
            if (_services.TryGetValue(serviceType, out object service))
            {
                if (service is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                        LogDebug($"已释放服务资源: {serviceType.Name}");
                    }
                    catch (Exception e)
                    {
                        LogError($"释放服务 {serviceType.Name} 资源时发生错误: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 释放所有服务资源
        /// </summary>
        private void DisposeAllServices()
        {
            foreach (var kvp in _services)
            {
                DisposeService(kvp.Key);
            }
        }

        #endregion

        #region 服务初始化

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void InitializeServices()
        {
            if (_isInitialized)
            {
                LogWarning("服务已经初始化过了");
                return;
            }

            LogDebug($"开始初始化服务 - 当前环境: {currentEnvironment}");

            try
            {
                // 根据环境初始化不同的服务
                InitializeEnvironmentServices();
                
                // 执行待处理的初始化操作
                ExecutePendingInitializations();
                
                _isInitialized = true;
                LogDebug("服务初始化完成");
            }
            catch (Exception e)
            {
                LogError($"服务初始化失败: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 根据环境初始化服务
        /// </summary>
        private void InitializeEnvironmentServices()
        {
            switch (currentEnvironment)
            {
                case EnvironmentType.Development:
                    LogDebug("初始化开发环境服务（Mock服务）");
                    // 这里会在后续的Phase中添加Mock服务注册
                    break;
                
                case EnvironmentType.Testing:
                    LogDebug("初始化测试环境服务");
                    // 这里会在后续的Phase中添加测试服务注册
                    break;
                
                case EnvironmentType.Production:
                    LogDebug("初始化生产环境服务（真实服务）");
                    // 这里会在后续的Phase中添加真实服务注册
                    break;
            }
        }

        /// <summary>
        /// 添加待处理的初始化操作
        /// </summary>
        /// <param name="initialization">初始化操作</param>
        public void AddPendingInitialization(Action initialization)
        {
            if (initialization == null) return;

            if (_isInitialized)
            {
                // 如果已经初始化，立即执行
                try
                {
                    initialization.Invoke();
                }
                catch (Exception e)
                {
                    LogError($"执行初始化操作时发生错误: {e.Message}");
                }
            }
            else
            {
                // 否则添加到待处理列表
                _pendingInitializations.Add(initialization);
            }
        }

        /// <summary>
        /// 执行待处理的初始化操作
        /// </summary>
        private void ExecutePendingInitializations()
        {
            foreach (var initialization in _pendingInitializations)
            {
                try
                {
                    initialization.Invoke();
                }
                catch (Exception e)
                {
                    LogError($"执行待处理初始化操作时发生错误: {e.Message}");
                }
            }
            
            _pendingInitializations.Clear();
        }

        /// <summary>
        /// 重新初始化（切换环境时使用）
        /// </summary>
        /// <param name="newEnvironment">新环境</param>
        public void Reinitialize(EnvironmentType newEnvironment)
        {
            LogDebug($"重新初始化服务 - 从 {currentEnvironment} 切换到 {newEnvironment}");
            
            // 清除现有服务
            ClearAllServices();
            
            // 更新环境
            currentEnvironment = newEnvironment;
            _isInitialized = false;
            
            // 重新初始化
            InitializeServices();
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 更新服务列表（用于Inspector显示）
        /// </summary>
        private void UpdateServiceList()
        {
            serviceNames.Clear();
            registeredServiceCount = 0;

            foreach (var kvp in _services)
            {
                string lifetime = _serviceLifetimes.TryGetValue(kvp.Key, out ServiceLifetime lt) ? lt.ToString() : "Unknown";
                serviceNames.Add($"{kvp.Key.Name} ({lifetime})");
                registeredServiceCount++;
            }

            foreach (var kvp in _serviceFactories)
            {
                if (!_services.ContainsKey(kvp.Key))
                {
                    string lifetime = _serviceLifetimes.TryGetValue(kvp.Key, out ServiceLifetime lt) ? lt.ToString() : "Unknown";
                    serviceNames.Add($"{kvp.Key.Name} (Factory, {lifetime})");
                    registeredServiceCount++;
                }
            }
        }

        /// <summary>
        /// 获取服务信息
        /// </summary>
        /// <returns>服务信息字符串</returns>
        public string GetServiceInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"ServiceLocator - 当前环境: {currentEnvironment}");
            info.AppendLine($"已注册服务数量: {ServiceCount}");
            info.AppendLine("服务列表:");

            foreach (var serviceName in serviceNames)
            {
                info.AppendLine($"  - {serviceName}");
            }

            return info.ToString();
        }

        /// <summary>
        /// 调试日志
        /// </summary>
        /// <param name="message">消息</param>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ServiceLocator] {message}");
            }
        }

        /// <summary>
        /// 警告日志
        /// </summary>
        /// <param name="message">消息</param>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[ServiceLocator] {message}");
        }

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="message">消息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[ServiceLocator] {message}");
        }

        #endregion

        #region 静态便捷方法

        /// <summary>
        /// 静态获取服务方法
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public static T Get<T>() where T : class
        {
            return Instance.GetService<T>();
        }

        /// <summary>
        /// 静态注册服务方法
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <param name="instance">服务实例</param>
        public static void Register<T>(T instance) where T : class
        {
            Instance.RegisterSingleton(instance);
        }

        /// <summary>
        /// 静态检查服务是否存在
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>是否存在</returns>
        public static bool Has<T>() where T : class
        {
            return Instance.IsRegistered<T>();
        }

        #endregion
    }

    /// <summary>
    /// 服务生命周期枚举
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// 单例 - 整个应用程序生命周期内只创建一个实例
        /// </summary>
        Singleton,
        
        /// <summary>
        /// 瞬时 - 每次请求都创建新实例
        /// </summary>
        Transient,
        
        /// <summary>
        /// 作用域 - 在特定作用域内是单例（暂未实现）
        /// </summary>
        Scoped
    }

    /// <summary>
    /// 环境类型枚举
    /// </summary>
    public enum EnvironmentType
    {
        /// <summary>
        /// 开发环境 - 使用Mock服务
        /// </summary>
        Development,
        
        /// <summary>
        /// 测试环境 - 使用测试服务器
        /// </summary>
        Testing,
        
        /// <summary>
        /// 生产环境 - 使用正式服务器
        /// </summary>
        Production
    }
}