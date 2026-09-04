# RaceDay API Endpoint Plan

## PROG6212 Programming 2B - PoE Part 1

This document defines the RESTful API endpoints planned for the
RaceDay Event Management System.

The API will support authentication, user profiles, event management,
event categories, participant enrolments and race results.

## Roles

| Role | Description |
|---|---|
| Public | No authentication required |
| Any Authenticated User | Any logged-in RaceDay user |
| Organiser | Organiser role only |
| Participant | Participant role only |

# 1. Authentication

| HTTP Method | Route | Description | Role | Required Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new RaceDay user account. | Public | `{ firstName, lastName, email, password, role, phoneNumber }` | 201 Created; 400 Bad Request; 409 Conflict |
| POST | /api/auth/login | Authenticates a registered user and returns an authentication token. | Public | `{ email, password }` | 200 OK; 400 Bad Request; 401 Unauthorized |

# 2. User Profile

| HTTP Method | Route | Description | Role | Required Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently authenticated user. | Any Authenticated User | None | 200 OK; 401 Unauthorized |
| PUT | /api/users/me | Updates the profile of the currently authenticated user. | Any Authenticated User | `{ firstName, lastName, phoneNumber }` | 200 OK; 400 Bad Request; 401 Unauthorized |

# 3. Events

| HTTP Method | Route | Description | Role | Required Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Returns all available RaceDay events. | Public | None | 200 OK |
| GET | /api/events/{id} | Returns details for a specific RaceDay event. | Public | None | 200 OK; 404 Not Found |
| POST | /api/events | Creates a new RaceDay event. | Organiser | `{ eventName, description, eventDate, location, distance, eventTypeId }` | 201 Created; 400 Bad Request; 401 Unauthorized; 403 Forbidden |
| PUT | /api/events/{id} | Updates an event managed by the authenticated Organiser. | Organiser | `{ eventName, description, eventDate, location, distance, eventTypeId }` | 200 OK; 400 Bad Request; 403 Forbidden; 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event managed by the authenticated Organiser. | Organiser | None | 204 No Content; 403 Forbidden; 404 Not Found |

# 4. Categories

| HTTP Method | Route | Description | Role | Required Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Returns all categories belonging to a specific event. | Public | None | 200 OK; 404 Not Found |
| GET | /api/categories/{id} | Returns the details of a specific event category. | Public | None | 200 OK; 404 Not Found |
| POST | /api/events/{eventId}/categories | Creates a new category for an event managed by the authenticated Organiser. | Organiser | `{ categoryName, description, minimumAge, maximumAge }` | 201 Created; 400 Bad Request; 403 Forbidden; 404 Not Found |
| PUT | /api/categories/{id} | Updates a category belonging to an event managed by the authenticated Organiser. | Organiser | `{ categoryName, description, minimumAge, maximumAge }` | 200 OK; 400 Bad Request; 403 Forbidden; 404 Not Found |
| DELETE | /api/categories/{id} | Deletes a category from an event managed by the authenticated Organiser. | Organiser | None | 204 No Content; 403 Forbidden; 404 Not Found |

# 5. Enrolments

| HTTP Method | Route | Description | Role | Required Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments | Enrols the authenticated Participant in an event and records the selected category. | Participant | `{ eventId, categoryId }` | 201 Created; 400 Bad Request; 401 Unauthorized; 409 Conflict |
| GET | /api/enrolments/me | Returns all event enrolments belonging to the authenticated Participant. | Participant | None | 200 OK; 401 Unauthorized |
| GET | /api/events/{eventId}/enrolments | Returns all enrolments for an event managed by the authenticated Organiser. | Organiser | None | 200 OK; 403 Forbidden; 404 Not Found |
| GET | /api/enrolments/{id} | Returns a specific enrolment for the authenticated user or an event managed by the Organiser. | Any Authenticated User | None | 200 OK; 403 Forbidden; 404 Not Found |
| PUT | /api/enrolments/{id}/status | Updates the status of an enrolment when permitted by the system. | Organiser | `{ status }` | 200 OK; 400 Bad Request; 403 Forbidden; 404 Not Found |

# 6. Results

| HTTP Method | Route | Description | Role | Required Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/result | Records a participant's finish time and finishing position for an enrolment. | Organiser | `{ finishTime, finishingPosition }` | 201 Created; 400 Bad Request; 403 Forbidden; 404 Not Found |
| GET | /api/results/me | Returns the authenticated Participant's race results and performance history. | Participant | None | 200 OK; 401 Unauthorized |
| GET | /api/enrolments/{enrolmentId}/result | Returns the result associated with a specific enrolment. | Any Authenticated User | None | 200 OK; 403 Forbidden; 404 Not Found |
| PUT | /api/enrolments/{enrolmentId}/result | Updates the recorded result for an enrolment. | Organiser | `{ finishTime, finishingPosition }` | 200 OK; 400 Bad Request; 403 Forbidden; 404 Not Found |
| DELETE | /api/enrolments/{enrolmentId}/result | Removes a result that was recorded incorrectly. | Organiser | None | 204 No Content; 403 Forbidden; 404 Not Found |

# 7. API Design Rules

## Authentication and Authorisation

Protected endpoints require an authenticated RaceDay user.

Organiser-only endpoints must verify that the authenticated user has the
ORGANISER role.

Participant-only endpoints must verify that the authenticated user has
the PARTICIPANT role.

## Data Validation

The API should validate required fields before database operations.

Event and Category identifiers must reference existing records.

A Participant must not be allowed to enrol in a Category that belongs
to a different Event.

A Participant must not be allowed to enrol in the same Event more than
once.

A Result must contain a valid finish time and a positive finishing
position.

## Response Codes

The API uses HTTP status codes to communicate the outcome of each
request. Common responses include:

- 200 OK
- 201 Created
- 204 No Content
- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 409 Conflict

