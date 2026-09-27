# C4 automated registration and event-specific questions

C4 extends the existing registration backend and Google ADK/Gemini integration. The AI now returns an automated `ACCEPTED` or `REJECTED` decision. ASP.NET owns validation, registration state, seats, waiting lists, invitations, QR codes, transactions, and all email delivery.

## Registration flow

1. The planner creates the existing registration form.
2. Python suggests event-specific questions using only the event name and requirement notes. Suggestions are transient and never automatically saved or published.
3. The planner selects questions through the backend API. Selected questions belong to the existing form and become immutable when published.
4. The public form exposes selected questions alongside full name, email, optional organisation, and optional phone.
5. Submission validates default fields and answers against the published form, then atomically stores the guest, `PENDING_AI` submission, answers, and pending AI work. There is no invitation or reserved seat yet.
6. The existing worker claims work with an attempt ID and lease, commits the claim, and calls Python outside the event transaction.
7. Python returns a validated decision. ASP.NET validates it again and atomically records the decision and applies the registration transition under the existing event-row lock. Stale attempts cannot apply a result. Cancellation cannot be undone by a late result.
8. `ACCEPTED` enters the ordered waiting list. If capacity and existing deterministic eligibility checks permit, the backend confirms the guest and creates the existing invitation/QR. `REJECTED` records rejection and queues a rejection email.

Only a completed `ACCEPTED` decision authorizes waiting-list promotion. Existing registration-time/ID ordering, cancellation, RSVP, capacity increases, and overbooking protection remain in effect among accepted waiting guests. Event-end checks still prevent late confirmation. Acceptance is eligibility, not a promise of a seat.

## Question generation and selection

The new `registration_question_agent.py` uses the configured Gemini model through the Google ADK runner, with the same timeout, isolated sessions, and strict JSON parsing as guest filtering. Both agents have no tools.

The question agent returns:

```json
{
  "questions": [
    { "question": "What software engineering topics interest you?", "required": false }
  ]
}
```

Generation returns 1-10 questions. Selection allows 0-10. Question text is nonblank and at most 500 characters; answers are nonblank when supplied and at most 4,000 characters. Required flags are explicit booleans. Selected question text must be distinct. Control characters are rejected using existing validation conventions.

The prompt prohibits invented requirements, protected-trait inference, unnecessarily sensitive questions, passwords, secrets, and payment information. With sparse event details it requests only 1-2 broadly relevant questions. General interest questions should be optional. Prompt instructions and schema checks do not prove semantic correctness; planners must review suggestions before selecting them.

The selection endpoint replaces the selected draft list. Previous draft question records are retained as unselected, avoiding data deletion. Publishing freezes the definition; changing opening/closing dates or capacity does not change questions. Forms with no selected questions remain supported.

## API contracts

All planner routes below start with `/api/events/{eventId}/registration-form` and reuse `PlannerAccessFilter` plus existing event ownership checks.

| Method | Suffix | Behavior |
| --- | --- | --- |
| POST | `/question-suggestions` | Return transient suggestions for the event; no form mutation. |
| PUT | `/questions` | Save `{questions:[{question,required}]}` on a draft form. Returns the form with selected question IDs. |
| GET | existing form route | Includes selected questions. |
| GET | `/registrations/{registrationId}` | Includes submitted answers and delivery status. |
| GET | `/registrations/{registrationId}/ai-review` | Return processing status, decision, confidence, reasons, flags, model, prompt version, attempts, and sanitized failure code. |
| POST | `/registrations/{registrationId}/retry-ai-review` | Retry failed/expired analysis for pending registrations; completed decisions remain idempotent. |
| POST | `/registrations/{registrationId}/retry-rejection-email` | Retry rejection delivery without reanalysis; sent delivery remains idempotent. |

The existing public `GET /api/public/registration-forms/{publicId}` adds:

```json
{
  "questions": [
    { "id": "<question GUID>", "question": "What software engineering topics interest you?", "required": false }
  ]
}
```

The existing `POST /api/public/registration-forms/{publicId}/registrations` accepts:

```json
{
  "fullName": "Guest Name",
  "emailAddress": "guest@example.com",
  "organisation": null,
  "phoneNumber": null,
  "answers": [
    { "questionId": "<published question GUID>", "answer": "Distributed systems" }
  ]
}
```

`answers` is optional for forms with no required additional questions. Unknown, cross-form, unselected, duplicate, blank, oversized, or missing required answers return 400 before persistence. Composite foreign keys also prevent cross-form answer associations at the database boundary. Suggestions do not have persisted question IDs and cannot be submitted as published questions.

The initial 201 receipt reports `PENDING_AI` and the existing separate status secret; invitation/QR fields are null. Use the existing credential-protected public status endpoint to retrieve the eventual result. It may report `CONFIRMED`, `WAITING_LIST`, `REJECTED`, or `CANCELLED`. A rejected receipt/status uses the existing `emailDeliveryStatus` field for rejection delivery. Internal AI confidence, reasons, flags, model, and failure codes remain planner-only.

