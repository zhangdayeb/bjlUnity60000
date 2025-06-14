Unity百家乐项目实施指南

基于现有百家乐JavaScript项目重构的Unity网络架构，完美对接后端API，支持Mock/真实环境无缝切换

🎯 项目概述
本项目将现有的百家乐JavaScript项目重构为Unity WebGL版本，保持后端API完全不变，实现前端技术栈的现代化升级。
核心目标：

✅ 完美对接现有百家乐后端API
✅ 复用现有业务逻辑（useBetting.js、useChips.js等）
✅ 支持Mock和真实环境无缝切换
✅ 实现响应式数据绑定和模块化UI
✅ 优化WebGL性能和Safari兼容性

📋 分阶段实施计划
Phase 1: 基础架构搭建（第1-2周）
目标：建立核心架构，确保项目可以编译运行
1.1 核心架构文件 🔥🔥🔥
Assets/_Core/Architecture/
├── ReactiveData.cs           # 响应式数据系统（对应Vue的ref）
├── ServiceLocator.cs         # 服务定位器（依赖注入容器）
└── GameDataStore.cs          # 全局数据存储（对应Pinia store）
作用：提供响应式数据绑定能力，类似Vue的数据响应系统
1.2 基础数据类型 🔥🔥🔥
Assets/_Core/Data/Types/
├── GameParams.cs             # URL游戏参数（table_id, game_type, user_id, token）
├── UserInfo.cs               # 用户信息（user_id, balance, currency等）
├── TableInfo.cs              # 台桌信息（id, video_urls, limits等）
└── BaccaratTypes.cs          # 百家乐专用类型（投注类型、牌面信息等）
作用：定义与JavaScript项目完全一致的数据结构
1.3 网络接口定义 🔥🔥
Assets/_Core/Network/Interfaces/
├── IGameApiService.cs        # 通用游戏API接口
├── IBaccaratGameService.cs   # 百家乐专用接口（对应bjlService）
└── IWebSocketService.cs      # WebSocket接口（对应optimizedSocket）
作用：定义网络服务契约，支持Mock和真实实现切换
Phase 1 验收标准：

 Unity项目可以编译通过
 基础架构类可以正常实例化
 数据类型可以正确序列化/反序列化
 ReactiveData可以触发变化事件


Phase 2: Mock服务实现（第2-3周）
目标：实现Mock服务，支持离线开发和测试
2.1 Mock实现 🔥🔥
Assets/_Core/Network/Mock/
├── MockBaccaratGameService.cs  # Mock游戏服务（模拟bjlService）
├── MockWebSocketService.cs     # Mock WebSocket（模拟optimizedSocket）
└── MockDataGenerator.cs        # Mock数据生成器
作用：提供模拟数据，支持前端独立开发
2.2 环境配置 🔥🔥
Assets/_Core/Data/Config/
├── EnvironmentConfig.cs        # 环境配置（Development/Testing/Production）
├── NetworkConfig.cs            # 网络配置（API URLs, WebSocket URLs）
└── GameConfig.cs               # 游戏配置（投注限额、筹码面值等）
作用：支持不同环境的配置管理
2.3 网络管理器 🔥🔥
Assets/_Core/Network/
└── NetworkManager.cs           # 网络管理器（统一服务注册和初始化）
作用：统一管理网络服务，根据环境选择Mock或真实服务
Phase 2 验收标准：

 Mock服务可以返回符合API格式的模拟数据
 环境配置可以正确切换Mock和真实服务
 NetworkManager可以正确初始化和注册服务
 可以模拟完整的投注流程


Phase 3: 业务逻辑层（第3-5周）
目标：实现核心业务逻辑，完美对应JavaScript composables
3.1 业务管理器 🔥🔥🔥
Assets/Game/Managers/
├── BaccaratBettingManager.cs   # 投注管理（对应useBetting.js）
│   ├── ExecuteClickBet()       # 对应executeClickBet
│   ├── ConfirmBet()            # 对应confirmBet  
│   ├── CancelBet()             # 对应cancelBet
│   └── ClearAfterGameResult()  # 对应clearAfterGameResult
│
├── ChipManager.cs              # 筹码管理（对应useChips.js）
│   ├── ConversionChip()        # 对应conversionChip
│   ├── FindMaxChip()           # 对应findMaxChip（递归算法）
│   └── HandleCurrentChip()     # 对应handleCurrentChip
│
├── ExemptManager.cs            # 免佣管理（对应useExempt.js）
│   ├── InitExemptSetting()     # 对应initExemptSetting
│   ├── ToggleExempt()          # 对应toggleExempt
│   └── GetExemptForBetting()   # 对应getExemptForBetting
│
└── BaccaratGameStateManager.cs # 游戏状态（对应useGameState.js）
    ├── ProcessGameMessage()    # 对应processGameMessage
    ├── HandleGameResult()      # 对应handleGameResult
    ├── SetFlashEffect()        # 对应setFlashEffect
    └── ShowWinningDisplay()    # 对应showWinningDisplay
