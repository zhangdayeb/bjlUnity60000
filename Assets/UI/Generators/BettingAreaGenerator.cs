// Assets/UI/Generators/BettingAreaGenerator.cs
// 投注区域生成器 - 专门生成百家乐投注区域相关的UI组件
// 包括投注目标创建、筹码显示设置、闪烁效果等核心功能

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UI.Framework;
using UI.Effects;
using Core.Data.Types;
using Core.Architecture;
using System.Linq;

namespace UI.Generators
{
    /// <summary>
    /// 投注区域生成器
    /// 专门负责生成百家乐投注区域，包括投注目标、筹码显示、闪烁效果等
    /// </summary>
    public class BettingAreaGenerator : UIGeneratorBase
    {
        #region 配置数据

        [Header("投注区域配置")]
        [Tooltip("投注区域布局设置")]
        [SerializeField] private BettingAreaLayout _areaLayout = new BettingAreaLayout();
        
        [Tooltip("投注区域颜色配置")]
        [SerializeField] private BettingAreaColors _areaColors = new BettingAreaColors();
        
        [Tooltip("投注区域尺寸配置")]
        [SerializeField] private BettingAreaSizes _areaSizes = new BettingAreaSizes();

        [Header("筹码显示配置")]
        [Tooltip("筹码显示设置")]
        [SerializeField] private ChipDisplayConfig _chipDisplayConfig = new ChipDisplayConfig();

        [Header("闪烁效果配置")]
        [Tooltip("闪烁效果设置")]
        [SerializeField] private FlashEffectConfig _flashConfig = new FlashEffectConfig();

        [Header("交互配置")]
        [Tooltip("投注区域交互设置")]
        [SerializeField] private BetAreaInteraction _interactionConfig = new BetAreaInteraction();

        [Header("赔率显示配置")]
        [Tooltip("赔率显示设置")]
        [SerializeField] private OddsDisplayConfig _oddsConfig = new OddsDisplayConfig();

        // 生成的投注区域
        private Dictionary<BaccaratBetType, BetTargetArea> _betTargets = new Dictionary<BaccaratBetType, BetTargetArea>();
        private Dictionary<BaccaratBetType, ChipDisplayArea> _chipDisplayAreas = new Dictionary<BaccaratBetType, ChipDisplayArea>();
        private Dictionary<BaccaratBetType, FlashEffect> _flashEffects = new Dictionary<BaccaratBetType, FlashEffect>();

        // 响应式数据绑定
        private Dictionary<BaccaratBetType, ReactiveData<float>> _betAmounts = new Dictionary<BaccaratBetType, ReactiveData<float>>();
        private Dictionary<BaccaratBetType, ReactiveData<bool>> _areaStates = new Dictionary<BaccaratBetType, ReactiveData<bool>>();

        // 投注限额配置
        private Dictionary<BaccaratBetType, BetLimit> _betLimits = new Dictionary<BaccaratBetType, BetLimit>();

        #endregion

        #region 重写基类方法

        protected override List<string> GetSupportedGenerationTypes()
        {
            return new List<string>
            {
                "complete_betting_areas",  // 完整投注区域
                "main_betting_areas",      // 主要投注区域（庄闲和）
                "side_betting_areas",      // 边注区域（对子、大小）
                "bet_targets",             // 投注目标
                "chip_displays",           // 筹码显示
                "flash_effects",           // 闪烁效果
                "odds_displays"            // 赔率显示
            };
        }

        public override void GenerateUI()
        {
            GenerateCompleteBettingAreas();
        }

        public override void ClearUI()
        {
            ClearAllBettingAreas();
        }

        protected override void OnGenerateSpecificUI(string generationType, Dictionary<string, object> parameters)
        {
            switch (generationType)
            {
                case "complete_betting_areas":
                    GenerateCompleteBettingAreas();
                    break;
                case "main_betting_areas":
                    GenerateMainBettingAreas();
                    break;
                case "side_betting_areas":
                    GenerateSideBettingAreas();
                    break;
                case "bet_targets":
                    GenerateBetTargets();
                    break;
                case "chip_displays":
                    GenerateChipDisplays();
                    break;
                case "flash_effects":
                    GenerateFlashEffects();
                    break;
                case "odds_displays":
                    GenerateOddsDisplays();
                    break;
                default:
                    base.OnGenerateSpecificUI(generationType, parameters);
                    break;
            }
        }

        #endregion

        #region 主要生成方法

        /// <summary>
        /// 生成完整的投注区域
        /// </summary>
        private void GenerateCompleteBettingAreas()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[BettingAreaGenerator] 开始生成完整投注区域");
            }

            // 清理现有区域
            ClearAllBettingAreas();

            // 初始化投注限额
            InitializeBetLimits();

            // 创建主容器
            CreateMainBettingContainer();

            // 生成各类投注区域
            GenerateMainBettingAreas();
            GenerateSideBettingAreas();

            // 设置特殊效果
            GenerateFlashEffects();
            GenerateOddsDisplays();

            // 设置响应式绑定
            SetupReactiveBindings();

            // 应用区域布局
            ApplyBettingAreaLayout();

