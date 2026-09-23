# Coordinator Agent

This service contains the LangGraph coordinator workflow for event planning.

```powershell
cd agentic-ai
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
python -m uvicorn src.main:app --reload
```

Set `GEMINI_API_KEY` in `.env` before enabling Gemini-backed nodes. The
coordinator packages are independent of the existing health and backend
integration routes.
