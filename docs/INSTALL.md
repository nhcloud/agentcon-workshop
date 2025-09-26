# Installation Guide

Set up the workshop locally by installing the required tooling, copying the shared environment template, and restoring each service.

## 1. Prerequisites

Install the following software and confirm the versions from a new terminal:

| Tool | Minimum Version | Verify | Link |
| --- | --- | --- | --- |
| Python | 3.11.7 | `python --version` | https://www.python.org/downloads/
| .NET SDK | 9.0 | `dotnet --version` | https://dotnet.microsoft.com/en-us/download/dotnet/9.0
| Node.js | 18.x | `node --version` | https://nodejs.org/en/download
| npm | 9.x | `npm --version` | 
| Git | Latest | `git --version` | https://git-scm.com/downloads
| Visual Studio Code | Latest | Launch VS Code and install the Python, Jupyter, and C# Dev Kit extensions | https://code.visualstudio.com/download |


> 💡 Install these VS Code extensions:
- Python
- Jupyter
- .NET Install Tool
- .NET Extension Pack
- C# Dev Kit
- Azure CLI Tools


> 💡 Working with Azure resources? Install the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) and run `az login`.

## 2. Configure environment variables

All backends read secrets from a single `.env` file. Copy the template once at the repo root and edit the values:

```powershell
# Windows PowerShell (from repo root)
Copy-Item Backend\env.template dotnet\sk\.env
Copy-Item Backend\env.template python\sk\.env
Copy-Item Backend\env.template python\langchain\.env

# macOS/Linux (from repo root)
cp Backend/env.template dotnet/sk/.env
cp Backend/env.template python/sk/.env
cp Backend/env.template python/langchain/.env

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

Install the React dependencies from the repo root:

```powershell
cd frontend
npm install
```

> 💡 If you prefer to stay at the root, you can run `npm install --prefix frontend` instead.

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

With the tooling installed and dependencies restored, follow the [Azure AI Services Guide](AI_SERVICES.md) to provision Azure resources, then start the backend(s) and frontend per their README instructions.