"""Test script for enhanced group chat functionality."""

import asyncio
import os
import sys

# Add the parent directory to the Python path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

from shared import AgentRegistry, AgentConfig, AgentType, ConfigFactory
from agents.langchain_agents import LangChainAgentFactory, LANGCHAIN_AGENT_CONFIGS
from agents.agent_group_chat import EnhancedLangChainAgentGroupChat, GroupChatConfig, GroupChatRole


async def test_group_chat():
    """Test the enhanced group chat with multiple agents."""
    
    # Initialize registry and factory
    agent_registry = AgentRegistry()
    factory = LangChainAgentFactory()
    agent_registry.register_factory(factory)
    
    # Register agents
    print("Registering agents...")
    for config in LANGCHAIN_AGENT_CONFIGS:
        await agent_registry.register_agent(config)
        print(f"  - Registered: {config.name}")
    
    # Create group chat
    print("\nCreating group chat...")
    group_config = GroupChatConfig(
        name="TestGroupChat",
        max_turns=6,
        auto_select_speaker=True
    )
    
    group_chat = EnhancedLangChainAgentGroupChat(group_config, agent_registry)
    await group_chat.initialize()
    
    # Add participants
    print("Adding participants...")
    await group_chat.add_participant("generic_agent", priority=1)
    await group_chat.add_participant("people_lookup", priority=2)
    await group_chat.add_participant("knowledge_finder", priority=3)
    
    # Test messages
    test_messages = [
        "Hello, can you help me understand what this group chat is about?",
        "Who is the team lead for the AI project?",
        "What is the documentation for setting up Azure OpenAI?"
    ]
    
    for msg in test_messages:
        print(f"\n{'='*60}")
        print(f"User: {msg}")
        print(f"{'='*60}")
        
        responses = await group_chat.send_message(msg)
        
        for response in responses:
            print(f"\n[Turn {response.metadata.get('turn', '?')}] {response.agent_name}:")
            print(f"{response.content}")
            print(f"Metadata: {response.metadata}")
    
    # Print summary
    summary = group_chat.get_conversation_summary()
    print(f"\n{'='*60}")
    print("Conversation Summary:")
    print(f"  - Total turns: {summary['total_turns']}")
    print(f"  - Active participants: {summary['active_participants']}")
    print(f"  - Message count: {summary['message_count']}")


if __name__ == "__main__":
    # Set required environment variables if not set
    if not os.getenv("AZURE_OPENAI_ENDPOINT"):
        print("Please set AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY, and AZURE_OPENAI_DEPLOYMENT_NAME")
        print("Example:")
        print("  export AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/")
        print("  export AZURE_OPENAI_API_KEY=your-api-key")
        print("  export AZURE_OPENAI_DEPLOYMENT_NAME=your-deployment-name")
        exit(1)
    
    asyncio.run(test_group_chat())