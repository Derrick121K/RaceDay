using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

/// <summary>
/// Records a participant's entry into an event and the category they chose.
/// An enrolment can have a result after the participant finishes the event.
/// </summary>
public class Enrolment
{
    // This number uniquely identifies each enrolment.
    [Key]
    public int EnrolmentID { get; set; }

    // Identifies the user who entered the event.
    // RaceDay must check that this user has the Participant role.
    public int ParticipantID { get; set; }

    // Identifies the event the participant wants to enter.
    public int EventID { get; set; }

    // Identifies the category selected by the participant.
    // The database will check that this category belongs to the event.
    public int CategoryID { get; set; }

    // Records the date and time the participant entered.
    // We will configure the database default in RaceDayDbContext.
    public DateTime EnrolmentDate { get; set; }

    // Shows the current state of the entry.
    // New enrolments start as ACTIVE.
    [Required]
    [StringLength(20)]
    [RegularExpression(
        "^(ACTIVE|CANCELLED|COMPLETED)$",
        ErrorMessage = "Status must be ACTIVE, CANCELLED or COMPLETED.")]
    public string Status { get; set; } = "ACTIVE";

    // Navigation property: the participant who made the entry.
    public User? Participant { get; set; }

    // Navigation property: the event being entered.
    public Event? Event { get; set; }

    // Navigation property: the category selected for the entry.
    public Category? Category { get; set; }

    // A participant may have no result yet, or one result when finished.
    public Result? Result { get; set; }
}