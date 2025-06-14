using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SlotMachine.Editor
{
    /// <summary>
    /// 资源处理器 - 自动优化和处理项目资源
    /// </summary>
    public class AssetProcessor : AssetPostprocessor, IPreprocessBuildWithReport
    {
        // 构建回调优先级
        public int callbackOrder => 0;
        
        // 纹理优化设置
        private static readonly Dictionary<string, TextureImporterSettings> TextureSettings = new Dictionary<string, TextureImporterSettings>
        {
            ["UI"] = new TextureImporterSettings
            {
                maxTextureSize = 2048,
                textureCompression = TextureImporterCompression.Compressed,
                format = TextureImporterFormat.ASTC_6x6,
                generateMipMaps = false,
                isReadable = false
            },
            ["Sprite"] = new TextureImporterSettings
            {
                maxTextureSize = 1024,
                textureCompression = TextureImporterCompression.Compressed,
                format = TextureImporterFormat.ASTC_4x4,
                generateMipMaps = false,
                isReadable = false
            },
            ["Background"] = new TextureImporterSettings
            {
                maxTextureSize = 2048,
                textureCompression = TextureImporterCompression.Compressed,
                format = TextureImporterFormat.ASTC_8x8,
                generateMipMaps = true,
                isReadable = false
            }
        };
        
        // 音频优化设置
        private static readonly Dictionary<string, AudioImporterSettings> AudioSettings = new Dictionary<string, AudioImporterSettings>
        {
            ["Music"] = new AudioImporterSettings
            {
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.7f,
                loadType = AudioClipLoadType.Streaming,
                sampleRateSetting = AudioSampleRateSetting.OptimizeSize
            },
            ["SFX"] = new AudioImporterSettings
            {
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.8f,
                loadType = AudioClipLoadType.CompressedInMemory,
                sampleRateSetting = AudioSampleRateSetting.OptimizeSize
            },
            ["Voice"] = new AudioImporterSettings
            {
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.9f,
                loadType = AudioClipLoadType.CompressedInMemory,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate
            }
        };
        
        #region Texture Processing
        
        /// <summary>
        /// 预处理纹理
        /// </summary>
        void OnPreprocessTexture()
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            
            // 根据路径确定纹理类型和优化设置
            string assetPath = textureImporter.assetPath.ToLower();
            string category = DetermineTextureCategory(assetPath);
            
            if (TextureSettings.ContainsKey(category))
            {
                ApplyTextureSettings(textureImporter, TextureSettings[category], category);
                Debug.Log($"[AssetProcessor] 应用纹理优化: {assetPath} -> {category}");
            }
        }
        
        /// <summary>
        /// 确定纹理类别
        /// </summary>
        private string DetermineTextureCategory(string assetPath)
        {
            if (assetPath.Contains("/ui/") || assetPath.Contains("/gui/"))
                return "UI";
            else if (assetPath.Contains("/background/") || assetPath.Contains("/bg/"))
                return "Background";
            else if (assetPath.Contains("/sprite/") || assetPath.Contains("/icon/"))
                return "Sprite";
            else
                return "Sprite"; // 默认类别
        }
        
        /// <summary>
        /// 应用纹理设置
        /// </summary>
        private void ApplyTextureSettings(TextureImporter importer, TextureImporterSettings settings, string category)
        {
            // 基础设置
            importer.textureType = category == "UI" ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            
            // 生成Mipmap设置
            importer.mipmapEnabled = settings.generateMipMaps;
            
            // 读取设置
            importer.isReadable = settings.isReadable;
            
            // 获取平台设置
            TextureImporterPlatformSettings webglSettings = importer.GetPlatformTextureSettings("WebGL");
            webglSettings.overridden = true;
            webglSettings.maxTextureSize = settings.maxTextureSize;
            webglSettings.textureCompression = settings.textureCompression;
            webglSettings.format = GetWebGLTextureFormat(settings.format);
            
            // 应用平台设置
            importer.SetPlatformTextureSettings(webglSettings);
            
            // Sprite特殊设置
            if (category == "UI" || category == "Sprite")
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
            }
        }
        
        /// <summary>
        /// 获取WebGL纹理格式
        /// </summary>
        private TextureImporterFormat GetWebGLTextureFormat(TextureImporterFormat format)
        {
            // WebGL平台优化的纹理格式映射
            switch (format)
            {
                case TextureImporterFormat.ASTC_4x4:
                    return TextureImporterFormat.DXT5; // WebGL fallback
                case TextureImporterFormat.ASTC_6x6:
                    return TextureImporterFormat.DXT5;
                case TextureImporterFormat.ASTC_8x8:
                    return TextureImporterFormat.DXT1;
                default:
                    return TextureImporterFormat.Automatic;
            }
        }
        
        #endregion
        
        #region Audio Processing
        
        /// <summary>
        /// 预处理音频
        /// </summary>
        void OnPreprocessAudio()
        {
            AudioImporter audioImporter = (AudioImporter)assetImporter;
            
            // 根据路径确定音频类型
            string assetPath = audioImporter.assetPath.ToLower();
            string category = DetermineAudioCategory(assetPath);
            
            if (AudioSettings.ContainsKey(category))
            {
                ApplyAudioSettings(audioImporter, AudioSettings[category], category);
                Debug.Log($"[AssetProcessor] 应用音频优化: {assetPath} -> {category}");
            }
        }
        
        /// <summary>
        /// 确定音频类别
        /// </summary>
        private string DetermineAudioCategory(string assetPath)
        {
            if (assetPath.Contains("/music/") || assetPath.Contains("/bgm/"))
                return "Music";
            else if (assetPath.Contains("/voice/") || assetPath.Contains("/speech/"))
                return "Voice";
            else
                return "SFX"; // 默认为音效
        }
        
        /// <summary>
        /// 应用音频设置
        /// </summary>
        private void ApplyAudioSettings(AudioImporter importer, AudioImporterSettings settings, string category)
        {
            // 获取WebGL平台设置
            AudioImporterSampleSettings webglSettings = importer.GetOverrideSampleSettings("WebGL");
            webglSettings.compressionFormat = settings.compressionFormat;
            webglSettings.quality = settings.quality;
            webglSettings.loadType = settings.loadType;
            webglSettings.sampleRateSetting = settings.sampleRateSetting;
            
            // 音乐特殊设置
            if (category == "Music")
            {
                webglSettings.loadType = AudioClipLoadType.Streaming;
                webglSettings.compressionFormat = AudioCompressionFormat.Vorbis;
            }
            
            // 应用设置
            importer.SetOverrideSampleSettings("WebGL", webglSettings);
            
            // 全局设置
            importer.forceToMono = category == "SFX"; // 音效强制单声道
            importer.normalize = true;
            importer.loadInBackground = category == "Music";
        }
        
        #endregion
        
        #region Model Processing
        
        /// <summary>
        /// 预处理模型
        /// </summary>
        void OnPreprocessModel()
        {
            ModelImporter modelImporter = (ModelImporter)assetImporter;
            
            // 基础模型优化
            OptimizeModel(modelImporter);
        }
        
        /// <summary>
        /// 优化模型设置
        /// </summary>
        private void OptimizeModel(ModelImporter importer)
        {
            // 网格优化
            importer.meshOptimization = MeshOptimizationFlags.Everything;
            importer.optimizeMeshVertices = true;
            importer.optimizeMeshPolygons = true;
            
            // 动画优化
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.animationPositionError = 0.5f;
            importer.animationRotationError = 0.5f;
            importer.animationScaleError = 0.5f;
            
            // 材质设置
            importer.materialImportMode = ModelImporterMaterialImportMode.None; // 不导入材质
            
            // 其他设置
            importer.importBlendShapes = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            
            Debug.Log($"[AssetProcessor] 优化模型: {importer.assetPath}");
        }
        
        #endregion
        
        #region Build Preprocessing
        
        /// <summary>
        /// 构建前预处理
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;
                
            Debug.Log("[AssetProcessor] 开始构建前资源优化...");
            
            try
            {
                // 优化所有资源
                OptimizeAllAssets();
                
                // 清理未使用的资源
                CleanupUnusedAssets();
                
                // 验证资源
                ValidateAssets();
                
                Debug.Log("[AssetProcessor] 构建前资源优化完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AssetProcessor] 构建前优化失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 优化所有资源
        /// </summary>
        private static void OptimizeAllAssets()
        {
            Debug.Log("[AssetProcessor] 批量优化资源...");
            
            // 查找所有纹理资源
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D");
            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    OptimizeTextureForWebGL(importer);
                }
            }
            
            // 查找所有音频资源
            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip");
            foreach (string guid in audioGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer != null)
                {
                    OptimizeAudioForWebGL(importer);
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// 为WebGL优化纹理
        /// </summary>
        private static void OptimizeTextureForWebGL(TextureImporter importer)
        {
            bool changed = false;
            
            // 获取WebGL设置
            TextureImporterPlatformSettings webglSettings = importer.GetPlatformTextureSettings("WebGL");
            
            if (!webglSettings.overridden)
            {
                webglSettings.overridden = true;
                webglSettings.maxTextureSize = 2048;
                webglSettings.textureCompression = TextureImporterCompression.Compressed;
                webglSettings.format = TextureImporterFormat.DXT5;
                
                importer.SetPlatformTextureSettings(webglSettings);
                changed = true;
            }
            
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
        
        /// <summary>
        /// 为WebGL优化音频
        /// </summary>
        private static void OptimizeAudioForWebGL(AudioImporter importer)
        {
            bool changed = false;
            
            // 获取WebGL设置
            AudioImporterSampleSettings webglSettings = importer.GetOverrideSampleSettings("WebGL");
            
            if (webglSettings.compressionFormat == AudioCompressionFormat.PCM)
            {
                webglSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                webglSettings.quality = 0.7f;
                webglSettings.loadType = AudioClipLoadType.CompressedInMemory;
                
                importer.SetOverrideSampleSettings("WebGL", webglSettings);
                changed = true;
            }
            
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
        
        /// <summary>
        /// 清理未使用的资源
        /// </summary>
        private static void CleanupUnusedAssets()
        {
            Debug.Log("[AssetProcessor] 清理未使用的资源...");
            
            // 查找未使用的资源
            string[] unusedAssets = AssetDatabase.GetDependencies(GetAllScenePaths(), false)
                .Where(path => !IsAssetUsed(path))
                .ToArray();
            
            if (unusedAssets.Length > 0)
            {
                Debug.Log($"[AssetProcessor] 发现 {unusedAssets.Length} 个未使用的资源");
                
                foreach (string asset in unusedAssets.Take(10)) // 限制日志输出
                {
                    Debug.Log($"[AssetProcessor] 未使用资源: {asset}");
                }
            }
        }
        
        /// <summary>
        /// 检查资源是否被使用
        /// </summary>
        private static bool IsAssetUsed(string assetPath)
        {
            // 排除特定文件夹
            string[] excludeFolders = { "Editor", "Documentation", "Testing" };
            
            foreach (string folder in excludeFolders)
            {
                if (assetPath.Contains($"/{folder}/"))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 验证资源
        /// </summary>
        private static void ValidateAssets()
        {
            Debug.Log("[AssetProcessor] 验证资源...");
            
            int errorCount = 0;
            int warningCount = 0;
            
            // 验证纹理
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D");
            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!ValidateTexture(path))
                    errorCount++;
            }
            
            // 验证音频
            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip");
            foreach (string guid in audioGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!ValidateAudio(path))
                    warningCount++;
            }
            
            Debug.Log($"[AssetProcessor] 资源验证完成 - 错误: {errorCount}, 警告: {warningCount}");
        }
        
        /// <summary>
        /// 验证纹理
        /// </summary>
        private static bool ValidateTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return false;
            
            // 检查纹理大小
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                if (texture.width > 4096 || texture.height > 4096)
                {
                    Debug.LogWarning($"[AssetProcessor] 纹理过大: {path} ({texture.width}x{texture.height})");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 验证音频
        /// </summary>
        private static bool ValidateAudio(string path)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return false;
            
            // 检查音频设置
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
            {
                if (clip.length > 300f) // 5分钟
                {
                    Debug.LogWarning($"[AssetProcessor] 音频文件过长: {path} ({clip.length:F2}s)");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取所有场景路径
        /// </summary>
        private static string[] GetAllScenePaths()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }
        
        #endregion
        
        #region Asset Database Events
        
        /// <summary>
        /// 资源导入完成后的处理
        /// </summary>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool hasRelevantChanges = false;
            
            // 检查是否有相关资源变化
            foreach (string asset in importedAssets)
            {
                if (IsRelevantAsset(asset))
                {
                    hasRelevantChanges = true;
                    break;
                }
            }
            
            if (hasRelevantChanges)
            {
                // 延迟刷新，避免在导入过程中执行
                EditorApplication.delayCall += () =>
                {
                    ValidateImportedAssets(importedAssets);
                };
            }
        }
        
        /// <summary>
        /// 检查是否为相关资源
        /// </summary>
        private static bool IsRelevantAsset(string assetPath)
        {
            string extension = Path.GetExtension(assetPath).ToLower();
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg" ||
                   extension == ".wav" || extension == ".mp3" || extension == ".ogg" ||
                   extension == ".fbx" || extension == ".obj";
        }
        
        /// <summary>
        /// 验证导入的资源
        /// </summary>
        private static void ValidateImportedAssets(string[] importedAssets)
        {
            foreach (string asset in importedAssets)
            {
                if (IsRelevantAsset(asset))
                {
                    // 检查资源是否符合命名规范
                    if (!ValidateAssetNaming(asset))
                    {
                        Debug.LogWarning($"[AssetProcessor] 资源命名不符合规范: {asset}");
                    }
                    
                    // 检查资源大小
                    FileInfo fileInfo = new FileInfo(asset);
                    if (fileInfo.Exists && fileInfo.Length > 10 * 1024 * 1024) // 10MB
                    {
                        Debug.LogWarning($"[AssetProcessor] 资源文件过大: {asset} ({fileInfo.Length / 1024 / 1024}MB)");
                    }
                }
            }
        }
        
        /// <summary>
        /// 验证资源命名
        /// </summary>
        private static bool ValidateAssetNaming(string assetPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            
            // 检查是否包含空格
            if (fileName.Contains(" "))
            {
                return false;
            }
            
            // 检查是否包含特殊字符
            char[] invalidChars = { '#', '%', '&', '{', '}', '\\', '<', '>', '*', '?', '/', '$', '!', '\'', '"', ':', '@', '+', '`', '|', '=' };
            if (fileName.IndexOfAny(invalidChars) >= 0)
            {
                return false;
            }
            
            return true;
        }
        
        #endregion
        
        #region Menu Items
        
        /// <summary>
        /// 手动优化所有资源
        /// </summary>
        [MenuItem("SlotMachine/Assets/Optimize All Assets")]
        public static void ManualOptimizeAllAssets()
        {
            Debug.Log("[AssetProcessor] 开始手动优化所有资源...");
            
            if (EditorUtility.DisplayDialog("优化资源", 
                "这将优化项目中的所有纹理和音频资源。\n此操作可能需要一些时间，是否继续？", 
                "确定", "取消"))
            {
                OptimizeAllAssets();
                EditorUtility.DisplayDialog("完成", "资源优化完成！", "确定");
            }
        }
        
        /// <summary>
        /// 验证项目资源
        /// </summary>
        [MenuItem("SlotMachine/Assets/Validate Assets")]
        public static void ManualValidateAssets()
        {
            Debug.Log("[AssetProcessor] 开始验证项目资源...");
            ValidateAssets();
            EditorUtility.DisplayDialog("完成", "资源验证完成！请查看控制台输出。", "确定");
        }
        
        /// <summary>
        /// 清理未使用资源
        /// </summary>
        [MenuItem("SlotMachine/Assets/Cleanup Unused Assets")]
        public static void ManualCleanupAssets()
        {
            Debug.Log("[AssetProcessor] 开始清理未使用资源...");
            CleanupUnusedAssets();
            EditorUtility.DisplayDialog("完成", "未使用资源检查完成！请查看控制台输出。", "确定");
        }
        
        /// <summary>
        /// 生成资源报告
        /// </summary>
        [MenuItem("SlotMachine/Assets/Generate Asset Report")]
        public static void GenerateAssetReport()
        {
            Debug.Log("[AssetProcessor] 生成资源报告...");
            
            var report = new AssetReport();
            report.Generate();
            
            string reportPath = Path.Combine(Application.dataPath, "..", "AssetReport.json");
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            
            Debug.Log($"[AssetProcessor] 资源报告已生成: {reportPath}");
            EditorUtility.DisplayDialog("完成", $"资源报告已生成:\n{reportPath}", "确定");
        }
        
        #endregion
        
        #region Helper Classes
        
        /// <summary>
        /// 纹理导入设置
        /// </summary>
        [System.Serializable]
        public class TextureImporterSettings
        {
            public int maxTextureSize;
            public TextureImporterCompression textureCompression;
            public TextureImporterFormat format;
            public bool generateMipMaps;
            public bool isReadable;
        }
        
        /// <summary>
        /// 音频导入设置
        /// </summary>
        [System.Serializable]
        public class AudioImporterSettings
        {
            public AudioCompressionFormat compressionFormat;
            public float quality;
            public AudioClipLoadType loadType;
            public AudioSampleRateSetting sampleRateSetting;
        }
        
        /// <summary>
        /// 资源报告
        /// </summary>
        [System.Serializable]
        public class AssetReport
        {
            public string generatedAt;
            public int totalTextures;
            public int totalAudioClips;
            public int totalModels;
            public long totalAssetSize;
            public AssetTypeReport[] typeReports;
            
            public void Generate()
            {
                generatedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                // 统计纹理
                string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D");
                totalTextures = textureGuids.Length;
                
                // 统计音频
                string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip");
                totalAudioClips = audioGuids.Length;
                
                // 统计模型
                string[] modelGuids = AssetDatabase.FindAssets("t:Model");
                totalModels = modelGuids.Length;
                
                // 计算总大小
                totalAssetSize = CalculateTotalAssetSize();
                
                // 生成类型报告
                GenerateTypeReports();
            }
            
            private long CalculateTotalAssetSize()
            {
                long totalSize = 0;
                string[] allAssets = AssetDatabase.GetAllAssetPaths();
                
                foreach (string assetPath in allAssets)
                {
                    if (File.Exists(assetPath))
                    {
                        FileInfo fileInfo = new FileInfo(assetPath);
                        totalSize += fileInfo.Length;
                    }
                }
                
                return totalSize;
            }
            
            private void GenerateTypeReports()
            {
                var reports = new List<AssetTypeReport>();
                
                // 纹理报告
                reports.Add(GenerateTextureReport());
                
                // 音频报告
                reports.Add(GenerateAudioReport());
                
                typeReports = reports.ToArray();
            }
            
            private AssetTypeReport GenerateTextureReport()
            {
                var report = new AssetTypeReport { type = "Texture" };
                string[] guids = AssetDatabase.FindAssets("t:Texture2D");
                
                long totalSize = 0;
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (File.Exists(path))
                    {
                        FileInfo fileInfo = new FileInfo(path);
                        totalSize += fileInfo.Length;
                    }
                }
                
                report.count = guids.Length;
                report.totalSize = totalSize;
                return report;
            }
            
            private AssetTypeReport GenerateAudioReport()
            {
                var report = new AssetTypeReport { type = "Audio" };
                string[] guids = AssetDatabase.FindAssets("t:AudioClip");
                
                long totalSize = 0;
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (File.Exists(path))
                    {
                        FileInfo fileInfo = new FileInfo(path);
                        totalSize += fileInfo.Length;
                    }
                }
                
                report.count = guids.Length;
                report.totalSize = totalSize;
                return report;
            }
        }
        
        /// <summary>
        /// 资源类型报告
        /// </summary>
        [System.Serializable]
        public class AssetTypeReport
        {
            public string type;
            public int count;
            public long totalSize;
        }
        
        #endregion
    }
}