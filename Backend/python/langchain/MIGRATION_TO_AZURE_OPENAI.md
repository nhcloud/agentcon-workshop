# Migration Guide: Azure AI Inference to Azure OpenAI

This guide helps you migrate from Azure AI Inference endpoints to Azure OpenAI endpoints in the LangChain implementation.

## Overview

The LangChain implementation has been updated to use Azure OpenAI instead of Azure AI Inference endpoints. This provides direct access to OpenAI models through Azure's infrastructure.

## Changes Made

### 1. Dependencies
- Removed: `langchain-azure-ai`
- Using: `langchain-openai` (already present in requirements.txt)

### 2. Code Updates
- Replaced `AzureAIChatCompletionsModel` with `AzureChatOpenAI`
- Updated all agent implementations (generic, group chat)
- Updated router implementation

### 3. Configuration Changes

#### Old Environment Variables (Azure AI Inference):
```bash
AZURE_INFERENCE_ENDPOINT=https://your-endpoint.inference.ai.azure.com
AZURE_INFERENCE_CREDENTIAL=your-credential-key
GENERIC_MODEL=gpt-4o-mini
```

#### New Environment Variables (Azure OpenAI):
```bash
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_API_VERSION=2024-02-01
AZURE_OPENAI_DEPLOYMENT_NAME=your-deployment-name
```

## Migration Steps

### 1. Set Up Azure OpenAI Resource
1. Create an Azure OpenAI resource in Azure Portal
2. Deploy a model (e.g., gpt-4, gpt-4o, gpt-35-turbo)
3. Note down:
   - Resource endpoint (e.g., `https://myresource.openai.azure.com/`)
   - API key
   - Deployment name

### 2. Update Environment Variables

Create or update your `.env` file:

```bash
# Azure OpenAI Configuration
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_API_VERSION=2024-02-01
AZURE_OPENAI_DEPLOYMENT_NAME=your-deployment-name

# Keep existing Azure Foundry configuration if using those agents
PROJECT_ENDPOINT=${PROJECT_ENDPOINT}
PEOPLE_AGENT_ID=${PEOPLE_AGENT_ID}
KNOWLEDGE_AGENT_ID=${KNOWLEDGE_AGENT_ID}
```

### 3. Update Configuration Files

The `config.yml` has been updated to use Azure OpenAI:

```yaml
# Azure OpenAI configuration
azure_openai:
  endpoint: "${AZURE_OPENAI_ENDPOINT}"
  api_key: "${AZURE_OPENAI_API_KEY}"
  api_version: "${AZURE_OPENAI_API_VERSION:2024-02-01}"
  deployment_name: "${AZURE_OPENAI_DEPLOYMENT_NAME:gpt-4o-mini}"
```

### 4. Install Dependencies

No new dependencies are required as `langchain-openai` is already in requirements.txt.

```bash
pip install -r requirements.txt
```

### 5. Test the Migration

Run the example scripts to verify everything works:

```bash
# Test basic functionality
python main.py

# Test group chat
python example_group_chat.py
```

## API Differences

### Temperature and Max Tokens
The Azure OpenAI implementation uses standard OpenAI parameters:
- `temperature`: Controls randomness (0-1)
- `max_tokens`: Maximum response length

These are now hardcoded in the implementations but can be made configurable if needed.

### Model Names
- Azure AI Inference used model names directly (e.g., "gpt-4o-mini")
- Azure OpenAI uses deployment names you create in Azure

## Troubleshooting

### Common Issues

1. **Authentication Error**
   - Ensure your API key is correct
   - Check that the endpoint URL ends with a trailing slash

2. **Model Not Found**
   - Verify your deployment name matches what's in Azure
   - Ensure the model is successfully deployed

3. **API Version Error**
   - Use a supported API version (2024-02-01 is recommended)

### Rollback Plan

If you need to rollback to Azure AI Inference:
1. Revert the code changes
2. Restore the old environment variables
3. Reinstall `langchain-azure-ai` package

## Benefits of Migration

1. **Direct OpenAI Integration**: More stable and feature-complete
2. **Better Documentation**: Azure OpenAI has extensive documentation
3. **Consistent API**: Uses standard OpenAI client patterns
4. **More Features**: Access to all OpenAI features through Azure

## Support

For issues or questions:
1. Check Azure OpenAI documentation
2. Review LangChain OpenAI integration docs
3. Verify your Azure resource configuration