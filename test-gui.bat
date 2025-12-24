@echo off
echo === 测试 RimWorld GUI 项目 ===
echo.

echo 正在检查 .NET 环境...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo 错误: 未找到 .NET SDK
    pause
    exit /b 1
)

echo .NET 环境正常
echo.

echo 正在构建 GUI 项目...
dotnet build src/RimWorldFramework.GUI/RimWorldFramework.GUI.csproj --verbosity minimal

if %errorlevel% neq 0 (
    echo GUI 项目构建失败！
    pause
    exit /b 1
)

echo GUI 项目构建成功！
echo.

echo 正在构建 StandaloneDemo 项目...
dotnet build src/RimWorldFramework.StandaloneDemo/RimWorldFramework.StandaloneDemo.csproj --verbosity minimal

if %errorlevel% neq 0 (
    echo StandaloneDemo 项目构建失败！
    pause
    exit /b 1
)

echo StandaloneDemo 项目构建成功！
echo.

echo 所有项目构建完成，可以运行 run-demo.bat 启动GUI程序
pause