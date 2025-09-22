# Semantic Kernel Implementation - Python Backend

A comprehensive Semantic Kernel-based implementation of the multi-agent system, providing Microsoft's enterprise-grade AI orchestration framework with Azure integration.

## 🚀 Quick Start

### Option 1: Workshop Notebook (Easiest for Learning)

1. **Open the workshop notebook:**
   ```bash
   # Open workshop_semantic_kernel_agents.ipynb in Jupyter or VS Code
   ```

2. **Run the first cell** (Section 0: Environment Setup)
   - Automatically installs all required packages
   - Checks for environment variables
   - Includes mock implementations if no Azure credentials

3. **Continue with the workshop** - Learn concepts hands-on!

### Option 2: Production API Setup

### Prerequisites
- Python 3.8+
- Azure OpenAI Service access
- (Optional) Azure AI Foundry project

### Installation

1. **Navigate to Semantic Kernel directory:**
   ```bash
   cd Backend/python/sk
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
- **API**: `http://localhost:8001`
- **API Docs**: `http://localhost:8001/docs`

## 🎓 Workshop Learning Path

### No Azure Credentials? No Problem!
The workshop includes **mock implementations** so you can:
- ✅ Learn all the concepts and patterns
- ✅ Understand Semantic Kernel architecture
- ✅ See enterprise features in action
- ✅ Progress from basic to advanced agents

### What You'll Learn
1. **Basic Semantic Kernel Agents** - Core concepts and patterns
2. **Azure Integration** - Multi-provider support and Azure services
3. **Advanced Features** - Plugins, memory, and context management
4. **Enterprise Deployment** - Azure AI Foundry for production

## 📚 Features

### Semantic Kernel Framework
- **Native Azure Integration**: Built specifically for Microsoft Azure ecosystem
- **Plugin Architecture**: Structured and type-safe plugin system
- **Planning Capabilities**: Built-in planning and orchestration
- **Enterprise Ready**: Microsoft-backed with enterprise support

### Agent Capabilities
- **ChatCompletion Agents**: Native SK agent implementations
- **Group Chat**: Multi-agent collaborative conversations
- **Template System**: YAML-based agent configurations
- **Session Management**: Persistent conversation state

### Azure Integration
- **Azure OpenAI**: Optimized integration with Azure OpenAI Service
- **Azure AI Foundry**: Enterprise agent deployment and management
- **Managed Identity**: Production-ready authentication
- **Security**: Enterprise security and compliance features

## 🏗️ Architecture

### Core Components
- **Agents**: Semantic Kernel ChatCompletion agents
- **Routers**: SK-based request routing and orchestration
- **Sessions**: State management with SK memory
- **Configuration**: YAML-based agent templates

### File Structure
```
sk/
├── agents/                    # Agent implementations
│   └── sk_agents.py
├── routers/                   # Request routing
│   └── sk_router.py
├── main.py                    # FastAPI application
├── config.yml                 # Agent configurations
├── requirements.txt           # Python dependencies
├── SETUP_README.md            # Setup instructions
├── example_group_chat.py      # Group chat example
└── workshop_semantic_kernel_agents.ipynb # Interactive tutorial
```

## 🎓 Learning Materials

### Interactive Workshop
Start with the interactive notebook:
```bash
jupyter notebook workshop_semantic_kernel_agents.ipynb
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
PORT=8001
LOG_LEVEL=INFO
```

### Agent Templates
Agents are configured in `config.yml`:
```yaml
agents:
  technical_expert:
    name: "Technical Expert"
    description: "Specialized in technical architecture and implementation"
    instructions: "You are a technical expert with deep knowledge..."
    
group_chats:
  templates:
    architecture_review:
      name: "Architecture Review Team"
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

### Semantic Kernel Capabilities
- **Kernel**: Central orchestration component
- **Plugins**: Structured function calling and tools
- **Memory**: Advanced memory management and retrieval
- **Planning**: Automatic task planning and execution

### Custom Agents
Create custom agents by extending the SK base classes:
```python
from semantic_kernel.agents import ChatCompletionAgent

class CustomAgent(ChatCompletionAgent):
    def __init__(self, kernel, config):
        super().__init__(
            service_id="custom-agent",
            kernel=kernel,
            name=config.name,
            instructions=config.instructions
        )
```

### Production Deployment
- **Azure Integration**: Native Azure service integration
- **Monitoring**: Built-in telemetry and logging
- **Security**: Azure AD authentication and RBAC
- **Scaling**: Optimized for Azure infrastructure

## 📖 Related Documentation

- **[Main README](../../../README.md)** - Project overview
- **[Python Backend Guide](../README.md)** - Python implementation guide
- **[Environment Guide](../../../docs/ENVIRONMENT_GUIDE.md)** - Configuration details
- **[Group Chat Guide](../../../docs/GROUP_CHAT.md)** - Multi-agent conversations

## 🐛 Troubleshooting

### Common Issues
1. **Import Errors**: Ensure semantic-kernel package is installed
2. **Azure Credentials**: Verify environment variables are set
3. **Port Conflicts**: Change PORT in .env if 8001 is in use

### Getting Help
- Check the [setup guide](SETUP_README.md) for detailed instructions
- Review the [environment guide](../../../docs/ENVIRONMENT_GUIDE.md) for configuration
- Run the validation script: `python validate_env.py`

## 🎯 Next Steps

1. **Explore Examples**: Try the group chat and template examples
2. **Customize Agents**: Modify agent configurations in config.yml
3. **Build Plugins**: Create custom SK plugins for specialized functionality
4. **Deploy**: Use Azure AI Foundry for production deployment

## 🔄 Framework Comparison

| Feature | Semantic Kernel | LangChain |
|---------|----------------|-----------|
| **Enterprise Focus** | ✅ Microsoft-backed | 🔶 Community-driven |
| **Azure Integration** | ✅ Native | 🔶 Via adapters |
| **Type Safety** | ✅ Strong typing | 🔶 Dynamic typing |
| **Planning** | ✅ Built-in | 🔶 External tools |
| **Ecosystem** | 🔶 Growing | ✅ Extensive |
| **Learning Curve** | 🔶 Structured | ✅ Gentle |

Choose Semantic Kernel for enterprise applications requiring structured, type-safe AI orchestration with native Azure integration.