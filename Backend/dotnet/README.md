# .NET AI Agent System

A modern .NET 9 implementation of the multi-agent system using Semantic Kernel framework with ASP.NET Core Web API.

## Overview

This .NET implementation provides a professional, enterprise-ready AI agent system with:

- 🚀 **Modern .NET 9**: Latest C# features and performance optimizations
- 🤖 **Semantic Kernel Integration**: Microsoft's official AI orchestration framework
- 🌐 **ASP.NET Core Web API**: High-performance REST API with Swagger documentation
- 🔄 **Dependency Injection**: Clean architecture with IoC container
- ⚡ **Async/Await**: Non-blocking operations for optimal performance
- 📊 **Swagger/OpenAPI**: Interactive API documentation
- 🔧 **Configuration Management**: Environment-based configuration with validation
- 📝 **Comprehensive Logging**: Structured logging with Serilog
- 🧪 **Interactive Testing**: HTTP files and Jupyter notebook support

## Prerequisites

- .NET 9 SDK
- Visual Studio 2022 (recommended) or VS Code
- Azure OpenAI Service access
- Azure AI Project Service (optional, for advanced agents)

## Quick Start

### 1. Setup and Configuration

```bash
# Navigate to the .NET project
cd Backend/dotnet/sk

# Restore dependencies
dotnet restore
```

### 2. Configure Environment

Copy the environment template and configure your Azure credentials:

```bash
# Copy and edit .env file
cp .env.example .env
```

Edit `.env` with your Azure OpenAI credentials:
```env
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your_api_key_here
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini
AZURE_OPENAI_API_VERSION=2024-02-01

# Optional: Azure AI Foundry project configuration
PROJECT_ENDPOINT=https://your-resource.services.ai.azure.com/api/projects/your-project
PEOPLE_AGENT_ID=asst-your-people-agent-id
KNOWLEDGE_AGENT_ID=asst-your-knowledge-agent-id

# Application settings
FRONTEND_URL=http://localhost:3000
LOG_LEVEL=Information
```

### 3. Run the Application

```bash
# Development mode with hot reload
dotnet watch run

# Or standard run
dotnet run
```

The API will be available at:
- **API**: `http://localhost:8000`
- **Swagger UI**: `http://localhost:8000/swagger`

## Project Structure

```
Backend/dotnet/sk/
├── Controllers/           # API Controllers
│   ├── AgentsController.cs      # Agent management endpoints
│   ├── ChatController.cs        # Chat functionality
│   └── GroupChatController.cs   # Group chat features
├── Services/             # Business Logic Services
│   ├── AgentService.cs          # Agent orchestration
│   ├── GroupChatService.cs      # Group chat management
│   └── SessionManager.cs       # Session handling
├── Agents/               # Agent Implementations
│   ├── BaseAgent.cs             # Base agent class
│   └── SpecificAgents.cs        # Specialized agents
├── Models/               # Data Transfer Objects
│   └── ChatModels.cs            # Request/response models
├── Configuration/        # Configuration Classes
│   └── AzureAIConfig.cs         # Azure service configuration
├── Properties/           # Project Settings
│   └── launchSettings.json      # Development launch profiles
├── Program.cs            # Application entry point
├── appsettings.json      # Application configuration
├── appsettings.Development.json # Development overrides
├── .env                  # Environment variables (create from template)
├── test-workshop.http    # API testing collection
├── workshop_dotnet_semantic_kernel.ipynb # Interactive notebook
└── DotNetSemanticKernel.csproj  # Project file
```

## Development Workflow

### 1. Interactive Learning with Jupyter Notebook

Open the interactive notebook for hands-on learning:
```bash
# Open in VS Code or Jupyter
workshop_dotnet_semantic_kernel.ipynb
```

The notebook provides:
- Step-by-step setup instructions
- Interactive code examples
- Live API testing
- Configuration validation

### 2. API Testing with HTTP Files

Use the provided HTTP file for testing:
```bash
# Open in VS Code with REST Client extension
test-workshop.http
```

Example requests included:
- Health check
- Agent listing
- Chat interactions
- Group chat creation

### 3. Development with Hot Reload

For active development:
```bash
dotnet watch run
```

This provides:
- Automatic restart on file changes
- Live API updates
- Immediate feedback loop

## API Endpoints

### Core Chat Endpoints
```http
POST /api/chat                    # Send chat message
POST /api/chat/stream             # Stream chat responses
GET /api/chat/history/{sessionId} # Get chat history
DELETE /api/chat/history/{sessionId} # Clear chat history
```

