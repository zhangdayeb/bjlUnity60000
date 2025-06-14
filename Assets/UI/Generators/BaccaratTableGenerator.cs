// Assets/UI/Generators/BaccaratTableGenerator.cs
// 百家乐桌台生成器 - 动态生成完整的百家乐游戏界面
// 包括投注区域、筹码显示、视频区域等核心功能模块

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI.Framework;
using Core.Data.Types;
using Core.Architecture;

namespace UI.Generators
{
    /// <summary>
    /// 百家乐桌台生成器
    /// 负责生成完整的百家乐游戏界面，包括所有投注区域和功能组件
    /// </summary>
    public class BaccaratTableGenerator : UIGeneratorBase
    {
        #region 配置数据

        [Header("百家乐桌台配置")]
        [Tooltip("桌台主题")]
        [SerializeField] private BaccaratTableTheme _tableTheme = BaccaratTableTheme.Classic;
        
        [Tooltip("是否生成视频区域")]
        [SerializeField] private bool _generateVideoArea = true;
        
        [Tooltip("是否生成路纸区域")]
        [SerializeField] private bool _generateRoadmapArea = true;
        
        [Tooltip("是否生成统计区域")]
        [SerializeField] private bool _generateStatisticsArea = true;

        [Header("投注区域配置")]
        [Tooltip("投注区域布局")]
        [SerializeField] private BettingAreaLayout _bettingLayout = new BettingAreaLayout();
        
        [Tooltip("投注区域颜色配置")]
        [SerializeField] private BettingAreaColors _areaColors = new BettingAreaColors();

        [Header("筹码配置")]
        [Tooltip("筹码数据")]
        [SerializeField] private ChipConfiguration _chipConfig = new ChipConfiguration();

        [Header("视频配置")]
        [Tooltip("视频区域配置")]
        [SerializeField] private VideoAreaConfig _videoConfig = new VideoAreaConfig();

        [Header("路纸配置")]
        [Tooltip("路纸显示配置")]
        [SerializeField] private RoadmapConfig _roadmapConfig = new RoadmapConfig();

        // 生成的UI组件引用
        private Dictionary<BaccaratBetType, BaccaratBetArea> _betAreas = new Dictionary<BaccaratBetType, BaccaratBetArea>();
        private ChipSelectionArea _chipArea;
        private VideoDisplayArea _videoArea;
        private RoadmapDisplayArea _roadmapArea;
        private StatisticsPanel _statisticsPanel;
        private GameInfoPanel _gameInfoPanel;
        private ControlPanel _controlPanel;

        // 响应式数据绑定
        private Dictionary<string, System.Action> _dataBindings = new Dictionary<string, System.Action>();

        #endregion

        #region 重写基类方法

        protected override List<string> GetSupportedGenerationTypes()
        {
            return new List<string>
            {
                "complete_table",    // 完整桌台
                "betting_areas",     // 投注区域
                "chip_area",         // 筹码区域
                "video_area",        // 视频区域
                "roadmap_area",      // 路纸区域
                "statistics_area",   // 统计区域
                "control_panel"      // 控制面板
            };
        }

        public override void GenerateUI()
        {
            GenerateCompleteTable();
        }

        public override void ClearUI()
        {
            ClearAllAreas();
        }

        protected override void OnGenerateSpecificUI(string generationType, Dictionary<string, object> parameters)
        {
            switch (generationType)
            {
                case "complete_table":
                    GenerateCompleteTable();
                    break;
                case "betting_areas":
                    GenerateBettingAreas();
                    break;
                case "chip_area":
                    GenerateChipArea();
                    break;
                case "video_area":
                    GenerateVideoArea();
                    break;
                case "roadmap_area":
                    GenerateRoadmapArea();
                    break;
                case "statistics_area":
                    GenerateStatisticsArea();
                    break;
                case "control_panel":
                    GenerateControlPanel();
                    break;
                default:
                    base.OnGenerateSpecificUI(generationType, parameters);
                    break;
            }
        }

        #endregion

        #region 主要生成方法

        /// <summary>
        /// 生成完整的百家乐桌台
        /// </summary>
        private void GenerateCompleteTable()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[BaccaratTableGenerator] 开始生成完整百家乐桌台");
            }

            // 清理现有UI
            ClearAllAreas();

            // 创建主容器
            CreateMainTableContainer();

            // 按顺序生成各个区域
            GenerateBettingAreas();
            GenerateChipArea();
            
            if (_generateVideoArea)
                GenerateVideoArea();
            
