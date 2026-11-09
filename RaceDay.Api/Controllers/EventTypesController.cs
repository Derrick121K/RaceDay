using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/event-types")]
public sealed class EventTypesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventTypesController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventTypeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventTypeResponse>>> GetEventTypes()
    {
        var eventTypes = await _db.EventTypes
            .AsNoTracking()
            .OrderBy(type => type.TypeName)
            .Select(type => new EventTypeResponse
            {
                EventTypeID = type.EventTypeID,
                TypeName = type.TypeName,
                Description = type.Description
            })
            .ToListAsync();

        return Ok(eventTypes);
    }
}
