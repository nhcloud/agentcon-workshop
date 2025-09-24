# Semantic Kernel Python Backend

Modern Semantic Kernel (SK) implementation that mirrors the LangChain and .NET workshops. This backend adds a YAML-driven agent catalog, Azure AI Foundry support, and enterprise-ready routing that matches the latest workshop curriculum.

## 🚀 Quick Start

### Option 1 — Interactive Workshop

1. Open `workshop_semantic_kernel_agents.ipynb` in VS Code or Jupyter.
2. Follow the first section to create/activate a virtual environment and install packages.
3. Run the cells to explore generic agents, Azure AI Foundry agents, and the group-chat orchestrator.

> The notebook uses the same configuration helpers as the API. When Azure credentials are missing it falls back to informative stubs so you can complete the learning path offline.

### Option 2 — Run the FastAPI Service

```powershell
cd Backend/python/sk
python -m venv .venv
.\.venv\Scripts\activate
pip install -r requirements.txt
copy ..\env.template .env  # Windows; use `cp` on macOS/Linux
uvicorn main:app --reload --port 8000
```

Endpoints:
- API root & docs → `http://localhost:8000/docs`
- Health check → `GET http://localhost:8000/health`
- Chat endpoint → `POST http://localhost:8000/chat`

## ⚙️ Configuration

### Environment Variables (`.env`)

The backend reads Azure secrets from `.env` (or the shell environment). Key entries:

```env
# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://<resource>.openai.azure.com
AZURE_OPENAI_API_KEY=<key>
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o
AZURE_OPENAI_API_VERSION=2024-10-21

# Azure AI Foundry (optional but required for enterprise agents)
PROJECT_ENDPOINT=https://<resource>.services.ai.azure.com/api/projects/<project>
PEOPLE_AGENT_ID=asst-people-agent
KNOWLEDGE_AGENT_ID=asst-knowledge-agent
MANAGED_IDENTITY_CLIENT_ID=<client-id-if-using-UMI>

# App settings
FRONTEND_URL=http://localhost:3001
LOG_LEVEL=INFO
PORT=8000
```

> Copy `Backend/env.template` to `.env` for an up-to-date scaffold that includes Azure AI Foundry and managed identity hints.

### YAML Agent Catalog (`config.yml`)

`config.yml` mirrors the .NET version and centralises:
- Agent definitions with instructions, metadata, and framework provider (`azure_openai` or `azure_foundry`).
- Regex + semantic routing rules for the hybrid router.
- Group chat templates that power the notebook demos and API endpoints.

At startup `main.py` loads the YAML via `shared.ConfigFactory`; any missing file falls back to the default presets in code, so you can start from a clean repo.

## 🧠 Architecture Highlights

- **Agents** live in `agents/semantic_kernel_agents.py` with both Azure OpenAI and Azure AI Foundry implementations.
- **Routing** is handled by `routers/semantic_kernel_router.py`, combining pattern heuristics with SK’s history-aware router.
- **Sessions & cache** come from the shared toolkit (`Backend/python/shared`), giving parity with the LangChain backend.
- **Group chat** lives in `agents/agent_group_chat.py` and reads the same template schema as the config file.

```
sk/
├── agents/
│   ├── agent_group_chat.py
│   └── semantic_kernel_agents.py
├── routers/
│   └── semantic_kernel_router.py
├── sessions/
│   └── ... (shared session utilities)
├── config.yml
├── main.py
├── requirements.txt
├── example_group_chat.py
├── example_template_usage.py
└── workshop_semantic_kernel_agents.ipynb
```

## 🔌 Azure Integration

- **Azure OpenAI** via Semantic Kernel with `AzureChatCompletion` services and prompt execution settings.
- **Azure AI Foundry** agents using `azure-ai-projects` and `DefaultAzureCredential`, including managed identity support and HTTPS validation.
- Built-in health check surfaces which services are configured (`/health`).

## 📡 API Surface

| Endpoint | Description |
|----------|-------------|
| `GET /health` | Readiness + provider configuration flags |
| `GET /agents` | Returns enabled agents with provider + capabilities |
| `POST /chat` | Chat with a single agent or route automatically |
| `POST /chat/stream` | Server-sent events streaming variant |
| `POST /group-chat` | Run a multi-agent roundtable using current templates |
| `GET /messages/{session_id}` | Retrieve session transcript |
| `DELETE /messages/{session_id}` | Clear a session |
| `POST /reset` | Thin wrapper for the frontend reset workflow |

The frontend passes `agents: []` for auto-routing or a specific agent name to pin the conversation. Group chat requests reuse the same YAML participants as the examples.

## 🧪 Examples & Notebook

- `example_group_chat.py` spins up a mini roundtable and prints each turn.
- `example_template_usage.py` loads YAML templates and inspects participants.
- `workshop_semantic_kernel_agents.ipynb` installs requirements, validates environment variables, walks through agent creation, Azure Foundry usage, and group-chat orchestration.

Run the validation helper anytime:

```powershell
python validate_structure.py
```

## 🧰 Troubleshooting

| Issue | Fix |
|-------|-----|
| `AgentInitializationException` for Foundry agents | Ensure `PROJECT_ENDPOINT` uses `https://` and agent IDs don’t contain unresolved `${...}` placeholders. |
| `ManagedIdentityCredential authentication unavailable` | Set `MANAGED_IDENTITY_CLIENT_ID` or authenticate via `az login` before running locally. |
| `ModuleNotFoundError` in notebook | Re-run the install cell; it now aligns with `requirements.txt`. |
| Redis session errors | Install Redis locally and set `SESSION_STORAGE_TYPE=redis`. |

## 📚 Related Guides

- [Project README](../../../README.md)
- [Azure AI Services setup](../../../docs/AI_SERVICES.md)
- [Installation checklist](../../../docs/INSTALL.md)
- [Group chat deep dive](../../../docs/GROUP_CHAT.md)

---

Ready to customise? Tweak `config.yml`, add new agents under `agents/`, and extend the router with your own heuristics. The Python and .NET backends now share the same configuration story, making cross-platform experimentation painless.