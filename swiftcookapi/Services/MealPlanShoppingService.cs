using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Services
{
    public record ShortageLine(
        int IngredientId,
        string IngredientName,
        int UnitId,
        string UnitName,
        decimal? Amount,
        bool UnitMismatch);

    /// <summary>
    /// Works out what to buy for the planned meals (Ticket 24). Needed amounts are scaled by
    /// planned servings / recipe yield and summed per ingredient within comparable units, then
    /// reduced by the Cupboard and by what is already on the shopping list. Staples, past dates
    /// and recipe lines without an amount are handled as described on <see cref="CalculateAsync"/>.
    /// </summary>
    public class MealPlanShoppingService
    {
        public const string SourceLabel = "Meal plan";
        public const string MismatchSourceLabel = "Meal plan - check cupboard (unit mismatch)";
        public const string CountUnitName = "sgl";

        private readonly SwiftCookDbContext _context;
        private readonly UnitConverter _converter;

        public MealPlanShoppingService(SwiftCookDbContext context, UnitConverter converter)
        {
            _context = context;
            _converter = converter;
        }

        private sealed class Bucket
        {
            public Unit Unit = null!;
            public decimal Amount;
        }

        private sealed class Need
        {
            public Ingredient Ingredient = null!;
            public List<Bucket> Buckets = new();
            public Unit? NoAmountUnit;
            public bool NoAmount;
        }

        /// <summary>
        /// Shortages for plan entries from max(<paramref name="from"/>, <paramref name="today"/>) to
        /// <paramref name="to"/>. A recipe line with an amount but no unit counts as 'sgl'. A line with
        /// no amount ("to taste") is listed without an amount only when the ingredient is in neither the
        /// Cupboard nor the shopping list. Stock in a unit that cannot be compared is ignored for the sum
        /// and flagged as a unit mismatch (over-buy rather than miss).
        /// </summary>
        public async Task<List<ShortageLine>> CalculateAsync(DateOnly from, DateOnly to, DateOnly today)
        {
            var start = from > today ? from : today;
            if (to < start) return new List<ShortageLine>();

            var entries = await _context.MealPlanEntries
                .Include(e => e.Recipe)
                .Where(e => e.Date >= start && e.Date <= to)
                .ToListAsync();
            if (entries.Count == 0) return new List<ShortageLine>();

            var recipeIds = entries.Select(e => e.RecipeId).Distinct().ToList();
            var lines = await _context.RecipeIngredients
                .Include(r => r.Ingredient)
                .Include(r => r.Unit)
                .Where(r => recipeIds.Contains(r.RecipeId) && !r.Ingredient.IsStaple)
                .ToListAsync();
            var linesByRecipe = lines.ToLookup(l => l.RecipeId);

            var countUnit = await _context.Units.FirstOrDefaultAsync(u => u.Name == CountUnitName);
            var needs = new Dictionary<int, Need>();

            foreach (var entry in entries)
            {
                var recipeYield = entry.Recipe.Yield is > 0 ? entry.Recipe.Yield.Value : 1;
                var scale = entry.Servings / (decimal)recipeYield;

                foreach (var line in linesByRecipe[entry.RecipeId])
                {
                    if (!needs.TryGetValue(line.IngredientId, out var need))
                        needs[line.IngredientId] = need = new Need { Ingredient = line.Ingredient };

                    var unit = line.Unit ?? countUnit;
                    if (line.Amount == null || unit == null)
                    {
                        need.NoAmount = true;
                        need.NoAmountUnit ??= unit;
                        continue;
                    }

                    var amount = line.Amount.Value * scale;
                    var bucket = need.Buckets.FirstOrDefault(b => _converter.CanConvert(unit, b.Unit));
                    if (bucket != null && _converter.TryAdd(bucket.Amount, bucket.Unit, amount, unit, out var sum))
                        bucket.Amount = sum;
                    else
                        need.Buckets.Add(new Bucket { Unit = unit, Amount = amount });
                }
            }

            var ingredientIds = needs.Keys.ToList();
            var cupboard = (await _context.Cupboards.Include(c => c.Unit)
                .Where(c => ingredientIds.Contains(c.IngredientId)).ToListAsync())
                .ToLookup(c => c.IngredientId);
            var listed = (await _context.ShoppingLists.Include(s => s.Unit)
                .Where(s => ingredientIds.Contains(s.IngredientId)).ToListAsync())
                .ToLookup(s => s.IngredientId);

            var result = new List<ShortageLine>();
            foreach (var need in needs.Values.OrderBy(n => n.Ingredient.Name))
            {
                var id = need.Ingredient.Id;
                var cupboardRows = cupboard[id].ToList();
                var listRows = listed[id].ToList();

                foreach (var bucket in need.Buckets)
                {
                    var fromCupboard = SumComparable(bucket.Unit, cupboardRows.Select(c => (c.Amount, c.Unit)), out var anyCupboard);
                    var fromList = SumComparable(bucket.Unit, listRows.Select(s => (s.Amount, s.Unit)), out _);

                    var shortage = Math.Round(bucket.Amount - fromCupboard - fromList, 4);
                    if (shortage <= 0) continue;

                    var cupboardHasAmounts = cupboardRows.Any(c => c.Amount.HasValue);
                    result.Add(new ShortageLine(id, need.Ingredient.Name, bucket.Unit.Id, bucket.Unit.Name,
                        shortage, UnitMismatch: cupboardHasAmounts && !anyCupboard));
                }

                if (need.NoAmount && need.Buckets.Count == 0 && need.NoAmountUnit != null
                    && cupboardRows.Count == 0 && listRows.Count == 0)
                {
                    result.Add(new ShortageLine(id, need.Ingredient.Name, need.NoAmountUnit.Id,
                        need.NoAmountUnit.Name, null, false));
                }
            }

            return result;
        }

        /// <summary>Adds the shortages to the shopping list and returns them. Never changes existing rows.</summary>
        public async Task<List<ShortageLine>> AddToShoppingListAsync(DateOnly from, DateOnly to, DateOnly today)
        {
            var lines = await CalculateAsync(from, to, today);
            foreach (var line in lines)
            {
                _context.ShoppingLists.Add(new ShoppingList
                {
                    IngredientId = line.IngredientId,
                    UnitId = line.UnitId,
                    Amount = line.Amount,
                    Source = line.UnitMismatch ? MismatchSourceLabel : SourceLabel,
                });
            }
            await _context.SaveChangesAsync();
            return lines;
        }

        // Sums the rows whose unit is comparable with the target; 'any' reports whether at least one was.
        private decimal SumComparable(Unit target, IEnumerable<(decimal? Amount, Unit Unit)> rows, out bool any)
        {
            decimal total = 0;
            any = false;
            foreach (var (amount, unit) in rows)
            {
                if (!amount.HasValue || !_converter.TryConvert(amount.Value, unit, target, out var converted)) continue;
                total += converted;
                any = true;
            }
            return total;
        }
    }
}
