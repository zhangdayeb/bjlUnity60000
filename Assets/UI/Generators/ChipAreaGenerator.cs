// Assets/UI/Generators/ChipAreaGenerator.cs
// 筹码区域生成器 - 专门生成筹码选择和显示相关的UI组件
// 包括筹码选择按钮、筹码堆叠显示、当前筹码展示等功能

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI.Framework;
using Core.Data.Types;
using Core.Architecture;
using System.Linq;

namespace UI.Generators
{
    /// <summary>
    /// 筹码区域生成器
    /// 专门负责生成筹码相关的UI组件，包括选择器、显示器、动画效果等
    /// </summary>
    public class ChipAreaGenerator : UIGeneratorBase
    {
        #region 配置数据

        [Header("筹码区域配置")]
        [Tooltip("筹码选择器布局")]
        [SerializeField] private ChipSelectorLayout _selectorLayout = new ChipSelectorLayout();
        
        [Tooltip("筹码堆叠显示配置")]
        [SerializeField] private ChipStackConfig _stackConfig = new ChipStackConfig();
        
        [Tooltip("当前筹码显示配置")]
        [SerializeField] private CurrentChipConfig _currentChipConfig = new CurrentChipConfig();

        [Header("筹码数据")]
        [Tooltip("所有可用筹码")]
        [SerializeField] private ChipData[] _allChips = new ChipData[0];
        
        [Tooltip("用户选中的筹码")]
        [SerializeField] private ChipSelection _userSelection = new ChipSelection();

        [Header("动画配置")]
        [Tooltip("筹码切换动画")]
        [SerializeField] private ChipAnimationConfig _animationConfig = new ChipAnimationConfig();

        [Header("交互配置")]
        [Tooltip("是否支持拖拽")]
        [SerializeField] private bool _enableDragDrop = true;
        
        [Tooltip("是否显示筹码面值")]
        [SerializeField] private bool _showChipValues = true;
        
        [Tooltip("是否启用音效")]
        [SerializeField] private bool _enableSoundEffects = true;

        // 生成的组件引用
        private ChipSelector _chipSelector;
        private ChipStackDisplay _chipStackDisplay;
        private CurrentChipDisplay _currentChipDisplay;
        private ChipCounterDisplay _chipCounterDisplay;
        private ChipTotalDisplay _chipTotalDisplay;

        // 筹码管理
        private Dictionary<float, ChipButton> _chipButtons = new Dictionary<float, ChipButton>();
        private Dictionary<string, List<ChipDisplayItem>> _chipStacks = new Dictionary<string, List<ChipDisplayItem>>();
        private Queue<ChipDisplayItem> _chipPool = new Queue<ChipDisplayItem>();

        // 响应式数据绑定
        private ReactiveData<ChipData> _currentChipData;
        private ReactiveData<float> _totalChipAmount;
        private ReactiveData<Dictionary<float, int>> _chipCounts;

        #endregion

        #region 重写基类方法

        protected override List<string> GetSupportedGenerationTypes()
        {
            return new List<string>
            {
                "complete_chip_area",    // 完整筹码区域
                "chip_selector",         // 筹码选择器
                "chip_stack",           // 筹码堆叠显示
                "current_chip",         // 当前筹码显示
                "chip_counter",         // 筹码计数器
                "chip_total"            // 筹码总额显示
            };
        }

        public override void GenerateUI()
        {
            GenerateCompleteChipArea();
        }

        public override void ClearUI()
        {
            ClearAllChipComponents();
        }

        protected override void OnGenerateSpecificUI(string generationType, Dictionary<string, object> parameters)
        {
            switch (generationType)
            {
                case "complete_chip_area":
                    GenerateCompleteChipArea();
                    break;
                case "chip_selector":
                    GenerateChipSelector();
                    break;
                case "chip_stack":
                    GenerateChipStackDisplay();
                    break;
                case "current_chip":
                    GenerateCurrentChipDisplay();
                    break;
                case "chip_counter":
                    GenerateChipCounterDisplay();
                    break;
                case "chip_total":
                    GenerateChipTotalDisplay();
                    break;
                default:
                    base.OnGenerateSpecificUI(generationType, parameters);
                    break;
            }
        }

        #endregion

        #region 主要生成方法

        /// <summary>
        /// 生成完整的筹码区域
        /// </summary>
        private void GenerateCompleteChipArea()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[ChipAreaGenerator] 开始生成完整筹码区域");
            }

            // 清理现有组件
            ClearAllChipComponents();

