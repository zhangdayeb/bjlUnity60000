// Assets/_Core/Architecture/GameDataStore.cs
// 全局数据存储 - 类似Pinia/Vuex Store，管理应用程序的全局状态

using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Data.Types;

namespace Core.Architecture
{
    /// <summary>
    /// 游戏数据存储 - 管理全局游戏状态数据
    /// 类似于Vue的Pinia Store，提供响应式的全局状态管理
    /// </summary>
    public class GameDataStore : MonoBehaviour
    {
        [Header("数据存储配置")]
        [Tooltip("是否在场景切换时保持")]
        public bool dontDestroyOnLoad = true;
        
        [Tooltip("是否启用数据持久化")]
        public bool enablePersistence = true;
        
        [Tooltip("是否启用调试日志")]
        public bool enableDebugLogs = true;

        [Header("状态监控")]
        [SerializeField] private bool isInitialized = false;
        [SerializeField] private int reactiveDataCount = 0;
        [SerializeField] private int subscriberCount = 0;

        // 单例实例
        private static GameDataStore _instance;

        #region 响应式数据 - 用户相关

        [Header("用户数据")]
        [Tooltip("用户信息")]
        public ReactiveData<UserInfo> UserInfo = new ReactiveData<UserInfo>("UserInfo");
        
        [Tooltip("用户余额")]
        public ReactiveData<float> UserBalance = new ReactiveData<float>(10000f, "UserBalance");
        
        [Tooltip("用户等级")]
        public ReactiveData<int> UserLevel = new ReactiveData<int>(1, "UserLevel");
        
        [Tooltip("用户VIP等级")]
        public ReactiveData<int> UserVipLevel = new ReactiveData<int>(0, "UserVipLevel");

        #endregion

        #region 响应式数据 - 游戏相关

        [Header("游戏状态")]
        [Tooltip("当前游戏阶段")]
        public ReactiveData<GamePhase> CurrentGamePhase = new ReactiveData<GamePhase>(GamePhase.Waiting, "CurrentGamePhase");
        
        [Tooltip("游戏局号")]
        public ReactiveData<string> GameNumber = new ReactiveData<string>("", "GameNumber");
        
        [Tooltip("倒计时")]
        public ReactiveData<int> Countdown = new ReactiveData<int>(0, "Countdown");
        
        [Tooltip("游戏轮次")]
        public ReactiveData<int> GameRound = new ReactiveData<int>(1, "GameRound");

        #endregion

        #region 响应式数据 - 台桌相关

        [Header("台桌信息")]
        [Tooltip("台桌信息")]
        public ReactiveData<TableInfo> TableInfo = new ReactiveData<TableInfo>("TableInfo");
        
        [Tooltip("台桌状态")]
        public ReactiveData<TableStatus> TableStatus = new ReactiveData<TableStatus>(Data.Types.TableStatus.Open, "TableStatus");
        
        [Tooltip("当前视频URL")]
        public ReactiveData<string> CurrentVideoUrl = new ReactiveData<string>("", "CurrentVideoUrl");
        
        [Tooltip("视频模式")]
        public ReactiveData<VideoMode> CurrentVideoMode = new ReactiveData<VideoMode>(VideoMode.Far, "CurrentVideoMode");

        #endregion

        #region 响应式数据 - 投注相关

        [Header("投注数据")]
        [Tooltip("当前选中筹码")]
        public ReactiveData<ChipData> CurrentChip = new ReactiveData<ChipData>("CurrentChip");
        
        [Tooltip("总投注金额")]
        public ReactiveData<float> TotalBetAmount = new ReactiveData<float>(0f, "TotalBetAmount");
        
        [Tooltip("已确认投注金额")]
        public ReactiveData<float> ConfirmedBetAmount = new ReactiveData<float>(0f, "ConfirmedBetAmount");
        
        [Tooltip("投注阶段")]
        public ReactiveData<BettingPhase> BettingPhase = new ReactiveData<BettingPhase>(Data.Types.BettingPhase.Waiting, "BettingPhase");
        
