import asyncio

import httpx
import pytest

from src.coordinator_agent.config import Settings, get_settings
from src import main
from src.main import app
from src.services.backend_client import BackendClient


def test_development_cors_defaults_are_explicit_local_origins() -> None:
    settings = Settings(
        app_environment="development", cors_origins=None, _env_file=None
    )

    assert settings.get_cors_origins() == (
        "http://localhost:5173",
        "http://127.0.0.1:5173",
    )


@pytest.mark.parametrize(
    "origins",
    [
        "*",
        "http://localhost:5173/path",
        "ftp://localhost:5173",
        "https://user:password@example.com",
        "http://localhost:bad-port",
        "http://localhost:5173,,https://example.com",
    ],
)
def test_invalid_cors_origins_fail_settings_validation(origins: str) -> None:
    with pytest.raises(ValueError):
        Settings(cors_origins=origins, _env_file=None)


def test_production_requires_explicit_cors_origins() -> None:
    settings = Settings(
        app_environment="production", cors_origins=None, _env_file=None
    )

    with pytest.raises(ValueError, match="CORS_ORIGINS must be configured"):
        settings.get_cors_origins()


def test_configured_production_cors_origins_are_used() -> None:
    settings = Settings(
        app_environment="production",
        cors_origins="https://events.example.com,https://admin.example.com",
        _env_file=None,
    )

    assert settings.get_cors_origins() == (
        "https://events.example.com",
        "https://admin.example.com",
    )


def test_backend_client_and_root_report_the_same_configured_url(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    configured_url = "http://backend.example.test:5207"
    monkeypatch.setenv("BACKEND_API_URL", configured_url)
    get_settings.cache_clear()
    client = BackendClient()

    async def read_root() -> dict[str, object]:
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(
            transport=transport, base_url="http://test"
        ) as test_client:
            return (await test_client.get("/")).json()

    try:
        root = asyncio.run(read_root())
        assert client.base_url == configured_url
        assert root["backend_url"] == configured_url
    finally:
        asyncio.run(client.close())
        get_settings.cache_clear()


@pytest.mark.parametrize(
    "method,path,payload,expected_status",
    [
        ("get", "/api/test/backend-ping", None, 503),
        ("post", "/api/test/ai-propose-message", {"message": "check"}, 500),
        ("get", "/api/test/ai-retrieve-message/1", None, 500),
    ],
)
def test_backend_diagnostics_remain_available_in_development(
    monkeypatch: pytest.MonkeyPatch,
    method: str,
    path: str,
    payload: dict[str, str] | None,
    expected_status: int,
) -> None:
    monkeypatch.setenv("APP_ENV", "development")
    get_settings.cache_clear()
    monkeypatch.setattr(main, "backend_client", None)

    async def request() -> httpx.Response:
        async with httpx.AsyncClient(
            transport=httpx.ASGITransport(app=app), base_url="http://test"
        ) as client:
            return await client.request(method, path, json=payload)

    try:
        response = asyncio.run(request())
        assert response.status_code == expected_status
    finally:
        get_settings.cache_clear()


@pytest.mark.parametrize(
    "method,path,payload",
    [
        ("get", "/api/test/backend-ping", None),
        ("post", "/api/test/ai-propose-message", {"message": "check"}),
        ("get", "/api/test/ai-retrieve-message/1", None),
    ],
)
def test_backend_diagnostics_are_local_only_in_development(
    monkeypatch: pytest.MonkeyPatch,
    method: str,
    path: str,
    payload: dict[str, str] | None,
) -> None:
    monkeypatch.setenv("APP_ENV", "development")
    get_settings.cache_clear()

    async def request() -> httpx.Response:
        async with httpx.AsyncClient(
            transport=httpx.ASGITransport(app=app, client=("192.0.2.1", 1234)),
            base_url="http://test",
        ) as client:
            return await client.request(method, path, json=payload)

    try:
        response = asyncio.run(request())
        assert response.status_code == 404
    finally:
        get_settings.cache_clear()


@pytest.mark.parametrize(
    "method,path,payload",
    [
        ("get", "/api/test/backend-ping", None),
        ("post", "/api/test/ai-propose-message", {"message": "check"}),
        ("get", "/api/test/ai-retrieve-message/1", None),
    ],
)
def test_backend_diagnostics_return_404_in_production(
    monkeypatch: pytest.MonkeyPatch,
    method: str,
    path: str,
    payload: dict[str, str] | None,
) -> None:
    monkeypatch.setenv("APP_ENV", "production")
    monkeypatch.setenv("CORS_ORIGINS", "https://events.example.com")
    get_settings.cache_clear()

    async def request() -> httpx.Response:
        async with httpx.AsyncClient(
            transport=httpx.ASGITransport(app=app), base_url="http://test"
        ) as client:
            return await client.request(method, path, json=payload)

    try:
        response = asyncio.run(request())
        assert response.status_code == 404
        health, business = asyncio.run(request_preserved_routes())
        assert health.status_code == 200
        assert business.status_code == 200
    finally:
        get_settings.cache_clear()


async def request_preserved_routes() -> tuple[httpx.Response, httpx.Response]:
    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://test"
    ) as client:
        health = await client.get("/health")
        business = await client.get("/api/coordinator/schema")
        return health, business
