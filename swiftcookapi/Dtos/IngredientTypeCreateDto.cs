public class IngredientTypeCreateDto
{
    public string Name { get; set; } = string.Empty;

    // Optional: if omitted, the type is assigned to the "Miscellaneous" category
    // (see IngredientTypeController.Create).
    public int? CategoryId { get; set; }
}
