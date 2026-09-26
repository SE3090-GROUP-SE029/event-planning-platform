"""FastAPI application factory for coordinator routes."""

from fastapi import FastAPI

from .routes import router


def create_app() -> FastAPI:
    """Create the coordinator API application."""

    app = FastAPI(title="Coordinator Agent API", version="0.1.0")
    app.include_router(router)
    return app


app = create_app()
