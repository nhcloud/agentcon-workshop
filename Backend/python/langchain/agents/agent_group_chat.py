"""Enhanced LangChain-based Agent Group Chat implementation that uses actual agents."""

import asyncio
import os
import sys
from typing import Any, Dict, List, Optional, Set, Tuple, Union
from dataclasses import dataclass, field
from enum import Enum
import logging

# Add the parent directory to the Python path to import shared modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))

from langchain_openai import AzureChatOpenAI
from langchain_core.messages import HumanMessage, AIMessage, SystemMessage, BaseMessage
from langchain_core.output_parsers import StrOutputParser
from langchain_core.prompts import ChatPromptTemplate

from shared import (
    AgentConfig, AgentMessage, AgentResponse, AgentType, 
    MessageRole, AgentInitializationException, BaseAgent
)


class GroupChatRole(Enum):
    """Roles for agents in group chat."""
    FACILITATOR = "facilitator"
    PARTICIPANT = "participant"
    OBSERVER = "observer"


@dataclass
class GroupChatConfig:
    """Configuration for group chat."""
    name: str = "DefaultGroupChat"
    description: str = ""
    max_turns: int = 10
    termination_keyword: str = "TERMINATE"
    enable_termination_keyword: bool = True
    require_facilitator: bool = True
    response_wait_time: float = 0.5
    auto_select_speaker: bool = True


@dataclass
class GroupChatParticipantInfo:
    """Information about a group chat participant."""
    agent_name: str
    role: GroupChatRole = GroupChatRole.PARTICIPANT
    priority: int = 1
    max_consecutive_turns: int = 3


