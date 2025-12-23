@echo off
echo === RimWorld Game Framework Standalone Demo ===
echo.
echo This demo shows the core game concepts in action
echo without dependencies on the complex framework components.
echo.
echo Checking .NET environment...

dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Error: .NET SDK not found
    echo Please download and install .NET 8.0 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo .NET environment check passed!
echo.
echo Building standalone demo project...
dotnet build src/RimWorldFramework.StandaloneDemo/RimWorldFramework.StandaloneDemo.csproj

if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo Build successful!
echo.
echo Starting standalone demo...
echo.

dotnet run --project src/RimWorldFramework.StandaloneDemo/RimWorldFramework.StandaloneDemo.csproj

pause