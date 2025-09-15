"""Agents module exports."""

from .base import (
    GenericAgent, PeopleLookupAgent, KnowledgeFinderAgent, 
    BaseAgentFactory, AgentPlugin, PluginManager, EnhancedAgent
)

__all__ = [
    "GenericAgent",
    "PeopleLookupAgent", 
    "KnowledgeFinderAgent",
    "BaseAgentFactory",
    "AgentPlugin",
    "PluginManager",
    "EnhancedAgent"
]