### Agent Management
```http
GET /api/agents                   # List available agents
GET /api/agents/{agentName}/info  # Get agent information
POST /api/agents/{agentName}/toggle # Enable/disable agent
```

### Group Chat (Advanced)
```http
POST /api/groupchat               # Create group chat session
GET /api/groupchat/{sessionId}/summary # Get chat summary
POST /api/groupchat/{sessionId}/reset  # Reset group chat
DELETE /api/groupchat/{sessionId} # Delete group chat
```

### System Health
```http
GET /health                       # Health check endpoint
GET /api/system/stats            # System statistics
```

## Configuration

### Application Settings

The application uses a layered configuration approach:

1. **appsettings.json**: Base configuration
2. **appsettings.Development.json**: Development overrides
3. **.env file**: Environment-specific secrets
4. **Environment variables**: Runtime overrides

### Azure OpenAI Configuration

```json
{
  "AzureAI": {
    "OpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "your_api_key",
      "DeploymentName": "gpt-4o-mini",
      "ApiVersion": "2024-02-01"
    }
  }
}
```

### Logging Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "SemanticKernel": "Information"
    }
  }
}
```

## Agent Development

### Creating Custom Agents

1. **Inherit from BaseAgent**:
```csharp
public class CustomAgent : BaseAgent
{
    public CustomAgent(ILogger<CustomAgent> logger, Kernel kernel) 
        : base(logger, kernel)
    {
    }

    public override async Task<string> ProcessMessageAsync(
        string message, 
        List<ChatMessage> history, 
        CancellationToken cancellationToken = default)
    {
        // Your custom logic here
        return await base.ProcessMessageAsync(message, history, cancellationToken);
    }
}
```

2. **Register in Dependency Injection**:
```csharp
builder.Services.AddScoped<CustomAgent>();
```

3. **Configure in AgentService**:
```csharp
public class AgentService
{
    // Add your agent to the available agents collection
}
```

## Testing

### Unit Testing
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Integration Testing
Use the provided HTTP file (`test-workshop.http`) with VS Code REST Client extension for comprehensive API testing.

### Interactive Testing
The Jupyter notebook provides interactive testing capabilities with live API calls and response validation.

## Deployment

### Development Deployment
```bash
dotnet run --environment Development
```

### Production Deployment
```bash
# Build for production
dotnet build --configuration Release

# Run in production mode
dotnet run --configuration Release --environment Production
```

### Docker Deployment
```dockerfile
# Example Dockerfile for containerized deployment
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "DotNetSemanticKernel.dll"]
```

## Best Practices

### 1. Configuration Management
- Use environment variables for secrets
- Validate configuration at startup
- Provide meaningful defaults
- Document all configuration options

### 2. Error Handling
- Implement global exception handling
- Use structured logging
- Return appropriate HTTP status codes
- Provide helpful error messages

### 3. Performance
- Use async/await consistently
- Implement proper cancellation token handling
- Consider memory usage with large conversations
- Monitor API response times

### 4. Security
- Never commit secrets to source control
- Use HTTPS in production
- Implement proper CORS policies
- Validate all inputs

## Troubleshooting

### Common Issues

1. **Configuration Errors**
   ```bash
   # Check configuration validity
   dotnet run --dry-run
   ```

2. **Azure OpenAI Connection Issues**
   ```bash
   # Test connectivity
   curl -H "Authorization: Bearer $AZURE_OPENAI_API_KEY" \
        "$AZURE_OPENAI_ENDPOINT/openai/deployments?api-version=2024-02-01"
   ```

3. **Build Issues**
   ```bash
   # Clean and rebuild
   dotnet clean
   dotnet restore
   dotnet build
   ```

### Debug Mode
Enable detailed logging:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "SemanticKernel": "Debug"
    }
  }
}
```

### Health Monitoring
```bash
# Check application health
curl http://localhost:8000/health

# Monitor system statistics
curl http://localhost:8000/api/system/stats
```

## Additional Resources

- [Semantic Kernel Documentation](https://docs.microsoft.com/en-us/semantic-kernel/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Azure OpenAI Service Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/openai/)
- [.NET 9 Documentation](https://docs.microsoft.com/en-us/dotnet/)

## Contributing

1. Follow .NET coding conventions
2. Add XML documentation to public APIs
3. Include unit tests for new features
4. Update this README for significant changes
5. Test with multiple agent configurations

For questions or issues, refer to the main project documentation or create detailed issue reports with configuration details and error logs.