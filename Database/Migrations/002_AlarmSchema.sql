-- =====================================================
-- TwoD5DMonitoring 报警管理数据库迁移脚本 - SQL Server
-- 版本: 1.2.0
-- =====================================================

-- 1. 报警组表
CREATE TABLE AlarmGroups (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500),
    IsEnabled BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- 2. 报警定义表
CREATE TABLE AlarmDefinitions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TagName NVARCHAR(100) NOT NULL,
    AlarmName NVARCHAR(100) NOT NULL,
    AlarmType INT NOT NULL DEFAULT 1,
    Priority INT NOT NULL DEFAULT 2,
    Category INT NOT NULL DEFAULT 1,
    Setpoint FLOAT NOT NULL DEFAULT 0,
    Deadband FLOAT DEFAULT 0.5,
    DelaySeconds INT DEFAULT 0,
    Message NVARCHAR(500),
    HelpText NVARCHAR(1000),
    IsEnabled BIT DEFAULT 1,
    RequireAck BIT DEFAULT 1,
    LogToHistory BIT DEFAULT 1,
    PrintAlarm BIT DEFAULT 0,
    PlcTagAddress NVARCHAR(200),
    EquipmentId INT,
    GroupId INT,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_AlarmDefinitions_Group FOREIGN KEY (GroupId) REFERENCES AlarmGroups(Id)
);

CREATE INDEX IX_AlarmDefinitions_TagName ON AlarmDefinitions(TagName);
CREATE INDEX IX_AlarmDefinitions_EquipmentId ON AlarmDefinitions(EquipmentId);
CREATE INDEX IX_AlarmDefinitions_IsEnabled ON AlarmDefinitions(IsEnabled);
CREATE INDEX IX_AlarmDefinitions_AlarmType ON AlarmDefinitions(AlarmType);
CREATE INDEX IX_AlarmDefinitions_Priority ON AlarmDefinitions(Priority);

-- 3. 报警分组成员表
CREATE TABLE AlarmGroupMembers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    GroupId INT NOT NULL,
    AlarmDefinitionId INT NOT NULL,
    CONSTRAINT FK_AlarmGroupMembers_Group FOREIGN KEY (GroupId) REFERENCES AlarmGroups(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AlarmGroupMembers_AlarmDefinition FOREIGN KEY (AlarmDefinitionId) REFERENCES AlarmDefinitions(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX UQ_AlarmGroupMembers_Group_Alarm ON AlarmGroupMembers(GroupId, AlarmDefinitionId);

-- 4. 报警实例表 (运行时活动报警)
CREATE TABLE AlarmInstances (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DefinitionId INT NOT NULL,
    State INT NOT NULL DEFAULT 1,
    CurrentValue FLOAT NOT NULL DEFAULT 0,
    Setpoint FLOAT NOT NULL DEFAULT 0,
    OccurTime DATETIME2 NOT NULL DEFAULT GETDATE(),
    AckTime DATETIME2,
    ReturnTime DATETIME2,
    AckedBy NVARCHAR(100),
    Comment NVARCHAR(500),
    OccurrenceCount INT DEFAULT 1,
    IsShelved BIT DEFAULT 0,
    CONSTRAINT FK_AlarmInstances_Definition FOREIGN KEY (DefinitionId) REFERENCES AlarmDefinitions(Id)
);

CREATE INDEX IX_AlarmInstances_DefinitionId ON AlarmInstances(DefinitionId);
CREATE INDEX IX_AlarmInstances_State ON AlarmInstances(State);
CREATE INDEX IX_AlarmInstances_OccurTime ON AlarmInstances(OccurTime DESC);
CREATE INDEX IX_AlarmInstances_IsShelved ON AlarmInstances(IsShelved);

-- 5. 历史报警记录表
CREATE TABLE AlarmHistory (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DefinitionId INT NOT NULL,
    FinalState INT NOT NULL,
    OccurValue FLOAT NOT NULL DEFAULT 0,
    ReturnValue FLOAT NOT NULL DEFAULT 0,
    OccurTime DATETIME2 NOT NULL DEFAULT GETDATE(),
    AckTime DATETIME2,
    ReturnTime DATETIME2,
    AckedBy NVARCHAR(100),
    Comment NVARCHAR(500),
    OccurrenceCount INT DEFAULT 1,
    TotalActiveTime BIGINT, -- TimeSpan ticks
    -- 冗余字段
    TagName NVARCHAR(100),
    AlarmName NVARCHAR(100),
    Priority INT,
    AlarmType INT,
    EquipmentName NVARCHAR(100),
    CONSTRAINT FK_AlarmHistory_Definition FOREIGN KEY (DefinitionId) REFERENCES AlarmDefinitions(Id)
);

CREATE INDEX IX_AlarmHistory_DefinitionId ON AlarmHistory(DefinitionId);
CREATE INDEX IX_AlarmHistory_OccurTime ON AlarmHistory(OccurTime DESC);
CREATE INDEX IX_AlarmHistory_Priority ON AlarmHistory(Priority);
CREATE INDEX IX_AlarmHistory_AlarmType ON AlarmHistory(AlarmType);
CREATE INDEX IX_AlarmHistory_EquipmentName ON AlarmHistory(EquipmentName);

-- 6. 报警通知配置表
CREATE TABLE AlarmNotifications (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Recipients NVARCHAR(MAX), -- JSON
    Method INT NOT NULL DEFAULT 1,
    MinPriority INT NOT NULL DEFAULT 2,
    IsEnabled BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- 7. 报警抑制规则表
CREATE TABLE AlarmSuppressions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Type INT NOT NULL DEFAULT 1,
    StartTime TIME NOT NULL DEFAULT '00:00:00',
    EndTime TIME NOT NULL DEFAULT '23:59:59',
    DaysOfWeekJson NVARCHAR(MAX), -- JSON
    TagFilter NVARCHAR(500),
    EquipmentId INT,
    IsEnabled BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- 触发器：更新 UpdatedAt
CREATE TRIGGER TR_AlarmDefinitions_UpdatedAt
ON AlarmDefinitions
AFTER UPDATE
AS
BEGIN
    UPDATE AlarmDefinitions SET UpdatedAt = GETDATE()
    FROM AlarmDefinitions d
    INNER JOIN inserted i ON d.Id = i.Id;
END;

-- 初始化默认报警组
INSERT INTO AlarmGroups (Name, Description, IsEnabled) VALUES
('炭黑系统', '炭黑输送、储存相关报警', 1),
('油品系统', '油品储存、供油相关报警', 1),
('混炼系统', '混炼机、秤量相关报警', 1),
('通用设备', '风机、泵、阀门等通用设备报警', 1),
('系统报警', '通讯、数据库、系统级报警', 1);

-- 完成
PRINT 'TwoD5DMonitoring 报警管理数据库迁移完成 (SQL Server)';