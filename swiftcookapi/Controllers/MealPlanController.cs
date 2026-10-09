using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MealPlanController : ControllerBase
    {
        // Recipes in this category are cocktails (see CocktailController).
        public const int CocktailCategoryId = 1;
        public const int MaxRangeDays = 100;

        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;

        public MealPlanController(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <summary>Entries from <paramref name="from"/> to <paramref name="to"/> inclusive.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MealPlanEntryDto>>> GetRange([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            if (from == default || to == default)
                return BadRequest(new { message = "'from' and 'to' dates are required." });
            if (to < from)
                return BadRequest(new { message = "'to' cannot be before 'from'." });
            if (to.DayNumber - from.DayNumber >= MaxRangeDays)
                return BadRequest(new { message = $"The range cannot exceed {MaxRangeDays} days." });

            var entries = await _context.MealPlanEntries
                .Include(e => e.Recipe)
                .Where(e => e.Date >= from && e.Date <= to)
                .ToListAsync();

            var ordered = entries
                .OrderBy(e => e.Date).ThenBy(e => e.MealType).ThenBy(e => e.SortOrder).ThenBy(e => e.Id);
            return _mapper.Map<List<MealPlanEntryDto>>(ordered);
        }

        [HttpPost]
        public async Task<ActionResult<MealPlanEntryDto>> Create(MealPlanEntryCreateDto dto)
        {
            var recipe = await _context.Recipes.FindAsync(dto.RecipeId);
            if (recipe == null) return BadRequest(new { message = "Unknown recipe." });

            var date = dto.Date!.Value;
            var mealType = dto.MealType!.Value;
            var mismatch = await MealTypeMismatchAsync(recipe, mealType);
            if (mismatch != null) return BadRequest(new { message = mismatch });

            var sortOrder = dto.SortOrder ?? await NextSortOrderAsync(date, mealType);
            var entry = new MealPlanEntry
            {
                Date = date,
                MealType = mealType,
                RecipeId = recipe.Id,
                Servings = dto.Servings ?? Math.Max(recipe.Yield ?? 1, 1),
                SortOrder = sortOrder
            };
            _context.MealPlanEntries.Add(entry);
            await _context.SaveChangesAsync();

            entry.Recipe = recipe;
            return CreatedAtAction(nameof(GetRange), new { from = date, to = date }, _mapper.Map<MealPlanEntryDto>(entry));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<MealPlanEntryDto>> Update(int id, MealPlanEntryUpdateDto dto)
        {
            var entry = await _context.MealPlanEntries.FindAsync(id);
            if (entry == null) return NotFound();

            var recipe = await _context.Recipes.FindAsync(dto.RecipeId);
            if (recipe == null) return BadRequest(new { message = "Unknown recipe." });

            // The cook log row belongs to the recipe that was made.
            if (entry.CookLogId != null && dto.RecipeId != entry.RecipeId)
                return BadRequest(new { message = "Undo 'made' before changing the recipe of this entry." });

            var mealType = dto.MealType!.Value;
            var mismatch = await MealTypeMismatchAsync(recipe, mealType);
            if (mismatch != null) return BadRequest(new { message = mismatch });

            entry.Date = dto.Date!.Value;
            entry.MealType = mealType;
            entry.RecipeId = recipe.Id;
            entry.Servings = dto.Servings;
            entry.SortOrder = dto.SortOrder;
            await _context.SaveChangesAsync();

            entry.Recipe = recipe;
            return _mapper.Map<MealPlanEntryDto>(entry);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entry = await _context.MealPlanEntries.FindAsync(id);
            if (entry == null) return NotFound();
            _context.MealPlanEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Creates a cook log row for the entry (date = planned date) and links it.</summary>
        [HttpPost("{id}/made")]
        public async Task<ActionResult<MealPlanEntryDto>> MarkMade(int id)
        {
            var entry = await _context.MealPlanEntries.Include(e => e.Recipe).FirstOrDefaultAsync(e => e.Id == id);
            if (entry == null) return NotFound();
            if (entry.CookLogId != null) return Conflict(new { message = "This entry is already marked as made." });

            var future = CookLogController.FutureError(entry.Date);
            if (future != null) return BadRequest(new { message = future });

            // One SaveChanges inserts the log and links it atomically.
            entry.CookLog = new CookLog { RecipeId = entry.RecipeId, CookedOn = entry.Date, Servings = entry.Servings };
            await _context.SaveChangesAsync();
            return _mapper.Map<MealPlanEntryDto>(entry);
        }

        /// <summary>Removes the link and the cook log row.</summary>
        [HttpDelete("{id}/made")]
        public async Task<ActionResult<MealPlanEntryDto>> UndoMade(int id)
        {
            var entry = await _context.MealPlanEntries.Include(e => e.Recipe).Include(e => e.CookLog).FirstOrDefaultAsync(e => e.Id == id);
            if (entry == null) return NotFound();
            if (entry.CookLog == null) return Conflict(new { message = "This entry is not marked as made." });

            _context.CookLogs.Remove(entry.CookLog);
            entry.CookLog = null;
            entry.CookLogId = null;
            await _context.SaveChangesAsync();
            return _mapper.Map<MealPlanEntryDto>(entry);
        }
        private async Task<int> NextSortOrderAsync(DateOnly date, MealType mealType)
        {
            var max = await _context.MealPlanEntries
                .Where(e => e.Date == date && e.MealType == mealType)
                .MaxAsync(e => (int?)e.SortOrder);
            return max + 1 ?? 0;
        }

        // Cocktails belong to the Cocktail row only, and nothing else may use it.
        private async Task<string?> MealTypeMismatchAsync(Recipe recipe, MealType mealType)
        {
            var isCocktail = await _context.RecipeCategories
                .AnyAsync(rc => rc.RecipeId == recipe.Id && rc.CategoryId == CocktailCategoryId);

            if (isCocktail && mealType != MealType.Cocktail)
                return $"'{recipe.Name}' is a cocktail and can only be planned as {nameof(MealType.Cocktail)}.";
            if (!isCocktail && mealType == MealType.Cocktail)
                return $"'{recipe.Name}' is not a cocktail and cannot be planned as {nameof(MealType.Cocktail)}.";
            return null;
        }
    }
}
