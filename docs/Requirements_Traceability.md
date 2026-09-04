# RaceDay Requirements Traceability

## Purpose

This document maps the main RaceDay Part 1 requirements to the technical
deliverables completed in the repository.

## Traceability Matrix

| RaceDay Requirement | ERD | SQL Database | API Plan | Evidence |
|---|---|---|---|---|
| User accounts and roles | User entity with Role | User table and Role constraint | Register and Login endpoints | Complete |
| Organisers manage events | User → Event relationship | OrganiserID foreign key | Event POST, PUT and DELETE endpoints | Complete |
| Participants enter events | User → Enrolment relationship | ParticipantID foreign key | Enrolment POST endpoint | Complete |
| Participants select categories | Event → Category and Category → Enrolment | CategoryID and EventID foreign keys | Enrolment request includes categoryId | Complete |
| Organisers view enrolments | Event → Enrolment relationship | Enrolment foreign keys | Event enrolment endpoint | Complete |
| Organisers capture results | Enrolment → Result relationship | Result table and foreign key | Result POST and PUT endpoints | Complete |
| Participants view results | Enrolment → Result relationship | Result linked to Enrolment | Participant results endpoint | Complete |
| Event types are supported | EventType → Event relationship | EventTypeID foreign key | Event creation includes eventTypeId | Complete |

## Part 1 Deliverables

### ERD

The ERD documents the entities, attributes, primary keys, foreign keys,
relationships and cardinality.

### SQL Database

The SQL script implements the ERD as a SQL Server database and includes
constraints and realistic sample data.

### API Endpoint Plan

The API plan defines the future RESTful API required to interact with
the RaceDay data and functionality.

### GitHub Actions

The CI workflow validates the required Part 1 repository files.

## Conclusion

The RaceDay ERD, SQL database and API Endpoint Plan were designed to
support the same system requirements and form a consistent technical
foundation for the later implementation phases.