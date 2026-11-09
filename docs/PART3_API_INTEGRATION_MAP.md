# RaceDay Part 3 API Integration Map

This document records the API contract implemented in the current Part 3
readiness sprint. The MVC application must call these HTTP endpoints; it must
not connect to SQL Server directly. JSON uses the ASP.NET Core web defaults
(camel-case property names).

## Session and role rules

- Login creates a server-side session and returns the `.RaceDay.Session` cookie.
- Send that cookie on subsequent protected requests. The session expires after
  30 minutes of inactivity.
- Public registration creates `PARTICIPANT` accounts only. The submitted role
  is never trusted to grant organiser access.
- Organiser-only endpoints require an `ORGANISER` session and, for event data,
  verify ownership of the event.
- Participant endpoints return data belonging to the signed-in participant.
- The API currently uses session cookies, not bearer tokens.

## Authentication

| Method and route | Session / role | Request DTO | Response |
|---|---|---|---|
| `POST /api/auth/register` | Public | `RegisterRequest`: `firstName`, `lastName`, `email`, `password`, `role`, optional `phoneNumber`. Public creation is restricted to participant role. | `201`: `{ message }`; `400`: validation; `403`: attempted organiser registration; `409`: duplicate email. |
| `POST /api/auth/login` | Public | `LoginRequest`: `email`, `password` | `200 AuthResponse`: `userId`, `firstName`, `lastName`, `email`, `role`; sets session cookie. `400` validation; `401` invalid credentials. |
| `POST /api/auth/logout` | Public; clears the current session if present | None | `204` |

## Profile

The `GET /api/auth/me` route remains available for compatibility. It and
`/api/users/me` return the same safe profile response.

| Method and route | Session / role | Request DTO | Response |
|---|---|---|---|
| `GET /api/users/me` | Authenticated user, either role | None | `200 UserProfileResponse`: `userId`, `firstName`, `lastName`, `email`, `phoneNumber`, `role`, `createdAt`; `401` if no valid session. |
| `GET /api/auth/me` | Authenticated user, either role; compatibility alias | None | Same `UserProfileResponse` as `/api/users/me`. |
| `PUT /api/users/me` | Authenticated user, either role | `UserProfileUpdateRequest`: required `firstName` and `lastName` (1–50 characters), optional `phoneNumber` (phone format, max 20 characters). | `200 UserProfileResponse`; `400` invalid fields; `401` invalid session. |
| `PUT /api/auth/me` | Authenticated user, either role; compatibility alias | Same `UserProfileUpdateRequest` | Same `UserProfileResponse`. |

Only first name, last name and phone number are editable. Email, role, account
ID, creation date and password hash are not accepted as update fields.
Passwords and password hashes are never part of a response DTO.

## Events

| Method and route | Session / role | Request DTO | Response |
|---|---|---|---|
| `GET /api/events` | Public | None | `200` list of event projections: `eventID`, `eventName`, `description`, `eventDate`, `location`, `distance`, `eventTypeID`, `eventTypeName`, `organiserID`. |
| `GET /api/events/{id}` | Public | Route `id` | `200` event projection; `404` missing event. |
| `POST /api/events` | Organiser | `EventRequest`: `eventName`, `description`, `eventDate`, `location`, `distance`, `eventTypeId` | `201` event projection; `400` invalid data/type; `401` unauthenticated; `403` wrong role. |
| `PUT /api/events/{id}` | Organiser who owns the event | `EventRequest` | `200` updated event projection; `400`, `401`, `403`, `404`. |
| `DELETE /api/events/{id}` | Organiser who owns the event | None | `204`; `401`, `403`, `404`; `409` if related records prevent deletion. |

## Categories

| Method and route | Session / role | Request DTO | Response |
|---|---|---|---|
| `GET /api/events/{eventId}/categories` | Public | Route `eventId` | `200` list of category projections (`categoryID`, `eventID`, `categoryName`, `description`, `minimumAge`, `maximumAge`); `404` missing event. |
| `GET /api/categories/{id}` | Public | Route `id` | `200` category projection; `404` missing category. |
| `POST /api/events/{eventId}/categories` | Organiser who owns the event | `CategoryRequest`: `categoryName`, optional `description`, `minimumAge`, `maximumAge` | `201` category projection; `400`, `401`, `403`, `404`; `409` duplicate name. |
| `PUT /api/categories/{id}` | Organiser who owns the category's event | `CategoryRequest` | `200` category projection; `400`, `401`, `403`, `404`; `409` duplicate name. |
| `DELETE /api/categories/{id}` | Organiser who owns the category's event | None | `204`; `401`, `403`, `404`; `409` if enrolments depend on it. |

## Enrolments

| Method and route | Session / role | Request DTO | Response |
|---|---|---|---|
| `POST /api/enrolments` | Participant | `EnrolmentRequest`: `eventId`, `categoryId` | `201` enrolment projection (`enrolmentID`, `eventID`, `categoryID`, `enrolmentDate`, `status`); `400` invalid category; `401`; `404` event; `409` duplicate entry. |
| `GET /api/enrolments/me` | Participant | None | `200` list of the participant's enrolment projections; `401`. |
| `GET /api/enrolments/{id}` | Participant who owns it, or organiser who owns its event | Route `id` | `200 EnrolmentDetailResponse`: `enrolmentId`, `participantId`, `eventId`, `eventName`, `categoryId`, `categoryName`, `enrolmentDate`, `status`; `401`, `403`, `404`. |
| `GET /api/events/{eventId}/enrolments` | Organiser who owns the event | Route `eventId` | `200` list of enrolment projections including `participantID`; `401`, `403`, `404`. |
| `PUT /api/enrolments/{id}/status` | Organiser who owns the enrolment's event | `EnrolmentStatusRequest`: `status` (`ACTIVE`, `CANCELLED`, or `COMPLETED`) | `200` `{ enrolmentID, status }`; `400`, `401`, `403`, `404`. |

