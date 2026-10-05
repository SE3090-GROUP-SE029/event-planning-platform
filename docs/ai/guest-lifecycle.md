# Guest lifecycle

Guest management is split into independently owned lifecycle stages. A guest
upload creates or updates a contact and durably queues a public registration
link for background email delivery; it does not register the guest, reserve a
seat, create an invitation, or send an invitation.

## Stages

1. **Form setup** — A planner creates a draft form, selects questions, and
   publishes it. Only a published form accepts uploads and public submissions.
2. **Guest upload** — Each valid uploaded row creates or updates a `Guest`
   identified by event and normalized email. Guest changes and
   `RegistrationLinkEmailJob` outbox records commit in one transaction. Hosted
   delivery workers send queued links with bounded retries. No
   `RegistrationSubmission`, `Invitation`, or RSVP state is created.
3. **Registration** — A guest submits the public form. The submitted contact
   details update a matching uploaded `Guest`, or create one if none exists.
   The system creates a `RegistrationSubmission` in `PENDING_REVIEW` and queues
   its AI review durably.
4. **Review** — AI or a planner records `ACCEPTED` or `REJECTED`, together with
   the review source and timestamp. Review does not reserve seats or create
   invitations. A planner can override an unallocated review while a rejection
   email has not already been sent.
5. **Seat allocation** — The allocation worker processes accepted submissions
   in registration order. Eligible guests become `CONFIRMED` while capacity is
   available; the remainder become `WAITLISTED`. When capacity opens, the
   oldest eligible waitlisted guest is promoted.
6. **Invitation** — Only confirmed registrations receive an `Invitation` with
   a QR token. The delivery worker sends the invitation email and retries
   pending delivery.
7. **RSVP** — The invitation owns its RSVP state (`NOT_RESPONDED`, `ACCEPTED`,
   `MAYBE`, or `DECLINED`). A decline cancels the confirmed registration,
   releases its seat, and runs allocation again.
8. **Check-in** — A planner's QR scan creates one `GuestCheckIn` record for the
   registration. The check-in record is separate from registration and
   invitation state.

## Entity relationships

- `Event` has a registration form and event-scoped guest records.
- `Guest` has at most one registration submission per event.
- `RegistrationSubmission` belongs to a form and guest, contains answers, and
  may have one AI review, one invitation, and one check-in record.
- `Invitation` owns delivery metadata and RSVP state; it exists only for a
  confirmed registration.
- `GuestCheckIn` has a one-to-one relationship with a registration submission.

## Registration status transitions

| Current status | Next status | Trigger |
| --- | --- | --- |
| `PENDING_REVIEW` | `ACCEPTED` or `REJECTED` | AI or planner review |
| `ACCEPTED` | `CONFIRMED` or `WAITLISTED` | Seat allocation |
| `REJECTED` | `ACCEPTED` | Planner override before rejection email delivery |
| `WAITLISTED` | `CONFIRMED` | Seat allocation or promotion |
| `CONFIRMED` | `CANCELLED` | Planner cancellation or RSVP decline |
| Any unallocated status | `CANCELLED` | Planner cancellation |

`CANCELLED` is terminal. AI and manual review do not allocate seats; invitation
creation is downstream of successful allocation.
