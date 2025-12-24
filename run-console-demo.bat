@echo off
echo === RimWorld Game Framework Console Demo ===
echo.
echo 正在启动控制台版本的RimWorld游戏框架演示...
echo.
echo 检查 .NET 环境...

dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo 错误: 未找到 .NET SDK
    echo 请从以下地址下载并安装 .NET 8.0 SDK:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo .NET 环境检查通过！
echo.
echo 正在构建独立演示项目...
dotnet build src/RimWorldFramework.StandaloneDemo/RimWorldFramework.StandaloneDemo.csproj

if %errorlevel% neq 0 (
    echo 构建失败！
    pause
    exit /b 1
)

echo 构建成功！
echo.
echo 正在启动RimWorld游戏框架演示...
echo 这个演示展示了所有核心概念: ECS、角色、技能、任务和游戏循环
echo.

dotnet run --project src/RimWorldFramework.StandaloneDemo/RimWorldFramework.StandaloneDemo.csproj

pause