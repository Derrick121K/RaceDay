using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

/// <summary>
/// Represents a race or other sporting event organised through RaceDay.
/// It stores the event details and links the event to its type and organiser.
/// </summary>
public class Event
{
    // This is the unique number used to identify each event.
    [Key]
    public int EventID { get; set; }

    // Every event needs a name so participants know what they are entering.
    [Required]
    [StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    // The description gives participants more information about the event.
    // In our Part 1 SQL design, this field is required.
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    // The event date tells participants when the race will take place.
    // The database column should contain a date, not a time.
    [Column(TypeName = "date")]
    public DateTime EventDate { get; set; }

    // This stores the place where the event will happen.
    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    // Distance is measured in kilometres.
    // We do not allow zero or a negative distance.
    // These limits also fit the DECIMAL(6,2) column from Part 1.
    [Range(
        typeof(decimal),
        "0.01",
        "9999.99",
        ErrorMessage = "Distance must be greater than zero.")]
    public decimal Distance { get; set; }

    // This foreign key points to the type of event,
    // such as Road Running, Walking, or Cycling.
    public int EventTypeID { get; set; }

    // This foreign key identifies the user who organises the event.
    public int OrganiserID { get; set; }

    // Records when the event was created.
    // We will configure the database default in RaceDayDbContext.
    public DateTime CreatedAt { get; set; }

    // Navigation property: lets Entity Framework load the event type
    // related to this event.
    public EventType? EventType { get; set; }

    // Navigation property: lets Entity Framework load the organiser
    // associated with this event.
    public User? Organiser { get; set; }
}