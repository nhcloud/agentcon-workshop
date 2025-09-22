# Azure Multi-Agent Workshop 

A comprehensive workshop for building sophisticated AI agent applications using Azure OpenAI services with multiple implementation options. Learn to create chatbot applications with specialized agent routing, group chat capabilities, and modern React frontends. The workshop provides both Python (Semantic Kernel & LangChain) and .NET implementations to demonstrate multi-agent architectures.

## Workshop Features 

- 🤖 **Multiple Implementation Options**:
  - Python Semantic Kernel implementation  
  - Python LangChain implementation  
  - .NET Semantic Kernel implementation
- 🔄 **Multi-Agent Architecture**: Agent routing and group chat scenarios
- 🌐 **Modern React Frontend**: Professional chat interface
- ⚡ **FastAPI Backend**: High-performance async API
- 🔄 **Real-time Chat**: Server-Sent Events (SSE) streaming
- 📊 **Token Usage Tracking**: Monitor AI service consumption
- 💾 **Session Management**: Persistent chat history
- 🎨 **Code Examples**: Comprehensive configuration samples
- 📱 **Production Ready**: Azure authentication and security
- � **Comprehensive Documentation**: Step-by-step guides

## Prerequisites

- Python 3.13+ with pip
- Node.js 18+ with npm
- Azure CLI installed
- Azure Account with:
  - Azure OpenAI Service
  - Azure AI Service (for agent deployment)
  - Azure Active Directory (for authentication)
  - Azure Project Service (for AI agent management)

## Environment Setup

1. Install Azure CLI (if not already installed):
```bash
# Windows (PowerShell Admin)
winget install -e --id Microsoft.AzureCLI

# macOS
brew install azure-cli

# Ubuntu/Debian
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

2. Login to Azure:
```bash
# Interactive login
az login

# If you have multiple subscriptions, set the right one
az account list
az account set --subscription <subscription-id>

# Verify selected subscription
az account show
```

3. Clone the repository:
```bash
git clone <repository-url>
cd agents-workshop
```

4. Create and activate a Python virtual environment:
```bash
python -m venv venv
# Windows
.\venv\Scripts\activate
# Unix/MacOS
source venv/bin/activate
```

5. Install Python dependencies:
```bash
cd backend/python/sk
pip install -r requirements.txt
cd ../../..
```

Required Python packages:
- **Semantic Kernel implementation**: semantic-kernel, azure-ai-projects, azure-identity
- **LangChain implementation**: langchain, langchain-azure-ai, langchain-core
- **Common dependencies**: python-dotenv, fastapi, uvicorn

6. Set up frontend dependencies:
```bash
cd frontend
npm install
cd ..
```

7. Create a `.env` file based on the template:
```bash
cp backend/python/env.template backend/python/.env
```

8. Configure the following environment variables in `.env`:
```
# Azure OpenAI Configuration
AZURE_OPENAI_ENDPOINT=https://your-resource-name.openai.azure.com
AZURE_OPENAI_DEPLOYMENT=gpt-4o
AZURE_OPENAI_KEY=your-azure-openai-api-key-here

# Azure AI Foundry Configuration  
PROJECT_ENDPOINT=https://your-resource-name.services.ai.azure.com/api/projects/your-project-name
AGENT_ID=your-agent-id-here

# Application Configuration
FRONTEND_URL=http://localhost:3000
```

9. Verify Azure services access:
```bash
# Verify OpenAI service access
az cognitiveservices account list

# Verify AI service access
az ai project list
```

## Running the Application

### Backend Options

Choose one implementation based on your preference:

#### 🐍 Python Implementations
```bash
# LangChain Framework
cd Backend/python/langchain
uvicorn main:app --reload

# Semantic Kernel Framework  
cd Backend/python/sk
uvicorn main:app --reload
```

#### 🔷 .NET Implementation
```bash
# .NET 9 with Semantic Kernel
cd Backend/dotnet/sk
dotnet watch run
```

### Frontend
```bash
cd frontend
npm start
```

### Access Points
- **Frontend**: `http://localhost:3000`
- **API Documentation**: `http://localhost:8000/docs`
- **API ReDoc**: `http://localhost:8000/redoc`

📚 **For detailed setup instructions, see the framework-specific README files:**
- **Python**: [Backend/python/README.md](Backend/python/README.md)
- **.NET**: [Backend/dotnet/README.md](Backend/dotnet/README.md)

## Quick Start

Choose your preferred implementation and follow the dedicated setup guide:

### 🐍 Python Developers
**Choose your AI framework:**

