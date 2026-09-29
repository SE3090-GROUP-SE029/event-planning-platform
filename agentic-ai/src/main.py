from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager
from datetime import datetime, timedelta, timezone
import logging
import os
from pathlib import Path
from dotenv import load_dotenv

os.environ["LANGGRAPH_STRICT_MSGPACK"] = "true"

from langgraph.checkpoint.sqlite.aio import AsyncSqliteSaver

from src.models.message_models import (
    PingResponse,
    MessageRequest,
    MessageResponse,
    PingRequest,
)
from src.services.backend_client import BackendClient
from src.api.routes import router as coordinator_router
from src.vendor_analysis.routes import router as vendor_analysis_router
from src.coordinator_agent.config import configure_logging, get_settings
from src.gemini_client.client import configure_gemini
from src.gemini_client.exceptions import GeminiConfigurationError
from src.models.guest_review_models import GuestReviewRequest, GuestReviewResponse
from src.services.guest_review_service import AnalysisError, GuestReviewService, get_guest_review_service
from src.models.registration_question_models import QuestionSuggestionRequest, QuestionSuggestions
from src.services.registration_question_service import RegistrationQuestionService, get_registration_question_service

# Load environment variables
load_dotenv()
configure_logging()

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Global backend client
backend_client: BackendClient | None = None