作用：实现与JavaScript版本完全一致的业务逻辑
3.2 业务数据类型 🔥🔥
Assets/_Core/Data/Types/
├── BettingTypes.cs             # 投注相关类型
│   ├── BaccaratBetTarget       # 投注区域（id, label, betAmount, showChip）
│   ├── BetRequest              # 投注请求（money, rate_id）
│   └── BetResult               # 投注结果
│
├── ChipTypes.cs                # 筹码相关类型
│   ├── ChipData                # 筹码数据（val, text, src, betSrc）
│   └── ChipSelection           # 筹码选择
│
├── GameStateTypes.cs           # 游戏状态类型
│   ├── TableRunInfo            # 桌台运行信息（end_time, run_status）
│   ├── GameResultMessage       # 开牌结果消息
│   └── WinningInfo             # 中奖信息
│
└── WebSocketMessageTypes.cs    # WebSocket消息类型
    ├── CountdownMessage        # 倒计时消息
    ├── GameResultMessage       # 开牌结果消息
    └── WinningMessage          # 中奖消息
作用：定义业务逻辑所需的完整数据结构
Phase 3 验收标准：

 投注逻辑与JavaScript版本行为完全一致（使用Mock数据）
 筹码转换算法结果与JavaScript版本相同
 免佣设置可以正确保存和读取本地存储
 游戏状态流转正确（等待→投注→开牌→结果）
 防抖机制工作正常（300ms投注间隔，1000ms确认间隔）


Phase 4: 真实网络服务（第5-7周）
目标：实现真实的HTTP和WebSocket服务，对接后端API
4.1 HTTP服务实现 🔥🔥🔥
Assets/_Core/Network/Http/
├── HttpClient.cs               # 通用HTTP客户端（对应httpClient.ts）
│   ├── Get/Post/Put/Delete     # HTTP方法
│   ├── SetAuthToken()          # Token管理
│   ├── ResponseInterceptors    # 响应拦截器
│   └── ErrorHandling           # 错误处理
│
├── HttpBaccaratGameService.cs  # 百家乐HTTP服务（对应bjlService）
│   ├── GetUserInfo()           # 获取用户信息
│   ├── GetTableInfo()          # 获取台桌信息
│   ├── PlaceBets()             # 提交投注（/bjl/bet/order）
│   └── GetBettingHistory()     # 获取投注历史
│
└── HttpPlayerDataService.cs    # 玩家数据HTTP服务
作用：实现与现有后端API的HTTP通信
4.2 WebSocket服务实现 🔥🔥🔥
Assets/_Core/Network/WebSocket/
├── WebSocketManager.cs         # WebSocket连接管理（对应OptimizedSocketManager）
│   ├── Connect/Disconnect      # 连接管理
│   ├── AutoReconnect           # 自动重连
│   ├── Heartbeat               # 心跳检测
│   └── MessageQueue            # 消息队列
│
├── BaccaratWebSocketService.cs # 百家乐WebSocket服务（对应useSocket.js）
│   ├── InitSocket()            # 初始化连接
│   ├── HandleMessage()         # 消息处理
│   └── SendMessage()           # 发送消息
│
└── GameMessageDispatcher.cs    # 消息分发器
    ├── ParseTableInfo()        # 解析桌台信息
    ├── ParseGameResult()       # 解析开牌结果
    └── ParseWinningData()      # 解析中奖数据
作用：实现WebSocket实时通信，接收服务器推送
4.3 错误处理和重试机制 🔥
Assets/_Core/Network/Utils/
├── NetworkErrorHandler.cs     # 网络错误处理
├── RetryManager.cs            # 重试机制
└── ConnectionMonitor.cs       # 连接监控
作用：提供稳定的网络服务
Phase 4 验收标准：

 可以成功连接真实的后端API
 HTTP请求格式与现有API完全匹配
 WebSocket可以接收和正确解析真实消息
 Token错误处理与JavaScript版本一致
 网络错误和重连机制工作正常
 完整投注流程可以成功执行


