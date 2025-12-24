@echo off
chcp 65001 >nul 2>&1
cls
echo ===============================================
echo    GUI项目构建错误诊断
echo ===============================================
echo.

echo [1] 检查 .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET SDK 未安装
    echo 请先运行: setup-dotnet.bat
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
echo ✅ .NET SDK 版本: %DOTNET_VERSION%

echo.
echo [2] 检查项目文件...
if not exist "src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj" (
    echo ❌ 项目文件不存在
    pause
    exit /b 1
)
echo ✅ 项目文件存在

echo.
echo [3] 清理旧的构建文件...
if exist "src\RimWorldFramework.GUI\bin" rmdir /s /q "src\RimWorldFramework.GUI\bin"
if exist "src\RimWorldFramework.GUI\obj" rmdir /s /q "src\RimWorldFramework.GUI\obj"
echo ✅ 清理完成

echo.
echo [4] 恢复NuGet包...
dotnet restore src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --verbosity normal
if %errorlevel% neq 0 (
    echo ❌ 包恢复失败
    pause
    exit /b 1
)
echo ✅ 包恢复成功

echo.
echo [5] 详细构建分析...
echo 正在构建项目，显示详细错误信息...
echo.
dotnet build src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --verbosity diagnostic --no-restore

if %errorlevel%==0 (
    echo.
    echo ✅ 构建成功！
    echo.
    echo [6] 尝试运行...
    dotnet run --project src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --no-build
) else (
    echo.
    echo ❌ 构建失败！
    echo.
    echo 常见解决方案:
    echo 1. 检查是否缺少必要的文件
    echo 2. 确认所有XAML文件格式正确
    echo 3. 检查命名空间是否一致
    echo 4. 确认所有引用的类都存在
    echo.
    echo 详细错误信息已显示在上方。
)

echo.
echo 诊断完成。
pause