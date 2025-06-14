// Assets/_Core/Data/Types/TableInfo.cs
// 台桌信息 - 对应JavaScript项目的台桌数据结构

using System;
using UnityEngine;

namespace Core.Data.Types
{
    /// <summary>
    /// 台桌信息 - 对应JavaScript中的TableInfo接口
    /// 包含台桌基础信息、视频流、投注限额等数据
    /// </summary>
    [System.Serializable]
    public class TableInfo
    {
        [Header("基础台桌信息")]
        [Tooltip("台桌ID")]
        public int id = 0;
        
        [Tooltip("台桌名称")]
        public string table_title = "";
        
        [Tooltip("台桌描述名称")]
        public string lu_zhu_name = "";
        
        [Tooltip("当前局号")]
        public string bureau_number = "";

        [Header("玩家信息")]
        [Tooltip("普通玩家数量")]
        public int num_pu = 0;
        
        [Tooltip("血战玩家数量")]
        public int num_xue = 0;
        
        [Tooltip("总玩家数量")]
        public int total_players = 0;

        [Header("视频流信息")]
        [Tooltip("远景视频流URL")]
        public string video_far = "";
        
        [Tooltip("近景视频流URL")]
        public string video_near = "";
        
        [Tooltip("当前视频模式")]
        public VideoMode current_video_mode = VideoMode.Far;

        [Header("时间信息")]
        [Tooltip("开始时间戳")]
        public long time_start = 0;
        
        [Tooltip("当前倒计时")]
        public int countdown = 0;
        
        [Tooltip("游戏状态")]
        public TableGameStatus game_status = TableGameStatus.Waiting;

        [Header("投注限额")]
        [Tooltip("庄家闲家投注限额")]
        public float right_money_banker_player = 0f;
        
        [Tooltip("和局投注限额")]
        public float right_money_tie = 0f;
        
        [Tooltip("最小投注金额")]
        public float min_bet = 10f;
        
        [Tooltip("最大投注金额")]
        public float max_bet = 50000f;

        [Header("台桌配置")]
        [Tooltip("台桌类型")]
        public TableType table_type = TableType.Standard;
        
        [Tooltip("台桌状态")]
        public TableStatus table_status = TableStatus.Open;
        
        [Tooltip("最大玩家容量")]
        public int max_capacity = 100;
        
        [Tooltip("是否启用聊天")]
        public bool chat_enabled = true;
        
        [Tooltip("是否显示统计")]
        public bool statistics_enabled = true;

        [Header("庄家信息")]
        [Tooltip("庄家ID")]
        public string dealer_id = "";
        
        [Tooltip("庄家姓名")]
        public string dealer_name = "";
        
        [Tooltip("庄家头像")]
        public string dealer_avatar = "";

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public TableInfo()
        {
        }

        /// <summary>
        /// 带基础参数的构造函数
        /// </summary>
        public TableInfo(int tableId, string tableName, string farVideo, string nearVideo)
        {
            id = tableId;
            table_title = tableName;
            lu_zhu_name = tableName;
            video_far = farVideo;
            video_near = nearVideo;
        }

        /// <summary>
        /// 验证台桌信息是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValid()
        {
            return id > 0 && 
                   !string.IsNullOrEmpty(table_title) &&
                   (!string.IsNullOrEmpty(video_far) || !string.IsNullOrEmpty(video_near));
        }

        /// <summary>
        /// 获取当前视频URL
        /// </summary>
        /// <returns>当前选择的视频URL</returns>
        public string GetCurrentVideoUrl()
        {
            switch (current_video_mode)
            {
                case VideoMode.Far:
                    return !string.IsNullOrEmpty(video_far) ? video_far : video_near;
                case VideoMode.Near:
                    return !string.IsNullOrEmpty(video_near) ? video_near : video_far;
                default:
                    return video_far;
            }
        }

        /// <summary>
        /// 切换视频模式
        /// </summary>
        /// <returns>切换后的视频URL</returns>
        public string SwitchVideoMode()
        {
            current_video_mode = current_video_mode == VideoMode.Far ? VideoMode.Near : VideoMode.Far;
            return GetCurrentVideoUrl();
        }

        /// <summary>
        /// 设置视频模式
        /// </summary>
        /// <param name="mode">视频模式</param>
        /// <returns>设置后的视频URL</returns>
        public string SetVideoMode(VideoMode mode)
        {
            current_video_mode = mode;
            return GetCurrentVideoUrl();
        }

        /// <summary>
        /// 检查是否有多个视频角度
        /// </summary>
        /// <returns>是否有多个视频角度</returns>
        public bool HasMultipleVideoAngles()
        {
            return !string.IsNullOrEmpty(video_far) && !string.IsNullOrEmpty(video_near);
        }

        /// <summary>
        /// 更新玩家数量
        /// </summary>
        /// <param name="puCount">普通玩家数量</param>
        /// <param name="xueCount">血战玩家数量</param>
        public void UpdatePlayerCount(int puCount, int xueCount)
        {
            num_pu = puCount;
            num_xue = xueCount;
            total_players = puCount + xueCount;
        }

        /// <summary>
        /// 更新倒计时
        /// </summary>
        /// <param name="newCountdown">新的倒计时值</param>
        public void UpdateCountdown(int newCountdown)
        {
            countdown = Math.Max(0, newCountdown);
        }

