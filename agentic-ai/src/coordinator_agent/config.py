"""Environment-backed configuration for the coordinator agent."""

import logging
from functools import lru_cache

from pydantic import Field, field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application settings loaded from environment variables or ``.env``."""

    gemini_api_key: str | None = Field(default=None, validation_alias="GEMINI_API_KEY")
    gemini_model: str = Field(default="gemini-3.8-flash", validation_alias="GEMINI_MODEL")
    gemini_api_keys: str = Field(default="", validation_alias="GEMINI_API_KEYS")
    gemini_fallback_models: str = Field(default="", validation_alias="GEMINI_FALLBACK_MODELS")
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

    @field_validator("gemini_model")
    @classmethod
    def validate_gemini_model_name(cls, value: str) -> str:
        model_name = value.strip()
        if not model_name.startswith("gemini-") or any(character.isspace() for character in model_name):
            raise ValueError("GEMINI_MODEL must be a Gemini model ID, such as gemini-2.5-flash")
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
