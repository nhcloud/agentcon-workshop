"""Semantic Kernel-based Agent Group Chat implementation."""

import asyncio
import os
import sys
from typing import Any, Dict, List, Optional, Set, Tuple, Union
from dataclasses import dataclass, field
from enum import Enum
import logging

# Add the parent directory to the Python path to import shared modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))

from semantic_kernel import Kernel
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion
from semantic_kernel.agents import ChatCompletionAgent, AgentGroupChat, ChatHistoryAgentThread
from semantic_kernel.contents import ChatHistory, ChatMessageContent, AuthorRole

from shared import (
    AgentConfig, AgentMessage, AgentResponse, AgentType, 
    MessageRole, AgentInitializationException
)


class GroupChatRole(Enum):
    """Roles for agents in group chat."""
    FACILITATOR = "facilitator"
    PARTICIPANT = "participant"
    OBSERVER = "observer"


@dataclass
class GroupChatConfig:
    """Configuration for group chat."""
    name: str
    description: str = ""
    max_turns: int = 10
    enable_termination_keyword: bool = True
    termination_keyword: str = "TERMINATE"
    require_facilitator: bool = True
    auto_select_speaker: bool = True


@dataclass
class GroupChatParticipant:
    """Represents a participant in the group chat."""
    agent: ChatCompletionAgent
    role: GroupChatRole = GroupChatRole.PARTICIPANT
    can_initiate: bool = True
    max_consecutive_turns: int = 3
    priority: int = 1  # Higher number = higher priority


