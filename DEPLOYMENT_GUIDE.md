# TwoD5DMonitoring 部署指南

## 1. 系统要求

### 1.1 运行环境
| 组件 | 版本要求 | 说明 |
|------|---------|------|
| 操作系统 | Windows 10/11 (1909+) / Windows Server 2019/2022 | 必须 64 位 |
| .NET Runtime | .NET 8.0 Desktop Runtime (x64) | 必须安装 Desktop Runtime，非 SDK |
| 内存 | 最小 4 GB，推荐 8 GB+ | 大型项目建议 16 GB |
| 磁盘 | 最小 2 GB 可用空间 | 日志/历史数据需额外空间 |
| 分辨率 | 最小 1920×1080 | 设计器需要足够画布空间 |
| 网络 | 千兆以太网 | PLC 通讯要求低延迟 |

### 1.2 依赖组件
- **OPC UA 证书存储**: `%CommonApplicationData%\OPC Foundation\CertificateStores\`
- **Visual C++ Redistributable**: 最新版 (x64)
- **数据库客户端** (按需):
  - MySQL: MySQL Connector/NET (已包含在 NuGet)
  - PostgreSQL: 无额外安装 (Npgsql 纯托管)
  - SQL Server: Microsoft ODBC Driver 17/18 for SQL Server

---

## 2. 安装部署

### 2.1 发布模式
```bash
# 发布自包含单文件 (推荐生产环境)
dotnet publish TwoD5DMonitoring.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o ./publish

# 发布框架依赖模式 (体积小，需目标机安装 .NET 8 Runtime)
dotnet publish TwoD5DMonitoring.csproj -c Release -r win-x64 --self-contained false -o ./publish
```

### 2.2 目录结构
```
publish/
├── TwoD5DMonitoring.exe           # 主程序
├── TwoD5DMonitoring.dll           # 程序集
├── appsettings.json               # 配置文件 (必须)
├── appsettings.Production.json    # 生产环境覆盖配置 (可选)
├── Resources/                     # 资源文件 (Brushes.xaml 等)
├── Logs/                          # 日志目录 (自动创建)
├── Certificates/                  # OPC UA 证书目录 (可选)
└── *.dll                          # 依赖程序集
```

### 2.3 配置文件部署
1. 复制 `appsettings.json` 到发布目录
2. 根据环境创建 `appsettings.Production.json` 覆盖敏感配置
3. **重要**: 生产环境必须修改默认密码和连接字符串

---

## 3. 配置说明

### 3.1 数据库配置 (appsettings.json)
```json
{
  "Database": {
    "Provider": "SqlServer",           // 可选: MySql, PostgreSql, SqlServer
    "ConnectionString": "Server=192.168.1.100;Database=TwoD5DMonitoring;User Id=app_user;Password=StrongPassword123!;TrustServerCertificate=true;",
    "CommandTimeout": 30,
    "EnableRetry": true,
    "MaxRetryAttempts": 3,
    "RetryDelayMs": 1000
  }
}
```

**连接字符串示例:**
- **SQL Server**: `Server=ip;Database=db;User Id=user;Password=pwd;TrustServerCertificate=true;Encrypt=false;`
- **MySQL**: `Server=ip;Port=3306;Database=db;Uid=user;Pwd=pwd;SslMode=Preferred;`
- **PostgreSQL**: `Host=ip;Port=5432;Database=db;Username=user;Password=pwd;SSL Mode=Prefer;`

### 3.2 PLC 通讯配置
```json
{
  "Plc": {
    "DefaultDriver": "OpcUa",
    "DefaultTimeoutMs": 5000,
    "ReconnectIntervalMs": 5000,
    "MaxReconnectAttempts": 10,
    "Connections": [
      {
        "Name": "Siemens_S7_1500_Main",
        "DriverType": "OpcUa_Siemens",
        "EndpointUrl": "opc.tcp://192.168.1.10:4840",
        "Username": "opcua_user",
        "Password": "opcua_pass",
        "UseSecurity": false,
        "PublishingIntervalMs": 1000,
        "NodeMappings": {
          "Mixer_Temperature": "ns=3;s=Mixer.Temperature",
          "Mixer_Pressure": "ns=3;s=Mixer.Pressure",
          "Mixer_MotorRunning": "ns=3;s=Mixer.MotorRunning"
        }
      },
      {
        "Name": "Omron_NJ_Carbon",
        "DriverType": "OpcUa_Omron",
        "EndpointUrl": "opc.tcp://192.168.1.20:4840",
        "NodeMappings": {
          "CarbonBin_Level": "ns=2;s=CarbonBin.Level",
          "CarbonBin_ValveState": "ns=2;s=CarbonBin.ValveState"
        }
      }
    ]
  }
}
```

### 3.3 日志配置
```json
{
  "Logging": {
    "LogDirectory": "Logs",
    "MinimumLevel": "Information",   // Debug/Information/Warning/Error
    "RetainedFileCountLimit": 30,
    "EnableConsole": false,          // 生产建议 false
    "EnableDebug": false             // 生产建议 false
  }
}
```

### 3.4 通用配置
```json
{
  "General": {
    "ApplicationName": "TwoD5DMonitoring",
    "Version": "1.0.0",
    "AutoStartRuntime": false,       // 启动后自动进入运行时
    "UiUpdateIntervalMs": 500,       // UI刷新间隔
    "Enable3DMonitoring": false      // 3D功能开关
  }
}
```

---

## 4. OPC UA 证书配置

### 4.1 证书存储位置
```
%CommonApplicationData%\OPC Foundation\CertificateStores\
├── MachineDefault\          # 应用程序证书
├── UA Certificate Authorities\  # 受信任 CA
├── UA Applications\         # 受信任对等证书
└── RejectedCertificates\    # 被拒绝证书
```

### 4.2 证书生成 (首次运行自动生成)
如需手动生成:
```bash
# 使用 OPC Foundation 工具或 OpenSSL
openssl req -x509 -newkey rsa:2048 -keyout private.key -out cert.pem -days 3650 -subj "/CN=TwoD5DMonitoring"
```

### 4.3 证书信任配置
1. 将服务器证书复制到 `UA Applications` 目录
2. 将 CA 证书复制到 `UA Certificate Authorities` 目录
3. 设置 `UseSecurity: true` 并在配置中指定 `CertificatePath`

---

## 5. 数据库初始化

### 5.1 SQL Server
```sql
-- 创建数据库
CREATE DATABASE TwoD5DMonitoring;
GO

