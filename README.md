# Azure Multi-Agent Workshop 

A comprehensive workshop for building sophisticated AI agent applications using Azure OpenAI services. Choose from **Python** (LangChain & Semantic Kernel) or **.NET** (Semantic Kernel) implementations to learn multi-agent architectures.

## 🚀 Quick Start

Choose your preferred platform and follow the specific setup guide:

### 🐍 **Python Developers**
```bash
cd Backend/python
# See: Backend/python/README.md for framework selection
```
**Frameworks**: LangChain, Semantic Kernel  
**Port**: 8000 (all Python implementations)  
📚 **[Complete Python Setup Guide →](Backend/python/README.md)**

### 🔷 **.NET Developers**  
```bash
cd Backend/dotnet/sk
# See: Backend/dotnet/README.md for setup
```
**Framework**: Semantic Kernel  
**Port**: 8000  
📚 **[Complete .NET Setup Guide →](Backend/dotnet/README.md)**

### 🌐 **Frontend** (All Platforms)
```bash
cd frontend
npm install && npm start
# Runs on http://localhost:3001
# Connects to backend on port 8000
```
📚 **[Frontend Documentation →](frontend/PROFESSIONAL_UI_README.md)**

## 📋 Prerequisites

- **Azure Account** with Azure OpenAI Service access
- **Platform-specific requirements**: See platform README for details
- **Optional**: Azure AI Foundry for enterprise features

## 🏗️ Workshop Architecture

This workshop demonstrates modern multi-agent patterns:

- **🤖 Multi-Agent Systems**: Collaborative AI conversations
- **🔄 Agent Routing**: Intelligent request distribution  
- **💬 Group Chat**: Multi-participant AI discussions
- **🌐 Modern Frontend**: Professional React interface
- **📊 Session Management**: Persistent conversation state
- **🔐 Enterprise Ready**: Azure authentication & security

## 📚 Documentation

- **[🐍 Python Implementation](Backend/python/README.md)** - LangChain & Semantic Kernel
- **[🔷 .NET Implementation](Backend/dotnet/README.md)** - Semantic Kernel  
- **[📖 Installation Guide](docs/INSTALL.md)** - Detailed setup instructions
- **[⚙️ Environment Guide](docs/ENVIRONMENT_GUIDE.md)** - Azure configuration
- **[👥 Group Chat Guide](docs/GROUP_CHAT.md)** - Multi-agent patterns

## 🎯 Learning Path

1. **Choose Platform**: Python or .NET (see platform READMEs)
2. **Follow Setup**: Platform-specific installation guides
3. **Configure Azure**: Environment and credentials setup  
4. **Run Application**: Start your chosen implementation on port 8000
5. **Run Frontend**: Start React frontend on port 3001  
6. **Run Examples**: Interactive notebooks and demos
7. **Build Custom**: Extend with your own agents

**💡 Note**: All backend implementations run on port 8000. The frontend (port 3001) connects to whichever backend you choose to run.

---

**🚀 Ready to start?** Pick your platform above and follow the setup guide!