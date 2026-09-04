/*
    RaceDay Event Management System
    PROG6212 Programming 2B - PoE Part 1

    Purpose:
    Creates the RaceDay database, tables, constraints and realistic
    sample data required for Part 1.

    Database Platform:
    Microsoft SQL Server
*/

------------------------------------------------------------
-- 1. CREATE DATABASE
------------------------------------------------------------

IF DB_ID('RaceDay') IS NULL
BEGIN
    CREATE DATABASE RaceDay;
END;
GO

USE RaceDay;
GO

------------------------------------------------------------
-- 2. CREATE EVENT TYPE TABLE
------------------------------------------------------------

CREATE TABLE EventType
(
    EventTypeID INT IDENTITY(1,1) NOT NULL,
    TypeName VARCHAR(50) NOT NULL,
    Description VARCHAR(200) NULL,

    CONSTRAINT PK_EventType
        PRIMARY KEY (EventTypeID),

    CONSTRAINT UQ_EventType_TypeName
        UNIQUE (TypeName)
);
GO

------------------------------------------------------------
-- 3. CREATE USER TABLE
------------------------------------------------------------

CREATE TABLE [User]
(
    UserID INT IDENTITY(1,1) NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    PhoneNumber VARCHAR(20) NULL,
    CreatedAt DATETIME2 NOT NULL
        CONSTRAINT DF_User_CreatedAt DEFAULT SYSDATETIME(),

    CONSTRAINT PK_User
        PRIMARY KEY (UserID),

    CONSTRAINT UQ_User_Email
        UNIQUE (Email),

    CONSTRAINT CK_User_Role
        CHECK (Role IN ('ORGANISER', 'PARTICIPANT'))
);
GO

------------------------------------------------------------
-- 4. CREATE EVENT TABLE
------------------------------------------------------------

CREATE TABLE Event
(
    EventID INT IDENTITY(1,1) NOT NULL,
    EventName VARCHAR(150) NOT NULL,
    Description VARCHAR(500) NOT NULL,
    EventDate DATE NOT NULL,
    Location VARCHAR(200) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    EventTypeID INT NOT NULL,
    OrganiserID INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL
        CONSTRAINT DF_Event_CreatedAt DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Event
        PRIMARY KEY (EventID),

    CONSTRAINT FK_Event_EventType
        FOREIGN KEY (EventTypeID)
        REFERENCES EventType(EventTypeID),

    CONSTRAINT FK_Event_Organiser
        FOREIGN KEY (OrganiserID)
        REFERENCES [User](UserID),

    CONSTRAINT CK_Event_Distance
        CHECK (Distance > 0)
);
GO

------------------------------------------------------------
-- 5. CREATE CATEGORY TABLE
------------------------------------------------------------

CREATE TABLE Category
(
    CategoryID INT IDENTITY(1,1) NOT NULL,
    EventID INT NOT NULL,
    CategoryName VARCHAR(100) NOT NULL,
    Description VARCHAR(200) NULL,
    MinimumAge INT NULL,
    MaximumAge INT NULL,

    CONSTRAINT PK_Category
        PRIMARY KEY (CategoryID),

    CONSTRAINT FK_Category_Event
        FOREIGN KEY (EventID)
        REFERENCES Event(EventID),

    CONSTRAINT CK_Category_MinimumAge
        CHECK (MinimumAge IS NULL OR MinimumAge >= 0),

    CONSTRAINT CK_Category_MaximumAge
        CHECK (MaximumAge IS NULL OR MaximumAge >= 0),

    CONSTRAINT CK_Category_AgeRange
        CHECK (
            MinimumAge IS NULL
            OR MaximumAge IS NULL
            OR MaximumAge >= MinimumAge
        ),

    CONSTRAINT UQ_Category_Event_Name
        UNIQUE (EventID, CategoryName),

    CONSTRAINT UQ_Category_Event_Category
        UNIQUE (EventID, CategoryID)
);
GO

------------------------------------------------------------
-- 6. CREATE ENROLMENT TABLE
------------------------------------------------------------

CREATE TABLE Enrolment
(
    EnrolmentID INT IDENTITY(1,1) NOT NULL,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME2 NOT NULL
        CONSTRAINT DF_Enrolment_EnrolmentDate DEFAULT SYSDATETIME(),
    Status VARCHAR(20) NOT NULL
        CONSTRAINT DF_Enrolment_Status DEFAULT 'ACTIVE',

    CONSTRAINT PK_Enrolment
        PRIMARY KEY (EnrolmentID),

    CONSTRAINT FK_Enrolment_Participant
        FOREIGN KEY (ParticipantID)
        REFERENCES [User](UserID),

    CONSTRAINT FK_Enrolment_Event
        FOREIGN KEY (EventID)
        REFERENCES Event(EventID),

    CONSTRAINT FK_Enrolment_Category
        FOREIGN KEY (CategoryID)
        REFERENCES Category(CategoryID),

    /*
        Ensures that the selected Category actually belongs
        to the selected Event.
    */
    CONSTRAINT FK_Enrolment_Event_Category
        FOREIGN KEY (EventID, CategoryID)
        REFERENCES Category(EventID, CategoryID),

    CONSTRAINT CK_Enrolment_Status
        CHECK (Status IN ('ACTIVE', 'CANCELLED', 'COMPLETED')),

    CONSTRAINT UQ_Enrolment_Participant_Event
        UNIQUE (ParticipantID, EventID)
);
GO

------------------------------------------------------------
-- 7. CREATE RESULT TABLE
------------------------------------------------------------

