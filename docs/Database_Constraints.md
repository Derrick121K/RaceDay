# RaceDay Database Constraints

## Purpose

This document records the main integrity rules implemented in the
RaceDay SQL Server database.

## Primary Keys

Each entity has a primary key that uniquely identifies every record.

- User.UserID
- Event.EventID
- EventType.EventTypeID
- Category.CategoryID
- Enrolment.EnrolmentID
- Result.ResultID

## Foreign Keys

Foreign keys maintain relationships between related tables.

- Event.EventTypeID → EventType.EventTypeID
- Event.OrganiserID → User.UserID
- Category.EventID → Event.EventID
- Enrolment.ParticipantID → User.UserID
- Enrolment.EventID → Event.EventID
- Enrolment.CategoryID → Category.CategoryID
- Result.EnrolmentID → Enrolment.EnrolmentID

## Other Constraints

### User

- Email addresses must be unique.
- User role must be ORGANISER or PARTICIPANT.
- Required user fields cannot be NULL.

### Event

- Event distance must be greater than zero.
- Events must reference an existing Organiser.
- Events must reference an existing EventType.

### Category

- Category names must be unique within an Event.
- Minimum and maximum ages cannot be negative.
- Maximum age cannot be less than minimum age.

### Enrolment

- Status must be ACTIVE, CANCELLED or COMPLETED.
- A participant cannot enrol in the same Event more than once.
- The selected Category must belong to the selected Event.

### Result

- Finishing position must be greater than zero.
- An Enrolment can have at most one Result.
- A Result must reference an existing Enrolment.

## Data Integrity

These constraints help prevent invalid or inconsistent RaceDay data
from being stored in the database.