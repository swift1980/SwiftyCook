public class RecipeIngredientSearchRequestDto
{
    /// <summary>
    /// Ingredient IDs that must ALL be present on a matching recipe.
    /// </summary>
    public List<int> MandatoryIngredientIds { get; set; } = new();

    /// <summary>
    /// Ingredient IDs that are "nice to have". A recipe must contain at least
    /// <see cref="OptionalThreshold"/> of these to qualify.
    /// </summary>
    public List<int> OptionalIngredientIds { get; set; } = new();

    /// <summary>
    /// Minimum number of <see cref="OptionalIngredientIds"/> that must be present.
    /// Applies only to the optional pool. Must be within [0, optional pool size].
    /// </summary>
    public int OptionalThreshold { get; set; }

    /// <summary>
    /// 1-based page number.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Requested page size. Server clamps this to an allowed maximum.
    /// </summary>
    public int PageSize { get; set; } = 20;
}