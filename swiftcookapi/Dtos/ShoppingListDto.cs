public class ShoppingListDto
{
    public int Id { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public float? Amount { get; set; }
    public string? UnitName { get; set; }
}
