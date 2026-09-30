"""Execute vendor recommendation ranking via Gemini structured output."""

from __future__ import annotations

import json
import logging
from typing import TYPE_CHECKING, Any
from uuid import UUID

from src.gemini_client.exceptions import (
    GeminiClientError,
    GeminiInvalidCredentialsError,
    GeminiInvalidModelError,
    GeminiNetworkError,
    GeminiQuotaError,
    GeminiRateLimitError,
    GeminiTimeoutError,
)

from .models import VendorRecommendationRequest, VendorRecommendationResponse
from .prompts import VENDOR_RECOMMENDATION_PROMPT

if TYPE_CHECKING:
    from src.gemini_client.client import GeminiClient

logger = logging.getLogger(__name__)


class VendorAnalysisError(RuntimeError):
    """Base failure for vendor analysis."""


class VendorAnalysisProviderError(VendorAnalysisError):
    """Upstream Gemini failure."""

    def __init__(
        self,
        message: str,
        code: str = "provider_error",
        status_code: int = 502,
        retry_after: int | None = None,
    ) -> None:
        super().__init__(message)
        self.code = code
        self.status_code = status_code
        self.retry_after = retry_after


class VendorAnalysisValidationError(VendorAnalysisError):
    """Generated recommendations failed candidate validation."""


def _candidate_index(request: VendorRecommendationRequest) -> dict[UUID, Any]:
    return {candidate.vendor_id: candidate for candidate in request.candidates}


def filter_to_candidates(
    response: VendorRecommendationResponse,
    request: VendorRecommendationRequest,
) -> VendorRecommendationResponse:
    """Drop invented IDs and invalid service references."""

    by_id = _candidate_index(request)
    kept = []
    for item in response.recommendations:
        candidate = by_id.get(item.vendor_id)
        if candidate is None:
            logger.warning("Dropping unknown vendorId from AI response")
            continue
        service_id = item.vendor_service_id
        matched = candidate.matched_service
        if service_id is not None:
            if matched is None or matched.vendor_service_id != service_id:
                logger.warning(
                    "Dropping invalid vendorServiceId for vendor %s", item.vendor_id
                )
                service_id = matched.vendor_service_id if matched is not None else None
        elif matched is not None:
            service_id = matched.vendor_service_id

        kept.append(
            item.model_copy(
                update={
                    "vendor_service_id": service_id,
                    "reasons": [reason[:200] for reason in item.reasons[:3]],
                }
            )
        )

    if not kept:
        raise VendorAnalysisValidationError(
            "AI returned no recommendations that match supplied candidates."
        )

    kept.sort(key=lambda item: item.score, reverse=True)
    return VendorRecommendationResponse(recommendations=kept[:10])


async def execute_vendor_analysis(
    request: VendorRecommendationRequest,
    gemini_client: GeminiClient | None = None,
) -> VendorRecommendationResponse:
    """Rank supplied candidates with Gemini and validate IDs."""

    from src.gemini_client.client import GeminiClient as GeminiClientImpl

    payload = request.model_dump(mode="json", by_alias=True)
    prompt = VENDOR_RECOMMENDATION_PROMPT.format(
        payload_json=json.dumps(payload, ensure_ascii=False, default=str)
    )

    client = gemini_client or GeminiClientImpl(temperature=0.2)
    try:
        raw = await client.generate_with_prompt(
            prompt,
            VendorRecommendationResponse,
            node_name="vendor_analysis_recommend",
        )
    except GeminiTimeoutError as exc:
        raise VendorAnalysisProviderError(
            str(exc), code="provider_timeout", status_code=504
        ) from exc
    except GeminiQuotaError as exc:
        raise VendorAnalysisProviderError(
            str(exc), code="quota_exhausted", status_code=503
        ) from exc
    except GeminiRateLimitError as exc:
        raise VendorAnalysisProviderError(
            str(exc),
            code="rate_limit",
            status_code=429,
            retry_after=getattr(exc, "retry_after", None),
        ) from exc
    except GeminiNetworkError as exc:
        raise VendorAnalysisProviderError(
            str(exc), code="network_issue", status_code=503
        ) from exc
    except GeminiInvalidModelError as exc:
        raise VendorAnalysisProviderError(
            str(exc), code="invalid_model", status_code=503
        ) from exc
    except GeminiInvalidCredentialsError as exc:
        raise VendorAnalysisProviderError(
            str(exc), code="invalid_credentials", status_code=503
        ) from exc
    except GeminiClientError as exc:
        raise VendorAnalysisProviderError(str(exc), code="provider_error", status_code=502) from exc

    return filter_to_candidates(raw, request)