        [Tooltip("是否可以投注")]
        public ReactiveData<bool> CanPlaceBet = new ReactiveData<bool>(false, "CanPlaceBet");

        #endregion

        #region 响应式数据 - 网络相关

        [Header("网络状态")]
        [Tooltip("WebSocket连接状态")]
        public ReactiveData<WSConnectionStatus> WSConnectionStatus = new ReactiveData<WSConnectionStatus>(Data.Types.WSConnectionStatus.Disconnected, "WSConnectionStatus");
        
        [Tooltip("是否已连接")]
        public ReactiveData<bool> IsConnected = new ReactiveData<bool>(false, "IsConnected");
        
        [Tooltip("重连次数")]
        public ReactiveData<int> ReconnectAttempts = new ReactiveData<int>(0, "ReconnectAttempts");
        
        [Tooltip("最后错误信息")]
        public ReactiveData<string> LastError = new ReactiveData<string>("", "LastError");

        #endregion

        #region 响应式数据 - UI相关

        [Header("UI状态")]
        [Tooltip("是否显示加载")]
        public ReactiveData<bool> IsLoading = new ReactiveData<bool>(false, "IsLoading");
        
        [Tooltip("是否显示中奖弹窗")]
        public ReactiveData<bool> ShowWinningPopup = new ReactiveData<bool>(false, "ShowWinningPopup");
        
        [Tooltip("中奖金额")]
        public ReactiveData<float> WinningAmount = new ReactiveData<float>(0f, "WinningAmount");
        
        [Tooltip("当前界面")]
        public ReactiveData<string> CurrentView = new ReactiveData<string>("MainMenu", "CurrentView");

        #endregion

        #region 响应式数据 - 设置相关

        [Header("设置数据")]
        [Tooltip("音效开关")]
        public ReactiveData<bool> SoundEnabled = new ReactiveData<bool>(true, "SoundEnabled");
        
        [Tooltip("音乐开关")]
        public ReactiveData<bool> MusicEnabled = new ReactiveData<bool>(true, "MusicEnabled");
        
        [Tooltip("震动开关")]
        public ReactiveData<bool> VibrationEnabled = new ReactiveData<bool>(true, "VibrationEnabled");
        
        [Tooltip("免佣设置")]
        public ReactiveData<bool> ExemptEnabled = new ReactiveData<bool>(false, "ExemptEnabled");

        #endregion

        /// <summary>
        /// 单例实例
        /// </summary>
        public static GameDataStore Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameDataStore>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GameDataStore");
                        _instance = go.AddComponent<GameDataStore>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => isInitialized;

        #region Unity生命周期

        private void Awake()
        {
            InitializeSingleton();
        }

        private void Start()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            SaveData();
            ClearAllSubscribers();
        }

        #endregion

        #region 单例初始化

        /// <summary>
        /// 初始化单例
        /// </summary>
        private void InitializeSingleton()
        {
            if (_instance == null)
            {
                _instance = this;
                
                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
                
                LogDebug("GameDataStore 单例已初始化");
            }
            else if (_instance != this)
            {
                LogDebug("GameDataStore 单例已存在，销毁重复实例");
                Destroy(gameObject);
            }
        }

        #endregion

        #region 数据存储初始化

