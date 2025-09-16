"""
Example usage of LangChain Agent Group Chat.

This demonstrates how to create and use multi-agent conversations 
using LangChain agents.
"""

import asyncio
import os
import logging
from dotenv import load_dotenv

from agents.agent_group_chat import (
    EnhancedLangChainAgentGroupChat, 
    GroupChatConfig, 
    GroupChatRole,
    GroupChatParticipantInfo
)

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Load environment variables
load_dotenv()


async def basic_group_chat_example():
    """Demonstrate basic group chat functionality."""
    print("=== LangChain Group Chat Example ===\n")
    
    # Create group chat configuration
    config = GroupChatConfig(
        name="Marketing Strategy Session",
        description="AI agents discussing marketing strategies",
        max_turns=6,
        auto_select_speaker=True
    )
    
    # Initialize group chat
    group_chat = LangChainAgentGroupChat(config)
    await group_chat.initialize()
    
    # Add participants
    await group_chat.add_participant(
        name="MarketingManager",
        instructions="You are a marketing manager. Focus on campaign strategies, target audiences, and brand positioning.",
        role=GroupChatRole.FACILITATOR,
        priority=3
    )
    
    await group_chat.add_participant(
        name="DataAnalyst",
        instructions="You are a data analyst. Focus on metrics, analytics, and data-driven insights for marketing decisions.",
        role=GroupChatRole.PARTICIPANT,
        priority=2
    )
    
    await group_chat.add_participant(
        name="CreativeDirector",
        instructions="You are a creative director. Focus on content creation, visual design, and creative campaign ideas.",
        role=GroupChatRole.PARTICIPANT,
        priority=1
    )
    
    print(f"Created group chat with participants: {group_chat.get_participants()}")
    print()
    
    # Start conversation
    message = "We're launching a new eco-friendly product line. How should we approach the marketing campaign to reach environmentally conscious consumers?"
    
    print(f"Initial message: {message}")
    print("\nGroup conversation:")
    print("-" * 50)
    
    responses = await group_chat.send_message(message, sender="CEO")
    
    for i, response in enumerate(responses, 1):
        print(f"\nTurn {i} - {response.agent_name}:")
        print(f"{response.content}")
        print(f"(Role: {response.metadata.get('speaker_role', 'unknown')})")
    
    print("\n" + "=" * 50)
    print(f"Conversation completed after {len(responses)} turns")
    
    # Get AI-generated conversation summary
    summary = await group_chat.generate_conversation_summary()
    print(f"\nAI-Generated Summary:\n{summary}")
    
    await group_chat.cleanup()


async def intelligent_selection_example():
    """Demonstrate intelligent speaker selection."""
    print("\n=== Intelligent Speaker Selection Example ===\n")
    
    config = GroupChatConfig(
        name="Technical Architecture Review",
        description="Expert review of system architecture",
        max_turns=5,
        auto_select_speaker=True  # This enables intelligent selection
    )
    
    group_chat = LangChainAgentGroupChat(config)
    await group_chat.initialize()
    
    # Add participants with specific expertise
    await group_chat.add_participant(
        name="CloudArchitect",
        instructions="You are a cloud architect specializing in AWS and Azure. Focus on scalability, reliability, and cloud best practices.",
        role=GroupChatRole.PARTICIPANT,
        priority=2
    )
    
    await group_chat.add_participant(
        name="SecurityArchitect",
        instructions="You are a security architect. Focus on security threats, compliance, data protection, and secure design patterns.",
        role=GroupChatRole.PARTICIPANT,
        priority=2
    )
    
    await group_chat.add_participant(
        name="DatabaseExpert",
        instructions="You are a database expert. Focus on data modeling, performance optimization, and database architecture decisions.",
        role=GroupChatRole.PARTICIPANT,
        priority=2
    )
    
    await group_chat.add_participant(
        name="SystemsEngineer",
        instructions="You are a systems engineer. Focus on system integration, monitoring, observability, and operational concerns.",
        role=GroupChatRole.PARTICIPANT,
        priority=1
    )
    
    message = "We're designing a high-traffic e-commerce platform that needs to handle 100K concurrent users, process payments securely, and maintain 99.9% uptime. What are the key architectural considerations?"
    
    print(f"Architecture question: {message}")
    print("\nExpert discussion:")
    print("-" * 50)
    
    responses = await group_chat.send_message(message, sender="CTO")
    
    for i, response in enumerate(responses, 1):
        print(f"\nTurn {i} - {response.agent_name}:")
        print(f"{response.content}")
        
        # Show metadata
        meta = response.metadata
        print(f"(Participants: {meta.get('total_participants', 0)}, Role: {meta.get('speaker_role', 'unknown')})")
    
    await group_chat.cleanup()


