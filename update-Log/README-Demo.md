# RimWorld游戏框架演示

## 概述

这是一个完整的RimWorld风格游戏框架的控制台演示程序。它展示了框架的核心功能，包括角色管理、任务系统、状态更新、地图生成和游戏进度跟踪。

## 系统要求

- .NET 8.0 SDK 或更高版本
- Windows、macOS 或 Linux 操作系统
- 至少 100MB 可用内存

## 安装 .NET SDK

如果您的系统上没有安装 .NET SDK，请按照以下步骤安装：

### Windows
1. 访问 https://dotnet.microsoft.com/download/dotnet/8.0
2. 下载 ".NET 8.0 SDK" (不是运行时)
3. 运行安装程序并按照提示完成安装

### macOS
```bash
# 使用 Homebrew
brew install dotnet

# 或者从官网下载安装包
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### Linux (Ubuntu/Debian)
```bash
# 添加 Microsoft 包源
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# 安装 .NET SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

## 运行演示

### 方法 1: 使用脚本 (推荐)

**Windows:**
```cmd
run-demo.bat
```

**Linux/macOS:**
```bash
chmod +x run-demo.sh
./run-demo.sh
```

### 方法 2: 手动运行

1. 构建项目:
```bash
dotnet build src/RimWorldFramework.Demo/RimWorldFramework.Demo.csproj
```

2. 运行演示:
```bash
dotnet run --project src/RimWorldFramework.Demo/RimWorldFramework.Demo.csproj
```

## 演示功能

### 启动过程
1. **框架初始化**: 加载所有核心系统
2. **地图生成**: 创建100x100的随机地图
3. **角色创建**: 生成3个初始角色，每个都有随机的技能和需求
4. **任务创建**: 生成5个不同类型的初始任务

### 实时显示
演示程序每5秒更新一次屏幕，显示：

- **游戏时间**: 从启动开始的累计时间
- **游戏统计**: 
  - 已完成任务数量
  - 技能升级次数
  - 角色创建总数
  - 研究和建筑数量
- **里程碑成就**: 达成的游戏里程碑
- **角色状态**: 每个角色的详细信息
  - 心情和效率
  - 需求值 (饥饿、疲劳、娱乐)
  - 主要技能等级

### 系统演示

#### 1. 角色系统
- 自动状态更新 (需求值随时间增长)
- 技能经验获得和升级
- 心情和效率计算

#### 2. 任务系统
- 自动任务分配给角色
- 任务进度跟踪
- 任务完成后的经验奖励

#### 3. 地图生成系统
- 程序化地图生成
- 随机种子支持
- 地形多样性

#### 4. 进度跟踪系统
- 实时统计收集
- 里程碑检测
- 成就系统

#### 5. 事件系统
- 系统间事件通信
- 状态变化通知
- 松耦合架构

## 控制说明

- **运行**: 程序启动后自动运行
- **退出**: 按 'q' 键退出程序
- **观察**: 屏幕每5秒自动刷新显示最新状态

## 预期行为

运行演示时，您应该看到：

1. **初始化消息**: 系统启动和内容创建的日志
2. **实时更新**: 每5秒刷新的游戏状态
3. **数值变化**: 
   - 角色需求值逐渐增长
   - 任务进度和完成
   - 技能经验积累
   - 里程碑达成

## 示例输出

```
=== RimWorld游戏框架演示 - 实时状态 ===
游戏时间: 00:02:15
当前角色数量: 3

=== 游戏统计 ===
已完成任务: 2
技能升级次数: 1
创建角色总数: 3
已完成研究: 0
建造建筑: 0

=== 里程碑成就 ===
✓ 第一个角色: 创建第一个角色
✓ 勤劳工作者: 完成100个任务

=== 角色状态 ===
艾莉丝 (ID: 1):
  心情: 75% | 效率: 68%
  饥饿: 45% | 疲劳: 32% | 娱乐: 28%
  主要技能: General:3, Construction:2, Crafting:1

鲍勃 (ID: 2):
  心情: 82% | 效率: 71%
  饥饿: 38% | 疲劳: 41% | 娱乐: 15%
  主要技能: Intellectual:4, Cooking:2, Mining:1

按 'q' 退出游戏
```

## 技术特性展示

### 架构模式
- **ECS (Entity-Component-System)**: 灵活的实体管理
- **事件驱动**: 松耦合的系统通信
- **依赖注入**: 可测试的组件设计

### 性能特性
- **增量更新**: 只更新变化的数据
- **内存管理**: 高效的对象池和资源管理
- **并发安全**: 线程安全的操作

### 扩展性
- **模块化设计**: 易于添加新系统
- **配置驱动**: 可调整的游戏参数
- **插件支持**: 模组系统架构

## 故障排除

### 常见问题

1. **"No .NET SDKs were found"**
   - 解决方案: 安装 .NET 8.0 SDK

2. **编译错误**
   - 检查所有文件是否完整
   - 确保项目引用正确

3. **运行时异常**
   - 检查控制台输出的错误信息
   - 确保有足够的系统资源

### 获取帮助

如果遇到问题，请检查：
1. .NET SDK 版本: `dotnet --version`
2. 项目构建: `dotnet build`
3. 控制台错误输出

## 下一步

这个演示展示了框架的核心功能。在实际游戏开发中，您可以：

1. **添加图形界面**: 集成游戏引擎 (Unity, Godot等)
2. **扩展游戏内容**: 添加更多角色类型、任务和系统
3. **网络功能**: 实现多人游戏支持
4. **保存系统**: 完善游戏状态持久化
5. **模组支持**: 开发模组API和工具

框架的模块化设计使得这些扩展都能够轻松实现。