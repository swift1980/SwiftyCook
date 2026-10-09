using System.Text.Json;

namespace swiftcookapi.Services
{
    // Hand-coded validation of the untrusted upload (docs/recipe-import-format.md).
    // Runs before any database lookup or name resolution.
    public static class RecipeImportValidator
    {
        public const int MaxRecipes = 50;
        public const int MaxIngredients = 100;
        public const int MaxInstructions = 100;
        public const int MaxLabels = 20;
        public const decimal MaxAmount = 10_000_000m;

        private static readonly HashSet<string> RecipeProps = new()
        {
            "name", "description", "image", "prepTime", "cookTime", "yield",
            "categories", "tags", "tools", "ingredients", "instructions", "source"
        };
        private static readonly HashSet<string> IngredientProps = new() { "name", "amount", "unit", "note" };
        private static readonly HashSet<string> SourceProps = new() { "url", "title", "author" };

        public static string? BestEffortName(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty("name", out var n)
                && n.ValueKind == JsonValueKind.String)
            {
                var s = n.GetString()?.Trim();
                return string.IsNullOrEmpty(s) ? null : (s.Length > 50 ? s[..50] : s);
            }
            return null;
        }

        public static (ImportRecipe? Recipe, List<string> Errors) Validate(JsonElement element)
        {
            var errors = new List<string>();
            if (element.ValueKind != JsonValueKind.Object)
            {
                errors.Add("Recipe must be a JSON object.");
                return (null, errors);
            }

            CheckProps(element, RecipeProps, "", errors);

            var recipe = new ImportRecipe
            {
                Name = RequiredString(element, "name", 50, "name", errors) ?? string.Empty,
                Description = OptionalString(element, "description", 5000, "description", errors),
                Image = Url(OptionalString(element, "image", 255, "image", errors), "image", errors),
                PrepTime = OptionalInt(element, "prepTime", 0, "prepTime", errors),
                CookTime = OptionalInt(element, "cookTime", 0, "cookTime", errors),
                Yield = OptionalInt(element, "yield", 1, "yield", errors),
                Categories = Labels(element, "categories", errors),
                Tags = Labels(element, "tags", errors),
                Tools = Labels(element, "tools", errors)
            };

            ReadIngredients(element, recipe, errors);
            ReadInstructions(element, recipe, errors);
            ReadSource(element, recipe, errors);

            return errors.Count > 0 ? (null, errors) : (recipe, errors);
        }

        private static void CheckProps(JsonElement obj, HashSet<string> allowed, string path, List<string> errors)
        {
            var seen = new HashSet<string>();
            foreach (var p in obj.EnumerateObject())
            {
                if (!allowed.Contains(p.Name))
                    errors.Add($"{path}{Truncate(p.Name)}: unknown property.");
                else if (!seen.Add(p.Name))
                    errors.Add($"{path}{p.Name}: duplicate property.");
            }
        }

        private static string Truncate(string s) => s.Length > 40 ? s[..40] + "..." : s;

        private static bool TryGet(JsonElement obj, string name, out JsonElement value)
        {
            if (obj.TryGetProperty(name, out value) && value.ValueKind != JsonValueKind.Null) return true;
            value = default;
            return false;
        }

        private static string? RequiredString(JsonElement obj, string prop, int max, string path, List<string> errors)
        {
            if (!TryGet(obj, prop, out var v)) { errors.Add($"{path}: required."); return null; }
            var s = ReadString(v, max, path, errors);
            if (s != null && s.Length == 0) { errors.Add($"{path}: must not be empty."); return null; }
            return s;
        }

