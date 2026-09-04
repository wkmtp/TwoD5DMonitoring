using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using TwoD5DMonitoring.Infrastructure.Database;

namespace TwoD5DMonitoring.Infrastructure.Database
{
    public class SqlServerProvider : DatabaseProviderBase
    {
        public override string ProviderName => "SQL Server";

        public override DbConnection CreateConnection()
        {
            return new SqlConnection();
        }

        public override string GetParameterPrefix() => "@";

        public override string GetLastInsertIdSql() => "SELECT SCOPE_IDENTITY();";

        public override string GetPagedQuerySql(string baseSql, int page, int pageSize)
        {
            var offset = (page - 1) * pageSize;
            return $"{baseSql} ORDER BY (SELECT NULL) OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
        }

        public override DbParameter CreateParameter(string name, object? value)
        {
            var param = new SqlParameter
            {
                ParameterName = name.StartsWith("@") ? name : $"@{name}",
                Value = value ?? DBNull.Value
            };
            return param;
        }
    }
}