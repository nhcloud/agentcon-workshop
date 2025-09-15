"""
Configuration validation and management utilities.
Provides validation, environment variable expansion, and configuration loading.
"""
import os
import re
import yaml
from typing import Dict, Any, Optional, List
from pathlib import Path
from pydantic import BaseModel, Field, validator
from dataclasses import dataclass
from enum import Enum

class ProviderType(str, Enum):
    """Supported AI provider types."""
    AZURE_OPENAI = "azure_openai"
    AZURE_FOUNDRY = "azure_foundry" 
    GEMINI = "gemini"
    BEDROCK = "bedrock"

class RouterType(str, Enum):
    """Supported routing strategies."""
    PATTERN = "pattern"
    LLM = "llm"
    HYBRID = "hybrid"

class SessionStorageType(str, Enum):
    """Supported session storage types."""
    MEMORY = "memory"
    FILE = "file"
    REDIS = "redis"

class AgentFrameworkConfig(BaseModel):
    """Framework-specific agent configuration."""
    provider: Optional[ProviderType] = ProviderType.AZURE_OPENAI
    model: Optional[str] = "gpt-4o"
    temperature: Optional[float] = Field(0.7, ge=0.0, le=2.0)
    max_tokens: Optional[int] = Field(1000, gt=0)
    timeout: Optional[int] = Field(30, gt=0)
    
    # Provider-specific settings
    endpoint: Optional[str] = None
    deployment: Optional[str] = None
    api_key: Optional[str] = None
    project_id: Optional[str] = None
    region: Optional[str] = None
    
    class Config:
        extra = "allow"  # Allow additional provider-specific fields

class AgentConfigModel(BaseModel):
    """Agent configuration model with validation."""
    type: str
    enabled: bool = True
    instructions: str
    metadata: Optional[Dict[str, Any]] = {}
    framework_config: AgentFrameworkConfig = AgentFrameworkConfig()
    
    @validator('instructions')
    def instructions_not_empty(cls, v):
        if not v or not v.strip():
            raise ValueError('Agent instructions cannot be empty')
        return v

class RouterConfigModel(BaseModel):
    """Router configuration model."""
    type: RouterType = RouterType.HYBRID
    fallback_to_llm: bool = True
    patterns: Optional[Dict[str, List[str]]] = {}
    llm_config: Optional[AgentFrameworkConfig] = AgentFrameworkConfig()

class SessionConfigModel(BaseModel):
    """Session management configuration."""
    storage_type: SessionStorageType = SessionStorageType.MEMORY
    redis_url: Optional[str] = None
    file_path: Optional[str] = "./sessions"
    max_sessions: int = Field(1000, gt=0)
    session_timeout: int = Field(3600, gt=0)  # seconds

class AppConfigModel(BaseModel):
    """Application configuration model."""
    title: str = "AI Agent System"
    version: str = "2.0.0"
    frontend_url: str = "*"
    log_level: str = "INFO"
    host: str = "0.0.0.0"
    port: int = Field(8000, gt=0, le=65535)
    workers: int = Field(1, gt=0)

class SystemConfigModel(BaseModel):
    """Complete system configuration model."""
    app: AppConfigModel = AppConfigModel()
    agents: Dict[str, AgentConfigModel]
    router: RouterConfigModel = RouterConfigModel()
    session: SessionConfigModel = SessionConfigModel()

@dataclass
class ValidationResult:
    """Configuration validation result."""
    is_valid: bool
    errors: List[str]
    warnings: List[str]
    
    def __bool__(self):
        return self.is_valid

