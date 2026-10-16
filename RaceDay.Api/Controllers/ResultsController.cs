
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    // Participants can view only their own results.
    [HttpGet("results/me")]
    [SessionAuthorize("PARTICIPANT")]
    public async Task<IActionResult> GetMyResults()
    {
        var participantId = HttpContext.Session.GetInt32("UserId");

        if (participantId is null)
            return Unauthorized();

        var results = await _db.Results
            .AsNoTracking()
            .Where(r => _db.Enrolments.Any(e =>
                e.EnrolmentID == r.EnrolmentID &&
                e.ParticipantID == participantId.Value))
            .OrderBy(r => r.FinishingPosition)
            .Select(r => new
            {
                r.ResultID,
                r.EnrolmentID,
                r.FinishTime,
                r.FinishingPosition,
                r.RecordedAt
            })
            .ToListAsync();

        return Ok(results);
    }

    // An authenticated participant or organiser may view a result
    // only when they own the enrolment or the event.
    [HttpGet("enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize]
    public async Task<IActionResult> GetEnrolmentResult(int enrolmentId)
    {
        var enrolment = await _db.Enrolments
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

        if (enrolment is null)
            return NotFound();

        var role = HttpContext.Session.GetString("Role");
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId is null || role is null)
            return Unauthorized();

        if (role == "PARTICIPANT" &&
            enrolment.ParticipantID != userId.Value)
        {
            return Forbid();
        }

        if (role == "ORGANISER")
        {
            var ownsEvent = await _db.Events.AnyAsync(e =>
                e.EventID == enrolment.EventID &&
                e.OrganiserID == userId.Value);

            if (!ownsEvent)
                return Forbid();
        }

        var result = await _db.Results
            .AsNoTracking()
            .Where(r => r.EnrolmentID == enrolmentId)
            .Select(r => new
            {
                r.ResultID,
                r.EnrolmentID,
                r.FinishTime,
                r.FinishingPosition,
                r.RecordedAt
            })
            .SingleOrDefaultAsync();

        return result is null ? NotFound() : Ok(result);
    }

    // Organisers can record results only for their own events.
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize("ORGANISER")]
    public async Task<IActionResult> CreateResult(
        int enrolmentId,
        ResultRequest request)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
            return Unauthorized();

        if (request.FinishTime < TimeSpan.Zero)
        {
            return BadRequest(new
            {
                message = "Finish time cannot be negative."
            });
        }

        var enrolment = await _db.Enrolments
            .SingleOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

        if (enrolment is null)
            return NotFound();

        var ownsEvent = await _db.Events.AnyAsync(e =>
            e.EventID == enrolment.EventID &&
            e.OrganiserID == organiserId.Value);

        if (!ownsEvent)
            return Forbid();

        var alreadyHasResult = await _db.Results
            .AnyAsync(r => r.EnrolmentID == enrolmentId);

        if (alreadyHasResult)
        {
            return Conflict(new
            {
                message = "A result already exists for this enrolment."
            });
        }

        var result = new Result
        {
            EnrolmentID = enrolmentId,
            FinishTime = request.FinishTime,
            FinishingPosition = request.FinishingPosition,
            RecordedAt = DateTime.Now
        };

        _db.Results.Add(result);
        enrolment.Status = "COMPLETED";

        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            result.ResultID,
            result.EnrolmentID,
            result.FinishTime,
            result.FinishingPosition,
            result.RecordedAt
        });
    }

    // Organisers can correct results for their own events.
    [HttpPut("enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize("ORGANISER")]
    public async Task<IActionResult> UpdateResult(
        int enrolmentId,
        ResultRequest request)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
            return Unauthorized();

        if (request.FinishTime < TimeSpan.Zero)
        {
            return BadRequest(new
            {
                message = "Finish time cannot be negative."
            });
        }

        var enrolment = await _db.Enrolments
            .SingleOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

        if (enrolment is null)
            return NotFound();

        var ownsEvent = await _db.Events.AnyAsync(e =>
            e.EventID == enrolment.EventID &&
            e.OrganiserID == organiserId.Value);

        if (!ownsEvent)
            return Forbid();

        var result = await _db.Results
            .SingleOrDefaultAsync(r => r.EnrolmentID == enrolmentId);

        if (result is null)
            return NotFound();

        result.FinishTime = request.FinishTime;
        result.FinishingPosition = request.FinishingPosition;
        result.RecordedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            result.ResultID,
            result.EnrolmentID,
            result.FinishTime,
            result.FinishingPosition,
            result.RecordedAt
        });
    }

    // Organisers can remove incorrect results for their own events.
    [HttpDelete("enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize("ORGANISER")]
    public async Task<IActionResult> DeleteResult(int enrolmentId)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
            return Unauthorized();

        var enrolment = await _db.Enrolments
            .SingleOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

        if (enrolment is null)
            return NotFound();

        var ownsEvent = await _db.Events.AnyAsync(e =>
            e.EventID == enrolment.EventID &&
            e.OrganiserID == organiserId.Value);

        if (!ownsEvent)
            return Forbid();

        var result = await _db.Results
            .SingleOrDefaultAsync(r => r.EnrolmentID == enrolmentId);

        if (result is null)
            return NotFound();

        _db.Results.Remove(result);
        enrolment.Status = "ACTIVE";

        await _db.SaveChangesAsync();

        return NoContent();
    }
}
