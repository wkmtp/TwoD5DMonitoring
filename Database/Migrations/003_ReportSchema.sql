-- =====================================================
-- TwoD5DMonitoring 报表系统数据库迁移脚本 - SQL Server
-- 版本: 1.3.0
-- =====================================================

-- 1. 历史趋势数据表 (分区表，按月分区)
CREATE TABLE HistoricalTrendData (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    TagName NVARCHAR(100) NOT NULL,
    Value FLOAT NOT NULL,
    Quality INT NOT NULL DEFAULT 192,
    Timestamp DATETIME2 NOT NULL
) ON PartitionScheme_Monthly(Timestamp);

CREATE INDEX IX_HistoricalTrendData_Tag_Time ON HistoricalTrendData (TagName, Timestamp DESC);
CREATE INDEX IX_HistoricalTrendData_Timestamp ON HistoricalTrendData (Timestamp DESC);

-- 2. 班次定义表
CREATE TABLE ShiftDefinitions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Color NVARCHAR(20) DEFAULT '#2196F3',
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- 初始化默认班次
INSERT INTO ShiftDefinitions (Name, StartTime, EndTime, Color) VALUES
('白班', '08:00:00', '20:00:00', '#FF9800'),
('夜班', '20:00:00', '08:00:00', '#3F51B5');

-- 3. 生产报表主表
CREATE TABLE ProductionReports (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ReportNumber NVARCHAR(50) NOT NULL UNIQUE,
    ReportType INT NOT NULL DEFAULT 2, -- 1=Shift, 2=Daily, 3=Monthly, 4=Yearly
    PeriodStart DATETIME2 NOT NULL,
    PeriodEnd DATETIME2 NOT NULL,
    ShiftId INT,
    ShiftName NVARCHAR(50),
    Status INT NOT NULL DEFAULT 1, -- 1=Draft, 2=Generated, 3=Approved, 4=Published, 5=Archived
    GeneratedBy NVARCHAR(100),
    GeneratedAt DATETIME2 DEFAULT GETDATE(),
    ApprovedAt DATETIME2,
    ApprovedBy NVARCHAR(100),
    SummaryDataJson NVARCHAR(MAX) DEFAULT '{}',
    DetailDataJson NVARCHAR(MAX) DEFAULT '{}',
    CONSTRAINT FK_ProductionReports_Shift FOREIGN KEY (ShiftId) REFERENCES ShiftDefinitions(Id)
);

CREATE INDEX IX_ProductionReports_Type_Period ON ProductionReports (ReportType, PeriodStart DESC);
CREATE INDEX IX_ProductionReports_Status ON ProductionReports (Status);
CREATE INDEX IX_ProductionReports_Shift ON ProductionReports (ShiftId);

