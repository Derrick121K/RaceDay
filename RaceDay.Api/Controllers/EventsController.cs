
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Api.Services;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // Anyone can browse the available events.
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _db.Events
            .AsNoTracking()
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventID,
                e.EventName,
                e.Description,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventTypeID,
                EventTypeName = e.EventType!.TypeName,
                e.OrganiserID,
                e.BannerImagePath
            })
            .ToListAsync();

        return Ok(events);
    }

    // Anyone can view an individual event.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var raceEvent = await _db.Events
            .AsNoTracking()
            .Where(e => e.EventID == id)
            .Select(e => new
            {
                e.EventID,
                e.EventName,
                e.Description,
                e.EventDate,
                e.Location,
                e.Distance,
                e.EventTypeID,
                EventTypeName = e.EventType!.TypeName,
                e.OrganiserID,
                e.BannerImagePath
            })
            .SingleOrDefaultAsync();

        if (raceEvent is null)
            return NotFound();

        return Ok(raceEvent);
    }

    // Only an organiser with a valid session can create an event.
    [HttpPost]
    [SessionAuthorize("ORGANISER")]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
            return Unauthorized();

        var typeExists = await _db.EventTypes
            .AnyAsync(t => t.EventTypeID == request.EventTypeId);

        if (!typeExists)
            return BadRequest(new { message = "The event type does not exist." });

        var raceEvent = new Event
        {
            EventName = request.EventName.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate.Date,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            EventTypeID = request.EventTypeId,
            OrganiserID = organiserId.Value,
            CreatedAt = DateTime.Now
        };

        _db.Events.Add(raceEvent);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEvent),
            new { id = raceEvent.EventID },
            new
            {
                raceEvent.EventID,
                raceEvent.EventName,
                raceEvent.EventDate,
                raceEvent.Location,
                raceEvent.Distance,
                raceEvent.EventTypeID,
                raceEvent.OrganiserID,
                raceEvent.BannerImagePath
            });
    }

    // Organisers may update only events they own.
    [HttpPut("{id:int}")]
    [SessionAuthorize("ORGANISER")]
    public async Task<IActionResult> UpdateEvent(
        int id,
        EventRequest request)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
            return Unauthorized();

        var raceEvent = await _db.Events.FindAsync(id);

        if (raceEvent is null)
            return NotFound();

        if (raceEvent.OrganiserID != organiserId.Value)
            return StatusCode(StatusCodes.Status403Forbidden);

        var typeExists = await _db.EventTypes
            .AnyAsync(t => t.EventTypeID == request.EventTypeId);

        if (!typeExists)
            return BadRequest(new { message = "The event type does not exist." });

        raceEvent.EventName = request.EventName.Trim();
        raceEvent.Description = request.Description.Trim();
        raceEvent.EventDate = request.EventDate.Date;
        raceEvent.Location = request.Location.Trim();
        raceEvent.Distance = request.Distance;
        raceEvent.EventTypeID = request.EventTypeId;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            raceEvent.EventID,
            raceEvent.EventName,
            raceEvent.EventDate,
            raceEvent.Location,
            raceEvent.Distance,
            BannerImagePath = raceEvent.BannerImagePath
        });
    }

    // Organisers may delete only events they own.
    [HttpDelete("{id:int}")]
    [SessionAuthorize("ORGANISER")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
            return Unauthorized();

        var raceEvent = await _db.Events.FindAsync(id);

        if (raceEvent is null)
            return NotFound();

        if (raceEvent.OrganiserID != organiserId.Value)
            return StatusCode(StatusCodes.Status403Forbidden);

        _db.Events.Remove(raceEvent);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "This event cannot be deleted because related records exist."
            });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/banner")]
    [SessionAuthorize("ORGANISER")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024 + 65536)]
    public async Task<IActionResult> UploadBanner(
        int id,
        IFormFile file,
        [FromServices] IBlobStorageService blobStorage,
        CancellationToken cancellationToken)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");
        if (organiserId is null)
            return Unauthorized();

        var raceEvent = await _db.Events.FindAsync(id);
        if (raceEvent is null)
            return NotFound();

        if (raceEvent.OrganiserID != organiserId.Value)
            return StatusCode(StatusCodes.Status403Forbidden);

        try
        {
            raceEvent.BannerImagePath = await blobStorage.UploadAsync(
                file, "event-banners", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Image storage is not configured." });
        }

        return Ok(new ImageUploadResponse { ImagePath = raceEvent.BannerImagePath! });
    }

    // Event banners are public event media, but the underlying blob container remains private.
    [HttpGet("{id:int}/banner-image")]
    public async Task<IActionResult> GetBannerImage(
        int id,
        [FromServices] IBlobStorageService blobStorage,
        CancellationToken cancellationToken)
    {
        var imagePath = await _db.Events
            .AsNoTracking()
            .Where(raceEvent => raceEvent.EventID == id)
            .Select(raceEvent => raceEvent.BannerImagePath)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(imagePath))
            return NotFound();

        try
        {
            var image = await blobStorage.DownloadAsync(imagePath, cancellationToken);
            if (image is null)
                return NotFound();

            return File(image.Content, image.ContentType);
        }
        catch (InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
