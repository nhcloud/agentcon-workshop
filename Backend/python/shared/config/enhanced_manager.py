"""
Enhanced configuration manager with environment-specific settings.
Extends the base configuration manager with deployment-specific functionality.
"""
import os
import logging
from pathlib import Path
from typing import Dict, Any, Optional, Union
from .manager import YamlConfigManager as BaseConfigManager
from .validation import SystemConfigModel, ValidationResult, load_and_validate_config

logger = logging.getLogger(__name__)

class ConfigurationManager(BaseConfigManager):
    """
    Enhanced configuration manager with environment detection and deployment support.
    """
    
    def __init__(self, 
                 config_path: Optional[Path] = None, 
                 environment: Optional[str] = None,
                 auto_detect_env: bool = True):
        """
        Initialize configuration manager.
        
        Args:
            config_path: Path to configuration file
            environment: Environment name (dev, staging, prod)
            auto_detect_env: Whether to auto-detect environment from ENV vars
        """
        super().__init__(config_path)
        self.environment = environment or (self._detect_environment() if auto_detect_env else None)
        self._setup_logging()
    
    def _detect_environment(self) -> str:
        """Detect environment from environment variables."""
        env = os.getenv('ENVIRONMENT', os.getenv('ENV', 'development')).lower()
        
        # Normalize environment names
        env_mapping = {
            'dev': 'development',
            'devel': 'development', 
            'develop': 'development',
            'staging': 'staging',
            'stage': 'staging',
            'prod': 'production',
            'production': 'production'
        }
        
        return env_mapping.get(env, env)
    
    def _setup_logging(self):
        """Setup logging based on environment."""
        if self.environment == 'production':
            log_level = logging.WARNING
        elif self.environment == 'staging':
            log_level = logging.INFO
        else:
            log_level = logging.DEBUG
        
        # Override with explicit LOG_LEVEL if set
        log_level_str = os.getenv('LOG_LEVEL')
        if log_level_str:
            log_level = getattr(logging, log_level_str.upper(), log_level)
        
        logging.basicConfig(
            level=log_level,
            format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
        )
    
    def load_config(self, config_path: Optional[Path] = None) -> SystemConfigModel:
        """Load configuration with environment-specific overrides."""
        config = super().load_config(config_path)
        
        # Apply environment-specific adjustments
        self._apply_environment_overrides(config)
        
        # Validate environment-specific requirements
        self._validate_environment_config(config)
        
        return config
    
    def _apply_environment_overrides(self, config: SystemConfigModel):
        """Apply environment-specific configuration overrides."""
        if self.environment == 'production':
            # Production-specific adjustments
            self._apply_production_settings(config)
        elif self.environment == 'staging':
            # Staging-specific adjustments  
            self._apply_staging_settings(config)
        elif self.environment == 'development':
            # Development-specific adjustments
            self._apply_development_settings(config)
    
    def _apply_production_settings(self, config: SystemConfigModel):
        """Apply production environment settings."""
        # Use Redis for session storage in production
        if config.session.storage_type == 'memory':
            logger.warning("Memory session storage detected in production - consider using Redis")
        
        # Adjust logging
        if config.app.log_level == 'DEBUG':
            config.app.log_level = 'INFO'
            logger.info("Adjusted log level to INFO for production")
        
        # Ensure secure settings
        config.app.frontend_url = os.getenv('FRONTEND_URL', 'https://your-frontend.com')
    
    def _apply_staging_settings(self, config: SystemConfigModel):
        """Apply staging environment settings."""
        # Similar to production but with more verbose logging
        if config.app.log_level == 'DEBUG':
            config.app.log_level = 'INFO'
    
    def _apply_development_settings(self, config: SystemConfigModel):
        """Apply development environment settings."""
        # Enable debug logging
        config.app.log_level = os.getenv('LOG_LEVEL', 'DEBUG')
        
        # Allow all origins for CORS in development
        config.app.frontend_url = os.getenv('FRONTEND_URL', '*')
    
    def _validate_environment_config(self, config: SystemConfigModel):
        """Validate environment-specific configuration requirements."""
        if self.environment == 'production':
            self._validate_production_config(config)
        elif self.environment == 'staging':
            self._validate_staging_config(config)
    
    def _validate_production_config(self, config: SystemConfigModel):
        """Validate production configuration requirements."""
        issues = []
        
        # Check for required environment variables
        required_env_vars = [
            'AZURE_INFERENCE_ENDPOINT',
            'AZURE_INFERENCE_CREDENTIAL'
        ]
        
        for var in required_env_vars:
            if not os.getenv(var):
                issues.append(f"Required environment variable not set: {var}")
        
        # Check session storage
        if config.session.storage_type == 'memory':
            issues.append("Memory session storage not recommended for production")
        
        if issues:
            logger.error(f"Production configuration issues: {'; '.join(issues)}")
            raise ValueError(f"Production configuration validation failed: {issues}")
    
    def _validate_staging_config(self, config: SystemConfigModel):
        """Validate staging configuration requirements."""
        # Similar to production but less strict
        if config.session.storage_type == 'memory':
            logger.warning("Memory session storage in staging - consider using Redis")
    
    def get_environment_info(self) -> Dict[str, Any]:
        """Get environment information."""
        return {
            'environment': self.environment,
            'python_version': os.sys.version,
            'working_directory': os.getcwd(),
            'config_path': str(self.config_path) if self.config_path else None,
            'environment_variables': {
                key: '***' if 'key' in key.lower() or 'secret' in key.lower() or 'password' in key.lower()
                else value
                for key, value in os.environ.items()
                if key.startswith(('AZURE_', 'GOOGLE_', 'AWS_', 'APP_', 'LOG_'))
            }
        }
    
    def health_check(self) -> Dict[str, Any]:
        """Perform configuration health check."""
        if not self._validated_config:
            return {
                'status': 'error',
                'message': 'No configuration loaded',
                'timestamp': self._get_timestamp()
            }
        
        try:
            # Basic validation
            validation_result = self.validate_config(self.to_dict())
            
            # Count enabled agents
            enabled_agents = self.get_enabled_agents()
            
            # Check external dependencies
            dependency_status = self._check_dependencies()
            
            status = 'healthy' if validation_result.is_valid and dependency_status['all_ok'] else 'warning'
            
            return {
                'status': status,
                'environment': self.environment,
                'config_valid': validation_result.is_valid,
                'enabled_agents': len(enabled_agents),
                'total_agents': len(self._validated_config.agents),
                'validation_errors': validation_result.errors,
                'validation_warnings': validation_result.warnings,
                'dependencies': dependency_status,
                'timestamp': self._get_timestamp()
            }
            
        except Exception as e:
            logger.error(f"Health check failed: {e}")
            return {
                'status': 'error',
                'message': str(e),
                'timestamp': self._get_timestamp()
            }
    
    def _check_dependencies(self) -> Dict[str, Any]:
        """Check external dependencies status."""
        dependencies = {
            'all_ok': True,
            'checks': {}
        }
        
        # Check environment variables
        env_check = self._check_environment_variables()
        dependencies['checks']['environment'] = env_check
        if not env_check['ok']:
            dependencies['all_ok'] = False
        
        # Check session storage
        session_check = self._check_session_storage()
        dependencies['checks']['session_storage'] = session_check
        if not session_check['ok']:
            dependencies['all_ok'] = False
        
        return dependencies
    
    def _check_environment_variables(self) -> Dict[str, Any]:
        """Check required environment variables."""
        if not self._validated_config:
            return {'ok': False, 'message': 'No configuration loaded'}
        
        missing_vars = []
        
        # Check based on enabled agents
        for agent_name, agent_config in self._validated_config.agents.items():
            if not agent_config.enabled:
                continue
                
            provider = agent_config.framework_config.provider
            
            if provider == 'azure_openai':
                required_vars = ['AZURE_OPENAI_ENDPOINT', 'AZURE_OPENAI_KEY']
            elif provider == 'azure_foundry':
                required_vars = ['PROJECT_ENDPOINT', 'AZURE_INFERENCE_CREDENTIAL']
            elif provider == 'gemini':
                required_vars = ['GOOGLE_API_KEY']
            elif provider == 'bedrock':
                required_vars = ['AWS_BEDROCK_AGENT_ID']
            else:
                continue
            
            for var in required_vars:
                if not os.getenv(var):
                    missing_vars.append(f"{var} (for {agent_name})")
        
        return {
            'ok': len(missing_vars) == 0,
            'missing_variables': missing_vars,
            'message': f"Missing environment variables: {', '.join(missing_vars)}" if missing_vars else "All required environment variables present"
        }
    
    def _check_session_storage(self) -> Dict[str, Any]:
        """Check session storage connectivity."""
        if not self._validated_config:
            return {'ok': False, 'message': 'No configuration loaded'}
        
        storage_type = self._validated_config.session.storage_type
        
        if storage_type == 'memory':
            return {'ok': True, 'message': 'Memory storage (no external dependencies)'}
        elif storage_type == 'file':
            file_path = Path(self._validated_config.session.file_path or './sessions')
            try:
                file_path.mkdir(parents=True, exist_ok=True)
                return {'ok': True, 'message': f'File storage accessible at {file_path}'}
            except Exception as e:
                return {'ok': False, 'message': f'File storage error: {e}'}
        elif storage_type == 'redis':
            redis_url = self._validated_config.session.redis_url
            if not redis_url:
                return {'ok': False, 'message': 'Redis URL not configured'}
            
            try:
                # Try to import and connect to Redis
                import redis
                r = redis.from_url(redis_url)
                r.ping()
                return {'ok': True, 'message': 'Redis connection successful'}
            except ImportError:
                return {'ok': False, 'message': 'Redis package not installed'}
            except Exception as e:
                return {'ok': False, 'message': f'Redis connection failed: {e}'}
        
        return {'ok': False, 'message': f'Unknown storage type: {storage_type}'}
    
    def _get_timestamp(self) -> str:
        """Get current timestamp."""
        from datetime import datetime
        return datetime.utcnow().isoformat() + 'Z'

def create_configuration_manager(
    config_path: Optional[Union[str, Path]] = None,
    environment: Optional[str] = None
) -> ConfigurationManager:
    """
    Factory function to create configuration manager.
    
    Args:
        config_path: Path to configuration file
        environment: Environment name
        
    Returns:
        Configured ConfigurationManager instance
    """
    if config_path:
        config_path = Path(config_path)
    
    return ConfigurationManager(
        config_path=config_path,
        environment=environment
    )