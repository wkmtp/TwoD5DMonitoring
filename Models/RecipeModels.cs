using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace TwoD5DMonitoring.Models
{
    #region 配方基础枚举与公共类

    /// <summary>
    /// 配方状态
    /// </summary>
    public enum RecipeStatus
    {
        Draft = 1,           // 草稿
        PendingReview = 2,   // 待审核
        Approved = 3,        // 已审核
        Released = 4,        // 已发布
        Archived = 5,        // 已归档
        Obsolete = 6         // 作废
    }

    /// <summary>
    /// 配方类型
    /// </summary>
    public enum RecipeType
    {
        Weighing = 1,        // 称量配方
        Mixing = 2,          // 混炼配方
        Extrusion = 3,       // 挤出配方
        Calendering = 4,     // 压延配方
        Other = 99           // 其他
    }

    /// <summary>
    /// 配方版本信息
    /// </summary>
    public class RecipeVersion : INotifyPropertyChanged
    {
        private string _version = "1.0";
        private string _changeDescription = "";
        private bool _isCurrent = true;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int RecipeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Version
        {
            get => _version;
            set { _version = value; OnPropertyChanged(nameof(Version)); }
        }

        [MaxLength(1000)]
        public string ChangeDescription
        {
            get => _changeDescription;
            set { _changeDescription = value; OnPropertyChanged(nameof(ChangeDescription)); }
        }

        public bool IsCurrent
        {
            get => _isCurrent;
            set { _isCurrent = value; OnPropertyChanged(nameof(IsCurrent)); }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "";

        [ForeignKey(nameof(RecipeId))]
        public virtual RecipeBase? Recipe { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 配方基类
    /// </summary>
    public abstract class RecipeBase : INotifyPropertyChanged
    {
        private string _code = "";
        private string _name = "";
        private RecipeType _recipeType = RecipeType.Weighing;
        private RecipeStatus _status = RecipeStatus.Draft;
        private string _productCode = "";
        private string _productName = "";
        private decimal _batchSize = 1000;      // 批次大小 (kg)
        private decimal _yieldRate = 100;       // 理论产率 (%)
        private string _remark = "";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(nameof(Code)); }
        }

        [Required]
        [MaxLength(100)]
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public RecipeType RecipeType
        {
            get => _recipeType;
            set { _recipeType = value; OnPropertyChanged(nameof(RecipeType)); }
        }

        public RecipeStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        [MaxLength(50)]
        public string ProductCode
        {
            get => _productCode;
            set { _productCode = value; OnPropertyChanged(nameof(ProductCode)); }
        }

        [MaxLength(100)]
        public string ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(nameof(ProductName)); }
        }

        public decimal BatchSize
        {
            get => _batchSize;
            set { _batchSize = value; OnPropertyChanged(nameof(BatchSize)); }
        }

        public decimal YieldRate
        {
            get => _yieldRate;
            set { _yieldRate = value; OnPropertyChanged(nameof(YieldRate)); }
        }

        [MaxLength(2000)]
        public string Remark
        {
            get => _remark;
            set { _remark = value; OnPropertyChanged(nameof(Remark)); }
        }

        // 版本控制
        public string CurrentVersion { get; set; } = "1.0";
        public int CurrentVersionId { get; set; }

        // 审计字段
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string UpdatedBy { get; set; } = "";
        public DateTime? ApprovedAt { get; set; }
        public string ApprovedBy { get; set; } = "";
        public DateTime? ReleasedAt { get; set; }
        public string ReleasedBy { get; set; } = "";

        // 导航属性
        public virtual ObservableCollection<RecipeVersion> Versions { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public string DisplayName => $"[{Code}] {Name}";
        public string StatusDisplayName => Status switch
        {
            RecipeStatus.Draft => "草稿",
            RecipeStatus.PendingReview => "待审核",
            RecipeStatus.Approved => "已审核",
            RecipeStatus.Released => "已发布",
            RecipeStatus.Archived => "已归档",
            RecipeStatus.Obsolete => "作废",
            _ => "未知"
        };
    }

    #endregion

    #region 称量配方

    /// <summary>
    /// 称量配方
    /// </summary>
    public class WeighingRecipe : RecipeBase
    {
        public WeighingRecipe()
        {
            RecipeType = RecipeType.Weighing;
        }

        private WeighingMode _weighingMode = WeighingMode.Sequential;
        private int _scaleId;                    // 使用的秤ID
        private decimal _totalWeight;            // 总重量 (自动计算)
        private int _decimalPlaces = 2;          // 小数位数
        private bool _allowManualAdjust = true;  // 允许人工调整

        public WeighingMode WeighingMode
        {
            get => _weighingMode;
            set { _weighingMode = value; OnPropertyChanged(nameof(WeighingMode)); }
        }

        public int ScaleId
        {
            get => _scaleId;
            set { _scaleId = value; OnPropertyChanged(nameof(ScaleId)); }
        }

        public decimal TotalWeight
        {
            get => _totalWeight;
            set { _totalWeight = value; OnPropertyChanged(nameof(TotalWeight)); }
        }

        public int DecimalPlaces
        {
            get => _decimalPlaces;
            set { _decimalPlaces = value; OnPropertyChanged(nameof(DecimalPlaces)); }
        }

        public bool AllowManualAdjust
        {
            get => _allowManualAdjust;
            set { _allowManualAdjust = value; OnPropertyChanged(nameof(AllowManualAdjust)); }
        }

        // 称量明细行
        public virtual ObservableCollection<WeighingRecipeLine> Lines { get; set; } = new();

        // 计算总重量
        public void CalculateTotalWeight()
        {
            TotalWeight = Lines.Sum(l => l.TargetWeight);
        }
    }

    /// <summary>
    /// 称量模式
    /// </summary>
    public enum WeighingMode
    {
        Sequential = 1,    // 顺序称量
        Simultaneous = 2,  // 同时称量 (多秤)
        Manual = 3         // 手工称量
    }

    /// <summary>
    /// 称量配方明细行
    /// </summary>
    public class WeighingRecipeLine : INotifyPropertyChanged
    {
        private int _lineNumber;
        private int _materialId;
        private decimal _targetWeight;
        private decimal _tolerancePositive = 1.0m;   // 正公差 (%)
        private decimal _toleranceNegative = 1.0m;   // 负公差 (%)
        private int _weighingOrder;                  // 称量顺序
        private bool _isCritical = true;             // 关键物料
        private string _remark = "";
        private FeederType _feederType = FeederType.Auto; // 加料方式

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int RecipeId { get; set; }

        public int LineNumber
        {
            get => _lineNumber;
            set { _lineNumber = value; OnPropertyChanged(nameof(LineNumber)); }
        }

        public int MaterialId
        {
            get => _materialId;
            set { _materialId = value; OnPropertyChanged(nameof(MaterialId)); }
        }

        public decimal TargetWeight
        {
            get => _targetWeight;
            set { _targetWeight = value; OnPropertyChanged(nameof(TargetWeight)); }
        }

        public decimal TolerancePositive
        {
            get => _tolerancePositive;
            set { _tolerancePositive = value; OnPropertyChanged(nameof(TolerancePositive)); }
        }

        public decimal ToleranceNegative
        {
            get => _toleranceNegative;
            set { _toleranceNegative = value; OnPropertyChanged(nameof(ToleranceNegative)); }
        }

        public int WeighingOrder
        {
            get => _weighingOrder;
            set { _weighingOrder = value; OnPropertyChanged(nameof(WeighingOrder)); }
        }

        public bool IsCritical
        {
            get => _isCritical;
            set { _isCritical = value; OnPropertyChanged(nameof(IsCritical)); }
        }

        [MaxLength(500)]
        public string Remark
        {
            get => _remark;
            set { _remark = value; OnPropertyChanged(nameof(Remark)); }
        }

        public FeederType FeederType
        {
            get => _feederType;
            set { _feederType = value; OnPropertyChanged(nameof(FeederType)); }
        }

        // PLC 相关
        public int? PlcTagIndex { get; set; }           // PLC 标签索引
        public string? PlcWeightTag { get; set; }       // PLC 重量标签地址
        public string? PlcMaterialTag { get; set; }     // PLC 物料代码标签地址

        // 导航属性
        [ForeignKey(nameof(RecipeId))]
        public virtual WeighingRecipe? Recipe { get; set; }
        [ForeignKey(nameof(MaterialId))]
        public virtual Material? Material { get; set; }

        // 显示用冗余字段
        [NotMapped]
        public string? MaterialCode { get; set; }
        [NotMapped]
        public string? MaterialName { get; set; }
        [NotMapped]
        public string? MaterialUnit { get; set; }

        // 计算公差范围
        public decimal MinWeight => TargetWeight * (1 - ToleranceNegative / 100);
        public decimal MaxWeight => TargetWeight * (1 + TolerancePositive / 100);
        public decimal Percentage => BatchSize > 0 ? Math.Round(TargetWeight / BatchSize * 100, 2) : 0;

        [NotMapped]
        public decimal BatchSize { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 加料方式
    /// </summary>
    public enum FeederType
    {
        Auto = 1,          // 自动加料
        SemiAuto = 2,      // 半自动
        Manual = 3,        // 手工加料
        Vacuum = 4,        // 真空吸料
        Screw = 5          // 螺旋加料
    }

    #endregion

    #region 混炼配方

    /// <summary>
    /// 混炼配方
    /// </summary>
    public class MixingRecipe : RecipeBase
    {
        public MixingRecipe()
        {
            RecipeType = RecipeType.Mixing;
        }

        private int _mixerId;                       // 混炼机ID
        private MixingProcessType _processType = MixingProcessType.Banbury; // 混炼工艺类型
        private int _totalSteps;                    // 总步骤数
        private decimal _targetTemperature;         // 目标温度
        private int _targetTime;                    // 目标时间 (秒)
        private int _dumpTemperature;               // 出料温度
        private decimal _ramPressure;               // 顶栓压力
        private int _ramPosition;                   // 顶栓位置
        private int _rotorSpeed;                    // 转子转速
        private bool _enableTempControl = true;     // 启用温控
        private bool _enableEnergyControl = false;  // 启用能耗控制

        public int MixerId
        {
            get => _mixerId;
            set { _mixerId = value; OnPropertyChanged(nameof(MixerId)); }
        }

        public MixingProcessType ProcessType
        {
            get => _processType;
            set { _processType = value; OnPropertyChanged(nameof(ProcessType)); }
        }

        public int TotalSteps
        {
            get => _totalSteps;
            set { _totalSteps = value; OnPropertyChanged(nameof(TotalSteps)); }
        }

        public decimal TargetTemperature
        {
            get => _targetTemperature;
            set { _targetTemperature = value; OnPropertyChanged(nameof(TargetTemperature)); }
        }

        public int TargetTime
        {
            get => _targetTime;
            set { _targetTime = value; OnPropertyChanged(nameof(TargetTime)); }
        }

        public int DumpTemperature
        {
            get => _dumpTemperature;
            set { _dumpTemperature = value; OnPropertyChanged(nameof(DumpTemperature)); }
        }

        public decimal RamPressure
        {
            get => _ramPressure;
            set { _ramPressure = value; OnPropertyChanged(nameof(RamPressure)); }
        }

        public int RamPosition
        {
            get => _ramPosition;
            set { _ramPosition = value; OnPropertyChanged(nameof(RamPosition)); }
        }

        public int RotorSpeed
        {
            get => _rotorSpeed;
            set { _rotorSpeed = value; OnPropertyChanged(nameof(RotorSpeed)); }
        }

        public bool EnableTempControl
        {
            get => _enableTempControl;
            set { _enableTempControl = value; OnPropertyChanged(nameof(EnableTempControl)); }
        }

        public bool EnableEnergyControl
        {
            get => _enableEnergyControl;
            set { _enableEnergyControl = value; OnPropertyChanged(nameof(EnableEnergyControl)); }
        }

        // 混炼步骤
        public virtual ObservableCollection<MixingRecipeStep> Steps { get; set; } = new();

        // 计算总步骤数
        public void CalculateTotalSteps()
        {
            TotalSteps = Steps.Count;
        }
    }

    /// <summary>
    /// 混炼工艺类型
    /// </summary>
    public enum MixingProcessType
    {
        Banbury = 1,        // 密炼机
        InternalMixer = 2,  // 内混机
        OpenMixer = 3,      // 开炼机
        Continuous = 4      // 连续混炼
    }

    /// <summary>
    /// 混炼配方步骤
    /// </summary>
    public class MixingRecipeStep : INotifyPropertyChanged
    {
        private int _stepNumber;
        private StepType _stepType = StepType.AddMaterial;
        private int? _materialId;
        private decimal _amount;                    // 投料量
        private int _addTime;                       // 投料时间 (秒，相对于步骤开始)
        private int _duration;                      // 步骤持续时间 (秒)
        private int _targetTemp;                    // 目标温度
        private int _rotorSpeed;                    // 转子转速
        private decimal _ramPressure;               // 顶栓压力
        private bool _isAutoDump = false;           // 自动出料
        private bool _waitForTemp = false;          // 等待温度
        private string _condition = "";             // 跳转条件表达式
        private string _remark = "";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int RecipeId { get; set; }

        public int StepNumber
        {
            get => _stepNumber;
            set { _stepNumber = value; OnPropertyChanged(nameof(StepNumber)); }
        }

        public StepType StepType
        {
            get => _stepType;
            set { _stepType = value; OnPropertyChanged(nameof(StepType)); }
        }

        public int? MaterialId
        {
            get => _materialId;
            set { _materialId = value; OnPropertyChanged(nameof(MaterialId)); }
        }

        public decimal Amount
        {
            get => _amount;
            set { _amount = value; OnPropertyChanged(nameof(Amount)); }
        }

        public int AddTime
        {
            get => _addTime;
            set { _addTime = value; OnPropertyChanged(nameof(AddTime)); }
        }

        public int Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(nameof(Duration)); }
        }

        public int TargetTemp
        {
            get => _targetTemp;
            set { _targetTemp = value; OnPropertyChanged(nameof(TargetTemp)); }
        }

        public int RotorSpeed
        {
            get => _rotorSpeed;
            set { _rotorSpeed = value; OnPropertyChanged(nameof(RotorSpeed)); }
        }

        public decimal RamPressure
        {
            get => _ramPressure;
            set { _ramPressure = value; OnPropertyChanged(nameof(RamPressure)); }
        }

        public bool IsAutoDump
        {
            get => _isAutoDump;
            set { _isAutoDump = value; OnPropertyChanged(nameof(IsAutoDump)); }
        }

        public bool WaitForTemp
        {
            get => _waitForTemp;
            set { _waitForTemp = value; OnPropertyChanged(nameof(WaitForTemp)); }
        }

        [MaxLength(500)]
        public string Condition
        {
            get => _condition;
            set { _condition = value; OnPropertyChanged(nameof(Condition)); }
        }

        [MaxLength(500)]
        public string Remark
        {
            get => _remark;
            set { _remark = value; OnPropertyChanged(nameof(Remark)); }
        }

        // PLC 相关
        public int? PlcStepIndex { get; set; }
        public string? PlcCommandTag { get; set; }      // PLC 命令标签
        public string? PlcParamTags { get; set; }       // PLC 参数标签 (JSON)

        // 导航属性
        [ForeignKey(nameof(RecipeId))]
        public virtual MixingRecipe? Recipe { get; set; }
        [ForeignKey(nameof(MaterialId))]
        public virtual Material? Material { get; set; }

        // 显示用
        [NotMapped]
        public string? MaterialCode { get; set; }
        [NotMapped]
        public string? MaterialName { get; set; }
        [NotMapped]
        public string StepTypeDisplayName => StepType switch
        {
            StepType.AddMaterial => "投料",
            StepType.Mix => "混炼",
            StepType.Heat => "升温",
            StepType.Cool => "降温",
            StepType.Dump => "出料",
            StepType.Wait => "等待",
            StepType.Custom => "自定义",
            _ => "未知"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 混炼步骤类型
    /// </summary>
    public enum StepType
    {
        AddMaterial = 1,    // 投料
        Mix = 2,            // 混炼
        Heat = 3,           // 升温
        Cool = 4,           // 降温
        Dump = 5,           // 出料
        Wait = 6,           // 等待
        Custom = 99         // 自定义
    }

    #endregion

    #region 配方管理相关

    /// <summary>
    /// 配方查询过滤器
    /// </summary>
    public class RecipeFilter
    {
        public string? Keyword { get; set; }
        public RecipeType? RecipeType { get; set; }
        public RecipeStatus? Status { get; set; }
        public string? ProductCode { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public string? CreatedBy { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
    }

    /// <summary>
    /// 配方下发记录
    /// </summary>
    public class RecipeDownloadLog : INotifyPropertyChanged
    {
        private int _recipeId;
        private string _recipeCode = "";
        private string _recipeName = "";
        private RecipeType _recipeType;
        private string _version = "";
        private string _targetPlc = "";
        private DownloadStatus _status = DownloadStatus.Pending;
        private string _errorMessage = "";
        private string _operator = "";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int RecipeId
        {
            get => _recipeId;
            set { _recipeId = value; OnPropertyChanged(nameof(RecipeId)); }
        }

        [MaxLength(50)]
        public string RecipeCode
        {
            get => _recipeCode;
            set { _recipeCode = value; OnPropertyChanged(nameof(RecipeCode)); }
        }

        [MaxLength(100)]
        public string RecipeName
        {
            get => _recipeName;
            set { _recipeName = value; OnPropertyChanged(nameof(RecipeName)); }
        }

        public RecipeType RecipeType
        {
            get => _recipeType;
            set { _recipeType = value; OnPropertyChanged(nameof(RecipeType)); }
        }

        [MaxLength(50)]
        public string Version
        {
            get => _version;
            set { _version = value; OnPropertyChanged(nameof(Version)); }
        }

        [MaxLength(100)]
        public string TargetPlc
        {
            get => _targetPlc;
            set { _targetPlc = value; OnPropertyChanged(nameof(TargetPlc)); }
        }

        public DownloadStatus Status
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

        [MaxLength(50)]
        public string Operator
        {
            get => _operator;
            set { _operator = value; OnPropertyChanged(nameof(Operator)); }
        }

        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

        // 详细数据 (JSON)
        public string? DownloadData { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 下发状态
    /// </summary>
    public enum DownloadStatus
    {
        Pending = 1,      // 待下发
        InProgress = 2,   // 下发中
        Success = 3,      // 成功
        Failed = 4,       // 失败
        Partial = 5,      // 部分成功
        Verified = 6      // 已校验
    }

    /// <summary>
    /// 批次生产记录
    /// </summary>
    public class BatchRecord : INotifyPropertyChanged
    {
        private string _batchNumber = "";
        private int _recipeId;
        private string _recipeCode = "";
        private string _recipeVersion = "";
        private int _mixerId;
        private int _scaleId;
        private BatchStatus _status = BatchStatus.Created;
        private decimal _actualWeight;
        private decimal _targetWeight;
        private DateTime _startTime;
        private DateTime? _endTime;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BatchNumber
        {
            get => _batchNumber;
            set { _batchNumber = value; OnPropertyChanged(nameof(BatchNumber)); }
        }

        public int RecipeId
        {
            get => _recipeId;
            set { _recipeId = value; OnPropertyChanged(nameof(RecipeId)); }
        }

        [MaxLength(50)]
        public string RecipeCode
        {
            get => _recipeCode;
            set { _recipeCode = value; OnPropertyChanged(nameof(RecipeCode)); }
        }

        [MaxLength(50)]
        public string RecipeVersion
        {
            get => _recipeVersion;
            set { _recipeVersion = value; OnPropertyChanged(nameof(RecipeVersion)); }
        }

        public int MixerId
        {
            get => _mixerId;
            set { _mixerId = value; OnPropertyChanged(nameof(MixerId)); }
        }

        public int ScaleId
        {
            get => _scaleId;
            set { _scaleId = value; OnPropertyChanged(nameof(ScaleId)); }
        }

        public BatchStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public decimal ActualWeight
        {
            get => _actualWeight;
            set { _actualWeight = value; OnPropertyChanged(nameof(ActualWeight)); }
        }

        public decimal TargetWeight
        {
            get => _targetWeight;
            set { _targetWeight = value; OnPropertyChanged(nameof(TargetWeight)); }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(nameof(StartTime)); }
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set { _endTime = value; OnPropertyChanged(nameof(EndTime)); }
        }

        public string? Operator { get; set; }
        public string? Shift { get; set; }
        public string? Remark { get; set; }

        // 称量明细记录 (JSON)
        public string? WeighingDetails { get; set; }
        // 混炼过程记录 (JSON)
        public string? MixingDetails { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 批次状态
    /// </summary>
    public enum BatchStatus
    {
        Created = 1,        // 已创建
        Weighing = 2,       // 称量中
        Weighed = 3,        // 称量完成
        Mixing = 4,         // 混炼中
        Mixed = 5,          // 混炼完成
        Dumped = 6,         // 已出料
        Completed = 7,      // 完成
        Aborted = 8,        // 中止
        OnHold = 9          // 暂停
    }

    #endregion

    #region 服务辅助模型

    public class RecipeComparisonResult
    {
        public int Recipe1Id { get; set; }
        public int Recipe2Id { get; set; }
        public string Recipe1Code { get; set; } = "";
        public string Recipe2Code { get; set; } = "";
        public bool AreIdentical { get; set; }
        public List<string> Differences { get; set; } = new();
    }

    public class BatchDownloadResult
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public string Message { get; set; } = "";
        public List<DownloadResult> Results { get; set; } = new();
    }

    public class DownloadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public List<string> Mismatches { get; set; } = new();
    }

    #endregion
}