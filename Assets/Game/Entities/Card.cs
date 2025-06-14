// Assets/Game/Entities/Card.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Data.Types;

namespace Game.Entities
{
    /// <summary>
    /// 卡牌实体类
    /// 表示百家乐游戏中的单张牌，包含牌面信息、动画状态、UI交互等功能
    /// </summary>
    [System.Serializable]
    public class Card : IEquatable<Card>, IComparable<Card>
    {
        [Header("牌面信息")]
        [SerializeField] private CardSuit suit = CardSuit.Spades;
        [SerializeField] private CardRank rank = CardRank.Ace;
        [SerializeField] private bool isRevealed = false;
        [SerializeField] private bool isVisible = true;

        [Header("显示信息")]
        [SerializeField] private string displayName = "";
        [SerializeField] private string imagePath = "";
        [SerializeField] private string backImagePath = "";
        [SerializeField] private Color cardColor = Color.white;

        [Header("游戏状态")]
        [SerializeField] private CardPosition position = CardPosition.None;
        [SerializeField] private int dealOrder = 0;
        [SerializeField] private DateTime dealTime;
        [SerializeField] private string gameNumber = "";

        [Header("动画状态")]
        [SerializeField] private bool isAnimating = false;
        [SerializeField] private CardAnimation currentAnimation = CardAnimation.None;
        [SerializeField] private float animationProgress = 0f;

        [Header("特殊效果")]
        [SerializeField] private bool isHighlighted = false;
        [SerializeField] private bool isGlowing = false;
        [SerializeField] private CardEffect activeEffect = CardEffect.None;

        #region 静态常量

        /// <summary>
        /// 标准花色颜色
        /// </summary>
        public static readonly Dictionary<CardSuit, Color> SuitColors = new Dictionary<CardSuit, Color>
        {
            { CardSuit.Spades, Color.black },      // 黑桃 - 黑色
            { CardSuit.Hearts, Color.red },        // 红桃 - 红色
            { CardSuit.Clubs, Color.black },       // 梅花 - 黑色
            { CardSuit.Diamonds, Color.red }       // 方块 - 红色
        };

        /// <summary>
        /// 花色符号
        /// </summary>
        public static readonly Dictionary<CardSuit, string> SuitSymbols = new Dictionary<CardSuit, string>
        {
            { CardSuit.Spades, "♠" },
            { CardSuit.Hearts, "♥" },
            { CardSuit.Clubs, "♣" },
            { CardSuit.Diamonds, "♦" }
        };

        /// <summary>
        /// 点数符号
        /// </summary>
        public static readonly Dictionary<CardRank, string> RankSymbols = new Dictionary<CardRank, string>
        {
            { CardRank.Ace, "A" },
            { CardRank.Two, "2" },
            { CardRank.Three, "3" },
            { CardRank.Four, "4" },
            { CardRank.Five, "5" },
            { CardRank.Six, "6" },
            { CardRank.Seven, "7" },
            { CardRank.Eight, "8" },
            { CardRank.Nine, "9" },
            { CardRank.Ten, "10" },
            { CardRank.Jack, "J" },
            { CardRank.Queen, "Q" },
            { CardRank.King, "K" }
        };

        #endregion

        #region 属性访问器

        /// <summary>
        /// 花色
        /// </summary>
        public CardSuit Suit 
        { 
            get => suit; 
            set 
            { 
                suit = value; 
                UpdateDisplayInfo();
            } 
        }

        /// <summary>
        /// 点数
        /// </summary>
        public CardRank Rank 
        { 
            get => rank; 
            set 
            { 
                rank = value; 
                UpdateDisplayInfo();
            } 
        }

