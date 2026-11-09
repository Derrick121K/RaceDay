using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Mvc.Models;

public sealed class ParticipantDashboardViewModel
{
    public UserProfileResponse Profile { get; init; } = null!;
    public IReadOnlyList<EventResponse> UpcomingEvents { get; init; } = [];
    public int EnrolmentCount { get; init; }
    public IReadOnlyList<EventEnrolmentResponse> RecentEnrolments { get; init; } = [];
    public IReadOnlyList<ParticipantResultResponse> RecentResults { get; init; } = [];
}

public sealed class ParticipantEventsViewModel
{
    public string? Search { get; init; }
    public IReadOnlyList<EventResponse> Events { get; init; } = [];
    public IReadOnlySet<int> EnrolledEventIds { get; init; } = new HashSet<int>();
}

public sealed class ParticipantEventDetailsViewModel
{
    public EventResponse Event { get; init; } = null!;
    public IReadOnlyList<CategoryResponse> Categories { get; init; } = [];
    public string? EnrolmentStatus { get; init; }
}

public sealed class ParticipantEnrolmentFormViewModel
{
    public int EventId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Choose an event category.")]
    public int CategoryId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public IReadOnlyList<CategoryResponse> Categories { get; set; } = [];
}

public sealed class ParticipantEnrolmentsViewModel
{
    public IReadOnlyList<ParticipantEnrolmentItemViewModel> Enrolments { get; init; } = [];
}

public sealed class ParticipantEnrolmentItemViewModel
{
    public EventEnrolmentResponse Enrolment { get; init; } = null!;
    public EventResponse? Event { get; init; }
    public CategoryResponse? Category { get; init; }
}

public sealed class ParticipantResultsViewModel
{
    public IReadOnlyList<ParticipantResultResponse> Results { get; init; } = [];
}
