using System;
using System.Collections.Generic;
using System.Windows;

namespace TwoD5DMonitoring.Infrastructure
{
    /// <summary>
    /// WeakEventManager 实现，防止事件订阅导致的内存泄漏
    /// </summary>
    public class WeakEventManager<TEventSource, TEventArgs> where TEventArgs : EventArgs
    {
        private readonly List<WeakReference<EventHandler<TEventArgs>>> _handlers = new();
        private readonly object _lock = new();

        public void AddHandler(TEventSource source, EventHandler<TEventArgs> handler)
        {
            lock (_lock)
            {
                CleanupDeadReferences();
                _handlers.Add(new WeakReference<EventHandler<TEventArgs>>(handler));
            }
        }

        public void RemoveHandler(TEventSource source, EventHandler<TEventArgs> handler)
        {
            lock (_lock)
            {
                for (int i = _handlers.Count - 1; i >= 0; i--)
                {
                    if (_handlers[i].TryGetTarget(out var target) && target == handler)
                    {
                        _handlers.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void RaiseEvent(TEventSource sender, TEventArgs args)
        {
            List<EventHandler<TEventArgs>> liveHandlers;
            
            lock (_lock)
            {
                CleanupDeadReferences();
                liveHandlers = new List<EventHandler<TEventArgs>>();
                foreach (var weakRef in _handlers)
                {
                    if (weakRef.TryGetTarget(out var handler))
                    {
                        liveHandlers.Add(handler);
                    }
                }
            }

            foreach (var handler in liveHandlers)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WeakEvent handler error: {ex}");
                }
            }
        }

        private void CleanupDeadReferences()
        {
            for (int i = _handlers.Count - 1; i >= 0; i--)
            {
                if (!_handlers[i].TryGetTarget(out _))
                {
                    _handlers.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// 通用弱引用事件管理器
    /// </summary>
    public static class WeakEvent
    {
        public static void AddHandler<TEventArgs>(object source, string eventName, EventHandler<TEventArgs> handler)
            where TEventArgs : EventArgs
        {
            var managerType = typeof(WeakEventManager<,>).MakeGenericType(source.GetType(), typeof(TEventArgs));
            var manager = Activator.CreateInstance(managerType);
            var addMethod = managerType.GetMethod("AddHandler");
            addMethod?.Invoke(manager, new object[] { source, handler });
        }

        public static void RemoveHandler<TEventArgs>(object source, string eventName, EventHandler<TEventArgs> handler)
            where TEventArgs : EventArgs
        {
            var managerType = typeof(WeakEventManager<,>).MakeGenericType(source.GetType(), typeof(TEventArgs));
            var manager = Activator.CreateInstance(managerType);
            var removeMethod = managerType.GetMethod("RemoveHandler");
            removeMethod?.Invoke(manager, new object[] { source, handler });
        }
    }
}