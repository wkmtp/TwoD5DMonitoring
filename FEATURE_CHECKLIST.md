# 功能对比清单 - 旧版 Delphi vs 新版 WPF

## 概述
本文档用于对照旧版 Delphi 程序，确保新版 WPF 系统功能完备，不遗漏任何生产必需功能。

---

## 1. 核心监控功能

### 1.1 实时数据监控
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 设备实时状态显示 (运行/停止/报警/维护) | ✅ | ✅ | EquipmentBase.State + StateColor |
| 模拟量实时数值显示 (温度/压力/流量/液位/重量) | ✅ | ✅ | TagBinding + RuntimeView 模拟器 |
| 开关量实时状态显示 (阀门/电机/传感器) | ✅ | ✅ | ValveState/MotorState/DoorState |
| 数据刷新率配置 (默认500ms) | ✅ | ✅ | DispatcherTimer 500ms |
| 数据质量标识 (好/坏/不确定) | ✅ | ⚠️ | 需在 PlcTag.Quality 基础上完善UI显示 |

### 1.2 设备动画与可视化
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 罗茨风机转子旋转动画 | ✅ | ✅ | BlowerControl 转子Storyboard |
| 除尘器风机/振动动画 | ✅ | ✅ | DustCollectorControl |
| 输送带带式动画 | ✅ | ✅ | ConveyorControl 皮带偏移动画 |
| 混合机转子动画 | ✅ | ✅ | MixerControl RotorAnimationSpeed |
| 油泵转子/流向动画 | ✅ | ✅ | OilPumpControl |
| 阀门开度动画 (角度/位置) | ✅ | ✅ | ValveControl/SideBlowValveControl |
| 储罐液位波浪/填充动画 | ✅ | ✅ | TankControl WaveGeometry |
| 秤斗填充/卸料动画 | ✅ | ✅ | ScaleControl |
| 管道流体流动动画 (虚线流动) | ✅ | ✅ | PipeControl DashOffset 动画 |

### 1.3 管道网络可视化
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 直管/弯管/三通/异径管/侧吹装置 | ✅ | ✅ | 5种管道类型 |
| 管道颜色按介质区分 (炭黑/油/水/气/除尘) | ✅ | ✅ | PipeType 枚举 + 颜色转换器 |
| 管道流向箭头显示 | ✅ | ✅ | PipeControl ArrowTransform |
| 管道流速可视化 | ✅ | ✅ | FlowSpeed 属性控制动画速度 |
| 设备连接点 (锚点) 可视化 | ✅ | ✅ | AnchorPoint + 8种类型 |
| 拖拽连线创建管道 | ✅ | ✅ | DesignerCanvas 连接模式 |

---

## 2. 组态设计器功能

### 2.1 画布操作
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 网格显示/隐藏/大小调整 | ✅ | ✅ | DesignerCanvas GridSize/SnapToGrid |
| 网格吸附 | ✅ | ✅ | SnapToGrid=true |
| 缩放/平移画布 | ❌ | ❌ | **缺失** - 需添加 ZoomBorder/ScrollViewer 缩放 |
| 标尺/坐标显示 | ❌ | ❌ | **缺失** |
| 撤销/重做 (多级) | ✅ | ✅ | UndoableCommand + 命令栈 |
| 多选/框选 | ❌ | ❌ | **缺失** |
| 对齐/分布工具 | ❌ | ❌ | **缺失** |

### 2.2 元件库与拖拽
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 分类工具箱 (输送/上辅机/管路) | ✅ | ✅ | ToolboxItem Category |
| 拖拽创建设备 | ✅ | ✅ | Canvas Drop 事件 |
| 设备属性面板 | ✅ | ✅ | PropertyPanelControl 动态生成 |
| 设备复制/粘贴/删除 | ✅ | ✅ | 命令已实现 |
| 设备锁定/隐藏/置顶/置底 | ⚠️ | ⚠️ | ZIndex/IsVisible 属性已有，缺UI |

### 2.3 管道编辑
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 点击锚点创建连接 | ✅ | ✅ | AnchorClicked 事件 |
| 管道控制点编辑 (贝塞尔曲线) | ✅ | ✅ | ControlPoints 集合 |
| 管道分段/合并 | ❌ | ❌ | **缺失** |
| 管道属性编辑 (直径/材质/颜色/介质) | ✅ | ✅ | PipeConfig 属性面板 |

