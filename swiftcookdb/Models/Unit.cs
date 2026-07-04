namespace SwiftCookDb.Models
{
    public class Unit
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PluralName { get; set; }
        public string? Description { get; set; }
        public string? Abbreviation { get; set; }

        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
        public ICollection<ShoppingList> ShoppingLists { get; set; } = new List<ShoppingList>();
        public ICollection<Cupboard> Cupboards { get; set; } = new List<Cupboard>();
    }
}