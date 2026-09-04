-- =====================================================
-- TwoD5DMonitoring 数据库迁移脚本
-- 版本: 1.1.0 - 物料管理与配方管理模块
-- 适用: SQL Server / PostgreSQL / MySQL
-- =====================================================

-- 1. 物料分类表
CREATE TABLE MaterialCategories (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    SortOrder INT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE INDEX IX_MaterialCategories_Code ON MaterialCategories(Code);
CREATE INDEX IX_MaterialCategories_IsActive ON MaterialCategories(IsActive);

-- 2. 物料主数据表
CREATE TABLE Materials (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    Specification NVARCHAR(200),
    Unit NVARCHAR(20) NOT NULL,
    MaterialType INT NOT NULL DEFAULT 1, -- 1=原材料, 2=半成品, 3=成品, 4=添加剂, 5=包材, 99=其他
    Status INT NOT NULL DEFAULT 1,       -- 1=启用, 2=停用, 3=废弃, 4=待审核
    CategoryId INT,
    Density DECIMAL(10,4) DEFAULT 0,
    MoistureContent DECIMAL(6,2) DEFAULT 0,
    ParticleSize DECIMAL(10,2) DEFAULT 0,
    Color NVARCHAR(50),
    Manufacturer NVARCHAR(100),
    Supplier NVARCHAR(100),
    StandardCost DECIMAL(18,4) DEFAULT 0,
    Remark NVARCHAR(1000),
    IsWeighable BIT DEFAULT 1,
    IsBatchManaged BIT DEFAULT 1,
    ShelfLifeDays INT DEFAULT 0,
    StorageCondition NVARCHAR(200),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy NVARCHAR(100),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedBy NVARCHAR(100),
    CONSTRAINT FK_Materials_Category FOREIGN KEY (CategoryId) REFERENCES MaterialCategories(Id)
);

CREATE INDEX IX_Materials_Code ON Materials(Code);
CREATE INDEX IX_Materials_Name ON Materials(Name);
CREATE INDEX IX_Materials_MaterialType ON Materials(MaterialType);
CREATE INDEX IX_Materials_Status ON Materials(Status);
CREATE INDEX IX_Materials_CategoryId ON Materials(CategoryId);
CREATE INDEX IX_Materials_IsWeighable ON Materials(IsWeighable);

-- 3. 物料扩展属性表
CREATE TABLE MaterialProperties (
    Id INT PRIMARY KEY IDENTITY(1,1),
    MaterialId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Value NVARCHAR(500),
    Unit NVARCHAR(20),
    DataType NVARCHAR(20) DEFAULT 'String', -- String, Number, Boolean, Date
    SortOrder INT DEFAULT 0,
    IsRequired BIT DEFAULT 0,
    ValidationRule NVARCHAR(MAX), -- JSON
    CONSTRAINT FK_MaterialProperties_Material FOREIGN KEY (MaterialId) REFERENCES Materials(Id) ON DELETE CASCADE
);

CREATE INDEX IX_MaterialProperties_MaterialId ON MaterialProperties(MaterialId);

-- 4. 罐体物料分配表
CREATE TABLE TankMaterials (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TankId INT NOT NULL,              -- 关联设备ID (EquipmentBase.Id 的哈希或映射)
    TankCode NVARCHAR(50),            // 罐体编码 (冗余)
    TankName NVARCHAR(100),           // 罐体名称 (冗余)
    TankType INT,                     // 罐体类型 (冗余)
    MaterialId INT NOT NULL,
    IsPrimary BIT DEFAULT 1,
    Priority INT DEFAULT 1,
    MaxCapacity DECIMAL(18,4) DEFAULT 0,
    MinLevel DECIMAL(18,4) DEFAULT 0,
    MaxLevel DECIMAL(18,4) DEFAULT 0,
    Remark NVARCHAR(500),
    EffectiveFrom DATETIME2 DEFAULT GETDATE(),
    EffectiveTo DATETIME2,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy NVARCHAR(100),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedBy NVARCHAR(100),
    CONSTRAINT FK_TankMaterials_Material FOREIGN KEY (MaterialId) REFERENCES Materials(Id)
);

CREATE INDEX IX_TankMaterials_TankId ON TankMaterials(TankId);
CREATE INDEX IX_TankMaterials_MaterialId ON TankMaterials(MaterialId);
CREATE INDEX IX_TankMaterials_IsActive ON TankMaterials(IsActive);

-- 5. 罐体物料变更历史表
CREATE TABLE TankMaterialHistory (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TankId INT NOT NULL,
    TankCode NVARCHAR(50),
    TankName NVARCHAR(100),
    OldMaterialId INT,
    NewMaterialId INT,
    ChangeReason NVARCHAR(500),
    Operator NVARCHAR(100),
    ChangeTime DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_TankMaterialHistory_OldMaterial FOREIGN KEY (OldMaterialId) REFERENCES Materials(Id),
    CONSTRAINT FK_TankMaterialHistory_NewMaterial FOREIGN KEY (NewMaterialId) REFERENCES Materials(Id)
);

CREATE INDEX IX_TankMaterialHistory_TankId ON TankMaterialHistory(TankId);
CREATE INDEX IX_TankMaterialHistory_ChangeTime ON TankMaterialHistory(ChangeTime);

-- 6. 配方基础表
CREATE TABLE Recipes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    RecipeType INT NOT NULL DEFAULT 1,  -- 1=称量, 2=混炼, 3=挤出, 4=压延, 99=其他
    Status INT NOT NULL DEFAULT 1,      -- 1=草稿, 2=待审核, 3=已审核, 4=已发布, 5=已归档, 6=作废
    ProductCode NVARCHAR(50),
    ProductName NVARCHAR(100),
    BatchSize DECIMAL(18,4) DEFAULT 1000,
    YieldRate DECIMAL(6,2) DEFAULT 100,
    Remark NVARCHAR(2000),
    CurrentVersion NVARCHAR(50) DEFAULT '1.0',
    CurrentVersionId INT,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy NVARCHAR(100),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedBy NVARCHAR(100),
    ApprovedAt DATETIME2,
    ApprovedBy NVARCHAR(100),
    ReleasedAt DATETIME2,
    ReleasedBy NVARCHAR(100)
);

