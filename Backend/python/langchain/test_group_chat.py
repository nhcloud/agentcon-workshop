"""
Simple test script to validate LangChain AgentGroupChat implementation.
"""

import asyncio
import os
import sys
from pathlib import Path

# Add parent directories to path for imports
sys.path.append(str(Path(__file__).parent))
sys.path.append(str(Path(__file__).parent.parent / "shared"))

# Test imports
try:
    print("Testing LangChain imports...")
    from agents.agent_group_chat import (
        LangChainAgentGroupChat, 
        GroupChatConfig, 
        GroupChatRole,
        LangChainAgent
    )
    print("✓ LangChain imports successful")
except ImportError as e:
    print(f"✗ LangChain import failed: {e}")

try:
    print("Testing shared imports...")
    from shared import AgentConfig, AgentType, MessageRole
    print("✓ Shared imports successful")
except ImportError as e:
    print(f"✗ Shared import failed: {e}")


async def test_langchain_basic():
    """Test basic LangChain group chat functionality."""
    print("\n=== Testing LangChain Group Chat ===")
    
    try:
        # Create basic config
        config = GroupChatConfig(
            name="Test LangChain Chat",
            description="Testing basic LangChain functionality",
            max_turns=3,
            auto_select_speaker=True
        )
        
        # Initialize group chat
        group_chat = LangChainAgentGroupChat(config)
        
        # Test initialization without actual Azure credentials
        print("✓ LangChain group chat object created successfully")
        
        # Test configuration
        assert group_chat.name == "Test LangChain Chat"
        assert group_chat.config.max_turns == 3
        assert group_chat.config.auto_select_speaker == True
        print("✓ Configuration properties work correctly")
        
        # Test participant management (without initialization)
        participants = group_chat.get_participants()
        assert participants == []
        
        active_participants = group_chat.get_active_participants()
        assert active_participants == []
        print("✓ Participant management methods work")
        
        # Test conversation state management
        await group_chat.reset_conversation()
        assert group_chat.turn_count == 0
        assert group_chat.current_speaker is None
        print("✓ Conversation state management works")
        
        # Test conversation summary (empty case)
        summary = await group_chat.get_conversation_summary()
        assert summary == "No conversation yet."
        print("✓ Conversation summary method works")
        
        print("✓ Basic LangChain group chat functionality validated")
        
    except Exception as e:
        print(f"✗ LangChain test failed: {e}")
        return False
    
    return True


async def test_langchain_agent_wrapper():
    """Test LangChain agent wrapper functionality."""
    print("\n=== Testing LangChain Agent Wrapper ===")
    
    try:
        # Test LangChainAgent creation (without actual LLM)
        agent = LangChainAgent(
            name="TestAgent",
            instructions="You are a test agent",
            llm=None,  # We'll set this to None for testing
            role=GroupChatRole.PARTICIPANT,
            priority=2
        )
        
        assert agent.name == "TestAgent"
        assert agent.role == GroupChatRole.PARTICIPANT
        assert agent.priority == 2
        print("✓ LangChain agent wrapper creation works")
        
        # Test system message generation
        system_msg = agent.get_system_message()
        assert "You are a test agent" in system_msg
        assert "participant" in system_msg.lower()
        print("✓ System message generation works")
        
        # Test facilitator role
        facilitator = LangChainAgent(
            name="Facilitator",
            instructions="You facilitate discussions",
            llm=None,
            role=GroupChatRole.FACILITATOR
        )
        
        facilitator_msg = facilitator.get_system_message()
        assert "facilitator" in facilitator_msg.lower()
        print("✓ Role-specific system messages work")
        
        print("✓ LangChain agent wrapper functionality validated")
        
    except Exception as e:
        print(f"✗ LangChain agent wrapper test failed: {e}")
        return False
    
    return True


async def test_validation():
    """Run validation tests."""
    print("=== Running LangChain Validation Tests ===")
    
    lc_success = await test_langchain_basic()
    wrapper_success = await test_langchain_agent_wrapper()
    
    if lc_success and wrapper_success:
        print("\n✓ All LangChain validation tests passed!")
        print("\nTo run full examples with Azure credentials:")
        print("1. Set up your environment variables (AZURE_INFERENCE_ENDPOINT, etc.)")
        print("2. Run: python example_group_chat.py")
    else:
        print("\n✗ Some LangChain validation tests failed")
    
    return lc_success and wrapper_success


if __name__ == "__main__":
    asyncio.run(test_validation())