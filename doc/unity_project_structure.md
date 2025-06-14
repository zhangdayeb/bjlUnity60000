# Unity现代化项目目录结构指南

> 基于响应式数据绑定、模块化UI、网络管理、Mock数据、WebGL兼容性的完整项目架构

## 📁 项目目录结构

```
Assets/
├── 📁 _Core/                          # 核心系统（下划线开头，排在最前面）
│   ├── 📁 Architecture/               # 架构相关
│   │   ├── ReactiveData.cs           # 响应式数据系统
│   │   ├── ServiceLocator.cs         # 服务定位器
│   │   ├── GameDataStore.cs          # 全局数据存储
│   │   └── EventSystem.cs            # 事件系统
│   │
│   ├── 📁 Network/                    # 网络系统
│   │   ├── Interfaces/               # 接口定义
│   │   │   ├── IPlayerDataService.cs
│   │   │   └── IGameService.cs
│   │   ├── Mock/                     # Mock实现
│   │   │   ├── MockPlayerDataService.cs
│   │   │   └── MockGameService.cs
│   │   ├── Http/                     # HTTP实现
│   │   │   ├── HttpPlayerDataService.cs
│   │   │   └── HttpGameService.cs
│   │   ├── WebSocket/                # WebSocket实现
│   │   │   ├── WebSocketManager.cs
│   │   │   └── GameWebSocketHandler.cs
│   │   └── NetworkManager.cs         # 网络管理器
│   │
│   ├── 📁 Data/                       # 数据定义
│   │   ├── Types/                    # 数据类型（类似TypeScript的types）
│   │   │   ├── PlayerData.cs
│   │   │   ├── GameData.cs
│   │   │   ├── NetworkMessage.cs
│   │   │   └── Enums.cs
│   │   ├── Validators/               # 数据验证器
│   │   │   ├── DataValidator.cs
│   │   │   └── ValidationResult.cs
│   │   └── Config/                   # 配置文件
│   │       ├── EnvironmentConfig.cs
│   │       ├── GameConfig.cs
│   │       └── NetworkConfig.cs
│   │
│   ├── 📁 Audio/                      # 音频系统
│   │   ├── AudioManager.cs
│   │   ├── SoundClip.cs
│   │   ├── MusicTrack.cs
│   │   └── SafariAudioManager.cs     # Safari音频兼容
│   │
│   └── 📁 Utils/                      # 工具类
│       ├── UIUtils.cs
│       ├── WebGLUtils.cs
│       ├── SafariOptimizer.cs
│       └── PerformanceMonitor.cs
│
├── 📁 Game/                           # 游戏逻辑
│   ├── 📁 Managers/                   # 游戏管理器
│   │   ├── GameManager.cs
│   │   ├── LevelManager.cs
│   │   ├── PlayerManager.cs
│   │   └── ScoreManager.cs
│   │
│   ├── 📁 Logic/                      # 游戏逻辑
│   │   ├── BaccaratLogic.cs
│   │   ├── CardSystem.cs
│   │   ├── BettingSystem.cs
│   │   └── GameRules.cs
│   │
│   ├── 📁 Entities/                   # 游戏实体
│   │   ├── Player.cs
│   │   ├── Card.cs
│   │   ├── Room.cs
│   │   └── Bet.cs
│   │
│   └── 📁 States/                     # 游戏状态
│       ├── GameState.cs
│       ├── MenuState.cs
│       ├── PlayingState.cs
│       └── GameOverState.cs
│
├── 📁 UI/                             # UI系统
│   ├── 📁 Framework/                  # UI框架
│   │   ├── ReactiveText.cs           # 响应式UI组件
│   │   ├── ReactiveImage.cs
│   │   ├── ReactiveSlider.cs
│   │   ├── UIUpdateManager.cs
│   │   └── SmartBindingComponent.cs
│   │
│   ├── 📁 Generators/                 # 模块化UI生成器
│   │   ├── UIGeneratorBase.cs
│   │   ├── HistoryAreaGenerator.cs
│   │   ├── BettingAreaGenerator.cs
│   │   ├── ChipAreaGenerator.cs
│   │   ├── ControlAreaGenerator.cs
│   │   └── ModularBaccaratUIBuilder.cs
│   │
│   ├── 📁 Panels/                     # UI面板
│   │   ├── MainMenuPanel.cs
│   │   ├── GamePanel.cs
│   │   ├── SettingsPanel.cs
│   │   ├── PlayerInfoPanel.cs
│   │   └── ResultPanel.cs
│   │
│   ├── 📁 Components/                 # UI组件
│   │   ├── CustomButton.cs
│   │   ├── CoinDisplay.cs
│   │   ├── TimerDisplay.cs
│   │   ├── PlayerAvatar.cs
│   │   └── ChipButton.cs
│   │
│   └── 📁 Animations/                 # UI动画
│       ├── UITweener.cs
│       ├── FadeAnimation.cs
│       ├── SlideAnimation.cs
│       └── PulseAnimation.cs
│
├── 📁 Resources/                      # 资源文件
│   ├── 📁 Audio/
│   │   ├── 📁 Music/
│   │   │   ├── BackgroundMusic.ogg
│   │   │   └── MenuMusic.ogg
│   │   ├── 📁 SFX/
│   │   │   ├── ButtonClick.wav
│   │   │   ├── CardFlip.wav
│   │   │   ├── ChipPlace.wav
│   │   │   └── WinSound.wav
│   │   └── 📁 Voice/
│   │       └── GameAnnouncements/
│   │
│   ├── 📁 Textures/
│   │   ├── 📁 UI/
│   │   │   ├── Buttons/
│   │   │   ├── Backgrounds/
│   │   │   └── Icons/
│   │   ├── 📁 Cards/
│   │   │   ├── CardFaces/
│   │   │   └── CardBacks/
│   │   └── 📁 Effects/
│   │       ├── Particles/
│   │       └── Animations/
│   │
│   ├── 📁 Prefabs/
│   │   ├── 📁 UI/
│   │   │   ├── UICanvas.prefab
│   │   │   ├── HistoryItemPrefab.prefab
│   │   │   └── PlayerInfoPrefab.prefab
│   │   ├── 📁 Game/
│   │   │   ├── Card.prefab
│   │   │   ├── Chip.prefab
│   │   │   └── Table.prefab
│   │   └── 📁 Effects/
│   │       ├── WinEffect.prefab
│   │       └── CardShuffleEffect.prefab
│   │
│   └── 📁 Fonts/
│       ├── MainFont.ttf
│       └── NumberFont.ttf
│
├── 📁 Scenes/                         # 场景文件
│   ├── 📁 Development/                # 开发场景
│   │   ├── TestScene.unity
│   │   ├── UITestScene.unity
│   │   └── NetworkTestScene.unity
│   ├── MainMenu.unity
│   ├── GameScene.unity
│   └── LoadingScene.unity
│
├── 📁 Scripts/                        # 其他脚本
│   ├── 📁 Editor/                     # 编辑器扩展
│   │   ├── BuildProcessor.cs         # 构建处理器
│   │   ├── WebGLBuildProcessor.cs    # WebGL构建优化
│   │   ├── UIGeneratorEditor.cs      # UI生成器编辑器
│   │   └── AssetProcessor.cs         # 资源处理器
│   │
│   ├── 📁 ThirdParty/                 # 第三方代码
│   │   ├── 📁 WebGL/
│   │   │   ├── BrowserDetection.cs
│   │   │   └── FullscreenManager.cs
│   │   └── 📁 Networking/
│   │       └── SimpleJSON.cs
│   │
│   └── 📁 Testing/                    # 测试相关
│       ├── MockDataGenerator.cs
│       ├── NetworkTester.cs
│       └── UITester.cs
│
├── 📁 StreamingAssets/                # 流式资源
│   ├── 📁 Config/
│   │   ├── GameConfig.json
│   │   ├── NetworkConfig.json
│   │   └── LocalizationData.json
│   └── 📁 Data/
│       └── GameData.json
│
├── 📁 WebGLTemplates/                 # WebGL模板
│   ├── 📁 BaccaratTemplate/
│   │   ├── index.html
│   │   ├── TemplateData/
│   │   │   ├── style.css
│   │   │   ├── safari-compatibility.js
│   │   │   └── fullscreen-manager.js
│   │   └── thumbnail.png
│   └── 📁 MobileTemplate/
│       └── index.html
│
└── 📁 Documentation/                  # 项目文档
    ├── API.md                        # API文档
    ├── Architecture.md               # 架构说明
    ├── BuildGuide.md                 # 构建指南
    └── WebGLOptimization.md          # WebGL优化指南
```

