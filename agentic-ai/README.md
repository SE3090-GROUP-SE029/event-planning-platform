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

Set `GEMINI_API_KEY` and `GEMINI_MODEL` in `.env` before starting the service.
Startup verifies that the configured model is available to that API key and
supports `generateContent`; an invalid model or key fails startup rather than
being retried on every plan request. The example uses `gemini-2.5-flash`.

The default plan-generation time budgets are 15 seconds per Gemini call, one
second for optional token counting, and one retry for transient provider
failures; the coordinator, ASP.NET-to-agent request, and mobile plan request
allow 240, 250, and 260 seconds respectively. Override them with the matching
settings in `.env.example`. The coordinator defaults to one graph pass to avoid
repeating the full multi-call workflow after validation failures.
