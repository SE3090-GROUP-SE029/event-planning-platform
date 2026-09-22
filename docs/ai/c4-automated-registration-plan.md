# C4 automated registration: phase 1 inspection

Status: the user approved implementation and the migration after this inspection. This document records the phase 1 proposal; see `guest-filtering-validation.md` for the implemented contracts, files, verification, and limitations. Branch: `jazeel`. Existing uncommitted work is preserved.

## Inspected implementation

Read the current repository instructions, README, C4 documentation, Python agents/models/services, .NET registration and AI layers, email/QR implementation, dependency manifests, and Flow 1/AI tests. `docs/architecture.md` referenced by AGENTS.md does not exist; the README, feature documentation, and actual layered implementation provide the architecture evidence.

The Python implementation already uses Google ADK 2.9.1, LiteLLM 1.101.0, Ollama, and `qwen3:8b`. It has strict output validation, bounded requests, sanitized errors, isolated ADK sessions, and a deterministic test using a local Ollama protocol fake. The README's older LangGraph label does not describe the implemented C4 agent.

The backend already has registration forms, submissions, invitations, event-scoped database locks, an ordered waiting list, QR generation, SMTP delivery tracking, and planner ownership checks. `GuestAiReview` provides durable work, leases, attempt fencing, failure tracking, and retry/audit endpoints.

Currently, submission immediately allocates a seat and sends an invitation before AI analysis. Analysis stores only `ELIGIBLE`, `REVIEW`, or `REJECTED` recommendations. Forms have no selected questions; submissions have no answers. Registration database constraints allow only `CONFIRMED`, `WAITING_LIST`, and `CANCELLED`. These facts make a migration necessary.

## Proposed question and answer flow

Add a question-suggestion agent alongside the existing filtering agent, using the same ADK/LiteLLM/Ollama integration and settings. Send only the event name and requirement notes. Return strict JSON containing `questions`, each with `question` and `required`.

Suggestions are transient. A planner explicitly submits a selected list for the existing draft form. Only saved selections are exposed after publication. Proposed bounds are at most 10 questions, 500 characters per question, and 4,000 characters per answer, following the existing bounded-list, reason-text, and event-notes conventions. Require strict booleans, nonblank question text, and valid bounded answers. Prompts prohibit hidden requirements, protected-trait inference, unnecessarily sensitive questions, and requests for credentials/payment details. Deterministic validation cannot prove semantic safety of every generated question; planner selection remains mandatory.

Proposed publication policy, included in the approval request: freeze selected questions when the form is published. This keeps answers tied to a stable definition without adding a second form system or versioning feature. Existing forms can retain an empty selection.

The public submission adds optional `answers: [{questionId, answer}]`. Existing bodies remain valid for forms with no required additional questions. Validate question IDs against that published form, reject unknown/cross-form/duplicate IDs, and enforce required selected questions. Persist submission, answers, and pending AI work together.

## Proposed schema and migration

Generate one EF migration named `AddRegistrationQuestionsAndAutomatedAiDecisions`, its designer, and an updated `AppDbContextModelSnapshot.cs`. Keep prior migrations intact. Proposed changes:

| Entity/table | Change and purpose |
| --- | --- |
| `RegistrationForm` / `RegistrationForms` | Add a questions relationship to the existing form; no second form table. |
| New `RegistrationQuestion` / `RegistrationQuestions` | Store ID, form relationship, question text, required flag, and display order for planner selections. |
| New `RegistrationAnswer` / `RegistrationAnswers` | Store submission/question relationships and answer text; enforce one answer per question per submission and consistent form associations. |
| `RegistrationSubmission` / `RegistrationSubmissions` | Allow `PENDING_AI` and `REJECTED`; add rejection-email delivery status, attempts, last-attempt and sent timestamps. A rejected registration must not need a fabricated invitation or QR. |
| `GuestAiReview` / `GuestAiReviews` | Add nullable `Decision`, constrained to `ACCEPTED`/`REJECTED`, and adapt result constraints. Keep the old recommendation column only for legacy records; it cannot authorize promotion. Preserve analysis status, confidence, reasons, flags, leases, and retry tracking. |