CREATE TABLE Result
(
    ResultID INT IDENTITY(1,1) NOT NULL,
    EnrolmentID INT NOT NULL,
    FinishTime TIME(0) NOT NULL,
    FinishingPosition INT NOT NULL,
    RecordedAt DATETIME2 NOT NULL
        CONSTRAINT DF_Result_RecordedAt DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Result
        PRIMARY KEY (ResultID),

    CONSTRAINT FK_Result_Enrolment
        FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolment(EnrolmentID),

    CONSTRAINT CK_Result_FinishingPosition
        CHECK (FinishingPosition > 0),

    CONSTRAINT UQ_Result_Enrolment
        UNIQUE (EnrolmentID)
);
GO

------------------------------------------------------------
-- 8. SEED EVENT TYPES
------------------------------------------------------------

INSERT INTO EventType
    (TypeName, Description)
VALUES
    (
        'Road Running',
        'Road running events for competitive and recreational participants.'
    ),
    (
        'Walking',
        'Organised walking events for recreational participants.'
    ),
    (
        'Cycling',
        'Road cycling events for competitive and recreational cyclists.'
    );
GO

------------------------------------------------------------
-- 9. SEED USERS
------------------------------------------------------------

INSERT INTO [User]
    (FirstName, LastName, Email, PasswordHash, Role, PhoneNumber)
VALUES
    (
        'Thabo',
        'Mokoena',
        'thabo.mokoena@raceday.co.za',
        'HASH_Thabo_2026',
        'ORGANISER',
        '0825551001'
    ),
    (
        'Naledi',
        'Ndlovu',
        'naledi.ndlovu@raceday.co.za',
        'HASH_Naledi_2026',
        'ORGANISER',
        '0835551002'
    ),
    (
        'Lerato',
        'Dlamini',
        'lerato.dlamini@example.com',
        'HASH_Lerato_2026',
        'PARTICIPANT',
        '0845551003'
    ),
    (
        'Sipho',
        'Nkosi',
        'sipho.nkosi@example.com',
        'HASH_Sipho_2026',
        'PARTICIPANT',
        '0855551004'
    );
GO

------------------------------------------------------------
-- 10. SEED EVENTS
------------------------------------------------------------

INSERT INTO Event
    (
        EventName,
        Description,
        EventDate,
        Location,
        Distance,
        EventTypeID,
        OrganiserID
    )
VALUES
    (
        'Mpumalanga Road Run 2026',
        'A community road running event for competitive and recreational runners.',
        '2026-10-18',
        'Mbombela, Mpumalanga',
        10.00,
        1,
        1
    ),
    (
        'Highveld Family Walk 2026',
        'A family-friendly walking event promoting health and community participation.',
        '2026-11-08',
        'eMalahleni, Mpumalanga',
        5.00,
        2,
        2
    ),
    (
        'Mpumalanga Cycle Challenge 2026',
        'A road cycling challenge for recreational and competitive cyclists.',
        '2026-11-29',
        'White River, Mpumalanga',
        40.00,
        3,
        1
    );
GO

------------------------------------------------------------
-- 11. SEED CATEGORIES
------------------------------------------------------------

INSERT INTO Category
    (
        EventID,
        CategoryName,
        Description,
        MinimumAge,
        MaximumAge
    )
VALUES
    (
        1,
        '5 km Run',
        'Five kilometre road running category.',
        10,
        NULL
    ),
    (
        1,
        '10 km Run',
        'Ten kilometre road running category.',
        16,
        NULL
    ),
    (
        1,
        '21 km Half Marathon',
        'Twenty-one kilometre half marathon category.',
        18,
        NULL
    ),
    (
        2,
        '5 km Family Walk',
        'Five kilometre family walking category.',
        5,
        NULL
    ),
    (
        2,
        '10 km Walk',
        'Ten kilometre walking category.',
        12,
        NULL
    ),
    (
        3,
        '40 km Road Cycle',
        'Forty kilometre road cycling category.',
        16,
        NULL
    ),
    (
        3,
        '80 km Road Cycle',
        'Eighty kilometre road cycling category.',
        18,
        NULL
    );
GO

------------------------------------------------------------
-- 12. SEED ENROLMENTS
------------------------------------------------------------

INSERT INTO Enrolment
    (
        ParticipantID,
        EventID,
        CategoryID,
        Status
    )
VALUES
    (
        3,
        1,
        2,
        'ACTIVE'
    ),
    (
        3,
        2,
        4,
        'ACTIVE'
    ),
    (
        4,
        1,
        1,
        'ACTIVE'
    ),
    (
        4,
        3,
        6,
        'ACTIVE'
    );
GO

------------------------------------------------------------
-- 13. SEED RESULTS
------------------------------------------------------------

INSERT INTO Result
    (
        EnrolmentID,
        FinishTime,
        FinishingPosition
    )
VALUES
    (
        1,
        '00:58:24',
        17
    ),
    (
        3,
        '00:31:42',
        6
    );
GO

------------------------------------------------------------
-- 14. BASIC VERIFICATION
------------------------------------------------------------

SELECT 'EventType' AS TableName, COUNT(*) AS RecordCount
FROM EventType

UNION ALL

SELECT 'User', COUNT(*)
FROM [User]

UNION ALL

SELECT 'Event', COUNT(*)
FROM Event

UNION ALL

SELECT 'Category', COUNT(*)
FROM Category

UNION ALL

SELECT 'Enrolment', COUNT(*)
FROM Enrolment

UNION ALL

SELECT 'Result', COUNT(*)
FROM Result;
GO