# RaceDay Database Test Evidence

## Purpose

This document records the database testing completed for the RaceDay
Programming 2B PoE Part 1 submission.

## Clean Database Rebuild

The RaceDay database was deleted from the local SQL Server instance and
recreated by executing the complete `RaceDay_Database.sql` script.

The script successfully:

1. Created the RaceDay database.
2. Created all six tables.
3. Created the required primary keys.
4. Created the required foreign keys.
5. Created database constraints.
6. Inserted the required sample data.
7. Completed the verification query successfully.

## Verification Results

| Table | Records |
|---|---:|
| EventType | 3 |
| User | 4 |
| Event | 3 |
| Category | 7 |
| Enrolment | 4 |
| Result | 2 |

## Foreign Key Verification

The foreign-key verification query successfully confirmed the relationships
between Event, EventType, User, Category, Enrolment and Result.

## Conclusion

The RaceDay SQL script successfully recreates the database structure and
required sample data from a clean SQL Server environment.