-- 创建用户
CREATE LOGIN app_user WITH PASSWORD = 'StrongPassword123!';
CREATE USER app_user FOR LOGIN app_user;
ALTER ROLE db_owner ADD MEMBER app_user;

-- 运行架构脚本 (见下方)
```

### 5.2 PostgreSQL
```sql
CREATE DATABASE "TwoD5DMonitoring";
CREATE USER app_user WITH ENCRYPTED PASSWORD 'StrongPassword123!';
GRANT ALL PRIVILEGES ON DATABASE "TwoD5DMonitoring" TO app_user;
```

### 5.3 MySQL
```sql
CREATE DATABASE TwoD5DMonitoring CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'app_user'@'%' IDENTIFIED BY 'StrongPassword123!';
GRANT ALL PRIVILEGES ON TwoD5DMonitoring.* TO 'app_user'@'%';
FLUSH PRIVILEGES;
```

### 5.4 核心表结构 (自动迁移或手动执行)
```sql
-- 历史趋势数据表 (建议分区)
CREATE TABLE HistoryTrend (
    Id BIGSERIAL PRIMARY KEY,
    TagName VARCHAR(200) NOT NULL,
    Value DOUBLE PRECISION NOT NULL,
    Quality SMALLINT NOT NULL DEFAULT 192,
    Timestamp TIMESTAMPTZ NOT NULL
);
CREATE INDEX idx_history_trend_tag_time ON HistoryTrend (TagName, Timestamp DESC);

-- 历史报警表
CREATE TABLE HistoryAlarm (
    Id BIGSERIAL PRIMARY KEY,
    AlarmId VARCHAR(100) NOT NULL,
    TagName VARCHAR(200),
    AlarmType VARCHAR(50),
    Priority SMALLINT,
    Message TEXT,
    Value DOUBLE PRECISION,
    Setpoint DOUBLE PRECISION,
    State SMALLINT,
    OccurTime TIMESTAMPTZ NOT NULL,
    AcknowledgeTime TIMESTAMPTZ,
    ReturnToNormalTime TIMESTAMPTZ,
    AcknowledgedBy VARCHAR(100)
);
CREATE INDEX idx_history_alarm_time ON HistoryAlarm (OccurTime DESC);

