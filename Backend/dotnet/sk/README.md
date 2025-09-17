# .NET Semantic Kernel Agents Workshop

A comprehensive .NET 9 implementation of Semantic Kernel agents that matches the Python workshop structure, demonstrating the progression from basic agents to enterprise-ready Azure AI Foundry solutions.

## 🚀 Quick Setup

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 or VS Code
- Azure OpenAI resource
- (Optional) Azure AI Foundry project

### Configuration Options

You can configure this workshop using either environment variables (recommended) or appsettings.json files.

#### Option 1: Environment Variables (Recommended)

1. Copy the environment template:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` file with your Azure credentials:
   ```bash
   # Azure OpenAI Configuration
   AZURE_OPENAI_ENDPOINT=https://your-resource-name.openai.azure.com
   AZURE_OPENAI_API_KEY=your-azure-openai-api-key-here
   AZURE_OPENAI_DEPLOYMENT_NAME=your-model-deployment-name
   AZURE_OPENAI_API_VERSION=2024-02-01

   # Azure AI Foundry (Optional)
   PROJECT_ENDPOINT=https://your-resource-name.services.ai.azure.com/api/projects/your-project-name
   PEOPLE_AGENT_ID=asst-your-people-agent-id-here
   KNOWLEDGE_AGENT_ID=asst-your-knowledge-agent-id-here

   # Frontend Configuration
   FRONTEND_URL=http://localhost:3001
   ```

#### Option 2: appsettings.json

Update `appsettings.json` or `appsettings.Development.json`:

```json
{
  "AzureAI": {
    "AzureOpenAI": {
      "Endpoint": "https://your-resource-name.openai.azure.com",
      "ApiKey": "your-azure-openai-api-key-here",
      "DeploymentName": "your-model-deployment-name",
      "ApiVersion": "2024-02-01"
    },
    "AzureAIFoundry": {
      "ProjectEndpoint": "https://your-resource-name.services.ai.azure.com/api/projects/your-project-name",
      "PeopleAgentId": "asst-your-people-agent-id-here",
      "KnowledgeAgentId": "asst-your-knowledge-agent-id-here"
    }
  }
}
```

### Running the Workshop

1. **Start the API:**
   ```bash
   dotnet run
   ```

2. **Access the Swagger UI:**
   Open http://localhost:8002 in your browser

3. **Check Configuration:**
   Visit http://localhost:8002/api/config to verify your setup

4. **Health Check:**
   Visit http://localhost:8002/health for service status

## 🏗️ Workshop Structure

This .NET implementation mirrors the Python workshop progression:

### 1. Basic Agents
- Simple chat completion agents
- Basic Semantic Kernel setup
- Azure OpenAI integration

### 2. Enhanced Agents
- Multi-provider support
- Context management
- Advanced error handling

### 3. Advanced Features
- Plugin system
- Memory management
- Custom functions

### 4. Enterprise Ready
- Azure AI Foundry integration
- Monitoring and telemetry
- Production configurations

## 📚 Key Components

### Services
- **AgentService**: Core agent management
- **GroupChatService**: Multi-agent conversations
- **SessionManager**: Session and state management

### Configuration
- **AzureAIConfig**: Centralized Azure configuration
- Environment variable support
- Development/Production configurations

### Controllers
- **AgentsController**: Individual agent endpoints
- **GroupChatController**: Multi-agent chat endpoints
- **ChatController**: Basic chat functionality

## 🔧 API Endpoints

### Core Endpoints
- `GET /health` - Service health and configuration status
- `GET /api/config` - Configuration verification
- `GET /swagger` - Interactive API documentation

### Agent Endpoints
- `POST /api/agents/{agentType}/chat` - Chat with specific agent types
- `GET /api/agents/types` - Available agent types
- `POST /api/agents/group-chat` - Multi-agent conversations

## 🧪 Testing the Workshop

### 1. Verify Configuration
```bash
curl http://localhost:8002/health
```

### 2. Test Basic Chat
```bash
curl -X POST http://localhost:8002/api/agents/generic/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello, what can you help me with?"}'
```

### 3. Test Group Chat
```bash
curl -X POST http://localhost:8002/api/agents/group-chat \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Discuss the benefits of AI agents",
    "agentTypes": ["creative", "technical"]
  }'
