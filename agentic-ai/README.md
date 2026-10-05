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

## ADK guest review and registration questions

Guest review and registration-question agents use Google ADK with the same
Gemini model, API-key rotation, and fallback-model settings. Each invocation
uses a separate in-memory ADK session. `GUEST_AI_TIMEOUT_SECONDS` sets the
request timeout and defaults to 110 seconds; no local model runtime or separate
provider endpoint is needed.

## Gemini resilience

Each Gemini operation logs its workflow node, active model, one-based API-key
index, masked key suffix, approximate input/output tokens, and per-attempt
timeout. HTTP 429 immediately marks the key `CoolingDown` and rotates without
retrying it. HTTP 500/502/503/504 receives at most one immediate retry on the
same key, then the key is marked `Unavailable` and failover continues.
Timeouts and transport/network errors immediately mark the key `CoolingDown`
and rotate; authentication or permission failures mark that key `Failed`.
Temporary health states use the existing ten-minute cooldown. Per-key health
tracks status, last success/failure, failure count, and cooldown expiry.
Only `InvalidArgument` is treated as a request-specific provider failure and
stops key rotation. Existing configured request and operation timeouts are
preserved; timeout-triggered failover occurs as soon as that timeout expires.
The Interactions SDK is explicitly configured for one transport attempt; the
application owns status retries and key rotation. SDK timeouts are passed in
milliseconds after conversion from the client's seconds.
Successful structured responses are cached in-process for 15 minutes (up to
512 entries) by prompt, schema, and configured model set.

ADK agents validate structured-output schemas before each provider call and
log the model, generation configuration, response schema, and a request
payload with user text redacted. Gemini-specific ADK output models omit
`additionalProperties`; the service still validates returned JSON against its
strict application models. The same validation/logging callback is used by
guest review and registration-question agents.

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

Coordinator Gemini nodes have a total operation deadline that bounds all
key/model failover attempts; by default it is 30 minutes per node (derived from
the coordinator timeout and `MAX_ITERATIONS`). Other Gemini client operations
retain the configured provider and operation timeouts.
The ASP.NET-to-agent and mobile plan requests allow 250 and 260 seconds,
respectively. Same-event lock waiting is included in the coordinator timeout,
and cancellation is propagated to an active Gemini request. Override the
coordinator timeout and maximum iterations, along with the default checkpoint
path, using settings in `.env.example`.

Example safe fallback logs:
The timeout values are illustrative and reflect each request's configured
remaining budget.

```text
INFO Calling Gemini node=analyze_requirements model=gemini-primary api_key_index=1 api_key=****a1b2 retry_count=0 input_tokens_estimate=420 attempt_timeout_seconds=30.000
WARNING Retrying Gemini key 1 once after HTTP 503 node=analyze_requirements model=gemini-primary
WARNING Gemini key 1 failed with 503 node=analyze_requirements model=gemini-primary health_status=Unavailable failure_count=1
INFO Rotating to Gemini key 2 node=analyze_requirements model=gemini-primary reason=network
INFO Gemini key 2 succeeded node=analyze_requirements model=gemini-primary
```

```text
INFO Calling Gemini node=propose_timeline model=gemini-primary api_key_index=1 api_key=****a1b2 retry_count=0 input_tokens_estimate=180 attempt_timeout_seconds=30.000
WARNING Gemini key 1 failed with timeout node=propose_timeline model=gemini-primary health_status=CoolingDown failure_count=1
INFO Rotating to Gemini key 2 node=propose_timeline model=gemini-primary reason=network
INFO Calling Gemini node=propose_timeline model=gemini-primary api_key_index=2 api_key=****c3d4 retry_count=0 input_tokens_estimate=180 attempt_timeout_seconds=30.000
```
