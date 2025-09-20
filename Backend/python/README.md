# Python AI Agent System

A modern Python implementation of the multi-agent system supporting both LangChain and Semantic Kernel frameworks with a unified architecture.

## Overview

This Python implementation provides two powerful AI agent frameworks:

- 🦜 **LangChain Implementation**: Industry-standard framework with extensive ecosystem
- 🔧 **Semantic Kernel Implementation**: Microsoft's AI orchestration framework
- 🏗️ **Shared Architecture**: Common components and interfaces for consistency
- ⚡ **FastAPI Backend**: High-performance async API with automatic documentation
- 🔄 **Flexible Routing**: Pattern-based and LLM-powered agent routing
- 💾 **Session Management**: Multiple storage backends (memory, file, Redis)
- 📊 **Health Monitoring**: Built-in system monitoring and health checks

## Prerequisites

- Python 3.13+
- pip package manager
- Azure OpenAI Service access
- Azure AI Project Service (for advanced agents)

## Quick Start

### 1. Environment Setup

```bash
# Create and activate virtual environment
python -m venv venv

# Windows
.\venv\Scripts\activate

# macOS/Linux
source venv/bin/activate
```

### 2. Choose Your Implementation

#### Option A: LangChain Implementation
```bash
cd Backend/python/langchain
pip install -r requirements.txt
```

#### Option B: Semantic Kernel Implementation
```bash
cd Backend/python/sk
pip install -r requirements.txt
```

### 3. Configure Environment

```bash
# Copy environment template
cp ../env.template ../.env

# Edit .env file with your Azure credentials
```

Configure your `.env` file:
```env
# Azure OpenAI Configuration
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your_api_key_here
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini
AZURE_OPENAI_API_VERSION=2024-02-01

# Azure AI Foundry Configuration  
PROJECT_ENDPOINT=https://your-resource.services.ai.azure.com/api/projects/your-project
PEOPLE_AGENT_ID=asst-your-people-agent-id
KNOWLEDGE_AGENT_ID=asst-your-knowledge-agent-id

# Application Configuration
FRONTEND_URL=http://localhost:3000
LOG_LEVEL=INFO
SESSION_STORAGE_TYPE=file
```

### 4. Run the Application

#### LangChain Implementation:
```bash
cd Backend/python/langchain
uvicorn main:app --reload
```

#### Semantic Kernel Implementation:
```bash
cd Backend/python/sk
uvicorn main:app --reload
```

The API will be available at:
- **API**: `http://localhost:8000`
- **API Docs**: `http://localhost:8000/docs`
- **ReDoc**: `http://localhost:8000/redoc`

## Project Structure

```
Backend/python/
├── shared/                    # Common Libraries & Interfaces
│   ├── core/                 # Base classes and interfaces
│   │   ├── agent.py         # Base agent interface
│   │   ├── router.py        # Routing interface
│   │   └── session.py       # Session management
│   ├── agents/              # Base agent implementations
│   ├── config/              # Configuration management
│   ├── routers/             # Routing implementations
│   └── sessions/            # Session storage backends
│
├── langchain/               # LangChain Implementation
│   ├── agents/             # LangChain-specific agents
│   │   ├── __init__.py
│   │   └── langchain_agents.py
│   ├── routers/            # LangChain-specific routers
│   │   ├── __init__.py
│   │   └── langchain_router.py
│   ├── main.py             # FastAPI application
│   ├── config.yml          # Configuration file
│   ├── requirements.txt    # Dependencies
│   ├── README.md           # LangChain-specific docs
│   ├── SETUP_README.md     # Setup instructions
│   ├── example_group_chat.py # Group chat demo
│   └── workshop_langchain_agents.ipynb # Interactive tutorial
│
├── sk/                     # Semantic Kernel Implementation
│   ├── agents/             # SK-specific agents
│   │   ├── __init__.py
│   │   └── sk_agents.py
│   ├── routers/            # SK-specific routers
│   │   ├── __init__.py
│   │   └── sk_router.py
│   ├── main.py             # FastAPI application
│   ├── config.yml          # Configuration file
│   ├── requirements.txt    # Dependencies
│   ├── SETUP_README.md     # Setup instructions
│   ├── example_group_chat.py # Group chat demo
│   └── workshop_semantic_kernel_agents.ipynb # Interactive tutorial
│
├── env.template            # Environment variables template
├── .gitignore             # Python-specific ignore rules
├── check_config.py        # Configuration validator
└── validate_env.py        # Environment checker
```

