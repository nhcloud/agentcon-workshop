"""Router implementations for message routing."""

import re
from typing import Dict, List, Optional, Any
from ..core import IRouter, AgentMessage, RoutingDecision, RoutingException


class SimpleKeywordRouter(IRouter):
    """Simple keyword-based router."""
    
    def __init__(self, routing_rules: Optional[Dict[str, List[str]]] = None):
        self.routing_rules = routing_rules or {
            "people_lookup": ["who is", "find person", "employee", "contact", "manager", "team member"],
            "knowledge_finder": ["documentation", "policy", "how to", "akumina", "procedure", "guide"],
            "generic": []  # fallback
        }
    
    async def route_message(
        self, 
        message: str, 
        available_agents: List[str],
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> str:
        """Route message based on keyword matching."""
        message_lower = message.lower()
        
        # Score each available agent
        scores = {}
        for agent_name in available_agents:
            if agent_name in self.routing_rules:
                keywords = self.routing_rules[agent_name]
                score = sum(1 for keyword in keywords if keyword.lower() in message_lower)
                scores[agent_name] = score
        
        # Return agent with highest score, or generic as fallback
        if scores:
            best_agent = max(scores.keys(), key=lambda x: scores[x])
            if scores[best_agent] > 0:
                return best_agent
        
        # Fallback to generic or first available agent
        if "generic_agent" in available_agents:
            return "generic_agent"
        elif "generic" in available_agents:
            return "generic"
        elif available_agents:
            return available_agents[0]
        
        raise RoutingException("No available agents for routing")


class PatternRouter(IRouter):
    """Pattern-based router using regular expressions."""
    
    def __init__(self, routing_patterns: Optional[Dict[str, List[str]]] = None):
        self.routing_patterns = routing_patterns or {
            "people_lookup": [
                r"\b(who is|find|lookup|contact)\b.*\b(person|employee|user|staff)\b",
                r"\b(manager|supervisor|lead|director)\b",
                r"\bemail.*@\b",
                r"\bphone.*number\b"
            ],
            "knowledge_finder": [
                r"\b(how to|procedure|process|guide|documentation)\b",
                r"\b(policy|guideline|standard|requirement)\b",
                r"\bakumina\b",
                r"\b(setup|configuration|install|deploy)\b"
            ]
        }
        
        # Compile patterns for efficiency
        self.compiled_patterns = {}
        for agent, patterns in self.routing_patterns.items():
            self.compiled_patterns[agent] = [re.compile(pattern, re.IGNORECASE) for pattern in patterns]
    
    async def route_message(
        self, 
        message: str, 
        available_agents: List[str],
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> str:
        """Route message based on pattern matching."""
        
        # Score each available agent based on pattern matches
        scores = {}
        for agent_name in available_agents:
            if agent_name in self.compiled_patterns:
                patterns = self.compiled_patterns[agent_name]
                score = sum(1 for pattern in patterns if pattern.search(message))
                scores[agent_name] = score
        
        # Return agent with highest score
        if scores:
            best_agent = max(scores.keys(), key=lambda x: scores[x])
            if scores[best_agent] > 0:
                return best_agent
        
        # Fallback logic
        return self._get_fallback_agent(available_agents)
    
    def _get_fallback_agent(self, available_agents: List[str]) -> str:
        """Get fallback agent when no patterns match."""
        fallback_order = ["generic_agent", "generic"]
        
        for fallback in fallback_order:
            if fallback in available_agents:
                return fallback
        
        if available_agents:
            return available_agents[0]
        
        raise RoutingException("No available agents for routing")


class HistoryAwareRouter(IRouter):
    """Router that considers conversation history for context."""
    
    def __init__(self, base_router: IRouter, history_weight: float = 0.3):
        self.base_router = base_router
        self.history_weight = history_weight
    
    async def route_message(
        self, 
        message: str, 
        available_agents: List[str],
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> str:
        """Route message considering conversation history."""
        
        # Get base routing decision
        base_agent = await self.base_router.route_message(message, available_agents, history, metadata)
        
        # If no history, return base decision
        if not history or len(history) < 2:
            return base_agent
        
        # Analyze recent history for agent patterns
        recent_messages = history[-5:]  # Look at last 5 messages
        agent_frequency = {}
        
        for msg in recent_messages:
            if msg.agent_name and msg.agent_name in available_agents:
                agent_frequency[msg.agent_name] = agent_frequency.get(msg.agent_name, 0) + 1
        
        # If the conversation has been dominated by one agent, consider continuing with it
        if agent_frequency:
            most_frequent_agent = max(agent_frequency.keys(), key=lambda x: agent_frequency[x])
            frequency_ratio = agent_frequency[most_frequent_agent] / len(recent_messages)
            
            # If one agent dominates and it's available, consider staying with it
            if frequency_ratio > 0.6 and most_frequent_agent in available_agents:
                # Use weighted decision between base router and history
                if self.history_weight > 0.5:
                    return most_frequent_agent
        
        return base_agent


class RoundRobinRouter(IRouter):
    """Simple round-robin router for testing purposes."""
    
    def __init__(self):
        self.last_agent_index = -1
    
    async def route_message(
        self, 
        message: str, 
        available_agents: List[str],
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> str:
        """Route message in round-robin fashion."""
        if not available_agents:
            raise RoutingException("No available agents for routing")
        
        self.last_agent_index = (self.last_agent_index + 1) % len(available_agents)
        return available_agents[self.last_agent_index]


class RouterFactory:
    """Factory for creating routers."""
    
    @staticmethod
    def create_keyword_router(routing_rules: Optional[Dict[str, List[str]]] = None) -> SimpleKeywordRouter:
        """Create a keyword-based router."""
        return SimpleKeywordRouter(routing_rules)
    
    @staticmethod
    def create_pattern_router(routing_patterns: Optional[Dict[str, List[str]]] = None) -> PatternRouter:
        """Create a pattern-based router."""
        return PatternRouter(routing_patterns)
    
    @staticmethod
    def create_history_aware_router(base_router: IRouter, history_weight: float = 0.3) -> HistoryAwareRouter:
        """Create a history-aware router."""
        return HistoryAwareRouter(base_router, history_weight)
    
    @staticmethod
    def create_round_robin_router() -> RoundRobinRouter:
        """Create a round-robin router."""
        return RoundRobinRouter()
    
    @staticmethod
    def create_default_router() -> IRouter:
        """Create a default router with sensible defaults."""
        pattern_router = PatternRouter()
        return HistoryAwareRouter(pattern_router)