class EnhancedLangChainAgentGroupChat:
    """Enhanced group chat that uses actual agent instances from registry."""
    
    def __init__(self, config: GroupChatConfig, agent_registry):
        self.config = config
        self.name = config.name
        self.agent_registry = agent_registry
        self.participants: Dict[str, GroupChatParticipantInfo] = {}
        self.conversation_history: List[AgentMessage] = []
        self.logger = logging.getLogger(f"GroupChat.{self.name}")
        self.is_initialized = False
        
        # Conversation state
        self.current_speaker: Optional[str] = None
        self.turn_count = 0
        self.consecutive_turns: Dict[str, int] = {}
        self.conversation_active = False
        
        # LangChain components for intelligent routing
        self.routing_llm: Optional[AzureChatOpenAI] = None
        self.parser = StrOutputParser()
    
    async def initialize(self) -> None:
        """Initialize the group chat and routing components."""
        if self.is_initialized:
            return
            
        try:
            # Initialize routing LLM for speaker selection
            azure_endpoint = os.getenv("AZURE_OPENAI_ENDPOINT")
            api_key = os.getenv("AZURE_OPENAI_API_KEY")
            api_version = os.getenv("AZURE_OPENAI_API_VERSION", "2024-02-01")
            deployment_name = os.getenv("AZURE_OPENAI_DEPLOYMENT_NAME", "gpt-4o-mini")
            
            if azure_endpoint and api_key:
                self.routing_llm = AzureChatOpenAI(
                    azure_endpoint=azure_endpoint,
                    openai_api_key=api_key,
                    openai_api_version=api_version,
                    deployment_name=deployment_name,
                    temperature=0.3,  # Low temperature for routing decisions
                    max_tokens=50,
                )
            
            self.is_initialized = True
            self.logger.info(f"Initialized enhanced group chat: {self.name}")
            
        except Exception as e:
            self.logger.error(f"Failed to initialize group chat: {e}")
            raise AgentInitializationException(f"Failed to initialize group chat: {e}")
    
    async def add_participant(
        self, 
        agent_name: str,
        role: GroupChatRole = GroupChatRole.PARTICIPANT,
        priority: int = 1,
        max_consecutive_turns: int = 3
    ) -> None:
        """Add a participant to the group chat."""
        # Verify agent exists in registry
        agent = self.agent_registry.get_agent(agent_name)
        if not agent:
            raise ValueError(f"Agent {agent_name} not found in registry")
        
        # Store participant info
        self.participants[agent_name] = GroupChatParticipantInfo(
            agent_name=agent_name,
            role=role,
            priority=priority,
            max_consecutive_turns=max_consecutive_turns
        )
        self.consecutive_turns[agent_name] = 0
        
        self.logger.info(f"Added participant: {agent_name} with role: {role.value}")
    
    async def _select_next_speaker(self, message: str, current_speaker: Optional[str] = None) -> str:
        """Select the next speaker based on message content and agent expertise."""
        active_participants = self.get_active_participants()
        
        if not active_participants:
            raise RuntimeError("No active participants available")
        
        # Check consecutive turn limits
        available_participants = [
            name for name in active_participants
            if self.consecutive_turns.get(name, 0) < self.participants[name].max_consecutive_turns
        ]
        
        if not available_participants:
            # Reset consecutive turns
            for name in active_participants:
                self.consecutive_turns[name] = 0
            available_participants = active_participants
        
        # Content-based selection
        message_lower = message.lower()
        
        # Check for people-related queries
        if any(keyword in message_lower for keyword in ['who', 'person', 'people', 'team', 'member', 'employee', 'colleague']):
            people_agents = [name for name in available_participants if 'people' in name.lower()]
            if people_agents:
                self.logger.info(f"Selected {people_agents[0]} for people-related query")
                return people_agents[0]
        
        # Check for knowledge/documentation queries
        if any(keyword in message_lower for keyword in ['what', 'how', 'explain', 'documentation', 'knowledge', 'information', 'guide', 'tutorial']):
            knowledge_agents = [name for name in available_participants if 'knowledge' in name.lower()]
            if knowledge_agents:
                self.logger.info(f"Selected {knowledge_agents[0]} for knowledge query")
                return knowledge_agents[0]
        
        # Try intelligent selection with LLM if available
        if self.routing_llm and len(available_participants) > 1:
            try:
                selected = await self._intelligent_speaker_selection(message, available_participants)
                if selected in available_participants:
                    self.logger.info(f"LLM selected {selected} as next speaker")
                    return selected
            except Exception as e:
                self.logger.warning(f"Intelligent selection failed: {e}")
        
        # Default: rotate or use priority
        if current_speaker in available_participants:
            idx = available_participants.index(current_speaker)
            next_idx = (idx + 1) % len(available_participants)
            return available_participants[next_idx]
        
        # Use highest priority
        return max(available_participants, key=lambda n: self.participants[n].priority)
    
    async def _intelligent_speaker_selection(self, message: str, available_participants: List[str]) -> str:
        """Use LLM to select the best agent for the message."""
        # Get agent descriptions
        agent_descriptions = []
        for name in available_participants:
            agent = self.agent_registry.get_agent(name)
            if agent and hasattr(agent, 'config'):
                agent_descriptions.append(f"{name}: {agent.config.instructions}")
        
        prompt = ChatPromptTemplate.from_messages([
            ("system", """Select the most appropriate agent to respond to the message.
            
Available agents:
{agents}

User message: {message}

Respond with ONLY the agent name.""")
        ])
        
        chain = prompt | self.routing_llm | self.parser
        result = await chain.ainvoke({
            "agents": "\n".join(agent_descriptions),
            "message": message
        })
        
        return result.strip()
    
    def get_active_participants(self) -> List[str]:
        """Get list of participants who can currently speak."""
        return [
            name for name, info in self.participants.items()
            if info.role != GroupChatRole.OBSERVER
        ]
    
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
        current_message = message
        
        # Add user message to history
        user_msg = AgentMessage(
            role=MessageRole.USER,
            content=message,
            metadata={"sender": sender or "User", "group_chat": self.name}
        )
        self.conversation_history.append(user_msg)
        
        try:
            while self.turn_count < self.config.max_turns and self.conversation_active:
                self.turn_count += 1
                
                # Select next speaker
                next_speaker = await self._select_next_speaker(current_message, self.current_speaker)
                self.current_speaker = next_speaker
                
                # Update consecutive turns
                for agent_name in self.consecutive_turns:
                    if agent_name == next_speaker:
                        self.consecutive_turns[agent_name] += 1
                    else:
                        self.consecutive_turns[agent_name] = 0
                
                # Get the actual agent from registry
                agent = self.agent_registry.get_agent(next_speaker)
                if not agent:
                    self.logger.error(f"Agent {next_speaker} not found in registry")
                    continue
                
                # Get response from the actual agent
                agent_response = await agent.process_message(
                    current_message,
                    history=self.conversation_history,
                    metadata={
                        "group_chat": self.name,
                        "turn": self.turn_count,
                        "speaker_role": self.participants[next_speaker].role.value,
                        "total_participants": len(self.participants)
                    }
                )
                
                # Update response with group chat metadata
                agent_response.metadata.update({
                    "turn": self.turn_count,
                    "group_chat": self.name,
                    "speaker_role": self.participants[next_speaker].role.value,
                    "total_participants": len(self.participants)
                })
                
                responses.append(agent_response)
                
                # Add to conversation history
                assistant_msg = AgentMessage(
                    role=MessageRole.ASSISTANT,
                    content=agent_response.content,
                    metadata={
                        "agent": next_speaker,
                        "turn": self.turn_count
                    }
                )
                self.conversation_history.append(assistant_msg)
                
                # Check for termination
                if await self._should_terminate(agent_response.content):
                    self.conversation_active = False
                    self.logger.info(f"Conversation terminated after {self.turn_count} turns")
                    break
                
                # Update current message for next iteration
                current_message = agent_response.content
                
                # Small delay between responses
                await asyncio.sleep(self.config.response_wait_time)
        
        except Exception as e:
            self.logger.error(f"Error in group chat: {e}")
            error_response = AgentResponse(
                content=f"Group chat error: {str(e)}",
                agent_name="system",
                metadata={"error": str(e)}
            )
            responses.append(error_response)
        
        finally:
            self.conversation_active = False
        
        return responses
    
    async def _should_terminate(self, message: str) -> bool:
        """Check if conversation should terminate."""
        if self.turn_count >= self.config.max_turns:
            return True
        
        if self.config.enable_termination_keyword:
            if self.config.termination_keyword.lower() in message.lower():
                return True
        
        return False
    
    def get_conversation_summary(self) -> Dict[str, Any]:
        """Get a summary of the conversation."""
        return {
            "group_chat_name": self.name,
            "total_turns": self.turn_count,
            "participants": list(self.participants.keys()),
            "active_participants": self.get_active_participants(),
            "conversation_active": self.conversation_active,
            "message_count": len(self.conversation_history)
        }