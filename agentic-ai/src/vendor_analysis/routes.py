"""Vendor analysis API routes."""

import logging

from fastapi import APIRouter, HTTPException

from .models import VendorRecommendationRequest, VendorRecommendationResponse
from .service import (
    VendorAnalysisProviderError,
    VendorAnalysisValidationError,
    execute_vendor_analysis,
)

router = APIRouter(prefix="/api/vendor-analysis", tags=["Vendor Analysis"])
logger = logging.getLogger(__name__)

_PROVIDER_MESSAGES = {
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
        return await execute_vendor_analysis(request)
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
    except VendorAnalysisProviderError as exc:
        logger.error(
            "Gemini provider failed for vendor analysis event=%s code=%s",
            request.event_id,
            exc.code,
        )
        raise HTTPException(
            status_code=exc.status_code,
            detail={
                "code": exc.code,
                "message": _PROVIDER_MESSAGES.get(
                    exc.code,
                    "The AI provider could not rank vendors. Please try again later.",
                ),
            },
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
