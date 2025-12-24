@echo off
chcp 65001 >nul 2>&1
cls
echo ===============================================
echo    GUI项目文件完整性检查
echo ===============================================
echo.

set GUI_PATH=src\RimWorldFramework.GUI

echo [检查] 必要的项目文件...
echo.

echo 项目文件:
if exist "%GUI_PATH%\RimWorldFramework.GUI.csproj" (
    echo ✅ RimWorldFramework.GUI.csproj
) else (
    echo ❌ RimWorldFramework.GUI.csproj
    set ERROR_FOUND=1
)

echo.
echo App文件:
if exist "%GUI_PATH%\App.xaml" (
    echo ✅ App.xaml
) else (
    echo ❌ App.xaml
    set ERROR_FOUND=1
)

if exist "%GUI_PATH%\App.xaml.cs" (
    echo ✅ App.xaml.cs
) else (
    echo ❌ App.xaml.cs
    set ERROR_FOUND=1
)

echo.
echo 主窗口文件:
if exist "%GUI_PATH%\SimpleMainWindow.xaml" (
    echo ✅ SimpleMainWindow.xaml
) else (
    echo ❌ SimpleMainWindow.xaml
    set ERROR_FOUND=1
)

if exist "%GUI_PATH%\SimpleMainWindow.xaml.cs" (
    echo ✅ SimpleMainWindow.xaml.cs
) else (
    echo ❌ SimpleMainWindow.xaml.cs
    set ERROR_FOUND=1
)

echo.
echo 游戏窗口文件:
if exist "%GUI_PATH%\SimpleGameWindow.xaml" (
    echo ✅ SimpleGameWindow.xaml
) else (
    echo ❌ SimpleGameWindow.xaml
    set ERROR_FOUND=1
)

if exist "%GUI_PATH%\SimpleGameWindow.xaml.cs" (
    echo ✅ SimpleGameWindow.xaml.cs
) else (
    echo ❌ SimpleGameWindow.xaml.cs
    set ERROR_FOUND=1
)

echo.
echo 游戏世界窗口文件:
if exist "%GUI_PATH%\GameWorldWindow.xaml" (
    echo ✅ GameWorldWindow.xaml
) else (
    echo ❌ GameWorldWindow.xaml
    set ERROR_FOUND=1
)

if exist "%GUI_PATH%\GameWorldWindow.xaml.cs" (
    echo ✅ GameWorldWindow.xaml.cs
) else (
    echo ❌ GameWorldWindow.xaml.cs
    set ERROR_FOUND=1
)

echo.
echo 其他窗口文件:
if exist "%GUI_PATH%\GameWindow.xaml" (
    echo ✅ GameWindow.xaml
) else (
    echo ⚠️ GameWindow.xaml (可选)
)

if exist "%GUI_PATH%\GameWindow.xaml.cs" (
    echo ✅ GameWindow.xaml.cs
) else (
    echo ⚠️ GameWindow.xaml.cs (可选)
)

if exist "%GUI_PATH%\MainWindow.xaml" (
    echo ✅ MainWindow.xaml
) else (
    echo ⚠️ MainWindow.xaml (可选)
)

if exist "%GUI_PATH%\MainWindow.xaml.cs" (
    echo ✅ MainWindow.xaml.cs
) else (
    echo ⚠️ MainWindow.xaml.cs (可选)
)

echo.
echo ===============================================
if defined ERROR_FOUND (
    echo ❌ 发现缺失的必要文件！
    echo.
    echo 解决方案:
    echo 1. 检查文件是否被意外删除
    echo 2. 从备份恢复缺失的文件
    echo 3. 重新创建缺失的文件
) else (
    echo ✅ 所有必要文件都存在
    echo.
    echo 如果构建仍然失败，可能的原因:
    echo 1. XAML语法错误
    echo 2. C#代码编译错误
    echo 3. 命名空间不匹配
    echo 4. .NET SDK版本问题
    echo.
    echo 运行详细诊断: diagnose-build-error.bat
)
echo ===============================================

echo.
pause