-- 4. OEE 结果表
CREATE TABLE OeeResults (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EquipmentCode NVARCHAR(50) NOT NULL,
    EquipmentName NVARCHAR(100),
    PeriodStart DATETIME2 NOT NULL,
    PeriodEnd DATETIME2 NOT NULL,
    Availability DECIMAL(6,2) NOT NULL DEFAULT 0,
    Performance DECIMAL(6,2) NOT NULL DEFAULT 0,
    Quality DECIMAL(6,2) NOT NULL DEFAULT 0,
    Oee DECIMAL(6,2) NOT NULL DEFAULT 0,
    PlannedProductionTime DECIMAL(18,2) DEFAULT 0, -- 分钟
    ActualRunTime DECIMAL(18,2) DEFAULT 0,
    Downtime DECIMAL(18,2) DEFAULT 0,
    IdealRunRate DECIMAL(18,4) DEFAULT 0,
    ActualOutput DECIMAL(18,4) DEFAULT 0,
    GoodOutput DECIMAL(18,4) DEFAULT 0,
    DefectOutput DECIMAL(18,4) DEFAULT 0,
    DowntimeRecordsJson NVARCHAR(MAX) DEFAULT '[]',
    LossCategoriesJson NVARCHAR(MAX) DEFAULT '[]',
    CalculatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE INDEX IX_OeeResults_Equipment_Period ON OeeResults (EquipmentCode, PeriodStart DESC);
CREATE INDEX IX_OeeResults_Period ON OeeResults (PeriodStart DESC);

-- 5. 停机记录表 (可选独立存储)
CREATE TABLE DowntimeRecords (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    EquipmentCode NVARCHAR(50) NOT NULL,
    EquipmentName NVARCHAR(100),
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2,
    Reason NVARCHAR(200),
    Category NVARCHAR(50),
    DurationMinutes DECIMAL(18,2),
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE INDEX IX_DowntimeRecords_Equipment_Time ON DowntimeRecords (EquipmentCode, StartTime DESC);

-- 6. 能耗记录表
CREATE TABLE EnergyRecords (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    MeterCode NVARCHAR(50) NOT NULL,
    MeterName NVARCHAR(100) NOT NULL,
    EnergyType INT NOT NULL DEFAULT 1, -- 1=Electricity, 2=Gas, 3=Water, 4=Steam, 5=CompressedAir
    Reading DECIMAL(18,4) NOT NULL,
    Consumption DECIMAL(18,4) NOT NULL DEFAULT 0,
    Cost DECIMAL(18,4) DEFAULT 0,
    Timestamp DATETIME2 NOT NULL,
    RecordedAt DATETIME2 DEFAULT GETDATE()
);

CREATE INDEX IX_EnergyRecords_Meter_Time ON EnergyRecords (MeterCode, Timestamp DESC);
CREATE INDEX IX_EnergyRecords_EnergyType_Time ON EnergyRecords (EnergyType, Timestamp DESC);
CREATE INDEX IX_EnergyRecords_Timestamp ON EnergyRecords (Timestamp DESC);

-- 7. 能耗统计缓存表
CREATE TABLE EnergyStatisticsCache (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EnergyType INT NOT NULL,
    MeterCode NVARCHAR(50),
    PeriodStart DATETIME2 NOT NULL,
    PeriodEnd DATETIME2 NOT NULL,
    PeriodType INT NOT NULL, -- 1=Minute, 2=Hour, 3=Shift, 4=Day, 5=Month
    TotalConsumption DECIMAL(18,4) DEFAULT 0,
    TotalCost DECIMAL(18,4) DEFAULT 0,
    AvgConsumptionPerHour DECIMAL(18,4) DEFAULT 0,
    PeakConsumption DECIMAL(18,4) DEFAULT 0,
    PeakTime DATETIME2,
    MinConsumption DECIMAL(18,4) DEFAULT 0,
    MinTime DATETIME2,
    ByEquipmentJson NVARCHAR(MAX) DEFAULT '{}',
    ByAreaJson NVARCHAR(MAX) DEFAULT '{}',
    HourlyDataJson NVARCHAR(MAX) DEFAULT '{}',
    DailyDataJson NVARCHAR(MAX) DEFAULT '{}',
    ConsumptionPerTon DECIMAL(18,4) DEFAULT 0,
    ConsumptionPerBatch DECIMAL(18,4) DEFAULT 0,
    YoYChangePercent DECIMAL(10,2) DEFAULT 0,
    MoMChangePercent DECIMAL(10,2) DEFAULT 0,
    CalculatedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT UQ_EnergyStatisticsCache UNIQUE (EnergyType, MeterCode, PeriodStart, PeriodEnd, PeriodType)
);

CREATE INDEX IX_EnergyStatisticsCache_Type_Period ON EnergyStatisticsCache (EnergyType, PeriodStart DESC);

-- 8. 报表模板表
CREATE TABLE ReportTemplates (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    ReportType INT NOT NULL DEFAULT 2,
    TemplateContent NVARCHAR(MAX),
    IsSystem BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy NVARCHAR(100)
);

-- 9. 报表生成任务表
CREATE TABLE ReportGenerationTasks (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TaskName NVARCHAR(100) NOT NULL,
    TemplateId INT,
    ReportType INT NOT NULL DEFAULT 2,
    ScheduledTime DATETIME2 NOT NULL,
    StartTime DATETIME2,
    EndTime DATETIME2,
    Status INT NOT NULL DEFAULT 1, -- 1=Pending, 2=Running, 3=Completed, 4=Failed, 5=Cancelled
    ErrorMessage NVARCHAR(1000),
    OutputPath NVARCHAR(500),
    ParametersJson NVARCHAR(MAX) DEFAULT '{}',
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_ReportGenerationTasks_Template FOREIGN KEY (TemplateId) REFERENCES ReportTemplates(Id)
);

CREATE INDEX IX_ReportGenerationTasks_Scheduled ON ReportGenerationTasks (ScheduledTime, Status);

-- 10. 报表模板初始化数据
INSERT INTO ReportTemplates (Name, Description, ReportType, TemplateContent, IsSystem, CreatedBy) VALUES
('标准班报模板', '标准生产班报，包含产量、能耗、质量、设备运行', 1, 
'{"sections":[{"title":"生产概况","type":"summary"},{"title":"批次明细","type":"batches"},{"title":"设备运行","type":"equipment"},{"title":"能耗统计","type":"energy"},{"title":"质量分析","type":"quality"},{"title":"报警汇总","type":"alarms"}]}', 1, 'SYSTEM'),

('标准日报模板', '标准生产日报，包含全天汇总', 2,
'{"sections":[{"title":"生产概况","type":"summary"},{"title":"班次对比","type":"shift_comparison"},{"title":"批次明细","type":"batches"},{"title":"设备运行","type":"equipment"},{"title":"能耗统计","type":"energy"},{"title":"质量分析","type":"quality"},{"title":"报警汇总","type":"alarms"}]}', 1, 'SYSTEM'),

('标准月报模板', '标准生产月报，包含趋势分析', 3,
'{"sections":[{"title":"月度概况","type":"summary"},{"title":"日产量趋势","type":"daily_trend"},{"title":"设备OEE趋势","type":"oee_trend"},{"title":"能耗趋势","type":"energy_trend"},{"title":"质量趋势","type":"quality_trend"},{"title":"Top问题分析","type":"top_issues"}]}', 1, 'SYSTEM'),

('OEE分析报表', '设备OEE详细分析报表', 2,
'{"sections":[{"title":"OEE概况","type":"oee_summary"},{"title":"可用率分析","type":"availability"},{"title":"性能分析","type":"performance"},{"title":"质量分析","type":"quality_rate"},{"title":"六大损失分析","type":"six_losses"},{"title":"停机原因分析","type":"downtime_analysis"}]}', 1, 'SYSTEM'),

('能耗分析报表', '能耗详细分析报表', 2,
'{"sections":[{"title":"能耗概况","type":"energy_summary"},{"title":"分项能耗","type":"energy_breakdown"},{"title":"能耗趋势","type":"energy_trend"},{"title":"单耗分析","type":"specific_consumption"},{"title":"同比环比分析","type":"yoy_mom"},{"title":"节能建议","type":"saving_suggestions"}]}', 1, 'SYSTEM');

-- 完成
PRINT 'TwoD5DMonitoring 报表系统数据库迁移完成 (SQL Server)';