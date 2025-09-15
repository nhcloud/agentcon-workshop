"""Core interfaces and abstract base classes for the AI Agent System."""

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Dict, List, Optional, Union, AsyncGenerator
import uuid
from datetime import datetime


class AgentType(Enum):
    """Types of agents supported by the system."""
    GENERIC = "generic"
    PEOPLE_LOOKUP = "people_lookup"
    KNOWLEDGE_FINDER = "knowledge_finder"
    ROUTER = "router"
    CUSTOM = "custom"


class MessageRole(Enum):
    """Roles for messages in conversations."""
    USER = "user"
    ASSISTANT = "assistant"
    SYSTEM = "system"


@dataclass
class AgentMessage:
    """Standard message format across all agent implementations."""
    id: str = field(default_factory=lambda: str(uuid.uuid4()))
    role: MessageRole = MessageRole.USER
    content: str = ""
    agent_name: Optional[str] = None
    metadata: Dict[str, Any] = field(default_factory=dict)
    timestamp: datetime = field(default_factory=datetime.utcnow)
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert message to dictionary format."""
        return {
            "id": self.id,
            "role": self.role.value,
            "content": self.content,
            "agent_name": self.agent_name,
            "metadata": self.metadata,
            "timestamp": self.timestamp.isoformat()
        }


@dataclass
class AgentResponse:
    """Standard response format from agents."""
    content: str
    agent_name: str
    usage: Optional[Dict[str, Any]] = None
    metadata: Dict[str, Any] = field(default_factory=dict)
    session_id: Optional[str] = None
    message_id: str = field(default_factory=lambda: str(uuid.uuid4()))
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert response to dictionary format."""
        return {
            "content": self.content,
            "agent_name": self.agent_name,
            "usage": self.usage,
            "metadata": self.metadata,
            "session_id": self.session_id,
            "message_id": self.message_id
        }


@dataclass
class AgentConfig:
    """Configuration for an agent."""
    name: str
    agent_type: AgentType
    instructions: str = ""
    enabled: bool = True
    metadata: Dict[str, Any] = field(default_factory=dict)
    
    # Framework-specific configurations
    framework_config: Dict[str, Any] = field(default_factory=dict)


class IAgent(ABC):
    """Abstract base class for all agents."""
    
    def __init__(self, config: AgentConfig):
        self.config = config
        self.name = config.name
        self.agent_type = config.agent_type
        self.enabled = config.enabled
    
    @abstractmethod
    async def process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Process a user message and return a response."""
        pass
    
    @abstractmethod
    async def initialize(self) -> None:
        """Initialize the agent. Called once when the agent is created."""
        pass
    
    @abstractmethod
    async def cleanup(self) -> None:
        """Cleanup resources when the agent is destroyed.""" 
        pass
    
    @property
    def is_available(self) -> bool:
        """Check if the agent is available for processing."""
        return self.enabled
    
    def get_capabilities(self) -> List[str]:
        """Return a list of capabilities this agent provides."""
        return []


class IRouter(ABC):
    """Abstract base class for message routers."""
    
    @abstractmethod
    async def route_message(
        self, 
        message: str, 
        available_agents: List[str],
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> str:
        """Route a message to the appropriate agent."""
        pass


class ISessionManager(ABC):
    """Abstract base class for session management."""
    
    @abstractmethod
    async def get_session(self, session_id: str) -> Dict[str, Any]:
        """Get or create a session."""
        pass
    
    @abstractmethod
    async def save_session(self, session_id: str, session_data: Dict[str, Any]) -> None:
        """Save session data."""
        pass
    
    @abstractmethod
    async def delete_session(self, session_id: str) -> None:
        """Delete a session."""
        pass
    
    @abstractmethod
    async def add_message(self, session_id: str, message: AgentMessage) -> None:
        """Add a message to the session history."""
        pass
    
    @abstractmethod
    async def get_messages(self, session_id: str) -> List[AgentMessage]:
        """Get all messages for a session."""
        pass


class IAgentFactory(ABC):
    """Abstract factory for creating agents."""
    
    @abstractmethod
    async def create_agent(self, config: AgentConfig) -> IAgent:
        """Create an agent based on the configuration."""
        pass
    
    @abstractmethod
    def get_supported_types(self) -> List[AgentType]:
        """Get the list of agent types this factory can create."""
        pass


class IConfigManager(ABC):
    """Abstract base class for configuration management."""
    
    @abstractmethod
    def get_agent_configs(self) -> List[AgentConfig]:
        """Get all agent configurations."""
        pass
    
    @abstractmethod
    def get_agent_config(self, agent_name: str) -> Optional[AgentConfig]:
        """Get configuration for a specific agent.""" 
        pass
    
    @abstractmethod
    def get_system_config(self) -> Dict[str, Any]:
        """Get system-wide configuration."""
        pass
    
    @abstractmethod
    def reload_config(self) -> None:
        """Reload configuration from source."""
        pass


@dataclass
class RoutingDecision:
    """Result of routing a message."""
    agent_name: str
    confidence: float = 1.0
    reasoning: Optional[str] = None
    metadata: Dict[str, Any] = field(default_factory=dict)


class AgentSystemException(Exception):
    """Base exception for the agent system."""
    pass


class AgentNotFoundException(AgentSystemException):
    """Exception raised when an agent is not found."""
    pass


class AgentInitializationException(AgentSystemException):
    """Exception raised when agent initialization fails."""
    pass


class RoutingException(AgentSystemException):
    """Exception raised when message routing fails."""
    pass


class SessionException(AgentSystemException):
    """Exception raised when session operations fail."""
    pass