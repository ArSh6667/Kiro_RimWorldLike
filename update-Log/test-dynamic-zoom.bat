@echo off
echo ========================================
echo 动态缩放边界调整测试
echo ========================================
echo.
echo 测试说明:
echo 1. 启动游戏后，按ESC打开菜单
echo 2. 使用WASD移动到地图边缘
echo 3. 使用鼠标滚轮放大缩小
echo 4. 观察相机是否始终保持在地图范围内
echo.
echo 预期行为:
echo - 在地图边缘放大时，相机自动向内调整
echo - 在任何缩放级别都不会显示地图外区域
echo - 缩放操作流畅，无突兀的跳跃
echo.
echo 正在启动游戏进行测试...
echo.

cd /d "%~dp0"
dotnet run --project ../src/RimWorldFramework.GUI/RimWorldFramework.GUI.csproj

pause