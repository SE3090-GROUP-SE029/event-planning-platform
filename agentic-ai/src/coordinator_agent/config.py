"""Environment-backed configuration for the coordinator agent."""

import logging
from functools import lru_cache

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application settings loaded from environment variables or ``.env``."""

    gemini_api_key: str | None = Field(default=None, validation_alias="GEMINI_API_KEY")
    log_level: str = Field(default="INFO", validation_alias="LOG_LEVEL")
    max_iterations: int = Field(default=3, ge=1, validation_alias="MAX_ITERATIONS")

    model_config = SettingsConfigDict(env_file=".env", extra="ignore")


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
