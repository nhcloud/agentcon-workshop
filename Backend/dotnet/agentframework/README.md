# .NET Agent Framework Implementation

Enterprise-grade .NET 9 backend implementing Microsoft Agent Framework with Azure OpenAI integration. This implementation provides a modern agent-based chat system with multi-agent orchestration capabilities for the AgentCon Workshop.

## ✅ Implementation Status

### **COMPLETED** - All compilation errors fixed and ChatClient issues resolved!

The implementation now includes:

- ✅ **Microsoft Agent Framework Integration** - Using the latest `Microsoft.Agents.AI` package (1.0.0-preview.251002.1)
- ✅ **Azure OpenAI Support** - Full integration with Azure OpenAI services using `Azure.AI.OpenAI` (2.1.0)
- ✅ **Group Chat Service** - Multi-agent conversation orchestration with Microsoft Agent Framework
- ✅ **Base Agent Classes** - Extensible agent foundation with proper interfaces and ChatClient initialization
- ✅ **Specialized Agents** - People Lookup, Knowledge Finder, and Generic agents with proper initialization
- ✅ **Configuration System** - YAML-based configuration with environment variable support
- ✅ **Session Management** - Conversation history and state management
- ✅ **REST API Controllers** - Complete API endpoints for chat and group chat
- ✅ **Error Handling** - Robust error handling throughout the system
- ✅ **ChatClient Initialization** - Fixed null reference issues in agent responses

## 🚀 Quick Start

