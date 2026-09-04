using System;
using System.Data;
using System.Data.Common;
using Npgsql;
using TwoD5DMonitoring.Infrastructure.Database;

namespace TwoD5DMonitoring.Infrastructure.Database
{
    public class PostgreSqlProvider : DatabaseProviderBase
    {
        public override string ProviderName => "PostgreSQL";

        public override DbConnection CreateConnection()
        {
            return new NpgsqlConnection();
        }

        public override string GetParameterPrefix() => "@";

        public override string GetLastInsertIdSql() => "SELECT LASTVAL();";

        public override string GetPagedQuerySql(string baseSql, int page, int pageSize)
        {
            var offset = (page - 1) * pageSize;
            return $"{baseSql} LIMIT {pageSize} OFFSET {offset}";
        }

        public override DbParameter CreateParameter(string name, object? value)
        {
            var param = new NpgsqlParameter
            {
                ParameterName = name.StartsWith("@") ? name : $"@{name}",
                Value = value ?? DBNull.Value
            };
            return param;
        }
    }
}