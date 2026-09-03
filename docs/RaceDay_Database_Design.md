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

Stores the accounts used by Organisers and Participants.

### Attributes

| Attribute | Type | Key | Required | Description |
|---|---|---|---|---|
| UserID | INT | PK | Yes | Unique user identifier |
| FirstName | VARCHAR | | Yes | User first name |
| LastName | VARCHAR | | Yes | User surname |
| Email | VARCHAR | UNIQUE | Yes | User email address |
| PasswordHash | VARCHAR | | Yes | Stored password hash |
| Role | VARCHAR | | Yes | Organiser or Participant |
| PhoneNumber | VARCHAR | | No | User contact number |
| CreatedAt | DATETIME | | Yes | Account creation date |

## 6. Event Entity

To be completed.

## 7. EventType Entity

To be completed.

## 8. Category Entity

To be completed.

## 9. Enrolment Entity

To be completed.

## 10. Result Entity

To be completed.

## 11. Relationships

To be completed after all entities have been analysed.

## 12. Cardinality

To be completed after relationships have been validated.

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