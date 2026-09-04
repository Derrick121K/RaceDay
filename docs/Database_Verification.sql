USE RaceDay;
GO

-- Verify table record counts

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

-- Verify organiser-managed events

SELECT
    e.EventID,
    e.EventName,
    u.FirstName + ' ' + u.LastName AS Organiser
FROM Event e
INNER JOIN [User] u
    ON e.OrganiserID = u.UserID
WHERE u.Role = 'ORGANISER'
ORDER BY e.EventID;
GO

-- Verify participant enrolments

SELECT
    en.EnrolmentID,
    u.FirstName + ' ' + u.LastName AS Participant,
    e.EventName,
    c.CategoryName,
    en.Status
FROM Enrolment en
INNER JOIN [User] u
    ON en.ParticipantID = u.UserID
INNER JOIN Event e
    ON en.EventID = e.EventID
INNER JOIN Category c
    ON en.CategoryID = c.CategoryID
ORDER BY en.EnrolmentID;
GO

-- Verify results

SELECT
    r.ResultID,
    u.FirstName + ' ' + u.LastName AS Participant,
    e.EventName,
    r.FinishTime,
    r.FinishingPosition
FROM Result r
INNER JOIN Enrolment en
    ON r.EnrolmentID = en.EnrolmentID
INNER JOIN [User] u
    ON en.ParticipantID = u.UserID
INNER JOIN Event e
    ON en.EventID = e.EventID
ORDER BY r.ResultID;
GO