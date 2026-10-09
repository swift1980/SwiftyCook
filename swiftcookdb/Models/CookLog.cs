namespace SwiftCookDb.Models
{
    // One row per time a recipe was made. Planned meals link to it via
    // MealPlanEntry.CookLogId and store no cooked date of their own.
    public class CookLog
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;

        public DateOnly CookedOn { get; set; }
        public int Servings { get; set; }
        public string? Notes { get; set; }
    }
}