Proposed legacy policy, included in the approval request: preserve already confirmed registrations and their invitations. Existing waiting-list guests require a fresh automated decision before promotion. Do not silently reinterpret an old advisory `ELIGIBLE`, `REVIEW`, or `REJECTED` value as a binding decision. Retain old recommendation data rather than dropping its column. Retry/reprocessing must handle legacy reviews explicitly and must not revoke prior confirmed invitations.

Approval authorizes generating the migration and testing it against the existing disposable test database mechanism. Applying it to an application database is a separate deployment step.

## Automated processing and email

New submissions start `PENDING_AI`, with no invitation or reserved seat. Reuse the durable review worker; execute the model call outside the event transaction. Include selected question text, required flags, and corresponding guest answers in the allowlisted AI context. Guest answers are data, never instructions. Optional missing phone/organisation alone cannot justify rejection.

The strict model result is `{decision, confidence, reasons, flags}` with `decision` restricted to `ACCEPTED`/`REJECTED`. The service supplies model and prompt-version metadata for audit. Confidence remains a model-reported value, not a calibrated probability.

Apply a validated result and registration transition atomically under the event lock, verifying the current attempt. For `ACCEPTED`, enter the existing ordered waiting list and run capacity-controlled promotion; create the existing invitation/QR only upon confirmation. For `REJECTED`, persist rejection and queue its email. Late results must not resurrect cancelled registrations. Stale worker attempts cannot apply decisions or send duplicate business actions.

Malformed output, timeout, unavailable services, and unexpected analysis failures leave the registration pending with an explicit `FAILED` AI review and a sanitized failure code. Existing planner retry requeues processing. These failures never become rejection.

Reuse `EmailSender` and its current SMTP settings for concise rejection messages as explicitly requested. Keep internal AI analysis out of guest emails. Preserve invitation delivery behavior and tokens. Track rejection delivery separately from AI processing, recover pending delivery after a restart, and allow failed delivery to be retried without rerunning AI. SMTP delivery cannot guarantee exactly-once receipt if a process crashes after the server accepts a message but before the sent status is saved.

Cancellation and capacity changes continue using the existing event lock and registration-time/ID ordering. Promotion additionally requires a completed `ACCEPTED` decision; rejected, failed, and still-pending registrations are ineligible.

## API changes

Proposed additions under the existing planner route `/api/events/{eventId}/registration-form`:

- `POST question-suggestions`: retrieve transient suggestions through Python.
- `PUT questions`: save the selected draft-form questions.
- `POST registrations/{registrationId}/retry-rejection-email`: retry delivery without changing the decision, analogous to the existing invitation retry.

Add Python `POST /api/registration-questions/suggest`, using the existing local-only service boundary. Keep guest analysis at `/api/guest-reviews/analyze`.

Existing planner/public form responses gain selected questions. Existing submission requests gain optional answers. Planner registration retrieval can show saved answers. Existing audit and AI retry routes remain, with `decision` replacing `recommendation` in the active contract.

Intentional compatibility changes: `recommendation` becomes `decision`; new submissions initially report `PENDING_AI` instead of an immediate final seat result; public status can later report `REJECTED`. Existing status credentials, default guest fields, route shapes, capacity rules, and invitation token/QR fields are reused. Legacy completed reviews may have no automated decision and must be documented as historical, not interpreted as approval.

No new environment values, credentials, dependencies, or authentication changes are expected. Existing planner filters and ownership checks apply to the added endpoints. Changes to `EmailSender` and service registration are limited to the explicitly requested feature; configuration binding and credential handling remain as they are.

## Exact files likely affected

Modify existing Python files:

- `agentic-ai/src/agents/guest_filtering_agent.py`
- `agentic-ai/src/models/guest_review_models.py`
- `agentic-ai/src/services/guest_review_service.py`
- `agentic-ai/src/main.py`
- `agentic-ai/tests/unit/test_guest_review.py`

Add Python files:

- `agentic-ai/src/agents/registration_question_agent.py`
- `agentic-ai/src/models/registration_question_models.py`
- `agentic-ai/src/services/registration_question_service.py`
- `agentic-ai/tests/unit/test_registration_questions.py`

Keep shared ADK transport in the existing service module or extract a small shared helper only if necessary; no second provider integration.

Modify existing backend files:

