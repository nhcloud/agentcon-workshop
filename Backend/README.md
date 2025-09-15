# Modern AI Agent System

A modernized, extensible multi-agent system supporting both LangChain and Semantic Kernel frameworks with a shared architecture for easy agent management.

## Architecture Overview

The system has been completely refactored using modern software engineering principles:

### Core Design Principles

1. **Plugin-Based Architecture**: Easy to add/remove agents without code changes
2. **Dependency Injection**: Loose coupling between components  
3. **Factory Pattern**: Standardized agent creation and management
4. **Abstract Interfaces**: Framework-agnostic design
5. **Separation of Concerns**: Clear boundaries between routing, session management, and agent logic

### Project Structure

```
Backend/
├── shared/                     # Common libraries and interfaces
│   ├── core/                   # Base classes and interfaces
│   ├── agents/                 # Base agent implementations
│   ├── config/                 # Configuration management
│   ├── routers/                # Routing implementations
│   └── sessions/               # Session management
├── langchain/                  # LangChain implementation (Python)
│   ├── agents/                 # LangChain-specific agents
│   ├── routers/                # LangChain-specific routers
│   ├── main.py                 # FastAPI application
│   ├── config.yml              # Configuration file
│   └── requirements.txt        # Dependencies
├── python_semantic_kernel/     # Semantic Kernel implementation (Python)
│   ├── agents/                 # SK-specific agents
│   ├── routers/                # SK-specific routers
│   ├── main.py                 # FastAPI application
│   ├── config.yml              # Configuration file
│   └── requirements.txt        # Dependencies
├── dotnet_semantic_kernel/     # .NET 9 Semantic Kernel implementation
│   ├── Controllers/            # ASP.NET Core API controllers
│   ├── Services/               # Business logic services
│   ├── Agents/                 # Agent implementations
│   ├── Models/                 # Data transfer objects
│   ├── Configuration/          # Configuration classes
│   ├── Program.cs              # Application entry point
│   └── DotNetSemanticKernel.csproj  # Project file
```

## Key Features

### 1. Unified Agent Interface
All agents implement the same `IAgent` interface:
```python
class IAgent(ABC):
    async def process_message(self, message: str, history: List[AgentMessage], metadata: Dict) -> AgentResponse
    async def initialize(self) -> None
    async def cleanup(self) -> None
    def get_capabilities(self) -> List[str]
```

### 2. Flexible Configuration
YAML-based configuration with environment variable substitution:
```yaml
agents:
  custom_agent:
    type: "generic"
    enabled: true
    instructions: "Your agent instructions here"
    framework_config:
      provider: "azure_openai"
      model: "gpt-4o"
```

### 3. Smart Routing
Multiple routing strategies:
- Pattern-based routing
- LLM-powered intelligent routing  
- Hybrid approach combining both
- History-aware routing for conversation continuity

### 4. Session Management
Multiple session storage options:
- In-memory (development)
- File-based persistence
- Redis for distributed deployments

### 5. Health Monitoring
Built-in health checks and system monitoring:
- Agent availability status
- Performance metrics
- Error tracking

## Quick Start

### LangChain Implementation

1. **Install dependencies:**
```bash
cd Backend/langchain
pip install -r requirements.txt
```

2. **Configure environment:**
```bash
# Copy and edit configuration
cp config.yml.example config.yml

# Set required environment variables
export AZURE_INFERENCE_ENDPOINT="https://your-endpoint.com"
export AZURE_INFERENCE_CREDENTIAL="your-key"
export PROJECT_ENDPOINT="https://your-project.com"
```

3. **Run the application:**
```bash
python main.py
```

### Semantic Kernel Implementation

1. **Install dependencies:**
```bash
cd Backend/python_semantic_kernel
pip install -r requirements.txt
```

2. **Configure environment:**
```bash
# Set required environment variables
export AZURE_OPENAI_ENDPOINT="https://your-openai.com"
export AZURE_OPENAI_DEPLOYMENT="your-deployment"
export AZURE_OPENAI_KEY="your-key"
```

3. **Run the application:**
```bash
python main.py
```

### .NET 9 Semantic Kernel Implementation

1. **Install dependencies:**
```bash
cd Backend/dotnet_semantic_kernel
dotnet restore
```

2. **Configure environment:**
```bash
# Copy environment template
cp .env.template .env

# Edit .env with your Azure OpenAI or GitHub Models credentials
# AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
# AZURE_OPENAI_API_KEY=your_api_key
# OR
# GITHUB_TOKEN=your_github_token (for GitHub Models)
```

3. **Run the application:**
```bash
# Development mode with hot reload
dotnet watch run

# Or standard run
dotnet run
```

The .NET API will be available at `http://localhost:8002` with Swagger UI for testing.

## Adding New Agents

### 1. Create Agent Class
```python
from shared import BaseAgent, AgentConfig, AgentMessage, AgentResponse

class CustomAgent(BaseAgent):
    async def initialize(self):
        await super().initialize()
        # Your initialization code here
    
    async def process_message(self, message: str, history: List[AgentMessage], metadata: Dict) -> AgentResponse:
        # Your agent logic here
        return self._create_response("Your response", metadata)
```

### 2. Register with Factory
```python
# In your agent factory
def create_agent(self, config: AgentConfig) -> BaseAgent:
    if config.agent_type == AgentType.CUSTOM:
        return CustomAgent(config)
    # ... other agents
```

### 3. Update Configuration
```yaml
agents:
  my_custom_agent:
    type: "custom"
    enabled: true
    instructions: "You are a custom agent that..."
    framework_config:
      custom_setting: "value"
```

