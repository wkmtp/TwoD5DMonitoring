using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using TwoD5DMonitoring.Infrastructure.Database;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.Services.Repositories
{
    // ===== 历史趋势仓储实现 =====
    public class HistoricalTrendRepository : IHistoricalTrendRepository
    {
        private readonly IDatabaseProvider _db;

        public HistoricalTrendRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<IEnumerable<HistoricalTrendPoint>> GetTrendDataAsync(List<string> tagNames, DateTime startTime, DateTime endTime, int maxPoints = 1000)
        {
            if (!tagNames.Any()) return Enumerable.Empty<HistoricalTrendPoint>();

            var tagList = string.Join(",", tagNames.Select((_, i) => $"@Tag{i}"));
            var parameters = new DynamicParameters();
            for (int i = 0; i < tagNames.Count; i++)
                parameters.Add($"@Tag{i}", tagNames[i]);

            parameters.Add("@StartTime", startTime);
            parameters.Add("@EndTime", endTime);
            parameters.Add("@MaxPoints", maxPoints);

            var sql = $@"
                SELECT TagName, Timestamp, Value, Quality
                FROM HistoricalTrendData
                WHERE TagName IN ({tagList}) AND Timestamp >= @StartTime AND Timestamp <= @EndTime
                ORDER BY Timestamp
                OFFSET 0 ROWS FETCH NEXT @MaxPoints ROWS ONLY";

            return await _db.QueryAsync<HistoricalTrendPoint>(sql, parameters);
        }

        public async Task<IEnumerable<TrendSeries>> GetAggregatedTrendAsync(TrendQueryParams param)
        {
            if (!param.TagNames.Any()) return Enumerable.Empty<TrendSeries>();

            var tagList = string.Join(",", param.TagNames.Select((_, i) => $"@Tag{i}"));
            var parameters = new DynamicParameters();
            for (int i = 0; i < param.TagNames.Count; i++)
                parameters.Add($"@Tag{i}", param.TagNames[i]);

            parameters.Add("@StartTime", param.StartTime);
            parameters.Add("@EndTime", param.EndTime);

            string aggregationSql;
            string intervalSql = "";

            if (param.Aggregation != AggregationType.Raw && param.Interval.HasValue)
            {
                var intervalMs = param.Interval.Value.TotalMilliseconds;
                intervalSql = $", DATEADD(MILLISECOND, FLOOR(DATEDIFF(MILLISECOND, '1970-01-01', Timestamp) / {intervalMs}) * {intervalMs}, '1970-01-01') as IntervalStart";
                
                aggregationSql = param.Aggregation switch
                {
                    AggregationType.Average => "AVG(Value) as Value",
                    AggregationType.Min => "MIN(Value) as Value",
                    AggregationType.Max => "MAX(Value) as Value",
                    AggregationType.Sum => "SUM(Value) as Value",
                    AggregationType.Count => "COUNT(*) as Value",
                    _ => "AVG(Value) as Value"
                };

                var sql = $@"
                    SELECT TagName, {aggregationSql}, MIN(Quality) as Quality, IntervalStart as Timestamp
                    FROM HistoricalTrendData
                    WHERE TagName IN ({tagList}) AND Timestamp >= @StartTime AND Timestamp <= @EndTime
                    GROUP BY TagName, IntervalStart
                    ORDER BY IntervalStart";

                var results = await _db.QueryAsync<dynamic>(sql, parameters);
                return results.GroupBy(r => (string)r.TagName)
                    .Select(g => new TrendSeries
                    {
                        TagName = g.Key,
                        DisplayName = g.Key,
                        Points = g.Select(r => new TrendPoint
                        {
                            Time = r.Timestamp,
                            Value = (double)r.Value,
                            Quality = (int)r.Quality
                        }).ToList()
                    });
            }
            else
            {
                // 原始数据，需要降采样
                var sql = $@"
                    SELECT TagName, Timestamp, Value, Quality
                    FROM HistoricalTrendData
                    WHERE TagName IN ({tagList}) AND Timestamp >= @StartTime AND Timestamp <= @EndTime
                    ORDER BY Timestamp";

                var results = await _db.QueryAsync<HistoricalTrendPoint>(sql, parameters);
                return results.GroupBy(r => r.TagName)
                    .Select(g => new TrendSeries
                    {
                        TagName = g.Key,
                        DisplayName = g.Key,
                        Points = g.Take(param.MaxPoints).Select(r => new TrendPoint
                        {
                            Time = r.Timestamp,
                            Value = r.Value,
                            Quality = r.Quality
                        }).ToList()
                    });
            }
        }

        public async Task<long> GetCountAsync(string tagName, DateTime start, DateTime end)
        {
            const string sql = "SELECT COUNT(*) FROM HistoricalTrendData WHERE TagName = @TagName AND Timestamp >= @Start AND Timestamp <= @End";
            return await _db.ExecuteScalarAsync<long>(sql, new { TagName = tagName, Start = start, End = end });
        }

        public async Task BulkInsertAsync(IEnumerable<HistoricalTrendPoint> points)
        {
            var pointList = points.ToList();
            if (!pointList.Any()) return;

            const string sql = @"
                INSERT INTO HistoricalTrendData (TagName, Timestamp, Value, Quality)
                VALUES (@TagName, @Timestamp, @Value, @Quality)";

            // 分批插入，每批 1000 条
            const int batchSize = 1000;
            for (int i = 0; i < pointList.Count; i += batchSize)
            {
                var batch = pointList.Skip(i).Take(batchSize).ToList();
                await _db.ExecuteAsync(sql, batch);
            }
        }

        public async Task DeleteOldDataAsync(DateTime beforeDate)
        {
            const string sql = "DELETE FROM HistoricalTrendData WHERE Timestamp < @BeforeDate";
            await _db.ExecuteAsync(sql, new { BeforeDate = beforeDate });
        }
    }

    // ===== 生产报表仓储实现 =====
    public class ProductionReportRepository : IProductionReportRepository
    {
        private readonly IDatabaseProvider _db;

        public ProductionReportRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<ProductionReport?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM ProductionReports WHERE Id = @Id";
            return await _db.QuerySingleAsync<ProductionReport>(sql, new { Id = id });
        }

        public async Task<ProductionReport?> GetByNumberAsync(string reportNumber)
        {
            const string sql = "SELECT * FROM ProductionReports WHERE ReportNumber = @ReportNumber";
            return await _db.QuerySingleAsync<ProductionReport>(sql, new { ReportNumber = reportNumber });
        }

        public async Task<(IEnumerable<ProductionReport> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, ReportFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(ReportNumber LIKE @Keyword OR GeneratedBy LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
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

            if (filter.PeriodFrom.HasValue)
            {
                where.Add("PeriodStart >= @PeriodFrom");
                parameters.Add("@PeriodFrom", filter.PeriodFrom.Value);
            }

            if (filter.PeriodTo.HasValue)
            {
                where.Add("PeriodEnd <= @PeriodTo");
                parameters.Add("@PeriodTo", filter.PeriodTo.Value);
            }

            if (filter.ShiftId.HasValue)
            {
                where.Add("ShiftId = @ShiftId");
                parameters.Add("@ShiftId", filter.ShiftId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.GeneratedBy))
            {
                where.Add("GeneratedBy = @GeneratedBy");
                parameters.Add("@GeneratedBy", filter.GeneratedBy);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var orderBy = !string.IsNullOrWhiteSpace(filter.SortBy) ? filter.SortBy : "GeneratedAt";
            var orderDir = filter.SortDescending ? "DESC" : "ASC";

            var sql = $@"
                SELECT * FROM ProductionReports
                {whereClause}
                ORDER BY {orderBy} {orderDir}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (filter.PageIndex - 1) * filter.PageSize);
            parameters.Add("@PageSize", filter.PageSize);

            var items = await _db.QueryAsync<ProductionReport>(sql, parameters);

            var countSql = $"SELECT COUNT(*) FROM ProductionReports {whereClause}";
            var total = await _db.ExecuteScalarAsync<int>(countSql, parameters);

            return (items, total);
        }

        public async Task<int> GetCountAsync(ReportFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(ReportNumber LIKE @Keyword OR GeneratedBy LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
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

            if (filter.PeriodFrom.HasValue)
            {
                where.Add("PeriodStart >= @PeriodFrom");
                parameters.Add("@PeriodFrom", filter.PeriodFrom.Value);
            }

            if (filter.PeriodTo.HasValue)
            {
                where.Add("PeriodEnd <= @PeriodTo");
                parameters.Add("@PeriodTo", filter.PeriodTo.Value);
            }

            if (filter.ShiftId.HasValue)
            {
                where.Add("ShiftId = @ShiftId");
                parameters.Add("@ShiftId", filter.ShiftId.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM ProductionReports {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<ProductionReport> AddAsync(ProductionReport report)
        {
            const string sql = @"
                INSERT INTO ProductionReports (ReportNumber, ReportType, PeriodStart, PeriodEnd, ShiftId, ShiftName,
                    Status, GeneratedBy, GeneratedAt, SummaryDataJson, DetailDataJson)
                VALUES (@ReportNumber, @ReportType, @PeriodStart, @PeriodEnd, @ShiftId, @ShiftName,
                    @Status, @GeneratedBy, @GeneratedAt, @SummaryDataJson, @DetailDataJson);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                report.ReportNumber,
                report.ReportType,
                report.PeriodStart,
                report.PeriodEnd,
                report.ShiftId,
                report.ShiftName,
                report.Status,
                report.GeneratedBy,
                report.GeneratedAt,
                report.SummaryDataJson,
                report.DetailDataJson
            });

            report.Id = id;
            return report;
        }

        public async Task<ProductionReport> UpdateAsync(ProductionReport report)
        {
            const string sql = @"
                UPDATE ProductionReports 
                SET ReportType = @ReportType, PeriodStart = @PeriodStart, PeriodEnd = @PeriodEnd,
                    ShiftId = @ShiftId, ShiftName = @ShiftName, Status = @Status,
                    ApprovedAt = @ApprovedAt, ApprovedBy = @ApprovedBy,
                    SummaryDataJson = @SummaryDataJson, DetailDataJson = @DetailDataJson
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                report.Id,
                report.ReportType,
                report.PeriodStart,
                report.PeriodEnd,
                report.ShiftId,
                report.ShiftName,
                report.Status,
                report.ApprovedAt,
                report.ApprovedBy,
                report.SummaryDataJson,
                report.DetailDataJson
            });

            return report;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM ProductionReports WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }

        public async Task<string> GenerateReportNumberAsync(ReportType type)
        {
            var prefix = type switch
            {
                ReportType.Shift => "SHIFT",
                ReportType.Daily => "DAILY",
                ReportType.Monthly => "MONTHLY",
                ReportType.Yearly => "YEARLY",
                _ => "RPT"
            };

            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var sql = "SELECT COUNT(*) FROM ProductionReports WHERE ReportNumber LIKE @Prefix";
            var count = await _db.ExecuteScalarAsync<int>(sql, new { Prefix = $"{prefix}{datePart}%" });
            return $"{prefix}{datePart}{(count + 1):D4}";
        }

        public async Task<ProductionReportSummary> GetSummaryStatisticsAsync(DateTime start, DateTime end, ReportType type)
        {
            const string sql = @"
                SELECT 
                    COUNT(*) as TotalBatches,
                    SUM(CASE WHEN JSON_VALUE(DetailDataJson, '$.Batches[*].Status') = 'Completed' THEN 1 ELSE 0 END) as CompletedBatches,
                    SUM(JSON_VALUE(DetailDataJson, '$.Batches[*].ActualWeight')) as TotalWeight
                FROM ProductionReports
                WHERE ReportType = @Type AND PeriodStart >= @Start AND PeriodEnd <= @End";

            // 简化版本，实际需要更复杂的 JSON 查询
            return await _db.QuerySingleAsync<ProductionReportSummary>(sql, new
            {
                Type = (int)type,
                Start = start,
                End = end
            }) ?? new ProductionReportSummary();
        }
    }

    // ===== 班次仓储实现 =====
    public class ShiftRepository : IShiftRepository
    {
        private readonly IDatabaseProvider _db;

        public ShiftRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ShiftDefinition>> GetAllAsync()
        {
            const string sql = "SELECT * FROM ShiftDefinitions ORDER BY StartTime";
            return await _db.QueryAsync<ShiftDefinition>(sql);
        }

        public async Task<ShiftDefinition?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM ShiftDefinitions WHERE Id = @Id";
            return await _db.QuerySingleAsync<ShiftDefinition>(sql, new { Id = id });
        }

        public async Task<ShiftDefinition> AddAsync(ShiftDefinition shift)
        {
            const string sql = @"
                INSERT INTO ShiftDefinitions (Name, StartTime, EndTime, Color, CreatedAt)
                VALUES (@Name, @StartTime, @EndTime, @Color, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                shift.Name,
                shift.StartTime,
                shift.EndTime,
                shift.Color,
                shift.CreatedAt
            });

            shift.Id = id;
            return shift;
        }

        public async Task<ShiftDefinition> UpdateAsync(ShiftDefinition shift)
        {
            const string sql = @"
                UPDATE ShiftDefinitions 
                SET Name = @Name, StartTime = @StartTime, EndTime = @EndTime, Color = @Color
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                shift.Id,
                shift.Name,
                shift.StartTime,
                shift.EndTime,
                shift.Color
            });

            return shift;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM ShiftDefinitions WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }
    }
}