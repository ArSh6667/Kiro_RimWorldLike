@echo off
echo ========================================
echo 鼠标控制增强功能测试
echo ========================================
echo.
echo 测试说明:
echo 1. 启动游戏后，按ESC打开菜单
echo 2. 测试鼠标中键拖动平移功能
echo 3. 测试以鼠标为中心的缩放功能
echo.
echo 测试项目:
echo.
echo 【鼠标中键拖动】
echo - 按住鼠标中键并拖动
echo - 观察视图是否跟随鼠标移动
echo - 检查光标是否变为移动图标
echo - 验证拖动时是否取消人物跟随
echo.
echo 【以鼠标为中心缩放】
echo - 将鼠标指向地图某个位置
echo - 滚动鼠标滚轮进行缩放
echo - 观察鼠标指向的位置是否保持不变
echo - 测试不同缩放级别的效果
echo.
echo 预期行为:
echo - 中键拖动: 视图平滑跟随鼠标移动
echo - 缩放中心: 鼠标位置作为缩放中心点
echo - 边界限制: 所有操作都不超出地图边界
echo - 操作流畅: 响应及时，无延迟感
echo.
echo 正在启动游戏进行测试...
echo.

cd /d "%~dp0"
dotnet run --project ../src/RimWorldFramework.GUI/RimWorldFramework.GUI.csproj

pause