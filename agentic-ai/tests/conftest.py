from collections.abc import Iterator

import pytest

from src.coordinator_agent.config import get_settings


@pytest.fixture(autouse=True)
def isolate_gemini_credentials(
    monkeypatch: pytest.MonkeyPatch,
) -> Iterator[None]:
    isolated_gemini_environment = {
        "GEMINI_API_KEY": "",
        "GEMINI_API_KEYS": "",
        "GEMINI_MODEL": "gemini-3.8-flash",
        "GEMINI_FALLBACK_MODELS": "",
        "GEMINI_TIMEOUT_SECONDS": "15",
        "GEMINI_MAX_RETRIES": "5",
        "GEMINI_RETRY_DELAY_SECONDS": "1",
        "GEMINI_RETRY_MAX_DELAY_SECONDS": "16",
    }
    for name, value in isolated_gemini_environment.items():
        monkeypatch.setenv(name, value)
    get_settings.cache_clear()
    yield
    get_settings.cache_clear()
