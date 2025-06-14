// Assets/Game/Entities/Player.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Data.Types;
using Core.Architecture;

namespace Game.Entities
{
    /// <summary>
    /// 玩家实体类
    /// 表示百家乐游戏中的玩家，包含用户信息、投注历史、统计数据等
    /// </summary>
    [System.Serializable]
    public class Player : IEquatable<Player>
    {
        [Header("基础信息")]
        [SerializeField] private string userId = "";
        [SerializeField] private string nickname = "";
        [SerializeField] private string avatar = "";
        [SerializeField] private int level = 1;
        [SerializeField] private int vipLevel = 0;

        [Header("账户信息")]
        [SerializeField] private float balance = 0f;
        [SerializeField] private string currency = "CNY";
        [SerializeField] private DateTime lastLoginTime;
        [SerializeField] private DateTime registerTime;

        [Header("游戏状态")]
        [SerializeField] private bool isOnline = false;
        [SerializeField] private bool isPlaying = false;
        [SerializeField] private string currentTableId = "";
        [SerializeField] private PlayerStatus status = PlayerStatus.Idle;

        [Header("投注信息")]
        [SerializeField] private List<Bet> currentBets = new List<Bet>();
        [SerializeField] private List<Bet> betHistory = new List<Bet>();
        [SerializeField] private float totalBetAmount = 0f;
        [SerializeField] private float sessionBetAmount = 0f;

        [Header("统计数据")]
        [SerializeField] private PlayerStatistics statistics = new PlayerStatistics();
        [SerializeField] private PlayerPreferences preferences = new PlayerPreferences();

        #region 属性访问器

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId 
        { 
            get => userId; 
            set => userId = value ?? ""; 
        }

        /// <summary>
        /// 昵称
        /// </summary>
        public string Nickname 
        { 
            get => nickname; 
            set => nickname = value ?? ""; 
        }

        /// <summary>
        /// 头像URL
        /// </summary>
        public string Avatar 
        { 
            get => avatar; 
            set => avatar = value ?? ""; 
        }

        /// <summary>
        /// 玩家等级
        /// </summary>
        public int Level 
        { 
            get => level; 
            set => level = Mathf.Max(1, value); 
        }

        /// <summary>
        /// VIP等级
        /// </summary>
        public int VipLevel 
        { 
            get => vipLevel; 
            set => vipLevel = Mathf.Max(0, value); 
        }

        /// <summary>
        /// 余额
        /// </summary>
        public float Balance 
        { 
            get => balance; 
            set => balance = Mathf.Max(0f, value); 
        }

        /// <summary>
        /// 货币类型
        /// </summary>
        public string Currency 
        { 
            get => currency; 
            set => currency = value ?? "CNY"; 
        }

        /// <summary>
        /// 是否在线
        /// </summary>
        public bool IsOnline 
        { 
            get => isOnline; 
            set => isOnline = value; 
        }

        /// <summary>
        /// 是否正在游戏
        /// </summary>
        public bool IsPlaying 
        { 
            get => isPlaying; 
            set => isPlaying = value; 
        }

        /// <summary>
        /// 当前桌台ID
        /// </summary>
        public string CurrentTableId 
        { 
            get => currentTableId; 
            set => currentTableId = value ?? ""; 
        }

        /// <summary>
        /// 玩家状态
        /// </summary>
        public PlayerStatus Status 
        { 
            get => status; 
            set => status = value; 
        }

        /// <summary>
        /// 当前投注列表
        /// </summary>
        public List<Bet> CurrentBets => new List<Bet>(currentBets);

        /// <summary>
        /// 投注历史
        /// </summary>
        public List<Bet> BetHistory => new List<Bet>(betHistory);

        /// <summary>
        /// 总投注金额
        /// </summary>
        public float TotalBetAmount => totalBetAmount;

        /// <summary>
        /// 本局投注金额
        /// </summary>
        public float SessionBetAmount => sessionBetAmount;

        /// <summary>
        /// 统计数据
        /// </summary>
        public PlayerStatistics Statistics => statistics;

        /// <summary>
        /// 玩家偏好设置
        /// </summary>
        public PlayerPreferences Preferences => preferences;

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Player()
        {
            InitializeDefaults();
        }

        /// <summary>
        /// 带用户ID的构造函数
        /// </summary>
        /// <param name="userId">用户ID</param>
        public Player(string userId)
        {
            this.userId = userId ?? "";
            InitializeDefaults();
        }

        /// <summary>
        /// 完整信息构造函数
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="nickname">昵称</param>
        /// <param name="balance">余额</param>
        public Player(string userId, string nickname, float balance)
        {
            this.userId = userId ?? "";
            this.nickname = nickname ?? "";
            this.balance = Mathf.Max(0f, balance);
            InitializeDefaults();
        }