### 4. Test Your Agent
```bash
curl -X POST "http://localhost:8000/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Hello custom agent",
    "agent": "my_custom_agent"
  }'
```

## API Endpoints

### Core Endpoints
- `POST /chat` - Process a chat message
- `POST /chat/stream` - Stream chat responses
- `GET /agents` - List all agents
- `GET /health` - System health check

### Session Management  
- `GET /messages/{session_id}` - Get session messages
- `DELETE /messages/{session_id}` - Delete session

### Agent Management
- `GET /agents/{agent_name}/info` - Get agent details
- `POST /agents/{agent_name}/toggle` - Enable/disable agent

### System Information
- `GET /system/stats` - System statistics

## Configuration Reference

### Environment Variables

#### Required (LangChain)
- `AZURE_INFERENCE_ENDPOINT` - Azure AI Inference endpoint
- `PROJECT_ENDPOINT` - Azure AI Foundry project endpoint

#### Required (Semantic Kernel)  
- `AZURE_OPENAI_ENDPOINT` - Azure OpenAI endpoint
- `AZURE_OPENAI_DEPLOYMENT` - Model deployment name
- `AZURE_OPENAI_KEY` - API key

#### Optional
- `FRONTEND_URL` - CORS origin (default: "*")
- `LOG_LEVEL` - Logging level (default: "INFO")
- `SESSION_STORAGE_TYPE` - Session storage ("memory", "file", "redis")
- `REDIS_URL` - Redis connection string
- `GOOGLE_API_KEY` - For Gemini agents
- `AWS_BEDROCK_AGENT_ID` - For Bedrock agents

### Configuration Sections

#### App Settings
```yaml
app:
  title: "Your Agent System"
  version: "2.0.0"
  frontend_url: "${FRONTEND_URL:*}"
```

#### Agent Configuration
```yaml
agents:
  agent_name:
    type: "generic|people_lookup|knowledge_finder|custom"
    enabled: true|false
    instructions: "Agent instructions"
    metadata:
      description: "Agent description"
      capabilities: ["capability1", "capability2"]
    framework_config:
      provider: "azure_openai|azure_foundry|gemini|bedrock"
      # Provider-specific settings
```

#### Router Configuration
```yaml
router:
  type: "pattern|llm|hybrid"
  fallback_to_llm: true
  patterns:
    agent_name:
      - "regex_pattern_1"
      - "regex_pattern_2"
```

## Best Practices

### 1. Agent Design
- Keep agents focused on specific capabilities
- Use meaningful names and clear instructions
- Implement proper error handling
- Add comprehensive logging

### 2. Configuration Management
- Use environment variables for secrets
- Provide sensible defaults
- Document all configuration options
- Validate configuration at startup

### 3. Testing
- Test each agent independently
- Use different test sessions
- Verify routing behavior
- Test error scenarios

### 4. Production Deployment
- Use Redis for session storage
- Enable structured logging
- Monitor health endpoints
- Set appropriate resource limits

## Troubleshooting

### Common Issues

1. **Agent Not Found**
   - Check agent is enabled in config
   - Verify agent factory registration
   - Check initialization errors in logs

2. **Routing Issues**
   - Review routing patterns
   - Check available agents list
   - Test with forced agent parameter

3. **Configuration Errors**
   - Validate YAML syntax
   - Check environment variable expansion
   - Review required vs optional settings

4. **Performance Issues**
   - Check agent initialization time
   - Monitor session storage performance
   - Review caching configuration

### Debug Mode
Enable debug logging:
```bash
export LOG_LEVEL=DEBUG
export DEBUG_LOGS=1
```

### Health Checks
Monitor system health:
```bash
curl http://localhost:8000/health
```

## Modern Architecture Benefits

The current system provides multiple implementation options:

### Advanced LangChain Features (langchain/)
- Unified shared component architecture
- Advanced routing with multiple strategies
- Comprehensive configuration management
- Professional session management
- Enterprise-grade error handling

### Enhanced Semantic Kernel Features (python_semantic_kernel/)
- Modern interface implementations
- Improved Kernel initialization
- Unified configuration system
- Multi-provider support with health checking
- Professional logging and monitoring

### .NET 9 Semantic Kernel Features (dotnet_semantic_kernel/)
- Modern .NET 9 and C# implementation
- Native Semantic Kernel integration
- ASP.NET Core Web API with Swagger documentation
- Dependency injection and professional architecture
- Azure AI Inference and GitHub Models support
- High-performance concurrent session management
- Comprehensive error handling and logging
- Multi-provider support with health checking
- Professional logging and monitoring

## Contributing

### Adding New Features
1. Define interfaces in `shared/core/`
2. Implement in framework-specific modules
3. Add configuration support
4. Write tests and documentation
5. Update this README

### Code Style
- Follow Python PEP 8
- Use type hints
- Add docstrings to all classes and methods
- Include error handling and logging

## Future Enhancements

### Planned Features
- [ ] Authentication and authorization
- [ ] Rate limiting
- [ ] Metrics and observability
- [ ] Plugin marketplace
- [ ] Multi-modal support
- [ ] Workflow orchestration
- [ ] Agent collaboration patterns

### Extension Points
- Custom session storage backends
- Additional routing strategies
- New agent providers
- Custom middleware
- Health check plugins

## Support

For questions and issues:
1. Check this documentation
2. Review configuration examples
3. Check application logs
4. Test with minimal configuration
5. Create detailed issue reports
