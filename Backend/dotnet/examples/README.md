# .NET Configuration Examples

This directory contains example configurations for .NET-based AI agent implementations using Semantic Kernel.

## Directory Structure

```
dotnet/examples/
├── README.md                      # This file
├── appsettings.json               # Default configuration
├── appsettings.Development.json   # Development environment settings
├── appsettings.Production.json    # Production environment settings
└── appsettings.Minimal.json       # Minimal configuration for testing
```

## Quick Start

### Copy Configuration Files

```bash
# Copy the default configuration
cp backend/dotnet/examples/appsettings.json backend/dotnet/sk/appsettings.json

# Copy development configuration (optional)
cp backend/dotnet/examples/appsettings.Development.json backend/dotnet/sk/appsettings.Development.json

# Edit the configuration files
notepad backend/dotnet/sk/appsettings.json
```

### Set Environment Variables

Create a `.env` file or set environment variables:

```bash
# Required for Azure OpenAI
$env:AZURE_OPENAI_ENDPOINT="https://your-openai.openai.azure.com"
$env:AZURE_OPENAI_API_KEY="your-openai-key"
$env:AZURE_OPENAI_DEPLOYMENT="gpt-4o"

# Optional for Azure AI Inference
$env:AZURE_AI_INFERENCE_ENDPOINT="https://your-inference-endpoint.com"
$env:AZURE_AI_INFERENCE_API_KEY="your-inference-key"
```

## Configuration Files

### Environment-Specific
- `appsettings.json` - Default configuration with comprehensive agent setup
- `appsettings.Development.json` - Development settings with debug logging and detailed errors
- `appsettings.Production.json` - Production configuration with security, monitoring, and performance optimizations
- `appsettings.Minimal.json` - Minimal configuration for quick testing

## Running the Application

```bash
cd backend/dotnet/sk
dotnet run
```

Or with specific environment:

```bash
cd backend/dotnet/sk
dotnet run --environment Development
dotnet run --environment Production
```

## Configuration Schema

All .NET configurations use JSON format with the following structure:

### Basic Structure
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
      "Endpoint": "${AZURE_OPENAI_ENDPOINT}",
      "ApiKey": "${AZURE_OPENAI_API_KEY}",
      "ChatDeployment": "gpt-4o",
      "EmbeddingDeployment": "text-embedding-3-small",
      "ApiVersion": "2024-08-01-preview"
    }
  },
  "AgentConfiguration": {
    "AgentName": {
      "Name": "Agent Display Name",
      "Instructions": "Agent system instructions",
      "Description": "Agent description",
      "Provider": "AzureOpenAI",
      "Model": "gpt-4o",
      "Temperature": 0.7,
      "MaxTokens": 1000
    }
  }
}
```

### Environment Variable Substitution

Use `${VARIABLE_NAME}` or `${VARIABLE_NAME:default_value}` syntax for environment variable substitution:

```json
{
  "AzureAI": {
    "AzureOpenAI": {
      "Endpoint": "${AZURE_OPENAI_ENDPOINT}",
      "ApiKey": "${AZURE_OPENAI_API_KEY}",
      "ChatDeployment": "${AZURE_OPENAI_DEPLOYMENT:gpt-4o}"
    }
  }
}
```

## Agent Configuration

Define multiple agents in the `AgentConfiguration` section:

```json
{
  "AgentConfiguration": {
    "GeneralAgent": {
      "Name": "GeneralAssistant",
      "Instructions": "You are a helpful AI assistant...",
      "Description": "General purpose assistant",
      "Provider": "AzureOpenAI",
      "Model": "gpt-4o",
      "Temperature": 0.7,
      "MaxTokens": 1000
    },
    "TechnicalAgent": {
      "Name": "TechnicalExpert",
      "Instructions": "You are a technical expert...",
      "Description": "Technical specialist",
      "Provider": "AzureOpenAI", 
      "Model": "gpt-4o",
      "Temperature": 0.3,
      "MaxTokens": 2000
    }
  }
}
```

## Environment-Specific Features

### Development
- Debug logging enabled
- Detailed error pages
- CORS enabled for local development
- Swagger UI enabled

### Production
- Optimized logging levels
- Security headers
- Application Insights integration
- Health checks
- Rate limiting

## Required Environment Variables

### Azure OpenAI
```bash
AZURE_OPENAI_ENDPOINT=https://your-openai.openai.azure.com
AZURE_OPENAI_API_KEY=your-openai-key
AZURE_OPENAI_DEPLOYMENT=gpt-4o
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-3-small
AZURE_OPENAI_API_VERSION=2024-08-01-preview
```

### Azure AI Inference (Optional)
```bash
AZURE_AI_INFERENCE_ENDPOINT=https://your-inference-endpoint.com
AZURE_AI_INFERENCE_API_KEY=your-inference-key
AZURE_AI_INFERENCE_MODEL=gpt-4.1
```

### Production (Additional)
```bash
FRONTEND_URL=https://your-frontend-domain.com
APPLICATIONINSIGHTS_INSTRUMENTATION_KEY=your-app-insights-key
APPLICATIONINSIGHTS_CONNECTION_STRING=your-app-insights-connection-string
```

## See Also

- [Semantic Kernel Documentation](../sk/README.md)
- [Python Examples](../../python/examples/)
- [Microsoft Semantic Kernel Documentation](https://learn.microsoft.com/semantic-kernel/)