        /// <summary>
        /// 更新游戏状态
        /// </summary>
        /// <param name="status">新的游戏状态</param>
        public void UpdateGameStatus(TableGameStatus status)
        {
            game_status = status;
        }

        /// <summary>
        /// 检查台桌是否可以投注
        /// </summary>
        /// <returns>是否可以投注</returns>
        public bool CanBet()
        {
            return table_status == TableStatus.Open &&
                   game_status == TableGameStatus.Betting &&
                   countdown > 0;
        }

        /// <summary>
        /// 检查台桌是否已满
        /// </summary>
        /// <returns>是否已满</returns>
        public bool IsFull()
        {
            return total_players >= max_capacity;
        }

        /// <summary>
        /// 获取台桌状态描述
        /// </summary>
        /// <returns>状态描述</returns>
        public string GetStatusDescription()
        {
            switch (table_status)
            {
                case TableStatus.Open:
                    return "开放中";
                case TableStatus.Closed:
                    return "已关闭";
                case TableStatus.Maintenance:
                    return "维护中";
                case TableStatus.Full:
                    return "已满员";
                default:
                    return "未知状态";
            }
        }

        /// <summary>
        /// 获取游戏状态描述
        /// </summary>
        /// <returns>游戏状态描述</returns>
        public string GetGameStatusDescription()
        {
            switch (game_status)
            {
                case TableGameStatus.Waiting:
                    return "等待开始";
                case TableGameStatus.Betting:
                    return "投注中";
                case TableGameStatus.Dealing:
                    return "开牌中";
                case TableGameStatus.Result:
                    return "结算中";
                default:
                    return "未知状态";
            }
        }

        /// <summary>
        /// 获取台桌类型描述
        /// </summary>
        /// <returns>台桌类型描述</returns>
        public string GetTableTypeDescription()
        {
            switch (table_type)
            {
                case TableType.Standard:
                    return "标准桌";
                case TableType.VIP:
                    return "VIP桌";
                case TableType.HighLimit:
                    return "高限桌";
                case TableType.Speed:
                    return "快速桌";
                default:
                    return "标准桌";
            }
        }

        /// <summary>
        /// 获取开始时间的DateTime对象
        /// </summary>
        /// <returns>开始时间</returns>
        public DateTime GetStartDateTime()
        {
            return DateTimeOffset.FromUnixTimeSeconds(time_start).DateTime;
        }

        /// <summary>
        /// 克隆台桌信息
        /// </summary>
        /// <returns>克隆的台桌信息</returns>
        public TableInfo Clone()
        {
            return new TableInfo
            {
                id = this.id,
                table_title = this.table_title,
                lu_zhu_name = this.lu_zhu_name,
                bureau_number = this.bureau_number,
                num_pu = this.num_pu,
                num_xue = this.num_xue,
                total_players = this.total_players,
                video_far = this.video_far,
                video_near = this.video_near,
                current_video_mode = this.current_video_mode,
                time_start = this.time_start,
                countdown = this.countdown,
                game_status = this.game_status,
                right_money_banker_player = this.right_money_banker_player,
                right_money_tie = this.right_money_tie,
                min_bet = this.min_bet,
                max_bet = this.max_bet,
                table_type = this.table_type,
                table_status = this.table_status,
                max_capacity = this.max_capacity,
                chat_enabled = this.chat_enabled,
                statistics_enabled = this.statistics_enabled,
                dealer_id = this.dealer_id,
                dealer_name = this.dealer_name,
                dealer_avatar = this.dealer_avatar
            };
        }

        /// <summary>
        /// 从JSON字符串反序列化
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>台桌信息对象</returns>
        public static TableInfo FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<TableInfo>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse TableInfo from JSON: {e.Message}");
                return new TableInfo();
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
                Debug.LogError($"Failed to serialize TableInfo to JSON: {e.Message}");
                return "{}";
            }
        }

        /// <summary>
        /// 转换为字符串（调试用）
        /// </summary>
        /// <returns>格式化的字符串</returns>
        public override string ToString()
        {
            return $"TableInfo[ID={id}, Title={table_title}, Status={GetStatusDescription()}, Players={total_players}/{max_capacity}, GameStatus={GetGameStatusDescription()}]";
        }
    }

    /// <summary>
    /// 视频模式枚举
    /// </summary>
    public enum VideoMode
    {
        Far = 0,    // 远景
        Near = 1    // 近景
    }

    /// <summary>
    /// 台桌类型枚举
    /// </summary>
    public enum TableType
    {
        Standard = 0,   // 标准桌
        VIP = 1,        // VIP桌
        HighLimit = 2,  // 高限桌
        Speed = 3       // 快速桌
    }

    /// <summary>
    /// 台桌状态枚举
    /// </summary>
    public enum TableStatus
    {
        Open = 0,       // 开放
        Closed = 1,     // 关闭
        Maintenance = 2, // 维护
        Full = 3        // 已满
    }

    /// <summary>
    /// 台桌游戏状态枚举
    /// </summary>
    public enum TableGameStatus
    {
        Waiting = 0,    // 等待
        Betting = 1,    // 投注
        Dealing = 2,    // 开牌
        Result = 3      // 结果
    }

    /// <summary>
    /// 台桌信息响应 - 对应API返回格式
    /// </summary>
    [System.Serializable]
    public class TableInfoResponse
    {
        public int code;
        public string message;
        public TableInfo data;
        
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