from pydantic import BaseModel
from datetime import datetime
from typing import Optional

class PingRequest(BaseModel):
    message: str = "ping"

class PingResponse(BaseModel):
    message: str
    timestamp: datetime
    service: str = "agentic-ai"

class MessageRequest(BaseModel):
    message: str

class MessageResponse(BaseModel):
    id: int
    message: str
    createdAt: datetime

class BackendServiceCallLog(BaseModel):
    service_name: str
    endpoint: str
    method: str
    status_code: int
    timestamp: datetime
    request_body: Optional[dict] = None
    response_body: Optional[dict] = None