## Framework Comparison

### LangChain Implementation
**Best for**: Rapid prototyping, extensive integrations, community-driven features

**Key Features**:
- Rich ecosystem of tools and integrations
- Extensive documentation and community support
- Advanced retrieval-augmented generation (RAG) capabilities
- Built-in support for many AI providers
- Chain-based workflow composition

**Use Cases**:
- Complex document processing workflows
- Multi-step reasoning tasks
- Integration with external tools and APIs
- Rapid prototyping of AI applications

### Semantic Kernel Implementation
**Best for**: Enterprise applications, Microsoft ecosystem, structured planning

**Key Features**:
- Microsoft-backed framework with enterprise support
- Native Azure integration
- Structured plugin architecture
- Built-in planning capabilities
- Type-safe development experience

**Use Cases**:
- Enterprise AI applications
- Microsoft ecosystem integration
- Structured business workflows
- Reliable, type-safe AI orchestration

## Interactive Learning

### Jupyter Notebooks

Both implementations include interactive Jupyter notebooks:

**LangChain Tutorial**:
```bash
cd Backend/python/langchain
jupyter notebook workshop_langchain_agents.ipynb
```

**Semantic Kernel Tutorial**:
```bash
cd Backend/python/sk
jupyter notebook workshop_semantic_kernel_agents.ipynb
```

The notebooks provide:
- Step-by-step setup instructions
- Interactive code examples
- Live API testing
- Configuration validation
- Best practices demonstrations

## Configuration

### YAML Configuration Structure

Both implementations use similar YAML configuration:

```yaml
# Application settings
app:
  title: "AI Agent System"
  version: "2.0.0"
  frontend_url: "${FRONTEND_URL:*}"
  log_level: "${LOG_LEVEL:INFO}"

# Azure OpenAI configuration
azure_openai:
  endpoint: "${AZURE_OPENAI_ENDPOINT}"
  api_key: "${AZURE_OPENAI_API_KEY}"
  api_version: "${AZURE_OPENAI_API_VERSION:2024-02-01}"
  deployment_name: "${AZURE_OPENAI_DEPLOYMENT_NAME:gpt-4o-mini}"

# Session management
sessions:
  storage_type: "${SESSION_STORAGE_TYPE:file}"
  storage_path: "${SESSION_STORAGE_PATH:./sessions}"
  cleanup_interval_hours: 24
  max_age_days: 7

# Agent configurations
agents:
  generic_agent:
    type: "generic"
    enabled: true
    instructions: "You are a helpful AI assistant."
    framework_config:
      provider: "azure_openai"
      model: "gpt-4o"
      temperature: 0.7
```

### Environment Variables

**Required**:
- `AZURE_OPENAI_ENDPOINT`: Your Azure OpenAI endpoint
- `AZURE_OPENAI_API_KEY`: Your Azure OpenAI API key
- `AZURE_OPENAI_DEPLOYMENT_NAME`: Model deployment name

**Optional**:
- `PROJECT_ENDPOINT`: Azure AI Foundry project endpoint
- `PEOPLE_AGENT_ID`: Agent ID for people lookup
- `KNOWLEDGE_AGENT_ID`: Agent ID for knowledge retrieval
- `FRONTEND_URL`: CORS origin (default: "*")
- `LOG_LEVEL`: Logging level (default: "INFO")
- `SESSION_STORAGE_TYPE`: Storage backend ("memory", "file", "redis")

## API Endpoints

### Core Chat Endpoints
```http
POST /chat                    # Send chat message
POST /chat/stream             # Stream chat responses
GET /messages/{session_id}    # Get chat history
DELETE /messages/{session_id} # Clear chat history
```

### Agent Management
```http
GET /agents                   # List available agents
GET /agents/{agent_name}/info # Get agent information
POST /agents/{agent_name}/toggle # Enable/disable agent
```

### Group Chat (Advanced)
```http
POST /group-chat              # Create group chat session
POST /group-chat/create       # Create with configuration
GET /group-chat/{session_id}/summary # Get chat summary
POST /group-chat/{session_id}/reset  # Reset group chat
DELETE /group-chat/{session_id}      # Delete group chat
GET /group-chats              # List active group chats
```

