public class IngredientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PluralName { get; set; } = string.Empty;
    public int? TypeId { get; set; }
    public string? TypeName { get; set; }
}