```

## 🌟 Workshop Features

### Semantic Kernel Integration
- Modern .NET 9 implementation
- ChatCompletion agents
- Plugin architecture
- Memory management

### Azure AI Services
- Azure OpenAI integration
- Azure AI Foundry support
- Managed identity ready
- Enterprise security

### Development Experience
- Hot reload support
- Comprehensive logging
- Swagger documentation
- Configuration validation

## 🔄 Comparison with Python Version

| Feature | Python Workshop | .NET Workshop |
|---------|----------------|---------------|
| Framework | Semantic Kernel Python | Semantic Kernel .NET |
| Configuration | .env files | .env + appsettings.json |
| Agent Types | Custom classes | Service-based architecture |
| API Framework | FastAPI/Flask | ASP.NET Core Web API |
| Documentation | Jupyter notebooks | Swagger/OpenAPI |
| Development | Interactive notebooks | Visual Studio/VS Code |

## 🎯 Learning Objectives

By completing this workshop, you'll understand:

1. **Semantic Kernel Fundamentals**
   - Plugin architecture
   - Memory management
   - Function calling

2. **Azure AI Integration**
   - OpenAI service setup
   - AI Foundry enterprise features
   - Configuration management

3. **Enterprise Patterns**
   - Service architecture
   - Dependency injection
   - Configuration management
   - API design

4. **.NET 9 Modern Features**
   - Minimal APIs
   - Configuration providers
   - Logging and telemetry
   - CORS and security

## 🚀 Next Steps

After completing this workshop:

1. **Extend Agent Capabilities**: Add custom plugins and functions
2. **Implement Memory**: Add vector databases and semantic memory
3. **Enterprise Deployment**: Configure for production with Azure services
4. **Frontend Integration**: Connect with React/Blazor applications
5. **Monitoring**: Add Application Insights and health checks

## 🤝 Workshop Alignment

This .NET implementation provides the same learning experience as the Python version while showcasing .NET-specific patterns and enterprise features. Both workshops teach the same core concepts using their respective platform strengths.

## 📁 Project Structure

```
sk/
├── Controllers/                 # ASP.NET Core API controllers
│   ├── AgentsController.cs     # Agent management endpoints
│   ├── ChatController.cs       # Single agent chat endpoints
│   └── GroupChatController.cs  # Multi-agent chat endpoints
├── Services/                   # Business logic services
│   ├── AgentService.cs         # Agent factory and management
│   ├── GroupChatService.cs     # Group chat orchestration
│   └── SessionManager.cs      # Session and history management
├── Agents/                     # Agent implementations
│   ├── BaseAgent.cs           # Abstract base agent class
│   └── SpecificAgents.cs      # Concrete agent implementations
├── Models/                     # Data transfer objects
│   └── ChatModels.cs          # Request/response models
├── Configuration/              # Configuration classes
│   └── AzureAIConfig.cs       # Azure AI settings
├── Program.cs                  # Application entry point
├── appsettings.json           # Configuration settings
├── .env.example               # Environment template
└── README.md                  # This file
```

## 🔧 Development

### Adding New Agents

1. Create a new agent class inheriting from `BaseAgent`:

```csharp
public class MyCustomAgent : BaseAgent
{
    public override string Name => "my_custom_agent";
    public override string Description => "Description of what this agent does";
    public override string Instructions => "System prompt for the agent";

    public MyCustomAgent(Kernel kernel, ILogger<MyCustomAgent> logger) 
        : base(kernel, logger) { }
}
```

2. Register the agent in `AgentService`:

```csharp
_agentFactories["my_custom_agent"] = () => new MyCustomAgent(_kernel, 
    _serviceProvider.GetRequiredService<ILogger<MyCustomAgent>>());
```

## 🔒 Security & Best Practices

- **API Keys**: Use environment variables, never hardcode
- **CORS**: Configured for frontend origins
- **Input Validation**: ASP.NET Core model binding
- **Error Handling**: Comprehensive exception handling
- **Logging**: Structured logging with semantic information

## 🐛 Troubleshooting

### Common Issues

1. **Configuration not loading**: Ensure .env file is in the project root
2. **API key errors**: Verify Azure OpenAI credentials
3. **Port conflicts**: Set PORT environment variable
4. **Model access errors**: Check Azure resource permissions

### Debug Configuration

Check your configuration status:
```bash
curl http://localhost:8002/api/config
```

Enable detailed logging:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.SemanticKernel": "Debug"
    }
  }
}