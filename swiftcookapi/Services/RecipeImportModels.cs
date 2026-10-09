using System.Text.Json;

namespace swiftcookapi.Services
{
    public class RecipeImportRequest
    {
        // An array of recipe objects, or a single recipe object.
        public JsonElement Recipes { get; set; }
        public ImportDecisions? Decisions { get; set; }
    }

    // User choices made in the dry-run preview. Dictionary keys are the names
    // exactly as written in the file (matched case-insensitively).
    public class ImportDecisions
    {
        public Dictionary<string, IngredientDecision> Ingredients { get; set; } = new();
        public Dictionary<string, int> Units { get; set; } = new();
        // Recipe indexes (position in the file) to leave out.
        public List<int> Skip { get; set; } = new();
        // Recipe indexes whose name already exists but should be imported anyway.
        public List<int> ImportDuplicates { get; set; } = new();
    }

    public class IngredientDecision
    {
        // Map to this existing ingredient...
        public int? IngredientId { get; set; }
        // ...or create a new one under this ingredient category's fallback type.
        public int? CreateCategoryId { get; set; }
    }

    public class ImportRecipe
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Image { get; set; }
        public int? PrepTime { get; set; }
        public int? CookTime { get; set; }
        public int? Yield { get; set; }
        public List<string> Categories { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<string> Tools { get; set; } = new();
        public List<ImportIngredient> Ingredients { get; set; } = new();
        public List<string> Instructions { get; set; } = new();
        public string? SourceUrl { get; set; }
        public string? SourceTitle { get; set; }
        public string? SourceAuthor { get; set; }
    }

    public class ImportIngredient
    {
        public string Name { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Unit { get; set; }
        public string? Note { get; set; }
    }

    public static class ImportStatus
    {
        public const string Invalid = "invalid";
        public const string Skipped = "skipped";
        public const string NeedsDecision = "needsDecision";
        public const string Duplicate = "duplicate";
        public const string Ready = "ready";
        public const string Imported = "imported";
        public const string Failed = "failed";
    }

    public class RecipeImportResponse
    {
        public bool DryRun { get; set; }
        public List<RecipeImportItem> Recipes { get; set; } = new();
    }

    public class RecipeImportItem
    {
        public int Index { get; set; }
        public string? Name { get; set; }
        public string Status { get; set; } = ImportStatus.Ready;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<ImportLinePreview> Ingredients { get; set; } = new();
        public List<string> NewCategories { get; set; } = new();
        public List<string> NewTags { get; set; } = new();
        public List<string> NewTools { get; set; } = new();
        public int? RecipeId { get; set; }
    }

    public class ImportLinePreview
    {
        public string Name { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Unit { get; set; }
        public string? Note { get; set; }

        // exact | mapped | create | near | unresolved
        public string Resolution { get; set; } = "unresolved";
        public int? IngredientId { get; set; }
        public string? IngredientName { get; set; }
        public int? SuggestedIngredientId { get; set; }
        public string? SuggestedIngredientName { get; set; }

        // none | matched | mapped | unresolved
        public string UnitResolution { get; set; } = "none";
        public int? UnitId { get; set; }
        public string? UnitName { get; set; }
    }
}
