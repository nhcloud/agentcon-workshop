"""Shared components for AI Agent System."""

from .core import *
from .config import *
from .routers import *
from .sessions import *
from .agents import *

__version__ = "1.0.0"

__all__ = [
    # Core interfaces and types
    "IAgent", "IRouter", "ISessionManager", "IAgentFactory", "IConfigManager",
    "AgentConfig", "AgentMessage", "AgentResponse", "AgentType", "MessageRole",
    "RoutingDecision", "AgentSystemException", "AgentNotFoundException",
    "AgentInitializationException", "RoutingException", "SessionException",
    
    # Base implementations
    "BaseAgent", "InMemorySessionManager", "AgentRegistry", "MessageCache",
    "setup_logging", "HealthChecker",
    
    # Configuration management
    "YamlConfigManager", "EnvironmentConfigManager", "ConfigFactory", "DEFAULT_AGENT_CONFIGS",
    
    # Routers
    "SimpleKeywordRouter", "PatternRouter", "HistoryAwareRouter", "RoundRobinRouter", "RouterFactory",
    
    # Session management
    "PersistentSessionManager", "RedisSessionManager", "SessionManagerFactory",
    
    # Agents
    "GenericAgent", "PeopleLookupAgent", "KnowledgeFinderAgent", 
    "BaseAgentFactory", "AgentPlugin", "PluginManager", "EnhancedAgent"
]