"""Configuration helpers for AgentGroupChat integration."""

import os
import yaml
from typing import Dict, List, Any, Optional
from pathlib import Path
from enum import Enum


class GroupChatRole(Enum):
    """Group chat participant roles."""
    FACILITATOR = "facilitator"
    PARTICIPANT = "participant"
    OBSERVER = "observer"


class GroupChatConfig:
    """Configuration for group chat sessions."""
    
    def __init__(
        self,
        name: str = "Group Chat",
        description: str = "",
        max_turns: int = 10,
        auto_select_speaker: bool = True
    ):
        self.name = name
        self.description = description
        self.max_turns = max_turns
        self.auto_select_speaker = auto_select_speaker


class GroupChatConfigLoader:
    """Loads group chat configurations from YAML files."""
    
    def __init__(self, config_path: str = "config.yml"):
        self.config_path = Path(config_path)
        self.config_data = self._load_yaml()
    
    def _load_yaml(self) -> Dict[str, Any]:
        """Load YAML configuration file."""
        if not self.config_path.exists():
            raise FileNotFoundError(f"Config file not found: {self.config_path}")
        
        with open(self.config_path, "r", encoding="utf-8") as f:
            data = yaml.safe_load(f)
        
        return data
    
    def get_group_chat_templates(self) -> Dict[str, Dict[str, Any]]:
        """Get all group chat templates from configuration."""
        return self.config_data.get("group_chats", {}).get("templates", {})
    
    def get_template(self, template_name: str) -> Optional[Dict[str, Any]]:
        """Get a specific group chat template."""
        templates = self.get_group_chat_templates()
        return templates.get(template_name)
    
    def create_group_chat_config(self, template_name: str) -> Optional[GroupChatConfig]:
        """Create GroupChatConfig from template."""
        template = self.get_template(template_name)
        if not template:
            return None
        
        return GroupChatConfig(
            name=template.get("name", template_name),
            description=template.get("description", ""),
            max_turns=template.get("max_turns", 10),
            auto_select_speaker=template.get("auto_select_speaker", True)
        )
    
    def get_template_participants(self, template_name: str) -> List[Dict[str, Any]]:
        """Get participants configuration for a template."""
        template = self.get_template(template_name)
        if not template:
            return []
        
        participants = []
        for participant_config in template.get("participants", []):
            # Ensure role is valid
            role = participant_config.get("role", "participant")
            if role not in ["facilitator", "participant", "observer"]:
                role = "participant"
            
            participants.append({
                "name": participant_config["name"],
                "instructions": participant_config["instructions"],
                "role": role,
                "priority": participant_config.get("priority", 1),
                "max_consecutive_turns": participant_config.get("max_consecutive_turns", 3)
            })
        
        return participants
    
    def list_available_templates(self) -> List[str]:
        """List all available group chat templates."""
        return list(self.get_group_chat_templates().keys())
    
    def get_template_info(self, template_name: str) -> Optional[Dict[str, Any]]:
        """Get template information for API responses."""
        template = self.get_template(template_name)
        if not template:
            return None
        
        participants = self.get_template_participants(template_name)
        
        return {
            "name": template.get("name", template_name),
            "description": template.get("description", ""),
            "max_turns": template.get("max_turns", 10),
            "auto_select_speaker": template.get("auto_select_speaker", True),
            "participants_count": len(participants),
            "participants": [
                {
                    "name": p["name"],
                    "role": p["role"],
                    "priority": p["priority"]
                }
                for p in participants
            ]
        }


# Global instance
_config_loader: Optional[GroupChatConfigLoader] = None


def get_config_loader() -> GroupChatConfigLoader:
    """Get or create global config loader instance."""
    global _config_loader
    if _config_loader is None:
        _config_loader = GroupChatConfigLoader()
    return _config_loader


def reload_config():
    """Reload configuration from file."""
    global _config_loader
    _config_loader = None  # Force reload on next access