### 2.4 项目管理
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 新建/打开/保存/另存为项目 | ✅ | ✅ | DesignerViewModel 命令 |
| 项目文件格式 (.config JSON) | ✅ | ✅ | ConfigModel Newtonsoft.Json |
| 导入/导出配置 | ✅ | ✅ | LoadFromConfigModel/ToConfigModel |
| 版本管理/修订历史 | ❌ | ❌ | **缺失** |

---

## 3. 运行时功能

### 3.1 系统控制
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 全局启动/停止/急停按钮 | ✅ | ✅ | RuntimeView OnStartAll/StopAll/EmergencyStop |
| 单设备手动控制 (开/停/复位) | ⚠️ | ⚠️ | 右键菜单/属性面板缺完整实现 |
| 远程/就地切换 | ❌ | ❌ | **缺失** |

### 3.2 参数设置
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 设备参数在线修改 | ✅ | ✅ | TagBinding + PropertyPanel |
| 参数下发确认/回读 | ⚠️ | ⚠️ | PLC WriteAsync 有返回，缺确认UI |
| 参数范围校验 | ❌ | ❌ | **缺失** - 需在模型层添加 Validation |
| 配方参数批量下发 | ❌ | ❌ | **缺失** - 见配方管理 |

### 3.3 标签/IO绑定
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 标签浏览/搜索 | ✅ | ✅ | TagBindingDialog + PLC BrowseTagsAsync |
| 标签拖拽绑定 | ❌ | ❌ | **缺失** - 当前仅对话框选择 |
| 标签类型自动匹配 | ⚠️ | ⚠️ | 驱动中有 GetDataTypeForTag，缺UI提示 |
| 在线/离线标签区分 | ❌ | ❌ | **缺失** |

---

## 4. 历史数据与报表 ⭐ 重点补全

### 4.1 历史趋势
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 实时曲线/历史曲线 | ✅ | ❌ | **完全缺失** - 需新建 TrendView |
| 多笔叠加/对比 | ✅ | ❌ | **缺失** |
| 时间轴缩放/平移 | ✅ | ❌ | **缺失** |
| 游标测值/统计 (最大/最小/平均) | ✅ | ❌ | **缺失** |
| 数据导出 (CSV/Excel) | ✅ | ❌ | **缺失** |

### 4.2 历史报警
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 报警列表 (实时/历史) | ✅ | ❌ | **完全缺失** - 需 AlarmView + 数据库表 |
| 报警分级 (高/中/低/信息) | ✅ | ❌ | **缺失** |
| 报警确认/消音/屏蔽 | ✅ | ❌ | **缺失** |
| 报警统计/分析 (频率/时长/TopN) | ✅ | ❌ | **缺失** |
| 报警导出/打印 | ✅ | ❌ | **缺失** |

### 4.3 生产报表
| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 班报/日报/月报自动生成 | ✅ | ❌ | **缺失** - 需定时任务+模板引擎 |
| 产量统计 (总量/班产/日产) | ✅ | ❌ | **缺失** |
| 能耗统计 (电/气/水/蒸汽) | ✅ | ❌ | **缺失** |
| 原料消耗/配比报表 | ✅ | ❌ | **缺失** |
| 设备运行率/停机分析 (OEE) | ✅ | ❌ | **缺失** |
| 报表模板自定义 | ✅ | ❌ | **缺失** |

---

## 5. 配方管理 ⭐ 重点补全

| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 配方创建/编辑/删除 | ✅ | ❌ | **完全缺失** - 需 RecipeView + RecipeModel |
| 配方版本管理 | ✅ | ❌ | **缺失** |
| 配方参数表格编辑 | ✅ | ❌ | **缺失** |
| 配方下发到PLC (单步/全量) | ✅ | ❌ | **缺失** - 需 PLC 批量写入 + 确认 |
| 配方上传 (从PLC读回) | ✅ | ❌ | **缺失** |
| 配方导入导出 (Excel/CSV/XML) | ✅ | ❌ | **缺失** |
| 配方对比/差异分析 | ✅ | ❌ | **缺失** |
| 批次配方关联追溯 | ✅ | ❌ | **缺失** |

---

## 6. 权限与用户管理

| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 用户登录/注销 | ✅ | ❌ | **缺失** - 需 LoginWindow + AuthService |
| 角色权限 (操作员/工程师/管理员/超管) | ✅ | ❌ | **缺失** - 需 RBAC 模型 |
| 操作权限控制 (按钮/菜单/页面级) | ✅ | ❌ | **缺失** |
| 操作日志审计 (谁/何时/做了什么) | ✅ | ⚠️ | LogManager 有基础，缺审计专用表/UI |
| 密码策略/修改/过期 | ✅ | ❌ | **缺失** |
| 电子签名/二次确认 | ✅ | ❌ | **缺失** (GMP要求) |

---

## 7. 系统配置与维护

| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 通讯配置 (PLC IP/端口/站号/变量映射) | ✅ | ✅ | appsettings.json + PlcConnectionConfig |
| 数据库配置切换 (MySQL/PG/SQL Server) | ❌ | ✅ | **新增优势** - DatabaseProviderFactory |
| 设备参数配置 (量程/报警限/工程单位) | ✅ | ⚠️ | EquipmentSpecificModels 有属性，缺专用配置界面 |
| 画面/页面配置管理 | ✅ | ⚠️ | 当前仅单页面，缺多页面导航 |
| 语言切换 (中/英) | ❌ | ❌ | **缺失** - 需资源文件本地化 |
| 皮肤/主题切换 | ❌ | ❌ | **缺失** |
| 系统诊断/性能监控页面 | ❌ | ⚠️ | PerformanceOptimizer 已有，缺UI |

---

## 8. 通讯与集成

| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| OPC UA Client (多PLC) | ✅ | ✅ | 3大厂商驱动 + PlcManager |
| OPC DA (兼容旧设备) | ✅ | ❌ | **缺失** - 可选，通过 COM Interop 或网关 |
| Modbus TCP/RTU | ✅ | ❌ | **缺失** - 部分现场仍在用 |
| 第三方系统对接 (MES/ERP/SCADA) | ✅ | ❌ | **缺失** - 需 REST API / OPC UA Server |
| 数据库直连 (历史数据归档) | ✅ | ✅ | IDatabaseProvider + 历史表设计 |

---

## 9. 3D监控 (预留框架)

| 功能项 | Delphi 实现 | WPF 状态 | 备注 |
|--------|------------|----------|------|
| 3D场景浏览 | ❌ | 🔄 | **框架就绪** - I3DSceneManager + 占位窗口 |
| 3D场景编辑 (设备布局/视角/动画) | ❌ | 🔄 | **框架就绪** - ThreeDEditorWindow |
| 3D实时数据映射 | ❌ | 🔄 | **接口就绪** - UpdateDeviceStatusAsync |
| Three.js/Unity/Helix Toolkit 集成 | ❌ | 🔄 | **预留接口** - 暂不实现具体渲染 |

---

## 优先级排序 (建议实施顺序)

### P0 - 必须立即补全 (影响生产使用)
1. **历史趋势曲线** - 核心监控功能
2. **历史报警管理** - 安全合规要求
3. **用户权限/登录** - 基础安全
4. **配方管理** - 生产核心流程
5. **生产报表** - 管理决策依据

### P1 - 近期补全 (提升易用性)
6. **画布缩放/平移/多选** - 组态效率
7. **参数范围校验/下发确认** - 操作安全
8. **标签拖拽绑定/在线浏览** - 组态效率
9. **设备锁定/隐藏/图层管理** - 组态规范

### P2 - 后续增强 (锦上添花)
10. **多语言/主题/皮肤** - 国际化
11. **OEE/能耗分析** - 精益管理
12. **Modbus/OPC DA/第三方对接** - 兼容性
13. **3D渲染引擎集成** - 未来扩展

---

## 数据库表结构设计建议 (历史数据)

