// Assets/Game/Logic/CardSystem.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Core.Data.Types;

namespace Game.Logic
{
    /// <summary>
    /// 卡牌系统类
    /// 负责管理牌组、发牌、洗牌等所有与牌面相关的逻辑
    /// </summary>
    public class CardSystem : MonoBehaviour
    {
        [Header("牌组配置")]
        [SerializeField] private int deckCount = 8;  // 牌靴数量（通常8副牌）
        [SerializeField] private bool enableShuffle = true;  // 是否启用洗牌
        [SerializeField] private float shuffleThreshold = 0.2f;  // 洗牌阈值（剩余20%时洗牌）
        
        [Header("发牌配置")]
        [SerializeField] private float dealCardDelay = 0.5f;  // 发牌间隔
        [SerializeField] private bool enableDealAnimation = true;  // 启用发牌动画
        [SerializeField] private bool enableCardReveal = true;  // 启用翻牌效果

        [Header("调试信息")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private bool showRemainingCards = true;

        #region 私有字段

        // 牌组管理
        private List<BaccaratCard> _currentDeck = new List<BaccaratCard>();
        private List<BaccaratCard> _usedCards = new List<BaccaratCard>();
        private int _currentCardIndex = 0;
        
        // 发牌历史
        private List<DealHistory> _dealHistory = new List<DealHistory>();
        
        // 随机数生成器
        private System.Random _randomGenerator;
        
        // 牌组统计
        private CardStatistics _statistics = new CardStatistics();

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前牌组剩余牌数
        /// </summary>
        public int RemainingCards => _currentDeck.Count - _currentCardIndex;
        
        /// <summary>
        /// 总牌数
        /// </summary>
        public int TotalCards => deckCount * 52;
        
        /// <summary>
        /// 已使用牌数
        /// </summary>
        public int UsedCardsCount => _usedCards.Count;
        
        /// <summary>
        /// 剩余牌数百分比
        /// </summary>
        public float RemainingPercentage => RemainingCards / (float)TotalCards;
        
        /// <summary>
        /// 是否需要洗牌
        /// </summary>
        public bool NeedsShuffle => RemainingPercentage < shuffleThreshold;
        
        /// <summary>
        /// 牌组统计信息
        /// </summary>
        public CardStatistics Statistics => _statistics;

        #endregion

        #region 事件

        public event Action<BaccaratCard> OnCardDealt;
        public event Action<List<BaccaratCard>> OnDeckShuffled;
        public event Action<string> OnShuffleRequired;
        public event Action<CardStatistics> OnStatisticsUpdated;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            InitializeRandomGenerator();
        }

        private void Start()
        {
            InitializeDeck();
        }

        private void Update()
        {
            if (showRemainingCards && enableDebugLog)
            {
                // 定期显示剩余牌数（每秒一次）
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"剩余牌数: {RemainingCards}/{TotalCards} ({RemainingPercentage:P1})");
                }
            }
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化随机数生成器
        /// </summary>
        private void InitializeRandomGenerator()
        {
            // 使用时间戳作为随机种子，确保每次运行结果不同
            int seed = System.DateTime.Now.Millisecond + UnityEngine.Random.Range(1, 10000);
            _randomGenerator = new System.Random(seed);
            
            if (enableDebugLog)
            {
                Debug.Log($"卡牌系统初始化，随机种子: {seed}");
            }
        }

        /// <summary>
        /// 初始化牌组
        /// </summary>
        public void InitializeDeck()
        {
            _currentDeck.Clear();
            _usedCards.Clear();
            _currentCardIndex = 0;

            // 生成指定数量的标准52张牌
            for (int deck = 0; deck < deckCount; deck++)
            {
                for (int suit = 1; suit <= 4; suit++)  // 4种花色
                {
                    for (int rank = 1; rank <= 13; rank++)  // 13个点数
                    {
                        var card = CreateCard(suit, rank);
                        _currentDeck.Add(card);
                    }
                }
            }

            // 洗牌
            if (enableShuffle)
            {
                ShuffleDeck();
            }

            // 重置统计
            _statistics.Reset();
            UpdateStatistics();

            if (enableDebugLog)
            {
                Debug.Log($"牌组初始化完成: {deckCount}副牌，共{_currentDeck.Count}张");
            }
        }

