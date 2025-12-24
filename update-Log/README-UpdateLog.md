# Update-Log 文件夹说明

这个文件夹包含了项目的更新文档、测试工具和辅助脚本，用于保持主目录的简洁。

## 📁 文件分类

### 📖 更新文档 (.md文件)
- `BUILD-SUCCESS.md` - GUI构建问题解决记录
- `CAMERA-CONTROLS.md` - 相机控制系统说明 (新增)
- `README-SETUP.md` - 安装和启动指南
- `README-Demo.md` - 演示功能说明
- `README-GameWorld.md` - 游戏世界详细说明
- `README-GUI.md` - 图形界面功能说明
- `TROUBLESHOOTING.md` - 完整故障排除指南
- `DEMO-QUICK-START.md` - 快速开始演示
- `QUICK-START.md` - 快速启动指南
- `demo-simulation.md` - 演示模拟说明

### 🔧 测试和诊断工具 (.bat文件)
- `test-gui-build.bat` - 快速GUI构建测试
- `test-gui-simple.bat` - 简单GUI启动测试
- `test-basic.bat` - 基础功能测试
- `test-gui.bat` - GUI测试
- `diagnose-build-error.bat` - 详细构建错误诊断
- `check-gui-files.bat` - 文件完整性检查
- `fix-gui-build.bat` - 自动修复GUI构建问题
- `debug-gui.bat` - 系统诊断工具

### ⚙️ 设置和辅助工具 (.bat文件)
- `setup-dotnet.bat` - .NET SDK安装助手
- `show-project-info.bat` - 项目信息展示
- `run-with-dotnet-check.bat` - 带.NET检查的启动器
- `quick-fix.bat` - 快速修复工具
- `run-minimal.bat` - 最小化启动

## 🚀 如何使用

### 从主目录访问
大多数工具可以通过主目录的 `launcher.bat` 智能启动器访问：
```bash
launcher.bat
```

### 直接运行
也可以直接运行update-Log文件夹中的工具：
```bash
# 例如：运行GUI构建测试
update-Log\test-gui-build.bat

# 例如：查看项目信息
update-Log\show-project-info.bat
```

### 查看文档
```bash
# 查看故障排除指南
notepad update-Log\TROUBLESHOOTING.md

# 查看安装指南
notepad update-Log\README-SETUP.md
```

## 📋 主目录保留的核心文件

主目录现在只保留最重要的启动文件：
- `launcher.bat` - 智能启动器（推荐）
- `run-demo.bat` - 主启动器
- `run-game-world.bat` - 游戏世界演示
- `run-simple-gui.bat` - 简化GUI界面
- `run-console-demo.bat` - 控制台版本
- `README.md` - 主要项目文档

## 🔄 文件整理历史

这些文件原本位于主目录中，为了保持项目结构的简洁性，已于 2024年12月24日 移动到此文件夹。所有功能保持不变，只是路径发生了变化。

---

**提示**: 如果您需要经常使用某个工具，建议使用 `launcher.bat` 智能启动器，它提供了友好的菜单界面来访问所有功能。