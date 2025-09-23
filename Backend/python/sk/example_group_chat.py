"""
Example usage of Semantic Kernel Agent Group Chat.

This demonstrates how to create and use multi-agent conversations 
using Semantic Kernel agents.
"""

import asyncio
import os
import logging
from dotenv import load_dotenv

from agents.agent_group_chat import (
    SemanticKernelAgentGroupChat, 
    GroupChatConfig, 
    GroupChatRole,
    create_example_group_chat
)

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Load environment variables
load_dotenv()


async def basic_group_chat_example():
    """Demonstrate basic group chat functionality."""
    print("=== Semantic Kernel Group Chat Example ===\n")
    
    # Create group chat configuration
    config = GroupChatConfig(
        name="Product Planning Session",
        description="AI agents discussing product features",
        max_turns=6,
        auto_select_speaker=True
    )
    
    # Initialize group chat
    group_chat = SemanticKernelAgentGroupChat(config)
    await group_chat.initialize()
    
    # Add participants
    await group_chat.add_participant(
        name="ProductManager",
        instructions="You are a product manager. Focus on user needs, market requirements, and feature prioritization.",
        role=GroupChatRole.FACILITATOR,
        priority=3
    )
    
    await group_chat.add_participant(
        name="Engineer",
        instructions="You are a software engineer. Focus on technical feasibility, implementation complexity, and technical constraints.",
        role=GroupChatRole.PARTICIPANT,
        priority=2
    )
    
    await group_chat.add_participant(
        name="Designer",
        instructions="You are a UX designer. Focus on user experience, design principles, and usability concerns.",
        role=GroupChatRole.PARTICIPANT,
        priority=1
    )
    
    print(f"Created group chat with participants: {group_chat.get_participants()}")
    print()
    
    # Start conversation
    message = "We need to plan a new feature for our mobile app that helps users track their daily habits. What should we consider?"
    
    print(f"Initial message: {message}")
    print("\nGroup conversation:")
    print("-" * 50)
    
    responses = await group_chat.send_message(message, sender="User")
    
    for i, response in enumerate(responses, 1):
        print(f"\nTurn {i} - {response.agent_name}:")
        print(f"{response.content}")
        print(f"(Role: {response.metadata.get('speaker_role', 'unknown')})")
    
    print("\n" + "=" * 50)
    print(f"Conversation completed after {len(responses)} turns")
    
    # Get conversation summary
    summary = await group_chat.get_conversation_summary()
    print(f"\nConversation Summary:\n{summary}")
    
    await group_chat.cleanup()


async def custom_roles_example():
    """Demonstrate group chat with custom roles and priorities."""
    print("\n=== Custom Roles Example ===\n")
    
    config = GroupChatConfig(
        name="Code Review Session",
        description="Team reviewing code changes",
        max_turns=5,
        auto_select_speaker=True
    )
    
    group_chat = SemanticKernelAgentGroupChat(config)
    await group_chat.initialize()
    
    # Add participants with different priorities
    await group_chat.add_participant(
        name="SeniorDev",
        instructions="You are a senior developer. Focus on code quality, best practices, and architectural decisions.",
        role=GroupChatRole.FACILITATOR,
        priority=3,
        max_consecutive_turns=2
    )
    
    await group_chat.add_participant(
        name="SecurityExpert",
        instructions="You are a security expert. Focus on security vulnerabilities, secure coding practices, and potential threats.",
        role=GroupChatRole.PARTICIPANT,
        priority=2,
        max_consecutive_turns=1
    )
    
    await group_chat.add_participant(
        name="JuniorDev",
        instructions="You are a junior developer. Ask questions about the code and seek to understand the implementation.",
        role=GroupChatRole.PARTICIPANT,
        priority=1,
        max_consecutive_turns=2
    )
    
    message = "I've implemented a new user authentication system. Here's the code structure: UserAuth class with login(), logout(), and validateToken() methods. What should we review?"
    
    print(f"Code review topic: {message}")
    print("\nReview discussion:")
    print("-" * 50)
    
    responses = await group_chat.send_message(message, sender="Developer")
    
    for i, response in enumerate(responses, 1):
        print(f"\nTurn {i} - {response.agent_name}:")
        print(f"{response.content}")
    
    await group_chat.cleanup()


async def termination_example():
    """Demonstrate conversation termination."""
    print("\n=== Termination Example ===\n")
    
    config = GroupChatConfig(
        name="Quick Decision",
        description="Quick decision making session",
        max_turns=3,  # Very limited
        termination_keyword="DECISION",
        auto_select_speaker=True
    )
    
    group_chat = SemanticKernelAgentGroupChat(config)
    await group_chat.initialize()
    
    await group_chat.add_participant(
        name="Manager",
        instructions="You are a manager. Make quick decisions and end discussions with 'DECISION: [your decision]'.",
        role=GroupChatRole.FACILITATOR
    )
    
    await group_chat.add_participant(
        name="Advisor",
        instructions="You are an advisor. Provide quick recommendations and support decisions.",
        role=GroupChatRole.PARTICIPANT
    )
    
    message = "Should we launch the beta version next week or wait another month for more features?"
    
    print(f"Decision topic: {message}")
    print("\nQuick discussion:")
    print("-" * 50)
    
    responses = await group_chat.send_message(message, sender="TeamLead")
    
    for i, response in enumerate(responses, 1):
        print(f"\nTurn {i} - {response.agent_name}:")
        print(f"{response.content}")
    
    await group_chat.cleanup()


async def main():
    """Run all examples."""
    try:
        await basic_group_chat_example()
        await custom_roles_example()
        await termination_example()
        
        print("\n=== All examples completed successfully! ===")
        
    except Exception as e:
        logger.error(f"Error running examples: {e}")
        print(f"Error: {e}")
        print("\nMake sure you have set the required environment variables:")
        print("- AZURE_OPENAI_ENDPOINT")
        print("- AZURE_OPENAI_DEPLOYMENT_NAME")
        print("- AZURE_OPENAI_API_KEY")
        print("- AZURE_OPENAI_API_VERSION (optional, defaults to 2024-02-01)")


if __name__ == "__main__":
    asyncio.run(main())