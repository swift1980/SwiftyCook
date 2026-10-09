namespace SwiftCookDb.Models
{
    public class Unit
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PluralName { get; set; }
        public string? Description { get; set; }
        public string? Abbreviation { get; set; }

        public UnitDimension Dimension { get; set; } = UnitDimension.Other;

        // Multiplier to the dimension's base unit (gram for Mass, millilitre for Volume).
        // Null for Count/Other units, which are never converted.
        public decimal? ToBaseFactor { get; set; }

        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
        public ICollection<ShoppingList> ShoppingLists { get; set; } = new List<ShoppingList>();
        public ICollection<Cupboard> Cupboards { get; set; } = new List<Cupboard>();
    }
}