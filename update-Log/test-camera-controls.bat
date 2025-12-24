@echo off
chcp 65001 >nul 2>&1
cls
echo ===============================================
echo    相机控制功能测试
echo ===============================================
echo.

echo 🎮 相机控制功能已添加到游戏世界！
echo.
echo 📋 测试清单:
echo ✅ WASD键移动视角
echo ✅ 方向键移动视角  
echo ✅ 鼠标滚轮缩放
echo ✅ 空格键居中到人物
echo ✅ R键重置缩放
echo ✅ F键切换跟随模式
echo ✅ 实时状态显示
echo ✅ UI控制说明
echo.

echo 🚀 启动游戏世界测试相机控制:
echo.
echo 1. 运行游戏世界
echo 2. 点击"进入游戏世界"按钮
echo 3. 测试以下控制:
echo    • 按WASD键移动视角
echo    • 滚动鼠标滚轮缩放
echo    • 按空格键居中
echo    • 按R键重置
echo    • 按F键切换跟随
echo 4. 观察状态栏的实时信息更新
echo.

set /p start_test="是否现在启动游戏世界进行测试? (y/n): "
if /i "%start_test%"=="y" (
    echo.
    echo 正在启动游戏世界...
    cd ..
    call run-game-world.bat
) else (
    echo.
    echo 您可以稍后运行以下命令测试:
    echo run-game-world.bat
)

echo.
echo 📖 详细说明文档: update-Log\CAMERA-CONTROLS.md
pause