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
`GEMINI_API_KEYS` accepts comma-separated secondary keys, and
`GEMINI_FALLBACK_MODELS` accepts comma-separated fallback model IDs. The
primary model is tried first with each configured key, followed by each
fallback model. API keys and model IDs must come from a secret manager or
environment configuration; never put real credentials in `.env.example`.

## Gemini resilience

Each Gemini operation logs its workflow node, active model, one-based API-key
index, masked key suffix, retry count, and approximate input/output tokens.
Quota exhaustion advances to the next key, then the next configured model.
Transient provider/network errors and identified rate limits use bounded
exponential backoff (1, 2, 4, 8, 16 seconds by default); invalid model and
credential errors move to the next configured fallback without retry loops.
Successful structured responses are cached in-process for 15 minutes (up to
512 entries) by prompt, schema, and configured model set.

The graph is sequential: analyze requirements → categorize services → propose
timeline → allocate budget → assess risks → detect missing requirements →
calculate completeness → generate rationale → self-validate. The Gemini nodes
are the first six plus rationale; completeness and validation are deterministic.
An interrupted run is checkpointed in `.data/coordinator-checkpoints.sqlite`.
Retrying the same event with unchanged input resumes from the failed node; a
changed event gets a separate checkpoint. Completed plans are persisted by the
existing backend and their temporary checkpoint is deleted. The checkpoint
file contains event input and generated state: protect its volume at rest,
back it up appropriately, and use one AI service instance per SQLite file.
Failed checkpoints older than `COORDINATOR_CHECKPOINT_TTL_HOURS` (168 hours by
default) are removed at startup.
For multi-replica deployments, replace SQLite checkpointing with a shared
production checkpoint store.

Provider failures use HTTP status codes plus `detail.code` values:
`quota_exhausted`, `rate_limit`, `network_issue`, `invalid_model`, and
`invalid_credentials`. Temporary quota/network responses include `Retry-After`.

## Quota efficiency

Per-node token counts are character-based estimates (`characters / 4`), not
provider billing totals. Token counting no longer makes a separate Gemini API
request. Prompt JSON preserves Unicode instead of escaping it, and prompts
request concise outputs without chain-of-thought text. Event details remain
present in several nodes because they serve distinct tasks; if input payloads
grow, trim optional fields before each node or summarize long requirements once
and reuse the summary.

The default Gemini timeout is 15 seconds per provider attempt; retries can
extend a node duration, while the coordinator, ASP.NET-to-agent request, and
mobile plan request allow 240, 250, and 260 seconds respectively. Override
these values and the default checkpoint path with the settings in
`.env.example`. The coordinator defaults to one graph pass to avoid repeating
the full multi-call workflow after validation failures.

Example safe fallback logs:

```text
INFO Calling Gemini node=analyze_requirements model=gemini-primary api_key_index=1 api_key=****a1b2 retry_count=0 input_tokens_estimate=420
WARNING Gemini quota-related failure node=analyze_requirements model=gemini-primary api_key_index=1 api_key=****a1b2 retry_count=0 category=quota
INFO Gemini model fallback node=analyze_requirements model=gemini-fallback fallback_attempt=1
INFO Calling Gemini node=analyze_requirements model=gemini-fallback api_key_index=1 api_key=****a1b2 retry_count=0 input_tokens_estimate=420
```

```text
INFO Calling Gemini node=propose_timeline model=gemini-primary api_key_index=1 api_key=****a1b2 retry_count=0 input_tokens_estimate=180
WARNING Gemini quota-related failure node=propose_timeline model=gemini-primary api_key_index=1 api_key=****a1b2 retry_count=0 category=quota
INFO Gemini API key fallback node=propose_timeline model=gemini-primary api_key_index=2 api_key=****c3d4 fallback_attempt=1
INFO Calling Gemini node=propose_timeline model=gemini-primary api_key_index=2 api_key=****c3d4 retry_count=0 input_tokens_estimate=180
```
