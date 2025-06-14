/**
 * Safari兼容性脚本
 * 解决Safari浏览器在WebGL游戏中的各种兼容性问题
 * 包括音频播放、内存管理、性能优化等
 */

(function() {
    'use strict';
    
    // Safari检测
    const isSafari = /^((?!chrome|android).)*safari/i.test(navigator.userAgent);
    const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent);
    const isMacOS = /Mac OS X/.test(navigator.userAgent);
    
    // Safari兼容性管理器
    window.safariCompatibility = {
        isInitialized: false,
        audioContext: null,
        audioUnlocked: false,
        memoryWarningShown: false,
        performanceOptimized: false,
        
        /**
         * 初始化Safari兼容性功能
         */
        initialize: function() {
            if (this.isInitialized) return;
            
            console.log('[Safari兼容性] 初始化开始', {
                isSafari: isSafari,
                isIOS: isIOS,
                isMacOS: isMacOS,
                userAgent: navigator.userAgent
            });
            
            if (isSafari) {
                this.initAudioCompatibility();
                this.initMemoryManagement();
                this.initPerformanceOptimizations();
                this.initWebGLOptimizations();
                this.initTouchHandling();
                this.initNetworkOptimizations();
            }
            
            this.initGeneralFixes();
            this.isInitialized = true;
            
            console.log('[Safari兼容性] 初始化完成');
        },
        
        /**
         * 音频兼容性处理
         */
        initAudioCompatibility: function() {
            console.log('[Safari兼容性] 初始化音频兼容性');
            
            // 创建AudioContext（Safari需要用户交互才能播放音频）
            const createAudioContext = () => {
                try {
                    window.AudioContext = window.AudioContext || window.webkitAudioContext;
                    this.audioContext = new AudioContext();
                    
                    // 检查AudioContext状态
                    if (this.audioContext.state === 'suspended') {
                        console.log('[Safari兼容性] AudioContext被暂停，等待用户交互');
                    }
                    
                    return true;
                } catch (error) {
                    console.error('[Safari兼容性] AudioContext创建失败:', error);
                    return false;
                }
            };
            
            // 解锁音频播放
            const unlockAudio = () => {
                if (this.audioUnlocked) return;
                
                if (this.audioContext && this.audioContext.state === 'suspended') {
                    this.audioContext.resume().then(() => {
                        console.log('[Safari兼容性] 音频已解锁');
                        this.audioUnlocked = true;
                        
                        // 通知Unity音频已解锁
                        window.dispatchEvent(new CustomEvent('safari-audio-unlocked'));
                        
                        // 移除事件监听器
                        document.removeEventListener('touchstart', unlockAudio, true);
                        document.removeEventListener('touchend', unlockAudio, true);
                        document.removeEventListener('mousedown', unlockAudio, true);
                        document.removeEventListener('click', unlockAudio, true);
                    }).catch(error => {
                        console.error('[Safari兼容性] 音频解锁失败:', error);
                    });
                }
            };
            
            createAudioContext();
            
            // 添加用户交互监听器来解锁音频
            document.addEventListener('touchstart', unlockAudio, true);
            document.addEventListener('touchend', unlockAudio, true);
            document.addEventListener('mousedown', unlockAudio, true);
            document.addEventListener('click', unlockAudio, true);
            
            // Safari特殊音频处理
            if (isIOS) {
                // iOS设备特殊处理
                this.setupIOSAudioWorkarounds();
            }
        },
        
        /**
         * iOS音频特殊处理
         */
        setupIOSAudioWorkarounds: function() {
            console.log('[Safari兼容性] 设置iOS音频修复');
            
            // 预加载静音音频文件来"预热"音频系统
            const silentAudio = document.createElement('audio');
            silentAudio.setAttribute('preload', 'auto');
            silentAudio.setAttribute('muted', 'true');
            silentAudio.setAttribute('playsinline', 'true');
            silentAudio.volume = 0;
            
            // 创建非常短的静音音频数据URL
            const silentMP3 = 'data:audio/mpeg;base64,SUQzBAAAAAABEVRYWFgAAAAtAAADY29tbWVudABCaWdTb3VuZEJhbmsuY29tIC8gTGFTb25vdGhlcXVlLm9yZwBURU5DAAAAHQAAAV9lbmNvZGVkX2J5AEZyYXVuaG9mZXIgRk1Q//kAAAaIAAAIEEAUIAAEAAAD/8QAAAAsAwAADAAAABoAAAoAAAAsAAAANAAAAGAAAAA=';
            silentAudio.src = silentMP3;
            
            document.body.appendChild(silentAudio);
            
            // 在用户首次交互时播放静音音频
            const playsilent = () => {
                silentAudio.play().then(() => {
                    console.log('[Safari兼容性] iOS音频系统已预热');
                    document.removeEventListener('touchstart', playsilent, true);
                    document.removeEventListener('click', playsilent, true);
                }).catch(error => {
                    console.warn('[Safari兼容性] iOS音频预热失败:', error);
                });
            };
            
            document.addEventListener('touchstart', playsilent, true);
            document.addEventListener('click', playsilent, true);
        },
        
        /**
         * 内存管理优化
         */
        initMemoryManagement: function() {
            console.log('[Safari兼容性] 初始化内存管理');
            
            // 监听内存警告（主要用于移动Safari）
            const handleMemoryWarning = () => {
                if (!this.memoryWarningShown) {
                    console.warn('[Safari兼容性] 检测到内存压力');
                    this.memoryWarningShown = true;
                    
                    // 通知Unity进行内存清理
                    window.dispatchEvent(new CustomEvent('safari-memory-warning'));
                    
                    // 执行垃圾回收建议
                    this.performGarbageCollection();
                }
            };
            
            // iOS/Safari特有的内存事件
            if ('onwebkitmemorywarning' in window) {
                window.addEventListener('webkitmemorywarning', handleMemoryWarning);
            }
            
            // 页面可见性变化时的内存管理
            document.addEventListener('visibilitychange', () => {
                if (document.hidden) {
                    console.log('[Safari兼容性] 页面隐藏，执行内存清理');
                    this.performGarbageCollection();
                    
                    // 通知Unity暂停
                    window.dispatchEvent(new CustomEvent('safari-page-hidden'));
                } else {
                    console.log('[Safari兼容性] 页面恢复显示');
                    
                    // 通知Unity恢复
                    window.dispatchEvent(new CustomEvent('safari-page-visible'));
                }
            });
            
            // 定期内存清理
            setInterval(() => {
                this.performGarbageCollection();
            }, 30000); // 每30秒执行一次
        },
        
        /**
         * 执行垃圾回收
         */
        performGarbageCollection: function() {
            try {
                // 手动触发垃圾回收（如果可用）
                if (window.gc) {
                    window.gc();
                }
                
                // 清理可能的内存泄漏
                this.cleanupMemoryLeaks();
                
            } catch (error) {
                console.warn('[Safari兼容性] 垃圾回收失败:', error);
            }
        },
        
        /**
         * 清理内存泄漏
         */
        cleanupMemoryLeaks: function() {
            // 清理事件监听器
            const elements = document.querySelectorAll('*');
            elements.forEach(element => {
                if (element._eventListeners) {
                    delete element._eventListeners;
                }
            });
            
            // 清理可能的循环引用
            if (window.unityInstance) {
                // Unity实例的内存清理会在Unity端处理
            }
        },
        
        /**
         * 性能优化
         */
        initPerformanceOptimizations: function() {
            console.log('[Safari兼容性] 初始化性能优化');
            
            // Safari特殊的性能设置
            if (isSafari) {
                // 禁用某些可能影响性能的功能
                this.optimizeRenderingPerformance();
                this.optimizeJavaScriptPerformance();
            }
            
            this.performanceOptimized = true;
        },
        
        /**
         * 渲染性能优化
         */
        optimizeRenderingPerformance: function() {
            const canvas = document.getElementById('unity-canvas');
            if (canvas) {
                // Safari特殊的canvas优化
                canvas.style.imageRendering = '-webkit-optimize-contrast';
                canvas.style.webkitTransform = 'translate3d(0,0,0)';
                canvas.style.webkitBackfaceVisibility = 'hidden';
                canvas.style.webkitPerspective = '1000';
                
                // 强制硬件加速
                canvas.style.willChange = 'transform';
            }
            
            // 优化页面渲染
            document.body.style.webkitTransform = 'translate3d(0,0,0)';
            document.body.style.webkitBackfaceVisibility = 'hidden';
        },
        
        /**
         * JavaScript性能优化
         */
        optimizeJavaScriptPerformance: function() {
            // 优化setTimeout和setInterval
            const originalSetTimeout = window.setTimeout;
            const originalSetInterval = window.setInterval;
            
            window.setTimeout = function(callback, delay) {
                // Safari最小延迟优化
                const optimizedDelay = Math.max(delay || 0, 4);
                return originalSetTimeout.call(this, callback, optimizedDelay);
            };
            
            window.setInterval = function(callback, delay) {
                // Safari最小间隔优化
                const optimizedDelay = Math.max(delay || 0, 10);
                return originalSetInterval.call(this, callback, optimizedDelay);
            };
        },
        
        /**
         * WebGL优化
         */
        initWebGLOptimizations: function() {
            console.log('[Safari兼容性] 初始化WebGL优化');
            
            // Safari WebGL上下文优化
            const canvas = document.getElementById('unity-canvas');
            if (canvas) {
                const originalGetContext = canvas.getContext;
                canvas.getContext = function(contextType, attributes) {
                    if (contextType === 'webgl' || contextType === 'experimental-webgl') {
                        // Safari WebGL优化属性
                        const optimizedAttributes = Object.assign({
                            antialias: false, // Safari抗锯齿性能影响较大
                            powerPreference: 'high-performance',
                            failIfMajorPerformanceCaveat: false,
                            preserveDrawingBuffer: false,
                            premultipliedAlpha: false,
                            depth: true,
                            stencil: false
                        }, attributes);
                        
                        return originalGetContext.call(this, contextType, optimizedAttributes);
                    }
                    return originalGetContext.call(this, contextType, attributes);
                };
            }
        },
        
        /**
         * 触摸处理优化
         */
        initTouchHandling: function() {
            if (isIOS) {
                console.log('[Safari兼容性] 初始化触摸处理优化');
                
                // 防止iOS Safari的双击缩放
                let lastTouchEnd = 0;
                document.addEventListener('touchend', function(event) {
                    const now = Date.now();
                    if (now - lastTouchEnd <= 300) {
                        event.preventDefault();
                    }
                    lastTouchEnd = now;
                }, false);
                
                // 防止iOS Safari的滑动回弹
                document.addEventListener('touchmove', function(event) {
                    if (event.scale !== 1) {
                        event.preventDefault();
                    }
                }, { passive: false });
                
                // 优化触摸响应
                document.addEventListener('touchstart', function(event) {
                    // 防止iOS Safari的300ms延迟
                    if (event.touches.length === 1) {
                        const touch = event.touches[0];
                        const target = touch.target;
                        
                        if (target && target.click) {
                            // 立即触发点击事件
                            setTimeout(() => {
                                const clickEvent = new MouseEvent('click', {
                                    bubbles: true,
                                    cancelable: true,
                                    clientX: touch.clientX,
                                    clientY: touch.clientY
                                });
                                target.dispatchEvent(clickEvent);
                            }, 0);
                        }
                    }
                }, { passive: true });
            }
        },
        
        /**
         * 网络优化
         */
        initNetworkOptimizations: function() {
            console.log('[Safari兼容性] 初始化网络优化');
            
            // Safari连接池优化
            if (isSafari) {
                // 限制并发连接数（Safari连接池较小）
                this.limitConcurrentConnections();
                
                // 优化fetch请求
                this.optimizeFetchRequests();
            }
        },
        
        /**
         * 限制并发连接
         */
        limitConcurrentConnections: function() {
            const maxConcurrent = isIOS ? 4 : 6; // iOS Safari连接限制更严格
            let activeConnections = 0;
            const connectionQueue = [];
            
            const originalFetch = window.fetch;
            window.fetch = function(...args) {
                return new Promise((resolve, reject) => {
                    const executeRequest = () => {
                        activeConnections++;
                        originalFetch.apply(this, args)
                            .then(response => {
                                activeConnections--;
                                processQueue();
                                resolve(response);
                            })
                            .catch(error => {
                                activeConnections--;
                                processQueue();
                                reject(error);
                            });
                    };
                    
                    const processQueue = () => {
                        if (connectionQueue.length > 0 && activeConnections < maxConcurrent) {
                            const nextRequest = connectionQueue.shift();
                            nextRequest();
                        }
                    };
                    
                    if (activeConnections < maxConcurrent) {
                        executeRequest();
                    } else {
                        connectionQueue.push(executeRequest);
                    }
                });
            };
        },
        
        /**
         * 优化Fetch请求
         */
        optimizeFetchRequests: function() {
            const originalFetch = window.fetch;
            window.fetch = function(input, init) {
                const optimizedInit = Object.assign({
                    credentials: 'same-origin', // Safari CORS优化
                    cache: 'default',
                    redirect: 'follow'
                }, init);
                
                // Safari特殊请求头优化
                if (!optimizedInit.headers) {
                    optimizedInit.headers = {};
                }
                
                if (!optimizedInit.headers['User-Agent']) {
                    optimizedInit.headers['User-Agent'] = navigator.userAgent;
                }
                
                return originalFetch.call(this, input, optimizedInit);
            };
        },
        
        /**
         * 通用修复
         */
        initGeneralFixes: function() {
            console.log('[Safari兼容性] 初始化通用修复');
            
            // 修复Safari的一些JavaScript兼容性问题
            this.fixConsoleIssues();
            this.fixPromiseIssues();
            this.fixArrayIssues();
        },
        
        /**
         * 修复Console问题
         */
        fixConsoleIssues: function() {
            // 确保console对象在所有Safari版本中都可用
            if (!window.console) {
                window.console = {
                    log: function() {},
                    warn: function() {},
                    error: function() {},
                    info: function() {},
                    debug: function() {}
                };
            }
        },
        
        /**
         * 修复Promise问题
         */
        fixPromiseIssues: function() {
            // Safari老版本Promise兼容性
            if (!window.Promise) {
                console.warn('[Safari兼容性] Promise不可用，建议升级Safari版本');
            }
        },
        
        /**
         * 修复Array方法问题
         */
        fixArrayIssues: function() {
            // Safari某些版本缺少的Array方法
            if (!Array.prototype.includes) {
                Array.prototype.includes = function(searchElement, fromIndex) {
                    return this.indexOf(searchElement, fromIndex) !== -1;
                };
            }
            
            if (!Array.prototype.find) {
                Array.prototype.find = function(callback, thisArg) {
                    for (let i = 0; i < this.length; i++) {
                        if (callback.call(thisArg, this[i], i, this)) {
                            return this[i];
                        }
                    }
                    return undefined;
                };
            }
        },
        
        /**
         * 获取Safari信息
         */
        getSafariInfo: function() {
            return {
                isSafari: isSafari,
                isIOS: isIOS,
                isMacOS: isMacOS,
                audioUnlocked: this.audioUnlocked,
                memoryOptimized: this.memoryWarningShown,
                performanceOptimized: this.performanceOptimized,
                userAgent: navigator.userAgent,
                webkitVersion: navigator.userAgent.match(/AppleWebKit\/([\d.]+)/)?.[1] || 'Unknown'
            };
        },
        
        /**
         * 强制音频解锁（供外部调用）
         */
        forceUnlockAudio: function() {
            if (this.audioContext && this.audioContext.state === 'suspended') {
                return this.audioContext.resume();
            }
            return Promise.resolve();
        }
    };
    
    // 自动初始化（如果页面已加载）
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            window.safariCompatibility.initialize();
        });
    } else {
        window.safariCompatibility.initialize();
    }
    
    // 向Unity暴露Safari信息
    window.getSafariCompatibilityInfo = function() {
        return window.safariCompatibility.getSafariInfo();
    };
    
    console.log('[Safari兼容性] 脚本加载完成');
    
})();