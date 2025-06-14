// Assets/_Core/Network/NetworkManager.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Network.Interfaces;
using Core.Network.Http;
using Core.Network.WebSocket;
using Core.Network.Mock;
using Core.Network.Utils;

namespace Core.Network
{
    /// <summary>
    /// 网络管理器 - 统一网络服务注册和初始化
    /// 负责管理所有网络组件的生命周期，提供统一的网络服务访问接口
    /// 支持Mock和真实环境的无缝切换
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        #region 单例模式

        private static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NetworkManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("NetworkManager");
                        _instance = go.AddComponent<NetworkManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 网络服务事件

        public event Action<NetworkServiceStatus> OnNetworkServiceStatusChanged;
        public event Action<NetworkDiagnostics> OnNetworkDiagnosticsUpdated;
        public event Action<string> OnNetworkServiceError;
        public event Action OnNetworkServicesInitialized;
        public event Action OnNetworkServicesDestroyed;

        #endregion

        #region Inspector配置

        [Header("环境配置")]
        [SerializeField] private EnvironmentType _currentEnvironment = EnvironmentType.Development;
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private bool _enableLogging = true;

        [Header("服务配置")]
        [SerializeField] private bool _enableHttpServices = true;
        [SerializeField] private bool _enableWebSocketServices = true;
        [SerializeField] private bool _enableMockServices = false;
        [SerializeField] private bool _enableNetworkUtils = true;

        [Header("网络配置")]
        [SerializeField] private NetworkConfig _networkConfig;

        [Header("诊断配置")]
        [SerializeField] private bool _enableDiagnostics = true;
        [SerializeField] private float _diagnosticsInterval = 10f;

        #endregion

        #region 私有字段

        // 服务状态
        private NetworkServiceStatus _serviceStatus = NetworkServiceStatus.Uninitialized;
        private bool _isInitialized = false;
        private bool _isInitializing = false;

        // 核心服务组件
        private HttpClient _httpClient;
        private WebSocketManager _webSocketManager;
        private BaccaratWebSocketService _baccaratWebSocketService;
        private GameMessageDispatcher _messageDispatcher;

        // 网络工具组件
        private NetworkErrorHandler _errorHandler;
        private RetryManager _retryManager;
        private ConnectionMonitor _connectionMonitor;

        // 游戏服务
        private IBaccaratGameService _baccaratGameService;
        private HttpPlayerDataService _playerDataService;

        // 服务注册表
        private Dictionary<Type, object> _serviceRegistry;
        private List<INetworkService> _managedServices;

        // 诊断和监控
        private NetworkDiagnostics _currentDiagnostics;
        private System.Collections.IEnumerator _diagnosticsCoroutine;

