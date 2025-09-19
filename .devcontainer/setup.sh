#!/bin/bash

# AgentCon Workshop Setup Script
# This script sets up the development environment for the workshop

set -e

echo "🚀 Starting AgentCon Workshop setup..."

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to install Node.js dependencies
setup_nodejs() {
    echo "📦 Setting up Node.js dependencies..."
    
    # Check if package.json exists in frontend directory
    if [ -f "frontend/package.json" ]; then
        echo "  Installing frontend dependencies..."
        cd frontend
        npm install
        cd ..
    else
        echo "  No frontend/package.json found, skipping Node.js setup"
    fi
    
    # Check for package.json in root directory
    if [ -f "package.json" ]; then
        echo "  Installing root dependencies..."
        npm install
    fi
}

# Function to install Python dependencies
setup_python() {
    echo "🐍 Setting up Python dependencies..."
    
    # Upgrade pip first
    python3 -m pip install --upgrade pip
    
    # Install common Python dependencies if requirements.txt files exist
    for req_file in $(find . -name "requirements.txt" -not -path "./venv/*" -not -path "./.venv/*"); do
        if [ -f "$req_file" ]; then
            echo "  Installing dependencies from $req_file..."
            python3 -m pip install -r "$req_file"
        fi
    done
    
    # Check for specific backend directories
    if [ -f "Backend/python/sk/requirements.txt" ]; then
        echo "  Installing Semantic Kernel backend dependencies..."
        python3 -m pip install -r Backend/python/sk/requirements.txt
    fi
    
    if [ -f "Backend/python/langchain/requirements.txt" ]; then
        echo "  Installing LangChain backend dependencies..."
        python3 -m pip install -r Backend/python/langchain/requirements.txt
    fi
}

# Function to setup .NET dependencies
setup_dotnet() {
    echo "🔷 Setting up .NET dependencies..."
    
    # Find and restore .csproj files
    for proj_file in $(find . -name "*.csproj"); do
        if [ -f "$proj_file" ]; then
            echo "  Restoring $proj_file..."
            dotnet restore "$proj_file"
        fi
    done
    
    # Check for solution files
    for sln_file in $(find . -name "*.sln"); do
        if [ -f "$sln_file" ]; then
            echo "  Restoring solution $sln_file..."
            dotnet restore "$sln_file"
        fi
    done
}

# Function to setup environment files
setup_environment() {
    echo "⚙️  Setting up environment configuration..."
    
    # Copy environment template if it exists
    if [ -f "Backend/python/env.template" ] && [ ! -f "Backend/python/.env" ]; then
        echo "  Copying environment template..."
        cp Backend/python/env.template Backend/python/.env
        echo "  ✅ Created Backend/python/.env from template"
        echo "  📝 Please configure your Azure credentials in Backend/python/.env"
    fi
}

# Function to display helpful information
show_info() {
    echo ""
    echo "🎉 Setup complete! Here's what you can do next:"
    echo ""
    echo "📚 Workshop Options:"
    echo "  1. Python Semantic Kernel:"
    echo "     cd Backend/python/sk && uvicorn main:app --reload"
    echo ""
    echo "  2. Python LangChain:"
    echo "     cd Backend/python/langchain && uvicorn main:app --reload"
    echo ""
    echo "  3. .NET Semantic Kernel:"
    echo "     cd Backend/dotnet/sk && dotnet run"
    echo ""
    echo "  4. Frontend (in a new terminal):"
    echo "     cd frontend && npm start"
    echo ""
    echo "📖 Documentation:"
    echo "  - Main README: README.md"
    echo "  - Installation Guide: docs/INSTALL.md"
    echo "  - Environment Guide: docs/ENVIRONMENT_GUIDE.md"
    echo ""
    echo "🔧 Configuration:"
    echo "  - Configure Azure credentials in Backend/python/.env"
    echo "  - Check docs/INSTALL.md for detailed setup instructions"
    echo ""
}

# Main setup process
echo "🔍 Checking environment..."

# Check if we're in the right directory
if [ ! -f "README.md" ] || [ ! -d "Backend" ] || [ ! -d "frontend" ]; then
    echo "❌ Error: This doesn't appear to be the agentcon-workshop repository root"
    echo "   Please run this script from the repository root directory"
    exit 1
fi

# Verify required tools are available
echo "  Checking for required tools..."
if ! command_exists node; then
    echo "❌ Node.js not found. This should be installed by devcontainer features."
    exit 1
fi

if ! command_exists python3; then
    echo "❌ Python 3 not found. This should be installed by devcontainer features."
    exit 1
fi

if ! command_exists dotnet; then
    echo "❌ .NET not found. This should be installed by devcontainer features."
    exit 1
fi

echo "  ✅ All required tools found"

# Run setup functions
setup_environment
setup_python
setup_nodejs
setup_dotnet

# Show completion information
show_info

echo "✨ AgentCon Workshop environment is ready!"