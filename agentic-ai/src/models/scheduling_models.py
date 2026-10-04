from typing import List, Optional
from pydantic import AliasChoices, BaseModel, ConfigDict, Field

class ActivityItem(BaseModel):
    title: str = Field(description="Name of the activity, e.g., Venue & Stage Setup")
    description: str = Field(description="Operational details")
    start_time: str = Field(description="Local event-date ISO 8601 timestamp without timezone")
    end_time: str = Field(description="Local event-date ISO 8601 timestamp without timezone")
    vendor_type: str = Field(description="Assigned category: AudioVisual, Catering, Photography, Hospitality")

class ScheduleState(BaseModel):
    event_id: str
    title: str
    event_description: Optional[str] = None
    event_type: str
    date: str
    start_time: str
    end_time: str
    guest_count: int
    requirements: Optional[str] = None
    vendor_service_context: List[dict] = []
    raw_activities: List[ActivityItem] = []
    validated_activities: List[ActivityItem] = []
    conflicts: List[str] = []
    error: Optional[str] = None

class GenerateScheduleRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    event_id: str = Field(validation_alias=AliasChoices("eventId", "event_id"))
    title: str = Field(validation_alias=AliasChoices("eventTitle", "title"))
    event_description: Optional[str] = Field(
        default=None,
        validation_alias=AliasChoices("eventDescription", "requirements"),
    )
    event_type: str = Field(validation_alias=AliasChoices("eventType", "event_type"))
    date: str = Field(validation_alias=AliasChoices("eventDate", "date"))
    start_time: str = Field(validation_alias=AliasChoices("eventStartTime", "start_time"))
    end_time: str = Field(validation_alias=AliasChoices("eventEndTime", "end_time"))
    guest_count: int = Field(validation_alias=AliasChoices("guestCount", "guest_count"))
    requirements: Optional[str] = None
    vendor_service_context: List[dict] = Field(
        default=[],
        validation_alias=AliasChoices("vendorServiceContext", "vendor_service_context"),
    )

class GenerateScheduleResponse(BaseModel):
    activities: List[ActivityItem]
    conflicts: List[str]