**Option 1: LangChain** (Recommended for rapid prototyping)
```bash
# See detailed setup guide
📚 Backend/python/README.md

# Quick start
cd Backend/python/langchain
pip install -r requirements.txt
uvicorn main:app --reload
```

**Option 2: Semantic Kernel** (Recommended for enterprise)
```bash
# See detailed setup guide  
📚 Backend/python/README.md

# Quick start
cd Backend/python/sk
pip install -r requirements.txt
uvicorn main:app --reload
```

### 🔷 .NET Developers
**Semantic Kernel with .NET 9:**
```bash
# See detailed setup guide
📚 Backend/dotnet/README.md

# Quick start
cd Backend/dotnet/sk
dotnet restore
dotnet watch run
```

### 🌐 Frontend Developers
**React Application:**
```bash
cd frontend
npm install && npm start
```

📚 **Need detailed instructions?** See the framework-specific README files above for comprehensive setup guides, configuration options, and troubleshooting.

## API Endpoints

### Core Chat Endpoints
- `POST /chat`: Send a chat message
- `POST /chat/stream`: Stream chat responses
- `GET /messages/{session_id}`: Retrieve chat history
- `DELETE /messages/{session_id}`: Delete chat history

### Agent Management
- `GET /agents`: List available agents
- `GET /agents/{agent_name}/info`: Get agent information
- `POST /agents/{agent_name}/toggle`: Enable/disable agent

### Group Chat (Advanced)
- `POST /group-chat`: Create group chat session
- `POST /group-chat/create`: Create group chat with configuration
- `GET /group-chat/{session_id}/summary`: Get chat summary
- `POST /group-chat/{session_id}/reset`: Reset group chat
- `DELETE /group-chat/{session_id}`: Delete group chat
- `GET /group-chats`: List active group chats

### System & Health
- `GET /health`: Health check endpoint
- `GET /system/stats`: System statistics

## Project Structure

```
agents-workshop/
├── 📚 docs/                        # 👈 START HERE: Workshop guides
│   ├── INSTALL.md                  #    Complete setup instructions
│   ├── ENVIRONMENT_GUIDE.md        #    Azure configuration help
│   └── GROUP_CHAT.md              #    Advanced multi-agent scenarios
│
├── 🐍 backend/python/              # Python implementations
│   ├── 🚀 sk/                     # 👈 OPTION 1: Semantic Kernel
│   │   ├── main.py                #    FastAPI server (run this!)
│   │   ├── 📓 workshop_semantic_kernel_agents.ipynb  # Interactive tutorial
│   │   ├── config.yml             #    Configuration file
│   │   ├── example_group_chat.py  #    Group chat demo
│   │   ├── agents/                #    Agent implementations
│   │   ├── routers/               #    API endpoints
│   │   └── requirements.txt       #    Dependencies
│   │
│   ├── 🚀 langchain/              # 👈 OPTION 2: LangChain
│   │   ├── main.py                #    FastAPI server (run this!)
│   │   ├── 📓 workshop_langchain_agents.ipynb       # Interactive tutorial
│   │   ├── config.yml             #    Configuration file
│   │   ├── example_group_chat.py  #    Group chat demo
│   │   ├── agents/                #    Agent implementations
│   │   ├── routers/               #    API endpoints
│   │   └── requirements.txt       #    Dependencies
│   │
│   ├── 🔧 shared/                 # Common utilities (don't modify)
│   │   ├── agents/                #    Base agent classes
│   │   ├── config/                #    Configuration management
│   │   ├── core/                  #    Core functionality
│   │   └── sessions/              #    Session handling
│   │
│   ├── ⚙️ env.template            # 👈 COPY TO .env (configure Azure keys)
│   ├── check_config.py            #    Validate your setup
│   └── validate_env.py            #    Check environment
│
├── 🔷 backend/dotnet/             # .NET implementation
│   ├── sk/                        # 👈 OPTION 3: .NET Semantic Kernel
│   │   ├── Program.cs             #    C# entry point
│   │   ├── 📓 workshop_dotnet_semantic_kernel.ipynb # C# tutorial
│   │   ├── Controllers/           #    ASP.NET controllers
│   │   ├── Agents/                #    Agent classes
│   │   └── appsettings.json       #    Configuration
│   ├── examples/                  #    .NET configuration examples
│   └── agents-workshop.sln        #    Visual Studio solution
│
├── 🌐 frontend/                   # React web interface
│   ├── src/
│   │   ├── App.js                 #    Main application
│   │   ├── components/            #    UI components
│   │   └── services/              #    API communication
│   ├── package.json               #    Frontend dependencies
│   └── PROFESSIONAL_UI_README.md  #    UI setup guide
│
└── 📄 README.md                   # 👈 YOU ARE HERE: Main documentation
```