-- 生产报表表
CREATE TABLE ProductionReport (
    Id BIGSERIAL PRIMARY KEY,
    ReportType VARCHAR(20),
    BatchId VARCHAR(100),
    RecipeId VARCHAR(100),
    StartTime TIMESTAMPTZ NOT NULL,
    EndTime TIMESTAMPTZ NOT NULL,
    TotalQuantity DOUBLE PRECISION,
    GoodQuantity DOUBLE PRECISION,
    ScrapQuantity DOUBLE PRECISION,
    EnergyConsumption JSONB,
    MaterialConsumption JSONB,
    EquipmentOEE JSONB,
    CreatedAt TIMESTAMPTZ DEFAULT NOW()
);

-- 配方表
CREATE TABLE Recipe (
    Id BIGSERIAL PRIMARY KEY,
    RecipeCode VARCHAR(100) UNIQUE NOT NULL,
    RecipeName VARCHAR(200) NOT NULL,
    Version VARCHAR(50) NOT NULL,
    ProductCode VARCHAR(100),
    Parameters JSONB NOT NULL,
    Status SMALLINT DEFAULT 0,
    CreatedBy VARCHAR(100),
    CreatedAt TIMESTAMPTZ DEFAULT NOW(),
    ApprovedBy VARCHAR(100),
    ApprovedAt TIMESTAMPTZ
);

-- 用户权限表
CREATE TABLE SysUser (
    Id BIGSERIAL PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    PasswordHash VARCHAR(256) NOT NULL,
    FullName VARCHAR(100),
    RoleId BIGINT,
    IsActive BOOLEAN DEFAULT TRUE,
    LastLoginAt TIMESTAMPTZ
);

-- 审计日志表
CREATE TABLE AuditLog (
    Id BIGSERIAL PRIMARY KEY,
    UserId BIGINT,
    Username VARCHAR(100),
    Action VARCHAR(100) NOT NULL,
    Resource VARCHAR(200),
    OldValue JSONB,
    NewValue JSONB,
    IpAddress INET,
    Timestamp TIMESTAMPTZ DEFAULT NOW(),
    Success BOOLEAN DEFAULT TRUE
);
```

---

## 6. 服务化部署 (Windows 服务)

### 6.1 创建 Windows 服务
```bash
# 使用 NSSM (Non-Sucking Service Manager)
nssm install TwoD5DMonitoring "C:\Deploy\TwoD5DMonitoring\TwoD5DMonitoring.exe"
nssm set TwoD5DMonitoring AppDirectory "C:\Deploy\TwoD5DMonitoring"
nssm set TwoD5DMonitoring AppStdout "C:\Deploy\TwoD5DMonitoring\Logs\stdout.log"
nssm set TwoD5DMonitoring AppStderr "C:\Deploy\TwoD5DMonitoring\Logs\stderr.log"
nssm set TwoD5DMonitoring AppRotateFiles 1
nssm start TwoD5DMonitoring
```

### 6.2 服务配置建议
- **启动类型**: 自动 (延迟启动)
- **恢复操作**: 失败后重启 (1分钟/1分钟/1分钟)
- **登录身份**: 专用服务账户 (需网络访问权限)

---

## 7. 防火墙与网络

### 7.1 端口要求
| 方向 | 端口 | 协议 | 用途 |
|------|------|------|------|
| 出站 | 4840 | TCP | OPC UA (默认) |
| 出站 | 1433 | TCP | SQL Server |
| 出站 | 3306 | TCP | MySQL |
| 出站 | 5432 | TCP | PostgreSQL |
| 出站 | 443 | HTTPS | 远程更新/证书验证 (可选) |

### 7.2 防火墙规则示例 (PowerShell 管理员)
```powershell
# 允许出站 OPC UA
New-NetFirewallRule -DisplayName "TwoD5DMonitoring OPC UA Out" -Direction Outbound -Protocol TCP -RemotePort 4840 -Action Allow

