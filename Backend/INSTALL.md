# Installation and Setup Guide

This guide explains how to install and set up the modern AI Agent System.

## Prerequisites

- Python 3.8 or higher
- pip (Python package manager)
- Git (for cloning the repository)

## Quick Installation

### 1. Clone the Repository
```bash
git clone <your-repository-url>
cd AI/Backend
```

### 2. Install the Shared Library
```bash
# Install shared library in development mode
cd shared
pip install -e .

# Or install with all optional dependencies
pip install -e ".[all,dev]"
```

### 3. Choose Your Framework

#### For LangChain Implementation:
```bash
cd python/langchain
pip install -r requirements.txt

# Copy example configuration
cp ../examples/langchain/config.yml config.yml
```

#### For Semantic Kernel Implementation:
```bash
cd python/sk
pip install -r requirements.txt

# Copy example configuration
cp ../examples/sk/config.yml config.yml
```

## Environment Setup

### Required Environment Variables

Create a `.env` file in your chosen implementation directory:

#### LangChain (.env for langchain/)
```bash
# Azure AI Inference
AZURE_INFERENCE_ENDPOINT=https://your-inference-endpoint.com
AZURE_INFERENCE_CREDENTIAL=your-credential-key

# Azure AI Foundry (optional)
PROJECT_ENDPOINT=https://your-project-endpoint.com

# Application settings
ENVIRONMENT=development
LOG_LEVEL=INFO
FRONTEND_URL=http://localhost:3000
```

#### Semantic Kernel (.env for python_semantic_kernel/)
```bash
# Azure OpenAI (required)
AZURE_OPENAI_ENDPOINT=https://your-openai.openai.azure.com
AZURE_OPENAI_DEPLOYMENT=gpt-4o
AZURE_OPENAI_KEY=your-openai-key

# Azure AI Foundry (optional)
PROJECT_ENDPOINT=https://your-project-endpoint.com

# Google Gemini (optional)
GOOGLE_API_KEY=your-gemini-key

# AWS Bedrock (optional)
AWS_BEDROCK_AGENT_ID=your-bedrock-agent-id
AWS_REGION=us-east-1

# Application settings
ENVIRONMENT=development
LOG_LEVEL=INFO
FRONTEND_URL=http://localhost:3000
```

### Optional Environment Variables
```bash
# Session storage
SESSION_STORAGE_TYPE=file  # or redis, memory
REDIS_URL=redis://localhost:6379

# Performance
WORKERS=1
```

## Configuration

### 1. Basic Configuration
Edit your `config.yml` file to customize agents:

```yaml
app:
  title: "My AI Agent System"
  version: "2.0.0"

agents:
  my_assistant:
    type: "generic"
    enabled: true
    instructions: "You are a helpful AI assistant..."
    framework_config:
      provider: "azure_openai"
      model: "gpt-4o"
      temperature: 0.7
```

### 2. Validate Configuration
```bash
python -c "
from shared.config import ConfigurationManager
from pathlib import Path

manager = ConfigurationManager()
result = manager.validate_config('config.yml')
print('✅ Valid' if result.is_valid else '❌ Invalid')
for error in result.errors:
    print(f'Error: {error}')
"
```

## Running the Application

### Development Mode
```bash
# For LangChain
cd langchain
python main.py

# For Semantic Kernel
cd semantic_kernel
python main.py
```

### Production Mode
```bash
# Install additional production dependencies
pip install gunicorn

# Run with Gunicorn
gunicorn main:app -w 4 -k uvicorn.workers.UvicornWorker --bind 0.0.0.0:8000
```

## Docker Setup (Optional)

### 1. Create Dockerfile
Create a `Dockerfile` in your implementation directory:

```dockerfile
FROM python:3.11-slim

WORKDIR /app

# Install shared library
COPY shared/ ./shared/
RUN cd shared && pip install -e .

# Install framework dependencies
COPY langchain/requirements.txt ./requirements.txt
RUN pip install -r requirements.txt

# Copy application
COPY langchain/ ./
COPY python/examples/langchain/config.yml ./config.yml

EXPOSE 8000

CMD ["python", "main.py"]
```

### 2. Build and Run
```bash
docker build -t ai-agent-system .
docker run -p 8000:8000 --env-file .env ai-agent-system
```

## Verification

### 1. Health Check
```bash
curl http://localhost:8000/health
```

### 2. List Agents
```bash
curl http://localhost:8000/agents
```

### 3. Test Chat
```bash
curl -X POST "http://localhost:8000/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Hello, how are you?",
    "agent": "general_assistant"
  }'
```

## Troubleshooting

### Common Issues

#### 1. Import Errors
```bash
# Ensure shared library is installed
cd shared
pip install -e .
```

#### 2. Configuration Errors
```bash
# Check environment variables
echo $AZURE_OPENAI_ENDPOINT
echo $AZURE_OPENAI_KEY

# Validate configuration
python -c "from shared.config import load_and_validate_config; print(load_and_validate_config('config.yml'))"
```

#### 3. Missing Dependencies
```bash
# Reinstall all dependencies
pip install -r requirements.txt
```

#### 4. Port Already in Use
```bash
# Change port in config.yml or environment
export PORT=8001
```

### Debug Mode
Enable debug logging:
```bash
export LOG_LEVEL=DEBUG
export DEBUG=1
python main.py
```

### Performance Issues
Monitor system resources:
```bash
# Check system stats
curl http://localhost:8000/system/stats

# Monitor logs
tail -f logs/app.log
```

## Development Setup

### 1. Install Development Dependencies
```bash
cd shared
pip install -e ".[dev]"

cd ../langchain  # or semantic_kernel
pip install -r requirements.txt
```

### 2. Setup Pre-commit Hooks
```bash
pre-commit install
```

### 3. Run Tests
```bash
pytest tests/
```

### 4. Format Code
```bash
black .
isort .
mypy .
```

## Updating

### 1. Update Dependencies
```bash
pip install --upgrade -r requirements.txt
```

### 2. Update Configuration
Compare your `config.yml` with the latest examples in `python/examples/` or `dotnet/examples/` depending on your platform.

### 3. Migration Guide
Check the CHANGELOG.md for breaking changes and migration instructions.

## Next Steps

- [Adding Custom Agents](docs/adding-agents.md)
- [Configuration Reference](docs/configuration.md)
- [API Documentation](docs/api.md)
- [Deployment Guide](docs/deployment.md)
- [Contributing](CONTRIBUTING.md)