### 🎯 Workshop Navigation Guide

**🐍 For Python Developers:**
1. 📚 **Start here**: [Backend/python/README.md](Backend/python/README.md) - Complete Python setup guide
2. � **Choose your framework**:
   - **LangChain**: Rapid prototyping, extensive ecosystem
   - **Semantic Kernel**: Enterprise-ready, Microsoft-backed
3. 📓 **Interactive learning**: Use the framework-specific Jupyter notebooks
4. ⚙️ **Configuration**: Copy `backend/python/env.template` to `backend/python/.env`

**🔷 For .NET Developers:**
1. 📚 **Start here**: [Backend/dotnet/README.md](Backend/dotnet/README.md) - Complete .NET setup guide
2. � **Open project**: `backend/dotnet/agentcon-workshop.sln` in Visual Studio
3. 📓 **Interactive tutorial**: `workshop_dotnet_semantic_kernel.ipynb`
4. 🔧 **Quick start**: `dotnet watch run` for hot reload

**🌐 For Frontend Developers:**
1. 🌐 `cd frontend` → `npm install` → `npm start`
2. 📄 Read `PROFESSIONAL_UI_README.md` for UI details
3. 🔗 **API Integration**: Connect to any backend implementation

**📚 For Workshop Instructors:**
1. 📖 **Documentation**: [docs/](docs/) directory for all guides
2. 🎯 **Installation**: [docs/INSTALL.md](docs/INSTALL.md) for complete setup
3. 🌐 **Environment**: [docs/ENVIRONMENT_GUIDE.md](docs/ENVIRONMENT_GUIDE.md) for Azure configuration

## Development

### Backend Development

The backend provides multiple implementation options:

**Python Implementations:**
- **Semantic Kernel (backend/python/sk/)**: Microsoft's Semantic Kernel with Azure AI integration
- **LangChain (backend/python/langchain/)**: LangChain framework with Azure AI services
- **Shared Components (backend/python/shared/)**: Common utilities and base classes

**Key Features:**
- Asynchronous request handling using FastAPI
- Session management with chat history and response caching
- Intelligent agent routing and group chat capabilities
- Azure OpenAI and Azure AI Project integration
- Message hashing for consistent responses
- Streaming responses using Server-Sent Events (SSE)

### Frontend Development

The React frontend features:
- Real-time chat interface
- Code syntax highlighting
- Mobile-responsive design
- Haptic feedback
- Session persistence

## Deployment

1. Build the frontend:
```bash
cd frontend
npm run build
```

2. Deploy the backend:
- Ensure all environment variables are properly set
- Use a production-grade ASGI server (e.g., Gunicorn with Uvicorn workers)
- Set up proper CORS configuration

## Security Considerations

- All Azure credentials should be kept secure
- CORS is configured to allow only the specified frontend URL
- Azure AD authentication is used for API access
- Environment variables are used for sensitive configuration

## Troubleshooting

Common issues and solutions:

1. Connection Issues:
   - Verify Azure credentials and endpoints
   - Check CORS configuration
   - Ensure proper network connectivity

2. Authentication Issues:
   - Verify Azure AD configuration
   - Check token expiration and renewal

3. Agent Issues:
   - Confirm model deployment status
   - Verify API key permissions
   - Check rate limits

## 📚 Documentation

### Framework-Specific Guides
- **[🐍 Python Implementation](Backend/python/README.md)** - Complete Python setup (LangChain & Semantic Kernel)
- **[🔷 .NET Implementation](Backend/dotnet/README.md)** - Complete .NET setup (Semantic Kernel)

### Workshop Documentation
- **[📖 Installation Guide](docs/INSTALL.md)** - Complete setup instructions
- **[⚙️ Environment Guide](docs/ENVIRONMENT_GUIDE.md)** - Azure configuration and credentials
- **[👥 Group Chat Guide](docs/GROUP_CHAT.md)** - Multi-agent conversation patterns

### Framework Comparison
| Feature | Python LangChain | Python SK | .NET SK |
|---------|------------------|-----------|---------|
| **Best For** | Rapid prototyping | Python enterprise | .NET ecosystem |
| **Ecosystem** | Extensive | Growing | Microsoft-backed |
| **Documentation** | [Backend/python/README.md](Backend/python/README.md) | [Backend/python/README.md](Backend/python/README.md) | [Backend/dotnet/README.md](Backend/dotnet/README.md) |
| **Interactive Tutorial** | Jupyter notebook | Jupyter notebook | Jupyter notebook |

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request
