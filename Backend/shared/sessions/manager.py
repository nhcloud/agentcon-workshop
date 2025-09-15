"""Advanced session management implementations."""

import json
import asyncio
from datetime import datetime, timedelta
from pathlib import Path
from typing import Any, Dict, List, Optional
import aiofiles

from ..core import ISessionManager, AgentMessage, MessageRole, SessionException


class PersistentSessionManager(ISessionManager):
    """File-based persistent session manager."""
    
    def __init__(self, storage_path: str = "./sessions", cleanup_interval_hours: int = 24):
        self.storage_path = Path(storage_path)
        self.storage_path.mkdir(exist_ok=True)
        self.cleanup_interval = timedelta(hours=cleanup_interval_hours)
        self._cleanup_task: Optional[asyncio.Task] = None
        self._start_cleanup_task()
    
    def _start_cleanup_task(self) -> None:
        """Start background cleanup task."""
        if self._cleanup_task is None or self._cleanup_task.done():
            self._cleanup_task = asyncio.create_task(self._periodic_cleanup())
    
    async def _periodic_cleanup(self) -> None:
        """Periodically clean up old sessions."""
        while True:
            try:
                await asyncio.sleep(self.cleanup_interval.total_seconds())
                await self._cleanup_expired_sessions()
            except asyncio.CancelledError:
                break
            except Exception as e:
                # Log error but continue
                print(f"Error during session cleanup: {e}")
    
    async def _cleanup_expired_sessions(self, max_age_days: int = 7) -> None:
        """Remove sessions older than max_age_days."""
        cutoff_time = datetime.utcnow() - timedelta(days=max_age_days)
        
        for session_file in self.storage_path.glob("*.json"):
            try:
                stat = session_file.stat()
                if datetime.fromtimestamp(stat.st_mtime) < cutoff_time:
                    session_file.unlink()
            except Exception:
                continue  # Skip problematic files
    
    def _get_session_file(self, session_id: str) -> Path:
        """Get the file path for a session."""
        return self.storage_path / f"{session_id}.json"
    
    async def get_session(self, session_id: str) -> Dict[str, Any]:
        """Get or create a session."""
        session_file = self._get_session_file(session_id)
        
        if session_file.exists():
            try:
                async with aiofiles.open(session_file, 'r') as f:
                    content = await f.read()
                    session_data = json.loads(content)
                    # Update last activity
                    session_data["last_activity"] = datetime.utcnow().isoformat()
                    await self.save_session(session_id, session_data)
                    return session_data
            except Exception as e:
                raise SessionException(f"Error loading session {session_id}: {e}")
        else:
            # Create new session
            session_data = {
                "id": session_id,
                "created_at": datetime.utcnow().isoformat(),
                "last_activity": datetime.utcnow().isoformat(),
                "metadata": {},
                "cache": {},
                "messages": []
            }
            await self.save_session(session_id, session_data)
            return session_data
    
    async def save_session(self, session_id: str, session_data: Dict[str, Any]) -> None:
        """Save session data to file."""
        session_file = self._get_session_file(session_id)
        session_data["last_activity"] = datetime.utcnow().isoformat()
        
        try:
            async with aiofiles.open(session_file, 'w') as f:
                await f.write(json.dumps(session_data, indent=2))
        except Exception as e:
            raise SessionException(f"Error saving session {session_id}: {e}")
    
    async def delete_session(self, session_id: str) -> None:
        """Delete a session."""
        session_file = self._get_session_file(session_id)
        if session_file.exists():
            try:
                session_file.unlink()
            except Exception as e:
                raise SessionException(f"Error deleting session {session_id}: {e}")
    
    async def add_message(self, session_id: str, message: AgentMessage) -> None:
        """Add a message to session history."""
        session_data = await self.get_session(session_id)
        session_data["messages"].append(message.to_dict())
        await self.save_session(session_id, session_data)
    
    async def get_messages(self, session_id: str) -> List[AgentMessage]:
        """Get all messages for a session."""
        session_data = await self.get_session(session_id)
        messages = []
        
        for msg_data in session_data.get("messages", []):
            message = AgentMessage(
                id=msg_data.get("id", ""),
                role=MessageRole(msg_data.get("role", "user")),
                content=msg_data.get("content", ""),
                agent_name=msg_data.get("agent_name"),
                metadata=msg_data.get("metadata", {}),
                timestamp=datetime.fromisoformat(msg_data.get("timestamp", datetime.utcnow().isoformat()))
            )
            messages.append(message)
        
        return messages
    
    async def cleanup(self) -> None:
        """Cleanup resources."""
        if self._cleanup_task and not self._cleanup_task.done():
            self._cleanup_task.cancel()
            try:
                await self._cleanup_task
            except asyncio.CancelledError:
                pass


