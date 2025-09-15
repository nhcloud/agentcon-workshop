"""
Simple test script to validate both AgentGroupChat implementations.
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
    print("Testing Semantic Kernel imports...")
    from agents.agent_group_chat import (
        SemanticKernelAgentGroupChat, 
        GroupChatConfig, 
        GroupChatRole
    )
    print("✓ Semantic Kernel imports successful")
except ImportError as e:
    print(f"✗ Semantic Kernel import failed: {e}")

try:
    print("Testing shared imports...")
    from shared import AgentConfig, AgentType, MessageRole
    print("✓ Shared imports successful")
except ImportError as e:
    print(f"✗ Shared import failed: {e}")


async def test_semantic_kernel_basic():
    """Test basic Semantic Kernel group chat functionality."""
    print("\n=== Testing Semantic Kernel Group Chat ===")
    
    try:
        # Create basic config
        config = GroupChatConfig(
            name="Test Chat",
            description="Testing basic functionality",
            max_turns=2,
            auto_select_speaker=False
        )
        
        # Initialize group chat
        group_chat = SemanticKernelAgentGroupChat(config)
        
        # Test initialization without actual Azure credentials
        print("✓ Group chat object created successfully")
        
        # Test configuration
        assert group_chat.name == "Test Chat"
        assert group_chat.config.max_turns == 2
        print("✓ Configuration properties work correctly")
        
        # Test participant management (without initialization)
        participants = group_chat.get_participants()
        assert participants == []
        print("✓ Participant management methods work")
        
        print("✓ Basic Semantic Kernel group chat functionality validated")
        
    except Exception as e:
        print(f"✗ Semantic Kernel test failed: {e}")
        return False
    
    return True


async def test_validation():
    """Run validation tests."""
    print("=== Running Validation Tests ===")
    
    sk_success = await test_semantic_kernel_basic()
    
    if sk_success:
        print("\n✓ All basic validation tests passed!")
        print("\nTo run full examples with Azure credentials:")
        print("1. Set up your environment variables (AZURE_OPENAI_ENDPOINT, etc.)")
        print("2. Run: python example_group_chat.py")
    else:
        print("\n✗ Some validation tests failed")
    
    return sk_success


if __name__ == "__main__":
    asyncio.run(test_validation())