        /// <summary>
        /// 从UserInfo创建Player
        /// </summary>
        /// <param name="userInfo">用户信息</param>
        /// <returns>玩家实例</returns>
        public static Player FromUserInfo(UserInfo userInfo)
        {
            if (userInfo == null) return new Player();

            var player = new Player
            {
                userId = userInfo.user_id ?? "",
                nickname = userInfo.nickname ?? "",
                avatar = userInfo.avatar ?? "",
                level = userInfo.level,
                vipLevel = userInfo.vip_level,
                balance = userInfo.balance,
                currency = userInfo.currency ?? "CNY"
            };

            return player;
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化默认值
        /// </summary>
        private void InitializeDefaults()
        {
            lastLoginTime = DateTime.UtcNow;
            registerTime = DateTime.UtcNow;
            status = PlayerStatus.Idle;
            
            if (statistics == null)
                statistics = new PlayerStatistics();
            
            if (preferences == null)
                preferences = new PlayerPreferences();
            
            if (currentBets == null)
                currentBets = new List<Bet>();
            
            if (betHistory == null)
                betHistory = new List<Bet>();
        }

        #endregion

        #region 投注管理方法

        /// <summary>
        /// 添加投注
        /// </summary>
        /// <param name="bet">投注对象</param>
        /// <returns>是否添加成功</returns>
        public bool AddBet(Bet bet)
        {
            if (bet == null) return false;

            // 验证余额
            if (bet.BetAmount > balance)
            {
                Debug.LogWarning($"余额不足: 需要{bet.BetAmount}, 当前{balance}");
                return false;
            }

            // 验证重复投注
            var existingBet = currentBets.FirstOrDefault(b => 
                b.BetType == bet.BetType && b.GameNumber == bet.GameNumber);

            if (existingBet != null)
            {
                // 累加投注金额
                existingBet.AddAmount(bet.BetAmount);
            }
            else
            {
                // 添加新投注
                bet.PlayerId = userId;
                currentBets.Add(bet);
            }

            // 更新金额
            sessionBetAmount += bet.BetAmount;
            totalBetAmount += bet.BetAmount;

            // 更新统计
            statistics.IncrementBetsPlaced();

            return true;
        }

        /// <summary>
        /// 移除投注
        /// </summary>
        /// <param name="betId">投注ID</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveBet(string betId)
        {
            var bet = currentBets.FirstOrDefault(b => b.BetId == betId);
            if (bet == null) return false;

            currentBets.Remove(bet);
            sessionBetAmount -= bet.BetAmount;
            totalBetAmount -= bet.BetAmount;

            return true;
        }

        /// <summary>
        /// 清空当前投注
        /// </summary>
        public void ClearCurrentBets()
        {
            foreach (var bet in currentBets)
            {
                sessionBetAmount -= bet.BetAmount;
                totalBetAmount -= bet.BetAmount;
            }
            
            currentBets.Clear();
        }

        /// <summary>
        /// 确认投注
        /// </summary>
        /// <param name="gameNumber">游戏局号</param>
        /// <returns>确认的投注列表</returns>
        public List<Bet> ConfirmBets(string gameNumber)
        {
            var confirmedBets = new List<Bet>();

            foreach (var bet in currentBets)
            {
                if (bet.GameNumber == gameNumber)
                {
                    bet.ConfirmBet();
                    confirmedBets.Add(bet);
                    betHistory.Add(bet);
                }
            }

            // 从当前投注中移除已确认的投注
            currentBets.RemoveAll(b => b.GameNumber == gameNumber && b.Status == BetStatus.Confirmed);

            return confirmedBets;
        }

        /// <summary>
        /// 处理投注结果
        /// </summary>
        /// <param name="gameResult">游戏结果</param>
        public void ProcessBetResults(BaccaratGameResult gameResult)
        {
            if (gameResult == null) return;

            var gameBets = betHistory.Where(b => b.GameNumber == gameResult.game_number).ToList();
            float totalWin = 0f;
            int winCount = 0;

            foreach (var bet in gameBets)
            {
                var result = bet.ProcessResult(gameResult);
                if (result.IsWin)
                {
                    balance += result.PayoutAmount;
                    totalWin += result.PayoutAmount;
                    winCount++;
                }
            }

            // 更新统计
            statistics.RecordGameResult(gameBets.Count, winCount, totalWin);
        }

        #endregion

        #region 余额管理方法

        /// <summary>
        /// 增加余额
        /// </summary>
        /// <param name="amount">金额</param>
        /// <param name="reason">原因</param>
        public void AddBalance(float amount, string reason = "")
        {
            if (amount <= 0) return;

            balance += amount;
            
            // 记录余额变化
            var transaction = new BalanceTransaction
            {
                amount = amount,
                type = TransactionType.Credit,
                reason = reason,
                timestamp = DateTime.UtcNow,
                balanceAfter = balance
            };

            statistics.RecordTransaction(transaction);
        }

        /// <summary>
        /// 扣除余额
        /// </summary>
        /// <param name="amount">金额</param>
        /// <param name="reason">原因</param>
        /// <returns>是否扣除成功</returns>
        public bool DeductBalance(float amount, string reason = "")
        {
            if (amount <= 0 || amount > balance) return false;

            balance -= amount;
            
            // 记录余额变化
            var transaction = new BalanceTransaction
            {
                amount = -amount,
                type = TransactionType.Debit,
                reason = reason,
                timestamp = DateTime.UtcNow,
                balanceAfter = balance
            };

            statistics.RecordTransaction(transaction);
            return true;
        }

        /// <summary>
        /// 检查是否有足够余额
        /// </summary>
        /// <param name="amount">需要的金额</param>
        /// <returns>是否有足够余额</returns>
        public bool HasSufficientBalance(float amount)
        {
            return balance >= amount;
        }

        #endregion

        #region 状态管理方法

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="tableId">桌台ID</param>
        public void Login(string tableId = "")
        {
            isOnline = true;
            lastLoginTime = DateTime.UtcNow;
            status = PlayerStatus.Online;
            
            if (!string.IsNullOrEmpty(tableId))
            {
                JoinTable(tableId);
            }

            statistics.IncrementLoginCount();
        }

        /// <summary>
        /// 登出
        /// </summary>
        public void Logout()
        {
            isOnline = false;
            isPlaying = false;
            status = PlayerStatus.Offline;
            currentTableId = "";
            
            // 清理当前投注
            ClearCurrentBets();

            statistics.RecordSessionEnd();
        }

        /// <summary>
        /// 加入桌台
        /// </summary>
        /// <param name="tableId">桌台ID</param>
        public void JoinTable(string tableId)
        {
            currentTableId = tableId ?? "";
            isPlaying = true;
            status = PlayerStatus.Playing;
            sessionBetAmount = 0f;

            statistics.IncrementTablesJoined();
        }

        /// <summary>
        /// 离开桌台
        /// </summary>
        public void LeaveTable()
        {
            currentTableId = "";
            isPlaying = false;
            status = PlayerStatus.Online;
            
            // 清理当前投注
            ClearCurrentBets();
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取指定游戏的投注
        /// </summary>
        /// <param name="gameNumber">游戏局号</param>
        /// <returns>投注列表</returns>
        public List<Bet> GetBetsForGame(string gameNumber)
        {
            return betHistory.Where(b => b.GameNumber == gameNumber).ToList();
        }

        /// <summary>
        /// 获取指定类型的投注历史
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <param name="count">数量</param>
        /// <returns>投注列表</returns>
        public List<Bet> GetBetsByType(BaccaratBetType betType, int count = 10)
        {
            return betHistory
                .Where(b => b.BetType == betType)
                .OrderByDescending(b => b.CreatedTime)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// 获取近期投注历史
        /// </summary>
        /// <param name="days">天数</param>
        /// <returns>投注列表</returns>
        public List<Bet> GetRecentBets(int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return betHistory
                .Where(b => b.CreatedTime >= cutoffDate)
                .OrderByDescending(b => b.CreatedTime)
                .ToList();
        }

        /// <summary>
        /// 计算净盈亏
        /// </summary>
        /// <returns>净盈亏金额</returns>
        public float CalculateNetProfit()
        {
            float totalBet = betHistory.Sum(b => b.BetAmount);
            float totalWin = betHistory.Where(b => b.Status == BetStatus.Won).Sum(b => b.PayoutAmount);
            return totalWin - totalBet;
        }

        /// <summary>
        /// 获取胜率
        /// </summary>
        /// <returns>胜率百分比</returns>
        public float GetWinRate()
        {
            var settledBets = betHistory.Where(b => b.Status == BetStatus.Won || b.Status == BetStatus.Lost).ToList();
            if (settledBets.Count == 0) return 0f;

            var winBets = settledBets.Count(b => b.Status == BetStatus.Won);
            return (float)winBets / settledBets.Count * 100f;
        }

        #endregion

        #region 序列化方法

        /// <summary>
        /// 转换为UserInfo
        /// </summary>
        /// <returns>用户信息</returns>
        public UserInfo ToUserInfo()
        {
            return new UserInfo
            {
                user_id = userId,
                nickname = nickname,
                avatar = avatar,
                level = level,
                vip_level = vipLevel,
                balance = balance,
                money_balance = balance,
                currency = currency
            };
        }

        /// <summary>
        /// 转换为JSON
        /// </summary>
        /// <returns>JSON字符串</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// 从JSON创建玩家
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>玩家实例</returns>
        public static Player FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<Player>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Player.FromJson失败: {ex.Message}");
                return new Player();
            }
        }

        #endregion

        #region IEquatable实现

        /// <summary>
        /// 相等性比较
        /// </summary>
        /// <param name="other">其他玩家</param>
        /// <returns>是否相等</returns>
        public bool Equals(Player other)
        {
            if (other == null) return false;
            return userId == other.userId;
        }

        /// <summary>
        /// 重写Equals
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Player);
        }

