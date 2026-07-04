namespace SwiftCookDb.Models
{
    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PluralName { get; set; } = string.Empty;
        public int? TypeId {  get; set; }
        public IngredientType? Type { get; set; }

        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
        public ICollection<ShoppingList> ShoppingLists { get; set; } = new List<ShoppingList>();
        public Cupboard? Cupboard { get; set; }
    }
}