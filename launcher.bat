@echo off
chcp 65001 >nul 2>&1
cls

:menu
echo ===============================================
echo    RimWorld 游戏框架 - 启动器
echo ===============================================
echo.

echo 📋 项目状态:
dotnet --version >nul 2>&1
if %errorlevel%==0 (
    for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
    echo ✅ .NET SDK: %DOTNET_VERSION%
    set DOTNET_OK=1
) else (
    echo ❌ .NET SDK: 未安装
    set DOTNET_OK=0
)

echo.
echo 🎮 游戏启动选项:
if %DOTNET_OK%==1 (
    echo 1. 🌍 游戏世界演示 (推荐)
    echo 2. 🎮 完整GUI界面
    echo 3. 📊 控制台演示
) else (
    echo 1. ❌ 游戏世界演示 (需要 .NET SDK)
    echo 2. ❌ 完整GUI界面 (需要 .NET SDK)  
    echo 3. ❌ 控制台演示 (需要 .NET SDK)
)

echo.
echo 🔧 工具和帮助:
echo 4. 📋 项目信息和文档
echo 5. 🔧 安装 .NET SDK
echo 6. 🩺 运行系统诊断
echo 7. 🔧 修复GUI构建问题
echo 8. ✅ 查看构建状态
echo 9. 📖 查看故障排除指南
echo 10. ❌ 退出

echo.
set /p choice="请选择 (1-10): "

if "%choice%"=="1" (
    if %DOTNET_OK%==1 (
        call run-game-world.bat
    ) else (
        echo 需要先安装 .NET SDK。选择选项 5 进行安装。
        pause
    )
    goto :menu
)

if "%choice%"=="2" (
    if %DOTNET_OK%==1 (
        call run-simple-gui.bat
    ) else (
        echo 需要先安装 .NET SDK。选择选项 5 进行安装。
        pause
    )
    goto :menu
)

if "%choice%"=="3" (
    if %DOTNET_OK%==1 (
        call run-console-demo.bat
    ) else (
        echo 需要先安装 .NET SDK。选择选项 5 进行安装。
        pause
    )
    goto :menu
)

if "%choice%"=="4" (
    call update-Log\show-project-info.bat
    goto :menu
)

if "%choice%"=="5" (
    call update-Log\setup-dotnet.bat
    goto :menu
)

if "%choice%"=="6" (
    call update-Log\debug-gui.bat
    goto :menu
)

if "%choice%"=="7" (
    call update-Log\fix-gui-build.bat
    goto :menu
)

if "%choice%"=="8" (
    if exist "update-Log\BUILD-SUCCESS.md" (
        notepad update-Log\BUILD-SUCCESS.md
    ) else (
        echo BUILD-SUCCESS.md 文件不存在
        pause
    )
    goto :menu
)

if "%choice%"=="9" (
    if exist "update-Log\TROUBLESHOOTING.md" (
        notepad update-Log\TROUBLESHOOTING.md
    ) else (
        echo TROUBLESHOOTING.md 文件不存在
        pause
    )
    goto :menu
)

if "%choice%"=="10" exit /b 0

echo 无效选择，请重新输入。
pause
goto :menu