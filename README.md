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

- Python 3.8+ with pip
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

1. Start the backend server (choose one):

   **For Semantic Kernel implementation:**
   ```bash
   cd backend/python/sk
   uvicorn main:app --reload
   ```

   **For LangChain implementation:**
   ```bash
   cd backend/python/langchain
   uvicorn main:app --reload
   ```

2. Start the frontend development server:
```bash
# From the frontend directory
cd frontend
npm start
```

3. Access the application at `http://localhost:3000`

## Quick Start

For workshop attendees who want to get started quickly:

1. **Clone and setup**:
   ```bash
   git clone <repository-url>
   cd agents-workshop
   cp backend/python/env.template backend/python/.env
   ```

2. **Configure Azure credentials** in `backend/python/.env`

3. **Run Semantic Kernel implementation**:
   ```bash
   cd backend/python/sk
   pip install -r requirements.txt
   uvicorn main:app --reload
   ```

4. **Run frontend** (in new terminal):
   ```bash
   cd frontend
   npm install && npm start
   ```

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
├── backend/                    # Backend implementations
│   ├── python/                # Python implementations
│   │   ├── shared/            # Shared utilities and components
│   │   ├── sk/               # Semantic Kernel implementation
│   │   │   ├── main.py       # FastAPI application
│   │   │   └── requirements.txt
│   │   ├── langchain/        # LangChain implementation
│   │   │   ├── main.py       # FastAPI application
│   │   │   └── requirements.txt
│   │   ├── examples/         # Python configuration examples
│   │   └── env.template      # Environment variables template
│   └── dotnet/               # .NET implementations
│       ├── sk/               # Semantic Kernel .NET implementation
│       ├── examples/         # .NET configuration examples
│       └── agents-workshop.sln
├── frontend/                 # React frontend application
│   ├── src/
│   │   ├── components/       # React components
│   │   ├── services/         # API service layer
│   │   └── App.js           # Main application component
│   ├── public/               # Static assets
│   └── package.json         # Frontend dependencies
└── docs/                    # Documentation
    ├── INSTALL.md           # Installation guide
    ├── ENVIRONMENT_GUIDE.md # Environment setup
    └── GROUP_CHAT.md        # Group chat documentation
```

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

For detailed documentation, see the [docs/](docs/) directory:

- **[Installation Guide](docs/INSTALL.md)** - Complete setup instructions
- **[Environment Guide](docs/ENVIRONMENT_GUIDE.md)** - Environment configuration
- **[Group Chat Guide](docs/GROUP_CHAT.md)** - Multi-agent conversations
- **[Python Examples](backend/python/examples/README.md)** - Python setup and configuration
- **[.NET Examples](backend/dotnet/examples/README.md)** - .NET setup and configuration

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request
