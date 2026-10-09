using SwiftCookDb.Models;

namespace swiftcookapi.Services
{
    /// <summary>
    /// Unit conversion rules (Ticket 21). Same-dimension conversion only: Mass units convert
    /// via grams and Volume units via millilitres. Count/Other units (and anything across
    /// dimensions, e.g. grams vs cups) are never converted - they are comparable only when
    /// they are the very same unit. Callers should represent a recipe line with no unit as
    /// the 'sgl' (Count) unit.
    /// </summary>
    public class UnitConverter
    {
        /// <summary>True when an amount in <paramref name="from"/> can be expressed in <paramref name="to"/>.</summary>
        public bool CanConvert(Unit from, Unit to)
        {
            if (from.Id == to.Id) return true;
            return from.Dimension == to.Dimension
                && (from.Dimension == UnitDimension.Mass || from.Dimension == UnitDimension.Volume)
                && from.ToBaseFactor is > 0
                && to.ToBaseFactor is > 0;
        }

        public bool TryConvert(decimal amount, Unit from, Unit to, out decimal result)
        {
            if (from.Id == to.Id)
            {
                result = amount;
                return true;
            }
            if (!CanConvert(from, to))
            {
                result = 0;
                return false;
            }
            result = amount * from.ToBaseFactor!.Value / to.ToBaseFactor!.Value;
            return true;
        }

        /// <summary>Sums two amounts, expressed in the first amount's unit. False when the units are incomparable.</summary>
        public bool TryAdd(decimal amountA, Unit unitA, decimal amountB, Unit unitB, out decimal sum)
        {
            if (!TryConvert(amountB, unitB, unitA, out var converted))
            {
                sum = 0;
                return false;
            }
            sum = amountA + converted;
            return true;
        }

        /// <summary>
        /// Compares two amounts (B is converted into A's unit). Null when the units are incomparable;
        /// otherwise negative, zero or positive like <see cref="decimal.CompareTo(decimal)"/>.
        /// </summary>
        public int? Compare(decimal amountA, Unit unitA, decimal amountB, Unit unitB)
        {
            if (!TryConvert(amountB, unitB, unitA, out var converted)) return null;
            return amountA.CompareTo(converted);
        }

        /// <summary>
        /// Picks a readable unit from <paramref name="candidates"/> (same dimension as
        /// <paramref name="unit"/>): the largest unit in which the amount is at least 1, otherwise
        /// the smallest. Amounts that can't be converted are returned unchanged. The caller
        /// chooses the candidates (e.g. g/kg and ml/l) so imperial units aren't picked unasked.
        /// </summary>
        public (decimal Amount, Unit Unit) ToDisplay(decimal amount, Unit unit, IEnumerable<Unit> candidates)
        {
            var usable = candidates
                .Where(c => CanConvert(unit, c) && c.ToBaseFactor is > 0)
                .OrderByDescending(c => c.ToBaseFactor)
                .ToList();
            if (usable.Count == 0 || unit.ToBaseFactor is not > 0) return (amount, unit);

            foreach (var candidate in usable)
            {
                TryConvert(amount, unit, candidate, out var converted);
                if (Math.Abs(converted) >= 1) return (Round(converted), candidate);
            }

            var smallest = usable[^1];
            TryConvert(amount, unit, smallest, out var smallestAmount);
            return (Round(smallestAmount), smallest);
        }

        private static decimal Round(decimal value) => Math.Round(value, 4);
    }
}