CREATE INDEX IX_Recipes_Code ON Recipes(Code);
CREATE INDEX IX_Recipes_RecipeType ON Recipes(RecipeType);
CREATE INDEX IX_Recipes_Status ON Recipes(Status);
CREATE INDEX IX_Recipes_ProductCode ON Recipes(ProductCode);

-- 7. 配方版本表
CREATE TABLE RecipeVersions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RecipeId INT NOT NULL,
    Version NVARCHAR(50) NOT NULL,
    ChangeDescription NVARCHAR(1000),
    IsCurrent BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy NVARCHAR(100),
    CONSTRAINT FK_RecipeVersions_Recipe FOREIGN KEY (RecipeId) REFERENCES Recipes(Id) ON DELETE CASCADE
);

CREATE INDEX IX_RecipeVersions_RecipeId ON RecipeVersions(RecipeId);
CREATE INDEX IX_RecipeVersions_IsCurrent ON RecipeVersions(IsCurrent);

-- 8. 称量配方扩展表
CREATE TABLE WeighingRecipes (
    RecipeId INT PRIMARY KEY,
    WeighingMode INT DEFAULT 1,         -- 1=顺序, 2=同时, 3=手工
    ScaleId INT,
    TotalWeight DECIMAL(18,4) DEFAULT 0,
    DecimalPlaces INT DEFAULT 2,
    AllowManualAdjust BIT DEFAULT 1,
    CONSTRAINT FK_WeighingRecipes_Recipe FOREIGN KEY (RecipeId) REFERENCES Recipes(Id) ON DELETE CASCADE
);

-- 9. 称量配方明细行
CREATE TABLE WeighingRecipeLines (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RecipeId INT NOT NULL,
    LineNumber INT NOT NULL,
    MaterialId INT NOT NULL,
    TargetWeight DECIMAL(18,4) NOT NULL,
    TolerancePositive DECIMAL(6,2) DEFAULT 1.0,
    ToleranceNegative DECIMAL(6,2) DEFAULT 1.0,
    WeighingOrder INT DEFAULT 0,
    IsCritical BIT DEFAULT 1,
    Remark NVARCHAR(500),
    FeederType INT DEFAULT 1,           -- 1=自动, 2=半自动, 3=手工, 4=真空, 5=螺旋
    PlcTagIndex INT,
    PlcWeightTag NVARCHAR(200),
    PlcMaterialTag NVARCHAR(200),
    CONSTRAINT FK_WeighingRecipeLines_Recipe FOREIGN KEY (RecipeId) REFERENCES Recipes(Id) ON DELETE CASCADE,
    CONSTRAINT FK_WeighingRecipeLines_Material FOREIGN KEY (MaterialId) REFERENCES Materials(Id)
);

CREATE INDEX IX_WeighingRecipeLines_RecipeId ON WeighingRecipeLines(RecipeId);
CREATE INDEX IX_WeighingRecipeLines_MaterialId ON WeighingRecipeLines(MaterialId);
CREATE UNIQUE INDEX UQ_WeighingRecipeLines_Recipe_LineNumber ON WeighingRecipeLines(RecipeId, LineNumber);

