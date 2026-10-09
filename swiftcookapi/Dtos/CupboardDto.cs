public class CupboardDto
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public int UnitId { get; set; }
    public string? UnitName { get; set; }
}
