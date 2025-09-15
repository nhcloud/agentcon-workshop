"""
Setup configuration for the AI Agent Shared Library.
"""
from setuptools import setup, find_packages
from pathlib import Path

# Read the contents of README file
this_directory = Path(__file__).parent
long_description = (this_directory / "README.md").read_text() if (this_directory / "README.md").exists() else ""

# Read requirements
def read_requirements(filename):
    """Read requirements from file."""
    requirements_path = this_directory / filename
    if requirements_path.exists():
        with open(requirements_path, 'r') as f:
            return [line.strip() for line in f.readlines() 
                   if line.strip() and not line.startswith('#') and not line.startswith('-e')]
    return []

setup(
    name="ai-agent-shared",
    version="2.0.0",
    description="Shared library for modern AI agent systems",
    long_description=long_description,
    long_description_content_type="text/markdown",
    author="Your Organization",
    author_email="your-email@example.com",
    url="https://github.com/your-org/ai-agent-system",
    
    packages=find_packages(),
    include_package_data=True,
    
    python_requires=">=3.8",
    
    install_requires=[
        "pydantic>=2.4.0",
        "python-dotenv>=1.0.0",
        "typing-extensions>=4.8.0",
        "PyYAML>=6.0",
        "pydantic-settings>=2.0.0",
        "aiofiles>=23.0.0",
    ],
    
    extras_require={
        "redis": ["redis>=4.5.0"],
        "database": ["sqlalchemy>=2.0.0"],
        "all": ["redis>=4.5.0", "sqlalchemy>=2.0.0"],
        "dev": [
            "pytest>=7.4.0",
            "pytest-asyncio>=0.21.0",
            "pytest-cov>=4.1.0",
            "black>=23.0.0",
            "isort>=5.12.0",
            "mypy>=1.6.0",
            "pre-commit>=3.0.0",
        ],
        "docs": [
            "sphinx>=7.0.0",
            "sphinx-rtd-theme>=1.3.0",
        ]
    },
    
    classifiers=[
        "Development Status :: 4 - Beta",
        "Intended Audience :: Developers",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
        "Topic :: Software Development :: Libraries :: Python Modules",
        "Topic :: Scientific/Engineering :: Artificial Intelligence",
    ],
    
    keywords=[
        "ai", "agents", "llm", "langchain", "semantic-kernel", 
        "azure", "openai", "chatbots", "conversational-ai"
    ],
    
    project_urls={
        "Bug Reports": "https://github.com/your-org/ai-agent-system/issues",
        "Source": "https://github.com/your-org/ai-agent-system",
        "Documentation": "https://your-org.github.io/ai-agent-system/",
    },
)