-- 10. 混炼配方扩展表
CREATE TABLE MixingRecipes (
    RecipeId INT PRIMARY KEY,
    MixerId INT,
    ProcessType INT DEFAULT 1,          -- 1=密炼机, 2=内混机, 3=开炼机, 4=连续混炼
    TotalSteps INT DEFAULT 0,
    TargetTemperature DECIMAL(6,2) DEFAULT 0,
    TargetTime INT DEFAULT 0,
    DumpTemperature INT DEFAULT 0,
    RamPressure DECIMAL(10,2) DEFAULT 0,
    RamPosition INT DEFAULT 0,
    RotorSpeed INT DEFAULT 0,
    EnableTempControl BIT DEFAULT 1,
    EnableEnergyControl BIT DEFAULT 0,
    CONSTRAINT FK_MixingRecipes_Recipe FOREIGN KEY (RecipeId) REFERENCES Recipes(Id) ON DELETE CASCADE
);

-- 11. 混炼配方步骤表
CREATE TABLE MixingRecipeSteps (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RecipeId INT NOT NULL,
    StepNumber INT NOT NULL,
    StepType INT DEFAULT 1,             -- 1=投料, 2=混炼, 3=升温, 4=降温, 5=出料, 6=等待, 99=自定义
    MaterialId INT,
    Amount DECIMAL(18,4) DEFAULT 0,
    AddTime INT DEFAULT 0,              // 相对步骤开始的投料时间(秒)
    Duration INT DEFAULT 0,             // 步骤持续时间(秒)
    TargetTemp INT DEFAULT 0,
    RotorSpeed INT DEFAULT 0,
    RamPressure DECIMAL(10,2) DEFAULT 0,
    IsAutoDump BIT DEFAULT 0,
    WaitForTemp BIT DEFAULT 0,
    Condition NVARCHAR(500),            // 跳转条件表达式
    Remark NVARCHAR(500),
    PlcStepIndex INT,
    PlcCommandTag NVARCHAR(200),
    PlcParamTags NVARCHAR(MAX),         // JSON
    CONSTRAINT FK_MixingRecipeSteps_Recipe FOREIGN KEY (RecipeId) REFERENCES Recipes(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MixingRecipeSteps_Material FOREIGN KEY (MaterialId) REFERENCES Materials(Id)
);

CREATE INDEX IX_MixingRecipeSteps_RecipeId ON MixingRecipeSteps(RecipeId);
CREATE INDEX IX_MixingRecipeSteps_MaterialId ON MixingRecipeSteps(MaterialId);
CREATE UNIQUE INDEX UQ_MixingRecipeSteps_Recipe_StepNumber ON MixingRecipeSteps(RecipeId, StepNumber);

-- 12. 配方下发日志表
CREATE TABLE RecipeDownloadLogs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RecipeId INT NOT NULL,
    RecipeCode NVARCHAR(50) NOT NULL,
    RecipeName NVARCHAR(100) NOT NULL,
    RecipeType INT NOT NULL,
    Version NVARCHAR(50) NOT NULL,
    TargetPlc NVARCHAR(100) NOT NULL,
    Status INT DEFAULT 1,               -- 1=待下发, 2=下发中, 3=成功, 4=失败, 5=部分成功, 6=已校验
    ErrorMessage NVARCHAR(1000),
    Operator NVARCHAR(100),
    StartTime DATETIME2 DEFAULT GETDATE(),
    EndTime DATETIME2,
    DownloadData NVARCHAR(MAX),         // JSON 格式的下发数据
    CONSTRAINT FK_RecipeDownloadLogs_Recipe FOREIGN KEY (RecipeId) REFERENCES Recipes(Id)
);

CREATE INDEX IX_RecipeDownloadLogs_RecipeId ON RecipeDownloadLogs(RecipeId);
CREATE INDEX IX_RecipeDownloadLogs_TargetPlc ON RecipeDownloadLogs(TargetPlc);
CREATE INDEX IX_RecipeDownloadLogs_StartTime ON RecipeDownloadLogs(StartTime);
CREATE INDEX IX_RecipeDownloadLogs_Status ON RecipeDownloadLogs(Status);

-- 13. 批次生产记录表
CREATE TABLE BatchRecords (
    Id INT PRIMARY KEY IDENTITY(1,1),
    BatchNumber NVARCHAR(50) NOT NULL UNIQUE,
    RecipeId INT NOT NULL,
    RecipeCode NVARCHAR(50) NOT NULL,
    RecipeVersion NVARCHAR(50) NOT NULL,
    MixerId INT,
    ScaleId INT,
    Status INT DEFAULT 1,               -- 1=已创建, 2=称量中, 3=称量完成, 4=混炼中, 5=混炼完成, 6=已出料, 7=完成, 8=中止, 9=暂停
    ActualWeight DECIMAL(18,4) DEFAULT 0,
    TargetWeight DECIMAL(18,4) DEFAULT 0,
    StartTime DATETIME2 DEFAULT GETDATE(),
    EndTime DATETIME2,
    Operator NVARCHAR(100),
    Shift NVARCHAR(50),
    Remark NVARCHAR(1000),
    WeighingDetails NVARCHAR(MAX),      // JSON
    MixingDetails NVARCHAR(MAX),        // JSON
    CONSTRAINT FK_BatchRecords_Recipe FOREIGN KEY (RecipeId) REFERENCES Recipes(Id)
);

