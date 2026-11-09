using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public class ProfileAndEndpointTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public ProfileAndEndpointTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Profile_GetReturnsCurrentUserOnNewAndCompatibilityRoutes()
    {
        var (client, email) = await CreateParticipantClientAsync();

        var currentProfile = await client.GetAsync("/api/users/me");
        var compatibilityProfile = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, currentProfile.StatusCode);
        Assert.Equal(HttpStatusCode.OK, compatibilityProfile.StatusCode);

        var profile = await currentProfile.Content
            .ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal(email, profile.Email);
        Assert.Equal("Profile", profile.FirstName);
        Assert.Equal("Participant", profile.LastName);
        Assert.Equal("PARTICIPANT", profile.Role);

        var json = await currentProfile.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Profile_PutUpdatesEditableFieldsAndIgnoresIdentityAndRoleChanges()
    {
        var (client, email) = await CreateParticipantClientAsync();

        var response = await client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Updated",
            lastName = "Runner",
            phoneNumber = "+27123456789",
            email = "attacker@example.com",
            role = "ORGANISER"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("Updated", profile.FirstName);
        Assert.Equal("Runner", profile.LastName);
        Assert.Equal("+27123456789", profile.PhoneNumber);
        Assert.Equal(email, profile.Email);
        Assert.Equal("PARTICIPANT", profile.Role);
    }

    [Fact]
    public async Task Profile_PutRejectsInvalidRequiredAndPhoneFields()
    {
        var (client, _) = await CreateParticipantClientAsync();

        var missingName = await client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = " ",
            lastName = "Runner",
            phoneNumber = (string?)null
        });
        var invalidPhone = await client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Updated",
            lastName = "Runner",
            phoneNumber = "not a phone number"
        });

        Assert.Equal(HttpStatusCode.BadRequest, missingName.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPhone.StatusCode);
    }

    [Fact]
    public async Task MyResults_ReturnRequiredFieldsAndOnlyCurrentParticipantsResults()
    {
        var (participantOne, emailOne) = await CreateParticipantClientAsync();
        var (participantTwo, emailTwo) = await CreateParticipantClientAsync();
        var firstResultId = await SeedTwoParticipantResultsAsync(emailOne, emailTwo);

        var firstResponse = await participantOne.GetAsync("/api/results/me");
        var secondResponse = await participantTwo.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var firstResults = await firstResponse.Content
            .ReadFromJsonAsync<List<ParticipantResultResponse>>();
        var secondResults = await secondResponse.Content
            .ReadFromJsonAsync<List<ParticipantResultResponse>>();

        var first = Assert.Single(firstResults!);
        Assert.Equal(firstResultId, first.ResultId);
        Assert.Equal("Sprint Test Race", first.EventName);
        Assert.Equal(new DateTime(2027, 5, 14), first.EventDate);
        Assert.Equal("10 km", first.CategoryName);
        Assert.Equal(TimeSpan.FromMinutes(47), first.FinishTime);
        Assert.Equal(3, first.FinishingPosition);
        Assert.Single(secondResults!);
        Assert.NotEqual(first.ResultId, secondResults![0].ResultId);
    }

    [Fact]
    public async Task EnrolmentDetail_IsVisibleOnlyToParticipantOwner()
    {
        var (owner, ownerEmail) = await CreateParticipantClientAsync();
        var (otherParticipant, _) = await CreateParticipantClientAsync();
        var enrolmentId = await SeedEnrolmentAsync(ownerEmail);

        var ownerResponse = await owner.GetAsync($"/api/enrolments/{enrolmentId}");
        var otherResponse = await otherParticipant.GetAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        var detail = await ownerResponse.Content
            .ReadFromJsonAsync<EnrolmentDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal(enrolmentId, detail.EnrolmentId);
        Assert.Equal("Ownership Test Race", detail.EventName);
        Assert.Equal("5 km", detail.CategoryName);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task EnrolmentDetail_OrganiserMustOwnTheEvent()
    {
        var (eventOrganiser, ownerEmail) = await CreateOrganiserClientAsync();
        var (otherOrganiser, _) = await CreateOrganiserClientAsync();
        var (_, participantEmail) = await CreateParticipantClientAsync();
        var enrolmentId = await SeedEnrolmentAsync(participantEmail, ownerEmail);

        var ownerResponse = await eventOrganiser.GetAsync($"/api/enrolments/{enrolmentId}");
        var otherResponse = await otherOrganiser.GetAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    private async Task<(HttpClient Client, string Email)> CreateParticipantClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"participant-{Guid.NewGuid():N}@example.com";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Profile",
            lastName = "Participant",
            email,
            password = "RaceDayTest2026!",
            role = "PARTICIPANT",
            phoneNumber = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "RaceDayTest2026!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (client, email);
    }

    private async Task<(HttpClient Client, string Email)> CreateOrganiserClientAsync()
    {
        var email = $"organiser-{Guid.NewGuid():N}@example.com";
        const string password = "RaceDayOrganiser2026!";
        using (var scope = _factory.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<RaceDayDbContext>();
            var hasher = services.GetRequiredService<
                Microsoft.AspNetCore.Identity.IPasswordHasher<User>>();
            var organiser = new User
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = email,
                Role = "ORGANISER",
                CreatedAt = DateTime.UtcNow
            };
            organiser.PasswordHash = hasher.HashPassword(organiser, password);
            db.Users.Add(organiser);
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (client, email);
    }

    private async Task<int> SeedEnrolmentAsync(
        string participantEmail,
        string? organiserEmail = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var participant = await db.Users.SingleAsync(user => user.Email == participantEmail);
        var organiserId = organiserEmail is null
            ? participant.UserID
            : await db.Users
                .Where(user => user.Email == organiserEmail)
                .Select(user => user.UserID)
                .SingleAsync();
        var eventType = new EventType { TypeName = $"Test-{Guid.NewGuid():N}" };
        db.EventTypes.Add(eventType);
        await db.SaveChangesAsync();

        var raceEvent = new Event
        {
            EventName = "Ownership Test Race",
            Description = "Test event",
            EventDate = new DateTime(2027, 5, 14),
            Location = "Test location",
            Distance = 5,
            EventTypeID = eventType.EventTypeID,
            OrganiserID = organiserId,
            CreatedAt = DateTime.UtcNow
        };
        db.Events.Add(raceEvent);
        await db.SaveChangesAsync();

        var category = new Category
        {
            EventID = raceEvent.EventID,
            CategoryName = "5 km"
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var enrolment = new Enrolment
        {
            ParticipantID = participant.UserID,
            EventID = raceEvent.EventID,
            CategoryID = category.CategoryID,
            EnrolmentDate = DateTime.UtcNow,
            Status = "ACTIVE"
        };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        return enrolment.EnrolmentID;
    }

    private async Task<int> SeedTwoParticipantResultsAsync(
        string firstParticipantEmail,
        string secondParticipantEmail)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var firstParticipant = await db.Users
            .SingleAsync(user => user.Email == firstParticipantEmail);
        var secondParticipant = await db.Users
            .SingleAsync(user => user.Email == secondParticipantEmail);

        var eventType = new EventType { TypeName = $"Test-{Guid.NewGuid():N}" };
        db.EventTypes.Add(eventType);
        await db.SaveChangesAsync();

        var raceEvent = new Event
        {
            EventName = "Sprint Test Race",
            Description = "Test event",
            EventDate = new DateTime(2027, 5, 14),
            Location = "Test location",
            Distance = 10,
            EventTypeID = eventType.EventTypeID,
            OrganiserID = firstParticipant.UserID,
            CreatedAt = DateTime.UtcNow
        };
        db.Events.Add(raceEvent);
        await db.SaveChangesAsync();

        var category = new Category
        {
            EventID = raceEvent.EventID,
            CategoryName = "10 km"
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var firstEnrolment = new Enrolment
        {
            ParticipantID = firstParticipant.UserID,
            EventID = raceEvent.EventID,
            CategoryID = category.CategoryID,
            EnrolmentDate = DateTime.UtcNow,
            Status = "COMPLETED"
        };
        var secondEnrolment = new Enrolment
        {
            ParticipantID = secondParticipant.UserID,
            EventID = raceEvent.EventID,
            CategoryID = category.CategoryID,
            EnrolmentDate = DateTime.UtcNow,
            Status = "COMPLETED"
        };
        db.Enrolments.AddRange(firstEnrolment, secondEnrolment);
        await db.SaveChangesAsync();

        var firstResult = new Result
        {
            EnrolmentID = firstEnrolment.EnrolmentID,
            FinishTime = TimeSpan.FromMinutes(47),
            FinishingPosition = 3,
            RecordedAt = DateTime.UtcNow
        };
        db.Results.AddRange(firstResult, new Result
        {
            EnrolmentID = secondEnrolment.EnrolmentID,
            FinishTime = TimeSpan.FromMinutes(50),
            FinishingPosition = 8,
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return firstResult.ResultID;
    }
}