        /// <summary>
        /// 是否已翻开
        /// </summary>
        public bool IsRevealed 
        { 
            get => isRevealed; 
            set => isRevealed = value; 
        }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible 
        { 
            get => isVisible; 
            set => isVisible = value; 
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// 图片路径
        /// </summary>
        public string ImagePath => imagePath;

        /// <summary>
        /// 背面图片路径
        /// </summary>
        public string BackImagePath => backImagePath;

        /// <summary>
        /// 牌面颜色
        /// </summary>
        public Color CardColor => cardColor;

        /// <summary>
        /// 位置
        /// </summary>
        public CardPosition Position 
        { 
            get => position; 
            set => position = value; 
        }

        /// <summary>
        /// 发牌顺序
        /// </summary>
        public int DealOrder 
        { 
            get => dealOrder; 
            set => dealOrder = value; 
        }

        /// <summary>
        /// 发牌时间
        /// </summary>
        public DateTime DealTime 
        { 
            get => dealTime; 
            set => dealTime = value; 
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
        /// 是否正在动画
        /// </summary>
        public bool IsAnimating => isAnimating;

        /// <summary>
        /// 当前动画
        /// </summary>
        public CardAnimation CurrentAnimation => currentAnimation;

        /// <summary>
        /// 动画进度
        /// </summary>
        public float AnimationProgress => animationProgress;

        /// <summary>
        /// 是否高亮
        /// </summary>
        public bool IsHighlighted 
        { 
            get => isHighlighted; 
            set => isHighlighted = value; 
        }

        /// <summary>
        /// 是否发光
        /// </summary>
        public bool IsGlowing 
        { 
            get => isGlowing; 
            set => isGlowing = value; 
        }

        /// <summary>
        /// 活跃效果
        /// </summary>
        public CardEffect ActiveEffect 
        { 
            get => activeEffect; 
            set => activeEffect = value; 
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Card()
        {
            InitializeDefaults();
        }

        /// <summary>
        /// 指定花色和点数的构造函数
        /// </summary>
        /// <param name="suit">花色</param>
        /// <param name="rank">点数</param>
        public Card(CardSuit suit, CardRank rank)
        {
            this.suit = suit;
            this.rank = rank;
            InitializeDefaults();
        }

        /// <summary>
        /// 从BaccaratCard创建Card
        /// </summary>
        /// <param name="baccaratCard">百家乐卡牌</param>
        /// <returns>卡牌实例</returns>
        public static Card FromBaccaratCard(BaccaratCard baccaratCard)
        {
            if (baccaratCard == null) return new Card();

            var card = new Card
            {
                suit = (CardSuit)baccaratCard.suit,
                rank = (CardRank)baccaratCard.rank,
                isRevealed = baccaratCard.is_revealed,
                imagePath = baccaratCard.image_url ?? "",
                backImagePath = baccaratCard.back_image_url ?? ""
            };

            card.UpdateDisplayInfo();
            return card;
        }

        /// <summary>
        /// 从整数值创建Card
        /// </summary>
        /// <param name="suitValue">花色值(1-4)</param>
        /// <param name="rankValue">点数值(1-13)</param>
        /// <returns>卡牌实例</returns>
        public static Card FromValues(int suitValue, int rankValue)
        {
            var suit = (CardSuit)Mathf.Clamp(suitValue, 1, 4);
            var rank = (CardRank)Mathf.Clamp(rankValue, 1, 13);
            return new Card(suit, rank);
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化默认值
        /// </summary>
        private void InitializeDefaults()
        {
            isRevealed = false;
            isVisible = true;
            position = CardPosition.None;
            dealOrder = 0;
            dealTime = DateTime.UtcNow;
            gameNumber = "";
            isAnimating = false;
            currentAnimation = CardAnimation.None;
            animationProgress = 0f;
            isHighlighted = false;
            isGlowing = false;
            activeEffect = CardEffect.None;
            
            UpdateDisplayInfo();
        }

        /// <summary>
        /// 更新显示信息
        /// </summary>
        private void UpdateDisplayInfo()
        {
            displayName = GetCardDisplayName();
            cardColor = GetCardColor();
            imagePath = GenerateImagePath();
            backImagePath = "assets/cards/back.png";
        }

        #endregion

        #region 核心计算方法

        /// <summary>
        /// 获取百家乐点数
        /// </summary>
        /// <returns>百家乐点数(0-9)</returns>
        public int GetBaccaratValue()
        {
            return rank switch
            {
                CardRank.Ace => 1,
                CardRank.Two => 2,
                CardRank.Three => 3,
                CardRank.Four => 4,
                CardRank.Five => 5,
                CardRank.Six => 6,
                CardRank.Seven => 7,
                CardRank.Eight => 8,
                CardRank.Nine => 9,
                _ => 0 // 10, J, Q, K = 0
            };
        }

        /// <summary>
        /// 获取标准点数
        /// </summary>
        /// <returns>标准点数(1-13)</returns>
        public int GetStandardValue()
        {
            return (int)rank;
        }

        /// <summary>
        /// 是否为红色牌
        /// </summary>
        /// <returns>是否为红色</returns>
        public bool IsRed()
        {
            return suit == CardSuit.Hearts || suit == CardSuit.Diamonds;
        }

        /// <summary>
        /// 是否为黑色牌
        /// </summary>
        /// <returns>是否为黑色</returns>
        public bool IsBlack()
        {
            return suit == CardSuit.Spades || suit == CardSuit.Clubs;
        }

        /// <summary>
        /// 是否为人头牌
        /// </summary>
        /// <returns>是否为人头牌</returns>
        public bool IsFaceCard()
        {
            return rank == CardRank.Jack || rank == CardRank.Queen || rank == CardRank.King;
        }

        /// <summary>
        /// 是否为数字牌
        /// </summary>
        /// <returns>是否为数字牌</returns>
        public bool IsNumberCard()
        {
            return rank >= CardRank.Two && rank <= CardRank.Ten;
        }

        #endregion

        #region 动画控制方法

        /// <summary>
        /// 开始动画
        /// </summary>
        /// <param name="animation">动画类型</param>
        /// <param name="duration">持续时间</param>
        public void StartAnimation(CardAnimation animation, float duration = 1f)
        {
            currentAnimation = animation;
            isAnimating = true;
            animationProgress = 0f;

            // 触发动画开始事件
            OnAnimationStarted?.Invoke(this, animation);
        }

        /// <summary>
        /// 更新动画进度
        /// </summary>
        /// <param name="progress">进度(0-1)</param>
        public void UpdateAnimation(float progress)
        {
            if (!isAnimating) return;

            animationProgress = Mathf.Clamp01(progress);

            if (animationProgress >= 1f)
            {
                StopAnimation();
            }
        }

        /// <summary>
        /// 停止动画
        /// </summary>
        public void StopAnimation()
        {
            if (!isAnimating) return;

            var previousAnimation = currentAnimation;
            isAnimating = false;
            currentAnimation = CardAnimation.None;
            animationProgress = 0f;

            // 触发动画结束事件
            OnAnimationCompleted?.Invoke(this, previousAnimation);
        }

        /// <summary>
        /// 翻牌动画
        /// </summary>
        /// <param name="reveal">是否翻开</param>
        public void PlayFlipAnimation(bool reveal = true)
        {
            StartAnimation(CardAnimation.Flip);
            isRevealed = reveal;
        }

        /// <summary>
        /// 发牌动画
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        public void PlayDealAnimation(CardPosition targetPosition)
        {
            position = targetPosition;
            StartAnimation(CardAnimation.Deal);
        }

        /// <summary>
        /// 收牌动画
        /// </summary>
        public void PlayCollectAnimation()
        {
            StartAnimation(CardAnimation.Collect);
        }

        #endregion

        #region 效果控制方法

        /// <summary>
        /// 设置高亮效果
        /// </summary>
        /// <param name="highlight">是否高亮</param>
        /// <param name="color">高亮颜色</param>
        public void SetHighlight(bool highlight, Color? color = null)
        {
            isHighlighted = highlight;
            
            if (highlight)
            {
                activeEffect = CardEffect.Highlight;
                OnEffectActivated?.Invoke(this, CardEffect.Highlight);
            }
            else if (activeEffect == CardEffect.Highlight)
            {
                activeEffect = CardEffect.None;
                OnEffectDeactivated?.Invoke(this, CardEffect.Highlight);
            }
        }

        /// <summary>
        /// 设置发光效果
        /// </summary>
        /// <param name="glow">是否发光</param>
        /// <param name="intensity">发光强度</param>
        public void SetGlow(bool glow, float intensity = 1f)
        {
            isGlowing = glow;
            
            if (glow)
            {
                activeEffect = CardEffect.Glow;
                OnEffectActivated?.Invoke(this, CardEffect.Glow);
            }
            else if (activeEffect == CardEffect.Glow)
            {
                activeEffect = CardEffect.None;
                OnEffectDeactivated?.Invoke(this, CardEffect.Glow);
            }
        }

        /// <summary>
        /// 清除所有效果
        /// </summary>
        public void ClearEffects()
        {
            var previousEffect = activeEffect;
            
            isHighlighted = false;
            isGlowing = false;
            activeEffect = CardEffect.None;
            
            if (previousEffect != CardEffect.None)
            {
                OnEffectDeactivated?.Invoke(this, previousEffect);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取牌面显示名称
        /// </summary>
        /// <returns>显示名称</returns>
        private string GetCardDisplayName()
        {
            if (!isRevealed) return "?";
            
            var suitSymbol = SuitSymbols.ContainsKey(suit) ? SuitSymbols[suit] : "?";
            var rankSymbol = RankSymbols.ContainsKey(rank) ? RankSymbols[rank] : "?";
            
            return suitSymbol + rankSymbol;
        }

        /// <summary>
        /// 获取牌面颜色
        /// </summary>
        /// <returns>牌面颜色</returns>
        private Color GetCardColor()
        {
            return SuitColors.ContainsKey(suit) ? SuitColors[suit] : Color.gray;
        }

        /// <summary>
        /// 生成图片路径
        /// </summary>
        /// <returns>图片路径</returns>
        private string GenerateImagePath()
        {
            if (!isRevealed) return backImagePath;
            
            string suitName = suit.ToString().ToLower();
            string rankName = rank == CardRank.Ace ? "ace" : 
                             rank == CardRank.Jack ? "jack" :
                             rank == CardRank.Queen ? "queen" :
                             rank == CardRank.King ? "king" :
                             ((int)rank).ToString();
            
            return $"assets/cards/{suitName}_{rankName}.png";
        }

        #endregion

        #region 转换方法

        /// <summary>
        /// 转换为BaccaratCard
        /// </summary>
        /// <returns>百家乐卡牌</returns>
        public BaccaratCard ToBaccaratCard()
        {
            return new BaccaratCard
            {
                suit = (int)suit,
                rank = (int)rank,
                display_name = displayName,
                baccarat_value = GetBaccaratValue(),
                image_url = imagePath,
                back_image_url = backImagePath,
                is_revealed = isRevealed
            };
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        /// <returns>字典格式</returns>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["suit"] = (int)suit,
                ["rank"] = (int)rank,
                ["displayName"] = displayName,
                ["baccaratValue"] = GetBaccaratValue(),
                ["isRevealed"] = isRevealed,
                ["isVisible"] = isVisible,
                ["position"] = position.ToString(),
                ["dealOrder"] = dealOrder,
                ["dealTime"] = dealTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ["gameNumber"] = gameNumber
            };
        }

        /// <summary>
        /// 从字典创建Card
        /// </summary>
        /// <param name="dict">字典</param>
        /// <returns>卡牌实例</returns>
        public static Card FromDictionary(Dictionary<string, object> dict)
        {
            try
            {
                var card = new Card
                {
                    suit = (CardSuit)Convert.ToInt32(dict["suit"]),
                    rank = (CardRank)Convert.ToInt32(dict["rank"]),
                    isRevealed = Convert.ToBoolean(dict["isRevealed"]),
                    isVisible = Convert.ToBoolean(dict["isVisible"]),
                    dealOrder = Convert.ToInt32(dict["dealOrder"]),
                    gameNumber = dict["gameNumber"].ToString()
                };

                if (Enum.TryParse<CardPosition>(dict["position"].ToString(), out var pos))
                    card.position = pos;

                if (DateTime.TryParse(dict["dealTime"].ToString(), out var dealTime))
                    card.dealTime = dealTime;

                card.UpdateDisplayInfo();
                return card;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Card.FromDictionary失败: {ex.Message}");
                return new Card();
            }
        }

        #endregion

        #region 比较和相等性

        /// <summary>
        /// 相等性比较
        /// </summary>
        /// <param name="other">其他卡牌</param>
        /// <returns>是否相等</returns>
        public bool Equals(Card other)
        {
            if (other == null) return false;
            return suit == other.suit && rank == other.rank;
        }

        /// <summary>
        /// 重写Equals
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Card);
        }

        /// <summary>
        /// 重写GetHashCode
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode()
        {
            return ((int)suit * 13) + (int)rank;
        }

        /// <summary>
        /// 比较大小（按标准扑克牌规则）
        /// </summary>
        /// <param name="other">其他卡牌</param>
        /// <returns>比较结果</returns>
        public int CompareTo(Card other)
        {
            if (other == null) return 1;
            
            // 先比较点数，再比较花色
            int rankComparison = rank.CompareTo(other.rank);
            return rankComparison != 0 ? rankComparison : suit.CompareTo(other.suit);
        }

        /// <summary>
        /// 重写ToString
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return isRevealed ? displayName : "[Hidden Card]";
        }

        #endregion

        #region 事件

        /// <summary>
        /// 动画开始事件
        /// </summary>
        public static event System.Action<Card, CardAnimation> OnAnimationStarted;

        /// <summary>
        /// 动画完成事件
        /// </summary>
        public static event System.Action<Card, CardAnimation> OnAnimationCompleted;

        /// <summary>
        /// 效果激活事件
        /// </summary>
        public static event System.Action<Card, CardEffect> OnEffectActivated;

        /// <summary>
        /// 效果停用事件
        /// </summary>
        public static event System.Action<Card, CardEffect> OnEffectDeactivated;

        #endregion
    }

    #region 枚举类型

    /// <summary>
    /// 花色枚举
    /// </summary>
    public enum CardSuit
    {
        Spades = 1,     // 黑桃 ♠
        Hearts = 2,     // 红桃 ♥
        Clubs = 3,      // 梅花 ♣
        Diamonds = 4    // 方块 ♦
    }

    /// <summary>
    /// 点数枚举
    /// </summary>
    public enum CardRank
    {
        Ace = 1,      // A
        Two = 2,      // 2
        Three = 3,    // 3
        Four = 4,     // 4
        Five = 5,     // 5
        Six = 6,      // 6
        Seven = 7,    // 7
        Eight = 8,    // 8
        Nine = 9,     // 9
        Ten = 10,     // 10
        Jack = 11,    // J
        Queen = 12,   // Q
        King = 13     // K
    }

    /// <summary>
    /// 卡牌位置枚举
    /// </summary>
    public enum CardPosition
    {
        None,           // 无位置
        Deck,           // 牌堆
        BankerFirst,    // 庄家第一张
        BankerSecond,   // 庄家第二张
        BankerThird,    // 庄家第三张
        PlayerFirst,    // 闲家第一张
        PlayerSecond,   // 闲家第二张
        PlayerThird,    // 闲家第三张
        Discard         // 弃牌堆
    }

    /// <summary>
    /// 卡牌动画枚举
    /// </summary>
    public enum CardAnimation
    {
        None,       // 无动画
        Deal,       // 发牌
        Flip,       // 翻牌
        Collect,    // 收牌
        Shuffle,    // 洗牌
        Highlight,  // 高亮
        Bounce,     // 弹跳
        Slide       // 滑动
    }

    /// <summary>
    /// 卡牌效果枚举
    /// </summary>
    public enum CardEffect
    {
        None,       // 无效果
        Highlight,  // 高亮
        Glow,       // 发光
        Pulse,      // 脉冲
        Shake,      // 震动
        Flash,      // 闪烁
        Shadow      // 阴影
    }

    #endregion
}