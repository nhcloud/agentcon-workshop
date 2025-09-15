"""Configuration management for the AI Agent System."""

import os
import re
from pathlib import Path
from typing import Any, Dict, List, Optional
import yaml

from ..core import IConfigManager, AgentConfig, AgentType


class YamlConfigManager(IConfigManager):
    """YAML-based configuration manager."""
    
    def __init__(self, config_path: str):
        self.config_path = Path(config_path)
        self._config_data: Optional[Dict[str, Any]] = None
        self._agent_configs: Optional[List[AgentConfig]] = None
        self.load_config()
    
    def load_config(self) -> None:
        """Load configuration from YAML file."""
        if not self.config_path.exists():
            raise FileNotFoundError(f"Configuration file not found: {self.config_path}")
        
        with open(self.config_path, 'r', encoding='utf-8') as f:
            self._config_data = yaml.safe_load(f) or {}
        
        self._parse_agent_configs()
        self._export_env_variables()
    
    def _parse_agent_configs(self) -> None:
        """Parse agent configurations from the loaded data."""
        self._agent_configs = []
        agents_data = self._config_data.get('agents', {})
        
        for agent_name, agent_data in agents_data.items():
            config = AgentConfig(
                name=agent_name,
                agent_type=AgentType(agent_data.get('type', 'generic')),
                instructions=agent_data.get('instructions', ''),
                enabled=agent_data.get('enabled', True),
                metadata=agent_data.get('metadata', {}),
                framework_config=agent_data.get('framework_config', {})
            )
            self._agent_configs.append(config)
    
    def _export_env_variables(self) -> None:
        """Export environment variables from configuration."""
        def interpolate_value(value: Any) -> Any:
            """Resolve placeholders like ${VAR} or ${VAR:default}."""
            if not isinstance(value, str):
                return value
            
            # Check if it's a placeholder
            if value.startswith("${") and value.endswith("}"):
                inner = value[2:-1]  # Remove ${ and }
                if ':' in inner:
                    var_name, default_value = inner.split(':', 1)
                else:
                    var_name, default_value = inner, None
                
                return os.getenv(var_name, default_value)
            return value
        
        def export_nested(data: Dict[str, Any], prefix: str = "") -> None:
            """Recursively export nested configuration as environment variables."""
            for key, value in data.items():
                if isinstance(value, dict):
                    export_nested(value, f"{prefix}{key.upper()}_")
                else:
                    env_key = f"{prefix}{key.upper()}"
                    resolved_value = interpolate_value(value)
                    if resolved_value is not None:
                        os.environ[env_key] = str(resolved_value)
        
        # Export all configuration sections as environment variables
        for section, data in self._config_data.items():
            if isinstance(data, dict):
                export_nested(data, f"{section.upper()}_")
    
    def get_agent_configs(self) -> List[AgentConfig]:
        """Get all agent configurations."""
        return self._agent_configs or []
    
    def get_agent_config(self, agent_name: str) -> Optional[AgentConfig]:
        """Get configuration for a specific agent."""
        for config in self.get_agent_configs():
            if config.name == agent_name:
                return config
        return None
    
    def get_system_config(self) -> Dict[str, Any]:
        """Get system-wide configuration."""
        return self._config_data or {}
    
    def reload_config(self) -> None:
        """Reload configuration from file."""
        self.load_config()
    
    def get_section(self, section_name: str) -> Dict[str, Any]:
        """Get a specific configuration section."""
        return self._config_data.get(section_name, {})


class EnvironmentConfigManager(IConfigManager):
    """Environment variable-based configuration manager."""
    
    def __init__(self, agent_configs: Optional[List[AgentConfig]] = None):
        self._agent_configs = agent_configs or []
    
    def get_agent_configs(self) -> List[AgentConfig]:
        """Get agent configurations from environment variables."""
        return self._agent_configs
    
    def get_agent_config(self, agent_name: str) -> Optional[AgentConfig]:
        """Get configuration for a specific agent."""
        for config in self._agent_configs:
            if config.name == agent_name:
                return config
        return None
    
    def get_system_config(self) -> Dict[str, Any]:
        """Get system configuration from environment variables."""
        return {
            "azure_openai_endpoint": os.getenv("AZURE_OPENAI_ENDPOINT"),
            "azure_openai_key": os.getenv("AZURE_OPENAI_KEY"),
            "project_endpoint": os.getenv("PROJECT_ENDPOINT"),
            "frontend_url": os.getenv("FRONTEND_URL", "*"),
            "debug_logs": os.getenv("DEBUG_LOGS", "1") == "1"
        }
    
    def reload_config(self) -> None:
        """Environment variables are always current, no reload needed."""
        pass
    
    def add_agent_config(self, config: AgentConfig) -> None:
        """Add an agent configuration."""
        # Remove existing config with same name
        self._agent_configs = [c for c in self._agent_configs if c.name != config.name]
        self._agent_configs.append(config)


class ConfigFactory:
    """Factory for creating configuration managers."""
    
    @staticmethod
    def create_yaml_config(config_path: str) -> YamlConfigManager:
        """Create a YAML configuration manager."""
        return YamlConfigManager(config_path)
    
    @staticmethod
    def create_env_config(agent_configs: Optional[List[AgentConfig]] = None) -> EnvironmentConfigManager:
        """Create an environment configuration manager."""
        return EnvironmentConfigManager(agent_configs)
    
    @staticmethod
    def create_hybrid_config(yaml_path: str) -> IConfigManager:
        """Create a hybrid configuration manager that uses YAML with env var overrides."""
        try:
            return YamlConfigManager(yaml_path)
        except FileNotFoundError:
            return EnvironmentConfigManager()


# Default agent configurations for common scenarios
DEFAULT_AGENT_CONFIGS = [
    AgentConfig(
        name="generic_agent",
        agent_type=AgentType.GENERIC,
        instructions="You are a helpful AI assistant. Provide accurate and helpful responses.",
        enabled=True
    ),
    AgentConfig(
        name="people_lookup",
        agent_type=AgentType.PEOPLE_LOOKUP,
        instructions="You help find information about people in the organization.",
        enabled=True
    ),
    AgentConfig(
        name="knowledge_finder",
        agent_type=AgentType.KNOWLEDGE_FINDER,
        instructions="You help find information from documentation and knowledge bases.",
        enabled=True
    )
]