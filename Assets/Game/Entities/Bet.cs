// Assets/Game/Entities/Bet.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Data.Types;
using Game.Logic;

namespace Game.Entities
{
    /// <summary>
    /// 投注实体类
    /// 表示百家乐游戏中的单个投注，包含投注信息、状态管理、结果处理等功能
    /// </summary>
    [System.Serializable]
    public class Bet : IEquatable<Bet>, IComparable<Bet>
    {
        [Header("基础信息")]
        [SerializeField] private string betId = "";
        [SerializeField] private string playerId = "";
        [SerializeField] private string gameNumber = "";
        [SerializeField] private string tableId = "";

        [Header("投注信息")]
        [SerializeField] private BaccaratBetType betType = BaccaratBetType.Banker;
        [SerializeField] private float betAmount = 0f;
        [SerializeField] private float originalAmount = 0f;
        [SerializeField] private float odds = 0f;
        [SerializeField] private bool isExempt = false;

        [Header("时间信息")]
        [SerializeField] private DateTime createdTime;
        [SerializeField] private DateTime confirmedTime;
        [SerializeField] private DateTime settledTime;

        [Header("状态信息")]
        [SerializeField] private BetStatus status = BetStatus.Pending;
        [SerializeField] private BetResult result = BetResult.Pending;
        [SerializeField] private string statusMessage = "";

        [Header("结果信息")]
        [SerializeField] private bool isWin = false;
        [SerializeField] private float payoutAmount = 0f;
        [SerializeField] private float commission = 0f;
        [SerializeField] private float netPayout = 0f;
        [SerializeField] private float profit = 0f;

        [Header("扩展信息")]
        [SerializeField] private Dictionary<string, object> metadata = new Dictionary<string, object>();
        [SerializeField] private List<string> tags = new List<string>();
        [SerializeField] private BetSource source = BetSource.Manual;

        #region 属性访问器

        /// <summary>
        /// 生成投注ID
        /// </summary>
        /// <returns>投注ID</returns>
        private string GenerateBetId()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = UnityEngine.Random.Range(100, 999);
            return $"BET_{timestamp}_{random}";
        }

        #endregion

        #region 状态管理方法

        /// <summary>
        /// 确认投注
        /// </summary>
        /// <returns>是否确认成功</returns>
        public bool ConfirmBet()
        {
            if (status != BetStatus.Pending)
            {
                Debug.LogWarning($"投注{betId}状态错误，无法确认");
                return false;
            }

            status = BetStatus.Confirmed;
            confirmedTime = DateTime.UtcNow;
            statusMessage = "投注已确认";
            
            // 添加确认标签
            AddTag("confirmed");
            AddMetadata("confirmedAt", confirmedTime);

            // 触发确认事件
            OnBetConfirmed?.Invoke(this);

            return true;
        }

        /// <summary>
        /// 取消投注
        /// </summary>
        /// <param name="reason">取消原因</param>
        /// <returns>是否取消成功</returns>
        public bool CancelBet(string reason = "")
        {
            if (status != BetStatus.Pending && status != BetStatus.Confirmed)
            {
                Debug.LogWarning($"投注{betId}状态错误，无法取消");
                return false;
            }

            var previousStatus = status;
            status = BetStatus.Cancelled;
            result = BetResult.Cancelled;
            statusMessage = string.IsNullOrEmpty(reason) ? "投注已取消" : $"投注已取消: {reason}";
            
            // 添加取消信息
            AddTag("cancelled");
            AddMetadata("cancelledAt", DateTime.UtcNow);
            AddMetadata("cancelReason", reason);

            // 触发取消事件
            OnBetCancelled?.Invoke(this, reason);

            return true;
        }