# 允许出站数据库
New-NetFirewallRule -DisplayName "TwoD5DMonitoring SQL Out" -Direction Outbound -Protocol TCP -RemotePort 1433,3306,5432 -Action Allow
```

---

## 8. 启动验证清单

### 8.1 首次启动检查
- [ ] 程序正常启动，无异常弹窗
- [ ] 日志目录 `Logs/` 自动创建，有 `TwoD5DMonitoring-<date>.log`
- [ ] 日志显示 "Application Starting" 和 "Logging Initialized"
- [ ] 进入主界面，标题栏显示版本号

### 8.2 数据库连接验证
- [ ] 设计器菜单 -> 数据库测试 -> 连接成功
- [ ] 或查看日志: "Database Provider Created: SQL Server"

### 8.3 PLC 连接验证
- [ ] 运行时模式 -> 状态栏显示 PLC 连接状态
- [ ] 日志显示 "PLC State Changed: Disconnected -> Connected"
- [ ] 标签值实时更新 (模拟器或真实 PLC)

### 8.4 功能验证
- [ ] 设计器: 新建/保存/加载项目正常
- [ ] 设计器: 拖拽设备、连线、属性编辑正常
- [ ] 运行时: 启动/停止/急停按钮响应
- [ ] 运行时: 设备动画、管道流动正常
- [ ] 导入/导出 .config 文件正常

---

## 9. 常见问题排查

### 9.1 启动失败
| 现象 | 原因 | 解决 |
|------|------|------|
| "应用程序已在运行中" | 进程残留 | 任务管理器结束进程，或重启电脑 |
| "未处理异常: 文件未找到" | 缺少 .NET Runtime | 安装 .NET 8.0 Desktop Runtime |
| 配置文件加载失败 | appsettings.json 缺失/格式错误 | 检查文件存在性和 JSON 格式 |
| OPC UA 证书错误 | 证书存储权限/缺失 | 以管理员运行一次，或手动配置证书 |

### 9.2 PLC 连接失败
| 现象 | 排查步骤 |
|------|---------|
| 连接超时 | 1. Ping PLC IP 2. Telnet 端口 3. 检查防火墙 4. 确认 OPC UA 服务器运行 |
| 证书被拒绝 | 查看 `RejectedCertificates` 目录，复制到 `UA Applications` |
| 变量读取失败 | 检查 NodeId 映射是否正确，使用 UaExpert 验证 |
| 频繁断线 | 增加 `ReconnectIntervalMs`，检查网络稳定性 |

### 9.3 数据库连接失败
| 现象 | 排查步骤 |
|------|---------|
| 认证失败 | 检查用户名/密码，确认数据库用户权限 |
| 连接超时 | 检查防火墙、端口、网络延迟 |
| SSL 错误 | SQL Server 添加 `TrustServerCertificate=true;Encrypt=false;` |

### 9.4 性能问题
| 现象 | 排查步骤 |
|------|---------|
| UI 卡顿 | 查看日志性能报告，检查 `Render` 和 `UI更新` 耗时 |
| 内存增长 | 检查是否有未释放的事件订阅，使用 dotMemory 分析 |
| CPU 高 | 降低 `UiUpdateIntervalMs`，减少动画复杂度 |

---

## 10. 维护操作

### 10.1 日志维护
```bash
# 手动清理超过 30 天的日志 (自动保留 30 天)
forfiles /p "C:\Deploy\TwoD5DMonitoring\Logs" /s /m *.log /d -30 /c "cmd /c del @path"
```

### 10.2 备份策略
| 对象 | 频率 | 方式 |
|------|------|------|
| 配置文件 | 每次修改后 | 复制 appsettings.json |
| 项目文件 (.config) | 每日 | 备份项目目录 |
| 数据库 | 每日全量 + 小时增量 | SQL Agent / pg_dump / mysqldump |
| 历史数据 | 按保留策略 | 分区表自动归档/删除 |

### 10.3 版本升级
1. 停止服务/程序
2. 备份当前发布目录和数据库
3. 替换程序文件 (保留 appsettings.json 和 Logs 目录)
4. 运行数据库迁移脚本 (如有)
5. 启动验证

---

## 11. 联系支持

- **技术文档**: 本目录下 `REFACTORING_SUMMARY.md` `FEATURE_CHECKLIST.md`
- **日志分析**: `Logs/TwoD5DMonitoring-<date>.log`
- **性能报告**: 日志中每分钟输出的 "=== 性能报告 ==="

---

*文档版本: 1.0 | 更新日期: 2026-08-30*