async def _prune_expired_checkpoints(checkpointer: AsyncSqliteSaver, ttl_hours: int) -> int:
    """Delete incomplete workflow checkpoints older than the configured retention period."""

    cutoff = datetime.now(timezone.utc) - timedelta(hours=ttl_hours)
    expired_threads: set[str] = set()
    async for checkpoint in checkpointer.alist(None):
        timestamp = checkpoint.checkpoint.get("ts")
        thread_id = checkpoint.config.get("configurable", {}).get("thread_id")
        if not timestamp or not thread_id:
            continue
        created_at = datetime.fromisoformat(str(timestamp).replace("Z", "+00:00"))
        if created_at < cutoff:
            expired_threads.add(thread_id)
    for thread_id in expired_threads:
        await checkpointer.adelete_thread(thread_id)
    return len(expired_threads)


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Manage app startup/shutdown"""
    global backend_client
    get_settings().get_cors_origins()
    try:
        configure_gemini()
    except GeminiConfigurationError:
        logger.exception(
            "Gemini configuration validation failed for model %s",
            get_settings().gemini_model,
        )
        raise
    settings = get_settings()
    checkpoint_path = Path(settings.coordinator_checkpoint_db_path).expanduser()
    if str(checkpoint_path) != ":memory:":
        checkpoint_path.parent.mkdir(parents=True, exist_ok=True)
    async with AsyncSqliteSaver.from_conn_string(str(checkpoint_path)) as checkpointer:
        await checkpointer.setup()
        expired_count = await _prune_expired_checkpoints(
            checkpointer, settings.coordinator_checkpoint_ttl_hours
        )
        app.state.coordinator_checkpointer = checkpointer
        backend_client = BackendClient()
        logger.info(
            "AI Service started with model=%s fallback_models=%d "
            "configured_api_keys=%d expired_checkpoints_removed=%d",
            settings.gemini_model,
            len(settings.get_gemini_models()) - 1,
            len(settings.get_gemini_api_keys()),
            expired_count,
        )
        try:
            yield
        finally:
            await backend_client.close()
            logger.info("AI Service stopped")

# Create FastAPI app
app = FastAPI(
    title="Event Planning AI Service",
    description="Event planning AI service with Google ADK guest review",
    version="0.1.0",
    lifespan=lifespan,
)

# Add CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=list(get_settings().get_cors_origins()),
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
app.include_router(coordinator_router)
app.include_router(vendor_analysis_router)

# ============================================================================
# HEALTH CHECK & PING ENDPOINTS
# ============================================================================

@app.exception_handler(AnalysisError)
async def analysis_error_handler(request: Request, exception: AnalysisError):
    from fastapi.responses import JSONResponse
    return JSONResponse(status_code=exception.status_code, content={"code": exception.code})


def _require_development_diagnostics(request: Request) -> None:
    if (
        get_settings().is_production
        or request.client is None
        or request.client.host not in {"127.0.0.1", "::1"}
    ):
        raise HTTPException(status_code=404, detail="Not Found")


@app.post("/api/guest-reviews/analyze", response_model=GuestReviewResponse, tags=["Internal Guest Review"])
async def analyze_guest(request: Request, context: GuestReviewRequest,
                        service: GuestReviewService = Depends(get_guest_review_service)):
    # Planner retrieval is exclusively through the protected ASP.NET API.
    if request.client is None or request.client.host not in {"127.0.0.1", "::1"}:
        raise HTTPException(status_code=403, detail="Local backend access required")
    return await service.analyze(context)

@app.get("/health", tags=["Health"])
async def health_check():
    """Health check endpoint"""
    return {
        "status": "ok",
        "service": "agentic-ai",
        "timestamp": datetime.utcnow(),
    }


@app.post("/api/registration-questions/suggest", response_model=QuestionSuggestions, tags=["Internal Registration Questions"])
async def suggest_questions(request: Request, context: QuestionSuggestionRequest,
                            service: RegistrationQuestionService = Depends(get_registration_question_service)):
    if request.client is None or request.client.host not in {"127.0.0.1", "::1"}:
        raise HTTPException(status_code=403, detail="Local backend access required")
    return await service.suggest(context)

@app.post("/api/test/ping", response_model=PingResponse, tags=["Test"])
async def ai_ping(request: PingRequest):
    """
    AI service ping endpoint.
    This endpoint demonstrates the AI service is running and healthy.
    """
    return PingResponse(
        message=f"pong from AI: {request.message}",
        timestamp=datetime.utcnow(),
    )

# ============================================================================
# BACKEND INTEGRATION ENDPOINTS
# ============================================================================

@app.get("/api/test/backend-ping", tags=["Test - Backend Integration"])
async def backend_ping(request: Request):
    """
    Test connectivity to the backend service.
    Calls backend's /api/test/ping endpoint and returns the result.
    """
    _require_development_diagnostics(request)
    try:
        if backend_client is None:
            raise RuntimeError("Backend client is not initialized")
        backend_response = await backend_client.ping()
        return {
            "message": "Backend connection verified",
            "backend_response": backend_response,
            "timestamp": datetime.utcnow(),
        }
    except Exception as e:
        raise HTTPException(
            status_code=503,
            detail=f"Backend service unavailable: {str(e)}",
        )

@app.post("/api/test/ai-propose-message", response_model=MessageResponse, tags=["Test - Backend Integration"])
async def ai_propose_message(request: MessageRequest, http_request: Request):
    """
    AI proposes a message through the backend.
    
    Flow:
    1. AI service receives message proposal
    2. AI service calls backend to create the message
    3. Backend validates and creates
    4. Response returned to AI service
    
    This demonstrates bidirectional backend↔AI communication.
    """
    _require_development_diagnostics(http_request)
    try:
        if backend_client is None:
            raise RuntimeError("Backend client is not initialized")
        logger.info(f"🤖 AI proposing message: '{request.message}'")
        result = await backend_client.create_message(request.message)
        logger.info(f"✅ Backend accepted AI proposal (ID: {result.id})")
        return result
    except Exception as e:
        logger.error(f"❌ AI message proposal failed: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Failed to propose message: {str(e)}",
        )

@app.get("/api/test/ai-retrieve-message/{message_id}", response_model=MessageResponse, tags=["Test - Backend Integration"])
async def ai_retrieve_message(message_id: int, request: Request):
    """
    AI retrieves a message from the backend.
    Demonstrates AI querying the backend for existing data.
    """
    _require_development_diagnostics(request)
    try:
        if backend_client is None:
            raise RuntimeError("Backend client is not initialized")
        logger.info(f"🤖 AI retrieving message ID: {message_id}")
        result = await backend_client.get_message(message_id)
        logger.info(f"✅ AI retrieved message: {result.message}")
        return result
    except Exception as e:
        logger.error(f"❌ AI retrieval failed: {e}")
        raise HTTPException(
            status_code=500,
            detail=f"Failed to retrieve message: {str(e)}",
        )

# ============================================================================
# ROOT ENDPOINT
# ============================================================================

@app.get("/", tags=["Root"])
async def root():
    """Root endpoint with service info"""
    return {
        "name": "Event Planning AI Service",
        "version": "0.1.0",
        "status": "running",
        "backend_url": get_settings().backend_api_url,
        "docs_url": "/docs",
    }

if __name__ == "__main__":
    import uvicorn
    port = int(os.getenv("AI_SERVICE_PORT", 8000))
    uvicorn.run(
        "src.main:app",
        host=os.getenv("AI_SERVICE_HOST", "127.0.0.1"),
        port=port,
        reload=True,
    )
