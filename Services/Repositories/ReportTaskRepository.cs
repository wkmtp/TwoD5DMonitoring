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
    /// 报表生成任务仓储实现
    /// </summary>
    public class ReportTaskRepository : IReportTaskRepository
    {
        private readonly IDatabaseProvider _db;

        public ReportTaskRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<ReportGenerationTask?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM ReportGenerationTasks WHERE Id = @Id";
            return await _db.QuerySingleAsync<ReportGenerationTask>(sql, new { Id = id });
        }

        public async Task<(IEnumerable<ReportGenerationTask> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, ReportTaskFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.TaskName))
            {
                where.Add("TaskName LIKE @TaskName");
                parameters.Add("@TaskName", $"%{filter.TaskName}%");
            }

            if (filter.TemplateId.HasValue)
            {
                where.Add("TemplateId = @TemplateId");
                parameters.Add("@TemplateId", filter.TemplateId.Value);
            }

            if (filter.ReportType.HasValue)
            {
                where.Add("ReportType = @ReportType");
                parameters.Add("@ReportType", (int)filter.ReportType.Value);
            }

            if (filter.Status.HasValue)
            {
                where.Add("Status = @Status");
                parameters.Add("@Status", (int)filter.Status.Value);
            }

            if (filter.ScheduledFrom.HasValue)
            {
                where.Add("ScheduledTime >= @ScheduledFrom");
                parameters.Add("@ScheduledFrom", filter.ScheduledFrom.Value);
            }

            if (filter.ScheduledTo.HasValue)
            {
                where.Add("ScheduledTime <= @ScheduledTo");
                parameters.Add("@ScheduledTo", filter.ScheduledTo.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var orderBy = !string.IsNullOrWhiteSpace(filter.SortBy) ? filter.SortBy : "ScheduledTime";
            var orderDir = filter.SortDescending ? "DESC" : "ASC";

            var sql = $@"
                SELECT * FROM ReportGenerationTasks
                {whereClause}
                ORDER BY {orderBy} {orderDir}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (filter.PageIndex - 1) * filter.PageSize);
            parameters.Add("@PageSize", filter.PageSize);

            var items = await _db.QueryAsync<ReportGenerationTask>(sql, parameters);

            var countSql = $"SELECT COUNT(*) FROM ReportGenerationTasks {whereClause}";
            var total = await _db.ExecuteScalarAsync<int>(countSql, parameters);

            return (items, total);
        }

        public async Task<int> GetCountAsync(ReportTaskFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.TaskName))
            {
                where.Add("TaskName LIKE @TaskName");
                parameters.Add("@TaskName", $"%{filter.TaskName}%");
            }

            if (filter.TemplateId.HasValue)
            {
                where.Add("TemplateId = @TemplateId");
                parameters.Add("@TemplateId", filter.TemplateId.Value);
            }

            if (filter.ReportType.HasValue)
            {
                where.Add("ReportType = @ReportType");
                parameters.Add("@ReportType", (int)filter.ReportType.Value);
            }

            if (filter.Status.HasValue)
            {
                where.Add("Status = @Status");
                parameters.Add("@Status", (int)filter.Status.Value);
            }

            if (filter.ScheduledFrom.HasValue)
            {
                where.Add("ScheduledTime >= @ScheduledFrom");
                parameters.Add("@ScheduledFrom", filter.ScheduledFrom.Value);
            }

            if (filter.ScheduledTo.HasValue)
            {
                where.Add("ScheduledTime <= @ScheduledTo");
                parameters.Add("@ScheduledTo", filter.ScheduledTo.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM ReportGenerationTasks {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<ReportGenerationTask> AddAsync(ReportGenerationTask task)
        {
            const string sql = @"
                INSERT INTO ReportGenerationTasks (TaskName, TemplateId, ReportType, Version, ScheduledTime,
                    Status, ErrorMessage, Operator, OutputPath, ParametersJson, CreatedAt)
                VALUES (@TaskName, @TemplateId, @ReportType, @Version, @ScheduledTime,
                    @Status, @ErrorMessage, @Operator, @OutputPath, @ParametersJson, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                task.TaskName,
                task.TemplateId,
                task.ReportType,
                task.Version,
                task.ScheduledTime,
                task.Status,
                task.ErrorMessage,
                task.Operator,
                task.OutputPath,
                task.ParametersJson,
                task.CreatedAt
            });

            task.Id = id;
            return task;
        }

        public async Task<ReportGenerationTask> UpdateAsync(ReportGenerationTask task)
        {
            const string sql = @"
                UPDATE ReportGenerationTasks 
                SET TaskName = @TaskName, TemplateId = @TemplateId, ReportType = @ReportType,
                    Version = @Version, ScheduledTime = @ScheduledTime, Status = @Status,
                    ErrorMessage = @ErrorMessage, OutputPath = @OutputPath, ParametersJson = @ParametersJson,
                    StartTime = @StartTime, EndTime = @EndTime, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                task.Id,
                task.TaskName,
                task.TemplateId,
                task.ReportType,
                task.Version,
                task.ScheduledTime,
                task.Status,
                task.ErrorMessage,
                task.OutputPath,
                task.ParametersJson,
                task.StartTime,
                task.EndTime,
                UpdatedAt = DateTime.Now,
                task.UpdatedBy
            });

            return task;
        }

        public async Task<IEnumerable<ReportGenerationTask>> GetPendingTasksAsync(DateTime beforeTime)
        {
            const string sql = @"
                SELECT * FROM ReportGenerationTasks
                WHERE Status = 1 AND ScheduledTime <= @BeforeTime
                ORDER BY ScheduledTime";
            return await _db.QueryAsync<ReportGenerationTask>(sql, new { BeforeTime = beforeTime });
        }
    }
}