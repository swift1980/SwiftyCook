public class RecipeCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public int PrepTime { get; set; }
    public int CookTime { get; set; }
    public int Yield { get; set; }

    public List<int> CategoryIds { get; set; } = new();
    public List<int> TagIds { get; set; } = new();
    public List<int> ToolIds { get; set; } = new();

    public List<RecipeIngredientCreateDto> Ingredients { get; set; } = new();
    public List<RecipeInstructionCreateDto> Instructions { get; set; } = new();
}
