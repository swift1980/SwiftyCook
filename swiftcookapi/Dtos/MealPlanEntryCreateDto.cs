using System.ComponentModel.DataAnnotations;
using SwiftCookDb.Models;

public class MealPlanEntryCreateDto
{
    [Required]
    public DateOnly? Date { get; set; }

    [Required, EnumDataType(typeof(MealType))]
    public MealType? MealType { get; set; }

    public int RecipeId { get; set; }

    /// <summary>Defaults to the recipe's yield (or 1) when omitted.</summary>
    [Range(1, 1000)]
    public int? Servings { get; set; }

    /// <summary>Defaults to the end of the day's slot when omitted.</summary>
    [Range(0, int.MaxValue)]
    public int? SortOrder { get; set; }
}
