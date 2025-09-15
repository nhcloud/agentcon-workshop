"""Modern FastAPI application for Semantic Kernel agents."""

import os
import uuid
import time
import logging
from contextlib import asynccontextmanager
from typing import Optional, Dict, Any, List

from fastapi import FastAPI, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import StreamingResponse
from pydantic import BaseModel
from dotenv import load_dotenv

from shared import (
    AgentRegistry, AgentConfig, AgentMessage, AgentResponse, MessageRole,
    YamlConfigManager, ConfigFactory, SessionManagerFactory, MessageCache,
    PatternRouter, HistoryAwareRouter, setup_logging, HealthChecker
)
from group_chat_config import get_config_loader, GroupChatConfigLoader

from agents.semantic_kernel_agents import SemanticKernelAgentFactory, SEMANTIC_KERNEL_AGENT_CONFIGS
from routers.semantic_kernel_router import HybridSemanticKernelRouter

# Load environment variables
load_dotenv()

# Setup logging
setup_logging(os.getenv("LOG_LEVEL", "INFO"))
logger = logging.getLogger(__name__)

# Global components
agent_registry: Optional[AgentRegistry] = None
session_manager = None
router = None
message_cache: Optional[MessageCache] = None
health_checker: Optional[HealthChecker] = None


async def initialize_system():
    """Initialize the agent system."""
    global agent_registry, session_manager, router, message_cache, health_checker
    
    logger.info("Initializing Semantic Kernel Agent System...")
    
    # Initialize core components
    agent_registry = AgentRegistry()
    session_manager = SessionManagerFactory.create_default_manager()
    message_cache = MessageCache()
    
    # Load configuration
    config_path = os.getenv("CONFIG_PATH", "config.yml")
    try:
        config_manager = ConfigFactory.create_hybrid_config(config_path)
        agent_configs = config_manager.get_agent_configs()
    except Exception as e:
        logger.warning(f"Could not load config file: {e}, using defaults")
        agent_configs = SEMANTIC_KERNEL_AGENT_CONFIGS
    
    # Register agent factory
    factory = SemanticKernelAgentFactory()
    agent_registry.register_factory(factory)
    
    # Register agents
    for config in agent_configs:
        try:
            await agent_registry.register_agent(config)
            logger.info(f"Registered agent: {config.name}")
        except Exception as e:
            logger.error(f"Failed to register agent {config.name}: {e}")
    
    # Initialize router
    router = HybridSemanticKernelRouter(fallback_to_sk=True)
    
    # Initialize health checker
    health_checker = HealthChecker(agent_registry, session_manager)
    
    logger.info("Semantic Kernel Agent System initialized successfully")