        private static string? OptionalString(JsonElement obj, string prop, int max, string path, List<string> errors)
        {
            if (!TryGet(obj, prop, out var v)) return null;
            var s = ReadString(v, max, path, errors);
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static string? ReadString(JsonElement v, int max, string path, List<string> errors)
        {
            if (v.ValueKind != JsonValueKind.String) { errors.Add($"{path}: must be a string."); return null; }
            var s = (v.GetString() ?? string.Empty).Trim();
            if (s.Length > max) { errors.Add($"{path}: longer than {max} characters."); return null; }
            return s;
        }

        // Image and source URLs end up as links/images in the UI, so only
        // absolute http(s) URLs are accepted (blocks javascript: and data: URIs).
        private static string? Url(string? s, string path, List<string> errors)
        {
            if (s == null) return null;
            if (!Uri.TryCreate(s, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add($"{path}: must be an http or https URL.");
                return null;
            }
            return s;
        }

        private static int? OptionalInt(JsonElement obj, string prop, int min, string path, List<string> errors)
        {
            if (!TryGet(obj, prop, out var v)) return null;
            if (v.ValueKind != JsonValueKind.Number || !v.TryGetInt32(out var i))
            {
                errors.Add($"{path}: must be an integer.");
                return null;
            }
            if (i < min) { errors.Add($"{path}: must be at least {min}."); return null; }
            return i;
        }

        private static List<string> Labels(JsonElement obj, string prop, List<string> errors)
        {
            var result = new List<string>();
            if (!TryGet(obj, prop, out var arr)) return result;
            if (arr.ValueKind != JsonValueKind.Array) { errors.Add($"{prop}: must be an array."); return result; }
            if (arr.GetArrayLength() > MaxLabels) { errors.Add($"{prop}: more than {MaxLabels} entries."); return result; }

            var i = 0;
            foreach (var item in arr.EnumerateArray())
            {
                var s = ReadString(item, 50, $"{prop}[{i++}]", errors);
                if (s == null) continue;
                if (s.Length == 0) { errors.Add($"{prop}[{i - 1}]: must not be empty."); continue; }
                if (!result.Contains(s, StringComparer.OrdinalIgnoreCase)) result.Add(s);
            }
            return result;
        }

        private static void ReadIngredients(JsonElement obj, ImportRecipe recipe, List<string> errors)
        {
            if (!TryGet(obj, "ingredients", out var arr)) { errors.Add("ingredients: required."); return; }
            if (arr.ValueKind != JsonValueKind.Array) { errors.Add("ingredients: must be an array."); return; }
            var count = arr.GetArrayLength();
            if (count == 0) { errors.Add("ingredients: at least one is required."); return; }
            if (count > MaxIngredients) { errors.Add($"ingredients: more than {MaxIngredients} entries."); return; }

            var i = 0;
            foreach (var item in arr.EnumerateArray())
            {
                var path = $"ingredients[{i++}]";
                if (item.ValueKind != JsonValueKind.Object) { errors.Add($"{path}: must be an object."); continue; }

                CheckProps(item, IngredientProps, path + ".", errors);
                var name = RequiredString(item, "name", 50, path + ".name", errors);
                var unit = OptionalString(item, "unit", 50, path + ".unit", errors);
                var note = OptionalString(item, "note", 255, path + ".note", errors);

                decimal? amount = null;
                if (TryGet(item, "amount", out var a))
                {
                    if (a.ValueKind != JsonValueKind.Number || !a.TryGetDecimal(out var d))
                        errors.Add($"{path}.amount: must be a number.");
                    else if (d < 0 || d > MaxAmount)
                        errors.Add($"{path}.amount: must be between 0 and {MaxAmount:0}.");
                    else
                        amount = Math.Round(d, 4);
                }

                if (name != null)
                    recipe.Ingredients.Add(new ImportIngredient { Name = name, Amount = amount, Unit = unit, Note = note });
            }
        }

        private static void ReadInstructions(JsonElement obj, ImportRecipe recipe, List<string> errors)
        {
            if (!TryGet(obj, "instructions", out var arr)) { errors.Add("instructions: required."); return; }
            if (arr.ValueKind != JsonValueKind.Array) { errors.Add("instructions: must be an array."); return; }
            var count = arr.GetArrayLength();
            if (count == 0) { errors.Add("instructions: at least one is required."); return; }
            if (count > MaxInstructions) { errors.Add($"instructions: more than {MaxInstructions} entries."); return; }

            var i = 0;
            foreach (var item in arr.EnumerateArray())
            {
                var path = $"instructions[{i++}]";
                var s = ReadString(item, 5000, path, errors);
                if (s == null) continue;
                if (s.Length == 0) { errors.Add($"{path}: must not be empty."); continue; }
                recipe.Instructions.Add(s);
            }
        }

        private static void ReadSource(JsonElement obj, ImportRecipe recipe, List<string> errors)
        {
            if (!TryGet(obj, "source", out var src)) return;
            if (src.ValueKind != JsonValueKind.Object) { errors.Add("source: must be an object."); return; }

            CheckProps(src, SourceProps, "source.", errors);
            recipe.SourceUrl = Url(OptionalString(src, "url", 500, "source.url", errors), "source.url", errors);
            recipe.SourceTitle = OptionalString(src, "title", 255, "source.title", errors);
            recipe.SourceAuthor = OptionalString(src, "author", 255, "source.author", errors);
        }
    }
}
