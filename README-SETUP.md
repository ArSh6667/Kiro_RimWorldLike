# RimWorld 游戏框架 - 安装和启动指南

## 🚀 快速开始

### 第一步：安装 .NET SDK

本项目需要 **.NET 8.0 SDK** 才能运行。这是一个免费的开发工具包。

#### 自动安装助手
```bash
setup-dotnet.bat
```

#### 手动安装
1. 访问 [.NET 8.0 下载页面](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 下载 "**.NET 8.0 SDK**" (不是 Runtime)
3. 运行安装程序
4. 重启命令提示符

### 第二步：启动游戏

安装 .NET SDK 后，选择以下启动方式：

#### 🌍 游戏世界演示 (推荐)
```bash
run-game-world.bat
```
- 256×256 像素地图
- 基于噪声的地形生成 (黑色、浅黄、浅绿、白色)
- 可视化人物 (红色圆圈)
- 随机移动AI
- 可缩放和滚动的地图视图

#### 🎮 完整GUI界面
```bash
run-demo.bat
```
- 主菜单界面
- 开始游戏/结束游戏按钮
- 多个演示模式

#### 📊 控制台版本 (最兼容)
```bash
run-console-demo.bat
```
- 纯文本界面
- 不需要图形支持
- 最稳定的版本

## 🔧 故障排除

### 问题：启动失败
1. 运行诊断: `debug-gui.bat`
2. 检查 .NET 安装: `dotnet --version`
3. 查看详细指南: `TROUBLESHOOTING.md`

### 问题：.NET SDK 未安装
- 运行: `setup-dotnet.bat`
- 或手动下载: https://dotnet.microsoft.com/download/dotnet/8.0

### 问题：图形界面无法启动
- 尝试控制台版本: `run-console-demo.bat`
- 检查 Windows 版本 (需要 Windows 10+)

## 📁 项目结构

```
RimWorldFramework/
├── src/
│   ├── RimWorldFramework.Core/     # 核心ECS框架
│   ├── RimWorldFramework.GUI/      # WPF图形界面
│   └── RimWorldFramework.StandaloneDemo/  # 控制台演示
├── docs/                           # 技术文档
├── run-demo.bat                    # 主启动器
├── run-game-world.bat             # 游戏世界演示
├── setup-dotnet.bat               # .NET 安装助手
└── debug-gui.bat                  # 诊断工具
```

## 🎮 功能特性

### 已实现功能
- ✅ ECS (Entity-Component-System) 架构
- ✅ 角色系统和AI行为
- ✅ 任务系统和任务树
- ✅ 路径寻找算法
- ✅ 协作系统
- ✅ 地图生成 (Perlin噪声)
- ✅ 数据持久化
- ✅ 模组系统
- ✅ 性能监控
- ✅ 可视化游戏世界
- ✅ WPF图形界面

### 技术规格
- **地图大小**: 256×256 格子
- **像素密度**: 32像素/格子
- **地形类型**: 4种 (黑色、浅黄、浅绿、白色)
- **人物表示**: 红色圆圈
- **移动模式**: 随机AI
- **渲染性能**: 60+ FPS

## 📚 文档

- `README-GameWorld.md` - 游戏世界详细说明
- `README-GUI.md` - 图形界面功能
- `TROUBLESHOOTING.md` - 完整故障排除指南
- `docs/` - 技术架构文档

## 🆘 获取帮助

1. **运行诊断**: `debug-gui.bat`
2. **查看文档**: `TROUBLESHOOTING.md`
3. **检查环境**: `setup-dotnet.bat`
4. **尝试备用版本**: `run-console-demo.bat`

---

**重要提示**: 如果遇到任何问题，首先运行 `setup-dotnet.bat` 确保 .NET SDK 正确安装。