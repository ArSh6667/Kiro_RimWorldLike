@echo off
echo 最小化启动测试...
echo.

echo 检查 .NET...
dotnet --version
if %errorlevel% neq 0 (
    echo .NET 不可用
    pause
    exit /b 1
)

echo.
echo 尝试直接运行 GUI 项目...
dotnet run --project src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj

echo.
echo 如果上面失败，尝试控制台版本...
if exist "src\RimWorldFramework.StandaloneDemo\RimWorldFramework.StandaloneDemo.csproj" (
    dotnet run --project src\RimWorldFramework.StandaloneDemo\RimWorldFramework.StandaloneDemo.csproj
)

pause