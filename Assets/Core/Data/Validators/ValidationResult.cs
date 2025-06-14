// ================================================================================================
// 数据验证结果 - ValidationResult.cs
// 用途：封装数据验证的结果信息，支持多种验证错误类型和详细信息
// ================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BaccaratGame.Data.Validators
{
    /// <summary>
    /// 验证错误级别枚举
    /// </summary>
    public enum ValidationSeverity
    {
        Info = 0,       // 信息提示
        Warning = 1,    // 警告
        Error = 2,      // 错误
        Critical = 3    // 严重错误
    }

    /// <summary>
    /// 验证错误类型枚举
    /// </summary>
    public enum ValidationErrorType
    {
        // 数据类型相关
        NullOrEmpty,        // 空值或空字符串
        InvalidFormat,      // 格式无效
        OutOfRange,         // 超出范围
        InvalidType,        // 类型错误
        
        // 业务逻辑相关
        BusinessRuleViolation,  // 违反业务规则
        DuplicateValue,         // 重复值
        ReferenceNotFound,      // 引用不存在
        
        // 网络相关
        InvalidUrl,         // 无效URL
        NetworkTimeout,     // 网络超时
        AuthenticationError, // 认证错误
        
        // 游戏相关
        InsufficientBalance,    // 余额不足
        BetLimitExceeded,      // 超出投注限制
        InvalidBetArea,        // 无效投注区域
        GameStateInvalid,      // 游戏状态无效
        
        // 配置相关
        ConfigurationError,    // 配置错误
        MissingRequired,       // 缺少必需项
        
        // 其他
        Unknown            // 未知错误
    }

    /// <summary>
    /// 单个验证错误信息
    /// </summary>
    [System.Serializable]
    public class ValidationError
    {
        [Header("错误信息")]
        [Tooltip("错误类型")]
        public ValidationErrorType errorType;
        
        [Tooltip("错误级别")]
        public ValidationSeverity severity;
        
        [Tooltip("错误消息")]
        public string message;
        
        [Tooltip("错误代码")]
        public string errorCode;
        
        [Tooltip("字段名称")]
        public string fieldName;
        
        [Tooltip("错误值")]
        public string errorValue;
        
        [Tooltip("建议修复方案")]
        public string suggestion;
        
        [Tooltip("错误发生时间")]
        public DateTime timestamp;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ValidationError()
        {
            timestamp = DateTime.Now;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public ValidationError(ValidationErrorType type, ValidationSeverity severity, string message, string fieldName = "")
        {
            this.errorType = type;
            this.severity = severity;
            this.message = message;
            this.fieldName = fieldName;
            this.timestamp = DateTime.Now;
            this.errorCode = GenerateErrorCode(type, severity);
        }

        /// <summary>
        /// 生成错误代码
        /// </summary>
        private string GenerateErrorCode(ValidationErrorType type, ValidationSeverity severity)
        {
            return $"{severity.ToString().ToUpper()[0]}{(int)type:D3}";
        }

        /// <summary>
        /// 获取格式化的错误信息
        /// </summary>
        public string GetFormattedMessage()
        {
            string prefix = severity switch
            {
                ValidationSeverity.Info => "ℹ️",
                ValidationSeverity.Warning => "⚠️",
                ValidationSeverity.Error => "❌",
                ValidationSeverity.Critical => "🚨",
                _ => "❓"
            };

            string fieldInfo = !string.IsNullOrEmpty(fieldName) ? $"[{fieldName}] " : "";
            return $"{prefix} {fieldInfo}{message}";
        }

        /// <summary>
        /// 获取详细信息
        /// </summary>
        public string GetDetailedInfo()
        {
            var details = new List<string>
            {
                $"错误代码: {errorCode}",
                $"错误类型: {errorType}",
                $"错误级别: {severity}",
                $"发生时间: {timestamp:yyyy-MM-dd HH:mm:ss}"
            };

            if (!string.IsNullOrEmpty(fieldName))
                details.Add($"字段名称: {fieldName}");

            if (!string.IsNullOrEmpty(errorValue))
                details.Add($"错误值: {errorValue}");

            if (!string.IsNullOrEmpty(suggestion))
                details.Add($"建议: {suggestion}");

            return string.Join("\n", details);
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            return GetFormattedMessage();
        }
    }

    /// <summary>
    /// 验证结果类 - 封装验证操作的完整结果
    /// </summary>
    [System.Serializable]
    public class ValidationResult
    {
        [Header("验证结果")]
        [Tooltip("是否验证成功")]
        public bool isValid;
        
        [Tooltip("验证错误列表")]
        public List<ValidationError> errors;
        
        [Tooltip("验证开始时间")]
        public DateTime startTime;
        
        [Tooltip("验证结束时间")]
        public DateTime endTime;
        
        [Tooltip("验证耗时（毫秒）")]
        public long durationMs;

        /// <summary>
        /// 默认构造函数 - 创建成功的验证结果
        /// </summary>
        public ValidationResult()
        {
            isValid = true;
            errors = new List<ValidationError>();
            startTime = DateTime.Now;
            endTime = startTime;
            durationMs = 0;
        }

        /// <summary>
        /// 带错误的构造函数
        /// </summary>
        public ValidationResult(ValidationError error)
        {
            isValid = false;
            errors = new List<ValidationError> { error };
            startTime = DateTime.Now;
            endTime = startTime;
            durationMs = 0;
        }

        /// <summary>
        /// 带多个错误的构造函数
        /// </summary>
        public ValidationResult(IEnumerable<ValidationError> errors)
        {
            this.errors = errors.ToList();
            isValid = this.errors.Count == 0;
            startTime = DateTime.Now;
            endTime = startTime;
            durationMs = 0;
        }

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasErrors => errors.Count > 0;

        /// <summary>
        /// 是否有警告
        /// </summary>
        public bool HasWarnings => errors.Any(e => e.severity == ValidationSeverity.Warning);

        /// <summary>
        /// 是否有严重错误
        /// </summary>
        public bool HasCriticalErrors => errors.Any(e => e.severity == ValidationSeverity.Critical);

        /// <summary>
        /// 错误数量
        /// </summary>
        public int ErrorCount => errors.Count;

        /// <summary>
        /// 获取最高错误级别
        /// </summary>
        public ValidationSeverity GetHighestSeverity()
        {
            if (errors.Count == 0) return ValidationSeverity.Info;
            return errors.Max(e => e.severity);
        }

        /// <summary>
        /// 添加错误
        /// </summary>
        public void AddError(ValidationError error)
        {
            errors.Add(error);
            isValid = false;
        }

        /// <summary>
        /// 添加错误（快捷方法）
        /// </summary>
        public void AddError(ValidationErrorType type, ValidationSeverity severity, string message, string fieldName = "")
        {
            AddError(new ValidationError(type, severity, message, fieldName));
        }

        /// <summary>
        /// 添加多个错误
        /// </summary>
        public void AddErrors(IEnumerable<ValidationError> errorsToAdd)
        {
            errors.AddRange(errorsToAdd);
            if (errors.Count > 0)
                isValid = false;
        }

        /// <summary>
        /// 合并其他验证结果
        /// </summary>
        public void Merge(ValidationResult other)
        {
            if (other != null && other.errors.Count > 0)
            {
                AddErrors(other.errors);
            }
        }

        /// <summary>
        /// 按严重程度获取错误
        /// </summary>
        public List<ValidationError> GetErrorsBySeverity(ValidationSeverity severity)
        {
            return errors.Where(e => e.severity == severity).ToList();
        }

        /// <summary>
        /// 按类型获取错误
        /// </summary>
        public List<ValidationError> GetErrorsByType(ValidationErrorType type)
        {
            return errors.Where(e => e.errorType == type).ToList();
        }

        /// <summary>
        /// 按字段名获取错误
        /// </summary>
        public List<ValidationError> GetErrorsByField(string fieldName)
        {
            return errors.Where(e => e.fieldName == fieldName).ToList();
        }

        /// <summary>
        /// 清除错误
        /// </summary>
        public void ClearErrors()
        {
            errors.Clear();
            isValid = true;
        }

        /// <summary>
        /// 清除指定级别的错误
        /// </summary>
        public void ClearErrorsBySeverity(ValidationSeverity severity)
        {
            errors.RemoveAll(e => e.severity == severity);
            isValid = errors.Count == 0;
        }

        /// <summary>
        /// 标记验证完成
        /// </summary>
        public void Complete()
        {
            endTime = DateTime.Now;
            durationMs = (long)(endTime - startTime).TotalMilliseconds;
        }

        /// <summary>
        /// 获取所有错误信息的字符串
        /// </summary>
        public string GetErrorMessages()
        {
            if (errors.Count == 0) return "验证成功";
            return string.Join("\n", errors.Select(e => e.GetFormattedMessage()));
        }

        /// <summary>
        /// 获取详细的验证报告
        /// </summary>
        public string GetDetailedReport()
        {
            var report = new List<string>
            {
                $"=== 验证报告 ===",
                $"验证状态: {(isValid ? "✅ 成功" : "❌ 失败")}",
                $"错误数量: {errors.Count}",
                $"验证耗时: {durationMs}ms",
                $"开始时间: {startTime:yyyy-MM-dd HH:mm:ss.fff}",
                $"结束时间: {endTime:yyyy-MM-dd HH:mm:ss.fff}",
                ""
            };

            if (errors.Count > 0)
            {
                // 按严重程度分组显示错误
                var groupedErrors = errors.GroupBy(e => e.severity).OrderByDescending(g => g.Key);
                
                foreach (var group in groupedErrors)
                {
                    report.Add($"=== {group.Key} ({group.Count()}个) ===");
                    foreach (var error in group)
                    {
                        report.Add(error.GetFormattedMessage());
                        if (!string.IsNullOrEmpty(error.suggestion))
                        {
                            report.Add($"   💡 建议: {error.suggestion}");
                        }
                    }
                    report.Add("");
                }
            }

            return string.Join("\n", report);
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            return $"ValidationResult: {(isValid ? "Valid" : "Invalid")} ({errors.Count} errors)";
        }

        /// <summary>
        /// 创建成功的验证结果
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult();
        }

        /// <summary>
        /// 创建失败的验证结果
        /// </summary>
        public static ValidationResult Failure(string message, ValidationErrorType type = ValidationErrorType.Unknown, string fieldName = "")
        {
            return new ValidationResult(new ValidationError(type, ValidationSeverity.Error, message, fieldName));
        }

        /// <summary>
        /// 创建带警告的验证结果
        /// </summary>
        public static ValidationResult Warning(string message, ValidationErrorType type = ValidationErrorType.Unknown, string fieldName = "")
        {
            var result = new ValidationResult();
            result.AddError(new ValidationError(type, ValidationSeverity.Warning, message, fieldName));
            result.isValid = true; // 警告不影响整体验证结果
            return result;
        }

        /// <summary>
        /// 创建严重错误的验证结果
        /// </summary>
        public static ValidationResult Critical(string message, ValidationErrorType type = ValidationErrorType.Unknown, string fieldName = "")
        {
            return new ValidationResult(new ValidationError(type, ValidationSeverity.Critical, message, fieldName));
        }
    }

    /// <summary>
    /// 验证结果扩展方法
    /// </summary>
    public static class ValidationResultExtensions
    {
        /// <summary>
        /// 输出到Unity控制台
        /// </summary>
        public static void LogToConsole(this ValidationResult result)
        {
            if (result.isValid)
            {
                Debug.Log($"[Validation] {result.GetErrorMessages()}");
            }
            else
            {
                var criticalErrors = result.GetErrorsBySeverity(ValidationSeverity.Critical);
                var regularErrors = result.GetErrorsBySeverity(ValidationSeverity.Error);
                var warnings = result.GetErrorsBySeverity(ValidationSeverity.Warning);

                foreach (var error in criticalErrors)
                    Debug.LogError($"[Validation] {error.GetFormattedMessage()}");

                foreach (var error in regularErrors)
                    Debug.LogError($"[Validation] {error.GetFormattedMessage()}");

                foreach (var warning in warnings)
                    Debug.LogWarning($"[Validation] {warning.GetFormattedMessage()}");
            }
        }

        /// <summary>
        /// 抛出异常（如果有严重错误）
        /// </summary>
        public static void ThrowIfCritical(this ValidationResult result)
        {
            if (result.HasCriticalErrors)
            {
                var criticalErrors = result.GetErrorsBySeverity(ValidationSeverity.Critical);
                throw new ValidationException($"严重验证错误: {string.Join("; ", criticalErrors.Select(e => e.message))}");
            }
        }
    }

    /// <summary>
    /// 验证异常类
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationResult ValidationResult { get; }

        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, ValidationResult result) : base(message)
        {
            ValidationResult = result;
        }

        public ValidationException(ValidationResult result) : base(result.GetErrorMessages())
        {
            ValidationResult = result;
        }
    }
}