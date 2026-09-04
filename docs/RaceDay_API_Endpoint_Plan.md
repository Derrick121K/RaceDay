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