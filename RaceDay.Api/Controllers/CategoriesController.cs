
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    // Anyone can view the categories for an existing event.
    [HttpGet("~/api/events/{eventId:int}/categories")]
    public async Task<IActionResult> GetEventCategories(int eventId)
    {
        var eventExists = await _db.Events
            .AnyAsync(e => e.EventID == eventId);

        if (!eventExists)
            return NotFound();

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.CategoryName)
            .Select(c => new
            {
                c.CategoryID,
                c.EventID,
                c.CategoryName,
                c.Description,
                c.MinimumAge,
                c.MaximumAge
            })
            .ToListAsync();

        return Ok(categories);
    }

    // Anyone can view one category.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategory(int id)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .Where(c => c.CategoryID == id)
            .Select(c => new
            {
                c.CategoryID,
                c.EventID,
                c.CategoryName,
                c.Description,
                c.MinimumAge,
                c.MaximumAge
            })
            .SingleOrDefaultAsync();

        return category is null ? NotFound() : Ok(category);
    }

    // Only the organiser who owns the event may add a category.
    [HttpPost("~/api/events/{eventId:int}/categories")]
    [SessionAuthorize("ORGANISER")]
    public async Task<IActionResult> CreateCategory(
        int eventId,
        CategoryRequest request)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
            return Unauthorized();

        var raceEvent = await _db.Events.FindAsync(eventId);

        if (raceEvent is null)
            return NotFound();

        if (raceEvent.OrganiserID != organiserId.Value)
            return Forbid();

        if (request.MinimumAge.HasValue &&
            request.MaximumAge.HasValue &&
            request.MaximumAge.Value < request.MinimumAge.Value)
        {
            return BadRequest(new
            {
                message = "Maximum age cannot be less than minimum age."
            });
        }

        var name = request.CategoryName.Trim();

        var duplicate = await _db.Categories.AnyAsync(c =>
            c.EventID == eventId && c.CategoryName == name);

        if (duplicate)
            return Conflict(new { message = "This category already exists for the event." });

        var category = new Category
        {
            EventID = eventId,
            CategoryName = name,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            MinimumAge = request.MinimumAge,
            MaximumAge = request.MaximumAge
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.CategoryID },
            new
            {
                category.CategoryID,
                category.EventID,
                category.CategoryName,
                category.Description,
                category.MinimumAge,
                category.MaximumAge
            });
    }

    // Only the event's organiser may update its category.
    [HttpPut("{id:int}")]
    [SessionAuthorize("ORGANISER")]
    public async Task<IActionResult> UpdateCategory(
        int id,
        CategoryRequest request)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
            return Unauthorized();

        var category = await _db.Categories.FindAsync(id);

        if (category is null)
            return NotFound();

        var raceEvent = await _db.Events.FindAsync(category.EventID);

        if (raceEvent is null)
            return NotFound();

        if (raceEvent.OrganiserID != organiserId.Value)
            return Forbid();

        if (request.MinimumAge.HasValue &&
            request.MaximumAge.HasValue &&
            request.MaximumAge.Value < request.MinimumAge.Value)
        {
            return BadRequest(new
            {
                message = "Maximum age cannot be less than minimum age."
            });
        }

        var name = request.CategoryName.Trim();

        var duplicate = await _db.Categories.AnyAsync(c =>
            c.EventID == category.EventID &&
            c.CategoryID != id &&
            c.CategoryName == name);

        if (duplicate)
            return Conflict(new { message = "This category already exists for the event." });

        category.CategoryName = name;
        category.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        category.MinimumAge = request.MinimumAge;
        category.MaximumAge = request.MaximumAge;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            category.CategoryID,
            category.EventID,
            category.CategoryName,
            category.Description,
            category.MinimumAge,
            category.MaximumAge
        });
    }

    // Only the event's organiser may delete its category.
    [HttpDelete("{id:int}")]
    [SessionAuthorize("ORGANISER")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
            return Unauthorized();

        var category = await _db.Categories.FindAsync(id);

        if (category is null)
            return NotFound();

        var raceEvent = await _db.Events.FindAsync(category.EventID);

        if (raceEvent is null)
            return NotFound();

        if (raceEvent.OrganiserID != organiserId.Value)
            return Forbid();

        _db.Categories.Remove(category);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "This category cannot be deleted because related enrolments exist."
            });
        }

        return NoContent();
    }
}
