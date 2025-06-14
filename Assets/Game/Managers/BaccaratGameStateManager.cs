// Assets/Game/Managers/BaccaratGameStateManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Core.Architecture;
using Core.Network.Interfaces;
using Core.Data.Types;

namespace Game.Managers
{
    /// <summary>
    /// 百家乐游戏状态管理器 - 对应JavaScript项目中的useGameState.js
    /// 负责处理游戏状态变化、消息处理、效果显示等核心逻辑
    /// </summary>
    public class BaccaratGameStateManager : MonoBehaviour
    {
        [Header("游戏状态配置")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private float flashEffectDuration = 2f;
        [SerializeField] private float winningDisplayDuration = 5f;
        
        [Header("效果配置")]
        [SerializeField] private Color flashWinColor = Color.green;
        [SerializeField] private Color flashLoseColor = Color.red;
        [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        #region 私有字段

        private IWebSocketService _webSocketService;
        private BaccaratBettingManager _bettingManager;
        private ChipManager _chipManager;
        private ExemptManager _exemptManager;
        
        // 响应式数据 - 对应Vue的ref
        private ReactiveData<BaccaratGamePhase> _currentPhase = new ReactiveData<BaccaratGamePhase>(BaccaratGamePhase.Waiting);
        private ReactiveData<int> _countdown = new ReactiveData<int>(0);
        private ReactiveData<string> _gameNumber = new ReactiveData<string>("");
        private ReactiveData<BaccaratGameResult> _lastGameResult = new ReactiveData<BaccaratGameResult>(null);
        private ReactiveData<bool> _isFlashing = new ReactiveData<bool>(false);
        private ReactiveData<WinningInfo> _currentWinning = new ReactiveData<WinningInfo>(null);
        
        // 游戏状态历史
        private List<GameStateSnapshot> _stateHistory = new List<GameStateSnapshot>();
        
        // 清理回调管理
        private CleanupCallbackManager _cleanupCallbacks = new CleanupCallbackManager();
        
        // 效果协程控制
        private Coroutine _flashEffectCoroutine;
        private Coroutine _winningDisplayCoroutine;
        
        // 消息处理统计
        private MessageProcessingStats _messageStats = new MessageProcessingStats();

        #endregion

        #region 公共属性 - 对应JavaScript中的computed

        /// <summary>
        /// 当前游戏阶段
        /// </summary>
        public BaccaratGamePhase CurrentPhase => _currentPhase.Value;
        
        /// <summary>
        /// 倒计时秒数
        /// </summary>
        public int Countdown => _countdown.Value;
        
        /// <summary>
        /// 当前游戏局号
        /// </summary>
        public string GameNumber => _gameNumber.Value;
        
        /// <summary>
        /// 最后的游戏结果
        /// </summary>
        public BaccaratGameResult LastGameResult => _lastGameResult.Value;
        
        /// <summary>
        /// 是否正在闪烁效果中
        /// </summary>
        public bool IsFlashing => _isFlashing.Value;
        
        /// <summary>
        /// 当前中奖信息
        /// </summary>
        public WinningInfo CurrentWinning => _currentWinning.Value;
        
        /// <summary>
        /// 是否可以投注
        /// </summary>
        public bool CanBet => _currentPhase.Value == BaccaratGamePhase.Betting && _countdown.Value > 0;
        
        /// <summary>
        /// 是否正在开牌
        /// </summary>
        public bool IsDealing => _currentPhase.Value == BaccaratGamePhase.Dealing;
        
        /// <summary>
        /// 是否显示结果
        /// </summary>
        public bool IsShowingResult => _currentPhase.Value == BaccaratGamePhase.Result;

        #endregion

        #region 事件 - 对应JavaScript中的emit

        public event Action<BaccaratGamePhase> OnPhaseChanged;
        public event Action<int> OnCountdownUpdated;
        public event Action<string> OnGameNumberChanged;
        public event Action<BaccaratGameResult> OnGameResult;
        public event Action<WinningInfo> OnWinningDisplayed;
        public event Action<bool> OnFlashEffectChanged;
        public event Action<GameStateSnapshot> OnStateSnapshotTaken;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            SetupReactiveBindings();
            InitializeCleanupCallbacks();
        }

        private void Start()
        {
            // 获取依赖组件
            _webSocketService = ServiceLocator.GetService<IWebSocketService>();
            _bettingManager = FindObjectOfType<BaccaratBettingManager>();
            _chipManager = FindObjectOfType<ChipManager>();
            _exemptManager = FindObjectOfType<ExemptManager>();
            
            if (_webSocketService == null)
            {
                Debug.LogError("BaccaratGameStateManager: 未找到IWebSocketService服务");
                return;
            }

            // 订阅WebSocket消息
            SubscribeToWebSocketEvents();
            
            // 初始化游戏状态
            InitializeGameState();
        }

        private void OnDestroy()
        {
            UnsubscribeFromWebSocketEvents();
            StopAllEffects();
            SaveStateHistory();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 设置响应式数据绑定
        /// </summary>
        private void SetupReactiveBindings()
        {
            _currentPhase.OnValueChanged += (phase) => OnPhaseChanged?.Invoke(phase);
            _countdown.OnValueChanged += (countdown) => OnCountdownUpdated?.Invoke(countdown);
            _gameNumber.OnValueChanged += (gameNumber) => OnGameNumberChanged?.Invoke(gameNumber);
            _lastGameResult.OnValueChanged += (result) => 
            {
                if (result != null) OnGameResult?.Invoke(result);
            };
            _currentWinning.OnValueChanged += (winning) => 
            {
                if (winning != null) OnWinningDisplayed?.Invoke(winning);
            };
            _isFlashing.OnValueChanged += (flashing) => OnFlashEffectChanged?.Invoke(flashing);
        }

        /// <summary>
        /// 初始化清理回调
        /// </summary>
        private void InitializeCleanupCallbacks()
        {
            // 注册投注管理器的清理回调
            _cleanupCallbacks.RegisterCallback((betTargets) =>
            {
                if (_bettingManager != null)
                {
                    _bettingManager.ClearAfterGameResult();
                }
            });
            
            // 注册筹码管理器的清理回调
            _cleanupCallbacks.RegisterCallback((betTargets) =>
            {
                if (_chipManager != null)
                {
                    _chipManager.ClearAreaChips();
                }
            });
        }

        /// <summary>
        /// 订阅WebSocket事件
        /// </summary>
        private void SubscribeToWebSocketEvents()
        {
            if (_webSocketService is BaccaratWebSocketService baccaratWS)
            {
                baccaratWS.OnCountdownMessage += HandleCountdownMessage;
                baccaratWS.OnGameResultMessage += HandleGameResultMessage;
                baccaratWS.OnWinningMessage += HandleWinningMessage;
                baccaratWS.OnGameStatusMessage += HandleGameStatusMessage;
            }
        }

        /// <summary>
        /// 取消订阅WebSocket事件
        /// </summary>
        private void UnsubscribeFromWebSocketEvents()
        {
            if (_webSocketService is BaccaratWebSocketService baccaratWS)
            {
                baccaratWS.OnCountdownMessage -= HandleCountdownMessage;
                baccaratWS.OnGameResultMessage -= HandleGameResultMessage;
                baccaratWS.OnWinningMessage -= HandleWinningMessage;
                baccaratWS.OnGameStatusMessage -= HandleGameStatusMessage;
            }
        }

        /// <summary>
        /// 初始化游戏状态
        /// </summary>
        private void InitializeGameState()
        {
            _currentPhase.Value = BaccaratGamePhase.Waiting;
            _countdown.Value = 0;
            _gameNumber.Value = "";
            
            LoadStateHistory();
            
            if (enableDebugLog)
            {
                Debug.Log("游戏状态管理器初始化完成");
            }
        }

        #endregion

        #region 核心消息处理方法 - 对应JavaScript中的主要函数

        /// <summary>
        /// 处理游戏消息 - 对应processGameMessage函数
        /// 这是消息处理的核心函数，根据消息类型分发到具体处理器
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageData">消息数据</param>
        /// <returns>处理是否成功</returns>
        public bool ProcessGameMessage(string messageType, object messageData)
        {
            try
            {
                _messageStats.totalProcessed++;
                var startTime = DateTime.UtcNow;

                bool processed = messageType.ToLower() switch
                {
                    "countdown" => ProcessCountdownMessage(messageData),
                    "game_result" => ProcessGameResultMessage(messageData),
                    "winning_data" => ProcessWinningMessage(messageData),
                    "game_status" => ProcessGameStatusMessage(messageData),
                    "table_info" => ProcessTableInfoMessage(messageData),
                    _ => ProcessUnknownMessage(messageType, messageData)
                };

                // 更新统计
                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _messageStats.totalProcessingTime += processingTime;
                
                if (processed)
                {
                    _messageStats.successfullyProcessed++;
                    TakeStateSnapshot($"Message: {messageType}");
                }
                else
                {
                    _messageStats.processingErrors++;
                }

                if (enableDebugLog)
                {
                    Debug.Log($"消息处理: {messageType} -> {(processed ? "成功" : "失败")} ({processingTime:F2}ms)");
                }

                return processed;
            }
            catch (Exception ex)
            {
                _messageStats.processingErrors++;
                Debug.LogError($"ProcessGameMessage异常: {messageType} - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 处理游戏结果 - 对应handleGameResult函数
        /// </summary>
        /// <param name="gameResult">游戏结果数据</param>
        public void HandleGameResult(BaccaratGameResult gameResult)
        {
            try
            {
                if (gameResult == null)
                {
                    Debug.LogWarning("HandleGameResult: 游戏结果为空");
                    return;
                }

                if (enableDebugLog)
                {
                    Debug.Log($"处理游戏结果: 局号={gameResult.game_number}, 赢家={gameResult.winner}");
                }

                // 1. 更新游戏状态
                _lastGameResult.Value = gameResult;
                _gameNumber.Value = gameResult.game_number;
                _currentPhase.Value = BaccaratGamePhase.Result;

                // 2. 设置闪烁效果
                SetFlashEffect(gameResult);

                // 3. 显示中奖信息（如果有）
                if (gameResult.total_payout > 0)
                {
                    var winningInfo = CreateWinningInfo(gameResult);
                    ShowWinningDisplay(winningInfo);
                }

                // 4. 执行清理回调
                ExecuteCleanupCallbacks();

                // 5. 延迟准备下一局
                _ = PrepareNextGameAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"HandleGameResult异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置闪烁效果 - 对应setFlashEffect函数
        /// </summary>
        /// <param name="gameResult">游戏结果</param>
        public void SetFlashEffect(BaccaratGameResult gameResult)
        {
            try
            {
                if (gameResult == null) return;

                // 停止现有闪烁效果
                if (_flashEffectCoroutine != null)
                {
                    StopCoroutine(_flashEffectCoroutine);
                }

                // 确定闪烁颜色（基于是否中奖）
                Color flashColor = gameResult.total_payout > 0 ? flashWinColor : flashLoseColor;
                
                // 启动新的闪烁效果
                _flashEffectCoroutine = StartCoroutine(FlashEffectCoroutine(flashColor, flashEffectDuration));

                if (enableDebugLog)
                {
                    Debug.Log($"设置闪烁效果: 颜色={flashColor}, 时长={flashEffectDuration}s");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"SetFlashEffect异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示中奖信息 - 对应showWinningDisplay函数
        /// </summary>
        /// <param name="winningInfo">中奖信息</param>
        public void ShowWinningDisplay(WinningInfo winningInfo)
        {
            try
            {
                if (winningInfo == null) return;

                // 停止现有中奖显示
                if (_winningDisplayCoroutine != null)
                {
                    StopCoroutine(_winningDisplayCoroutine);
                }

                // 更新中奖信息
                _currentWinning.Value = winningInfo;

                // 启动中奖显示协程
                _winningDisplayCoroutine = StartCoroutine(WinningDisplayCoroutine(winningDisplayDuration));

                if (enableDebugLog)
                {
                    Debug.Log($"显示中奖信息: 金额={winningInfo.winAmount}, 类型={winningInfo.winType}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ShowWinningDisplay异常: {ex.Message}");
            }
        }

        #endregion

        #region 消息处理辅助方法

        /// <summary>
        /// 处理倒计时消息
        /// </summary>
        private bool ProcessCountdownMessage(object messageData)
        {
            try
            {
                if (messageData is CountdownMessage countdown)
                {
                    _countdown.Value = countdown.countdown;
                    _gameNumber.Value = countdown.game_number;
                    
                    // 根据倒计时更新游戏阶段
                    if (countdown.countdown > 0)
                    {
                        _currentPhase.Value = BaccaratGamePhase.Betting;
                    }
                    else if (_currentPhase.Value == BaccaratGamePhase.Betting)
                    {
                        _currentPhase.Value = BaccaratGamePhase.Dealing;
                    }
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ProcessCountdownMessage异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 处理游戏结果消息
        /// </summary>
        private bool ProcessGameResultMessage(object messageData)
        {
            try
            {
                if (messageData is GameResultMessage resultMsg)
                {
                    var gameResult = ConvertToGameResult(resultMsg);
                    HandleGameResult(gameResult);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ProcessGameResultMessage异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 处理中奖消息
        /// </summary>
        private bool ProcessWinningMessage(object messageData)
        {
            try
            {
                if (messageData is WinningMessage winMsg)
                {
                    var winningInfo = ConvertToWinningInfo(winMsg);
                    ShowWinningDisplay(winningInfo);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ProcessWinningMessage异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 处理游戏状态消息
        /// </summary>
        private bool ProcessGameStatusMessage(object messageData)
        {
            try
            {
                if (messageData is GameStatusMessage statusMsg)
                {
                    _gameNumber.Value = statusMsg.game_number;
                    
                    // 解析游戏阶段
                    var phase = statusMsg.status.ToLower() switch
                    {
                        "waiting" => BaccaratGamePhase.Waiting,
                        "betting" => BaccaratGamePhase.Betting,
                        "dealing" => BaccaratGamePhase.Dealing,
                        "result" => BaccaratGamePhase.Result,
                        _ => BaccaratGamePhase.Waiting
                    };
                    
                    _currentPhase.Value = phase;
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ProcessGameStatusMessage异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 处理桌台信息消息
        /// </summary>
        private bool ProcessTableInfoMessage(object messageData)
        {
            try
            {
                // 桌台信息更新逻辑
                if (enableDebugLog)
                {
                    Debug.Log("处理桌台信息更新");
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ProcessTableInfoMessage异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 处理未知消息
        /// </summary>
        private bool ProcessUnknownMessage(string messageType, object messageData)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"收到未知消息类型: {messageType}");
            }
            return false;
        }

        #endregion

        #region WebSocket事件处理器

        private void HandleCountdownMessage(CountdownMessage message)
        {
            ProcessGameMessage("countdown", message);
        }

        private void HandleGameResultMessage(GameResultMessage message)
        {
            ProcessGameMessage("game_result", message);
        }

        private void HandleWinningMessage(WinningMessage message)
        {
            ProcessGameMessage("winning_data", message);
        }

        private void HandleGameStatusMessage(GameStatusMessage message)
        {
            ProcessGameMessage("game_status", message);
        }

        #endregion

        #region 效果协程

        /// <summary>
        /// 闪烁效果协程
        /// </summary>
        private System.Collections.IEnumerator FlashEffectCoroutine(Color flashColor, float duration)
        {
            _isFlashing.Value = true;
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // 使用动画曲线控制闪烁强度
                float intensity = flashCurve.Evaluate(progress);
                
                // 这里可以通过事件通知UI组件更新闪烁效果
                // OnFlashIntensityChanged?.Invoke(flashColor, intensity);
                
                yield return null;
            }
            
            _isFlashing.Value = false;
            _flashEffectCoroutine = null;
        }

        /// <summary>
        /// 中奖显示协程
        /// </summary>
        private System.Collections.IEnumerator WinningDisplayCoroutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            
            _currentWinning.Value = null;
            _winningDisplayCoroutine = null;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 执行清理回调
        /// </summary>
        private void ExecuteCleanupCallbacks()
        {
            try
            {
                var betTargets = _bettingManager?.BetTargets ?? new List<BaccaratBetTarget>();
                _cleanupCallbacks.ExecuteCallbacks(betTargets);
                
                if (enableDebugLog)
                {
                    Debug.Log("执行清理回调完成");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ExecuteCleanupCallbacks异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 准备下一局游戏
        /// </summary>
        private async Task PrepareNextGameAsync()
        {
            try
            {
                // 等待结果显示完成
                await Task.Delay((int)(winningDisplayDuration * 1000));
                
                // 重置到等待状态
                _currentPhase.Value = BaccaratGamePhase.Waiting;
                _countdown.Value = 0;
                
                if (enableDebugLog)
                {
                    Debug.Log("准备下一局游戏");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"PrepareNextGameAsync异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 转换消息到游戏结果
        /// </summary>
        private BaccaratGameResult ConvertToGameResult(GameResultMessage message)
        {
            return new BaccaratGameResult
            {
                game_number = message.game_number,
                winner = message.winner,
                banker_points = message.banker_points,
                player_points = message.player_points,
                banker_cards = message.banker_cards ?? new List<BaccaratCard>(),
                player_cards = message.player_cards ?? new List<BaccaratCard>(),
                banker_pair = message.banker_pair,
                player_pair = message.player_pair,
                is_big = message.is_big,
                winning_bets = message.winning_bets ?? new List<string>(),
                total_payout = message.total_payout,
                result_time = message.result_time
            };
        }

        /// <summary>
        /// 转换消息到中奖信息
        /// </summary>
        private WinningInfo ConvertToWinningInfo(WinningMessage message)
        {
            return new WinningInfo
            {
                winAmount = message.win_amount,
                winType = message.win_type,
                winDescription = message.win_description,
                multiplier = message.multiplier,
                timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 从游戏结果创建中奖信息
        /// </summary>
        private WinningInfo CreateWinningInfo(BaccaratGameResult gameResult)
        {
            return new WinningInfo
            {
                winAmount = gameResult.total_payout,
                winType = string.Join(", ", gameResult.winning_bets),
                winDescription = $"恭喜中奖！获得 {gameResult.total_payout:F2} 元",
                multiplier = 1f,
                timestamp = gameResult.result_time
            };
        }

        /// <summary>
        /// 记录状态快照
        /// </summary>
        private void TakeStateSnapshot(string reason)
        {
            try
            {
                var snapshot = new GameStateSnapshot
                {
                    timestamp = DateTime.UtcNow,
                    gamePhase = _currentPhase.Value,
                    countdown = _countdown.Value,
                    gameNumber = _gameNumber.Value,
                    reason = reason
                };

                _stateHistory.Add(snapshot);
                
                // 限制历史记录数量
                if (_stateHistory.Count > 100)
                {
                    _stateHistory.RemoveAt(0);
                }

                OnStateSnapshotTaken?.Invoke(snapshot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"TakeStateSnapshot异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止所有效果
        /// </summary>
        private void StopAllEffects()
        {
            if (_flashEffectCoroutine != null)
            {
                StopCoroutine(_flashEffectCoroutine);
                _flashEffectCoroutine = null;
            }

            if (_winningDisplayCoroutine != null)
            {
                StopCoroutine(_winningDisplayCoroutine);
                _winningDisplayCoroutine = null;
            }

            _isFlashing.Value = false;
            _currentWinning.Value = null;
        }

        /// <summary>
        /// 加载状态历史
        /// </summary>
        private void LoadStateHistory()
        {
            try
            {
                string historyJson = PlayerPrefs.GetString("game_state_history", "[]");
                var historyData = JsonUtility.FromJson<GameStateHistoryData>(historyJson);
                _stateHistory = historyData?.snapshots ?? new List<GameStateSnapshot>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"加载状态历史失败: {ex.Message}");
                _stateHistory = new List<GameStateSnapshot>();
            }
        }

        /// <summary>
        /// 保存状态历史
        /// </summary>
        private void SaveStateHistory()
        {
            try
            {
                var historyData = new GameStateHistoryData { snapshots = _stateHistory };
                string historyJson = JsonUtility.ToJson(historyData);
                PlayerPrefs.SetString("game_state_history", historyJson);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"保存状态历史失败: {ex.Message}");
            }
        }

        #endregion

        #region 公共查询方法

        /// <summary>
        /// 获取当前游戏状态信息
        /// </summary>
        public GameStateInfo GetCurrentGameStateInfo()
        {
            return new GameStateInfo
            {
                phase = _currentPhase.Value,
                countdown = _countdown.Value,
                gameNumber = _gameNumber.Value,
                canBet = CanBet,
                isDealing = IsDealing,
                isShowingResult = IsShowingResult,
                hasWinning = _currentWinning.Value != null,
                isFlashing = _isFlashing.Value
            };
        }

        /// <summary>
        /// 获取消息处理统计
        /// </summary>
        public MessageProcessingStats GetMessageProcessingStats()
        {
            return _messageStats.Clone();
        }

        /// <summary>
        /// 获取状态历史
        /// </summary>
        public List<GameStateSnapshot> GetStateHistory()
        {
            return new List<GameStateSnapshot>(_stateHistory);
        }

        /// <summary>
        /// 注册清理回调
        /// </summary>
        public void RegisterCleanupCallback(CleanupCallback callback)
        {
            _cleanupCallbacks.RegisterCallback(callback);
        }

        /// <summary>
        /// 取消注册清理回调
        /// </summary>
        public void UnregisterCleanupCallback(CleanupCallback callback)
        {
            _cleanupCallbacks.UnregisterCallback(callback);
        }

        /// <summary>
        /// 手动触发状态重置
        /// </summary>
        public void ResetGameState()
        {
            StopAllEffects();
            _currentPhase.Value = BaccaratGamePhase.Waiting;
            _countdown.Value = 0;
            _lastGameResult.Value = null;
            _currentWinning.Value = null;
            
            TakeStateSnapshot("Manual Reset");
        }

        #endregion
    }

    #region 数据类型定义

    /// <summary>
    /// 百家乐游戏阶段枚举
    /// </summary>
    public enum BaccaratGamePhase
    {
        Waiting,    // 等待
        Betting,    // 投注
        Dealing,    // 开牌
        Result      // 结果
    }

    /// <summary>
    /// 游戏状态信息
    /// </summary>
    [System.Serializable]
    public class GameStateInfo
    {
        public BaccaratGamePhase phase;
        public int countdown;
        public string gameNumber;
        public bool canBet;
        public bool isDealing;
        public bool isShowingResult;
        public bool hasWinning;
        public bool isFlashing;
    }

    /// <summary>
    /// 消息处理统计
    /// </summary>
    [System.Serializable]
    public class MessageProcessingStats
    {
        public int totalProcessed = 0;
        public int successfullyProcessed = 0;
        public int processingErrors = 0;
        public double totalProcessingTime = 0; // 毫秒
        
        public double AverageProcessingTime => totalProcessed > 0 ? totalProcessingTime / totalProcessed : 0;
        public float SuccessRate => totalProcessed > 0 ? (float)successfullyProcessed / totalProcessed * 100 : 0;

        public MessageProcessingStats Clone()
        {
            return new MessageProcessingStats
            {
                totalProcessed = this.totalProcessed,
                successfullyProcessed = this.successfullyProcessed,
                processingErrors = this.processingErrors,
                totalProcessingTime = this.totalProcessingTime
            };
        }
    }

    /// <summary>
    /// 状态历史数据（用于序列化）
    /// </summary>
    [System.Serializable]
    public class GameStateHistoryData
    {
        public List<GameStateSnapshot> snapshots;
    }

    #endregion
}