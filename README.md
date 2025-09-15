# Azure Multi-Agent Chatbot

A sophisticated chatbot application that combines Azure OpenAI services with a React frontend to provide both general chat capabilities and specialized people lookup functionality. The system uses FastAPI for the backend and implements a multi-agent architecture to route queries to the most appropriate AI model.

## Features

- 🤖 Dual Agent System:
  - General Chat Agent (Azure OpenAI)
  - People Lookup Agent (Azure AI Agent)
- 🌐 Modern React Frontend
- ⚡ FastAPI Backend
- 🔄 Real-time Chat with SSE (Server-Sent Events)
- 📊 Token Usage Tracking
- 💾 Session Management
- 🎨 Syntax Highlighting for Code Blocks
- 📱 Haptic Feedback Support
- 🔐 Azure Authentication Integration

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
cd Azure-Chatbot
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
cd Backend
pip install -r requirements.txt
cd ..
```

Required Python packages:
- semantic-kernel
- azure-ai-projects
- azure-identity
- python-dotenv
- fastapi
- uvicorn

6. Set up frontend dependencies:
```bash
cd frontend
npm install
cd ..
```

7. Create a `.env` file based on the template:
```bash
cp env.template .env
```

8. Configure the following environment variables in `.env`:
```
FRONTEND_URL=http://localhost:3000

# Azure OpenAI Configuration
AZURE_OPENAI_ENDPOINT=<your-azure-openai-endpoint>
AZURE_OPENAI_DEPLOYMENT=<your-model-deployment-name>
AZURE_OPENAI_KEY=<your-azure-openai-api-key>

# Azure AI Project Configuration
PROJECT_ENDPOINT=<your-project-endpoint>
AGENT_ID=<your-agent-id>
```

9. Verify Azure services access:
```bash
# Verify OpenAI service access
az cognitiveservices account list

# Verify AI service access
az ai project list
```

## Running the Application

1. Start the backend server:
```bash
# From the root directory
uvicorn main:app --reload
```

2. Start the frontend development server:
```bash
# From the frontend directory
cd frontend
npm start
```

3. Access the application at `http://localhost:3000`

## API Endpoints

- `POST /chat`: Send a chat message
- `GET /chat/stream`: Stream chat responses
- `GET /messages/{session_id}`: Retrieve chat history
- `DELETE /messages/{session_id}`: Delete chat history
- `GET /sessions`: List active sessions
- `GET /health`: Health check endpoint
- `GET /`: Welcome message

## Project Structure

```
AI/
├── backend/                 # FastAPI backend application
│   ├── __init__.py
│   ├── main.py             # FastAPI application and routes
│   ├── multi_agent.py      # Agent initialization and routing
│   └── requirements.txt    # Python dependencies
├── Frontend/               # React frontend application
│   ├── src/
│   │   ├── components/     # React components
│   │   ├── services/       # API service layer
│   │   └── App.js         # Main application component
│   ├── public/             # Static assets
│   └── package.json       # Frontend dependencies
└── env.template           # Environment variables template
```

## Development

### Backend Development

The backend is built with FastAPI and implements:
- Asynchronous request handling using FastAPI
- Session management with chat history and response caching
- Intelligent agent routing using Semantic Kernel
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

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request
