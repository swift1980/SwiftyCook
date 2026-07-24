namespace swiftcookapi.Services
{
    public class RecipeIngredientSearchOutcome
    {
        public bool IsValid { get; init; }
        public string? Error { get; init; }
        public PagedResultDto<RecipeSearchResultDto>? Result { get; init; }

        public static RecipeIngredientSearchOutcome Invalid(string error) =>
            new() { IsValid = false, Error = error };

        public static RecipeIngredientSearchOutcome Success(PagedResultDto<RecipeSearchResultDto> result) =>
            new() { IsValid = true, Result = result };
    }
}