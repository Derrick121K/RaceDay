# RaceDay Database Design Specification

## PROG6212 Programming 2B - PoE Part 1

## 1. Purpose

This document is the working database design specification for the
RaceDay Event Management System.

It is used to define and validate the entities, attributes, keys,
relationships, cardinality and business rules before creating the
final Entity Relationship Diagram (ERD) and SQL Server database script.

## 2. System Overview

RaceDay is a web-based event management system for South African road
running, walking and cycling events.

## 3. User Roles

### Organiser

An Organiser can:

- Create events
- Edit events
- Delete events
- Manage event categories
- View event enrolments
- Capture participant results
- View information relating to events they manage

### Participant

A Participant can:

- Create an account
- Log in
- Browse available events
- Enter an event
- Select a category
- View their own enrolments
- Track their own race results and performance history

## 4. Proposed Entities

The initial entities identified for analysis are:

1. User
2. Event
3. EventType
4. Category
5. Enrolment
6. Result

## 5. User Entity

### Purpose

The User entity stores the accounts used by Organisers and Participants.

### Attributes

| Attribute | Type | Key | Required | Description |
|---|---|---|---|---|
| UserID | INT | PK | Yes | Unique identifier for the user |
| FirstName | VARCHAR(50) | | Yes | User first name |
| LastName | VARCHAR(50) | | Yes | User surname |
| Email | VARCHAR(255) | UNIQUE | Yes | User email address |
| PasswordHash | VARCHAR(255) | | Yes | Stored password hash |
| Role | VARCHAR(20) | | Yes | Organiser or Participant |
| PhoneNumber | VARCHAR(20) | | No | User contact number |
| CreatedAt | DATETIME2 | | Yes | Date and time the account was created |

### Role Values

The Role attribute supports two values:

- ORGANISER
- PARTICIPANT

### User Business Rules

1. Every user must have one valid RaceDay role.
2. Email addresses must be unique.
3. Organisers can manage events.
4. Participants can enrol in events.

## 6. Event Entity

### Purpose

The Event entity stores information about RaceDay events created and
managed by Organisers.

### Attributes

| Attribute | Type | Key | Required | Description |
|---|---|---|---|---|
| EventID | INT | PK | Yes | Unique identifier for the event |
| EventName | VARCHAR(150) | | Yes | Name of the event |
| Description | VARCHAR(500) | | Yes | Description of the event |
| EventDate | DATE | | Yes | Date on which the event takes place |
| Location | VARCHAR(200) | | Yes | Event location |
| Distance | DECIMAL(6,2) | | Yes | Event distance in kilometres |
| EventTypeID | INT | FK | Yes | Type of event |
| OrganiserID | INT | FK | Yes | User who organises the event |
| CreatedAt | DATETIME2 | | Yes | Date and time the event was created |

### Event Business Rules

1. Every event must have one Organiser.
2. An Organiser can manage many events.
3. Every event must have one Event Type.
4. An Event Type can be used by many events.
5. Event names should clearly identify the event.
6. The event date is required.
7. The event distance is required.

## 7. EventType Entity

### Purpose

The EventType entity stores the different types of events supported by
RaceDay.

### Attributes

| Attribute | Type | Key | Required | Description |
|---|---|---|---|---|
| EventTypeID | INT | PK | Yes | Unique identifier for the event type |
| TypeName | VARCHAR(50) | UNIQUE | Yes | Name of the event type |
| Description | VARCHAR(200) | | No | Description of the event type |

### EventType Business Rules

1. Every event type must have a unique name.
2. An event must have one Event Type.
3. One Event Type can be used by many Events.

## 8. Category Entity

### Purpose

The Category entity stores the categories available for each RaceDay event.
Participants select a category when they enrol for an event.

### Attributes

| Attribute | Type | Key | Required | Description |
|---|---|---|---|---|
| CategoryID | INT | PK | Yes | Unique identifier for the category |
| EventID | INT | FK | Yes | Event to which the category belongs |
| CategoryName | VARCHAR(100) | | Yes | Name of the category |
| Description | VARCHAR(200) | | No | Description of the category |
| MinimumAge | INT | | No | Minimum participant age for the category |
| MaximumAge | INT | | No | Maximum participant age for the category |

