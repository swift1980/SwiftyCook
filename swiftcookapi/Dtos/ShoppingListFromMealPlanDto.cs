using System.ComponentModel.DataAnnotations;

public class ShoppingListFromMealPlanDto
{
    [Required]
    public DateOnly? From { get; set; }

    [Required]
    public DateOnly? To { get; set; }

    /// <summary>When true the shortages are calculated and returned but nothing is saved.</summary>
    public bool DryRun { get; set; }
}

public class ShoppingListGenerationLineDto
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public bool UnitMismatch { get; set; }
}