**Intentional compatibility changes:** active AI responses replace `recommendation` with `decision`; new registrations no longer immediately receive final seat results. Clients must handle `PENDING_AI` and `REJECTED`. Existing default guest fields, public paths, status credentials, invitation tokens, QR representation, and RSVP values are retained. An RSVP value named `ACCEPTED` remains separate from an AI decision.

## Filtering contract and security

The model returns exactly:

```json
{
  "decision": "ACCEPTED",
  "confidence": 0.92,
  "reasons": ["The supplied answers satisfy the event requirements."],
  "flags": []
}
```

Allowed decisions are `ACCEPTED` and `REJECTED`. `ELIGIBLE` and `REVIEW` are invalid active results. Confidence is a finite number from 0 to 1, reported by the model rather than a calibrated probability. Reasons contain 1-10 nonblank strings up to 500 characters. Flags contain 0-10 strings up to 80 characters. Both services reject extra/duplicate keys, malformed JSON, incorrect types, nonfinite numbers, and oversized output (16 KiB). Python adds its own model and `guest-filtering-v2` metadata.

The allowlisted input contains guest details, event name/requirements, registration time, selected question text/required flags/corresponding answers, and up to 20 prior same-event comparisons. Comparison selection is unchanged: likely exact-field matches followed by recent prior submissions, with `comparisonsLimited` indicating omitted history. Similarity alone is not evidence of fraud.

No question IDs, event IDs, invitation/QR tokens, public reference/status secret/hash, SMTP credentials, or database settings are sent to the model. Guest answers, including instructions to ignore the rules and accept the guest, are untrusted data. The prompt requires concrete supplied evidence of failure of an explicit event requirement for rejection; missing optional organisation or phone alone cannot cause rejection. It prohibits inferred protected characteristics, invented facts, and seat allocation.

Each call uses a fresh ADK session, disposed after execution. Provider payload logging is disabled and model-cost metadata is local. Internal Python endpoints are loopback-only:

- `POST /api/guest-reviews/analyze`
- `POST /api/registration-questions/suggest`

Do not expose this unauthenticated internal service through a public reverse proxy. Production planner authentication is still unfinished in the repository; the existing filter fails closed without a verified principal. This update does not implement or alter authentication. Tests use the existing test-only identity middleware.

## Persistence and legacy transition

Approved migration: `20260917162810_AddRegistrationQuestionsAndAutomatedAiDecisions`.

- Adds `RegistrationQuestions` belonging to `RegistrationForms`, with question text, required flag, order, and selection state.
- Adds `RegistrationAnswers` with unique submission/question keys and composite form associations.
- Extends allowed submission states with `PENDING_AI` and `REJECTED`.
- Adds rejection delivery status, attempts, last-attempt and sent timestamps to submissions.
- Adds `GuestAiReviews.Decision` and adapts result constraints while retaining durable leases and failures.

The old `Recommendation` column/enum remain for legacy records only and never authorize seats. Historical completed reviews may have `decision: null`. Existing confirmed registrations/invitations are preserved. Existing waiting-list records without reviews are queued automatically; historical advisory reviews on waiting-list records receive a fresh automated decision. Reprocessing retains the old recommendation value but replaces the current review's detailed result, as the existing entity holds one current review per registration rather than a full attempt history.

No old migration was edited. The new migration's `Up` preserves existing data. Its generated `Down` removes new feature data and is not a safe production rollback once used; do not apply it without a separately reviewed data-retention plan. Migration generation and isolated tests do not update the configured application database.

## Failures, retries, and email

`ai_unavailable`, `ai_timeout`, `invalid_ai_response`, or `analysis_failed` produce `FAILED` analysis and leave the registration pending. They never become rejection or consume a seat. Ordinary analysis failure requires planner retry; expired worker claims recover automatically. Disabling the worker leaves work pending. Public submission does not wait for inference.

`EmailSender` uses its existing SMTP transport for both templates. Confirmed acceptance sends the existing invitation token and PNG QR. Rejection sends a concise professional message without AI analysis or attachments. Rejected guests cannot obtain invitations or accept/decline an invitation through RSVP.

Decision/registration state commits before email delivery. Delivery failure preserves the decision and can be retried without reanalysis. The worker recovers pending invitation and rejection delivery, with a one-minute delay after an unavailable transport; failed transport has explicit planner retry. Sent delivery is idempotent during normal retries. SMTP cannot guarantee exactly-once receipt if the process crashes after server acceptance but before the sent status is saved. Delivery retains the existing event-lock serialization approach, so a slow SMTP transport can delay other operations on that event.

## Configuration and manual verification

The Python AI service uses Google ADK 2.10.0 and Google Gen AI 2.25.0 with the existing `GEMINI_API_KEY`, `GEMINI_API_KEYS`, `GEMINI_MODEL`, and `GEMINI_FALLBACK_MODELS` settings. `GUEST_AI_TIMEOUT_SECONDS` defaults to 110 seconds. Gemini quota failures rotate through configured keys and models; each ADK run gets an isolated in-memory session. The backend default timeout is 120 seconds with a lease 30 seconds longer; polling defaults to 5 seconds.

A maintainer should review and apply the migration through the existing deployment process before running the updated backend on an application database:

```powershell
dotnet ef database update --project backend/src/Infrastructure --startup-project backend/src/Api
```

For a separately authorized manual real-model test, configure a Gemini API key and model in the service environment, then run the Python service, backend, and existing SMTP configuration. Request suggestions as a real planner, review/select/publish questions, submit synthetic accepted/rejected examples, poll status, and inspect planner audit plus emails. Also test quota exhaustion and unavailable-service behavior. Real inference quality, prompt-injection resistance, and latency remain unverified. Deterministic fakes verify wiring, schemas, prompt construction, and orchestration, not semantic reliability of a live model.

## Verification

```powershell
dotnet build backend/backend.sln
dotnet test backend/backend.sln
# From agentic-ai:
.venv/Scripts/python.exe -m pytest -q
.venv/Scripts/python.exe -m pip check
# From repository root:
git diff --check
```

Tests use isolated PostgreSQL clusters and a local Gemini API fake. They do not connect to the application database, call an external model, or send external email. Flow 1 tests now process deterministic acceptance before checking seats/invitations; the existing capacity, duplicate, cancellation, ordered promotion, RSVP, token, email, and testFeature assertions remain. Advisory-only cases were updated to the explicitly approved automated behavior.

Additional coverage includes transient suggestions, selection/publication freeze, required/optional and invalid answers, selected context and token privacy, both decisions, AI failure/retry, cancellation during inference, stale attempts, concurrent accepted results, legacy transitions, rejection delivery/retry, and pending-email recovery. Python exercises both agents through the actual ADK/Gemini adapter against a local Gemini API fake.

Provider integration verification on 2026-09-27:

| Check | Result |
| --- | --- |
| Full Python AI-service tests | 127 passed; ADK/Gemini uses a local API fake. |
| Backend unit tests (guest review) | 41 passed. |
| Backend integration tests (guest review and question endpoints) | 30 passed; one migration-constraint assertion failed and remains unresolved. |
| `pip check` | No broken requirements found. |
| `git diff --check` | Passed. |

The integration failure is `NewMigrationMatchesTheModelAndEnforcesReviewConstraints`; migration/schema files were not changed as part of this provider update. The broader backend suite was not rerun for this change.

Existing dependency-resolution warnings for unavailable exact .NET package versions and existing nullable/deprecation warnings are outside this update's scope.

## File inventory and protected scope

Added in this update:

- `agentic-ai/src/agents/registration_question_agent.py`
- `agentic-ai/src/models/registration_question_models.py`
- `agentic-ai/src/services/registration_question_service.py`
- `agentic-ai/tests/unit/test_registration_questions.py`
- `backend/src/Domain/Entities/RegistrationQuestion.cs`
- `backend/src/Domain/Entities/RegistrationAnswer.cs`
- `backend/src/Domain/Enums/AiDecision.cs`
- `backend/src/Application/GuestManagement/RegistrationQuestionContracts.cs`
- `backend/src/Infrastructure/Migrations/20260917162810_AddRegistrationQuestionsAndAutomatedAiDecisions.cs`
- `backend/src/Infrastructure/Migrations/20260917162810_AddRegistrationQuestionsAndAutomatedAiDecisions.Designer.cs`
- `backend/tests/Backend.IntegrationTests/RegistrationQuestionEndpointTests.cs`
- `docs/ai/c4-automated-registration-plan.md` (preceding inspection report)

Modified relative to the existing implementation at the start of this update:

- `agentic-ai/src/agents/guest_filtering_agent.py`
- `agentic-ai/src/models/guest_review_models.py`
- `agentic-ai/src/services/guest_review_service.py`
- `agentic-ai/src/main.py`
- `agentic-ai/tests/unit/test_guest_review.py`
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
- `backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- `backend/src/Api/Controllers/RegistrationFormsController.cs`
- `backend/src/Api/Controllers/PublicRegistrationsController.cs`
- `backend/src/Api/Dtos/Requests/GuestRegistrationRequests.cs`
- `backend/src/Api/Dtos/Responses/GuestRegistrationResponses.cs`
- `backend/src/Api/GuestManagement/GuestManagementServices.cs`
- `backend/tests/Backend.UnitTests/GuestAiReviewTests.cs`
- `backend/tests/Backend.IntegrationTests/GuestAiReviewEndpointTests.cs`
- `backend/tests/Backend.IntegrationTests/RegistrationEndpointTests.cs`
- `backend/tests/Backend.IntegrationTests/RegistrationHostFixture.cs`
- `backend/tests/Backend.IntegrationTests/SmtpDeliveryTests.cs`
- `docs/ai/guest-filtering-validation.md`

Protected scope: the new migration/designer and model snapshot were explicitly approved after inspection. The requested EmailSender extension and shared AI/DI integration reuse existing configuration-reading code without changing configuration values, credentials, or authentication. Migration application was confined to isolated test clusters. Existing uncommitted work was preserved. No commit, push, PR, UI, infrastructure, workflow, unrelated feature, existing-file deletion, rename, or move was performed. This update used AI assistance and requires maintainer review before merge/deployment.