class SemanticKernelAgentGroupChat:
    """Group chat implementation using Semantic Kernel agents."""
    
    def __init__(self, config: GroupChatConfig):
        self.config = config
        self.name = config.name
        self.participants: Dict[str, GroupChatParticipant] = {}
        self.chat_history: ChatHistory = ChatHistory()
        self.kernel: Optional[Kernel] = None
        self.group_chat: Optional[AgentGroupChat] = None
        self.logger = logging.getLogger(f"GroupChat.{self.name}")
        self.is_initialized = False
        
        # Conversation state
        self.current_speaker: Optional[str] = None
        self.turn_count = 0
        self.consecutive_turns: Dict[str, int] = {}
        self.conversation_active = False
    
    async def initialize(self) -> None:
        """Initialize the group chat and its agents."""
        if self.is_initialized:
            return
            
        try:
            # Initialize Semantic Kernel
            endpoint = os.getenv("AZURE_OPENAI_ENDPOINT")
            deployment = os.getenv("AZURE_OPENAI_DEPLOYMENT")
            api_key = os.getenv("AZURE_OPENAI_KEY")
            
            if not all([endpoint, deployment, api_key]):
                raise AgentInitializationException(
                    "AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_DEPLOYMENT, and AZURE_OPENAI_KEY are required"
                )
            
            self.kernel = Kernel()
            self.kernel.add_service(
                AzureChatCompletion(
                    endpoint=endpoint,
                    deployment_name=deployment,
                    api_key=api_key
                )
            )
            
            self.is_initialized = True
            self.logger.info(f"Initialized group chat: {self.name}")
            
        except Exception as e:
            raise AgentInitializationException(f"Failed to initialize group chat: {e}")
    
    async def add_participant(
        self, 
        name: str, 
        instructions: str,
        role: GroupChatRole = GroupChatRole.PARTICIPANT,
        **kwargs
    ) -> None:
        """Add a participant to the group chat."""
        if not self.is_initialized:
            await self.initialize()
        
        try:
            # Create Semantic Kernel chat agent
            agent = ChatCompletionAgent(
                kernel=self.kernel,
                name=name,
                instructions=instructions
            )
            
            # Create participant wrapper
            participant = GroupChatParticipant(
                agent=agent,
                role=role,
                can_initiate=kwargs.get('can_initiate', True),
                max_consecutive_turns=kwargs.get('max_consecutive_turns', 3),
                priority=kwargs.get('priority', 1)
            )
            
            self.participants[name] = participant
            self.consecutive_turns[name] = 0
            
            self.logger.info(f"Added participant: {name} with role: {role.value}")
            
        except Exception as e:
            raise AgentInitializationException(f"Failed to add participant {name}: {e}")
    
    async def remove_participant(self, name: str) -> bool:
        """Remove a participant from the group chat."""
        if name in self.participants:
            del self.participants[name]
            if name in self.consecutive_turns:
                del self.consecutive_turns[name]
            
            if self.current_speaker == name:
                self.current_speaker = None
            
            self.logger.info(f"Removed participant: {name}")
            return True
        return False
    
    def get_participants(self) -> List[str]:
        """Get list of participant names."""
        return list(self.participants.keys())
    
    def get_active_participants(self) -> List[str]:
        """Get list of participants who can currently speak."""
        return [
            name for name, participant in self.participants.items()
            if participant.role != GroupChatRole.OBSERVER
        ]
    
    async def _select_next_speaker(self, message: str, current_speaker: Optional[str] = None) -> str:
        """Select the next speaker based on the message content and group dynamics."""
        active_participants = self.get_active_participants()
        
        if not active_participants:
            raise RuntimeError("No active participants available")
        
        # If auto-selection is disabled, return first available participant
        if not self.config.auto_select_speaker:
            return active_participants[0]
        
        # Check consecutive turn limits
        available_participants = [
            name for name in active_participants
            if self.consecutive_turns.get(name, 0) < self.participants[name].max_consecutive_turns
        ]
        
        if not available_participants:
            # Reset consecutive turns and use all active participants
            for name in active_participants:
                self.consecutive_turns[name] = 0
            available_participants = active_participants
        
        # Simple selection strategy: rotate through participants
        # In a more sophisticated implementation, you could use ML to select based on expertise
        if current_speaker and current_speaker in available_participants:
            current_index = available_participants.index(current_speaker)
            next_index = (current_index + 1) % len(available_participants)
            return available_participants[next_index]
        
        # Sort by priority and return highest priority participant
        available_participants.sort(
            key=lambda name: self.participants[name].priority, 
            reverse=True
        )
        
        return available_participants[0]
    
    async def _should_terminate(self, message: str) -> bool:
        """Check if the conversation should terminate."""
        if not self.config.enable_termination_keyword:
            return False
        
        if self.turn_count >= self.config.max_turns:
            return True
        
        if self.config.termination_keyword.lower() in message.lower():
            return True
        
        return False
    
    async def send_message(
        self, 
        message: str, 
        sender: Optional[str] = None,
        metadata: Optional[Dict[str, Any]] = None
    ) -> List[AgentResponse]:
        """Send a message to the group chat and get responses."""
        if not self.is_initialized:
            await self.initialize()
        
        if not self.participants:
            raise RuntimeError("No participants in group chat")
        
        responses: List[AgentResponse] = []
        self.conversation_active = True
        
        try:
            # Add initial message to history
            if sender:
                self.chat_history.add_message(
                    ChatMessageContent(
                        role=AuthorRole.USER,
                        content=message,
                        name=sender
                    )
                )
            else:
                self.chat_history.add_user_message(message)
            
            current_message = message
            
            while self.conversation_active and self.turn_count < self.config.max_turns:
                # Select next speaker
                next_speaker = await self._select_next_speaker(current_message, self.current_speaker)
                participant = self.participants[next_speaker]
                
                # Update conversation state
                self.current_speaker = next_speaker
                self.turn_count += 1
                self.consecutive_turns[next_speaker] += 1
                
                # Reset consecutive turns for other participants
                for name in self.consecutive_turns:
                    if name != next_speaker:
                        self.consecutive_turns[name] = 0
                
                self.logger.debug(f"Turn {self.turn_count}: {next_speaker} speaking")
                
                # Get response from selected agent
                try:
                    # Create a thread with the current chat history
                    thread = ChatHistoryAgentThread(chat_history=self.chat_history)
                    
                    # Get agent response using the correct invoke method
                    agent_responses = []
                    async for response in participant.agent.invoke(
                        messages=current_message,
                        thread=thread
                    ):
                        agent_responses.append(response)
                    
                    # Update chat history from the thread
                    self.chat_history = thread.chat_history
                    
                    # Extract response content from the agent responses
                    if agent_responses and len(agent_responses) > 0:
                        # Get the last response content
                        last_response = agent_responses[-1]
                        if hasattr(last_response, 'content'):
                            response_content = last_response.content
                        elif hasattr(last_response, 'message') and hasattr(last_response.message, 'content'):
                            response_content = last_response.message.content
                        else:
                            response_content = str(last_response)
                    else:
                        response_content = "I don't have a response at this time."
                    
                    # Ensure response_content is a string
                    response_content_str = str(response_content) if response_content else "No response"
                    
                    # Add response to history
                    self.chat_history.add_assistant_message(response_content_str, name=next_speaker)
                    
                    # Create response object
                    response = AgentResponse(
                        content=response_content_str,
                        agent_name=next_speaker,
                        metadata={
                            **(metadata or {}),
                            "turn": self.turn_count,
                            "group_chat": self.name,
                            "speaker_role": participant.role.value
                        }
                    )
                    
                    responses.append(response)
                    
                    # Check for termination
                    if await self._should_terminate(response_content_str):
                        self.conversation_active = False
                        self.logger.info(f"Conversation terminated after {self.turn_count} turns")
                        break
                    
                    current_message = response_content_str
                    
                except Exception as e:
                    self.logger.error(f"Error getting response from {next_speaker}: {e}")
                    error_response = AgentResponse(
                        content=f"I encountered an error: {e}",
                        agent_name=next_speaker,
                        metadata={
                            **(metadata or {}),
                            "error": True,
                            "turn": self.turn_count
                        }
                    )
                    responses.append(error_response)
                    break
            
            if self.turn_count >= self.config.max_turns:
                self.logger.info(f"Conversation reached maximum turns ({self.config.max_turns})")
            
            return responses
            
        except Exception as e:
            self.logger.error(f"Error in group chat: {e}")
            raise
        finally:
            self.conversation_active = False
    
    async def reset_conversation(self) -> None:
        """Reset the conversation state."""
        self.chat_history = ChatHistory()
        self.current_speaker = None
        self.turn_count = 0
        self.consecutive_turns = {name: 0 for name in self.participants}
        self.conversation_active = False
        self.logger.info("Conversation state reset")
    
    async def get_conversation_summary(self) -> str:
        """Get a summary of the conversation."""
        messages = []
        for message in self.chat_history.messages:
            speaker = getattr(message, 'name', 'Unknown')
            content = message.content
            messages.append(f"{speaker}: {content}")
        
        return "\n".join(messages) if messages else "No conversation yet."
    
    async def cleanup(self) -> None:
        """Cleanup resources."""
        self.participants.clear()
        self.chat_history = ChatHistory()
        self.conversation_active = False
        self.logger.info(f"Cleaned up group chat: {self.name}")


async def create_example_group_chat() -> SemanticKernelAgentGroupChat:
    """Create an example group chat with sample agents."""
    config = GroupChatConfig(
        name="Example Group Chat",
        description="A sample group chat with multiple AI agents",
        max_turns=8,
        auto_select_speaker=True
    )
    
    group_chat = SemanticKernelAgentGroupChat(config)
    await group_chat.initialize()
    
    # Add sample participants
    await group_chat.add_participant(
        name="Analyst",
        instructions="You are a data analyst. Provide analytical insights and ask clarifying questions about data.",
        role=GroupChatRole.PARTICIPANT,
        priority=2
    )
    
    await group_chat.add_participant(
        name="Researcher", 
        instructions="You are a researcher. Provide research-based information and suggest areas for investigation.",
        role=GroupChatRole.PARTICIPANT,
        priority=1
    )
    
    await group_chat.add_participant(
        name="Facilitator",
        instructions="You are a meeting facilitator. Keep the discussion on track and summarize key points.",
        role=GroupChatRole.FACILITATOR,
        priority=3
    )
    
    return group_chat