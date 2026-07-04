public class RecipeIngredientReadDto
{
    public int RecipeId { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public float? Amount { get; set; }
    public UnitDto? Unit { get; set; }
    public int? Position { get; set; }
}