        /// <summary>
        /// 添加投注金额
        /// </summary>
        /// <param name="amount">追加金额</param>
        /// <returns>是否添加成功</returns>
        public bool AddAmount(float amount)
        {
            if (amount <= 0) return false;
            if (status != BetStatus.Pending && status != BetStatus.Confirmed) return false;

            betAmount += amount;
            AddMetadata("lastAddedAmount", amount);
            AddMetadata("lastAddedAt", DateTime.UtcNow);
            AddTag("modified");

            // 触发金额变更事件
            OnAmountChanged?.Invoke(this, amount);

            return true;
        }

        /// <summary>
        /// 设置赔率
        /// </summary>
        /// <param name="newOdds">新赔率</param>
        public void SetOdds(float newOdds)
        {
            var oldOdds = odds;
            odds = Mathf.Max(0f, newOdds);
            
            AddMetadata("previousOdds", oldOdds);
            AddMetadata("oddsUpdatedAt", DateTime.UtcNow);

            // 触发赔率变更事件
            OnOddsChanged?.Invoke(this, oldOdds, odds);
        }

        #endregion

        #region 结果处理方法

        /// <summary>
        /// 处理投注结果
        /// </summary>
        /// <param name="gameResult">游戏结果</param>
        /// <returns>投注结果信息</returns>
        public BetResultInfo ProcessResult(BaccaratGameResult gameResult)
        {
            if (gameResult == null)
            {
                return new BetResultInfo
                {
                    IsValid = false,
                    ErrorMessage = "游戏结果为空"
                };
            }

            if (status != BetStatus.Confirmed)
            {
                return new BetResultInfo
                {
                    IsValid = false,
                    ErrorMessage = "投注未确认，无法处理结果"
                };
            }

            try
            {
                // 1. 判断是否中奖
                isWin = BaccaratLogic.IsBetWinning((int)betType, gameResult);
                
                // 2. 计算赔率（如果未设置）
                if (odds <= 0)
                {
                    odds = BaccaratLogic.GetBetOdds(betType, gameResult, isExempt);
                }

                // 3. 计算派彩
                if (isWin)
                {
                    payoutAmount = betAmount * (1 + odds);
                    
                    // 计算佣金（仅庄家投注且非免佣）
                    if (betType == BaccaratBetType.Banker && !isExempt)
                    {
                        commission = betAmount * odds * 0.05f; // 5%佣金
                        netPayout = payoutAmount - commission;
                    }
                    else
                    {
                        commission = 0f;
                        netPayout = payoutAmount;
                    }
                    
                    profit = netPayout - betAmount;
                    result = BetResult.Win;
                    statusMessage = $"中奖！获得 {netPayout:F2} 元";
                }
                else
                {
                    payoutAmount = 0f;
                    commission = 0f;
                    netPayout = 0f;
                    profit = -betAmount;
                    result = BetResult.Loss;
                    statusMessage = "未中奖";
                }

                // 4. 更新状态
                status = isWin ? BetStatus.Won : BetStatus.Lost;
                settledTime = DateTime.UtcNow;

                // 5. 记录结果信息
                AddMetadata("gameResult", gameResult.winner.ToString());
                AddMetadata("bankerPoints", gameResult.banker_points);
                AddMetadata("playerPoints", gameResult.player_points);
                AddMetadata("settledAt", settledTime);
                AddTag("settled");

                if (isWin)
                {
                    AddTag("winner");
                }

                // 6. 触发结果事件
                var resultInfo = new BetResultInfo
                {
                    IsValid = true,
                    IsWin = isWin,
                    PayoutAmount = netPayout,
                    Profit = profit,
                    Commission = commission,
                    Odds = odds,
                    Description = statusMessage
                };

                OnBetSettled?.Invoke(this, resultInfo);
                return resultInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理投注结果失败: {ex.Message}");
                status = BetStatus.Processing;
                statusMessage = "结果处理失败";
                
                return new BetResultInfo
                {
                    IsValid = false,
                    ErrorMessage = $"处理失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 强制设置结果
        /// </summary>
        /// <param name="win">是否中奖</param>
        /// <param name="payout">派彩金额</param>
        /// <param name="reason">原因</param>
        public void ForceSetResult(bool win, float payout = 0f, string reason = "")
        {
            isWin = win;
            payoutAmount = payout;
            netPayout = payout;
            profit = payout - betAmount;
            result = win ? BetResult.Win : BetResult.Loss;
            status = win ? BetStatus.Won : BetStatus.Lost;
            settledTime = DateTime.UtcNow;
            statusMessage = reason ?? (win ? "强制中奖" : "强制不中奖");

            AddTag("force_settled");
            AddMetadata("forceSettledAt", settledTime);
            AddMetadata("forceReason", reason);

            // 触发强制结果事件
            OnBetForceSettled?.Invoke(this, win, payout);
        }

        #endregion

        #region 元数据管理

        /// <summary>
        /// 添加元数据
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public void AddMetadata(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return;
            metadata[key] = value;
        }

        /// <summary>
        /// 获取元数据
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>值</returns>
        public T GetMetadata<T>(string key, T defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(key) || !metadata.ContainsKey(key))
                return defaultValue;

            try
            {
                return (T)metadata[key];
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 移除元数据
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveMetadata(string key)
        {
            return metadata.Remove(key);
        }

        /// <summary>
        /// 添加标签
        /// </summary>
        /// <param name="tag">标签</param>
        public void AddTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || tags.Contains(tag)) return;
            tags.Add(tag);
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        /// <param name="tag">标签</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveTag(string tag)
        {
            return tags.Remove(tag);
        }

        /// <summary>
        /// 检查是否有标签
        /// </summary>
        /// <param name="tag">标签</param>
        /// <returns>是否有该标签</returns>
        public bool HasTag(string tag)
        {
            return tags.Contains(tag);
        }

        #endregion

        #region 查询和验证方法

        /// <summary>
        /// 检查投注是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public BetValidation ValidateBet()
        {
            var validation = new BetValidation { IsValid = true };

            // 检查基础信息
            if (string.IsNullOrEmpty(playerId))
            {
                validation.IsValid = false;
                validation.Errors.Add("玩家ID不能为空");
            }

            if (betAmount <= 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("投注金额必须大于0");
            }

            if (!Enum.IsDefined(typeof(BaccaratBetType), betType))
            {
                validation.IsValid = false;
                validation.Errors.Add("无效的投注类型");
            }

            // 检查状态一致性
            if (status == BetStatus.Won && !isWin)
            {
                validation.IsValid = false;
                validation.Errors.Add("状态与结果不一致");
            }

            return validation;
        }

        /// <summary>
        /// 获取投注摘要
        /// </summary>
        /// <returns>投注摘要</returns>
        public BetSummary GetSummary()
        {
            return new BetSummary
            {
                BetId = betId,
                BetType = betType.ToString(),
                BetAmount = betAmount,
                Status = status.ToString(),
                IsWin = isWin,
                PayoutAmount = netPayout,
                Profit = profit,
                CreatedTime = createdTime,
                SettledTime = settledTime,
                Duration = status == BetStatus.Won || status == BetStatus.Lost ? 
                          settledTime - createdTime : DateTime.UtcNow - createdTime
            };
        }

        /// <summary>
        /// 计算投注价值评分
        /// </summary>
        /// <returns>价值评分(0-100)</returns>
        public float CalculateValueScore()
        {
            float score = 0f;

            // 基础分数：根据投注金额
            score += Mathf.Clamp(betAmount / 1000f * 20f, 0f, 20f);

            // 赔率分数：高赔率给更高分数
            score += Mathf.Clamp(odds * 10f, 0f, 30f);

            // 结果分数：中奖给高分
            if (isWin)
                score += 30f;

            // 时间分数：快速确认给分
            if (status == BetStatus.Confirmed)
            {
                var confirmDelay = (confirmedTime - createdTime).TotalSeconds;
                score += Mathf.Clamp(10f - (float)confirmDelay / 6f, 0f, 10f);
            }

            // 特殊标签加分
            if (HasTag("vip"))
                score += 5f;
            if (HasTag("bonus"))
                score += 5f;

            return Mathf.Clamp(score, 0f, 100f);
        }

        #endregion

        #region 转换和序列化方法

        /// <summary>
        /// 转换为BaccaratBetRequest
        /// </summary>
        /// <returns>投注请求</returns>
        public BaccaratBetRequest ToBetRequest()
        {
            return new BaccaratBetRequest
            {
                money = betAmount,
                rate_id = (int)betType,
                betType = betType.ToString(),
                isMainBet = betType <= BaccaratBetType.Tie,
                originalAmount = originalAmount,
                timestamp = createdTime
            };
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        /// <returns>字典格式</returns>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["betId"] = betId,
                ["playerId"] = playerId,
                ["gameNumber"] = gameNumber,
                ["tableId"] = tableId,
                ["betType"] = betType.ToString(),
                ["betAmount"] = betAmount,
                ["originalAmount"] = originalAmount,
                ["odds"] = odds,
                ["isExempt"] = isExempt,
                ["createdTime"] = createdTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ["confirmedTime"] = confirmedTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ["settledTime"] = settledTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ["status"] = status.ToString(),
                ["result"] = result.ToString(),
                ["statusMessage"] = statusMessage,
                ["isWin"] = isWin,
                ["payoutAmount"] = payoutAmount,
                ["commission"] = commission,
                ["netPayout"] = netPayout,
                ["profit"] = profit,
                ["source"] = source.ToString(),
                ["tags"] = tags,
                ["metadata"] = metadata
            };

            return dict;
        }

        /// <summary>
        /// 转换为JSON
        /// </summary>
        /// <returns>JSON字符串</returns>
        public string ToJson()
        {
            try
            {
                return JsonUtility.ToJson(this, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Bet.ToJson失败: {ex.Message}");
                return "{}";
            }
        }

        /// <summary>
        /// 从JSON创建Bet
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>投注实例</returns>
        public static Bet FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<Bet>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Bet.FromJson失败: {ex.Message}");
                return new Bet();
            }
        }

        /// <summary>
        /// 克隆投注
        /// </summary>
        /// <returns>克隆的投注</returns>
        public Bet Clone()
        {
            var json = ToJson();
            var cloned = FromJson(json);
            cloned.betId = GenerateBetId(); // 生成新的ID
            return cloned;
        }

        #endregion

        #region 比较和相等性

        /// <summary>
        /// 相等性比较
        /// </summary>
        /// <param name="other">其他投注</param>
        /// <returns>是否相等</returns>
        public bool Equals(Bet other)
        {
            if (other == null) return false;
            return betId == other.betId;
        }

        /// <summary>
        /// 重写Equals
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Bet);
        }

        /// <summary>
        /// 重写GetHashCode
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode()
        {
            return betId?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// 比较大小（按创建时间排序）
        /// </summary>
        /// <param name="other">其他投注</param>
        /// <returns>比较结果</returns>
        public int CompareTo(Bet other)
        {
            if (other == null) return 1;
            return createdTime.CompareTo(other.createdTime);
        }

        /// <summary>
        /// 重写ToString
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"Bet[{betId}] {betType} {betAmount:F2} - {status} ({statusMessage})";
        }

        #endregion

        #region 事件

        /// <summary>
        /// 投注确认事件
        /// </summary>
        public static event System.Action<Bet> OnBetConfirmed;

        /// <summary>
        /// 投注取消事件
        /// </summary>
        public static event System.Action<Bet, string> OnBetCancelled;

        /// <summary>
        /// 金额变更事件
        /// </summary>
        public static event System.Action<Bet, float> OnAmountChanged;

        /// <summary>
        /// 赔率变更事件
        /// </summary>
        public static event System.Action<Bet, float, float> OnOddsChanged;

        /// <summary>
        /// 投注结算事件
        /// </summary>
        public static event System.Action<Bet, BetResultInfo> OnBetSettled;

        /// <summary>
        /// 强制结算事件
        /// </summary>
        public static event System.Action<Bet, bool, float> OnBetForceSettled;

        #endregion
    }

    #region 辅助数据类型

    /// <summary>
    /// 投注结果枚举
    /// </summary>
    public enum BetResult
    {
        Pending,    // 待定
        Win,        // 中奖
        Loss,       // 未中奖
        Push,       // 平局
        Cancelled,  // 已取消
        Void        // 无效
    }

    /// <summary>
    /// 投注来源枚举
    /// </summary>
    public enum BetSource
    {
        Manual,     // 手动投注
        AutoPlay,   // 自动投注
        Quick,      // 快速投注
        Repeat,     // 重复投注
        System,     // 系统投注
        Bonus       // 奖金投注
    }

    /// <summary>
    /// 投注结果信息
    /// </summary>
    [System.Serializable]
    public class BetResultInfo
    {
        public bool IsValid;
        public bool IsWin;
        public float PayoutAmount;
        public float Profit;
        public float Commission;
        public float Odds;
        public string Description;
        public string ErrorMessage;
    }

    /// <summary>
    /// 投注验证结果
    /// </summary>
    [System.Serializable]
    public class BetValidation
    {
        public bool IsValid = true;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// 投注摘要
    /// </summary>
    [System.Serializable]
    public class BetSummary
    {
        public string BetId;
        public string BetType;
        public float BetAmount;
        public string Status;
        public bool IsWin;
        public float PayoutAmount;
        public float Profit;
        public DateTime CreatedTime;
        public DateTime SettledTime;
        public TimeSpan Duration;
    }

    #endregion
}投注ID
        /// </summary>
        public string BetId 
        { 
            get => betId; 
            set => betId = value ?? Guid.NewGuid().ToString(); 
        }

        /// <summary>
        /// 玩家ID
        /// </summary>
        public string PlayerId 
        { 
            get => playerId; 
            set => playerId = value ?? ""; 
        }

        /// <summary>
        /// 游戏局号
        /// </summary>
        public string GameNumber 
        { 
            get => gameNumber; 
            set => gameNumber = value ?? ""; 
        }

        /// <summary>
        /// 桌台ID
        /// </summary>
        public string TableId 
        { 
            get => tableId; 
            set => tableId = value ?? ""; 
        }

        /// <summary>
        /// 投注类型
        /// </summary>
        public BaccaratBetType BetType 
        { 
            get => betType; 
            set => betType = value; 
        }

        /// <summary>
        /// 投注金额
        /// </summary>
        public float BetAmount 
        { 
            get => betAmount; 
            set => betAmount = Mathf.Max(0f, value); 
        }

        /// <summary>
        /// 原始金额
        /// </summary>
        public float OriginalAmount 
        { 
            get => originalAmount; 
            set => originalAmount = Mathf.Max(0f, value); 
        }

        /// <summary>
        /// 赔率
        /// </summary>
        public float Odds 
        { 
            get => odds; 
            set => odds = Mathf.Max(0f, value); 
        }

        /// <summary>
        /// 是否免佣
        /// </summary>
        public bool IsExempt 
        { 
            get => isExempt; 
            set => isExempt = value; 
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime 
        { 
            get => createdTime; 
            set => createdTime = value; 
        }

        /// <summary>
        /// 确认时间
        /// </summary>
        public DateTime ConfirmedTime 
        { 
            get => confirmedTime; 
            set => confirmedTime = value; 
        }

        /// <summary>
        /// 结算时间
        /// </summary>
        public DateTime SettledTime 
        { 
            get => settledTime; 
            set => settledTime = value; 
        }

        /// <summary>
        /// 投注状态
        /// </summary>
        public BetStatus Status 
        { 
            get => status; 
            private set => status = value; 
        }

        /// <summary>
        /// 投注结果
        /// </summary>
        public BetResult Result 
        { 
            get => result; 
            private set => result = value; 
        }

        /// <summary>
        /// 状态信息
        /// </summary>
        public string StatusMessage 
        { 
            get => statusMessage; 
            private set => statusMessage = value ?? ""; 
        }

        /// <summary>
        /// 是否中奖
        /// </summary>
        public bool IsWin 
        { 
            get => isWin; 
            private set => isWin = value; 
        }

        /// <summary>
        /// 派彩金额
        /// </summary>
        public float PayoutAmount 
        { 
            get => payoutAmount; 
            private set => payoutAmount = Mathf.Max(0f, value); 
        }

        /// <summary>
        /// 佣金
        /// </summary>
        public float Commission 
        { 
            get => commission; 
            private set => commission = Mathf.Max(0f, value); 
        }

        /// <summary>
        /// 净派彩
        /// </summary>
        public float NetPayout 
        { 
            get => netPayout; 
            private set => netPayout = Mathf.Max(0f, value); 
        }

        /// <summary>
        /// 盈亏
        /// </summary>
        public float Profit 
        { 
            get => profit; 
            private set => profit = value; 
        }

        /// <summary>
        /// 元数据
        /// </summary>
        public Dictionary<string, object> Metadata => new Dictionary<string, object>(metadata);

        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags => new List<string>(tags);

        /// <summary>
        /// 投注来源
        /// </summary>
        public BetSource Source 
        { 
            get => source; 
            set => source = value; 
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Bet()
        {
            InitializeDefaults();
        }

        /// <summary>
        /// 基础信息构造函数
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="betType">投注类型</param>
        /// <param name="amount">投注金额</param>
        public Bet(string playerId, BaccaratBetType betType, float amount)
        {
            this.playerId = playerId ?? "";
            this.betType = betType;
            this.betAmount = Mathf.Max(0f, amount);
            this.originalAmount = this.betAmount;
            InitializeDefaults();
        }

        /// <summary>
        /// 完整信息构造函数
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="gameNumber">游戏局号</param>
        /// <param name="betType">投注类型</param>
        /// <param name="amount">投注金额</param>
        public Bet(string playerId, string gameNumber, BaccaratBetType betType, float amount)
        {
            this.playerId = playerId ?? "";
            this.gameNumber = gameNumber ?? "";
            this.betType = betType;
            this.betAmount = Mathf.Max(0f, amount);
            this.originalAmount = this.betAmount;
            InitializeDefaults();
        }

        /// <summary>
        /// 从BaccaratBetRequest创建Bet
        /// </summary>
        /// <param name="betRequest">投注请求</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="gameNumber">游戏局号</param>
        /// <returns>投注实例</returns>
        public static Bet FromBetRequest(BaccaratBetRequest betRequest, string playerId, string gameNumber)
        {
            if (betRequest == null) return new Bet();

            var bet = new Bet
            {
                playerId = playerId ?? "",
                gameNumber = gameNumber ?? "",
                betType = (BaccaratBetType)betRequest.rate_id,
                betAmount = betRequest.money,
                originalAmount = betRequest.money,
                isExempt = betRequest.betType?.ToLower().Contains("exempt") ?? false
            };

            // 添加来源信息
            bet.AddMetadata("betTypeDescription", betRequest.betType ?? "");
            bet.AddMetadata("isMainBet", betRequest.isMainBet);
            bet.AddMetadata("requestTimestamp", betRequest.timestamp);

            return bet;
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化默认值
        /// </summary>
        private void InitializeDefaults()
        {
            if (string.IsNullOrEmpty(betId))
                betId = GenerateBetId();
            
            createdTime = DateTime.UtcNow;
            status = BetStatus.Pending;
            result = BetResult.Pending;
            statusMessage = "投注已创建";
            source = BetSource.Manual;
            
            if (metadata == null)
                metadata = new Dictionary<string, object>();
            
            if (tags == null)
                tags = new List<string>();
        }

        /// <summary>
        ///