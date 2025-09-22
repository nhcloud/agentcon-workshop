# .NET Semantic Kernel Implementation

Enterprise-ready .NET 9 implementation using Semantic Kernel framework with ASP.NET Core Web API.

## 🚀 Quick Start

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 (recommended) or VS Code
- Azure OpenAI Service access

### Setup

1. **Navigate to project:**
   ```bash
   cd Backend/dotnet/sk
   dotnet restore
   ```

2. **Configure environment:**
   ```bash
   # Edit .env file with your Azure credentials
   ```

3. **Run application:**
   ```bash
   dotnet watch run
   # API: http://localhost:8002
   # Swagger: http://localhost:8002/swagger
   ```

## 📚 Features

### .NET Semantic Kernel
- **Native Azure Integration**: Optimized for Azure OpenAI
- **Enterprise Architecture**: ASP.NET Core with dependency injection
- **Type Safety**: Strong typing and structured development
- **Performance**: Async/await and modern .NET optimizations

### Learning Materials
- **Interactive Notebook**: `workshop_dotnet_semantic_kernel.ipynb`
- **HTTP Test Files**: `test-workshop.http` for API testing
- **Swagger Documentation**: Interactive API exploration

## ⚙️ Configuration

### Environment Variables
```env
# Azure OpenAI (Required)
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your_api_key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o-mini

# Azure AI Foundry (Optional)
PROJECT_ENDPOINT=https://your-resource.services.ai.azure.com/api/projects/your-project
PEOPLE_AGENT_ID=asst-your-people-agent-id
KNOWLEDGE_AGENT_ID=asst-your-knowledge-agent-id

# Application
FRONTEND_URL=http://localhost:3001
PORT=8002
```

## 🏗️ Architecture

### Project Structure
```
Backend/dotnet/sk/
├── Controllers/           # API endpoints
├── Services/             # Business logic  
├── Agents/               # Agent implementations
├── Models/               # Data models
├── Configuration/        # Azure configuration
├── Program.cs            # Application entry
└── appsettings.json      # Configuration
```

### Key Components
- **AgentService**: Core agent orchestration
- **GroupChatService**: Multi-agent conversations  
- **SessionManager**: State and session management
- **AzureAIConfig**: Centralized Azure configuration

## 🚀 API Endpoints

### Core Endpoints
- `POST /api/agents/{agentType}/chat` - Chat with agents
- `POST /api/agents/group-chat` - Multi-agent conversations
- `GET /api/agents/types` - Available agent types
- `GET /health` - Health check

## 🎓 Development

### Interactive Learning
```bash
# Open Jupyter notebook
workshop_dotnet_semantic_kernel.ipynb

# Test APIs with HTTP files  
test-workshop.http
```

### Custom Agents
Extend the base agent classes:
```csharp
public class CustomAgent : BaseAgent
{
    public CustomAgent(IConfiguration config) : base(config)
    {
        // Custom implementation
    }
}
```

## 🔧 Advanced Features

- **Dependency Injection**: Clean IoC architecture
- **Swagger/OpenAPI**: Auto-generated documentation  
- **Structured Logging**: Production-ready logging
- **Hot Reload**: Fast development workflow
- **Azure Integration**: Native Azure service support

## 📖 Related Documentation

- **[Main README](../../README.md)** - Project overview
- **[Environment Guide](../../docs/ENVIRONMENT_GUIDE.md)** - Azure configuration
- **[Group Chat Guide](../../docs/GROUP_CHAT.md)** - Multi-agent patterns

## 🐛 Troubleshooting

### Common Issues
1. **.NET SDK**: Ensure .NET 9 is installed
2. **Azure Credentials**: Verify environment variables
3. **Port Conflicts**: Change PORT in configuration

### Getting Help
- Review configuration in `appsettings.json`
- Check Azure credentials in `.env` file
- Use Swagger UI for API testing

## 🎯 Next Steps

1. **Run Examples**: Try the interactive notebook
2. **Test APIs**: Use HTTP files or Swagger UI
3. **Build Custom**: Create your own agents
4. **Deploy**: Use Azure App Service for production
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

- [Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [.NET 9 Documentation](https://learn.microsoft.com/en-us/dotnet/)

## Contributing

1. Follow .NET coding conventions
2. Add XML documentation to public APIs
3. Include unit tests for new features
4. Update this README for significant changes
5. Test with multiple agent configurations

For questions or issues, refer to the main project documentation or create detailed issue reports with configuration details and error logs.