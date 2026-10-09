public class MealPlanEntryDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string MealType { get; set; } = string.Empty;
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public int Servings { get; set; }
    public int SortOrder { get; set; }
    public int? CookLogId { get; set; }
}