            // 初始化筹码数据
            InitializeChipData();

            // 创建主容器
            CreateMainChipContainer();

            // 按顺序生成各个组件
            GenerateChipSelector();
            GenerateCurrentChipDisplay();
            GenerateChipStackDisplay();
            GenerateChipCounterDisplay();
            GenerateChipTotalDisplay();

            // 设置响应式绑定
            SetupReactiveBindings();

            // 初始化筹码对象池
            InitializeChipPool();

            if (_enableDebugMode)
            {
                Debug.Log("[ChipAreaGenerator] 完整筹码区域生成完成");
            }
        }

        /// <summary>
        /// 生成筹码选择按钮
        /// </summary>
        public void GenerateChipButtons()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[ChipAreaGenerator] 生成筹码选择按钮");
            }

            GameObject buttonContainer = CreateUIObject("ChipButtonContainer", _rootContainer, "chip_buttons");
            if (buttonContainer == null)
            {
                buttonContainer = CreateUIComponent<RectTransform>("ChipButtonContainer", _rootContainer, "chip_buttons").gameObject;
                
                // 设置布局
                SetRectTransform(buttonContainer.GetComponent<RectTransform>(), 
                    _selectorLayout.containerAnchor, _selectorLayout.containerPosition, _selectorLayout.containerSize);
            }

            // 应用水平布局
            ApplyLayoutGroup(buttonContainer.GetComponent<RectTransform>(), LayoutType.Horizontal, _selectorLayout.buttonSpacing);

            // 生成每个筹码按钮
            foreach (var chipData in _userSelection.selectedChips)
            {
                CreateSingleChipButton(chipData, buttonContainer.transform);
            }

            // 添加筹码选择器组件
            _chipSelector = buttonContainer.GetComponent<ChipSelector>();
            if (_chipSelector == null)
            {
                _chipSelector = buttonContainer.AddComponent<ChipSelector>();
            }
            _chipSelector.Initialize(_userSelection, _selectorLayout);
        }

        /// <summary>
        /// 生成筹码堆叠显示
        /// </summary>
        public void GenerateChipStack()
        {
            if (_enableDebugMode)
            {
                Debug.Log("[ChipAreaGenerator] 生成筹码堆叠显示");
            }

            GameObject stackContainer = CreateUIObject("ChipStackContainer", _rootContainer, "chip_stack");
            if (stackContainer == null)
            {
                stackContainer = CreateUIComponent<RectTransform>("ChipStackContainer", _rootContainer, "chip_stack").gameObject;
                
                // 设置位置和大小
                SetRectTransform(stackContainer.GetComponent<RectTransform>(), 
                    _stackConfig.containerAnchor, _stackConfig.containerPosition, _stackConfig.containerSize);
            }

            // 添加背景
            Image bgImage = stackContainer.GetComponent<Image>();
            if (bgImage == null)
            {
                bgImage = stackContainer.AddComponent<Image>();
                bgImage.color = _stackConfig.backgroundColor;
            }

            // 添加筹码堆叠显示组件
            _chipStackDisplay = stackContainer.GetComponent<ChipStackDisplay>();
            if (_chipStackDisplay == null)
            {
                _chipStackDisplay = stackContainer.AddComponent<ChipStackDisplay>();
            }
            _chipStackDisplay.Initialize(_stackConfig);

            // 生成各个投注区域的筹码堆叠区域
            GenerateBetAreaChipStacks(stackContainer.transform);
        }

        #endregion

        #region 详细生成方法

        /// <summary>
        /// 初始化筹码数据
        /// </summary>
        private void InitializeChipData()
        {
            // 如果没有配置筹码数据，使用默认配置
            if (_allChips.Length == 0)
            {
                _allChips = GenerateDefaultChipData();
            }

            // 初始化用户选择
            _userSelection.Initialize(_allChips.ToList());

            // 设置当前筹码
            if (_userSelection.selectedChips.Count > 0)
            {
                _userSelection.currentChip = _userSelection.selectedChips[0];
            }
        }

        /// <summary>
        /// 创建主筹码容器
        /// </summary>
        private void CreateMainChipContainer()
        {
            // 确保根容器有正确的设置
            if (_rootContainer != null)
            {
                // 添加容器布局组件
                VerticalLayoutGroup layout = _rootContainer.GetComponent<VerticalLayoutGroup>();
                if (layout == null)
                {
                    layout = _rootContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                }
                
                layout.spacing = 10f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandHeight = false;
            }
        }

        /// <summary>
        /// 生成筹码选择器
        /// </summary>
        private void GenerateChipSelector()
        {
            GameObject selectorContainer = CreateUIComponent<RectTransform>("ChipSelector", _rootContainer, "chip_selector").gameObject;
            
            // 设置选择器布局
            SetRectTransform(selectorContainer.GetComponent<RectTransform>(), 
                _selectorLayout.containerAnchor, _selectorLayout.containerPosition, _selectorLayout.containerSize);

            // 添加水平布局
            HorizontalLayoutGroup hLayout = selectorContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = _selectorLayout.buttonSpacing.x;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandHeight = false;

            // 生成筹码按钮
            foreach (var chipData in _userSelection.selectedChips)
            {
                CreateSingleChipButton(chipData, selectorContainer.transform);
            }

            // 添加选择器组件
            _chipSelector = selectorContainer.AddComponent<ChipSelector>();
            _chipSelector.Initialize(_userSelection, _selectorLayout);
        }

        /// <summary>
        /// 创建单个筹码按钮
        /// </summary>
        /// <param name="chipData">筹码数据</param>
        /// <param name="parent">父容器</param>
        private void CreateSingleChipButton(ChipData chipData, Transform parent)
        {
            GameObject chipButton = CreateUIObject("ChipButtonPrefab", parent, "chip_buttons");
            if (chipButton == null)
            {
                chipButton = CreateUIComponent<Button>($"ChipButton_{chipData.val}", parent, "chip_buttons").gameObject;
                
                // 设置按钮大小
                RectTransform buttonRect = chipButton.GetComponent<RectTransform>();
                buttonRect.sizeDelta = _selectorLayout.buttonSize;

                // 设置按钮图片
                Image buttonImage = chipButton.GetComponent<Image>();
                if (chipData.sprite != null)
                {
                    buttonImage.sprite = chipData.sprite;
                }
                else
                {
                    buttonImage.color = chipData.themeColor;
                }

                // 添加筹码值文本
                CreateChipValueText(chipData, chipButton.transform);

                // 添加边框效果
                CreateChipBorder(chipButton.transform, chipData.borderColor);
            }

            // 添加筹码按钮组件
            ChipButton chipButtonComponent = chipButton.GetComponent<ChipButton>();
            if (chipButtonComponent == null)
            {
                chipButtonComponent = chipButton.AddComponent<ChipButton>();
            }
            
            chipButtonComponent.Initialize(chipData, _animationConfig);
            
            // 设置点击事件
            Button button = chipButton.GetComponent<Button>();
            button.onClick.AddListener(() => OnChipButtonClicked(chipData));

            // 保存到字典
            _chipButtons[chipData.val] = chipButtonComponent;
        }

        /// <summary>
        /// 创建筹码值文本
        /// </summary>
        /// <param name="chipData">筹码数据</param>
        /// <param name="parent">父容器</param>
        private void CreateChipValueText(ChipData chipData, Transform parent)
        {
            if (!_showChipValues) return;

            GameObject textObj = CreateUIComponent<Text>("ValueText", parent, "chip_buttons").gameObject;
            
            Text valueText = textObj.GetComponent<Text>();
            valueText.text = chipData.GetFormattedValue();
            valueText.fontSize = _selectorLayout.valueFontSize;
            valueText.color = chipData.textColor;
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.fontStyle = FontStyle.Bold;

            // 设置文本位置（居中）
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 创建筹码边框
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="borderColor">边框颜色</param>
        private void CreateChipBorder(Transform parent, Color borderColor)
        {
            GameObject borderObj = CreateUIComponent<Image>("Border", parent, "chip_buttons").gameObject;
            
            Image borderImage = borderObj.GetComponent<Image>();
            borderImage.color = borderColor;
            borderImage.raycastTarget = false;

            // 设置边框位置（略大于按钮）
            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.one * -2f;
            borderRect.offsetMax = Vector2.one * 2f;
            
            // 移到最后面
            borderObj.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// 生成当前筹码显示
        /// </summary>
        private void GenerateCurrentChipDisplay()
        {
            GameObject currentChipObj = CreateUIComponent<RectTransform>("CurrentChipDisplay", _rootContainer, "current_chip").gameObject;
            
            // 设置位置和大小
            SetRectTransform(currentChipObj.GetComponent<RectTransform>(), 
                _currentChipConfig.containerAnchor, _currentChipConfig.containerPosition, _currentChipConfig.containerSize);

            // 添加背景
            Image bgImage = currentChipObj.AddComponent<Image>();
            bgImage.color = _currentChipConfig.backgroundColor;

            // 添加当前筹码图片
            GameObject chipImageObj = CreateUIComponent<Image>("ChipImage", currentChipObj.transform, "current_chip").gameObject;
            RectTransform chipImageRect = chipImageObj.GetComponent<RectTransform>();
            chipImageRect.sizeDelta = _currentChipConfig.chipSize;

            // 添加标签文本
            GameObject labelObj = CreateUIComponent<Text>("Label", currentChipObj.transform, "current_chip").gameObject;
            Text labelText = labelObj.GetComponent<Text>();
            labelText.text = "当前筹码";
            labelText.fontSize = _currentChipConfig.labelFontSize;
            labelText.color = _currentChipConfig.labelColor;
            labelText.alignment = TextAnchor.MiddleCenter;

            // 添加筹码值文本
            GameObject valueObj = CreateUIComponent<Text>("Value", currentChipObj.transform, "current_chip").gameObject;
            Text valueText = valueObj.GetComponent<Text>();
            valueText.fontSize = _currentChipConfig.valueFontSize;
            valueText.color = _currentChipConfig.valueColor;
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.fontStyle = FontStyle.Bold;

            // 应用垂直布局
            VerticalLayoutGroup layout = currentChipObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // 添加当前筹码显示组件
            _currentChipDisplay = currentChipObj.AddComponent<CurrentChipDisplay>();
            _currentChipDisplay.Initialize(_currentChipConfig, chipImageObj.GetComponent<Image>(), valueText);
        }

        /// <summary>
        /// 生成筹码堆叠显示
        /// </summary>
        private void GenerateChipStackDisplay()
        {
            GameObject stackContainer = CreateUIComponent<RectTransform>("ChipStackDisplay", _rootContainer, "chip_stack").gameObject;
            
            // 设置位置和大小
            SetRectTransform(stackContainer.GetComponent<RectTransform>(), 
                _stackConfig.containerAnchor, _stackConfig.containerPosition, _stackConfig.containerSize);

            // 添加滚动视图
            ScrollRect scrollRect = stackContainer.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;

            // 创建内容区域
            GameObject content = CreateUIComponent<RectTransform>("Content", stackContainer.transform, "chip_stack").gameObject;
            scrollRect.content = content.GetComponent<RectTransform>();

            // 添加水平布局
            HorizontalLayoutGroup hLayout = content.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = _stackConfig.stackSpacing;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;

            // 添加筹码堆叠显示组件
            _chipStackDisplay = stackContainer.AddComponent<ChipStackDisplay>();
            _chipStackDisplay.Initialize(_stackConfig, content.transform);
        }

        /// <summary>
        /// 生成投注区域筹码堆叠
        /// </summary>
        /// <param name="parent">父容器</param>
        private void GenerateBetAreaChipStacks(Transform parent)
        {
            // 为每个投注区域创建筹码堆叠显示区域
            string[] betAreas = { "banker", "player", "tie", "banker_pair", "player_pair", "big", "small" };

            foreach (string betArea in betAreas)
            {
                GameObject areaStack = CreateUIComponent<RectTransform>($"Stack_{betArea}", parent, "chip_stack").gameObject;
                
                // 添加区域标签
                GameObject labelObj = CreateUIComponent<Text>("Label", areaStack.transform, "chip_stack").gameObject;
                Text labelText = labelObj.GetComponent<Text>();
                labelText.text = GetBetAreaDisplayName(betArea);
                labelText.fontSize = 12;
                labelText.alignment = TextAnchor.MiddleCenter;

                // 添加筹码堆叠区域
                GameObject stackObj = CreateUIComponent<RectTransform>("Stack", areaStack.transform, "chip_stack").gameObject;
                
                // 应用垂直布局
                VerticalLayoutGroup layout = areaStack.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 2f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;

                // 初始化筹码堆叠列表
                _chipStacks[betArea] = new List<ChipDisplayItem>();
            }
        }

        /// <summary>
        /// 生成筹码计数器显示
        /// </summary>
        private void GenerateChipCounterDisplay()
        {
            GameObject counterContainer = CreateUIComponent<RectTransform>("ChipCounterDisplay", _rootContainer, "chip_counter").gameObject;
            
            // 添加背景
            Image bgImage = counterContainer.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.3f);

            // 添加标题
            GameObject titleObj = CreateUIComponent<Text>("Title", counterContainer.transform, "chip_counter").gameObject;
            Text titleText = titleObj.GetComponent<Text>();
            titleText.text = "筹码统计";
            titleText.fontSize = 16;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            // 创建筹码计数列表
            GameObject listContainer = CreateUIComponent<RectTransform>("CountList", counterContainer.transform, "chip_counter").gameObject;
            
            // 应用垂直布局
            VerticalLayoutGroup layout = counterContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            // 为每种筹码创建计数显示
            foreach (var chipData in _userSelection.selectedChips)
            {
                CreateChipCountItem(chipData, listContainer.transform);
            }

            // 添加筹码计数器组件
            _chipCounterDisplay = counterContainer.AddComponent<ChipCounterDisplay>();
            _chipCounterDisplay.Initialize(_userSelection.selectedChips);
        }

        /// <summary>
        /// 创建筹码计数项
        /// </summary>
        /// <param name="chipData">筹码数据</param>
        /// <param name="parent">父容器</param>
        private void CreateChipCountItem(ChipData chipData, Transform parent)
        {
            GameObject countItem = CreateUIComponent<RectTransform>($"Count_{chipData.val}", parent, "chip_counter").gameObject;
            
            // 添加水平布局
            HorizontalLayoutGroup hLayout = countItem.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10f;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;

            // 筹码图标
            GameObject iconObj = CreateUIComponent<Image>("Icon", countItem.transform, "chip_counter").gameObject;
            Image iconImage = iconObj.GetComponent<Image>();
            if (chipData.sprite != null)
            {
                iconImage.sprite = chipData.sprite;
            }
            iconImage.color = chipData.themeColor;
            
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = Vector2.one * 30f;

            // 筹码值文本
            GameObject valueObj = CreateUIComponent<Text>("Value", countItem.transform, "chip_counter").gameObject;
            Text valueText = valueObj.GetComponent<Text>();
            valueText.text = chipData.GetFormattedValue();
            valueText.fontSize = 14;
            valueText.color = Color.white;

            // 计数文本
            GameObject countObj = CreateUIComponent<Text>("Count", countItem.transform, "chip_counter").gameObject;
            Text countText = countObj.GetComponent<Text>();
            countText.text = "x0";
            countText.fontSize = 14;
            countText.color = Color.yellow;
        }

        /// <summary>
        /// 生成筹码总额显示
        /// </summary>
        private void GenerateChipTotalDisplay()
        {
            GameObject totalContainer = CreateUIComponent<RectTransform>("ChipTotalDisplay", _rootContainer, "chip_total").gameObject;
            
            // 添加背景
            Image bgImage = totalContainer.AddComponent<Image>();
            bgImage.color = new Color(0, 0.5f, 0, 0.8f);

            // 添加标签
            GameObject labelObj = CreateUIComponent<Text>("Label", totalContainer.transform, "chip_total").gameObject;
            Text labelText = labelObj.GetComponent<Text>();
            labelText.text = "总投注:";
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;

            // 添加总额显示
            GameObject amountObj = CreateUIComponent<Text>("Amount", totalContainer.transform, "chip_total").gameObject;
            Text amountText = amountObj.GetComponent<Text>();
            amountText.text = "¥0.00";
            amountText.fontSize = 20;
            amountText.color = Color.yellow;
            amountText.alignment = TextAnchor.MiddleRight;
            amountText.fontStyle = FontStyle.Bold;

            // 应用水平布局
            HorizontalLayoutGroup hLayout = totalContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;

            // 添加筹码总额显示组件
            _chipTotalDisplay = totalContainer.AddComponent<ChipTotalDisplay>();
            _chipTotalDisplay.Initialize(amountText);
        }

        #endregion

        #region 对象池管理

        /// <summary>
        /// 初始化筹码对象池
        /// </summary>
        private void InitializeChipPool()
        {
            for (int i = 0; i < _stackConfig.poolSize; i++)
            {
                GameObject chipObj = CreateUIComponent<Image>($"PooledChip_{i}", null, "chip_pool").gameObject;
                chipObj.SetActive(false);
                
                ChipDisplayItem chipItem = chipObj.AddComponent<ChipDisplayItem>();
                _chipPool.Enqueue(chipItem);
            }
        }

        /// <summary>
        /// 从对象池获取筹码显示项
        /// </summary>
        /// <returns>筹码显示项</returns>
        public ChipDisplayItem GetPooledChipItem()
        {
            if (_chipPool.Count > 0)
            {
                var item = _chipPool.Dequeue();
                item.gameObject.SetActive(true);
                return item;
            }
            else
            {
                // 池中没有可用对象，创建新的
                GameObject chipObj = CreateUIComponent<Image>("DynamicChip", null, "chip_pool").gameObject;
                return chipObj.AddComponent<ChipDisplayItem>();
            }
        }

        /// <summary>
        /// 将筹码显示项归还到对象池
        /// </summary>
        /// <param name="item">筹码显示项</param>
        public void ReturnPooledChipItem(ChipDisplayItem item)
        {
            if (item != null)
            {
                item.gameObject.SetActive(false);
                item.transform.SetParent(null);
                _chipPool.Enqueue(item);
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 筹码按钮点击事件
        /// </summary>
        /// <param name="chipData">被点击的筹码数据</param>
        private void OnChipButtonClicked(ChipData chipData)
        {
            if (_enableDebugMode)
            {
                Debug.Log($"[ChipAreaGenerator] 筹码按钮被点击: {chipData.text}");
            }

            // 更新当前选中的筹码
            _userSelection.currentChip = chipData;
            
            // 更新响应式数据
            if (_currentChipData != null)
            {
                _currentChipData.Value = chipData;
            }

            // 更新当前筹码显示
            if (_currentChipDisplay != null)
            {
                _currentChipDisplay.UpdateCurrentChip(chipData);
            }

            // 更新按钮选中状态
            UpdateChipButtonStates();

            // 播放音效
            if (_enableSoundEffects)
            {
                PlayChipSelectSound();
            }
        }

        /// <summary>
        /// 更新筹码按钮状态
        /// </summary>
        private void UpdateChipButtonStates()
        {
            foreach (var kvp in _chipButtons)
            {
                bool isSelected = kvp.Key == _userSelection.currentChip?.val;
                kvp.Value.SetSelected(isSelected);
            }
        }

        /// <summary>
        /// 播放筹码选择音效
        /// </summary>
        private void PlayChipSelectSound()
        {
            // 这里可以播放筹码选择音效
            // AudioManager.Instance?.PlaySFX("chip_select");
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

            // 初始化响应式数据
            _currentChipData = uiManager.GetOrCreateReactiveData<ChipData>("currentChip", _userSelection.currentChip);
            _totalChipAmount = uiManager.GetOrCreateReactiveData<float>("totalChipAmount", 0f);
            _chipCounts = uiManager.GetOrCreateReactiveData<Dictionary<float, int>>("chipCounts", new Dictionary<float, int>());

            // 绑定数据变化事件
            AddReactiveBinding<ChipData>(this, "currentChip", OnCurrentChipChanged);
            AddReactiveBinding<float>(this, "totalChipAmount", OnTotalAmountChanged);
            AddReactiveBinding<Dictionary<float, int>>(this, "chipCounts", OnChipCountsChanged);
        }

        /// <summary>
        /// 当前筹码变化事件
        /// </summary>
        /// <param name="newChip">新的当前筹码</param>
        private void OnCurrentChipChanged(ChipData newChip)
        {
            if (newChip != null)
            {
                _userSelection.currentChip = newChip;
                UpdateChipButtonStates();
                
                if (_currentChipDisplay != null)
                {
                    _currentChipDisplay.UpdateCurrentChip(newChip);
                }
            }
        }

        /// <summary>
        /// 总投注额变化事件
        /// </summary>
        /// <param name="newAmount">新的总投注额</param>
        private void OnTotalAmountChanged(float newAmount)
        {
            if (_chipTotalDisplay != null)
            {
                _chipTotalDisplay.UpdateTotalAmount(newAmount);
            }
        }

        /// <summary>
        /// 筹码计数变化事件
        /// </summary>
        /// <param name="newCounts">新的筹码计数</param>
        private void OnChipCountsChanged(Dictionary<float, int> newCounts)
        {
            if (_chipCounterDisplay != null)
            {
                _chipCounterDisplay.UpdateChipCounts(newCounts);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加筹码到投注区域
        /// </summary>
        /// <param name="betArea">投注区域</param>
        /// <param name="chipValue">筹码面值</param>
        /// <param name="count">数量</param>
        public void AddChipsToBetArea(string betArea, float chipValue, int count = 1)
        {
            if (!_chipStacks.ContainsKey(betArea))
            {
                Debug.LogWarning($"[ChipAreaGenerator] 未找到投注区域: {betArea}");
                return;
            }

            var chipData = _userSelection.selectedChips.Find(c => c.val == chipValue);
            if (chipData == null)
            {
                Debug.LogWarning($"[ChipAreaGenerator] 未找到筹码数据: {chipValue}");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var chipItem = GetPooledChipItem();
                chipItem.Initialize(chipData.ToDisplayData());
                _chipStacks[betArea].Add(chipItem);
                
                // 设置筹码位置（堆叠效果）
                SetChipStackPosition(chipItem, betArea, _chipStacks[betArea].Count - 1);
            }

            // 更新总计
            UpdateTotalAmount();
        }

        /// <summary>
        /// 从投注区域移除筹码
        /// </summary>
        /// <param name="betArea">投注区域</param>
        /// <param name="count">移除数量</param>
        public void RemoveChipsFromBetArea(string betArea, int count = 1)
        {
            if (!_chipStacks.ContainsKey(betArea))
                return;

            var stack = _chipStacks[betArea];
            int removeCount = Mathf.Min(count, stack.Count);

            for (int i = 0; i < removeCount; i++)
            {
                var chipItem = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                ReturnPooledChipItem(chipItem);
            }

            // 更新总计
            UpdateTotalAmount();
        }

        /// <summary>
        /// 清空所有投注区域的筹码
        /// </summary>
        public void ClearAllChips()
        {
            foreach (var kvp in _chipStacks)
            {
                foreach (var chipItem in kvp.Value)
                {
                    ReturnPooledChipItem(chipItem);
                }
                kvp.Value.Clear();
            }

            // 更新总计
            UpdateTotalAmount();
        }

        /// <summary>
        /// 获取投注区域的总金额
        /// </summary>
        /// <param name="betArea">投注区域</param>
        /// <returns>总金额</returns>
        public float GetBetAreaTotal(string betArea)
        {
            if (!_chipStacks.ContainsKey(betArea))
                return 0f;

            float total = 0f;
            foreach (var chipItem in _chipStacks[betArea])
            {
                total += chipItem.GetChipValue();
            }
            return total;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成默认筹码数据
        /// </summary>
        /// <returns>默认筹码数据数组</returns>
        private ChipData[] GenerateDefaultChipData()
        {
            return new ChipData[]
            {
                new ChipData(0, 10f, "10", "chip_10", "chip_10_bet") { themeColor = Color.red },
                new ChipData(1, 50f, "50", "chip_50", "chip_50_bet") { themeColor = Color.blue },
                new ChipData(2, 100f, "100", "chip_100", "chip_100_bet") { themeColor = Color.green },
                new ChipData(3, 500f, "500", "chip_500", "chip_500_bet") { themeColor = Color.yellow },
                new ChipData(4, 1000f, "1K", "chip_1000", "chip_1000_bet") { themeColor = Color.cyan },
                new ChipData(5, 5000f, "5K", "chip_5000", "chip_5000_bet") { themeColor = Color.magenta },
                new ChipData(6, 10000f, "10K", "chip_10000", "chip_10000_bet") { themeColor = Color.gray },
                new ChipData(7, 50000f, "50K", "chip_50000", "chip_50000_bet") { themeColor = Color.black }
            };
        }

        /// <summary>
        /// 获取投注区域显示名称
        /// </summary>
        /// <param name="betArea">投注区域ID</param>
        /// <returns>显示名称</returns>
        private string GetBetAreaDisplayName(string betArea)
        {
            return betArea switch
            {
                "banker" => "庄",
                "player" => "闲",
                "tie" => "和",
                "banker_pair" => "庄对",
                "player_pair" => "闲对",
                "big" => "大",
                "small" => "小",
                _ => betArea
            };
        }

        /// <summary>
        /// 设置筹码堆叠位置
        /// </summary>
        /// <param name="chipItem">筹码显示项</param>
        /// <param name="betArea">投注区域</param>
        /// <param name="index">堆叠索引</param>
        private void SetChipStackPosition(ChipDisplayItem chipItem, string betArea, int index)
        {
            // 计算堆叠位置（稍微偏移创建堆叠效果）
            Vector3 basePosition = Vector3.zero;
            Vector3 offset = new Vector3(
                (index % _stackConfig.maxHorizontalStack) * _stackConfig.horizontalOffset,
                (index / _stackConfig.maxHorizontalStack) * _stackConfig.verticalOffset,
                -index * 0.1f
            );

            chipItem.transform.localPosition = basePosition + offset;
        }

        /// <summary>
        /// 更新总投注金额
        /// </summary>
        private void UpdateTotalAmount()
        {
            float total = 0f;
            foreach (var kvp in _chipStacks)
            {
                total += GetBetAreaTotal(kvp.Key);
            }

            if (_totalChipAmount != null)
            {
                _totalChipAmount.Value = total;
            }
        }

        /// <summary>
        /// 清理所有筹码组件
        /// </summary>
        private void ClearAllChipComponents()
        {
            _chipSelector = null;
            _chipStackDisplay = null;
            _currentChipDisplay = null;
            _chipCounterDisplay = null;
            _chipTotalDisplay = null;

            _chipButtons.Clear();
            _chipStacks.Clear();

            // 清理对象池
            while (_chipPool.Count > 0)
            {
                var item = _chipPool.Dequeue();
                if (item != null && item.gameObject != null)
                {
                    DestroyImmediate(item.gameObject);
                }
            }
        }

        #endregion

        #region 配置数据结构

        /// <summary>
        /// 筹码选择器布局配置
        /// </summary>
        [System.Serializable]
        public class ChipSelectorLayout
        {
            public AnchorSettings containerAnchor = new AnchorSettings { min = new Vector2(0, 0), max = new Vector2(1, 0.3f) };
            public Vector2 containerPosition = Vector2.zero;
            public Vector2 containerSize = Vector2.zero;
            public Vector2 buttonSize = new Vector2(80, 80);
            public Vector2 buttonSpacing = new Vector2(10, 10);
            public int valueFontSize = 14;
        }

        /// <summary>
        /// 筹码堆叠配置
        /// </summary>
        [System.Serializable]
        public class ChipStackConfig
        {
            public AnchorSettings containerAnchor = new AnchorSettings { min = new Vector2(0, 0.3f), max = new Vector2(1, 0.7f) };
            public Vector2 containerPosition = Vector2.zero;
            public Vector2 containerSize = Vector2.zero;
            public Color backgroundColor = new Color(0, 0, 0, 0.3f);
            public float stackSpacing = 10f;
            public int maxHorizontalStack = 5;
            public float horizontalOffset = 2f;
            public float verticalOffset = 3f;
            public int poolSize = 100;
        }

        /// <summary>
        /// 当前筹码配置
        /// </summary>
        [System.Serializable]
        public class CurrentChipConfig
        {
            public AnchorSettings containerAnchor = new AnchorSettings { min = new Vector2(0, 0.7f), max = new Vector2(1, 1f) };
            public Vector2 containerPosition = Vector2.zero;
            public Vector2 containerSize = Vector2.zero;
            public Color backgroundColor = new Color(0, 0.3f, 0, 0.8f);
            public Vector2 chipSize = new Vector2(60, 60);
            public int labelFontSize = 12;
            public int valueFontSize = 16;
            public Color labelColor = Color.white;
            public Color valueColor = Color.yellow;
        }

        /// <summary>
        /// 筹码动画配置
        /// </summary>
        [System.Serializable]
        public class ChipAnimationConfig
        {
            public float selectAnimationDuration = 0.2f;
            public float scaleMultiplier = 1.2f;
            public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            public bool enableBounceEffect = true;
            public float bounceStrength = 0.1f;
        }

        #endregion
    }

    // 这些组件类将在后续提供
    public class ChipSelector : MonoBehaviour 
    { 
        public void Initialize(ChipSelection selection, ChipAreaGenerator.ChipSelectorLayout layout) { }
    }

    public class ChipStackDisplay : MonoBehaviour 
    { 
        public void Initialize(ChipAreaGenerator.ChipStackConfig config) { }
        public void Initialize(ChipAreaGenerator.ChipStackConfig config, Transform contentParent) { }
    }

    public class CurrentChipDisplay : MonoBehaviour 
    { 
        public void Initialize(ChipAreaGenerator.CurrentChipConfig config, Image chipImage, Text valueText) { }
        public void UpdateCurrentChip(ChipData chipData) { }
    }

    public class ChipCounterDisplay : MonoBehaviour 
    { 
        public void Initialize(List<ChipData> chips) { }
        public void UpdateChipCounts(Dictionary<float, int> counts) { }
    }

    public class ChipTotalDisplay : MonoBehaviour 
    { 
        public void Initialize(Text amountText) { }
        public void UpdateTotalAmount(float amount) { }
    }

    public class ChipButton : MonoBehaviour 
    { 
        public void Initialize(ChipData chipData, ChipAreaGenerator.ChipAnimationConfig animConfig) { }
        public void SetSelected(bool selected) { }
    }

    public class ChipDisplayItem : MonoBehaviour 
    { 
        public void Initialize(ChipDisplayData displayData) { }
        public float GetChipValue() { return 0f; }
    }
}