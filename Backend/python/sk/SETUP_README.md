# Workshop Environment Setup Guide

## Quick Start (Recommended for Attendees)

### Option 1: Setup Everything from the Notebook (Easiest!)

1. **Open the notebook**: `workshop_semantic_kernel_agents.ipynb`
2. **Run the first cell** (Section 0: Environment Setup)
   - This will automatically install all required packages
   - It will also check for environment variables
3. **Continue with the workshop** - that's it!

### Option 2: Manual Setup (If you prefer command line)

1. **Install requirements**:
   ```bash
   pip install -r requirements.txt
   ```

2. **Setup environment variables**:
   - Copy `.env.template` to `.env`
   - Fill in your Azure credentials

3. **Open the notebook and start from Section 1**

## Environment Variables Needed

### Required (for real Azure services):
- `AZURE_OPENAI_API_KEY` - Your Azure OpenAI API key
- `AZURE_OPENAI_ENDPOINT` - Your Azure OpenAI endpoint URL
- `AZURE_OPENAI_DEPLOYMENT_NAME` - Your model deployment name (e.g., "gpt-4")

### Optional (for enterprise features):
- `AZURE_AI_FOUNDRY_ENDPOINT` - Azure AI Foundry endpoint
- `AZURE_AI_FOUNDRY_API_KEY` - Azure AI Foundry API key
- `AZURE_SUBSCRIPTION_ID` - Your Azure subscription ID
- `AZURE_RESOURCE_GROUP` - Your resource group name
- `AZURE_AI_PROJECT_NAME` - Your AI project name

## No Azure Credentials? No Problem!

The workshop includes **mock implementations** so you can:
- ✅ Learn all the concepts
- ✅ See the code structure and patterns
- ✅ Understand the progression from basic to enterprise agents
- ✅ Get hands-on experience with Semantic Kernel

You'll see simulated responses that demonstrate what would happen with real Azure services.

## Troubleshooting

### Import Errors
- The notebook automatically handles missing packages
- Mock implementations are used when services aren't available
- You'll still learn all the key concepts!

### Package Installation Issues
- Make sure you're using Python 3.8+
- Try upgrading pip: `pip install --upgrade pip`
- On Windows, you might need: `pip install --upgrade setuptools wheel`

### Permission Issues
- Use `pip install --user -r requirements.txt` if you get permission errors
- Or use a virtual environment (recommended)

## What You'll Learn

1. **Basic Semantic Kernel Agents** - Core concepts and patterns
2. **Azure Integration** - Multi-provider support and Azure services
3. **Advanced Features** - Plugins, memory, and context management
4. **Enterprise Deployment** - Azure AI Foundry for production

## Support

If you run into issues:
1. Check that the first notebook cell ran successfully
2. Look for any error messages in the output
3. The workshop will adapt to your environment automatically
4. Focus on learning the patterns and concepts!

Happy learning! 🚀