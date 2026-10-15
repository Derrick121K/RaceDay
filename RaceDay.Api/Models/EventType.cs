using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

/// <summary>
/// Stores the types of events that RaceDay supports,
/// such as road running, walking and cycling.
/// </summary>
public class EventType
{
    // This number uniquely identifies each type of event.
    [Key]
    public int EventTypeID { get; set; }

    // Every event type needs a name, such as "Walking".
    // We will also enforce uniqueness in the database configuration.
    [Required]
    [StringLength(50)]
    public string TypeName { get; set; } = string.Empty;

    // This explains what the event type means.
    // A description is optional, just as in the Part 1 design.
    [StringLength(200)]
    public string? Description { get; set; }
}