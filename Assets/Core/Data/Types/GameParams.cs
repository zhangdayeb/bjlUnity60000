// Assets/_Core/Data/Types/GameParams.cs
// URL游戏参数 - 对应JavaScript项目的游戏初始化参数

using System;
using UnityEngine;

namespace Core.Data.Types
{
    /// <summary>
    /// 游戏启动参数 - 从URL解析或手动设置
    /// 对应JavaScript中的GameParams接口
    /// </summary>
    [System.Serializable]
    public class GameParams
    {
        [Header("基础游戏参数")]
        [Tooltip("桌台ID")]
        public string table_id = "";
        
        [Tooltip("游戏类型 (2=龙虎, 3=百家乐)")]
        public string game_type = "3";
        
        [Tooltip("用户ID")]
        public string user_id = "";
        
        [Tooltip("用户令牌")]
        public string token = "";

        [Header("可选参数")]
        [Tooltip("语言设置")]
        public string language = "zh";
        
        [Tooltip("货币类型")]
        public string currency = "CNY";
        
        [Tooltip("时区")]
        public string timezone = "Asia/Shanghai";

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public GameParams()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public GameParams(string tableId, string gameType, string userId, string userToken)
        {
            table_id = tableId;
            game_type = gameType;
            user_id = userId;
            token = userToken;
        }

        /// <summary>
        /// 验证游戏参数是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public GameParamsValidation Validate()
        {
            var result = new GameParamsValidation();
            
            // 检查必填参数
            if (string.IsNullOrEmpty(table_id))
            {
                result.IsValid = false;
                result.MissingParams.Add("table_id");
            }
            
            if (string.IsNullOrEmpty(game_type))
            {
                result.IsValid = false;
                result.MissingParams.Add("game_type");
            }
            
            if (string.IsNullOrEmpty(user_id))
            {
                result.IsValid = false;
                result.MissingParams.Add("user_id");
            }
            
            if (string.IsNullOrEmpty(token))
            {
                result.IsValid = false;
                result.MissingParams.Add("token");
            }

            // 验证游戏类型
            if (!string.IsNullOrEmpty(game_type))
            {
                if (game_type != "2" && game_type != "3")
                {
                    result.IsValid = false;
                    result.Errors.Add($"无效的游戏类型: {game_type}，只支持 2(龙虎) 或 3(百家乐)");
                }
            }

            // 验证桌台ID格式
            if (!string.IsNullOrEmpty(table_id))
            {
                if (!int.TryParse(table_id, out int tableIdNum) || tableIdNum <= 0)
                {
                    result.IsValid = false;
                    result.Errors.Add($"无效的桌台ID格式: {table_id}");
                }
            }

            return result;
        }

        /// <summary>
        /// 从URL查询字符串解析游戏参数
        /// </summary>
        /// <param name="queryString">URL查询字符串</param>
        /// <returns>解析出的游戏参数</returns>
        public static GameParams ParseFromUrl(string queryString)
        {
            var gameParams = new GameParams();
            
            if (string.IsNullOrEmpty(queryString))
                return gameParams;

            // 移除开头的 '?' 字符
            if (queryString.StartsWith("?"))
                queryString = queryString.Substring(1);

            // 分割参数
            string[] pairs = queryString.Split('&');
            
            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length != 2) continue;

                string key = Uri.UnescapeDataString(keyValue[0]);
                string value = Uri.UnescapeDataString(keyValue[1]);

                switch (key.ToLower())
                {
                    case "table_id":
                    case "tableid":
                        gameParams.table_id = value;
                        break;
                    case "game_type":
                    case "gametype":
                        gameParams.game_type = value;
                        break;
                    case "user_id":
                    case "userid":
                        gameParams.user_id = value;
                        break;
                    case "token":
                        gameParams.token = value;
                        break;
                    case "language":
                    case "lang":
                        gameParams.language = value;
                        break;
                    case "currency":
                        gameParams.currency = value;
                        break;
                    case "timezone":
                        gameParams.timezone = value;
                        break;
                }
            }

            return gameParams;
        }

        /// <summary>
        /// 转换为URL查询字符串
        /// </summary>
        /// <returns>URL查询字符串</returns>
        public string ToQueryString()
        {
            var parameters = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(table_id))
                parameters.Add($"table_id={Uri.EscapeDataString(table_id)}");
                
            if (!string.IsNullOrEmpty(game_type))
                parameters.Add($"game_type={Uri.EscapeDataString(game_type)}");
                
            if (!string.IsNullOrEmpty(user_id))
                parameters.Add($"user_id={Uri.EscapeDataString(user_id)}");
                
            if (!string.IsNullOrEmpty(token))
                parameters.Add($"token={Uri.EscapeDataString(token)}");
                
            if (!string.IsNullOrEmpty(language))
                parameters.Add($"language={Uri.EscapeDataString(language)}");
                
            if (!string.IsNullOrEmpty(currency))
                parameters.Add($"currency={Uri.EscapeDataString(currency)}");
                
            if (!string.IsNullOrEmpty(timezone))
                parameters.Add($"timezone={Uri.EscapeDataString(timezone)}");

            return string.Join("&", parameters);
        }

        /// <summary>
        /// 克隆游戏参数
        /// </summary>
        /// <returns>克隆的游戏参数</returns>
        public GameParams Clone()
        {
            return new GameParams
            {
                table_id = this.table_id,
                game_type = this.game_type,
                user_id = this.user_id,
                token = this.token,
                language = this.language,
                currency = this.currency,
                timezone = this.timezone
            };
        }

        /// <summary>
        /// 获取游戏类型描述
        /// </summary>
        /// <returns>游戏类型描述</returns>
        public string GetGameTypeDescription()
        {
            switch (game_type)
            {
                case "2":
                    return "龙虎";
                case "3":
                    return "百家乐";
                default:
                    return "未知游戏";
            }
        }

        /// <summary>
        /// 是否为百家乐游戏
        /// </summary>
        /// <returns>是否为百家乐</returns>
        public bool IsBaccarat()
        {
            return game_type == "3";
        }

        /// <summary>
        /// 是否为龙虎游戏
        /// </summary>
        /// <returns>是否为龙虎</returns>
        public bool IsLongHu()
        {
            return game_type == "2";
        }

        /// <summary>
        /// 转换为字符串（调试用）
        /// </summary>
        /// <returns>格式化的字符串</returns>
        public override string ToString()
        {
            return $"GameParams[TableId={table_id}, GameType={game_type}({GetGameTypeDescription()}), UserId={user_id}, Token={token?.Substring(0, Math.Min(8, token.Length))}...]";
        }
    }

    /// <summary>
    /// 游戏参数验证结果
    /// </summary>
    [System.Serializable]
    public class GameParamsValidation
    {
        [Tooltip("是否验证通过")]
        public bool IsValid = true;
        
        [Tooltip("缺失的参数列表")]
        public System.Collections.Generic.List<string> MissingParams = new System.Collections.Generic.List<string>();
        
        [Tooltip("错误信息列表")]
        public System.Collections.Generic.List<string> Errors = new System.Collections.Generic.List<string>();

        /// <summary>
        /// 获取所有错误信息
        /// </summary>
        /// <returns>错误信息字符串</returns>
        public string GetErrorMessage()
        {
            var messages = new System.Collections.Generic.List<string>();
            
            if (MissingParams.Count > 0)
            {
                messages.Add($"缺失参数: {string.Join(", ", MissingParams)}");
            }
            
            if (Errors.Count > 0)
            {
                messages.AddRange(Errors);
            }
            
            return string.Join("; ", messages);
        }
    }
}