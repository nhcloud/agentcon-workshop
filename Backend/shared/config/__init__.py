"""
Configuration package for the AI Agent System.
Provides configuration management, validation, and environment-specific settings.
"""

from .manager import YamlConfigManager, EnvironmentConfigManager, ConfigFactory, DEFAULT_AGENT_CONFIGS
from .enhanced_manager import ConfigurationManager, create_configuration_manager
from .validation import (
    SystemConfigModel,
    AgentConfigModel, 
    RouterConfigModel,
    SessionConfigModel,
    AppConfigModel,
    AgentFrameworkConfig,
    ProviderType,
    RouterType,
    SessionStorageType,
    ValidationResult,
    load_and_validate_config,
    create_default_config
)

__all__ = [
    # Legacy configuration managers
    "YamlConfigManager",
    "EnvironmentConfigManager", 
    "ConfigFactory",
    "DEFAULT_AGENT_CONFIGS",
    
    # Modern configuration manager
    'ConfigurationManager',
    'create_configuration_manager',
    
    # Configuration models
    'SystemConfigModel',
    'AgentConfigModel',
    'RouterConfigModel', 
    'SessionConfigModel',
    'AppConfigModel',
    'AgentFrameworkConfig',
    
    # Enums
    'ProviderType',
    'RouterType',
    'SessionStorageType',
    
    # Utilities
    'ValidationResult',
    'load_and_validate_config',
    'create_default_config'
]