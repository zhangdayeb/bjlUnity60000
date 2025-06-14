// Assets/_Core/Data/Types/BettingTypes.cs
// 投注相关类型 - 对应JavaScript项目的投注管理数据结构

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Data.Types
{
    /// <summary>
    /// 投注状态枚举
    /// </summary>
    public enum BetStatus
    {
        Pending = 0,     // 待确认
        Confirmed = 1,   // 已确认
        Cancelled = 2,   // 已取消
        Won = 3,         // 已中奖
        Lost = 4,        // 已输
        Processing = 5   // 处理中
    }

    /// <summary>
    /// 投注阶段枚举 - 对应JavaScript项目的投注阶段管理
    /// </summary>
    public enum BettingPhase
    {
        Waiting = 0,     // 等待开始
        Betting = 1,     // 可以投注
        Confirmed = 2,   // 已确认投注，可继续加注
        Dealing = 3,     // 开牌中，禁止投注
        Result = 4       // 结果阶段
    }

    /// <summary>
    /// 投注记录 - 对应JavaScript项目的投注数据
    /// </summary>
    [System.Serializable]
    public class BettingRecord
    {
        [Header("基础信息")]
        [Tooltip("投注记录ID")]
        public string id = "";
        
        [Tooltip("游戏局号")]
        public string game_number = "";
        
        [Tooltip("桌台ID")]
        public string table_id = "";
        
        [Tooltip("用户ID")]
        public string user_id = "";

        [Header("时间信息")]
        [Tooltip("投注时间")]
        public string bet_time = "";
        
        [Tooltip("结算时间")]
        public string settle_time = "";

        [Header("投注详情")]
        [Tooltip("投注详情列表")]
        public List<BetDetail> bet_details = new List<BetDetail>();
        
        [Tooltip("总投注金额")]
        public float total_bet_amount = 0f;
        
        [Tooltip("总中奖金额")]
        public float total_win_amount = 0f;
        
        [Tooltip("净盈亏")]
        public float net_amount = 0f;

        [Header("开奖信息")]
        [Tooltip("开奖结果")]
        public List<int> dice_results = new List<int>();
        
        [Tooltip("总点数")]
        public int dice_total = 0;

        [Header("状态信息")]
        [Tooltip("投注状态")]
        public BetStatus status = BetStatus.Pending;
        
        [Tooltip("是否已结算")]
        public bool is_settled = false;
        
        [Tooltip("货币类型")]
        public string currency = "CNY";

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public BettingRecord()
        {
        }

        /// <summary>
        /// 计算净盈亏
        /// </summary>
        /// <returns>净盈亏金额</returns>
        public float CalculateNetAmount()
        {
            net_amount = total_win_amount - total_bet_amount;
            return net_amount;
        }

        /// <summary>
        /// 是否盈利
        /// </summary>
        /// <returns>是否盈利</returns>
        public bool IsProfit()
        {
            return net_amount > 0;
        }

        /// <summary>
        /// 获取状态描述
        /// </summary>
        /// <returns>状态描述</returns>
        public string GetStatusDescription()
        {
            switch (status)
            {
                case BetStatus.Pending: return "待开奖";
                case BetStatus.Confirmed: return "已确认";
                case BetStatus.Cancelled: return "已取消";
                case BetStatus.Won: return "已中奖";
                case BetStatus.Lost: return "未中奖";
                case BetStatus.Processing: return "处理中";
                default: return "未知状态";
            }
        }

        /// <summary>
        /// 获取状态颜色
        /// </summary>
        /// <returns>状态颜色</returns>
        public Color GetStatusColor()
        {
            switch (status)
            {
                case BetStatus.Pending: return Color.yellow;
                case BetStatus.Confirmed: return Color.blue;
                case BetStatus.Cancelled: return Color.gray;
                case BetStatus.Won: return Color.green;
                case BetStatus.Lost: return Color.red;
                case BetStatus.Processing: return Color.cyan;
                default: return Color.white;
            }
        }
    }

    /// <summary>
    /// 投注详情 - 对应JavaScript项目的单个投注项
    /// </summary>
    [System.Serializable]
    public class BetDetail
    {
        [Header("投注信息")]
        [Tooltip("投注类型")]
        public string bet_type = "";
        
        [Tooltip("投注类型名称")]
        public string bet_type_name = "";
        
        [Tooltip("投注金额")]
        public float bet_amount = 0f;

        [Header("赔率信息")]
        [Tooltip("赔率")]
        public string odds = "1:1";
        
        [Tooltip("中奖金额")]
        public float win_amount = 0f;
        
        [Tooltip("是否中奖")]
        public bool is_win = false;

        [Header("系统信息")]
        [Tooltip("赔率ID")]
        public int rate_id = 0;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public BetDetail()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public BetDetail(string betType, string betTypeName, float betAmount, string betOdds, int rateId)
        {
            bet_type = betType;
            bet_type_name = betTypeName;
            bet_amount = betAmount;
            odds = betOdds;
            rate_id = rateId;
        }

        /// <summary>
        /// 计算中奖金额
        /// </summary>
        /// <param name="multiplier">倍数</param>
        /// <returns>中奖金额</returns>
        public float CalculateWinAmount(float multiplier)
        {
            if (is_win)
            {
                win_amount = bet_amount * multiplier;
            }
            else
            {
                win_amount = 0f;
            }
            return win_amount;
        }

        /// <summary>
        /// 设置中奖结果
        /// </summary>
        /// <param name="won">是否中奖</param>
        /// <param name="winMoney">中奖金额</param>
        public void SetWinResult(bool won, float winMoney = 0f)
        {
            is_win = won;
            win_amount = won ? winMoney : 0f;
        }
    }

    /// <summary>
    /// 投注历史查询参数 - 对应JavaScript项目的查询参数
    /// </summary>
    [System.Serializable]
    public class BettingHistoryParams
    {
        [Header("分页参数")]
        [Tooltip("页码（从1开始）")]
        public int page = 1;
        
        [Tooltip("每页大小")]
        public int pageSize = 20;

        [Header("筛选条件")]
        [Tooltip("开始日期（YYYY-MM-DD）")]
        public string start_date = "";
        
        [Tooltip("结束日期（YYYY-MM-DD）")]
        public string end_date = "";
        
        [Tooltip("投注状态筛选")]
        public BetStatus? status_filter = null;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public BettingHistoryParams()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public BettingHistoryParams(int pageNumber, int pageSizeValue)
        {
            page = pageNumber;
            pageSize = pageSizeValue;
        }

        /// <summary>
        /// 设置日期范围
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        public void SetDateRange(string startDate, string endDate)
        {
            start_date = startDate;
            end_date = endDate;
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValid()
        {
            return page > 0 && pageSize > 0 && pageSize <= 100;
        }
    }

    /// <summary>
    /// 投注历史响应 - 对应JavaScript项目的API响应格式
    /// </summary>
    [System.Serializable]
    public class BettingHistoryResponse
    {
        [Header("记录数据")]
        [Tooltip("投注记录列表")]
        public List<BettingRecord> records = new List<BettingRecord>();

        [Header("分页信息")]
        [Tooltip("分页信息")]
        public PaginationInfo pagination = new PaginationInfo();

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public BettingHistoryResponse()
        {
        }
    }

    /// <summary>
    /// 分页信息 - 对应JavaScript项目的分页数据
    /// </summary>
    [System.Serializable]
    public class PaginationInfo
    {
        [Tooltip("当前页码")]
        public int current_page = 1;
        
        [Tooltip("总页数")]
        public int total_pages = 0;
        
        [Tooltip("总记录数")]
        public int total_records = 0;
        
        [Tooltip("每页大小")]
        public int page_size = 20;
        
        [Tooltip("是否还有更多数据")]
        public bool has_more = false;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public PaginationInfo()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public PaginationInfo(int currentPage, int totalPages, int totalRecords, int pageSize)
        {
            current_page = currentPage;
            total_pages = totalPages;
            total_records = totalRecords;
            page_size = pageSize;
            has_more = currentPage < totalPages;
        }

        /// <summary>
        /// 计算总页数
        /// </summary>
        /// <param name="totalRecords">总记录数</param>
        /// <param name="pageSize">每页大小</param>
        public void CalculateTotalPages(int totalRecords, int pageSize)
        {
            this.total_records = totalRecords;
            this.page_size = pageSize;
            this.total_pages = (int)Math.Ceiling((double)totalRecords / pageSize);
            this.has_more = current_page < total_pages;
        }
    }

    /// <summary>
    /// 投注加载状态 - 对应JavaScript项目的加载状态管理
    /// </summary>
    [System.Serializable]
    public class BettingLoadingState
    {
        [Tooltip("初始加载")]
        public bool loading = false;
        
        [Tooltip("下拉刷新")]
        public bool refreshing = false;
        
        [Tooltip("上拉加载更多")]
        public bool loadingMore = false;
        
        [Tooltip("错误信息")]
        public string error = "";

        /// <summary>
        /// 是否正在加载
        /// </summary>
        /// <returns>是否正在加载</returns>
        public bool IsLoading()
        {
            return loading || refreshing || loadingMore;
        }

        /// <summary>
        /// 设置加载状态
        /// </summary>
        /// <param name="isLoading">是否加载</param>
        /// <param name="loadingType">加载类型</param>
        public void SetLoading(bool isLoading, string loadingType = "loading")
        {
            // 重置所有状态
            loading = false;
            refreshing = false;
            loadingMore = false;

            if (isLoading)
            {
                switch (loadingType)
                {
                    case "loading":
                        loading = true;
                        break;
                    case "refreshing":
                        refreshing = true;
                        break;
                    case "loadingMore":
                        loadingMore = true;
                        break;
                }
            }

            if (!isLoading)
            {
                error = "";
            }
        }

        /// <summary>
        /// 设置错误
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        public void SetError(string errorMessage)
        {
            loading = false;
            refreshing = false;
            loadingMore = false;
            error = errorMessage;
        }

        /// <summary>
        /// 清除错误
        /// </summary>
        public void ClearError()
        {
            error = "";
        }
    }

    /// <summary>
    /// 日期筛选条件 - 对应JavaScript项目的日期筛选
    /// </summary>
    [System.Serializable]
    public class DateFilter
    {
        [Tooltip("开始日期")]
        public string start = "";
        
        [Tooltip("结束日期")]
        public string end = "";

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public DateFilter()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public DateFilter(string startDate, string endDate)
        {
            start = startDate;
            end = endDate;
        }

        /// <summary>
        /// 是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end);
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        /// <returns>是否为空</returns>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end);
        }

        /// <summary>
        /// 清空筛选
        /// </summary>
        public void Clear()
        {
            start = "";
            end = "";
        }

        /// <summary>
        /// 设置今天
        /// </summary>
        public void SetToday()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            start = today;
            end = today;
        }

        /// <summary>
        /// 设置本周
        /// </summary>
        public void SetThisWeek()
        {
            DateTime now = DateTime.Now;
            DateTime startOfWeek = now.AddDays(-(int)now.DayOfWeek);
            start = startOfWeek.ToString("yyyy-MM-dd");
            end = now.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 设置本月
        /// </summary>
        public void SetThisMonth()
        {
            DateTime now = DateTime.Now;
            DateTime startOfMonth = new DateTime(now.Year, now.Month, 1);
            start = startOfMonth.ToString("yyyy-MM-dd");
            end = now.ToString("yyyy-MM-dd");
        }
    }

    /// <summary>
    /// 投注统计信息 - 对应JavaScript项目的统计计算
    /// </summary>
    [System.Serializable]
    public class BettingStatistics
    {
        [Header("投注统计")]
        [Tooltip("总投注金额")]
        public float totalBet = 0f;
        
        [Tooltip("总中奖金额")]
        public float totalWin = 0f;
        
        [Tooltip("净盈亏")]
        public float netAmount = 0f;

        [Header("次数统计")]
        [Tooltip("中奖次数")]
        public int winCount = 0;
        
        [Tooltip("总次数")]
        public int totalCount = 0;
        
        [Tooltip("胜率")]
        public float winRate = 0f;

        /// <summary>
        /// 计算统计信息
        /// </summary>
        /// <param name="records">投注记录列表</param>
        public void Calculate(List<BettingRecord> records)
        {
            totalBet = 0f;
            totalWin = 0f;
            winCount = 0;
            totalCount = records.Count;

            foreach (var record in records)
            {
                totalBet += record.total_bet_amount;
                totalWin += record.total_win_amount;
                
                if (record.IsProfit())
                {
                    winCount++;
                }
            }

            netAmount = totalWin - totalBet;
            winRate = totalCount > 0 ? (float)winCount / totalCount * 100f : 0f;
        }

        /// <summary>
        /// 获取胜率百分比字符串
        /// </summary>
        /// <returns>胜率百分比</returns>
        public string GetWinRatePercentage()
        {
            return $"{winRate:F1}%";
        }

        /// <summary>
        /// 是否盈利
        /// </summary>
        /// <returns>是否盈利</returns>
        public bool IsProfit()
        {
            return netAmount > 0;
        }
    }
}