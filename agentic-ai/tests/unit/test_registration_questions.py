import asyncio
import json
import httpx
import pytest
from pydantic import ValidationError
from src.main import app
from src.models.registration_question_models import QuestionSuggestionRequest
from src.services.registration_question_service import RegistrationQuestionService, get_registration_question_service
from src.services.guest_review_service import AnalysisError, ReviewSettings
from src.agents.registration_question_agent import SYSTEM_PROMPT


def context():
    return QuestionSuggestionRequest.model_validate({"event": {"eventName": "Software Engineering Conference", "requirementNotes": "For software engineers"}})


def service(raw):
    async def generate(request, settings):
        assert set(request.model_dump()) == {"event"}
        assert settings.model == "qwen3:8b"
        return raw
    return RegistrationQuestionService(ReviewSettings(), generate)


def test_question_suggestions_use_only_event_and_return_strict_contract():
    expected = {"questions": [{"question": "What software projects interest you?", "required": False}]}
    result = asyncio.run(service(json.dumps(expected)).suggest(context()))
    assert result.model_dump() == expected
    for rule in ("ONLY", "untrusted data", "hidden requirements", "protected traits", "passwords", "payment", "1-2", "planner"):
        assert rule in SYSTEM_PROMPT


@pytest.mark.parametrize("raw", [
    "invalid", "{}", "[]", '{"questions":[]}',
    json.dumps({"questions": [{"question": "x", "required": "true"}]}),
    json.dumps({"questions": [{"question": "x"}]}),
    json.dumps({"questions": [{"question": " ", "required": True}]}),
    json.dumps({"questions": [{"question": "x" * 501, "required": True}]}),
    json.dumps({"questions": [{"question": "x", "required": True, "id": "extra"}]}),
    json.dumps({"questions": [{"question": "x", "required": True}] * 11}),
    '{"questions":[{"question":"x","required":true,"required":false}]}',
])
def test_malformed_suggestions_are_failures(raw):
    with pytest.raises(AnalysisError) as error:
        asyncio.run(service(raw).suggest(context()))
    assert error.value.code == "invalid_ai_response"


def test_question_input_rejects_secrets_and_guest_payloads():
    for field in ("guest", "statusSecret", "invitationToken", "smtpPassword"):
        payload = context().model_dump(by_alias=True)
        payload[field] = "not allowed"
        with pytest.raises(ValidationError):
            QuestionSuggestionRequest.model_validate(payload)


@pytest.mark.parametrize("failure,code", [(ConnectionError("private data"), "ai_unavailable"), (TimeoutError(), "ai_timeout")])
def test_suggestion_failures_are_sanitized(failure, code):
    async def generate(request, settings):
        raise failure
    with pytest.raises(AnalysisError) as error:
        asyncio.run(RegistrationQuestionService(ReviewSettings(), generate).suggest(context()))
    assert error.value.code == code
    assert "private" not in str(error.value)


@pytest.mark.parametrize("host,status", [("127.0.0.1", 200), ("192.0.2.1", 403)])
def test_suggestions_endpoint_preserves_local_service_boundary(host, status):
    app.dependency_overrides[get_registration_question_service] = lambda: service('{"questions":[{"question":"Why attend?","required":false}]}')
    async def run():
        async with httpx.AsyncClient(transport=httpx.ASGITransport(app=app, client=(host, 1)), base_url="http://local") as client:
            response = await client.post("/api/registration-questions/suggest", json=context().model_dump(by_alias=True))
            assert response.status_code == status
            if status == 200:
                assert set(response.json()) == {"questions"}
    try:
        asyncio.run(run())
    finally:
        app.dependency_overrides.clear()
