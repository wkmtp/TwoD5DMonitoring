using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using Serilog;
using TwoD5DMonitoring.Infrastructure.Logging;

namespace TwoD5DMonitoring.Infrastructure.Database
{
    public abstract class DatabaseProviderBase : IDatabaseProvider
    {
        private readonly ILogger _logger;
        private bool _disposed;

        protected DatabaseProviderBase()
        {
            _logger = LogManager.CreateModuleLogger(GetType().Name);
        }

        public abstract string ProviderName { get; }
        public abstract DbConnection CreateConnection();
        public abstract string GetParameterPrefix();
        public abstract string GetLastInsertIdSql();
        public abstract string GetPagedQuerySql(string baseSql, int page, int pageSize);
        public abstract DbParameter CreateParameter(string name, object? value);

        public virtual DbConnection CreateConnection(string connectionString)
        {
            var conn = CreateConnection();
            conn.ConnectionString = connectionString;
            return conn;
        }

        public virtual async Task<DbConnection> OpenConnectionAsync(string connectionString)
        {
            var conn = CreateConnection(connectionString);
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                await conn.OpenAsync();
                _logger.Debug("Database connection opened: {Provider} in {ElapsedMs}ms", ProviderName, stopwatch.ElapsedMilliseconds);
                return conn;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, "Failed to open database connection: {ConnectionString}", MaskConnectionString(connectionString));
                throw;
            }
        }

        public virtual async Task CloseConnectionAsync(DbConnection connection)
        {
            if (connection != null && connection.State != ConnectionState.Closed)
            {
                try
                {
                    await connection.CloseAsync();
                    await connection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    LogManager.LogException(ex, "Error closing database connection");
                }
            }
        }

        public virtual async Task<int> ExecuteAsync(string sql, object? parameters = null, CommandType commandType = CommandType.Text, IDbTransaction? transaction = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();
                var result = await conn.ExecuteAsync(sql, parameters, transaction as DbTransaction, null, commandType);
                LogManager.LogDataAccess("Execute", GetTableName(sql), stopwatch.Elapsed, result);
                return result;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, "Execute failed: {Sql}", sql);
                throw;
            }
        }

        public virtual async Task<T?> QuerySingleAsync<T>(string sql, object? parameters = null, CommandType commandType = CommandType.Text, IDbTransaction? transaction = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();
                var result = await conn.QuerySingleOrDefaultAsync<T>(sql, parameters, transaction as DbTransaction, null, commandType);
                LogManager.LogDataAccess("QuerySingle", GetTableName(sql), stopwatch.Elapsed, result != null ? 1 : 0);
                return result;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, "QuerySingle failed: {Sql}", sql);
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, CommandType commandType = CommandType.Text, IDbTransaction? transaction = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();
                var result = await conn.QueryAsync<T>(sql, parameters, transaction as DbTransaction, null, commandType);
                var list = result.AsList();
                LogManager.LogDataAccess("Query", GetTableName(sql), stopwatch.Elapsed, list.Count);
                return list;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, "Query failed: {Sql}", sql);
                throw;
            }
        }

        public virtual async Task<IEnumerable<dynamic>> QueryDynamicAsync(string sql, object? parameters = null, CommandType commandType = CommandType.Text, IDbTransaction? transaction = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();
                var result = await conn.QueryAsync(sql, parameters, transaction as DbTransaction, null, commandType);
                var list = result.AsList();
                LogManager.LogDataAccess("QueryDynamic", GetTableName(sql), stopwatch.Elapsed, list.Count);
                return list;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, "QueryDynamic failed: {Sql}", sql);
                throw;
            }
        }

        public virtual async Task<T> ExecuteScalarAsync<T>(string sql, object? parameters = null, CommandType commandType = CommandType.Text, IDbTransaction? transaction = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync();
                var result = await conn.ExecuteScalarAsync<T>(sql, parameters, transaction as DbTransaction, null, commandType);
                LogManager.LogDataAccess("ExecuteScalar", GetTableName(sql), stopwatch.Elapsed);
                return result!;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, "ExecuteScalar failed: {Sql}", sql);
                throw;
            }
        }

        public virtual async Task<IDbTransaction> BeginTransactionAsync(DbConnection connection)
        {
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            
            return await Task.FromResult(connection.BeginTransaction());
        }

        public virtual async Task CommitTransactionAsync(IDbTransaction transaction)
        {
            try
            {
                transaction.Commit();
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, "Commit transaction failed");
                throw;
            }
        }

        public virtual async Task RollbackTransactionAsync(IDbTransaction transaction)
        {
            try
            {
                transaction.Rollback();
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, "Rollback transaction failed");
                throw;
            }
        }

        public virtual bool TestConnection(string connectionString)
        {
            try
            {
                using var conn = CreateConnection(connectionString);
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public virtual void AddParameter(DbCommand command, string name, object? value)
        {
            var param = CreateParameter(name, value);
            command.Parameters.Add(param);
        }

        protected virtual string GetTableName(string sql)
        {
            try
            {
                var upper = sql.Trim().ToUpperInvariant();
                if (upper.StartsWith("SELECT"))
                {
                    var fromIndex = upper.IndexOf(" FROM ");
                    if (fromIndex >= 0)
                    {
                        var afterFrom = upper.Substring(fromIndex + 6).TrimStart();
                        var endIndex = afterFrom.IndexOfAny(new[] { ' ', '\t', '\n', '\r', ';', '(', 'J', 'W', 'G', 'O', 'L' });
                        if (endIndex >= 0)
                            return afterFrom.Substring(0, endIndex);
                        return afterFrom;
                    }
                }
                else if (upper.StartsWith("INSERT"))
                {
                    var intoIndex = upper.IndexOf(" INTO ");
                    if (intoIndex >= 0)
                    {
                        var afterInto = upper.Substring(intoIndex + 6).TrimStart();
                        var endIndex = afterInto.IndexOfAny(new[] { ' ', '\t', '\n', '\r', '(', ';', 'V' });
                        if (endIndex >= 0)
                            return afterInto.Substring(0, endIndex);
                        return afterInto;
                    }
                }
                else if (upper.StartsWith("UPDATE"))
                {
                    var afterUpdate = upper.Substring(6).TrimStart();
                    var endIndex = afterUpdate.IndexOfAny(new[] { ' ', '\t', '\n', '\r', 'S' });
                    if (endIndex >= 0)
                        return afterUpdate.Substring(0, endIndex);
                    return afterUpdate;
                }
                else if (upper.StartsWith("DELETE"))
                {
                    var fromIndex = upper.IndexOf(" FROM ");
                    if (fromIndex >= 0)
                    {
                        var afterFrom = upper.Substring(fromIndex + 6).TrimStart();
                        var endIndex = afterFrom.IndexOfAny(new[] { ' ', '\t', '\n', '\r', 'W', ';' });
                        if (endIndex >= 0)
                            return afterFrom.Substring(0, endIndex);
                        return afterFrom;
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        protected virtual string MaskConnectionString(string connectionString)
        {
            try
            {
                var parts = connectionString.Split(';');
                var masked = new List<string>();
                foreach (var part in parts)
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length == 2 && (kv[0].Trim().ToLower().Contains("password") || kv[0].Trim().ToLower().Contains("pwd")))
                    {
                        masked.Add($"{kv[0]}=***");
                    }
                    else
                    {
                        masked.Add(part);
                    }
                }
                return string.Join(";", masked);
            }
            catch
            {
                return "***";
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
                _disposed = true;
            }
        }

        ~DatabaseProviderBase()
        {
            Dispose(false);
        }
    }
}