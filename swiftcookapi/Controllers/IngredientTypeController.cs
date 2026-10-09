using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientTypeController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;
        public IngredientTypeController(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IngredientTypeDto>>> GetAll() =>
            await _context.IngredientTypes
                .Select(t => new IngredientTypeDto { Id = t.Id, Name = t.Name, CategoryId = t.CategoryId })
                .ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<IngredientTypeDto>> GetById(int id)
        {
            var type = await _context.IngredientTypes
                .Select(t => new IngredientTypeDto { Id = t.Id, Name = t.Name, CategoryId = t.CategoryId })
                .FirstOrDefaultAsync(t => t.Id == id);

            return type == null ? NotFound() : type;
        }

        [HttpGet("{id}/ingredients")]
        public async Task<ActionResult<IEnumerable<IngredientDto>>> GetIngredients(int id)
        {
            var type = await _context.IngredientTypes.FirstOrDefaultAsync(t => t.Id == id);
            if (type == null) return NotFound();

            return await _context.Ingredients
                .Where(i => i.TypeId == id)
                .Select(i => new IngredientDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    PluralName = i.PluralName,
                    TypeId = i.TypeId,
                    TypeName = type.Name
                })
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<IngredientTypeDto>> Create(IngredientTypeCreateDto dto)
        {
            var type = _mapper.Map<IngredientType>(dto);

            // Default to the "Miscellaneous" category when none is specified (Ticket 9:
            // IngredientType.CategoryId is NOT NULL, so every type must resolve to one).
            type.CategoryId = dto.CategoryId ?? await _context.IngredientCategories
                .Where(c => c.Name == "Miscellaneous")
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            _context.IngredientTypes.Add(type);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = type.Id },
                new IngredientTypeDto { Id = type.Id, Name = type.Name, CategoryId = type.CategoryId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, IngredientTypeCreateDto dto)
        {
            var type = await _context.IngredientTypes.FindAsync(id);
            if (type == null) return NotFound();
            type.Name = dto.Name;
            if (dto.CategoryId.HasValue) type.CategoryId = dto.CategoryId.Value;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var type = await _context.IngredientTypes.FindAsync(id);
            if (type == null) return NotFound();

            var ingredientCount = await _context.Ingredients.CountAsync(i => i.TypeId == id);
            var isFallback = await _context.IngredientCategories.AnyAsync(c => c.FallbackTypeId == id);
            if (ingredientCount > 0 || isFallback)
            {
                var reasons = new List<string>();
                if (ingredientCount > 0) reasons.Add($"{ingredientCount} ingredient{(ingredientCount == 1 ? "" : "s")} use it");
                if (isFallback) reasons.Add("it is a category's fallback type");
                return Conflict(new { message = $"Cannot delete type '{type.Name}': {string.Join(" and ", reasons)}." });
            }

            _context.IngredientTypes.Remove(type);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}