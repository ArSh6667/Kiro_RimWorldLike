@echo off
chcp 65001 >nul
echo === RimWorld 简化GUI演示 ===
echo.

echo 检查 .NET 环境...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo 错误: 未找到 .NET SDK
    echo 请安装 .NET 8.0 SDK
    pause
    exit /b 1
)

echo .NET 环境正常
echo.

echo 正在构建简化GUI项目...
dotnet build src/RimWorldFramework.GUI/RimWorldFramework.GUI.csproj --verbosity minimal --nologo

if %errorlevel% neq 0 (
    echo 构建失败！
    echo.
    echo 显示详细错误信息:
    dotnet build src/RimWorldFramework.GUI/RimWorldFramework.GUI.csproj --verbosity normal
    pause
    exit /b 1
)

echo 构建成功！
echo.
echo 正在启动简化版GUI...
echo 这个版本包含:
echo - 主菜单 (开始游戏/结束游戏按钮)
echo - 游戏演示窗口 (模拟游戏运行)
echo - 内置演示内容 (不依赖外部项目)
echo.

dotnet run --project src/RimWorldFramework.GUI/RimWorldFramework.GUI.csproj

echo.
echo 程序已退出。
pause