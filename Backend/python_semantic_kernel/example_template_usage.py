"""Example usage of SK Modern AgentGroupChat with templates."""

import asyncio
import os
from pathlib import Path

from group_chat_config import get_config_loader
from agents.agent_group_chat import (
    SemanticKernelAgentGroupChat, 
    GroupChatConfig, 
    GroupChatRole,
    GroupChatParticipant
)


async def main():
    """Demonstrate group chat with templates."""
    
    # Load configuration
    config_loader = get_config_loader()
    
    # List available templates
    print("Available group chat templates:")
    templates = config_loader.list_available_templates()
    for template in templates:
        info = config_loader.get_template_info(template)
        print(f"  - {template}: {info['description']} ({info['participants_count']} participants)")
    
    if not templates:
        print("No templates found. Please check your config.yml file.")
        return
    
    # Use the first template as an example
    template_name = templates[0]
    print(f"\n--- Using template: {template_name} ---")
    
    # Create group chat from template
    group_chat_config = config_loader.create_group_chat_config(template_name)
    if not group_chat_config:
        print(f"Failed to load template: {template_name}")
        return
    
    # Create group chat
    group_chat = SemanticKernelAgentGroupChat(config=group_chat_config)
    
    # Get participants from template
    participants_config = config_loader.get_template_participants(template_name)
    
    print(f"Participants:")
    for participant_config in participants_config:
        print(f"  - {participant_config['name']} ({participant_config['role']})")
        print(f"    Instructions: {participant_config['instructions'][:100]}...")
    
    print(f"\nGroup chat created with {len(participants_config)} participants")
    print(f"Max turns: {group_chat_config.max_turns}")
    print(f"Auto select speaker: {group_chat_config.auto_select_speaker}")
    
    # You would normally add participants here with actual agents
    # For this example, we just show the configuration
    
    return group_chat


async def example_custom_group_chat():
    """Example of creating a custom group chat without templates."""
    
    # Create custom configuration
    config = GroupChatConfig(
        name="Custom Discussion",
        description="A custom group discussion example",
        max_turns=5,
        auto_select_speaker=True
    )
    
    # Create group chat
    group_chat = SemanticKernelAgentGroupChat(config=config)
    
    # Add custom participants (you would create actual agents here)
    print("Custom group chat created")
    print(f"Name: {config.name}")
    print(f"Description: {config.description}")
    
    return group_chat


if __name__ == "__main__":
    print("=== SK Modern AgentGroupChat Examples ===\n")
    
    # Template example
    asyncio.run(main())
    
    print("\n" + "="*50 + "\n")
    
    # Custom example
    asyncio.run(example_custom_group_chat())