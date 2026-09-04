using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using TwoD5DMonitoring.Infrastructure.Database;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.Services.Repositories
{
    /// <summary>
    /// 配方下发日志仓储实现
    /// </summary>
    public class RecipeDownloadLogRepository : IRecipeDownloadLogRepository
    {
        private readonly IDatabaseProvider _db;

        public RecipeDownloadLogRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<RecipeDownloadLog?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM RecipeDownloadLogs WHERE Id = @Id";
            return await _db.QuerySingleAsync<RecipeDownloadLog>(sql, new { Id = id });
        }

        public async Task<IEnumerable<RecipeDownloadLog>> GetByRecipeIdAsync(int recipeId)
        {
            const string sql = "SELECT * FROM RecipeDownloadLogs WHERE RecipeId = @RecipeId ORDER BY StartTime DESC";
            return await _db.QueryAsync<RecipeDownloadLog>(sql, new { RecipeId = recipeId });
        }

        public async Task<IEnumerable<RecipeDownloadLog>> GetByPlcAsync(string plcName, DateTime? from = null, DateTime? to = null)
        {
            var where = new List<string> { "TargetPlc = @PlcName" };
            var parameters = new DynamicParameters();
            parameters.Add("@PlcName", plcName);

            if (from.HasValue)
            {
                where.Add("StartTime >= @From");
                parameters.Add("@From", from.Value);
            }

            if (to.HasValue)
            {
                where.Add("StartTime <= @To");
                parameters.Add("@To", to.Value);
            }

            var whereClause = "WHERE " + string.Join(" AND ", where);
            var sql = $"SELECT * FROM RecipeDownloadLogs {whereClause} ORDER BY StartTime DESC";
            return await _db.QueryAsync<RecipeDownloadLog>(sql, parameters);
        }

        public async Task<IEnumerable<RecipeDownloadLog>> GetPagedAsync(int page, int pageSize, string? plcName = null, DownloadStatus? status = null)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(plcName))
            {
                where.Add("TargetPlc = @PlcName");
                parameters.Add("@PlcName", plcName);
            }

            if (status.HasValue)
            {
                where.Add("Status = @Status");
                parameters.Add("@Status", (int)status.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $@"
                SELECT * FROM RecipeDownloadLogs 
                {whereClause}
                ORDER BY StartTime DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (page - 1) * pageSize);
            parameters.Add("@PageSize", pageSize);

            return await _db.QueryAsync<RecipeDownloadLog>(sql, parameters);
        }

        public async Task<int> GetCountAsync(string? plcName = null, DownloadStatus? status = null)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(plcName))
            {
                where.Add("TargetPlc = @PlcName");
                parameters.Add("@PlcName", plcName);
            }

            if (status.HasValue)
            {
                where.Add("Status = @Status");
                parameters.Add("@Status", (int)status.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM RecipeDownloadLogs {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<RecipeDownloadLog> AddAsync(RecipeDownloadLog log)
        {
            const string sql = @"
                INSERT INTO RecipeDownloadLogs (RecipeId, RecipeCode, RecipeName, RecipeType, Version, TargetPlc,
                    Status, ErrorMessage, Operator, StartTime, DownloadData)
                VALUES (@RecipeId, @RecipeCode, @RecipeName, @RecipeType, @Version, @TargetPlc,
                    @Status, @ErrorMessage, @Operator, @StartTime, @DownloadData);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                log.RecipeId,
                log.RecipeCode,
                log.RecipeName,
                log.RecipeType,
                log.Version,
                log.TargetPlc,
                log.Status,
                log.ErrorMessage,
                log.Operator,
                log.StartTime,
                log.DownloadData
            });

            log.Id = id;
            return log;
        }

        public async Task<RecipeDownloadLog> UpdateAsync(RecipeDownloadLog log)
        {
            const string sql = @"
                UPDATE RecipeDownloadLogs 
                SET Status = @Status, ErrorMessage = @ErrorMessage, EndTime = @EndTime, DownloadData = @DownloadData
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                log.Id,
                log.Status,
                log.ErrorMessage,
                log.EndTime,
                log.DownloadData
            });

            return log;
        }
    }

    /// <summary>
    /// 批次记录仓储实现
    /// </summary>
    public class BatchRecordRepository : IBatchRecordRepository
    {
        private readonly IDatabaseProvider _db;

        public BatchRecordRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<BatchRecord?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM BatchRecords WHERE Id = @Id";
            return await _db.QuerySingleAsync<BatchRecord>(sql, new { Id = id });
        }

        public async Task<BatchRecord?> GetByBatchNumberAsync(string batchNumber)
        {
            const string sql = "SELECT * FROM BatchRecords WHERE BatchNumber = @BatchNumber";
            return await _db.QuerySingleAsync<BatchRecord>(sql, new { BatchNumber = batchNumber });
        }

        public async Task<IEnumerable<BatchRecord>> GetPagedAsync(int page, int pageSize, BatchFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.BatchNumber))
            {
                where.Add("BatchNumber LIKE @BatchNumber");
                parameters.Add("@BatchNumber", $"%{filter.BatchNumber}%");
            }

            if (filter.RecipeId.HasValue)
            {
                where.Add("RecipeId = @RecipeId");
                parameters.Add("@RecipeId", filter.RecipeId.Value);
            }

            if (filter.Status.HasValue)
            {
                where.Add("Status = @Status");
                parameters.Add("@Status", (int)filter.Status.Value);
            }

            if (filter.StartFrom.HasValue)
            {
                where.Add("StartTime >= @StartFrom");
                parameters.Add("@StartFrom", filter.StartFrom.Value);
            }

            if (filter.StartTo.HasValue)
            {
                where.Add("StartTime <= @StartTo");
                parameters.Add("@StartTo", filter.StartTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Operator))
            {
                where.Add("Operator = @Operator");
                parameters.Add("@Operator", filter.Operator);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var orderBy = !string.IsNullOrWhiteSpace(filter.SortBy) ? filter.SortBy : "StartTime";
            var orderDir = filter.SortDescending ? "DESC" : "ASC";

            var sql = $@"
                SELECT * FROM BatchRecords 
                {whereClause}
                ORDER BY {orderBy} {orderDir}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (page - 1) * pageSize);
            parameters.Add("@PageSize", pageSize);

            return await _db.QueryAsync<BatchRecord>(sql, parameters);
        }

        public async Task<int> GetCountAsync(BatchFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.BatchNumber))
            {
                where.Add("BatchNumber LIKE @BatchNumber");
                parameters.Add("@BatchNumber", $"%{filter.BatchNumber}%");
            }

            if (filter.RecipeId.HasValue)
            {
                where.Add("RecipeId = @RecipeId");
                parameters.Add("@RecipeId", filter.RecipeId.Value);
            }

            if (filter.Status.HasValue)
            {
                where.Add("Status = @Status");
                parameters.Add("@Status", (int)filter.Status.Value);
            }

            if (filter.StartFrom.HasValue)
            {
                where.Add("StartTime >= @StartFrom");
                parameters.Add("@StartFrom", filter.StartFrom.Value);
            }

            if (filter.StartTo.HasValue)
            {
                where.Add("StartTime <= @StartTo");
                parameters.Add("@StartTo", filter.StartTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Operator))
            {
                where.Add("Operator = @Operator");
                parameters.Add("@Operator", filter.Operator);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM BatchRecords {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<BatchRecord> AddAsync(BatchRecord record)
        {
            const string sql = @"
                INSERT INTO BatchRecords (BatchNumber, RecipeId, RecipeCode, RecipeVersion, MixerId, ScaleId,
                    Status, ActualWeight, TargetWeight, StartTime, EndTime, Operator, Shift, Remark,
                    WeighingDetails, MixingDetails)
                VALUES (@BatchNumber, @RecipeId, @RecipeCode, @RecipeVersion, @MixerId, @ScaleId,
                    @Status, @ActualWeight, @TargetWeight, @StartTime, @EndTime, @Operator, @Shift, @Remark,
                    @WeighingDetails, @MixingDetails);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                record.BatchNumber,
                record.RecipeId,
                record.RecipeCode,
                record.RecipeVersion,
                record.MixerId,
                record.ScaleId,
                record.Status,
                record.ActualWeight,
                record.TargetWeight,
                record.StartTime,
                record.EndTime,
                record.Operator,
                record.Shift,
                record.Remark,
                record.WeighingDetails,
                record.MixingDetails
            });

            record.Id = id;
            return record;
        }

        public async Task<BatchRecord> UpdateAsync(BatchRecord record)
        {
            const string sql = @"
                UPDATE BatchRecords 
                SET Status = @Status, ActualWeight = @ActualWeight, EndTime = @EndTime,
                    Operator = @Operator, Shift = @Shift, Remark = @Remark,
                    WeighingDetails = @WeighingDetails, MixingDetails = @MixingDetails
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                record.Id,
                record.Status,
                record.ActualWeight,
                record.EndTime,
                record.Operator,
                record.Shift,
                record.Remark,
                record.WeighingDetails,
                record.MixingDetails
            });

            return record;
        }

        public async Task<string> GenerateBatchNumberAsync(string prefix = "BATCH")
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var sql = "SELECT COUNT(*) FROM BatchRecords WHERE BatchNumber LIKE @Prefix";
            var count = await _db.ExecuteScalarAsync<int>(sql, new { Prefix = $"{prefix}{datePart}%" });
            return $"{prefix}{datePart}{(count + 1):D4}";
        }
    }
}