            if (_generateRoadmapArea)
                GenerateRoadmapArea();
            
            if (_generateStatisticsArea)
                GenerateStatisticsArea();
            
            GenerateGameInfoPanel();
            GenerateControlPanel();

            // 设置响应式绑定
            SetupReactiveBindings();
            
            // 应用主题
            ApplyTableTheme();

            if (_enableDebugMode)
            {
                Debug.Log("[BaccaratTableGenerator] 完整百家乐桌台生成完成");
            }
        }

        /// <summary>
        /// 生成投注区域（庄、闲、和、对子）
        /// </summary>
        public void GenerateBettingAreas()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[BaccaratTableGenerator] 生成投注区域");
            }

            // 创建投注区域容器
            GameObject bettingContainer = CreateUIObject("BettingAreaContainer", _rootContainer, "betting_areas");
            if (bettingContainer == null)
            {
                bettingContainer = CreateUIComponent<RectTransform>("BettingAreasContainer", _rootContainer, "betting_areas").gameObject;
            }

            RectTransform bettingRect = bettingContainer.GetComponent<RectTransform>();
            SetRectTransform(bettingRect, _bettingLayout.containerAnchor, _bettingLayout.containerPosition, _bettingLayout.containerSize);

            // 应用网格布局
            ApplyLayoutGroup(bettingRect, LayoutType.Grid, _bettingLayout.areaSpacing);

            // 生成主要投注区域
            GenerateBankerArea(bettingRect);
            GeneratePlayerArea(bettingRect);
            GenerateTieArea(bettingRect);

            // 生成对子投注区域
            GeneratePairAreas(bettingRect);

            // 生成大小投注区域
            GenerateBigSmallAreas(bettingRect);
        }

        /// <summary>
        /// 生成筹码选择区域
        /// </summary>
        public void GenerateChipArea()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[BaccaratTableGenerator] 生成筹码区域");
            }

            GameObject chipContainer = CreateUIObject("ChipAreaContainer", _rootContainer, "chip_area");
            if (chipContainer == null)
            {
                chipContainer = CreateUIComponent<RectTransform>("ChipAreaContainer", _rootContainer, "chip_area").gameObject;
            }

            RectTransform chipRect = chipContainer.GetComponent<RectTransform>();
            SetRectTransform(chipRect, _chipConfig.containerAnchor, _chipConfig.containerPosition, _chipConfig.containerSize);

            // 添加筹码选择区域组件
            _chipArea = chipContainer.AddComponent<ChipSelectionArea>();
            _chipArea.Initialize(_chipConfig);

            // 生成筹码按钮
            GenerateChipButtons(chipRect);
            
            // 生成当前筹码显示
            GenerateCurrentChipDisplay(chipRect);
            
            // 生成筹码堆叠显示区域
            GenerateChipStackDisplay(chipRect);
        }

        /// <summary>
        /// 生成视频显示区域
        /// </summary>
        public void GenerateVideoArea()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[BaccaratTableGenerator] 生成视频区域");
            }

            GameObject videoContainer = CreateUIObject("VideoAreaContainer", _rootContainer, "video_area");
            if (videoContainer == null)
            {
                videoContainer = CreateUIComponent<RectTransform>("VideoAreaContainer", _rootContainer, "video_area").gameObject;
            }

            RectTransform videoRect = videoContainer.GetComponent<RectTransform>();
            SetRectTransform(videoRect, _videoConfig.containerAnchor, _videoConfig.containerPosition, _videoConfig.containerSize);

            // 添加视频显示组件
            _videoArea = videoContainer.AddComponent<VideoDisplayArea>();
            _videoArea.Initialize(_videoConfig);

            // 生成视频播放器
            GenerateVideoPlayer(videoRect);
            
            // 生成视频控制按钮
            GenerateVideoControls(videoRect);
            
            // 生成扑克牌显示区域
            GenerateCardDisplayArea(videoRect);
        }

        /// <summary>
        /// 生成路纸显示区域
        /// </summary>
        private void GenerateRoadmapArea()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[BaccaratTableGenerator] 生成路纸区域");
            }

            GameObject roadmapContainer = CreateUIObject("RoadmapContainer", _rootContainer, "roadmap_area");
            if (roadmapContainer == null)
            {
                roadmapContainer = CreateUIComponent<RectTransform>("RoadmapContainer", _rootContainer, "roadmap_area").gameObject;
            }

            RectTransform roadmapRect = roadmapContainer.GetComponent<RectTransform>();
            SetRectTransform(roadmapRect, _roadmapConfig.containerAnchor, _roadmapConfig.containerPosition, _roadmapConfig.containerSize);

            // 添加路纸显示组件
            _roadmapArea = roadmapContainer.AddComponent<RoadmapDisplayArea>();
            _roadmapArea.Initialize(_roadmapConfig);

            // 生成各种路纸
            GenerateMainRoad(roadmapRect);
            GenerateBigEyeRoad(roadmapRect);
            GenerateSmallRoad(roadmapRect);
            GenerateCockroachRoad(roadmapRect);
        }

        /// <summary>
        /// 生成统计面板
        /// </summary>
        private void GenerateStatisticsArea()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[BaccaratTableGenerator] 生成统计区域");
            }

            GameObject statsContainer = CreateUIObject("StatisticsContainer", _rootContainer, "statistics_area");
            if (statsContainer == null)
            {
                statsContainer = CreateUIComponent<RectTransform>("StatisticsContainer", _rootContainer, "statistics_area").gameObject;
            }

            RectTransform statsRect = statsContainer.GetComponent<RectTransform>();
            // 统计面板通常在右侧
            SetRectTransform(statsRect, new AnchorSettings { min = new Vector2(0.8f, 0), max = new Vector2(1, 1) }, Vector2.zero, Vector2.zero);

            // 添加统计面板组件
            _statisticsPanel = statsContainer.AddComponent<StatisticsPanel>();

            // 生成各种统计显示
            GenerateBankerPlayerStats(statsRect);
            GeneratePairStats(statsRect);
            GenerateWinRateDisplay(statsRect);
            GenerateTrendAnalysis(statsRect);
        }

        #endregion

        #region 详细生成方法

        /// <summary>
        /// 创建主桌台容器
        /// </summary>
        private void CreateMainTableContainer()
        {
            // 确保根容器有正确的设置
            if (_rootContainer != null)
            {
                // 添加背景图片
                Image bgImage = _rootContainer.gameObject.GetComponent<Image>();
                if (bgImage == null)
                {
                    bgImage = _rootContainer.gameObject.AddComponent<Image>();
                }
                
                // 设置桌台背景
                var bgSprite = GetTableBackgroundSprite();
                if (bgSprite != null)
                {
                    bgImage.sprite = bgSprite;
                }
            }
        }

        /// <summary>
        /// 生成庄家投注区域
        /// </summary>
        private void GenerateBankerArea(RectTransform parent)
        {
            var bankerArea = CreateBetArea(BaccaratBetType.Banker, "庄", _areaColors.bankerColor, parent);
            _betAreas[BaccaratBetType.Banker] = bankerArea;
        }

        /// <summary>
        /// 生成闲家投注区域
        /// </summary>
        private void GeneratePlayerArea(RectTransform parent)
        {
            var playerArea = CreateBetArea(BaccaratBetType.Player, "闲", _areaColors.playerColor, parent);
            _betAreas[BaccaratBetType.Player] = playerArea;
        }

        /// <summary>
        /// 生成和局投注区域
        /// </summary>
        private void GenerateTieArea(RectTransform parent)
        {
            var tieArea = CreateBetArea(BaccaratBetType.Tie, "和", _areaColors.tieColor, parent);
            _betAreas[BaccaratBetType.Tie] = tieArea;
        }

        /// <summary>
        /// 生成对子投注区域
        /// </summary>
        private void GeneratePairAreas(RectTransform parent)
        {
            var bankerPairArea = CreateBetArea(BaccaratBetType.BankerPair, "庄对", _areaColors.pairColor, parent);
            var playerPairArea = CreateBetArea(BaccaratBetType.PlayerPair, "闲对", _areaColors.pairColor, parent);
            
            _betAreas[BaccaratBetType.BankerPair] = bankerPairArea;
            _betAreas[BaccaratBetType.PlayerPair] = playerPairArea;
        }

        /// <summary>
        /// 生成大小投注区域
        /// </summary>
        private void GenerateBigSmallAreas(RectTransform parent)
        {
            var bigArea = CreateBetArea(BaccaratBetType.BigBig, "大", _areaColors.bigColor, parent);
            var smallArea = CreateBetArea(BaccaratBetType.SmallSmall, "小", _areaColors.smallColor, parent);
            
            _betAreas[BaccaratBetType.BigBig] = bigArea;
            _betAreas[BaccaratBetType.SmallSmall] = smallArea;
        }

        /// <summary>
        /// 创建投注区域
        /// </summary>
        private BaccaratBetArea CreateBetArea(BaccaratBetType betType, string label, Color color, RectTransform parent)
        {
            GameObject areaObj = CreateUIObject("BetAreaPrefab", parent, "betting_areas");
            if (areaObj == null)
            {
                areaObj = CreateUIComponent<RectTransform>($"BetArea_{betType}", parent, "betting_areas").gameObject;
                
                // 添加背景图片
                Image bgImage = areaObj.AddComponent<Image>();
                bgImage.color = color;
                
                // 添加按钮功能
                Button button = areaObj.AddComponent<Button>();
                
                // 添加文本标签
                GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(areaObj.transform, false);
                
                Text labelText = textObj.GetComponent<Text>();
                labelText.text = label;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.fontSize = _bettingLayout.labelFontSize;
                labelText.color = Color.white;
                
                // 设置文本位置
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }

            // 添加投注区域组件
            BaccaratBetArea betArea = areaObj.GetComponent<BaccaratBetArea>();
            if (betArea == null)
            {
                betArea = areaObj.AddComponent<BaccaratBetArea>();
            }

            // 初始化投注区域
            betArea.Initialize(betType, label, color);

            return betArea;
        }

        /// <summary>
        /// 生成筹码按钮
        /// </summary>
        private void GenerateChipButtons(RectTransform parent)
        {
            GameObject chipButtonContainer = new GameObject("ChipButtons", typeof(RectTransform));
            chipButtonContainer.transform.SetParent(parent, false);
            
            RectTransform buttonRect = chipButtonContainer.GetComponent<RectTransform>();
            ApplyLayoutGroup(buttonRect, LayoutType.Horizontal, Vector2.one * 5f);

            // 生成每个筹码面值的按钮
            foreach (var chipValue in _chipConfig.chipValues)
            {
                CreateChipButton(chipValue, buttonRect);
            }
        }

        /// <summary>
        /// 创建筹码按钮
        /// </summary>
        private void CreateChipButton(ChipData chipData, RectTransform parent)
        {
            GameObject chipButton = CreateUIObject("ChipButtonPrefab", parent, "chip_area");
            if (chipButton == null)
            {
                chipButton = CreateUIComponent<Button>($"ChipButton_{chipData.val}", parent, "chip_area").gameObject;
                
                // 设置按钮图片
                Image buttonImage = chipButton.GetComponent<Image>();
                if (chipData.buttonSprite != null)
                {
                    buttonImage.sprite = chipData.buttonSprite;
                }
                
                // 添加文本显示
                GameObject textObj = new GameObject("ValueText", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(chipButton.transform, false);
                
                Text valueText = textObj.GetComponent<Text>();
                valueText.text = chipData.text;
                valueText.alignment = TextAnchor.MiddleCenter;
                valueText.fontSize = 16;
                valueText.color = Color.white;
            }

            // 添加筹码按钮组件
            ChipButton chipButtonComponent = chipButton.GetComponent<ChipButton>();
            if (chipButtonComponent == null)
            {
                chipButtonComponent = chipButton.AddComponent<ChipButton>();
            }
            
            chipButtonComponent.Initialize(chipData);
        }

        /// <summary>
        /// 生成当前筹码显示
        /// </summary>
        private void GenerateCurrentChipDisplay(RectTransform parent)
        {
            GameObject currentChipObj = CreateUIComponent<Image>("CurrentChipDisplay", parent, "chip_area").gameObject;
            
            // 设置显示位置
            RectTransform chipRect = currentChipObj.GetComponent<RectTransform>();
            SetRectTransform(chipRect, new AnchorSettings { min = new Vector2(0.5f, 0), max = new Vector2(0.5f, 0) }, 
                           Vector2.zero, _chipConfig.currentChipSize);
        }

        /// <summary>
        /// 生成筹码堆叠显示
        /// </summary>
        private void GenerateChipStackDisplay(RectTransform parent)
        {
            GameObject stackContainer = CreateUIComponent<RectTransform>("ChipStackContainer", parent, "chip_area").gameObject;
            
            // 这里可以添加筹码堆叠的视觉效果
            ChipStackDisplay stackDisplay = stackContainer.AddComponent<ChipStackDisplay>();
            stackDisplay.Initialize(_chipConfig);
        }

        #endregion

        #region 视频和其他区域生成

        /// <summary>
        /// 生成视频播放器
        /// </summary>
        private void GenerateVideoPlayer(RectTransform parent)
        {
            GameObject videoPlayer = CreateUIComponent<RawImage>("VideoPlayer", parent, "video_area").gameObject;
            
            RectTransform videoRect = videoPlayer.GetComponent<RectTransform>();
            SetRectTransform(videoRect, new AnchorSettings { min = Vector2.zero, max = Vector2.one }, 
                           Vector2.zero, Vector2.zero);
        }

        /// <summary>
        /// 生成视频控制按钮
        /// </summary>
        private void GenerateVideoControls(RectTransform parent)
        {
            GameObject controlsContainer = CreateUIComponent<RectTransform>("VideoControls", parent, "video_area").gameObject;
            
            // 添加切换按钮等
            CreateVideoControlButton("近景", controlsContainer.transform);
            CreateVideoControlButton("远景", controlsContainer.transform);
        }

        /// <summary>
        /// 创建视频控制按钮
        /// </summary>
        private void CreateVideoControlButton(string label, Transform parent)
        {
            GameObject button = CreateUIComponent<Button>(label + "Button", parent, "video_area").gameObject;
            
            // 添加文本
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(button.transform, false);
            
            Text buttonText = textObj.GetComponent<Text>();
            buttonText.text = label;
            buttonText.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// 生成扑克牌显示区域
        /// </summary>
        private void GenerateCardDisplayArea(RectTransform parent)
        {
            GameObject cardArea = CreateUIComponent<RectTransform>("CardDisplayArea", parent, "video_area").gameObject;
            
            // 添加庄家牌区域
            CreateCardSlots("BankerCards", cardArea.transform, 3);
            
            // 添加闲家牌区域
            CreateCardSlots("PlayerCards", cardArea.transform, 3);
        }

        /// <summary>
        /// 创建牌位
        /// </summary>
        private void CreateCardSlots(string name, Transform parent, int count)
        {
            GameObject slotsContainer = CreateUIComponent<RectTransform>(name, parent, "video_area").gameObject;
            ApplyLayoutGroup(slotsContainer.GetComponent<RectTransform>(), LayoutType.Horizontal, Vector2.one * 5f);
            
            for (int i = 0; i < count; i++)
            {
                GameObject slot = CreateUIComponent<Image>($"CardSlot_{i}", slotsContainer.transform, "video_area").gameObject;
                // 设置牌位样式
            }
        }

        #endregion

        #region 路纸生成

        /// <summary>
        /// 生成大路
        /// </summary>
        private void GenerateMainRoad(RectTransform parent)
        {
            GameObject mainRoad = CreateUIComponent<RectTransform>("MainRoad", parent, "roadmap_area").gameObject;
            
            // 添加网格布局
            GridLayoutGroup grid = mainRoad.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = _roadmapConfig.mainRoadColumns;
            grid.cellSize = _roadmapConfig.roadCellSize;
            grid.spacing = _roadmapConfig.roadSpacing;
        }

        /// <summary>
        /// 生成大眼仔路
        /// </summary>
        private void GenerateBigEyeRoad(RectTransform parent)
        {
            GameObject bigEyeRoad = CreateUIComponent<RectTransform>("BigEyeRoad", parent, "roadmap_area").gameObject;
            // 类似的网格设置，但尺寸更小
        }

        /// <summary>
        /// 生成小路
        /// </summary>
        private void GenerateSmallRoad(RectTransform parent)
        {
            GameObject smallRoad = CreateUIComponent<RectTransform>("SmallRoad", parent, "roadmap_area").gameObject;
            // 小路的网格设置
        }

        /// <summary>
        /// 生成蟑螂路
        /// </summary>
        private void GenerateCockroachRoad(RectTransform parent)
        {
            GameObject cockroachRoad = CreateUIComponent<RectTransform>("CockroachRoad", parent, "roadmap_area").gameObject;
            // 蟑螂路的网格设置
        }

        #endregion

        #region 统计和控制面板

        /// <summary>
        /// 生成庄闲统计
        /// </summary>
        private void GenerateBankerPlayerStats(RectTransform parent)
        {
            GameObject statsContainer = CreateUIComponent<RectTransform>("BankerPlayerStats", parent, "statistics_area").gameObject;
            
            // 添加庄家胜率显示
            CreateStatDisplay("庄家胜率", "0%", statsContainer.transform);
            
            // 添加闲家胜率显示
            CreateStatDisplay("闲家胜率", "0%", statsContainer.transform);
            
            // 添加和局胜率显示
            CreateStatDisplay("和局胜率", "0%", statsContainer.transform);
        }

        /// <summary>
        /// 生成对子统计
        /// </summary>
        private void GeneratePairStats(RectTransform parent)
        {
            GameObject pairStats = CreateUIComponent<RectTransform>("PairStats", parent, "statistics_area").gameObject;
            
            CreateStatDisplay("庄对出现率", "0%", pairStats.transform);
            CreateStatDisplay("闲对出现率", "0%", pairStats.transform);
        }

        /// <summary>
        /// 创建统计显示项
        /// </summary>
        private void CreateStatDisplay(string label, string value, Transform parent)
        {
            GameObject statItem = CreateUIComponent<RectTransform>(label, parent, "statistics_area").gameObject;
            
            // 添加标签文本
            GameObject labelObj = CreateUIComponent<Text>("Label", statItem.transform, "statistics_area").gameObject;
            Text labelText = labelObj.GetComponent<Text>();
            labelText.text = label;
            labelText.fontSize = 14;
            
            // 添加数值文本
            GameObject valueObj = CreateUIComponent<Text>("Value", statItem.transform, "statistics_area").gameObject;
            Text valueText = valueObj.GetComponent<Text>();
            valueText.text = value;
            valueText.fontSize = 16;
            valueText.color = Color.yellow;
        }

        /// <summary>
        /// 生成胜率显示
        /// </summary>
        private void GenerateWinRateDisplay(RectTransform parent)
        {
            // 生成胜率图表或简单显示
        }

        /// <summary>
        /// 生成趋势分析
        /// </summary>
        private void GenerateTrendAnalysis(RectTransform parent)
        {
            // 生成趋势分析图表
        }

        /// <summary>
        /// 生成游戏信息面板
        /// </summary>
        private void GenerateGameInfoPanel()
        {
            GameObject infoPanel = CreateUIComponent<RectTransform>("GameInfoPanel", _rootContainer, "info_panel").gameObject;
            
            // 设置在顶部
            RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
            SetRectTransform(infoRect, new AnchorSettings { min = new Vector2(0, 0.9f), max = new Vector2(1, 1) }, 
                           Vector2.zero, Vector2.zero);
            
            _gameInfoPanel = infoPanel.AddComponent<GameInfoPanel>();
            
            // 添加局号显示
            CreateInfoDisplay("局号", "---", infoPanel.transform);
            
            // 添加倒计时显示
            CreateInfoDisplay("倒计时", "00:00", infoPanel.transform);
            
            // 添加余额显示
            CreateInfoDisplay("余额", "¥0.00", infoPanel.transform);
        }

        /// <summary>
        /// 生成控制面板
        /// </summary>
        private void GenerateControlPanel()
        {
            GameObject controlPanel = CreateUIComponent<RectTransform>("ControlPanel", _rootContainer, "control_panel").gameObject;
            
            // 设置在底部
            RectTransform controlRect = controlPanel.GetComponent<RectTransform>();
            SetRectTransform(controlRect, new AnchorSettings { min = new Vector2(0, 0), max = new Vector2(1, 0.1f) }, 
                           Vector2.zero, Vector2.zero);
            
            _controlPanel = controlPanel.AddComponent<ControlPanel>();
            
            // 添加确认投注按钮
            CreateControlButton("确认投注", controlPanel.transform);
            
            // 添加取消投注按钮
            CreateControlButton("取消投注", controlPanel.transform);
            
            // 添加重复投注按钮
            CreateControlButton("重复投注", controlPanel.transform);
        }

        /// <summary>
        /// 创建信息显示项
        /// </summary>
        private void CreateInfoDisplay(string label, string value, Transform parent)
        {
            GameObject infoItem = CreateUIComponent<RectTransform>(label, parent, "info_panel").gameObject;
            ApplyLayoutGroup(infoItem.GetComponent<RectTransform>(), LayoutType.Horizontal, Vector2.one * 5f);
            
            // 标签
            GameObject labelObj = CreateUIComponent<Text>("Label", infoItem.transform, "info_panel").gameObject;
            Text labelText = labelObj.GetComponent<Text>();
            labelText.text = label + ":";
            
            // 值
            GameObject valueObj = CreateUIComponent<Text>("Value", infoItem.transform, "info_panel").gameObject;
            Text valueText = valueObj.GetComponent<Text>();
            valueText.text = value;
            valueText.color = Color.cyan;
        }

        /// <summary>
        /// 创建控制按钮
        /// </summary>
        private void CreateControlButton(string label, Transform parent)
        {
            GameObject button = CreateUIComponent<Button>(label + "Button", parent, "control_panel").gameObject;
            
            // 添加文本
            GameObject textObj = CreateUIComponent<Text>("Text", button.transform, "control_panel").gameObject;
            Text buttonText = textObj.GetComponent<Text>();
            buttonText.text = label;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
        }

        #endregion

        #region 响应式绑定和主题

        /// <summary>
        /// 设置响应式数据绑定
        /// </summary>
        private void SetupReactiveBindings()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager == null) return;

            // 绑定游戏状态
            AddReactiveBinding<string>(this, "gamePhase", OnGamePhaseChanged);
            AddReactiveBinding<float>(this, "userBalance", OnBalanceChanged);
            AddReactiveBinding<string>(this, "currentBureauNumber", OnBureauNumberChanged);
            AddReactiveBinding<int>(this, "countdown", OnCountdownChanged);

            // 绑定投注数据
            foreach (var betType in _betAreas.Keys)
            {
                string dataKey = $"betAmount_{(int)betType}";
                AddReactiveBinding<float>(this, dataKey, amount => OnBetAmountChanged(betType, amount));
            }
        }

        /// <summary>
        /// 应用桌台主题
        /// </summary>
        private void ApplyTableTheme()
        {
            switch (_tableTheme)
            {
                case BaccaratTableTheme.Classic:
                    ApplyClassicTheme();
                    break;
                case BaccaratTableTheme.Modern:
                    ApplyModernTheme();
                    break;
                case BaccaratTableTheme.Luxury:
                    ApplyLuxuryTheme();
                    break;
            }
        }

        /// <summary>
        /// 应用经典主题
        /// </summary>
        private void ApplyClassicTheme()
        {
            // 设置经典绿色桌面等
            _areaColors.bankerColor = new Color(0.8f, 0.2f, 0.2f); // 红色庄家
            _areaColors.playerColor = new Color(0.2f, 0.2f, 0.8f); // 蓝色闲家
            _areaColors.tieColor = new Color(0.2f, 0.7f, 0.2f);   // 绿色和局
        }

        /// <summary>
        /// 应用现代主题
        /// </summary>
        private void ApplyModernTheme()
        {
            // 现代化的配色方案
        }

        /// <summary>
        /// 应用豪华主题
        /// </summary>
        private void ApplyLuxuryTheme()
        {
            // 豪华金色主题
        }

        #endregion

        #region 事件回调

        /// <summary>
        /// 游戏阶段变化回调
        /// </summary>
        private void OnGamePhaseChanged(string newPhase)
        {
            // 根据游戏阶段更新UI状态
            foreach (var betArea in _betAreas.Values)
            {
                betArea.SetPhase(newPhase);
            }
        }

        /// <summary>
        /// 余额变化回调
        /// </summary>
        private void OnBalanceChanged(float newBalance)
        {
            // 更新余额显示
            if (_gameInfoPanel != null)
            {
                _gameInfoPanel.UpdateBalance(newBalance);
            }
        }

        /// <summary>
        /// 局号变化回调
        /// </summary>
        private void OnBureauNumberChanged(string newBureauNumber)
        {
            // 更新局号显示
            if (_gameInfoPanel != null)
            {
                _gameInfoPanel.UpdateBureauNumber(newBureauNumber);
            }
        }

        /// <summary>
        /// 倒计时变化回调
        /// </summary>
        private void OnCountdownChanged(int newCountdown)
        {
            // 更新倒计时显示
            if (_gameInfoPanel != null)
            {
                _gameInfoPanel.UpdateCountdown(newCountdown);
            }
        }

        /// <summary>
        /// 投注金额变化回调
        /// </summary>
        private void OnBetAmountChanged(BaccaratBetType betType, float amount)
        {
            if (_betAreas.ContainsKey(betType))
            {
                _betAreas[betType].UpdateBetAmount(amount);
            }
        }

        #endregion

        #region 清理方法

        /// <summary>
        /// 清理所有区域
        /// </summary>
        private void ClearAllAreas()
        {
            _betAreas.Clear();
            _chipArea = null;
            _videoArea = null;
            _roadmapArea = null;
            _statisticsPanel = null;
            _gameInfoPanel = null;
            _controlPanel = null;

            // 清理所有生成的对象
            foreach (var category in _generatedObjects.Keys.ToList())
            {
                ClearUI(category);
            }
        }

        /// <summary>
        /// 获取桌台背景精灵
        /// </summary>
        private Sprite GetTableBackgroundSprite()
        {
            // 根据主题返回不同的背景图片
            string spriteName = _tableTheme switch
            {
                BaccaratTableTheme.Classic => "table_bg_classic",
                BaccaratTableTheme.Modern => "table_bg_modern",
                BaccaratTableTheme.Luxury => "table_bg_luxury",
                _ => "table_bg_classic"
            };
            
            return Resources.Load<Sprite>($"UI/Backgrounds/{spriteName}");
        }

        #endregion

        #region 配置数据结构

        /// <summary>
        /// 桌台主题枚举
        /// </summary>
        public enum BaccaratTableTheme
        {
            Classic,  // 经典主题
            Modern,   // 现代主题
            Luxury    // 豪华主题
        }

        /// <summary>
        /// 投注区域布局配置
        /// </summary>
        [System.Serializable]
        public class BettingAreaLayout
        {
            public AnchorSettings containerAnchor = new AnchorSettings { min = new Vector2(0.1f, 0.3f), max = new Vector2(0.9f, 0.8f) };
            public Vector2 containerPosition = Vector2.zero;
            public Vector2 containerSize = Vector2.zero;
            public Vector2 areaSpacing = new Vector2(10, 10);
            public int labelFontSize = 18;
        }

        /// <summary>
        /// 投注区域颜色配置
        /// </summary>
        [System.Serializable]
        public class BettingAreaColors
        {
            public Color bankerColor = Color.red;
            public Color playerColor = Color.blue;
            public Color tieColor = Color.green;
            public Color pairColor = Color.yellow;
            public Color bigColor = Color.cyan;
            public Color smallColor = Color.magenta;
        }

        /// <summary>
        /// 筹码配置
        /// </summary>
        [System.Serializable]
        public class ChipConfiguration
        {
            public AnchorSettings containerAnchor = new AnchorSettings { min = new Vector2(0, 0), max = new Vector2(1, 0.2f) };
            public Vector2 containerPosition = Vector2.zero;
            public Vector2 containerSize = Vector2.zero;
            public Vector2 currentChipSize = new Vector2(60, 60);
            public ChipData[] chipValues = new ChipData[0];
        }

        /// <summary>
        /// 视频区域配置
        /// </summary>
        [System.Serializable]
        public class VideoAreaConfig
        {
            public AnchorSettings containerAnchor = new AnchorSettings { min = new Vector2(0.1f, 0.5f), max = new Vector2(0.6f, 0.9f) };
            public Vector2 containerPosition = Vector2.zero;
            public Vector2 containerSize = Vector2.zero;
            public bool showControls = true;
            public bool showCards = true;
        }

        /// <summary>
        /// 路纸配置
        /// </summary>
        [System.Serializable]
        public class RoadmapConfig
        {
            public AnchorSettings containerAnchor = new AnchorSettings { min = new Vector2(0.7f, 0.3f), max = new Vector2(1, 0.8f) };
            public Vector2 containerPosition = Vector2.zero;
            public Vector2 containerSize = Vector2.zero;
            public int mainRoadColumns = 6;
            public Vector2 roadCellSize = new Vector2(20, 20);
            public Vector2 roadSpacing = new Vector2(2, 2);
        }

        #endregion
    }

    // 这些组件类将在后续提供
    public class BaccaratBetArea : MonoBehaviour 
    { 
        public void Initialize(BaccaratBetType betType, string label, Color color) { }
        public void SetPhase(string phase) { }
        public void UpdateBetAmount(float amount) { }
    }

    public class ChipSelectionArea : MonoBehaviour 
    { 
        public void Initialize(BaccaratTableGenerator.ChipConfiguration config) { }
    }

    public class VideoDisplayArea : MonoBehaviour 
    { 
        public void Initialize(BaccaratTableGenerator.VideoAreaConfig config) { }
    }

    public class RoadmapDisplayArea : MonoBehaviour 
    { 
        public void Initialize(BaccaratTableGenerator.RoadmapConfig config) { }
    }

    public class StatisticsPanel : MonoBehaviour { }

    public class GameInfoPanel : MonoBehaviour 
    { 
        public void UpdateBalance(float balance) { }
        public void UpdateBureauNumber(string bureauNumber) { }
        public void UpdateCountdown(int countdown) { }
    }

    public class ControlPanel : MonoBehaviour { }

    public class ChipButton : MonoBehaviour 
    { 
        public void Initialize(ChipData chipData) { }
    }

    public class ChipStackDisplay : MonoBehaviour 
    { 
        public void Initialize(BaccaratTableGenerator.ChipConfiguration config) { }
    }
}