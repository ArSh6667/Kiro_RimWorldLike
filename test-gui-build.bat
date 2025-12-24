@echo off
chcp 65001 >nul 2>&1
cls
echo ===============================================
echo    快速GUI构建测试
echo ===============================================
echo.

echo 检查.NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET SDK未安装
    echo 请运行: setup-dotnet.bat
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
echo ✅ .NET SDK: %DOTNET_VERSION%

echo.
echo 快速构建测试...
dotnet build src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --verbosity minimal --nologo

if %errorlevel%==0 (
    echo ✅ 构建成功！
    echo.
    echo 可以运行以下命令启动GUI:
    echo • run-simple-gui.bat
    echo • run-game-world.bat
    echo • run-demo.bat
) else (
    echo ❌ 构建失败！
    echo.
    echo 解决方案:
    echo 1. 运行完整修复: fix-gui-build.bat
    echo 2. 查看详细错误: diagnose-build-error.bat
    echo 3. 检查文件完整性: check-gui-files.bat
)

echo.
pause