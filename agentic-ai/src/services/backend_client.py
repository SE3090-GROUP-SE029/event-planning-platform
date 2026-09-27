import httpx
from typing import Any, Optional
from datetime import datetime
from src.models.message_models import MessageResponse
from src.coordinator_agent.config import get_settings
import logging

logger = logging.getLogger(__name__)

class BackendClient:
    def __init__(self):
        self.base_url = get_settings().backend_api_url
        self.client = httpx.AsyncClient(base_url=self.base_url, timeout=10.0)

    async def ping(self) -> dict:
        """Call backend /api/test/ping endpoint"""
        try:
            response = await self.client.get("/api/test/ping")
            response.raise_for_status()
            logger.info(f"✅ Backend ping successful: {response.json()}")
            return response.json()
        except httpx.HTTPError as e:
            logger.error(f"❌ Backend ping failed: {e}")
            raise

    async def create_message(self, message: str) -> MessageResponse:
        """Call backend /api/test/message (POST)"""
        try:
            response = await self.client.post(
                "/api/test/message",
                json={"message": message}
            )
            response.raise_for_status()
            data = response.json()
            logger.info(f"✅ Message created via backend: {data}")
            return MessageResponse(**data)
        except httpx.HTTPError as e:
            logger.error(f"❌ Create message failed: {e}")
            raise

    async def get_message(self, message_id: int) -> MessageResponse:
        """Call backend /api/test/message/{id} (GET)"""
        try:
            response = await self.client.get(f"/api/test/message/{message_id}")
            response.raise_for_status()
            data = response.json()
            logger.info(f"✅ Message retrieved via backend: {data}")
            return MessageResponse(**data)
        except httpx.HTTPError as e:
            logger.error(f"❌ Get message failed: {e}")
            raise

    async def close(self):
        """Close the HTTP client"""
        await self.client.aclose()
