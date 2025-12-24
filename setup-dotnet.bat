@echo off
chcp 65001 >nul 2>&1
cls
echo ===============================================
echo    .NET SDK 安装助手
echo ===============================================
echo.

echo 检查当前 .NET 状态...
dotnet --version >nul 2>&1
if %errorlevel%==0 (
    for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
    echo ✅ .NET SDK 已安装: %DOTNET_VERSION%
    echo.
    echo 您可以直接运行游戏:
    echo • run-demo.bat (主启动器)
    echo • run-game-world.bat (游戏世界)
    echo • run-simple-gui.bat (简化界面)
    echo.
    pause
    exit /b 0
)

echo ❌ 未检测到 .NET SDK
echo.
echo 🔧 安装选项:
echo.
echo 1. 自动打开官方下载页面 (推荐)
echo 2. 显示手动安装说明
echo 3. 检查系统信息
echo 4. 退出
echo.
set /p choice="请选择 (1-4): "

if "%choice%"=="1" goto :open_download
if "%choice%"=="2" goto :manual_instructions
if "%choice%"=="3" goto :system_info
if "%choice%"=="4" goto :exit

:open_download
echo.
echo 正在打开 .NET 8.0 下载页面...
echo.
echo 📋 安装提示:
echo 1. 选择 ".NET 8.0 SDK" (不是 Runtime)
echo 2. 根据您的系统选择 x64 或 x86 版本
echo 3. 下载并运行安装程序
echo 4. 安装完成后重启命令提示符
echo 5. 运行 run-demo.bat 启动游戏
echo.
start https://dotnet.microsoft.com/download/dotnet/8.0
goto :exit

:manual_instructions
echo.
echo 📋 手动安装说明:
echo.
echo 1. 访问: https://dotnet.microsoft.com/download/dotnet/8.0
echo 2. 在 ".NET 8.0" 部分找到 "SDK" 下载链接
echo 3. 选择适合您系统的版本:
echo    • Windows x64 (64位系统，推荐)
echo    • Windows x86 (32位系统)
echo    • Windows Arm64 (ARM处理器)
echo.
echo 4. 下载 .exe 安装文件
echo 5. 以管理员身份运行安装程序
echo 6. 按照安装向导完成安装
echo 7. 重启命令提示符
echo 8. 验证安装: dotnet --version
echo.
goto :exit

:system_info
echo.
echo 💻 系统信息:
echo 操作系统: %OS%
echo 处理器架构: %PROCESSOR_ARCHITECTURE%
echo 计算机名: %COMPUTERNAME%
echo 用户名: %USERNAME%
echo.
echo 推荐下载版本:
if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    echo • .NET 8.0 SDK - Windows x64
) else if "%PROCESSOR_ARCHITECTURE%"=="x86" (
    echo • .NET 8.0 SDK - Windows x86  
) else (
    echo • .NET 8.0 SDK - Windows %PROCESSOR_ARCHITECTURE%
)
echo.
goto :exit

:exit
echo.
echo 安装完成后，您可以运行以下命令启动游戏:
echo • run-demo.bat
echo • run-game-world.bat  
echo • run-simple-gui.bat
echo.
pause