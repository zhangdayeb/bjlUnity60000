// Assets/_Core/Data/Types/BaccaratTypes.cs
// 百家乐专用类型 - 对应JavaScript项目的百家乐相关数据结构

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Data.Types
{
    /// <summary>
    /// 百家乐投注类型枚举
    /// 对应JavaScript项目中的投注区域ID
    /// </summary>
    public enum BaccaratBetType
    {
        Banker = 1,        // 庄家
        Player = 2,        // 闲家
        Tie = 3,           // 和局
        BankerPair = 4,    // 庄对
        PlayerPair = 5,    // 闲对
        BigBig = 6,        // 大大
        SmallSmall = 7     // 小小
    }

    /// <summary>
    /// 百家乐投注目标 - 对应JavaScript中的投注区域数据
    /// </summary>
    [System.Serializable]
    public class BaccaratBetTarget
    {
        [Header("基础信息")]
        [Tooltip("投注区域ID（对应rate_id）")]
        public int id = 0;
        
        [Tooltip("投注区域标签")]
        public string label = "";
        
        [Tooltip("投注区域类名（CSS样式）")]
        public string className = "";

        [Header("投注数据")]
        [Tooltip("当前投注金额")]
        public float betAmount = 0f;
        
        [Tooltip("显示的筹码列表")]
        public List<ChipDisplayData> showChip = new List<ChipDisplayData>();

        [Header("视觉效果")]
        [Tooltip("闪烁样式类名")]
        public string flashClass = "";
        
        [Tooltip("是否正在闪烁")]
        public bool isFlashing = false;

        [Header("赔率信息")]
        [Tooltip("赔率")]
        public float odds = 1.0f;
        
        [Tooltip("赔率描述")]
        public string oddsDescription = "1:1";

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public BaccaratBetTarget()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public BaccaratBetTarget(int betId, string betLabel, string betClassName, float betOdds)
        {
            id = betId;
            label = betLabel;
            className = betClassName;
            odds = betOdds;
            oddsDescription = $"1:{betOdds}";
        }

        /// <summary>
        /// 添加投注金额
        /// </summary>
        /// <param name="amount">投注金额</param>
        public void AddBetAmount(float amount)
        {
            if (amount > 0)
            {
                betAmount += amount;
            }
        }

        /// <summary>
        /// 设置闪烁效果
        /// </summary>
        /// <param name="flashClassName">闪烁样式类名</param>
        public void SetFlashEffect(string flashClassName)
        {
            flashClass = flashClassName;
            isFlashing = !string.IsNullOrEmpty(flashClassName);
        }

        /// <summary>
        /// 清除闪烁效果
        /// </summary>
        public void ClearFlashEffect()
        {
            flashClass = "";
            isFlashing = false;
        }

        /// <summary>
        /// 清除投注数据
        /// </summary>
        public void ClearBet()
        {
            betAmount = 0f;
            showChip.Clear();
        }

        /// <summary>
        /// 获取投注类型
        /// </summary>
        /// <returns>投注类型枚举</returns>
        public BaccaratBetType GetBetType()
        {
            if (Enum.IsDefined(typeof(BaccaratBetType), id))
            {
                return (BaccaratBetType)id;
            }
            return BaccaratBetType.Banker;
        }
    }

    /// <summary>
    /// 筹码显示数据 - 对应JavaScript中的筹码显示
    /// </summary>
    [System.Serializable]
    public class ChipDisplayData
    {
        [Tooltip("筹码面值")]
        public float value = 0f;
        
        [Tooltip("筹码文本")]
        public string text = "";
        
        [Tooltip("筹码图片路径")]
        public string imagePath = "";
        
        [Tooltip("筹码精灵（运行时设置）")]
        [System.NonSerialized]
        public Sprite sprite;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ChipDisplayData()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public ChipDisplayData(float chipValue, string chipText, string chipImagePath)
        {
            value = chipValue;
            text = chipText;
            imagePath = chipImagePath;
        }
    }

    /// <summary>
    /// 百家乐投注请求 - 对应JavaScript中的投注请求格式
    /// </summary>
    [System.Serializable]
    public class BaccaratBetRequest
    {
        [Tooltip("投注金额")]
        public float money = 0f;
        
        [Tooltip("投注类型ID")]
        public int rate_id = 0;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public BaccaratBetRequest()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public BaccaratBetRequest(float betMoney, int rateId)
        {
            money = betMoney;
            rate_id = rateId;
        }
    }

    /// <summary>
    /// 百家乐投注响应 - 对应JavaScript中的投注响应格式
    /// </summary>
    [System.Serializable]
    public class BaccaratBetResponse
    {
        [Header("响应信息")]
        public int code = 0;
        public string message = "";

        [Header("投注结果")]
        public string bet_id = "";
        public float new_balance = 0f;
        public float total_amount = 0f;
        public List<BaccaratBetRequest> bets = new List<BaccaratBetRequest>();

        /// <summary>
        /// 是否成功
        /// </summary>
        /// <returns>是否成功</returns>
        public bool IsSuccess()
        {
            return code == 200 || code == 1;
        }
    }

    /// <summary>
    /// 百家乐牌面信息 - 对应JavaScript中的牌面数据
    /// </summary>
    [System.Serializable]
    public class BaccaratCard
    {
        [Tooltip("花色（1=黑桃, 2=红桃, 3=梅花, 4=方块）")]
        public int suit = 1;
        
        [Tooltip("点数（1-13）")]
        public int rank = 1;
        
        [Tooltip("牌面显示值")]
        public string display = "";
        
        [Tooltip("牌面图片路径")]
        public string imagePath = "";

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public BaccaratCard()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public BaccaratCard(int cardSuit, int cardRank)
        {
            suit = cardSuit;
            rank = cardRank;
            display = GetCardDisplay();
        }

        /// <summary>
        /// 获取百家乐点数（A=1, J/Q/K=0, 其他按面值，最后取个位数）
        /// </summary>
        /// <returns>百家乐点数</returns>
        public int GetBaccaratValue()
        {
            if (rank == 1) return 1;        // A = 1
            if (rank >= 11) return 0;       // J/Q/K = 0
            return rank % 10;               // 其他牌取个位数
        }

        /// <summary>
        /// 获取花色描述
        /// </summary>
        /// <returns>花色描述</returns>
        public string GetSuitDescription()
        {
            switch (suit)
            {
                case 1: return "♠";
                case 2: return "♥";
                case 3: return "♣";
                case 4: return "♦";
                default: return "?";
            }
        }

        /// <summary>
        /// 获取点数描述
        /// </summary>
        /// <returns>点数描述</returns>
        public string GetRankDescription()
        {
            switch (rank)
            {
                case 1: return "A";
                case 11: return "J";
                case 12: return "Q";
                case 13: return "K";
                default: return rank.ToString();
            }
        }

        /// <summary>
        /// 获取牌面显示
        /// </summary>
        /// <returns>牌面显示字符串</returns>
        public string GetCardDisplay()
        {
            return $"{GetRankDescription()}{GetSuitDescription()}";
        }
    }

    /// <summary>
    /// 百家乐游戏结果 - 对应JavaScript中的开牌结果
    /// </summary>
    [System.Serializable]
    public class BaccaratGameResult
    {
        [Header("基础信息")]
        [Tooltip("游戏局号")]
        public string game_number = "";
        
        [Tooltip("开奖时间戳")]
        public long timestamp = 0;

        [Header("牌面信息")]
        [Tooltip("庄家牌")]
        public List<BaccaratCard> banker_cards = new List<BaccaratCard>();
        
        [Tooltip("闲家牌")]
        public List<BaccaratCard> player_cards = new List<BaccaratCard>();

        [Header("点数信息")]
        [Tooltip("庄家点数")]
        public int banker_points = 0;
        
        [Tooltip("闲家点数")]
        public int player_points = 0;

        [Header("开奖结果")]
        [Tooltip("获胜方")]
        public BaccaratWinner winner = BaccaratWinner.Tie;
        
        [Tooltip("是否庄对")]
        public bool has_banker_pair = false;
        
        [Tooltip("是否闲对")]
        public bool has_player_pair = false;

        [Header("中奖投注类型")]
        [Tooltip("中奖的投注类型ID数组")]
        public List<int> winning_bet_ids = new List<int>();
        
        [Tooltip("闪烁区域ID数组（pai_flash）")]
        public List<int> flash_areas = new List<int>();

        /// <summary>
        /// 计算庄家点数
        /// </summary>
        /// <returns>庄家点数</returns>
        public int CalculateBankerPoints()
        {
            int total = 0;
            foreach (var card in banker_cards)
            {
                total += card.GetBaccaratValue();
            }
            banker_points = total % 10;
            return banker_points;
        }

        /// <summary>
        /// 计算闲家点数
        /// </summary>
        /// <returns>闲家点数</returns>
        public int CalculatePlayerPoints()
        {
            int total = 0;
            foreach (var card in player_cards)
            {
                total += card.GetBaccaratValue();
            }
            player_points = total % 10;
            return player_points;
        }

        /// <summary>
        /// 判断获胜方
        /// </summary>
        /// <returns>获胜方</returns>
        public BaccaratWinner DetermineWinner()
        {
            CalculateBankerPoints();
            CalculatePlayerPoints();

            if (banker_points > player_points)
            {
                winner = BaccaratWinner.Banker;
            }
            else if (player_points > banker_points)
            {
                winner = BaccaratWinner.Player;
            }
            else
            {
                winner = BaccaratWinner.Tie;
            }

            return winner;
        }

        /// <summary>
        /// 检查庄对
        /// </summary>
        /// <returns>是否庄对</returns>
        public bool CheckBankerPair()
        {
            if (banker_cards.Count >= 2)
            {
                has_banker_pair = banker_cards[0].rank == banker_cards[1].rank;
            }
            return has_banker_pair;
        }

        /// <summary>
        /// 检查闲对
        /// </summary>
        /// <returns>是否闲对</returns>
        public bool CheckPlayerPair()
        {
            if (player_cards.Count >= 2)
            {
                has_player_pair = player_cards[0].rank == player_cards[1].rank;
            }
            return has_player_pair;
        }

        /// <summary>
        /// 获取获胜方描述
        /// </summary>
        /// <returns>获胜方描述</returns>
        public string GetWinnerDescription()
        {
            switch (winner)
            {
                case BaccaratWinner.Banker: return "庄家";
                case BaccaratWinner.Player: return "闲家";
                case BaccaratWinner.Tie: return "和局";
                default: return "未知";
            }
        }
    }

    /// <summary>
    /// 百家乐获胜方枚举
    /// </summary>
    public enum BaccaratWinner
    {
        Banker = 1,     // 庄家胜
        Player = 2,     // 闲家胜
        Tie = 3         // 和局
    }

    /// <summary>
    /// WebSocket游戏结果消息 - 对应JavaScript中的WebSocket消息格式
    /// </summary>
    [System.Serializable]
    public class BaccaratWSGameResultMessage
    {
        public int code = 0;
        public string msg = "";
        public BaccaratWSResultData data;

        [System.Serializable]
        public class BaccaratWSResultData
        {
            public string bureau_number = "";
            public BaccaratWSResultInfo result_info;
        }

        [System.Serializable]
        public class BaccaratWSResultInfo
        {
            [Tooltip("中奖金额")]
            public float money = 0f;
            
            [Tooltip("闪烁区域ID数组")]
            public List<int> pai_flash = new List<int>();
            
            [Tooltip("牌面信息")]
            public List<BaccaratCard> pai_info = new List<BaccaratCard>();
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        /// <returns>是否成功</returns>
        public bool IsSuccess()
        {
            return code == 200;
        }
    }

    /// <summary>
    /// 百家乐投注限额配置
    /// </summary>
    [System.Serializable]
    public class BaccaratBetLimits
    {
        [Header("基础投注限额")]
        public float banker_min = 10f;
        public float banker_max = 50000f;
        public float player_min = 10f;
        public float player_max = 50000f;
        public float tie_min = 10f;
        public float tie_max = 10000f;

        [Header("对子投注限额")]
        public float pair_min = 10f;
        public float pair_max = 5000f;

        [Header("边注投注限额")]
        public float side_bet_min = 10f;
        public float side_bet_max = 1000f;

        /// <summary>
        /// 获取指定投注类型的限额
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <returns>投注限额（最小值，最大值）</returns>
        public (float min, float max) GetLimits(BaccaratBetType betType)
        {
            switch (betType)
            {
                case BaccaratBetType.Banker:
                    return (banker_min, banker_max);
                case BaccaratBetType.Player:
                    return (player_min, player_max);
                case BaccaratBetType.Tie:
                    return (tie_min, tie_max);
                case BaccaratBetType.BankerPair:
                case BaccaratBetType.PlayerPair:
                    return (pair_min, pair_max);
                case BaccaratBetType.BigBig:
                case BaccaratBetType.SmallSmall:
                    return (side_bet_min, side_bet_max);
                default:
                    return (10f, 1000f);
            }
        }
    }
}