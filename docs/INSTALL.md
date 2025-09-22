# Installation Guide (Python)

This guide covers Python platform installation for the AI Agents Workshop.

> **📌 Note**: For .NET installation and setup, see [.NET Backend README](../Backend/dotnet/sk/README.md)

## 🛠️ Prerequisites

Before you begin, ensure you have:
- **Python 3.8+** installed on your system
- **Git** for cloning the repository
- **Azure OpenAI** or **OpenAI** API access

## 📥 Installation Steps

### 1. Clone the Repository

```bash
git clone <repository-url>
cd agentcon-workshop
```

### 2. Choose Your Python Implementation

Navigate to your preferred Python backend:

```bash
# For LangChain implementation
cd Backend/python/langchain

# OR for Semantic Kernel implementation  
cd Backend/python/sk
```

### 3. Create Virtual Environment

```bash
# Create virtual environment
python -m venv venv

# Activate virtual environment
# On Windows:
venv\Scripts\activate

# On macOS/Linux:
source venv/bin/activate
```

### 4. Install Dependencies

```bash
pip install -r requirements.txt
```

### 5. Environment Configuration

1. Copy the environment template:
   ```bash
   # From Backend/python directory
   cp env.template .env
   ```

2. Configure your `.env` file with your API credentials. See [ENVIRONMENT_GUIDE.md](ENVIRONMENT_GUIDE.md) for detailed configuration instructions.

## 🚀 Quick Verification

Test your installation:

```bash
# Run the validation script
python validate_env.py

# Test basic functionality
python example_template_usage.py
```

## 📂 Project Structure (Python)

```
Backend/python/
├── langchain/           # LangChain implementation
│   ├── main.py         # Main application entry point
│   ├── requirements.txt # Python dependencies
│   └── agents/         # Agent implementations
├── sk/                 # Semantic Kernel implementation
│   ├── main.py         # Main application entry point
│   ├── requirements.txt # Python dependencies
│   └── agents/         # Agent implementations
└── shared/             # Shared utilities and components
```

## 🔧 Alternative Platforms

- **[.NET Implementation](../Backend/dotnet/sk/README.md)** - Complete setup guide for .NET Semantic Kernel
- **[Frontend Setup](../frontend/)** - React frontend installation

## 🐛 Troubleshooting

### Common Issues

1. **Python Version**: Ensure you're using Python 3.8 or higher
2. **Virtual Environment**: Always activate your virtual environment before installing packages
3. **Dependencies**: If installation fails, try upgrading pip: `python -m pip install --upgrade pip`

### Getting Help

- Check the [Environment Guide](ENVIRONMENT_GUIDE.md) for configuration issues
- Review platform-specific README files for detailed setup
- Ensure all API credentials are properly configured in your `.env` file

## ✅ Next Steps

After installation:
1. **[Configure Environment](ENVIRONMENT_GUIDE.md)** - Set up your API credentials
2. **[Explore Group Chat](GROUP_CHAT.md)** - Learn about multi-agent conversations
3. **[Choose Your Framework](../README.md#quick-start)** - Python or .NET implementation guides
