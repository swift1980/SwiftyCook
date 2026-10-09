using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Services
{
    // Ticket 16: imports recipes from the JSON format in docs/recipe-import-format.md.
    // Each recipe is validated, resolved and saved independently (own transaction)
    // so one bad recipe never blocks the rest of the file.
    public class RecipeImportService
    {
        // Unit used for lines that give no unit ("2 onions"); RecipeIngredient's
        // primary key includes UnitId, so a unit is always stored.
        private const string DefaultUnitName = "sgl";

        private readonly SwiftCookDbContext _context;

        public RecipeImportService(SwiftCookDbContext context)
        {
            _context = context;
        }

        public async Task<RecipeImportResponse> ImportAsync(
            IReadOnlyList<JsonElement> elements, ImportDecisions? decisions, bool dryRun)
        {
            decisions ??= new ImportDecisions();
            var response = new RecipeImportResponse { DryRun = dryRun };
            var plannedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < elements.Count; index++)
            {
                var item = new RecipeImportItem
                {
                    Index = index,
                    Name = RecipeImportValidator.BestEffortName(elements[index])
                };
                response.Recipes.Add(item);

                if (decisions.Skip.Contains(index))
                {
                    item.Status = ImportStatus.Skipped;
                    continue;
                }

                var (recipe, errors) = RecipeImportValidator.Validate(elements[index]);
                if (recipe == null)
                {
                    item.Status = ImportStatus.Invalid;
                    item.Errors.AddRange(errors);
                    continue;
                }

                item.Name = recipe.Name;
                _context.ChangeTracker.Clear();
                var plan = await ResolveAsync(recipe, decisions, item, plannedNames, index);
                if (item.Status != ImportStatus.Ready) continue;

                plannedNames.Add(recipe.Name);
                if (dryRun) continue;

                await SaveAsync(recipe, plan, item);
            }

            return response;
        }

        private class Plan
        {
            public Dictionary<string, Ingredient> NewIngredients { get; } = new(StringComparer.OrdinalIgnoreCase);
            public List<(ImportIngredient Line, ImportLinePreview Preview)> Lines { get; } = new();
            public List<Category> Categories { get; } = new();
            public List<Tag> Tags { get; } = new();
            public List<Tool> Tools { get; } = new();
        }

        private async Task<Plan> ResolveAsync(
            ImportRecipe recipe, ImportDecisions decisions, RecipeImportItem item,
            HashSet<string> plannedNames, int index)
        {
            var plan = new Plan();

            var ingredients = await _context.Ingredients.AsNoTracking().ToListAsync();
            var units = await _context.Units.AsNoTracking().ToListAsync();
            var categoryMap = await _context.IngredientCategories.AsNoTracking()
                .ToDictionaryAsync(c => c.Id);

            var ingredientByName = new Dictionary<string, Ingredient>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in ingredients.OrderBy(i => i.Id))
            {
                ingredientByName.TryAdd(i.Name, i);
                if (!string.IsNullOrWhiteSpace(i.PluralName)) ingredientByName.TryAdd(i.PluralName, i);
            }
            var ingredientById = ingredients.ToDictionary(i => i.Id);

            var unitByName = new Dictionary<string, Unit>(StringComparer.OrdinalIgnoreCase);
            foreach (var u in units.OrderBy(u => u.Id))
            {
                unitByName.TryAdd(u.Name, u);
                if (!string.IsNullOrWhiteSpace(u.PluralName)) unitByName.TryAdd(u.PluralName, u);
                if (!string.IsNullOrWhiteSpace(u.Abbreviation)) unitByName.TryAdd(u.Abbreviation, u);
            }
            var unitById = units.ToDictionary(u => u.Id);

            var ingDecisions = new Dictionary<string, IngredientDecision>(decisions.Ingredients, StringComparer.OrdinalIgnoreCase);
            var unitDecisions = new Dictionary<string, int>(decisions.Units, StringComparer.OrdinalIgnoreCase);

            var needsDecision = false;
            var lineKeys = new HashSet<string>();

            foreach (var line in recipe.Ingredients)
            {
                var preview = new ImportLinePreview
                {
                    Name = line.Name, Amount = line.Amount, Unit = line.Unit, Note = line.Note
                };
                item.Ingredients.Add(preview);
                plan.Lines.Add((line, preview));

                string ingredientKey;
                if (ingredientByName.TryGetValue(line.Name, out var exact))
                {
                    preview.Resolution = "exact";
                    preview.IngredientId = exact.Id;
                    preview.IngredientName = exact.Name;
                    ingredientKey = "id:" + exact.Id;
                }
                else
                {
                    var near = NameMatcher.FindClosest(line.Name, ingredients, i => i.Name);
                    ingDecisions.TryGetValue(line.Name, out var decision);

                    if (decision?.IngredientId is int mappedId && ingredientById.TryGetValue(mappedId, out var mapped))
                    {
                        preview.Resolution = "mapped";
                        preview.IngredientId = mapped.Id;
                        preview.IngredientName = mapped.Name;
                        ingredientKey = "id:" + mapped.Id;
                    }
                    else if (decision?.CreateCategoryId is int catId
                        && categoryMap.TryGetValue(catId, out var cat) && cat.FallbackTypeId != null)
                    {
                        preview.Resolution = "create";
                        preview.IngredientName = line.Name;
                        ingredientKey = "new:" + line.Name.ToLowerInvariant();
                        if (!plan.NewIngredients.ContainsKey(line.Name))
                        {
                            plan.NewIngredients[line.Name] = new Ingredient
                            {
                                Name = line.Name,
                                PluralName = line.Name,
                                TypeId = cat.FallbackTypeId
                            };
                        }
                    }
                    else
                    {
                        preview.Resolution = near != null ? "near" : "unresolved";
                        preview.SuggestedIngredientId = near?.Id;
                        preview.SuggestedIngredientName = near?.Name;
                        needsDecision = true;
                        ingredientKey = "unresolved:" + line.Name.ToLowerInvariant();
                    }
                }

                Unit? unit = null;
                if (string.IsNullOrWhiteSpace(line.Unit))
                {
                    if (unitByName.TryGetValue(DefaultUnitName, out unit))
                    {
                        preview.UnitResolution = "none";
                    }
                    else
                    {
                        item.Errors.Add($"The default unit '{DefaultUnitName}' does not exist.");
                    }
                }
                else if (unitByName.TryGetValue(line.Unit.TrimEnd('.'), out unit))
                {
                    preview.UnitResolution = "matched";
                }
                else if (unitDecisions.TryGetValue(line.Unit, out var unitId) && unitById.TryGetValue(unitId, out unit))
                {
                    preview.UnitResolution = "mapped";
                }
                else
                {
                    preview.UnitResolution = "unresolved";
                    needsDecision = true;
                }

                if (unit != null)
                {
                    preview.UnitId = unit.Id;
                    preview.UnitName = unit.Name;
                    if (!ingredientKey.StartsWith("unresolved:") && !lineKeys.Add($"{ingredientKey}|{unit.Id}"))
                        item.Errors.Add($"'{line.Name}' appears more than once with the same unit.");
                }
            }

            await ResolveLabelsAsync(_context.Categories, recipe.Categories, plan.Categories, item.NewCategories, c => c.Name, n => new Category { Name = n });
            await ResolveLabelsAsync(_context.Tags, recipe.Tags, plan.Tags, item.NewTags, t => t.Name, n => new Tag { Name = n });
            await ResolveLabelsAsync(_context.Tools, recipe.Tools, plan.Tools, item.NewTools, t => t.Name, n => new Tool { Name = n });

            var lower = recipe.Name.ToLowerInvariant();
            var exists = plannedNames.Contains(recipe.Name)
                || await _context.Recipes.AnyAsync(r => r.NameNormalized == lower || r.Name == recipe.Name);
            var importAnyway = decisions.ImportDuplicates.Contains(index);
            if (exists) item.Warnings.Add($"A recipe named '{recipe.Name}' already exists.");

            if (item.Errors.Count > 0) item.Status = ImportStatus.Invalid;
            else if (needsDecision) item.Status = ImportStatus.NeedsDecision;
            else if (exists && !importAnyway) item.Status = ImportStatus.Duplicate;
            else item.Status = ImportStatus.Ready;

            return plan;
        }

        // Categories, tags and tools are matched case-insensitively and created when missing.
        private static async Task ResolveLabelsAsync<T>(
            DbSet<T> set, List<string> names, List<T> resolved, List<string> newNames,
            Func<T, string> nameOf, Func<string, T> create) where T : class
        {
            var existing = await set.ToListAsync();
            foreach (var name in names)
            {
                var match = existing.FirstOrDefault(e => string.Equals(nameOf(e), name, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    match = create(name);
                    newNames.Add(name);
                }
                resolved.Add(match);
            }
        }

        private async Task SaveAsync(ImportRecipe recipe, Plan plan, RecipeImportItem item)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = new Recipe
                {
                    Name = recipe.Name,
                    NameNormalized = recipe.Name.ToLowerInvariant(),
                    Description = recipe.Description,
                    DescriptionNormalized = recipe.Description?.ToLowerInvariant(),
                    Image = recipe.Image,
                    PrepTime = recipe.PrepTime,
                    CookTime = recipe.CookTime,
                    Yield = recipe.Yield,
                    SourceUrl = recipe.SourceUrl,
                    SourceTitle = recipe.SourceTitle,
                    SourceAuthor = recipe.SourceAuthor
                };

                foreach (var c in plan.Categories) entity.RecipeCategories.Add(new RecipeCategory { Category = c });
                foreach (var t in plan.Tags) entity.RecipeTags.Add(new RecipeTag { Tag = t });
                foreach (var t in plan.Tools) entity.RecipeTools.Add(new RecipeTool { Tool = t });

                var position = 1;
                foreach (var (line, preview) in plan.Lines)
                {
                    var ri = new RecipeIngredient
                    {
                        UnitId = preview.UnitId,
                        Amount = line.Amount,
                        Position = position++,
                        Note = line.Note
                    };
                    if (preview.IngredientId != null) ri.IngredientId = preview.IngredientId.Value;
                    else ri.Ingredient = plan.NewIngredients[line.Name];
                    entity.RecipeIngredients.Add(ri);
                }

                position = 1;
                foreach (var step in recipe.Instructions)
                    entity.Instructions.Add(new RecipeInstruction { Position = position++, Step = step });

                _context.Recipes.Add(entity);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                item.Status = ImportStatus.Imported;
                item.RecipeId = entity.Id;
            }
            catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException)
            {
                await tx.RollbackAsync();
                _context.ChangeTracker.Clear();
                item.Status = ImportStatus.Failed;
                item.Errors.Add("The recipe could not be saved.");
            }
        }
    }
}
