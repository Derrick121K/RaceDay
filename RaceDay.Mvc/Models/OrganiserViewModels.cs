using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Mvc.Models;

public sealed class OrganiserDashboardViewModel
{
    public int EventCount { get; init; }
    public int UpcomingEventCount { get; init; }
    public int EnrolmentCount { get; init; }
    public IReadOnlyList<EventResponse> UpcomingEvents { get; init; } = [];
}

public sealed class OrganiserEventsViewModel
{
    public IReadOnlyList<EventResponse> Events { get; init; } = [];
    public int OrganiserId { get; init; }
}

public sealed class OrganiserEventFormViewModel
{
    public int? EventId { get; set; }

    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime EventDate { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 9999.99)]
    public decimal Distance { get; set; }

    [Range(1, int.MaxValue)]
    public int EventTypeId { get; set; }

    public IReadOnlyList<EventTypeResponse> EventTypes { get; set; } = [];
}

public sealed class OrganiserEventDetailsViewModel
{
    public EventResponse Event { get; init; } = new();
    public bool IsOwner { get; init; }
}

public sealed class OrganiserCategoriesViewModel
{
    public EventResponse Event { get; init; } = new();
    public IReadOnlyList<CategoryResponse> Categories { get; init; } = [];
}

public sealed class OrganiserCategoryFormViewModel
{
    public int EventId { get; set; }
    public int? CategoryId { get; set; }
    public string EventName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinimumAge { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaximumAge { get; set; }
}

public sealed class OrganiserEnrolmentsViewModel
{
    public EventResponse Event { get; init; } = new();
    public IReadOnlyList<EventEnrolmentResponse> Enrolments { get; init; } = [];
    public IReadOnlyDictionary<int, string> CategoryNames { get; init; } =
        new Dictionary<int, string>();
}

public sealed class OrganiserResultsViewModel
{
    public EventResponse Event { get; init; } = new();
    public IReadOnlyList<OrganiserResultRowViewModel> Results { get; init; } = [];
}

public sealed class OrganiserResultRowViewModel
{
    public EventEnrolmentResponse Enrolment { get; init; } = new();
    public string CategoryName { get; init; } = string.Empty;
    public RaceResultResponse? Result { get; init; }
}

public sealed class OrganiserResultFormViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public int? ResultId { get; set; }
    public bool HasResult { get; set; }

    [Required]
    [RegularExpression(@"^\d+:[0-5]\d(?::[0-5]\d)?$",
        ErrorMessage = "Enter a time as HH:mm or HH:mm:ss.")]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int FinishingPosition { get; set; }
}

public sealed class OrganiserEnrolmentStatusViewModel
{
    public int EventId { get; set; }

    [Required]
    [RegularExpression("^(ACTIVE|CANCELLED|COMPLETED)$")]
    public string Status { get; set; } = "ACTIVE";
}
