public class RecipeReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public int? PrepTime { get; set; }
    public int? CookTime { get; set; }
    public int? Yield { get; set; }

    public List<int> CategoryIds { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> Tools { get; set; } = new();
    public List<RecipeIngredientReadDto> Ingredients { get; set; } = new();
    public List<RecipeInstructionReadDto> Instructions { get; set; } = new();
    public NutritionDto? Nutrition { get; set; }
}
