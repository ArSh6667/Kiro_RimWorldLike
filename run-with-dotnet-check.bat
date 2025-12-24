@echo off
chcp 65001 >nul 2>&1
cls
echo ===============================================
echo    RimWorld 游戏框架 - 智能启动器
echo ===============================================
echo.

echo [检查] 验证 .NET SDK 环境...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 未检测到 .NET SDK
    echo.
    echo 🔧 解决方案:
    echo.
    echo 1. 安装 .NET 8.0 SDK (推荐)
    echo    下载地址: https://dotnet.microsoft.com/download/dotnet/8.0
    echo    选择 "SDK" 版本，不是 "Runtime"
    echo.
    echo 2. 安装后重启命令提示符，再次运行此脚本
    echo.
    echo 3. 或者查看项目文档:
    echo    • README-GameWorld.md (游戏世界说明)
    echo    • README-GUI.md (界面功能说明)
    echo    • docs/ (完整技术文档)
    echo.
    echo ===============================================
    echo 按任意键打开 .NET 下载页面...
    pause >nul
    start https://dotnet.microsoft.com/download/dotnet/8.0
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
echo ✅ .NET SDK 版本: %DOTNET_VERSION%

echo.
echo [选择] 请选择启动模式:
echo.
echo 1. 🌍 游戏世界演示 (推荐) - 可视化地图和人物移动
echo 2. 🎮 完整GUI界面 - 主菜单和所有功能
echo 3. 📊 控制台演示 - 纯文本版本，最稳定
echo 4. 🔧 运行诊断 - 检查系统环境
echo.
set /p choice="请输入选择 (1-4): "

if "%choice%"=="1" goto :game_world
if "%choice%"=="2" goto :full_gui  
if "%choice%"=="3" goto :console_demo
if "%choice%"=="4" goto :diagnostics
echo 无效选择，默认启动游戏世界...

:game_world
echo.
echo 🌍 启动游戏世界演示...
echo 功能: 256×256地图，噪声地形，人物随机移动
call run-game-world.bat
goto :end

:full_gui
echo.
echo 🎮 启动完整GUI界面...
call run-simple-gui.bat
goto :end

:console_demo
echo.
echo 📊 启动控制台演示...
call run-console-demo.bat
goto :end

:diagnostics
echo.
echo 🔧 运行系统诊断...
call debug-gui.bat
goto :end

:end
echo.
echo 程序已退出。
pause