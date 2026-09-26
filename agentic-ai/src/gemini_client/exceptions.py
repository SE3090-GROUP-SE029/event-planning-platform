"""Exceptions raised by the Gemini integration."""


class GeminiClientError(RuntimeError):
    """Base exception for Gemini client failures."""


class GeminiConfigurationError(GeminiClientError):
    """Raised when Gemini cannot be configured safely."""


class GeminiTimeoutError(GeminiClientError):
    """Raised when the Gemini provider exceeds its request timeout."""


class GeminiQuotaError(GeminiClientError):
    """Raised when all configured Gemini quota fallbacks are exhausted."""


class GeminiRateLimitError(GeminiClientError):
    """Raised when Gemini continues rate limiting after bounded retries."""


class GeminiNetworkError(GeminiClientError):
    """Raised when a retryable network or provider availability failure persists."""


class GeminiInvalidModelError(GeminiClientError):
    """Raised when no configured Gemini model can serve the request."""


class GeminiInvalidCredentialsError(GeminiClientError):
    """Raised when none of the configured Gemini API keys is accepted."""


class GeminiResponseError(GeminiClientError):
    """Raised when Gemini returns unusable or invalid content."""


class GeminiTokenLimitError(GeminiClientError):
    """Raised when a prompt exceeds the configured token limit."""
