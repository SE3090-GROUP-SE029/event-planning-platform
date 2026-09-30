from typing import List, Optional
from pydantic import BaseModel, Field

class ActivityItem(BaseModel):
    title: str = Field(description="Name of the activity, e.g., Venue & Stage Setup")
    description: str = Field(description="Operational details")
    start_time: str = Field(description="ISO 8601 UTC timestamp")
    end_time: str = Field(description="ISO 8601 UTC timestamp")
    vendor_type: str = Field(description="Assigned category: AudioVisual, Catering, Photography, Hospitality")

class ScheduleState(BaseModel):
    event_id: str
    title: str
    event_type: str
    date: str
    start_time: str
    end_time: str
    guest_count: int
    requirements: Optional[str] = None
    raw_activities: List[ActivityItem] = []
    validated_activities: List[ActivityItem] = []
    conflicts: List[str] = []
    error: Optional[str] = None

class GenerateScheduleRequest(BaseModel):
    event_id: str
    title: str
    event_type: str
    date: str
    start_time: str
    end_time: str
    guest_count: int
    requirements: Optional[str] = None

class GenerateScheduleResponse(BaseModel):
    activities: List[ActivityItem]
    conflicts: List[str]