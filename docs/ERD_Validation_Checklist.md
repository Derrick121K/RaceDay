# RaceDay ERD Validation Checklist

## Purpose

This checklist confirms that the RaceDay Entity Relationship Diagram
(ERD) was reviewed against the Part 1 requirements before submission.

## Entity Validation

| Requirement | Status |
|---|---|
| At least six entities are included | Complete |
| User entity included | Complete |
| Event entity included | Complete |
| EventType entity included | Complete |
| Category entity included | Complete |
| Enrolment entity included | Complete |
| Result entity included | Complete |

## Attribute Validation

| Entity | Attributes Present | Status |
|---|---|---|
| User | UserID, FirstName, LastName, Email, PasswordHash, Role, PhoneNumber, CreatedAt | Complete |
| Event | EventID, EventName, Description, EventDate, Location, Distance, EventTypeID, OrganiserID, CreatedAt | Complete |
| EventType | EventTypeID, TypeName, Description | Complete |
| Category | CategoryID, EventID, CategoryName, Description, MinimumAge, MaximumAge | Complete |
| Enrolment | EnrolmentID, ParticipantID, EventID, CategoryID, EnrolmentDate, Status | Complete |
| Result | ResultID, EnrolmentID, FinishTime, FinishingPosition, RecordedAt | Complete |

## Key Validation

### Primary Keys

- User.UserID
- Event.EventID
- EventType.EventTypeID
- Category.CategoryID
- Enrolment.EnrolmentID
- Result.ResultID

### Foreign Keys

- Event.EventTypeID → EventType.EventTypeID
- Event.OrganiserID → User.UserID
- Category.EventID → Event.EventID
- Enrolment.ParticipantID → User.UserID
- Enrolment.EventID → Event.EventID
- Enrolment.CategoryID → Category.CategoryID
- Result.EnrolmentID → Enrolment.EnrolmentID

## Relationship Validation

| Relationship | Cardinality | Status |
|---|---|---|
| User (Organiser) → Event | 1 : Many | Complete |
| EventType → Event | 1 : Many | Complete |
| Event → Category | 1 : Many | Complete |
| User (Participant) → Enrolment | 1 : Many | Complete |
| Event → Enrolment | 1 : Many | Complete |
| Category → Enrolment | 1 : Many | Complete |
| Enrolment → Result | 1 : 0..1 | Complete |

## Final Validation

The ERD contains the required entities, attributes, primary keys,
foreign keys, relationships and cardinality.

The ERD was designed to match the SQL Server database structure and the
planned RaceDay API functionality.