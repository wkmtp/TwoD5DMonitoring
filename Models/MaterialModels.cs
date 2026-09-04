using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace TwoD5DMonitoring.Models
{
    #region 物料分类

    /// <summary>
    /// 物料分类
    /// </summary>
    public class MaterialCategory : INotifyPropertyChanged
    {
        private string _name = "";
        private string _code = "";
        private string _description = "";
        private int _sortOrder;
        private bool _isActive = true;

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

        [MaxLength(500)]
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public int SortOrder
        {
            get => _sortOrder;
            set { _sortOrder = value; OnPropertyChanged(nameof(SortOrder)); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string UpdatedBy { get; set; } = "";

        // 导航属性
        public virtual ObservableCollection<Material> Materials { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region 物料基础信息

    /// <summary>
    /// 物料类型枚举
    /// </summary>
    public enum MaterialType
    {
        RawMaterial = 1,      // 原材料
        SemiFinished = 2,     // 半成品
        FinishedProduct = 3,  // 成品
        Additive = 4,         // 添加剂
        Packaging = 5,        // 包装材料
        Other = 99            // 其他
    }

    /// <summary>
    /// 物料状态枚举
    /// </summary>
    public enum MaterialStatus
    {
        Active = 1,       // 启用
        Inactive = 2,     // 停用
        Obsolete = 3      // 废弃
    }

    /// <summary>
    /// 物料基础信息
    /// </summary>
    public class Material : INotifyPropertyChanged
    {
        private string _code = "";
        private string _name = "";
        private string _specification = "";
        private string _unit = "";
        private MaterialType _materialType = MaterialType.RawMaterial;
        private MaterialStatus _status = MaterialStatus.Active;
        private int _categoryId;
        private decimal _density;
        private decimal _moistureContent;
        private string _particleSize = "";
        private string _color = "";
        private string _manufacturer = "";
        private string _supplier = "";
        private decimal _standardCost;
        private string _remark = "";
        private bool _isWeighable = true;
        private bool _isBatchManaged = false;
        private int? _shelfLifeDays;
        private string _storageCondition = "";

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

        [MaxLength(200)]
        public string Specification
        {
            get => _specification;
            set { _specification = value; OnPropertyChanged(nameof(Specification)); }
        }

        [MaxLength(20)]
        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(nameof(Unit)); }
        }

        public MaterialType MaterialType
        {
            get => _materialType;
            set { _materialType = value; OnPropertyChanged(nameof(MaterialType)); }
        }

        public MaterialStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public int CategoryId
        {
            get => _categoryId;
            set { _categoryId = value; OnPropertyChanged(nameof(CategoryId)); }
        }

        [NotMapped]
        public string CategoryName { get; set; } = "";

        public decimal Density
        {
            get => _density;
            set { _density = value; OnPropertyChanged(nameof(Density)); }
        }

        public decimal MoistureContent
        {
            get => _moistureContent;
            set { _moistureContent = value; OnPropertyChanged(nameof(MoistureContent)); }
        }

        [MaxLength(50)]
        public string ParticleSize
        {
            get => _particleSize;
            set { _particleSize = value; OnPropertyChanged(nameof(ParticleSize)); }
        }

        [MaxLength(50)]
        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(nameof(Color)); }
        }

        [MaxLength(100)]
        public string Manufacturer
        {
            get => _manufacturer;
            set { _manufacturer = value; OnPropertyChanged(nameof(Manufacturer)); }
        }

        [MaxLength(100)]
        public string Supplier
        {
            get => _supplier;
            set { _supplier = value; OnPropertyChanged(nameof(Supplier)); }
        }

        public decimal StandardCost
        {
            get => _standardCost;
            set { _standardCost = value; OnPropertyChanged(nameof(StandardCost)); }
        }

        [MaxLength(500)]
        public string Remark
        {
            get => _remark;
            set { _remark = value; OnPropertyChanged(nameof(Remark)); }
        }

        public bool IsWeighable
        {
            get => _isWeighable;
            set { _isWeighable = value; OnPropertyChanged(nameof(IsWeighable)); }
        }

        public bool IsBatchManaged
        {
            get => _isBatchManaged;
            set { _isBatchManaged = value; OnPropertyChanged(nameof(IsBatchManaged)); }
        }

        public int? ShelfLifeDays
        {
            get => _shelfLifeDays;
            set { _shelfLifeDays = value; OnPropertyChanged(nameof(ShelfLifeDays)); }
        }

        [MaxLength(500)]
        public string StorageCondition
        {
            get => _storageCondition;
            set { _storageCondition = value; OnPropertyChanged(nameof(StorageCondition)); }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string UpdatedBy { get; set; } = "";

        // 导航属性
        [ForeignKey(nameof(CategoryId))]
        public virtual MaterialCategory? Category { get; set; }

        public virtual ObservableCollection<MaterialProperty> Properties { get; set; } = new();
        public virtual ObservableCollection<TankMaterialAssignment> TankMaterials { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region 物料属性

    /// <summary>
    /// 物料属性
    /// </summary>
    public class MaterialProperty : INotifyPropertyChanged
    {
        private int _materialId;
        private string _name = "";
        private string _value = "";
        private string _unit = "";
        private string _dataType = "";
        private int _sortOrder;
        private bool _isRequired;
        private string _validationRule = "";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int MaterialId
        {
            get => _materialId;
            set { _materialId = value; OnPropertyChanged(nameof(MaterialId)); }
        }

        [Required]
        [MaxLength(100)]
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        [MaxLength(500)]
        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(nameof(Value)); }
        }

        [MaxLength(20)]
        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(nameof(Unit)); }
        }

        [MaxLength(50)]
        public string DataType
        {
            get => _dataType;
            set { _dataType = value; OnPropertyChanged(nameof(DataType)); }
        }

        public int SortOrder
        {
            get => _sortOrder;
            set { _sortOrder = value; OnPropertyChanged(nameof(SortOrder)); }
        }

        public bool IsRequired
        {
            get => _isRequired;
            set { _isRequired = value; OnPropertyChanged(nameof(IsRequired)); }
        }

        [MaxLength(500)]
        public string ValidationRule
        {
            get => _validationRule;
            set { _validationRule = value; OnPropertyChanged(nameof(ValidationRule)); }
        }

        [ForeignKey(nameof(MaterialId))]
        public virtual Material? Material { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region 罐体物料分配

    /// <summary>
    /// 罐体物料分配记录
    /// </summary>
    public class TankMaterialAssignment : INotifyPropertyChanged
    {
        private int _tankId;
        private int _materialId;
        private bool _isPrimary = true;        // 是否主物料
        private int _priority = 1;             // 优先级
        private decimal _maxCapacity;          // 最大容量
        private decimal _minLevel;             // 最低液位
        private decimal _maxLevel;             // 最高液位
        private string _remark = "";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int TankId
        {
            get => _tankId;
            set { _tankId = value; OnPropertyChanged(nameof(TankId)); }
        }

        [MaxLength(50)]
        public string TankCode { get; set; } = "";

        [MaxLength(100)]
        public string TankName { get; set; } = "";

        [MaxLength(50)]
        public string TankType { get; set; } = "";

        public int MaterialId
        {
            get => _materialId;
            set { _materialId = value; OnPropertyChanged(nameof(MaterialId)); }
        }

        public bool IsPrimary
        {
            get => _isPrimary;
            set { _isPrimary = value; OnPropertyChanged(nameof(IsPrimary)); }
        }

        public int Priority
        {
            get => _priority;
            set { _priority = value; OnPropertyChanged(nameof(Priority)); }
        }

        public decimal MaxCapacity
        {
            get => _maxCapacity;
            set { _maxCapacity = value; OnPropertyChanged(nameof(MaxCapacity)); }
        }

        public decimal MinLevel
        {
            get => _minLevel;
            set { _minLevel = value; OnPropertyChanged(nameof(MinLevel)); }
        }

        public decimal MaxLevel
        {
            get => _maxLevel;
            set { _maxLevel = value; OnPropertyChanged(nameof(MaxLevel)); }
        }

        [MaxLength(500)]
        public string Remark
        {
            get => _remark;
            set { _remark = value; OnPropertyChanged(nameof(Remark)); }
        }

        public DateTime EffectiveFrom { get; set; } = DateTime.Now;
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string UpdatedBy { get; set; } = "";

        // 导航属性
        [ForeignKey(nameof(MaterialId))]
        public virtual Material? Material { get; set; }

        // 罐体信息 (冗余字段，用于显示)
        [NotMapped]
        public string? MaterialName => Material?.Name ?? "";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // 别名，保持向后兼容
    public class TankMaterial : TankMaterialAssignment { }

    /// <summary>
    /// 罐体物料变更历史
    /// </summary>
    public class TankMaterialHistory : INotifyPropertyChanged
    {
        private int _tankId;
        private int _oldMaterialId;
        private int _newMaterialId;
        private string _changeReason = "";
        private string _operator = "";

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int TankId
        {
            get => _tankId;
            set { _tankId = value; OnPropertyChanged(nameof(TankId)); }
        }

        public int OldMaterialId
        {
            get => _oldMaterialId;
            set { _oldMaterialId = value; OnPropertyChanged(nameof(OldMaterialId)); }
        }

        public int NewMaterialId
        {
            get => _newMaterialId;
            set { _newMaterialId = value; OnPropertyChanged(nameof(NewMaterialId)); }
        }

        [MaxLength(500)]
        public string ChangeReason
        {
            get => _changeReason;
            set { _changeReason = value; OnPropertyChanged(nameof(ChangeReason)); }
        }

        [MaxLength(50)]
        public string Operator
        {
            get => _operator;
            set { _operator = value; OnPropertyChanged(nameof(Operator)); }
        }

        public DateTime ChangeTime { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string TankCode { get; set; } = "";

        [MaxLength(100)]
        public string TankName { get; set; } = "";

        // 导航属性
        [ForeignKey(nameof(OldMaterialId))]
        public virtual Material? OldMaterial { get; set; }
        [ForeignKey(nameof(NewMaterialId))]
        public virtual Material? NewMaterial { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region 服务辅助模型

    public class ImportResult
    {
        public bool Success { get; set; }
        public int ImportedCount { get; set; }
        public int FailedCount { get; set; }
        public string Message { get; set; } = "";
        public List<string> Errors { get; set; } = new();
    }

    public class BatchOperationResult
    {
        public bool Success { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public string Message { get; set; } = "";
        public List<string> Errors { get; set; } = new();
    }

    public class InventoryAlert
    {
        public enum AlertType
        {
            LowStock,
            Expired,
            TankLowLevel,
            TankHighLevel,
            QualityIssue
        }

        public AlertType Type { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string EquipmentCode { get; set; } = "";
        public int MaterialId { get; set; }
        public int TankId { get; set; }
        public DateTime AlertTime { get; set; } = DateTime.Now;
        public bool IsAcknowledged { get; set; }
        public string AcknowledgedBy { get; set; } = "";
    }

    #endregion
}