"""Vendor analysis API routes."""

import asyncio
import logging
from typing import Any

from fastapi import APIRouter, HTTPException

from src.coordinator_agent.config import AGENTIC_AI_REQUEST_TIMEOUT_SECONDS

from .models import VendorRecommendationRequest, VendorRecommendationResponse
from .service import (
    VendorAnalysisConfigurationError,
    VendorAnalysisProviderError,
    VendorAnalysisValidationError,
    execute_vendor_analysis,
)

router = APIRouter(prefix="/api/vendor-analysis", tags=["Vendor Analysis"])
logger = logging.getLogger(__name__)

_PROVIDER_MESSAGES = {
    "all_gemini_keys_exhausted": (
        "All Gemini API keys are temporarily exhausted. Retry after the cooldown."
    ),
    "quota_exhausted": "Gemini quota is exhausted. Retry later.",
    "rate_limit": "Gemini is rate limiting requests. Retry later.",
    "network_issue": "Gemini is temporarily unavailable.",
    "invalid_model": "No configured Gemini model can serve this request.",
    "invalid_credentials": "Gemini credentials are invalid or expired.",
    "provider_timeout": "Vendor recommendation took too long. Please try again.",
}


@router.get("/schema", response_model=dict[str, object])
async def vendor_analysis_schema() -> dict[str, object]:
    """Expose the structured output schema for integration clients."""

    return VendorRecommendationResponse.get_json_schema_for_gemini()


@router.post("/recommend", response_model=VendorRecommendationResponse)
async def recommend_vendors(
    request: VendorRecommendationRequest,
) -> VendorRecommendationResponse:
    """Rank pre-filtered candidate vendors for an approved event plan."""

    try:
        return await asyncio.wait_for(
            execute_vendor_analysis(request),
            timeout=AGENTIC_AI_REQUEST_TIMEOUT_SECONDS,
        )
    except TimeoutError as exc:
        logger.warning(
            "Vendor analysis timed out for event %s plan %s",
            request.event_id,
            request.plan_id,
        )
        raise HTTPException(
            status_code=504,
            detail={
                "code": "provider_timeout",
                "message": "Vendor recommendation took too long. Please try again.",
            },
        ) from exc
    except VendorAnalysisConfigurationError as exc:
        logger.error(
            "Vendor analysis Gemini request configuration is invalid for event %s: %s",
            request.event_id,
            exc,
        )
        raise HTTPException(
            status_code=500,
            detail={
                "code": "gemini_request_invalid",
                "message": (
                    "The AI service generated a request Gemini could not accept. "
                    "The service configuration requires correction."
                ),
            },
        ) from exc
    except VendorAnalysisProviderError as exc:
        logger.error(
            "Gemini provider failed for vendor analysis event=%s code=%s",
            request.event_id,
            exc.code,
        )
        detail: dict[str, Any] = {
            "code": exc.code,
            "message": _PROVIDER_MESSAGES.get(
                exc.code,
                "The AI provider could not rank vendors. Please try again later.",
            ),
        }
        if exc.code == "all_gemini_keys_exhausted":
            detail.update(
                error="all_gemini_keys_exhausted",
                available_keys=0,
            )
        raise HTTPException(
            status_code=exc.status_code,
            detail=detail,
            headers=(
                {"Retry-After": str(exc.retry_after)}
                if exc.retry_after is not None
                else None
            ),
        ) from exc
    except VendorAnalysisValidationError as exc:
        logger.warning(
            "Vendor analysis validation failed for event %s: %s",
            request.event_id,
            exc,
        )
        raise HTTPException(
            status_code=422,
            detail="The generated recommendations did not match supplied candidates.",
        ) from exc
    except Exception as exc:
        logger.exception(
            "Vendor analysis failed for event %s plan %s",
            request.event_id,
            request.plan_id,
        )
        raise HTTPException(
            status_code=500,
            detail="Vendor recommendation failed. Please try again later.",
        ) from exc
