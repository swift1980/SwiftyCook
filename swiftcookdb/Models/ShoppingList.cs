using SwiftCookDb.Models;

namespace SwiftCookDb.Models
{
    public class ShoppingList
    {
        public int Id { get; set; }

        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; } = null!;

        public int UnitId { get; set; }
        public Unit Unit { get; set; } = null!;

        public decimal? Amount { get; set; }

        /// <summary>Marks generated lines (e.g. 'Meal plan'); null for rows the user added.</summary>
        public string? Source { get; set; }
    }
}