Enrolment creation verifies that the category belongs to the selected event and
the database enforces one enrolment per participant/event.

## Results

| Method and route | Session / role | Request DTO | Response |
|---|---|---|---|
| `GET /api/results/me` | Participant | None | `200` list of `ParticipantResultResponse`: `resultId`, `enrolmentId`, `eventName`, `eventDate`, `categoryName`, `finishTime`, `finishingPosition`. Results are filtered to the current participant. `401`. |
| `GET /api/enrolments/{enrolmentId}/result` | Participant who owns the enrolment, or organiser who owns its event | Route `enrolmentId` | `200` result projection (`resultID`, `enrolmentID`, `finishTime`, `finishingPosition`, `recordedAt`); `401`, `403`, `404`. |
| `POST /api/enrolments/{enrolmentId}/result` | Organiser who owns the event | `ResultRequest`: `finishTime`, `finishingPosition` | `201` result projection; `400`, `401`, `403`, `404`; `409` result already exists. |
| `PUT /api/enrolments/{enrolmentId}/result` | Organiser who owns the event | `ResultRequest` | `200` result projection; `400`, `401`, `403`, `404`. |
| `DELETE /api/enrolments/{enrolmentId}/result` | Organiser who owns the event | None | `204`; `401`, `403`, `404`. |

## Future media uploads (not implemented)

| Planned capability | Current state |
|---|---|
| Event banner upload | No upload endpoint, request DTO, storage provider, size/type validation, or response DTO exists. |
| Profile image upload | No upload endpoint, request DTO, storage provider, size/type validation, or response DTO exists. |

These routes and payloads must be agreed before the MVC client implements upload
flows. No MVC-side database access should be added to work around the missing
API endpoints.

## Contract gaps and compatibility notes

- `GET` and `PUT /api/users/me` now implement the Part 1 profile routes. The old
  `GET /api/auth/me` route is retained, and a matching `PUT /api/auth/me` alias
  is available.
- `GET /api/enrolments/{id}` from the Part 1 plan is implemented with
  participant and organiser ownership checks.
- The Part 1 plan describes login as returning a token. The actual API uses a
  server-side session cookie; clients must follow the session contract above.
- Organiser accounts cannot self-register. A local Development-only interactive
  provisioning command is documented in the repository README. There is no
  public organiser-creation endpoint.
- Event-banner and profile-image uploads remain incomplete.

## MVC integration implemented in Sprint 2

The MVC project is a server-rendered API client. It references
`RaceDay.Contracts` for request/response types and contains no SQL Server,
EF Core, or API database context registration.

### MVC routes

| Method and route | Access | Purpose / request fields | Result |
|---|---|---|---|
| `GET /` | Public | RaceDay landing page | Home page. |
| `GET /Account/Login` | Public | None | Login form (`Email`, `Password`) with antiforgery token. |
| `POST /Account/Login` | Public, antiforgery required | `LoginViewModel`: `Email`, `Password`; sends API `LoginRequest` to `POST /api/auth/login`. | On success redirects to dashboard and forwards API cookies; invalid credentials stay on the form with an error. |
| `GET /Account/Register` | Public | None | Participant-only registration form; it has no role selector. |
| `POST /Account/Register` | Public, antiforgery required | `RegisterViewModel`: `FirstName`, `LastName`, `Email`, `Password`, optional `PhoneNumber`; MVC always supplies `role=PARTICIPANT` to the API. | On success redirects to login; API validation/connection errors are shown on the form. |
| `POST /Account/Logout` | Antiforgery required | None; calls `POST /api/auth/logout` with the current API session cookie. | Redirects to login and expires `.RaceDay.Session` in the browser. |
| `GET /Dashboard` | API session required | Calls `GET /api/users/me`. | Participant- or organiser-specific dashboard and navigation. A missing/expired session redirects to login. |
| `GET /Profile` | API session required | Calls `GET /api/users/me`. | Safe profile details for the signed-in user. |

### API client and cookies

- `Api:BaseUrl` in `RaceDay.Mvc/appsettings.json` configures the API origin.
  Set `Api__BaseUrl` to override it through environment configuration.
- `RaceDayApiClient` uses a typed `IHttpClientFactory` client for the actual API
  routes and DTOs listed above.
- `ApiSessionCookieHandler` forwards only the current request's
  `.RaceDay.Session` cookie. Its `HttpClientHandler` has `UseCookies=false`, so
  no shared `CookieContainer` can mix users' sessions.
- API `Set-Cookie` headers are copied to the browser as received, preserving
  their attributes. Logout also expires the browser's API session cookie if the
  API response did not include a clearing cookie.
- Session cookies stay HttpOnly and are not written to logs or made available
  to JavaScript. MVC unsafe form submissions use ASP.NET Core antiforgery
  validation.
- The MVC UI maps API 401 responses to an expired-session sign-in redirect,
  403 to an access-denied page, and connection/timeout failures to a
  service-unavailable page or form error.

The initial MVC sprint includes login, logout, participant registration,
profile retrieval and role-aware navigation. Event, category, enrolment, and
results screens are not implemented in MVC yet; the API endpoints above remain
available for later integration.
