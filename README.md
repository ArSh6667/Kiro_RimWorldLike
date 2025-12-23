# RimWorld游戏框架

一个类似于RimWorld的游戏框架，采用Entity-Component-System (ECS) 架构，支持智能AI行为、任务管理、程序化地图生成等功能。

## 项目结构

```
RimWorldFramework/
├── src/
│   └── RimWorldFramework.Core/          # 核心框架库
│       ├── ECS/                         # Entity-Component-System
│       ├── Systems/                     # 游戏系统
│       ├── Configuration/               # 配置管理
│       ├── Events/                      # 事件系统
│       └── Common/                      # 通用工具类
├── tests/
│   └── RimWorldFramework.Tests/         # 单元测试和属性测试
└── RimWorldFramework.sln                # 解决方案文件
```

## 技术栈

- **.NET 8.0**: 现代C#开发平台
- **ECS架构**: 高性能的实体组件系统
- **NUnit**: 单元测试框架
- **FsCheck**: 基于属性的测试库
- **Microsoft.Extensions**: 配置和日志管理

## 核心特性

- 🎮 **ECS架构**: 高性能的实体组件系统
- 🤖 **智能AI**: 基于行为树的角色AI系统
- 📋 **任务管理**: 层次化任务树和依赖管理
- 🗺️ **地图生成**: 程序化地图和地形生成
- 🔧 **模组支持**: 安全的模组加载和热重载
- 📦 **安装包**: 跨平台安装程序生成

## 构建和运行

```bash
# 恢复依赖包
dotnet restore

# 构建项目
dotnet build

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