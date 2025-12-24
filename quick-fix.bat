@echo off
echo ===============================================
echo    RimWorld 框架 - 快速修复工具
echo ===============================================
echo.

echo [1] 清理项目缓存...
if exist "src\RimWorldFramework.GUI\bin" rmdir /s /q "src\RimWorldFramework.GUI\bin"
if exist "src\RimWorldFramework.GUI\obj" rmdir /s /q "src\RimWorldFramework.GUI\obj"
echo ✅ 缓存已清理

echo.
echo [2] 恢复 NuGet 包...
dotnet restore src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --verbosity quiet
if %errorlevel%==0 (
    echo ✅ 包恢复成功
) else (
    echo ❌ 包恢复失败
)

echo.
echo [3] 重新构建项目...
dotnet build src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --verbosity quiet
if %errorlevel%==0 (
    echo ✅ 构建成功
    echo.
    echo 修复完成！现在可以尝试运行:
    echo • run-demo.bat
    echo • run-simple-gui.bat  
    echo • run-game-world.bat
) else (
    echo ❌ 构建失败
    echo.
    echo 请尝试:
    echo • debug-gui.bat (详细诊断)
    echo • run-console-demo.bat (备用方案)
)

echo.
pause