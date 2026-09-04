using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace TwoD5DMonitoring.Models
{
    #region 报警枚举

    /// <summary>
    /// 报警优先级
    /// </summary>
    public enum AlarmPriority
    {
        Low = 1,          // 低 - 信息类
        Medium = 2,       // 中 - 警告类
        High = 3,         // 高 - 严重报警
        Critical = 4      // 紧急 - 紧急停机
    }

    /// <summary>
    /// 报警状态
    /// </summary>
    public enum AlarmState
    {
        Inactive = 1,     // 未激活
        Active = 2,       // 激活中
        Acknowledged = 3, // 已确认
        Returned = 4,     // 已恢复
        Shelved = 5,      // 已屏蔽/搁置
        Suppressed = 6    // 已抑制
    }

    /// <summary>
    /// 报警类型
    /// </summary>
    public enum AlarmType
    {
        HiHi = 1,         // 高高报警
        Hi = 2,           // 高报警
        Lo = 3,           // 低报警
        LoLo = 4,         // 低低报警
        ROC = 5,          // 变化率报警
        Dev = 6,          // 偏差报警
        Digital = 7,      // 数字量报警
        System = 8,       // 系统报警
        Communication = 9, // 通讯故障
        Custom = 99       // 自定义
    }

    /// <summary>
    /// 报警分类
    /// </summary>
    public enum AlarmCategory
    {
        Process = 1,      // 过程报警
        Equipment = 2,    // 设备报警
        Quality = 3,      // 质量报警
        Safety = 4,       // 安全报警
        Environmental = 5, // 环保报警
        Maintenance = 6,  // 维护报警
        System = 7        // 系统报警
    }

    #endregion

    #region 报警定义 (配置)

    /// <summary>
    /// 报警定义 - 配置层面的报警设置
    /// </summary>
    public class AlarmDefinition : INotifyPropertyChanged
    {
        private string _tagName = "";
        private string _alarmName = "";
        private AlarmType _alarmType = AlarmType.Hi;
        private AlarmPriority _priority = AlarmPriority.Medium;
        private AlarmCategory _category = AlarmCategory.Process;
        private double _setpoint;
        private double _deadband = 0.5;
        private int _delaySeconds = 0;
        private string _message = "";
        private string _helpText = "";
        private bool _isEnabled = true;
        private bool _requireAck = true;
        private bool _logToHistory = true;
        private bool _printAlarm = false;
        private string _plcTagAddress = "";
        private int _equipmentId;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string TagName
        {
            get => _tagName;
            set { _tagName = value; OnPropertyChanged(nameof(TagName)); }
        }

        [Required]
        [MaxLength(100)]
        public string AlarmName
        {
            get => _alarmName;
            set { _alarmName = value; OnPropertyChanged(nameof(AlarmName)); }
        }

        public AlarmType AlarmType
        {
            get => _alarmType;
            set { _alarmType = value; OnPropertyChanged(nameof(AlarmType)); }
        }

        public AlarmPriority Priority
        {
            get => _priority;
            set { _priority = value; OnPropertyChanged(nameof(Priority)); }
        }

        public AlarmCategory Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public double Setpoint
        {
            get => _setpoint;
            set { _setpoint = value; OnPropertyChanged(nameof(Setpoint)); }
        }

        public double Deadband
        {
            get => _deadband;
            set { _deadband = value; OnPropertyChanged(nameof(Deadband)); }
        }

        public int DelaySeconds
        {
            get => _delaySeconds;
            set { _delaySeconds = value; OnPropertyChanged(nameof(DelaySeconds)); }
        }

        [MaxLength(500)]
        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(nameof(Message)); }
        }

        [MaxLength(1000)]
        public string HelpText
        {
            get => _helpText;
            set { _helpText = value; OnPropertyChanged(nameof(HelpText)); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        public bool RequireAck
        {
            get => _requireAck;
            set { _requireAck = value; OnPropertyChanged(nameof(RequireAck)); }
        }

        public bool LogToHistory
        {
            get => _logToHistory;
            set { _logToHistory = value; OnPropertyChanged(nameof(LogToHistory)); }
        }

        public bool PrintAlarm
        {
            get => _printAlarm;
            set { _printAlarm = value; OnPropertyChanged(nameof(PrintAlarm)); }
        }

        [MaxLength(200)]
        public string PlcTagAddress
        {
            get => _plcTagAddress;
            set { _plcTagAddress = value; OnPropertyChanged(nameof(PlcTagAddress)); }
        }

        public int EquipmentId
        {
            get => _equipmentId;
            set { _equipmentId = value; OnPropertyChanged(nameof(EquipmentId)); }
        }

        public int? GroupId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 显示用
        [NotMapped]
        public string? EquipmentName { get; set; }

        [NotMapped]
        public string PriorityDisplayName => Priority switch
        {
            AlarmPriority.Low => "低",
            AlarmPriority.Medium => "中",
            AlarmPriority.High => "高",
            AlarmPriority.Critical => "紧急",
            _ => "未知"
        };

        [NotMapped]
        public string TypeDisplayName => AlarmType switch
        {
            AlarmType.HiHi => "高高",
            AlarmType.Hi => "高",
            AlarmType.Lo => "低",
            AlarmType.LoLo => "低低",
            AlarmType.ROC => "变化率",
            AlarmType.Dev => "偏差",
            AlarmType.Digital => "数字量",
            AlarmType.System => "系统",
            AlarmType.Communication => "通讯",
            AlarmType.Custom => "自定义",
            _ => "未知"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 报警组 - 用于分组管理报警
    /// </summary>
    public class AlarmGroup : INotifyPropertyChanged
    {
        private string _name = "";
        private string _description = "";
        private bool _isEnabled = true;

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

        [MaxLength(500)]
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ObservableCollection<AlarmDefinition> Alarms { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 报警分组成员
    /// </summary>
    public class AlarmGroupMember
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int GroupId { get; set; }
        public int AlarmDefinitionId { get; set; }

        [ForeignKey(nameof(GroupId))]
        public virtual AlarmGroup? Group { get; set; }

        [ForeignKey(nameof(AlarmDefinitionId))]
        public virtual AlarmDefinition? AlarmDefinition { get; set; }
    }

    #endregion

    #region 报警实例 (运行时)

    /// <summary>
    /// 报警实例 - 运行时实际发生的报警
    /// </summary>
    public class AlarmInstance : INotifyPropertyChanged
    {
        private int _definitionId;
        private AlarmState _state = AlarmState.Inactive;
        private double _currentValue;
        private double _setpoint;
        private DateTime _occurTime;
        private DateTime? _ackTime;
        private DateTime? _returnTime;
        private string _ackedBy = "";
        private string _comment = "";
        private int _occurrenceCount = 1;
        private bool _isShelved;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int DefinitionId
        {
            get => _definitionId;
            set { _definitionId = value; OnPropertyChanged(nameof(DefinitionId)); }
        }

        public AlarmState State
        {
            get => _state;
            set { _state = value; OnPropertyChanged(nameof(State)); OnPropertyChanged(nameof(StateDisplayName)); }
        }

        public double CurrentValue
        {
            get => _currentValue;
            set { _currentValue = value; OnPropertyChanged(nameof(CurrentValue)); }
        }

        public double Setpoint
        {
            get => _setpoint;
            set { _setpoint = value; OnPropertyChanged(nameof(Setpoint)); }
        }

        public DateTime OccurTime
        {
            get => _occurTime;
            set { _occurTime = value; OnPropertyChanged(nameof(OccurTime)); }
        }

        public DateTime? AckTime
        {
            get => _ackTime;
            set { _ackTime = value; OnPropertyChanged(nameof(AckTime)); }
        }

        public DateTime? ReturnTime
        {
            get => _returnTime;
            set { _returnTime = value; OnPropertyChanged(nameof(ReturnTime)); }
        }

        [MaxLength(100)]
        public string AckedBy
        {
            get => _ackedBy;
            set { _ackedBy = value; OnPropertyChanged(nameof(AckedBy)); }
        }

        [MaxLength(500)]
        public string Comment
        {
            get => _comment;
            set { _comment = value; OnPropertyChanged(nameof(Comment)); }
        }

        public int OccurrenceCount
        {
            get => _occurrenceCount;
            set { _occurrenceCount = value; OnPropertyChanged(nameof(OccurrenceCount)); }
        }

        public bool IsShelved
        {
            get => _isShelved;
            set { _isShelved = value; OnPropertyChanged(nameof(IsShelved)); }
        }

        // 导航属性
        [ForeignKey(nameof(DefinitionId))]
        public virtual AlarmDefinition? Definition { get; set; }

        // 显示用冗余字段
        [NotMapped]
        public string? TagName { get; set; }
        [NotMapped]
        public string? AlarmName { get; set; }
        [NotMapped]
        public AlarmPriority? Priority { get; set; }
        [NotMapped]
        public AlarmType? AlarmType { get; set; }
        [NotMapped]
        public string? EquipmentName { get; set; }

        [NotMapped]
        public string StateDisplayName => State switch
        {
            AlarmState.Inactive => "未激活",
            AlarmState.Active => "激活中",
            AlarmState.Acknowledged => "已确认",
            AlarmState.Shelved => "已屏蔽",
            AlarmState.Suppressed => "已抑制",
            _ => "未知"
        };

        [NotMapped]
        public TimeSpan? Duration => ReturnTime.HasValue ? ReturnTime.Value - OccurTime : (DateTime.Now - OccurTime);

        [NotMapped]
        public TimeSpan? AckDuration => AckTime.HasValue ? AckTime.Value - OccurTime : null;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 历史报警记录
    /// </summary>
    public class AlarmHistory : INotifyPropertyChanged
    {
        private int _definitionId;
        private AlarmState _finalState;
        private double _occurValue;
        private double _returnValue;
        private DateTime _occurTime;
        private DateTime? _ackTime;
        private DateTime? _returnTime;
        private string _ackedBy = "";
        private string _comment = "";
        private int _occurrenceCount;
        private TimeSpan _totalActiveTime;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int DefinitionId
        {
            get => _definitionId;
            set { _definitionId = value; OnPropertyChanged(nameof(DefinitionId)); }
        }

        public AlarmState FinalState
        {
            get => _finalState;
            set { _finalState = value; OnPropertyChanged(nameof(FinalState)); }
        }

        public double OccurValue
        {
            get => _occurValue;
            set { _occurValue = value; OnPropertyChanged(nameof(OccurValue)); }
        }

        public double ReturnValue
        {
            get => _returnValue;
            set { _returnValue = value; OnPropertyChanged(nameof(ReturnValue)); }
        }

        public DateTime OccurTime
        {
            get => _occurTime;
            set { _occurTime = value; OnPropertyChanged(nameof(OccurTime)); }
        }

        public DateTime? AckTime
        {
            get => _ackTime;
            set { _ackTime = value; OnPropertyChanged(nameof(AckTime)); }
        }

        public DateTime? ReturnTime
        {
            get => _returnTime;
            set { _returnTime = value; OnPropertyChanged(nameof(ReturnTime)); }
        }

        [MaxLength(100)]
        public string AckedBy
        {
            get => _ackedBy;
            set { _ackedBy = value; OnPropertyChanged(nameof(AckedBy)); }
        }

        [MaxLength(500)]
        public string Comment
        {
            get => _comment;
            set { _comment = value; OnPropertyChanged(nameof(Comment)); }
        }

        public int OccurrenceCount
        {
            get => _occurrenceCount;
            set { _occurrenceCount = value; OnPropertyChanged(nameof(OccurrenceCount)); }
        }

        public TimeSpan TotalActiveTime
        {
            get => _totalActiveTime;
            set { _totalActiveTime = value; OnPropertyChanged(nameof(TotalActiveTime)); }
        }

        // 冗余字段
        [MaxLength(100)]
        public string TagName { get; set; } = "";
        [MaxLength(100)]
        public string AlarmName { get; set; } = "";
        public AlarmPriority Priority { get; set; }
        public AlarmType AlarmType { get; set; }
        [MaxLength(100)]
        public string EquipmentName { get; set; } = "";
        public AlarmCategory Category { get; set; }
        public double Setpoint { get; set; }

        [NotMapped]
        public string StateDisplayName => FinalState switch
        {
            AlarmState.Inactive => "未激活",
            AlarmState.Active => "激活中",
            AlarmState.Acknowledged => "已确认",
            AlarmState.Returned => "已恢复",
            AlarmState.Shelved => "已屏蔽",
            AlarmState.Suppressed => "已抑制",
            _ => "未知"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region 报警通知/订阅

    /// <summary>
    /// 报警通知配置
    /// </summary>
    public class AlarmNotification : INotifyPropertyChanged
    {
        private string _name = "";
        private string _recipients = ""; // JSON 数组
        private NotificationMethod _method = NotificationMethod.UI;
        private AlarmPriority _minPriority = AlarmPriority.Medium;
        private bool _isEnabled = true;

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

        public string Recipients
        {
            get => _recipients;
            set { _recipients = value; OnPropertyChanged(nameof(Recipients)); }
        }

        public NotificationMethod Method
        {
            get => _method;
            set { _method = value; OnPropertyChanged(nameof(Method)); }
        }

        public AlarmPriority MinPriority
        {
            get => _minPriority;
            set { _minPriority = value; OnPropertyChanged(nameof(MinPriority)); }
        }

        [MaxLength(500)]
        public string WebhookUrl { get; set; } = "";

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public List<string> RecipientList
        {
            get => string.IsNullOrEmpty(_recipients) ? new List<string>() : 
                Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(_recipients)!;
            set => _recipients = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public enum NotificationMethod
    {
        UI = 1,           // 界面弹窗
        Sound = 2,        // 声音
        Email = 3,        // 邮件
        SMS = 4,          // 短信
        Webhook = 5,      // Webhook
        Print = 6         // 打印
    }

    /// <summary>
    /// 报警抑制规则
    /// </summary>
    public class AlarmSuppression : INotifyPropertyChanged
    {
        private string _name = "";
        private SuppressionType _type = SuppressionType.TimeBased;
        private TimeSpan _startTime;
        private TimeSpan _endTime;
        private DayOfWeek[] _daysOfWeek = new DayOfWeek[0];
        private string _tagFilter = ""; // 正则表达式
        private int _equipmentId;
        private bool _isEnabled = true;

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

        public SuppressionType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(nameof(Type)); }
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

        public string DaysOfWeekJson
        {
            get => Newtonsoft.Json.JsonConvert.SerializeObject(_daysOfWeek);
            set => _daysOfWeek = string.IsNullOrEmpty(value) ? new DayOfWeek[0] : 
                Newtonsoft.Json.JsonConvert.DeserializeObject<DayOfWeek[]>(value)!;
        }

        [NotMapped]
        public DayOfWeek[] DaysOfWeek
        {
            get => _daysOfWeek;
            set { _daysOfWeek = value; OnPropertyChanged(nameof(DaysOfWeek)); }
        }

        [MaxLength(500)]
        public string TagFilter
        {
            get => _tagFilter;
            set { _tagFilter = value; OnPropertyChanged(nameof(TagFilter)); }
        }

        public int EquipmentId
        {
            get => _equipmentId;
            set { _equipmentId = value; OnPropertyChanged(nameof(EquipmentId)); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public enum SuppressionType
    {
        TimeBased = 1,      // 基于时间
        EquipmentBased = 2, // 基于设备
        TagBased = 3,       // 基于标签
        Manual = 4          // 手动抑制
    }

    #endregion

    #region 查询过滤器

    public class AlarmFilter
    {
        public string? Keyword { get; set; }
        public AlarmState? State { get; set; }
        public AlarmPriority? Priority { get; set; }
        public AlarmType? AlarmType { get; set; }
        public AlarmCategory? Category { get; set; }
        public int? EquipmentId { get; set; }
        public string? TagName { get; set; }
        public DateTime? OccurFrom { get; set; }
        public DateTime? OccurTo { get; set; }
        public DateTime? AckFrom { get; set; }
        public DateTime? AckTo { get; set; }
        public string? AckedBy { get; set; }
        public bool? IsShelved { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "OccurTime";
        public bool SortDescending { get; set; } = true;
    }

    public class AlarmHistoryFilter
    {
        public string? Keyword { get; set; }
        public AlarmPriority? Priority { get; set; }
        public AlarmType? AlarmType { get; set; }
        public int? EquipmentId { get; set; }
        public DateTime? OccurFrom { get; set; }
        public DateTime? OccurTo { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AlarmDefinitionFilter
    {
        public string? Keyword { get; set; }
        public AlarmType? AlarmType { get; set; }
        public AlarmPriority? Priority { get; set; }
        public AlarmCategory? Category { get; set; }
        public bool? IsEnabled { get; set; }
        public int? EquipmentId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
    }

    #endregion

    #region 报警关联/趋势分析模型

    /// <summary>
    /// 报警关联分析结果
    /// </summary>
    public class AlarmCorrelation
    {
        public int DefinitionId1 { get; set; }
        public int DefinitionId2 { get; set; }
        public string AlarmName1 { get; set; } = "";
        public string AlarmName2 { get; set; } = "";
        public string TagName1 { get; set; } = "";
        public string TagName2 { get; set; } = "";
        public int CoOccurrenceCount { get; set; }
        public int TotalOccurrences1 { get; set; }
        public int TotalOccurrences2 { get; set; }
        public double Support { get; set; }
        public double Confidence { get; set; }
        public double Lift { get; set; }
        public double Conviction { get; set; }
    }

    /// <summary>
    /// 报警关联规则
    /// </summary>
    public class AlarmAssociationRule
    {
        public string Antecedent { get; set; } = "";
        public string Consequent { get; set; } = "";
        public double Support { get; set; }
        public double Confidence { get; set; }
        public double Lift { get; set; }
        public double Conviction { get; set; }
    }

    /// <summary>
    /// 报警趋势统计
    /// </summary>
    public class AlarmTrendStatistics
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int IntervalMinutes { get; set; }
        public int TotalAlarms { get; set; }
        public double AcknowledgmentRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public List<AlarmTimePoint> TimeSeries { get; set; } = new();
        public List<TopAlarm> TopAlarms { get; set; } = new();
        public Dictionary<AlarmPriority, int> PriorityDistribution { get; set; } = new();
        public Dictionary<AlarmType, int> TypeDistribution { get; set; } = new();
        public Dictionary<AlarmCategory, int> CategoryDistribution { get; set; } = new();
    }

    /// <summary>
    /// 报警时间点统计
    /// </summary>
    public class AlarmTimePoint
    {
        public DateTime Time { get; set; }
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int AcknowledgedCount { get; set; }
        public int ReturnedCount { get; set; }
        public Dictionary<AlarmPriority, int> ByPriority { get; set; } = new();
        public Dictionary<AlarmType, int> ByType { get; set; } = new();
        public Dictionary<AlarmCategory, int> ByCategory { get; set; } = new();
        public List<TopAlarm> TopAlarms { get; set; } = new();
    }

    /// <summary>
    /// 顶部报警
    /// </summary>
    public class TopAlarm
    {
        public int DefinitionId { get; set; }
        public string AlarmName { get; set; } = "";
        public string TagName { get; set; } = "";
        public int Count { get; set; }
        public AlarmPriority Priority { get; set; }
        public TimeSpan AvgDuration { get; set; }
    }

    #endregion

    #region 导出配置模型

    /// <summary>
    /// SMTP 邮件配置
    /// </summary>
    public class SmtpConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public bool EnableSsl { get; set; } = true;
    }

    /// <summary>
    /// 短信网关配置
    /// </summary>
    public class SmsGatewayConfig
    {
        public string ApiUrl { get; set; } = "";
        public string SignName { get; set; } = "";
        public string TemplateCode { get; set; } = "";
        public string AppKey { get; set; } = "";
        public string AppSecret { get; set; } = "";
    }

    /// <summary>
    /// Webhook 配置
    /// </summary>
    public class WebhookConfig
    {
        public string Url { get; set; } = "";
        public Dictionary<string, string> Headers { get; set; } = new();
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// 语音网关配置
    /// </summary>
    public class VoiceGatewayConfig
    {
        public string ApiUrl { get; set; } = "";
        public string Voice { get; set; } = "zh-CN-XiaoxiaoNeural";
        public string ApiKey { get; set; } = "";
    }

    #endregion
}
