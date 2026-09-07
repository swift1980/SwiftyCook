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
            if (string.IsNullOrWhiteSpace(q)) return BadRequest("Query parameter 'q' is required.");

            return await _context.Recipes
                .Where(r => r.Name.Contains(q) &&
                r.RecipeCategories.Any(rc => rc.CategoryId == 1))
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<RecipeReadDto>> Create([FromBody] RecipeCreateDto dto)
        {
            if (dto == null) return BadRequest("Invalid recipe data");

            // Map basic Recipe
            var recipe = _mapper.Map<Recipe>(dto);

            // Normalize
            recipe.NameNormalized = recipe.Name.ToLowerInvariant();
            recipe.DescriptionNormalized = recipe.Description?.ToLowerInvariant();

            // Add to context first (so we get recipe.Id)
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            // Ingredients
            if (dto.Ingredients?.Any() == true)
            {
                var recipeIngredients = dto.Ingredients.Select((riDto, index) =>
                {
                    var ri = _mapper.Map<RecipeIngredient>(riDto);
                    ri.RecipeId = recipe.Id;
                    ri.Position = riDto.Position ?? (index + 1);
                    return ri;
                }).ToList();

                _context.RecipeIngredients.AddRange(recipeIngredients);
            }

            // Instructions
            if (dto.Instructions?.Any() == true)
            {
                var instructions = dto.Instructions.Select((stepDto, index) =>
                {
                    var step = _mapper.Map<RecipeInstruction>(stepDto);
                    step.RecipeId = recipe.Id;
                    step.Position = stepDto.Position > 0 ? stepDto.Position : (index + 1);
                    return step;
                }).ToList();

                _context.RecipeInstructions.AddRange(instructions);
            }

            // Categories - always tag as a cocktail (CategoryId 1), plus any extra categories supplied
            var categoryIds = (dto.CategoryIds ?? new List<int>()).Append(1).Distinct();
            var recipeCategories = categoryIds.Select(catId => new RecipeCategory
            {
                RecipeId = recipe.Id,
                CategoryId = catId
            });
            _context.RecipeCategories.AddRange(recipeCategories);

            // Tags
            if (dto.TagIds?.Any() == true)
            {
                var recipeTags = dto.TagIds.Select(tagId => new RecipeTag
                {
                    RecipeId = recipe.Id,
                    TagId = tagId
                });
                _context.RecipeTags.AddRange(recipeTags);
            }

            // Tools
            if (dto.ToolIds?.Any() == true)
            {
                var recipeTools = dto.ToolIds.Select(toolId => new RecipeTool
                {
                    RecipeId = recipe.Id,
                    ToolId = toolId
                });
                _context.RecipeTools.AddRange(recipeTools);
            }

            await _context.SaveChangesAsync();

            // Re-fetch with relationships
            var recipeWithIncludes = await _context.Recipes
                .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
                .Include(r => r.RecipeTags).ThenInclude(rt => rt.Tag)
                .Include(r => r.RecipeTools).ThenInclude(rt => rt.Tool)
                .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
                .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Unit)
                .Include(r => r.Instructions)
                .FirstOrDefaultAsync(r => r.Id == recipe.Id);

            var readDto = _mapper.Map<RecipeReadDto>(recipeWithIncludes);
            return CreatedAtAction(nameof(GetById), new { id = recipe.Id }, readDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, RecipeCreateDto dto)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null) return NotFound();

            _mapper.Map(dto, recipe);
            recipe.NameNormalized = recipe.Name.ToLowerInvariant();
            recipe.DescriptionNormalized = recipe.Description?.ToLowerInvariant();
            recipe.DateUpdated = DateTime.Now;

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