- `backend/src/Domain/Entities/RegistrationForm.cs`
- `backend/src/Domain/Entities/RegistrationSubmission.cs`
- `backend/src/Domain/Entities/GuestAiReview.cs`
- `backend/src/Domain/Enums/RegistrationStatus.cs`
- `backend/src/Application/GuestManagement/RegistrationContracts.cs`
- `backend/src/Application/GuestManagement/RegistrationValidator.cs`
- `backend/src/Application/GuestManagement/RegistrationService.cs`
- `backend/src/Application/GuestManagement/IGuestRegistrationRepository.cs`
- `backend/src/Application/GuestManagement/GuestAiReviewContracts.cs`
- `backend/src/Application/GuestManagement/GuestAiReviewService.cs`
- `backend/src/Infrastructure/Data/AppDbContext.cs`
- `backend/src/Infrastructure/Data/GuestManagementConfiguration.cs`
- `backend/src/Infrastructure/Repositories/GuestRegistrationRepository.cs`
- `backend/src/Infrastructure/Repositories/GuestAiReviewRepository.cs`
- `backend/src/Infrastructure/ExternalServices/AiClient.cs`
- `backend/src/Infrastructure/ExternalServices/EmailSender.cs`
- `backend/src/Api/Controllers/RegistrationFormsController.cs`
- `backend/src/Api/Controllers/PublicRegistrationsController.cs`
- `backend/src/Api/Dtos/Requests/GuestRegistrationRequests.cs`
- `backend/src/Api/Dtos/Responses/GuestRegistrationResponses.cs`
- `backend/src/Api/GuestManagement/GuestManagementServices.cs`
- `backend/src/Api/GuestManagement/GuestAiReviewWorker.cs`

Add backend files:

- `backend/src/Domain/Entities/RegistrationQuestion.cs`
- `backend/src/Domain/Entities/RegistrationAnswer.cs`
- `backend/src/Domain/Enums/AiDecision.cs`
- `backend/src/Application/GuestManagement/RegistrationQuestionContracts.cs`

Protected files, only after approval:

- `backend/src/Infrastructure/Migrations/<generated timestamp>_AddRegistrationQuestionsAndAutomatedAiDecisions.cs`
- `backend/src/Infrastructure/Migrations/<generated timestamp>_AddRegistrationQuestionsAndAutomatedAiDecisions.Designer.cs`
- `backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.cs`

Update existing tests and feature documentation:

- `backend/tests/Backend.UnitTests/GuestAiReviewTests.cs`
- `backend/tests/Backend.UnitTests/RegistrationValidationTests.cs`
- `backend/tests/Backend.IntegrationTests/GuestAiReviewEndpointTests.cs`
- `backend/tests/Backend.IntegrationTests/RegistrationEndpointTests.cs`
- `backend/tests/Backend.IntegrationTests/RegistrationHostFixture.cs`
- `backend/tests/Backend.IntegrationTests/SmtpDeliveryTests.cs`
- `docs/ai/guest-filtering-validation.md`

Add `backend/tests/Backend.IntegrationTests/RegistrationQuestionEndpointTests.cs` for planner selection, publication, answer validation, and ownership boundaries.

## Verification after implementation

Update advisory-specific assertions for the explicitly changed business behavior. Adapt Flow 1 setup to process deterministic fake acceptance before asserting confirmation, QR/email, cancellation, RSVP, capacity, and promotion outcomes; preserve those checks and the existing eligibility-policy extension coverage.

Add acceptance/rejection orchestration, AI failure/retry, question generation, strict schemas, answer/context privacy, injection-as-data, publication boundaries, cancellation races, stale claims, concurrent capacity, rejection delivery/retry, and legacy transition tests. Continue exercising the real ADK/LiteLLM adapter against a local protocol fake. Such tests verify transport and prompt construction, not the real model's semantic reliability.

Run `dotnet build`, `dotnet test`, Python `pytest`, `pip check`, and `git diff --check`, including testFeature and disposable-database migration/model checks. No new tests were run for this inspection-only phase.

Real Qwen inference remains a separate manual verification. Document `ollama pull qwen3:8b`; do not execute it automatically or claim live-model success without testing.

The implementation stays on `jazeel`, without commits, pushes, PR creation, UI changes, existing-file deletion/renaming/moving, infrastructure changes, or application-database updates.