### Prerequisites
- .NET 9 SDK
- VS Code (C# Dev Kit) or Visual Studio 2022 17.10+
- Azure OpenAI resource with GPT-4 or GPT-4o deployment
- (Optional) Azure AI Foundry project with people/knowledge agents

### Configure Environment

```powershell
cd Backend/dotnet/agentframework
copy .env.template .env
# Edit .env with your Azure credentials
```

Edit `.env` with your Azure credentials:
```bash
# Required - Azure OpenAI Configuration
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
AZURE_OPENAI_API_KEY=your-api-key-here
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o
AZURE_OPENAI_API_VERSION=2024-10-21

# Optional - Azure AI Foundry Configuration
PROJECT_ENDPOINT=https://your-project.services.ai.azure.com/api/projects/your-project
PEOPLE_AGENT_ID=asst-people-agent
KNOWLEDGE_AGENT_ID=asst-knowledge-agent
MANAGED_IDENTITY_CLIENT_ID=<client-id-if-using-UMI>

# Application Configuration
FRONTEND_URL=http://localhost:3001
LOG_LEVEL=Information
```

### Build and Run

```powershell
# Build the project
dotnet build

# Run the application
dotnet run --urls http://localhost:8000
```

**Access Points:**
- **Swagger UI**: `http://localhost:8000` - Interactive API documentation
- **Health Check**: `GET http://localhost:8000/health` - System status
- **Agents List**: `GET http://localhost:8000/agents` - Available agents

> The application runs on HTTP by default for development to avoid certificate issues. Use HTTPS in production.

## ⚙️ Configuration Architecture

### Environment Variables (Priority 1)
```env
# Core Azure OpenAI settings (required)
AZURE_OPENAI_ENDPOINT=https://<resource>.openai.azure.com
AZURE_OPENAI_API_KEY=<your-api-key>
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o
AZURE_OPENAI_API_VERSION=2024-10-21

# Azure AI Foundry settings (optional but recommended)
PROJECT_ENDPOINT=https://<resource>.services.ai.azure.com/api/projects/<project>
PEOPLE_AGENT_ID=asst-people-agent
KNOWLEDGE_AGENT_ID=asst-knowledge-agent
MANAGED_IDENTITY_CLIENT_ID=<client-id>

# Application settings
FRONTEND_URL=http://localhost:3001
LOG_LEVEL=Information
ENVIRONMENT=Development
```

### YAML Configuration (Priority 2)
The `config.yml` file provides:
- **Agent Instructions**: Customizable prompts and behavior
- **Routing Rules**: Smart agent selection patterns
- **Group Chat Templates**: Pre-configured multi-agent scenarios
- **Provider Settings**: Framework-specific configurations

### Configuration Loading Order
1. **Environment Variables** (`.env` file) - Highest priority
2. **YAML Configuration** (`config.yml`) - Fallback for complex settings
3. **Default Values** - Built-in fallbacks for essential functionality

## 🧩 Architecture Overview

### Core Components

```
agentframework/
├── 🏗️ Agents/
│   ├── BaseAgent.cs              # Abstract base with ChatClient management
│   └── SpecificAgents.cs         # GenericAgent, PeopleLookupAgent, KnowledgeFinderAgent
├── ⚙️ Configuration/
│   ├── AgentConfig.cs            # Strongly-typed configuration models
│   └── AzureAIConfig.cs          # Azure service configurations
├── 🌐 Controllers/
│   ├── AgentsController.cs       # Agent discovery and metadata
│   ├── ChatController.cs         # Single-agent conversations
│   └── GroupChatController.cs    # Multi-agent orchestration
├── 🔧 Services/
│   ├── IGroupChatService.cs      # Group chat interface
│   ├── GroupChatService.cs       # Multi-agent coordination logic
│   ├── AgentService.cs           # Agent lifecycle management
│   ├── AgentInstructionsService.cs # YAML-based instruction loading
│   └── SessionManager.cs         # Conversation state management
├── 📊 Models/
│   └── ChatModels.cs             # Request/response data models
├── 📄 config.yml                 # Agent configurations and templates
├── 🔐 .env.template              # Environment setup guide
└── 🚀 Program.cs                 # Application startup and DI container
```

### Service Architecture
- **Dependency Injection**: Full .NET 9 DI container integration
- **Configuration Binding**: Strongly-typed settings with `IOptions<T}`
- **Logging**: Structured logging with configurable levels
- **Error Handling**: Comprehensive exception handling and user-friendly responses
- **Timeout Management**: Intelligent timeout handling for long-running operations

## 🔌 Azure Integration

### Microsoft Agent Framework
- **Package**: `Microsoft.Agents.AI` (1.0.0-preview.251002.1)
- **Features**: Agent orchestration, conversation management, extensible architecture
- **Benefits**: Official Microsoft framework for AI agent development

### Azure OpenAI Integration
- **Primary Client**: `Azure.AI.OpenAI` (2.1.0) for direct Azure integration
- **Fallback Client**: `OpenAI` (2.1.0) for standard OpenAI compatibility
- **Models Supported**: GPT-4, GPT-4o, GPT-3.5-turbo
- **Features**: Streaming responses, token usage tracking, error handling

### Azure AI Foundry (Optional)
- **Package**: `Azure.AI.Projects` (1.0.0-beta.11)
- **Capabilities**: Enterprise agent definitions, specialized workflows
- **Authentication**: `DefaultAzureCredential` with managed identity support
- **Fallback**: Graceful degradation to Azure OpenAI when Foundry is unavailable

## 📡 API Reference

### Core Endpoints

| Method | Endpoint | Description | Request Body |
|--------|----------|-------------|--------------|
| `GET` | `/health` | System health and configuration status | None |
| `GET` | `/agents` | List all available agents with capabilities | None |
| `POST` | `/chat` | Single-agent conversation | `{"message": "string", "agent": "string?", "session_id": "string?"}` |
| `POST` | `/group-chat` | Multi-agent orchestration | `{"message": "string", "agents": ["string"]?, "max_turns": number?}` |
| `GET` | `/group-chat/templates` | Available group chat templates | None |
| `GET` | `/group-chats` | Active group chat sessions | None |

### Response Format
All endpoints return JSON with consistent error handling:
```json
{
  "content": "Agent response content",
  "agent": "agent_name",
  "session_id": "unique_session_id",
  "timestamp": "2024-01-01T00:00:00Z",
  "metadata": {
    "agent_framework": true,
    "processing_time_ms": 1500,
    "model": "gpt-4o"
  }
}
```

## 🧪 Testing & Development

### Interactive Notebook
The `workshop_dotnet_agent_framework.ipynb` notebook provides:
- **Step-by-step tutorials** for Agent Framework concepts
- **Live code examples** with executable cells
- **Agent testing scenarios** for different use cases
- **Group chat demonstrations** with multiple agents
- **API integration examples** connecting to the backend

### Manual API Testing
```bash
# Health check
curl http://localhost:8000/health

# List available agents
curl http://localhost:8000/agents

# Single agent conversation
curl -X POST http://localhost:8000/chat \
     -H "Content-Type: application/json" \
     -d '{"message": "Explain Microsoft Agent Framework", "agent": "generic_agent"}'

# Multi-agent group chat
curl -X POST http://localhost:8000/group-chat \
     -H "Content-Type: application/json" \
     -d '{"message": "Plan a software project with team coordination", "max_turns": 2}'

# Auto-select agents (no agents specified)
curl -X POST http://localhost:8000/group-chat \
     -H "Content-Type: application/json" \
     -d '{"message": "Help me with both people and technical questions"}'
```

### Development Tools
- **Swagger UI**: Comprehensive API documentation and testing interface
- **Structured Logging**: Detailed request/response logging for debugging
- **Error Details**: Descriptive error messages with troubleshooting hints
- **Performance Metrics**: Processing time tracking for optimization

## 🛠️ Key Features & Capabilities

### 1. Microsoft Agent Framework Integration
- **Latest Preview**: Uses cutting-edge `Microsoft.Agents.AI` package
- **Native Patterns**: Implements Microsoft's recommended agent architectures
- **Extensible Design**: Easy to add new agent types and capabilities
- **Production Ready**: Robust error handling and performance optimization

### 2. Multi-Agent Orchestration
- **Smart Routing**: Context-aware agent selection based on message content
- **Sequential Processing**: Organized turn-based multi-agent conversations
- **Termination Logic**: Intelligent conversation ending based on agent signals
- **Session Management**: Persistent conversation history across requests

### 3. Configuration-Driven Architecture
- **YAML-Based Setup**: Human-readable agent configurations
- **Environment Override**: Flexible deployment across environments
- **Hot Reload**: Runtime configuration updates without restart
- **Validation**: Comprehensive configuration validation with helpful error messages

### 4. Production-Grade Features
- **Timeout Protection**: Prevents hanging requests with configurable timeouts
- **Rate Limiting**: Built-in protection against API abuse
- **Health Monitoring**: Comprehensive health checks for all dependencies
- **CORS Support**: Frontend-friendly cross-origin resource sharing
- **Structured Logging**: Detailed operational insights for monitoring

## 📦 Dependencies & Versions

### Core Packages
```xml
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-preview.251002.1" />
<PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" />
<PackageReference Include="OpenAI" Version="2.1.0" />
<PackageReference Include="Azure.Identity" Version="1.16.0" />
<PackageReference Include="Azure.AI.Projects" Version="1.0.0-beta.11" />
```

### Configuration & Utilities
```xml
<PackageReference Include="DotNetEnv" Version="3.1.1" />
<PackageReference Include="NetEscapades.Configuration.Yaml" Version="3.1.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.8" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.8" />
<PackageReference Include="Microsoft.Extensions.Logging.Console" Version="9.0.8" />
```

## 🔧 Build & Deployment Status

### ✅ Current Status
- **Build**: ✅ **SUCCESS** - No compilation errors
- **Tests**: ✅ **PASSING** - All integration tests working
- **Runtime**: ✅ **STABLE** - Application starts and serves requests
- **ChatClient**: ✅ **FIXED** - Null reference issues resolved
- **Agents**: ✅ **OPERATIONAL** - All agent types working correctly

### Recent Fixes
- **ChatClient Initialization**: Fixed null reference exceptions in agent responses
- **Agent Constructor**: Updated to properly inject Azure configuration
- **Environment Loading**: Improved .env file processing and fallback handling
- **Error Messages**: Enhanced error reporting with actionable troubleshooting steps

## 🎯 Workshop Integration

### Learning Objectives
This implementation demonstrates:

1. **Modern .NET 9 Development**
   - Latest C# language features and patterns
   - Advanced dependency injection and configuration
   - Async/await best practices and timeout handling

2. **Microsoft Agent Framework Mastery**
   - Official Microsoft AI agent framework usage
   - Agent lifecycle management and orchestration
   - Multi-agent conversation coordination

3. **Azure Cloud Integration**
   - Production-ready Azure OpenAI integration
   - Azure AI Foundry specialized agent capabilities
   - Azure Identity and credential management

4. **Enterprise Architecture Patterns**
   - Clean architecture with separation of concerns
   - Configuration-driven design for flexibility
   - Comprehensive error handling and logging

### Workshop Activities
- **Hands-on Agent Development**: Create custom agents with specific capabilities
- **Multi-Agent Scenarios**: Build complex workflows with agent coordination
- **Configuration Customization**: Modify agent behavior through YAML configs
- **API Integration**: Connect frontend applications to the agent backend
- **Performance Optimization**: Tune timeout and concurrency settings

## 🛠️ Extending the System

### Adding New Agents
1. **Create Agent Class**: Inherit from `BaseAgent` or specialized base classes
2. **Define Instructions**: Add agent configuration to `config.yml`
3. **Register Service**: Update `AgentService` with new agent factory
4. **Update Routing**: Modify routing logic in `GroupChatService` if needed

### Custom Configurations
1. **Environment Variables**: Add new settings to `.env.template`
2. **YAML Sections**: Extend `config.yml` with new configuration blocks
3. **Configuration Models**: Create strongly-typed classes in `Configuration/`
4. **Validation**: Add configuration validation in startup code

### API Extensions
1. **New Controllers**: Follow existing patterns for consistent API design
2. **Request Models**: Define request/response models in `Models/`
3. **Service Integration**: Wire new functionality through dependency injection
4. **Documentation**: Update Swagger documentation with XML comments

## 🔒 Security & Best Practices

### Environment Security
- **Secret Management**: Keep `.env` files out of source control
- **API Key Rotation**: Support for runtime credential updates
- **Managed Identity**: Prefer Azure managed identities in production
- **HTTPS Enforcement**: Enable HTTPS redirection for production deployments

### Operational Security
- **Input Validation**: Comprehensive request validation and sanitization
- **Rate Limiting**: Protection against API abuse and DoS attacks
- **Error Handling**: Secure error messages without sensitive data exposure
- **Logging**: Structured logging without credential leakage

### Development Practices
- **Configuration Validation**: Startup-time validation of all required settings
- **Graceful Degradation**: Fallback behavior when optional services are unavailable
- **Timeout Management**: Prevent resource exhaustion with proper timeout handling
- **Resource Cleanup**: Proper disposal of HTTP clients and other resources

## 🆘 Troubleshooting Guide

### Common Issues

#### Build Errors
- **Missing Dependencies**: Run `dotnet restore` to ensure all packages are installed
- **Version Conflicts**: Check package versions match the ones specified above
- **SDK Version**: Ensure .NET 9 SDK is installed and active

#### Runtime Issues
- **Application Won't Start**: Check `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_API_KEY` in `.env`
- **Agent Null Reference**: Verify ChatClient initialization by checking logs for initialization messages
- **Timeout Errors**: Reduce `max_turns` in group chat requests or increase timeout settings

#### Configuration Problems
- **Invalid YAML**: Validate `config.yml` syntax using online YAML validators
- **Missing Environment Variables**: Copy from `.env.template` and populate required values
- **Azure Authentication**: Ensure Azure credentials are properly configured

### Debugging Tips
- **Enable Debug Logging**: Set `LOG_LEVEL=Debug` in `.env` for detailed logs
- **Check Health Endpoint**: Use `/health` to verify all services are properly configured
- **Swagger UI**: Use the interactive API documentation for testing endpoints
- **Agent Response Debugging**: Check individual agent responses before testing group chat

### Performance Optimization
- **Concurrent Requests**: Adjust `max_concurrent_requests` in `config.yml`
- **Timeout Settings**: Balance responsiveness with completion rates
- **Agent Selection**: Use targeted agent lists instead of auto-selection for better performance
- **Session Management**: Clean up old sessions periodically to prevent memory leaks

---

This implementation provides a comprehensive foundation for building sophisticated AI agent systems using Microsoft's Agent Framework in .NET 9. The system is production-ready, extensively documented, and perfect for learning modern AI agent development patterns.

For additional support, refer to the interactive notebook (`workshop_dotnet_agent_framework.ipynb`) or examine the source code with its comprehensive inline documentation.