        /// <summary>
        /// 创建单张牌
        /// </summary>
        /// <param name="suit">花色</param>
        /// <param name="rank">点数</param>
        /// <returns>百家乐卡牌</returns>
        private BaccaratCard CreateCard(int suit, int rank)
        {
            var card = new BaccaratCard
            {
                suit = suit,
                rank = rank,
                display_name = GetCardDisplayName(suit, rank),
                baccarat_value = GetBaccaratValue(rank),
                image_url = GenerateCardImageUrl(suit, rank),
                back_image_url = "assets/cards/back.png",
                is_revealed = false
            };

            return card;
        }

        #endregion

        #region 核心发牌方法

        /// <summary>
        /// 发一张牌
        /// </summary>
        /// <param name="isRevealed">是否立即翻开</param>
        /// <returns>发出的牌</returns>
        public BaccaratCard DealCard(bool isRevealed = true)
        {
            // 检查是否需要洗牌
            if (NeedsShuffle)
            {
                OnShuffleRequired?.Invoke($"剩余牌数不足{shuffleThreshold:P0}，建议洗牌");
                
                if (enableShuffle)
                {
                    ReshuffleDeck();
                }
            }

            // 检查是否还有牌
            if (_currentCardIndex >= _currentDeck.Count)
            {
                Debug.LogError("牌组已用完，无法继续发牌！");
                return null;
            }

            // 发牌
            var card = _currentDeck[_currentCardIndex];
            card.is_revealed = isRevealed;
            _currentCardIndex++;

            // 记录已使用的牌
            _usedCards.Add(card);

            // 记录发牌历史
            RecordDealHistory(card);

            // 更新统计
            UpdateStatistics();

            // 触发事件
            OnCardDealt?.Invoke(card);

            if (enableDebugLog)
            {
                Debug.Log($"发牌: {card.display_name} (剩余: {RemainingCards})");
            }

            return card;
        }

        /// <summary>
        /// 发多张牌
        /// </summary>
        /// <param name="count">牌数</param>
        /// <param name="isRevealed">是否立即翻开</param>
        /// <returns>发出的牌列表</returns>
        public List<BaccaratCard> DealCards(int count, bool isRevealed = true)
        {
            var cards = new List<BaccaratCard>();

            for (int i = 0; i < count; i++)
            {
                var card = DealCard(isRevealed);
                if (card != null)
                {
                    cards.Add(card);
                }
                else
                {
                    Debug.LogWarning($"只成功发出{i}张牌，要求{count}张");
                    break;
                }
            }

            return cards;
        }

