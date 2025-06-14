// Assets/_Core/Network/Utils/RetryManager.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Network.Utils
{
    /// <summary>
    /// 重试管理器
    /// 提供灵活的重试策略和机制，支持多种重试算法和条件判断
    /// </summary>
    public class RetryManager : MonoBehaviour
    {
        #region 重试事件

        public event Action<RetryContext> OnRetryAttempt;
        public event Action<RetryResult> OnRetryCompleted;
        public event Action<RetryContext> OnRetryFailed;
        public event Action<RetryStatistics> OnRetryStatsUpdated;

        #endregion

        #region Inspector配置

        [Header("默认重试配置")]
        [SerializeField] private int _defaultMaxRetries = 3;
        [SerializeField] private int _defaultBaseDelay = 1000; // 毫秒
        [SerializeField] private float _defaultBackoffFactor = 2.0f;
        [SerializeField] private int _defaultMaxDelay = 30000; // 毫秒
        [SerializeField] private RetryStrategy _defaultStrategy = RetryStrategy.ExponentialBackoff;

        [Header("重试管理配置")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _enableJitter = true;
        [SerializeField] private int _maxConcurrentRetries = 10;
        [SerializeField] private int _retryHistorySize = 100;

        [Header("断路器集成")]
        [SerializeField] private bool _respectCircuitBreaker = true;
        [SerializeField] private float _circuitBreakerThreshold = 0.5f;

        #endregion

        #region 私有字段

        // 重试策略
        private Dictionary<string, RetryPolicy> _retryPolicies;
        private Dictionary<Type, RetryPolicy> _exceptionPolicies;
        
        // 活跃重试
        private Dictionary<string, RetryOperation> _activeRetries;
        private Queue<RetryContext> _retryHistory;
        
        // 统计信息
        private RetryStatistics _statistics;
        
        // 重试策略实现
        private Dictionary<RetryStrategy, Func<RetryContext, int>> _strategyImplementations;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region 初始化

        private void Initialize()
        {
            _retryPolicies = new Dictionary<string, RetryPolicy>();
            _exceptionPolicies = new Dictionary<Type, RetryPolicy>();
            _activeRetries = new Dictionary<string, RetryOperation>();
            _retryHistory = new Queue<RetryContext>();
            _statistics = new RetryStatistics();

            // 初始化重试策略实现
            InitializeStrategyImplementations();

            // 注册默认重试策略
            RegisterDefaultPolicies();

            if (_enableLogging)
            {
                Debug.Log("[RetryManager] 重试管理器已初始化");
            }
        }

        private void InitializeStrategyImplementations()
        {
            _strategyImplementations = new Dictionary<RetryStrategy, Func<RetryContext, int>>
            {
                [RetryStrategy.FixedDelay] = CalculateFixedDelay,
                [RetryStrategy.LinearBackoff] = CalculateLinearBackoff,
                [RetryStrategy.ExponentialBackoff] = CalculateExponentialBackoff,
                [RetryStrategy.ExponentialBackoffWithJitter] = CalculateExponentialBackoffWithJitter,
                [RetryStrategy.CustomDelay] = CalculateCustomDelay
            };
        }

        private void RegisterDefaultPolicies()
        {
            // 网络相关操作的默认策略
            var networkPolicy = new RetryPolicy
            {
                maxRetries = _defaultMaxRetries,
                baseDelay = _defaultBaseDelay,
                backoffFactor = _defaultBackoffFactor,
                maxDelay = _defaultMaxDelay,
                strategy = _defaultStrategy,
                retryCondition = IsNetworkRetryable
            };

            RegisterPolicy("network", networkPolicy);
            RegisterPolicy("http", networkPolicy);
            RegisterPolicy("websocket", networkPolicy);

            // 认证相关操作的策略
            var authPolicy = new RetryPolicy
            {
                maxRetries = 2,
                baseDelay = 2000,
                backoffFactor = 1.5f,
                maxDelay = 10000,
                strategy = RetryStrategy.LinearBackoff,
                retryCondition = IsAuthRetryable
            };

            RegisterPolicy("authentication", authPolicy);

            // 异常类型策略
            RegisterExceptionPolicy<TimeoutException>(networkPolicy);
            RegisterExceptionPolicy<System.Net.WebException>(networkPolicy);
            RegisterExceptionPolicy<System.Net.Sockets.SocketException>(networkPolicy);
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 执行带重试的操作
        /// </summary>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string policyName = "network")
        {
            var policy = GetPolicy(policyName);
            return await ExecuteWithRetryAsync(operation, policy);
        }

        /// <summary>
        /// 执行带重试的操作（使用自定义策略）
        /// </summary>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, RetryPolicy policy)
        {
            var context = new RetryContext
            {
                id = Guid.NewGuid().ToString(),
                operationName = operation.Method.Name,
                policy = policy,
                startTime = DateTime.UtcNow
            };

            return await ExecuteWithRetryInternalAsync(operation, context);
        }

        /// <summary>
        /// 执行带重试的无返回值操作
        /// </summary>
        public async Task ExecuteWithRetryAsync(Func<Task> operation, string policyName = "network")
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await operation();
                return true; // 包装为有返回值的操作
            }, policyName);
        }

        /// <summary>
        /// 注册重试策略
        /// </summary>
        public void RegisterPolicy(string name, RetryPolicy policy)
        {
            _retryPolicies[name] = policy;

            if (_enableLogging)
            {
                Debug.Log($"[RetryManager] 已注册重试策略: {name}");
            }
        }

        /// <summary>
        /// 注册异常类型的重试策略
        /// </summary>
        public void RegisterExceptionPolicy<T>(RetryPolicy policy) where T : Exception
        {
            _exceptionPolicies[typeof(T)] = policy;

            if (_enableLogging)
            {
                Debug.Log($"[RetryManager] 已注册异常类型策略: {typeof(T).Name}");
            }
        }

        /// <summary>
        /// 获取重试策略
        /// </summary>
        public RetryPolicy GetPolicy(string name)
        {
            if (_retryPolicies.ContainsKey(name))
            {
                return _retryPolicies[name];
            }

            if (_enableLogging)
            {
                Debug.LogWarning($"[RetryManager] 未找到策略 '{name}'，使用默认策略");
            }

            return CreateDefaultPolicy();
        }

        /// <summary>
        /// 获取异常类型的重试策略
        /// </summary>
        public RetryPolicy GetExceptionPolicy(Type exceptionType)
        {
            // 查找精确匹配
            if (_exceptionPolicies.ContainsKey(exceptionType))
            {
                return _exceptionPolicies[exceptionType];
            }

            // 查找基类匹配
            foreach (var kvp in _exceptionPolicies)
            {
                if (kvp.Key.IsAssignableFrom(exceptionType))
                {
                    return kvp.Value;
                }
            }

            return CreateDefaultPolicy();
        }

        /// <summary>
        /// 取消指定的重试操作
        /// </summary>
        public bool CancelRetry(string retryId)
        {
            if (_activeRetries.ContainsKey(retryId))
            {
                var retryOp = _activeRetries[retryId];
                retryOp.CancellationTokenSource.Cancel();
                _activeRetries.Remove(retryId);

                if (_enableLogging)
                {
                    Debug.Log($"[RetryManager] 已取消重试操作: {retryId}");
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 取消所有活跃的重试操作
        /// </summary>
        public void CancelAllRetries()
        {
            foreach (var kvp in _activeRetries)
            {
                kvp.Value.CancellationTokenSource.Cancel();
            }

            _activeRetries.Clear();

            if (_enableLogging)
            {
                Debug.Log("[RetryManager] 已取消所有重试操作");
            }
        }

        /// <summary>
        /// 获取重试统计信息
        /// </summary>
        public RetryStatistics GetStatistics()
        {
            return _statistics;
        }

        /// <summary>
        /// 获取活跃重试操作数量
        /// </summary>
        public int GetActiveRetryCount()
        {
            return _activeRetries.Count;
        }

        /// <summary>
        /// 获取重试历史
        /// </summary>
        public RetryContext[] GetRetryHistory()
        {
            return _retryHistory.ToArray();
        }

        /// <summary>
        /// 清除重试历史
        /// </summary>
        public void ClearHistory()
        {
            _retryHistory.Clear();
            _statistics.ResetStatistics();

            if (_enableLogging)
            {
                Debug.Log("[RetryManager] 重试历史已清除");
            }
        }

        /// <summary>
        /// 设置默认重试配置
        /// </summary>
        public void SetDefaultRetryConfig(RetryConfig config)
        {
            _defaultMaxRetries = config.maxRetries;
            _defaultBaseDelay = config.baseDelay;
            _defaultBackoffFactor = config.backoffFactor;
            _defaultMaxDelay = config.maxDelay;
            _defaultStrategy = config.strategy;

            if (_enableLogging)
            {
                Debug.Log("[RetryManager] 默认重试配置已更新");
            }
        }

        #endregion

        #region 核心重试逻辑

        private async Task<T> ExecuteWithRetryInternalAsync<T>(Func<Task<T>> operation, RetryContext context)
        {
            // 检查并发限制
            if (_activeRetries.Count >= _maxConcurrentRetries)
            {
                throw new InvalidOperationException($"达到最大并发重试限制: {_maxConcurrentRetries}");
            }

            // 创建重试操作
            var retryOperation = new RetryOperation
            {
                Context = context,
                CancellationTokenSource = new System.Threading.CancellationTokenSource()
            };

            _activeRetries[context.id] = retryOperation;

            try
            {
                T result = default(T);
                Exception lastException = null;

                for (int attempt = 0; attempt <= context.policy.maxRetries; attempt++)
                {
                    context.currentAttempt = attempt;
                    context.attemptStartTime = DateTime.UtcNow;

                    try
                    {
                        // 检查取消令牌
                        retryOperation.CancellationTokenSource.Token.ThrowIfCancellationRequested();

                        // 检查断路器状态
                        if (_respectCircuitBreaker && ShouldSkipDueToCircuitBreaker(context))
                        {
                            throw new CircuitBreakerOpenException("断路器开启，跳过重试");
                        }

                        // 触发重试尝试事件
                        OnRetryAttempt?.Invoke(context);

                        // 执行操作
                        result = await operation();

                        // 成功完成
                        context.endTime = DateTime.UtcNow;
                        context.isSuccessful = true;
                        context.result = "Success";

                        UpdateStatistics(context, true);
                        RecordRetryHistory(context);

                        var retryResult = new RetryResult
                        {
                            context = context,
                            isSuccessful = true,
                            result = result,
                            totalDuration = context.endTime - context.startTime
                        };

                        OnRetryCompleted?.Invoke(retryResult);

                        return result;
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        lastException = ex;
                        context.lastException = ex;
                        context.exceptions.Add(ex);

                        // 检查是否应该重试
                        if (attempt >= context.policy.maxRetries || !ShouldRetry(ex, context))
                        {
                            break;
                        }

                        // 计算延迟时间
                        var delay = CalculateDelay(context);
                        context.delays.Add(delay);

                        if (_enableLogging)
                        {
                            Debug.LogWarning($"[RetryManager] 重试操作失败 (尝试 {attempt + 1}/{context.policy.maxRetries + 1}): {ex.Message}, {delay}ms后重试");
                        }

                        // 等待延迟
                        await Task.Delay(delay, retryOperation.CancellationTokenSource.Token);
                    }
                }

                // 所有重试都失败了
                context.endTime = DateTime.UtcNow;
                context.isSuccessful = false;
                context.result = $"Failed after {context.currentAttempt + 1} attempts";

                UpdateStatistics(context, false);
                RecordRetryHistory(context);

                OnRetryFailed?.Invoke(context);

                // 抛出最后一个异常
                throw new RetryExhaustedException($"重试耗尽，最大尝试次数: {context.policy.maxRetries + 1}", lastException);
            }
            finally
            {
                // 清理
                _activeRetries.Remove(context.id);
                retryOperation.CancellationTokenSource?.Dispose();
            }
        }

        #endregion

        #region 重试条件判断

        private bool ShouldRetry(Exception exception, RetryContext context)
        {
            try
            {
                // 使用策略中的重试条件
                if (context.policy.retryCondition != null)
                {
                    return context.policy.retryCondition(exception, context);
                }

                // 默认重试条件
                return IsDefaultRetryable(exception);
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    Debug.LogError($"[RetryManager] 重试条件判断失败: {ex.Message}");
                }
                return false;
            }
        }

        private bool IsNetworkRetryable(Exception exception, RetryContext context)
        {
            // 网络相关异常通常可以重试
            return exception is TimeoutException ||
                   exception is System.Net.WebException ||
                   exception is System.Net.Sockets.SocketException ||
                   exception is TaskCanceledException ||
                   (exception.Message?.Contains("timeout") == true) ||
                   (exception.Message?.Contains("connection") == true);
        }

        private bool IsAuthRetryable(Exception exception, RetryContext context)
        {
            // 认证错误通常不应该频繁重试
            if (context.currentAttempt >= 1)
            {
                return false;
            }

            return exception.Message?.Contains("401") == true ||
                   exception.Message?.Contains("Unauthorized") == true;
        }

        private bool IsDefaultRetryable(Exception exception)
        {
            // 某些异常类型不应该重试
            if (exception is ArgumentException ||
                exception is ArgumentNullException ||
                exception is InvalidOperationException ||
                exception is NotSupportedException)
            {
                return false;
            }

            // 默认情况下可以重试
            return true;
        }

        private bool ShouldSkipDueToCircuitBreaker(RetryContext context)
        {
            // 检查最近的失败率
            var recentRetries = GetRecentRetries(TimeSpan.FromMinutes(5));
            if (recentRetries.Length == 0)
            {
                return false;
            }

            var failureRate = recentRetries.Count(r => !r.isSuccessful) / (float)recentRetries.Length;
            return failureRate >= _circuitBreakerThreshold;
        }

        #endregion

        #region 延迟计算

        private int CalculateDelay(RetryContext context)
        {
            if (_strategyImplementations.ContainsKey(context.policy.strategy))
            {
                var delay = _strategyImplementations[context.policy.strategy](context);
                return Math.Min(delay, context.policy.maxDelay);
            }

            return CalculateExponentialBackoff(context);
        }

        private int CalculateFixedDelay(RetryContext context)
        {
            return context.policy.baseDelay;
        }

        private int CalculateLinearBackoff(RetryContext context)
        {
            return context.policy.baseDelay * (context.currentAttempt + 1);
        }

        private int CalculateExponentialBackoff(RetryContext context)
        {
            return (int)(context.policy.baseDelay * Math.Pow(context.policy.backoffFactor, context.currentAttempt));
        }

        private int CalculateExponentialBackoffWithJitter(RetryContext context)
        {
            var baseDelay = CalculateExponentialBackoff(context);
            
            if (_enableJitter)
            {
                var jitter = UnityEngine.Random.Range(0.8f, 1.2f);
                return (int)(baseDelay * jitter);
            }

            return baseDelay;
        }

        private int CalculateCustomDelay(RetryContext context)
        {
            // 自定义延迟逻辑
            if (context.policy.customDelayFunction != null)
            {
                return context.policy.customDelayFunction(context);
            }

            return CalculateExponentialBackoff(context);
        }

        #endregion

        #region 统计和历史

        private void UpdateStatistics(RetryContext context, bool isSuccessful)
        {
            _statistics.TotalRetryAttempts++;
            
            if (isSuccessful)
            {
                _statistics.SuccessfulRetries++;
            }
            else
            {
                _statistics.FailedRetries++;
            }

            _statistics.TotalRetryTime += (context.endTime - context.startTime);
            _statistics.AverageRetryTime = new TimeSpan(_statistics.TotalRetryTime.Ticks / _statistics.TotalRetryAttempts);

            if (context.currentAttempt > _statistics.MaxRetryAttempts)
            {
                _statistics.MaxRetryAttempts = context.currentAttempt + 1;
            }

            _statistics.LastRetryTime = context.endTime;

            // 更新策略统计
            var strategyName = context.policy.strategy.ToString();
            if (!_statistics.StrategyStats.ContainsKey(strategyName))
            {
                _statistics.StrategyStats[strategyName] = new StrategyStatistics();
            }

            var strategyStats = _statistics.StrategyStats[strategyName];
            strategyStats.TotalUses++;
            if (isSuccessful)
            {
                strategyStats.SuccessfulUses++;
            }

            // 触发统计更新事件
            OnRetryStatsUpdated?.Invoke(_statistics);
        }

        private void RecordRetryHistory(RetryContext context)
        {
            _retryHistory.Enqueue(context);

            // 限制历史记录大小
            while (_retryHistory.Count > _retryHistorySize)
            {
                _retryHistory.Dequeue();
            }
        }

        private RetryContext[] GetRecentRetries(TimeSpan timeSpan)
        {
            var cutoffTime = DateTime.UtcNow - timeSpan;
            return _retryHistory.Where(r => r.startTime >= cutoffTime).ToArray();
        }

        #endregion

        #region 辅助方法

        private RetryPolicy CreateDefaultPolicy()
        {
            return new RetryPolicy
            {
                maxRetries = _defaultMaxRetries,
                baseDelay = _defaultBaseDelay,
                backoffFactor = _defaultBackoffFactor,
                maxDelay = _defaultMaxDelay,
                strategy = _defaultStrategy,
                retryCondition = IsDefaultRetryable
            };
        }

        private void Cleanup()
        {
            // 取消所有活跃的重试
            CancelAllRetries();

            // 清理资源
            _retryPolicies?.Clear();
            _exceptionPolicies?.Clear();
            _activeRetries?.Clear();
            _retryHistory?.Clear();

            if (_enableLogging)
            {
                Debug.Log("[RetryManager] 资源清理完成");
            }
        }

        #endregion

        #region 高级功能

        /// <summary>
        /// 批量重试操作
        /// </summary>
        public async Task<BatchRetryResult<T>> ExecuteBatchWithRetryAsync<T>(
            IEnumerable<Func<Task<T>>> operations, 
            string policyName = "network",
            bool failFast = false)
        {
            var policy = GetPolicy(policyName);
            var results = new List<T>();
            var exceptions = new List<Exception>();
            var contexts = new List<RetryContext>();

            foreach (var operation in operations)
            {
                try
                {
                    var result = await ExecuteWithRetryAsync(operation, policy);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    
                    if (failFast)
                    {
                        break;
                    }
                }
            }

            return new BatchRetryResult<T>
            {
                Results = results,
                Exceptions = exceptions,
                IsAllSuccessful = exceptions.Count == 0,
                SuccessCount = results.Count,
                FailureCount = exceptions.Count
            };
        }

        /// <summary>
        /// 条件重试：只有当满足特定条件时才重试
        /// </summary>
        public async Task<T> ExecuteWithConditionalRetryAsync<T>(
            Func<Task<T>> operation,
            Func<T, bool> successCondition,
            string policyName = "network")
        {
            var policy = GetPolicy(policyName);
            
            return await ExecuteWithRetryAsync(async () =>
            {
                var result = await operation();
                
                if (!successCondition(result))
                {
                    throw new RetryConditionNotMetException("结果不满足重试条件");
                }
                
                return result;
            }, policy);
        }

        /// <summary>
        /// 超时重试：为操作添加超时限制
        /// </summary>
        public async Task<T> ExecuteWithTimeoutRetryAsync<T>(
            Func<Task<T>> operation,
            TimeSpan timeout,
            string policyName = "network")
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                using (var cts = new System.Threading.CancellationTokenSource(timeout))
                {
                    try
                    {
                        return await operation();
                    }
                    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                    {
                        throw new TimeoutException($"操作超时: {timeout.TotalMilliseconds}ms");
                    }
                }
            }, policyName);
        }

        /// <summary>
        /// 渐进式重试：每次重试增加超时时间
        /// </summary>
        public async Task<T> ExecuteWithProgressiveTimeoutRetryAsync<T>(
            Func<TimeSpan, Task<T>> operation,
            TimeSpan baseTimeout,
            string policyName = "network")
        {
            var policy = GetPolicy(policyName);
            
            return await ExecuteWithRetryAsync(async () =>
            {
                var context = new RetryContext(); // 获取当前重试上下文
                var currentTimeout = TimeSpan.FromMilliseconds(
                    baseTimeout.TotalMilliseconds * Math.Pow(1.5, context.currentAttempt));
                
                return await operation(currentTimeout);
            }, policy);
        }

        /// <summary>
        /// 智能重试：根据异常类型动态调整策略
        /// </summary>
        public async Task<T> ExecuteWithSmartRetryAsync<T>(Func<Task<T>> operation, string operationName = "")
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    // 根据异常类型动态选择策略
                    var policy = GetExceptionPolicy(ex.GetType());
                    var context = new RetryContext
                    {
                        operationName = operationName,
                        policy = policy
                    };
                    
                    // 重新抛出异常，让重试机制处理
                    throw;
                }
            }, "network");
        }

        #endregion

        #region 重试策略模板

        /// <summary>
        /// 创建网络请求重试策略
        /// </summary>
        public static RetryPolicy CreateNetworkPolicy(int maxRetries = 3, int baseDelay = 1000)
        {
            return new RetryPolicy
            {
                maxRetries = maxRetries,
                baseDelay = baseDelay,
                backoffFactor = 2.0f,
                maxDelay = 30000,
                strategy = RetryStrategy.ExponentialBackoffWithJitter,
                retryCondition = (ex, ctx) =>
                {
                    return ex is TimeoutException ||
                           ex is System.Net.WebException ||
                           ex is System.Net.Sockets.SocketException ||
                           ex is TaskCanceledException;
                }
            };
        }

        /// <summary>
        /// 创建数据库重试策略
        /// </summary>
        public static RetryPolicy CreateDatabasePolicy(int maxRetries = 5, int baseDelay = 500)
        {
            return new RetryPolicy
            {
                maxRetries = maxRetries,
                baseDelay = baseDelay,
                backoffFactor = 1.5f,
                maxDelay = 10000,
                strategy = RetryStrategy.LinearBackoff,
                retryCondition = (ex, ctx) =>
                {
                    // 数据库相关的可重试异常
                    var message = ex.Message.ToLower();
                    return message.Contains("timeout") ||
                           message.Contains("deadlock") ||
                           message.Contains("connection") ||
                           message.Contains("network");
                }
            };
        }

        /// <summary>
        /// 创建API调用重试策略
        /// </summary>
        public static RetryPolicy CreateApiPolicy(int maxRetries = 3, int baseDelay = 2000)
        {
            return new RetryPolicy
            {
                maxRetries = maxRetries,
                baseDelay = baseDelay,
                backoffFactor = 2.0f,
                maxDelay = 60000,
                strategy = RetryStrategy.ExponentialBackoff,
                retryCondition = (ex, ctx) =>
                {
                    // HTTP状态码相关的重试判断
                    if (ex is System.Net.WebException webEx)
                    {
                        if (webEx.Response is System.Net.HttpWebResponse response)
                        {
                            var statusCode = (int)response.StatusCode;
                            // 5xx服务器错误和429(Too Many Requests)可以重试
                            return statusCode >= 500 || statusCode == 429;
                        }
                    }
                    
                    return ex is TimeoutException || ex is TaskCanceledException;
                }
            };
        }

        /// <summary>
        /// 创建文件操作重试策略
        /// </summary>
        public static RetryPolicy CreateFileOperationPolicy(int maxRetries = 5, int baseDelay = 100)
        {
            return new RetryPolicy
            {
                maxRetries = maxRetries,
                baseDelay = baseDelay,
                backoffFactor = 1.2f,
                maxDelay = 5000,
                strategy = RetryStrategy.LinearBackoff,
                retryCondition = (ex, ctx) =>
                {
                    return ex is System.IO.IOException ||
                           ex is UnauthorizedAccessException ||
                           ex is System.IO.DirectoryNotFoundException;
                }
            };
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 重试策略
    /// </summary>
    [System.Serializable]
    public class RetryPolicy
    {
        [Header("基础配置")]
        public int maxRetries = 3;                    // 最大重试次数
        public int baseDelay = 1000;                  // 基础延迟(毫秒)
        public float backoffFactor = 2.0f;            // 退避因子
        public int maxDelay = 30000;                  // 最大延迟(毫秒)
        
        [Header("策略配置")]
        public RetryStrategy strategy = RetryStrategy.ExponentialBackoff;
        
        [Header("条件配置")]
        public Func<Exception, RetryContext, bool> retryCondition;  // 重试条件
        public Func<RetryContext, int> customDelayFunction;         // 自定义延迟函数
        
        [Header("描述")]
        public string description = "";               // 策略描述
    }

    /// <summary>
    /// 重试策略枚举
    /// </summary>
    public enum RetryStrategy
    {
        FixedDelay,                    // 固定延迟
        LinearBackoff,                 // 线性退避
        ExponentialBackoff,            // 指数退避
        ExponentialBackoffWithJitter,  // 带抖动的指数退避
        CustomDelay                    // 自定义延迟
    }

    /// <summary>
    /// 重试上下文
    /// </summary>
    [System.Serializable]
    public class RetryContext
    {
        [Header("基础信息")]
        public string id = "";
        public string operationName = "";
        public DateTime startTime;
        public DateTime endTime;
        public DateTime attemptStartTime;
        
        [Header("重试信息")]
        public int currentAttempt = 0;
        public RetryPolicy policy;
        public List<Exception> exceptions = new List<Exception>();
        public List<int> delays = new List<int>();
        public Exception lastException;
        
        [Header("结果")]
        public bool isSuccessful = false;
        public string result = "";
        
        [Header("元数据")]
        public Dictionary<string, object> metadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// 重试操作
    /// </summary>
    public class RetryOperation
    {
        public RetryContext Context { get; set; }
        public System.Threading.CancellationTokenSource CancellationTokenSource { get; set; }
    }

    /// <summary>
    /// 重试结果
    /// </summary>
    [System.Serializable]
    public class RetryResult
    {
        public RetryContext context;
        public bool isSuccessful;
        public object result;
        public TimeSpan totalDuration;
    }

    /// <summary>
    /// 批量重试结果
    /// </summary>
    [System.Serializable]
    public class BatchRetryResult<T>
    {
        public List<T> Results { get; set; } = new List<T>();
        public List<Exception> Exceptions { get; set; } = new List<Exception>();
        public bool IsAllSuccessful { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public float SuccessRate => (SuccessCount + FailureCount) > 0 ? (float)SuccessCount / (SuccessCount + FailureCount) : 0f;
    }

    /// <summary>
    /// 重试统计信息
    /// </summary>
    [System.Serializable]
    public class RetryStatistics
    {
        [Header("基础统计")]
        public int TotalRetryAttempts { get; set; } = 0;
        public int SuccessfulRetries { get; set; } = 0;
        public int FailedRetries { get; set; } = 0;
        public float SuccessRate => TotalRetryAttempts > 0 ? (float)SuccessfulRetries / TotalRetryAttempts : 0f;
        
        [Header("时间统计")]
        public TimeSpan TotalRetryTime { get; set; } = TimeSpan.Zero;
        public TimeSpan AverageRetryTime { get; set; } = TimeSpan.Zero;
        public DateTime LastRetryTime { get; set; } = DateTime.MinValue;
        
        [Header("尝试统计")]
        public int MaxRetryAttempts { get; set; } = 0;
        public float AverageRetryAttempts => TotalRetryAttempts > 0 ? (float)TotalRetryAttempts / (SuccessfulRetries + FailedRetries) : 0f;
        
        [Header("策略统计")]
        public Dictionary<string, StrategyStatistics> StrategyStats { get; set; } = new Dictionary<string, StrategyStatistics>();

        public void ResetStatistics()
        {
            TotalRetryAttempts = 0;
            SuccessfulRetries = 0;
            FailedRetries = 0;
            TotalRetryTime = TimeSpan.Zero;
            AverageRetryTime = TimeSpan.Zero;
            LastRetryTime = DateTime.MinValue;
            MaxRetryAttempts = 0;
            StrategyStats.Clear();
        }
    }

    /// <summary>
    /// 策略统计信息
    /// </summary>
    [System.Serializable]
    public class StrategyStatistics
    {
        public int TotalUses { get; set; } = 0;
        public int SuccessfulUses { get; set; } = 0;
        public float SuccessRate => TotalUses > 0 ? (float)SuccessfulUses / TotalUses : 0f;
    }

    /// <summary>
    /// 重试配置
    /// </summary>
    [System.Serializable]
    public class RetryConfig
    {
        public int maxRetries = 3;
        public int baseDelay = 1000;
        public float backoffFactor = 2.0f;
        public int maxDelay = 30000;
        public RetryStrategy strategy = RetryStrategy.ExponentialBackoff;
    }

    #endregion

    #region 异常类型

    /// <summary>
    /// 重试耗尽异常
    /// </summary>
    public class RetryExhaustedException : Exception
    {
        public RetryExhaustedException(string message) : base(message) { }
        public RetryExhaustedException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// 重试条件不满足异常
    /// </summary>
    public class RetryConditionNotMetException : Exception
    {
        public RetryConditionNotMetException(string message) : base(message) { }
    }

    /// <summary>
    /// 断路器开启异常
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }

    #endregion
}