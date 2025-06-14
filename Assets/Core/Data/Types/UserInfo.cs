// Assets/_Core/Data/Types/UserInfo.cs
// 用户信息 - 对应JavaScript项目的用户数据结构

using System;
using UnityEngine;

namespace Core.Data.Types
{
    /// <summary>
    /// 用户信息 - 对应JavaScript中的UserInfo接口
    /// 包含用户基础信息、余额、等级等数据
    /// </summary>
    [System.Serializable]
    public class UserInfo
    {
        [Header("基础用户信息")]
        [Tooltip("用户ID")]
        public string user_id = "";
        
        [Tooltip("用户名")]
        public string username = "";
        
        [Tooltip("昵称")]
        public string nickname = "";

        [Header("余额信息")]
        [Tooltip("用户余额")]
        public float balance = 0f;
        
        [Tooltip("货币余额")]
        public float money_balance = 0f;
        
        [Tooltip("货币类型")]
        public string currency = "CNY";

        [Header("等级信息")]
        [Tooltip("用户等级")]
        public int level = 1;
        
        [Tooltip("VIP等级")]
        public int vip_level = 0;
        
        [Tooltip("经验值")]
        public int experience = 0;

        [Header("头像和显示")]
        [Tooltip("头像URL")]
        public string avatar = "";
        
        [Tooltip("头像Sprite（运行时设置）")]
        [System.NonSerialized]
        public Sprite avatarSprite;

        [Header("游戏统计")]
        [Tooltip("总投注金额")]
        public float total_bet_amount = 0f;
        
        [Tooltip("总中奖金额")]
        public float total_win_amount = 0f;
        
        [Tooltip("游戏局数")]
        public int games_played = 0;
        
        [Tooltip("胜率")]
        public float win_rate = 0f;

        [Header("账户状态")]
        [Tooltip("账户状态")]
        public UserAccountStatus status = UserAccountStatus.Active;
        
        [Tooltip("是否在线")]
        public bool is_online = true;
        
        [Tooltip("最后登录时间")]
        public string last_login_time = "";

        [Header("权限和设置")]
        [Tooltip("是否允许投注")]
        public bool can_bet = true;
        
        [Tooltip("是否允许聊天")]
        public bool can_chat = true;
        
        [Tooltip("语言设置")]
        public string language = "zh";
        
        [Tooltip("时区")]
        public string timezone = "Asia/Shanghai";

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public UserInfo()
        {
        }

        /// <summary>
        /// 带基础参数的构造函数
        /// </summary>
        public UserInfo(string userId, string userName, float userBalance, string userCurrency = "CNY")
        {
            user_id = userId;
            username = userName;
            nickname = userName;
            balance = userBalance;
            money_balance = userBalance;
            currency = userCurrency;
        }

        /// <summary>
        /// 验证用户信息是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(user_id) && 
                   !string.IsNullOrEmpty(username) &&
                   balance >= 0 &&
                   money_balance >= 0 &&
                   level > 0;
        }

        /// <summary>
        /// 更新余额
        /// </summary>
        /// <param name="newBalance">新余额</param>
        public void UpdateBalance(float newBalance)
        {
            if (newBalance >= 0)
            {
                balance = newBalance;
                money_balance = newBalance;
            }
        }

        /// <summary>
        /// 增加余额
        /// </summary>
        /// <param name="amount">增加金额</param>
        public void AddBalance(float amount)
        {
            if (amount > 0)
            {
                balance += amount;
                money_balance += amount;
            }
        }

