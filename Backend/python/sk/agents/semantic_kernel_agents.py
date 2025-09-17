"""Semantic Kernel-specific agent implementations."""

import os
import sys
from typing import Any, Dict, List, Optional

# Add the parent directory to the Python path to import shared modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))

from semantic_kernel import Kernel
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion
from semantic_kernel.agents import ChatCompletionAgent, AzureAIAgent, ChatHistoryAgentThread, AzureAIAgentThread
from semantic_kernel.contents import ChatHistory, ChatMessageContent, AuthorRole

from azure.identity.aio import DefaultAzureCredential
from azure.ai.projects.aio import AIProjectClient

from shared import (
    BaseAgent, AgentConfig, AgentMessage, AgentResponse, AgentType, 
    MessageRole, IAgentFactory, AgentInitializationException
)


class SemanticKernelGenericAgent(BaseAgent):
    """Generic agent using Semantic Kernel with Azure OpenAI."""
    
    def __init__(self, config: AgentConfig):
        super().__init__(config)
        self.kernel: Optional[Kernel] = None
        self.chat_agent: Optional[ChatCompletionAgent] = None
        self.chat_history: Optional[ChatHistory] = None
    
    async def initialize(self) -> None:
        """Initialize the Semantic Kernel agent."""
        await super().initialize()
        
        endpoint = os.getenv("AZURE_OPENAI_ENDPOINT")
        deployment = os.getenv("AZURE_OPENAI_DEPLOYMENT_NAME") 
        api_key = os.getenv("AZURE_OPENAI_API_KEY")
        
        if not all([endpoint, deployment, api_key]):
            raise AgentInitializationException("AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_DEPLOYMENT_NAME, and AZURE_OPENAI_API_KEY are required")
        
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
            
            # Create chat completion agent
            self.chat_agent = ChatCompletionAgent(
                kernel=self.kernel,
                name=self.name,
                instructions=self.config.instructions or "You are a helpful AI assistant."
            )
            
            # Initialize chat history
            self.chat_history = ChatHistory()
            
            self.logger.info(f"Initialized Semantic Kernel agent with deployment: {deployment}")
            
        except Exception as e:
            raise AgentInitializationException(f"Failed to initialize Semantic Kernel: {e}")
    
    def _convert_history_to_sk(self, history: List[AgentMessage]) -> ChatHistory:
        """Convert AgentMessage history to Semantic Kernel ChatHistory."""
        sk_history = ChatHistory()
        
        for msg in history or []:
            if msg.role == MessageRole.USER:
                sk_history.add_user_message(msg.content)
            elif msg.role == MessageRole.ASSISTANT:
                sk_history.add_assistant_message(msg.content)
            elif msg.role == MessageRole.SYSTEM:
                sk_history.add_system_message(msg.content)
        
        return sk_history
    
    async def process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Process message using Semantic Kernel."""
        if not self.chat_agent:
            raise RuntimeError("Agent not initialized")
        
        try:
            # Convert history to Semantic Kernel format
            working_history = self._convert_history_to_sk(history or [])
            
            # Add current user message
            working_history.add_user_message(message)
            
            # Create a thread with the working history
            thread = ChatHistoryAgentThread(chat_history=working_history)
            
            # Get response from agent using the correct method
            response = await self.chat_agent.get_response(messages=message, thread=thread)
            
            # Extract content from the response
            if response and hasattr(response, 'content'):
                content = response.content
            elif response and hasattr(response, 'message') and hasattr(response.message, 'content'):
                content = response.message.content
            elif response:
                content = str(response)
            else:
                content = "I apologize, but I couldn't generate a response."
            
            # Ensure content is a string for length calculation
            content_str = str(content) if content else ""
            self.logger.debug(f"Generated response length: {len(content_str)}")
            
            return self._create_response(content_str, metadata)
            
        except Exception as e:
            self.logger.error(f"Error processing message: {e}")
            return self._create_response(f"I apologize, but I encountered an error: {e}", metadata)


class SemanticKernelAzureFoundryAgent(BaseAgent):
    """Agent using Azure AI Foundry through Semantic Kernel."""
    
    def __init__(self, config: AgentConfig):
        super().__init__(config)
        self.agent_id = config.framework_config.get("agent_id")
        self.project_endpoint = config.framework_config.get("project_endpoint")
        self.azure_agent: Optional[AzureAIAgent] = None
        self.client: Optional[AIProjectClient] = None
        
        if not self.agent_id:
            env_key = "PEOPLE_AGENT_ID" if config.agent_type == AgentType.PEOPLE_LOOKUP else "KNOWLEDGE_AGENT_ID"
            self.agent_id = os.getenv(env_key)
        
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
        
        try:
            # Use simple DefaultAzureCredential - the HTTPS check above should prevent the bearer token error
            credential = DefaultAzureCredential()
            self.client = AIProjectClient(endpoint=self.project_endpoint, credential=credential)
            definition = await self.client.agents.get_agent(agent_id=self.agent_id)
            
            self.azure_agent = AzureAIAgent(
                client=self.client,
                definition=definition,
                name=self.name,
                instructions=self.config.instructions or ""
            )
            
            self.logger.info(f"Initialized Azure Foundry agent: {self.agent_id}")
            
        except Exception as e:
            raise AgentInitializationException(f"Failed to initialize Azure Foundry agent: {e}")
    
    async def process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Process message using Azure Foundry agent."""
        if not self.azure_agent or not self.client:
            raise RuntimeError("Agent not initialized")
        
        try:
            # Create an Azure AI Agent thread with the required client
            thread = AzureAIAgentThread(client=self.client)
            
            # Prepare messages list including history and current message
            messages = []
            
            # Add previous messages to the messages list
            for msg in history or []:
                if msg.role == MessageRole.USER:
                    messages.append(ChatMessageContent(role=AuthorRole.USER, content=msg.content))
                elif msg.role == MessageRole.ASSISTANT:
                    messages.append(ChatMessageContent(role=AuthorRole.ASSISTANT, content=msg.content))
            
            # Add current user message
            messages.append(ChatMessageContent(role=AuthorRole.USER, content=message))
            
            # Invoke the agent with the messages and thread
            response_item = await self.azure_agent.get_response(messages=messages, thread=thread)
            
            # Extract content from response
            if response_item and hasattr(response_item, 'message') and hasattr(response_item.message, 'content'):
                content = response_item.message.content
            elif response_item and hasattr(response_item, 'content'):
                content = response_item.content
            else:
                content = "I apologize, but I couldn't generate a response."
            
            self.logger.debug(f"Azure Foundry response length: {len(content)}")
            
            return self._create_response(content, metadata)
            
        except Exception as e:
            self.logger.error(f"Error in Azure Foundry agent: {e}")
            return self._create_response(f"I apologize, but I encountered an error: {e}", metadata)


