@echo off
echo 基础功能测试...
echo.

echo 临时切换到测试模式...

REM 备份原始App.xaml
if exist "src\RimWorldFramework.GUI\App.xaml.bak" del "src\RimWorldFramework.GUI\App.xaml.bak"
copy "src\RimWorldFramework.GUI\App.xaml" "src\RimWorldFramework.GUI\App.xaml.bak" >nul

REM 备份原始App.xaml.cs
if exist "src\RimWorldFramework.GUI\App.xaml.cs.bak" del "src\RimWorldFramework.GUI\App.xaml.cs.bak"
copy "src\RimWorldFramework.GUI\App.xaml.cs" "src\RimWorldFramework.GUI\App.xaml.cs.bak" >nul

REM 使用测试版本
copy "src\RimWorldFramework.GUI\TestApp.xaml" "src\RimWorldFramework.GUI\App.xaml" >nul
copy "src\RimWorldFramework.GUI\TestApp.xaml.cs" "src\RimWorldFramework.GUI\App.xaml.cs" >nul

echo 正在测试基础WPF功能...
dotnet run --project src\RimWorldFramework.GUI\RimWorldFramework.GUI.csproj

echo.
echo 恢复原始文件...
copy "src\RimWorldFramework.GUI\App.xaml.bak" "src\RimWorldFramework.GUI\App.xaml" >nul
copy "src\RimWorldFramework.GUI\App.xaml.cs.bak" "src\RimWorldFramework.GUI\App.xaml.cs" >nul

del "src\RimWorldFramework.GUI\App.xaml.bak" >nul 2>&1
del "src\RimWorldFramework.GUI\App.xaml.cs.bak" >nul 2>&1

echo 测试完成。
pause