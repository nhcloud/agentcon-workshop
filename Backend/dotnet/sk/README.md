# .NET 9 Semantic Kernel Agents

A modern .NET 9 implementation of intelligent agents using Microsoft Semantic Kernel, featuring group chat capabilities, Azure AI integration, and RESTful APIs.

## 🚀 Features

- **Modern .NET 9**: Built with the latest .NET framework and C# features
- **Semantic Kernel Integration**: Leverages Microsoft's Semantic Kernel for AI orchestration
- **Azure AI Integration**: Supports Azure OpenAI and Azure AI Inference endpoints
- **GitHub Models Support**: Development-friendly GitHub Models integration
- **Group Chat**: Multi-agent conversations with intelligent coordination
- **RESTful APIs**: Comprehensive ASP.NET Core Web API with Swagger documentation
- **Session Management**: Persistent conversation history and context
- **Professional Architecture**: Clean separation of concerns with dependency injection

## 📁 Project Structure

```
dotnet_semantic_kernel/
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
└── appsettings.json           # Configuration settings
```

## 🤖 Available Agents

### 1. People Lookup Agent (`people_lookup`)
- **Purpose**: Find information about people, contacts, and team members
- **Capabilities**: Directory searches, contact information, organizational structure
- **Use Cases**: Team coordination, contact discovery, role identification

### 2. Knowledge Finder Agent (`knowledge_finder`)
- **Purpose**: Search and retrieve relevant knowledge and documentation
- **Capabilities**: Document search, information summarization, research assistance
- **Use Cases**: Knowledge discovery, research support, documentation queries

### 3. Task Assistant Agent (`task_assistant`)
- **Purpose**: Help with task management, planning, and productivity
- **Capabilities**: Project breakdown, prioritization, scheduling, productivity tips
- **Use Cases**: Project planning, time management, goal setting

### 4. Technical Advisor Agent (`technical_advisor`)
- **Purpose**: Provide technical guidance and architectural advice
- **Capabilities**: Code review, architecture guidance, technical decisions, debugging
- **Use Cases**: Code review, system design, technology recommendations

## 🛠️ Quick Start

### Prerequisites

- .NET 9 SDK
- Visual Studio 2024 or VS Code
- Azure OpenAI or GitHub Models access

### 1. Installation

```bash
# Clone the repository (if needed)
cd Backend/dotnet_semantic_kernel

# Restore NuGet packages
dotnet restore
```

### 2. Configuration

Copy the example configuration and configure your API keys:

```bash
cp ../examples/appsettings.json appsettings.json
```

Edit `appsettings.json` with your configuration:

```env
# Primary option: Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your_azure_openai_api_key

# Alternative: GitHub Models (for development)
GITHUB_TOKEN=your_github_token
AZURE_AI_INFERENCE_ENDPOINT=https://models.github.ai/inference
```

### 3. Run the Application

```bash
# Development mode with hot reload
dotnet watch run

# Or standard run
dotnet run
```

The API will be available at:
- **API**: `http://localhost:8002`
- **Swagger UI**: `http://localhost:8002` (redirects to Swagger in development)

## 📚 API Endpoints

### Agents Management

```http
GET /api/agents                    # Get all available agents
GET /api/agents/{agentName}        # Get specific agent info
```

### Single Agent Chat

```http
POST /api/chat                     # Generic chat endpoint
POST /api/chat/{agentName}         # Chat with specific agent

# Request body:
{
  "message": "Your question here",
  "agent": "technical_advisor",     # Optional for generic endpoint
  "session_id": "optional-uuid"
}
```

### Group Chat

```http
POST /api/groupchat                # Start group chat
GET /api/groupchat/sessions/{id}/history    # Get session history
DELETE /api/groupchat/sessions/{id}          # Clear session
GET /api/groupchat/sessions/{id}/exists      # Check session exists

# Group chat request body:
{
  "message": "Your question here",
  "agents": ["people_lookup", "knowledge_finder", "task_assistant"],
  "session_id": "optional-uuid",
  "max_turns": 5
}
```

