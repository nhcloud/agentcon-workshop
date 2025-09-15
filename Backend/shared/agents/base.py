"""Base agent implementations for different frameworks."""

from abc import ABC, abstractmethod
from typing import Any, Dict, List, Optional, Type
import logging

from ..core import BaseAgent, AgentConfig, AgentMessage, AgentResponse, AgentType, IAgentFactory


class GenericAgent(BaseAgent):
    """Generic conversational agent that can be extended."""
    
    async def process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Process a generic message."""
        # This is a placeholder - subclasses should implement actual AI processing
        response_content = f"I received your message: {message}"
        
        if history:
            response_content += f" (with {len(history)} previous messages)"
        
        return self._create_response(response_content, metadata)


class PeopleLookupAgent(BaseAgent):
    """Agent specialized for looking up people information."""
    
    async def process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Process people lookup requests."""
        # Placeholder implementation
        response_content = f"Looking up people information for: {message}"
        return self._create_response(response_content, metadata)
    
    def get_capabilities(self) -> List[str]:
        """Return capabilities of the people lookup agent."""
        return [
            "employee_search",
            "contact_information",
            "organizational_hierarchy",
            "team_membership"
        ]


class KnowledgeFinderAgent(BaseAgent):
    """Agent specialized for finding knowledge and documentation."""
    
    async def process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Process knowledge search requests."""
        # Placeholder implementation
        response_content = f"Searching knowledge base for: {message}"
        return self._create_response(response_content, metadata)
    
    def get_capabilities(self) -> List[str]:
        """Return capabilities of the knowledge finder agent."""
        return [
            "documentation_search",
            "policy_lookup",
            "procedure_guidance",
            "technical_support"
        ]


class BaseAgentFactory(IAgentFactory):
    """Base factory for creating agents."""
    
    def __init__(self):
        self.agent_classes: Dict[AgentType, Type[BaseAgent]] = {
            AgentType.GENERIC: GenericAgent,
            AgentType.PEOPLE_LOOKUP: PeopleLookupAgent,
            AgentType.KNOWLEDGE_FINDER: KnowledgeFinderAgent
        }
    
    async def create_agent(self, config: AgentConfig) -> BaseAgent:
        """Create an agent based on configuration."""
        agent_class = self.agent_classes.get(config.agent_type)
        if not agent_class:
            raise ValueError(f"Unsupported agent type: {config.agent_type}")
        
        agent = agent_class(config)
        await agent.initialize()
        return agent
    
    def get_supported_types(self) -> List[AgentType]:
        """Get supported agent types."""
        return list(self.agent_classes.keys())
    
    def register_agent_type(self, agent_type: AgentType, agent_class: Type[BaseAgent]) -> None:
        """Register a new agent type."""
        self.agent_classes[agent_type] = agent_class


class AgentPlugin:
    """Base class for agent plugins that can extend functionality."""
    
    def __init__(self, name: str):
        self.name = name
        self.enabled = True
    
    async def initialize(self) -> None:
        """Initialize the plugin."""
        pass
    
    async def cleanup(self) -> None:
        """Cleanup plugin resources."""
        pass
    
    async def pre_process(self, message: str, metadata: Dict[str, Any]) -> Dict[str, Any]:
        """Pre-process message before agent handles it."""
        return metadata
    
    async def post_process(self, response: AgentResponse, metadata: Dict[str, Any]) -> AgentResponse:
        """Post-process agent response."""
        return response


class PluginManager:
    """Manager for agent plugins."""
    
    def __init__(self):
        self.plugins: Dict[str, AgentPlugin] = {}
        self.logger = logging.getLogger("plugin_manager")
    
    async def register_plugin(self, plugin: AgentPlugin) -> None:
        """Register a plugin."""
        await plugin.initialize()
        self.plugins[plugin.name] = plugin
        self.logger.info(f"Registered plugin: {plugin.name}")
    
    async def unregister_plugin(self, plugin_name: str) -> None:
        """Unregister a plugin."""
        if plugin_name in self.plugins:
            plugin = self.plugins[plugin_name]
            await plugin.cleanup()
            del self.plugins[plugin_name]
            self.logger.info(f"Unregistered plugin: {plugin_name}")
    
    async def pre_process_message(self, message: str, metadata: Dict[str, Any]) -> Dict[str, Any]:
        """Apply all pre-processing plugins."""
        for plugin in self.plugins.values():
            if plugin.enabled:
                metadata = await plugin.pre_process(message, metadata)
        return metadata
    
    async def post_process_response(self, response: AgentResponse, metadata: Dict[str, Any]) -> AgentResponse:
        """Apply all post-processing plugins."""
        for plugin in self.plugins.values():
            if plugin.enabled:
                response = await plugin.post_process(response, metadata)
        return response


class EnhancedAgent(BaseAgent):
    """Enhanced agent with plugin support."""
    
    def __init__(self, config: AgentConfig, plugin_manager: Optional[PluginManager] = None):
        super().__init__(config)
        self.plugin_manager = plugin_manager or PluginManager()
    
    async def process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Process message with plugin support."""
        metadata = metadata or {}
        
        # Pre-process with plugins
        metadata = await self.plugin_manager.pre_process_message(message, metadata)
        
        # Core processing (to be implemented by subclasses)
        response = await self._core_process_message(message, history, metadata)
        
        # Post-process with plugins
        response = await self.plugin_manager.post_process_response(response, metadata)
        
        return response
    
    async def _core_process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Core message processing to be implemented by subclasses."""
        return self._create_response(f"Enhanced agent processed: {message}", metadata)