"""LangChain-based Agent Group Chat implementation."""

import asyncio
import os
import sys
from typing import Any, Dict, List, Optional, Set, Tuple, Union
from dataclasses import dataclass, field
from enum import Enum
import logging

# Add the parent directory to the Python path to import shared modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..'))

from langchain_azure_ai.chat_models import AzureAIChatCompletionsModel
from langchain_core.messages import HumanMessage, AIMessage, SystemMessage, BaseMessage
from langchain_core.output_parsers import StrOutputParser
from langchain_core.prompts import ChatPromptTemplate

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
    agent: 'LangChainAgent'
    role: GroupChatRole = GroupChatRole.PARTICIPANT
    can_initiate: bool = True
    max_consecutive_turns: int = 3
    priority: int = 1  # Higher number = higher priority


@dataclass
class LangChainAgent:
    """Wrapper for LangChain agent in group chat context."""
    name: str
    instructions: str
    llm: AzureAIChatCompletionsModel
    role: GroupChatRole = GroupChatRole.PARTICIPANT
    can_initiate: bool = True
    max_consecutive_turns: int = 3
    priority: int = 1  # Higher number = higher priority
    
    def get_system_message(self) -> str:
        """Get the system message for this agent."""
        role_context = ""
        if self.role == GroupChatRole.FACILITATOR:
            role_context = " You are acting as a facilitator in this group discussion."
        elif self.role == GroupChatRole.OBSERVER:
            role_context = " You are observing this group discussion."
        
        return f"{self.instructions}{role_context}"