```sql
-- 历史趋势数据表 (分区表按月)
CREATE TABLE HistoryTrend (
    Id BIGSERIAL PRIMARY KEY,
    TagName VARCHAR(200) NOT NULL,
    Value DOUBLE PRECISION NOT NULL,
    Quality SMALLINT NOT NULL DEFAULT 192, -- 192=Good
    Timestamp TIMESTAMPTZ NOT NULL,
    INDEX idx_tag_time (TagName, Timestamp DESC)
) PARTITION BY RANGE (Timestamp);

-- 历史报警表
CREATE TABLE HistoryAlarm (
    Id BIGSERIAL PRIMARY KEY,
    AlarmId VARCHAR(100) NOT NULL,
    TagName VARCHAR(200),
    AlarmType VARCHAR(50), -- HiHi/Hi/Lo/LoLo/ROC/Dev
    Priority SMALLINT, -- 1=High, 2=Medium, 3=Low, 4=Info
    Message TEXT,
    Value DOUBLE PRECISION,
    Setpoint DOUBLE PRECISION,
    State SMALLINT, -- 0=Inactive, 1=Active, 2=Acked, 3=Shelved
    OccurTime TIMESTAMPTZ NOT NULL,
    AcknowledgeTime TIMESTAMPTZ,
    ReturnToNormalTime TIMESTAMPTZ,
    AcknowledgedBy VARCHAR(100),
    INDEX idx_alarm_time (OccurTime DESC),
    INDEX idx_alarm_tag (TagName, OccurTime DESC)
);

-- 生产报表汇总表
CREATE TABLE ProductionReport (
    Id BIGSERIAL PRIMARY KEY,
    ReportType VARCHAR(20), -- Shift/Day/Month
    BatchId VARCHAR(100),
    RecipeId VARCHAR(100),
    StartTime TIMESTAMPTZ NOT NULL,
    EndTime TIMESTAMPTZ NOT NULL,
    TotalQuantity DOUBLE PRECISION,
    GoodQuantity DOUBLE PRECISION,
    ScrapQuantity DOUBLE PRECISION,
    EnergyConsumption JSONB, -- 电/气/水/蒸汽
    MaterialConsumption JSONB, -- 原料消耗
    EquipmentOEE JSONB, -- 设备OEE明细
    CreatedAt TIMESTAMPTZ DEFAULT NOW()
);

-- 配方表
CREATE TABLE Recipe (
    Id BIGSERIAL PRIMARY KEY,
    RecipeCode VARCHAR(100) UNIQUE NOT NULL,
    RecipeName VARCHAR(200) NOT NULL,
    Version VARCHAR(50) NOT NULL,
    ProductCode VARCHAR(100),
    Parameters JSONB NOT NULL, -- 参数名/值/单位/范围
    Status SMALLINT DEFAULT 0, -- 0=Draft, 1=Approved, 2=Released, 3=Archived
    CreatedBy VARCHAR(100),
    CreatedAt TIMESTAMPTZ DEFAULT NOW(),
    ApprovedBy VARCHAR(100),
    ApprovedAt TIMESTAMPTZ,
    UNIQUE (RecipeCode, Version)
);

-- 用户权限表
CREATE TABLE SysUser (
    Id BIGSERIAL PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    PasswordHash VARCHAR(256) NOT NULL, -- BCrypt
    FullName VARCHAR(100),
    Email VARCHAR(200),
    RoleId BIGINT REFERENCES SysRole(Id),
    IsActive BOOLEAN DEFAULT TRUE,
    LastLoginAt TIMESTAMPTZ,
    PasswordChangedAt TIMESTAMPTZ,
    FailedLoginCount INT DEFAULT 0,
    LockedUntil TIMESTAMPTZ
);

CREATE TABLE SysRole (
    Id BIGSERIAL PRIMARY KEY,
    RoleCode VARCHAR(50) UNIQUE NOT NULL, -- Operator/Engineer/Admin/SuperAdmin
    RoleName VARCHAR(100) NOT NULL,
    Permissions JSONB -- 资源:权限 映射
);

-- 审计日志
CREATE TABLE AuditLog (
    Id BIGSERIAL PRIMARY KEY,
    UserId BIGINT REFERENCES SysUser(Id),
    Username VARCHAR(100),
    Action VARCHAR(100) NOT NULL, -- Login/Logout/ConfigChange/Control/RecipeDownload
    Resource VARCHAR(200), -- 受影响资源
    OldValue JSONB,
    NewValue JSONB,
    IpAddress INET,
    UserAgent TEXT,
    Timestamp TIMESTAMPTZ DEFAULT NOW(),
    Success BOOLEAN DEFAULT TRUE,
    ErrorMessage TEXT
);
```

---

## 更新记录
| 日期 | 版本 | 更新内容 |
|------|------|----------|
| 2026-08-30 | 1.0 | 初始版本 - 基于需求文档整理 |