class ConfigurationManager:
    """
    Configuration management with validation and environment variable expansion.
    """
    
    ENV_VAR_PATTERN = re.compile(r'\$\{([^}]+)\}')
    
    def __init__(self, config_path: Optional[Path] = None):
        self.config_path = config_path
        self._raw_config = {}
        self._validated_config: Optional[SystemConfigModel] = None
    
    def load_config(self, config_path: Optional[Path] = None) -> SystemConfigModel:
        """Load and validate configuration from file."""
        if config_path:
            self.config_path = config_path
        
        if not self.config_path or not self.config_path.exists():
            raise FileNotFoundError(f"Configuration file not found: {self.config_path}")
        
        # Load raw YAML
        with open(self.config_path, 'r', encoding='utf-8') as f:
            self._raw_config = yaml.safe_load(f)
        
        # Expand environment variables
        expanded_config = self._expand_env_vars(self._raw_config)
        
        # Validate configuration
        try:
            self._validated_config = SystemConfigModel(**expanded_config)
            return self._validated_config
        except Exception as e:
            raise ValueError(f"Configuration validation failed: {e}")
    
    def validate_config(self, config_dict: Dict[str, Any]) -> ValidationResult:
        """Validate configuration dictionary."""
        errors = []
        warnings = []
        
        try:
            # Expand environment variables
            expanded_config = self._expand_env_vars(config_dict)
            
            # Validate with Pydantic
            SystemConfigModel(**expanded_config)
            
            # Additional business logic validation
            errors.extend(self._validate_business_rules(expanded_config))
            warnings.extend(self._validate_best_practices(expanded_config))
            
        except Exception as e:
            errors.append(f"Schema validation failed: {e}")
        
        return ValidationResult(
            is_valid=len(errors) == 0,
            errors=errors,
            warnings=warnings
        )
    
    def _expand_env_vars(self, obj: Any) -> Any:
        """Recursively expand environment variables in configuration."""
        if isinstance(obj, dict):
            return {key: self._expand_env_vars(value) for key, value in obj.items()}
        elif isinstance(obj, list):
            return [self._expand_env_vars(item) for item in obj]
        elif isinstance(obj, str):
            return self._expand_string_env_vars(obj)
        else:
            return obj
    
    def _expand_string_env_vars(self, text: str) -> str:
        """Expand environment variables in a string."""
        def replace_var(match):
            var_expr = match.group(1)
            if ':' in var_expr:
                var_name, default_value = var_expr.split(':', 1)
                return os.getenv(var_name, default_value)
            else:
                value = os.getenv(var_expr)
                if value is None:
                    raise ValueError(f"Required environment variable not set: {var_expr}")
                return value
        
        return self.ENV_VAR_PATTERN.sub(replace_var, text)
    
    def _validate_business_rules(self, config: Dict[str, Any]) -> List[str]:
        """Validate business logic rules."""
        errors = []
        
        # Check that at least one agent is enabled
        agents = config.get('agents', {})
        enabled_agents = [name for name, agent in agents.items() if agent.get('enabled', True)]
        if not enabled_agents:
            errors.append("At least one agent must be enabled")
        
        # Validate router patterns reference existing agents
        router = config.get('router', {})
        patterns = router.get('patterns', {})
        for agent_name in patterns.keys():
            if agent_name not in agents:
                errors.append(f"Router pattern references unknown agent: {agent_name}")
        
        # Validate Redis configuration if using Redis session storage
        session = config.get('session', {})
        if session.get('storage_type') == 'redis' and not session.get('redis_url'):
            errors.append("Redis URL is required when using Redis session storage")
        
        # Validate provider-specific requirements
        for agent_name, agent_config in agents.items():
            framework_config = agent_config.get('framework_config', {})
            provider = framework_config.get('provider')
            
            if provider == 'azure_openai':
                if not framework_config.get('endpoint'):
                    errors.append(f"Agent {agent_name}: Azure OpenAI endpoint is required")
            elif provider == 'gemini':
                if not framework_config.get('api_key'):
                    errors.append(f"Agent {agent_name}: Gemini API key is required")
        
        return errors
    
    def _validate_best_practices(self, config: Dict[str, Any]) -> List[str]:
        """Validate best practices (warnings)."""
        warnings = []
        
        # Check for default/weak configurations
        agents = config.get('agents', {})
        for agent_name, agent_config in agents.items():
            instructions = agent_config.get('instructions', '')
            if len(instructions) < 50:
                warnings.append(f"Agent {agent_name}: Instructions are very short, consider providing more context")
            
            framework_config = agent_config.get('framework_config', {})
            if framework_config.get('temperature', 0.7) > 1.5:
                warnings.append(f"Agent {agent_name}: High temperature ({framework_config.get('temperature')}) may produce inconsistent results")
        
        # Check session configuration
        session = config.get('session', {})
        if session.get('storage_type') == 'memory':
            warnings.append("Using memory session storage - sessions will be lost on restart")
        
        return warnings
    
    def get_config(self) -> Optional[SystemConfigModel]:
        """Get the validated configuration."""
        return self._validated_config
    
    def get_agent_config(self, agent_name: str) -> Optional[AgentConfigModel]:
        """Get configuration for a specific agent."""
        if not self._validated_config:
            return None
        return self._validated_config.agents.get(agent_name)
    
    def is_agent_enabled(self, agent_name: str) -> bool:
        """Check if an agent is enabled."""
        agent_config = self.get_agent_config(agent_name)
        return agent_config.enabled if agent_config else False
    
    def get_enabled_agents(self) -> List[str]:
        """Get list of enabled agent names."""
        if not self._validated_config:
            return []
        return [name for name, config in self._validated_config.agents.items() if config.enabled]
    
    def update_agent_status(self, agent_name: str, enabled: bool) -> bool:
        """Update agent enabled status."""
        if not self._validated_config or agent_name not in self._validated_config.agents:
            return False
        
        self._validated_config.agents[agent_name].enabled = enabled
        return True
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert configuration to dictionary."""
        if not self._validated_config:
            return {}
        return self._validated_config.dict()
    
    def save_config(self, output_path: Optional[Path] = None) -> None:
        """Save current configuration to file."""
        if not self._validated_config:
            raise ValueError("No configuration loaded")
        
        output_path = output_path or self.config_path
        if not output_path:
            raise ValueError("No output path specified")
        
        config_dict = self.to_dict()
        with open(output_path, 'w', encoding='utf-8') as f:
            yaml.dump(config_dict, f, default_flow_style=False, sort_keys=False, indent=2)

def load_and_validate_config(config_path: Path) -> SystemConfigModel:
    """Convenience function to load and validate configuration."""
    manager = ConfigurationManager(config_path)
    return manager.load_config()

def create_default_config() -> Dict[str, Any]:
    """Create a default configuration template."""
    return {
        'app': {
            'title': 'AI Agent System',
            'version': '2.0.0',
            'frontend_url': '${FRONTEND_URL:*}',
            'log_level': '${LOG_LEVEL:INFO}',
            'host': '0.0.0.0',
            'port': 8000
        },
        'agents': {
            'general_assistant': {
                'type': 'generic',
                'enabled': True,
                'instructions': 'You are a helpful AI assistant. Provide clear, accurate, and helpful responses.',
                'metadata': {
                    'description': 'General purpose AI assistant',
                    'capabilities': ['general_conversation', 'question_answering']
                },
                'framework_config': {
                    'provider': 'azure_openai',
                    'model': 'gpt-4o',
                    'temperature': 0.7,
                    'max_tokens': 1000
                }
            }
        },
        'router': {
            'type': 'hybrid',
            'fallback_to_llm': True,
            'patterns': {
                'general_assistant': [
                    '.*general.*',
                    '.*help.*',
                    '.*assist.*'
                ]
            }
        },
        'session': {
            'storage_type': 'memory',
            'max_sessions': 1000,
            'session_timeout': 3600
        }
    }