Phase 5: UI框架和生成器（第7-9周）
目标：实现响应式UI和模块化生成器
5.1 响应式UI框架 🔥🔥
Assets/UI/Framework/
├── ReactiveText.cs             # 响应式文本组件
│   └── 绑定ReactiveData<int/string> 自动更新显示
│
├── ReactiveImage.cs            # 响应式图片组件
│   └── 绑定ReactiveData<Sprite> 自动更新图片
│
├── ReactiveButton.cs           # 响应式按钮组件
│   └── 绑定ReactiveData<bool> 控制可点击状态
│
└── UIUpdateManager.cs          # UI更新管理器
    └── 统一管理UI组件的数据绑定和更新
作用：实现数据变化时UI自动更新，类似Vue的数据绑定
5.2 UI生成器 🔥
Assets/UI/Generators/
├── UIGeneratorBase.cs          # UI生成器基类
├── BaccaratTableGenerator.cs   # 百家乐桌台生成器
│   ├── GenerateBettingAreas()  # 生成投注区域（庄、闲、和、对子）
│   ├── GenerateChipDisplay()   # 生成筹码显示
│   └── GenerateVideoArea()     # 生成视频区域
│
├── ChipAreaGenerator.cs        # 筹码区域生成器
│   ├── GenerateChipButtons()   # 生成筹码选择按钮
│   └── GenerateChipStack()     # 生成筹码堆叠显示
│
└── BettingAreaGenerator.cs     # 投注区域生成器
    ├── CreateBetTarget()       # 创建投注目标
    ├── SetupChipDisplay()      # 设置筹码显示
    └── SetupFlashEffect()      # 设置闪烁效果
作用：通过代码动态生成UI，支持不同设备适配
5.3 UI组件库 🔥
Assets/UI/Components/
├── BaccaratBetButton.cs        # 百家乐投注按钮
├── ChipButton.cs               # 筹码按钮
├── TimerDisplay.cs             # 倒计时显示
├── BalanceDisplay.cs           # 余额显示
└── WinningPopup.cs             # 中奖弹窗
作用：提供可复用的UI组件
Phase 5 验收标准：

 UI可以响应数据变化自动更新
 可以通过代码动态生成完整的游戏界面
 UI组件与业务逻辑完全解耦
 支持不同分辨率和设备的适配
 筹码显示和投注区域与JavaScript版本视觉一致


Phase 6: 音频和效果（第9-10周）
目标：实现音频管理和视觉效果
6.1 音频系统 🔥
Assets/_Core/Audio/
├── AudioManager.cs             # 音频管理器
│   ├── PlayBetSound()          # 播放下注音效
│   ├── PlayConfirmSound()      # 播放确认音效
│   ├── PlayWinSound()          # 播放中奖音效
│   └── PlayOpenCardSequence() # 播放开牌音效序列
│
├── BaccaratAudioController.cs  # 百家乐音频控制器
│   ├── PlayStartBetSound()     # 播放开始下注音效
│   ├── PlayStopBetSound()      # 播放停止下注音效
│   └── PlayWinSoundByAmount()  # 根据金额播放中奖音效
│
└── SafariAudioManager.cs       # Safari兼容音频
    └── 处理Safari浏览器的音频播放限制
作用：提供完整的音频体验，与JavaScript版本一致
6.2 视觉效果 🔥
Assets/UI/Effects/
├── FlashEffect.cs              # 闪烁效果（开牌时中奖区域闪烁）
├── ChipAnimation.cs            # 筹码动画（下注时筹码飞入动画）
├── WinningEffect.cs            # 中奖特效（粒子效果、光效）
└── CountdownAnimation.cs       # 倒计时动画
作用：提供视觉反馈，提升用户体验
Phase 6 验收标准：

 音频可以正常播放（包括Safari兼容）
 视觉效果流畅且符合游戏需求
 音效触发时机与JavaScript版本一致
 闪烁效果与开牌结果正确对应


