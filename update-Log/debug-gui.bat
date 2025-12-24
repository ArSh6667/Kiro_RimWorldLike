@echo off
chcp 65001 >nul 2>&1
cls
echo ===============================================
echo    RimWorld 框架 - 诊断工具
echo ===============================================
echo.

echo [检查 1] 当前目录和项目文件...
echo 当前目录: %CD%
echo.

set PROJECT_FOUND=0

if exist "src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj" (
    echo ✅ GUI项目文件存在
    set PROJECT_FOUND=1
) else (
    echo ❌ GUI项目文件不存在
    echo    路径: src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj
)

if exist "src\RimWorldFramework.StandaloneDemo\RimWorldFramework.StandaloneDemo.csproj" (
    echo ✅ StandaloneDemo项目文件存在
    set PROJECT_FOUND=1
) else (
    echo ❌ StandaloneDemo项目文件不存在
    echo    路径: src\RimWorldFramework.StandaloneDemo\RimWorldFramework.StandaloneDemo.csproj
)

if exist "RimWorldFramework.sln" (
    echo ✅ 解决方案文件存在
) else (
    echo ❌ 解决方案文件不存在
)

if %PROJECT_FOUND%==0 (
    echo.
    echo ❌ 未找到任何项目文件！
    echo 请确保在正确的目录中运行此脚本。
    goto :end
)

echo.
echo [检查 2] .NET 环境...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET SDK 不可用
    echo.
    echo 请安装 .NET 8.0 SDK:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    goto :end
) else (
    for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
    echo ✅ .NET SDK 版本: %DOTNET_VERSION%
)

echo.
echo [检查 3] 系统信息...
echo 操作系统: %OS%
echo 处理器架构: %PROCESSOR_ARCHITECTURE%
echo 用户名: %USERNAME%

echo.
echo [检查 4] 尝试恢复包...
if exist "src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj" (
    echo 正在恢复 GUI 项目包...
    dotnet restore src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --verbosity quiet
    if %errorlevel%==0 (
        echo ✅ GUI 项目包恢复成功
    ) else (
        echo ❌ GUI 项目包恢复失败
    )
)

echo.
echo [检查 5] 尝试构建项目...
if exist "src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj" (
    echo 正在构建 GUI 项目...
    dotnet build src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --verbosity normal --nologo
    
    if %errorlevel%==0 (
        echo ✅ GUI 项目构建成功
        set GUI_BUILD_OK=1
    ) else (
        echo ❌ GUI 项目构建失败
        set GUI_BUILD_OK=0
    )
) else (
    set GUI_BUILD_OK=0
)

if exist "src\RimWorldFramework.StandaloneDemo\RimWorldFramework.StandaloneDemo.csproj" (
    echo.
    echo 正在构建 StandaloneDemo 项目...
    dotnet build src\RimWorldFramework.StandaloneDemo\RimWorldFramework.StandaloneDemo.csproj --verbosity normal --nologo
    
    if %errorlevel%==0 (
        echo ✅ StandaloneDemo 项目构建成功
        set DEMO_BUILD_OK=1
    ) else (
        echo ❌ StandaloneDemo 项目构建失败
        set DEMO_BUILD_OK=0
    )
) else (
    set DEMO_BUILD_OK=0
)

echo.
echo ===============================================
echo 诊断结果总结:
echo ===============================================

if %GUI_BUILD_OK%==1 (
    echo ✅ GUI 版本可用 - 运行: run-simple-gui.bat
)

if %DEMO_BUILD_OK%==1 (
    echo ✅ 控制台版本可用 - 运行: run-console-demo.bat
)

if %GUI_BUILD_OK%==0 (
    if %DEMO_BUILD_OK%==0 (
        echo ❌ 所有版本都不可用
        echo.
        echo 建议解决方案:
        echo 1. 检查 .NET SDK 安装
        echo 2. 重新下载项目文件
        echo 3. 以管理员身份运行
        echo 4. 检查防病毒软件设置
    )
)

echo.
echo 可用的启动脚本:
echo • run-simple-gui.bat    (简化GUI版本)
echo • run-game-world.bat    (游戏世界演示)  
echo • run-console-demo.bat  (控制台版本)
echo • test-gui.bat          (测试构建)

:end
echo.
echo 诊断完成。按任意键退出...
pause >nul