CREATE INDEX IX_BatchRecords_BatchNumber ON BatchRecords(BatchNumber);
CREATE INDEX IX_BatchRecords_RecipeId ON BatchRecords(RecipeId);
CREATE INDEX IX_BatchRecords_Status ON BatchRecords(Status);
CREATE INDEX IX_BatchRecords_StartTime ON BatchRecords(StartTime);

-- 14. 初始化基础物料分类数据
INSERT INTO MaterialCategories (Code, Name, Description, SortOrder, IsActive) VALUES
('RM', '橡胶原料', '天然橡胶、合成橡胶等', 1, 1),
('CB', '炭黑', '各类炭黑产品', 2, 1),
('OIL', '油类', '工艺油、软化剂等', 3, 1),
('CHEM', '化学品', '硫化剂、促进剂、防老剂等', 4, 1),
('ADD', '添加剂', '填充剂、增塑剂等', 5, 1),
('PKG', '包装材料', '编织袋、内衬袋等', 6, 1),
('SFG', '半成品', '母胶、混炼胶等', 7, 1),
('FG', '成品', '最终成品胶料', 8, 1),
('OTH', '其他', '其他辅助材料', 99, 1);

-- 15. 初始化示例物料数据
INSERT INTO Materials (Code, Name, Specification, Unit, MaterialType, Status, CategoryId, Density, MoistureContent, ParticleSize, IsWeighable, IsBatchManaged, CreatedBy) VALUES
('NR-001', '天然橡胶 STR 20', 'STR 20级', 'KG', 1, 1, 1, 0.92, 0.5, 0, 1, 1, 'SYSTEM'),
('SBR-001', '丁苯橡胶 1502', '1502型', 'KG', 1, 1, 1, 0.94, 0.3, 0, 1, 1, 'SYSTEM'),
('BR-001', '顺丁橡胶 BR 9000', 'BR 9000', 'KG', 1, 1, 1, 0.91, 0.2, 0, 1, 1, 'SYSTEM'),
('CB-N330', '炭黑 N330', 'N330 高耐磨', 'KG', 1, 1, 2, 1.85, 0.5, 30, 1, 1, 'SYSTEM'),
('CB-N550', '炭黑 N550', 'N550 半补强', 'KG', 1, 1, 2, 1.82, 0.5, 40, 1, 1, 'SYSTEM'),
('OIL-ARA', '芳香族油', '工艺用芳香油', 'KG', 1, 1, 3, 0.98, 0.1, 0, 1, 1, 'SYSTEM'),
('SUL-001', '硫磺', '工业级硫磺', 'KG', 4, 1, 4, 2.07, 0.1, 200, 1, 1, 'SYSTEM'),
('ACC-TBBS', '促进剂 TBBS', 'N-叔丁基-2-苯并噻唑次磺酰胺', 'KG', 4, 1, 4, 1.35, 0.2, 150, 1, 1, 'SYSTEM'),
('ANT-4010', '防老剂 4010', 'N-异丙基-N'-苯基对苯二胺', 'KG', 4, 1, 4, 1.15, 0.3, 100, 1, 1, 'SYSTEM'),
('ZNO-001', '氧化锌', '活性氧化锌', 'KG', 4, 1, 4, 5.6, 0.5, 50, 1, 1, 'SYSTEM'),
('STEARIC', '硬脂酸', '工业级硬脂酸', 'KG', 4, 1, 4, 0.94, 0.2, 0, 1, 1, 'SYSTEM');

-- 16. 创建更新时间触发器 (SQL Server)
CREATE TRIGGER TR_Materials_UpdatedAt
ON Materials
AFTER UPDATE
AS
BEGIN
    UPDATE Materials SET UpdatedAt = GETDATE()
    FROM Materials m
    INNER JOIN inserted i ON m.Id = i.Id;
END;

CREATE TRIGGER TR_Recipes_UpdatedAt
ON Recipes
AFTER UPDATE
AS
BEGIN
    UPDATE Recipes SET UpdatedAt = GETDATE()
    FROM Recipes r
    INNER JOIN inserted i ON r.Id = i.Id;
END;

-- 完成
PRINT 'TwoD5DMonitoring 物料与配方数据库迁移完成 (SQL Server)';