using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;
using swiftcookapi.Services;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipeController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;
        private readonly IRecipeIngredientSearchService _ingredientSearchService;

        public RecipeController(
            SwiftCookDbContext context,
            IMapper mapper,
            IRecipeIngredientSearchService ingredientSearchService)
        {
            _context = context;
            _mapper = mapper;
            _ingredientSearchService = ingredientSearchService;
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
                .Where(r => r.RecipeCategories.Any(rc => rc.CategoryId != 1))
                .ToListAsync();

            return _mapper.Map<List<RecipeReadDto>>(recipes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RecipeReadDto>> GetRecipeById(int id)
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
                !r.RecipeCategories.Any(rc => rc.CategoryId == 1))
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<RecipeReadDto>> CreateRecipe([FromBody] RecipeCreateDto dto)
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

            // Categories
            if (dto.CategoryIds?.Any() == true)
            {
                var recipeCategories = dto.CategoryIds.Select(catId => new RecipeCategory
                {
                    RecipeId = recipe.Id,
                    CategoryId = catId
                });
                _context.RecipeCategories.AddRange(recipeCategories);
            }

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
            return CreatedAtAction(nameof(GetRecipeById), new { id = recipe.Id }, readDto);
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

        [HttpPost("search/ingredients")]
        [ProducesResponseType(typeof(PagedResultDto<RecipeSearchResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResultDto<RecipeSearchResultDto>>> SearchByIngredients(
            [FromBody] RecipeIngredientSearchRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            var outcome = await _ingredientSearchService.SearchAsync(request, cancellationToken);

            if (!outcome.IsValid)
                return BadRequest(outcome.Error);

            return Ok(outcome.Result);
        }
    }

}
