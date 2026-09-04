# RaceDay API Design Rules

## Purpose

This document defines the main rules that will guide the future RaceDay
RESTful API implementation.

## Authentication

Public endpoints may be accessed without signing in.

Protected endpoints require an authenticated RaceDay user.

## Authorisation

### Organiser

Organisers may:

- Create events
- Update events
- Delete events
- Manage event categories
- View enrolments for events they manage
- Record and update participant results

### Participant

Participants may:

- View their profile
- Browse events
- Enrol in events
- Select an event category
- View their own enrolments
- View their own race results

## HTTP Methods

| Method | Purpose |
|---|---|
| GET | Retrieve information |
| POST | Create a new resource |
| PUT | Update an existing resource |
| DELETE | Remove a resource |

## Validation Rules

The API must validate required request fields.

Event IDs and Category IDs must reference existing records.

A Participant must not enrol in the same Event more than once.

A Participant must not select a Category belonging to another Event.

A finishing position must be greater than zero.

A Result must belong to an existing Enrolment.

## HTTP Response Codes

| Code | Meaning |
|---|---|
| 200 | Request completed successfully |
| 201 | Resource created successfully |
| 204 | Request completed with no response body |
| 400 | Invalid request |
| 401 | Authentication required or failed |
| 403 | User does not have permission |
| 404 | Requested resource was not found |
| 409 | Request conflicts with existing data |