"""Semantic Kernel-specific agent implementations."""

import os
import sys
import boto3
from typing import Any, Dict, List, Optional

# Add the parent directory to the Python path to import shared modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))

from semantic_kernel import Kernel
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion
from semantic_kernel.connectors.ai.google.google_ai import GoogleAIChatCompletion
from semantic_kernel.agents import ChatCompletionAgent, AzureAIAgent, BedrockAgent
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
        deployment = os.getenv("AZURE_OPENAI_DEPLOYMENT") 
        api_key = os.getenv("AZURE_OPENAI_KEY")
        
        if not all([endpoint, deployment, api_key]):
            raise AgentInitializationException("AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_DEPLOYMENT, and AZURE_OPENAI_KEY are required")
        
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
            
            # Update agent's chat history
            self.chat_agent.history = working_history
            
            # Get response from agent
            response = await self.chat_agent.invoke()
            
            # Extract content from the last message
            if response and len(response) > 0:
                last_message = response[-1]
                content = last_message.content if hasattr(last_message, 'content') else str(last_message)
            else:
                content = "I apologize, but I couldn't generate a response."
            
            self.logger.debug(f"Generated response length: {len(content) if content else 0}")
            
            return self._create_response(content, metadata)
            
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
        
        if not self.agent_id:
            env_key = "PEOPLE_AGENT_ID" if config.agent_type == AgentType.PEOPLE_LOOKUP else "KNOWLEDGE_AGENT_ID"
            self.agent_id = os.getenv(env_key)
        
        if not self.project_endpoint:
            self.project_endpoint = os.getenv("PROJECT_ENDPOINT")
    
    async def initialize(self) -> None:
        """Initialize the Azure Foundry agent."""
        await super().initialize()
        
        if not self.agent_id:
            raise AgentInitializationException(f"Agent ID required for {self.name}")
        
        if not self.project_endpoint:
            raise AgentInitializationException("PROJECT_ENDPOINT required for Azure Foundry agents")
        
        try:
            # Create Azure AI agent
            credential = DefaultAzureCredential()
            client = AIProjectClient(endpoint=self.project_endpoint, credential=credential)
            definition = await client.agents.get_agent(agent_id=self.agent_id)
            
            self.azure_agent = AzureAIAgent(
                client=client,
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
        if not self.azure_agent:
            raise RuntimeError("Agent not initialized")
        
        try:
            # Create fresh chat history for this conversation
            chat_history = ChatHistory()
            
            # Add previous messages to history
            for msg in history or []:
                if msg.role == MessageRole.USER:
                    chat_history.add_user_message(msg.content)
                elif msg.role == MessageRole.ASSISTANT:
                    chat_history.add_assistant_message(msg.content)
            
            # Add current user message
            chat_history.add_user_message(message)
            
            # Set the agent's history
            self.azure_agent.history = chat_history
            
            # Invoke the agent
            response = await self.azure_agent.invoke()
            
            # Extract content from response
            if response and len(response) > 0:
                last_message = response[-1]
                content = last_message.content if hasattr(last_message, 'content') else str(last_message)
            else:
                content = "I apologize, but I couldn't generate a response."
            
            self.logger.debug(f"Azure Foundry response length: {len(content)}")
            
            return self._create_response(content, metadata)
            
        except Exception as e:
            self.logger.error(f"Error in Azure Foundry agent: {e}")
            return self._create_response(f"I apologize, but I encountered an error: {e}", metadata)


class SemanticKernelGeminiAgent(BaseAgent):
    """Agent using Google Gemini through Semantic Kernel."""
    
    def __init__(self, config: AgentConfig):
        super().__init__(config)
        self.kernel: Optional[Kernel] = None
        self.chat_agent: Optional[ChatCompletionAgent] = None
    
    async def initialize(self) -> None:
        """Initialize the Gemini agent."""
        await super().initialize()
        
        api_key = os.getenv("GOOGLE_API_KEY")
        model_id = os.getenv("GEMINI_MODEL_ID", "gemini-2.0-flash")
        
        if not api_key:
            raise AgentInitializationException("GOOGLE_API_KEY is required for Gemini agent")
        
        try:
            # Create kernel and add Gemini service
            self.kernel = Kernel()
            self.kernel.add_service(
                GoogleAIChatCompletion(
                    gemini_model_id=model_id,
                    api_key=api_key
                )
            )
            
            # Create chat completion agent
            self.chat_agent = ChatCompletionAgent(
                kernel=self.kernel,
                name=self.name,
                instructions=self.config.instructions or "You are a helpful AI assistant."
            )
            
            self.logger.info(f"Initialized Gemini agent with model: {model_id}")
            
        except Exception as e:
            raise AgentInitializationException(f"Failed to initialize Gemini agent: {e}")
    
    async def process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Process message using Gemini."""
        if not self.chat_agent:
            raise RuntimeError("Agent not initialized")
        
        try:
            # Create chat history
            chat_history = ChatHistory()
            
            # Add conversation history
            for msg in history or []:
                if msg.role == MessageRole.USER:
                    chat_history.add_user_message(msg.content)
                elif msg.role == MessageRole.ASSISTANT:
                    chat_history.add_assistant_message(msg.content)
            
            # Add current message
            chat_history.add_user_message(message)
            
            # Set agent history and invoke
            self.chat_agent.history = chat_history
            response = await self.chat_agent.invoke()
            
            # Extract content
            if response and len(response) > 0:
                last_message = response[-1]
                content = last_message.content if hasattr(last_message, 'content') else str(last_message)
            else:
                content = "I apologize, but I couldn't generate a response."
            
            self.logger.debug(f"Gemini response length: {len(content)}")
            
            return self._create_response(content, metadata)
            
        except Exception as e:
            self.logger.error(f"Error in Gemini agent: {e}")
            return self._create_response(f"I apologize, but I encountered an error: {e}", metadata)


class SemanticKernelBedrockAgent(BaseAgent):
    """Agent using AWS Bedrock through Semantic Kernel."""
    
    def __init__(self, config: AgentConfig):
        super().__init__(config)
        self.bedrock_agent: Optional[BedrockAgent] = None
        self.agent_id = config.framework_config.get("agent_id")
        self.region = config.framework_config.get("region")
        
        if not self.agent_id:
            self.agent_id = os.getenv("AWS_BEDROCK_AGENT_ID")
        
        if not self.region:
            self.region = os.getenv("AWS_DEFAULT_REGION", "us-east-1")
    
    async def initialize(self) -> None:
        """Initialize the Bedrock agent."""
        await super().initialize()
        
        if not self.agent_id:
            raise AgentInitializationException("AWS_BEDROCK_AGENT_ID is required for Bedrock agent")
        
        try:
            # Create Bedrock client
            session = boto3.Session()
            bedrock_client = session.client("bedrock-agent-runtime", region_name=self.region)
            
            # Create Bedrock agent
            self.bedrock_agent = BedrockAgent(
                agent_id=self.agent_id,
                bedrock_agent_client=bedrock_client,
                name=self.name
            )
            
            self.logger.info(f"Initialized Bedrock agent: {self.agent_id}")
            
        except Exception as e:
            raise AgentInitializationException(f"Failed to initialize Bedrock agent: {e}")
    
    async def process_message(
        self, 
        message: str, 
        history: Optional[List[AgentMessage]] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> AgentResponse:
        """Process message using Bedrock agent."""
        if not self.bedrock_agent:
            raise RuntimeError("Agent not initialized")
        
        try:
            # For Bedrock agents, we typically need to manage conversations differently
            # This is a simplified implementation
            response = await self.bedrock_agent.invoke(message)
            
            # Extract content
            if response and len(response) > 0:
                last_message = response[-1]
                content = last_message.content if hasattr(last_message, 'content') else str(last_message)
            else:
                content = "I apologize, but I couldn't generate a response."
            
            self.logger.debug(f"Bedrock response length: {len(content)}")
            
            return self._create_response(content, metadata)
            
        except Exception as e:
            self.logger.error(f"Error in Bedrock agent: {e}")
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
        
        if provider == "gemini":
            return SemanticKernelGeminiAgent(config)
        elif provider == "bedrock":
            return SemanticKernelBedrockAgent(config)
        elif provider == "azure_foundry" or config.agent_type in [AgentType.PEOPLE_LOOKUP, AgentType.KNOWLEDGE_FINDER]:
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
        enabled=True,
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
        enabled=True,
        framework_config={
            "provider": "azure_foundry",
            "agent_id": None,  # Will be loaded from env
            "project_endpoint": None  # Will be loaded from env
        }
    ),
    AgentConfig(
        name="gemini_agent",
        agent_type=AgentType.CUSTOM,
        instructions="You are a helpful AI assistant powered by Google Gemini.",
        enabled=False,  # Disabled by default
        framework_config={
            "provider": "gemini"
        }
    ),
    AgentConfig(
        name="bedrock_agent",
        agent_type=AgentType.CUSTOM,
        instructions="You are a helpful AI assistant powered by AWS Bedrock.",
        enabled=False,  # Disabled by default
        framework_config={
            "provider": "bedrock"
        }
    )
]