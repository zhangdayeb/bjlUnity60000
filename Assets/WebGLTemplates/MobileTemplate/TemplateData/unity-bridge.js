/**
 * Unity游戏桥接脚本
 * 提供JavaScript与Unity C#之间的双向通信
 * 处理游戏参数传递、状态同步、事件分发等功能
 */

(function() {
    'use strict';
    
    // Unity桥接管理器
    window.unityBridge = {
        unityInstance: null,
        isInitialized: false,
        isUnityReady: false,
        messageQueue: [],
        eventListeners: {},
        
        /**
         * 初始化Unity桥接
         * @param {Object} unity Unity实例
         */
        initialize: function(unity) {
            if (this.isInitialized) return;
            
            this.unityInstance = unity;
            this.isInitialized = true;
            
            console.log('[Unity桥接] 初始化开始');
            
            // 设置Unity准备状态监听
            this.setupUnityReadyListener();
            
            // 注册全局方法供Unity调用
            this.registerGlobalMethods();
            
            // 初始化游戏参数
            this.initializeGameParameters();
            
            // 设置浏览器事件监听
            this.setupBrowserEventListeners();
            
            // 处理消息队列
            this.processMessageQueue();
            
            console.log('[Unity桥接] 初始化完成');
        },
        
        /**
         * 设置Unity准备状态监听
         */
        setupUnityReadyListener: function() {
            // 监听Unity发送的准备信号
            window.addEventListener('unity-ready', () => {
                this.isUnityReady = true;
                console.log('[Unity桥接] Unity游戏已准备就绪');
                
                // 处理等待的消息
                this.processMessageQueue();
                
                // 发送浏览器信息到Unity
                this.sendBrowserInfo();
                
                // 发送游戏参数到Unity
                this.sendGameParameters();
            });
        },
        
        /**
         * 注册全局方法供Unity调用
         */
        registerGlobalMethods: function() {
            // === Unity调用JavaScript的方法 ===
            
            /**
             * Unity调用：显示消息
             */
            window.ShowMessage = (message, type = 'info') => {
                console.log(`[Unity消息] ${type.toUpperCase()}: ${message}`);
                this.showNotification(message, type);
            };
            
            /**
             * Unity调用：打开外部链接
             */
            window.OpenURL = (url) => {
                console.log('[Unity桥接] 打开链接:', url);
                window.open(url, '_blank', 'noopener,noreferrer');
            };
            
            /**
             * Unity调用：复制到剪贴板
             */
            window.CopyToClipboard = (text) => {
                return navigator.clipboard.writeText(text).then(() => {
                    console.log('[Unity桥接] 文本已复制到剪贴板');
                    return true;
                }).catch(error => {
                    console.error('[Unity桥接] 复制失败:', error);
                    return false;
                });
            };
            
            /**
             * Unity调用：获取浏览器信息
             */
            window.GetBrowserInfo = () => {
                return JSON.stringify(this.getBrowserInfo());
            };
            
            /**
             * Unity调用：获取URL参数
             */
            window.GetUrlParameters = () => {
                return JSON.stringify(this.getUrlParameters());
            };
            
            /**
             * Unity调用：设置页面标题
             */
            window.SetPageTitle = (title) => {
                document.title = title;
                console.log('[Unity桥接] 页面标题已更新:', title);
            };
            
            /**
             * Unity调用：本地存储操作
             */
            window.SetLocalStorage = (key, value) => {
                try {
                    localStorage.setItem(key, value);
                    return true;
                } catch (error) {
                    console.error('[Unity桥接] 本地存储设置失败:', error);
                    return false;
                }
            };
            
            window.GetLocalStorage = (key) => {
                try {
                    return localStorage.getItem(key) || '';
                } catch (error) {
                    console.error('[Unity桥接] 本地存储读取失败:', error);
                    return '';
                }
            };
            
            window.RemoveLocalStorage = (key) => {
                try {
                    localStorage.removeItem(key);
                    return true;
                } catch (error) {
                    console.error('[Unity桥接] 本地存储删除失败:', error);
                    return false;
                }
            };
            
            /**
             * Unity调用：会话存储操作
             */
            window.SetSessionStorage = (key, value) => {
                try {
                    sessionStorage.setItem(key, value);
                    return true;
                } catch (error) {
                    console.error('[Unity桥接] 会话存储设置失败:', error);
                    return false;
                }
            };
            
            window.GetSessionStorage = (key) => {
                try {
                    return sessionStorage.getItem(key) || '';
                } catch (error) {
                    console.error('[Unity桥接] 会话存储读取失败:', error);
                    return '';
                }
            };
            
            /**
             * Unity调用：发送分析事件
             */
            window.SendAnalyticsEvent = (eventName, parameters) => {
                console.log('[Unity桥接] 分析事件:', eventName, parameters);
                
                // Google Analytics集成示例
                if (typeof gtag !== 'undefined') {
                    gtag('event', eventName, JSON.parse(parameters || '{}'));
                }
                
                // 其他分析工具集成
                this.sendAnalyticsEvent(eventName, JSON.parse(parameters || '{}'));
            };
            
            /**
             * Unity调用：震动反馈（移动端）
             */
            window.Vibrate = (pattern) => {
                if ('vibrate' in navigator) {
                    navigator.vibrate(pattern);
                    return true;
                }
                return false;
            };
            
            /**
             * Unity调用：请求全屏
             */
            window.RequestFullscreen = () => {
                const element = document.documentElement;
                if (element.requestFullscreen) {
                    element.requestFullscreen();
                } else if (element.webkitRequestFullscreen) {
                    element.webkitRequestFullscreen();
                } else if (element.mozRequestFullScreen) {
                    element.mozRequestFullScreen();
                } else if (element.msRequestFullscreen) {
                    element.msRequestFullscreen();
                }
            };
            
            /**
             * Unity调用：退出全屏
             */
            window.ExitFullscreen = () => {
                if (document.exitFullscreen) {
                    document.exitFullscreen();
                } else if (document.webkitExitFullscreen) {
                    document.webkitExitFullscreen();
                } else if (document.mozCancelFullScreen) {
                    document.mozCancelFullScreen();
                } else if (document.msExitFullscreen) {
                    document.msExitFullscreen();
                }
            };
            
            /**
             * Unity调用：获取设备信息
             */
            window.GetDeviceInfo = () => {
                return JSON.stringify(this.getDeviceInfo());
            };
            
            /**
             * Unity调用：获取网络状态
             */
            window.GetNetworkInfo = () => {
                return JSON.stringify(this.getNetworkInfo());
            };
            
            /**
             * Unity调用：显示原生分享菜单
             */
            window.NativeShare = (title, text, url) => {
                if (navigator.share) {
                    navigator.share({
                        title: title,
                        text: text,
                        url: url
                    }).then(() => {
                        console.log('[Unity桥接] 分享成功');
                        return true;
                    }).catch(error => {
                        console.error('[Unity桥接] 分享失败:', error);
                        return false;
                    });
                } else {
                    // 降级处理：复制链接到剪贴板
                    return this.CopyToClipboard(url);
                }
            };
            
            /**
             * Unity调用：重新加载页面
             */
            window.ReloadPage = () => {
                window.location.reload();
            };
            
            /**
             * Unity调用：导航到新页面
             */
            window.NavigateToPage = (url) => {
                window.location.href = url;
            };
            
            console.log('[Unity桥接] 全局方法注册完成');
        },
        
        /**
         * 初始化游戏参数
         */
        initializeGameParameters: function() {
            // 从URL获取游戏参数
            const urlParams = this.getUrlParameters();
            
            // 验证必需的游戏参数
            const requiredParams = ['table_id', 'game_type', 'user_id', 'token'];
            const missingParams = requiredParams.filter(param => !urlParams[param]);
            
            if (missingParams.length > 0) {
                console.warn('[Unity桥接] 缺少必需的游戏参数:', missingParams);
                this.showNotification('游戏参数不完整', 'warning');
            }
            
            // 存储游戏参数供Unity使用
            this.gameParameters = urlParams;
        },
        
        /**
         * 发送游戏参数到Unity
         */
        sendGameParameters: function() {
            if (this.gameParameters && Object.keys(this.gameParameters).length > 0) {
                this.sendMessageToUnity('GameManager', 'SetUrlParameters', JSON.stringify(this.gameParameters));
                console.log('[Unity桥接] 游戏参数已发送到Unity:', this.gameParameters);
            }
        },
        
        /**
         * 设置浏览器事件监听
         */
        setupBrowserEventListeners: function() {
            // 页面可见性变化
            document.addEventListener('visibilitychange', () => {
                const isVisible = !document.hidden;
                this.sendMessageToUnity('GameManager', 'OnPageVisibilityChanged', isVisible.toString());
                console.log('[Unity桥接] 页面可见性变化:', isVisible);
            });
            
            // 窗口焦点变化
            window.addEventListener('focus', () => {
                this.sendMessageToUnity('GameManager', 'OnWindowFocus', 'true');
                console.log('[Unity桥接] 窗口获得焦点');
            });
            
            window.addEventListener('blur', () => {
                this.sendMessageToUnity('GameManager', 'OnWindowFocus', 'false');
                console.log('[Unity桥接] 窗口失去焦点');
            });
            
            // 网络状态变化
            window.addEventListener('online', () => {
                this.sendMessageToUnity('NetworkManager', 'OnNetworkStatusChanged', 'true');
                console.log('[Unity桥接] 网络连接恢复');
            });
            
            window.addEventListener('offline', () => {
                this.sendMessageToUnity('NetworkManager', 'OnNetworkStatusChanged', 'false');
                console.log('[Unity桥接] 网络连接断开');
            });
            
            // 窗口大小变化
            window.addEventListener('resize', () => {
                const screenInfo = {
                    width: window.innerWidth,
                    height: window.innerHeight,
                    devicePixelRatio: window.devicePixelRatio || 1
                };
                this.sendMessageToUnity('UIManager', 'OnScreenSizeChanged', JSON.stringify(screenInfo));
            });
            
            // 全屏状态变化
            document.addEventListener('fullscreenchange', () => {
                const isFullscreen = !!document.fullscreenElement;
                this.sendMessageToUnity('UIManager', 'OnFullscreenChanged', isFullscreen.toString());
                console.log('[Unity桥接] 全屏状态变化:', isFullscreen);
            });
            
            // 键盘事件（用于调试和快捷键）
            document.addEventListener('keydown', (event) => {
                // 发送特殊按键到Unity
                if (event.key === 'F11') {
                    event.preventDefault();
                    this.sendMessageToUnity('UIManager', 'ToggleFullscreen', '');
                } else if (event.key === 'F12' && event.ctrlKey) {
                    event.preventDefault();
                    this.sendMessageToUnity('DebugManager', 'ToggleDebugPanel', '');
                }
            });
            
            // Safari特殊事件监听
            if (window.safariCompatibility) {
                window.addEventListener('safari-audio-unlocked', () => {
                    this.sendMessageToUnity('AudioManager', 'OnAudioUnlocked', '');
                });
                
                window.addEventListener('safari-memory-warning', () => {
                    this.sendMessageToUnity('GameManager', 'OnMemoryWarning', '');
                });
                
                window.addEventListener('safari-page-hidden', () => {
                    this.sendMessageToUnity('GameManager', 'OnPageHidden', '');
                });
                
                window.addEventListener('safari-page-visible', () => {
                    this.sendMessageToUnity('GameManager', 'OnPageVisible', '');
                });
            }
        },
        
        /**
         * 向Unity发送消息
         * @param {string} gameObject Unity游戏对象名称
         * @param {string} methodName 方法名称
         * @param {string} parameter 参数（字符串格式）
         */
        sendMessageToUnity: function(gameObject, methodName, parameter = '') {
            if (!this.unityInstance || !this.isUnityReady) {
                // 将消息添加到队列等待Unity准备
                this.messageQueue.push({
                    gameObject: gameObject,
                    methodName: methodName,
                    parameter: parameter,
                    timestamp: Date.now()
                });
                return;
            }
            
            try {
                this.unityInstance.SendMessage(gameObject, methodName, parameter);
                console.log(`[Unity桥接] 消息已发送: ${gameObject}.${methodName}(${parameter})`);
            } catch (error) {
                console.error('[Unity桥接] 发送消息失败:', error);
            }
        },
        
        /**
         * 处理消息队列
         */
        processMessageQueue: function() {
            if (!this.isUnityReady || this.messageQueue.length === 0) return;
            
            console.log(`[Unity桥接] 处理消息队列: ${this.messageQueue.length} 条消息`);
            
            while (this.messageQueue.length > 0) {
                const message = this.messageQueue.shift();
                
                // 检查消息是否过期（10秒）
                if (Date.now() - message.timestamp < 10000) {
                    this.sendMessageToUnity(message.gameObject, message.methodName, message.parameter);
                } else {
                    console.warn('[Unity桥接] 丢弃过期消息:', message);
                }
            }
        },
        
        /**
         * 发送浏览器信息到Unity
         */
        sendBrowserInfo: function() {
            const browserInfo = this.getBrowserInfo();
            this.sendMessageToUnity('SystemManager', 'SetBrowserInfo', JSON.stringify(browserInfo));
            console.log('[Unity桥接] 浏览器信息已发送到Unity');
        },
        
        /**
         * 获取浏览器信息
         */
        getBrowserInfo: function() {
            return {
                userAgent: navigator.userAgent,
                platform: navigator.platform,
                language: navigator.language,
                languages: navigator.languages || [navigator.language],
                cookieEnabled: navigator.cookieEnabled,
                onLine: navigator.onLine,
                hardwareConcurrency: navigator.hardwareConcurrency || 1,
                maxTouchPoints: navigator.maxTouchPoints || 0,
                deviceMemory: navigator.deviceMemory || 0,
                connection: this.getConnectionInfo(),
                screen: {
                    width: screen.width,
                    height: screen.height,
                    availWidth: screen.availWidth,
                    availHeight: screen.availHeight,
                    colorDepth: screen.colorDepth,
                    pixelDepth: screen.pixelDepth
                },
                window: {
                    innerWidth: window.innerWidth,
                    innerHeight: window.innerHeight,
                    outerWidth: window.outerWidth,
                    outerHeight: window.outerHeight,
                    devicePixelRatio: window.devicePixelRatio || 1
                },
                features: {
                    webgl: this.checkWebGLSupport(),
                    webgl2: this.checkWebGL2Support(),
                    webAssembly: typeof WebAssembly !== 'undefined',
                    serviceWorker: 'serviceWorker' in navigator,
                    indexedDB: 'indexedDB' in window,
                    localStorage: this.checkLocalStorageSupport(),
                    sessionStorage: this.checkSessionStorageSupport(),
                    geolocation: 'geolocation' in navigator,
                    notifications: 'Notification' in window,
                    vibration: 'vibrate' in navigator,
                    fullscreen: this.checkFullscreenSupport(),
                    share: 'share' in navigator
                },
                safari: window.safariCompatibility ? window.safariCompatibility.getSafariInfo() : null
            };
        },
        
        /**
         * 获取URL参数
         */
        getUrlParameters: function() {
            const params = new URLSearchParams(window.location.search);
            const result = {};
            
            for (const [key, value] of params) {
                result[key] = value;
            }
            
            return result;
        },
        
        /**
         * 获取设备信息
         */
        getDeviceInfo: function() {
            return {
                isMobile: /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent),
                isTablet: /iPad|Android(?=.*\bMobile\b)(?=.*\bSafari\b)|Android(?=.*\bSD4930UR\b)|SAMSUNG.*\bGT-[IP]|GT-P|GT-N|Android(?=.*\b(?:KFOT|KFTT|KFJWI|KFJWA|KFSOWI|KFTHWI|KFTHWA|KFAPWI|KFAPWA|KFARWI|KFASWI|KFSAWI|KFSAWA)\b)/i.test(navigator.userAgent),
                isDesktop: !/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent),
                os: this.getOS(),
                browser: this.getBrowser(),
                touchSupported: 'ontouchstart' in window || navigator.maxTouchPoints > 0,
                orientation: this.getOrientation(),
                battery: this.getBatteryInfo(),
                memory: navigator.deviceMemory || 0,
                cores: navigator.hardwareConcurrency || 1
            };
        },
        
        /**
         * 获取网络信息
         */
        getNetworkInfo: function() {
            const connection = this.getConnectionInfo();
            return {
                online: navigator.onLine,
                connection: connection,
                effectiveType: connection ? connection.effectiveType : 'unknown',
                downlink: connection ? connection.downlink : 0,
                rtt: connection ? connection.rtt : 0,
                saveData: connection ? connection.saveData : false
            };
        },
        
        /**
         * 获取连接信息
         */
        getConnectionInfo: function() {
            return navigator.connection || navigator.mozConnection || navigator.webkitConnection || null;
        },
        
        /**
         * 获取操作系统
         */
        getOS: function() {
            const userAgent = navigator.userAgent;
            if (userAgent.indexOf('Windows') !== -1) return 'Windows';
            if (userAgent.indexOf('Mac') !== -1) return 'macOS';
            if (userAgent.indexOf('Linux') !== -1) return 'Linux';
            if (userAgent.indexOf('Android') !== -1) return 'Android';
            if (userAgent.indexOf('iPhone') !== -1 || userAgent.indexOf('iPad') !== -1) return 'iOS';
            return 'Unknown';
        },
        
        /**
         * 获取浏览器
         */
        getBrowser: function() {
            const userAgent = navigator.userAgent;
            if (userAgent.indexOf('Chrome') !== -1) return 'Chrome';
            if (userAgent.indexOf('Firefox') !== -1) return 'Firefox';
            if (userAgent.indexOf('Safari') !== -1) return 'Safari';
            if (userAgent.indexOf('Edge') !== -1) return 'Edge';
            if (userAgent.indexOf('Opera') !== -1) return 'Opera';
            return 'Unknown';
        },
        
        /**
         * 获取设备方向
         */
        getOrientation: function() {
            if (screen.orientation) {
                return screen.orientation.type;
            } else if (window.orientation !== undefined) {
                return Math.abs(window.orientation) === 90 ? 'landscape' : 'portrait';
            }
            return 'unknown';
        },
        
        /**
         * 获取电池信息
         */
        getBatteryInfo: function() {
            if ('getBattery' in navigator) {
                navigator.getBattery().then(battery => {
                    const batteryInfo = {
                        charging: battery.charging,
                        level: battery.level,
                        chargingTime: battery.chargingTime,
                        dischargingTime: battery.dischargingTime
                    };
                    this.sendMessageToUnity('SystemManager', 'OnBatteryInfoUpdated', JSON.stringify(batteryInfo));
                });
            }
            return null;
        },
        
        /**
         * 检查WebGL支持
         */
        checkWebGLSupport: function() {
            try {
                const canvas = document.createElement('canvas');
                return !!(canvas.getContext('webgl') || canvas.getContext('experimental-webgl'));
            } catch (e) {
                return false;
            }
        },
        
        /**
         * 检查WebGL2支持
         */
        checkWebGL2Support: function() {
            try {
                const canvas = document.createElement('canvas');
                return !!canvas.getContext('webgl2');
            } catch (e) {
                return false;
            }
        },
        
        /**
         * 检查本地存储支持
         */
        checkLocalStorageSupport: function() {
            try {
                const test = 'test';
                localStorage.setItem(test, test);
                localStorage.removeItem(test);
                return true;
            } catch (e) {
                return false;
            }
        },
        
        /**
         * 检查会话存储支持
         */
        checkSessionStorageSupport: function() {
            try {
                const test = 'test';
                sessionStorage.setItem(test, test);
                sessionStorage.removeItem(test);
                return true;
            } catch (e) {
                return false;
            }
        },
        
        /**
         * 检查全屏支持
         */
        checkFullscreenSupport: function() {
            const element = document.documentElement;
            return !!(element.requestFullscreen || element.webkitRequestFullscreen || 
                     element.mozRequestFullScreen || element.msRequestFullscreen);
        },
        
        /**
         * 显示通知
         */
        showNotification: function(message, type = 'info') {
            // 创建通知元素
            const notification = document.createElement('div');
            notification.className = `unity-notification unity-notification-${type}`;
            notification.textContent = message;
            
            // 样式
            Object.assign(notification.style, {
                position: 'fixed',
                top: '20px',
                right: '20px',
                padding: '12px 20px',
                borderRadius: '8px',
                color: 'white',
                fontFamily: 'Arial, sans-serif',
                fontSize: '14px',
                fontWeight: '500',
                zIndex: '10000',
                opacity: '0',
                transform: 'translateX(100%)',
                transition: 'all 0.3s ease',
                maxWidth: '300px',
                wordWrap: 'break-word',
                boxShadow: '0 4px 12px rgba(0,0,0,0.3)'
            });
            
            // 类型颜色
            const colors = {
                info: '#4ECDC4',
                success: '#45B7D1',
                warning: '#FFEAA7',
                error: '#FF6B6B'
            };
            notification.style.backgroundColor = colors[type] || colors.info;
            
            document.body.appendChild(notification);
            
            // 动画显示
            setTimeout(() => {
                notification.style.opacity = '1';
                notification.style.transform = 'translateX(0)';
            }, 10);
            
            // 自动隐藏
            setTimeout(() => {
                notification.style.opacity = '0';
                notification.style.transform = 'translateX(100%)';
                setTimeout(() => {
                    if (notification.parentNode) {
                        notification.parentNode.removeChild(notification);
                    }
                }, 300);
            }, 3000);
        },
        
        /**
         * 发送分析事件
         */
        sendAnalyticsEvent: function(eventName, parameters) {
            // 自定义分析事件处理
            console.log('[Unity桥接] 分析事件:', {
                event: eventName,
                parameters: parameters,
                timestamp: new Date().toISOString(),
                sessionId: this.getSessionId(),
                userId: this.gameParameters ? this.gameParameters.user_id : null
            });
            
            // 可以在这里集成其他分析工具
            // 例如：Facebook Pixel、百度统计等
        },
        
        /**
         * 获取会话ID
         */
        getSessionId: function() {
            let sessionId = sessionStorage.getItem('unity_session_id');
            if (!sessionId) {
                sessionId = 'session_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
                sessionStorage.setItem('unity_session_id', sessionId);
            }
            return sessionId;
        },
        
        /**
         * 添加事件监听器
         */
        addEventListener: function(eventName, callback) {
            if (!this.eventListeners[eventName]) {
                this.eventListeners[eventName] = [];
            }
            this.eventListeners[eventName].push(callback);
        },
        
        /**
         * 移除事件监听器
         */
        removeEventListener: function(eventName, callback) {
            if (this.eventListeners[eventName]) {
                const index = this.eventListeners[eventName].indexOf(callback);
                if (index > -1) {
                    this.eventListeners[eventName].splice(index, 1);
                }
            }
        },
        
        /**
         * 触发事件
         */
        dispatchEvent: function(eventName, data) {
            if (this.eventListeners[eventName]) {
                this.eventListeners[eventName].forEach(callback => {
                    try {
                        callback(data);
                    } catch (error) {
                        console.error('[Unity桥接] 事件回调错误:', error);
                    }
                });
            }
        },
        
        /**
         * 获取Unity实例（供外部调用）
         */
        getUnityInstance: function() {
            return this.unityInstance;
        },
        
        /**
         * 检查Unity是否准备就绪
         */
        isReady: function() {
            return this.isUnityReady;
        },
        
        /**
         * 清理资源
         */
        cleanup: function() {
            this.eventListeners = {};
            this.messageQueue = [];
            console.log('[Unity桥接] 资源已清理');
        }
    };
    
    // 页面卸载时清理资源
    window.addEventListener('beforeunload', function() {
        if (window.unityBridge) {
            window.unityBridge.cleanup();
        }
    });
    
    console.log('[Unity桥接] 桥接脚本加载完成');
    
})();