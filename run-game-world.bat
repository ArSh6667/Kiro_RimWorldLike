@echo off
chcp 65001 >nul
echo === RimWorld 游戏世界演示 ===
echo.
echo 🌍 特性:
echo - 256×256格地图 (每格32像素)
echo - 基于噪声的地形生成
echo - 四种地形: 深水/岩石(黑), 沙地(浅黄), 草地(浅绿), 雪地(白)
echo - 1个红色小圆代表人物
echo - 人物随机移动
echo - 可缩放和滚动的地图视图
echo - 跟随人物功能
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

echo 正在构建游戏世界项目...
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
echo 正在启动游戏世界...
echo.
echo 使用说明:
echo 1. 点击"进入游戏世界"按钮
echo 2. 点击"开始游戏"让人物开始移动
echo 3. 使用鼠标滚轮缩放地图
echo 4. 拖拽地图或点击"跟随人物"
echo 5. 观察人物在不同地形上的随机移动
echo.

dotnet run --project src/RimWorldFramework.GUI/RimWorldFramework.GUI.csproj

echo.
echo 程序已退出。
pause