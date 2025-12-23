@echo off
echo === RimWorld Game Framework Demo ===
echo.
echo NOTE: The full framework demo has compilation issues due to missing base classes.
echo Running the standalone demo instead, which demonstrates all core concepts.
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
echo Starting RimWorld game framework demo...
echo This demo shows all core concepts: ECS, Characters, Skills, Tasks, and Game Loop
echo.

dotnet run --project src/RimWorldFramework.StandaloneDemo/RimWorldFramework.StandaloneDemo.csproj

pause