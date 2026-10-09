public class CupboardSearchRequestDto
{
    /// <summary>
    /// The cupboard ingredients to cook with (a selection, not necessarily everything in the cupboard).
    /// </summary>
    public List<int> IngredientIds { get; set; } = new();

    /// <summary>
    /// False (default): only recipes whose every ingredient is selected or a staple.
    /// True: also recipes with missing ingredients, as long as at least one selected ingredient is used.
    /// </summary>
    public bool IncludeMissing { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    /// <summary>Optional recipe Category filter (set server-side for Cocktails).</summary>
    public int? CategoryId { get; set; }
}
