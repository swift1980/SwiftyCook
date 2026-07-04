namespace SwiftCookDb.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Image { get; set; }
        public int? PrepTime { get; set; }
        public int? CookTime { get; set; }
        public int? Yield { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public DateTime? DateUpdated { get; set; }
        public string NameNormalized { get; set; } = string.Empty;
        public string? DescriptionNormalized { get; set; }

        public ICollection<RecipeCategory> RecipeCategories { get; set; } = new List<RecipeCategory>();
        public ICollection<RecipeTag> RecipeTags { get; set; } = new List<RecipeTag>();
        public ICollection<RecipeTool> RecipeTools { get; set; } = new List<RecipeTool>();
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
        public ICollection<RecipeInstruction> Instructions { get; set; } = new List<RecipeInstruction>();
        public Nutrition? Nutrition { get; set; }
        public ICollection<Cooked> CookedHistory { get; set; } = new List<Cooked>();
        public ICollection<Note> Notes { get; set; } = new List<Note>();
    }
}