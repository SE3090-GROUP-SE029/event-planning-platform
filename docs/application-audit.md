# Application capability audit

Date: 2026-09-19

The checkout does not contain the referenced `docs/architecture.md`, so this
audit uses the repository source, project files, and EF Core model as the
source of truth. Generated build output and dependency directories are
excluded.

## Backend capability matrix

| Module | Implemented APIs | Role access | Status |
| --- | --- | --- | --- |
| Authentication | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/me` | Register/login/refresh/logout are anonymous; `me` requires an authenticated user | Implemented |
| Events | `POST /api/events`, `GET /api/events`, `GET /api/events/{id}`, `PUT /api/events/{id}`, `DELETE /api/events/{id}` | Any authenticated role; ownership is enforced, while admins can list/read across users | Implemented |
| Admin event review | `GET /api/admin/events`, `GET /api/admin/events/{id}` | `ADMIN` only | Implemented |
| Vendor profile | `POST /api/vendors`, `GET /api/vendors/me`, `PUT /api/vendors/me` | `VENDOR` only | Implemented |
| Diagnostic test API | `GET /api/test/ping`, `POST /api/test/message`, `GET /api/test/message/{id}` | Anonymous diagnostic endpoints | Implemented as a backend diagnostic surface; not a product feature |

### Backend modules and services

- API controllers: `AuthController`, `EventsController`,
  `AdminEventsController`, `VendorsController`, and `TestController`.
- Application services: authentication/token services, event services
  (including admin event review), vendor service, and the diagnostic test
  service.
- Domain entities: `User`, `Role`, `UserRole`, `RefreshToken`, `Event`, `Vendor`,
  and `TestMessage`.
- Roles: `ADMIN`, `EVENT_PLANNER`, and `VENDOR`.
- Authorization policies: `AdminOnly`, `EventPlannerOnly`, and `VendorOnly`.
  The event controller currently permits all authenticated roles; the
  event-planner policy is registered but not applied by a controller.

### Database model

The active EF Core model contains these tables:

`Users`, `Roles`, `UserRoles`, `RefreshTokens`, `Events`, and `Vendors`.

`TestMessage` has a service/API implementation but no `DbSet` or migration-backed
table in the active model.

## React/admin audit

| Frontend item | Backend support | Result |
| --- | --- | --- |
| Login and registration pages | Auth endpoints | Kept |
| Protected dashboard landing page | No dashboard endpoint | Rebuilt as a non-statistical module launcher; fake metrics and hard-coded events removed |
| Admin event list and filters | `GET /api/admin/events` | Kept |
| Admin event details | `GET /api/admin/events/{id}` | Kept |
| Vendor profile page | Vendor profile endpoints | Kept and routed at `/vendor/profile` for `VENDOR` |
| Dashboard statistics, booking cards, calendar rail, schedule/reports/articles/billing/documents/settings links | No matching API | Removed |
| Browser backend integration test page/API client | Diagnostic endpoint only | Removed from product routes and navigation |

The sidebar now renders only dashboard, events (for admins), vendor profile
(for vendors), and sign out. The top bar is limited to event search and the
authenticated user indicator; unsupported filter categories and inert utility
buttons were removed.

## Flutter/mobile audit

| Frontend item | Backend support | Result |
| --- | --- | --- |
| Login and registration | Auth endpoints | Kept |
| Home/dashboard | No dashboard endpoint | Rebuilt as a role-aware launcher without fake statistics |
| Event list, create, details, update, delete | Event endpoints | Kept |
| Vendor profile | Vendor profile endpoints | Kept and routed at `/vendors/profile` |
| API test screen/client/models | Diagnostic endpoint only | Removed from routes and auth/home navigation |
| Hard-coded event dashboard cards and timeline | No dashboard endpoint | Removed |
| Unused pastel dashboard widgets | No active consumers after dashboard cleanup | Removed |

The mobile route set now contains authentication, the supported home page,
event CRUD screens, and the vendor profile screen. Loading, error, empty, and
save/delete feedback in the existing event/vendor screens remains the source
of truth for those API-backed flows.

## Deliberately unchanged

Backend authentication/authorization, EF Core migrations, infrastructure, and
configuration were not modified. The backend diagnostic controller was also
left intact because it is an implemented API and may still be used by
development/integration tests; it is no longer exposed as application
functionality in either client.
