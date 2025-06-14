using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Random = UnityEngine.Random;

namespace SlotMachine.Testing
{
    /// <summary>
    /// Mock数据生成器 - 为测试和开发提供模拟数据
    /// </summary>
    public class MockDataGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private bool autoGenerateOnStart = true;
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private int defaultDataCount = 100;
        
        [Header("Slot Machine Settings")]
        [SerializeField] private SlotMachineConfig slotConfig;
        
        [Header("Player Settings")]
        [SerializeField] private PlayerConfig playerConfig;
        
        [Header("Game Session Settings")]
        [SerializeField] private SessionConfig sessionConfig;
        
        // 缓存生成的数据
        private static Dictionary<string, object> cachedData = new Dictionary<string, object>();
        
        // 事件
        public static System.Action<string> OnDataGenerated;
        public static System.Action OnAllDataGenerated;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (autoGenerateOnStart)
            {
                GenerateAllMockData();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 生成所有Mock数据
        /// </summary>
        public void GenerateAllMockData()
        {
            LogInfo("开始生成所有Mock数据...");
            
            try
            {
                // 生成基础数据
                GenerateSlotMachineData();
                GeneratePlayerData();
                GenerateGameSessionData();
                
                // 生成游戏相关数据
                GenerateSpinResults();
                GenerateBonusData();
                GenerateJackpotData();
                
                // 生成统计数据
                GenerateStatisticsData();
                
                OnAllDataGenerated?.Invoke();
                LogInfo("所有Mock数据生成完成");
            }
            catch (Exception e)
            {
                LogError($"生成Mock数据失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 获取Mock数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="dataKey">数据键</param>
        /// <returns>Mock数据</returns>
        public static T GetMockData<T>(string dataKey) where T : class
        {
            if (cachedData.ContainsKey(dataKey))
            {
                return cachedData[dataKey] as T;
            }
            
            Debug.LogWarning($"[MockDataGenerator] 未找到Mock数据: {dataKey}");
            return null;
        }
        
        /// <summary>
        /// 检查是否存在Mock数据
        /// </summary>
        /// <param name="dataKey">数据键</param>
        /// <returns>是否存在</returns>
        public static bool HasMockData(string dataKey)
        {
            return cachedData.ContainsKey(dataKey);
        }
        
        /// <summary>
        /// 清除所有Mock数据
        /// </summary>
        public static void ClearAllMockData()
        {
            cachedData.Clear();
            Debug.Log("[MockDataGenerator] 已清除所有Mock数据");
        }
        
        #endregion
        
        #region Slot Machine Data Generation
        
        /// <summary>
        /// 生成老虎机配置数据
        /// </summary>
        private void GenerateSlotMachineData()
        {
            LogInfo("生成老虎机配置数据...");
            
            var slotMachines = new List<SlotMachineData>();
            
            for (int i = 0; i < 5; i++)
            {
                var slotMachine = new SlotMachineData
                {
                    id = $"slot_{i + 1}",
                    name = $"经典老虎机 {i + 1}",
                    type = GetRandomSlotType(),
                    minBet = GetRandomMinBet(),
                    maxBet = GetRandomMaxBet(),
                    paylines = GetRandomPaylines(),
                    rtp = GetRandomRTP(),
                    volatility = GetRandomVolatility(),
                    symbols = GenerateSymbols(),
                    paytable = GeneratePaytable(),
                    bonusFeatures = GenerateBonusFeatures(),
                    isActive = Random.value > 0.2f, // 80%概率激活
                    createdAt = GetRandomDateTime(-30, 0),
                    updatedAt = GetRandomDateTime(-7, 0)
                };
                
                slotMachines.Add(slotMachine);
            }
            
            CacheData("slotMachines", slotMachines);
            OnDataGenerated?.Invoke("slotMachines");
        }
        
        /// <summary>
        /// 生成老虎机符号
        /// </summary>
        private List<SlotSymbol> GenerateSymbols()
        {
            var symbols = new List<SlotSymbol>();
            string[] symbolNames = { "樱桃", "柠檬", "橙子", "李子", "铃铛", "钻石", "七", "BAR" };
            
            for (int i = 0; i < symbolNames.Length; i++)
            {
                var symbol = new SlotSymbol
                {
                    id = i,
                    name = symbolNames[i],
                    value = (i + 1) * 10,
                    rarity = GetSymbolRarity(i),
                    isWild = i == symbolNames.Length - 1, // 最后一个是百搭
                    isScatter = i == symbolNames.Length - 2, // 倒数第二个是散布
                    multiplier = i >= 6 ? Random.Range(2f, 5f) : 1f
                };
                
                symbols.Add(symbol);
            }
            
            return symbols;
        }
        
        /// <summary>
        /// 生成赔付表
        /// </summary>
        private Dictionary<string, PayoutData> GeneratePaytable()
        {
            var paytable = new Dictionary<string, PayoutData>();
            string[] combinations = { "3x樱桃", "3x柠檬", "3x橙子", "3x李子", "3x铃铛", "3x钻石", "3x七", "3xBAR" };
            
            for (int i = 0; i < combinations.Length; i++)
            {
                var payout = new PayoutData
                {
                    combination = combinations[i],
                    multiplier = (i + 1) * 5,
                    probability = 1f / (float)Math.Pow(2, i + 3), // 递减概率
                    minBetRequired = 1f
                };
                
                paytable[combinations[i]] = payout;
            }
            
            return paytable;
        }
        
        /// <summary>
        /// 生成奖励功能
        /// </summary>
        private List<BonusFeature> GenerateBonusFeatures()
        {
            var features = new List<BonusFeature>
            {
                new BonusFeature
                {
                    id = "free_spins",
                    name = "免费旋转",
                    type = BonusType.FreeSpins,
                    triggerSymbols = new List<int> { 6 }, // 散布符号
                    minTriggerCount = 3,
                    baseReward = 10,
                    multiplier = 2f,
                    isActive = true
                },
                new BonusFeature
                {
                    id = "bonus_game",
                    name = "奖励游戏",
                    type = BonusType.BonusGame,
                    triggerSymbols = new List<int> { 7 }, // 特殊符号
                    minTriggerCount = 3,
                    baseReward = 50,
                    multiplier = 1f,
                    isActive = Random.value > 0.3f
                }
            };
            
            return features;
        }
        
        #endregion
        
        #region Player Data Generation
        
        /// <summary>
        /// 生成玩家数据
        /// </summary>
        private void GeneratePlayerData()
        {
            LogInfo("生成玩家数据...");
            
            var players = new List<PlayerData>();
            
            for (int i = 0; i < defaultDataCount; i++)
            {
                var player = new PlayerData
                {
                    id = $"player_{i + 1:D4}",
                    username = GenerateRandomUsername(),
                    email = GenerateRandomEmail(),
                    level = Random.Range(1, 51),
                    experience = Random.Range(0, 100000),
                    balance = Random.Range(10f, 10000f),
                    totalWagered = Random.Range(100f, 50000f),
                    totalWon = Random.Range(50f, 60000f),
                    gamesPlayed = Random.Range(10, 1000),
                    favoriteSlotId = $"slot_{Random.Range(1, 6)}",
                    lastLoginTime = GetRandomDateTime(-30, 0),
                    registrationTime = GetRandomDateTime(-365, -30),
                    isVip = Random.value > 0.85f, // 15%概率为VIP
                    preferences = GeneratePlayerPreferences(),
                    achievements = GeneratePlayerAchievements(),
                    statistics = GeneratePlayerStatistics()
                };
                
                players.Add(player);
            }
            
            CacheData("players", players);
            OnDataGenerated?.Invoke("players");
        }
        
        /// <summary>
        /// 生成玩家偏好设置
        /// </summary>
        private PlayerPreferences GeneratePlayerPreferences()
        {
            return new PlayerPreferences
            {
                soundEnabled = Random.value > 0.2f,
                musicEnabled = Random.value > 0.3f,
                animationsEnabled = Random.value > 0.1f,
                autoSpin = Random.value > 0.6f,
                quickSpin = Random.value > 0.4f,
                language = GetRandomLanguage(),
                currency = GetRandomCurrency(),
                theme = GetRandomTheme()
            };
        }
        
        /// <summary>
        /// 生成玩家成就
        /// </summary>
        private List<Achievement> GeneratePlayerAchievements()
        {
            var achievements = new List<Achievement>();
            string[] achievementNames = {
                "首次胜利", "连胜专家", "高额投注者", "幸运之星", "坚持不懈",
                "探索者", "收藏家", "大赢家", "VIP玩家", "老手"
            };
            
            for (int i = 0; i < achievementNames.Length; i++)
            {
                if (Random.value > 0.5f) // 50%概率获得成就
                {
                    var achievement = new Achievement
                    {
                        id = $"achievement_{i + 1}",
                        name = achievementNames[i],
                        description = $"获得{achievementNames[i]}成就",
                        unlockedAt = GetRandomDateTime(-180, 0),
                        reward = Random.Range(100, 1000),
                        isUnlocked = true
                    };
                    
                    achievements.Add(achievement);
                }
            }
            
            return achievements;
        }
        
        /// <summary>
        /// 生成玩家统计数据
        /// </summary>
        private PlayerStatistics GeneratePlayerStatistics()
        {
            return new PlayerStatistics
            {
                biggestWin = Random.Range(100f, 50000f),
                longestWinStreak = Random.Range(1, 20),
                longestLoseStreak = Random.Range(1, 15),
                averageBet = Random.Range(1f, 100f),
                totalPlayTime = Random.Range(600, 86400 * 30), // 10分钟到30天
                favoriteSymbol = Random.Range(0, 8),
                luckyNumber = Random.Range(1, 101),
                winRate = Random.Range(0.1f, 0.4f),
                bonusRoundsTriggered = Random.Range(0, 100),
                jackpotsWon = Random.Range(0, 5)
            };
        }
        
        #endregion
        
        #region Game Session Data Generation
        
        /// <summary>
        /// 生成游戏会话数据
        /// </summary>
        private void GenerateGameSessionData()
        {
            LogInfo("生成游戏会话数据...");
            
            var sessions = new List<GameSession>();
            
            for (int i = 0; i < defaultDataCount * 2; i++) // 每个玩家平均2个会话
            {
                var session = new GameSession
                {
                    id = Guid.NewGuid().ToString(),
                    playerId = $"player_{Random.Range(1, defaultDataCount + 1):D4}",
                    slotMachineId = $"slot_{Random.Range(1, 6)}",
                    startTime = GetRandomDateTime(-7, 0),
                    endTime = DateTime.MinValue, // 待计算
                    totalSpins = Random.Range(10, 500),
                    totalWagered = 0f, // 待计算
                    totalWon = 0f, // 待计算
                    netResult = 0f, // 待计算
                    maxWin = 0f, // 待计算
                    bonusRoundsTriggered = Random.Range(0, 10),
                    freeSpinsEarned = Random.Range(0, 50),
                    currency = GetRandomCurrency(),
                    deviceType = GetRandomDeviceType(),
                    ipAddress = GenerateRandomIP(),
                    userAgent = GenerateRandomUserAgent()
                };
                
                // 计算会话持续时间和结果
                CalculateSessionDetails(session);
                
                sessions.Add(session);
            }
            
            CacheData("gameSessions", sessions);
            OnDataGenerated?.Invoke("gameSessions");
        }
        
        /// <summary>
        /// 计算会话详细信息
        /// </summary>
        private void CalculateSessionDetails(GameSession session)
        {
            // 计算会话持续时间（10分钟到4小时）
            int durationMinutes = Random.Range(10, 240);
            session.endTime = session.startTime.AddMinutes(durationMinutes);
            
            // 计算下注和获胜金额
            float averageBet = Random.Range(1f, 20f);
            session.totalWagered = session.totalSpins * averageBet;
            
            // 根据RTP计算获胜金额（添加一些随机性）
            float baseRtp = Random.Range(0.85f, 0.98f);
            float variance = Random.Range(-0.3f, 0.8f); // 增加方差
            session.totalWon = session.totalWagered * (baseRtp + variance);
            session.totalWon = Math.Max(0, session.totalWon); // 确保非负
            
            session.netResult = session.totalWon - session.totalWagered;
            session.maxWin = Random.Range(averageBet * 2, averageBet * 100);
        }
        
        #endregion
        
        #region Spin Results Generation
        
        /// <summary>
        /// 生成旋转结果数据
        /// </summary>
        private void GenerateSpinResults()
        {
            LogInfo("生成旋转结果数据...");
            
            var spinResults = new List<SpinResult>();
            var sessions = GetMockData<List<GameSession>>("gameSessions");
            
            if (sessions != null)
            {
                foreach (var session in sessions.Take(10)) // 只为前10个会话生成详细旋转数据
                {
                    for (int i = 0; i < session.totalSpins; i++)
                    {
                        var spinResult = new SpinResult
                        {
                            id = Guid.NewGuid().ToString(),
                            sessionId = session.id,
                            playerId = session.playerId,
                            slotMachineId = session.slotMachineId,
                            spinNumber = i + 1,
                            betAmount = Random.Range(1f, 20f),
                            winAmount = 0f, // 待计算
                            symbols = GenerateRandomSymbols(),
                            paylines = GenerateWinningPaylines(),
                            bonusTriggered = Random.value > 0.95f, // 5%概率触发奖励
                            freeSpinsAwarded = 0,
                            multiplier = 1f,
                            timestamp = session.startTime.AddMinutes(i * 0.5f), // 每30秒一次旋转
                            isJackpot = false
                        };
                        
                        // 计算获胜金额
                        CalculateSpinWin(spinResult);
                        
                        spinResults.Add(spinResult);
                    }
                }
            }
            
            CacheData("spinResults", spinResults);
            OnDataGenerated?.Invoke("spinResults");
        }
        
        /// <summary>
        /// 生成随机符号组合
        /// </summary>
        private int[,] GenerateRandomSymbols()
        {
            int[,] symbols = new int[5, 3]; // 5列3行
            
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 3; row++)
                {
                    symbols[col, row] = Random.Range(0, 8); // 8种符号
                }
            }
            
            return symbols;
        }
        
        /// <summary>
        /// 生成获胜连线
        /// </summary>
        private List<WinningPayline> GenerateWinningPaylines()
        {
            var paylines = new List<WinningPayline>();
            
            // 随机生成0-3条获胜连线
            int winningCount = Random.Range(0, 4);
            
            for (int i = 0; i < winningCount; i++)
            {
                var payline = new WinningPayline
                {
                    lineNumber = i + 1,
                    symbolId = Random.Range(0, 8),
                    symbolCount = Random.Range(3, 6),
                    multiplier = Random.Range(5, 100),
                    winAmount = Random.Range(10f, 500f)
                };
                
                paylines.Add(payline);
            }
            
            return paylines;
        }
        
        /// <summary>
        /// 计算旋转获胜金额
        /// </summary>
        private void CalculateSpinWin(SpinResult spinResult)
        {
            float totalWin = 0f;
            
            foreach (var payline in spinResult.paylines)
            {
                totalWin += payline.winAmount;
            }
            
            // 添加奖励和倍数
            if (spinResult.bonusTriggered)
            {
                spinResult.freeSpinsAwarded = Random.Range(5, 20);
                spinResult.multiplier = Random.Range(2f, 5f);
                totalWin *= spinResult.multiplier;
            }
            
            // 检查是否为头奖
            if (totalWin > spinResult.betAmount * 1000)
            {
                spinResult.isJackpot = true;
            }
            
            spinResult.winAmount = totalWin;
        }
        
        #endregion
        
        #region Bonus and Jackpot Data Generation
        
        /// <summary>
        /// 生成奖励数据
        /// </summary>
        private void GenerateBonusData()
        {
            LogInfo("生成奖励数据...");
            
            var bonusRounds = new List<BonusRound>();
            
            for (int i = 0; i < 50; i++)
            {
                var bonusRound = new BonusRound
                {
                    id = Guid.NewGuid().ToString(),
                    playerId = $"player_{Random.Range(1, defaultDataCount + 1):D4}",
                    slotMachineId = $"slot_{Random.Range(1, 6)}",
                    bonusType = GetRandomBonusType(),
                    triggerSymbols = GenerateRandomTriggerSymbols(),
                    baseWin = Random.Range(50f, 1000f),
                    multiplier = Random.Range(2f, 10f),
                    freeSpinsAwarded = Random.Range(5, 25),
                    totalWin = 0f, // 待计算
                    startTime = GetRandomDateTime(-7, 0),
                    duration = Random.Range(30, 300), // 30秒到5分钟
                    isCompleted = Random.value > 0.1f // 90%完成率
                };
                
                bonusRound.totalWin = bonusRound.baseWin * bonusRound.multiplier;
                bonusRounds.Add(bonusRound);
            }
            
            CacheData("bonusRounds", bonusRounds);
            OnDataGenerated?.Invoke("bonusRounds");
        }
        
        /// <summary>
        /// 生成头奖数据
        /// </summary>
        private void GenerateJackpotData()
        {
            LogInfo("生成头奖数据...");
            
            var jackpots = new List<JackpotWin>();
            
            for (int i = 0; i < 10; i++)
            {
                var jackpot = new JackpotWin
                {
                    id = Guid.NewGuid().ToString(),
                    playerId = $"player_{Random.Range(1, defaultDataCount + 1):D4}",
                    slotMachineId = $"slot_{Random.Range(1, 6)}",
                    jackpotType = GetRandomJackpotType(),
                    winAmount = GetRandomJackpotAmount(),
                    betAmount = Random.Range(5f, 100f),
                    symbols = GenerateJackpotSymbols(),
                    timestamp = GetRandomDateTime(-30, 0),
                    isPaid = Random.value > 0.05f, // 95%已支付
                    currency = GetRandomCurrency()
                };
                
                jackpots.Add(jackpot);
            }
            
            CacheData("jackpotWins", jackpots);
            OnDataGenerated?.Invoke("jackpotWins");
        }
        
        #endregion
        
        #region Statistics Data Generation
        
        /// <summary>
        /// 生成统计数据
        /// </summary>
        private void GenerateStatisticsData()
        {
            LogInfo("生成统计数据...");
            
            // 生成每日统计
            GenerateDailyStats();
            
            // 生成实时统计
            GenerateRealtimeStats();
            
            // 生成趋势数据
            GenerateTrendData();
        }
        
        /// <summary>
        /// 生成每日统计
        /// </summary>
        private void GenerateDailyStats()
        {
            var dailyStats = new List<DailyStatistics>();
            
            for (int i = 30; i >= 0; i--) // 过去30天
            {
                var date = DateTime.Now.Date.AddDays(-i);
                var stats = new DailyStatistics
                {
                    date = date,
                    totalPlayers = Random.Range(100, 1000),
                    newPlayers = Random.Range(10, 100),
                    totalSpins = Random.Range(1000, 10000),
                    totalWagered = Random.Range(10000f, 100000f),
                    totalWon = Random.Range(8000f, 120000f),
                    grossRevenue = 0f, // 待计算
                    averageSessionTime = Random.Range(600, 3600), // 10分钟到1小时
                    peakConcurrentPlayers = Random.Range(50, 500),
                    topSlotMachineId = $"slot_{Random.Range(1, 6)}",
                    biggestWin = Random.Range(1000f, 50000f)
                };
                
                stats.grossRevenue = stats.totalWagered - stats.totalWon;
                dailyStats.Add(stats);
            }
            
            CacheData("dailyStatistics", dailyStats);
            OnDataGenerated?.Invoke("dailyStatistics");
        }
        
        /// <summary>
        /// 生成实时统计
        /// </summary>
        private void GenerateRealtimeStats()
        {
            var realtimeStats = new RealtimeStatistics
            {
                currentOnlinePlayers = Random.Range(50, 300),
                spinsPerMinute = Random.Range(100, 1000),
                currentRevenue = Random.Range(1000f, 10000f),
                activeSlotMachines = Random.Range(3, 5),
                averageBetSize = Random.Range(5f, 50f),
                topWinToday = Random.Range(1000f, 25000f),
                systemStatus = GetRandomSystemStatus(),
                lastUpdated = DateTime.Now
            };
            
            CacheData("realtimeStatistics", realtimeStats);
            OnDataGenerated?.Invoke("realtimeStatistics");
        }
        
        /// <summary>
        /// 生成趋势数据
        /// </summary>
        private void GenerateTrendData()
        {
            var trends = new TrendData
            {
                playerGrowthRate = Random.Range(-5f, 15f), // -5% 到 +15%
                revenueGrowthRate = Random.Range(-10f, 20f),
                retentionRate = Random.Range(60f, 85f),
                averageLifetimeValue = Random.Range(100f, 1000f),
                popularTimeSlots = GeneratePopularTimeSlots(),
                topPerformingSlots = GenerateTopPerformingSlots(),
                regionalData = GenerateRegionalData()
            };
            
            CacheData("trendData", trends);
            OnDataGenerated?.Invoke("trendData");
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// 缓存数据
        /// </summary>
        private void CacheData(string key, object data)
        {
            cachedData[key] = data;
            LogInfo($"已缓存数据: {key}");
        }
        
        /// <summary>
        /// 获取随机老虎机类型
        /// </summary>
        private SlotMachineType GetRandomSlotType()
        {
            var types = Enum.GetValues(typeof(SlotMachineType));
            return (SlotMachineType)types.GetValue(Random.Range(0, types.Length));
        }
        
        /// <summary>
        /// 获取随机最小投注
        /// </summary>
        private float GetRandomMinBet()
        {
            float[] minBets = { 0.1f, 0.5f, 1f, 2f, 5f };
            return minBets[Random.Range(0, minBets.Length)];
        }
        
        /// <summary>
        /// 获取随机最大投注
        /// </summary>
        private float GetRandomMaxBet()
        {
            float[] maxBets = { 10f, 25f, 50f, 100f, 500f };
            return maxBets[Random.Range(0, maxBets.Length)];
        }
        
        /// <summary>
        /// 获取随机支付线数
        /// </summary>
        private int GetRandomPaylines()
        {
            int[] paylines = { 5, 10, 15, 20, 25, 30, 50 };
            return paylines[Random.Range(0, paylines.Length)];
        }
        
        /// <summary>
        /// 获取随机RTP
        /// </summary>
        private float GetRandomRTP()
        {
            return Random.Range(0.85f, 0.98f);
        }
        
        /// <summary>
        /// 获取随机波动性
        /// </summary>
        private SlotVolatility GetRandomVolatility()
        {
            var volatilities = Enum.GetValues(typeof(SlotVolatility));
            return (SlotVolatility)volatilities.GetValue(Random.Range(0, volatilities.Length));
        }
        
        /// <summary>
        /// 获取符号稀有度
        /// </summary>
        private SymbolRarity GetSymbolRarity(int index)
        {
            if (index < 3) return SymbolRarity.Common;
            if (index < 5) return SymbolRarity.Uncommon;
            if (index < 7) return SymbolRarity.Rare;
            return SymbolRarity.Legendary;
        }
        
        /// <summary>
        /// 生成随机用户名
        /// </summary>
        private string GenerateRandomUsername()
        {
            string[] prefixes = { "Lucky", "Spin", "Win", "Mega", "Super", "Golden", "Diamond", "Royal" };
            string[] suffixes = { "Player", "Gamer", "Winner", "Star", "Hero", "Master", "Pro", "Legend" };
            
            return $"{prefixes[Random.Range(0, prefixes.Length)]}{suffixes[Random.Range(0, suffixes.Length)]}{Random.Range(1, 10000)}";
        }
        
        /// <summary>
        /// 生成随机邮箱
        /// </summary>
        private string GenerateRandomEmail()
        {
            string[] domains = { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "qq.com" };
            string username = GenerateRandomUsername().ToLower();
            string domain = domains[Random.Range(0, domains.Length)];
            
            return $"{username}@{domain}";
        }
        
        /// <summary>
        /// 获取随机语言
        /// </summary>
        private string GetRandomLanguage()
        {
            string[] languages = { "zh", "en", "es", "fr", "de", "ja", "ko" };
            return languages[Random.Range(0, languages.Length)];
        }
        
        /// <summary>
        /// 获取随机货币
        /// </summary>
        private string GetRandomCurrency()
        {
            string[] currencies = { "USD", "EUR", "CNY", "JPY", "GBP", "AUD", "CAD" };
            return currencies[Random.Range(0, currencies.Length)];
        }
        
        /// <summary>
        /// 获取随机主题
        /// </summary>
        private string GetRandomTheme()
        {
            string[] themes = { "classic", "modern", "dark", "bright", "neon", "retro" };
            return themes[Random.Range(0, themes.Length)];
        }
        
        /// <summary>
        /// 获取随机设备类型
        /// </summary>
        private string GetRandomDeviceType()
        {
            string[] devices = { "Desktop", "Mobile", "Tablet" };
            return devices[Random.Range(0, devices.Length)];
        }
        
        /// <summary>
        /// 生成随机IP地址
        /// </summary>
        private string GenerateRandomIP()
        {
            return $"{Random.Range(1, 256)}.{Random.Range(0, 256)}.{Random.Range(0, 256)}.{Random.Range(1, 256)}";
        }
        
        /// <summary>
        /// 生成随机User Agent
        /// </summary>
        private string GenerateRandomUserAgent()
        {
            string[] userAgents = {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15",
                "Mozilla/5.0 (Android 11; Mobile; rv:68.0) Gecko/68.0 Firefox/88.0"
            };
            
            return userAgents[Random.Range(0, userAgents.Length)];
        }
        
        /// <summary>
        /// 获取随机日期时间
        /// </summary>
        private DateTime GetRandomDateTime(int minDaysAgo, int maxDaysAgo)
        {
            int daysAgo = Random.Range(minDaysAgo, maxDaysAgo + 1);
            return DateTime.Now.AddDays(daysAgo).AddHours(Random.Range(-12, 12)).AddMinutes(Random.Range(-30, 30));
        }
        
        /// <summary>
        /// 获取随机奖励类型
        /// </summary>
        private BonusType GetRandomBonusType()
        {
            var types = Enum.GetValues(typeof(BonusType));
            return (BonusType)types.GetValue(Random.Range(0, types.Length));
        }
        
        /// <summary>
        /// 生成随机触发符号
        /// </summary>
        private List<int> GenerateRandomTriggerSymbols()
        {
            var symbols = new List<int>();
            int count = Random.Range(3, 6);
            
            for (int i = 0; i < count; i++)
            {
                symbols.Add(Random.Range(0, 8));
            }
            
            return symbols;
        }
        
        /// <summary>
        /// 获取随机头奖类型
        /// </summary>
        private JackpotType GetRandomJackpotType()
        {
            var types = Enum.GetValues(typeof(JackpotType));
            return (JackpotType)types.GetValue(Random.Range(0, types.Length));
        }
        
        /// <summary>
        /// 获取随机头奖金额
        /// </summary>
        private float GetRandomJackpotAmount()
        {
            return Random.Range(10000f, 1000000f);
        }
        
        /// <summary>
        /// 生成头奖符号组合
        /// </summary>
        private int[] GenerateJackpotSymbols()
        {
            int[] symbols = new int[5];
            int jackpotSymbol = 7; // 假设7是头奖符号
            
            for (int i = 0; i < 5; i++)
            {
                symbols[i] = jackpotSymbol;
            }
            
            return symbols;
        }
        
        /// <summary>
        /// 获取随机系统状态
        /// </summary>
        private SystemStatus GetRandomSystemStatus()
        {
            var statuses = Enum.GetValues(typeof(SystemStatus));
            return (SystemStatus)statuses.GetValue(Random.Range(0, statuses.Length));
        }
        
        /// <summary>
        /// 生成热门时间段
        /// </summary>
        private List<TimeSlotData> GeneratePopularTimeSlots()
        {
            var timeSlots = new List<TimeSlotData>();
            
            for (int hour = 0; hour < 24; hour++)
            {
                var timeSlot = new TimeSlotData
                {
                    hour = hour,
                    playerCount = Random.Range(10, 200),
                    averageBet = Random.Range(5f, 50f),
                    totalRevenue = Random.Range(1000f, 10000f)
                };
                
                timeSlots.Add(timeSlot);
            }
            
            return timeSlots;
        }
        
        /// <summary>
        /// 生成顶级表现老虎机
        /// </summary>
        private List<SlotPerformanceData> GenerateTopPerformingSlots()
        {
            var performance = new List<SlotPerformanceData>();
            
            for (int i = 1; i <= 5; i++)
            {
                var data = new SlotPerformanceData
                {
                    slotId = $"slot_{i}",
                    totalSpins = Random.Range(1000, 10000),
                    totalRevenue = Random.Range(10000f, 100000f),
                    averagePlayTime = Random.Range(300, 1800),
                    popularityScore = Random.Range(0.6f, 1f)
                };
                
                performance.Add(data);
            }
            
            return performance.OrderByDescending(p => p.popularityScore).ToList();
        }
        
        /// <summary>
        /// 生成区域数据
        /// </summary>
        private List<RegionalData> GenerateRegionalData()
        {
            var regions = new List<RegionalData>();
            string[] regionNames = { "North America", "Europe", "Asia", "South America", "Oceania" };
            
            foreach (string region in regionNames)
            {
                var data = new RegionalData
                {
                    region = region,
                    playerCount = Random.Range(100, 5000),
                    revenue = Random.Range(10000f, 500000f),
                    averageBet = Random.Range(5f, 100f),
                    topCurrency = GetRandomCurrency()
                };
                
                regions.Add(data);
            }
            
            return regions;
        }
        
        /// <summary>
        /// 记录信息日志
        /// </summary>
        private void LogInfo(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[MockDataGenerator] {message}");
            }
        }
        
        /// <summary>
        /// 记录错误日志
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[MockDataGenerator] {message}");
        }
        
        #endregion
        
        #region Configuration Classes
        
        [Serializable]
        public class SlotMachineConfig
        {
            public int defaultSlotCount = 5;
            public float minRTP = 0.85f;
            public float maxRTP = 0.98f;
            public int[] availablePaylines = { 5, 10, 15, 20, 25, 30, 50 };
        }
        
        [Serializable]
        public class PlayerConfig
        {
            public int defaultPlayerCount = 100;
            public float minBalance = 10f;
            public float maxBalance = 10000f;
            public int minLevel = 1;
            public int maxLevel = 50;
        }
        
        [Serializable]
        public class SessionConfig
        {
            public int sessionsPerPlayer = 2;
            public int minSpinsPerSession = 10;
            public int maxSpinsPerSession = 500;
            public int minSessionDuration = 10; // 分钟
            public int maxSessionDuration = 240; // 分钟
        }
        
        #endregion
        
        #region Menu Items
        
#if UNITY_EDITOR
        [UnityEditor.MenuItem("SlotMachine/Testing/Generate Mock Data")]
        public static void GenerateMockDataFromMenu()
        {
            var generator = FindObjectOfType<MockDataGenerator>();
            if (generator == null)
            {
                var go = new GameObject("MockDataGenerator");
                generator = go.AddComponent<MockDataGenerator>();
            }
            
            generator.GenerateAllMockData();
            UnityEditor.EditorUtility.DisplayDialog("Mock数据生成", "Mock数据生成完成！", "确定");
        }
        
        [UnityEditor.MenuItem("SlotMachine/Testing/Clear Mock Data")]
        public static void ClearMockDataFromMenu()
        {
            ClearAllMockData();
            UnityEditor.EditorUtility.DisplayDialog("清除Mock数据", "所有Mock数据已清除！", "确定");
        }
#endif
        
        #endregion
    }
}