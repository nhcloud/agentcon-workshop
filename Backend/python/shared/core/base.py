"""Core utilities and base implementations."""

import asyncio
import hashlib
import logging
from datetime import datetime
from typing import Any, Dict, List, Optional, Type
import uuid

from .interfaces import (
    IAgent, IRouter, ISessionManager, IAgentFactory, IConfigManager,
    AgentConfig, AgentMessage, AgentResponse, AgentType, MessageRole,
    AgentSystemException, AgentNotFoundException, SessionException
)


class BaseAgent(IAgent):
    """Base implementation of IAgent with common functionality."""
    
    def __init__(self, config: AgentConfig):
        super().__init__(config)
        self.logger = logging.getLogger(f"agent.{self.name}")
        self._initialized = False
    
    async def initialize(self) -> None:
        """Base initialization."""
        self.logger.info(f"Initializing agent: {self.name}")
        self._initialized = True
    
    async def cleanup(self) -> None:
        """Base cleanup."""
        self.logger.info(f"Cleaning up agent: {self.name}")
        self._initialized = False
    
    @property
    def is_available(self) -> bool:
        """Check if agent is available."""
        return self.enabled and self._initialized
    
    def _create_response(self, content: str, metadata: Optional[Dict[str, Any]] = None) -> AgentResponse:
        """Helper to create a standardized response."""
        return AgentResponse(
            content=content,
            agent_name=self.name,
            metadata=metadata or {}
        )


class InMemorySessionManager(ISessionManager):
    """In-memory implementation of session management."""
    
    def __init__(self):
        self._sessions: Dict[str, Dict[str, Any]] = {}
        self._messages: Dict[str, List[AgentMessage]] = {}
        self.logger = logging.getLogger("session_manager")
    
    async def get_session(self, session_id: str) -> Dict[str, Any]:
        """Get or create a session."""
        if session_id not in self._sessions:
            self._sessions[session_id] = {
                "id": session_id,
                "created_at": datetime.utcnow(),
                "last_activity": datetime.utcnow(),
                "metadata": {},
                "cache": {}
            }
            self._messages[session_id] = []
            self.logger.info(f"Created new session: {session_id}")
        else:
            self._sessions[session_id]["last_activity"] = datetime.utcnow()
        
        return self._sessions[session_id]
    
    async def save_session(self, session_id: str, session_data: Dict[str, Any]) -> None:
        """Save session data."""
        if session_id in self._sessions:
            self._sessions[session_id].update(session_data)
            self._sessions[session_id]["last_activity"] = datetime.utcnow()
    
    async def delete_session(self, session_id: str) -> None:
        """Delete a session."""
        if session_id in self._sessions:
            del self._sessions[session_id]
            del self._messages[session_id]
            self.logger.info(f"Deleted session: {session_id}")
    
    async def add_message(self, session_id: str, message: AgentMessage) -> None:
        """Add a message to session history."""
        await self.get_session(session_id)  # Ensure session exists
        self._messages[session_id].append(message)
    
    async def get_messages(self, session_id: str) -> List[AgentMessage]:
        """Get all messages for a session."""
        await self.get_session(session_id)  # Ensure session exists
        return self._messages[session_id].copy()


class AgentRegistry:
    """Registry for managing agent instances."""
    
    def __init__(self):
        self._agents: Dict[str, IAgent] = {}
        self._factories: List[IAgentFactory] = []
        self.logger = logging.getLogger("agent_registry")
    
    def register_factory(self, factory: IAgentFactory) -> None:
        """Register an agent factory."""
        self._factories.append(factory)
        self.logger.info(f"Registered factory for types: {factory.get_supported_types()}")
    
    async def register_agent(self, config: AgentConfig) -> None:
        """Register an agent with the system."""
        if config.name in self._agents:
            self.logger.warning(f"Agent {config.name} already registered, replacing")
        
        # Find appropriate factory
        factory = None
        for f in self._factories:
            if config.agent_type in f.get_supported_types():
                factory = f
                break
        
        if not factory:
            raise AgentNotFoundException(f"No factory found for agent type: {config.agent_type}")
        
        # Create and initialize agent
        agent = await factory.create_agent(config)
        await agent.initialize()
        
        self._agents[config.name] = agent
        self.logger.info(f"Registered agent: {config.name} (type: {config.agent_type})")
    
    async def unregister_agent(self, agent_name: str) -> None:
        """Unregister an agent."""
        if agent_name in self._agents:
            agent = self._agents[agent_name]
            await agent.cleanup()
            del self._agents[agent_name]
            self.logger.info(f"Unregistered agent: {agent_name}")
    
    def get_agent(self, agent_name: str) -> Optional[IAgent]:
        """Get an agent by name."""
        return self._agents.get(agent_name)
    
    def get_available_agents(self) -> List[str]:
        """Get list of available agent names."""
        return [name for name, agent in self._agents.items() if agent.is_available]
    
    def get_all_agents(self) -> Dict[str, IAgent]:
        """Get all registered agents."""
        return self._agents.copy()


class MessageCache:
    """Simple message response cache."""
    
    def __init__(self, max_size: int = 1000):
        self._cache: Dict[str, AgentResponse] = {}
        self.max_size = max_size
    
    def _generate_key(self, message: str, agent_name: str, session_id: str) -> str:
        """Generate cache key for a message."""
        content = f"{session_id}:{agent_name}:{message.strip().lower()}"
        return hashlib.md5(content.encode()).hexdigest()
    
    def get(self, message: str, agent_name: str, session_id: str) -> Optional[AgentResponse]:
        """Get cached response."""
        key = self._generate_key(message, agent_name, session_id)
        return self._cache.get(key)
    
    def set(self, message: str, agent_name: str, session_id: str, response: AgentResponse) -> None:
        """Cache a response."""
        if len(self._cache) >= self.max_size:
            # Simple eviction: remove oldest entry
            oldest_key = next(iter(self._cache))
            del self._cache[oldest_key]
        
        key = self._generate_key(message, agent_name, session_id)
        self._cache[key] = response
    
    def clear(self) -> None:
        """Clear the cache."""
        self._cache.clear()


def setup_logging(log_level: str = "INFO") -> None:
    """Setup structured logging for the agent system."""
    logging.basicConfig(
        level=getattr(logging, log_level.upper()),
        format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S"
    )


class HealthChecker:
    """Health checking utilities for the agent system."""
    
    def __init__(self, registry: AgentRegistry, session_manager: ISessionManager):
        self.registry = registry
        self.session_manager = session_manager
    
    async def check_system_health(self) -> Dict[str, Any]:
        """Check overall system health."""
        health_status = {
            "status": "healthy",
            "timestamp": datetime.utcnow().isoformat(),
            "agents": {},
            "session_manager": "healthy"
        }
        
        # Check each agent
        agents = self.registry.get_all_agents()
        for name, agent in agents.items():
            try:
                health_status["agents"][name] = {
                    "status": "healthy" if agent.is_available else "unavailable",
                    "type": agent.agent_type.value,
                    "enabled": agent.enabled
                }
            except Exception as e:
                health_status["agents"][name] = {
                    "status": "error",
                    "error": str(e)
                }
                health_status["status"] = "degraded"
        
        # Simple session manager check
        try:
            test_session = await self.session_manager.get_session("health_check")
            await self.session_manager.delete_session("health_check")
        except Exception as e:
            health_status["session_manager"] = f"error: {str(e)}"
            health_status["status"] = "degraded"
        
        return health_status