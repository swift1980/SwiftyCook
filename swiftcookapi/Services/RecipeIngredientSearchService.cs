using Microsoft.EntityFrameworkCore;
using SwiftCookDb;

namespace swiftcookapi.Services
{
    public interface IRecipeIngredientSearchService
    {
        Task<RecipeIngredientSearchOutcome> SearchAsync(
            RecipeIngredientSearchRequestDto request,
            CancellationToken cancellationToken = default);
    }

    public class RecipeIngredientSearchService : IRecipeIngredientSearchService
    {
        private const int MaxPageSize = 50;
        private readonly SwiftCookDbContext _context;

        public RecipeIngredientSearchService(SwiftCookDbContext context)
        {
            _context = context;
        }

        public async Task<RecipeIngredientSearchOutcome> SearchAsync(
            RecipeIngredientSearchRequestDto request,
            CancellationToken cancellationToken = default)
        {
            // --- Normalize quietly: de-duplicate; mandatory wins over optional overlap ---
            var mandatory = request.MandatoryIngredientIds.Distinct().ToList();
            var optional = request.OptionalIngredientIds
                .Distinct()
                .Where(id => !mandatory.Contains(id))
                .ToList();

            // --- Reject loudly: completely empty search ---
            if (mandatory.Count == 0 && optional.Count == 0)
            {
                return RecipeIngredientSearchOutcome.Invalid(
                    "At least one mandatory or optional ingredient must be provided.");
            }

            // --- Empty optional pool: threshold is treated as 0 ---
            var threshold = optional.Count == 0 ? 0 : request.OptionalThreshold;

            // --- Reject loudly: threshold out of range for a non-empty optional pool ---
            if (optional.Count > 0 && (request.OptionalThreshold < 0 || request.OptionalThreshold > optional.Count))
            {
                return RecipeIngredientSearchOutcome.Invalid(
                    $"OptionalThreshold must be between 0 and {optional.Count} (the number of optional ingredients).");
            }

            // --- Reject loudly: unknown ingredient IDs (curated master list) ---
            var allSearchedIds = mandatory.Concat(optional).ToList();
            var existingIds = await _context.Ingredients
                .Where(i => allSearchedIds.Contains(i.Id))
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            var unknownIds = allSearchedIds.Except(existingIds).ToList();
            if (unknownIds.Count > 0)
            {
                return RecipeIngredientSearchOutcome.Invalid(
                    $"Unknown ingredient Id(s): {string.Join(", ", unknownIds.OrderBy(id => id))}.");
            }

            // --- Pagination bounds ---
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 1
                : request.PageSize > MaxPageSize ? MaxPageSize
                : request.PageSize;

            // --- Distinct (RecipeId, IngredientId): collapse UnitId-driven duplicates ---
            var distinctRecipeIngredients = _context.RecipeIngredients
                .Select(ri => new { ri.RecipeId, ri.IngredientId })
                .Distinct();

            // --- Aggregate per recipe (translates to SUM(CASE WHEN ...) counts) ---
            var mandatoryCount = mandatory.Count;

            var aggregated = distinctRecipeIngredients
                .GroupBy(x => x.RecipeId)
                .Select(g => new
                {
                    RecipeId = g.Key,
                    TotalIngredientCount = g.Count(),
                    MandatoryMatchCount = g.Count(x => mandatory.Contains(x.IngredientId)),
                    OptionalMatchCount = g.Count(x => optional.Contains(x.IngredientId))
                })
                .Where(x => x.MandatoryMatchCount == mandatoryCount
                         && x.OptionalMatchCount >= threshold);

            // --- Join to Recipe for Name/Image and compute extras; then sort ---
            var projected = from a in aggregated
                            join r in _context.Recipes on a.RecipeId equals r.Id
                            select new
                            {
                                a.RecipeId,
                                r.Name,
                                r.Image,
                                a.TotalIngredientCount,
                                a.OptionalMatchCount,
                                ExtraIngredientCount = a.TotalIngredientCount - mandatoryCount - a.OptionalMatchCount
                            };

            var totalCount = await projected.CountAsync(cancellationToken);

            var pageRows = await projected
                .OrderByDescending(x => x.OptionalMatchCount)
                .ThenBy(x => x.ExtraIngredientCount)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.RecipeId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // --- Second query: matched optional IDs for just this page's recipes ---
            var pageRecipeIds = pageRows.Select(x => x.RecipeId).ToList();
            var matchedOptionalByRecipe = optional.Count == 0 || pageRecipeIds.Count == 0
                ? new Dictionary<int, List<int>>()
                : (await _context.RecipeIngredients
                    .Where(ri => pageRecipeIds.Contains(ri.RecipeId) && optional.Contains(ri.IngredientId))
                    .Select(ri => new { ri.RecipeId, ri.IngredientId })
                    .Distinct()
                    .ToListAsync(cancellationToken))
                    .GroupBy(x => x.RecipeId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.IngredientId).ToList());

            var items = pageRows.Select(x => new RecipeSearchResultDto
            {
                RecipeId = x.RecipeId,
                Name = x.Name,
                Image = x.Image,
                OptionalMatchCount = x.OptionalMatchCount,
                OptionalTotal = optional.Count,
                MatchedOptionalIds = matchedOptionalByRecipe.TryGetValue(x.RecipeId, out var ids)
                    ? ids
                    : new List<int>(),
                ExtraIngredientCount = x.ExtraIngredientCount,
                TotalIngredientCount = x.TotalIngredientCount
            }).ToList();

            var result = new PagedResultDto<RecipeSearchResultDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return RecipeIngredientSearchOutcome.Success(result);
        }
    }
}