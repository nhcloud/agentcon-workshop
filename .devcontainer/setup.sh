#!/bin/bash
set -e

echo "Installing Node.js dependencies (if package.json exists)..."
if [ -f package.json ]; then
  npm install
fi

echo "Installing Python dependencies (if requirements.txt exists)..."
if [ -f requirements.txt ]; then
  pip install --upgrade pip
  pip install -r requirements.txt
fi

echo "Restoring .NET projects (if any *.csproj exists)..."
find . -name "*.csproj" -exec dotnet restore {} \;

echo "Setup complete!"