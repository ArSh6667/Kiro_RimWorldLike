@echo off
echo ========================================
echo 人物平滑移动测试
echo ========================================
echo.
echo 测试说明:
echo 1. 启动游戏后，按ESC打开菜单
echo 2. 点击"开始游戏"让人物开始移动
echo 3. 观察人物是否平滑移动而不是跳跃
echo 4. 启用"跟随人物"观察相机跟随效果
echo.
echo 预期行为:
echo - 人物在格子间平滑移动，无跳跃
echo - 移动速度适中，约0.27秒/格
echo - 对角线移动时间约0.38秒
echo - 跟随模式下相机平滑跟随
echo.
echo 技术参数:
echo - 移动速度: 2.0像素/帧
echo - 更新频率: 60FPS
echo - 单格距离: 32像素
echo.
echo 正在启动游戏进行测试...
echo.

cd /d "%~dp0"
dotnet run --project ../src/RimWorldFramework.GUI/RimWorldFramework.GUI.csproj

pause