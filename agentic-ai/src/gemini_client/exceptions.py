"""Exceptions raised by the Gemini integration."""


class GeminiClientError(RuntimeError):
    """Base exception for Gemini client failures."""


class GeminiConfigurationError(GeminiClientError):
    """Raised when Gemini cannot be configured safely."""


class GeminiTimeoutError(GeminiClientError):
    """Raised when the Gemini provider exceeds its request timeout."""


class GeminiResponseError(GeminiClientError):
    """Raised when Gemini returns unusable or invalid content."""


class GeminiTokenLimitError(GeminiClientError):
    """Raised when a prompt exceeds the configured token limit."""