async def cleanup_system():
    """Cleanup system resources."""
    global agent_registry, session_manager
    
    logger.info("Cleaning up Semantic Kernel Agent System...")
    
    if agent_registry:
        agents = agent_registry.get_all_agents()
        for agent_name in agents:
            await agent_registry.unregister_agent(agent_name)
    
    if hasattr(session_manager, 'cleanup'):
        await session_manager.cleanup()
    
    logger.info("Cleanup completed")


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Manage application lifespan."""
    await initialize_system()
    yield
    await cleanup_system()


# Create FastAPI app
app = FastAPI(
    title="Semantic Kernel AI Agent System",
    description="Modern multi-agent system built with Semantic Kernel",
    version="2.0.0",
    lifespan=lifespan
)

# Configure CORS
frontend_url = os.getenv("FRONTEND_URL", "*")
app.add_middleware(
    CORSMiddleware,
    allow_origins=[frontend_url] if frontend_url != "*" else ["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Pydantic models
class ChatRequest(BaseModel):
    message: str
    agent: Optional[str] = None
    session_id: Optional[str] = None
    metadata: Optional[Dict[str, Any]] = None


class ChatResponse(BaseModel):
    content: str
    agent: str
    usage: Optional[Dict[str, Any]] = None
    session_id: str
    message_id: str
    metadata: Optional[Dict[str, Any]] = None


class AgentInfo(BaseModel):
    name: str
    type: str
    available: bool
    capabilities: list
    provider: Optional[str] = None


@app.get("/health")
async def health_check():
    """Health check endpoint."""
    if health_checker:
        return await health_checker.check_system_health()
    return {"status": "unknown"}


@app.get("/agents", response_model=Dict[str, Any])
async def list_agents():
    """List all available agents with detailed information."""
    if not agent_registry:
        raise HTTPException(500, "System not initialized")
    
    agents = agent_registry.get_all_agents()
    agent_list = []
    
    for name, agent in agents.items():
        provider = getattr(agent.config, 'framework_config', {}).get('provider', 'unknown')
        
        agent_info = AgentInfo(
            name=name,
            type=agent.agent_type.value,
            available=agent.is_available,
            capabilities=agent.get_capabilities(),
            provider=provider
        )
        agent_list.append(agent_info.dict())
    
    return {
        "agents": agent_list,
        "total": len(agent_list),
        "available": len([a for a in agent_list if a["available"]])
    }


@app.post("/chat", response_model=ChatResponse)
async def chat(request: ChatRequest):
    """Process a chat message."""
    if not all([agent_registry, session_manager, router]):
        raise HTTPException(500, "System not initialized")
    
    # Generate session ID if not provided
    session_id = request.session_id or str(uuid.uuid4())
    
    try:
        # Check cache first
        if message_cache:
            cached_response = message_cache.get(request.message, request.agent or "auto", session_id)
            if cached_response:
                logger.debug(f"Cache hit for session {session_id}")
                return ChatResponse(
                    content=cached_response.content,
                    agent=cached_response.agent_name,
                    usage=cached_response.usage,
                    session_id=session_id,
                    message_id=cached_response.message_id,
                    metadata=cached_response.metadata
                )
        
        # Get session and message history
        await session_manager.get_session(session_id)
        history = await session_manager.get_messages(session_id)
        
        # Add user message to session
        user_message = AgentMessage(
            role=MessageRole.USER,
            content=request.message,
            metadata=request.metadata or {}
        )
        await session_manager.add_message(session_id, user_message)
        
        # Route to appropriate agent
        if request.agent:
            # Forced agent
            agent = agent_registry.get_agent(request.agent)
            if not agent:
                raise HTTPException(404, f"Agent '{request.agent}' not found")
            if not agent.is_available:
                raise HTTPException(503, f"Agent '{request.agent}' is not available")
            selected_agent_name = request.agent
        else:
            # Auto-route
            available_agents = agent_registry.get_available_agents()
            if not available_agents:
                raise HTTPException(503, "No agents available")
            
            selected_agent_name = await router.route_message(
                request.message, 
                available_agents, 
                history, 
                request.metadata
            )
            agent = agent_registry.get_agent(selected_agent_name)
        
        # Process message with selected agent
        response = await agent.process_message(
            request.message,
            history,
            request.metadata
        )
        
        # Add assistant message to session
        assistant_message = AgentMessage(
            role=MessageRole.ASSISTANT,
            content=response.content,
            agent_name=selected_agent_name,
            metadata=response.metadata
        )
        await session_manager.add_message(session_id, assistant_message)
        
        # Cache the response
        if message_cache:
            message_cache.set(request.message, selected_agent_name, session_id, response)
        
        logger.debug(f"Processed message for session {session_id} with agent {selected_agent_name}")
        
        return ChatResponse(
            content=response.content,
            agent=selected_agent_name,
            usage=response.usage,
            session_id=session_id,
            message_id=response.message_id,
            metadata=response.metadata
        )
        
    except Exception as e:
        logger.error(f"Error processing chat message: {e}")
        raise HTTPException(500, f"Internal server error: {str(e)}")


@app.post("/chat/stream")
async def chat_stream(request: Request):
    """Stream chat responses."""
    # Parse query parameters
    params = request.query_params
    message = params.get("message", "")
    forced_agent = params.get("agent")
    session_id = params.get("session_id") or str(uuid.uuid4())
    
    if not message:
        raise HTTPException(400, "Message parameter required")
    
    async def generate_stream():
        try:
            start_time = time.time()
            
            # Create chat request
            chat_request = ChatRequest(
                message=message,
                agent=forced_agent,
                session_id=session_id
            )
            
            # Process the message (reuse the chat endpoint logic)
            response = await chat(chat_request)
            
            # Calculate latency
            latency_ms = int((time.time() - start_time) * 1000)
            
            # Stream the response
            payload = {
                "session_id": session_id,
                "agent": response.agent,
                "chunk": response.content,
                "tokens": None,
                "latency": latency_ms,
                "message_id": response.message_id,
                "metadata": response.metadata
            }
            
            yield f"data: {payload}\n\n"
            
        except Exception as e:
            error_payload = {
                "session_id": session_id,
                "error": str(e)
            }
            yield f"data: {error_payload}\n\n"
    
    return StreamingResponse(
        generate_stream(), 
        media_type="text/event-stream",
        headers={"Cache-Control": "no-cache"}
    )


@app.get("/messages/{session_id}")
async def get_messages(session_id: str):
    """Get all messages for a session."""
    if not session_manager:
        raise HTTPException(500, "System not initialized")
    
    try:
        messages = await session_manager.get_messages(session_id)
        return {
            "session_id": session_id,
            "messages": [msg.to_dict() for msg in messages],
            "count": len(messages)
        }
    except Exception as e:
        raise HTTPException(404, f"Session not found: {e}")


@app.delete("/messages/{session_id}")
async def delete_session(session_id: str):
    """Delete a session and its messages."""
    if not session_manager:
        raise HTTPException(500, "System not initialized")
    
    try:
        await session_manager.delete_session(session_id)
        
        # Clear cache for this session
        if message_cache:
            message_cache.clear()  # Simple approach - clear all cache
        
        return {"message": f"Session {session_id} deleted successfully"}
        
    except Exception as e:
        raise HTTPException(500, f"Error deleting session: {e}")


@app.get("/agents/{agent_name}/info")
async def get_agent_info(agent_name: str):
    """Get detailed information about a specific agent."""
    if not agent_registry:
        raise HTTPException(500, "System not initialized")
    
    agent = agent_registry.get_agent(agent_name)
    if not agent:
        raise HTTPException(404, f"Agent '{agent_name}' not found")
    
    return {
        "name": agent.name,
        "type": agent.agent_type.value,
        "available": agent.is_available,
        "enabled": agent.enabled,
        "capabilities": agent.get_capabilities(),
        "instructions": agent.config.instructions,
        "provider": agent.config.framework_config.get('provider', 'unknown'),
        "metadata": agent.config.metadata
    }


@app.post("/agents/{agent_name}/toggle")
async def toggle_agent(agent_name: str):
    """Enable or disable an agent."""
    if not agent_registry:
        raise HTTPException(500, "System not initialized")
    
    agent = agent_registry.get_agent(agent_name)
    if not agent:
        raise HTTPException(404, f"Agent '{agent_name}' not found")
    
    # Toggle the enabled state
    agent.enabled = not agent.enabled
    
    return {
        "agent": agent_name,
        "enabled": agent.enabled,
        "available": agent.is_available
    }


@app.get("/system/stats")
async def get_system_stats():
    """Get system statistics."""
    if not all([agent_registry, session_manager]):
        raise HTTPException(500, "System not initialized")
    
    agents = agent_registry.get_all_agents()
    
    stats = {
        "agents": {
            "total": len(agents),
            "available": len([a for a in agents.values() if a.is_available]),
            "by_type": {}
        },
        "cache": {
            "enabled": message_cache is not None,
            "size": len(message_cache._cache) if message_cache else 0
        },
        "system": {
            "framework": "Semantic Kernel",
            "version": "2.0.0"
        }
    }
    
    # Count agents by type
    for agent in agents.values():
        agent_type = agent.agent_type.value
        if agent_type not in stats["agents"]["by_type"]:
            stats["agents"]["by_type"][agent_type] = 0
        stats["agents"]["by_type"][agent_type] += 1
    
    return stats


# Group Chat functionality
from agents.agent_group_chat import (
    SemanticKernelAgentGroupChat, 
    GroupChatConfig, 
    GroupChatRole,
    GroupChatParticipant
)

class GroupChatRequest(BaseModel):
    message: str
    session_id: Optional[str] = None
    config: Optional[Dict[str, Any]] = None
    participants: Optional[List[Dict[str, Any]]] = None
    sender: Optional[str] = None
    metadata: Optional[Dict[str, Any]] = None


class GroupChatResponse(BaseModel):
    responses: List[ChatResponse]
    conversation_id: str
    total_turns: int
    active_participants: List[str]
    metadata: Optional[Dict[str, Any]] = None


class GroupChatConfigRequest(BaseModel):
    name: str
    description: Optional[str] = ""
    max_turns: Optional[int] = 10
    auto_select_speaker: Optional[bool] = True
    participants: List[Dict[str, Any]]


class ParticipantConfig(BaseModel):
    name: str
    instructions: str
    role: Optional[str] = "participant"
    priority: Optional[int] = 1
    max_consecutive_turns: Optional[int] = 3


# Store group chats by session
GROUP_CHATS: Dict[str, SemanticKernelAgentGroupChat] = {}


@app.post("/group-chat", response_model=GroupChatResponse)
async def group_chat_endpoint(request: GroupChatRequest):
    """Start or continue a group chat conversation."""
    if not all([agent_registry]):
        raise HTTPException(500, "System not initialized")
    
    try:
        session_id = request.session_id or str(uuid.uuid4())
        
        # Get or create group chat
        if session_id not in GROUP_CHATS:
            # Create new group chat
            config = GroupChatConfig(
                name=f"GroupChat-{session_id[:8]}",
                description=request.config.get("description", "") if request.config else "",
                max_turns=request.config.get("max_turns", 6) if request.config else 6,
                auto_select_speaker=request.config.get("auto_select_speaker", True) if request.config else True
            )
            
            group_chat = SemanticKernelAgentGroupChat(config)
            await group_chat.initialize()
            
            # Add participants from request or use defaults
            if request.participants:
                for participant in request.participants:
                    await group_chat.add_participant(
                        name=participant["name"],
                        instructions=participant["instructions"],
                        role=GroupChatRole(participant.get("role", "participant")),
                        priority=participant.get("priority", 1),
                        max_consecutive_turns=participant.get("max_consecutive_turns", 3)
                    )
            else:
                # Add default participants from registered agents
                available_agents = agent_registry.get_available_agents()
                for agent_name in available_agents[:3]:  # Limit to 3 for demo
                    agent = agent_registry.get_agent(agent_name)
                    await group_chat.add_participant(
                        name=agent_name,
                        instructions=f"You are {agent_name}. Provide helpful responses based on your expertise.",
                        role=GroupChatRole.PARTICIPANT,
                        priority=1
                    )
            
            GROUP_CHATS[session_id] = group_chat
        
        group_chat = GROUP_CHATS[session_id]
        
        # Send message to group chat
        agent_responses = await group_chat.send_message(
            request.message,
            sender=request.sender or "User",
            metadata=request.metadata
        )
        
        # Convert to API response format
        responses = []
        for agent_response in agent_responses:
            responses.append(ChatResponse(
                content=agent_response.content,
                agent=agent_response.agent_name,
                usage=agent_response.usage,
                session_id=session_id,
                message_id=agent_response.message_id,
                metadata=agent_response.metadata
            ))
        
        return GroupChatResponse(
            responses=responses,
            conversation_id=session_id,
            total_turns=group_chat.turn_count,
            active_participants=group_chat.get_active_participants(),
            metadata={
                "group_chat_name": group_chat.name,
                "max_turns": group_chat.config.max_turns,
                "conversation_active": group_chat.conversation_active
            }
        )
    
    except Exception as e:
        logger.error(f"Error in group chat: {e}")
        raise HTTPException(500, f"Group chat error: {str(e)}")


@app.post("/group-chat/create", response_model=Dict[str, Any])
async def create_group_chat(request: GroupChatConfigRequest):
    """Create a new group chat with specific configuration."""
    try:
        session_id = str(uuid.uuid4())
        
        config = GroupChatConfig(
            name=request.name,
            description=request.description,
            max_turns=request.max_turns,
            auto_select_speaker=request.auto_select_speaker
        )
        
        group_chat = SemanticKernelAgentGroupChat(config)
        await group_chat.initialize()
        
        # Add participants
        for participant in request.participants:
            role = GroupChatRole.PARTICIPANT
            if participant.get("role"):
                role = GroupChatRole(participant["role"])
            
            await group_chat.add_participant(
                name=participant["name"],
                instructions=participant["instructions"],
                role=role,
                priority=participant.get("priority", 1),
                max_consecutive_turns=participant.get("max_consecutive_turns", 3)
            )
        
        GROUP_CHATS[session_id] = group_chat
        
        return {
            "session_id": session_id,
            "name": request.name,
            "participants": group_chat.get_participants(),
            "config": {
                "max_turns": config.max_turns,
                "auto_select_speaker": config.auto_select_speaker
            }
        }
    
    except Exception as e:
        logger.error(f"Error creating group chat: {e}")
        raise HTTPException(500, f"Failed to create group chat: {str(e)}")


@app.get("/group-chat/{session_id}/summary")
async def get_group_chat_summary(session_id: str):
    """Get conversation summary for a group chat."""
    if session_id not in GROUP_CHATS:
        raise HTTPException(404, "Group chat not found")
    
    try:
        group_chat = GROUP_CHATS[session_id]
        summary = await group_chat.get_conversation_summary()
        
        return {
            "session_id": session_id,
            "summary": summary,
            "total_turns": group_chat.turn_count,
            "participants": group_chat.get_participants(),
            "active": group_chat.conversation_active
        }
    
    except Exception as e:
        logger.error(f"Error getting group chat summary: {e}")
        raise HTTPException(500, f"Failed to get summary: {str(e)}")


@app.post("/group-chat/{session_id}/reset")
async def reset_group_chat(session_id: str):
    """Reset a group chat conversation."""
    if session_id not in GROUP_CHATS:
        raise HTTPException(404, "Group chat not found")
    
    try:
        group_chat = GROUP_CHATS[session_id]
        await group_chat.reset_conversation()
        
        return {
            "session_id": session_id,
            "status": "reset",
            "participants": group_chat.get_participants()
        }
    
    except Exception as e:
        logger.error(f"Error resetting group chat: {e}")
        raise HTTPException(500, f"Failed to reset group chat: {str(e)}")


@app.delete("/group-chat/{session_id}")
async def delete_group_chat(session_id: str):
    """Delete a group chat session."""
    if session_id not in GROUP_CHATS:
        raise HTTPException(404, "Group chat not found")
    
    try:
        group_chat = GROUP_CHATS[session_id]
        await group_chat.cleanup()
        del GROUP_CHATS[session_id]
        
        return {"session_id": session_id, "status": "deleted"}
    
    except Exception as e:
        logger.error(f"Error deleting group chat: {e}")
        raise HTTPException(500, f"Failed to delete group chat: {str(e)}")


@app.get("/group-chats")
async def list_group_chats():
    """List all active group chats."""
    chats = []
    for session_id, group_chat in GROUP_CHATS.items():
        chats.append({
            "session_id": session_id,
            "name": group_chat.name,
            "participants": group_chat.get_participants(),
            "turn_count": group_chat.turn_count,
            "active": group_chat.conversation_active
        })
    
    return {"group_chats": chats, "total": len(chats)}


@app.get("/group-chat/templates")
async def list_group_chat_templates():
    """List all available group chat templates."""
    try:
        config_loader = get_config_loader()
        templates = config_loader.list_available_templates()
        
        template_info = []
        for template_name in templates:
            info = config_loader.get_template_info(template_name)
            if info:
                template_info.append({
                    "id": template_name,
                    **info
                })
        
        return {"templates": template_info, "total": len(template_info)}
    
    except Exception as e:
        logger.error(f"Error listing templates: {e}")
        raise HTTPException(500, f"Failed to list templates: {str(e)}")


@app.get("/group-chat/templates/{template_name}")
async def get_group_chat_template(template_name: str):
    """Get detailed information about a specific template."""
    try:
        config_loader = get_config_loader()
        template_info = config_loader.get_template_info(template_name)
        
        if not template_info:
            raise HTTPException(404, f"Template '{template_name}' not found")
        
        # Get detailed participant info
        participants = config_loader.get_template_participants(template_name)
        template_info["participants_detail"] = participants
        
        return template_info
    
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error getting template {template_name}: {e}")
        raise HTTPException(500, f"Failed to get template: {str(e)}")


@app.post("/group-chat/from-template", response_model=Dict[str, Any])
async def create_group_chat_from_template(request: Dict[str, Any]):
    """Create a group chat from a predefined template."""
    template_name = request.get("template_name")
    if not template_name:
        raise HTTPException(400, "Template name is required")
    
    try:
        config_loader = get_config_loader()
        
        # Get template configuration
        group_chat_config = config_loader.create_group_chat_config(template_name)
        if not group_chat_config:
            raise HTTPException(404, f"Template '{template_name}' not found")
        
        participants_config = config_loader.get_template_participants(template_name)
        if not participants_config:
            raise HTTPException(400, f"Template '{template_name}' has no participants")
        
        # Create session ID
        session_id = str(uuid.uuid4())
        
        # Create agent group chat
        group_chat = SemanticKernelAgentGroupChat(config=group_chat_config)
        
        # Add participants from template
        for participant_config in participants_config:
            # Create agent with template instructions
            agent_config = AgentConfig(
                agent_type="group_chat_participant",
                instructions=participant_config["instructions"],
                model_config=group_chat.kernel.service_configs[0] if group_chat.kernel.service_configs else None
            )
            
            participant = GroupChatParticipant(
                name=participant_config["name"],
                agent=await agent_registry.create_agent(agent_config),
                role=GroupChatRole(participant_config["role"]),
                priority=participant_config["priority"],
                max_consecutive_turns=participant_config["max_consecutive_turns"]
            )
            
            group_chat.add_participant(participant)
        
        # Store the group chat
        GROUP_CHATS[session_id] = group_chat
        
        return {
            "session_id": session_id,
            "template_name": template_name,
            "name": group_chat_config.name,
            "description": group_chat_config.description,
            "participants": group_chat.get_participants(),
            "status": "created"
        }
    
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error creating group chat from template: {e}")
        raise HTTPException(500, f"Failed to create group chat from template: {str(e)}")


if __name__ == "__main__":
    import uvicorn
    
    port = int(os.getenv("PORT", "8001"))  # Different default port from LC
    host = os.getenv("HOST", "0.0.0.0")
    
    uvicorn.run(
        "main:app",
        host=host,
        port=port,
        reload=os.getenv("ENVIRONMENT") == "development",
        log_level=os.getenv("LOG_LEVEL", "info").lower()
    )