        /// <summary>
        /// 重写GetHashCode
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode()
        {
            return userId?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// 重写ToString
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"Player[{userId}] {nickname} (Level {level}, Balance {balance:F2} {currency})";
        }

        #endregion
    }

    #region 辅助数据类型

    /// <summary>
    /// 玩家状态枚举
    /// </summary>
    public enum PlayerStatus
    {
        Offline,    // 离线
        Online,     // 在线
        Idle,       // 空闲
        Playing,    // 游戏中
        Away,       // 暂离
        Banned      // 被禁
    }

    /// <summary>
    /// 玩家统计数据
    /// </summary>
    [System.Serializable]
    public class PlayerStatistics
    {
        [Header("基础统计")]
        public int totalGamesPlayed = 0;
        public int totalBetsPlaced = 0;
        public int totalWins = 0;
        public int totalLosses = 0;
        public float totalAmountWagered = 0f;
        public float totalAmountWon = 0f;

        [Header("会话统计")]
        public int loginCount = 0;
        public int tablesJoined = 0;
        public DateTime lastSessionStart;
        public TimeSpan totalPlayTime;

        [Header("连胜统计")]
        public int currentWinStreak = 0;
        public int currentLossStreak = 0;
        public int longestWinStreak = 0;
        public int longestLossStreak = 0;

        [Header("余额历史")]
        public List<BalanceTransaction> transactions = new List<BalanceTransaction>();

        public void IncrementBetsPlaced() => totalBetsPlaced++;
        public void IncrementLoginCount() => loginCount++;
        public void IncrementTablesJoined() => tablesJoined++;

        public void RecordGameResult(int betsCount, int winsCount, float winAmount)
        {
            totalGamesPlayed++;
            totalWins += winsCount;
            totalLosses += (betsCount - winsCount);
            totalAmountWon += winAmount;

            if (winsCount > 0)
            {
                currentWinStreak++;
                currentLossStreak = 0;
                longestWinStreak = Mathf.Max(longestWinStreak, currentWinStreak);
            }
            else
            {
                currentLossStreak++;
                currentWinStreak = 0;
                longestLossStreak = Mathf.Max(longestLossStreak, currentLossStreak);
            }
        }

        public void RecordTransaction(BalanceTransaction transaction)
        {
            transactions.Add(transaction);
            
            // 限制历史记录数量
            if (transactions.Count > 1000)
            {
                transactions.RemoveAt(0);
            }
        }

        public void RecordSessionEnd()
        {
            // 计算本次会话时长
            var sessionDuration = DateTime.UtcNow - lastSessionStart;
            totalPlayTime = totalPlayTime.Add(sessionDuration);
        }
    }

    /// <summary>
    /// 玩家偏好设置
    /// </summary>
    [System.Serializable]
    public class PlayerPreferences
    {
        [Header("游戏偏好")]
        public bool enableSound = true;
        public bool enableAnimation = true;
        public bool autoConfirmBets = false;
        public bool showStatistics = true;

        [Header("界面偏好")]
        public string preferredLanguage = "zh-CN";
        public bool darkMode = false;
        public float soundVolume = 1.0f;

        [Header("投注偏好")]
        public List<float> favoriteChipValues = new List<float> { 10f, 50f, 100f, 500f, 1000f };
        public BaccaratBetType preferredBetType = BaccaratBetType.Banker;
        public bool enableQuickBet = true;
    }

    /// <summary>
    /// 余额交易记录
    /// </summary>
    [System.Serializable]
    public class BalanceTransaction
    {
        public float amount;
        public TransactionType type;
        public string reason;
        public DateTime timestamp;
        public float balanceAfter;
    }

    /// <summary>
    /// 交易类型
    /// </summary>
    public enum TransactionType
    {
        Credit,     // 入账
        Debit,      // 出账
        Refund,     // 退款
        Bonus,      // 奖金
        Commission  // 佣金
    }

    #endregion
}