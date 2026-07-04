using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        public CategoryController(SwiftCookDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll() =>
        await _context.Categories
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetActive() =>
        await _context.Categories
            .Where(c => c.RecipeCategories.Any())
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetById(int id)
        {
            var category = await _context.Categories
                .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
                .FirstOrDefaultAsync(c => c.Id == id);

            return category == null ? NotFound() : category;
        }

        [HttpPost]
        public async Task<ActionResult<Category>> Create(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Category category)
        {
            if (id != category.Id) return BadRequest();
            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
