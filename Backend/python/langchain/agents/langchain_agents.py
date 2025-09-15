"""LangChain-specific agent implementations."""

import os
from typing import Any, Dict, List, Optional

from langchain_azure_ai.chat_models import AzureAIChatCompletionsModel
from langchain_core.messages import HumanMessage, AIMessage, SystemMessage, BaseMessage
from langchain_core.output_parsers import StrOutputParser
from langchain_core.prompts import ChatPromptTemplate

from azure.ai.projects.aio import AIProjectClient
from azure.identity.aio import DefaultAzureCredential

# Import ListSortOrder compatibly across versions
try:
    from azure.ai.projects.models import ListSortOrder
except ImportError:
    try:
        from azure.ai.agents.models import ListSortOrder
    except ImportError:
        class ListSortOrder:
            ASCENDING = "asc"
            DESCENDING = "desc"

from shared import (
    BaseAgent, AgentConfig, AgentMessage, AgentResponse, AgentType, 
    MessageRole, IAgentFactory, AgentInitializationException
)


class LangChainGenericAgent(BaseAgent):
    """Generic agent using LangChain Azure AI Chat Completions."""
    
    def __init__(self, config: AgentConfig):
        super().__init__(config)
        self.llm: Optional[AzureAIChatCompletionsModel] = None
        self.system_message = config.instructions or "You are a helpful AI assistant."
    
    async def initialize(self) -> None:
        """Initialize the LangChain agent."""
        await super().initialize()
        
        endpoint = os.getenv("AZURE_INFERENCE_ENDPOINT")
        credential = os.getenv("AZURE_INFERENCE_CREDENTIAL")
        model = os.getenv("GENERIC_MODEL", "gpt-4o-mini")
        
        if not endpoint:
            raise AgentInitializationException("AZURE_INFERENCE_ENDPOINT is required")
        
        try:
            self.llm = AzureAIChatCompletionsModel(
                endpoint=endpoint,
                credential=credential,
                model=model,
            )
            self.logger.info(f"Initialized LangChain agent with model: {model}")
        except Exception as e:
            raise AgentInitializationException(f"Failed to initialize LangChain model: {e}")
    
    def _convert_history_to_langchain(self, history: List[AgentMessage]) -> List[BaseMessage]:
        """Convert AgentMessage history to LangChain messages."""
        messages = []
        
        # Add system message
        if self.system_message:
            messages.append(SystemMessage(content=self.system_message))
        
        # Add conversation history
        for msg in history or []:
            if msg.role == MessageRole.USER:
                messages.append(HumanMessage(content=msg.content))
            elif msg.role == MessageRole.ASSISTANT:
                messages.append(AIMessage(content=msg.content))
            elif msg.role == MessageRole.SYSTEM:
                messages.append(SystemMessage(content=msg.content))
        
        return messages
    
    async def process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Process message using LangChain."""
        if not self.llm:
            raise RuntimeError("Agent not initialized")
        
        try:
            # Build message chain
            messages = self._convert_history_to_langchain(history or [])
            messages.append(HumanMessage(content=message))
            
            # Get response from LangChain
            response = await self.llm.ainvoke(messages)
            
            # Extract content
            if isinstance(response, AIMessage):
                content = response.content
            else:
                content = str(response)
            
            self.logger.debug(f"Generated response length: {len(content) if content else 0}")
            
            return self._create_response(content, metadata)
            
        except Exception as e:
            self.logger.error(f"Error processing message: {e}")
            return self._create_response(f"I apologize, but I encountered an error: {e}", metadata)


class LangChainAzureFoundryAgent(BaseAgent):
    """Agent using Azure AI Foundry through LangChain integration."""
    
    def __init__(self, config: AgentConfig):
        super().__init__(config)
        self.agent_id = config.framework_config.get("agent_id")
        self.project_endpoint = config.framework_config.get("project_endpoint")
        
        if not self.agent_id:
            self.agent_id = os.getenv("PEOPLE_AGENT_ID" if config.agent_type == AgentType.PEOPLE_LOOKUP else "KNOWLEDGE_AGENT_ID")
        
        if not self.project_endpoint:
            self.project_endpoint = os.getenv("PROJECT_ENDPOINT")
    
    async def initialize(self) -> None:
        """Initialize the Azure Foundry agent."""
        await super().initialize()
        
        if not self.agent_id:
            raise AgentInitializationException(f"Agent ID required for {self.name}")
        
        if not self.project_endpoint:
            raise AgentInitializationException("PROJECT_ENDPOINT required for Azure Foundry agents")
        
        self.logger.info(f"Initialized Azure Foundry agent: {self.agent_id}")
    
    async def _create_foundry_run(self, message: str, thread_id: Optional[str] = None) -> tuple[str, str]:
        """Create and process an Azure Foundry agent run."""
        cred = DefaultAzureCredential()
        client = AIProjectClient(endpoint=self.project_endpoint, credential=cred)
        
        try:
            # Create or reuse thread
            if thread_id:
                class _Thread:
                    def __init__(self, id: str):
                        self.id = id
                thread = _Thread(thread_id)
            else:
                thread = await client.agents.threads.create()
            
            # Add user message
            await client.agents.messages.create(
                thread_id=thread.id,
                role="user",
                content=message,
            )
            
            # Run the agent
            await client.agents.runs.create_and_process(
                thread_id=thread.id,
                agent_id=self.agent_id,
            )
            
            # Get the latest assistant response
            msgs = client.agents.messages.list(
                thread_id=thread.id, 
                order=ListSortOrder.ASCENDING
            )
            
            last_assistant = ""
            async for m in msgs:
                if getattr(m, "role", None) == "assistant" and getattr(m, "text_messages", None):
                    last_assistant = m.text_messages[-1].text.value
            
            return (last_assistant or "(no assistant text returned)", thread.id)
            
        finally:
            try:
                await client.close()
                await cred.close()
            except Exception:
                pass
    
    async def process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Process message using Azure Foundry agent."""
        try:
            # Extract thread ID from metadata if available
            thread_id = metadata.get("thread_id") if metadata else None
            
            # Create agent run
            content, new_thread_id = await self._create_foundry_run(message, thread_id)
            
            # Update metadata with thread ID for conversation continuity
            response_metadata = (metadata or {}).copy()
            response_metadata["thread_id"] = new_thread_id
            
            self.logger.debug(f"Azure Foundry response length: {len(content)}")
            
            return self._create_response(content, response_metadata)
            
        except Exception as e:
            self.logger.error(f"Error in Azure Foundry agent: {e}")
            return self._create_response(f"I apologize, but I encountered an error: {e}", metadata)


