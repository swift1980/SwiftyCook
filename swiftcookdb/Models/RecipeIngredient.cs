using SwiftCookDb.Models;

namespace SwiftCookDb.Models
{
    public class RecipeIngredient
    {
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;

        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; } = null!;

        public int? UnitId { get; set; }
        public Unit Unit { get; set; } = null!;

        public decimal? Amount { get; set; }
        public int? Position { get; set; }

        // Free-text preparation note, e.g. "finely chopped" or "to taste"
        public string? Note { get; set; }
    }
}