async def mixed_roles_example():
    """Demonstrate different participant roles."""
    print("\n=== Mixed Roles Example ===\n")
    
    config = GroupChatConfig(
        name="Project Planning Meeting",
        description="Planning a new project with different stakeholders",
        max_turns=4,
        auto_select_speaker=True
    )
    
    group_chat = LangChainAgentGroupChat(config)
    await group_chat.initialize()
    
    # Add participants with different roles
    await group_chat.add_participant(
        name="ProjectManager",
        instructions="You are a project manager. Keep discussions focused, track timelines, and ensure deliverables are clear.",
        role=GroupChatRole.FACILITATOR,
        priority=3
    )
    
    await group_chat.add_participant(
        name="TechLead",
        instructions="You are a technical lead. Assess technical feasibility and provide implementation guidance.",
        role=GroupChatRole.PARTICIPANT,
        priority=2
    )
    
    await group_chat.add_participant(
        name="BusinessAnalyst",
        instructions="You are a business analyst. Focus on requirements gathering and business value assessment.",
        role=GroupChatRole.PARTICIPANT,
        priority=2
    )
    
    await group_chat.add_participant(
        name="Observer",
        instructions="You are observing this meeting to take notes and provide occasional insights.",
        role=GroupChatRole.OBSERVER,  # Observer role
        priority=1
    )
    
    message = "We need to build a customer feedback system that integrates with our existing CRM. The deadline is 3 months. Let's discuss the approach."
    
    print(f"Project discussion: {message}")
    print("\nPlanning meeting:")
    print("-" * 50)
    
    responses = await group_chat.send_message(message, sender="Stakeholder")
    
    for i, response in enumerate(responses, 1):
        print(f"\nTurn {i} - {response.agent_name}:")
        print(f"{response.content}")
    
    # Show conversation transcript
    transcript = await group_chat.get_conversation_summary()
    print(f"\nMeeting Transcript:\n{transcript}")
    
    await group_chat.cleanup()


async def error_handling_example():
    """Demonstrate error handling and recovery."""
    print("\n=== Error Handling Example ===\n")
    
    config = GroupChatConfig(
        name="Resilient Chat",
        description="Testing error recovery",
        max_turns=2,
        auto_select_speaker=False  # Disable auto-selection for simpler testing
    )
    
    group_chat = LangChainAgentGroupChat(config)
    
    try:
        await group_chat.initialize()
        
        # Add one participant
        await group_chat.add_participant(
            name="Assistant",
            instructions="You are a helpful assistant. Provide brief, helpful responses.",
            role=GroupChatRole.PARTICIPANT
        )
        
        print("Testing basic functionality...")
        
        responses = await group_chat.send_message("Hello, how are you?", sender="User")
        
        for response in responses:
            print(f"{response.agent_name}: {response.content}")
        
        print("✓ Basic functionality works")
        
        # Test conversation reset
        await group_chat.reset_conversation()
        print("✓ Conversation reset works")
        
        # Test participant management
        success = await group_chat.remove_participant("Assistant")
        print(f"✓ Participant removal: {success}")
        
        await group_chat.cleanup()
        print("✓ Cleanup completed")
        
    except Exception as e:
        logger.error(f"Error in testing: {e}")
        print(f"Error during testing: {e}")


async def main():
    """Run all examples."""
    try:
        await basic_group_chat_example()
        await intelligent_selection_example()
        await mixed_roles_example()
        await error_handling_example()
        
        print("\n=== All LangChain examples completed successfully! ===")
        
    except Exception as e:
        logger.error(f"Error running examples: {e}")
        print(f"Error: {e}")
        print("\nMake sure you have set the required environment variables:")
        print("- AZURE_INFERENCE_ENDPOINT")
        print("- AZURE_INFERENCE_CREDENTIAL (optional)")
        print("- GENERIC_MODEL (optional, defaults to gpt-4o-mini)")


if __name__ == "__main__":
    asyncio.run(main())