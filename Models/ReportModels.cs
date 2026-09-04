using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace TwoD5DMonitoring.Models
{
    #region 历史趋势数据

    /// <summary>
    /// 历史趋势数据点
    /// </summary>
    public class HistoricalTrendPoint : INotifyPropertyChanged
    {
        private DateTime _timestamp;
        private double _value;
        private int _quality = 192; // 192 = Good
        private string _tagName = "";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string TagName
        {
            get => _tagName;
            set { _tagName = value; OnPropertyChanged(nameof(TagName)); }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(nameof(Timestamp)); }
        }

        public double Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(nameof(Value)); }
        }

        public int Quality
        {
            get => _quality;
            set { _quality = value; OnPropertyChanged(nameof(Quality)); }
        }

        public string QualityDisplayName => Quality switch
        {
            192 => "Good",
            64 => "Uncertain",
            0 => "Bad",
            _ => Quality.ToString()
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 趋势查询参数
    /// </summary>
    public class TrendQueryParams
    {
        public List<string> TagNames { get; set; } = new();
        public DateTime StartTime { get; set; } = DateTime.Now.AddHours(-1);
        public DateTime EndTime { get; set; } = DateTime.Now;
        public int MaxPoints { get; set; } = 1000;
        public AggregationType Aggregation { get; set; } = AggregationType.Raw;
        public TimeSpan? Interval { get; set; }
    }

    public enum AggregationType
    {
        Raw = 0,        // 原始数据
        Average = 1,    // 平均值
        Min = 2,        // 最小值
        Max = 3,        // 最大值
        Sum = 4,        // 求和
        Count = 5       // 计数
    }

    /// <summary>
    /// 趋势曲线数据系列
    /// </summary>
    public class TrendSeries
    {
        public string TagName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Unit { get; set; } = "";
        public List<TrendPoint> Points { get; set; } = new();
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double AvgValue { get; set; }
        public string Color { get; set; } = "#2196F3";
        public bool Visible { get; set; } = true;
    }

    public class TrendPoint
    {
        public DateTime Time { get; set; }
        public double Value { get; set; }
        public int Quality { get; set; } = 192;
    }

    #endregion

    #region 生产报表

    /// <summary>
    /// 报表类型
    /// </summary>
    public enum ReportType
    {
        Shift = 1,      // 班报
        Daily = 2,      // 日报
        Monthly = 3,    // 月报
        Yearly = 4,     // 年报
        Custom = 99     // 自定义
    }

    /// <summary>
    /// 班次定义
    /// </summary>
    public class ShiftDefinition : INotifyPropertyChanged
    {
        private string _name = "";
        private TimeSpan _startTime;
        private TimeSpan _endTime;
        private string _color = "#2196F3";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public TimeSpan StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(nameof(StartTime)); }
        }

        public TimeSpan EndTime
        {
            get => _endTime;
            set { _endTime = value; OnPropertyChanged(nameof(EndTime)); }
        }

        [MaxLength(20)]
        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(nameof(Color)); }
        }

        public bool IsOvernight => EndTime <= StartTime;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 生产报表主表
    /// </summary>
    public class ProductionReport : INotifyPropertyChanged
    {
        private string _reportNumber = "";
        private ReportType _reportType = ReportType.Daily;
        private DateTime _periodStart;
        private DateTime _periodEnd;
        private string _shiftName = "";
        private int _shiftId;
        private ReportStatus _status = ReportStatus.Draft;
        private string _generatedBy = "";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReportNumber
        {
            get => _reportNumber;
            set { _reportNumber = value; OnPropertyChanged(nameof(ReportNumber)); }
        }

        public ReportType ReportType
        {
            get => _reportType;
            set { _reportType = value; OnPropertyChanged(nameof(ReportType)); }
        }

        public DateTime PeriodStart
        {
            get => _periodStart;
            set { _periodStart = value; OnPropertyChanged(nameof(PeriodStart)); }
        }

        public DateTime PeriodEnd
        {
            get => _periodEnd;
            set { _periodEnd = value; OnPropertyChanged(nameof(PeriodEnd)); }
        }

        public int ShiftId
        {
            get => _shiftId;
            set { _shiftId = value; OnPropertyChanged(nameof(ShiftId)); }
        }

        [MaxLength(50)]
        public string ShiftName
        {
            get => _shiftName;
            set { _shiftName = value; OnPropertyChanged(nameof(ShiftName)); }
        }

        public ReportStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        [MaxLength(100)]
        public string GeneratedBy
        {
            get => _generatedBy;
            set { _generatedBy = value; OnPropertyChanged(nameof(GeneratedBy)); }
        }

        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }

        // 汇总数据 (JSON 存储)
        public string SummaryDataJson { get; set; } = "{}";
        public string DetailDataJson { get; set; } = "{}";

        [NotMapped]
        public ProductionReportSummary Summary
        {
            get => string.IsNullOrEmpty(SummaryDataJson) ? new ProductionReportSummary() : 
                Newtonsoft.Json.JsonConvert.DeserializeObject<ProductionReportSummary>(SummaryDataJson)!;
            set => SummaryDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }

        [NotMapped]
        public ProductionReportDetails Details
        {
            get => string.IsNullOrEmpty(DetailDataJson) ? new ProductionReportDetails() : 
                Newtonsoft.Json.JsonConvert.DeserializeObject<ProductionReportDetails>(DetailDataJson)!;
            set => DetailDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public enum ReportStatus
    {
        Draft = 1,      // 草稿
        Generated = 2,  // 已生成
        Approved = 3,   // 已审核
        Published = 4,  // 已发布
        Archived = 5    // 已归档
    }

    /// <summary>
    /// 生产报表汇总数据
    /// </summary>
    public class ProductionReportSummary
    {
        public decimal TotalBatches { get; set; }
        public decimal CompletedBatches { get; set; }
        public decimal FailedBatches { get; set; }
        public decimal TotalWeight { get; set; } // kg
        public decimal QualifiedWeight { get; set; }
        public decimal ScrapWeight { get; set; }
        public decimal YieldRate { get; set; } // %
        
        // 能耗
        public decimal ElectricityConsumption { get; set; } // kWh
        public decimal GasConsumption { get; set; } // Nm³
        public decimal WaterConsumption { get; set; } // m³
        public decimal SteamConsumption { get; set; } // kg
        
        // 成本
        public decimal TotalCost { get; set; }
        
        // 原料消耗
        public Dictionary<string, decimal> MaterialConsumption { get; set; } = new();
        
        // 设备运行
        public decimal TotalRunTime { get; set; } // 分钟
        public decimal TotalDowntime { get; set; }
        public decimal Availability { get; set; } // %
        
        // 质量
        public decimal FirstPassYield { get; set; } // %
        public decimal ReworkRate { get; set; } // %
    }

    /// <summary>
    /// 生产报表明细数据
    /// </summary>
    public class ProductionReportDetails
    {
        public List<BatchSummary> Batches { get; set; } = new();
        public List<EquipmentRunRecord> EquipmentRuns { get; set; } = new();
        public List<AlarmSummary> Alarms { get; set; } = new();
        public List<MaterialConsumptionRecord> MaterialConsumptions { get; set; } = new();
        public List<EnergyConsumptionRecord> EnergyConsumptions { get; set; } = new();
    }

    public class BatchSummary
    {
        public string BatchNumber { get; set; } = "";
        public string RecipeCode { get; set; } = "";
        public string RecipeName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal TargetWeight { get; set; }
        public decimal ActualWeight { get; set; }
        public string Status { get; set; } = "";
        public string Operator { get; set; } = "";
        public TimeSpan? Duration { get; set; }
    }

    public class EquipmentRunRecord
    {
        public string EquipmentCode { get; set; } = "";
        public string EquipmentName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan RunDuration { get; set; }
        public TimeSpan Downtime { get; set; }
        public string StopReason { get; set; } = "";
        public decimal Availability { get; set; }
    }

    public class AlarmSummary
    {
        public int TotalAlarms { get; set; }
        public int CriticalAlarms { get; set; }
        public int HighAlarms { get; set; }
        public int MediumAlarms { get; set; }
        public int LowAlarms { get; set; }
        public TimeSpan TotalAlarmTime { get; set; }
        public List<ReportTopAlarm> TopAlarms { get; set; } = new();
    }

    public class ReportTopAlarm
    {
        public string AlarmName { get; set; } = "";
        public int Count { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }

    public class MaterialConsumptionRecord
    {
        public string MaterialCode { get; set; } = "";
        public string MaterialName { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal PlannedQuantity { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal Variance { get; set; }
        public decimal Cost { get; set; }
    }

    public class EnergyConsumptionRecord
    {
        public string EnergyType { get; set; } = ""; // Electricity, Gas, Water, Steam
        public string MeterName { get; set; } = "";
        public decimal StartReading { get; set; }
        public decimal EndReading { get; set; }
        public decimal Consumption { get; set; }
        public string Unit { get; set; } = "";
        public decimal Cost { get; set; }
    }

    #endregion

    #region OEE 分析

    /// <summary>
    /// OEE 计算结果
    /// </summary>
    public class OeeResult : INotifyPropertyChanged
    {
        private string _equipmentCode = "";
        private string _equipmentName = "";
        private DateTime _periodStart;
        private DateTime _periodEnd;
        private decimal _availability;
        private decimal _performance;
        private decimal _quality;
        private decimal _oee;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(50)]
        public string EquipmentCode
        {
            get => _equipmentCode;
            set { _equipmentCode = value; OnPropertyChanged(nameof(EquipmentCode)); }
        }

        [MaxLength(100)]
        public string EquipmentName
        {
            get => _equipmentName;
            set { _equipmentName = value; OnPropertyChanged(nameof(EquipmentName)); }
        }

        public DateTime PeriodStart
        {
            get => _periodStart;
            set { _periodStart = value; OnPropertyChanged(nameof(PeriodStart)); }
        }

        public DateTime PeriodEnd
        {
            get => _periodEnd;
            set { _periodEnd = value; OnPropertyChanged(nameof(PeriodEnd)); }
        }

        public decimal Availability
        {
            get => _availability;
            set { _availability = Math.Round(value, 2); OnPropertyChanged(nameof(Availability)); OnPropertyChanged(nameof(Oee)); }
        }

        public decimal Performance
        {
            get => _performance;
            set { _performance = Math.Round(value, 2); OnPropertyChanged(nameof(Performance)); OnPropertyChanged(nameof(Oee)); }
        }

        public decimal Quality
        {
            get => _quality;
            set { _quality = Math.Round(value, 2); OnPropertyChanged(nameof(Quality)); OnPropertyChanged(nameof(Oee)); }
        }

        [NotMapped]
        public decimal Oee
        {
            get => _oee;
            set { _oee = Math.Round(value, 2); OnPropertyChanged(nameof(Oee)); }
        }

        // 明细数据
        public decimal PlannedProductionTime { get; set; } // 分钟
        public decimal ActualRunTime { get; set; }
        public decimal Downtime { get; set; }
        public decimal IdealRunRate { get; set; } // 标准产能
        public decimal ActualOutput { get; set; }
        public decimal GoodOutput { get; set; }
        public decimal DefectOutput { get; set; }

        public DateTime CalculatedAt { get; set; } = DateTime.Now;

        // 停机分析
        public List<DowntimeRecord> DowntimeRecords { get; set; } = new();
        public List<LossCategory> LossCategories { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName is nameof(Availability) or nameof(Performance) or nameof(Quality))
            {
                OnPropertyChanged(nameof(Oee));
            }
        }
    }

    /// <summary>
    /// 停机记录
    /// </summary>
    public class DowntimeRecord : INotifyPropertyChanged
    {
        private DateTime _startTime;
        private DateTime? _endTime;
        private string _reason = "";
        private string _category = "";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [MaxLength(50)]
        public string EquipmentCode { get; set; } = "";

        public DateTime StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(nameof(StartTime)); OnPropertyChanged(nameof(Duration)); }
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set { _endTime = value; OnPropertyChanged(nameof(EndTime)); OnPropertyChanged(nameof(Duration)); }
        }

        [NotMapped]
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.Now - StartTime;

        [MaxLength(200)]
        public string Reason
        {
            get => _reason;
            set { _reason = value; OnPropertyChanged(nameof(Reason)); }
        }

        [MaxLength(50)]
        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        [MaxLength(100)]
        public string EquipmentName { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public double DurationMinutes => Duration?.TotalMinutes ?? 0;

        public string CategoryDisplayName => Category switch
        {
            "Planned" => "计划停机",
            "Unplanned" => "非计划停机",
            "Changeover" => "换型",
            "Maintenance" => "维护",
            "Breakdown" => "故障",
            "Setup" => "调试",
            "NoMaterial" => "缺料",
            "Quality" => "质量",
            _ => Category
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 损失分类
    /// </summary>
    public class LossCategory
    {
        public string Category { get; set; } = "";
        public decimal TimeLoss { get; set; } // 分钟
        public decimal Count { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// OEE 查询参数
    /// </summary>
    public class OeeQueryParams
    {
        public List<int> EquipmentIds { get; set; } = new();
        public DateTime StartTime { get; set; } = DateTime.Today;
        public DateTime EndTime { get; set; } = DateTime.Now;
        public OeePeriod Period { get; set; } = OeePeriod.Shift;
    }

    public enum OeePeriod
    {
        Hour = 1,
        Shift = 2,
        Day = 3,
        Week = 4,
        Month = 5
    }

    #endregion

    #region 能耗统计

    /// <summary>
    /// 能源类型
    /// </summary>
    public enum EnergyType
    {
        Electricity = 1,    // 电
        Gas = 2,            // 气
        Water = 3,          // 水
        Steam = 4,          // 蒸汽
        CompressedAir = 5,  // 压缩空气
        Other = 99
    }

    /// <summary>
    /// 能耗记录
    /// </summary>
    public class EnergyRecord : INotifyPropertyChanged
    {
        private string _meterCode = "";
        private string _meterName = "";
        private EnergyType _energyType = EnergyType.Electricity;
        private DateTime _timestamp;
        private decimal _reading;
        private decimal _consumption;
        private decimal _cost;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [MaxLength(50)]
        public string MeterCode
        {
            get => _meterCode;
            set { _meterCode = value; OnPropertyChanged(nameof(MeterCode)); }
        }

        [MaxLength(100)]
        public string MeterName
        {
            get => _meterName;
            set { _meterName = value; OnPropertyChanged(nameof(MeterName)); }
        }

        public EnergyType EnergyType
        {
            get => _energyType;
            set { _energyType = value; OnPropertyChanged(nameof(EnergyType)); }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(nameof(Timestamp)); }
        }

        public DateTime RecordedAt => Timestamp;

        public decimal Reading
        {
            get => _reading;
            set { _reading = value; OnPropertyChanged(nameof(Reading)); }
        }

        public decimal Consumption
        {
            get => _consumption;
            set { _consumption = value; OnPropertyChanged(nameof(Consumption)); }
        }

        public decimal Cost
        {
            get => _cost;
            set { _cost = value; OnPropertyChanged(nameof(Cost)); }
        }

        public string Unit => EnergyType switch
        {
            EnergyType.Electricity => "kWh",
            EnergyType.Gas => "Nm³",
            EnergyType.Water => "m³",
            EnergyType.Steam => "kg",
            EnergyType.CompressedAir => "Nm³",
            _ => ""
        };

        public string EnergyTypeDisplayName => EnergyType switch
        {
            EnergyType.Electricity => "电",
            EnergyType.Gas => "气",
            EnergyType.Water => "水",
            EnergyType.Steam => "蒸汽",
            EnergyType.CompressedAir => "压缩空气",
            _ => "其他"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 能耗统计结果
    /// </summary>
    public class EnergyStatistics
    {
        public EnergyType EnergyType { get; set; }
        public string MeterCode { get; set; } = "";
        public string MeterName { get; set; } = "";
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalConsumption { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AvgConsumptionPerHour { get; set; }
        public decimal PeakConsumption { get; set; }
        public DateTime PeakTime { get; set; }
        public decimal MinConsumption { get; set; }
        public DateTime MinTime { get; set; }

        // 分项统计
        public Dictionary<string, decimal> ByEquipment { get; set; } = new();
        public Dictionary<string, decimal> ByArea { get; set; } = new();
        public Dictionary<DateTime, decimal> HourlyData { get; set; } = new();
        public Dictionary<DateTime, decimal> DailyData { get; set; } = new();

        // 对比
        public decimal SamePeriodLastYear { get; set; }
        public decimal SamePeriodLastMonth { get; set; }
        public decimal YoYChangePercent { get; set; }
        public decimal MoMChangePercent { get; set; }

        // 单位消耗
        public decimal ConsumptionPerTon { get; set; } // 吨产品能耗
        public decimal ConsumptionPerBatch { get; set; } // 批次能耗
    }

    /// <summary>
    /// 能耗查询参数
    /// </summary>
    public class EnergyQueryParams
    {
        public List<string> MeterCodes { get; set; } = new();
        public EnergyType? EnergyType { get; set; }
        public DateTime StartTime { get; set; } = DateTime.Today;
        public DateTime EndTime { get; set; } = DateTime.Now;
        public StatisticsPeriod Period { get; set; } = StatisticsPeriod.Hour;
    }

    public enum StatisticsPeriod
    {
        Minute = 1,
        Hour = 2,
        Shift = 3,
        Day = 4,
        Month = 5
    }

    #endregion

    #region 报表模板

    /// <summary>
    /// 报表模板
    /// </summary>
    public class ReportTemplate : INotifyPropertyChanged
    {
        private string _name = "";
        private string _description = "";
        private ReportType _reportType = ReportType.Daily;
        private string _templateContent = "";
        private string _parametersJson = "{}";
        private int _version = 1;
        private bool _isDefault = false;
        private bool _isSystem = false;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        [MaxLength(500)]
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public ReportType ReportType
        {
            get => _reportType;
            set { _reportType = value; OnPropertyChanged(nameof(ReportType)); }
        }

        public string TemplateContent
        {
            get => _templateContent;
            set { _templateContent = value; OnPropertyChanged(nameof(TemplateContent)); }
        }

        public string ParametersJson
        {
            get => _parametersJson;
            set { _parametersJson = value; OnPropertyChanged(nameof(ParametersJson)); }
        }

        public int Version
        {
            get => _version;
            set { _version = value; OnPropertyChanged(nameof(Version)); }
        }

        public bool IsDefault
        {
            get => _isDefault;
            set { _isDefault = value; OnPropertyChanged(nameof(IsDefault)); }
        }

        public bool IsSystem
        {
            get => _isSystem;
            set { _isSystem = value; OnPropertyChanged(nameof(IsSystem)); }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string UpdatedBy { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 报表生成任务
    /// </summary>
    public class ReportGenerationTask : INotifyPropertyChanged
    {
        private string _taskName = "";
        private int _templateId;
        private ReportType _reportType;
        private GenerationStatus _status = GenerationStatus.Pending;
        private string _errorMessage = "";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(100)]
        public string TaskName
        {
            get => _taskName;
            set { _taskName = value; OnPropertyChanged(nameof(TaskName)); }
        }

        public int TemplateId
        {
            get => _templateId;
            set { _templateId = value; OnPropertyChanged(nameof(TemplateId)); }
        }

        public int Version { get; set; } = 1;

        public ReportType ReportType
        {
            get => _reportType;
            set { _reportType = value; OnPropertyChanged(nameof(ReportType)); }
        }

        public DateTime ScheduledTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public GenerationStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        [MaxLength(1000)]
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }

        public string OutputPath { get; set; } = "";
        public string ParametersJson { get; set; } = "{}";

        public string Operator { get; set; } = "";
        public string UpdatedBy { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public enum GenerationStatus
    {
        Pending = 1,
        Running = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }

    #endregion

    public class ReportFilter
    {
        public string? Keyword { get; set; }
        public ReportType? ReportType { get; set; }
        public ReportStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? PeriodFrom { get; set; }
        public DateTime? PeriodTo { get; set; }
        public int? ShiftId { get; set; }
        public string? GeneratedBy { get; set; }
        public string? ReportNumber { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
    }

    public class OeeFilter
    {
        public string? EquipmentCode { get; set; }
        public List<string>? EquipmentCodes { get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public DateTime? PeriodFrom { get; set; }
        public DateTime? PeriodTo { get; set; }
        public OeePeriod? Period { get; set; }
        public decimal? MinOee { get; set; }
        public decimal? MaxOee { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
    }
}