class LangChainAgentFactory(IAgentFactory):
    """Factory for creating LangChain-based agents."""
    
    def get_supported_types(self) -> List[AgentType]:
        """Get supported agent types."""
        return [
            AgentType.GENERIC,
            AgentType.PEOPLE_LOOKUP,
            AgentType.KNOWLEDGE_FINDER
        ]
    
    async def create_agent(self, config: AgentConfig) -> BaseAgent:
        """Create a LangChain agent based on configuration."""
        
        if config.agent_type == AgentType.GENERIC:
            return LangChainGenericAgent(config)
        
        elif config.agent_type in [AgentType.PEOPLE_LOOKUP, AgentType.KNOWLEDGE_FINDER]:
            return LangChainAzureFoundryAgent(config)
        
        else:
            raise ValueError(f"Unsupported agent type: {config.agent_type}")


# Agent configuration presets for LangChain
LANGCHAIN_AGENT_CONFIGS = [
    AgentConfig(
        name="generic_agent",
        agent_type=AgentType.GENERIC,
        instructions="You are a helpful AI assistant. Provide accurate and helpful responses.",
        enabled=True,
        framework_config={
            "model": "gpt-4o-mini",
            "temperature": 0.7
        }
    ),
    AgentConfig(
        name="people_lookup",
        agent_type=AgentType.PEOPLE_LOOKUP,
        instructions="You help find information about people in the organization.",
        enabled=True,
        framework_config={
            "agent_id": None,  # Will be loaded from env
            "project_endpoint": None  # Will be loaded from env
        }
    ),
    AgentConfig(
        name="knowledge_finder",
        agent_type=AgentType.KNOWLEDGE_FINDER,
        instructions="You help find information from documentation and knowledge bases.",
        enabled=True,
        framework_config={
            "agent_id": None,  # Will be loaded from env
            "project_endpoint": None  # Will be loaded from env
        }
    )
]