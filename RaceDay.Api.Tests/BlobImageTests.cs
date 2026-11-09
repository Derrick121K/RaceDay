using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public sealed class BlobImageTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public BlobImageTests(RaceDayWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task EventBanner_UploadAndDownloadRequireCorrectOrganiserSession()
    {
        var (organiser, organiserEmail) = await CreateOrganiserClientAsync();
        var (otherOrganiser, _) = await CreateOrganiserClientAsync();
        var (participant, _) = await CreateParticipantClientAsync();
        var eventId = await SeedEventAsync(organiserEmail);

        using var form = CreatePngForm();
        var upload = await organiser.PostAsync($"/api/events/{eventId}/banner", form);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var response = await upload.Content.ReadFromJsonAsync<ImageUploadResponse>();
        Assert.NotNull(response);
        Assert.StartsWith("event-banners/", response.ImagePath);

        var eventResponse = await organiser.GetFromJsonAsync<EventResponse>($"/api/events/{eventId}");
        Assert.Equal(response.ImagePath, eventResponse!.BannerImagePath);

        var download = await organiser.GetAsync($"/api/events/{eventId}/banner-image");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/png", download.Content.Headers.ContentType?.MediaType);

        var bytes = await download.Content.ReadAsByteArrayAsync();
        Assert.Equal(PngBytes, bytes);

        using var otherForm = CreatePngForm();
        var wrongOwner = await otherOrganiser.PostAsync($"/api/events/{eventId}/banner", otherForm);
        using var participantForm = CreatePngForm();
        var wrongRole = await participant.PostAsync($"/api/events/{eventId}/banner", participantForm);
        Assert.Equal(HttpStatusCode.Forbidden, wrongOwner.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrongRole.StatusCode);
    }

    [Fact]
    public async Task ProfileImage_UploadAndDownloadAreLimitedToCurrentSession()
    {
        var (participant, _) = await CreateParticipantClientAsync();

        using var form = CreatePngForm();
        var upload = await participant.PostAsync("/api/users/me/profile-image", form);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var response = await upload.Content.ReadFromJsonAsync<ImageUploadResponse>();
        Assert.NotNull(response);
        Assert.StartsWith("profile-images/", response.ImagePath);

        var profile = await participant.GetFromJsonAsync<UserProfileResponse>("/api/users/me");
        Assert.Equal(response.ImagePath, profile!.ProfileImagePath);

        var download = await participant.GetAsync("/api/users/me/profile-image");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(PngBytes, await download.Content.ReadAsByteArrayAsync());

        using var anonymous = _factory.CreateClient();
        var denied = await anonymous.GetAsync("/api/users/me/profile-image");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }

    [Fact]
    public async Task ImageUpload_RejectsUnsupportedOrMismatchedImageContent()
    {
        var (participant, _) = await CreateParticipantClientAsync();
        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(content, "file", "not-an-image.png");

        var response = await participant.PostAsync("/api/users/me/profile-image", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(HttpClient Client, string Email)> CreateParticipantClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"image-participant-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Image",
            lastName = "Participant",
            email,
            password = "RaceDayImageTest2026!",
            role = "PARTICIPANT"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "RaceDayImageTest2026!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (client, email);
    }

    private async Task<(HttpClient Client, string Email)> CreateOrganiserClientAsync()
    {
        const string password = "RaceDayImageOrganiser2026!";
        var email = $"image-organiser-{Guid.NewGuid():N}@example.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var user = new User
            {
                FirstName = "Image",
                LastName = "Organiser",
                Email = email,
                Role = "ORGANISER",
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (client, email);
    }

    private async Task<int> SeedEventAsync(string organiserEmail)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var organiser = await db.Users.SingleAsync(user => user.Email == organiserEmail);
        var type = new EventType { TypeName = $"Image-{Guid.NewGuid():N}" };
        db.EventTypes.Add(type);
        await db.SaveChangesAsync();

        var raceEvent = new Event
        {
            EventName = "Image Test Race",
            Description = "Banner upload test",
            EventDate = new DateTime(2027, 6, 1),
            Location = "Test venue",
            Distance = 5,
            EventTypeID = type.EventTypeID,
            OrganiserID = organiser.UserID,
            CreatedAt = DateTime.UtcNow
        };
        db.Events.Add(raceEvent);
        await db.SaveChangesAsync();
        return raceEvent.EventID;
    }

    private static MultipartFormDataContent CreatePngForm()
    {
        var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(PngBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(content, "file", "race-banner.png");
        return form;
    }

    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44
    ];
}