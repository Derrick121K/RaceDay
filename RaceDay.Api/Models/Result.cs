using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// Stores the finishing information for a participant's enrolment.
/// A result is recorded after the participant finishes the event.
/// </summary>
public class Result
{
    // This number uniquely identifies each result.
    [Key]
    public int ResultID { get; set; }

    // Identifies the enrolment this result belongs to.
    // We will configure the relationship in RaceDayDbContext.
    public int EnrolmentID { get; set; }

    // The participant's finishing time.
    // TimeSpan represents an amount of time, and SQL Server stores
    // this value in the TIME column defined in our Part 1 design.
    [Column(TypeName = "time(0)")]
    public TimeSpan FinishTime { get; set; }

    // The position in which the participant finished.
    // A finishing position must be a positive number.
    [Range(1, int.MaxValue,
        ErrorMessage = "Finishing position must be greater than zero.")]
    public int FinishingPosition { get; set; }

    // Records when the result was entered into the system.
    // We will configure the database default in RaceDayDbContext.
    public DateTime RecordedAt { get; set; }

    // Navigation property: lets Entity Framework connect the result
    // to the enrolment it belongs to.
    public Enrolment? Enrolment { get; set; }
}