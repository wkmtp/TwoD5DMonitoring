# TwoD5DMonitoring - 上辅机 2.5D 矢量组态监控系统

## 项目概述

这是一个基于 WPF 的上辅机及物料输送系统 2.5D 矢量组态监控系统，包含设计器和运行时两大模块。

## 功能模块

### 1. 管道动效控件
- **StraightPipe** - 直管段
- **Elbow90Pipe** - 90°弯管（四个方向）
- **TeePipe** - 三通管
- **2.5D 渐变管壁**：中间亮、两边暗的 LinearGradientBrush
- **粒子流动动画**：StrokeDashArray + DoubleAnimation 控制 StrokeDashOffset
- **支持正/反向、可调速度**

### 2. 基础控件基类
- **BaseEquipmentControl** - 锚点管理、拖拽/吸附/旋转、选中/编辑态、状态灯、模板方法模式
- **AnchorPoint** - 连接点模型，支持 8 方向 + 自定义
- **完整的交互系统**：选中、编辑、拖拽、网格吸附

### 3. 核心设备控件
| 控件 | 功能特性 |
|------|----------|
| **TankControl** | 炭黑罐/油罐 - 圆柱+锥体、动态料位、阀门动画、传感器灯 |
| **MixerControl** | 密炼机 - 双转子反向旋转、加料/卸料门动画、参数面板 |
| **BlowerControl** | 罗茨风机 - 8字形双转子反向旋转、电机罩、状态灯 |
| **DustCollectorControl** | 除尘器 - 脉冲箱、梯形箱体、灰斗、电机动画 |
| **FeedBinControl** | 投料仓 - 梯形仓体、气缸驱动仓门、除尘口、出料法兰 |
| **PressurizedTankControl** | 压送仓 - 圆柱+倒锥、料位透明填充、阀门动画 |
| **ScaleControl** | 秤控件 - 倒锥形、料位填充、数码管显示、传感器支架 |
| **ConveyorControl** | 输送带 - 皮带纹理偏移动画、双滚轮旋转、胶料块动态 |
| **SideBlowValveControl** | 侧吹阀 - 45°斜向插入、阀芯偏转动画 |
| **ValveControl** | 岔道阀 - Y型/V型、阀芯偏转动画、流向指示 |
| **OilPumpControl** | 油泵 - 齿轮泵转子旋转、流向箭头、进出口法兰 |

### 4. 设计器
- **工具箱** - 分类设备拖拽创建
- **画布** - 网格吸附、拖拽创建、橡皮筋连线预览、锚点吸附
- **对象树** - 层级化元素管理
- **属性面板** - 选中元素参数编辑
- **JSON 序列化** - 完整项目保存/加载
- **撤销/重做** - 历史操作管理

### 5. 运行时
- **实时数据绑定** - PLC 标签映射
- **实时动画** - 管道粒子流动、设备动画
- **全屏模式** - 生产现场全屏监控
- **报警面板** - 实时报警显示

## 项目结构

```
TwoD5DMonitoring/
├── Models/                    # 数据模型
│   ├── CoreModels.cs         # 核心基础模型
│   └── EquipmentSpecificModels.cs  # 具体设备模型
├── Converters/               # 转换器
│   ├── PipeConverters.cs     # 管道相关转换器
│   ├── EquipmentConverters.cs # 设备相关转换器
│   └── TankConverters.cs     # 罐体相关转换器
├── Controls/                 # 控件库
│   ├── BaseEquipmentControl.cs  # 基础控件基类
│   ├── Pipes/                # 管道控件
│   │   ├── PipeControl.xaml
│   │   └── PipeControl.xaml.cs
│   └── Equipment/            # 设备控件
│       ├── TankControl.xaml/.cs
│       ├── MixerControl.xaml/.cs
│       ├── BlowerControl.xaml/.cs
│       ├── DustCollectorControl.xaml/.cs
│       ├── FeedBinControl.xaml/.cs
│       ├── PressurizedTankControl.xaml/.cs
│       ├── ScaleControl.xaml/.cs
│       ├── ConveyorControl.xaml/.cs
│       ├── SideBlowValveControl.xaml/.cs
│       ├── ValveControl.xaml/.cs
│       └── OilPumpControl.xaml/.cs
├── Designer/                 # 设计器
│   ├── DesignerCanvas.xaml/.cs
│   ├── DesignerViewModel.cs
│   ├── DesignerModels.cs
│   └── RelayCommand.cs
├── Runtime/                  # 运行时
│   ├── RuntimeView.xaml/.cs
├── Resources/                # 资源
│   └── Brushes.xaml
├── Extensions/               # 扩展方法
│   └── GeometryExtensions.cs
├── App.xaml/.cs              # 应用入口
└── MainWindow.xaml/.cs       # 主窗口
```

## 编译要求

- .NET 8.0 / .NET 9.0
- Windows 平台 (WPF)
- NuGet 包：
  - Newtonsoft.Json 13.0.3+
  - System.Reactive 6.0.1+
  - Microsoft.Xaml.Behaviors.Wpf 1.1.135+

## 编译命令

```bash
dotnet build TwoD5DMonitoring.csproj -c Release
```

## 运行

```bash
dotnet run --project TwoD5DMonitoring.csproj
```

## 使用说明

### 设计器模式
1. 从左侧工具箱拖拽设备到画布
2. 点击设备锚点，拖拽到另一个设备锚点创建管道连接
3. 点击管道可编辑控制点、调整管径、流速
4. 右侧属性面板编辑设备参数
5. Ctrl+S 保存项目为 JSON

### 运行时模式
1. 点击工具栏 "▶ 运行时" 切换
2. 点击 "全部启动" 开始仿真
3. 实时查看设备状态、管道流动、参数面板
4. 支持急停、变速、全屏

## 扩展开发

### 添加新设备类型
1. 在 `EquipmentType` 枚举添加类型
2. 在 `Models/EquipmentSpecificModels.cs` 添加模型类
3. 在 `Controls/Equipment/` 创建 XAML + CS 控件
4. 在 `DesignerViewModel.CreateEquipmentControl()` 注册创建逻辑
5. 在 `InitializeTools()` 添加工具箱项

### 添加新管道类型
1. 在 `PipeType` 枚举添加类型
2. 继承 `PipeSegment` 实现几何
3. 在 `PipeControl.UpdateGeometry()` 处理渲染
4. 在 `PipeTypeToBrushConverter` 添加渐变刷

## 技术特点

- **纯 WPF 矢量绘制** - 无位图，任意缩放无失真
- **2.5D 视觉** - 多层渐变模拟立体光影
- **高性能动画** - GPU 加速的 Storyboard 动画
- **MVVM 架构** - 数据与视图分离，易测试易维护
- **插件化设计** - 控件即插即用，易扩展