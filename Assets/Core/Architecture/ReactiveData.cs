// Assets/_Core/Architecture/ReactiveData.cs
// 响应式数据系统 - 类似Vue的ref，实现数据变化时自动通知UI更新

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Architecture
{
    /// <summary>
    /// 响应式数据容器 - 类似Vue的ref
    /// 当数据变化时自动触发事件通知所有订阅者
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    [System.Serializable]
    public class ReactiveData<T>
    {
        [Header("响应式数据")]
        [SerializeField] private T _value;
        [SerializeField] private bool _hasInitialValue = false;
        [SerializeField] private string _debugName = "";

        [Header("调试信息")]
        [SerializeField] private int _subscriberCount = 0;
        [SerializeField] private int _notificationCount = 0;
        [SerializeField] private float _lastChangeTime = 0f;

        // 事件系统
        [System.NonSerialized] private event Action<T> _onValueChanged;
        [System.NonSerialized] private event Action<T, T> _onValueChangedWithOld;
        [System.NonSerialized] private List<IReactiveDataObserver<T>> _observers;

        // 比较器
        [System.NonSerialized] private IEqualityComparer<T> _comparer;

        /// <summary>
        /// 数据值
        /// </summary>
        public T Value
        {
            get => _value;
            set => SetValue(value, true);
        }

        /// <summary>
        /// 是否有初始值
        /// </summary>
        public bool HasValue => _hasInitialValue;

        /// <summary>
        /// 订阅者数量
        /// </summary>
        public int SubscriberCount => _subscriberCount;

        /// <summary>
        /// 通知次数
        /// </summary>
        public int NotificationCount => _notificationCount;

        /// <summary>
        /// 最后变化时间
        /// </summary>
        public float LastChangeTime => _lastChangeTime;

        /// <summary>
        /// 调试名称
        /// </summary>
        public string DebugName
        {
            get => _debugName;
            set => _debugName = value;
        }

        /// <summary>
        /// 值变化事件
        /// </summary>
        public event Action<T> OnValueChanged
        {
            add
            {
                _onValueChanged += value;
                _subscriberCount++;
                // 如果已有值，立即通知新订阅者
                if (_hasInitialValue && value != null)
                {
                    try
                    {
                        value.Invoke(_value);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"ReactiveData<{typeof(T).Name}> '{_debugName}' 立即通知新订阅者时发生错误: {e.Message}");
                    }
                }
            }
            remove
            {
                _onValueChanged -= value;
                _subscriberCount = Math.Max(0, _subscriberCount - 1);
            }
        }

        /// <summary>
        /// 值变化事件（包含旧值）
        /// </summary>
        public event Action<T, T> OnValueChangedWithOld
        {
            add
            {
                _onValueChangedWithOld += value;
                _subscriberCount++;
            }
            remove
            {
                _onValueChangedWithOld -= value;
                _subscriberCount = Math.Max(0, _subscriberCount - 1);
            }
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ReactiveData()
        {
            InitializeInternal("");
        }

        /// <summary>
        /// 带初始值的构造函数
        /// </summary>
        /// <param name="initialValue">初始值</param>
        public ReactiveData(T initialValue)
        {
            InitializeInternal("");
            SetValue(initialValue, false); // 初始化时不触发事件
        }

        /// <summary>
        /// 带初始值和调试名称的构造函数
        /// </summary>
        /// <param name="initialValue">初始值</param>
        /// <param name="debugName">调试名称</param>
        public ReactiveData(T initialValue, string debugName)
        {
            InitializeInternal(debugName);
            SetValue(initialValue, false); // 初始化时不触发事件
        }

        /// <summary>
        /// 内部初始化
        /// </summary>
        /// <param name="debugName">调试名称</param>
        private void InitializeInternal(string debugName)
        {
            _debugName = debugName;
            _hasInitialValue = false;
            _subscriberCount = 0;
            _notificationCount = 0;
            _lastChangeTime = 0f;
            _observers = new List<IReactiveDataObserver<T>>();
            _comparer = EqualityComparer<T>.Default;
        }

        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="newValue">新值</param>
        /// <param name="notify">是否通知订阅者</param>
        public void SetValue(T newValue, bool notify = true)
        {
            T oldValue = _value;
            bool hasChanged = !_hasInitialValue || !_comparer.Equals(_value, newValue);

            _value = newValue;
            _hasInitialValue = true;

            if (hasChanged && notify)
            {
                _lastChangeTime = Time.time;
                _notificationCount++;
                NotifySubscribers(oldValue, newValue);
            }
        }

        /// <summary>
        /// 静默设置值（不触发通知）
        /// </summary>
        /// <param name="newValue">新值</param>
        public void SetValueSilent(T newValue)
        {
            SetValue(newValue, false);
        }

        /// <summary>
        /// 强制通知所有订阅者（即使值没有变化）
        /// </summary>
        public void ForceNotify()
        {
            if (_hasInitialValue)
            {
                _lastChangeTime = Time.time;
                _notificationCount++;
                NotifySubscribers(_value, _value);
            }
        }

        /// <summary>
        /// 通知订阅者
        /// </summary>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        private void NotifySubscribers(T oldValue, T newValue)
        {
            // 通知Action订阅者
            try
            {
                _onValueChanged?.Invoke(newValue);
            }
            catch (Exception e)
            {
                Debug.LogError($"ReactiveData<{typeof(T).Name}> '{_debugName}' OnValueChanged 事件处理时发生错误: {e.Message}");
            }

            try
            {
                _onValueChangedWithOld?.Invoke(oldValue, newValue);
            }
            catch (Exception e)
            {
                Debug.LogError($"ReactiveData<{typeof(T).Name}> '{_debugName}' OnValueChangedWithOld 事件处理时发生错误: {e.Message}");
            }

            // 通知Observer订阅者
            for (int i = _observers.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (_observers[i] != null)
                    {
                        _observers[i].OnValueChanged(oldValue, newValue);
                    }
                    else
                    {
                        // 移除null引用
                        _observers.RemoveAt(i);
                        _subscriberCount = Math.Max(0, _subscriberCount - 1);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"ReactiveData<{typeof(T).Name}> '{_debugName}' Observer 通知时发生错误: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 添加观察者
        /// </summary>
        /// <param name="observer">观察者</param>
        public void AddObserver(IReactiveDataObserver<T> observer)
        {
            if (observer != null && !_observers.Contains(observer))
            {
                _observers.Add(observer);
                _subscriberCount++;

                // 如果已有值，立即通知新观察者
                if (_hasInitialValue)
                {
                    try
                    {
                        observer.OnValueChanged(_value, _value);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"ReactiveData<{typeof(T).Name}> '{_debugName}' 立即通知新观察者时发生错误: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 移除观察者
        /// </summary>
        /// <param name="observer">观察者</param>
        public void RemoveObserver(IReactiveDataObserver<T> observer)
        {
            if (observer != null && _observers.Remove(observer))
            {
                _subscriberCount = Math.Max(0, _subscriberCount - 1);
            }
        }

        /// <summary>
        /// 清除所有订阅者
        /// </summary>
        public void ClearAllSubscribers()
        {
            _onValueChanged = null;
            _onValueChangedWithOld = null;
            _observers.Clear();
            _subscriberCount = 0;
        }

        /// <summary>
        /// 设置自定义比较器
        /// </summary>
        /// <param name="comparer">比较器</param>
        public void SetComparer(IEqualityComparer<T> comparer)
        {
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void Reset()
        {
            SetValue(default(T), true);
        }

        /// <summary>
        /// 重置为默认值且清除订阅者
        /// </summary>
        public void ResetAndClear()
        {
            ClearAllSubscribers();
            _value = default(T);
            _hasInitialValue = false;
            _notificationCount = 0;
            _lastChangeTime = 0f;
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        /// <returns>调试信息字符串</returns>
        public string GetDebugInfo()
        {
            return $"ReactiveData<{typeof(T).Name}> '{_debugName}': " +
                   $"Value={_value}, HasValue={_hasInitialValue}, " +
                   $"Subscribers={_subscriberCount}, Notifications={_notificationCount}, " +
                   $"LastChange={_lastChangeTime:F2}s";
        }

        /// <summary>
        /// 隐式转换到T类型
        /// </summary>
        /// <param name="reactiveData">响应式数据</param>
        public static implicit operator T(ReactiveData<T> reactiveData)
        {
            return reactiveData != null ? reactiveData.Value : default(T);
        }

        /// <summary>
        /// 从T类型隐式转换
        /// </summary>
        /// <param name="value">值</param>
        public static implicit operator ReactiveData<T>(T value)
        {
            return new ReactiveData<T>(value);
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return _hasInitialValue ? _value?.ToString() ?? "null" : "未初始化";
        }

        /// <summary>
        /// 相等性比较
        /// </summary>
        /// <param name="obj">比较对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object obj)
        {
            if (obj is ReactiveData<T> other)
            {
                return _comparer.Equals(_value, other._value);
            }
            if (obj is T directValue)
            {
                return _comparer.Equals(_value, directValue);
            }
            return false;
        }

        /// <summary>
        /// 获取哈希码
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode()
        {
            return _hasInitialValue ? _comparer.GetHashCode(_value) : 0;
        }
    }

    /// <summary>
    /// 响应式数据观察者接口
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public interface IReactiveDataObserver<T>
    {
        /// <summary>
        /// 值变化时的回调
        /// </summary>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        void OnValueChanged(T oldValue, T newValue);
    }

    /// <summary>
    /// 响应式数据扩展方法
    /// </summary>
    public static class ReactiveDataExtensions
    {
        /// <summary>
        /// 绑定到另一个响应式数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="source">源数据</param>
        /// <param name="target">目标数据</param>
        /// <returns>取消绑定的Action</returns>
        public static System.Action BindTo<T>(this ReactiveData<T> source, ReactiveData<T> target)
        {
            if (source == null || target == null) return null;

            System.Action<T> handler = value => target.Value = value;
            source.OnValueChanged += handler;

            // 立即同步当前值
            if (source.HasValue)
            {
                target.Value = source.Value;
            }

            // 返回取消绑定的方法
            return () => source.OnValueChanged -= handler;
        }

        /// <summary>
        /// 双向绑定
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data1">数据1</param>
        /// <param name="data2">数据2</param>
        /// <returns>取消绑定的Action</returns>
        public static System.Action BindTwoWay<T>(this ReactiveData<T> data1, ReactiveData<T> data2)
        {
            if (data1 == null || data2 == null) return null;

            bool updating = false;

            System.Action<T> handler1 = value =>
            {
                if (!updating)
                {
                    updating = true;
                    data2.Value = value;
                    updating = false;
                }
            };

            System.Action<T> handler2 = value =>
            {
                if (!updating)
                {
                    updating = true;
                    data1.Value = value;
                    updating = false;
                }
            };

            data1.OnValueChanged += handler1;
            data2.OnValueChanged += handler2;

            // 同步初始值
            if (data1.HasValue && !data2.HasValue)
            {
                data2.Value = data1.Value;
            }
            else if (data2.HasValue && !data1.HasValue)
            {
                data1.Value = data2.Value;
            }

            // 返回取消绑定的方法
            return () =>
            {
                data1.OnValueChanged -= handler1;
                data2.OnValueChanged -= handler2;
            };
        }

        /// <summary>
        /// 映射到另一种类型的响应式数据
        /// </summary>
        /// <typeparam name="TSource">源类型</typeparam>
        /// <typeparam name="TTarget">目标类型</typeparam>
        /// <param name="source">源数据</param>
        /// <param name="mapper">映射函数</param>
        /// <returns>映射后的响应式数据</returns>
        public static ReactiveData<TTarget> Map<TSource, TTarget>(this ReactiveData<TSource> source, Func<TSource, TTarget> mapper)
        {
            if (source == null || mapper == null) return new ReactiveData<TTarget>();

            var result = new ReactiveData<TTarget>($"{source.DebugName}_Mapped");
            
            source.OnValueChanged += value =>
            {
                try
                {
                    result.Value = mapper(value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"ReactiveData Map 转换时发生错误: {e.Message}");
                }
            };

            // 立即映射当前值
            if (source.HasValue)
            {
                try
                {
                    result.Value = mapper(source.Value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"ReactiveData Map 初始转换时发生错误: {e.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// 过滤值变化
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="source">源数据</param>
        /// <param name="predicate">过滤条件</param>
        /// <returns>过滤后的响应式数据</returns>
        public static ReactiveData<T> Filter<T>(this ReactiveData<T> source, Func<T, bool> predicate)
        {
            if (source == null || predicate == null) return new ReactiveData<T>();

            var result = new ReactiveData<T>($"{source.DebugName}_Filtered");
            
            source.OnValueChanged += value =>
            {
                try
                {
                    if (predicate(value))
                    {
                        result.Value = value;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"ReactiveData Filter 过滤时发生错误: {e.Message}");
                }
            };

            // 检查初始值
            if (source.HasValue)
            {
                try
                {
                    if (predicate(source.Value))
                    {
                        result.Value = source.Value;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"ReactiveData Filter 初始过滤时发生错误: {e.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// 防抖处理
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="source">源数据</param>
        /// <param name="delay">延迟时间（秒）</param>
        /// <returns>防抖后的响应式数据</returns>
        public static ReactiveData<T> Debounce<T>(this ReactiveData<T> source, float delay)
        {
            if (source == null) return new ReactiveData<T>();

            var result = new ReactiveData<T>($"{source.DebugName}_Debounced");
            
            // 这里需要配合MonoBehaviour的协程来实现防抖
            // 简化版本，实际使用时可能需要更复杂的实现
            T lastValue = default(T);
            float lastChangeTime = 0f;

            source.OnValueChanged += value =>
            {
                lastValue = value;
                lastChangeTime = Time.time;
                
                // 这里应该启动一个延迟检查
                // 简化实现：立即设置值（实际项目中应使用协程）
                result.Value = value;
            };

            return result;
        }
    }
}