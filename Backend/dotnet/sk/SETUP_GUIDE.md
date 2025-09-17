# .NET Semantic Kernel Agents Workshop - Complete Setup Guide

This guide provides examples for setting up the .NET Semantic Kernel workshop to match the Python implementation.

## Configuration Examples

### Option 1: Environment Variables (.env file)

Create a `.env` file in the `sk` directory:

```bash
# Azure OpenAI Configuration (Required)
AZURE_OPENAI_ENDPOINT=https://your-resource-name.openai.azure.com
AZURE_OPENAI_API_KEY=your-azure-openai-api-key-here
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o
AZURE_OPENAI_API_VERSION=2024-02-01

# Azure AI Foundry Configuration (Optional - for enterprise features)
PROJECT_ENDPOINT=https://your-resource-name.services.ai.azure.com/api/projects/your-project-name
PEOPLE_AGENT_ID=asst-your-people-agent-id-here
KNOWLEDGE_AGENT_ID=asst-your-knowledge-agent-id-here

# Application Configuration
FRONTEND_URL=http://localhost:3001
PORT=8002
ASPNETCORE_ENVIRONMENT=Development
```

### Option 2: appsettings.json

Update your `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.SemanticKernel": "Information"
    }
  },
  "AllowedHosts": "*",
  "FRONTEND_URL": "http://localhost:3001",
  "AzureAI": {
    "AzureOpenAI": {
      "Endpoint": "https://your-resource-name.openai.azure.com",
      "ApiKey": "your-azure-openai-api-key-here",
      "DeploymentName": "gpt-4o",
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

## Available Agents

### Standard Agents
- `generic` - General-purpose conversational agent
- `people_lookup` - Expert at finding people and contact information
- `knowledge_finder` - Specialist at searching and retrieving information
- `task_assistant` - Expert at task management and productivity
- `technical_advisor` - Specialist in technical guidance and code review
- `creative_assistant` - Expert at creative thinking and content creation

### Azure AI Foundry Agents (Enterprise)
- `foundry_people_lookup` - Enterprise people lookup with Azure AD integration
- `foundry_knowledge_finder` - Enterprise knowledge search with advanced security

## API Examples

### 1. Test Single Agent
```bash
curl -X POST "http://localhost:8002/api/agents/technical_advisor/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What are the best practices for .NET 9 development?",
    "session_id": "my-session-123"
  }'
```

### 2. Start Group Chat
```bash
curl -X POST "http://localhost:8002/api/groupchat" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "How should we approach building a scalable AI agent system?",
    "agents": ["technical_advisor", "task_assistant", "creative_assistant"],
    "max_turns": 2,
    "use_semantic_kernel_groupchat": true
  }'
```

### 3. Test Azure AI Foundry Agent
```bash
curl -X POST "http://localhost:8002/api/agents/foundry/people_lookup/test" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Find experts in machine learning within our organization"
  }'
```

### 4. Get Available Agents
```bash
curl "http://localhost:8002/api/agents"
```

### 5. Check Configuration Status
```bash
curl "http://localhost:8002/api/config"
```

## Workshop Progression

### Step 1: Basic Setup
1. Configure Azure OpenAI credentials
2. Test basic agent functionality
3. Explore different agent types

### Step 2: Group Chat
1. Start simple group chats
2. Try enhanced Semantic Kernel group chat
3. Experiment with different agent combinations

### Step 3: Azure AI Foundry (Enterprise)
1. Configure Azure AI Foundry project
2. Test enterprise agents
3. Compare with standard agents

### Step 4: Advanced Features
1. Session management
2. Conversation summarization
3. Agent capability exploration

## Troubleshooting

### Common Issues

1. **Configuration not loading**
   - Ensure `.env` file is in the correct location (`sk` directory)
   - Check file permissions and encoding
   - Verify environment variable names match exactly

2. **Azure OpenAI connection errors**
   - Verify endpoint URL format (include https://)
   - Check API key permissions
   - Ensure deployment name exists in your Azure resource

3. **Agent not found errors**
   - Use exact agent names (case-sensitive)
   - Check available agents with `/api/agents` endpoint
   - Verify agent initialization completed successfully

4. **Azure AI Foundry agents not working**
   - Verify project endpoint configuration
   - Check agent IDs are correctly specified
   - Ensure proper Azure permissions

### Debug Commands

```bash
# Check health status
curl http://localhost:8002/health

# Verify configuration
curl http://localhost:8002/api/config

# List available agents
curl http://localhost:8002/api/agents

# Get agent capabilities
curl http://localhost:8002/api/agents/technical_advisor/capabilities
```

## Comparison with Python Version

| Feature | Python Workshop | .NET Workshop |
|---------|----------------|---------------|
| Configuration | `.env` files | `.env` + appsettings.json |
| Agent Framework | Semantic Kernel Python | Semantic Kernel .NET |
| API Style | FastAPI | ASP.NET Core Web API |
| Agent Types | Custom classes | Service-based architecture |
| Group Chat | AgentGroupChat | Enhanced simulation + standard |
| Documentation | Jupyter notebooks | Swagger/OpenAPI |
| Enterprise Features | Azure AI Foundry | Azure AI Foundry + .NET patterns |

## Next Steps

1. **Custom Agents**: Create domain-specific agents for your use case
2. **Memory Integration**: Add vector databases and persistent memory
3. **Production Deployment**: Configure for Azure App Service or Kubernetes
4. **Frontend Integration**: Connect with React, Blazor, or other frontends
5. **Monitoring**: Add Application Insights and comprehensive logging

## Workshop Completion

You've successfully implemented:
- ? Basic Semantic Kernel agents (.NET style)
- ? Azure OpenAI integration
- ? Group chat capabilities
- ? Azure AI Foundry enterprise agents
- ? Session management
- ? Comprehensive API endpoints
- ? Configuration flexibility
- ? Error handling and logging

The .NET implementation provides equivalent functionality to the Python workshop while leveraging .NET-specific patterns and enterprise features.