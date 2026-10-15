using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

/// <summary>
/// Represents a race category that belongs to a particular event.
/// For example, an event might offer a 5 km run and a 10 km run.
/// </summary>
public class Category
{
    // The unique number used to identify this category.
    [Key]
    public int CategoryID { get; set; }

    // This identifies the event that owns the category.
    // Every category must belong to an existing event.
    public int EventID { get; set; }

    // The name participants see when choosing a category.
    [Required]
    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    // Extra information about the category is optional.
    [StringLength(200)]
    public string? Description { get; set; }

    // The youngest age allowed for this category.
    // We use int? because the minimum age can be left unspecified.
    [Range(0, int.MaxValue, ErrorMessage = "Minimum age cannot be negative.")]
    public int? MinimumAge { get; set; }

    // The oldest age allowed for this category.
    // This is also optional.
    [Range(0, int.MaxValue, ErrorMessage = "Maximum age cannot be negative.")]
    public int? MaximumAge { get; set; }

    // Navigation property: lets Entity Framework connect this
    // category to the Event object it belongs to.
    public Event? Event { get; set; }
}