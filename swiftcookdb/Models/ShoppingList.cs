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

        public float? Amount { get; set; }
    }
}