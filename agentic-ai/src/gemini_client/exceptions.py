"""Exceptions raised by the Gemini integration."""


class GeminiClientError(RuntimeError):
    """Base exception for Gemini client failures."""


class GeminiConfigurationError(GeminiClientError):
    """Raised when Gemini cannot be configured safely."""


class GeminiSchemaError(GeminiClientError):
    """Raised when a response schema cannot be represented for Gemini."""


class GeminiInvalidRequestError(GeminiClientError):
    """Raised when Gemini rejects a locally valid request payload."""


class GeminiTimeoutError(GeminiClientError):
    """Raised when the Gemini provider exceeds its request timeout."""


class GeminiQuotaError(GeminiClientError):
    """Raised when all configured Gemini quota fallbacks are exhausted."""

    def __init__(
        self,
        message: str,
        *,
        error_code: str = "quota_exhausted",
        available_keys: int | None = None,
        retry_after: int | None = None,
    ) -> None:
        super().__init__(message)
        self.error_code = error_code
        self.available_keys = available_keys
        self.retry_after = retry_after


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
