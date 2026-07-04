using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CocktailController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;

        public CocktailController(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RecipeReadDto>>> GetAll()
        {
            var recipes = await _context.Recipes
                .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
                .Include(r => r.RecipeTags).ThenInclude(rt => rt.Tag)
                .Include(r => r.RecipeTools).ThenInclude(rt => rt.Tool)
                .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
                .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Unit)
                .Include(r => r.Instructions)
                .Include(r => r.Nutrition)
                .Where(r => r.RecipeCategories.Any(rc => rc.CategoryId == 1))
                .ToListAsync();

            return _mapper.Map<List<RecipeReadDto>>(recipes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RecipeReadDto>> GetById(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
                .Include(r => r.RecipeTags).ThenInclude(rt => rt.Tag)
                .Include(r => r.RecipeTools).ThenInclude(rt => rt.Tool)
                .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
                .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Unit)
                .Include(r => r.Instructions)
                .Include(r => r.Nutrition)
                .FirstOrDefaultAsync(r => r.Id == id);

            return recipe == null ? NotFound() : _mapper.Map<RecipeReadDto>(recipe);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Recipe>>> Search(string q)
        {
            return await _context.Recipes
                .Where(r => r.Name.Contains(q) &&
                r.RecipeCategories.Any(rc => rc.CategoryId == 1))
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Recipe>> Create(Recipe recipe)
        {
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = recipe.Id }, recipe);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Recipe recipe)
        {
            if (id != recipe.Id) return BadRequest();
            _context.Entry(recipe).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null) return NotFound();
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