Phase 7: WebGL优化和部署（第10-11周）
目标：优化WebGL性能和部署配置
7.1 WebGL优化 🔥
Assets/_Core/Utils/
├── WebGLUtils.cs               # WebGL工具类
│   ├── ParseUrlParams()        # 解析URL参数
│   ├── PostMessageToParent()   # 与父页面通信
│   └── DetectBrowser()         # 检测浏览器类型
│
├── SafariOptimizer.cs          # Safari优化
│   ├── OptimizeMemoryUsage()   # 内存优化
│   ├── HandleTouchEvents()     # 触摸事件优化
│   └── AudioContextFix()       # 音频上下文修复
│
└── PerformanceMonitor.cs       # 性能监控
    ├── MonitorFrameRate()      # 监控帧率
    ├── MonitorMemoryUsage()    # 监控内存使用
    └── ReportPerformance()     # 性能报告
作用：确保WebGL版本性能和兼容性
7.2 构建配置 🔥
Assets/Scripts/Editor/
├── WebGLBuildProcessor.cs      # WebGL构建处理器
│   ├── OptimizeForSafari()     # Safari优化设置
│   ├── CompressAssets()        # 资源压缩
│   └── GenerateBuildReport()   # 构建报告
│
└── AssetProcessor.cs           # 资源处理器
    ├── OptimizeTextures()      # 纹理优化
    ├── CompressAudio()         # 音频压缩
    └── MinifyCode()            # 代码压缩
作用：自动化构建优化
7.3 WebGL模板 🔥
WebGLTemplates/BaccaratTemplate/
├── index.html                  # 自定义模板
├── TemplateData/
│   ├── style.css               # 样式（响应式布局）
│   ├── safari-compatibility.js # Safari兼容脚本
│   ├── unity-bridge.js         # Unity通信桥接
│   └── error-handler.js        # 错误处理
└── Assets/
    ├── favicon.ico             # 网站图标
    └── loading-spinner.svg     # 加载动画
作用：提供优化的WebGL运行环境
Phase 7 验收标准：

 WebGL构建可以正常运行
 Safari浏览器兼容性良好
 性能满足游戏需求（60fps）
 资源加载优化，启动时间合理
 可以正确解析URL参数并初始化游戏


📊 项目文件创建清单
优先级说明

🔥🔥🔥 = 核心文件，必须优先创建
🔥🔥 = 重要文件，早期需要
🔥 = 普通文件，可以延后

