#!/bin/bash

echo "=== RimWorld Game Framework Demo ==="
echo
echo "Checking .NET environment..."

if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not found"
    echo "Please download and install .NET 8.0 SDK from:"
    echo "https://dotnet.microsoft.com/download/dotnet/8.0"
    echo
    read -p "Press any key to exit..."
    exit 1
fi

echo ".NET environment check passed!"
echo
echo "Building project..."
dotnet build src/RimWorldFramework.Demo/RimWorldFramework.Demo.csproj

if [ $? -ne 0 ]; then
    echo "Build failed!"
    read -p "Press any key to exit..."
    exit 1
fi

echo "Build successful!"
echo
echo "Starting game demo..."
echo "(Press 'q' to quit the game)"
echo

dotnet run --project src/RimWorldFramework.Demo/RimWorldFramework.Demo.csproj

read -p "Press any key to exit..."