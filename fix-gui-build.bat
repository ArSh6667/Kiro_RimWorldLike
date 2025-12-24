@echo off
chcp 65001 >nul 2>&1
cls
echo ===============================================
echo    GUI构建问题修复工具
echo ===============================================
echo.

echo [1] 检查.NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET SDK未安装，请先运行: setup-dotnet.bat
    pause
    exit /b 1
)
echo ✅ .NET SDK可用

echo.
echo [2] 清理构建缓存...
if exist "src\RimWorldFramework.GUI\bin" (
    rmdir /s /q "src\RimWorldFramework.GUI\bin"
    echo ✅ 清理bin目录
)
if exist "src\RimWorldFramework.GUI\obj" (
    rmdir /s /q "src\RimWorldFramework.GUI\obj"
    echo ✅ 清理obj目录
)

echo.
echo [3] 清理NuGet缓存...
dotnet nuget locals all --clear >nul 2>&1
echo ✅ NuGet缓存已清理

echo.
echo [4] 恢复包依赖...
echo 正在恢复NuGet包...
dotnet restore src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --verbosity minimal
if %errorlevel% neq 0 (
    echo ❌ 包恢复失败
    echo.
    echo 尝试强制恢复...
    dotnet restore src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --force --verbosity normal
    if %errorlevel% neq 0 (
        echo ❌ 强制恢复也失败，可能是网络问题
        pause
        exit /b 1
    )
)
echo ✅ 包恢复成功

echo.
echo [5] 检查关键文件...
set MISSING_FILES=0

if not exist "src\RimWorldFramework.GUI\App.xaml" (
    echo ❌ 缺失: App.xaml
    set MISSING_FILES=1
)

if not exist "src\RimWorldFramework.GUI\SimpleMainWindow.xaml" (
    echo ❌ 缺失: SimpleMainWindow.xaml
    set MISSING_FILES=1
)

if %MISSING_FILES%==1 (
    echo.
    echo ❌ 发现缺失的关键文件！
    echo 请检查项目完整性或重新下载项目文件。
    pause
    exit /b 1
)
echo ✅ 关键文件完整

echo.
echo [6] 尝试构建...
echo 正在构建GUI项目...
dotnet build src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --configuration Debug --verbosity normal

if %errorlevel%==0 (
    echo.
    echo ✅ 构建成功！
    echo.
    echo [7] 测试运行...
    echo 尝试启动应用程序...
    timeout /t 2 >nul
    dotnet run --project src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj --configuration Debug --no-build
    
    if %errorlevel%==0 (
        echo ✅ 应用程序运行成功！
    ) else (
        echo ⚠️ 构建成功但运行时出现问题
        echo 这可能是运行时依赖或WPF兼容性问题
    )
) else (
    echo.
    echo ❌ 构建失败！
    echo.
    echo 常见解决方案:
    echo 1. 检查Windows版本是否支持WPF (.NET 8需要Windows 10+)
    echo 2. 确认安装的是.NET SDK而不是Runtime
    echo 3. 检查XAML文件语法
    echo 4. 检查C#代码编译错误
    echo.
    echo 运行详细诊断获取更多信息: diagnose-build-error.bat
)

echo.
echo 修复尝试完成。
pause