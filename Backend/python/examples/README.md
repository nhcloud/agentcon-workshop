# Python Configuration Examples

This directory contains example configurations for Python-based AI agent implementations using LangChain and Semantic Kernel.

## Directory Structure

```
python/examples/
├── README.md                   # This file
├── config.minimal.yml         # Minimal configuration with one agent
├── config.development.yml     # Development configuration with debug settings
├── config.production.yml      # Production-ready configuration template
├── langchain/
│   └── config.yml             # LangChain-optimized configuration
└── sk/
    └── config.yml             # Semantic Kernel multi-provider configuration
```

## Quick Start

### For LangChain Implementation

```bash
# Copy the LangChain configuration
cp backend/python/examples/langchain/config.yml backend/python/langchain/config.yml

# Edit the configuration file
nano backend/python/langchain/config.yml
```

### For Semantic Kernel Implementation

```bash
# Copy the Semantic Kernel configuration
cp backend/python/examples/sk/config.yml backend/python/sk/config.yml

# Edit the configuration file
nano backend/python/sk/config.yml
```

## Configuration Files

### Framework-Specific
- `langchain/config.yml` - LangChain-specific configuration with Azure AI integration
- `sk/config.yml` - Semantic Kernel with multi-provider support (Azure OpenAI, Azure AI Foundry, Gemini, Bedrock)

### Environment-Specific
- `config.minimal.yml` - Minimal configuration for quick testing
- `config.development.yml` - Development-friendly settings with debugging enabled
- `config.production.yml` - Production-ready configuration template

## Environment Variables

### Required for LangChain
```bash
export AZURE_INFERENCE_ENDPOINT="https://your-inference-endpoint.com"
export AZURE_INFERENCE_CREDENTIAL="your-credential-key"
export PROJECT_ENDPOINT="https://your-project-endpoint.com"
```

### Required for Semantic Kernel (Azure OpenAI)
```bash
export AZURE_OPENAI_ENDPOINT="https://your-openai.openai.azure.com"
export AZURE_OPENAI_DEPLOYMENT="gpt-4o"
export AZURE_OPENAI_KEY="your-openai-key"
```

### Optional - Additional Providers
```bash
# Google Gemini
export GOOGLE_API_KEY="your-gemini-key"

# AWS Bedrock
export AWS_ACCESS_KEY_ID="your-aws-access-key"
export AWS_SECRET_ACCESS_KEY="your-aws-secret-key"
export AWS_REGION="us-east-1"
```

## Running the Applications

### LangChain
```bash
cd backend/python/langchain
uvicorn main:app --reload --port 8000
```

### Semantic Kernel
```bash
cd backend/python/sk
uvicorn main:app --reload --port 8001
```

## Configuration Schema

All Python configurations use YAML format with the following structure:

```yaml
app:
  title: "Application Title"
  version: "2.0.0"
  frontend_url: "${FRONTEND_URL:*}"
  log_level: "${LOG_LEVEL:INFO}"
  host: "0.0.0.0"
  port: 8000

agents:
  agent_name:
    type: "agent_type"
    enabled: true
    instructions: "Agent instructions"
    metadata:
      description: "Agent description"
      capabilities: ["capability1", "capability2"]
    framework_config:
      provider: "azure_openai"
      model: "gpt-4o"
      # Additional provider-specific settings
```

## See Also

- [LangChain Documentation](../langchain/README.md)
- [Semantic Kernel Documentation](../sk/README.md)
- [.NET Examples](../../dotnet/examples/)