public class CookLogDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public DateOnly CookedOn { get; set; }
    public int Servings { get; set; }
    public string? Notes { get; set; }
}
