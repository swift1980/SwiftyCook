public class ShoppingListDto
{
    public int Id { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? UnitName { get; set; }
    public string? Source { get; set; }
}
