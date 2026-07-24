public class RecipeSearchResultDto
{
    public int RecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }

    /// <summary>
    /// Number of optional ingredients present on this recipe. Primary sort key (DESC).
    /// </summary>
    public int OptionalMatchCount { get; set; }

    /// <summary>
    /// Total number of optional ingredients that were searched for.
    /// </summary>
    public int OptionalTotal { get; set; }

    /// <summary>
    /// The specific optional ingredient IDs that matched (lets the UI highlight them).
    /// </summary>
    public List<int> MatchedOptionalIds { get; set; } = new();

    /// <summary>
    /// Distinct recipe ingredients not covered by the search. Secondary sort key (ASC).
    /// </summary>
    public int ExtraIngredientCount { get; set; }

    /// <summary>
    /// Distinct ingredient count on the recipe (counted by distinct IngredientId).
    /// </summary>
    public int TotalIngredientCount { get; set; }
}