            if (_enableDebugMode)
            {
                Debug.Log("[BettingAreaGenerator] 完整投注区域生成完成");
            }
        }

        /// <summary>
        /// 创建投注目标
        /// </summary>
        public BetTargetArea CreateBetTarget(BaccaratBetType betType, string label, Vector2 position, Vector2 size)
        {
            if (_enableDebugMode)
            {
                Debug.Log($"[BettingAreaGenerator] 创建投注目标: {betType} - {label}");
            }

            GameObject targetObj = CreateUIComponent<RectTransform>($"BetTarget_{betType}", _rootContainer, "bet_targets").gameObject;
            
            // 设置位置和大小
            RectTransform targetRect = targetObj.GetComponent<RectTransform>();
            targetRect.anchoredPosition = position;
            targetRect.sizeDelta = size;

            // 添加背景图片
            Image bgImage = targetObj.AddComponent<Image>();
            bgImage.color = GetBetAreaColor(betType);
            bgImage.type = Image.Type.Sliced;

            // 添加按钮组件
            Button button = targetObj.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            // 添加事件触发器（支持更多交互）
            EventTrigger eventTrigger = targetObj.AddComponent<EventTrigger>();
            SetupBetAreaEvents(eventTrigger, betType);

            // 创建标签文本
            CreateBetAreaLabel(targetObj.transform, label, betType);

            // 创建赔率显示
            CreateOddsDisplay(targetObj.transform, betType);

            // 创建筹码显示区域
            CreateChipDisplayArea(targetObj.transform, betType);

            // 添加投注目标组件
            BetTargetArea betTarget = targetObj.AddComponent<BetTargetArea>();
            betTarget.Initialize(betType, label, _interactionConfig, _betLimits.GetValueOrDefault(betType));

            // 注册到字典
            _betTargets[betType] = betTarget;

            return betTarget;
        }

        /// <summary>
        /// 设置筹码显示
        /// </summary>
        public void SetupChipDisplay(BaccaratBetType betType, Transform parent)
        {
            if (_enableDebugMode)
            {
                Debug.Log($"[BettingAreaGenerator] 设置筹码显示: {betType}");
            }

            GameObject chipDisplayObj = CreateUIComponent<RectTransform>($"ChipDisplay_{betType}", parent, "chip_displays").gameObject;
            
            // 设置筹码显示区域位置（在投注区域内部）
            RectTransform chipRect = chipDisplayObj.GetComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(0.1f, 0.1f);
            chipRect.anchorMax = new Vector2(0.9f, 0.6f);
            chipRect.offsetMin = Vector2.zero;
            chipRect.offsetMax = Vector2.zero;

            // 添加筹码显示组件
            ChipDisplayArea chipDisplay = chipDisplayObj.AddComponent<ChipDisplayArea>();
            chipDisplay.Initialize(betType, _chipDisplayConfig);

            // 注册到字典
            _chipDisplayAreas[betType] = chipDisplay;
        }

        /// <summary>
        /// 设置闪烁效果
        /// </summary>
        public void SetupFlashEffect(BaccaratBetType betType, Transform parent)
        {
            if (_enableDebugMode)
            {
                Debug.Log($"[BettingAreaGenerator] 设置闪烁效果: {betType}");
            }

            GameObject flashObj = CreateUIComponent<Image>($"FlashEffect_{betType}", parent, "flash_effects").gameObject;
            
            // 设置闪烁层位置（覆盖整个投注区域）
            RectTransform flashRect = flashObj.GetComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;

            // 设置闪烁效果图片
            Image flashImage = flashObj.GetComponent<Image>();
            flashImage.color = GetFlashColor(betType);
            flashImage.raycastTarget = false; // 不阻挡交互

            // 添加闪烁效果组件
            FlashEffect flashEffect = flashObj.AddComponent<FlashEffect>();
            flashEffect.Initialize(_flashConfig);

            // 注册到字典
            _flashEffects[betType] = flashEffect;

            // 初始时隐藏
            flashObj.SetActive(false);
        }

        #endregion

        #region 详细生成方法

        /// <summary>
        /// 初始化投注限额
        /// </summary>
        private void InitializeBetLimits()
        {
            // 设置各投注区域的限额
            _betLimits[BaccaratBetType.Banker] = new BetLimit { min = 10f, max = 50000f };
            _betLimits[BaccaratBetType.Player] = new BetLimit { min = 10f, max = 50000f };
            _betLimits[BaccaratBetType.Tie] = new BetLimit { min = 10f, max = 10000f };
            _betLimits[BaccaratBetType.BankerPair] = new BetLimit { min = 5f, max = 5000f };
            _betLimits[BaccaratBetType.PlayerPair] = new BetLimit { min = 5f, max = 5000f };
            _betLimits[BaccaratBetType.BigBig] = new BetLimit { min = 5f, max = 5000f };
            _betLimits[BaccaratBetType.SmallSmall] = new BetLimit { min = 5f, max = 5000f };
        }

        /// <summary>
        /// 创建主投注容器
        /// </summary>
        private void CreateMainBettingContainer()
        {
            // 确保根容器有正确的设置
            if (_rootContainer != null)
            {
                // 添加内容尺寸适配器
                ContentSizeFitter fitter = _rootContainer.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                {
                    fitter = _rootContainer.gameObject.AddComponent<ContentSizeFitter>();
                }
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        /// <summary>
        /// 生成主要投注区域（庄、闲、和）
        /// </summary>
        private void GenerateMainBettingAreas()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[BettingAreaGenerator] 生成主要投注区域");
            }

            // 创建主要投注区域容器
            GameObject mainContainer = CreateUIComponent<RectTransform>("MainBettingAreas", _rootContainer, "main_areas").gameObject;
            
            // 设置主区域布局
            SetRectTransform(mainContainer.GetComponent<RectTransform>(), 
                _areaLayout.mainAreasAnchor, _areaLayout.mainAreasPosition, _areaLayout.mainAreasSize);

            // 应用网格布局（1行3列）
            GridLayoutGroup gridLayout = mainContainer.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.cellSize = _areaSizes.mainAreaSize;
            gridLayout.spacing = _areaLayout.mainAreaSpacing;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            // 生成庄家区域
            var bankerTarget = CreateBetTarget(BaccaratBetType.Banker, "庄", Vector2.zero, _areaSizes.mainAreaSize);
            bankerTarget.transform.SetParent(mainContainer.transform, false);

            // 生成闲家区域
            var playerTarget = CreateBetTarget(BaccaratBetType.Player, "闲", Vector2.zero, _areaSizes.mainAreaSize);
            playerTarget.transform.SetParent(mainContainer.transform, false);

            // 生成和局区域
            var tieTarget = CreateBetTarget(BaccaratBetType.Tie, "和", Vector2.zero, _areaSizes.mainAreaSize);
            tieTarget.transform.SetParent(mainContainer.transform, false);
        }

        /// <summary>
        /// 生成边注投注区域（对子、大小）
        /// </summary>
        private void GenerateSideBettingAreas()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[BettingAreaGenerator] 生成边注投注区域");
            }

            // 生成对子投注区域
            GeneratePairBettingAreas();

            // 生成大小投注区域
            GenerateBigSmallBettingAreas();
        }

        /// <summary>
        /// 生成对子投注区域
        /// </summary>
        private void GeneratePairBettingAreas()
        {
            GameObject pairContainer = CreateUIComponent<RectTransform>("PairBettingAreas", _rootContainer, "pair_areas").gameObject;
            
            // 设置对子区域布局
            SetRectTransform(pairContainer.GetComponent<RectTransform>(), 
                _areaLayout.pairAreasAnchor, _areaLayout.pairAreasPosition, _areaLayout.pairAreasSize);

            // 应用水平布局
            HorizontalLayoutGroup hLayout = pairContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = _areaLayout.pairAreaSpacing.x;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childAlignment = TextAnchor.MiddleCenter;

            // 生成庄对区域
            var bankerPairTarget = CreateBetTarget(BaccaratBetType.BankerPair, "庄对", Vector2.zero, _areaSizes.pairAreaSize);
            bankerPairTarget.transform.SetParent(pairContainer.transform, false);

            // 生成闲对区域
            var playerPairTarget = CreateBetTarget(BaccaratBetType.PlayerPair, "闲对", Vector2.zero, _areaSizes.pairAreaSize);
            playerPairTarget.transform.SetParent(pairContainer.transform, false);
        }

        /// <summary>
        /// 生成大小投注区域
        /// </summary>
        private void GenerateBigSmallBettingAreas()
        {
            GameObject bigSmallContainer = CreateUIComponent<RectTransform>("BigSmallBettingAreas", _rootContainer, "bigsmall_areas").gameObject;
            
            // 设置大小区域布局
            SetRectTransform(bigSmallContainer.GetComponent<RectTransform>(), 
                _areaLayout.bigSmallAreasAnchor, _areaLayout.bigSmallAreasPosition, _areaLayout.bigSmallAreasSize);

            // 应用水平布局
            HorizontalLayoutGroup hLayout = bigSmallContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = _areaLayout.bigSmallAreaSpacing.x;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childAlignment = TextAnchor.MiddleCenter;

            // 生成大区域
            var bigTarget = CreateBetTarget(BaccaratBetType.BigBig, "大", Vector2.zero, _areaSizes.bigSmallAreaSize);
            bigTarget.transform.SetParent(bigSmallContainer.transform, false);

            // 生成小区域
            var smallTarget = CreateBetTarget(BaccaratBetType.SmallSmall, "小", Vector2.zero, _areaSizes.bigSmallAreaSize);
            smallTarget.transform.SetParent(bigSmallContainer.transform, false);
        }

        /// <summary>
        /// 生成投注目标
        /// </summary>
        private void GenerateBetTargets()
        {
            // 为每个投注类型生成目标
            foreach (BaccaratBetType betType in Enum.GetValues(typeof(BaccaratBetType)))
            {
                if (!_betTargets.ContainsKey(betType))
                {
                    string label = GetBetTypeLabel(betType);
                    Vector2 position = GetBetTypePosition(betType);
                    Vector2 size = GetBetTypeSize(betType);
                    
                    CreateBetTarget(betType, label, position, size);
                }
            }
        }

        /// <summary>
        /// 生成筹码显示
        /// </summary>
        private void GenerateChipDisplays()
        {
            foreach (var kvp in _betTargets)
            {
                if (!_chipDisplayAreas.ContainsKey(kvp.Key))
                {
                    SetupChipDisplay(kvp.Key, kvp.Value.transform);
                }
            }
        }

        /// <summary>
        /// 生成闪烁效果
        /// </summary>
        private void GenerateFlashEffects()
        {
            foreach (var kvp in _betTargets)
            {
                if (!_flashEffects.ContainsKey(kvp.Key))
                {
                    SetupFlashEffect(kvp.Key, kvp.Value.transform);
                }
            }
        }

        /// <summary>
        /// 生成赔率显示
        /// </summary>
        private void GenerateOddsDisplays()
        {
            foreach (var kvp in _betTargets)
            {
                CreateOddsDisplay(kvp.Value.transform, kvp.Key);
            }
        }

        #endregion

        #region 组件创建方法

        /// <summary>
        /// 创建投注区域标签
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="label">标签文本</param>
        /// <param name="betType">投注类型</param>
        private void CreateBetAreaLabel(Transform parent, string label, BaccaratBetType betType)
        {
            GameObject labelObj = CreateUIComponent<Text>("Label", parent, "bet_labels").gameObject;
            
            Text labelText = labelObj.GetComponent<Text>();
            labelText.text = label;
            labelText.fontSize = GetLabelFontSize(betType);
            labelText.color = GetLabelColor(betType);
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.fontStyle = FontStyle.Bold;
            labelText.raycastTarget = false;

            // 设置标签位置（在投注区域上方）
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.7f);
            labelRect.anchorMax = new Vector2(1f, 0.95f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            // 添加文本阴影效果
            Shadow shadow = labelObj.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = Vector2.one;

            // 添加文本轮廓效果
            Outline outline = labelObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = Vector2.one * 0.5f;
        }

        /// <summary>
        /// 创建赔率显示
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="betType">投注类型</param>
        private void CreateOddsDisplay(Transform parent, BaccaratBetType betType)
        {
            if (!_oddsConfig.showOdds) return;

            GameObject oddsObj = CreateUIComponent<Text>("Odds", parent, "odds_displays").gameObject;
            
            Text oddsText = oddsObj.GetComponent<Text>();
            oddsText.text = GetOddsText(betType);
            oddsText.fontSize = _oddsConfig.fontSize;
            oddsText.color = _oddsConfig.textColor;
            oddsText.alignment = TextAnchor.MiddleRight;
            oddsText.fontStyle = FontStyle.Normal;
            oddsText.raycastTarget = false;

            // 设置赔率位置（在投注区域右下角）
            RectTransform oddsRect = oddsObj.GetComponent<RectTransform>();
            oddsRect.anchorMin = new Vector2(0.6f, 0.05f);
            oddsRect.anchorMax = new Vector2(0.95f, 0.25f);
            oddsRect.offsetMin = Vector2.zero;
            oddsRect.offsetMax = Vector2.zero;

            // 添加背景
            if (_oddsConfig.showBackground)
            {
                Image bgImage = oddsObj.AddComponent<Image>();
                bgImage.color = _oddsConfig.backgroundColor;
                bgImage.raycastTarget = false;
            }
        }

        /// <summary>
        /// 创建筹码显示区域
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="betType">投注类型</param>
        private void CreateChipDisplayArea(Transform parent, BaccaratBetType betType)
        {
            GameObject chipAreaObj = CreateUIComponent<RectTransform>("ChipArea", parent, "chip_areas").gameObject;
            
            // 设置筹码区域位置（在投注区域中央偏下）
            RectTransform chipRect = chipAreaObj.GetComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(0.1f, 0.1f);
            chipRect.anchorMax = new Vector2(0.9f, 0.6f);
            chipRect.offsetMin = Vector2.zero;
            chipRect.offsetMax = Vector2.zero;

            // 添加网格布局（用于筹码堆叠）
            GridLayoutGroup gridLayout = chipAreaObj.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = _chipDisplayConfig.maxChipsPerRow;
            gridLayout.cellSize = _chipDisplayConfig.chipSize;
            gridLayout.spacing = _chipDisplayConfig.chipSpacing;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            // 添加筹码显示组件
            ChipDisplayArea chipDisplay = chipAreaObj.AddComponent<ChipDisplayArea>();
            chipDisplay.Initialize(betType, _chipDisplayConfig);

            // 注册到字典
            _chipDisplayAreas[betType] = chipDisplay;
        }

        /// <summary>
        /// 设置投注区域事件
        /// </summary>
        /// <param name="eventTrigger">事件触发器</param>
        /// <param name="betType">投注类型</param>
        private void SetupBetAreaEvents(EventTrigger eventTrigger, BaccaratBetType betType)
        {
            // 点击事件
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => OnBetAreaClicked(betType, (PointerEventData)data));
            eventTrigger.triggers.Add(clickEntry);

            // 鼠标进入事件
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => OnBetAreaEnter(betType));
            eventTrigger.triggers.Add(enterEntry);

            // 鼠标离开事件
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => OnBetAreaExit(betType));
            eventTrigger.triggers.Add(exitEntry);

            // 长按事件（可选）
            if (_interactionConfig.enableLongPress)
            {
                EventTrigger.Entry downEntry = new EventTrigger.Entry();
                downEntry.eventID = EventTriggerType.PointerDown;
                downEntry.callback.AddListener((data) => OnBetAreaPointerDown(betType));
                eventTrigger.triggers.Add(downEntry);

                EventTrigger.Entry upEntry = new EventTrigger.Entry();
                upEntry.eventID = EventTriggerType.PointerUp;
                upEntry.callback.AddListener((data) => OnBetAreaPointerUp(betType));
                eventTrigger.triggers.Add(upEntry);
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 投注区域点击事件
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <param name="eventData">事件数据</param>
        private void OnBetAreaClicked(BaccaratBetType betType, PointerEventData eventData)
        {
            if (_enableDebugMode)
            {
                Debug.Log($"[BettingAreaGenerator] 投注区域被点击: {betType}");
            }

            // 检查是否可以投注
            if (!CanPlaceBet(betType))
            {
                ShowBetError($"无法在{GetBetTypeLabel(betType)}区域投注");
                return;
            }

            // 获取当前筹码值
            float currentChipValue = GetCurrentChipValue();
            if (currentChipValue <= 0)
            {
                ShowBetError("请先选择筹码");
                return;
            }

            // 检查投注限额
            if (!CheckBetLimit(betType, currentChipValue))
            {
                var limit = _betLimits[betType];
                ShowBetError($"投注金额超出限制 ({limit.min}-{limit.max})");
                return;
            }

            // 执行投注
            PlaceBet(betType, currentChipValue);

            // 播放音效
            if (_interactionConfig.enableSoundEffects)
            {
                PlayBetPlacedSound();
            }

            // 显示视觉反馈
            ShowBetPlacedFeedback(betType);
        }

        /// <summary>
        /// 鼠标进入投注区域
        /// </summary>
        /// <param name="betType">投注类型</param>
        private void OnBetAreaEnter(BaccaratBetType betType)
        {
            if (_interactionConfig.enableHoverEffects)
            {
                // 显示悬停效果
                if (_betTargets.ContainsKey(betType))
                {
                    _betTargets[betType].SetHoverState(true);
                }

                // 显示投注预览
                ShowBetPreview(betType);
            }
        }

        /// <summary>
        /// 鼠标离开投注区域
        /// </summary>
        /// <param name="betType">投注类型</param>
        private void OnBetAreaExit(BaccaratBetType betType)
        {
            if (_interactionConfig.enableHoverEffects)
            {
                // 隐藏悬停效果
                if (_betTargets.ContainsKey(betType))
                {
                    _betTargets[betType].SetHoverState(false);
                }

                // 隐藏投注预览
                HideBetPreview(betType);
            }
        }

        /// <summary>
        /// 鼠标按下事件
        /// </summary>
        /// <param name="betType">投注类型</param>
        private void OnBetAreaPointerDown(BaccaratBetType betType)
        {
            // 开始长按计时
            if (_interactionConfig.enableLongPress)
            {
                StartLongPressTimer(betType);
            }
        }

        /// <summary>
        /// 鼠标抬起事件
        /// </summary>
        /// <param name="betType">投注类型</param>
        private void OnBetAreaPointerUp(BaccaratBetType betType)
        {
            // 停止长按计时
            if (_interactionConfig.enableLongPress)
            {
                StopLongPressTimer(betType);
            }
        }

        #endregion

        #region 投注逻辑

        /// <summary>
        /// 执行投注
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <param name="amount">投注金额</param>
        private void PlaceBet(BaccaratBetType betType, float amount)
        {
            // 更新投注金额
            if (_betAmounts.ContainsKey(betType))
            {
                _betAmounts[betType].Value += amount;
            }

            // 添加筹码到显示区域
            if (_chipDisplayAreas.ContainsKey(betType))
            {
                _chipDisplayAreas[betType].AddChip(amount);
            }

            // 触发投注事件
            OnBetPlaced?.Invoke(betType, amount);

            if (_enableDebugMode)
            {
                Debug.Log($"[BettingAreaGenerator] 投注成功: {betType} - {amount}");
            }
        }

        /// <summary>
        /// 检查是否可以投注
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <returns>是否可以投注</returns>
        private bool CanPlaceBet(BaccaratBetType betType)
        {
            // 检查游戏状态
            if (!IsGameInBettingPhase())
            {
                return false;
            }

            // 检查投注区域状态
            if (_areaStates.ContainsKey(betType) && !_areaStates[betType].Value)
            {
                return false;
            }

            // 检查用户余额
            if (!HasSufficientBalance())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查投注限额
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <param name="amount">投注金额</param>
        /// <returns>是否在限额内</returns>
        private bool CheckBetLimit(BaccaratBetType betType, float amount)
        {
            if (!_betLimits.ContainsKey(betType))
                return true;

            var limit = _betLimits[betType];
            float currentTotal = _betAmounts.ContainsKey(betType) ? _betAmounts[betType].Value : 0f;
            float newTotal = currentTotal + amount;

            return newTotal >= limit.min && newTotal <= limit.max;
        }

        /// <summary>
        /// 获取当前筹码值
        /// </summary>
        /// <returns>当前筹码值</returns>
        private float GetCurrentChipValue()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager != null)
            {
                var currentChip = uiManager.GetReactiveValue<ChipData>("currentChip");
                return currentChip?.val ?? 0f;
            }
            return 0f;
        }

        /// <summary>
        /// 检查游戏是否在投注阶段
        /// </summary>
        /// <returns>是否在投注阶段</returns>
        private bool IsGameInBettingPhase()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager != null)
            {
                string gamePhase = uiManager.GetReactiveValue<string>("gamePhase", "");
                return gamePhase == "betting" || gamePhase == "waiting";
            }
            return false;
        }

        /// <summary>
        /// 检查用户余额是否足够
        /// </summary>
        /// <returns>余额是否足够</returns>
        private bool HasSufficientBalance()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager != null)
            {
                float balance = uiManager.GetReactiveValue<float>("userBalance", 0f);
                float currentChipValue = GetCurrentChipValue();
                return balance >= currentChipValue;
            }
            return false;
        }

        #endregion

        #region 视觉反馈

        /// <summary>
        /// 显示投注错误
        /// </summary>
        /// <param name="message">错误消息</param>
        private void ShowBetError(string message)
        {
            // 这里可以显示错误提示UI
            Debug.LogWarning($"[BettingAreaGenerator] 投注错误: {message}");
        }

        /// <summary>
        /// 显示投注成功反馈
        /// </summary>
        /// <param name="betType">投注类型</param>
        private void ShowBetPlacedFeedback(BaccaratBetType betType)
        {
            // 播放成功动画
            if (_betTargets.ContainsKey(betType))
            {
                _betTargets[betType].PlayBetPlacedAnimation();
            }
        }

        /// <summary>
        /// 显示投注预览
        /// </summary>
        /// <param name="betType">投注类型</param>
        private void ShowBetPreview(BaccaratBetType betType)
        {
            // 显示预览筹码
            float chipValue = GetCurrentChipValue();
            if (chipValue > 0 && _chipDisplayAreas.ContainsKey(betType))
            {
                _chipDisplayAreas[betType].ShowPreviewChip(chipValue);
            }
        }

        /// <summary>
        /// 隐藏投注预览
        /// </summary>
        /// <param name="betType">投注类型</param>
        private void HideBetPreview(BaccaratBetType betType)
        {
            if (_chipDisplayAreas.ContainsKey(betType))
            {
                _chipDisplayAreas[betType].HidePreviewChip();
            }
        }

        /// <summary>
        /// 播放投注音效
        /// </summary>
        private void PlayBetPlacedSound()
        {
            // 播放投注音效
            // AudioManager.Instance?.PlaySFX("bet_placed");
        }

        /// <summary>
        /// 开始长按计时
        /// </summary>
        /// <param name="betType">投注类型</param>
        private void StartLongPressTimer(BaccaratBetType betType)
        {
            // 实现长按逻辑
        }

        /// <summary>
        /// 停止长按计时
        /// </summary>
        /// <param name="betType">投注类型</param>
        private void StopLongPressTimer(BaccaratBetType betType)
        {
            // 停止长按逻辑
        }

        #endregion

        #region 闪烁效果控制

        /// <summary>
        /// 开始闪烁效果
        /// </summary>
        /// <param name="betType">投注类型</param>
        public void StartFlashEffect(BaccaratBetType betType)
        {
            if (_flashEffects.ContainsKey(betType))
            {
                _flashEffects[betType].StartFlash();
            }
        }

        /// <summary>
        /// 停止闪烁效果
        /// </summary>
        /// <param name="betType">投注类型</param>
        public void StopFlashEffect(BaccaratBetType betType)
        {
            if (_flashEffects.ContainsKey(betType))
            {
                _flashEffects[betType].StopFlash();
            }
        }

        /// <summary>
        /// 停止所有闪烁效果
        /// </summary>
        public void StopAllFlashEffects()
        {
            foreach (var flashEffect in _flashEffects.Values)
            {
                flashEffect.StopFlash();
            }
        }

        #endregion

        #region 响应式绑定

        /// <summary>
        /// 设置响应式数据绑定
        /// </summary>
        private void SetupReactiveBindings()
        {
            var uiManager = UIUpdateManager.Instance;
            if (uiManager == null) return;

            // 为每个投注类型设置响应式绑定
            foreach (BaccaratBetType betType in Enum.GetValues(typeof(BaccaratBetType)))
            {
                // 投注金额绑定
                string amountKey = $"betAmount_{(int)betType}";
                _betAmounts[betType] = uiManager.GetOrCreateReactiveData<float>(amountKey, 0f);
                AddReactiveBinding<float>(this, amountKey, amount => OnBetAmountChanged(betType, amount));

                // 区域状态绑定
                string stateKey = $"areaState_{(int)betType}";
                _areaStates[betType] = uiManager.GetOrCreateReactiveData<bool>(stateKey, true);
                AddReactiveBinding<bool>(this, stateKey, enabled => OnAreaStateChanged(betType, enabled));
            }

            // 游戏状态绑定
            AddReactiveBinding<string>(this, "gamePhase", OnGamePhaseChanged);
            AddReactiveBinding<List<BaccaratBetType>>(this, "winningAreas", OnWinningAreasChanged);
        }

        /// <summary>
        /// 投注金额变化事件
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <param name="amount">新金额</param>
        private void OnBetAmountChanged(BaccaratBetType betType, float amount)
        {
            if (_chipDisplayAreas.ContainsKey(betType))
            {
                _chipDisplayAreas[betType].UpdateTotalAmount(amount);
            }

            if (_betTargets.ContainsKey(betType))
            {
                _betTargets[betType].UpdateBetAmount(amount);
            }
        }

        /// <summary>
        /// 区域状态变化事件
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <param name="enabled">是否启用</param>
        private void OnAreaStateChanged(BaccaratBetType betType, bool enabled)
        {
            if (_betTargets.ContainsKey(betType))
            {
                _betTargets[betType].SetEnabled(enabled);
            }
        }

        /// <summary>
        /// 游戏阶段变化事件
        /// </summary>
        /// <param name="newPhase">新阶段</param>
        private void OnGamePhaseChanged(string newPhase)
        {
            bool canBet = newPhase == "betting" || newPhase == "waiting";
            
            foreach (var betTarget in _betTargets.Values)
            {
                betTarget.SetBettingEnabled(canBet);
            }
        }

        /// <summary>
        /// 中奖区域变化事件
        /// </summary>
        /// <param name="winningAreas">中奖区域列表</param>
        private void OnWinningAreasChanged(List<BaccaratBetType> winningAreas)
        {
            // 停止所有闪烁
            StopAllFlashEffects();

            // 对中奖区域开始闪烁
            foreach (var betType in winningAreas)
            {
                StartFlashEffect(betType);
            }
        }

        #endregion

        #region 布局和样式

        /// <summary>
        /// 应用投注区域布局
        /// </summary>
        private void ApplyBettingAreaLayout()
        {
            // 设置整体布局
            if (_rootContainer != null)
            {
                VerticalLayoutGroup vLayout = _rootContainer.GetComponent<VerticalLayoutGroup>();
                if (vLayout == null)
                {
                    vLayout = _rootContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                }
                
                vLayout.spacing = _areaLayout.containerSpacing;
                vLayout.childControlWidth = true;
                vLayout.childControlHeight = false;
                vLayout.childForceExpandHeight = false;
                vLayout.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        /// <summary>
        /// 获取投注区域颜色
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <returns>区域颜色</returns>
        private Color GetBetAreaColor(BaccaratBetType betType)
        {
            return betType switch
            {
                BaccaratBetType.Banker => _areaColors.bankerColor,
                BaccaratBetType.Player => _areaColors.playerColor,
                BaccaratBetType.Tie => _areaColors.tieColor,
                BaccaratBetType.BankerPair => _areaColors.bankerPairColor,
                BaccaratBetType.PlayerPair => _areaColors.playerPairColor,
                BaccaratBetType.BigBig => _areaColors.bigColor,
                BaccaratBetType.SmallSmall => _areaColors.smallColor,
                _ => Color.white
            };
        }

        /// <summary>
        /// 获取闪烁颜色
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <returns>闪烁颜色</returns>
        private Color GetFlashColor(BaccaratBetType betType)
        {
            Color baseColor = GetBetAreaColor(betType);
            return new Color(baseColor.r, baseColor.g, baseColor.b, _flashConfig.flashAlpha);
        }

        /// <summary>
        /// 获取标签颜色
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <returns>标签颜色</returns>
        private Color GetLabelColor(BaccaratBetType betType)
        {
            // 根据背景色决定文字颜色
            Color bgColor = GetBetAreaColor(betType);
            float brightness = (bgColor.r + bgColor.g + bgColor.b) / 3f;
            return brightness > 0.5f ? Color.black : Color.white;
        }

        /// <summary>
        /// 获取标签字体大小
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <returns>字体大小</returns>
        private int GetLabelFontSize(BaccaratBetType betType)
        {
            return betType switch
            {
                BaccaratBetType.Banker or BaccaratBetType.Player or BaccaratBetType.Tie => 24,
                _ => 18
            };
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取投注类型标签
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <returns>标签文本</returns>
        private string GetBetTypeLabel(BaccaratBetType betType)
        {
            return betType switch
            {
                BaccaratBetType.Banker => "庄",
                BaccaratBetType.Player => "闲",
                BaccaratBetType.Tie => "和",
                BaccaratBetType.BankerPair => "庄对",
                BaccaratBetType.PlayerPair => "闲对",
                BaccaratBetType.BigBig => "大",
                BaccaratBetType.SmallSmall => "小",
                _ => betType.ToString()
            };
        }

        /// <summary>
        /// 获取投注类型位置
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <returns>位置</returns>
        private Vector2 GetBetTypePosition(BaccaratBetType betType)
        {
            // 根据投注类型返回默认位置
            return betType switch
            {
                BaccaratBetType.Banker => new Vector2(-150, 0),
                BaccaratBetType.Player => new Vector2(0, 0),
                BaccaratBetType.Tie => new Vector2(150, 0),
                BaccaratBetType.BankerPair => new Vector2(-75, -100),
                BaccaratBetType.PlayerPair => new Vector2(75, -100),
                BaccaratBetType.BigBig => new Vector2(-75, -200),
                BaccaratBetType.SmallSmall => new Vector2(75, -200),
                _ => Vector2.zero
            };
        }

        /// <summary>
        /// 获取投注类型尺寸
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <returns>尺寸</returns>
        private Vector2 GetBetTypeSize(BaccaratBetType betType)
        {
            return betType switch
            {
                BaccaratBetType.Banker or BaccaratBetType.Player or BaccaratBetType.Tie => _areaSizes.mainAreaSize,
                BaccaratBetType.BankerPair or BaccaratBetType.PlayerPair => _areaSizes.pairAreaSize,
                BaccaratBetType.BigBig or BaccaratBetType.SmallSmall => _areaSizes.bigSmallAreaSize,
                _ => _areaSizes.mainAreaSize
            };
        }

        /// <summary>
        /// 获取赔率文本
        /// </summary>
        /// <param name="betType">投注类型</param>
        /// <returns>赔率文本</returns>
        private string GetOddsText(BaccaratBetType betType)
        {
            return betType switch
            {
                BaccaratBetType.Banker => "1:0.95",
                BaccaratBetType.Player => "1:1",
                BaccaratBetType.Tie => "1:8",
                BaccaratBetType.BankerPair => "1:11",
                BaccaratBetType.PlayerPair => "1:11",
                BaccaratBetType.BigBig => "1:0.54",
                BaccaratBetType.SmallSmall => "1:1.5",
                _ => "1:1"
            };
        }

        /// <summary>
        /// 清理所有投注区域
        /// </summary>
        private void ClearAllBettingAreas()
        {
            _betTargets.Clear();
            _chipDisplayAreas.Clear();
            _flashEffects.Clear();
            _betAmounts.Clear();
            _areaStates.Clear();
        }

        #endregion

        #region 公共事件

        /// <summary>
        /// 投注下注事件
        /// </summary>
        public System.Action<BaccaratBetType, float> OnBetPlaced;

        /// <summary>
        /// 投注区域悬停事件
        /// </summary>
        public System.Action<BaccaratBetType, bool> OnBetAreaHovered;

        #endregion

        #region 配置数据结构

        /// <summary>
        /// 投注区域布局配置
        /// </summary>
        [System.Serializable]
        public class BettingAreaLayout
        {
            [Header("主要区域")]
            public AnchorSettings mainAreasAnchor = new AnchorSettings { min = new Vector2(0, 0.6f), max = new Vector2(1, 1f) };
            public Vector2 mainAreasPosition = Vector2.zero;
            public Vector2 mainAreasSize = Vector2.zero;
            public Vector2 mainAreaSpacing = new Vector2(20, 20);

            [Header("对子区域")]
            public AnchorSettings pairAreasAnchor = new AnchorSettings { min = new Vector2(0, 0.3f), max = new Vector2(1, 0.6f) };
            public Vector2 pairAreasPosition = Vector2.zero;
            public Vector2 pairAreasSize = Vector2.zero;
            public Vector2 pairAreaSpacing = new Vector2(15, 15);

            [Header("大小区域")]
            public AnchorSettings bigSmallAreasAnchor = new AnchorSettings { min = new Vector2(0, 0f), max = new Vector2(1, 0.3f) };
            public Vector2 bigSmallAreasPosition = Vector2.zero;
            public Vector2 bigSmallAreasSize = Vector2.zero;
            public Vector2 bigSmallAreaSpacing = new Vector2(15, 15);

            [Header("整体设置")]
            public float containerSpacing = 10f;
        }

        /// <summary>
        /// 投注区域颜色配置
        /// </summary>
        [System.Serializable]
        public class BettingAreaColors
        {
            [Header("主要区域颜色")]
            public Color bankerColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            public Color playerColor = new Color(0.2f, 0.2f, 0.8f, 1f);
            public Color tieColor = new Color(0.2f, 0.8f, 0.2f, 1f);

            [Header("边注区域颜色")]
            public Color bankerPairColor = new Color(0.8f, 0.4f, 0.4f, 1f);
            public Color playerPairColor = new Color(0.4f, 0.4f, 0.8f, 1f);
            public Color bigColor = new Color(0.8f, 0.8f, 0.2f, 1f);
            public Color smallColor = new Color(0.8f, 0.2f, 0.8f, 1f);
        }

        /// <summary>
        /// 投注区域尺寸配置
        /// </summary>
        [System.Serializable]
        public class BettingAreaSizes
        {
            public Vector2 mainAreaSize = new Vector2(150, 100);
            public Vector2 pairAreaSize = new Vector2(100, 60);
            public Vector2 bigSmallAreaSize = new Vector2(80, 50);
        }

        /// <summary>
        /// 筹码显示配置
        /// </summary>
        [System.Serializable]
        public class ChipDisplayConfig
        {
            public Vector2 chipSize = new Vector2(30, 30);
            public Vector2 chipSpacing = new Vector2(2, 2);
            public int maxChipsPerRow = 5;
            public int maxChipsPerStack = 10;
            public float stackOffsetY = 5f;
            public bool enableChipAnimation = true;
            public float chipAnimationDuration = 0.3f;
        }

        /// <summary>
        /// 闪烁效果配置
        /// </summary>
        [System.Serializable]
        public class FlashEffectConfig
        {
            public float flashDuration = 0.5f;
            public float flashAlpha = 0.6f;
            public int flashCount = 3;
            public Color flashColor = Color.yellow;
            public AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }

        /// <summary>
        /// 投注区域交互配置
        /// </summary>
        [System.Serializable]
        public class BetAreaInteraction
        {
            public bool enableHoverEffects = true;
            public bool enableLongPress = false;
            public bool enableSoundEffects = true;
            public float longPressDuration = 1f;
            public float hoverScaleMultiplier = 1.05f;
        }

        /// <summary>
        /// 赔率显示配置
        /// </summary>
        [System.Serializable]
        public class OddsDisplayConfig
        {
            public bool showOdds = true;
            public int fontSize = 12;
            public Color textColor = Color.white;
            public bool showBackground = true;
            public Color backgroundColor = new Color(0, 0, 0, 0.5f);
        }

        /// <summary>
        /// 投注限额
        /// </summary>
        [System.Serializable]
        public class BetLimit
        {
            public float min = 10f;
            public float max = 50000f;
        }

        #endregion
    }

    // 这些组件类将在后续提供
    public class BetTargetArea : MonoBehaviour 
    { 
        public void Initialize(BaccaratBetType betType, string label, BettingAreaGenerator.BetAreaInteraction interaction, BettingAreaGenerator.BetLimit limit) { }
        public void SetHoverState(bool hover) { }
        public void PlayBetPlacedAnimation() { }
        public void UpdateBetAmount(float amount) { }
        public void SetEnabled(bool enabled) { }
        public void SetBettingEnabled(bool enabled) { }
    }

    public class ChipDisplayArea : MonoBehaviour 
    { 
        public void Initialize(BaccaratBetType betType, BettingAreaGenerator.ChipDisplayConfig config) { }
        public void AddChip(float value) { }
        public void UpdateTotalAmount(float amount) { }
        public void ShowPreviewChip(float value) { }
        public void HidePreviewChip() { }
    }
}