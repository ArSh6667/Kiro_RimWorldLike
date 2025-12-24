@echo off
chcp 65001 >nul 2>&1
cls
echo ===============================================
echo    简单GUI启动测试
echo ===============================================
echo.

echo 正在启动GUI应用程序...
echo 如果看到图形界面窗口，说明启动成功！
echo.

dotnet run --project src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --configuration Debug

echo.
echo GUI应用程序已退出。
pause