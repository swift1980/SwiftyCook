using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;

namespace swiftcookapi.Controllers
{
    // Ticket 9: read-only for now. Full CRUD (create/rename/reparent categories) is
    // deferred until there's real demand beyond the seeded 7-category structure
    // (6 families + Miscellaneous) — see BACKLOG.md Ticket 9.
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientCategoryController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;

        public IngredientCategoryController(SwiftCookDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IngredientCategoryDto>>> GetAll() =>
            await _context.IngredientCategories
                .Select(c => new IngredientCategoryDto { Id = c.Id, Name = c.Name, ParentCategoryId = c.ParentCategoryId })
                .ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<IngredientCategoryDto>> GetById(int id)
        {
            var category = await _context.IngredientCategories
                .Select(c => new IngredientCategoryDto { Id = c.Id, Name = c.Name, ParentCategoryId = c.ParentCategoryId })
                .FirstOrDefaultAsync(c => c.Id == id);

            return category == null ? NotFound() : category;
        }

        // Types belonging directly to this category (used by AdvancedSearch.vue,
        // Ticket 8, to populate each category box's ingredient options).
        [HttpGet("{id}/types")]
        public async Task<ActionResult<IEnumerable<IngredientTypeDto>>> GetTypes(int id)
        {
            var exists = await _context.IngredientCategories.AnyAsync(c => c.Id == id);
            if (!exists) return NotFound();

            return await _context.IngredientTypes
                .Where(t => t.CategoryId == id)
                .Select(t => new IngredientTypeDto { Id = t.Id, Name = t.Name, CategoryId = t.CategoryId })
                .ToListAsync();
        }
    }
}
