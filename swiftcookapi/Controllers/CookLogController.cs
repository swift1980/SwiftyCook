using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    // Ticket 20: the cook log is the single source of truth for "when was this made".
    [Route("api/[controller]")]
    [ApiController]
    public class CookLogController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;

        public CookLogController(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>Newest first. <paramref name="recipeId"/> is required to keep results bounded.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CookLogDto>>> GetForRecipe([FromQuery] int recipeId)
        {
            if (recipeId <= 0) return BadRequest(new { message = "'recipeId' is required." });

            var logs = await _context.CookLogs
                .Where(l => l.RecipeId == recipeId)
                .OrderByDescending(l => l.CookedOn).ThenByDescending(l => l.Id)
                .ToListAsync();
            return _mapper.Map<List<CookLogDto>>(logs);
        }

        [HttpPost]
        public async Task<ActionResult<CookLogDto>> Create(CookLogCreateDto dto)
        {
            if (!await _context.Recipes.AnyAsync(r => r.Id == dto.RecipeId))
                return BadRequest(new { message = "Unknown recipe." });
            var future = FutureError(dto.CookedOn!.Value);
            if (future != null) return BadRequest(new { message = future });

            var log = new CookLog
            {
                RecipeId = dto.RecipeId,
                CookedOn = dto.CookedOn!.Value,
                Servings = dto.Servings,
                Notes = CleanNotes(dto.Notes)
            };
            _context.CookLogs.Add(log);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetForRecipe), new { recipeId = log.RecipeId }, _mapper.Map<CookLogDto>(log));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CookLogDto>> Update(int id, CookLogUpdateDto dto)
        {
            var log = await _context.CookLogs.FindAsync(id);
            if (log == null) return NotFound();
            var future = FutureError(dto.CookedOn!.Value);
            if (future != null) return BadRequest(new { message = future });

            log.CookedOn = dto.CookedOn!.Value;
            log.Servings = dto.Servings;
            log.Notes = CleanNotes(dto.Notes);
            await _context.SaveChangesAsync();
            return _mapper.Map<CookLogDto>(log);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var log = await _context.CookLogs.FindAsync(id);
            if (log == null) return NotFound();

            // Planned meals that pointed at this log become "not made" again.
            var linked = await _context.MealPlanEntries.Where(e => e.CookLogId == id).ToListAsync();
            foreach (var entry in linked) entry.CookLogId = null;
            _context.CookLogs.Remove(log);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // One day of slack so a client running ahead of the server's UTC date is not rejected.
        public static DateOnly LatestAllowedDate() => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        public static string? FutureError(DateOnly date) =>
            date > LatestAllowedDate() ? "A meal cannot be logged as made in the future." : null;

        private static string? CleanNotes(string? notes) =>
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
