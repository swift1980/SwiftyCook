public class RecipeIngredientCreateDto
{
    public int IngredientId { get; set; }

    public decimal? Amount { get; set; }

    public int UnitId { get; set; }

    public int? Position { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(255)]
    public string? Note { get; set; }
}
