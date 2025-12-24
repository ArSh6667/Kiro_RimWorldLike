@echo off
chcp 65001 >nul 2>&1
cls
echo ===============================================
echo    RimWorld Game Framework - 启动程序
echo ===============================================
echo.

echo [1/4] 检查当前目录...
echo 当前目录: %CD%
if not exist "src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj" (
    echo ❌ 错误: 找不到GUI项目文件
    echo 请确保在正确的项目根目录中运行此脚本
    echo 预期路径: src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj
    goto :error_exit
)
echo ✅ 项目文件存在

echo.
echo [2/4] 检查 .NET 环境...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 错误: 未找到 .NET SDK
    echo.
    echo 🔧 解决方案:
    echo 1. 运行安装助手: update-Log\setup-dotnet.bat
    echo 2. 或手动下载: https://dotnet.microsoft.com/download/dotnet/8.0
    echo 3. 安装后重启命令提示符，再次运行此脚本
    echo.
    echo 📖 查看完整安装指南: update-Log\README-SETUP.md
    echo.
    set /p install_choice="是否现在打开安装助手? (y/n): "
    if /i "%install_choice%"=="y" (
        call update-Log\setup-dotnet.bat
    )
    goto :error_exit
)

for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
echo ✅ .NET SDK 版本: %DOTNET_VERSION%

echo.
echo [3/4] 构建项目...
echo 正在构建 GUI 项目，请稍候...
dotnet build src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --configuration Release --verbosity quiet --nologo

if %errorlevel% neq 0 (
    echo ❌ GUI项目构建失败！
    echo.
    echo 尝试构建备用演示项目...
    
    if exist "src\RimWorldFramework.StandaloneDemo\RimWorldFramework.StandaloneDemo.csproj" (
        dotnet build src\RimWorldFramework.StandaloneDemo\RimWorldFramework.StandaloneDemo.csproj --configuration Release --verbosity quiet --nologo
        
        if %errorlevel% neq 0 (
            echo ❌ 备用项目也构建失败！
            echo.
            echo 显示详细错误信息:
            dotnet build src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --verbosity normal
            goto :error_exit
        )
        
        echo ✅ 备用项目构建成功！
        echo.
        echo [4/4] 启动控制台演示...
        echo 正在启动 RimWorld 框架控制台演示...
        echo.
        
        dotnet run --project src\RimWorldFramework.StandaloneDemo\RimWorldFramework.StandaloneDemo.csproj --configuration Release
        goto :normal_exit
    ) else (
        echo ❌ 找不到备用演示项目
        goto :error_exit
    )
)

echo ✅ GUI项目构建成功！

echo.
echo [4/4] 启动图形界面...
echo 正在启动 RimWorld 游戏框架...
echo.
echo 🎮 功能包括:
echo   • 主菜单界面
echo   • 🌍 游戏世界 (256×256地图，噪声地形)
echo   • 🔴 可视化人物 (随机移动)
echo   • 🎯 演示框架 (ECS系统演示)
echo.

dotnet run --project src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --configuration Release

goto :normal_exit

:error_exit
echo.
echo ===============================================
echo 启动失败！解决方案:
echo.
echo 🔧 立即解决:
echo 1. update-Log\setup-dotnet.bat     (安装 .NET SDK)
echo 2. update-Log\debug-gui.bat        (运行完整诊断)
echo 3. update-Log\README-SETUP.md      (查看安装指南)
echo.
echo 🎮 备用启动方式:
echo • run-console-demo.bat  (控制台版本，如果可用)
echo • 查看项目文档和截图
echo.
echo 📚 文档:
echo • update-Log\README-GameWorld.md   (游戏世界说明)
echo • update-Log\TROUBLESHOOTING.md    (完整故障排除)
echo ===============================================
echo.
echo 按任意键退出...
pause >nul
exit /b 1

:normal_exit
echo.
echo ===============================================
echo 程序已正常退出
echo 感谢使用 RimWorld 游戏框架！
echo ===============================================
echo.
echo 按任意键退出...
pause >nul
exit /b 0