namespace SwiftCookDb.Models
{
    public class MealPlanEntry
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public MealType MealType { get; set; }

        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;

        public int Servings { get; set; }
        public int SortOrder { get; set; }

        // Reserved for the cook log (Ticket 20); the foreign key is added there.
        public int? CookLogId { get; set; }
    }
}