        // 游戏参数
        private GameParams _gameParams;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            // 单例模式初始化
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (_autoInitialize)
            {
                _ = InitializeNetworkServicesAsync();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseNetworkServices();
            }
            else
            {
                ResumeNetworkServices();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // 应用失去焦点时暂停某些网络活动
                PauseBackgroundNetworkActivity();
            }
            else
            {
                // 应用重新获得焦点时恢复网络活动
                ResumeBackgroundNetworkActivity();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region 初始化

        private void Initialize()
        {
            _serviceRegistry = new Dictionary<Type, object>();
            _managedServices = new List<INetworkService>();
            _currentDiagnostics = new NetworkDiagnostics();

            // 设置默认网络配置
            if (_networkConfig == null)
            {
                _networkConfig = CreateDefaultNetworkConfig();
            }

            if (_enableLogging)
            {
                Debug.Log("[NetworkManager] 网络管理器已初始化");
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 初始化网络服务
        /// </summary>
        public async Task<bool> InitializeNetworkServicesAsync(GameParams gameParams = null)
        {
            if (_isInitialized || _isInitializing)
            {
                if (_enableLogging)
                {
                    Debug.LogWarning("[NetworkManager] 网络服务已初始化或正在初始化中");
                }
                return _isInitialized;
            }

            try
            {
                _isInitializing = true;
                _gameParams = gameParams;
                SetServiceStatus(NetworkServiceStatus.Initializing);

                if (_enableLogging)
                {
                    Debug.Log($"[NetworkManager] 开始初始化网络服务 - 环境: {_currentEnvironment}");
                }

                // 初始化网络工具
                if (_enableNetworkUtils)
                {
                    await InitializeNetworkUtils();
                }

                // 初始化HTTP服务
                if (_enableHttpServices)
                {
                    await InitializeHttpServices();
                }

                // 初始化WebSocket服务
                if (_enableWebSocketServices)
                {
                    await InitializeWebSocketServices();
                }

                // 初始化Mock服务（如果需要）
                if (_enableMockServices || _currentEnvironment == EnvironmentType.Development)
                {
                    await InitializeMockServices();
                }

                // 初始化游戏服务
                await InitializeGameServices();

                // 注册服务到注册表
                RegisterServices();

                // 启动诊断
                if (_enableDiagnostics)
                {
                    StartDiagnostics();
                }

                _isInitialized = true;
                SetServiceStatus(NetworkServiceStatus.Ready);
                OnNetworkServicesInitialized?.Invoke();

                if (_enableLogging)
                {
                    Debug.Log("[NetworkManager] 网络服务初始化完成");
                }

                return true;
            }
            catch (Exception ex)
            {
                SetServiceStatus(NetworkServiceStatus.Error);
                var errorMessage = $"网络服务初始化失败: {ex.Message}";
                
                if (_enableLogging)
                {
                    Debug.LogError($"[NetworkManager] {errorMessage}");
                }
                
                OnNetworkServiceError?.Invoke(errorMessage);
                return false;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        public T GetService<T>() where T : class
        {
            var serviceType = typeof(T);
            
            if (_serviceRegistry.ContainsKey(serviceType))
            {
                return _serviceRegistry[serviceType] as T;
            }

            // 尝试从组件查找
            var component = GetComponent<T>();
            if (component != null)
            {
                _serviceRegistry[serviceType] = component;
                return component;
            }

            // 尝试从子对象查找
            component = GetComponentInChildren<T>();
            if (component != null)
            {
                _serviceRegistry[serviceType] = component;
                return component;
            }

            if (_enableLogging)
            {
                Debug.LogWarning($"[NetworkManager] 未找到服务: {serviceType.Name}");
            }

            return null;
        }

        /// <summary>
        /// 注册服务实例
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            var serviceType = typeof(T);
            _serviceRegistry[serviceType] = service;

            if (service is INetworkService networkService)
            {
                _managedServices.Add(networkService);
            }

            if (_enableLogging)
            {
                Debug.Log($"[NetworkManager] 已注册服务: {serviceType.Name}");
            }
        }

        /// <summary>
        /// 注销服务实例
        /// </summary>
        public void UnregisterService<T>() where T : class
        {
            var serviceType = typeof(T);
            
            if (_serviceRegistry.ContainsKey(serviceType))
            {
                var service = _serviceRegistry[serviceType];
                
                if (service is INetworkService networkService)
                {
                    _managedServices.Remove(networkService);
                }
                
                _serviceRegistry.Remove(serviceType);

                if (_enableLogging)
                {
                    Debug.Log($"[NetworkManager] 已注销服务: {serviceType.Name}");
                }
            }
        }

        /// <summary>
        /// 切换环境
        /// </summary>
        public async Task<bool> SwitchEnvironmentAsync(EnvironmentType newEnvironment, GameParams gameParams = null)
        {
            if (_currentEnvironment == newEnvironment)
            {
                return true;
            }

            if (_enableLogging)
            {
                Debug.Log($"[NetworkManager] 切换环境: {_currentEnvironment} -> {newEnvironment}");
            }

            // 停止当前服务
            await StopNetworkServicesAsync();

            // 更新环境
            _currentEnvironment = newEnvironment;
            _enableMockServices = newEnvironment == EnvironmentType.Development;

            // 重新初始化服务
            return await InitializeNetworkServicesAsync(gameParams);
        }

        /// <summary>
        /// 停止网络服务
        /// </summary>
        public async Task StopNetworkServicesAsync()
        {
            try
            {
                SetServiceStatus(NetworkServiceStatus.Stopping);

                // 停止诊断
                StopDiagnostics();

                // 停止所有托管服务
                foreach (var service in _managedServices)
                {
                    try
                    {
                        await service.StopAsync();
                    }
                    catch (Exception ex)
                    {
                        if (_enableLogging)
                        {
                            Debug.LogError($"[NetworkManager] 停止服务失败: {ex.Message}");
                        }
                    }
                }

                // 断开WebSocket连接
                if (_webSocketManager != null && _webSocketManager.IsConnected)
                {
                    await _webSocketManager.DisconnectAsync("NetworkManager停止");
                }

                // 清理服务注册表
                _serviceRegistry.Clear();
                _managedServices.Clear();

                _isInitialized = false;
                SetServiceStatus(NetworkServiceStatus.Stopped);

                if (_enableLogging)
                {
                    Debug.Log("[NetworkManager] 网络服务已停止");
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    Debug.LogError($"[NetworkManager] 停止网络服务时发生错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取当前服务状态
        /// </summary>
        public NetworkServiceStatus GetServiceStatus()
        {
            return _serviceStatus;
        }

        /// <summary>
        /// 获取网络诊断信息
        /// </summary>
        public NetworkDiagnostics GetNetworkDiagnostics()
        {
            return _currentDiagnostics;
        }

        /// <summary>
        /// 设置网络配置
        /// </summary>
        public void SetNetworkConfig(NetworkConfig config)
        {
            _networkConfig = config;

            if (_enableLogging)
            {
                Debug.Log("[NetworkManager] 网络配置已更新");
            }
        }

        /// <summary>
        /// 获取当前环境类型
        /// </summary>
        public EnvironmentType GetCurrentEnvironment()
        {
            return _currentEnvironment;
        }

        /// <summary>
        /// 检查服务是否可用
        /// </summary>
        public bool IsServiceAvailable<T>() where T : class
        {
            return GetService<T>() != null;
        }

        /// <summary>
        /// 执行网络健康检查
        /// </summary>
        public async Task<NetworkHealthCheckResult> PerformHealthCheckAsync()
        {
            var result = new NetworkHealthCheckResult
            {
                CheckTime = DateTime.UtcNow,
                OverallHealth = NetworkHealth.Unknown
            };

            try
            {
                var checks = new List<Task<ServiceHealthCheck>>();

                // HTTP服务健康检查
                if (_httpClient != null)
                {
                    checks.Add(CheckHttpServiceHealth());
                }

                // WebSocket服务健康检查
                if (_webSocketManager != null)
                {
                    checks.Add(CheckWebSocketServiceHealth());
                }

                // 游戏服务健康检查
                if (_baccaratGameService != null)
                {
                    checks.Add(CheckGameServiceHealth());
                }

                // 连接监控健康检查
                if (_connectionMonitor != null)
                {
                    checks.Add(CheckConnectionMonitorHealth());
                }

                // 等待所有检查完成
                var healthChecks = await Task.WhenAll(checks);
                result.ServiceChecks = healthChecks;

                // 计算整体健康状况
                result.OverallHealth = CalculateOverallHealth(healthChecks);

                if (_enableLogging)
                {
                    Debug.Log($"[NetworkManager] 网络健康检查完成: {result.OverallHealth}");
                }
            }
            catch (Exception ex)
            {
                result.OverallHealth = NetworkHealth.Critical;
                result.ErrorMessage = ex.Message;

                if (_enableLogging)
                {
                    Debug.LogError($"[NetworkManager] 网络健康检查失败: {ex.Message}");
                }
            }

            return result;
        }

        #endregion

        #region 服务初始化方法

        private async Task InitializeNetworkUtils()
        {
            try
            {
                // 初始化错误处理器
                _errorHandler = GetComponent<NetworkErrorHandler>();
                if (_errorHandler == null)
                {
                    _errorHandler = gameObject.AddComponent<NetworkErrorHandler>();
                }

                // 初始化重试管理器
                _retryManager = GetComponent<RetryManager>();
                if (_retryManager == null)
                {
                    _retryManager = gameObject.AddComponent<RetryManager>();
                }

                // 初始化连接监控器
                _connectionMonitor = GetComponent<ConnectionMonitor>();
                if (_connectionMonitor == null)
                {
                    _connectionMonitor = gameObject.AddComponent<ConnectionMonitor>();
                }

                if (_enableLogging)
                {
                    Debug.Log("[NetworkManager] 网络工具初始化完成");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"网络工具初始化失败: {ex.Message}");
            }
        }

        private async Task InitializeHttpServices()
        {
            try
            {
                // 初始化HTTP客户端
                _httpClient = GetComponent<HttpClient>();
                if (_httpClient == null)
                {
                    _httpClient = gameObject.AddComponent<HttpClient>();
                }

                // 配置HTTP客户端
                _httpClient.SetBaseUrl(_networkConfig.HttpConfig.BaseUrl);
                _httpClient.SetTimeout(_networkConfig.HttpConfig.Timeout);

                // 设置错误处理集成
                if (_errorHandler != null)
                {
                    _httpClient.SetGlobalErrorHandler((error) =>
                    {
                        _ = _errorHandler.HandleErrorAsync(new Exception(error.message), "HTTP请求");
                    });
                }

                if (_enableLogging)
                {
                    Debug.Log("[NetworkManager] HTTP服务初始化完成");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"HTTP服务初始化失败: {ex.Message}");
            }
        }

        private async Task InitializeWebSocketServices()
        {
            try
            {
                // 初始化WebSocket管理器
                _webSocketManager = GetComponent<WebSocketManager>();
                if (_webSocketManager == null)
                {
                    _webSocketManager = gameObject.AddComponent<WebSocketManager>();
                }

                // 初始化百家乐WebSocket服务
                _baccaratWebSocketService = GetComponent<BaccaratWebSocketService>();
                if (_baccaratWebSocketService == null)
                {
                    _baccaratWebSocketService = gameObject.AddComponent<BaccaratWebSocketService>();
                }

                // 初始化消息分发器
                _messageDispatcher = new GameMessageDispatcher();

                if (_enableLogging)
                {
                    Debug.Log("[NetworkManager] WebSocket服务初始化完成");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"WebSocket服务初始化失败: {ex.Message}");
            }
        }

        private async Task InitializeMockServices()
        {
            try
            {
                if (_currentEnvironment == EnvironmentType.Development || _enableMockServices)
                {
                    // Mock服务在需要时创建
                    if (_enableLogging)
                    {
                        Debug.Log("[NetworkManager] Mock服务已准备就绪");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Mock服务初始化失败: {ex.Message}");
            }
        }

        private async Task InitializeGameServices()
        {
            try
            {
                // 根据环境选择游戏服务实现
                if (_currentEnvironment == EnvironmentType.Development && _enableMockServices)
                {
                    _baccaratGameService = new MockBaccaratGameService();
                }
                else
                {
                    _baccaratGameService = new HttpBaccaratGameService(_httpClient);
                }

                // 初始化玩家数据服务
                if (_gameParams != null)
                {
                    _playerDataService = new HttpPlayerDataService(_httpClient, _gameParams.user_id);
                }

                if (_enableLogging)
                {
                    Debug.Log("[NetworkManager] 游戏服务初始化完成");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"游戏服务初始化失败: {ex.Message}");
            }
        }

        private void RegisterServices()
        {
            // 注册核心服务
            if (_httpClient != null)
                RegisterService<HttpClient>(_httpClient);
            
            if (_webSocketManager != null)
                RegisterService<IWebSocketService>(_webSocketManager);
            
            if (_baccaratWebSocketService != null)
                RegisterService<BaccaratWebSocketService>(_baccaratWebSocketService);
            
            if (_messageDispatcher != null)
                RegisterService<GameMessageDispatcher>(_messageDispatcher);

            // 注册网络工具
            if (_errorHandler != null)
                RegisterService<NetworkErrorHandler>(_errorHandler);
            
            if (_retryManager != null)
                RegisterService<RetryManager>(_retryManager);
            
            if (_connectionMonitor != null)
                RegisterService<ConnectionMonitor>(_connectionMonitor);

            // 注册游戏服务
            if (_baccaratGameService != null)
                RegisterService<IBaccaratGameService>(_baccaratGameService);
            
            if (_playerDataService != null)
                RegisterService<HttpPlayerDataService>(_playerDataService);
        }

        #endregion

        #region 服务管理

        private void PauseNetworkServices()
        {
            try
            {
                // 暂停连接监控
                if (_connectionMonitor != null)
                {
                    _connectionMonitor.PauseMonitoring();
                }

                // 暂停WebSocket连接
                if (_webSocketManager != null && _webSocketManager.IsConnected)
                {
                    _ = _webSocketManager.DisconnectAsync("应用暂停");
                }

                if (_enableLogging)
                {
                    Debug.Log("[NetworkManager] 网络服务已暂停");
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    Debug.LogError($"[NetworkManager] 暂停网络服务失败: {ex.Message}");
                }
            }
        }

        private void ResumeNetworkServices()
        {
            try
            {
                // 恢复连接监控
                if (_connectionMonitor != null)
                {
                    _connectionMonitor.ResumeMonitoring();
                }

                // 恢复WebSocket连接
                if (_webSocketManager != null && _gameParams != null)
                {
                    _ = _baccaratWebSocketService?.InitializeAndConnectAsync(_gameParams);
                }

                if (_enableLogging)
                {
                    Debug.Log("[NetworkManager] 网络服务已恢复");
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    Debug.LogError($"[NetworkManager] 恢复网络服务失败: {ex.Message}");
                }
            }
        }

        private void PauseBackgroundNetworkActivity()
        {
            // 暂停后台网络活动，如定期的数据同步
            try
            {
                if (_connectionMonitor != null)
                {
                    _connectionMonitor.PauseMonitoring();
                }

                if (_enableLogging)
                {
                    Debug.Log("[NetworkManager] 后台网络活动已暂停");
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    Debug.LogError($"[NetworkManager] 暂停后台网络活动失败: {ex.Message}");
                }
            }
        }

        private void ResumeBackgroundNetworkActivity()
        {
            // 恢复后台网络活动
            try
            {
                if (_connectionMonitor != null)
                {
                    _connectionMonitor.ResumeMonitoring();
                }

                if (_enableLogging)
                {
                    Debug.Log("[NetworkManager] 后台网络活动已恢复");
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    Debug.LogError($"[NetworkManager] 恢复后台网络活动失败: {ex.Message}");
                }
            }
        }

        #endregion

        #region 健康检查

        private async Task<ServiceHealthCheck> CheckHttpServiceHealth()
        {
            var check = new ServiceHealthCheck
            {
                ServiceName = "HTTP服务",
                ServiceType = "HttpClient"
            };

            try
            {
                var stats = _httpClient.GetStatistics();
                check.IsHealthy = true;
                check.ResponseTime = stats.averageResponseTime;
                check.Details = $"成功率: {stats.successRate:P}, 平均响应时间: {stats.averageResponseTime:F0}ms";
            }
            catch (Exception ex)
            {
                check.IsHealthy = false;
                check.ErrorMessage = ex.Message;
            }

            return check;
        }

        private async Task<ServiceHealthCheck> CheckWebSocketServiceHealth()
        {
            var check = new ServiceHealthCheck
            {
                ServiceName = "WebSocket服务",
                ServiceType = "WebSocketManager"
            };

            try
            {
                check.IsHealthy = _webSocketManager.IsConnected;
                check.ResponseTime = _webSocketManager.Latency;
                check.Details = $"连接状态: {_webSocketManager.ConnectionStatus}, 延迟: {_webSocketManager.Latency}ms";
                
                if (!check.IsHealthy)
                {
                    check.ErrorMessage = $"WebSocket未连接: {_webSocketManager.ConnectionStatus}";
                }
            }
            catch (Exception ex)
            {
                check.IsHealthy = false;
                check.ErrorMessage = ex.Message;
            }

            return check;
        }

        private async Task<ServiceHealthCheck> CheckGameServiceHealth()
        {
            var check = new ServiceHealthCheck
            {
                ServiceName = "游戏服务",
                ServiceType = "BaccaratGameService"
            };

            try
            {
                var status = _baccaratGameService.GetServiceStatus();
                check.IsHealthy = status == ApiServiceStatus.Ready;
                check.Details = $"服务状态: {status}";
                
                if (!check.IsHealthy)
                {
                    check.ErrorMessage = $"游戏服务未就绪: {status}";
                }
            }
            catch (Exception ex)
            {
                check.IsHealthy = false;
                check.ErrorMessage = ex.Message;
            }

            return check;
        }

        private async Task<ServiceHealthCheck> CheckConnectionMonitorHealth()
        {
            var check = new ServiceHealthCheck
            {
                ServiceName = "连接监控",
                ServiceType = "ConnectionMonitor"
            };

            try
            {
                var quality = _connectionMonitor.GetCurrentQuality();
                var status = _connectionMonitor.GetCurrentStatus();
                
                check.IsHealthy = status != ConnectionStatus.Disconnected && quality != ConnectionQuality.Poor;
                check.Details = $"连接状态: {status}, 质量: {quality}";
                
                if (!check.IsHealthy)
                {
                    check.ErrorMessage = $"网络连接质量差: {status}/{quality}";
                }
            }
            catch (Exception ex)
            {
                check.IsHealthy = false;
                check.ErrorMessage = ex.Message;
            }

            return check;
        }

        private NetworkHealth CalculateOverallHealth(ServiceHealthCheck[] checks)
        {
            if (checks == null || checks.Length == 0)
            {
                return NetworkHealth.Unknown;
            }

            var healthyCount = 0;
            var criticalFailures = 0;

            foreach (var check in checks)
            {
                if (check.IsHealthy)
                {
                    healthyCount++;
                }
                else if (check.ServiceType == "WebSocketManager" || check.ServiceType == "HttpClient")
                {
                    criticalFailures++;
                }
            }

            if (criticalFailures > 0)
            {
                return NetworkHealth.Critical;
            }

            var healthRatio = (float)healthyCount / checks.Length;

            if (healthRatio >= 0.9f)
                return NetworkHealth.Excellent;
            else if (healthRatio >= 0.7f)
                return NetworkHealth.Good;
            else if (healthRatio >= 0.5f)
                return NetworkHealth.Fair;
            else
                return NetworkHealth.Poor;
        }

        #endregion

        #region 诊断监控

        private void StartDiagnostics()
        {
            if (_diagnosticsCoroutine != null)
            {
                StopCoroutine(_diagnosticsCoroutine);
            }

            _diagnosticsCoroutine = StartCoroutine(DiagnosticsCoroutine());

            if (_enableLogging)
            {
                Debug.Log("[NetworkManager] 网络诊断已启动");
            }
        }

        private void StopDiagnostics()
        {
            if (_diagnosticsCoroutine != null)
            {
                StopCoroutine(_diagnosticsCoroutine);
                _diagnosticsCoroutine = null;
            }

            if (_enableLogging)
            {
                Debug.Log("[NetworkManager] 网络诊断已停止");
            }
        }

        private System.Collections.IEnumerator DiagnosticsCoroutine()
        {
            while (_isInitialized)
            {
                try
                {
                    UpdateNetworkDiagnostics();
                }
                catch (Exception ex)
                {
                    if (_enableLogging)
                    {
                        Debug.LogError($"[NetworkManager] 诊断更新失败: {ex.Message}");
                    }
                }

                yield return new WaitForSeconds(_diagnosticsInterval);
            }
        }

        private void UpdateNetworkDiagnostics()
        {
            _currentDiagnostics.LastUpdated = DateTime.UtcNow;
            _currentDiagnostics.ServiceStatus = _serviceStatus;

            // HTTP诊断
            if (_httpClient != null)
            {
                var httpStats = _httpClient.GetStatistics();
                _currentDiagnostics.HttpStatistics = new HttpDiagnostics
                {
                    TotalRequests = httpStats.totalRequests,
                    SuccessfulRequests = httpStats.successfulRequests,
                    FailedRequests = httpStats.failedRequests,
                    AverageResponseTime = httpStats.averageResponseTime,
                    SuccessRate = httpStats.successRate
                };
            }

            // WebSocket诊断
            if (_webSocketManager != null)
            {
                var wsStats = _webSocketManager.GetStatistics();
                _currentDiagnostics.WebSocketStatistics = new WebSocketDiagnostics
                {
                    ConnectionStatus = _webSocketManager.ConnectionStatus.ToString(),
                    IsConnected = _webSocketManager.IsConnected,
                    Latency = _webSocketManager.Latency,
                    MessagesSent = wsStats.messagesSent,
                    MessagesReceived = wsStats.messagesReceived,
                    ConnectionErrors = wsStats.connectionErrors
                };
            }

            // 连接监控诊断
            if (_connectionMonitor != null)
            {
                var connectionMetrics = _connectionMonitor.GetCurrentMetrics();
                _currentDiagnostics.ConnectionStatistics = new ConnectionDiagnostics
                {
                    ConnectionStatus = _connectionMonitor.GetCurrentStatus().ToString(),
                    ConnectionQuality = _connectionMonitor.GetCurrentQuality().ToString(),
                    SuccessRate = connectionMetrics.SuccessRate,
                    AverageLatency = connectionMetrics.AverageLatency,
                    ActiveAlerts = _connectionMonitor.GetActiveAlerts().Length
                };
            }

            // 错误统计
            if (_errorHandler != null)
            {
                var errorStats = _errorHandler.GetErrorStats();
                _currentDiagnostics.ErrorStatistics = new ErrorDiagnostics
                {
                    TotalErrors = errorStats.TotalErrors,
                    RecoveredErrors = errorStats.RecoveredErrors,
                    RecoveryRate = errorStats.RecoveryRate,
                    ErrorRate = errorStats.ErrorRate
                };
            }

            OnNetworkDiagnosticsUpdated?.Invoke(_currentDiagnostics);
        }

        #endregion

        #region 辅助方法

        private void SetServiceStatus(NetworkServiceStatus newStatus)
        {
            if (_serviceStatus != newStatus)
            {
                var oldStatus = _serviceStatus;
                _serviceStatus = newStatus;

                OnNetworkServiceStatusChanged?.Invoke(newStatus);

                if (_enableLogging)
                {
                    Debug.Log($"[NetworkManager] 服务状态变更: {oldStatus} -> {newStatus}");
                }
            }
        }

        private NetworkConfig CreateDefaultNetworkConfig()
        {
            return new NetworkConfig
            {
                HttpConfig = new HttpServiceConfig
                {
                    BaseUrl = "https://api.yourgame.com",
                    Timeout = 10000,
                    MaxRetries = 3,
                    EnableLogging = _enableLogging
                },
                WebSocketConfig = new WebSocketServiceConfig
                {
                    BaseUrl = "wss://ws.yourgame.com",
                    ConnectionTimeout = 10000,
                    HeartbeatInterval = 30000,
                    AutoReconnect = true,
                    MaxReconnectAttempts = 5
                },
                UtilsConfig = new NetworkUtilsConfig
                {
                    EnableErrorHandler = true,
                    EnableRetryManager = true,
                    EnableConnectionMonitor = true,
                    MonitoringInterval = 10f
                }
            };
        }

        private void Cleanup()
        {
            try
            {
                // 停止诊断
                StopDiagnostics();

                // 停止网络服务
                if (_isInitialized)
                {
                    _ = StopNetworkServicesAsync();
                }

                // 清理事件
                OnNetworkServiceStatusChanged = null;
                OnNetworkDiagnosticsUpdated = null;
                OnNetworkServiceError = null;
                OnNetworkServicesInitialized = null;
                OnNetworkServicesDestroyed = null;

                OnNetworkServicesDestroyed?.Invoke();

                if (_enableLogging)
                {
                    Debug.Log("[NetworkManager] 网络管理器清理完成");
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    Debug.LogError($"[NetworkManager] 清理过程中发生错误: {ex.Message}");
                }
            }
        }

        #endregion

        #region 静态便利方法

        /// <summary>
        /// 快速获取百家乐游戏服务
        /// </summary>
        public static IBaccaratGameService GetBaccaratGameService()
        {
            return Instance.GetService<IBaccaratGameService>();
        }

        /// <summary>
        /// 快速获取WebSocket服务
        /// </summary>
        public static IWebSocketService GetWebSocketService()
        {
            return Instance.GetService<IWebSocketService>();
        }

        /// <summary>
        /// 快速获取HTTP客户端
        /// </summary>
        public static HttpClient GetHttpClient()
        {
            return Instance.GetService<HttpClient>();
        }

        /// <summary>
        /// 快速获取连接监控器
        /// </summary>
        public static ConnectionMonitor GetConnectionMonitor()
        {
            return Instance.GetService<ConnectionMonitor>();
        }

        /// <summary>
        /// 快速获取重试管理器
        /// </summary>
        public static RetryManager GetRetryManager()
        {
            return Instance.GetService<RetryManager>();
        }

        /// <summary>
        /// 快速获取错误处理器
        /// </summary>
        public static NetworkErrorHandler GetErrorHandler()
        {
            return Instance.GetService<NetworkErrorHandler>();
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 环境类型
    /// </summary>
    public enum EnvironmentType
    {
        Development,  // 开发环境
        Testing,      // 测试环境
        Production    // 生产环境
    }

    /// <summary>
    /// 网络服务状态
    /// </summary>
    public enum NetworkServiceStatus
    {
        Uninitialized,  // 未初始化
        Initializing,   // 初始化中
        Ready,          // 就绪
        Error,          // 错误
        Stopping,       // 停止中
        Stopped         // 已停止
    }

    /// <summary>
    /// 网络健康状态
    /// </summary>
    public enum NetworkHealth
    {
        Unknown,     // 未知
        Critical,    // 严重
        Poor,        // 较差
        Fair,        // 一般
        Good,        // 良好
        Excellent    // 优秀
    }

    /// <summary>
    /// 网络配置
    /// </summary>
    [System.Serializable]
    public class NetworkConfig
    {
        public HttpServiceConfig HttpConfig;
        public WebSocketServiceConfig WebSocketConfig;
        public NetworkUtilsConfig UtilsConfig;
    }

    /// <summary>
    /// HTTP服务配置
    /// </summary>
    [System.Serializable]
    public class HttpServiceConfig
    {
        public string BaseUrl = "https://api.yourgame.com";
        public int Timeout = 10000;
        public int MaxRetries = 3;
        public bool EnableLogging = true;
    }

    /// <summary>
    /// WebSocket服务配置
    /// </summary>
    [System.Serializable]
    public class WebSocketServiceConfig
    {
        public string BaseUrl = "wss://ws.yourgame.com";
        public int ConnectionTimeout = 10000;
        public int HeartbeatInterval = 30000;
        public bool AutoReconnect = true;
        public int MaxReconnectAttempts = 5;
    }

    /// <summary>
    /// 网络工具配置
    /// </summary>
    [System.Serializable]
    public class NetworkUtilsConfig
    {
        public bool EnableErrorHandler = true;
        public bool EnableRetryManager = true;
        public bool EnableConnectionMonitor = true;
        public float MonitoringInterval = 10f;
    }

    /// <summary>
    /// 网络服务接口
    /// </summary>
    public interface INetworkService
    {
        Task<bool> StartAsync();
        Task<bool> StopAsync();
        bool IsRunning { get; }
    }

    /// <summary>
    /// 网络健康检查结果
    /// </summary>
    [System.Serializable]
    public class NetworkHealthCheckResult
    {
        public DateTime CheckTime;
        public NetworkHealth OverallHealth;
        public ServiceHealthCheck[] ServiceChecks;
        public string ErrorMessage;
    }

    /// <summary>
    /// 服务健康检查
    /// </summary>
    [System.Serializable]
    public class ServiceHealthCheck
    {
        public string ServiceName;
        public string ServiceType;
        public bool IsHealthy;
        public float ResponseTime;
        public string Details;
        public string ErrorMessage;
    }

    /// <summary>
    /// 网络诊断信息
    /// </summary>
    [System.Serializable]
    public class NetworkDiagnostics
    {
        public DateTime LastUpdated;
        public NetworkServiceStatus ServiceStatus;
        public HttpDiagnostics HttpStatistics;
        public WebSocketDiagnostics WebSocketStatistics;
        public ConnectionDiagnostics ConnectionStatistics;
        public ErrorDiagnostics ErrorStatistics;
    }

    /// <summary>
    /// HTTP诊断信息
    /// </summary>
    [System.Serializable]
    public class HttpDiagnostics
    {
        public int TotalRequests;
        public int SuccessfulRequests;
        public int FailedRequests;
        public float AverageResponseTime;
        public float SuccessRate;
    }

    /// <summary>
    /// WebSocket诊断信息
    /// </summary>
    [System.Serializable]
    public class WebSocketDiagnostics
    {
        public string ConnectionStatus;
        public bool IsConnected;
        public int Latency;
        public long MessagesSent;
        public long MessagesReceived;
        public int ConnectionErrors;
    }

    /// <summary>
    /// 连接诊断信息
    /// </summary>
    [System.Serializable]
    public class ConnectionDiagnostics
    {
        public string ConnectionStatus;
        public string ConnectionQuality;
        public float SuccessRate;
        public float AverageLatency;
        public int ActiveAlerts;
    }

    /// <summary>
    /// 错误诊断信息
    /// </summary>
    [System.Serializable]
    public class ErrorDiagnostics
    {
        public int TotalErrors;
        public int RecoveredErrors;
        public float RecoveryRate;
        public int ErrorRate;
    }

    #endregion
}