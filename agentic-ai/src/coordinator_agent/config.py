"""Environment-backed configuration for the coordinator agent."""

import logging
from functools import lru_cache
from urllib.parse import urlsplit

from pydantic import Field, field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict

_DEVELOPMENT_CORS_ORIGINS = (
    "http://localhost:5173",
    "http://127.0.0.1:5173",
)
DEFAULT_GEMINI_MODEL = "gemini-3.8-flash"
DEFAULT_GEMINI_FALLBACK_MODELS = "gemini-3.7-flash,gemini-3.5-flash-lite"


def _parse_cors_origins(value: str) -> tuple[str, ...]:
    origins: list[str] = []
    for configured_origin in value.split(","):
        origin = configured_origin.strip()
        if not origin or any(character.isspace() for character in origin):
            raise ValueError("CORS_ORIGINS must be a comma-separated list of origins")
        try:
            parsed = urlsplit(origin)
            port = parsed.port
        except ValueError as exc:
            raise ValueError("CORS_ORIGINS contains an invalid origin") from exc
        if (
            parsed.scheme not in {"http", "https"}
            or not parsed.hostname
            or parsed.username is not None
            or parsed.password is not None
            or parsed.path not in {"", "/"}
            or parsed.query
            or parsed.fragment
            or (parsed.netloc.endswith(":") and port is None)
        ):
            raise ValueError(
                "CORS_ORIGINS entries must be exact http(s) origins without credentials"
            )
        normalized = f"{parsed.scheme}://{parsed.netloc}"
        if normalized not in origins:
            origins.append(normalized)
    if not origins:
        raise ValueError("CORS_ORIGINS must contain at least one origin")
    return tuple(origins)