        /// <summary>
        /// 初始化数据存储
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                LogWarning("GameDataStore 已经初始化过了");
                return;
            }

            LogDebug("开始初始化 GameDataStore");

            try
            {
                // 设置调试名称（如果还没有设置）
                SetupDebugNames();
                
                // 设置数据绑定
                SetupDataBindings();
                
                // 加载持久化数据
                if (enablePersistence)
                {
                    LoadData();
                }
                
                // 更新统计信息
                UpdateStatistics();
                
                isInitialized = true;
                LogDebug("GameDataStore 初始化完成");
            }
            catch (Exception e)
            {
                LogError($"GameDataStore 初始化失败: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 设置调试名称
        /// </summary>
        private void SetupDebugNames()
        {
            // 确保所有响应式数据都有正确的调试名称
            var properties = GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                if (property.FieldType.IsGenericType && 
                    property.FieldType.GetGenericTypeDefinition() == typeof(ReactiveData<>))
                {
                    var reactiveData = property.GetValue(this);
                    if (reactiveData != null)
                    {
                        var debugNameProperty = property.FieldType.GetProperty("DebugName");
                        if (debugNameProperty != null)
                        {
                            string currentName = debugNameProperty.GetValue(reactiveData) as string;
                            if (string.IsNullOrEmpty(currentName))
                            {
                                debugNameProperty.SetValue(reactiveData, property.Name);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 设置数据绑定
        /// </summary>
        private void SetupDataBindings()
        {
            // 设置连接状态绑定
            WSConnectionStatus.OnValueChanged += (status) =>
            {
                IsConnected.Value = status == Data.Types.WSConnectionStatus.Connected;
            };

            // 设置用户信息绑定
            UserInfo.OnValueChanged += (userInfo) =>
            {
                if (userInfo != null)
                {
                    UserBalance.SetValueSilent(userInfo.balance);
                    UserLevel.SetValueSilent(userInfo.level);
                    UserVipLevel.SetValueSilent(userInfo.vip_level);
                }
            };

            // 设置投注能力绑定
            var updateCanPlaceBet = new Action(() =>
            {
                bool canBet = CurrentGamePhase.Value == GamePhase.Betting && 
                             IsConnected.Value && 
                             Countdown.Value > 0 &&
                             BettingPhase.Value != Data.Types.BettingPhase.Dealing;
                CanPlaceBet.Value = canBet;
            });

            CurrentGamePhase.OnValueChanged += _ => updateCanPlaceBet();
            IsConnected.OnValueChanged += _ => updateCanPlaceBet();
            Countdown.OnValueChanged += _ => updateCanPlaceBet();
            BettingPhase.OnValueChanged += _ => updateCanPlaceBet();

            LogDebug("数据绑定设置完成");
        }

        #endregion

        #region 数据操作方法

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="userInfo">用户信息</param>
        public void UpdateUserInfo(UserInfo userInfo)
        {
            if (userInfo != null)
            {
                UserInfo.Value = userInfo;
                LogDebug($"用户信息已更新: {userInfo.GetDisplayName()}");
            }
        }

        /// <summary>
        /// 更新用户余额
        /// </summary>
        /// <param name="newBalance">新余额</param>
        public void UpdateUserBalance(float newBalance)
        {
            UserBalance.Value = newBalance;
            
            // 同时更新用户信息中的余额
            if (UserInfo.HasValue)
            {
                var userInfo = UserInfo.Value.Clone();
                userInfo.UpdateBalance(newBalance);
                UserInfo.SetValueSilent(userInfo);
            }
            
            LogDebug($"用户余额已更新: {newBalance}");
        }

        /// <summary>
        /// 更新台桌信息
        /// </summary>
        /// <param name="tableInfo">台桌信息</param>
        public void UpdateTableInfo(TableInfo tableInfo)
        {
            if (tableInfo != null)
            {
                TableInfo.Value = tableInfo;
                TableStatus.Value = tableInfo.table_status;
                CurrentVideoUrl.Value = tableInfo.GetCurrentVideoUrl();
                CurrentVideoMode.Value = tableInfo.current_video_mode;
                
                LogDebug($"台桌信息已更新: {tableInfo.table_title}");
            }
        }

        /// <summary>
        /// 更新游戏状态
        /// </summary>
        /// <param name="gamePhase">游戏阶段</param>
        /// <param name="countdown">倒计时</param>
        /// <param name="gameNumber">游戏局号</param>
        public void UpdateGameState(GamePhase gamePhase, int countdown = -1, string gameNumber = null)
        {
            CurrentGamePhase.Value = gamePhase;
            
            if (countdown >= 0)
            {
                Countdown.Value = countdown;
            }
            
            if (!string.IsNullOrEmpty(gameNumber))
            {
                GameNumber.Value = gameNumber;
            }
            
            LogDebug($"游戏状态已更新: {gamePhase}, 倒计时: {countdown}, 局号: {gameNumber}");
        }

        /// <summary>
        /// 更新网络连接状态
        /// </summary>
        /// <param name="connectionStatus">连接状态</param>
        /// <param name="errorMessage">错误信息</param>
        public void UpdateConnectionStatus(WSConnectionStatus connectionStatus, string errorMessage = "")
        {
            WSConnectionStatus.Value = connectionStatus;
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                LastError.Value = errorMessage;
            }
            
            LogDebug($"连接状态已更新: {connectionStatus}");
        }

        /// <summary>
        /// 显示中奖信息
        /// </summary>
        /// <param name="amount">中奖金额</param>
        public void ShowWinning(float amount)
        {
            if (amount > 0)
            {
                WinningAmount.Value = amount;
                ShowWinningPopup.Value = true;
                LogDebug($"显示中奖: {amount}");
            }
        }

        /// <summary>
        /// 隐藏中奖弹窗
        /// </summary>
        public void HideWinning()
        {
            ShowWinningPopup.Value = false;
            WinningAmount.Value = 0f;
        }

        /// <summary>
        /// 切换视频模式
        /// </summary>
        public void SwitchVideoMode()
        {
            var newMode = CurrentVideoMode.Value == VideoMode.Far ? VideoMode.Near : VideoMode.Far;
            CurrentVideoMode.Value = newMode;
            
            // 如果有台桌信息，更新视频URL
            if (TableInfo.HasValue)
            {
                var tableInfo = TableInfo.Value;
                tableInfo.SetVideoMode(newMode);
                CurrentVideoUrl.Value = tableInfo.GetCurrentVideoUrl();
            }
            
            LogDebug($"视频模式已切换: {newMode}");
        }

        #endregion

        #region 数据持久化

        /// <summary>
        /// 保存数据到本地存储
        /// </summary>
        public void SaveData()
        {
            if (!enablePersistence) return;

            try
            {
                // 保存用户设置
                SaveUserSettings();
                
                // 保存游戏状态（可选）
                SaveGameState();
                
                LogDebug("数据已保存到本地存储");
            }
            catch (Exception e)
            {
                LogError($"保存数据失败: {e.Message}");
            }
        }

        /// <summary>
        /// 从本地存储加载数据
        /// </summary>
        public void LoadData()
        {
            if (!enablePersistence) return;

            try
            {
                // 加载用户设置
                LoadUserSettings();
                
                // 加载游戏状态（可选）
                LoadGameState();
                
                LogDebug("数据已从本地存储加载");
            }
            catch (Exception e)
            {
                LogError($"加载数据失败: {e.Message}");
            }
        }

        /// <summary>
        /// 保存用户设置
        /// </summary>
        private void SaveUserSettings()
        {
            PlayerPrefs.SetInt("SoundEnabled", SoundEnabled.Value ? 1 : 0);
            PlayerPrefs.SetInt("MusicEnabled", MusicEnabled.Value ? 1 : 0);
            PlayerPrefs.SetInt("VibrationEnabled", VibrationEnabled.Value ? 1 : 0);
            PlayerPrefs.SetInt("ExemptEnabled", ExemptEnabled.Value ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 加载用户设置
        /// </summary>
        private void LoadUserSettings()
        {
            SoundEnabled.SetValueSilent(PlayerPrefs.GetInt("SoundEnabled", 1) == 1);
            MusicEnabled.SetValueSilent(PlayerPrefs.GetInt("MusicEnabled", 1) == 1);
            VibrationEnabled.SetValueSilent(PlayerPrefs.GetInt("VibrationEnabled", 1) == 1);
            ExemptEnabled.SetValueSilent(PlayerPrefs.GetInt("ExemptEnabled", 0) == 1);
        }

        /// <summary>
        /// 保存游戏状态
        /// </summary>
        private void SaveGameState()
        {
            // 保存一些基础的游戏状态
            PlayerPrefs.SetString("LastGameNumber", GameNumber.Value ?? "");
            PlayerPrefs.SetInt("GameRound", GameRound.Value);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 加载游戏状态
        /// </summary>
        private void LoadGameState()
        {
            // 加载基础的游戏状态
            string lastGameNumber = PlayerPrefs.GetString("LastGameNumber", "");
            if (!string.IsNullOrEmpty(lastGameNumber))
            {
                GameNumber.SetValueSilent(lastGameNumber);
            }
            
            int gameRound = PlayerPrefs.GetInt("GameRound", 1);
            GameRound.SetValueSilent(gameRound);
        }

        #endregion

        #region 统计和监控

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            reactiveDataCount = 0;
            subscriberCount = 0;

            var properties = GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                if (property.FieldType.IsGenericType && 
                    property.FieldType.GetGenericTypeDefinition() == typeof(ReactiveData<>))
                {
                    reactiveDataCount++;
                    
                    var reactiveData = property.GetValue(this);
                    if (reactiveData != null)
                    {
                        var subscriberCountProperty = property.FieldType.GetProperty("SubscriberCount");
                        if (subscriberCountProperty != null)
                        {
                            int count = (int)subscriberCountProperty.GetValue(reactiveData);
                            subscriberCount += count;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取数据存储状态信息
        /// </summary>
        /// <returns>状态信息字符串</returns>
        public string GetStatusInfo()
        {
            UpdateStatistics();
            
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== GameDataStore 状态信息 ===");
            info.AppendLine($"初始化状态: {(isInitialized ? "已初始化" : "未初始化")}");
            info.AppendLine($"响应式数据数量: {reactiveDataCount}");
            info.AppendLine($"总订阅者数量: {subscriberCount}");
            info.AppendLine($"持久化: {(enablePersistence ? "启用" : "禁用")}");
            
            info.AppendLine("\n=== 核心状态 ===");
            info.AppendLine($"用户余额: {UserBalance.Value:F2}");
            info.AppendLine($"游戏阶段: {CurrentGamePhase.Value}");
            info.AppendLine($"连接状态: {WSConnectionStatus.Value}");
            info.AppendLine($"投注阶段: {BettingPhase.Value}");
            info.AppendLine($"倒计时: {Countdown.Value}");
            
            if (!string.IsNullOrEmpty(GameNumber.Value))
            {
                info.AppendLine($"当前局号: {GameNumber.Value}");
            }
            
            if (!string.IsNullOrEmpty(LastError.Value))
            {
                info.AppendLine($"最后错误: {LastError.Value}");
            }
            
            return info.ToString();
        }

        /// <summary>
        /// 获取所有响应式数据的调试信息
        /// </summary>
        /// <returns>调试信息列表</returns>
        public List<string> GetAllReactiveDataDebugInfo()
        {
            var debugInfoList = new List<string>();
            var properties = GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                if (property.FieldType.IsGenericType && 
                    property.FieldType.GetGenericTypeDefinition() == typeof(ReactiveData<>))
                {
                    var reactiveData = property.GetValue(this);
                    if (reactiveData != null)
                    {
                        var getDebugInfoMethod = property.FieldType.GetMethod("GetDebugInfo");
                        if (getDebugInfoMethod != null)
                        {
                            string debugInfo = getDebugInfoMethod.Invoke(reactiveData, null) as string;
                            debugInfoList.Add(debugInfo);
                        }
                    }
                }
            }
            
            return debugInfoList;
        }

        #endregion

        #region 清理和重置

        /// <summary>
        /// 清除所有订阅者
        /// </summary>
        public void ClearAllSubscribers()
        {
            var properties = GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                if (property.FieldType.IsGenericType && 
                    property.FieldType.GetGenericTypeDefinition() == typeof(ReactiveData<>))
                {
                    var reactiveData = property.GetValue(this);
                    if (reactiveData != null)
                    {
                        var clearMethod = property.FieldType.GetMethod("ClearAllSubscribers");
                        clearMethod?.Invoke(reactiveData, null);
                    }
                }
            }
            
            LogDebug("已清除所有响应式数据的订阅者");
        }

        /// <summary>
        /// 重置游戏数据（保留用户设置）
        /// </summary>
        public void ResetGameData()
        {
            LogDebug("重置游戏数据");
            
            // 重置游戏状态
            CurrentGamePhase.Value = GamePhase.Waiting;
            GameNumber.Value = "";
            Countdown.Value = 0;
            GameRound.Value = 1;
            
            // 重置投注数据
            TotalBetAmount.Value = 0f;
            ConfirmedBetAmount.Value = 0f;
            BettingPhase.Value = Data.Types.BettingPhase.Waiting;
            CurrentChip.Value = null;
            
            // 重置UI状态
            IsLoading.Value = false;
            ShowWinningPopup.Value = false;
            WinningAmount.Value = 0f;
            
            // 重置网络状态
            WSConnectionStatus.Value = Data.Types.WSConnectionStatus.Disconnected;
            ReconnectAttempts.Value = 0;
            LastError.Value = "";
            
            LogDebug("游戏数据重置完成");
        }

        /// <summary>
        /// 重置所有数据（包括用户设置）
        /// </summary>
        public void ResetAllData()
        {
            LogDebug("重置所有数据");
            
            // 重置游戏数据
            ResetGameData();
            
            // 重置用户数据
            UserInfo.Value = null;
            UserBalance.Value = 10000f;
            UserLevel.Value = 1;
            UserVipLevel.Value = 0;
            
            // 重置台桌数据
            TableInfo.Value = null;
            TableStatus.Value = Data.Types.TableStatus.Open;
            CurrentVideoUrl.Value = "";
            CurrentVideoMode.Value = VideoMode.Far;
            
            // 重置设置数据
            SoundEnabled.Value = true;
            MusicEnabled.Value = true;
            VibrationEnabled.Value = true;
            ExemptEnabled.Value = false;
            
            // 清除本地存储
            if (enablePersistence)
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
            }
            
            LogDebug("所有数据重置完成");
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 调试日志
        /// </summary>
        /// <param name="message">消息</param>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[GameDataStore] {message}");
            }
        }

        /// <summary>
        /// 警告日志
        /// </summary>
        /// <param name="message">消息</param>
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[GameDataStore] {message}");
        }

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="message">消息</param>
        private void LogError(string message)
        {
            Debug.LogError($"[GameDataStore] {message}");
        }

        #endregion

        #region 静态便捷方法

        /// <summary>
        /// 静态获取用户余额
        /// </summary>
        /// <returns>用户余额</returns>
        public static float GetUserBalance()
        {
            return Instance.UserBalance.Value;
        }

        /// <summary>
        /// 静态更新用户余额
        /// </summary>
        /// <param name="newBalance">新余额</param>
        public static void SetUserBalance(float newBalance)
        {
            Instance.UpdateUserBalance(newBalance);
        }

        /// <summary>
        /// 静态获取连接状态
        /// </summary>
        /// <returns>是否已连接</returns>
        public static bool GetConnectionStatus()
        {
            return Instance.IsConnected.Value;
        }

        /// <summary>
        /// 静态获取游戏阶段
        /// </summary>
        /// <returns>当前游戏阶段</returns>
        public static GamePhase GetGamePhase()
        {
            return Instance.CurrentGamePhase.Value;
        }

        /// <summary>
        /// 静态检查是否可以投注
        /// </summary>
        /// <returns>是否可以投注</returns>
        public static bool CanBet()
        {
            return Instance.CanPlaceBet.Value;
        }

        #endregion

        #region Editor工具方法

#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中显示所有响应式数据状态
        /// </summary>
        [ContextMenu("显示所有响应式数据状态")]
        public void ShowAllReactiveDataStatus()
        {
            var debugInfoList = GetAllReactiveDataDebugInfo();
            
            Debug.Log("=== 所有响应式数据状态 ===");
            foreach (var info in debugInfoList)
            {
                Debug.Log(info);
            }
        }

        /// <summary>
        /// 强制保存数据
        /// </summary>
        [ContextMenu("强制保存数据")]
        public void ForceSaveData()
        {
            SaveData();
            Debug.Log("数据已强制保存");
        }

        /// <summary>
        /// 强制加载数据
        /// </summary>
        [ContextMenu("强制加载数据")]
        public void ForceLoadData()
        {
            LoadData();
            Debug.Log("数据已强制加载");
        }

        /// <summary>
        /// 测试所有响应式数据
        /// </summary>
        [ContextMenu("测试所有响应式数据")]
        public void TestAllReactiveData()
        {
            Debug.Log("开始测试所有响应式数据...");
            
            // 触发所有数据的变化来测试响应性
            UserBalance.ForceNotify();
            CurrentGamePhase.ForceNotify();
            WSConnectionStatus.ForceNotify();
            BettingPhase.ForceNotify();
            
            Debug.Log("响应式数据测试完成");
        }
#endif

        #endregion
    }

    /// <summary>
    /// 游戏数据存储扩展方法
    /// </summary>
    public static class GameDataStoreExtensions
    {
        /// <summary>
        /// 订阅用户余额变化
        /// </summary>
        /// <param name="store">数据存储</param>
        /// <param name="callback">回调函数</param>
        /// <returns>取消订阅的Action</returns>
        public static System.Action SubscribeToBalanceChanges(this GameDataStore store, System.Action<float> callback)
        {
            store.UserBalance.OnValueChanged += callback;
            return () => store.UserBalance.OnValueChanged -= callback;
        }

        /// <summary>
        /// 订阅游戏阶段变化
        /// </summary>
        /// <param name="store">数据存储</param>
        /// <param name="callback">回调函数</param>
        /// <returns>取消订阅的Action</returns>
        public static System.Action SubscribeToGamePhaseChanges(this GameDataStore store, System.Action<GamePhase> callback)
        {
            store.CurrentGamePhase.OnValueChanged += callback;
            return () => store.CurrentGamePhase.OnValueChanged -= callback;
        }

        /// <summary>
        /// 订阅连接状态变化
        /// </summary>
        /// <param name="store">数据存储</param>
        /// <param name="callback">回调函数</param>
        /// <returns>取消订阅的Action</returns>
        public static System.Action SubscribeToConnectionChanges(this GameDataStore store, System.Action<bool> callback)
        {
            store.IsConnected.OnValueChanged += callback;
            return () => store.IsConnected.OnValueChanged -= callback;
        }

        /// <summary>
        /// 批量更新游戏状态
        /// </summary>
        /// <param name="store">数据存储</param>
        /// <param name="gamePhase">游戏阶段</param>
        /// <param name="countdown">倒计时</param>
        /// <param name="gameNumber">游戏局号</param>
        /// <param name="bettingPhase">投注阶段</param>
        public static void UpdateGameStateBatch(this GameDataStore store, 
            GamePhase? gamePhase = null, 
            int? countdown = null, 
            string gameNumber = null, 
            BettingPhase? bettingPhase = null)
        {
            if (gamePhase.HasValue)
                store.CurrentGamePhase.Value = gamePhase.Value;
                
            if (countdown.HasValue)
                store.Countdown.Value = countdown.Value;
                
            if (!string.IsNullOrEmpty(gameNumber))
                store.GameNumber.Value = gameNumber;
                
            if (bettingPhase.HasValue)
                store.BettingPhase.Value = bettingPhase.Value;
        }
    }
}