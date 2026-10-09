using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Services
{
    /// <summary>
    /// Adds stock to the Cupboard. Same unit: amounts are summed (unknown + anything stays unknown).
    /// A different unit in the same dimension (e.g. g and kg) is converted and merged into the existing
    /// row, shown in the larger sensible unit. Incomparable units, or a missing amount, get their own row.
    /// Saves after every call so consecutive calls see each other's changes.
    /// </summary>
    public class CupboardStockService
    {
        private readonly SwiftCookDbContext _context;
        private readonly UnitConverter _converter;

        public CupboardStockService(SwiftCookDbContext context, UnitConverter converter)
        {
            _context = context;
            _converter = converter;
        }

        public virtual async Task<Cupboard> AddStockAsync(int ingredientId, Unit unit, decimal? amount)
        {
            var rows = await _context.Cupboards
                .Include(c => c.Unit)
                .Where(c => c.IngredientId == ingredientId)
                .ToListAsync();

            var sameUnit = rows.FirstOrDefault(r => r.UnitId == unit.Id);
            Cupboard result;

            if (sameUnit != null)
            {
                sameUnit.Amount = sameUnit.Amount.HasValue && amount.HasValue
                    ? sameUnit.Amount + amount
                    : null;
                result = sameUnit;
            }
            else
            {
                var convertible = amount.HasValue
                    ? rows.FirstOrDefault(r => r.Amount.HasValue && _converter.CanConvert(unit, r.Unit))
                    : null;

                if (convertible != null
                    && _converter.TryAdd(convertible.Amount!.Value, convertible.Unit, amount!.Value, unit, out var sum))
                {
                    var candidates = new[] { convertible.Unit, unit };
                    var (merged, displayUnit) = _converter.ToDisplay(sum, convertible.Unit, candidates);

                    // The unit is part of the key, so a unit change means replacing the row.
                    _context.Cupboards.Remove(convertible);
                    result = new Cupboard { IngredientId = ingredientId, UnitId = displayUnit.Id, Amount = merged };
                    _context.Cupboards.Add(result);
                }
                else
                {
                    result = new Cupboard { IngredientId = ingredientId, UnitId = unit.Id, Amount = amount };
                    _context.Cupboards.Add(result);
                }
            }

            await _context.SaveChangesAsync();
            return result;
        }
    }
}
