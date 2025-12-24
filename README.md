# RimWorld游戏框架

一个类似于RimWorld的游戏框架，采用Entity-Component-System (ECS) 架构，支持智能AI行为、任务管理、程序化地图生成等功能。现在包含完整的图形用户界面！

## ⚡ 立即开始

### 第一次使用？
```bash
launcher.bat
```
这个智能启动器会检查您的系统并指导您完成设置。

### 需要帮助？
```bash
setup-dotnet.bat
```
自动安装 .NET SDK（运行游戏的必要条件）。

## 🚀 快速开始

**重要**: 本项目需要 .NET 8.0 SDK。如果您还没有安装，请先运行 `setup-dotnet.bat`。

### 游戏世界可视化（最新功能！）
```bash
run-game-world.bat
```
- 🌍 256×256格噪声地图
- 🔴 可视化人物（小圆）
- 🎲 随机移动AI
- 🖱️ 可缩放滚动视图

### 图形界面版本
```bash
run-simple-gui.bat
```

### 控制台版本
```bash
run-console-demo.bat
```

## 🎮 新功能：可视化游戏世界

RimWorld游戏框架现在包含了一个完整的可视化游戏世界：

- **🌍 巨大地图**: 256×256格，基于噪声生成的地形
- **🎨 四种地形**: 深水(黑)、沙地(浅黄)、草地(浅绿)、雪地(白)
- **🔴 可视化人物**: 红色小圆代表角色，支持随机移动
- **🖱️ 交互视图**: 可缩放、滚动、跟随人物的地图视图
- **⚡ 实时渲染**: 60+ FPS的流畅游戏体验
- **📊 性能监控**: FPS计数器和游戏时间显示

![游戏世界预览](README-GameWorld.md)

## 🎮 新功能：图形用户界面

RimWorld游戏框架现在包含了一个现代化的图形用户界面：

- **主菜单界面**: 包含开始游戏和结束游戏按钮
- **游戏演示窗口**: 实时显示框架运行情况
- **实时输出**: 查看游戏框架的运行日志
- **进程管理**: 启动/停止演示，控制游戏流程

![GUI界面预览](README-GUI.md)

## 项目结构

```
RimWorldFramework/
├── src/
│   ├── RimWorldFramework.GUI/           # 🆕 图形用户界面
│   ├── RimWorldFramework.Core/          # 核心框架库
│   │   ├── ECS/                         # Entity-Component-System
│   │   ├── Systems/                     # 游戏系统
│   │   ├── Configuration/               # 配置管理
│   │   ├── Events/                      # 事件系统
│   │   └── Common/                      # 通用工具类
│   └── RimWorldFramework.StandaloneDemo/ # 独立演示程序
├── tests/
│   └── RimWorldFramework.Tests/         # 单元测试和属性测试
└── RimWorldFramework.sln                # 解决方案文件
```

## 技术栈

- **.NET 8.0**: 现代C#开发平台
- **WPF**: Windows Presentation Foundation (GUI)
- **ECS架构**: 高性能的实体组件系统
- **NUnit**: 单元测试框架
- **FsCheck**: 基于属性的测试库
- **Microsoft.Extensions**: 配置和日志管理

## 核心特性

- 🎮 **ECS架构**: 高性能的实体组件系统
- 🌍 **可视化世界**: 256×256格噪声地图，四种地形类型
- 🔴 **人物系统**: 可视化角色，随机移动AI
- 🖥️ **图形界面**: 现代化的WPF用户界面
- 🤖 **智能AI**: 基于行为树的角色AI系统
- 📋 **任务管理**: 层次化任务树和依赖管理
- 🗺️ **地图生成**: 程序化地图和地形生成
- 🔧 **模组支持**: 安全的模组加载和热重载
- 📦 **安装包**: 跨平台安装程序生成

## 构建和运行

### 快速启动
```bash
# 启动图形界面版本
run-demo.bat

# 或启动控制台版本
run-console-demo.bat
```

### 手动构建
```bash
# 恢复依赖包
dotnet restore

# 构建项目
dotnet build

# 运行GUI版本
dotnet run --project src/RimWorldFramework.GUI/RimWorldFramework.GUI.csproj

# 运行测试
dotnet test
```

## 开发状态

项目正在积极开发中，当前已完成：
- [x] 项目结构和核心接口
- [ ] ECS核心系统实现
- [ ] 游戏框架主类
- [ ] 人物系统和AI
- [ ] 任务管理系统
- [ ] 地图生成系统
- [ ] 数据持久化
- [ ] 模组系统
- [ ] 安装包系统

## 许可证

本项目采用MIT许可证。详见 [LICENSE](LICENSE) 文件。