完整文件清单
Assets/
├── _Core/                              # 核心系统
│   ├── Architecture/                   # 🔥🔥🔥 架构核心
│   │   ├── ReactiveData.cs
│   │   ├── ServiceLocator.cs
│   │   └── GameDataStore.cs
│   │
│   ├── Network/                        # 网络系统
│   │   ├── Interfaces/                 # 🔥🔥 接口定义
│   │   │   ├── IGameApiService.cs
│   │   │   ├── IBaccaratGameService.cs
│   │   │   └── IWebSocketService.cs
│   │   │
│   │   ├── Mock/                       # 🔥🔥 Mock实现
│   │   │   ├── MockBaccaratGameService.cs
│   │   │   ├── MockWebSocketService.cs
│   │   │   └── MockDataGenerator.cs
│   │   │
│   │   ├── Http/                       # 🔥🔥🔥 HTTP实现
│   │   │   ├── HttpClient.cs
│   │   │   ├── HttpBaccaratGameService.cs
│   │   │   └── HttpPlayerDataService.cs
│   │   │
│   │   ├── WebSocket/                  # 🔥🔥🔥 WebSocket实现
│   │   │   ├── WebSocketManager.cs
│   │   │   ├── BaccaratWebSocketService.cs
│   │   │   └── GameMessageDispatcher.cs
│   │   │
│   │   ├── Utils/                      # 🔥 网络工具
│   │   │   ├── NetworkErrorHandler.cs
│   │   │   ├── RetryManager.cs
│   │   │   └── ConnectionMonitor.cs
│   │   │
│   │   └── NetworkManager.cs           # 🔥🔥 网络管理器
│   │
│   ├── Data/                           # 数据定义
│   │   ├── Types/                      # 🔥🔥🔥 数据类型
│   │   │   ├── GameParams.cs
│   │   │   ├── UserInfo.cs
│   │   │   ├── TableInfo.cs
│   │   │   ├── BaccaratTypes.cs
│   │   │   ├── BettingTypes.cs
│   │   │   ├── ChipTypes.cs
│   │   │   ├── GameStateTypes.cs
│   │   │   └── WebSocketMessageTypes.cs
│   │   │
│   │   ├── Config/                     # 🔥🔥 配置文件
│   │   │   ├── EnvironmentConfig.cs
│   │   │   ├── NetworkConfig.cs
│   │   │   └── GameConfig.cs
│   │   │
│   │   └── Validators/                 # 🔥 数据验证
│   │       ├── DataValidator.cs
│   │       └── ValidationResult.cs
│   │
│   ├── Audio/                          # 🔥 音频系统
│   │   ├── AudioManager.cs
│   │   ├── BaccaratAudioController.cs
│   │   └── SafariAudioManager.cs
│   │
│   └── Utils/                          # 🔥 工具类
│       ├── WebGLUtils.cs
│       ├── SafariOptimizer.cs
│       └── PerformanceMonitor.cs
│
├── Game/                               # 游戏逻辑
│   ├── Managers/                       # 🔥🔥🔥 业务管理器
│   │   ├── BaccaratBettingManager.cs
│   │   ├── ChipManager.cs
│   │   ├── ExemptManager.cs
│   │   └── BaccaratGameStateManager.cs
│   │
│   ├── Logic/                          # 🔥 游戏逻辑
│   │   ├── BaccaratLogic.cs
│   │   ├── CardSystem.cs
│   │   └── GameRules.cs
│   │
│   └── Entities/                       # 🔥 游戏实体
│       ├── Player.cs
│       ├── Card.cs
│       └── Bet.cs
│
├── UI/                                 # UI系统
│   ├── Framework/                      # 🔥🔥 UI框架
│   │   ├── ReactiveText.cs
│   │   ├── ReactiveImage.cs
│   │   ├── ReactiveButton.cs
│   │   └── UIUpdateManager.cs
│   │
│   ├── Generators/                     # 🔥 UI生成器
│   │   ├── UIGeneratorBase.cs
│   │   ├── BaccaratTableGenerator.cs
│   │   ├── ChipAreaGenerator.cs
│   │   └── BettingAreaGenerator.cs
│   │
│   ├── Components/                     # 🔥 UI组件
│   │   ├── BaccaratBetButton.cs
│   │   ├── ChipButton.cs
│   │   ├── TimerDisplay.cs
│   │   ├── BalanceDisplay.cs
│   │   └── WinningPopup.cs
│   │
│   └── Effects/                        # 🔥 视觉效果
│       ├── FlashEffect.cs
│       ├── ChipAnimation.cs
│       ├── WinningEffect.cs
│       └── CountdownAnimation.cs
│
├── Scripts/                            # 其他脚本
│   ├── Editor/                         # 🔥 编辑器扩展
│   │   ├── WebGLBuildProcessor.cs
│   │   └── AssetProcessor.cs
│   │
│   └── Testing/                        # 🔥 测试相关
│       ├── MockDataGenerator.cs
│       └── NetworkTester.cs
│
├── Scenes/                             # 场景文件
│   ├── Development/                    # 🔥 开发场景
│   │   ├── TestScene.unity
│   │   └── NetworkTestScene.unity
│   ├── MainMenu.unity                  # 🔥🔥
│   └── GameScene.unity                 # 🔥🔥🔥
│
└── StreamingAssets/                    # 🔥 流式资源
    └── Config/
        ├── GameConfig.json
        └── NetworkConfig.json

WebGLTemplates/                         # 🔥 WebGL模板
└── BaccaratTemplate/
    ├── index.html
    ├── TemplateData/
    │   ├── style.css
    │   ├── safari-compatibility.js
    │   ├── unity-bridge.js
    │   └── error-handler.js
    └── Assets/
        ├── favicon.ico
        └── loading-spinner.svg
🚀 开始实施建议
第一步：创建基础架构（本周）

创建Unity新项目
按Phase 1清单创建核心架构文件
实现ReactiveData基础功能
设置基础的项目结构

第二步：实现Mock服务（下周）

按Phase 2清单创建Mock相关文件
实现基础的Mock数据生成
测试服务注册和环境切换

第三步：业务逻辑迁移

逐个迁移JavaScript composables到C#
确保业务逻辑与原版本一致
使用Mock数据测试所有功能

这个实施指南确保了项目的有序进行，同时保持了与现有百家乐项目的完美兼容！