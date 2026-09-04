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
    // ===== OEE 仓储实现 =====
    public class OeeRepository : IOeeRepository
    {
        private readonly IDatabaseProvider _db;

        public OeeRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<OeeResult?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM OeeResults WHERE Id = @Id";
            return await _db.QuerySingleAsync<OeeResult>(sql, new { Id = id });
        }

        public async Task<IEnumerable<OeeResult>> GetByEquipmentAsync(string equipmentCode, DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT * FROM OeeResults 
                WHERE EquipmentCode = @EquipmentCode AND PeriodStart >= @Start AND PeriodEnd <= @End
                ORDER BY PeriodStart";
            return await _db.QueryAsync<OeeResult>(sql, new { EquipmentCode = equipmentCode, Start = start, End = end });
        }

        public async Task<(IEnumerable<OeeResult> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, OeeFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (filter.EquipmentCodes.Any())
            {
                var codes = string.Join(",", filter.EquipmentCodes.Select((_, i) => $"@EqCode{i}"));
                for (int i = 0; i < filter.EquipmentCodes.Count; i++)
                    parameters.Add($"@EqCode{i}", filter.EquipmentCodes[i]);
                where.Add($"EquipmentCode IN ({codes})");
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

            if (filter.Period.HasValue)
            {
                // 可以根据 Period 类型添加额外过滤
            }

            if (filter.MinOee.HasValue)
            {
                where.Add("Oee >= @MinOee");
                parameters.Add("@MinOee", filter.MinOee.Value);
            }

            if (filter.MaxOee.HasValue)
            {
                where.Add("Oee <= @MaxOee");
                parameters.Add("@MaxOee", filter.MaxOee.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var orderBy = !string.IsNullOrWhiteSpace(filter.SortBy) ? filter.SortBy : "PeriodStart";
            var orderDir = filter.SortDescending ? "DESC" : "ASC";

            var sql = $@"
                SELECT * FROM OeeResults
                {whereClause}
                ORDER BY {orderBy} {orderDir}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (filter.PageIndex - 1) * filter.PageSize);
            parameters.Add("@PageSize", filter.PageSize);

            var items = await _db.QueryAsync<OeeResult>(sql, parameters);

            var countSql = $"SELECT COUNT(*) FROM OeeResults {whereClause}";
            var total = await _db.ExecuteScalarAsync<int>(countSql, parameters);

            return (items, total);
        }

        public async Task<int> GetCountAsync(OeeFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (filter.EquipmentCodes.Any())
            {
                var codes = string.Join(",", filter.EquipmentCodes.Select((_, i) => $"@EqCode{i}"));
                for (int i = 0; i < filter.EquipmentCodes.Count; i++)
                    parameters.Add($"@EqCode{i}", filter.EquipmentCodes[i]);
                where.Add($"EquipmentCode IN ({codes})");
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

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM OeeResults {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<OeeResult> AddAsync(OeeResult oee)
        {
            const string sql = @"
                INSERT INTO OeeResults (EquipmentCode, EquipmentName, PeriodStart, PeriodEnd,
                    Availability, Performance, Quality, Oee, PlannedProductionTime, ActualRunTime,
                    Downtime, IdealRunRate, ActualOutput, GoodOutput, DefectOutput,
                    DowntimeRecordsJson, LossCategoriesJson, CalculatedAt)
                VALUES (@EquipmentCode, @EquipmentName, @PeriodStart, @PeriodEnd,
                    @Availability, @Performance, @Quality, @Oee, @PlannedProductionTime, @ActualRunTime,
                    @Downtime, @IdealRunRate, @ActualOutput, @GoodOutput, @DefectOutput,
                    @DowntimeRecordsJson, @LossCategoriesJson, @CalculatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                oee.EquipmentCode,
                oee.EquipmentName,
                oee.PeriodStart,
                oee.PeriodEnd,
                oee.Availability,
                oee.Performance,
                oee.Quality,
                oee.Oee,
                oee.PlannedProductionTime,
                oee.ActualRunTime,
                oee.Downtime,
                oee.IdealRunRate,
                oee.ActualOutput,
                oee.GoodOutput,
                oee.DefectOutput,
                DowntimeRecordsJson = Newtonsoft.Json.JsonConvert.SerializeObject(oee.DowntimeRecords),
                LossCategoriesJson = Newtonsoft.Json.JsonConvert.SerializeObject(oee.LossCategories),
                oee.CalculatedAt
            });

            oee.Id = id;
            return oee;
        }

        public async Task<OeeResult> UpdateAsync(OeeResult oee)
        {
            const string sql = @"
                UPDATE OeeResults 
                SET Availability = @Availability, Performance = @Performance, Quality = @Quality,
                    Oee = @Oee, PlannedProductionTime = @PlannedProductionTime, ActualRunTime = @ActualRunTime,
                    Downtime = @Downtime, IdealRunRate = @IdealRunRate, ActualOutput = @ActualOutput,
                    GoodOutput = @GoodOutput, DefectOutput = @DefectOutput,
                    DowntimeRecordsJson = @DowntimeRecordsJson, LossCategoriesJson = @LossCategoriesJson
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                oee.Id,
                oee.Availability,
                oee.Performance,
                oee.Quality,
                oee.Oee,
                oee.PlannedProductionTime,
                oee.ActualRunTime,
                oee.Downtime,
                oee.IdealRunRate,
                oee.ActualOutput,
                oee.GoodOutput,
                oee.DefectOutput,
                DowntimeRecordsJson = Newtonsoft.Json.JsonConvert.SerializeObject(oee.DowntimeRecords),
                LossCategoriesJson = Newtonsoft.Json.JsonConvert.SerializeObject(oee.LossCategories)
            });

            return oee;
        }

        public async Task<IEnumerable<OeeResult>> GetOeeTrendAsync(string equipmentCode, DateTime start, DateTime end, OeePeriod period)
        {
            // 根据周期类型确定聚合方式
            var sql = @"
                SELECT * FROM OeeResults
                WHERE EquipmentCode = @EquipmentCode AND PeriodStart >= @Start AND PeriodEnd <= @End
                ORDER BY PeriodStart";

            return await _db.QueryAsync<OeeResult>(sql, new { EquipmentCode = equipmentCode, Start = start, End = end });
        }

        public async Task<Dictionary<string, decimal>> GetOeeSummaryAsync(DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT EquipmentCode, AVG(Oee) as AvgOee, AVG(Availability) as AvgAvail, 
                       AVG(Performance) as AvgPerf, AVG(Quality) as AvgQual
                FROM OeeResults
                WHERE PeriodStart >= @Start AND PeriodEnd <= @End
                GROUP BY EquipmentCode";

            var results = await _db.QueryAsync<dynamic>(sql, new { Start = start, End = end });
            return results.ToDictionary(
                r => (string)r.EquipmentCode,
                r => (decimal)r.AvgOee
            );
        }

        public async Task<List<DowntimeRecord>> GetDowntimeRecordsAsync(string equipmentCode, DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT * FROM DowntimeRecords
                WHERE EquipmentCode = @EquipmentCode AND StartTime >= @Start AND StartTime <= @End
                ORDER BY StartTime";
            return (await _db.QueryAsync<DowntimeRecord>(sql, new { EquipmentCode = equipmentCode, Start = start, End = end })).ToList();
        }
    }

    // ===== 停机记录仓储实现 =====
    public class DowntimeRepository : IDowntimeRepository
    {
        private readonly IDatabaseProvider _db;

        public DowntimeRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<DowntimeRecord?> GetByIdAsync(long id)
        {
            const string sql = "SELECT * FROM DowntimeRecords WHERE Id = @Id";
            return await _db.QuerySingleAsync<DowntimeRecord>(sql, new { Id = id });
        }

        public async Task<IEnumerable<DowntimeRecord>> GetByEquipmentAsync(string equipmentCode, DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT * FROM DowntimeRecords
                WHERE EquipmentCode = @EquipmentCode AND StartTime >= @Start AND StartTime <= @End
                ORDER BY StartTime";
            return await _db.QueryAsync<DowntimeRecord>(sql, new { EquipmentCode = equipmentCode, Start = start, End = end });
        }

        public async Task<(IEnumerable<DowntimeRecord> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, DowntimeFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.EquipmentCode))
            {
                where.Add("EquipmentCode = @EquipmentCode");
                parameters.Add("@EquipmentCode", filter.EquipmentCode);
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                where.Add("Category = @Category");
                parameters.Add("@Category", filter.Category);
            }

            if (filter.From.HasValue)
            {
                where.Add("StartTime >= @From");
                parameters.Add("@From", filter.From.Value);
            }

            if (filter.To.HasValue)
            {
                where.Add("StartTime <= @To");
                parameters.Add("@To", filter.To.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $@"
                SELECT * FROM DowntimeRecords
                {whereClause}
                ORDER BY StartTime DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (filter.PageIndex - 1) * filter.PageSize);
            parameters.Add("@PageSize", filter.PageSize);

            var items = await _db.QueryAsync<DowntimeRecord>(sql, parameters);

            var countSql = $"SELECT COUNT(*) FROM DowntimeRecords {whereClause}";
            var total = await _db.ExecuteScalarAsync<int>(countSql, parameters);

            return (items, total);
        }

        public async Task<int> GetCountAsync(DowntimeFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.EquipmentCode))
            {
                where.Add("EquipmentCode = @EquipmentCode");
                parameters.Add("@EquipmentCode", filter.EquipmentCode);
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                where.Add("Category = @Category");
                parameters.Add("@Category", filter.Category);
            }

            if (filter.From.HasValue)
            {
                where.Add("StartTime >= @From");
                parameters.Add("@From", filter.From.Value);
            }

            if (filter.To.HasValue)
            {
                where.Add("StartTime <= @To");
                parameters.Add("@To", filter.To.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM DowntimeRecords {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<DowntimeRecord> AddAsync(DowntimeRecord record)
        {
            const string sql = @"
                INSERT INTO DowntimeRecords (EquipmentCode, EquipmentName, StartTime, EndTime, Reason, Category, DurationMinutes, CreatedAt)
                VALUES (@EquipmentCode, @EquipmentName, @StartTime, @EndTime, @Reason, @Category, @DurationMinutes, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() as bigint);";

            var id = await _db.ExecuteScalarAsync<long>(sql, new
            {
                record.EquipmentCode,
                record.EquipmentName,
                record.StartTime,
                record.EndTime,
                record.Reason,
                record.Category,
                record.DurationMinutes,
                record.CreatedAt
            });

            record.Id = id;
            return record;
        }

        public async Task<DowntimeRecord> UpdateAsync(DowntimeRecord record)
        {
            const string sql = @"
                UPDATE DowntimeRecords 
                SET EquipmentCode = @EquipmentCode, EquipmentName = @EquipmentName, 
                    StartTime = @StartTime, EndTime = @EndTime, Reason = @Reason, 
                    Category = @Category, DurationMinutes = @DurationMinutes
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                record.Id,
                record.EquipmentCode,
                record.EquipmentName,
                record.StartTime,
                record.EndTime,
                record.Reason,
                record.Category,
                record.DurationMinutes
            });

            return record;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            const string sql = "DELETE FROM DowntimeRecords WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }

        public async Task<Dictionary<string, decimal>> GetDowntimeByCategoryAsync(string equipmentCode, DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT Category, SUM(ISNULL(DurationMinutes, 0)) as TotalMinutes
                FROM DowntimeRecords
                WHERE EquipmentCode = @EquipmentCode AND StartTime >= @Start AND StartTime <= @End
                GROUP BY Category";

            var results = await _db.QueryAsync<dynamic>(sql, new { EquipmentCode = equipmentCode, Start = start, End = end });
            return results.ToDictionary(r => (string)r.Category, r => (decimal)r.TotalMinutes);
        }
    }

    // ===== 能耗仓储实现 =====
    public class EnergyRepository : IEnergyRepository
    {
        private readonly IDatabaseProvider _db;
        private readonly IEnergyStatisticsCacheRepository _cacheRepository;

        public EnergyRepository(IDatabaseProvider db, IEnergyStatisticsCacheRepository cacheRepository)
        {
            _db = db;
            _cacheRepository = cacheRepository;
        }

        public async Task<EnergyRecord?> GetByIdAsync(long id)
        {
            const string sql = "SELECT * FROM EnergyRecords WHERE Id = @Id";
            return await _db.QuerySingleAsync<EnergyRecord>(sql, new { Id = id });
        }

        public async Task<IEnumerable<EnergyRecord>> GetByMeterAsync(string meterCode, DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT * FROM EnergyRecords
                WHERE MeterCode = @MeterCode AND Timestamp >= @Start AND Timestamp <= @End
                ORDER BY Timestamp";
            return await _db.QueryAsync<EnergyRecord>(sql, new { MeterCode = meterCode, Start = start, End = end });
        }

        public async Task<IEnumerable<EnergyRecord>> GetByEnergyTypeAsync(EnergyType energyType, DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT * FROM EnergyRecords
                WHERE EnergyType = @EnergyType AND Timestamp >= @Start AND Timestamp <= @End
                ORDER BY Timestamp";
            return await _db.QueryAsync<EnergyRecord>(sql, new { EnergyType = (int)energyType, Start = start, End = end });
        }

        public async Task<(IEnumerable<EnergyRecord> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, EnergyFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (filter.MeterCodes.Any())
            {
                var codes = string.Join(",", filter.MeterCodes.Select((_, i) => $"@Meter{i}"));
                for (int i = 0; i < filter.MeterCodes.Count; i++)
                    parameters.Add($"@Meter{i}", filter.MeterCodes[i]);
                where.Add($"MeterCode IN ({codes})");
            }

            if (filter.EnergyType.HasValue)
            {
                where.Add("EnergyType = @EnergyType");
                parameters.Add("@EnergyType", (int)filter.EnergyType.Value);
            }

            if (filter.StartTime.HasValue)
            {
                where.Add("Timestamp >= @StartTime");
                parameters.Add("@StartTime", filter.StartTime.Value);
            }

            if (filter.EndTime.HasValue)
            {
                where.Add("Timestamp <= @EndTime");
                parameters.Add("@EndTime", filter.EndTime.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $@"
                SELECT * FROM EnergyRecords
                {whereClause}
                ORDER BY Timestamp DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (filter.PageIndex - 1) * filter.PageSize);
            parameters.Add("@PageSize", filter.PageSize);

            var items = await _db.QueryAsync<EnergyRecord>(sql, parameters);

            var countSql = $"SELECT COUNT(*) FROM EnergyRecords {whereClause}";
            var total = await _db.ExecuteScalarAsync<int>(countSql, parameters);

            return (items, total);
        }

        public async Task<int> GetCountAsync(EnergyFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (filter.MeterCodes.Any())
            {
                var codes = string.Join(",", filter.MeterCodes.Select((_, i) => $"@Meter{i}"));
                for (int i = 0; i < filter.MeterCodes.Count; i++)
                    parameters.Add($"@Meter{i}", filter.MeterCodes[i]);
                where.Add($"MeterCode IN ({codes})");
            }

            if (filter.EnergyType.HasValue)
            {
                where.Add("EnergyType = @EnergyType");
                parameters.Add("@EnergyType", (int)filter.EnergyType.Value);
            }

            if (filter.StartTime.HasValue)
            {
                where.Add("Timestamp >= @StartTime");
                parameters.Add("@StartTime", filter.StartTime.Value);
            }

            if (filter.EndTime.HasValue)
            {
                where.Add("Timestamp <= @EndTime");
                parameters.Add("@EndTime", filter.EndTime.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM EnergyRecords {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<EnergyRecord> AddAsync(EnergyRecord record)
        {
            const string sql = @"
                INSERT INTO EnergyRecords (MeterCode, MeterName, EnergyType, Reading, Consumption, Cost, Timestamp, RecordedAt)
                VALUES (@MeterCode, @MeterName, @EnergyType, @Reading, @Consumption, @Cost, @Timestamp, @RecordedAt);
                SELECT CAST(SCOPE_IDENTITY() as bigint);";

            var id = await _db.ExecuteScalarAsync<long>(sql, new
            {
                record.MeterCode,
                record.MeterName,
                record.EnergyType,
                record.Reading,
                record.Consumption,
                record.Cost,
                record.Timestamp,
                record.RecordedAt
            });

            record.Id = id;
            return record;
        }

        public async Task<bool> BulkInsertAsync(IEnumerable<EnergyRecord> records)
        {
            var recordList = records.ToList();
            if (!recordList.Any()) return true;

            const string sql = @"
                INSERT INTO EnergyRecords (MeterCode, MeterName, EnergyType, Reading, Consumption, Cost, Timestamp, RecordedAt)
                VALUES (@MeterCode, @MeterName, @EnergyType, @Reading, @Consumption, @Cost, @Timestamp, @RecordedAt)";

            const int batchSize = 1000;
            for (int i = 0; i < recordList.Count; i += batchSize)
            {
                var batch = recordList.Skip(i).Take(batchSize).ToList();
                await _db.ExecuteAsync(sql, batch);
            }

            return true;
        }

        public async Task<EnergyStatistics> GetStatisticsAsync(EnergyQueryParams param)
        {
            // 先尝试从缓存获取
            var cache = await _cacheRepository.GetAsync(param.EnergyType ?? EnergyType.Electricity, 
                param.MeterCodes.FirstOrDefault(), param.StartTime, param.EndTime, param.Period);
            
            if (cache != null)
            {
                return new EnergyStatistics
                {
                    EnergyType = (EnergyType)cache.EnergyType,
                    PeriodStart = cache.PeriodStart,
                    PeriodEnd = cache.PeriodEnd,
                    TotalConsumption = cache.TotalConsumption,
                    TotalCost = cache.TotalCost,
                    AvgConsumptionPerHour = cache.AvgConsumptionPerHour,
                    PeakConsumption = cache.PeakConsumption,
                    PeakTime = cache.PeakTime ?? DateTime.MinValue,
                    MinConsumption = cache.MinConsumption,
                    MinTime = cache.MinTime ?? DateTime.MinValue,
                    ByEquipment = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, decimal>>(cache.ByEquipmentJson ?? "{}"),
                    ByArea = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, decimal>>(cache.ByAreaJson ?? "{}"),
                    HourlyData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<DateTime, decimal>>(cache.HourlyDataJson ?? "{}"),
                    DailyData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<DateTime, decimal>>(cache.DailyDataJson ?? "{}"),
                    ConsumptionPerTon = cache.ConsumptionPerTon,
                    ConsumptionPerBatch = cache.ConsumptionPerBatch,
                    YoYChangePercent = cache.YoYChangePercent,
                    MoMChangePercent = cache.MoMChangePercent
                };
            }

            // 实时计算（简化版）
            var where = new List<string> { "Timestamp >= @Start AND Timestamp <= @End" };
            var parameters = new DynamicParameters();
            parameters.Add("@Start", param.StartTime);
            parameters.Add("@End", param.EndTime);

            if (param.MeterCodes.Any())
            {
                var codes = string.Join(",", param.MeterCodes.Select((_, i) => $"@Meter{i}"));
                for (int i = 0; i < param.MeterCodes.Count; i++)
                    parameters.Add($"@Meter{i}", param.MeterCodes[i]);
            }

            if (param.EnergyType.HasValue)
            {
                parameters.Add("@EnergyType", (int)param.EnergyType.Value);
            }

            var meterClause = param.MeterCodes.Any() ? "AND MeterCode IN (" + string.Join(",", param.MeterCodes.Select((_, i) => $"@Meter{i}")) + ")" : "";
            for (int i = 0; i < param.MeterCodes.Count; i++)
                parameters.Add($"@Meter{i}", param.MeterCodes[i]);

            if (param.EnergyType.HasValue)
            {
                parameters.Add("@EnergyType", (int)param.EnergyType.Value);
            }

            var statsSql = $@"
                SELECT 
                    SUM(Consumption) as TotalConsumption,
                    SUM(Cost) as TotalCost,
                    AVG(Consumption) as AvgConsumption,
                    MAX(Consumption) as PeakConsumption,
                    MIN(Consumption) as MinConsumption
                FROM EnergyRecords
                WHERE Timestamp >= @Start AND Timestamp <= @End {(param.EnergyType.HasValue ? "AND EnergyType = @EnergyType" : "")}";

            var stats = await _db.QuerySingleAsync<dynamic>(statsSql, parameters);

            return new EnergyStatistics
            {
                EnergyType = param.EnergyType ?? EnergyType.Electricity,
                PeriodStart = param.StartTime,
                PeriodEnd = param.EndTime,
                TotalConsumption = (decimal)(stats?.TotalConsumption ?? 0),
                TotalCost = (decimal)(stats?.TotalCost ?? 0),
                AvgConsumptionPerHour = (decimal)(stats?.AvgConsumption ?? 0),
                PeakConsumption = (decimal)(stats?.PeakConsumption ?? 0),
                MinConsumption = (decimal)(stats?.MinConsumption ?? 0)
            };
        }

        public async Task<Dictionary<string, decimal>> GetConsumptionByEquipmentAsync(EnergyType energyType, DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT MeterName, SUM(Consumption) as TotalConsumption
                FROM EnergyRecords
                WHERE EnergyType = @EnergyType AND Timestamp >= @Start AND Timestamp <= @End
                GROUP BY MeterName
                ORDER BY TotalConsumption DESC";

            var results = await _db.QueryAsync<dynamic>(sql, new { EnergyType = (int)energyType, Start = start, End = end });
            return results.ToDictionary(r => (string)r.MeterName, r => (decimal)r.TotalConsumption);
        }

        public async Task<Dictionary<DateTime, decimal>> GetHourlyDataAsync(string meterCode, DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0) as HourStart, 
                       SUM(Consumption) as HourlyConsumption
                FROM EnergyRecords
                WHERE MeterCode = @MeterCode AND Timestamp >= @Start AND Timestamp <= @End
                GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0)
                ORDER BY HourStart";

            var results = await _db.QueryAsync<dynamic>(sql, new { MeterCode = meterCode, Start = start, End = end });
            return results.ToDictionary(r => (DateTime)r.HourStart, r => (decimal)r.HourlyConsumption);
        }

        public async Task<Dictionary<DateTime, decimal>> GetDailyDataAsync(string meterCode, DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT CAST(Timestamp as DATE) as DayStart, 
                       SUM(Consumption) as DailyConsumption
                FROM EnergyRecords
                WHERE MeterCode = @MeterCode AND Timestamp >= @Start AND Timestamp <= @End
                GROUP BY CAST(Timestamp as DATE)
                ORDER BY DayStart";

            var results = await _db.QueryAsync<dynamic>(sql, new { MeterCode = meterCode, Start = start, End = end });
            return results.ToDictionary(r => (DateTime)r.DayStart, r => (decimal)r.DailyConsumption);
        }

        public async Task<EnergyStatisticsCache?> GetCachedStatisticsAsync(EnergyType energyType, string? meterCode, DateTime start, DateTime end, StatisticsPeriod period)
        {
            return await _cacheRepository.GetAsync(energyType, meterCode, start, end, period);
        }

        public async Task<EnergyStatisticsCache> AddOrUpdateCacheAsync(EnergyStatisticsCache cache)
        {
            return await _cacheRepository.AddOrUpdateAsync(cache);
        }
    }

    // ===== 能耗统计缓存仓储实现 =====
    public class EnergyStatisticsCacheRepository : IEnergyStatisticsCacheRepository
    {
        private readonly IDatabaseProvider _db;

        public EnergyStatisticsCacheRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<EnergyStatisticsCache?> GetAsync(EnergyType energyType, string? meterCode, DateTime start, DateTime end, StatisticsPeriod period)
        {
            const string sql = @"
                SELECT * FROM EnergyStatisticsCache
                WHERE EnergyType = @EnergyType 
                AND (@MeterCode IS NULL OR MeterCode = @MeterCode)
                AND PeriodStart = @Start AND PeriodEnd = @End AND PeriodType = @PeriodType";

            return await _db.QuerySingleAsync<EnergyStatisticsCache>(sql, new
            {
                EnergyType = (int)energyType,
                MeterCode = meterCode ?? (object)DBNull.Value,
                Start = start,
                End = end,
                PeriodType = (int)period
            });
        }

        public async Task<EnergyStatisticsCache> AddOrUpdateAsync(EnergyStatisticsCache cache)
        {
            const string upsertSql = @"
                MERGE EnergyStatisticsCache AS target
                USING (SELECT @EnergyType as EnergyType, @MeterCode as MeterCode, @PeriodStart as PeriodStart, 
                      @PeriodEnd as PeriodEnd, @PeriodType as PeriodType) AS source
                ON target.EnergyType = source.EnergyType 
                AND target.MeterCode = source.MeterCode
                AND target.PeriodStart = source.PeriodStart
                AND target.PeriodEnd = source.PeriodEnd
                AND target.PeriodType = source.PeriodType
                WHEN MATCHED THEN
                    UPDATE SET TotalConsumption = @TotalConsumption, TotalCost = @TotalCost,
                        AvgConsumptionPerHour = @AvgConsumptionPerHour, PeakConsumption = @PeakConsumption,
                        PeakTime = @PeakTime, MinConsumption = @MinConsumption, MinTime = @MinTime,
                        ByEquipmentJson = @ByEquipmentJson, ByAreaJson = @ByAreaJson,
                        HourlyDataJson = @HourlyDataJson, DailyDataJson = @DailyDataJson,
                        ConsumptionPerTon = @ConsumptionPerTon, ConsumptionPerBatch = @ConsumptionPerBatch,
                        YoYChangePercent = @YoYChangePercent, MoMChangePercent = @MoMChangePercent,
                        CalculatedAt = @CalculatedAt
                WHEN NOT MATCHED THEN
                    INSERT (EnergyType, MeterCode, PeriodStart, PeriodEnd, PeriodType,
                        TotalConsumption, TotalCost, AvgConsumptionPerHour, PeakConsumption,
                        PeakTime, MinConsumption, MinTime, ByEquipmentJson, ByAreaJson,
                        HourlyDataJson, DailyDataJson, ConsumptionPerTon, ConsumptionPerBatch,
                        YoYChangePercent, MoMChangePercent, CalculatedAt)
                    VALUES (@EnergyType, @MeterCode, @PeriodStart, @PeriodEnd, @PeriodType,
                        @TotalConsumption, @TotalCost, @AvgConsumptionPerHour, @PeakConsumption,
                        @PeakTime, @MinConsumption, @MinTime, @ByEquipmentJson, @ByAreaJson,
                        @HourlyDataJson, @DailyDataJson, @ConsumptionPerTon, @ConsumptionPerBatch,
                        @YoYChangePercent, @MoMChangePercent, @CalculatedAt);";

            await _db.ExecuteAsync(upsertSql, new
            {
                EnergyType = cache.EnergyType,
                MeterCode = cache.MeterCode ?? (object)DBNull.Value,
                PeriodStart = cache.PeriodStart,
                PeriodEnd = cache.PeriodEnd,
                PeriodType = cache.PeriodType,
                cache.TotalConsumption,
                cache.TotalCost,
                cache.AvgConsumptionPerHour,
                cache.PeakConsumption,
                cache.PeakTime,
                cache.MinConsumption,
                cache.MinTime,
                ByEquipmentJson = Newtonsoft.Json.JsonConvert.SerializeObject(cache.ByEquipment),
                ByAreaJson = Newtonsoft.Json.JsonConvert.SerializeObject(cache.ByArea),
                HourlyDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(cache.HourlyData),
                DailyDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(cache.DailyData),
                cache.ConsumptionPerTon,
                cache.ConsumptionPerBatch,
                cache.YoYChangePercent,
                cache.MoMChangePercent,
                cache.CalculatedAt
            });

            return cache;
        }

        public async Task<IEnumerable<EnergyStatisticsCache>> GetByEnergyTypeAsync(EnergyType energyType, DateTime start, DateTime end, StatisticsPeriod period)
        {
            const string sql = @"
                SELECT * FROM EnergyStatisticsCache
                WHERE EnergyType = @EnergyType AND PeriodStart >= @Start AND PeriodEnd <= @End AND PeriodType = @PeriodType
                ORDER BY PeriodStart";

            return await _db.QueryAsync<EnergyStatisticsCache>(sql, new
            {
                EnergyType = (int)energyType,
                Start = start,
                End = end,
                PeriodType = (int)period
            });
        }

        public async Task<bool> DeleteExpiredAsync(DateTime beforeDate)
        {
            const string sql = "DELETE FROM EnergyStatisticsCache WHERE PeriodEnd < @BeforeDate";
            var affected = await _db.ExecuteAsync(sql, new { BeforeDate = beforeDate });
            return affected > 0;
        }
    }
}