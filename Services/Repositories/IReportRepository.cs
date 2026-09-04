using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Services.Repositories
{
    // ===== 历史趋势仓储 =====
    public interface IHistoricalTrendRepository
    {
        Task<IEnumerable<HistoricalTrendPoint>> GetTrendDataAsync(List<string> tagNames, DateTime startTime, DateTime endTime, int maxPoints = 1000);
        Task<IEnumerable<TrendSeries>> GetAggregatedTrendAsync(TrendQueryParams param);
        Task<long> GetCountAsync(string tagName, DateTime start, DateTime end);
        Task BulkInsertAsync(IEnumerable<HistoricalTrendPoint> points);
        Task DeleteOldDataAsync(DateTime beforeDate);
    }

    // ===== 生产报表仓储 =====
    public interface IProductionReportRepository
    {
        Task<ProductionReport?> GetByIdAsync(int id);
        Task<ProductionReport?> GetByNumberAsync(string reportNumber);
        Task<(IEnumerable<ProductionReport> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, ReportFilter filter);
        Task<int> GetCountAsync(ReportFilter filter);
        Task<ProductionReport> AddAsync(ProductionReport report);
        Task<ProductionReport> UpdateAsync(ProductionReport report);
        Task<bool> DeleteAsync(int id);
        Task<string> GenerateReportNumberAsync(ReportType type);
        Task<ProductionReportSummary> GetSummaryStatisticsAsync(DateTime start, DateTime end, ReportType type);
    }

    // ===== 班次仓储 =====
    public interface IShiftRepository
    {
        Task<IEnumerable<ShiftDefinition>> GetAllAsync();
        Task<ShiftDefinition?> GetByIdAsync(int id);
        Task<ShiftDefinition> AddAsync(ShiftDefinition shift);
        Task<ShiftDefinition> UpdateAsync(ShiftDefinition shift);
        Task<bool> DeleteAsync(int id);
    }

    // ===== OEE 仓储 =====
    public interface IOeeRepository
    {
        Task<OeeResult?> GetByIdAsync(int id);
        Task<IEnumerable<OeeResult>> GetByEquipmentAsync(string equipmentCode, DateTime start, DateTime end);
        Task<(IEnumerable<OeeResult> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, OeeFilter filter);
        Task<int> GetCountAsync(OeeFilter filter);
        Task<OeeResult> AddAsync(OeeResult oee);
        Task<OeeResult> UpdateAsync(OeeResult oee);
        Task<IEnumerable<OeeResult>> GetOeeTrendAsync(string equipmentCode, DateTime start, DateTime end, OeePeriod period);
        Task<Dictionary<string, decimal>> GetOeeSummaryAsync(DateTime start, DateTime end);
        Task<List<DowntimeRecord>> GetDowntimeRecordsAsync(string equipmentCode, DateTime start, DateTime end);
    }

    // ===== 停机记录仓储 =====
    public interface IDowntimeRepository
    {
        Task<DowntimeRecord?> GetByIdAsync(long id);
        Task<IEnumerable<DowntimeRecord>> GetByEquipmentAsync(string equipmentCode, DateTime start, DateTime end);
        Task<(IEnumerable<DowntimeRecord> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, DowntimeFilter filter);
        Task<int> GetCountAsync(DowntimeFilter filter);
        Task<DowntimeRecord> AddAsync(DowntimeRecord record);
        Task<DowntimeRecord> UpdateAsync(DowntimeRecord record);
        Task<bool> DeleteAsync(long id);
        Task<Dictionary<string, decimal>> GetDowntimeByCategoryAsync(string equipmentCode, DateTime start, DateTime end);
    }

    public class DowntimeFilter
    {
        public string? EquipmentCode { get; set; }
        public string? Category { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    // ===== 能耗仓储 =====
    public interface IEnergyRepository
    {
        Task<EnergyRecord?> GetByIdAsync(long id);
        Task<IEnumerable<EnergyRecord>> GetByMeterAsync(string meterCode, DateTime start, DateTime end);
        Task<IEnumerable<EnergyRecord>> GetByEnergyTypeAsync(EnergyType energyType, DateTime start, DateTime end);
        Task<(IEnumerable<EnergyRecord> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, EnergyFilter filter);
        Task<int> GetCountAsync(EnergyFilter filter);
        Task<EnergyRecord> AddAsync(EnergyRecord record);
        Task<bool> BulkInsertAsync(IEnumerable<EnergyRecord> records);
        Task<EnergyStatistics> GetStatisticsAsync(EnergyQueryParams param);
        Task<Dictionary<string, decimal>> GetConsumptionByEquipmentAsync(EnergyType energyType, DateTime start, DateTime end);
        Task<Dictionary<DateTime, decimal>> GetHourlyDataAsync(string meterCode, DateTime start, DateTime end);
        Task<Dictionary<DateTime, decimal>> GetDailyDataAsync(string meterCode, DateTime start, DateTime end);
        Task<EnergyStatisticsCache?> GetCachedStatisticsAsync(EnergyType energyType, string? meterCode, DateTime start, DateTime end, StatisticsPeriod period);
        Task<EnergyStatisticsCache> AddOrUpdateCacheAsync(EnergyStatisticsCache cache);
    }

    public class EnergyFilter
    {
        public List<string> MeterCodes { get; set; } = new();
        public EnergyType? EnergyType { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public StatisticsPeriod? Period { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    // ===== 能耗统计缓存仓储 =====
    public interface IEnergyStatisticsCacheRepository
    {
        Task<EnergyStatisticsCache?> GetAsync(EnergyType energyType, string? meterCode, DateTime start, DateTime end, StatisticsPeriod period);
        Task<EnergyStatisticsCache> AddOrUpdateAsync(EnergyStatisticsCache cache);
        Task<IEnumerable<EnergyStatisticsCache>> GetByEnergyTypeAsync(EnergyType energyType, DateTime start, DateTime end, StatisticsPeriod period);
        Task<bool> DeleteExpiredAsync(DateTime beforeDate);
    }

    // ===== 报表模板仓储 =====
    public interface IReportTemplateRepository
    {
        Task<ReportTemplate?> GetByIdAsync(int id);
        Task<IEnumerable<ReportTemplate>> GetByTypeAsync(ReportType type);
        Task<IEnumerable<ReportTemplate>> GetAllAsync();
        Task<ReportTemplate> AddAsync(ReportTemplate template);
        Task<ReportTemplate> UpdateAsync(ReportTemplate template);
        Task<bool> DeleteAsync(int id);
    }

    // ===== 报表生成任务仓储 =====
    public interface IReportTaskRepository
    {
        Task<ReportGenerationTask?> GetByIdAsync(int id);
        Task<(IEnumerable<ReportGenerationTask> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, ReportTaskFilter filter);
        Task<int> GetCountAsync(ReportTaskFilter filter);
        Task<ReportGenerationTask> AddAsync(ReportGenerationTask task);
        Task<ReportGenerationTask> UpdateAsync(ReportGenerationTask task);
        Task<IEnumerable<ReportGenerationTask>> GetPendingTasksAsync(DateTime beforeTime);
    }

    public class ReportTaskFilter
    {
        public string? TaskName { get; set; }
        public ReportType? ReportType { get; set; }
        public GenerationStatus? Status { get; set; }
        public DateTime? ScheduledFrom { get; set; }
        public DateTime? ScheduledTo { get; set; }
        public int? TemplateId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
    }
}