        /// <summary>
        /// 发百家乐初始牌（庄家和闲家各2张）
        /// </summary>
        /// <returns>发牌结果</returns>
        public BaccaratDealResult DealInitialCards()
        {
            var result = new BaccaratDealResult
            {
                banker_cards = new List<BaccaratCard>(),
                player_cards = new List<BaccaratCard>(),
                deal_time = DateTime.UtcNow
            };

            try
            {
                // 按百家乐规则发牌：闲家第一张、庄家第一张、闲家第二张、庄家第二张
                result.player_cards.Add(DealCard(true));   // 闲家第一张
                result.banker_cards.Add(DealCard(true));   // 庄家第一张
                result.player_cards.Add(DealCard(true));   // 闲家第二张
                result.banker_cards.Add(DealCard(true));   // 庄家第二张

                // 计算初始点数
                result.player_initial_points = BaccaratLogic.CalculatePoints(result.player_cards);
                result.banker_initial_points = BaccaratLogic.CalculatePoints(result.banker_cards);

                // 检查是否为天牌
                result.is_player_natural = BaccaratLogic.IsNatural(result.player_cards);
                result.is_banker_natural = BaccaratLogic.IsNatural(result.banker_cards);

                result.success = true;
                result.message = "初始发牌成功";

                if (enableDebugLog)
                {
                    Debug.Log($"初始发牌完成 - 庄家: {result.banker_initial_points}点, 闲家: {result.player_initial_points}点");
                }
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = $"发牌失败: {ex.Message}";
                Debug.LogError($"初始发牌失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 根据规则补牌
        /// </summary>
        /// <param name="bankerCards">庄家牌</param>
        /// <param name="playerCards">闲家牌</param>
        /// <returns>补牌结果</returns>
        public DrawCardResult DrawAdditionalCards(List<BaccaratCard> bankerCards, List<BaccaratCard> playerCards)
        {
            var result = new DrawCardResult
            {
                banker_cards = new List<BaccaratCard>(bankerCards),
                player_cards = new List<BaccaratCard>(playerCards),
                cards_drawn = new List<BaccaratCard>(),
                draw_time = DateTime.UtcNow
            };

            try
            {
                // 检查补牌规则
                var drawInfo = BaccaratLogic.CheckDrawCard(bankerCards, playerCards);

                if (!drawInfo.shouldDraw)
                {
                    result.success = true;
                    result.message = drawInfo.reason;
                    return result;
                }

                // 闲家补牌
                if (drawInfo.playerShouldDraw)
                {
                    var playerThirdCard = DealCard(true);
                    result.player_cards.Add(playerThirdCard);
                    result.cards_drawn.Add(playerThirdCard);
                    result.player_drew_card = true;

                    if (enableDebugLog)
                    {
                        Debug.Log($"闲家补牌: {playerThirdCard.display_name}");
                    }
                }

                // 庄家补牌
                if (drawInfo.bankerShouldDraw)
                {
                    var bankerThirdCard = DealCard(true);
                    result.banker_cards.Add(bankerThirdCard);
                    result.cards_drawn.Add(bankerThirdCard);
                    result.banker_drew_card = true;

                    if (enableDebugLog)
                    {
                        Debug.Log($"庄家补牌: {bankerThirdCard.display_name}");
                    }
                }

                // 计算最终点数
                result.final_banker_points = BaccaratLogic.CalculatePoints(result.banker_cards);
                result.final_player_points = BaccaratLogic.CalculatePoints(result.player_cards);

                result.success = true;
                result.message = $"补牌完成 - {drawInfo.reason}";
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = $"补牌失败: {ex.Message}";
                Debug.LogError($"补牌失败: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region 洗牌方法

        /// <summary>
        /// 洗牌
        /// </summary>
        public void ShuffleDeck()
        {
            if (_currentDeck.Count == 0)
            {
                Debug.LogWarning("牌组为空，无法洗牌");
                return;
            }

            // Fisher-Yates洗牌算法
            for (int i = _currentDeck.Count - 1; i > 0; i--)
            {
                int randomIndex = _randomGenerator.Next(0, i + 1);
                var temp = _currentDeck[i];
                _currentDeck[i] = _currentDeck[randomIndex];
                _currentDeck[randomIndex] = temp;
            }

            // 触发洗牌事件
            OnDeckShuffled?.Invoke(new List<BaccaratCard>(_currentDeck));

            if (enableDebugLog)
            {
                Debug.Log($"洗牌完成，牌组重新排列");
            }
        }

        /// <summary>
        /// 重新洗牌（包含已使用的牌）
        /// </summary>
        public void ReshuffleDeck()
        {
            // 将已使用的牌放回牌组
            _currentDeck.AddRange(_usedCards);
            _usedCards.Clear();
            _currentCardIndex = 0;

            // 重置所有牌的翻开状态
            foreach (var card in _currentDeck)
            {
                card.is_revealed = false;
            }

            // 洗牌
            ShuffleDeck();

            // 重置统计
            _statistics.Reset();
            UpdateStatistics();

            if (enableDebugLog)
            {
                Debug.Log($"重新洗牌完成，牌组已重置");
            }
        }

        /// <summary>
        /// 切牌（模拟真实赌场的切牌操作）
        /// </summary>
        /// <param name="cutPosition">切牌位置（百分比）</param>
        public void CutDeck(float cutPosition = 0.5f)
        {
            if (_currentDeck.Count == 0) return;

            cutPosition = Mathf.Clamp01(cutPosition);
            int cutIndex = Mathf.RoundToInt(_currentDeck.Count * cutPosition);

            var topHalf = _currentDeck.GetRange(0, cutIndex);
            var bottomHalf = _currentDeck.GetRange(cutIndex, _currentDeck.Count - cutIndex);

            _currentDeck.Clear();
            _currentDeck.AddRange(bottomHalf);
            _currentDeck.AddRange(topHalf);

            if (enableDebugLog)
            {
                Debug.Log($"切牌完成，位置: {cutPosition:P1} (第{cutIndex}张)");
            }
        }

        #endregion

        #region 牌面分析方法

        /// <summary>
        /// 分析剩余牌组成
        /// </summary>
        /// <returns>牌组分析结果</returns>
        public DeckAnalysis AnalyzeDeck()
        {
            var analysis = new DeckAnalysis();
            var remainingCards = _currentDeck.Skip(_currentCardIndex).ToList();

            // 按花色分类
            analysis.spades = remainingCards.Count(c => c.suit == 1);
            analysis.hearts = remainingCards.Count(c => c.suit == 2);
            analysis.clubs = remainingCards.Count(c => c.suit == 3);
            analysis.diamonds = remainingCards.Count(c => c.suit == 4);

            // 按点数分类
            analysis.aces = remainingCards.Count(c => c.rank == 1);
            analysis.tens = remainingCards.Count(c => c.rank == 10);
            analysis.faces = remainingCards.Count(c => c.rank >= 11);

            // 按百家乐点数分类
            for (int i = 0; i <= 9; i++)
            {
                analysis.baccaratValues[i] = remainingCards.Count(c => GetBaccaratValue(c.rank) == i);
            }

            // 计算期望值
            analysis.averageBaccaratValue = remainingCards.Average(c => GetBaccaratValue(c.rank));

            return analysis;
        }

        /// <summary>
        /// 获取特定牌的剩余数量
        /// </summary>
        /// <param name="suit">花色</param>
        /// <param name="rank">点数</param>
        /// <returns>剩余数量</returns>
        public int GetRemainingCardCount(int suit, int rank)
        {
            return _currentDeck.Skip(_currentCardIndex).Count(c => c.suit == suit && c.rank == rank);
        }

        /// <summary>
        /// 获取特定百家乐点数的剩余牌数
        /// </summary>
        /// <param name="baccaratValue">百家乐点数</param>
        /// <returns>剩余数量</returns>
        public int GetRemainingBaccaratValueCount(int baccaratValue)
        {
            return _currentDeck.Skip(_currentCardIndex).Count(c => GetBaccaratValue(c.rank) == baccaratValue);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取牌面显示名称
        /// </summary>
        private string GetCardDisplayName(int suit, int rank)
        {
            string suitChar = suit switch
            {
                1 => "♠",
                2 => "♥",
                3 => "♣",
                4 => "♦",
                _ => "?"
            };

            string rankChar = rank switch
            {
                1 => "A",
                11 => "J",
                12 => "Q",
                13 => "K",
                _ => rank.ToString()
            };

            return suitChar + rankChar;
        }

        /// <summary>
        /// 获取百家乐点数
        /// </summary>
        private int GetBaccaratValue(int rank)
        {
            if (rank == 1) return 1;       // A = 1
            if (rank >= 10) return 0;      // 10, J, Q, K = 0
            return rank;                   // 2-9 = 面值
        }

        /// <summary>
        /// 生成牌面图片URL
        /// </summary>
        private string GenerateCardImageUrl(int suit, int rank)
        {
            string suitName = suit switch
            {
                1 => "spades",
                2 => "hearts",
                3 => "clubs",
                4 => "diamonds",
                _ => "unknown"
            };

            string rankName = rank switch
            {
                1 => "ace",
                11 => "jack",
                12 => "queen",
                13 => "king",
                _ => rank.ToString()
            };

            return $"assets/cards/{suitName}_{rankName}.png";
        }

        /// <summary>
        /// 记录发牌历史
        /// </summary>
        private void RecordDealHistory(BaccaratCard card)
        {
            var history = new DealHistory
            {
                card = card,
                deal_time = DateTime.UtcNow,
                remaining_cards = RemainingCards
            };

            _dealHistory.Add(history);

            // 限制历史记录数量
            if (_dealHistory.Count > 1000)
            {
                _dealHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            _statistics.total_cards_dealt = _usedCards.Count;
            _statistics.remaining_cards = RemainingCards;
            _statistics.shuffle_count = _statistics.shuffle_count; // 保持现有值
            _statistics.last_update_time = DateTime.UtcNow;

            // 计算各种统计
            if (_usedCards.Count > 0)
            {
                _statistics.average_baccarat_value = _usedCards.Average(c => c.baccarat_value);
                
                for (int i = 0; i <= 9; i++)
                {
                    _statistics.baccarat_value_distribution[i] = _usedCards.Count(c => c.baccarat_value == i);
                }
            }

            OnStatisticsUpdated?.Invoke(_statistics);
        }

        #endregion

        #region 公共查询方法

        /// <summary>
        /// 获取发牌历史
        /// </summary>
        /// <param name="count">获取数量</param>
        /// <returns>发牌历史列表</returns>
        public List<DealHistory> GetDealHistory(int count = 50)
        {
            return _dealHistory.TakeLast(count).ToList();
        }

        /// <summary>
        /// 检查牌组完整性
        /// </summary>
        /// <returns>完整性检查结果</returns>
        public DeckIntegrityResult CheckDeckIntegrity()
        {
            var result = new DeckIntegrityResult { IsValid = true };

            // 检查总牌数
            int totalCards = _currentDeck.Count + _usedCards.Count;
            int expectedTotal = deckCount * 52;
            
            if (totalCards != expectedTotal)
            {
                result.IsValid = false;
                result.Errors.Add($"总牌数错误: 期望{expectedTotal}，实际{totalCards}");
            }

            // 检查牌面重复
            var allCards = new List<BaccaratCard>();
            allCards.AddRange(_currentDeck);
            allCards.AddRange(_usedCards);

            var cardGroups = allCards.GroupBy(c => new { c.suit, c.rank });
            foreach (var group in cardGroups)
            {
                if (group.Count() != deckCount)
                {
                    result.IsValid = false;
                    result.Errors.Add($"牌面{group.Key}数量错误: 期望{deckCount}，实际{group.Count()}");
                }
            }

            return result;
        }

        /// <summary>
        /// 重置卡牌系统
        /// </summary>
        public void ResetCardSystem()
        {
            _dealHistory.Clear();
            _statistics.Reset();
            InitializeDeck();
            
            if (enableDebugLog)
            {
                Debug.Log("卡牌系统已重置");
            }
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 百家乐发牌结果
    /// </summary>
    [System.Serializable]
    public class BaccaratDealResult
    {
        public List<BaccaratCard> banker_cards;
        public List<BaccaratCard> player_cards;
        public int banker_initial_points;
        public int player_initial_points;
        public bool is_banker_natural;
        public bool is_player_natural;
        public bool success;
        public string message;
        public DateTime deal_time;
    }

    /// <summary>
    /// 补牌结果
    /// </summary>
    [System.Serializable]
    public class DrawCardResult
    {
        public List<BaccaratCard> banker_cards;
        public List<BaccaratCard> player_cards;
        public List<BaccaratCard> cards_drawn;
        public bool banker_drew_card;
        public bool player_drew_card;
        public int final_banker_points;
        public int final_player_points;
        public bool success;
        public string message;
        public DateTime draw_time;
    }

    /// <summary>
    /// 牌组分析结果
    /// </summary>
    [System.Serializable]
    public class DeckAnalysis
    {
        [Header("花色分布")]
        public int spades;      // 黑桃
        public int hearts;      // 红桃
        public int clubs;       // 梅花
        public int diamonds;    // 方块

        [Header("特殊牌面")]
        public int aces;        // A
        public int tens;        // 10
        public int faces;       // J, Q, K

        [Header("百家乐点数分布")]
        public int[] baccaratValues = new int[10];  // 0-9点的牌数

        [Header("统计信息")]
        public float averageBaccaratValue;
    }

    /// <summary>
    /// 发牌历史
    /// </summary>
    [System.Serializable]
    public class DealHistory
    {
        public BaccaratCard card;
        public DateTime deal_time;
        public int remaining_cards;
    }

    /// <summary>
    /// 牌组统计
    /// </summary>
    [System.Serializable]
    public class CardStatistics
    {
        [Header("基础统计")]
        public int total_cards_dealt;
        public int remaining_cards;
        public int shuffle_count;
        public DateTime last_update_time;

        [Header("点数统计")]
        public float average_baccarat_value;
        public int[] baccarat_value_distribution = new int[10];

        public void Reset()
        {
            total_cards_dealt = 0;
            remaining_cards = 0;
            shuffle_count = 0;
            average_baccarat_value = 0f;
            baccarat_value_distribution = new int[10];
            last_update_time = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 牌组完整性检查结果
    /// </summary>
    [System.Serializable]
    public class DeckIntegrityResult
    {
        public bool IsValid = true;
        public List<string> Errors = new List<string>();
    }

    #endregion
}