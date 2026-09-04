using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace TwoD5DMonitoring.Models
{
    public class EnergyStatisticsCache : INotifyPropertyChanged
    {
        private int _energyType;
        private string? _meterCode;
        private DateTime _periodStart;
        private DateTime _periodEnd;
        private int _periodType;
        private decimal _totalConsumption;
        private decimal _totalCost;
        private decimal _avgConsumptionPerHour;
        private decimal _peakConsumption;
        private DateTime? _peakTime;
        private decimal _minConsumption;
        private DateTime? _minTime;
        private string? _byEquipmentJson;
        private string? _byAreaJson;
        private string? _hourlyDataJson;
        private string? _dailyDataJson;
        private decimal _consumptionPerTon;
        private decimal _consumptionPerBatch;
        private decimal _yoyChangePercent;
        private decimal _momChangePercent;
        private DateTime _calculatedAt;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int EnergyType
        {
            get => _energyType;
            set { _energyType = value; OnPropertyChanged(nameof(EnergyType)); }
        }

        [MaxLength(50)]
        public string? MeterCode
        {
            get => _meterCode;
            set { _meterCode = value; OnPropertyChanged(nameof(MeterCode)); }
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

        public int PeriodType
        {
            get => _periodType;
            set { _periodType = value; OnPropertyChanged(nameof(PeriodType)); }
        }

        public decimal TotalConsumption
        {
            get => _totalConsumption;
            set { _totalConsumption = value; OnPropertyChanged(nameof(TotalConsumption)); }
        }

        public decimal TotalCost
        {
            get => _totalCost;
            set { _totalCost = value; OnPropertyChanged(nameof(TotalCost)); }
        }

        public decimal AvgConsumptionPerHour
        {
            get => _avgConsumptionPerHour;
            set { _avgConsumptionPerHour = value; OnPropertyChanged(nameof(AvgConsumptionPerHour)); }
        }

        public decimal PeakConsumption
        {
            get => _peakConsumption;
            set { _peakConsumption = value; OnPropertyChanged(nameof(PeakConsumption)); }
        }

        public DateTime? PeakTime
        {
            get => _peakTime;
            set { _peakTime = value; OnPropertyChanged(nameof(PeakTime)); }
        }

        public decimal MinConsumption
        {
            get => _minConsumption;
            set { _minConsumption = value; OnPropertyChanged(nameof(MinConsumption)); }
        }

        public DateTime? MinTime
        {
            get => _minTime;
            set { _minTime = value; OnPropertyChanged(nameof(MinTime)); }
        }

        public string? ByEquipmentJson
        {
            get => _byEquipmentJson;
            set { _byEquipmentJson = value; OnPropertyChanged(nameof(ByEquipmentJson)); }
        }

        public string? ByAreaJson
        {
            get => _byAreaJson;
            set { _byAreaJson = value; OnPropertyChanged(nameof(ByAreaJson)); }
        }

        public string? HourlyDataJson
        {
            get => _hourlyDataJson;
            set { _hourlyDataJson = value; OnPropertyChanged(nameof(HourlyDataJson)); }
        }

        public string? DailyDataJson
        {
            get => _dailyDataJson;
            set { _dailyDataJson = value; OnPropertyChanged(nameof(DailyDataJson)); }
        }

        public decimal ConsumptionPerTon
        {
            get => _consumptionPerTon;
            set { _consumptionPerTon = value; OnPropertyChanged(nameof(ConsumptionPerTon)); }
        }

        public decimal ConsumptionPerBatch
        {
            get => _consumptionPerBatch;
            set { _consumptionPerBatch = value; OnPropertyChanged(nameof(ConsumptionPerBatch)); }
        }

        public decimal YoYChangePercent
        {
            get => _yoyChangePercent;
            set { _yoyChangePercent = value; OnPropertyChanged(nameof(YoYChangePercent)); }
        }

        public decimal MoMChangePercent
        {
            get => _momChangePercent;
            set { _momChangePercent = value; OnPropertyChanged(nameof(MoMChangePercent)); }
        }

        public DateTime CalculatedAt
        {
            get => _calculatedAt;
            set { _calculatedAt = value; OnPropertyChanged(nameof(CalculatedAt)); }
        }

        [NotMapped]
        public Dictionary<string, decimal> ByEquipment
        {
            get => string.IsNullOrEmpty(_byEquipmentJson) ? new Dictionary<string, decimal>() : Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, decimal>>(_byEquipmentJson)!;
            set => _byEquipmentJson = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }

        [NotMapped]
        public Dictionary<string, decimal> ByArea
        {
            get => string.IsNullOrEmpty(_byAreaJson) ? new Dictionary<string, decimal>() : Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, decimal>>(_byAreaJson)!;
            set => _byAreaJson = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }

        [NotMapped]
        public Dictionary<DateTime, decimal> HourlyData
        {
            get => string.IsNullOrEmpty(_hourlyDataJson) ? new Dictionary<DateTime, decimal>() : Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<DateTime, decimal>>(_hourlyDataJson)!;
            set => _hourlyDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }

        [NotMapped]
        public Dictionary<DateTime, decimal> DailyData
        {
            get => string.IsNullOrEmpty(_dailyDataJson) ? new Dictionary<DateTime, decimal>() : Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<DateTime, decimal>>(_dailyDataJson)!;
            set => _dailyDataJson = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}