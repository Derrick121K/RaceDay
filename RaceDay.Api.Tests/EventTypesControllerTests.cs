using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public class EventTypesControllerTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EventTypesControllerTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEventTypes_ReturnsLookupValuesPublicly()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            db.EventTypes.AddRange(
                new EventType { TypeName = "Road Running", Description = "Running events" },
                new EventType { TypeName = "Cycling", Description = "Cycle events" });
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/event-types");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var eventTypes = await response.Content.ReadFromJsonAsync<List<EventTypeResponse>>();
        Assert.NotNull(eventTypes);
        Assert.Equal(new[] { "Cycling", "Road Running" },
            eventTypes.Select(type => type.TypeName));
    }

    [Fact]
    public async Task CreateEvent_RequiresOrganiserSession()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/events", new EventRequest
        {
            EventName = "Unauthorized race",
            Description = "Should be rejected",
            EventDate = new DateTime(2027, 8, 1),
            Location = "Test location",
            Distance = 10,
            EventTypeId = 1
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void EventRequest_DistanceValidation_IsCultureSafeForSouthAfrica()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var southAfricanCulture = CultureInfo.GetCultureInfo("en-ZA");
            CultureInfo.CurrentCulture = southAfricanCulture;
            CultureInfo.CurrentUICulture = southAfricanCulture;

            var request = new EventRequest
            {
                EventName = "Culture validation race",
                Description = "Regression test",
                EventDate = new DateTime(2027, 8, 1),
                Location = "Test location",
                Distance = 6,
                EventTypeId = 1
            };

            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                results,
                validateAllProperties: true);

            Assert.True(isValid, string.Join("; ", results.Select(result => result.ErrorMessage)));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