## 🎯 设计理念

### 1. 按功能分层
- **_Core/** - 基础架构和通用系统
- **Game/** - 具体游戏逻辑
- **UI/** - 界面相关，完全模块化
- **Resources/** - 按类型组织资源

### 2. 关注点分离
```
数据流向：
WebSocketManager → GameDataStore → ReactiveData → UI Components
NetworkService → DataValidator → GameLogic → StateManager
```

### 3. 开发友好
- **Editor/** - 提升开发效率的工具
- **Testing/** - 测试和Mock数据
- **Documentation/** - 完整的项目文档

### 4. 部署优化
- **WebGLTemplates/** - 针对不同平台的模板
- **StreamingAssets/** - 可热更新的配置
- **BuildProcessor** - 自动化构建优化

## 🚀 核心组件说明

### _Core/Architecture/ - 架构核心

#### ReactiveData.cs
```csharp
// 响应式数据系统，类似Vue的reactive
public class ReactiveData<T> 
{
    private T _value;
    public event Action<T> OnValueChanged;
    
    public T Value 
    {
        get => _value;
        set 
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value)) 
            {
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
    }
}
```

#### ServiceLocator.cs
```csharp
// 服务定位器，管理依赖注入
public class ServiceLocator : MonoBehaviour 
{
    public static ServiceLocator Instance { get; private set; }
    
    public IPlayerDataService PlayerDataService { get; private set; }
    public IGameService GameService { get; private set; }
    
    void Awake() 
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }
    }
}
```

#### GameDataStore.cs
```csharp
// 全局数据存储，类似Vuex Store
public class GameDataStore : MonoBehaviour 
{
    public static GameDataStore Instance { get; private set; }
    
    [Header("玩家数据")]
    public ReactiveData<int> PlayerCoins = new ReactiveData<int>();
    public ReactiveData<int> PlayerLevel = new ReactiveData<int>();
    
    [Header("游戏数据")]
    public ReactiveData<string> GameStatus = new ReactiveData<string>();
    public ReactiveData<List<Player>> RoomPlayers = new ReactiveData<List<Player>>();
}
```

### _Core/Network/ - 网络系统

#### 接口定义
```csharp
// IPlayerDataService.cs - 玩家数据服务接口
public interface IPlayerDataService 
{
    Task<PlayerData> GetPlayerInfoAsync();
    Task<bool> UpdatePlayerCoinsAsync(int coins);
    Task<List<GameRoom>> GetGameRoomsAsync();
}

// IGameService.cs - 游戏服务接口
public interface IGameService 
{
    Task<GameStatus> JoinRoomAsync(string roomId);
    Task<bool> StartGameAsync();
    Task<GameResult> SubmitMoveAsync(GameMove move);
}
```

#### Mock实现
```csharp
// MockPlayerDataService.cs - 开发测试用Mock数据
public class MockPlayerDataService : IPlayerDataService 
{
    public async Task<PlayerData> GetPlayerInfoAsync() 
    {
        await Task.Delay(Random.Range(100, 500)); // 模拟网络延迟
        return mockPlayerData;
    }
}
```

#### HTTP实现
```csharp
// HttpPlayerDataService.cs - 生产环境HTTP实现
public class HttpPlayerDataService : IPlayerDataService 
{
    public async Task<PlayerData> GetPlayerInfoAsync() 
    {
        var response = await httpClient.GetAsync($"{baseUrl}/api/player/info");
        var json = await response.Content.ReadAsStringAsync();
        return JsonUtility.FromJson<PlayerData>(json);
    }
}
```

### _Core/Data/Types/ - 数据类型定义

#### PlayerData.cs
```csharp
// 类似TypeScript的类型定义
[System.Serializable]
public class PlayerData 
{
    [Header("基础信息")]
    public string playerId;
    public string playerName;
    
    [Header("游戏数据")]
    [Range(1, 999)]
    public int level = 1;
    
    [Min(0)]
    public int coins = 0;
    
    // 数据验证
    public bool IsValid() 
    {
        return !string.IsNullOrEmpty(playerId) && 
               level > 0 && coins >= 0;
    }
}
```

### UI/Framework/ - 响应式UI框架

#### ReactiveText.cs
```csharp
// 响应式UI组件，数据变化自动更新
public class ReactiveText : MonoBehaviour 
{
    public ReactiveData<int> intDataSource;
    public string format = "{0}";
    
    void Start() 
    {
        var textComponent = GetComponent<Text>();
        intDataSource?.OnValueChanged += (value) => 
            textComponent.text = string.Format(format, value);
    }
}
```

### UI/Generators/ - 模块化UI生成器

#### BettingAreaGenerator.cs
```csharp
// 投注区域生成器
public class BettingAreaGenerator : MonoBehaviour
{
    public BettingAreaSettings settings = new BettingAreaSettings();
    
    [ContextMenu("生成投注区域")]
    public GameObject GenerateBettingArea(Transform parent = null)
    {
        // 创建闲家、和局、庄家三个按钮
        // 完全通过代码生成，可配置样式
    }
}
```

## 🔄 项目启动流程

```csharp
// 1. ServiceLocator初始化环境
ServiceLocator.Instance.InitializeEnvironment();

// 2. 根据环境加载对应服务
if (isDevelopment) {
    playerService = new MockPlayerDataService();
} else {
    playerService = new HttpPlayerDataService();
}

// 3. 初始化响应式数据
GameDataStore.Instance.Initialize();

// 4. 加载UI（完全模块化）
ModularBaccaratUIBuilder.Instance.GenerateCompleteUI();

// 5. 启动游戏逻辑
GameManager.Instance.StartGame();
```

## 🌐 环境配置管理

### EnvironmentConfig.cs
```csharp
public enum EnvironmentType 
{
    Development,  // 开发环境（使用Mock）
    Testing,      // 测试环境（使用测试服务器）
    Production    // 生产环境（使用正式服务器）
}

[System.Serializable]
public class EnvironmentConfig 
{
    public EnvironmentType environment = EnvironmentType.Development;
    public string apiBaseUrl = "https://api.yourgame.com";
    public string websocketUrl = "wss://ws.yourgame.com";
    public bool enableDebugLog = true;
    public bool useMockData = true;
}
```

## 📱 WebGL特殊支持

### WebGLTemplates/BaccaratTemplate/
- **index.html** - 自定义WebGL模板
- **safari-compatibility.js** - Safari浏览器兼容
- **fullscreen-manager.js** - 全屏管理
- **style.css** - 响应式样式

### 特性支持
- ✅ **URL参数解析** - 支持通过URL传递游戏参数
- ✅ **iframe嵌入** - 完美支持在其他网站中嵌入
- ✅ **Safari兼容** - 特殊处理Safari的兼容性问题
- ✅ **WebRTC直播** - 支持游戏直播功能
- ✅ **双向通信** - 与父页面的消息通信

## 📋 开发工作流

### 1. 开发阶段
```bash
# 使用Mock数据
EnvironmentConfig.environment = EnvironmentType.Development
```

### 2. 测试阶段
```bash
# 切换到测试服务器
EnvironmentConfig.environment = EnvironmentType.Testing
```

### 3. 生产部署
```bash
# 切换到生产环境
EnvironmentConfig.environment = EnvironmentType.Production
```

## 🔧 构建优化

### Scripts/Editor/WebGLBuildProcessor.cs
```csharp
public class WebGLBuildProcessor 
{
    [MenuItem("Build/构建WebGL优化版")]
    static void BuildOptimizedWebGL()
    {
        // Safari内存优化
        PlayerSettings.WebGL.memorySize = 256;
        
        // 压缩优化
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        
        // 性能优化
        PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
        
        BuildPipeline.BuildPlayer(...);
    }
}
```

## 📚 文档结构

### Documentation/
- **API.md** - 完整的API接口文档
- **Architecture.md** - 架构设计说明
- **BuildGuide.md** - 构建和部署指南
- **WebGLOptimization.md** - WebGL性能优化指南

## ⚡ 核心优势

### 开发效率
- ✅ **响应式数据** - 数据变化UI自动更新
- ✅ **模块化UI** - 组件化开发，高度复用
- ✅ **Mock数据** - 前后端分离，独立开发
- ✅ **类型安全** - 严格的数据类型定义

### 团队协作
- ✅ **关注点分离** - 各层职责清晰
- ✅ **版本控制友好** - 脚本生成减少冲突
- ✅ **统一架构** - 一致的开发模式
- ✅ **完整文档** - 降低学习成本

### 扩展性
- ✅ **插件化架构** - 功能模块独立
- ✅ **环境隔离** - 多环境无缝切换
- ✅ **平台适配** - 统一代码多平台部署
- ✅ **性能优化** - 针对性优化策略

这个目录结构可以完美支撑一个现代化的Unity项目，具备前端框架的所有优势！🎮✨

---

*最后更新：2025年6月14日*