### Health Check

```http
GET /health                        # Application health status
```

## 🔧 Configuration Options

### Azure OpenAI Configuration

```json
{
  "AzureAI": {
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "your-api-key",
      "ChatDeployment": "gpt-4o",
      "EmbeddingDeployment": "text-embedding-3-small",
      "ApiVersion": "2024-06-01"
    }
  }
}
```

### Azure AI Inference Configuration

```json
{
  "AzureAI": {
    "AzureAIInference": {
      "Endpoint": "https://models.github.ai/inference",
      "ApiKey": "your-github-token",
      "ModelName": "gpt-4.1"
    }
  }
}
```

## 🎯 Usage Examples

### Example 1: Single Agent Chat

```bash
curl -X POST "http://localhost:8002/api/chat/technical_advisor" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "How should I structure a microservices architecture?",
    "session_id": "my-session-123"
  }'
```

### Example 2: Group Chat

```bash
curl -X POST "http://localhost:8002/api/groupchat" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "We need to plan a new project launch",
    "agents": ["task_assistant", "technical_advisor", "people_lookup"],
    "max_turns": 3
  }'
```

### Example 3: Get Available Agents

```bash
curl -X GET "http://localhost:8002/api/agents"
```

## 🏗️ Architecture

### Dependency Injection

The application uses .NET's built-in dependency injection container with the following service registrations:

- **Scoped Services**: Kernel, AgentService, GroupChatService (per request)
- **Singleton Services**: SessionManager (application lifetime)
- **Configuration**: AzureAIConfig bound from appsettings.json

### Agent Factory Pattern

Agents are created using a factory pattern in `AgentService`, allowing for:
- Dynamic agent instantiation
- Consistent agent lifecycle management
- Easy addition of new agent types

### Session Management

Sessions are managed in-memory with the `SessionManager` service:
- Thread-safe concurrent collections
- Automatic session creation
- Conversation history persistence

## 🚀 Development

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

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Building for Production

```bash
# Build release version
dotnet build --configuration Release

# Publish self-contained
dotnet publish --configuration Release --self-contained true --runtime win-x64
```

## 🌐 Integration with Frontend

This .NET implementation is compatible with the existing React frontend. Update the frontend's `ChatService.js` to include the new endpoint:

```javascript
// Add to available endpoints
const endpoints = {
  python_langchain: 'http://localhost:8000',
  python_semantic_kernel: 'http://localhost:8001', 
  dotnet_semantic_kernel: 'http://localhost:8002'  // New endpoint
};
```

## 📊 Performance

- **Cold Start**: ~2-3 seconds (includes Semantic Kernel initialization)
- **Warm Requests**: ~100-500ms per agent response
- **Memory Usage**: ~50-100MB base + model context
- **Concurrent Sessions**: Supports multiple concurrent sessions in memory

## 🔒 Security

- **API Keys**: Stored in environment variables, never in code
- **CORS**: Configured for specific frontend origins
- **Input Validation**: Request validation using ASP.NET Core model binding
- **Error Handling**: Comprehensive exception handling with appropriate HTTP status codes

## 🐛 Troubleshooting

### Common Issues

1. **"Agent not found" errors**: Ensure agent names match exactly (case-sensitive)
2. **API key errors**: Verify environment variables are set correctly
3. **Port conflicts**: Change the PORT environment variable if 8002 is in use
4. **Model access errors**: Ensure your Azure/GitHub credentials have proper permissions

### Logging

Enable detailed logging by setting:

```env
LOG_LEVEL=Debug
ASPNETCORE_LOGGING__LOGLEVEL__MICROSOFT_SEMANTICKERNEL=Debug
```

## 🤝 Contributing

1. Follow C# coding conventions and .NET best practices
2. Use dependency injection for all services
3. Add comprehensive XML documentation for public APIs
4. Include unit tests for new functionality
5. Update this README for significant changes

## 📝 License

This project follows the same license as the parent agents-workshop repository.