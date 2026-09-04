using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace TwoD5DMonitoring.Infrastructure.Database
{
    public interface IDatabaseProvider : IDisposable
    {
        string ProviderName { get; }
        
        DbConnection CreateConnection();
        
        DbConnection CreateConnection(string connectionString);
        
        Task<DbConnection> OpenConnectionAsync(string connectionString);
        
        Task CloseConnectionAsync(DbConnection connection);
        
        Task<int> ExecuteAsync(string sql, object? parameters = null, CommandType commandType = CommandType.Text, IDbTransaction? transaction = null);
        
        Task<T?> QuerySingleAsync<T>(string sql, object? parameters = null, CommandType commandType = CommandType.Text, IDbTransaction? transaction = null);
        
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, CommandType commandType = CommandType.Text, IDbTransaction? transaction = null);
        
        Task<IEnumerable<dynamic>> QueryDynamicAsync(string sql, object? parameters = null, CommandType commandType = CommandType.Text, IDbTransaction? transaction = null);
        
        Task<T> ExecuteScalarAsync<T>(string sql, object? parameters = null, CommandType commandType = CommandType.Text, IDbTransaction? transaction = null);
        
        Task<IDbTransaction> BeginTransactionAsync(DbConnection connection);
        
        Task CommitTransactionAsync(IDbTransaction transaction);
        
        Task RollbackTransactionAsync(IDbTransaction transaction);
        
        bool TestConnection(string connectionString);
        
        string GetParameterPrefix();
        
        string GetLastInsertIdSql();
        
        string GetPagedQuerySql(string baseSql, int page, int pageSize);
        
        DbParameter CreateParameter(string name, object? value);
        
        void AddParameter(DbCommand command, string name, object? value);
    }
}