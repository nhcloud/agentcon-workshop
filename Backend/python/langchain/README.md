# LangChain Implementation - Python Backend

A comprehensive LangChain-based implementation of the multi-agent system, providing advanced conversational AI capabilities with Azure integration.

## 🚀 Quick Start

### Prerequisites
- Python 3.8+
- Azure OpenAI Service access
- (Optional) Azure AI Foundry project

### Installation

1. **Navigate to LangChain directory:**
   ```bash
   cd Backend/python/langchain
   ```

2. **Install dependencies:**
   ```bash
   pip install -r requirements.txt
   ```

3. **Configure environment:**
   ```bash
   # Copy template
   cp ../env.template .env
   
   # Edit .env with your Azure credentials
   ```

4. **Run the application:**
   ```bash
   uvicorn main:app --reload
   ```

The API will be available at:
- **API**: `http://localhost:8000`
- **API Docs**: `http://localhost:8000/docs`

## 📚 Features

### LangChain Framework
- **Chain Composition**: Build complex workflows with LangChain chains
- **Multi-Provider Support**: Azure OpenAI, OpenAI, and other providers
- **Advanced RAG**: Retrieval-augmented generation capabilities
- **Tool Integration**: Extensive ecosystem of tools and integrations

### Agent Capabilities
- **Conversational Agents**: Natural dialogue with memory
- **Group Chat**: Multi-agent collaborative conversations
- **Template System**: Predefined agent configurations
- **Session Management**: Persistent conversation state

### Azure Integration
- **Azure OpenAI**: Native integration with Azure OpenAI Service
- **Azure AI Foundry**: Enterprise-grade agent deployment
- **Managed Identity**: Production-ready authentication
- **Security**: Enterprise security and compliance

## 🏗️ Architecture

### Core Components
- **Agents**: LangChain-based conversational agents
- **Routers**: Request routing and orchestration
- **Sessions**: State management and persistence
- **Configuration**: YAML-based agent templates

### File Structure
```
langchain/
├── agents/                    # Agent implementations
│   └── langchain_agents.py
├── routers/                   # Request routing
│   └── langchain_router.py
├── main.py                    # FastAPI application
├── config.yml                 # Agent configurations
├── requirements.txt           # Python dependencies
├── SETUP_README.md            # Setup instructions
├── example_group_chat.py      # Group chat example
└── workshop_langchain_agents.ipynb # Interactive tutorial
```

## 🎓 Learning Materials

### Interactive Workshop
Start with the interactive notebook:
```bash
jupyter notebook workshop_langchain_agents.ipynb
```

### Example Scripts
Run the example scripts:
```bash
# Group chat example
python example_group_chat.py

# Template usage
python example_template_usage.py
```

## ⚙️ Configuration

### Environment Variables
```env
# Azure OpenAI (Primary)
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your_api_key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini

# Azure AI Foundry (Optional)
PROJECT_ENDPOINT=https://your-resource.services.ai.azure.com/api/projects/your-project
PEOPLE_AGENT_ID=asst-your-people-agent-id
KNOWLEDGE_AGENT_ID=asst-your-knowledge-agent-id

# Application
PORT=8000
LOG_LEVEL=INFO
```

### Agent Templates
Agents are configured in `config.yml`:
```yaml
agents:
  creative_writer:
    name: "Creative Writer"
    description: "Specialized in creative content generation"
    instructions: "You are a creative writing expert..."
    
group_chats:
  templates:
    product_team:
      name: "Product Development Team"
      participants: [...]
```

## 🚀 API Endpoints

### Chat Endpoints
- `POST /chat` - Send message to agent
- `GET /chat/history/{session_id}` - Get chat history
- `DELETE /chat/history/{session_id}` - Clear history

### Group Chat
- `POST /group-chat` - Group conversation
- `POST /group-chat/from-template` - Create from template
- `GET /group-chat/templates` - List templates

### Agent Management
- `GET /agents` - List available agents
- `GET /agents/{agent_name}/info` - Agent details

## 🔧 Advanced Features

### LangChain Capabilities
- **Memory**: Conversation memory and context retention
- **Tools**: Integration with external APIs and services
- **Chains**: Complex multi-step workflows
- **Retrievers**: Document search and RAG implementations

### Custom Agents
Create custom agents by extending the base classes:
```python
from agents.langchain_agents import BaseLangChainAgent

class CustomAgent(BaseLangChainAgent):
    def __init__(self, config):
        super().__init__(config)
        # Custom initialization
```

### Production Deployment
- **Session Storage**: Redis backend for distributed sessions
- **Monitoring**: Application Insights integration
- **Security**: CORS, rate limiting, authentication
- **Scaling**: Async operations and connection pooling

## 📖 Related Documentation

- **[Main README](../../../README.md)** - Project overview
- **[Python Backend Guide](../README.md)** - Python implementation guide
- **[Environment Guide](../../../docs/ENVIRONMENT_GUIDE.md)** - Configuration details
- **[Group Chat Guide](../../../docs/GROUP_CHAT.md)** - Multi-agent conversations

## 🐛 Troubleshooting

### Common Issues
1. **Import Errors**: Ensure all dependencies are installed
2. **Azure Credentials**: Verify environment variables are set
3. **Port Conflicts**: Change PORT in .env if 8000 is in use

### Getting Help
- Check the [setup guide](SETUP_README.md) for detailed instructions
- Review the [environment guide](../../../docs/ENVIRONMENT_GUIDE.md) for configuration
- Run the validation script: `python validate_env.py`

## 🎯 Next Steps

1. **Explore Examples**: Try the group chat and template examples
2. **Customize Agents**: Modify agent configurations in config.yml
3. **Build Workflows**: Create custom LangChain chains
4. **Deploy**: Use Azure AI Foundry for production deployment