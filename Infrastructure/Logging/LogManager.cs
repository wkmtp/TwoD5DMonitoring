using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;

namespace TwoD5DMonitoring.Infrastructure.Logging
{
    public static class LogManager
    {
        private static readonly object _lock = new();
        private static bool _initialized = false;
        private static LoggingLevelSwitch _levelSwitch = new(LogEventLevel.Information);

        public static void Initialize(string logDirectory = "Logs", LogEventLevel minimumLevel = LogEventLevel.Information)
        {
            lock (_lock)
            {
                if (_initialized) return;

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var fullLogDir = Path.Combine(baseDir, logDirectory);
                
                if (!Directory.Exists(fullLogDir))
                    Directory.CreateDirectory(fullLogDir);

                var logPath = Path.Combine(fullLogDir, "TwoD5DMonitoring-.log");

                _levelSwitch.MinimumLevel = minimumLevel;

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(_levelSwitch)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {ThreadId}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.Debug(
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        logPath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {ThreadId}] {Message:lj}{NewLine}{Exception}",
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(5))
                    .CreateLogger();

                _initialized = true;
                Log.Information("========================================");
                Log.Information("TwoD5DMonitoring Logging Initialized");
                Log.Information("Log Directory: {LogDirectory}", fullLogDir);
                Log.Information("Minimum Level: {MinimumLevel}", minimumLevel);
                Log.Information("========================================");
            }
        }

        public static void SetMinimumLevel(LogEventLevel level)
        {
            _levelSwitch.MinimumLevel = level;
            Log.Information("Log level changed to {Level}", level);
        }

        public static ILogger CreateModuleLogger(string moduleName)
        {
            return Log.ForContext("Module", moduleName);
        }

        public static void LogException(Exception ex, string message = "An error occurred", params object[] args)
        {
            Log.Error(ex, message, args);
        }

        public static void LogOperation(string operation, string details = "", bool success = true, params object[] args)
        {
            if (success)
                Log.Information("OPERATION: {Operation} - {Details}", operation, string.Format(details, args));
            else
                Log.Warning("OPERATION FAILED: {Operation} - {Details}", operation, string.Format(details, args));
        }

        public static void LogDataAccess(string operation, string table, TimeSpan elapsed, int rowsAffected = 0)
        {
            Log.Information("DATA ACCESS: {Operation} on {Table} took {ElapsedMs}ms, Rows: {Rows}", 
                operation, table, elapsed.TotalMilliseconds, rowsAffected);
        }

        public static void LogPlcCommunication(string plcName, string operation, TimeSpan elapsed, bool success, string details = "")
        {
            if (success)
                Log.Information("PLC: {PlcName} {Operation} took {ElapsedMs}ms - {Details}", 
                    plcName, operation, elapsed.TotalMilliseconds, details);
            else
                Log.Error("PLC ERROR: {PlcName} {Operation} failed after {ElapsedMs}ms - {Details}", 
                    plcName, operation, elapsed.TotalMilliseconds, details);
        }

        public static void Shutdown()
        {
            Log.Information("Shutting down logging...");
            Log.CloseAndFlush();
        }
    }
}