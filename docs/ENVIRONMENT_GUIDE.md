# Environment Configuration Guide

This guide explains the environment variable configuration for both Semantic Kernel and LangChain implementations of the Agents Workshop.

## 🔐 Security Best Practices

### 1. File Protection
- **Never commit `.env` files** to version control
- Use `.env.template` files for documentation
- Set appropriate file permissions: `chmod 600 .env`
- Store production secrets in secure vaults (Azure Key Vault, AWS Secrets Manager)

### 2. Environment Separation
- Use different credentials for development, staging, and production
- Rotate API keys regularly
- Use managed identities in production when possible
- Monitor API usage and set up alerts for unusual activity

## 📁 File Structure

```
Backend/python/
├── env.template         # Shared template with placeholder values for Python
├── sk/
│   └── .env            # Actual environment variables (SK-specific)
└── langchain/
    └── .env            # Actual environment variables (LC-specific)
```

## ⚙️ Configuration Sections

### Application Configuration
Controls basic application behavior:
- `ENVIRONMENT`: Determines logging level and debug features
- `HOST`/`PORT`: Server binding configuration
- `LOG_LEVEL`: Controls log verbosity

### AI Service Configuration

#### Semantic Kernel (semantic_kernel)
- **Primary**: Azure OpenAI with direct SK integration
- **Fallback**: Azure AI Foundry agents
- **Optional**: AWS Bedrock, Google Gemini

#### LangChain (langchain)  
- **Primary**: Azure AI Inference API for LangChain compatibility
- **Fallback**: Azure OpenAI via LangChain adapters
- **Optional**: AWS Bedrock, Google Gemini via LangChain

### Framework-Specific Features

#### Semantic Kernel
```env
# Native SK kernel configuration
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
AZURE_OPENAI_API_KEY=your_key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4
```

#### LangChain
```env
# LangChain AI inference configuration  
AZURE_INFERENCE_ENDPOINT=https://your-resource.cognitiveservices.azure.com/models
AZURE_INFERENCE_CREDENTIAL=your_credential

# LangChain-specific features
LANGCHAIN_TRACING_V2=true
LANGCHAIN_PROJECT=your-project-name
```

## 🚀 Quick Setup

### 1. Copy Template Files
```bash
# For Semantic Kernel
cd Backend/python/sk
cp ../env.template .env

# For LangChain  
cd Backend/python/langchain
cp ../env.template .env
```

### 2. Configure Required Variables

#### Minimum Configuration (Both Frameworks)
```env
# Application
ENVIRONMENT=development
PORT=8000  # or 8001 for SK

# Azure OpenAI (Primary)
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
AZURE_OPENAI_API_KEY=your_key_here
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4

# Azure AI Foundry (For existing agents)
AZURE_AI_PROJECT_ENDPOINT=https://your-resource.services.ai.azure.com/api/projects/your-project
AZURE_FOUNDRY_PEOPLE_AGENT_ID=asst_your_people_agent
AZURE_FOUNDRY_KNOWLEDGE_AGENT_ID=asst_your_knowledge_agent
```

#### Additional Configuration (LangChain)
```env
# LangChain-specific Azure AI Inference
AZURE_INFERENCE_ENDPOINT=https://your-resource.cognitiveservices.azure.com/models
AZURE_INFERENCE_CREDENTIAL=your_credential

# Enable AI features
GROUP_CHAT_AI_SPEAKER_SELECTION=true
GROUP_CHAT_AI_SUMMARIZATION=true
```

### 3. Validate Configuration
```bash
# Test SK configuration
cd Backend/python/sk
python -c "from dotenv import load_dotenv; load_dotenv(); import os; print('SK config loaded:', bool(os.getenv('AZURE_OPENAI_ENDPOINT')))"

# Test LC configuration  
cd Backend/python/langchain
python -c "from dotenv import load_dotenv; load_dotenv(); import os; print('LC config loaded:', bool(os.getenv('AZURE_INFERENCE_ENDPOINT')))"
```

## 🔧 Advanced Configuration

