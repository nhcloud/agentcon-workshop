# Workshop Exercise Walkthrough

Follow this sequence to experience the full agent workshop stack across .NET and Python implementations.

## Step 1 – Prepare your local environment

Complete the tooling and project setup described in [`docs/INSTALL.md`](docs/INSTALL.md). When finished you should have:

- Required runtimes (Python 3.11.7+, .NET 9, Node.js 18.x, npm 9.x)
- A repo-root virtual environment (`.venv`) activated
- Dependencies restored for any services you plan to run
- The shared environment file copied to `Backend/.env`

## Step 2 – Configure Azure resources

Provision Azure OpenAI (and optionally Azure AI Foundry) resources by following [`docs/AI_SERVICES.md`](AI_SERVICES.md). Capture the endpoint URLs, API keys, deployment names, and agent identifiers you will paste into `Backend/.env`.

## Step 3 – Explore the .NET Semantic Kernel workshop

1. Launch VS Code or Jupyter and open `Backend/dotnet/sk/workshop_dotnet_semantic_kernel.ipynb`.
2. Select the **.NET Interactive** kernel (C#) when prompted.
3. Work through the cells in order:
   - Environment verification and package restore (#r directives)
   - `.env` loading and Azure OpenAI configuration checks
   - Semantic Kernel agent creation and group chat demos
   - Configuration-based agents that mirror the YAML catalog

The notebook mirrors the backend runtime, so completing it ensures your configuration is valid.

## Step 4 – Run the .NET Semantic Kernel API

From the repo root (with `.venv` activated for consistency) start the backend:

```powershell
cd Backend\dotnet\sk
dotnet restore
dotnet run
```

The API hosts at `http://localhost:8000`. Visit `/swagger` for interactive docs or call `/health` to make sure Azure settings are loaded.

## Step 5 – Explore the Python LangChain workshop

1. Open `Backend/python/langchain/workshop_langchain_agents.ipynb` in VS Code or Jupyter.
2. Use the repo-root virtual environment as the kernel (`.venv`).
3. Execute the cells sequentially:
   - Environment & dependency installer (installs `requirements.txt` automatically)
   - Configuration inspection (`config.yml`, `.env`)
   - Agent creation, routing, and group chat samples
   - Azure AI Foundry examples (skipped gracefully if not configured)

## Step 6 – Run the Python LangChain API

With the virtual environment active:

```powershell
cd Backend\python\langchain
uvicorn main:app --reload --port 8000
```

The service exposes FastAPI docs at `http://localhost:8000/docs`, plus health and chat endpoints that align with the frontend expectations.

## Step 7 – Explore the Python Semantic Kernel workshop

1. Open `Backend/python/sk/workshop_semantic_kernel_agents.ipynb`.
2. Attach the same `.venv` kernel to reuse installed packages.
3. Run each section in order to:
   - Validate Azure OpenAI and (optional) Azure AI Foundry credentials
   - Instantiate generic ChatCompletion agents
   - Exercise the hybrid router and group chat orchestrator
   - Compare YAML-driven instructions with live agent responses

## Step 8 – Run the Python Semantic Kernel API

Still inside the virtual environment, start the SK backend:

```powershell
cd Backend\python\sk
uvicorn main:app --reload --port 8001
```

A different port (8001) avoids clashing with the LangChain service if you keep both running. FastAPI docs are available at `http://localhost:8001/docs`.

## Step 9 – Run the React frontend

With at least one backend running, start the UI from the repo root:

```powershell
cd frontend
npm start             # launches on http://localhost:3001
# macOS/Linux: PORT=3001 npm start
```

The frontend proxies API calls to `http://localhost:8000` by default (see `frontend/package.json`). If you are running a backend on a different port, update the `proxy` entry or set `REACT_APP_API_BASE` accordingly before launching.

---

Once each backend is up, you can point the React frontend or API clients at the desired port to compare behaviours across frameworks. Feel free to iterate between notebooks and running services as you experiment with configuration changes.
