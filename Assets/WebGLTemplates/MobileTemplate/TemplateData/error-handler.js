/**
 * 错误处理脚本
 * 全局错误捕获、错误分类、错误报告和用户友好的错误处理
 * 支持Unity WebGL特有的错误处理和Safari兼容性
 */

(function() {
    'use strict';
    
    // 错误处理管理器
    window.errorHandler = {
        isInitialized: false,
        errorCount: 0,
        maxErrors: 10,
        reportEndpoint: null,
        enableReporting: false,
        errorQueue: [],
        
        /**
         * 初始化错误处理器
         */
        initialize: function() {
            if (this.isInitialized) return;
            
            console.log('[错误处理] 初始化开始');
            
            // 设置全局错误监听
            this.setupGlobalErrorHandlers();
            
            // 设置Unity特定错误处理
            this.setupUnityErrorHandlers();
            
            // 设置网络错误处理
            this.setupNetworkErrorHandlers();
            
            // 设置Promise错误处理
            this.setupPromiseErrorHandlers();
            
            // 初始化错误报告
            this.initializeErrorReporting();
            
            this.isInitialized = true;
            console.log('[错误处理] 初始化完成');
        },
        
        /**
         * 设置全局错误监听
         */
        setupGlobalErrorHandlers: function() {
            // JavaScript运行时错误
            window.addEventListener('error', (event) => {
                const error = {
                    type: 'javascript_error',
                    message: event.message || '未知JavaScript错误',
                    filename: event.filename || '',
                    line: event.lineno || 0,
                    column: event.colno || 0,
                    stack: event.error ? event.error.stack : '',
                    timestamp: new Date().toISOString(),
                    userAgent: navigator.userAgent,
                    url: window.location.href
                };
                
                this.handleError(error);
            });
            
            // 资源加载错误
            window.addEventListener('error', (event) => {
                if (event.target !== window) {
                    const error = {
                        type: 'resource_error',
                        message: '资源加载失败',
                        resource: event.target.src || event.target.href || '未知资源',
                        element: event.target.tagName || '未知元素',
                        timestamp: new Date().toISOString(),
                        userAgent: navigator.userAgent,
                        url: window.location.href
                    };
                    
                    this.handleError(error);
                }
            }, true);
            
            console.log('[错误处理] 全局错误监听已设置');
        },
        
        /**
         * 设置Unity特定错误处理
         */
        setupUnityErrorHandlers: function() {
            // 监听Unity加载错误
            window.addEventListener('unity-load-error', (event) => {
                const error = {
                    type: 'unity_load_error',
                    message: event.detail || 'Unity加载失败',
                    timestamp: new Date().toISOString(),
                    buildInfo: window.gameConfig || {},
                    browserInfo: this.getBrowserInfo()
                };
                
                this.handleUnityError(error);
            });
            
            // WebGL上下文丢失
            const canvas = document.getElementById('unity-canvas');
            if (canvas) {
                canvas.addEventListener('webglcontextlost', (event) => {
                    event.preventDefault();
                    
                    const error = {
                        type: 'webgl_context_lost',
                        message: 'WebGL上下文丢失',
                        timestamp: new Date().toISOString(),
                        canvasInfo: {
                            width: canvas.width,
                            height: canvas.height,
                            clientWidth: canvas.clientWidth,
                            clientHeight: canvas.clientHeight
                        }
                    };
                    
                    this.handleUnityError(error);
                });
                
                canvas.addEventListener('webglcontextrestored', () => {
                    console.log('[错误处理] WebGL上下文已恢复');
                    this.showUserMessage('图形上下文已恢复，游戏正在重新加载...', 'info');
                });
            }
            
            console.log('[错误处理] Unity错误监听已设置');
        },
        
        /**
         * 设置网络错误处理
         */
        setupNetworkErrorHandlers: function() {
            // Fetch错误拦截
            const originalFetch = window.fetch;
            window.fetch = async function(...args) {
                try {
                    const response = await originalFetch.apply(this, args);
                    
                    if (!response.ok) {
                        const error = {
                            type: 'network_error',
                            message: `HTTP ${response.status}: ${response.statusText}`,
                            url: args[0],
                            status: response.status,
                            statusText: response.statusText,
                            timestamp: new Date().toISOString()
                        };
                        
                        window.errorHandler.handleError(error);
                    }
                    
                    return response;
                } catch (fetchError) {
                    const error = {
                        type: 'network_error',
                        message: fetchError.message || '网络请求失败',
                        url: args[0],
                        error: fetchError.toString(),
                        timestamp: new Date().toISOString()
                    };
                    
                    window.errorHandler.handleError(error);
                    throw fetchError;
                }
            };
            
            // XMLHttpRequest错误拦截
            const originalXHROpen = XMLHttpRequest.prototype.open;
            XMLHttpRequest.prototype.open = function(...args) {
                this.addEventListener('error', () => {
                    const error = {
                        type: 'xhr_error',
                        message: 'XMLHttpRequest错误',
                        url: args[1],
                        status: this.status,
                        timestamp: new Date().toISOString()
                    };
                    
                    window.errorHandler.handleError(error);
                });
                
                return originalXHROpen.apply(this, args);
            };
            
            console.log('[错误处理] 网络错误监听已设置');
        },
        
        /**
         * 设置Promise错误处理
         */
        setupPromiseErrorHandlers: function() {
            window.addEventListener('unhandledrejection', (event) => {
                const error = {
                    type: 'promise_rejection',
                    message: event.reason ? event.reason.toString() : '未处理的Promise拒绝',
                    reason: event.reason,
                    stack: event.reason && event.reason.stack ? event.reason.stack : '',
                    timestamp: new Date().toISOString()
                };
                
                this.handleError(error);
                
                // 防止在控制台显示未处理的Promise拒绝
                event.preventDefault();
            });
            
            console.log('[错误处理] Promise错误监听已设置');
        },
        
        /**
         * 初始化错误报告
         */
        initializeErrorReporting: function() {
            // 可以配置错误报告服务端点
            this.reportEndpoint = '/api/error-report';
            this.enableReporting = false; // 根据环境配置
            
            // 从配置中读取设置
            if (window.gameConfig && window.gameConfig.errorReporting) {
                this.enableReporting = window.gameConfig.errorReporting.enabled || false;
                this.reportEndpoint = window.gameConfig.errorReporting.endpoint || this.reportEndpoint;
            }
            
            console.log('[错误处理] 错误报告配置:', {
                enabled: this.enableReporting,
                endpoint: this.reportEndpoint
            });
        },
        
        /**
         * 处理错误
         */
        handleError: function(error) {
            this.errorCount++;
            
            console.error('[错误处理] 捕获错误:', error);
            
            // 分类处理错误
            const errorCategory = this.categorizeError(error);
            const userMessage = this.getErrorMessage(errorCategory, error);
            
            // 显示用户友好的错误信息
            this.showUserMessage(userMessage.message, userMessage.type);
            
            // 记录错误到队列
            this.errorQueue.push({
                ...error,
                category: errorCategory,
                errorId: this.generateErrorId()
            });
            
            // 限制错误队列大小
            if (this.errorQueue.length > 100) {
                this.errorQueue = this.errorQueue.slice(-50);
            }
            
            // 发送错误报告
            if (this.enableReporting) {
                this.reportError(error);
            }
            
            // 通知Unity
            if (window.unityBridge && window.unityBridge.isReady()) {
                window.unityBridge.sendMessageToUnity('ErrorManager', 'OnErrorOccurred', JSON.stringify({
                    type: error.type,
                    message: error.message,
                    category: errorCategory
                }));
            }
            
            // 检查是否需要采取特殊措施
            this.checkErrorThreshold();
        },
        
        /**
         * 处理Unity特定错误
         */
        handleUnityError: function(error) {
            console.error('[错误处理] Unity错误:', error);
            
            // Unity错误需要特殊处理
            switch (error.type) {
                case 'unity_load_error':
                    this.handleUnityLoadError(error);
                    break;
                case 'webgl_context_lost':
                    this.handleWebGLContextLost(error);
                    break;
                default:
                    this.handleError(error);
            }
        },
        
        /**
         * 处理Unity加载错误
         */
        handleUnityLoadError: function(error) {
            this.showErrorPage('游戏加载失败', [
                '请检查网络连接',
                '确保浏览器支持WebGL',
                '尝试刷新页面重新加载',
                '如果问题持续，请联系技术支持'
            ]);
            
            // 记录详细的加载错误信息
            this.errorQueue.push({
                ...error,
                category: 'critical',
                errorId: this.generateErrorId(),
                gameConfig: window.gameConfig,
                browserCapabilities: this.getBrowserCapabilities()
            });
            
            if (this.enableReporting) {
                this.reportError(error);
            }
        },
        
        /**
         * 处理WebGL上下文丢失
         */
        handleWebGLContextLost: function(error) {
            this.showUserMessage('图形上下文丢失，正在尝试恢复...', 'warning');
            
            // 尝试重新加载游戏
            setTimeout(() => {
                this.showUserMessage('正在重新加载游戏...', 'info');
                window.location.reload();
            }, 3000);
        },
        
        /**
         * 错误分类
         */
        categorizeError: function(error) {
            const criticalErrors = [
                'unity_load_error',
                'webgl_context_lost',
                'out_of_memory'
            ];
            
            const networkErrors = [
                'network_error',
                'xhr_error',
                'resource_error'
            ];
            
            const scriptErrors = [
                'javascript_error',
                'promise_rejection'
            ];
            
            if (criticalErrors.includes(error.type)) {
                return 'critical';
            } else if (networkErrors.includes(error.type)) {
                return 'network';
            } else if (scriptErrors.includes(error.type)) {
                return 'script';
            } else {
                return 'general';
            }
        },
        
        /**
         * 获取用户友好的错误消息
         */
        getErrorMessage: function(category, error) {
            const messages = {
                critical: {
                    message: '游戏遇到严重错误，请刷新页面重试',
                    type: 'error'
                },
                network: {
                    message: '网络连接异常，请检查网络后重试',
                    type: 'warning'
                },
                script: {
                    message: '程序运行异常，正在尝试恢复',
                    type: 'warning'
                },
                general: {
                    message: '发生未知错误，如果问题持续请联系支持',
                    type: 'info'
                }
            };
            
            // 特殊错误的自定义消息
            if (error.type === 'unity_load_error') {
                if (error.message.includes('wasm')) {
                    return {
                        message: '浏览器不支持WebAssembly，请使用最新版浏览器',
                        type: 'error'
                    };
                } else if (error.message.includes('memory')) {
                    return {
                        message: '内存不足，请关闭其他标签页后重试',
                        type: 'error'
                    };
                }
            }
            
            return messages[category] || messages.general;
        },
        
        /**
         * 显示用户消息
         */
        showUserMessage: function(message, type = 'info') {
            // 复用Unity桥接的通知系统
            if (window.unityBridge) {
                window.unityBridge.showNotification(message, type);
            } else {
                // 降级到alert（仅在开发环境）
                if (window.location.hostname === 'localhost') {
                    console.warn('[错误处理] ' + message);
                }
            }
        },
        
        /**
         * 显示错误页面
         */
        showErrorPage: function(title, suggestions = []) {
            const errorContainer = document.getElementById('error-container');
            if (errorContainer) {
                const errorMessage = document.getElementById('error-message');
                if (errorMessage) {
                    let content = title;
                    if (suggestions.length > 0) {
                        content += '\n\n建议解决方案：\n' + suggestions.map(s => '• ' + s).join('\n');
                    }
                    errorMessage.textContent = content;
                }
                errorContainer.style.display = 'flex';
            }
        },
        
        /**
         * 检查错误阈值
         */
        checkErrorThreshold: function() {
            if (this.errorCount >= this.maxErrors) {
                console.error('[错误处理] 错误数量达到阈值，建议重新加载页面');
                
                this.showErrorPage('检测到多个错误', [
                    '页面可能存在问题',
                    '建议刷新页面重新开始',
                    '如果问题持续，请联系技术支持'
                ]);
                
                // 自动重新加载（延迟5秒）
                setTimeout(() => {
                    if (confirm('检测到多个错误，是否重新加载页面？')) {
                        window.location.reload();
                    }
                }, 5000);
            }
        },
        
        /**
         * 报告错误到服务器
         */
        reportError: function(error) {
            if (!this.reportEndpoint) return;
            
            const errorReport = {
                ...error,
                sessionId: this.getSessionId(),
                timestamp: new Date().toISOString(),
                browserInfo: this.getBrowserInfo(),
                gameInfo: this.getGameInfo(),
                errorCount: this.errorCount,
                url: window.location.href,
                referrer: document.referrer
            };
            
            // 异步发送，不影响游戏性能
            setTimeout(() => {
                fetch(this.reportEndpoint, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(errorReport)
                }).catch(reportError => {
                    console.warn('[错误处理] 错误报告发送失败:', reportError);
                });
            }, 100);
        },
        
        /**
         * 生成错误ID
         */
        generateErrorId: function() {
            return 'error_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
        },
        
        /**
         * 获取会话ID
         */
        getSessionId: function() {
            let sessionId = sessionStorage.getItem('error_session_id');
            if (!sessionId) {
                sessionId = 'session_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
                sessionStorage.setItem('error_session_id', sessionId);
            }
            return sessionId;
        },
        
        /**
         * 获取浏览器信息
         */
        getBrowserInfo: function() {
            return {
                userAgent: navigator.userAgent,
                platform: navigator.platform,
                language: navigator.language,
                cookieEnabled: navigator.cookieEnabled,
                onLine: navigator.onLine,
                screen: {
                    width: screen.width,
                    height: screen.height,
                    colorDepth: screen.colorDepth
                },
                window: {
                    innerWidth: window.innerWidth,
                    innerHeight: window.innerHeight,
                    devicePixelRatio: window.devicePixelRatio || 1
                }
            };
        },
        
        /**
         * 获取浏览器能力
         */
        getBrowserCapabilities: function() {
            return {
                webgl: this.checkWebGLSupport(),
                webgl2: this.checkWebGL2Support(),
                webAssembly: typeof WebAssembly !== 'undefined',
                serviceWorker: 'serviceWorker' in navigator,
                indexedDB: 'indexedDB' in window,
                localStorage: this.checkLocalStorageSupport(),
                audioContext: 'AudioContext' in window || 'webkitAudioContext' in window
            };
        },
        
        /**
         * 获取游戏信息
         */
        getGameInfo: function() {
            return {
                config: window.gameConfig || {},
                unityReady: window.unityBridge ? window.unityBridge.isReady() : false,
                safariCompatibility: window.safariCompatibility ? window.safariCompatibility.getSafariInfo() : null
            };
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
         * 获取错误统计
         */
        getErrorStats: function() {
            const categories = {};
            this.errorQueue.forEach(error => {
                const category = error.category || 'unknown';
                categories[category] = (categories[category] || 0) + 1;
            });
            
            return {
                totalErrors: this.errorCount,
                queueSize: this.errorQueue.length,
                categories: categories,
                recentErrors: this.errorQueue.slice(-5)
            };
        },
        
        /**
         * 清除错误队列
         */
        clearErrorQueue: function() {
            this.errorQueue = [];
            this.errorCount = 0;
            console.log('[错误处理] 错误队列已清除');
        },
        
        /**
         * 手动报告错误（供外部调用）
         */
        reportCustomError: function(type, message, details = {}) {
            const error = {
                type: type,
                message: message,
                details: details,
                timestamp: new Date().toISOString(),
                source: 'manual'
            };
            
            this.handleError(error);
        }
    };
    
    // 导出公共方法
    window.reportError = function(type, message, details) {
        if (window.errorHandler) {
            window.errorHandler.reportCustomError(type, message, details);
        }
    };
    
    window.getErrorStats = function() {
        return window.errorHandler ? window.errorHandler.getErrorStats() : null;
    };
    
    console.log('[错误处理] 错误处理脚本加载完成');
    
})();