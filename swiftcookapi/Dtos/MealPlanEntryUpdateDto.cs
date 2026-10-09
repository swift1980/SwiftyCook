using System.ComponentModel.DataAnnotations;
using SwiftCookDb.Models;

public class MealPlanEntryUpdateDto
{
    [Required]
    public DateOnly? Date { get; set; }

    [Required, EnumDataType(typeof(MealType))]
    public MealType? MealType { get; set; }

    public int RecipeId { get; set; }

    [Range(1, 1000)]
    public int Servings { get; set; } = 1;

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }
}
