"""LangChain-specific router implementation."""

import os
import sys
from typing import Any, Dict, List, Optional

# Add the parent directory to the Python path to import shared modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))

from langchain_openai import AzureChatOpenAI
from langchain_core.messages import HumanMessage, SystemMessage
from langchain_core.output_parsers import StrOutputParser
from langchain_core.prompts import ChatPromptTemplate

from shared import IRouter, AgentMessage, RoutingException


class LangChainLLMRouter(IRouter):
    """Router that uses LangChain LLM for intelligent routing decisions."""
    
    def __init__(self, routing_prompt: Optional[str] = None):
        self.llm: Optional[AzureChatOpenAI] = None
        self.parser = StrOutputParser()
        self.routing_prompt = routing_prompt or self._default_routing_prompt()
        self.prompt_template = ChatPromptTemplate.from_messages([
            ("system", self.routing_prompt)
        ])
    
    def _default_routing_prompt(self) -> str:
        """Default routing prompt."""
        return """You are an agent router. Read the user's message and pick exactly ONE agent to answer.

Agents:
  - people_lookup: Finds information about specific people (name/role/email/manager/phone/team/employee details).
  - knowledge_finder: Answers questions based on documentation, policies, product/technical info, and internal how-tos.
  - generic_agent: General knowledge and chit-chat (cities, weather, general topics not specific to organization or a person).

Rules:
  - Return EXACTLY one of: people_lookup | knowledge_finder | generic_agent
  - Output ONLY the label — no punctuation, no explanation, no quotes.
  - If the request is about a specific person (identity, contact, org info), choose people_lookup.
  - If the request is about documentation/policies/product/tech details/how-to, choose knowledge_finder.
  - Otherwise choose generic_agent.
  - If uncertain, choose generic_agent.

Examples:
  Q: "Write about Boston" -> generic_agent
  Q: "Get me the details of John" -> people_lookup
  Q: "What is our PIM approval flow?" -> knowledge_finder
  Q: "Show the mobile app Intune setup steps" -> knowledge_finder
  Q: "Who is Jane Doe from Sales?" -> people_lookup

Available agents: {available_agents}

User message:
{message}"""
    
    async def initialize(self) -> None:
        """Initialize the LangChain LLM for routing."""
        # Azure OpenAI configuration
        azure_endpoint = os.getenv("AZURE_OPENAI_ENDPOINT")
        api_key = os.getenv("AZURE_OPENAI_API_KEY")
        api_version = os.getenv("AZURE_OPENAI_API_VERSION", "2024-02-01")
        deployment_name = os.getenv("AZURE_OPENAI_DEPLOYMENT_NAME", "gpt-4o-mini")
        
        if not azure_endpoint:
            raise RoutingException("AZURE_OPENAI_ENDPOINT required for LLM router")
        if not api_key:
            raise RoutingException("AZURE_OPENAI_API_KEY required for LLM router")
        
        try:
            self.llm = AzureChatOpenAI(
                azure_endpoint=azure_endpoint,
                api_key=api_key,
                api_version=api_version,
                deployment_name=deployment_name,
                temperature=0.3,  # Lower temperature for routing decisions
                max_tokens=150,  # Routing decisions should be concise
            )
        except Exception as e:
            raise RoutingException(f"Failed to initialize routing LLM: {e}")
    
    async def route_message(
        self, 
        message: str, 
        available_agents: List[str],
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> str:
        """Route message using LangChain LLM."""
        if not self.llm:
            await self.initialize()
        
        try:
            # Build the routing chain
            chain = self.prompt_template | self.llm | self.parser
            
            # Prepare history context if available
            history_context = ""
            if history:
                recent_history = history[-5:]  # Last 5 messages for context
                for msg in recent_history:
                    role = "User" if msg.role.value == "user" else f"Assistant ({msg.agent_name})"
                    history_context += f"{role}: {msg.content[:100]}...\n"
            
            # Get routing decision
            full_prompt = f"""
{history_context}

Current message: {message}
"""
            
            choice = await chain.ainvoke({
                "message": full_prompt,
                "available_agents": ", ".join(available_agents)
            })
            
            selected_agent = choice.strip().lower()
            
            # Validate the choice
            if selected_agent in available_agents:
                return selected_agent
            
            # Try to find a partial match
            for agent in available_agents:
                if selected_agent in agent or agent in selected_agent:
                    return agent
            
            # Fallback to generic agent or first available
            fallback_options = ["generic_agent", "generic"]
            for fallback in fallback_options:
                if fallback in available_agents:
                    return fallback
            
            if available_agents:
                return available_agents[0]
            
            raise RoutingException("No available agents for routing")
            
        except Exception as e:
            # Fallback routing on error
            fallback_options = ["generic_agent", "generic"]
            for fallback in fallback_options:
                if fallback in available_agents:
                    return fallback
            
            if available_agents:
                return available_agents[0]
            
            raise RoutingException(f"Routing failed: {e}")


class HybridLangChainRouter(IRouter):
    """Hybrid router that combines pattern matching with LLM routing."""
    
    def __init__(self, fallback_to_llm: bool = True):
        self.pattern_rules = {
            "people_lookup": [
                r"\b(who is|find|lookup|contact)\b.*\b(person|employee|user|staff)\b",
                r"\b(manager|supervisor|lead|director)\b",
                r"\bemail.*@\b",
                r"\bphone.*number\b"
            ],
            "knowledge_finder": [
                r"\b(how to|procedure|process|guide|documentation)\b",
                r"\b(policy|guideline|standard|requirement)\b",
                r"\b(setup|configuration|install|deploy)\b",
                r"\b(akumina|platform|technical)\b"
            ]
        }
        self.fallback_to_llm = fallback_to_llm
        self.llm_router = LangChainLLMRouter() if fallback_to_llm else None
    
    async def route_message(
        self, 
        message: str, 
        available_agents: List[str],
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> str:
        """Route using pattern matching first, then LLM if needed."""
        import re
        
        message_lower = message.lower()
        
        # Try pattern matching first
        scores = {}
        for agent_name, patterns in self.pattern_rules.items():
            if agent_name in available_agents:
                score = 0
                for pattern in patterns:
                    if re.search(pattern, message_lower):
                        score += 1
                scores[agent_name] = score
        
        # If we have a clear winner from patterns, use it
        if scores:
            best_agent = max(scores.keys(), key=lambda x: scores[x])
            if scores[best_agent] > 0:
                return best_agent
        
        # Fallback to LLM routing if enabled
        if self.fallback_to_llm and self.llm_router:
            try:
                return await self.llm_router.route_message(message, available_agents, history, metadata)
            except Exception:
                pass  # Continue to final fallback
        
        # Final fallback
        fallback_options = ["generic_agent", "generic"]
        for fallback in fallback_options:
            if fallback in available_agents:
                return fallback
        
        if available_agents:
            return available_agents[0]
        
        raise RoutingException("No available agents for routing")