class Settings(BaseSettings):
    """Application settings loaded from environment variables or ``.env``."""

    app_environment: str = Field(default="development", validation_alias="APP_ENV")
    cors_origins: str | None = Field(default=None, validation_alias="CORS_ORIGINS")
    backend_api_url: str = Field(
        default="http://localhost:5207", validation_alias="BACKEND_API_URL"
    )
    gemini_api_key: str | None = Field(default=None, validation_alias="GEMINI_API_KEY")
    gemini_model: str = Field(default=DEFAULT_GEMINI_MODEL, validation_alias="GEMINI_MODEL")
    gemini_api_keys: str = Field(default="", validation_alias="GEMINI_API_KEYS")
    gemini_fallback_models: str = Field(
        default=DEFAULT_GEMINI_FALLBACK_MODELS,
        validation_alias="GEMINI_FALLBACK_MODELS",
    )
    gemini_timeout_seconds: float = Field(
        default=15.0, gt=0, validation_alias="GEMINI_TIMEOUT_SECONDS"
    )
    gemini_max_retries: int = Field(default=5, ge=0, le=5, validation_alias="GEMINI_MAX_RETRIES")
    gemini_retry_delay_seconds: float = Field(
        default=1.0, gt=0, validation_alias="GEMINI_RETRY_DELAY_SECONDS"
    )
    gemini_retry_max_delay_seconds: float = Field(
        default=16.0, gt=0, validation_alias="GEMINI_RETRY_MAX_DELAY_SECONDS"
    )
    coordinator_timeout_seconds: int = Field(
        default=240, gt=0, validation_alias="COORDINATOR_TIMEOUT_SECONDS"
    )
    coordinator_checkpoint_db_path: str = Field(
        default=".data/coordinator-checkpoints.sqlite",
        validation_alias="COORDINATOR_CHECKPOINT_DB_PATH",
    )
    coordinator_checkpoint_ttl_hours: int = Field(
        default=168, gt=0, validation_alias="COORDINATOR_CHECKPOINT_TTL_HOURS"
    )
    log_level: str = Field(default="INFO", validation_alias="LOG_LEVEL")
    max_iterations: int = Field(default=1, ge=1, le=2, validation_alias="MAX_ITERATIONS")

    model_config = SettingsConfigDict(
        env_file=".env",
        extra="ignore",
        populate_by_name=True,
    )

    @field_validator("app_environment")
    @classmethod
    def validate_app_environment(cls, value: str) -> str:
        normalized = value.strip().lower()
        aliases = {"dev": "development", "local": "development", "prod": "production"}
        normalized = aliases.get(normalized, normalized)
        if normalized not in {"development", "production", "test"}:
            raise ValueError("APP_ENV must be development, production, or test")
        return normalized

    @field_validator("cors_origins")
    @classmethod
    def validate_configured_cors_origins(cls, value: str | None) -> str | None:
        if value is not None:
            _parse_cors_origins(value)
        return value

    @field_validator("backend_api_url")
    @classmethod
    def validate_backend_api_url(cls, value: str) -> str:
        backend_url = value.strip()
        try:
            parsed = urlsplit(backend_url)
            parsed.port
        except ValueError as exc:
            raise ValueError("BACKEND_API_URL must be a valid http(s) URL") from exc
        if (
            parsed.scheme not in {"http", "https"}
            or not parsed.hostname
            or parsed.username is not None
            or parsed.password is not None
            or parsed.path not in {"", "/"}
            or parsed.query
            or parsed.fragment
            or (parsed.netloc.endswith(":") and parsed.port is None)
        ):
            raise ValueError(
                "BACKEND_API_URL must be a root http(s) URL without credentials"
            )
        return f"{parsed.scheme}://{parsed.netloc}"

    def get_cors_origins(self) -> tuple[str, ...]:
        """Return validated browser origins, requiring explicit production config."""

        if self.cors_origins is not None:
            return _parse_cors_origins(self.cors_origins)
        if self.app_environment == "production":
            raise ValueError("CORS_ORIGINS must be configured in production")
        return _parse_cors_origins(",".join(_DEVELOPMENT_CORS_ORIGINS))

    @property
    def is_production(self) -> bool:
        """Whether production-only restrictions should be applied."""

        return self.app_environment == "production"

    @field_validator("gemini_model")
    @classmethod
    def validate_gemini_model_name(cls, value: str) -> str:
        model_name = value.strip()
        if not model_name.startswith("gemini-") or any(character.isspace() for character in model_name):
            raise ValueError("GEMINI_MODEL must be a Gemini model ID, such as gemini-3.8-flash")
        return model_name

    @field_validator("gemini_fallback_models")
    @classmethod
    def validate_fallback_model_names(cls, value: str) -> str:
        models = [model.strip() for model in value.split(",") if model.strip()]
        if any(
            not model.startswith("gemini-") or any(character.isspace() for character in model)
            for model in models
        ):
            raise ValueError("GEMINI_FALLBACK_MODELS must contain Gemini model IDs")
        return ",".join(models)

    def get_gemini_api_keys(self) -> tuple[str, ...]:
        """Return configured API keys in primary-then-fallback order."""

        configured = [self.gemini_api_key or ""]
        configured.extend(self.gemini_api_keys.split(","))
        return tuple(dict.fromkeys(key.strip() for key in configured if key.strip()))

    def get_gemini_models(self) -> tuple[str, ...]:
        """Return configured models in primary-then-fallback order."""

        configured = [self.gemini_model]
        configured.extend(self.gemini_fallback_models.split(","))
        return tuple(dict.fromkeys(model.strip() for model in configured if model.strip()))


@lru_cache
def get_settings() -> Settings:
    """Return the process-wide settings instance."""

    return Settings()


def configure_logging(level: str | None = None) -> None:
    """Configure a useful default logging format."""

    resolved_level = (level or get_settings().log_level).upper()
    logging.basicConfig(
        level=getattr(logging, resolved_level, logging.INFO),
        format="%(asctime)s %(levelname)s %(name)s: %(message)s",
    )
