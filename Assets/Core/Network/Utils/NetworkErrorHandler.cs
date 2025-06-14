// Assets/_Core/Network/Utils/NetworkErrorHandler.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Core.Network.Interfaces;

namespace Core.Network.Utils
{
    /// <summary>
    /// 网络错误处理器
    /// 提供统一的网络错误分类、处理、恢复策略和用户友好的错误提示
    /// </summary>
    public class NetworkErrorHandler : MonoBehaviour
    {
        #region 错误处理事件

        public event Action<NetworkError> OnNetworkError;
        public event Action<string> OnUserFriendlyError;
        public event Action<RecoveryAction> OnRecoveryActionRequired;
        public event Action<NetworkErrorStats> OnErrorStatsUpdated;

        #endregion

        #region Inspector配置

        [Header("错误处理配置")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _enableUserNotification = true;
        [SerializeField] private bool _enableAutoRecovery = true;
        [SerializeField] private int _maxErrorHistory = 100;

        [Header("错误阈值配置")]
        [SerializeField] private int _consecutiveErrorThreshold = 5;
        [SerializeField] private int _errorRateThreshold = 10; // 错误/分钟
        [SerializeField] private float _circuitBreakerThreshold = 0.5f; // 50%错误率

        [Header("用户提示配置")]
        [SerializeField] private float _notificationCooldown = 5f; // 通知冷却时间
        [SerializeField] private bool _showTechnicalDetails = false;

        #endregion

        #region 私有字段

        // 错误统计
        private NetworkErrorStats _errorStats;
        private Queue<NetworkError> _errorHistory;
        private Dictionary<NetworkErrorType, int> _errorTypeCount;
        private Dictionary<string, DateTime> _lastNotificationTime;

        // 恢复策略
        private Dictionary<NetworkErrorType, Func<NetworkError, Task<bool>>> _recoveryStrategies;
        private Dictionary<string, RecoveryAction> _activeRecoveryActions;

        // 熔断器状态
        private CircuitBreakerState _circuitBreakerState;
        private DateTime _circuitBreakerOpenTime;
        private int _circuitBreakerFailureCount;

        // 错误模式检测
        private ErrorPatternDetector _patternDetector;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            StartErrorMonitoring();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region 初始化

        private void Initialize()
        {
            _errorStats = new NetworkErrorStats();
            _errorHistory = new Queue<NetworkError>();
            _errorTypeCount = new Dictionary<NetworkErrorType, int>();
            _lastNotificationTime = new Dictionary<string, DateTime>();
            _recoveryStrategies = new Dictionary<NetworkErrorType, Func<NetworkError, Task<bool>>>();
            _activeRecoveryActions = new Dictionary<string, RecoveryAction>();
            _circuitBreakerState = CircuitBreakerState.Closed;
            _patternDetector = new ErrorPatternDetector();

            // 注册默认恢复策略
            RegisterDefaultRecoveryStrategies();

            if (_enableLogging)
            {
                Debug.Log("[NetworkErrorHandler] 网络错误处理器已初始化");
            }
        }

        private void RegisterDefaultRecoveryStrategies()
        {
            // 连接失败恢复策略
            _recoveryStrategies[NetworkErrorType.ConnectionFailed] = async (error) =>
            {
                await Task.Delay(1000); // 等待1秒
                return await AttemptReconnection(error);
            };

            // 超时错误恢复策略
            _recoveryStrategies[NetworkErrorType.Timeout] = async (error) =>
            {
                await Task.Delay(500); // 等待500ms
                return await RetryLastOperation(error);
            };

            // 认证失败恢复策略
            _recoveryStrategies[NetworkErrorType.AuthenticationFailed] = async (error) =>
            {
                return await RefreshAuthentication(error);
            };

            // 服务器错误恢复策略
            _recoveryStrategies[NetworkErrorType.ServerError] = async (error) =>
            {
                // 服务器错误通常需要等待修复
                await Task.Delay(5000); // 等待5秒
                return false; // 不自动重试
            };

            // 网络不可达恢复策略
            _recoveryStrategies[NetworkErrorType.NetworkUnreachable] = async (error) =>
            {
                return await WaitForNetworkRecovery(error);
            };
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 处理网络错误
        /// </summary>
        public async Task<bool> HandleErrorAsync(Exception exception, string context = "")
        {
            var networkError = ClassifyError(exception, context);
            return await HandleErrorAsync(networkError);
        }

        /// <summary>
        /// 处理网络错误
        /// </summary>
        public async Task<bool> HandleErrorAsync(NetworkError error)
        {
            if (error == null)
            {
                Debug.LogWarning("[NetworkErrorHandler] 尝试处理空错误");
                return false;
            }

            try
            {
                // 记录错误
                RecordError(error);

                // 检查熔断器状态
                if (_circuitBreakerState == CircuitBreakerState.Open)
                {
                    if (_enableLogging)
                    {
                        Debug.LogWarning("[NetworkErrorHandler] 熔断器开启，拒绝处理请求");
                    }
                    return false;
                }

                // 错误模式检测
                var pattern = _patternDetector.DetectPattern(_errorHistory.ToArray());
                if (pattern.IsAbnormal)
                {
                    await HandleAbnormalPattern(pattern);
                }

                // 用户通知
                if (_enableUserNotification)
                {
                    NotifyUser(error);
                }

                // 触发错误事件
                OnNetworkError?.Invoke(error);

                // 自动恢复
                bool recovered = false;
                if (_enableAutoRecovery && error.IsRecoverable)
                {
                    recovered = await AttemptRecovery(error);
                }

                // 更新熔断器状态
                UpdateCircuitBreaker(error, recovered);

                // 更新统计
                UpdateErrorStats(error, recovered);

                return recovered;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkErrorHandler] 处理错误时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 注册自定义恢复策略
        /// </summary>
        public void RegisterRecoveryStrategy(NetworkErrorType errorType, Func<NetworkError, Task<bool>> strategy)
        {
            _recoveryStrategies[errorType] = strategy;

            if (_enableLogging)
            {
                Debug.Log($"[NetworkErrorHandler] 已注册恢复策略: {errorType}");
            }
        }

        /// <summary>
        /// 获取错误统计信息
        /// </summary>
        public NetworkErrorStats GetErrorStats()
        {
            return _errorStats;
        }

        /// <summary>
        /// 获取错误历史
        /// </summary>
        public NetworkError[] GetErrorHistory()
        {
            return _errorHistory.ToArray();
        }

        /// <summary>
        /// 清除错误历史
        /// </summary>
        public void ClearErrorHistory()
        {
            _errorHistory.Clear();
            _errorTypeCount.Clear();
            _errorStats.ResetStats();

            if (_enableLogging)
            {
                Debug.Log("[NetworkErrorHandler] 错误历史已清除");
            }
        }

        /// <summary>
        /// 手动重置熔断器
        /// </summary>
        public void ResetCircuitBreaker()
        {
            _circuitBreakerState = CircuitBreakerState.Closed;
            _circuitBreakerFailureCount = 0;

            if (_enableLogging)
            {
                Debug.Log("[NetworkErrorHandler] 熔断器已重置");
            }
        }

        /// <summary>
        /// 获取熔断器状态
        /// </summary>
        public CircuitBreakerState GetCircuitBreakerState()
        {
            return _circuitBreakerState;
        }

        /// <summary>
        /// 设置错误处理配置
        /// </summary>
        public void SetErrorHandlingConfig(NetworkErrorHandlingConfig config)
        {
            _enableLogging = config.enableLogging;
            _enableUserNotification = config.enableUserNotification;
            _enableAutoRecovery = config.enableAutoRecovery;
            _consecutiveErrorThreshold = config.consecutiveErrorThreshold;
            _errorRateThreshold = config.errorRateThreshold;
            _circuitBreakerThreshold = config.circuitBreakerThreshold;
            _notificationCooldown = config.notificationCooldown;

            if (_enableLogging)
            {
                Debug.Log("[NetworkErrorHandler] 错误处理配置已更新");
            }
        }

        #endregion

        #region 错误分类

        private NetworkError ClassifyError(Exception exception, string context)
        {
            var error = new NetworkError
            {
                id = Guid.NewGuid().ToString(),
                timestamp = DateTime.UtcNow,
                context = context,
                originalException = exception,
                message = exception.Message
            };

            // 根据异常类型分类
            if (exception is System.Net.WebException webEx)
            {
                error.errorType = ClassifyWebException(webEx);
                error.httpStatusCode = GetHttpStatusCode(webEx);
            }
            else if (exception is TaskCanceledException)
            {
                error.errorType = NetworkErrorType.Timeout;
                error.severity = ErrorSeverity.Medium;
            }
            else if (exception is System.Net.Sockets.SocketException socketEx)
            {
                error.errorType = ClassifySocketException(socketEx);
                error.severity = ErrorSeverity.High;
            }
            else if (exception.Message.Contains("401") || exception.Message.Contains("Unauthorized"))
            {
                error.errorType = NetworkErrorType.AuthenticationFailed;
                error.severity = ErrorSeverity.High;
                error.httpStatusCode = 401;
            }
            else if (exception.Message.Contains("403") || exception.Message.Contains("Forbidden"))
            {
                error.errorType = NetworkErrorType.AuthorizationFailed;
                error.severity = ErrorSeverity.High;
                error.httpStatusCode = 403;
            }
            else
            {
                error.errorType = NetworkErrorType.Unknown;
                error.severity = ErrorSeverity.Medium;
            }

            // 设置是否可恢复
            error.isRecoverable = IsErrorRecoverable(error.errorType);

            // 生成用户友好的消息
            error.userFriendlyMessage = GenerateUserFriendlyMessage(error);

            return error;
        }

        private NetworkErrorType ClassifyWebException(System.Net.WebException webEx)
        {
            switch (webEx.Status)
            {
                case System.Net.WebExceptionStatus.Timeout:
                    return NetworkErrorType.Timeout;
                case System.Net.WebExceptionStatus.ConnectFailure:
                    return NetworkErrorType.ConnectionFailed;
                case System.Net.WebExceptionStatus.NameResolutionFailure:
                    return NetworkErrorType.DnsResolutionFailed;
                case System.Net.WebExceptionStatus.ServerProtocolViolation:
                    return NetworkErrorType.ProtocolError;
                default:
                    return NetworkErrorType.HttpError;
            }
        }

        private NetworkErrorType ClassifySocketException(System.Net.Sockets.SocketException socketEx)
        {
            switch (socketEx.SocketErrorCode)
            {
                case System.Net.Sockets.SocketError.TimedOut:
                    return NetworkErrorType.Timeout;
                case System.Net.Sockets.SocketError.ConnectionRefused:
                    return NetworkErrorType.ConnectionRefused;
                case System.Net.Sockets.SocketError.NetworkUnreachable:
                    return NetworkErrorType.NetworkUnreachable;
                case System.Net.Sockets.SocketError.HostUnreachable:
                    return NetworkErrorType.HostUnreachable;
                default:
                    return NetworkErrorType.SocketError;
            }
        }

        private int GetHttpStatusCode(System.Net.WebException webEx)
        {
            if (webEx.Response is System.Net.HttpWebResponse response)
            {
                return (int)response.StatusCode;
            }
            return 0;
        }

        private bool IsErrorRecoverable(NetworkErrorType errorType)
        {
            switch (errorType)
            {
                case NetworkErrorType.Timeout:
                case NetworkErrorType.ConnectionFailed:
                case NetworkErrorType.NetworkUnreachable:
                case NetworkErrorType.TemporaryUnavailable:
                    return true;

                case NetworkErrorType.AuthenticationFailed:
                case NetworkErrorType.AuthorizationFailed:
                case NetworkErrorType.InvalidRequest:
                case NetworkErrorType.NotFound:
                    return false;

                default:
                    return true; // 默认认为可恢复
            }
        }

        private string GenerateUserFriendlyMessage(NetworkError error)
        {
            switch (error.errorType)
            {
                case NetworkErrorType.ConnectionFailed:
                    return "无法连接到服务器，请检查网络连接";

                case NetworkErrorType.Timeout:
                    return "网络请求超时，请稍后重试";

                case NetworkErrorType.AuthenticationFailed:
                    return "登录已过期，请重新登录";

                case NetworkErrorType.AuthorizationFailed:
                    return "权限不足，无法访问此功能";

                case NetworkErrorType.ServerError:
                    return "服务器暂时不可用，请稍后重试";

                case NetworkErrorType.NetworkUnreachable:
                    return "网络不可达，请检查网络设置";

                case NetworkErrorType.DnsResolutionFailed:
                    return "域名解析失败，请检查DNS设置";

                default:
                    return _showTechnicalDetails ? error.message : "网络连接出现问题，请稍后重试";
            }
        }

        #endregion

        #region 错误记录和统计

        private void RecordError(NetworkError error)
        {
            // 添加到历史记录
            _errorHistory.Enqueue(error);

            // 限制历史记录大小
            while (_errorHistory.Count > _maxErrorHistory)
            {
                _errorHistory.Dequeue();
            }

            // 更新错误类型计数
            if (_errorTypeCount.ContainsKey(error.errorType))
            {
                _errorTypeCount[error.errorType]++;
            }
            else
            {
                _errorTypeCount[error.errorType] = 1;
            }

            // 记录日志
            if (_enableLogging)
            {
                LogError(error);
            }
        }

        private void LogError(NetworkError error)
        {
            var logLevel = GetLogLevel(error.severity);
            var message = $"[NetworkErrorHandler] {error.errorType}: {error.userFriendlyMessage}";

            if (_showTechnicalDetails)
            {
                message += $" | Context: {error.context} | Exception: {error.originalException?.GetType().Name}";
            }

            switch (logLevel)
            {
                case LogLevel.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogLevel.Error:
                    Debug.LogError(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }

        private LogLevel GetLogLevel(ErrorSeverity severity)
        {
            switch (severity)
            {
                case ErrorSeverity.Low:
                    return LogLevel.Info;
                case ErrorSeverity.Medium:
                    return LogLevel.Warning;
                case ErrorSeverity.High:
                case ErrorSeverity.Critical:
                    return LogLevel.Error;
                default:
                    return LogLevel.Info;
            }
        }

        #endregion

        #region 恢复策略

        private async Task<bool> AttemptRecovery(NetworkError error)
        {
            try
            {
                if (_recoveryStrategies.ContainsKey(error.errorType))
                {
                    var strategy = _recoveryStrategies[error.errorType];
                    var recovered = await strategy(error);

                    if (recovered && _enableLogging)
                    {
                        Debug.Log($"[NetworkErrorHandler] 错误恢复成功: {error.errorType}");
                    }

                    return recovered;
                }
                else
                {
                    // 默认恢复策略
                    return await DefaultRecoveryStrategy(error);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkErrorHandler] 恢复策略执行失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> DefaultRecoveryStrategy(NetworkError error)
        {
            // 简单的等待重试策略
            await Task.Delay(1000);
            return false; // 默认不恢复
        }

        private async Task<bool> AttemptReconnection(NetworkError error)
        {
            // 实现重连逻辑
            try
            {
                if (_enableLogging)
                {
                    Debug.Log("[NetworkErrorHandler] 尝试重新连接...");
                }

                // 这里应该调用实际的重连逻辑
                // 例如：networkService.ReconnectAsync()
                await Task.Delay(2000); // 模拟重连时间

                return true; // 假设重连成功
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkErrorHandler] 重连失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RetryLastOperation(NetworkError error)
        {
            // 实现重试逻辑
            try
            {
                if (_enableLogging)
                {
                    Debug.Log("[NetworkErrorHandler] 重试上次操作...");
                }

                // 这里应该调用实际的重试逻辑
                await Task.Delay(500); // 模拟重试时间

                return true; // 假设重试成功
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkErrorHandler] 重试失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RefreshAuthentication(NetworkError error)
        {
            // 实现认证刷新逻辑
            try
            {
                if (_enableLogging)
                {
                    Debug.Log("[NetworkErrorHandler] 刷新认证信息...");
                }

                // 这里应该调用实际的认证刷新逻辑
                await Task.Delay(1000); // 模拟认证时间

                return true; // 假设认证刷新成功
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkErrorHandler] 认证刷新失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> WaitForNetworkRecovery(NetworkError error)
        {
            // 等待网络恢复
            try
            {
                if (_enableLogging)
                {
                    Debug.Log("[NetworkErrorHandler] 等待网络恢复...");
                }

                // 检查网络可达性
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(1000);
                    
                    // 这里应该实际检查网络状态
                    if (Application.internetReachability != NetworkReachability.NotReachable)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkErrorHandler] 网络恢复检查失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 用户通知

        private void NotifyUser(NetworkError error)
        {
            var notificationKey = error.errorType.ToString();
            
            // 检查通知冷却时间
            if (_lastNotificationTime.ContainsKey(notificationKey))
            {
                var timeSinceLastNotification = DateTime.UtcNow - _lastNotificationTime[notificationKey];
                if (timeSinceLastNotification.TotalSeconds < _notificationCooldown)
                {
                    return; // 还在冷却时间内
                }
            }

            // 更新最后通知时间
            _lastNotificationTime[notificationKey] = DateTime.UtcNow;

            // 触发用户友好错误事件
            OnUserFriendlyError?.Invoke(error.userFriendlyMessage);

            if (_enableLogging)
            {
                Debug.Log($"[NetworkErrorHandler] 用户通知: {error.userFriendlyMessage}");
            }
        }

        #endregion

        #region 熔断器

        private void UpdateCircuitBreaker(NetworkError error, bool recovered)
        {
            switch (_circuitBreakerState)
            {
                case CircuitBreakerState.Closed:
                    if (!recovered)
                    {
                        _circuitBreakerFailureCount++;
                        
                        // 计算错误率
                        var recentErrors = GetRecentErrors(TimeSpan.FromMinutes(1));
                        var errorRate = recentErrors.Count(e => !e.isRecovered) / (float)Math.Max(recentErrors.Length, 1);
                        
                        if (errorRate >= _circuitBreakerThreshold || _circuitBreakerFailureCount >= _consecutiveErrorThreshold)
                        {
                            _circuitBreakerState = CircuitBreakerState.Open;
                            _circuitBreakerOpenTime = DateTime.UtcNow;
                            
                            if (_enableLogging)
                            {
                                Debug.LogWarning("[NetworkErrorHandler] 熔断器开启");
                            }
                        }
                    }
                    else
                    {
                        _circuitBreakerFailureCount = 0;
                    }
                    break;

                case CircuitBreakerState.Open:
                    // 检查是否可以进入半开状态
                    if (DateTime.UtcNow - _circuitBreakerOpenTime > TimeSpan.FromMinutes(1))
                    {
                        _circuitBreakerState = CircuitBreakerState.HalfOpen;
                        
                        if (_enableLogging)
                        {
                            Debug.Log("[NetworkErrorHandler] 熔断器进入半开状态");
                        }
                    }
                    break;

                case CircuitBreakerState.HalfOpen:
                    if (recovered)
                    {
                        _circuitBreakerState = CircuitBreakerState.Closed;
                        _circuitBreakerFailureCount = 0;
                        
                        if (_enableLogging)
                        {
                            Debug.Log("[NetworkErrorHandler] 熔断器关闭");
                        }
                    }
                    else
                    {
                        _circuitBreakerState = CircuitBreakerState.Open;
                        _circuitBreakerOpenTime = DateTime.UtcNow;
                        
                        if (_enableLogging)
                        {
                            Debug.LogWarning("[NetworkErrorHandler] 熔断器重新开启");
                        }
                    }
                    break;
            }
        }

        private NetworkError[] GetRecentErrors(TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.UtcNow - timeSpan;
            return _errorHistory.Where(e => e.timestamp >= cutoffTime).ToArray();
        }

        #endregion

        #region 异常模式检测

        private async Task HandleAbnormalPattern(ErrorPattern pattern)
        {
            if (_enableLogging)
            {
                Debug.LogWarning($"[NetworkErrorHandler] 检测到异常模式: {pattern.PatternType}");
            }

            // 根据模式类型采取不同的应对措施
            switch (pattern.PatternType)
            {
                case ErrorPatternType.ConsecutiveFailures:
                    await HandleConsecutiveFailures(pattern);
                    break;

                case ErrorPatternType.HighErrorRate:
                    await HandleHighErrorRate(pattern);
                    break;

                case ErrorPatternType.SpecificErrorSpike:
                    await HandleSpecificErrorSpike(pattern);
                    break;

                case ErrorPatternType.CascadingFailures:
                    await HandleCascadingFailures(pattern);
                    break;
            }
        }

        private async Task HandleConsecutiveFailures(ErrorPattern pattern)
        {
            // 处理连续失败
            if (_enableLogging)
            {
                Debug.LogWarning($"[NetworkErrorHandler] 连续失败 {pattern.ErrorCount} 次");
            }

            // 可能需要重置连接或切换服务器
            await Task.Delay(5000); // 等待较长时间
        }

        private async Task HandleHighErrorRate(ErrorPattern pattern)
        {
            // 处理高错误率
            if (_enableLogging)
            {
                Debug.LogWarning($"[NetworkErrorHandler] 高错误率: {pattern.ErrorRate:P}");
            }

            // 可能需要降级服务或启用缓存
            await Task.Delay(1000);
        }

        private async Task HandleSpecificErrorSpike(ErrorPattern pattern)
        {
            // 处理特定错误激增
            if (_enableLogging)
            {
                Debug.LogWarning($"[NetworkErrorHandler] 特定错误激增: {pattern.DominantErrorType}");
            }

            // 针对特定错误类型的处理
            await Task.Delay(2000);
        }

        private async Task HandleCascadingFailures(ErrorPattern pattern)
        {
            // 处理级联失败
            if (_enableLogging)
            {
                Debug.LogError("[NetworkErrorHandler] 检测到级联失败，可能需要紧急措施");
            }

            // 紧急措施，如完全重启网络组件
            await Task.Delay(10000);
        }

        #endregion

        #region 统计更新

        private void UpdateErrorStats(NetworkError error, bool recovered)
        {
            _errorStats.TotalErrors++;
            _errorStats.LastErrorTime = error.timestamp;

            if (recovered)
            {
                _errorStats.RecoveredErrors++;
            }

            // 计算错误率
            var recentErrors = GetRecentErrors(TimeSpan.FromMinutes(1));
            _errorStats.ErrorRate = recentErrors.Length;

            // 计算恢复率
            _errorStats.RecoveryRate = _errorStats.TotalErrors > 0 
                ? (float)_errorStats.RecoveredErrors / _errorStats.TotalErrors 
                : 0f;

            // 更新最严重的错误
            if (_errorStats.MostSevereError == null || error.severity > _errorStats.MostSevereError.severity)
            {
                _errorStats.MostSevereError = error;
            }

            // 更新最常见的错误类型
            var mostCommonType = _errorTypeCount.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
            _errorStats.MostCommonErrorType = mostCommonType.Key;

            // 触发统计更新事件
            OnErrorStatsUpdated?.Invoke(_errorStats);
        }

        #endregion

        #region 监控和清理

        private void StartErrorMonitoring()
        {
            // 启动定期监控协程
            StartCoroutine(ErrorMonitoringCoroutine());
        }

        private System.Collections.IEnumerator ErrorMonitoringCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(60f); // 每分钟检查一次

                // 清理过期的错误记录
                CleanupExpiredErrors();

                // 检查错误模式
                var pattern = _patternDetector.DetectPattern(_errorHistory.ToArray());
                if (pattern.IsAbnormal)
                {
                    _ = HandleAbnormalPattern(pattern);
                }

                // 重置通知冷却时间
                var expiredNotifications = _lastNotificationTime
                    .Where(kvp => DateTime.UtcNow - kvp.Value > TimeSpan.FromSeconds(_notificationCooldown))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredNotifications)
                {
                    _lastNotificationTime.Remove(key);
                }
            }
        }

        private void CleanupExpiredErrors()
        {
            var cutoffTime = DateTime.UtcNow - TimeSpan.FromHours(1); // 保留1小时内的错误
            var errorsToRemove = new Queue<NetworkError>();

            foreach (var error in _errorHistory)
            {
                if (error.timestamp < cutoffTime)
                {
                    errorsToRemove.Enqueue(error);
                }
                else
                {
                    break; // 队列是按时间排序的
                }
            }

            while (errorsToRemove.Count > 0)
            {
                _errorHistory.Dequeue();
            }
        }

        private void Cleanup()
        {
            StopAllCoroutines();
            _errorHistory?.Clear();
            _errorTypeCount?.Clear();
            _lastNotificationTime?.Clear();
            _recoveryStrategies?.Clear();
            _activeRecoveryActions?.Clear();

            if (_enableLogging)
            {
                Debug.Log("[NetworkErrorHandler] 资源清理完成");
            }
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 网络错误
    /// </summary>
    [System.Serializable]
    public class NetworkError
    {
        public string id;
        public DateTime timestamp;
        public NetworkErrorType errorType;
        public ErrorSeverity severity;
        public string message;
        public string userFriendlyMessage;
        public string context;
        public int httpStatusCode;
        public bool isRecoverable;
        public bool isRecovered;
        public Exception originalException;
        public Dictionary<string, object> metadata;
    }

    /// <summary>
    /// 网络错误类型
    /// </summary>
    public enum NetworkErrorType
    {
        Unknown,
        ConnectionFailed,
        Timeout,
        AuthenticationFailed,
        AuthorizationFailed,
        HttpError,
        SocketError,
        DnsResolutionFailed,
        NetworkUnreachable,
        HostUnreachable,
        ConnectionRefused,
        ServerError,
        InvalidRequest,
        NotFound,
        ProtocolError,
        TemporaryUnavailable
    }

    /// <summary>
    /// 错误严重程度
    /// </summary>
    public enum ErrorSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// 熔断器状态
    /// </summary>
    public enum CircuitBreakerState
    {
        Closed,   // 正常状态
        Open,     // 熔断开启
        HalfOpen  // 半开状态（测试恢复）
    }

    /// <summary>
    /// 恢复动作
    /// </summary>
    [System.Serializable]
    public class RecoveryAction
    {
        public string id;
        public string description;
        public RecoveryActionType actionType;
        public DateTime scheduledTime;
        public bool isCompleted;
        public string result;
    }

    /// <summary>
    /// 恢复动作类型
    /// </summary>
    public enum RecoveryActionType
    {
        Retry,
        Reconnect,
        RefreshAuth,
        SwitchServer,
        Restart,
        WaitAndRetry
    }

    /// <summary>
    /// 网络错误统计
    /// </summary>
    [System.Serializable]
    public class NetworkErrorStats
    {
        public int TotalErrors { get; set; }
        public int RecoveredErrors { get; set; }
        public float RecoveryRate { get; set; }
        public int ErrorRate { get; set; } // 每分钟错误数
        public DateTime LastErrorTime { get; set; }
        public NetworkError MostSevereError { get; set; }
        public NetworkErrorType MostCommonErrorType { get; set; }

        public void ResetStats()
        {
            TotalErrors = 0;
            RecoveredErrors = 0;
            RecoveryRate = 0f;
            ErrorRate = 0;
            LastErrorTime = default(DateTime);
            MostSevereError = null;
            MostCommonErrorType = NetworkErrorType.Unknown;
        }
    }

    /// <summary>
    /// 错误处理配置
    /// </summary>
    [System.Serializable]
    public class NetworkErrorHandlingConfig
    {
        public bool enableLogging = true;
        public bool enableUserNotification = true;
        public bool enableAutoRecovery = true;
        public int consecutiveErrorThreshold = 5;
        public int errorRateThreshold = 10;
        public float circuitBreakerThreshold = 0.5f;
        public float notificationCooldown = 5f;
        public bool showTechnicalDetails = false;
    }

    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    #endregion

    #region 错误模式检测

    /// <summary>
    /// 错误模式检测器
    /// </summary>
    public class ErrorPatternDetector
    {
        public ErrorPattern DetectPattern(NetworkError[] errors)
        {
            if (errors == null || errors.Length == 0)
            {
                return new ErrorPattern { IsAbnormal = false };
            }

            // 检测连续失败
            var consecutiveFailures = DetectConsecutiveFailures(errors);
            if (consecutiveFailures.IsAbnormal)
            {
                return consecutiveFailures;
            }

            // 检测高错误率
            var highErrorRate = DetectHighErrorRate(errors);
            if (highErrorRate.IsAbnormal)
            {
                return highErrorRate;
            }

            // 检测特定错误激增
            var errorSpike = DetectSpecificErrorSpike(errors);
            if (errorSpike.IsAbnormal)
            {
                return errorSpike;
            }

            // 检测级联失败
            var cascadingFailures = DetectCascadingFailures(errors);
            if (cascadingFailures.IsAbnormal)
            {
                return cascadingFailures;
            }

            return new ErrorPattern { IsAbnormal = false };
        }

        private ErrorPattern DetectConsecutiveFailures(NetworkError[] errors)
        {
            var recentErrors = errors.Where(e => DateTime.UtcNow - e.timestamp < TimeSpan.FromMinutes(5)).ToArray();
            var consecutiveCount = 0;
            var maxConsecutive = 0;

            foreach (var error in recentErrors.OrderBy(e => e.timestamp))
            {
                if (!error.isRecovered)
                {
                    consecutiveCount++;
                    maxConsecutive = Math.Max(maxConsecutive, consecutiveCount);
                }
                else
                {
                    consecutiveCount = 0;
                }
            }

            return new ErrorPattern
            {
                IsAbnormal = maxConsecutive >= 5,
                PatternType = ErrorPatternType.ConsecutiveFailures,
                ErrorCount = maxConsecutive,
                TimeWindow = TimeSpan.FromMinutes(5)
            };
        }

        private ErrorPattern DetectHighErrorRate(NetworkError[] errors)
        {
            var recentErrors = errors.Where(e => DateTime.UtcNow - e.timestamp < TimeSpan.FromMinutes(1)).ToArray();
            var errorRate = recentErrors.Length / 1.0f; // 每分钟错误数

            return new ErrorPattern
            {
                IsAbnormal = errorRate > 10, // 每分钟超过10个错误
                PatternType = ErrorPatternType.HighErrorRate,
                ErrorRate = errorRate,
                TimeWindow = TimeSpan.FromMinutes(1)
            };
        }

        private ErrorPattern DetectSpecificErrorSpike(NetworkError[] errors)
        {
            var recentErrors = errors.Where(e => DateTime.UtcNow - e.timestamp < TimeSpan.FromMinutes(5)).ToArray();
            var errorTypeCounts = recentErrors.GroupBy(e => e.errorType).ToDictionary(g => g.Key, g => g.Count());

            foreach (var kvp in errorTypeCounts)
            {
                if (kvp.Value >= 5) // 5分钟内同类型错误超过5次
                {
                    return new ErrorPattern
                    {
                        IsAbnormal = true,
                        PatternType = ErrorPatternType.SpecificErrorSpike,
                        DominantErrorType = kvp.Key,
                        ErrorCount = kvp.Value,
                        TimeWindow = TimeSpan.FromMinutes(5)
                    };
                }
            }

            return new ErrorPattern { IsAbnormal = false };
        }

        private ErrorPattern DetectCascadingFailures(NetworkError[] errors)
        {
            var recentErrors = errors.Where(e => DateTime.UtcNow - e.timestamp < TimeSpan.FromMinutes(2)).ToArray();
            var severeErrors = recentErrors.Where(e => e.severity >= ErrorSeverity.High).Count();

            return new ErrorPattern
            {
                IsAbnormal = severeErrors >= 3, // 2分钟内3个以上严重错误
                PatternType = ErrorPatternType.CascadingFailures,
                ErrorCount = severeErrors,
                TimeWindow = TimeSpan.FromMinutes(2)
            };
        }
    }

    /// <summary>
    /// 错误模式
    /// </summary>
    [System.Serializable]
    public class ErrorPattern
    {
        public bool IsAbnormal { get; set; }
        public ErrorPatternType PatternType { get; set; }
        public int ErrorCount { get; set; }
        public float ErrorRate { get; set; }
        public NetworkErrorType DominantErrorType { get; set; }
        public TimeSpan TimeWindow { get; set; }
    }

    /// <summary>
    /// 错误模式类型
    /// </summary>
    public enum ErrorPatternType
    {
        ConsecutiveFailures,  // 连续失败
        HighErrorRate,        // 高错误率
        SpecificErrorSpike,   // 特定错误激增
        CascadingFailures     // 级联失败
    }

    #endregion
}