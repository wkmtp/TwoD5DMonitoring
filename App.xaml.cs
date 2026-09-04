using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TwoD5DMonitoring.Configuration;
using TwoD5DMonitoring.Infrastructure.Database;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Infrastructure.PLC;
using TwoD5DMonitoring.Services;
using TwoD5DMonitoring.Services.Repositories;
using TwoD5DMonitoring.ViewModels;
using TwoD5DMonitoring.Views;
using TwoD5DMonitoring.Designer;
using TwoD5DMonitoring.Runtime;

namespace TwoD5DMonitoring
{
    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;
        private IServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 单实例检查
            var appName = Assembly.GetExecutingAssembly().GetName().Name;
            _singleInstanceMutex = new Mutex(true, $"Global\\{appName}", out bool createdNew);
            
            if (!createdNew)
            {
                MessageBox.Show("应用程序已在运行中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            // 初始化配置
            ConfigManager.Load();

            // 初始化日志系统
            var logConfig = ConfigManager.Current.Logging;
            if (Enum.TryParse<Serilog.Events.LogEventLevel>(logConfig.MinimumLevel, out var logLevel))
            {
                LogManager.Initialize(logConfig.LogDirectory, logLevel);
            }
            else
            {
                LogManager.Initialize(logConfig.LogDirectory);
            }

            // 记录启动信息
            LogManager.LogOperation("Application Starting", $"Version: {ConfigManager.Current.General.Version}, ProcessId: {Process.GetCurrentProcess().Id}");

            // 配置依赖注入
            ConfigureServices();

            base.OnStartup(e);

            // 设置全局异常处理
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // 创建并显示主窗口
            var mainWindow = _serviceProvider!.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // 配置
            services.AddSingleton(ConfigManager.Current);
            services.AddSingleton(ConfigManager.Current.Database);
            services.AddSingleton(ConfigManager.Current.Plc);
            services.AddSingleton(ConfigManager.Current.Logging);
            services.AddSingleton(ConfigManager.Current.General);

            // 数据库
            services.AddSingleton<IDatabaseProvider>(sp => 
                DatabaseProviderFactory.GetProvider());
            
            // 仓储
            services.AddScoped<IMaterialCategoryRepository, MaterialCategoryRepository>();
            services.AddScoped<IMaterialRepository, MaterialRepository>();
            services.AddScoped<ITankMaterialRepository, TankMaterialRepository>();
            services.AddScoped<IRecipeRepository, RecipeRepository>();
            services.AddScoped<IRecipeDownloadLogRepository, RecipeDownloadLogRepository>();
            services.AddScoped<IBatchRecordRepository, BatchRecordRepository>();

            // 报警仓储
            services.AddScoped<IAlarmDefinitionRepository, AlarmDefinitionRepository>();
            services.AddScoped<IAlarmInstanceRepository, AlarmInstanceRepository>();
            services.AddScoped<IAlarmHistoryRepository, AlarmHistoryRepository>();
            services.AddScoped<IAlarmNotificationRepository, AlarmNotificationRepository>();
            services.AddScoped<IAlarmSuppressionRepository, AlarmSuppressionRepository>();

            // PLC
            services.AddSingleton<PlcManager>();
            services.AddSingleton<PlcRecipeDownloadService>();

            // 服务
            services.AddScoped<MaterialService>();
            services.AddScoped<RecipeService>();
            services.AddScoped<AlarmService>();

            // ViewModels
            services.AddScoped<MaterialManagementViewModel>();
            services.AddScoped<RecipeManagementViewModel>();
            services.AddScoped<TankMaterialAssignmentViewModel>();
            services.AddScoped<MainWindowViewModel>();

            // Views
            services.AddScoped<MainWindow>();
            services.AddScoped<Views.MaterialManagementView>();
            services.AddScoped<Views.RecipeManagementView>();
            services.AddScoped<Views.TankMaterialAssignmentView>();

            // Designer/Runtime (已有的)
            services.AddScoped<Designer.DesignerCanvas>();
            services.AddScoped<Runtime.RuntimeView>();

            _serviceProvider = services.BuildServiceProvider();
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogManager.LogException(e.Exception, "UI线程未处理异常");
            
            // 显示友好错误提示
            var message = $"应用程序发生错误:\n{e.Exception.Message}\n\n详细信息已记录到日志文件。";
            MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogManager.LogException(ex ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown error"), "非UI线程未处理异常");
            
            if (e.IsTerminating)
            {
                LogManager.LogOperation("Application Terminating", "由于未处理异常导致应用程序终止");
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogManager.LogException(e.Exception, "Task未观察到的异常");
            e.SetObserved();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogManager.LogOperation("Application Exiting", $"ExitCode: {e.ApplicationExitCode}");
            LogManager.Shutdown();
            
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
            
            (_serviceProvider as IDisposable)?.Dispose();
            
            base.OnExit(e);
        }
    }
}