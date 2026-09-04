using System;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using TwoD5DMonitoring.Infrastructure.Database;

namespace TwoD5DMonitoring.Infrastructure.Database
{
    public class MySqlProvider : DatabaseProviderBase
    {
        public override string ProviderName => "MySQL";

        public override DbConnection CreateConnection()
        {
            return new MySqlConnection();
        }

        public override string GetParameterPrefix() => "@";

        public override string GetLastInsertIdSql() => "SELECT LAST_INSERT_ID();";

        public override string GetPagedQuerySql(string baseSql, int page, int pageSize)
        {
            var offset = (page - 1) * pageSize;
            return $"{baseSql} LIMIT {pageSize} OFFSET {offset}";
        }

        public override DbParameter CreateParameter(string name, object? value)
        {
            var param = new MySqlParameter
            {
                ParameterName = name.StartsWith("@") ? name : $"@{name}",
                Value = value ?? DBNull.Value
            };
            return param;
        }
    }
}