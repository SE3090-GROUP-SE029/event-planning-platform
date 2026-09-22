from pydantic import BaseModel, ConfigDict, Field
from src.models.guest_review_models import EventContext, InputModel, Reason


class QuestionSuggestion(BaseModel):
    model_config = ConfigDict(extra="forbid", strict=True)
    question: Reason
    required: bool


class QuestionSuggestions(BaseModel):
    model_config = ConfigDict(extra="forbid", strict=True)
    questions: list[QuestionSuggestion] = Field(min_length=1, max_length=10)


class QuestionSuggestionRequest(InputModel):
    event: EventContext
