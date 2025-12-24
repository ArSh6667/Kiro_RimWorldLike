@echo off
chcp 65001 >nul 2>&1
cls
echo ===============================================
echo    RimWorld 游戏框架 - 项目信息
echo ===============================================
echo.

echo 📋 项目概述:
echo • 基于 C# 和 .NET 8.0 的游戏开发框架
echo • 采用 ECS (Entity-Component-System) 架构
echo • 类似 RimWorld 的游戏机制
echo • 包含可视化游戏世界和图形界面
echo.

echo 🎮 主要功能:
echo • 角色系统和AI行为
echo • 任务系统和任务树  
echo • 路径寻找算法
echo • 协作系统
echo • 程序化地图生成
echo • 数据持久化
echo • 模组系统支持
echo.

echo 🌍 游戏世界特性:
echo • 地图大小: 256×256 格子 (32像素/格子)
echo • 地形生成: 基于 Perlin 噪声算法
echo • 地形类型: 4种颜色 (黑色、浅黄、浅绿、白色)
echo • 人物表示: 红色圆圈
echo • 移动AI: 随机移动模式
echo • 视图控制: 可缩放和滚动
echo.

echo 💻 技术架构:
echo • 编程语言: C#
echo • 框架: .NET 8.0
echo • 图形界面: WPF (Windows Presentation Foundation)
echo • 架构模式: ECS (Entity-Component-System)
echo • 测试框架: NUnit + FsCheck (属性测试)
echo.

echo 📁 项目结构:
echo src/RimWorldFramework.Core/        # 核心ECS框架
echo src/RimWorldFramework.GUI/         # WPF图形界面  
echo src/RimWorldFramework.StandaloneDemo/  # 控制台演示
echo tests/RimWorldFramework.Tests/     # 单元测试
echo docs/                              # 技术文档
echo.

echo 🚀 启动要求:
dotnet --version >nul 2>&1
if %errorlevel%==0 (
    for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
    echo ✅ .NET SDK 已安装: %DOTNET_VERSION%
    echo.
    echo 🎮 可用启动方式:
    echo • run-demo.bat          (主启动器)
    echo • run-game-world.bat    (游戏世界演示)
    echo • run-simple-gui.bat    (简化界面)
    echo • run-console-demo.bat  (控制台版本)
) else (
    echo ❌ 需要安装 .NET 8.0 SDK
    echo.
    echo 🔧 安装方法:
    echo • 运行: setup-dotnet.bat
    echo • 或访问: https://dotnet.microsoft.com/download/dotnet/8.0
)

echo.
echo 📚 文档文件:
if exist "README-SETUP.md" echo ✅ README-SETUP.md      (安装和启动指南)
if exist "README-GameWorld.md" echo ✅ README-GameWorld.md   (游戏世界详细说明)
if exist "README-GUI.md" echo ✅ README-GUI.md         (图形界面功能)
if exist "TROUBLESHOOTING.md" echo ✅ TROUBLESHOOTING.md    (故障排除指南)
if exist "README.md" echo ✅ README.md            (项目总览)

echo.
echo 🛠️ 开发工具:
echo • debug-gui.bat          (系统诊断)
echo • setup-dotnet.bat       (.NET 安装助手)
echo • test-gui.bat           (构建测试)
echo • quick-fix.bat          (快速修复)

echo.
echo ===============================================
echo 选择操作:
echo 1. 安装 .NET SDK
echo 2. 查看安装指南
echo 3. 运行诊断
echo 4. 退出
echo ===============================================
echo.
set /p choice="请输入选择 (1-4): "

if "%choice%"=="1" call setup-dotnet.bat
if "%choice%"=="2" (
    if exist "README-SETUP.md" (
        notepad README-SETUP.md
    ) else (
        echo README-SETUP.md 文件不存在
    )
)
if "%choice%"=="3" call debug-gui.bat
if "%choice%"=="4" exit /b 0

echo.
pause