### System Health
```http
GET /health                   # Health check endpoint
GET /system/stats            # System statistics
```

## Development

### Adding Custom Agents

1. **Create Agent Class**:
```python
from shared.core.agent import BaseAgent
from shared.core.models import AgentMessage, AgentResponse

class CustomAgent(BaseAgent):
    async def initialize(self):
        await super().initialize()
        # Your initialization code here
    
    async def process_message(
        self, 
        message: str, 
        history: List[AgentMessage], 
        metadata: dict
    ) -> AgentResponse:
        # Your agent logic here
        return self._create_response("Your response", metadata)
```

2. **Register Agent**:
```python
# In your agent factory
from .custom_agent import CustomAgent

def create_agent(self, config: AgentConfig) -> BaseAgent:
    if config.agent_type == "custom":
        return CustomAgent(config)
    # ... other agents
```

3. **Update Configuration**:
```yaml
agents:
  my_custom_agent:
    type: "custom"
    enabled: true
    instructions: "You are a custom agent that..."
    framework_config:
      custom_setting: "value"
```

### Testing

#### Unit Testing
```bash
# Run tests for LangChain
cd Backend/python/langchain
python -m pytest tests/

# Run tests for Semantic Kernel
cd Backend/python/sk
python -m pytest tests/
```

#### Configuration Validation
```bash
# Validate environment setup
python Backend/python/validate_env.py

# Check configuration
python Backend/python/check_config.py
```

#### Interactive Testing
Use the provided example scripts:
```bash
# Test group chat - LangChain
cd Backend/python/langchain
python example_group_chat.py

# Test group chat - Semantic Kernel
cd Backend/python/sk
python example_group_chat.py
```

## Deployment

### Development
```bash
# With auto-reload
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

### Production
```bash
# Production server
gunicorn main:app -w 4 -k uvicorn.workers.UvicornWorker --bind 0.0.0.0:8000
```

### Docker
```dockerfile
FROM python:3.13-slim

WORKDIR /app
COPY requirements.txt .
RUN pip install -r requirements.txt

COPY . .
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
```

## Best Practices

### 1. Agent Design
- Keep agents focused on specific capabilities
- Use clear, descriptive instructions
- Implement proper error handling
- Add comprehensive logging

### 2. Configuration Management
- Use environment variables for secrets
- Validate configuration at startup
- Provide sensible defaults
- Document all options

### 3. Session Management
- Choose appropriate storage backend
- Implement session cleanup
- Monitor memory usage
- Handle concurrent sessions

### 4. Performance
- Use async/await consistently
- Implement response caching
- Monitor API response times
- Optimize agent initialization

## Troubleshooting

### Common Issues

1. **Import Errors**
   ```bash
   # Ensure virtual environment is activated
   which python
   pip list
   ```

2. **Configuration Issues**
   ```bash
   # Validate configuration
   python Backend/python/check_config.py
   ```

3. **Azure Connection Issues**
   ```bash
   # Test environment variables
   python Backend/python/validate_env.py
   ```

4. **Port Conflicts**
   ```bash
   # Use different port
   uvicorn main:app --port 8001
   ```

### Debug Mode
```bash
# Enable debug logging
export LOG_LEVEL=DEBUG
export DEBUG_LOGS=1
uvicorn main:app --reload --log-level debug
```

### Health Monitoring
```bash
# Check application health
curl http://localhost:8000/health

# Monitor system statistics
curl http://localhost:8000/system/stats
```

## Migration Between Frameworks

### From LangChain to Semantic Kernel
1. Update dependencies in `requirements.txt`
2. Modify agent implementations
3. Update configuration if needed
4. Test thoroughly

### From Semantic Kernel to LangChain
1. Install LangChain dependencies
2. Adapt agent logic to LangChain patterns
3. Update routing configuration
4. Validate functionality

## Additional Resources

- [LangChain Documentation](https://python.langchain.com/)
- [Semantic Kernel Documentation](https://docs.microsoft.com/en-us/semantic-kernel/)
- [FastAPI Documentation](https://fastapi.tiangolo.com/)
- [Azure OpenAI Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/openai/)

## Contributing

1. Follow Python PEP 8 style guidelines
2. Add type hints to all functions
3. Include docstrings for classes and methods
4. Write tests for new features
5. Update documentation for changes

For questions or issues, refer to the framework-specific documentation or the interactive Jupyter notebooks for hands-on guidance.