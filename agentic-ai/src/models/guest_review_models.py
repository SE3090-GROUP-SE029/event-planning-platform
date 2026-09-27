"""Bounded, explicit contracts; registration entities and tokens are never accepted."""

import unicodedata
from typing import Annotated, Literal

from pydantic import AfterValidator, AwareDatetime, BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


def clean_text(value: str) -> str:
    if not value.strip() or any(unicodedata.category(char) == "Cc" for char in value):
        raise ValueError("Expected non-empty text without control characters")
    return value


Reason = Annotated[str, Field(min_length=1, max_length=500), AfterValidator(clean_text)]
Flag = Annotated[str, Field(min_length=1, max_length=80), AfterValidator(clean_text)]


class InputModel(BaseModel):
    model_config = ConfigDict(extra="forbid", alias_generator=to_camel, populate_by_name=True)


class GuestContext(InputModel):
    full_name: str = Field(min_length=1, max_length=200)
    email_address: str = Field(min_length=1, max_length=254)
    organisation: str | None = Field(default=None, max_length=200)
    phone_number: str | None = Field(default=None, max_length=40)


class EventContext(InputModel):
    event_name: str = Field(min_length=1, max_length=200)
    requirement_notes: str | None = Field(default=None, max_length=4000)


class ComparisonContext(InputModel):
    guest: GuestContext
    registered_at: AwareDatetime


class QuestionAnswerContext(InputModel):
    question: Reason
    required: bool = Field(strict=True)
    answer: str | None = Field(default=None, max_length=4000)


class GuestReviewRequest(InputModel):
    guest: GuestContext
    event: EventContext
    registered_at: AwareDatetime
    comparisons: list[ComparisonContext] = Field(max_length=20)
    comparisons_limited: bool = Field(strict=True)
    questions: list[QuestionAnswerContext] = Field(default_factory=list, max_length=10)


class GuestDecision(BaseModel):
    model_config = ConfigDict(extra="forbid", strict=True, allow_inf_nan=False)
    decision: Literal["ACCEPTED", "REJECTED"]
    confidence: float = Field(ge=0, le=1)
    reasons: list[Reason] = Field(min_length=1, max_length=10)
    flags: list[Flag] = Field(max_length=10)


class GuestReviewResponse(GuestDecision):
    model_config = ConfigDict(extra="forbid", strict=True, allow_inf_nan=False,
                              alias_generator=to_camel, populate_by_name=True)
    model: str = Field(min_length=1, max_length=120)
    prompt_version: str = Field(min_length=1, max_length=40)
