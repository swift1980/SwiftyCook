using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;

        public IngredientController(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IngredientDto>>> GetAll()
        {
            var ingredients = await _context.Ingredients
                .Include(i => i.Type)
                .ToListAsync();
            return _mapper.Map<List<IngredientDto>>(ingredients);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IngredientDto>> GetById(int id)
        {
            var ingredient = await _context.Ingredients
                .Include(i => i.Type)
                .FirstOrDefaultAsync(i => i.Id == id);
            return ingredient == null ? NotFound() : _mapper.Map<IngredientDto>(ingredient);
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
                    TypeName = type.Name,
                    IsStaple = i.IsStaple
                })
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<IngredientDto>> Create(IngredientCreateDto dto)
        {
            dto.Name = dto.Name?.Trim() ?? string.Empty;
            if (dto.Name.Length == 0) return BadRequest(new { message = "Name is required." });
            if (await NameExistsAsync(dto.Name, null))
                return Conflict(new { message = $"An ingredient named '{dto.Name}' already exists." });

            var ingredient = _mapper.Map<Ingredient>(dto);
            _context.Ingredients.Add(ingredient);
            await _context.SaveChangesAsync();

            // Re-fetch with the Type navigation so TypeName is populated in the response
            var created = await _context.Ingredients
                .Include(i => i.Type)
                .FirstOrDefaultAsync(i => i.Id == ingredient.Id);

            var result = _mapper.Map<IngredientDto>(created);
            return CreatedAtAction(nameof(GetById), new { id = ingredient.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, IngredientCreateDto dto)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null) return NotFound();

            dto.Name = dto.Name?.Trim() ?? string.Empty;
            if (dto.Name.Length == 0) return BadRequest(new { message = "Name is required." });
            if (await NameExistsAsync(dto.Name, id))
                return Conflict(new { message = $"An ingredient named '{dto.Name}' already exists." });

            _mapper.Map(dto, ingredient);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null) return NotFound();

            // The DB cascades these deletes, so block instead of silently breaking recipes/lists.
            var recipes = await _context.RecipeIngredients
                .Where(ri => ri.IngredientId == id)
                .Select(ri => ri.RecipeId).Distinct().CountAsync();
            var inCupboard = await _context.Cupboards.AnyAsync(c => c.IngredientId == id);
            var onShoppingList = await _context.ShoppingLists.AnyAsync(s => s.IngredientId == id);

            var usage = new List<string>();
            if (recipes > 0) usage.Add($"used in {recipes} recipe{(recipes == 1 ? "" : "s")}");
            if (inCupboard) usage.Add("in the cupboard");
            if (onShoppingList) usage.Add("on the shopping list");
            if (usage.Count > 0)
                return Conflict(new { message = $"Cannot delete '{ingredient.Name}': {string.Join(", ", usage)}." });

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private Task<bool> NameExistsAsync(string name, int? excludeId)
        {
            var lower = name.ToLower();
            return _context.Ingredients.AnyAsync(i => i.Name.ToLower() == lower && i.Id != excludeId);
        }
    }

}
