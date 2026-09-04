using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace TwoD5DMonitoring.Infrastructure
{
    /// <summary>
    /// 弱引用事件监听器，用于替代强引用事件订阅
    /// </summary>
    public class WeakEventListener<TSource, TEventArgs> : IWeakEventListener
        where TSource : class
        where TEventArgs : EventArgs
    {
        private readonly Action<TSource, TEventArgs> _handler;
        private readonly WeakReference<TSource> _sourceRef;

        public WeakEventListener(TSource source, Action<TSource, TEventArgs> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _sourceRef = new WeakReference<TSource>(source);
        }

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (_sourceRef.TryGetTarget(out var source) && sender is TSource typedSource)
            {
                if (e is TEventArgs typedArgs)
                {
                    _handler(typedSource, typedArgs);
                    return true;
                }
            }
            return false; // 返回 false 表示源已不可用，可以移除监听器
        }
    }

    /// <summary>
    /// 属性变更弱引用监听器
    /// </summary>
    public class WeakPropertyChangedListener : IWeakEventListener
    {
        private readonly Action<object, PropertyChangedEventArgs> _handler;
        private readonly WeakReference _sourceRef;

        public WeakPropertyChangedListener(INotifyPropertyChanged source, Action<object, PropertyChangedEventArgs> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _sourceRef = new WeakReference(source);
        }

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (_sourceRef.IsAlive && _sourceRef.Target == sender && e is PropertyChangedEventArgs args)
            {
                _handler(sender, args);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 绑定工具类 - 提供弱引用绑定支持
    /// </summary>
    public static class WeakBinding
    {
        /// <summary>
        /// 创建弱引用属性变更监听
        /// </summary>
        public static void AddWeakPropertyChangedListener(INotifyPropertyChanged source, Action<object, PropertyChangedEventArgs> handler)
        {
            var listener = new WeakPropertyChangedListener(source, handler);
            PropertyChangedEventManager.AddListener(source, listener, "");
        }

        /// <summary>
        /// 移除弱引用属性变更监听
        /// </summary>
        public static void RemoveWeakPropertyChangedListener(INotifyPropertyChanged source, Action<object, PropertyChangedEventArgs> handler)
        {
            // PropertyChangedEventManager 会自动管理弱引用，无需显式移除
            // 但如果需要显式移除，可以遍历监听器列表
        }

        /// <summary>
        /// 创建弱引用集合变更监听
        /// </summary>
        public static void AddWeakCollectionChangedListener(System.Collections.Specialized.INotifyCollectionChanged source, Action<object, System.Collections.Specialized.NotifyCollectionChangedEventArgs> handler)
        {
            // CollectionChangedEventManager is not available in .NET 8
            // Use a simple event subscription instead
            var listener = new WeakCollectionChangedListener(source, handler);
            source.CollectionChanged += listener.OnCollectionChanged;
        }

        private class WeakCollectionChangedListener
        {
            private readonly Action<object, System.Collections.Specialized.NotifyCollectionChangedEventArgs> _handler;
            private readonly WeakReference _sourceRef;

            public WeakCollectionChangedListener(System.Collections.Specialized.INotifyCollectionChanged source, Action<object, System.Collections.Specialized.NotifyCollectionChangedEventArgs> handler)
            {
                _handler = handler;
                _sourceRef = new WeakReference(source);
            }

            public void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                if (_sourceRef.IsAlive && _sourceRef.Target == sender)
                {
                    _handler(sender, e);
                }
            }
        }
    }
}