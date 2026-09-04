using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Serilog;
using TwoD5DMonitoring.Infrastructure.Logging;

namespace TwoD5DMonitoring.Infrastructure
{
    /// <summary>
    /// 基础可释放视图模型，实现 IDisposable 和 INotifyPropertyChanged
    /// 防止内存泄漏，确保资源正确释放
    /// </summary>
    public abstract class DisposableViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILogger _logger;
        private bool _disposed;
        private readonly List<IDisposable> _disposables = new();
        private readonly List<Action> _cleanupActions = new();

        protected DisposableViewModel()
        {
            _logger = LogManager.CreateModuleLogger(GetType().Name);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(ref T field, T value, Action? onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            onChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 注册需要释放的对象
        /// </summary>
        protected void RegisterDisposable(IDisposable disposable)
        {
            if (disposable != null)
                _disposables.Add(disposable);
        }

        /// <summary>
        /// 注册清理动作
        /// </summary>
        protected void RegisterCleanup(Action cleanup)
        {
            if (cleanup != null)
                _cleanupActions.Add(cleanup);
        }

        /// <summary>
        /// 取消注册清理动作
        /// </summary>
        protected void UnregisterCleanup(Action cleanup)
        {
            _cleanupActions.Remove(cleanup);
        }

        /// <summary>
        /// 安全执行 UI 线程操作
        /// </summary>
        protected void ExecuteOnUI(Action action)
        {
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
            {
                action();
            }
            else
            {
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(action);
            }
        }

        /// <summary>
        /// 安全执行异步操作并处理异常
        /// </summary>
        protected async void SafeExecuteAsync(Func<Task> asyncAction, string operationName = "Async Operation")
        {
            try
            {
                await asyncAction();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in {OperationName}", operationName);
            }
        }

        /// <summary>
        /// 安全执行同步操作并处理异常
        /// </summary>
        protected void SafeExecute(Action action, string operationName = "Operation")
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in {OperationName}", operationName);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _logger.Debug("Disposing {ViewModelType}", GetType().Name);

                // 执行清理动作
                foreach (var cleanup in _cleanupActions)
                {
                    try
                    {
                        cleanup();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error during cleanup action");
                    }
                }
                _cleanupActions.Clear();

                // 释放注册的 disposable 对象
                foreach (var disposable in _disposables)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error disposing {Type}", disposable.GetType().Name);
                    }
                }
                _disposables.Clear();

                // 子类清理
                OnDisposing();

                _disposed = true;
            }
        }

        protected virtual void OnDisposing() { }

        ~DisposableViewModel()
        {
            if (!_disposed)
            {
                _logger.Warning("{ViewModelType} was not disposed properly - finalizer called", GetType().Name);
                Dispose(false);
            }
        }
    }
}