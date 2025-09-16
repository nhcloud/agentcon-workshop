"""LangChain-specific agent implementations."""

import os
import sys
from typing import Any, Dict, List, Optional

# Add the parent directory to the Python path to import shared modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))

from langchain_openai import AzureChatOpenAI
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
    """Generic agent using LangChain Azure OpenAI."""
    
    def __init__(self, config: AgentConfig):
        super().__init__(config)
        self.llm: Optional[AzureChatOpenAI] = None
        self.system_message = config.instructions or "You are a helpful AI assistant."
    
    async def initialize(self) -> None:
        """Initialize the LangChain agent with Azure OpenAI."""
        await super().initialize()
        
        # Azure OpenAI configuration
        azure_endpoint = os.getenv("AZURE_OPENAI_ENDPOINT")
        # Expand literal placeholder form like ${AZURE_OPENAI_ENDPOINT}
        if azure_endpoint and azure_endpoint.startswith("${") and azure_endpoint.endswith("}"):
            var_name = azure_endpoint[2:-1]
            expanded = os.getenv(var_name)
            if expanded:
                azure_endpoint = expanded
            else:
                self.logger.warning(f"Environment placeholder {azure_endpoint} not resolved; value empty.")
        api_key = os.getenv("AZURE_OPENAI_API_KEY")
        api_version = os.getenv("AZURE_OPENAI_API_VERSION", "2024-02-01")
        deployment_name = os.getenv("AZURE_OPENAI_DEPLOYMENT_NAME", "gpt-4o-mini")
        
        if not azure_endpoint:
            raise AgentInitializationException("AZURE_OPENAI_ENDPOINT is required")
        if not api_key:
            raise AgentInitializationException("AZURE_OPENAI_API_KEY is required")
        # Guard against non-HTTPS endpoints which will break bearer/token auth flows downstream
        if not azure_endpoint.lower().startswith("https://"):
            raise AgentInitializationException(
                f"AZURE_OPENAI_ENDPOINT must start with https:// (got: {azure_endpoint}). "
                "Bearer token or API key authentication is not permitted over plain HTTP."
            )
        if "${" in azure_endpoint:
            self.logger.warning(f"AZURE_OPENAI_ENDPOINT appears to contain an unresolved placeholder: {azure_endpoint}")
        
        try:
            self.llm = AzureChatOpenAI(
                azure_endpoint=azure_endpoint,
                api_key=api_key,
                api_version=api_version,
                deployment_name=deployment_name,
                temperature=0.7,
                max_tokens=800,
            )
            self.logger.info(f"Initialized LangChain agent with deployment: {deployment_name}")
        except Exception as e:
            raise AgentInitializationException(f"Failed to initialize Azure OpenAI model: {e}")
    
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

        # Expand placeholder patterns like ${PEOPLE_AGENT_ID}
        if self.agent_id and self.agent_id.startswith("${") and self.agent_id.endswith("}"):
            var_name = self.agent_id[2:-1]
            expanded = os.getenv(var_name)
            if expanded:
                self.logger.info(f"Expanded agent_id placeholder {var_name} -> {expanded}")
                self.agent_id = expanded
            else:
                self.logger.warning(f"Agent ID placeholder {self.agent_id} could not be resolved from environment.")
    
    async def initialize(self) -> None:
        """Initialize the Azure Foundry agent."""
        await super().initialize()
        
        if not self.agent_id:
            raise AgentInitializationException(f"Agent ID required for {self.name}")
        
        if not self.project_endpoint:
            raise AgentInitializationException("PROJECT_ENDPOINT required for Azure Foundry agents")
        # Support unresolved literal placeholder patterns like ${PROJECT_ENDPOINT}
        if self.project_endpoint.startswith("${") and self.project_endpoint.endswith("}"):
            var_name = self.project_endpoint[2:-1]
            expanded = os.getenv(var_name)
            if expanded:
                self.project_endpoint = expanded
                self.logger.info(f"Expanded PROJECT_ENDPOINT placeholder {var_name} -> {self.project_endpoint}")
            else:
                self.logger.warning(f"PROJECT_ENDPOINT placeholder {self.project_endpoint} could not be resolved from environment.")
        # Explicit HTTPS check – DefaultAzureCredential (bearer token) cannot be used with non-TLS endpoints
        if not self.project_endpoint.lower().startswith("https://"):
            raise AgentInitializationException(
                f"PROJECT_ENDPOINT must start with https:// (got: {self.project_endpoint}). "
                "Azure credentials refuse bearer token authentication for non-TLS URLs. If you are tunneling locally, use an HTTPS tunnel (e.g. 'https://<subdomain>.ngrok.io') instead of http://localhost."
            )
        if "${" in self.project_endpoint:
            self.logger.warning(f"PROJECT_ENDPOINT appears to contain an unresolved placeholder: {self.project_endpoint}")

        # Validate agent_id format (alphanumeric, underscore, dash)
        if self.agent_id and not self.agent_id.replace('_','').replace('-','').isalnum():
            raise AgentInitializationException(
                f"Agent ID '{self.agent_id}' has invalid characters. Ensure environment variable contains only letters, numbers, underscores, or dashes."
            )
        if "${" in (self.agent_id or ""):
            raise AgentInitializationException(
                f"Agent ID contains unresolved placeholder: {self.agent_id}. Set PEOPLE_AGENT_ID / KNOWLEDGE_AGENT_ID properly."
            )
        
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