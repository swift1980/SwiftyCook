using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using swiftcookapi.Services;

namespace swiftcookapi.Controllers
{
    [Route("api/recipe/import")]
    [ApiController]
    public class RecipeImportController : ControllerBase
    {
        // Slightly above the documented 1 MB file limit to leave room for the decisions envelope.
        public const int MaxRequestBytes = 1_300_000;

        private readonly RecipeImportService _service;

        public RecipeImportController(RecipeImportService service)
        {
            _service = service;
        }

        [HttpPost]
        [RequestSizeLimit(MaxRequestBytes)]
        public async Task<ActionResult<RecipeImportResponse>> Import(
            [FromBody] RecipeImportRequest request, [FromQuery] bool dryRun = true)
        {
            if (request == null) return BadRequest(new { message = "Request body is required." });

            var elements = new List<JsonElement>();
            switch (request.Recipes.ValueKind)
            {
                case JsonValueKind.Array:
                    elements.AddRange(request.Recipes.EnumerateArray());
                    break;
                case JsonValueKind.Object:
                    elements.Add(request.Recipes);
                    break;
                default:
                    return BadRequest(new { message = "'recipes' must be a recipe object or an array of recipe objects." });
            }

            if (elements.Count == 0) return BadRequest(new { message = "No recipes found in the file." });
            if (elements.Count > RecipeImportValidator.MaxRecipes)
                return BadRequest(new { message = $"A file may contain at most {RecipeImportValidator.MaxRecipes} recipes." });

            return await _service.ImportAsync(elements, request.Decisions, dryRun);
        }
    }
}