class SemanticKernelAgentFactory(IAgentFactory):
    """Factory for creating Semantic Kernel-based agents."""
    
    def get_supported_types(self) -> List[AgentType]:
        """Get supported agent types."""
        return [
            AgentType.GENERIC,
            AgentType.PEOPLE_LOOKUP,
            AgentType.KNOWLEDGE_FINDER,
            AgentType.CUSTOM  # For Gemini, Bedrock, etc.
        ]
    
    async def create_agent(self, config: AgentConfig) -> BaseAgent:
        """Create a Semantic Kernel agent based on configuration."""
        
        # Check for custom agent types in framework config
        provider = config.framework_config.get("provider", "azure_openai")
        
        if provider == "azure_foundry" or config.agent_type in [AgentType.PEOPLE_LOOKUP, AgentType.KNOWLEDGE_FINDER]:
            return SemanticKernelAzureFoundryAgent(config)
        else:
            # Default to Azure OpenAI generic agent
            return SemanticKernelGenericAgent(config)


# Agent configuration presets for Semantic Kernel
SEMANTIC_KERNEL_AGENT_CONFIGS = [
    AgentConfig(
        name="generic_agent",
        agent_type=AgentType.GENERIC,
        instructions="You are a helpful AI assistant. Provide accurate and helpful responses.",
        enabled=True,
        framework_config={
            "provider": "azure_openai"
        }
    ),
    AgentConfig(
        name="people_lookup",
        agent_type=AgentType.PEOPLE_LOOKUP,
        instructions="You help find information about people in the organization.",
        enabled=True,  # Re-enabled with authentication fix
        framework_config={
            "provider": "azure_foundry",
            "agent_id": None,  # Will be loaded from env
            "project_endpoint": None  # Will be loaded from env
        }
    ),
    AgentConfig(
        name="knowledge_finder",
        agent_type=AgentType.KNOWLEDGE_FINDER,
        instructions="You help find information from documentation and knowledge bases.",
        enabled=True,  # Re-enabled with authentication fix
        framework_config={
            "provider": "azure_foundry",
            "agent_id": None,  # Will be loaded from env
            "project_endpoint": None  # Will be loaded from env
        }
    )
]