class RedisSessionManager(ISessionManager):
    """Redis-based session manager for distributed deployments."""
    
    def __init__(self, redis_url: str = "redis://localhost:6379", key_prefix: str = "session:"):
        self.redis_url = redis_url
        self.key_prefix = key_prefix
        self._redis = None
    
    async def _get_redis(self):
        """Get Redis connection (lazy initialization)."""
        if self._redis is None:
            try:
                import aioredis
                self._redis = aioredis.from_url(self.redis_url)
            except ImportError:
                raise SessionException("aioredis package required for Redis session manager")
        return self._redis
    
    def _get_session_key(self, session_id: str) -> str:
        """Get Redis key for a session."""
        return f"{self.key_prefix}{session_id}"
    
    async def get_session(self, session_id: str) -> Dict[str, Any]:
        """Get or create a session."""
        redis = await self._get_redis()
        session_key = self._get_session_key(session_id)
        
        session_data = await redis.get(session_key)
        if session_data:
            try:
                data = json.loads(session_data)
                data["last_activity"] = datetime.utcnow().isoformat()
                await self.save_session(session_id, data)
                return data
            except json.JSONDecodeError as e:
                raise SessionException(f"Error decoding session data: {e}")
        else:
            # Create new session
            data = {
                "id": session_id,
                "created_at": datetime.utcnow().isoformat(),
                "last_activity": datetime.utcnow().isoformat(),
                "metadata": {},
                "cache": {},
                "messages": []
            }
            await self.save_session(session_id, data)
            return data
    
    async def save_session(self, session_id: str, session_data: Dict[str, Any]) -> None:
        """Save session data to Redis."""
        redis = await self._get_redis()
        session_key = self._get_session_key(session_id)
        session_data["last_activity"] = datetime.utcnow().isoformat()
        
        try:
            await redis.setex(session_key, 86400 * 7, json.dumps(session_data))  # 7 day expiry
        except Exception as e:
            raise SessionException(f"Error saving session to Redis: {e}")
    
    async def delete_session(self, session_id: str) -> None:
        """Delete a session."""
        redis = await self._get_redis()
        session_key = self._get_session_key(session_id)
        await redis.delete(session_key)
    
    async def add_message(self, session_id: str, message: AgentMessage) -> None:
        """Add a message to session history."""
        session_data = await self.get_session(session_id)
        session_data["messages"].append(message.to_dict())
        await self.save_session(session_id, session_data)
    
    async def get_messages(self, session_id: str) -> List[AgentMessage]:
        """Get all messages for a session."""
        session_data = await self.get_session(session_id)
        messages = []
        
        for msg_data in session_data.get("messages", []):
            message = AgentMessage(
                id=msg_data.get("id", ""),
                role=MessageRole(msg_data.get("role", "user")),
                content=msg_data.get("content", ""),
                agent_name=msg_data.get("agent_name"),
                metadata=msg_data.get("metadata", {}),
                timestamp=datetime.fromisoformat(msg_data.get("timestamp", datetime.utcnow().isoformat()))
            )
            messages.append(message)
        
        return messages
    
    async def cleanup(self) -> None:
        """Cleanup Redis connection."""
        if self._redis:
            await self._redis.close()


class SessionManagerFactory:
    """Factory for creating session managers."""
    
    @staticmethod
    def create_memory_manager() -> ISessionManager:
        """Create an in-memory session manager."""
        from ..core import InMemorySessionManager
        return InMemorySessionManager()
    
    @staticmethod
    def create_persistent_manager(storage_path: str = "./sessions") -> PersistentSessionManager:
        """Create a file-based persistent session manager."""
        return PersistentSessionManager(storage_path)
    
    @staticmethod
    def create_redis_manager(redis_url: str = "redis://localhost:6379") -> RedisSessionManager:
        """Create a Redis-based session manager."""
        return RedisSessionManager(redis_url)
    
    @staticmethod
    def create_default_manager() -> ISessionManager:
        """Create a default session manager based on environment."""
        import os
        redis_url = os.getenv("REDIS_URL")
        if redis_url:
            return SessionManagerFactory.create_redis_manager(redis_url)
        
        storage_path = os.getenv("SESSION_STORAGE_PATH", "./sessions")
        return SessionManagerFactory.create_persistent_manager(storage_path)