// ================================================================================================
// 数据验证器 - DataValidator.cs
// 用途：提供统一的数据验证功能，支持游戏数据、网络数据、配置数据等各种验证场景
// ================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using BaccaratGame.Data.Types;
using BaccaratGame.Config;

namespace BaccaratGame.Data.Validators
{
    /// <summary>
    /// 数据验证器 - 提供各种数据验证功能
    /// 对应JavaScript项目中的数据验证逻辑
    /// </summary>
    public static class DataValidator
    {
        #region 基础数据验证

        /// <summary>
        /// 验证字符串不为空
        /// </summary>
        public static ValidationResult ValidateNotEmpty(string value, string fieldName = "")
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(value))
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Error, 
                    $"字段不能为空", fieldName);
            }
            
            return result;
        }

        /// <summary>
        /// 验证字符串不为空白
        /// </summary>
        public static ValidationResult ValidateNotWhiteSpace(string value, string fieldName = "")
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrWhiteSpace(value))
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Error, 
                    $"字段不能为空白字符", fieldName);
            }
            
            return result;
        }

        /// <summary>
        /// 验证数值范围
        /// </summary>
        public static ValidationResult ValidateRange(int value, int min, int max, string fieldName = "")
        {
            var result = new ValidationResult();
            
            if (value < min || value > max)
            {
                result.AddError(ValidationErrorType.OutOfRange, ValidationSeverity.Error, 
                    $"值 {value} 超出范围 [{min}, {max}]", fieldName);
            }
            
            return result;
        }

        /// <summary>
        /// 验证浮点数范围
        /// </summary>
        public static ValidationResult ValidateRange(float value, float min, float max, string fieldName = "")
        {
            var result = new ValidationResult();
            
            if (value < min || value > max)
            {
                result.AddError(ValidationErrorType.OutOfRange, ValidationSeverity.Error, 
                    $"值 {value:F2} 超出范围 [{min:F2}, {max:F2}]", fieldName);
            }
            
            return result;
        }

        /// <summary>
        /// 验证URL格式
        /// </summary>
        public static ValidationResult ValidateUrl(string url, string fieldName = "")
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(url))
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Error, 
                    "URL不能为空", fieldName);
                return result;
            }
            
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) || 
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps && 
                 uriResult.Scheme != "ws" && uriResult.Scheme != "wss"))
            {
                result.AddError(ValidationErrorType.InvalidUrl, ValidationSeverity.Error, 
                    $"URL格式无效: {url}", fieldName);
            }
            
            return result;
        }

        /// <summary>
        /// 验证邮箱格式
        /// </summary>
        public static ValidationResult ValidateEmail(string email, string fieldName = "")
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(email))
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Error, 
                    "邮箱地址不能为空", fieldName);
                return result;
            }
            
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email, pattern))
            {
                result.AddError(ValidationErrorType.InvalidFormat, ValidationSeverity.Error, 
                    $"邮箱格式无效: {email}", fieldName);
            }
            
            return result;
        }

        #endregion

        #region 游戏数据验证

        /// <summary>
        /// 验证游戏参数
        /// </summary>
        public static ValidationResult ValidateGameParams(GameParams gameParams)
        {
            var result = new ValidationResult();
            
            if (gameParams == null)
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Critical, 
                    "游戏参数不能为空", "GameParams");
                return result;
            }
            
            // 验证桌台ID
            result.Merge(ValidateNotEmpty(gameParams.table_id, "table_id"));
            
            // 验证游戏类型
            result.Merge(ValidateNotEmpty(gameParams.game_type, "game_type"));
            
            // 验证用户ID
            result.Merge(ValidateNotEmpty(gameParams.user_id, "user_id"));
            
            // 验证Token
            result.Merge(ValidateNotEmpty(gameParams.token, "token"));
            
            // 验证桌台ID格式（应该是数字）
            if (!string.IsNullOrEmpty(gameParams.table_id) && !int.TryParse(gameParams.table_id, out _))
            {
                result.AddError(ValidationErrorType.InvalidFormat, ValidationSeverity.Error, 
                    $"桌台ID格式无效: {gameParams.table_id}", "table_id");
            }
            
            // 验证游戏类型格式（应该是数字）
            if (!string.IsNullOrEmpty(gameParams.game_type) && !int.TryParse(gameParams.game_type, out _))
            {
                result.AddError(ValidationErrorType.InvalidFormat, ValidationSeverity.Error, 
                    $"游戏类型格式无效: {gameParams.game_type}", "game_type");
            }
            
            return result;
        }

        /// <summary>
        /// 验证用户信息
        /// </summary>
        public static ValidationResult ValidateUserInfo(UserInfo userInfo)
        {
            var result = new ValidationResult();
            
            if (userInfo == null)
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Critical, 
                    "用户信息不能为空", "UserInfo");
                return result;
            }
            
            // 验证用户ID
            result.Merge(ValidateNotEmpty(userInfo.user_id, "user_id"));
            
            // 验证余额
            if (userInfo.balance < 0)
            {
                result.AddError(ValidationErrorType.OutOfRange, ValidationSeverity.Error, 
                    $"用户余额不能为负数: {userInfo.balance}", "balance");
            }
            
            // 验证货币代码
            result.Merge(ValidateNotEmpty(userInfo.currency, "currency"));
            
            // 验证货币代码格式（通常是3位字母）
            if (!string.IsNullOrEmpty(userInfo.currency) && 
                (userInfo.currency.Length != 3 || !userInfo.currency.All(char.IsLetter)))
            {
                result.AddError(ValidationErrorType.InvalidFormat, ValidationSeverity.Warning, 
                    $"货币代码格式可能不正确: {userInfo.currency}", "currency");
            }
            
            return result;
        }

        /// <summary>
        /// 验证桌台信息
        /// </summary>
        public static ValidationResult ValidateTableInfo(TableInfo tableInfo)
        {
            var result = new ValidationResult();
            
            if (tableInfo == null)
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Critical, 
                    "桌台信息不能为空", "TableInfo");
                return result;
            }
            
            // 验证桌台ID
            if (tableInfo.id <= 0)
            {
                result.AddError(ValidationErrorType.OutOfRange, ValidationSeverity.Error, 
                    $"桌台ID无效: {tableInfo.id}", "id");
            }
            
            // 验证视频URL
            if (!string.IsNullOrEmpty(tableInfo.video_near))
            {
                result.Merge(ValidateUrl(tableInfo.video_near, "video_near"));
            }
            
            if (!string.IsNullOrEmpty(tableInfo.video_far))
            {
                result.Merge(ValidateUrl(tableInfo.video_far, "video_far"));
            }
            
            // 验证投注限额
            if (tableInfo.right_money_banker_player < 0)
            {
                result.AddError(ValidationErrorType.OutOfRange, ValidationSeverity.Error, 
                    "庄闲投注限额不能为负数", "right_money_banker_player");
            }
            
            if (tableInfo.right_money_tie < 0)
            {
                result.AddError(ValidationErrorType.OutOfRange, ValidationSeverity.Error, 
                    "和局投注限额不能为负数", "right_money_tie");
            }
            
            return result;
        }

        /// <summary>
        /// 验证投注数据
        /// </summary>
        public static ValidationResult ValidateBetData(BetData betData, GameConfig gameConfig = null)
        {
            var result = new ValidationResult();
            
            if (betData == null)
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Critical, 
                    "投注数据不能为空", "BetData");
                return result;
            }
            
            // 验证投注金额
            if (betData.amount <= 0)
            {
                result.AddError(ValidationErrorType.OutOfRange, ValidationSeverity.Error, 
                    $"投注金额必须大于0: {betData.amount}", "amount");
            }
            
            // 验证投注区域
            result.Merge(ValidateNotEmpty(betData.betAreaId, "betAreaId"));
            
            // 如果有游戏配置，验证投注限额
            if (gameConfig != null && !string.IsNullOrEmpty(betData.betAreaId))
            {
                var areaConfig = gameConfig.GetBettingAreaConfig(betData.betAreaId);
                if (areaConfig == null)
                {
                    result.AddError(ValidationErrorType.ReferenceNotFound, ValidationSeverity.Error, 
                        $"投注区域不存在: {betData.betAreaId}", "betAreaId");
                }
                else
                {
                    if (betData.amount < areaConfig.minBet)
                    {
                        result.AddError(ValidationErrorType.BetLimitExceeded, ValidationSeverity.Error, 
                            $"投注金额低于最小限额: {betData.amount} < {areaConfig.minBet}", "amount");
                    }
                    
                    if (betData.amount > areaConfig.maxBet)
                    {
                        result.AddError(ValidationErrorType.BetLimitExceeded, ValidationSeverity.Error, 
                            $"投注金额超过最大限额: {betData.amount} > {areaConfig.maxBet}", "amount");
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// 验证投注请求列表
        /// </summary>
        public static ValidationResult ValidateBetRequests(List<BaccaratBetRequest> betRequests, GameConfig gameConfig = null)
        {
            var result = new ValidationResult();
            
            if (betRequests == null)
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Error, 
                    "投注请求列表不能为空", "BetRequests");
                return result;
            }
            
            if (betRequests.Count == 0)
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Warning, 
                    "投注请求列表为空", "BetRequests");
                return result;
            }
            
            // 验证总投注金额
            float totalAmount = betRequests.Sum(bet => bet.money);
            if (gameConfig != null && totalAmount > gameConfig.maxTotalBetPerRound)
            {
                result.AddError(ValidationErrorType.BetLimitExceeded, ValidationSeverity.Error, 
                    $"总投注金额超过限制: {totalAmount} > {gameConfig.maxTotalBetPerRound}", "TotalAmount");
            }
            
            // 验证每个投注请求
            for (int i = 0; i < betRequests.Count; i++)
            {
                var bet = betRequests[i];
                if (bet.money <= 0)
                {
                    result.AddError(ValidationErrorType.OutOfRange, ValidationSeverity.Error, 
                        $"投注[{i}]金额无效: {bet.money}", $"BetRequests[{i}].money");
                }
            }
            
            return result;
        }

        #endregion

        #region 配置验证

        /// <summary>
        /// 验证环境配置
        /// </summary>
        public static ValidationResult ValidateEnvironmentConfig(EnvironmentConfig config)
        {
            var result = new ValidationResult();
            
            if (config == null)
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Critical, 
                    "环境配置不能为空", "EnvironmentConfig");
                return result;
            }
            
            // 验证API URLs
            if (!string.IsNullOrEmpty(config.testingApiBaseUrl))
            {
                result.Merge(ValidateUrl(config.testingApiBaseUrl, "testingApiBaseUrl"));
            }
            
            if (!string.IsNullOrEmpty(config.productionApiBaseUrl))
            {
                result.Merge(ValidateUrl(config.productionApiBaseUrl, "productionApiBaseUrl"));
            }
            
            // 验证WebSocket URLs
            if (!string.IsNullOrEmpty(config.testingWebSocketUrl))
            {
                result.Merge(ValidateUrl(config.testingWebSocketUrl, "testingWebSocketUrl"));
            }
            
            if (!string.IsNullOrEmpty(config.productionWebSocketUrl))
            {
                result.Merge(ValidateUrl(config.productionWebSocketUrl, "productionWebSocketUrl"));
            }
            
            // 生产环境必须有API密钥
            if (config.IsProduction && string.IsNullOrEmpty(config.GetApiKey()))
            {
                result.AddError(ValidationErrorType.MissingRequired, ValidationSeverity.Critical, 
                    "生产环境必须设置API密钥", "apiKey");
            }
            
            return result;
        }

        /// <summary>
        /// 验证网络配置
        /// </summary>
        public static ValidationResult ValidateNetworkConfig(NetworkConfig config)
        {
            var result = new ValidationResult();
            
            if (config == null)
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Critical, 
                    "网络配置不能为空", "NetworkConfig");
                return result;
            }
            
            // 验证超时配置
            result.Merge(ValidateRange(config.httpTimeout, 1, 300, "httpTimeout"));
            result.Merge(ValidateRange(config.wsConnectionTimeout, 1, 60, "wsConnectionTimeout"));
            result.Merge(ValidateRange(config.heartbeatInterval, 5, 300, "heartbeatInterval"));
            
            // 验证重试配置
            result.Merge(ValidateRange(config.httpRetryCount, 0, 10, "httpRetryCount"));
            result.Merge(ValidateRange(config.maxReconnectAttempts, 0, 20, "maxReconnectAttempts"));
            
            // 验证队列配置
            result.Merge(ValidateRange(config.messageQueueMaxSize, 10, 10000, "messageQueueMaxSize"));
            
            return result;
        }

        /// <summary>
        /// 验证游戏配置
        /// </summary>
        public static ValidationResult ValidateGameConfig(GameConfig config)
        {
            var result = new ValidationResult();
            
            if (config == null)
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Critical, 
                    "游戏配置不能为空", "GameConfig");
                return result;
            }
            
            // 验证投注区域配置
            if (config.bettingAreas == null || config.bettingAreas.Count == 0)
            {
                result.AddError(ValidationErrorType.MissingRequired, ValidationSeverity.Critical, 
                    "必须配置至少一个投注区域", "bettingAreas");
            }
            else
            {
                for (int i = 0; i < config.bettingAreas.Count; i++)
                {
                    var area = config.bettingAreas[i];
                    if (string.IsNullOrEmpty(area.areaId))
                    {
                        result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Error, 
                            $"投注区域[{i}]ID不能为空", $"bettingAreas[{i}].areaId");
                    }
                    
                    if (area.minBet > area.maxBet)
                    {
                        result.AddError(ValidationErrorType.BusinessRuleViolation, ValidationSeverity.Error, 
                            $"投注区域[{i}]最小投注不能大于最大投注", $"bettingAreas[{i}]");
                    }
                    
                    if (area.odds <= 0)
                    {
                        result.AddError(ValidationErrorType.OutOfRange, ValidationSeverity.Error, 
                            $"投注区域[{i}]赔率必须大于0", $"bettingAreas[{i}].odds");
                    }
                }
            }
            
            // 验证筹码配置
            if (config.chipConfigs == null || config.chipConfigs.Count == 0)
            {
                result.AddError(ValidationErrorType.MissingRequired, ValidationSeverity.Error, 
                    "必须配置至少一种筹码", "chipConfigs");
            }
            else
            {
                bool hasDefault = false;
                var values = new HashSet<int>();
                
                for (int i = 0; i < config.chipConfigs.Count; i++)
                {
                    var chip = config.chipConfigs[i];
                    
                    if (chip.value <= 0)
                    {
                        result.AddError(ValidationErrorType.OutOfRange, ValidationSeverity.Error, 
                            $"筹码[{i}]面值必须大于0", $"chipConfigs[{i}].value");
                    }
                    
                    if (values.Contains(chip.value))
                    {
                        result.AddError(ValidationErrorType.DuplicateValue, ValidationSeverity.Error, 
                            $"筹码面值重复: {chip.value}", $"chipConfigs[{i}].value");
                    }
                    else
                    {
                        values.Add(chip.value);
                    }
                    
                    if (chip.isDefault)
                    {
                        if (hasDefault)
                        {
                            result.AddError(ValidationErrorType.DuplicateValue, ValidationSeverity.Warning, 
                                "存在多个默认筹码", $"chipConfigs[{i}].isDefault");
                        }
                        hasDefault = true;
                    }
                }
                
                if (!hasDefault)
                {
                    result.AddError(ValidationErrorType.MissingRequired, ValidationSeverity.Warning, 
                        "没有设置默认筹码", "chipConfigs");
                }
            }
            
            return result;
        }

        #endregion

        #region 网络数据验证

        /// <summary>
        /// 验证WebSocket消息
        /// </summary>
        public static ValidationResult ValidateWebSocketMessage(string message)
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(message))
            {
                result.AddError(ValidationErrorType.NullOrEmpty, ValidationSeverity.Error, 
                    "WebSocket消息不能为空", "message");
                return result;
            }
            
            // 尝试解析为JSON
            try
            {
                JsonUtility.FromJson<object>(message);
            }
            catch (Exception ex)
            {
                result.AddError(ValidationErrorType.InvalidFormat, ValidationSeverity.Error, 
                    $"WebSocket消息JSON格式无效: {ex.Message}", "message");
            }
            
            return result;
        }

        #endregion

        #region 复合验证

        /// <summary>
        /// 验证完整的游戏初始化数据
        /// </summary>
        public static ValidationResult ValidateGameInitialization(GameParams gameParams, UserInfo userInfo, TableInfo tableInfo)
        {
            var result = new ValidationResult();
            
            // 验证各个组件
            result.Merge(ValidateGameParams(gameParams));
            result.Merge(ValidateUserInfo(userInfo));
            result.Merge(ValidateTableInfo(tableInfo));
            
            // 验证数据一致性
            if (gameParams != null && userInfo != null && gameParams.user_id != userInfo.user_id)
            {
                result.AddError(ValidationErrorType.BusinessRuleViolation, ValidationSeverity.Error, 
                    "游戏参数中的用户ID与用户信息不匹配", "user_id");
            }
            
            if (gameParams != null && tableInfo != null && 
                int.TryParse(gameParams.table_id, out int tableId) && tableId != tableInfo.id)
            {
                result.AddError(ValidationErrorType.BusinessRuleViolation, ValidationSeverity.Error, 
                    "游戏参数中的桌台ID与桌台信息不匹配", "table_id");
            }
            
            return result;
        }

        /// <summary>
        /// 验证投注能力（余额、限额等）
        /// </summary>
        public static ValidationResult ValidateBettingCapability(UserInfo userInfo, BetData betData, GameConfig gameConfig)
        {
            var result = new ValidationResult();
            
            // 验证余额是否足够
            if (userInfo.balance < betData.amount)
            {
                result.AddError(ValidationErrorType.InsufficientBalance, ValidationSeverity.Error, 
                    $"余额不足: 当前{userInfo.balance}, 需要{betData.amount}", "balance");
            }
            
            // 验证投注后余额是否满足最小要求
            float remainingBalance = userInfo.balance - betData.amount;
            if (gameConfig != null && remainingBalance < gameConfig.minBalanceRequired)
            {
                result.AddError(ValidationErrorType.InsufficientBalance, ValidationSeverity.Warning, 
                    $"投注后余额过低: {remainingBalance} < {gameConfig.minBalanceRequired}", "balance");
            }
            
            return result;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 批量验证
        /// </summary>
        public static ValidationResult ValidateAll(params Func<ValidationResult>[] validators)
        {
            var result = new ValidationResult();
            
            foreach (var validator in validators)
            {
                try
                {
                    result.Merge(validator());
                }
                catch (Exception ex)
                {
                    result.AddError(ValidationErrorType.Unknown, ValidationSeverity.Critical, 
                        $"验证过程中发生异常: {ex.Message}");
                }
            }
            
            return result;
        }

        /// <summary>
        /// 条件验证
        /// </summary>
        public static ValidationResult ValidateIf(bool condition, Func<ValidationResult> validator)
        {
            if (condition)
            {
                return validator();
            }
            return ValidationResult.Success();
        }

        #endregion
    }
}