### Session Management
```env
# Basic session configuration
SESSION_TIMEOUT_MINUTES=30
SESSION_CLEANUP_INTERVAL_MINUTES=5

# Redis for distributed sessions (optional)
REDIS_URL=redis://localhost:6379
REDIS_PASSWORD=your_redis_password
```

### Security Settings
```env
# CORS configuration
CORS_ALLOW_ORIGINS=["http://localhost:3000","http://localhost:3001"]
CORS_ALLOW_CREDENTIALS=true

# Rate limiting
RATE_LIMIT_PER_MINUTE=60
RATE_LIMIT_BURST=10
```

### Group Chat Features
```env
# General settings
GROUP_CHAT_MAX_PARTICIPANTS=10
GROUP_CHAT_MAX_TURNS=50
GROUP_CHAT_DEFAULT_TIMEOUT_MINUTES=60

# LangChain AI features
GROUP_CHAT_AI_SPEAKER_SELECTION=true
GROUP_CHAT_AI_SUMMARIZATION=true
GROUP_CHAT_CONVERSATION_MEMORY=true
```

### Monitoring & Observability
```env
# Application Insights
APPINSIGHTS_CONNECTION_STRING=your_connection_string

# Health checks
HEALTH_CHECK_TIMEOUT_SECONDS=30

# LangChain tracing
LANGCHAIN_TRACING_V2=true
LANGCHAIN_API_KEY=your_langsmith_key
```

## 🌍 Environment-Specific Configurations

### Development
```env
ENVIRONMENT=development
LOG_LEVEL=debug
PORT=8000  # LC
PORT=8001  # SK
CORS_ALLOW_ORIGINS=["http://localhost:3000","http://localhost:3001"]
```

### Staging
```env
ENVIRONMENT=staging
LOG_LEVEL=info
HOST=0.0.0.0
CORS_ALLOW_ORIGINS=["https://staging.yourapp.com"]
RATE_LIMIT_PER_MINUTE=120
```

### Production
```env
ENVIRONMENT=production
LOG_LEVEL=warning
HOST=0.0.0.0
CORS_ALLOW_ORIGINS=["https://yourapp.com"]
RATE_LIMIT_PER_MINUTE=600
HEALTH_CHECK_TIMEOUT_SECONDS=10
```

## 🔍 Troubleshooting

### Common Issues

#### 1. Authentication Errors
```bash
# Check API key validity
curl -H "api-key: YOUR_KEY" https://your-resource.openai.azure.com/openai/deployments

# Verify endpoint format
echo $AZURE_OPENAI_ENDPOINT  # Should include https://
```

#### 2. Configuration Loading
```python
# Debug environment loading
from dotenv import load_dotenv
import os

load_dotenv()
print("Loaded variables:", [k for k in os.environ.keys() if 'AZURE' in k])
```

#### 3. Service Connectivity
```bash
# Test endpoint connectivity
curl -I https://your-resource.openai.azure.com

# Check DNS resolution
nslookup your-resource.openai.azure.com
```

### Environment Variable Validation

Both frameworks include built-in validation:
```python
# Will fail fast if required variables are missing
python main.py
```

## 📚 Additional Resources

- [Azure OpenAI Documentation](https://docs.microsoft.com/azure/openai/)
- [Azure AI Foundry Guide](https://docs.microsoft.com/azure/ai-foundry/)
- [LangChain Azure Integration](https://python.langchain.com/docs/integrations/providers/azure/)
- [Semantic Kernel Documentation](https://learn.microsoft.com/semantic-kernel/)

## 🔄 Migration Guide

### From Old Format to New Format
1. Back up existing `.env` files
2. Copy values to new structured format
3. Update variable names per the mapping below
4. Test configuration with validation scripts

### Variable Name Mappings
```
# Old → New
AZURE_OPENAI_KEY → AZURE_OPENAI_API_KEY
PROJECT_ENDPOINT → AZURE_AI_PROJECT_ENDPOINT
AGENT_ID → AZURE_FOUNDRY_PEOPLE_AGENT_ID
KNOWLEDGE_AGENT_ID → AZURE_FOUNDRY_KNOWLEDGE_AGENT_ID
AZURE_SPEECH_KEY → AZURE_SPEECH_API_KEY
```