class LangChainAgentGroupChat:
    """Group chat implementation using LangChain agents."""
    
    def __init__(self, config: GroupChatConfig):
        self.config = config
        self.name = config.name
        self.participants: Dict[str, LangChainAgent] = {}
        self.conversation_history: List[BaseMessage] = []
        self.logger = logging.getLogger(f"GroupChat.{self.name}")
        self.is_initialized = False
        
        # Conversation state
        self.current_speaker: Optional[str] = None
        self.turn_count = 0
        self.consecutive_turns: Dict[str, int] = {}
        self.conversation_active = False
        
        # LangChain components
        self.llm: Optional[AzureAIChatCompletionsModel] = None
        self.parser = StrOutputParser()
    
    async def initialize(self) -> None:
        """Initialize the group chat and LangChain components."""
        if self.is_initialized:
            return
            
        try:
            # Initialize LangChain model
            endpoint = os.getenv("AZURE_INFERENCE_ENDPOINT")
            credential = os.getenv("AZURE_INFERENCE_CREDENTIAL")
            model = os.getenv("GENERIC_MODEL", "gpt-4o-mini")
            
            if not endpoint:
                raise AgentInitializationException("AZURE_INFERENCE_ENDPOINT is required")
            
            self.llm = AzureAIChatCompletionsModel(
                endpoint=endpoint,
                credential=credential,
                model=model,
            )
            
            self.is_initialized = True
            self.logger.info(f"Initialized LangChain group chat: {self.name}")
            
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
            # Create LangChain agent wrapper
            agent = LangChainAgent(
                name=name,
                instructions=instructions,
                llm=self.llm,
                role=role,
                can_initiate=kwargs.get('can_initiate', True),
                max_consecutive_turns=kwargs.get('max_consecutive_turns', 3),
                priority=kwargs.get('priority', 1)
            )
            
            self.participants[name] = agent
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
            name for name, agent in self.participants.items()
            if agent.role != GroupChatRole.OBSERVER
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
        # In a more sophisticated implementation, you could use an LLM to select based on expertise
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
    
    async def _intelligent_speaker_selection(self, message: str, conversation_context: str) -> str:
        """Use LLM to intelligently select the next speaker based on content."""
        active_participants = self.get_active_participants()
        
        if len(active_participants) <= 1:
            return active_participants[0] if active_participants else ""
        
        # Create participant descriptions
        participant_descriptions = []
        for name in active_participants:
            agent = self.participants[name]
            participant_descriptions.append(f"- {name}: {agent.instructions[:100]}...")
        
        # Create selection prompt
        selection_prompt = ChatPromptTemplate.from_messages([
            ("system", """You are a conversation moderator. Given the current message and conversation context, 
            select the most appropriate participant to respond next. Consider their expertise and the conversation flow.
            
            Available participants:
            {participants}
            
            Recent conversation context:
            {context}
            
            Current message: {message}
            
            Respond with ONLY the participant name, nothing else."""),
        ])
        
        try:
            chain = selection_prompt | self.llm | self.parser
            selected = await chain.ainvoke({
                "participants": "\n".join(participant_descriptions),
                "context": conversation_context[-500:],  # Last 500 chars
                "message": message
            })
            
            selected = selected.strip()
            if selected in active_participants:
                return selected
            
        except Exception as e:
            self.logger.warning(f"Error in intelligent speaker selection: {e}")
        
        # Fallback to simple selection
        return await self._select_next_speaker(message, self.current_speaker)
    
    def _build_conversation_context(self) -> str:
        """Build a conversation context string from recent history."""
        if not self.conversation_history:
            return ""
        
        context_messages = []
        for msg in self.conversation_history[-6:]:  # Last 6 messages
            if isinstance(msg, HumanMessage):
                speaker = getattr(msg, 'name', 'User')
                context_messages.append(f"{speaker}: {msg.content}")
            elif isinstance(msg, AIMessage):
                speaker = getattr(msg, 'name', 'Assistant')
                context_messages.append(f"{speaker}: {msg.content}")
        
        return "\n".join(context_messages)
    
    async def _should_terminate(self, message: str) -> bool:
        """Check if the conversation should terminate."""
        if not self.config.enable_termination_keyword:
            return False
        
        if self.turn_count >= self.config.max_turns:
            return True
        
        if self.config.termination_keyword.lower() in message.lower():
            return True
        
        return False
    
    async def _get_agent_response(self, agent: LangChainAgent, message: str) -> str:
        """Get response from a specific agent."""
        # Build message chain with agent's system message and conversation history
        messages = [SystemMessage(content=agent.get_system_message())]
        
        # Add recent conversation history
        messages.extend(self.conversation_history[-10:])  # Last 10 messages for context
        
        # Add current message
        messages.append(HumanMessage(content=message, name="GroupChat"))
        
        # Get response
        response = await agent.llm.ainvoke(messages)
        
        if isinstance(response, AIMessage):
            return response.content or ""
        else:
            return str(response)
    
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
            initial_msg = HumanMessage(content=message, name=sender or "User")
            self.conversation_history.append(initial_msg)
            
            current_message = message
            conversation_context = self._build_conversation_context()
            
            while self.conversation_active and self.turn_count < self.config.max_turns:
                # Select next speaker - use intelligent selection if enabled
                if self.config.auto_select_speaker and len(self.get_active_participants()) > 2:
                    next_speaker = await self._intelligent_speaker_selection(
                        current_message, conversation_context
                    )
                else:
                    next_speaker = await self._select_next_speaker(current_message, self.current_speaker)
                
                agent = self.participants[next_speaker]
                
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
                    response_content = await self._get_agent_response(agent, current_message)
                    
                    if not response_content:
                        response_content = "I don't have a response at this time."
                    
                    # Add response to history
                    ai_message = AIMessage(content=response_content, name=next_speaker)
                    self.conversation_history.append(ai_message)
                    
                    # Create response object
                    response = AgentResponse(
                        content=response_content,
                        agent_name=next_speaker,
                        metadata={
                            **(metadata or {}),
                            "turn": self.turn_count,
                            "group_chat": self.name,
                            "speaker_role": agent.role.value,
                            "total_participants": len(self.participants)
                        }
                    )
                    
                    responses.append(response)
                    
                    # Check for termination
                    if await self._should_terminate(response_content):
                        self.conversation_active = False
                        self.logger.info(f"Conversation terminated after {self.turn_count} turns")
                        break
                    
                    current_message = response_content
                    conversation_context = self._build_conversation_context()
                    
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
        self.conversation_history = []
        self.current_speaker = None
        self.turn_count = 0
        self.consecutive_turns = {name: 0 for name in self.participants}
        self.conversation_active = False
        self.logger.info("Conversation state reset")
    
    async def get_conversation_summary(self) -> str:
        """Get a summary of the conversation."""
        if not self.conversation_history:
            return "No conversation yet."
        
        messages = []
        for msg in self.conversation_history:
            if isinstance(msg, HumanMessage):
                speaker = getattr(msg, 'name', 'User')
                messages.append(f"{speaker}: {msg.content}")
            elif isinstance(msg, AIMessage):
                speaker = getattr(msg, 'name', 'Assistant')
                messages.append(f"{speaker}: {msg.content}")
            elif isinstance(msg, SystemMessage):
                messages.append(f"System: {msg.content}")
        
        return "\n".join(messages)
    
    async def generate_conversation_summary(self) -> str:
        """Generate an AI-powered summary of the conversation."""
        if not self.conversation_history:
            return "No conversation to summarize."
        
        conversation_text = await self.get_conversation_summary()
        
        summary_prompt = ChatPromptTemplate.from_messages([
            ("system", """You are a conversation summarizer. Provide a concise summary of the group conversation, 
            highlighting key points, decisions made, and action items discussed. Keep it under 200 words."""),
            ("human", "Summarize this group conversation:\n\n{conversation}")
        ])
        
        try:
            chain = summary_prompt | self.llm | self.parser
            summary = await chain.ainvoke({"conversation": conversation_text})
            return summary.strip()
        except Exception as e:
            self.logger.error(f"Error generating summary: {e}")
            return f"Error generating summary: {e}"
    
    async def cleanup(self) -> None:
        """Cleanup resources."""
        self.participants.clear()
        self.conversation_history = []
        self.conversation_active = False
        self.logger.info(f"Cleaned up group chat: {self.name}")


async def create_example_group_chat() -> LangChainAgentGroupChat:
    """Create an example group chat with sample agents."""
    config = GroupChatConfig(
        name="Example LangChain Group Chat",
        description="A sample group chat with multiple AI agents using LangChain",
        max_turns=8,
        auto_select_speaker=True
    )
    
    group_chat = LangChainAgentGroupChat(config)
    await group_chat.initialize()
    
    # Add sample participants
    await group_chat.add_participant(
        name="Analyst",
        instructions="You are a data analyst. Provide analytical insights and ask clarifying questions about data. Focus on quantitative analysis and data-driven recommendations.",
        role=GroupChatRole.PARTICIPANT,
        priority=2
    )
    
    await group_chat.add_participant(
        name="Researcher", 
        instructions="You are a researcher. Provide research-based information and suggest areas for investigation. Focus on evidence-based insights and thorough investigation.",
        role=GroupChatRole.PARTICIPANT,
        priority=1
    )
    
    await group_chat.add_participant(
        name="Facilitator",
        instructions="You are a meeting facilitator. Keep the discussion on track, summarize key points, and ensure all participants contribute meaningfully.",
        role=GroupChatRole.FACILITATOR,
        priority=3
    )
    
    await group_chat.add_participant(
        name="Strategist",
        instructions="You are a strategic advisor. Think about long-term implications, identify risks and opportunities, and provide strategic recommendations.",
        role=GroupChatRole.PARTICIPANT,
        priority=2
    )
    
    return group_chat