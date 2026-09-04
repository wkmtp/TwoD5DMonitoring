using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.Services
{
    // ===== 报表服务基类 =====
    public abstract class ReportServiceBase
    {
        protected readonly ILogger _logger;

        protected ReportServiceBase()
        {
            _logger = LogManager.CreateModuleLogger(GetType().Name);
        }

        protected void LogOperation(string operation, string message, params object[] args)
        {
            _logger.Information($"[{GetType().Name}] {operation}: {message}", args);
        }
    }

    // ===== 历史趋势服务 =====
    public class HistoricalTrendService : ReportServiceBase
    {
        private readonly IHistoricalTrendRepository _repository;

        public HistoricalTrendService(IHistoricalTrendRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<TrendSeries>> GetTrendAsync(TrendQueryParams param)
        {
            try
            {
                return await _repository.GetAggregatedTrendAsync(param);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取趋势数据失败");
                return Enumerable.Empty<TrendSeries>();
            }
        }

        public async Task<Dictionary<string, TrendSeries>> GetMultipleTrendsAsync(List<TrendQueryParams> paramsList)
        {
            var tasks = paramsList.Select(p => GetTrendAsync(p));
            var results = await Task.WhenAll(tasks);
            
            var dict = new Dictionary<string, TrendSeries>();
            foreach (var seriesList in results)
            {
                foreach (var series in seriesList)
                {
                    dict[series.TagName] = series;
                }
            }
            return dict;
        }

        public async Task<bool> ArchiveRealTimeDataAsync(string tagName, double value, int quality = 192)
        {
            try
            {
                var point = new HistoricalTrendPoint
                {
                    TagName = tagName,
                    Timestamp = DateTime.Now,
                    Value = value,
                    Quality = quality
                };

                await _repository.BulkInsertAsync(new[] { point });
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "归档实时数据失败: {TagName}", tagName);
                return false;
            }
        }

        public async Task<bool> BulkArchiveAsync(IEnumerable<HistoricalTrendPoint> points)
        {
            try
            {
                await _repository.BulkInsertAsync(points);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "批量归档数据失败");
                return false;
            }
        }

        public async Task CleanupOldDataAsync(int retentionDays = 365)
        {
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            await _repository.DeleteOldDataAsync(cutoffDate);
            _logger.Information("清理历史趋势数据，截止日期: {CutoffDate}", cutoffDate);
        }
    }

    // ===== 生产报表服务 =====
    public class ProductionReportService : ReportServiceBase
    {
        private readonly IProductionReportRepository _reportRepository;
        private readonly IShiftRepository _shiftRepository;
        private readonly IBatchRecordRepository _batchRepository;
        private readonly IAlarmHistoryRepository _alarmRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly IRecipeRepository _recipeRepository;
        private readonly IOeeRepository _oeeRepository;
        private readonly IEnergyRepository _energyRepository;

        public ProductionReportService(
            IProductionReportRepository reportRepository,
            IShiftRepository shiftRepository,
            IBatchRecordRepository batchRepository,
            IAlarmHistoryRepository alarmRepository,
            IMaterialRepository materialRepository,
            IRecipeRepository recipeRepository,
            IOeeRepository oeeRepository,
            IEnergyRepository energyRepository)
        {
            _reportRepository = reportRepository;
            _shiftRepository = shiftRepository;
            _batchRepository = batchRepository;
            _alarmRepository = alarmRepository;
            _materialRepository = materialRepository;
            _recipeRepository = recipeRepository;
            _oeeRepository = oeeRepository;
            _energyRepository = energyRepository;
        }

        public async Task<ProductionReport> GenerateShiftReportAsync(int shiftId, DateTime date, string operatorName)
        {
            var shift = await _shiftRepository.GetByIdAsync(shiftId);
            if (shift == null) throw new InvalidOperationException("班次不存在");

            var (periodStart, periodEnd) = CalculateShiftPeriod(shift, date);
            return await GenerateReportAsync(ReportType.Shift, periodStart, periodEnd, shiftId, shift.Name, operatorName);
        }

        public async Task<ProductionReport> GenerateDailyReportAsync(DateTime date, string operatorName)
        {
            var periodStart = date.Date;
            var periodEnd = date.Date.AddDays(1).AddSeconds(-1);
            return await GenerateReportAsync(ReportType.Daily, periodStart, periodEnd, 0, "", operatorName);
        }

        public async Task<ProductionReport> GenerateMonthlyReportAsync(int year, int month, string operatorName)
        {
            var periodStart = new DateTime(year, month, 1);
            var periodEnd = periodStart.AddMonths(1).AddSeconds(-1);
            return await GenerateReportAsync(ReportType.Monthly, periodStart, periodEnd, 0, "", operatorName);
        }

        private async Task<ProductionReport> GenerateReportAsync(ReportType type, DateTime start, DateTime end, int shiftId, string shiftName, string operatorName)
        {
            var reportNumber = await _reportRepository.GenerateReportNumberAsync(type);
            
            var report = new ProductionReport
            {
                ReportNumber = reportNumber,
                ReportType = type,
                PeriodStart = start,
                PeriodEnd = end,
                ShiftId = shiftId,
                ShiftName = shiftName,
                Status = ReportStatus.Generated,
                GeneratedBy = operatorName,
                GeneratedAt = DateTime.Now
            };

            // 收集数据
            report.Summary = await CollectSummaryDataAsync(start, end);
            report.Details = await CollectDetailDataAsync(start, end);

            // 计算汇总指标
            CalculateSummaryMetrics(report);

            var result = await _reportRepository.AddAsync(report);
            
            LogOperation("生成报表", "类型: {Type}, 编号: {Number}, 周期: {Start} - {End}", type, report.ReportNumber, start, end);
            
            return result;
        }

        private async Task<ProductionReportSummary> CollectSummaryDataAsync(DateTime start, DateTime end)
        {
            var filter = new BatchFilter { StartFrom = start, StartTo = end };
            var batches = await _batchRepository.GetPagedAsync(1, int.MaxValue, filter);
            var batchesList = batches.ToList();

            var completedBatches = batchesList.Where(b => b.Status == BatchStatus.Completed).ToList();
            var totalWeight = completedBatches.Sum(b => b.ActualWeight);
            var targetWeight = completedBatches.Sum(b => b.TargetWeight);

            // 获取能耗数据
            var energyStats = await _energyRepository.GetStatisticsAsync(new EnergyQueryParams
            {
                StartTime = start,
                EndTime = end,
                Period = StatisticsPeriod.Hour
            });

            // 获取报警统计
            var alarmStats = await _alarmRepository.GetStatisticsAsync(start, end);

            // 获取 OEE 数据
            var oeeSummary = await _oeeRepository.GetOeeSummaryAsync(start, end);

            return new ProductionReportSummary
            {
                TotalBatches = batches.Count(),
                CompletedBatches = completedBatches.Count(),
                FailedBatches = batches.Count(b => b.Status == BatchStatus.Aborted),
                TotalWeight = totalWeight,
                YieldRate = targetWeight > 0 ? (totalWeight / targetWeight * 100) : 0,
                
                ElectricityConsumption = energyStats.TotalConsumption,
                TotalCost = energyStats.TotalCost,
                
                MaterialConsumption = new Dictionary<string, decimal>(), // TODO: 从批次明细计算
                
                TotalRunTime = 0, // TODO: 从设备运行记录计算
                Availability = oeeSummary.Values.Any() ? oeeSummary.Values.Average() : 0
            };
        }

        private async Task<ProductionReportDetails> CollectDetailDataAsync(DateTime start, DateTime end)
        {
            var filter = new BatchFilter { StartFrom = start, StartTo = end };
            var batches = await _batchRepository.GetPagedAsync(1, int.MaxValue, filter);
            var batchesList = batches.ToList();

            var details = new ProductionReportDetails
            {
                Batches = batchesList.Select(b => new BatchSummary
                {
                    BatchNumber = b.BatchNumber,
                    RecipeCode = b.RecipeCode,
                    RecipeName = "", // TODO: 从配方获取
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    TargetWeight = b.TargetWeight,
                    ActualWeight = b.ActualWeight,
                    Status = b.Status.ToString(),
                    Operator = b.Operator,
                    Duration = b.EndTime.HasValue ? b.EndTime.Value - b.StartTime : null
                }).ToList()
            };

            return details;
        }

        private void CalculateSummaryMetrics(ProductionReport report)
        {
            // 计算合格率、一次通过率等
            if (report.Details.Batches.Any())
            {
                var completed = report.Details.Batches.Where(b => b.Status == "Completed").ToList();
                if (completed.Any())
                {
                    report.Summary.QualifiedWeight = completed.Sum(b => b.ActualWeight);
                    report.Summary.YieldRate = report.Summary.TotalWeight > 0 
                        ? (report.Summary.QualifiedWeight / report.Summary.TotalWeight * 100) : 0;
                }
            }
        }

        private (DateTime, DateTime) CalculateShiftPeriod(ShiftDefinition shift, DateTime date)
        {
            var start = date.Date + shift.StartTime;
            var end = shift.IsOvernight ? date.AddDays(1).Date + shift.EndTime : date.Date + shift.EndTime;
            return (start, end);
        }

        public async Task<ProductionReport?> GetReportAsync(int id)
        {
            return await _reportRepository.GetByIdAsync(id);
        }

        public async Task<(IEnumerable<ProductionReport> Items, int TotalCount)> GetReportsPagedAsync(int page, int pageSize, Models.ReportFilter filter)
        {
            return await _reportRepository.GetPagedAsync(page, pageSize, filter);
        }

        public async Task<bool> ApproveReportAsync(int id, string approver)
        {
            var report = await _reportRepository.GetByIdAsync(id);
            if (report == null) return false;

            report.Status = ReportStatus.Approved;
            report.ApprovedAt = DateTime.Now;
            report.ApprovedBy = approver;
            await _reportRepository.UpdateAsync(report);

            LogOperation("审核报表", "报表: {Number}, 审核人: {Approver}", report.ReportNumber, approver);
            return true;
        }

        public async Task<IEnumerable<ShiftDefinition>> GetShiftsAsync()
        {
            return await _shiftRepository.GetAllAsync();
        }

        public async Task DeleteReportAsync(int id)
        {
            await _reportRepository.DeleteAsync(id);
            LogOperation("删除报表", "ID: {Id}", id);
        }
    }

    // ===== OEE 分析服务 =====
    public class OeeAnalysisService : ReportServiceBase
    {
        private readonly IOeeRepository _oeeRepository;
        private readonly IDowntimeRepository _downtimeRepository;

        public OeeAnalysisService(IOeeRepository oeeRepository, IDowntimeRepository downtimeRepository)
        {
            _oeeRepository = oeeRepository;
            _downtimeRepository = downtimeRepository;
        }

        public async Task<OeeResult> CalculateOeeAsync(string equipmentCode, DateTime periodStart, DateTime periodEnd, OeeCalculationParams param)
        {
            // 获取设备运行数据
            var downtimeRecords = await _downtimeRepository.GetByEquipmentAsync(equipmentCode, periodStart, periodEnd);
            var downtimeList = downtimeRecords.ToList();

            // 计算各项指标
            var plannedTime = param.PlannedProductionTime; // 分钟
            var downtime = downtimeList.Sum(d => d.Duration?.TotalMinutes ?? 0);
            var actualRunTime = Math.Max(0, plannedTime - (decimal)downtime);
            var availability = plannedTime > 0 ? (actualRunTime / plannedTime * 100) : 0;

            // 性能率（需要理想产能和实际产出）
            var idealOutput = param.IdealRunRate * (actualRunTime / 60); // 理想产能 = 理想速率 * 运行小时数
            var actualOutput = param.ActualOutput; // 实际产出
            var performance = idealOutput > 0 ? (actualOutput / idealOutput * 100) : 0;

            // 质量率
            var goodOutput = param.GoodOutput;
            var quality = actualOutput > 0 ? (goodOutput / actualOutput * 100) : 0;

            var oee = (availability * performance * quality) / 10000;

            // 停机分类统计
            var downtimeByCategory = await _downtimeRepository.GetDowntimeByCategoryAsync(equipmentCode, periodStart, periodEnd);
            var lossCategories = downtimeByCategory.Select(kvp => new LossCategory
            {
                Category = kvp.Key,
                TimeLoss = kvp.Value,
                Percentage = plannedTime > 0 ? (kvp.Value / plannedTime * 100) : 0
            }).OrderByDescending(lc => lc.TimeLoss).ToList();

            var result = new OeeResult
            {
                EquipmentCode = param.EquipmentCode,
                EquipmentName = param.EquipmentName,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Availability = Math.Round(availability, 2),
                Performance = Math.Round(performance, 2),
                Quality = Math.Round(quality, 2),
                Oee = Math.Round(oee, 2),
                PlannedProductionTime = plannedTime,
                ActualRunTime = actualRunTime,
                Downtime = (decimal)downtime,
                IdealRunRate = param.IdealRunRate,
                ActualOutput = param.ActualOutput,
                GoodOutput = param.GoodOutput,
                DefectOutput = param.DefectOutput,
                DowntimeRecords = downtimeList.Select(d => new DowntimeRecord
                {
                    StartTime = d.StartTime,
                    EndTime = d.EndTime,
                    Reason = d.Reason,
                    Category = d.Category
                }).ToList(),
                LossCategories = lossCategories
            };

            // 保存到数据库
            await _oeeRepository.AddAsync(result);

            LogOperation("计算 OEE", "设备: {Code}, OEE: {OEE}%, 可用率: {A}%, 性能: {P}%, 质量: {Q}%", 
                equipmentCode, result.Oee, result.Availability, result.Performance, result.Quality);

            return result;
        }

        public async Task<OeeResult> GetOeeAsync(int id)
        {
            return await _oeeRepository.GetByIdAsync(id);
        }

        public async Task<(IEnumerable<OeeResult> Items, int TotalCount)> GetOeePagedAsync(int page, int pageSize, Models.OeeFilter filter)
        {
            return await _oeeRepository.GetPagedAsync(page, pageSize, filter);
        }

        public async Task<IEnumerable<OeeResult>> GetOeeTrendAsync(string equipmentCode, DateTime start, DateTime end, OeePeriod period)
        {
            return await _oeeRepository.GetOeeTrendAsync(equipmentCode, start, end, period);
        }

        public async Task<Dictionary<string, decimal>> GetOeeSummaryAsync(DateTime start, DateTime end)
        {
            return await _oeeRepository.GetOeeSummaryAsync(start, end);
        }

        public async Task<List<DowntimeRecord>> GetDowntimeRecordsAsync(string equipmentCode, DateTime start, DateTime end)
        {
            var result = await _downtimeRepository.GetByEquipmentAsync(equipmentCode, start, end);
            return result.ToList();
        }

        public async Task<Dictionary<string, decimal>> GetDowntimeByCategoryAsync(string equipmentCode, DateTime start, DateTime end)
        {
            return await _downtimeRepository.GetDowntimeByCategoryAsync(equipmentCode, start, end);
        }
    }

    // ===== OEE 计算参数 =====
    public class OeeCalculationParams
    {
        public string EquipmentCode { get; set; } = "";
        public string EquipmentName { get; set; } = "";
        public decimal PlannedProductionTime { get; set; } = 480; // 分钟
        public decimal IdealRunRate { get; set; } = 1000; // kg/h 或 件/h
        public decimal ActualOutput { get; set; }
        public decimal GoodOutput { get; set; }
        public decimal DefectOutput { get; set; }
    }

    // ===== 能耗统计服务 =====
    public class EnergyStatisticsService : ReportServiceBase
    {
        private readonly IEnergyRepository _energyRepository;
        private readonly IEnergyStatisticsCacheRepository _cacheRepository;

        public EnergyStatisticsService(IEnergyRepository energyRepository, IEnergyStatisticsCacheRepository cacheRepository)
        {
            _energyRepository = energyRepository;
            _cacheRepository = cacheRepository;
        }

        public async Task<EnergyStatistics> GetStatisticsAsync(EnergyQueryParams param)
        {
            return await _energyRepository.GetStatisticsAsync(param);
        }

        public async Task<EnergyStatistics> GetCachedOrCalculateAsync(EnergyQueryParams param)
        {
            // 优先从缓存获取
            if (param.EnergyType.HasValue)
            {
                var cache = await _cacheRepository.GetAsync(param.EnergyType.Value, 
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
            }

            return await _energyRepository.GetStatisticsAsync(param);
        }

        public async Task<EnergyStatistics> CalculateAndCacheAsync(EnergyQueryParams param)
        {
            var stats = await _energyRepository.GetStatisticsAsync(param);
            
            // 存入缓存
            if (param.EnergyType.HasValue)
            {
                var cache = new EnergyStatisticsCache
                {
                    EnergyType = (int)param.EnergyType.Value,
                    MeterCode = param.MeterCodes.FirstOrDefault(),
                    PeriodStart = param.StartTime,
                    PeriodEnd = param.EndTime,
                    PeriodType = (int)param.Period,
                    TotalConsumption = stats.TotalConsumption,
                    TotalCost = stats.TotalCost,
                    AvgConsumptionPerHour = stats.AvgConsumptionPerHour,
                    PeakConsumption = stats.PeakConsumption,
                    PeakTime = stats.PeakTime,
                    MinConsumption = stats.MinConsumption,
                    MinTime = stats.MinTime,
                    ByEquipmentJson = Newtonsoft.Json.JsonConvert.SerializeObject(stats.ByEquipment),
                    ByAreaJson = Newtonsoft.Json.JsonConvert.SerializeObject(stats.ByArea),
                    HourlyDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(stats.HourlyData),
                    DailyDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(stats.DailyData),
                    ConsumptionPerTon = stats.ConsumptionPerTon,
                    ConsumptionPerBatch = stats.ConsumptionPerBatch,
                    YoYChangePercent = stats.YoYChangePercent,
                    MoMChangePercent = stats.MoMChangePercent,
                    CalculatedAt = DateTime.Now
                };

                await _cacheRepository.AddOrUpdateAsync(cache);
            }

            return stats;
        }

        public async Task<IEnumerable<EnergyStatistics>> GetCachedStatisticsAsync(EnergyType energyType, string? meterCode, DateTime start, DateTime end, StatisticsPeriod period)
        {
            var caches = await _cacheRepository.GetByEnergyTypeAsync(energyType, start, end, period);
            var results = new List<EnergyStatistics>();
            
            foreach (var cache in caches.Where(c => string.IsNullOrEmpty(meterCode) || c.MeterCode == meterCode))
            {
                results.Add(new EnergyStatistics
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
                });
            }
            
            return results;
        }

        public async Task<Dictionary<string, decimal>> GetConsumptionByEquipmentAsync(EnergyType energyType, DateTime start, DateTime end)
        {
            return await _energyRepository.GetConsumptionByEquipmentAsync(energyType, start, end);
        }

        public async Task<Dictionary<DateTime, decimal>> GetHourlyDataAsync(string meterCode, DateTime start, DateTime end)
        {
            return await _energyRepository.GetHourlyDataAsync(meterCode, start, end);
        }

        public async Task<Dictionary<DateTime, decimal>> GetDailyDataAsync(string meterCode, DateTime start, DateTime end)
        {
            return await _energyRepository.GetDailyDataAsync(meterCode, start, end);
        }

        public async Task<EnergyStatistics> ComparePeriodsAsync(EnergyType energyType, DateTime currentStart, DateTime currentEnd, DateTime compareStart, DateTime compareEnd)
        {
            var current = await _energyRepository.GetStatisticsAsync(new EnergyQueryParams
            {
                EnergyType = energyType,
                StartTime = currentStart,
                EndTime = currentEnd,
                Period = StatisticsPeriod.Day
            });

            var previous = await _energyRepository.GetStatisticsAsync(new EnergyQueryParams
            {
                EnergyType = energyType,
                StartTime = compareStart,
                EndTime = compareEnd,
                Period = StatisticsPeriod.Day
            });

            current.YoYChangePercent = previous.TotalConsumption > 0 
                ? ((current.TotalConsumption - previous.TotalConsumption) / previous.TotalConsumption * 100) : 0;
            current.MoMChangePercent = current.YoYChangePercent; // 简化

            return current;
        }
    }

    // ===== 报表模板与生成服务 =====
    public class ReportTemplateService : ReportServiceBase
    {
        private readonly IReportTemplateRepository _templateRepository;
        private readonly IReportTaskRepository _taskRepository;

        public ReportTemplateService(IReportTemplateRepository templateRepository, IReportTaskRepository taskRepository)
        {
            _templateRepository = templateRepository;
            _taskRepository = taskRepository;
        }

        public async Task<ReportTemplate> CreateTemplateAsync(ReportTemplate template, string operatorName)
        {
            template.CreatedAt = DateTime.Now;
            template.CreatedBy = operatorName;
            return await _templateRepository.AddAsync(template);
        }

        public async Task<ReportTemplate> UpdateTemplateAsync(ReportTemplate template, string operatorName)
        {
            return await _templateRepository.UpdateAsync(template);
        }

        public async Task<ReportTemplate?> GetTemplateAsync(int id)
        {
            return await _templateRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<ReportTemplate>> GetTemplatesByTypeAsync(ReportType type)
        {
            return await _templateRepository.GetByTypeAsync(type);
        }

        public async Task<ReportGenerationTask> ScheduleReportAsync(ReportGenerationTask task, string operatorName)
        {
            task.CreatedAt = DateTime.Now;
            return await _taskRepository.AddAsync(task);
        }

        public async Task<(IEnumerable<ReportGenerationTask> Items, int TotalCount)> GetTasksPagedAsync(int page, int pageSize, ReportTaskFilter filter)
        {
            return await _taskRepository.GetPagedAsync(page, pageSize, filter);
        }

        public async Task<ReportGenerationTask?> GetTaskAsync(int id)
        {
            return await _taskRepository.GetByIdAsync(id);
        }

        public async Task<bool> ExecuteTaskAsync(int taskId)
        {
            var task = await _taskRepository.GetByIdAsync(taskId);
            if (task == null) return false;

            task.Status = GenerationStatus.Running;
            task.StartTime = DateTime.Now;
            await _taskRepository.UpdateAsync(task);

            try
            {
                // TODO: 实际报表生成逻辑
                // 这里根据模板和参数生成报表文件
                
                await Task.Delay(1000); // 模拟生成耗时
                
                task.Status = GenerationStatus.Completed;
                task.EndTime = DateTime.Now;
                task.OutputPath = $"/reports/{task.TaskName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                await _taskRepository.UpdateAsync(task);
                
                return true;
            }
            catch (Exception ex)
            {
                task.Status = GenerationStatus.Failed;
                task.EndTime = DateTime.Now;
                task.ErrorMessage = ex.Message;
                await _taskRepository.UpdateAsync(task);
                return false;
            }
        }

        public async Task ProcessPendingTasksAsync()
        {
            var pendingTasks = await _taskRepository.GetPendingTasksAsync(DateTime.Now);
            foreach (var task in pendingTasks)
            {
                _ = ExecuteTaskAsync(task.Id); // 后台异步执行
            }
        }
    }
}