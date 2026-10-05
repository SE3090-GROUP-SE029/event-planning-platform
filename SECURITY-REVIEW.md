# Security Review

## Scope and method

This review examined the backend API and authorization paths, registration and
check-in flows, database entities and migrations, React and Flutter request
flows, the Agentic AI service, and CI/deployment configuration. The review was
read-only and assessed the repository state present at the time of review.

## Findings

### SR-01 — Public registration can claim an existing guest identity

- **Severity:** Medium
- **Confidence:** 9/10
- **Affected code:** `backend/src/Application/GuestManagement/RegistrationService.cs`
  (lines 152–169); `backend/src/Application/GuestManagement/BulkGuestUploadService.cs`
  (lines 90–115); `backend/src/Api/Controllers/PublicRegistrationsController.cs`
  (lines 32–36)
- **Issue:** Public registration accepts an email address without verifying
  control of that address. When the address matches an existing guest, the
  service updates that guest's profile. Uploaded guest lists create matching
  guest records before public registration. The public submit response also
  returns a status secret, which can be used to retrieve invitation
  credentials for an approved registration. A person who knows the public
  registration link and an invitee's email can therefore claim or alter that
  guest record and obtain credentials associated with the registration.
- **Impact:** Unauthorized modification of an invitee's stored details and
  potential disclosure of invitation credentials.
- **Recommended remediation:** Verify email ownership before attaching a
  submission to or updating an existing guest identity, and gate issuance or
  retrieval of invitation credentials on verified ownership.

## Positive controls observed

- JWT validation checks the signing key, issuer, audience, and token lifetime.
- Passwords are stored using BCrypt hashing.
- Registration and invitation credentials use cryptographically secure
  randomness.
- Planner actions perform event ownership checks.
- AI guest-review prompts identify supplied values as untrusted, and
  structured AI output is validated by application models.

## Areas reviewed without additional findings

Backend route authorization and vendor-recommendation ownership checks,
registration and check-in flows, schema and migration changes, AI routes and
prompt handling, web and mobile request flows, and CI/deployment
configuration. This statement records the result of this review; it is not a
guarantee that the reviewed areas are free of vulnerabilities.