        /// <summary>
        /// 减少余额
        /// </summary>
        /// <param name="amount">减少金额</param>
        /// <returns>是否成功</returns>
        public bool SubtractBalance(float amount)
        {
            if (amount > 0 && balance >= amount)
            {
                balance -= amount;
                money_balance -= amount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取格式化的余额显示
        /// </summary>
        /// <returns>格式化的余额字符串</returns>
        public string GetFormattedBalance()
        {
            return $"{currency} {balance:F2}";
        }

        /// <summary>
        /// 获取格式化的余额显示（带千分位分隔符）
        /// </summary>
        /// <returns>格式化的余额字符串</returns>
        public string GetFormattedBalanceWithComma()
        {
            return $"{currency} {balance:N2}";
        }

        /// <summary>
        /// 更新VIP等级
        /// </summary>
        /// <param name="newVipLevel">新VIP等级</param>
        public void UpdateVipLevel(int newVipLevel)
        {
            if (newVipLevel >= 0)
            {
                vip_level = newVipLevel;
            }
        }

        /// <summary>
        /// 获取VIP等级描述
        /// </summary>
        /// <returns>VIP等级描述</returns>
        public string GetVipLevelDescription()
        {
            switch (vip_level)
            {
                case 0: return "普通用户";
                case 1: return "VIP 1";
                case 2: return "VIP 2";
                case 3: return "VIP 3";
                case 4: return "VIP 4";
                case 5: return "VIP 5";
                default: return $"VIP {vip_level}";
            }
        }

        /// <summary>
        /// 计算净盈亏
        /// </summary>
        /// <returns>净盈亏金额</returns>
        public float GetNetProfit()
        {
            return total_win_amount - total_bet_amount;
        }

        /// <summary>
        /// 更新游戏统计
        /// </summary>
        /// <param name="betAmount">投注金额</param>
        /// <param name="winAmount">中奖金额</param>
        public void UpdateGameStats(float betAmount, float winAmount)
        {
            total_bet_amount += betAmount;
            total_win_amount += winAmount;
            games_played++;
            
            // 重新计算胜率
            if (games_played > 0)
            {
                int winCount = winAmount > betAmount ? 1 : 0;
                // 这里简化处理，实际应该记录胜负次数
                win_rate = total_win_amount / total_bet_amount * 100f;
            }
        }

        /// <summary>
        /// 检查是否可以投注指定金额
        /// </summary>
        /// <param name="amount">投注金额</param>
        /// <returns>是否可以投注</returns>
        public bool CanBet(float amount)
        {
            return can_bet && 
                   status == UserAccountStatus.Active && 
                   balance >= amount && 
                   amount > 0;
        }

        /// <summary>
        /// 获取显示用的用户名
        /// </summary>
        /// <returns>显示用的用户名</returns>
        public string GetDisplayName()
        {
            return !string.IsNullOrEmpty(nickname) ? nickname : username;
        }

        /// <summary>
        /// 克隆用户信息
        /// </summary>
        /// <returns>克隆的用户信息</returns>
        public UserInfo Clone()
        {
            return new UserInfo
            {
                user_id = this.user_id,
                username = this.username,
                nickname = this.nickname,
                balance = this.balance,
                money_balance = this.money_balance,
                currency = this.currency,
                level = this.level,
                vip_level = this.vip_level,
                experience = this.experience,
                avatar = this.avatar,
                total_bet_amount = this.total_bet_amount,
                total_win_amount = this.total_win_amount,
                games_played = this.games_played,
                win_rate = this.win_rate,
                status = this.status,
                is_online = this.is_online,
                last_login_time = this.last_login_time,
                can_bet = this.can_bet,
                can_chat = this.can_chat,
                language = this.language,
                timezone = this.timezone
            };
        }

        /// <summary>
        /// 从JSON字符串反序列化
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>用户信息对象</returns>
        public static UserInfo FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<UserInfo>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse UserInfo from JSON: {e.Message}");
                return new UserInfo();
            }
        }

        /// <summary>
        /// 序列化为JSON字符串
        /// </summary>
        /// <returns>JSON字符串</returns>
        public string ToJson()
        {
            try
            {
                return JsonUtility.ToJson(this, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to serialize UserInfo to JSON: {e.Message}");
                return "{}";
            }
        }

        /// <summary>
        /// 转换为字符串（调试用）
        /// </summary>
        /// <returns>格式化的字符串</returns>
        public override string ToString()
        {
            return $"UserInfo[ID={user_id}, Name={GetDisplayName()}, Balance={GetFormattedBalance()}, Level={level}, VIP={vip_level}]";
        }
    }

    /// <summary>
    /// 用户账户状态枚举
    /// </summary>
    public enum UserAccountStatus
    {
        Active = 0,      // 正常激活
        Suspended = 1,   // 暂停
        Banned = 2,      // 封禁
        Frozen = 3       // 冻结
    }

    /// <summary>
    /// 用户信息响应 - 对应API返回格式
    /// </summary>
    [System.Serializable]
    public class UserInfoResponse
    {
        public int code;
        public string message;
        public UserInfo data;
        
        /// <summary>
        /// 是否成功
        /// </summary>
        /// <returns>是否成功</returns>
        public bool IsSuccess()
        {
            return code == 200 || code == 1;
        }
    }
}