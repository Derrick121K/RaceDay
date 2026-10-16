
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // Participants can view only their own enrolments.
    [HttpGet("me")]
    [SessionAuthorize("PARTICIPANT")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var participantId = HttpContext.Session.GetInt32("UserId");

        if (participantId is null)
            return Unauthorized();

        var enrolments = await _db.Enrolments
            .AsNoTracking()
            .Where(e => e.ParticipantID == participantId.Value)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => new
            {
                e.EnrolmentID,
                e.EventID,
                e.CategoryID,
                e.EnrolmentDate,
                e.Status
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    // Participants may enter an event once, choosing one of its categories.
    [HttpPost]
    [SessionAuthorize("PARTICIPANT")]
    public async Task<IActionResult> CreateEnrolment(EnrolmentRequest request)
    {
        var participantId = HttpContext.Session.GetInt32("UserId");

        if (participantId is null)
            return Unauthorized();

        var eventExists = await _db.Events
            .AnyAsync(e => e.EventID == request.EventId);

        if (!eventExists)
            return NotFound(new { message = "Event not found." });

        var categoryExists = await _db.Categories.AnyAsync(c =>
            c.CategoryID == request.CategoryId &&
            c.EventID == request.EventId);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                message = "Choose a category that belongs to the selected event."
            });
        }

        var alreadyEntered = await _db.Enrolments.AnyAsync(e =>
            e.ParticipantID == participantId.Value &&
            e.EventID == request.EventId);

        if (alreadyEntered)
        {
            return Conflict(new
            {
                message = "You have already enrolled in this event."
            });
        }

        var enrolment = new Enrolment
        {
            ParticipantID = participantId.Value,
            EventID = request.EventId,
            CategoryID = request.CategoryId,
            EnrolmentDate = DateTime.Now,
            Status = "ACTIVE"
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            enrolment.EnrolmentID,
            enrolment.EventID,
            enrolment.CategoryID,
            enrolment.EnrolmentDate,
            enrolment.Status
        });
    }

    // Organisers can view entries for events they own.
    [HttpGet("~/api/events/{eventId:int}/enrolments")]
    [SessionAuthorize("ORGANISER")]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
            return Unauthorized();

        var raceEvent = await _db.Events.FindAsync(eventId);

        if (raceEvent is null)
            return NotFound();

        if (raceEvent.OrganiserID != organiserId.Value)
            return Forbid();

        var enrolments = await _db.Enrolments
            .AsNoTracking()
            .Where(e => e.EventID == eventId)
            .Select(e => new
            {
                e.EnrolmentID,
                e.ParticipantID,
                e.EventID,
                e.CategoryID,
                e.EnrolmentDate,
                e.Status
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    // Organisers may update enrolment status only for their own events.
    [HttpPut("{id:int}/status")]
    [SessionAuthorize("ORGANISER")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        EnrolmentStatusRequest request)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
            return Unauthorized();

        var enrolment = await _db.Enrolments.FindAsync(id);

        if (enrolment is null)
            return NotFound();

        var raceEvent = await _db.Events.FindAsync(enrolment.EventID);

        if (raceEvent is null)
            return NotFound();

        if (raceEvent.OrganiserID != organiserId.Value)
            return Forbid();

        enrolment.Status = request.Status.Trim().ToUpperInvariant();

        await _db.SaveChangesAsync();

        return Ok(new
        {
            enrolment.EnrolmentID,
            enrolment.Status
        });
    }
}
