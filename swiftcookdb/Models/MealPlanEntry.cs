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

        // Set once the planned meal has been marked as made (Ticket 20).
        public int? CookLogId { get; set; }
        public CookLog? CookLog { get; set; }
    }
}
