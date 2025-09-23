# Installation Guide

Set up the workshop locally by installing the required tooling, copying the shared environment template, and restoring each service.

## 1. Prerequisites

Install the following software and confirm the versions from a new terminal:

| Tool | Minimum Version | Verify |
| --- | --- | --- |
| Python | 3.11.7 | `python --version`
| .NET SDK | 9.0 | `dotnet --version`
| Node.js | 18.x | `node --version`
| npm | 9.x | `npm --version`
| Git | Latest | `git --version`
| Visual Studio Code (optional) | Latest | Launch VS Code and install the Python, Jupyter, and C# Dev Kit extensions |

> 💡 Working with Azure resources? Install the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) and run `az login`.

## 2. Configure environment variables

All backends read secrets from a single `.env` file. Copy the template once at the repo root and edit the values:

```powershell
cd Backend
copy env.template .env      # Windows PowerShell
# or
cp env.template .env        # macOS/Linux
```

Update the new `Backend/.env` with your Azure OpenAI endpoint, API key, deployment name, and (optionally) Azure AI Foundry project settings. The file is ignored by Git.

## 3. Set up backends

### Python services (LangChain or Semantic Kernel)

Create a virtual environment at the repository root, activate it, and then install dependencies for the backend(s) you plan to use:

```powershell
# Create virtual environment at repo root
python -m venv .venv
.\.venv\Scripts\activate      # `source .venv/bin/activate` on macOS/Linux
```

Install dependencies:

```powershell
# LangChain backend
pip install -r Backend\python\langchain\requirements.txt

# Semantic Kernel backend (run this only if you need the SK service)
pip install -r Backend\python\sk\requirements.txt
```

### .NET Semantic Kernel API

```powershell
cd Backend\dotnet\sk
dotnet restore
dotnet build
```

## 4. Set up the frontend

```powershell
cd frontend
npm install
```

## 5. Quick validation

Run these lightweight checks before starting the services:

```powershell
python --version
dotnet --version
node --version
python -c "import fastapi"
dotnet build Backend\dotnet\sk\DotNetSemanticKernel.csproj
```

## 6. Next steps

With the tooling installed and dependencies restored, follow the [Azure AI Services Guide](AI_SERVICES_GUIDE.md) to provision Azure resources, then start the backend(s) and frontend per their README instructions.