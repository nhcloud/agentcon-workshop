# Configuration Examples

This directory contains example configurations for different deployment scenarios.

## Quick Start

Copy one of the example configurations and customize it for your environment:

```bash
# For LangChain implementation
cp examples/config.langchain.yml langchain/config.yml

# For Semantic Kernel implementation  
cp examples/config.semantic_kernel.yml semantic_kernel/config.yml

# Edit the configuration file
nano langchain/config.yml
```

## Configuration Files

- `config.minimal.yml` - Minimal configuration with one agent
- `config.langchain.yml` - LangChain-specific configuration
- `config.semantic_kernel.yml` - Semantic Kernel with multi-provider support
- `config.production.yml` - Production-ready configuration template
- `config.development.yml` - Development configuration with debug settings

## Environment Variables

Set these environment variables before starting the application:

### Required (LangChain)
```bash
export AZURE_INFERENCE_ENDPOINT="https://your-inference-endpoint.com"
export AZURE_INFERENCE_CREDENTIAL="your-credential-key"
export PROJECT_ENDPOINT="https://your-project-endpoint.com"
```

### Required (Semantic Kernel - Azure OpenAI)
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
export AWS_BEDROCK_AGENT_ID="your-bedrock-agent-id"
export AWS_REGION="us-east-1"

# Redis (for production session storage)
export REDIS_URL="redis://localhost:6379"

# Application settings
export ENVIRONMENT="development"  # or staging, production
export LOG_LEVEL="INFO"
export FRONTEND_URL="http://localhost:3000"
```

## Validation

Validate your configuration before starting:

```python
from shared.config import ConfigurationManager
from pathlib import Path

manager = ConfigurationManager()
result = manager.validate_config("config.yml")

if result.is_valid:
    print("✅ Configuration is valid")
else:
    print("❌ Configuration errors:")
    for error in result.errors:
        print(f"  - {error}")
```
