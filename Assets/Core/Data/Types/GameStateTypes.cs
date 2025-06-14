// Assets/_Core/Data/Types/GameStateTypes.cs
// 游戏状态相关类型 - 对应JavaScript项目的游戏状态管理

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Data.Types
{
    /// <summary>
    /// 游戏阶段枚举 - 对应JavaScript项目的游戏状态
    /// </summary>
    public enum GamePhase
    {
        Waiting = 0,    // 等待开始
        Betting = 1,    // 投注阶段
        Dealing = 2,    // 开牌阶段
        Result = 3,     // 结果阶段
        Settling = 4    // 结算阶段
    }

    /// <summary>
    /// 桌台运行信息 - 对应JavaScript项目的table_run_info
    /// </summary>
    [System.Serializable]
    public class TableRunInfo
    {
        [Header("时间信息")]
        [Tooltip("倒计时结束时间")]
        public int end_time = 0;
        
        [Tooltip("当前服务器时间戳")]
        public long server_time = 0;

        [Header("状态信息")]
        [Tooltip("运行状态 (1=投注中, 2=开牌中, 3=等待中)")]
        public int run_status = 3;
        
        [Tooltip("游戏局号")]
        public string bureau_number = "";

        [Header("游戏信息")]
        [Tooltip("当前轮次")]
        public int round = 1;
        
        [Tooltip("桌台ID")]
        public int table_id = 0;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public TableRunInfo()
        {
        }

        /// <summary>
        /// 获取游戏阶段
        /// </summary>
        /// <returns>游戏阶段</returns>
        public GamePhase GetGamePhase()
        {
            switch (run_status)
            {
                case 1: return GamePhase.Betting;   // 投注中
                case 2: return GamePhase.Dealing;   // 开牌中
                case 3: return GamePhase.Waiting;   // 等待中
                default: return GamePhase.Waiting;
            }
        }

        /// <summary>
        /// 是否可以投注
        /// </summary>
        /// <returns>是否可以投注</returns>
        public bool CanBet()
        {
            return run_status == 1 && end_time > 0;
        }

        /// <summary>
        /// 获取状态描述
        /// </summary>
        /// <returns>状态描述</returns>
        public string GetStatusDescription()
        {
            switch (run_status)
            {
                case 1: return "投注中";
                case 2: return "开牌中";
                case 3: return "等待中";
                default: return "未知状态";
            }
        }

        /// <summary>
        /// 获取剩余时间（秒）
        /// </summary>
        /// <returns>剩余时间</returns>
        public int GetRemainingTime()
        {
            return Math.Max(0, end_time);
        }

        /// <summary>
        /// 更新倒计时
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        public void UpdateCountdown(float deltaTime)
        {
            if (end_time > 0)
            {
                end_time = Math.Max(0, end_time - (int)deltaTime);
            }
        }
    }

    /// <summary>
    /// 游戏状态管理 - 对应JavaScript项目的游戏状态管理
    /// </summary>
    [System.Serializable]
    public class GameStateManager
    {
        [Header("当前状态")]
        [Tooltip("当前游戏阶段")]
        public GamePhase currentPhase = GamePhase.Waiting;
        
        [Tooltip("上一个游戏阶段")]
        public GamePhase previousPhase = GamePhase.Waiting;
        
        [Tooltip("状态变化时间")]
        public float stateChangeTime = 0f;

        [Header("桌台信息")]
        [Tooltip("桌台运行信息")]
        public TableRunInfo tableRunInfo = new TableRunInfo();
        
        [Tooltip("上次更新时间")]
        public float lastUpdateTime = 0f;

        [Header("闪烁管理")]
        [Tooltip("当前闪烁区域")]
        public List<int> flashingAreas = new List<int>();
        
        [Tooltip("是否当前局已闪烁")]
        public bool currentGameFlashed = false;
        
        [Tooltip("闪烁持续时间")]
        public float flashDuration = 5.0f;

        [Header("局号管理")]
        [Tooltip("当前局号")]
        public string currentBureauNumber = "";
        
        [Tooltip("上次处理的局号")]
        public string lastProcessedBureauNumber = "";

        /// <summary>
        /// 更新游戏状态
        /// </summary>
        /// <param name="newTableRunInfo">新的桌台运行信息</param>
        public void UpdateGameState(TableRunInfo newTableRunInfo)
        {
            if (newTableRunInfo == null) return;

            // 保存上一个阶段
            previousPhase = currentPhase;
            
            // 更新桌台信息
            tableRunInfo = newTableRunInfo;
            lastUpdateTime = Time.time;

            // 更新当前阶段
            GamePhase newPhase = newTableRunInfo.GetGamePhase();
            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                stateChangeTime = Time.time;
                OnPhaseChanged(previousPhase, currentPhase);
            }

            // 更新局号
            if (!string.IsNullOrEmpty(newTableRunInfo.bureau_number) && 
                newTableRunInfo.bureau_number != currentBureauNumber)
            {
                lastProcessedBureauNumber = currentBureauNumber;
                currentBureauNumber = newTableRunInfo.bureau_number;
                OnNewRound(currentBureauNumber);
            }
        }

        /// <summary>
        /// 阶段变化回调
        /// </summary>
        /// <param name="fromPhase">从哪个阶段</param>
        /// <param name="toPhase">到哪个阶段</param>
        private void OnPhaseChanged(GamePhase fromPhase, GamePhase toPhase)
        {
            Debug.Log($"游戏阶段变化: {fromPhase} -> {toPhase}");
            
            // 如果进入新一轮等待状态，清理闪烁效果
            if (toPhase == GamePhase.Waiting && fromPhase != GamePhase.Waiting)
            {
                ClearFlashEffect();
            }
        }

        /// <summary>
        /// 新一轮开始回调
        /// </summary>
        /// <param name="newBureauNumber">新局号</param>
        private void OnNewRound(string newBureauNumber)
        {
            Debug.Log($"新一轮开始: {newBureauNumber}");
            
            // 重置本轮状态
            currentGameFlashed = false;
            ClearFlashEffect();
        }

        /// <summary>
        /// 设置闪烁效果
        /// </summary>
        /// <param name="areas">闪烁区域ID列表</param>
        public void SetFlashEffect(List<int> areas)
        {
            if (areas == null || areas.Count == 0) return;

            flashingAreas = new List<int>(areas);
            currentGameFlashed = true;
            
            Debug.Log($"设置闪烁效果: [{string.Join(", ", areas)}]");
        }

        /// <summary>
        /// 清除闪烁效果
        /// </summary>
        public void ClearFlashEffect()
        {
            flashingAreas.Clear();
            Debug.Log("清除闪烁效果");
        }

        /// <summary>
        /// 检查是否处于投注阶段
        /// </summary>
        /// <returns>是否可以投注</returns>
        public bool CanBet()
        {
            return currentPhase == GamePhase.Betting && tableRunInfo.CanBet();
        }

        /// <summary>
        /// 获取当前状态描述
        /// </summary>
        /// <returns>状态描述</returns>
        public string GetCurrentStateDescription()
        {
            return $"{GetPhaseDescription(currentPhase)} - {tableRunInfo.GetStatusDescription()}";
        }

        /// <summary>
        /// 获取阶段描述
        /// </summary>
        /// <param name="phase">游戏阶段</param>
        /// <returns>阶段描述</returns>
        private string GetPhaseDescription(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Waiting: return "等待开始";
                case GamePhase.Betting: return "投注阶段";
                case GamePhase.Dealing: return "开牌阶段";
                case GamePhase.Result: return "结果阶段";
                case GamePhase.Settling: return "结算阶段";
                default: return "未知阶段";
            }
        }

        /// <summary>
        /// 检查是否为新局
        /// </summary>
        /// <param name="bureauNumber">局号</param>
        /// <returns>是否为新局</returns>
        public bool IsNewRound(string bureauNumber)
        {
            return !string.IsNullOrEmpty(bureauNumber) && bureauNumber != currentBureauNumber;
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset()
        {
            currentPhase = GamePhase.Waiting;
            previousPhase = GamePhase.Waiting;
            stateChangeTime = 0f;
            tableRunInfo = new TableRunInfo();
            lastUpdateTime = 0f;
            flashingAreas.Clear();
            currentGameFlashed = false;
            currentBureauNumber = "";
            lastProcessedBureauNumber = "";
        }
    }

    /// <summary>
    /// 中奖显示信息 - 对应JavaScript项目的中奖弹窗
    /// </summary>
    [System.Serializable]
    public class WinningDisplayInfo
    {
        [Header("显示状态")]
        [Tooltip("是否显示中奖弹窗")]
        public bool showWinningPopup = false;
        
        [Tooltip("中奖金额")]
        public float winningAmount = 0f;
        
        [Tooltip("中奖局号")]
        public string winningRoundId = "";

        [Header("音效状态")]
        [Tooltip("是否已播放中奖音效")]
        public bool audioPlayed = false;
        
        [Tooltip("音效播放标识（防重复）")]
        public string audioPlayedKey = "";

        [Header("显示配置")]
        [Tooltip("显示持续时间")]
        public float displayDuration = 3.0f;
        
        [Tooltip("自动关闭")]
        public bool autoClose = true;

        /// <summary>
        /// 显示中奖信息
        /// </summary>
        /// <param name="amount">中奖金额</param>
        /// <param name="roundId">局号</param>
        /// <returns>是否成功显示</returns>
        public bool ShowWinning(float amount, string roundId = "")
        {
            if (amount <= 0) return false;

            winningAmount = amount;
            winningRoundId = roundId;
            showWinningPopup = true;
            audioPlayed = false;
            audioPlayedKey = "";
            
            return true;
        }

        /// <summary>
        /// 关闭中奖显示
        /// </summary>
        public void CloseWinning()
        {
            showWinningPopup = false;
            winningAmount = 0f;
            winningRoundId = "";
        }

        /// <summary>
        /// 播放中奖音效
        /// </summary>
        /// <param name="roundId">局号（用于防重复）</param>
        /// <returns>是否需要播放</returns>
        public bool PlayWinningAudio(string roundId = "")
        {
            string currentKey = $"{roundId}_{winningAmount}";
            
            if (audioPlayed && audioPlayedKey == currentKey)
            {
                return false; // 已经播放过了
            }

            audioPlayed = true;
            audioPlayedKey = currentKey;
            return true;
        }

        /// <summary>
        /// 重置音效状态
        /// </summary>
        public void ResetAudioState()
        {
            audioPlayed = false;
            audioPlayedKey = "";
        }
    }

    /// <summary>
    /// 多消息处理状态 - 对应JavaScript项目的重复消息处理
    /// </summary>
    [System.Serializable]
    public class MultiMessageProcessState
    {
        [Header("处理状态")]
        [Tooltip("当前处理的局号")]
        public string bureauNumber = "";
        
        [Tooltip("是否已设置闪烁")]
        public bool flashSet = false;
        
        [Tooltip("是否已显示中奖")]
        public bool winningShown = false;
        
        [Tooltip("是否已清理")]
        public bool cleared = false;

        [Header("时间记录")]
        [Tooltip("首次处理时间")]
        public float firstProcessTime = 0f;
        
        [Tooltip("最后处理时间")]
        public float lastProcessTime = 0f;

        /// <summary>
        /// 重置为新局
        /// </summary>
        /// <param name="newBureauNumber">新局号</param>
        public void ResetForNewRound(string newBureauNumber)
        {
            bureauNumber = newBureauNumber;
            flashSet = false;
            winningShown = false;
            cleared = false;
            firstProcessTime = Time.time;
            lastProcessTime = Time.time;
        }

        /// <summary>
        /// 更新处理时间
        /// </summary>
        public void UpdateProcessTime()
        {
            lastProcessTime = Time.time;
        }

        /// <summary>
        /// 检查是否为新局
        /// </summary>
        /// <param name="currentBureauNumber">当前局号</param>
        /// <returns>是否为新局</returns>
        public bool IsNewRound(string currentBureauNumber)
        {
            return bureauNumber != currentBureauNumber;
        }

        /// <summary>
        /// 检查是否需要处理闪烁
        /// </summary>
        /// <returns>是否需要处理</returns>
        public bool ShouldProcessFlash()
        {
            return !flashSet;
        }

        /// <summary>
        /// 检查是否需要显示中奖
        /// </summary>
        /// <returns>是否需要显示</returns>
        public bool ShouldShowWinning()
        {
            return !winningShown;
        }

        /// <summary>
        /// 标记闪烁已处理
        /// </summary>
        public void MarkFlashProcessed()
        {
            flashSet = true;
            UpdateProcessTime();
        }

        /// <summary>
        /// 标记中奖已显示
        /// </summary>
        public void MarkWinningShown()
        {
            winningShown = true;
            UpdateProcessTime();
        }

        /// <summary>
        /// 标记已清理
        /// </summary>
        public void MarkCleared()
        {
            cleared = true;
            UpdateProcessTime();
        }
    }

    /// <summary>
    /// 游戏状态快照 - 用于调试和状态保存
    /// </summary>
    [System.Serializable]
    public class GameStateSnapshot
    {
        [Header("时间信息")]
        [Tooltip("快照时间")]
        public string timestamp = "";
        
        [Tooltip("游戏局号")]
        public string gameNumber = "";

        [Header("状态信息")]
        [Tooltip("游戏阶段")]
        public GamePhase gamePhase = GamePhase.Waiting;
        
        [Tooltip("倒计时")]
        public int countdown = 0;
        
        [Tooltip("运行状态")]
        public int runStatus = 0;

        [Header("投注信息")]
        [Tooltip("用户余额")]
        public float userBalance = 0f;
        
        [Tooltip("总投注金额")]
        public float totalBetAmount = 0f;

        [Header("连接状态")]
        [Tooltip("WebSocket连接状态")]
        public string connectionStatus = "";
        
        [Tooltip("最后错误信息")]
        public string lastError = "";

        /// <summary>
        /// 创建当前状态快照
        /// </summary>
        /// <param name="gameState">游戏状态管理器</param>
        /// <param name="userInfo">用户信息</param>
        /// <param name="connectionStatus">连接状态</param>
        /// <returns>状态快照</returns>
        public static GameStateSnapshot CreateSnapshot(GameStateManager gameState, UserInfo userInfo, string connectionStatus)
        {
            return new GameStateSnapshot
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                gameNumber = gameState.currentBureauNumber,
                gamePhase = gameState.currentPhase,
                countdown = gameState.tableRunInfo.GetRemainingTime(),
                runStatus = gameState.tableRunInfo.run_status,
                userBalance = userInfo?.balance ?? 0f,
                connectionStatus = connectionStatus,
                lastError = ""
            };
        }

        /// <summary>
        /// 转换为JSON字符串
        /// </summary>
        /// <returns>JSON字符串</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// 从JSON字符串创建快照
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>状态快照</returns>
        public static GameStateSnapshot FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<GameStateSnapshot>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse GameStateSnapshot from JSON: {e.Message}");
                return new GameStateSnapshot();
            }
        }
    }

    /// <summary>
    /// 清理回调委托 - 对应JavaScript项目的清理回调
    /// </summary>
    public delegate void CleanupCallback(List<BaccaratBetTarget> betTargets);

    /// <summary>
    /// 清理回调管理器
    /// </summary>
    [System.Serializable]
    public class CleanupCallbackManager
    {
        [Header("回调管理")]
        [Tooltip("注册的回调数量")]
        [SerializeField] private int registeredCallbackCount = 0;
        
        // 实际的回调列表（非序列化）
        [System.NonSerialized]
        private List<CleanupCallback> callbacks = new List<CleanupCallback>();

        /// <summary>
        /// 注册清理回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        public void RegisterCallback(CleanupCallback callback)
        {
            if (callback != null && !callbacks.Contains(callback))
            {
                callbacks.Add(callback);
                registeredCallbackCount = callbacks.Count;
            }
        }

        /// <summary>
        /// 取消注册回调
        /// </summary>
        /// <param name="callback">回调函数</param>
        public void UnregisterCallback(CleanupCallback callback)
        {
            if (callback != null)
            {
                callbacks.Remove(callback);
                registeredCallbackCount = callbacks.Count;
            }
        }

        /// <summary>
        /// 执行所有清理回调
        /// </summary>
        /// <param name="betTargets">投注目标列表</param>
        public void ExecuteCallbacks(List<BaccaratBetTarget> betTargets)
        {
            foreach (var callback in callbacks)
            {
                try
                {
                    callback?.Invoke(betTargets);
                }
                catch (Exception e)
                {
                    Debug.LogError($"清理回调执行失败: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 清空所有回调
        /// </summary>
        public void ClearCallbacks()
        {
            callbacks.Clear();
            registeredCallbackCount = 0;
        }

        /// <summary>
        /// 获取回调数量
        /// </summary>
        /// <returns>回调数量</returns>
        public int GetCallbackCount()
        {
            return callbacks.Count;
        }
    }
}