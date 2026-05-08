#!/bin/bash

# Build script for Nanobot C# .NET 10 migration

echo "🐈 Building Nanobot C# (.NET 10)..."

# Check if .NET is installed
if ! command -v dotnet &> /dev/null
then
    echo "❌ .NET SDK is not installed or not in PATH"
    echo "Please install .NET 10 SDK from: https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo "📌 .NET Version: $DOTNET_VERSION"

# Restore packages
echo "📦 Restoring NuGet packages..."
dotnet restore Nanobot.sln

if [ $? -ne 0 ]; then
    echo "❌ Package restore failed"
    exit 1
fi

# Build solution
echo "🔨 Building solution..."
dotnet build Nanobot.sln --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful!"
echo ""
echo "🚀 To run:"
echo "  dotnet run --project src/Nanobot.CLI -- --help"
echo "  dotnet run --project src/Nanobot.CLI -- onboard"
echo "  dotnet run --project src/Nanobot.CLI -- agent"
