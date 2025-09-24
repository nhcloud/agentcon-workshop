# .NET Semantic Kernel Agents Workshop

Enterprise-grade .NET 9 backend that mirrors the Python Semantic Kernel experience. It loads the same YAML agent catalog, supports Azure AI Foundry agents, and exposes a frontend-friendly API for multi-agent chat.

## 🚀 Quick Start

### Prerequisites
- .NET 9 SDK
- VS Code (C# Dev Kit) or Visual Studio 2022 17.10+
- Azure OpenAI resource
- (Optional) Azure AI Foundry project with people/knowledge agents

### Configure Secrets

```powershell
cd Backend/dotnet/sk
copy ..\..\env.template .env
# edit .env to add Azure OpenAI + Foundry values
```

The application reads from `.env`, `config.yml`, environment variables, or `appsettings*.json` (in that order). If the YAML is missing the API falls back to baked-in defaults.

### Run the API

```powershell
dotnet restore
dotnet run
```

Default URLs:
- Swagger UI → `http://localhost:8000`
- Health check → `GET http://localhost:8000/health`
- Agents list → `GET http://localhost:8000/agents`

> Set `PORT=8000` or any other value in `.env` to change the listening port. HTTPS is disabled by default for local dev to avoid certificate friction.

## ⚙️ Configuration Model

### `.env`

```env
AZURE_OPENAI_ENDPOINT=https://<resource>.openai.azure.com
AZURE_OPENAI_API_KEY=<key>
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o
AZURE_OPENAI_API_VERSION=2024-10-21

PROJECT_ENDPOINT=https://<resource>.services.ai.azure.com/api/projects/<project>
PEOPLE_AGENT_ID=asst-people-agent
KNOWLEDGE_AGENT_ID=asst-knowledge-agent
MANAGED_IDENTITY_CLIENT_ID=<client-id-if-using-UMI>

FRONTEND_URL=http://localhost:3001
LOG_LEVEL=Information
```

### `config.yml`

`config.yml` mirrors the Python backend and ships with:
- App metadata + logging defaults
- Azure OpenAI + Azure AI Foundry connection info
- Agent definitions (instructions/metadata/provider)
- Regex + SK routing configuration
- Group chat templates reused by the frontend + notebook

`Program.cs` uses `Env.Load()` + `NetEscapades.Configuration.Yaml` to merge YAML with environment variables at runtime. New sections can be bound via `Configuration.Bind<T>()` using the `Configuration/AgentConfig.cs` types.

## 🧩 Architecture Overview

- **Program.cs** wires YAML, Swagger, long-running request limits, and CORS
- **Configuration/AgentConfig.cs** holds strongly-typed YAML config sections
- **Services/AgentInstructionsService.cs** overrides default prompts based on YAML entries
- **Services/AgentService.cs** caches Azure AI Foundry agents and falls back to Azure OpenAI
- **Controllers** expose `/agents`, `/chat`, `/group-chat`, `/reset`, `/health`

```
sk/
├── Agents/
│   ├── BaseAgent.cs
│   └── SpecificAgents.cs
├── Configuration/
│   ├── AgentConfig.cs
│   └── AzureAIConfig.cs
├── Controllers/
│   ├── AgentsController.cs
│   ├── ChatController.cs
│   └── GroupChatController.cs
├── Services/
│   ├── AgentInstructionsService.cs
│   ├── AgentService.cs
│   ├── GroupChatService.cs
│   └── SessionManager.cs
├── Models/
│   └── ChatModels.cs
├── config.yml
├── Program.cs
└── DotNetSemanticKernel.csproj
```

## 🔌 Azure Integration

- `Microsoft.SemanticKernel.Agents.AzureAI` + `Azure.AI.Projects` for Foundry agents
- `DefaultAzureCredential` with optional managed identity client ID
- Extended HTTP/Kestrel timeouts to handle long-running Foundry conversations
- `/health` endpoint surfaces whether Azure OpenAI / Foundry settings are present

## 📡 API Summary

| Endpoint | Description |
|----------|-------------|
| `GET /health` | Overall status + configured providers |
| `GET /agents` | List available agents (Foundry + fallback) |
| `POST /chat` | Single-agent chat with conversation history replay |
| `POST /group-chat` | Multi-agent orchestration (supports Foundry agents) |
| `POST /reset` | Frontend-aligned session reset |

All routes are root-relative (`/chat`, `/agents`, etc.) to keep the React frontend wiring simple. Swagger (`/swagger`) is enabled in Development mode.

## 🧪 Quick Smoke Tests

```powershell
# Health check
curl http://localhost:8000/health

# Chat with the generic agent
curl -X POST http://localhost:8000/chat \
     -H "Content-Type: application/json" \
     -d '{"message": "Summarise the workshop focus areas."}'

# Group chat using hybrid routing
curl -X POST http://localhost:8000/group-chat \
     -H "Content-Type: application/json" \
     -d '{"message": "Who should help onboard new engineers?", "agents": ["generic_agent","people_lookup"]}'
```

## 🛠️ Extending Agents

1. Add instructions + metadata to `config.yml` under `agents:`.
2. Implement a new class inheriting `BaseAgent` (or reuse `GenericAgent`, `PeopleLookupAgent`, etc.).
3. Register the agent in `AgentService` by adding to `_agentFactories`.
4. Update routing patterns if the agent should participate in auto-selection.

Because `AgentInstructionsService` binds YAML at startup you can adjust prompts without recompiling.

## 🔒 Best Practices & Troubleshooting

- **Environment**: `.env` overrides everything; restart after edits so `DotNetEnv` reloads values.
- **YAML typos**: invalid YAML will be logged at startup and the system will fall back to default instructions.
- **Foundry agent errors**: ensure IDs don’t contain unresolved `${...}` placeholders and `PROJECT_ENDPOINT` is HTTPS.
- **Long responses**: timeouts bumped to ≥2 minutes for chat and 5 minutes for group chat; adjust in `Program.cs` if needed.
- **CORS**: `AllowFrontend` policy allows all origins in Development, otherwise uses `FRONTEND_URL`.

## 📚 Related Material

- [Root project README](../../../README.md)
- [Python Semantic Kernel backend](../sk/README.md) for parity details
- [Azure AI Services Guide](../../../docs/AI_SERVICES.md)
- [Installation checklist](../../../docs/INSTALL.md)

---

With the shared YAML + env template, you can swap between Python and .NET implementations without reconfiguring Azure resources. Hack on whichever stack you prefer while keeping behaviour consistent.