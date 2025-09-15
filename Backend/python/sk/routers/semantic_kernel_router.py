"""Semantic Kernel-specific router implementation."""

import os
import sys
from typing import Any, Dict, List, Optional

# Add the parent directory to the Python path to import shared modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))

from semantic_kernel import Kernel
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion
from semantic_kernel.functions import KernelFunctionFromPrompt
from semantic_kernel.contents import ChatHistory

from shared import IRouter, AgentMessage, RoutingException


class SemanticKernelLLMRouter(IRouter):
    """Router that uses Semantic Kernel for intelligent routing decisions."""
    
    def __init__(self, routing_prompt: Optional[str] = None):
        self.kernel: Optional[Kernel] = None
        self.routing_function: Optional[KernelFunctionFromPrompt] = None
        self.routing_prompt = routing_prompt or self._default_routing_prompt()
    
    def _default_routing_prompt(self) -> str:
        """Default routing prompt."""
        return """You are an agent router. Read the user's message and conversation history, then pick exactly ONE agent to answer.

Available Agents:
{{$available_agents}}

Agent Descriptions:
- people_lookup: Finds information about specific people (name/role/email/manager/phone/team/employee details).
- knowledge_finder: Answers questions based on documentation, policies, product/technical info, and internal how-tos.
- generic_agent: General knowledge and chit-chat (cities, weather, general topics not specific to organization or a person).
- gemini_agent: Google Gemini-powered assistant for creative and diverse responses.
- bedrock_agent: AWS Bedrock-powered assistant for enterprise-focused responses.

Conversation History:
{{$history}}

Current User Message:
{{$message}}

Rules:
- Return EXACTLY one agent name from the available agents list
- Output ONLY the agent name — no punctuation, no explanation, no quotes
- If the request is about a specific person (identity, contact, org info), choose people_lookup
- If the request is about documentation/policies/product/tech details/how-to, choose knowledge_finder
- For creative tasks or diverse responses, prefer gemini_agent if available
- For enterprise/business focused tasks, prefer bedrock_agent if available
- Otherwise choose generic_agent
- If uncertain, choose generic_agent

Examples:
Q: "Write about Boston" -> generic_agent
Q: "Get me the details of John" -> people_lookup
Q: "What is our PIM approval flow?" -> knowledge_finder
Q: "Write a creative story" -> gemini_agent (if available)
Q: "Analyze business metrics" -> bedrock_agent (if available)

Selected Agent:"""
    
    async def initialize(self) -> None:
        """Initialize the Semantic Kernel for routing."""
        endpoint = os.getenv("AZURE_OPENAI_ENDPOINT")
        deployment = os.getenv("AZURE_OPENAI_DEPLOYMENT")
        api_key = os.getenv("AZURE_OPENAI_KEY")
        
        if not all([endpoint, deployment, api_key]):
            raise RoutingException("Azure OpenAI configuration required for SK router")
        
        try:
            # Create kernel and add chat completion service
            self.kernel = Kernel()
            self.kernel.add_service(
                AzureChatCompletion(
                    endpoint=endpoint,
                    deployment_name=deployment,
                    api_key=api_key
                )
            )
            
            # Create routing function
            self.routing_function = KernelFunctionFromPrompt(
                function_name="route_message",
                prompt=self.routing_prompt
            )
            
        except Exception as e:
            raise RoutingException(f"Failed to initialize SK routing: {e}")
    
    async def route_message(
        self, 
        message: str, 
        available_agents: List[str],
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> str:
        """Route message using Semantic Kernel."""
        if not self.kernel or not self.routing_function:
            await self.initialize()
        
        try:
            # Prepare history context
            history_context = ""
            if history:
                recent_history = history[-5:]  # Last 5 messages for context
                for msg in recent_history:
                    role = "User" if msg.role.value == "user" else f"Assistant ({msg.agent_name})"
                    history_context += f"{role}: {msg.content[:100]}...\n"
            
            # Invoke routing function
            result = await self.routing_function.invoke(
                self.kernel,
                message=message,
                available_agents=", ".join(available_agents),
                history=history_context
            )
            
            selected_agent = str(result).strip().lower()
            
            # Validate the choice
            if selected_agent in available_agents:
                return selected_agent
            
            # Try to find a partial match
            for agent in available_agents:
                if selected_agent in agent or agent in selected_agent:
                    return agent
            
            # Fallback logic
            return self._get_fallback_agent(available_agents)
            
        except Exception as e:
            # Fallback routing on error
            return self._get_fallback_agent(available_agents)
    
    def _get_fallback_agent(self, available_agents: List[str]) -> str:
        """Get fallback agent when routing fails."""
        fallback_options = ["generic_agent", "generic"]
        
        for fallback in fallback_options:
            if fallback in available_agents:
                return fallback
        
        if available_agents:
            return available_agents[0]
        
        raise RoutingException("No available agents for routing")


class MultiModalSemanticKernelRouter(IRouter):
    """Advanced router that can handle multi-modal routing decisions."""
    
    def __init__(self):
        self.kernel: Optional[Kernel] = None
        self.routing_functions: Dict[str, KernelFunctionFromPrompt] = {}
        self._initialize_routing_functions()
    
    def _initialize_routing_functions(self) -> None:
        """Initialize different routing functions for different scenarios."""
        
        # Standard text routing
        self.text_routing_prompt = """Analyze this text message and determine the best agent:

Message: {{$message}}
Available Agents: {{$available_agents}}

Route to:
- people_lookup: For person/employee information requests
- knowledge_finder: For documentation/policy/technical questions  
- generic_agent: For general conversation
- gemini_agent: For creative/artistic tasks
- bedrock_agent: For business/enterprise analysis

Output only the agent name:"""

        # Context-aware routing (considers conversation flow)
        self.context_routing_prompt = """Consider the conversation context and route appropriately:

Previous Messages:
{{$history}}

Current Message: {{$message}}
Available Agents: {{$available_agents}}

Routing Strategy:
1. Maintain conversation continuity when possible
2. Switch agents only when topic clearly changes
3. Consider user's apparent workflow/intent

Best Agent:"""

    async def initialize(self) -> None:
        """Initialize the multi-modal router."""
        endpoint = os.getenv("AZURE_OPENAI_ENDPOINT")
        deployment = os.getenv("AZURE_OPENAI_DEPLOYMENT")
        api_key = os.getenv("AZURE_OPENAI_KEY")
        
        if not all([endpoint, deployment, api_key]):
            raise RoutingException("Azure OpenAI configuration required")
        
        try:
            self.kernel = Kernel()
            self.kernel.add_service(
                AzureChatCompletion(
                    endpoint=endpoint,
                    deployment_name=deployment,
                    api_key=api_key
                )
            )
            
            # Create routing functions
            self.routing_functions["text"] = KernelFunctionFromPrompt(
                function_name="route_text",
                prompt=self.text_routing_prompt
            )
            
            self.routing_functions["context"] = KernelFunctionFromPrompt(
                function_name="route_context",
                prompt=self.context_routing_prompt
            )
            
        except Exception as e:
            raise RoutingException(f"Failed to initialize multi-modal router: {e}")
    
    async def route_message(
        self, 
        message: str, 
        available_agents: List[str],
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> str:
        """Route message using context-aware logic."""
        if not self.kernel:
            await self.initialize()
        
        try:
            # Choose routing strategy based on context
            if history and len(history) > 2:
                routing_function = self.routing_functions["context"]
                
                # Prepare history context
                history_context = ""
                recent_history = history[-3:]
                for msg in recent_history:
                    role = "User" if msg.role.value == "user" else f"Assistant"
                    history_context += f"{role}: {msg.content[:150]}\n"
                
                result = await routing_function.invoke(
                    self.kernel,
                    message=message,
                    available_agents=", ".join(available_agents),
                    history=history_context
                )
            else:
                # Use simple text routing for new conversations
                routing_function = self.routing_functions["text"]
                result = await routing_function.invoke(
                    self.kernel,
                    message=message,
                    available_agents=", ".join(available_agents)
                )
            
            selected_agent = str(result).strip().lower()
            
            # Validate and return
            if selected_agent in available_agents:
                return selected_agent
            
            # Try partial matching
            for agent in available_agents:
                if selected_agent in agent or agent in selected_agent:
                    return agent
            
            # Fallback
            fallback_options = ["generic_agent", "generic"]
            for fallback in fallback_options:
                if fallback in available_agents:
                    return fallback
            
            return available_agents[0] if available_agents else "generic_agent"
            
        except Exception as e:
            # Error fallback
            fallback_options = ["generic_agent", "generic"]
            for fallback in fallback_options:
                if fallback in available_agents:
                    return fallback
            
            if available_agents:
                return available_agents[0]
            
            raise RoutingException(f"Routing failed: {e}")


class HybridSemanticKernelRouter(IRouter):
    """Hybrid router combining pattern matching with Semantic Kernel intelligence."""
    
    def __init__(self, fallback_to_sk: bool = True):
        self.pattern_rules = {
            "people_lookup": [
                r"\b(who is|find|lookup|contact)\b.*\b(person|employee|user|staff)\b",
                r"\b(manager|supervisor|lead|director|ceo|cto)\b",
                r"\bemail.*@\b",
                r"\bphone.*number\b",
                r"\b(team|department|organization|org chart)\b"
            ],
            "knowledge_finder": [
                r"\b(how to|procedure|process|guide|documentation|docs)\b",
                r"\b(policy|guideline|standard|requirement|compliance)\b",
                r"\b(setup|configuration|install|deploy|implementation)\b",
                r"\b(akumina|platform|technical|api|integration)\b",
                r"\b(manual|tutorial|instruction|walkthrough)\b"
            ],
            "gemini_agent": [
                r"\b(creative|story|poem|art|design|imagine)\b",
                r"\b(brainstorm|ideate|innovative|artistic)\b"
            ],
            "bedrock_agent": [
                r"\b(analyze|analysis|business|enterprise|metrics|kpi)\b",
                r"\b(strategy|planning|forecast|budget|roi)\b"
            ]
        }
        
        self.fallback_to_sk = fallback_to_sk
        self.sk_router = SemanticKernelLLMRouter() if fallback_to_sk else None
    
    async def route_message(
        self, 
        message: str, 
        available_agents: List[str],
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> str:
        """Route using pattern matching first, then SK if needed."""
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
        
        # Fallback to Semantic Kernel routing if enabled
        if self.fallback_to_sk and self.sk_router:
            try:
                return await self.sk_router.route_message(message, available_agents, history, metadata)
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