### Category Business Rules

1. Every category belongs to one Event.
2. One Event can have many Categories.
3. A category name should be unique within its Event.
4. A Participant selects a Category when enrolling in an Event.

## 9. Enrolment Entity

### Purpose

The Enrolment entity records when a Participant enters a RaceDay Event
and the Category selected by the Participant.

### Attributes

| Attribute | Type | Key | Required | Description |
|---|---|---|---|---|
| EnrolmentID | INT | PK | Yes | Unique identifier for the enrolment |
| ParticipantID | INT | FK | Yes | User who is enrolling in the event |
| EventID | INT | FK | Yes | Event being entered |
| CategoryID | INT | FK | Yes | Category selected by the participant |
| EnrolmentDate | DATETIME2 | | Yes | Date and time of enrolment |
| Status | VARCHAR(20) | | Yes | Current enrolment status |

### Enrolment Business Rules

1. Every enrolment belongs to one Participant.
2. Every enrolment belongs to one Event.
3. Every enrolment uses one Category.
4. A Participant can have many Enrolments.
5. An Event can have many Enrolments.
6. A Category can be selected by many Enrolments.
7. The selected Category must belong to the selected Event.

## 10. Result Entity

### Purpose

The Result entity stores the final performance of a Participant for an
Event enrolment.

### Attributes

| Attribute | Type | Key | Required | Description |
|---|---|---|---|---|
| ResultID | INT | PK | Yes | Unique identifier for the result |
| EnrolmentID | INT | FK | Yes | Enrolment associated with the result |
| FinishTime | TIME | | Yes | Time taken by the participant to finish |
| FinishingPosition | INT | | Yes | Final position achieved by the participant |
| RecordedAt | DATETIME2 | | Yes | Date and time the result was recorded |

### Result Business Rules

1. Every result belongs to one Enrolment.
2. An Enrolment can have one Result.
3. A result must contain a finish time.
4. A result must contain a finishing position.
5. Results are recorded by an Organiser.
6. Participants can view their own results.

## 11. Relationships

### User to Event

A User with the Organiser role can manage many Events.

`User (Organiser) 1 : Many Event`

The Event stores the OrganiserID as a foreign key referencing UserID.

### User to Enrolment

A User with the Participant role can have many Enrolments.

`User (Participant) 1 : Many Enrolment`

The Enrolment stores the ParticipantID as a foreign key referencing UserID.

### EventType to Event

One EventType can be associated with many Events.

`EventType 1 : Many Event`

The Event stores EventTypeID as a foreign key.

### Event to Category

One Event can have many Categories.

`Event 1 : Many Category`

The Category stores EventID as a foreign key.

### Event to Enrolment

One Event can have many Enrolments.

`Event 1 : Many Enrolment`

The Enrolment stores EventID as a foreign key.

### Category to Enrolment

One Category can be selected by many Enrolments.

`Category 1 : Many Enrolment`

The Enrolment stores CategoryID as a foreign key.

### Enrolment to Result

An Enrolment can have zero or one Result.

`Enrolment 1 : 0..1 Result`

The Result stores EnrolmentID as a foreign key.

## 12. Cardinality

| Relationship | Cardinality | Meaning |
|---|---|---|
| Organiser → Event | 1 : Many | One Organiser can manage many Events |
| Participant → Enrolment | 1 : Many | One Participant can have many Enrolments |
| EventType → Event | 1 : Many | One EventType can be used by many Events |
| Event → Category | 1 : Many | One Event can have many Categories |
| Event → Enrolment | 1 : Many | One Event can have many Enrolments |
| Category → Enrolment | 1 : Many | One Category can be used by many Enrolments |
| Enrolment → Result | 1 : 0..1 | An Enrolment may have no Result or one Result |

## 13. Business Rules

To be completed.

## 14. Database Constraints

To be completed.

## 15. Design Validation

The final database design must:

- Support all RaceDay requirements.
- Contain at least six entities.
- Clearly identify primary keys.
- Clearly identify foreign keys.
- Correctly represent relationship cardinality.
- Match the final SQL Server database.
- Support the future RESTful API.