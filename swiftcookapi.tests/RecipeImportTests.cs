using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;
using swiftcookapi.Controllers;
using swiftcookapi.Services;
using Xunit;

namespace swiftcookapi.tests
{
    public class RecipeImportTests : IDisposable
    {
        private readonly RecipeIngredientSearchTestContextFactory _factory = new();

        public RecipeImportTests()
        {
            using var ctx = _factory.CreateContext();
            ctx.Units.AddRange(
                new Unit { Id = 3, Name = "gram", PluralName = "grams", Abbreviation = "g" },
                new Unit { Id = 4, Name = "pinch", PluralName = "pinches" },
                new Unit { Id = 20, Name = "sgl" });
            ctx.IngredientCategories.Add(new IngredientCategory { Id = 1, Name = "Vegetables" });
            ctx.SaveChanges();
            ctx.IngredientTypes.Add(new IngredientType { Id = 1, Name = "Other vegetable", CategoryId = 1 });
            ctx.SaveChanges();
            ctx.IngredientCategories.Find(1)!.FallbackTypeId = 1;
            ctx.SaveChanges();
        }

        public void Dispose() => _factory.Dispose();

        private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

        private static string Recipe(string name = "Pancakes", string ingredients = "[{\"name\":\"Flour\",\"amount\":200,\"unit\":\"g\",\"note\":\"sifted\"}]", string extra = "") =>
            $"{{\"name\":\"{name}\",\"ingredients\":{ingredients},\"instructions\":[\"Mix.\"]{extra}}}";

        private async Task<RecipeImportResponse> Run(SwiftCookDbContext ctx, bool dryRun, ImportDecisions? decisions, params string[] recipes) =>
            await new RecipeImportService(ctx).ImportAsync(recipes.Select(Json).ToList(), decisions, dryRun);

        [Fact]
        public async Task DryRun_ResolvesNames_AndWritesNothing()
        {
            using var ctx = _factory.CreateContext();
            var before = await ctx.Recipes.CountAsync();

            var response = await Run(ctx, true, null, Recipe(ingredients:
                "[{\"name\":\"flour\",\"amount\":1,\"unit\":\"G\"},{\"name\":\"Suger\",\"amount\":1,\"unit\":\"g\"},{\"name\":\"Zucchini\",\"unit\":\"splash\"},{\"name\":\"Salt\"}]"));

            var item = Assert.Single(response.Recipes);
            Assert.Equal(ImportStatus.NeedsDecision, item.Status);
            Assert.Equal("exact", item.Ingredients[0].Resolution);
            Assert.Equal(3, item.Ingredients[0].UnitId);
            Assert.Equal("near", item.Ingredients[1].Resolution);
            Assert.Equal("Sugar", item.Ingredients[1].SuggestedIngredientName);
            Assert.Equal("unresolved", item.Ingredients[2].Resolution);
            Assert.Equal("unresolved", item.Ingredients[2].UnitResolution);
            Assert.Equal(20, item.Ingredients[3].UnitId);
            Assert.Equal(before, await ctx.Recipes.CountAsync());
        }

        [Fact]
        public async Task Import_WithDecisions_SavesRecipeWithNotesSourceAndNewEntities()
        {
            using var ctx = _factory.CreateContext();
            var decisions = new ImportDecisions();
            decisions.Ingredients["Suger"] = new IngredientDecision { IngredientId = 2 };
            decisions.Ingredients["Zucchini"] = new IngredientDecision { CreateCategoryId = 1 };
            decisions.Units["splash"] = 4;

            var response = await Run(ctx, false, decisions, Recipe(
                ingredients: "[{\"name\":\"Flour\",\"amount\":200,\"unit\":\"g\",\"note\":\"sifted\"},{\"name\":\"Suger\",\"amount\":1,\"unit\":\"g\"},{\"name\":\"Zucchini\",\"unit\":\"splash\"}]",
                extra: ",\"categories\":[\"dinner\",\"Brunch\"],\"tags\":[\"Quick\"],\"tools\":[\"Whisk\"],\"source\":{\"url\":\"https://example.com/p\",\"title\":\"T\",\"author\":\"A\"},\"prepTime\":5,\"yield\":4"));

            var item = Assert.Single(response.Recipes);
            Assert.Equal(ImportStatus.Imported, item.Status);
            Assert.Equal(new[] { "Brunch" }, item.NewCategories);

            using var check = _factory.CreateContext();
            var recipe = await check.Recipes
                .Include(r => r.RecipeIngredients).Include(r => r.Instructions)
                .Include(r => r.RecipeCategories).Include(r => r.RecipeTags).Include(r => r.RecipeTools)
                .SingleAsync(r => r.Id == item.RecipeId);

            Assert.Equal("pancakes", recipe.NameNormalized);
            Assert.Equal("https://example.com/p", recipe.SourceUrl);
            Assert.Equal("A", recipe.SourceAuthor);
            Assert.Equal(4, recipe.Yield);
            Assert.Equal(2, recipe.RecipeCategories.Count);
            Assert.Single(recipe.RecipeTags);
            Assert.Single(recipe.RecipeTools);
            Assert.Single(recipe.Instructions);
            Assert.Equal(3, recipe.RecipeIngredients.Count);
            Assert.Equal("sifted", recipe.RecipeIngredients.Single(i => i.IngredientId == 1).Note);
            Assert.Contains(recipe.RecipeIngredients, i => i.IngredientId == 2);

            var zucchini = await check.Ingredients.SingleAsync(i => i.Name == "Zucchini");
            Assert.Equal(1, zucchini.TypeId);
            Assert.Equal(4, recipe.RecipeIngredients.Single(i => i.IngredientId == zucchini.Id).UnitId);
            Assert.Equal(1, await check.Categories.CountAsync(c => c.Name == "Dinner"));
        }

        [Fact]
        public async Task Import_NearMatchWithoutDecision_IsNotSilentlyMapped()
        {
            using var ctx = _factory.CreateContext();
            var response = await Run(ctx, false, null, Recipe(ingredients: "[{\"name\":\"Suger\",\"unit\":\"g\"}]"));

            Assert.Equal(ImportStatus.NeedsDecision, response.Recipes[0].Status);
            Assert.Null(response.Recipes[0].RecipeId);
            Assert.False(await ctx.Recipes.AnyAsync(r => r.Name == "Pancakes"));
        }

        [Fact]
        public async Task Import_ExistingName_IsDuplicateUnlessImportAnyway()
        {
            using var ctx = _factory.CreateContext();
            Assert.Equal(ImportStatus.Imported, (await Run(ctx, false, null, Recipe())).Recipes[0].Status);

            var second = (await Run(ctx, false, null, Recipe("PANCAKES"))).Recipes[0];
            Assert.Equal(ImportStatus.Duplicate, second.Status);
            Assert.Single(second.Warnings);

            var decisions = new ImportDecisions { ImportDuplicates = { 0 } };
            Assert.Equal(ImportStatus.Imported, (await Run(ctx, false, decisions, Recipe("PANCAKES"))).Recipes[0].Status);
            Assert.Equal(2, await ctx.Recipes.CountAsync(r => r.Name.ToLower() == "pancakes"));
        }

        [Fact]
        public async Task Import_DuplicateNameWithinFile_IsFlaggedInDryRun()
        {
            using var ctx = _factory.CreateContext();
            var response = await Run(ctx, true, null, Recipe("Twin"), Recipe("twin"));

            Assert.Equal(ImportStatus.Ready, response.Recipes[0].Status);
            Assert.Equal(ImportStatus.Duplicate, response.Recipes[1].Status);
        }

        [Fact]
        public async Task Import_InvalidRecipe_DoesNotBlockOthers_AndSkipIsHonoured()
        {
            using var ctx = _factory.CreateContext();
            var decisions = new ImportDecisions { Skip = { 2 } };

            var response = await Run(ctx, false, decisions,
                "{\"name\":\"Bad\",\"ingredients\":[],\"instructions\":[\"x\"],\"surprise\":1}",
                Recipe("Good"),
                Recipe("Skipped"));

            Assert.Equal(ImportStatus.Invalid, response.Recipes[0].Status);
            Assert.Contains(response.Recipes[0].Errors, e => e.Contains("surprise"));
            Assert.Equal(ImportStatus.Imported, response.Recipes[1].Status);
            Assert.Equal(ImportStatus.Skipped, response.Recipes[2].Status);
            Assert.True(await ctx.Recipes.AnyAsync(r => r.Name == "Good"));
            Assert.False(await ctx.Recipes.AnyAsync(r => r.Name == "Bad" || r.Name == "Skipped"));
        }

        [Fact]
        public async Task Import_SameIngredientAndUnitTwice_IsRejectedBeforeSaving()
        {
            using var ctx = _factory.CreateContext();
            var response = await Run(ctx, false, null, Recipe(ingredients:
                "[{\"name\":\"Flour\",\"unit\":\"g\"},{\"name\":\"flour\",\"unit\":\"gram\"}]"));

            Assert.Equal(ImportStatus.Invalid, response.Recipes[0].Status);
            Assert.False(await ctx.Recipes.AnyAsync(r => r.Name == "Pancakes"));
        }

        [Theory]
        [InlineData("\"image\":\"javascript:alert(1)\"")]
        [InlineData("\"source\":{\"url\":\"data:text/html,x\"}")]
        [InlineData("\"source\":{\"link\":\"https://x.test\"}")]
        [InlineData("\"yield\":0")]
        [InlineData("\"prepTime\":1.5")]
        [InlineData("\"tags\":[\"a\",1]")]
        [InlineData("\"description\":null,\"name\":\"dup\"")]
        public void Validator_RejectsBadInput(string extra)
        {
            var (recipe, errors) = RecipeImportValidator.Validate(Json(Recipe(extra: "," + extra)));
            Assert.Null(recipe);
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void Validator_EnforcesLimits()
        {
            string Many(int n, string item) => "[" + string.Join(",", Enumerable.Repeat(item, n)) + "]";

            var tooManyIngredients = Many(101, "{\"name\":\"Salt\"}");
            Assert.NotEmpty(RecipeImportValidator.Validate(Json(Recipe(ingredients: tooManyIngredients))).Errors);

            var tooManySteps = $"{{\"name\":\"x\",\"ingredients\":[{{\"name\":\"Salt\"}}],\"instructions\":{Many(101, "\"s\"")}}}";
            Assert.NotEmpty(RecipeImportValidator.Validate(Json(tooManySteps)).Errors);

            var tooManyTags = Many(21, "\"t\"");
            Assert.NotEmpty(RecipeImportValidator.Validate(Json(Recipe(extra: $",\"tags\":{tooManyTags}"))).Errors);

            var longName = new string('a', 51);
            Assert.NotEmpty(RecipeImportValidator.Validate(Json(Recipe(longName))).Errors);

            Assert.NotEmpty(RecipeImportValidator.Validate(Json("[]")).Errors);
        }

        [Fact]
        public void Validator_AcceptsDocumentedExample()
        {
            var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "recipe-import.example.json"));
            var root = Json(File.ReadAllText(path));
            var elements = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToList() : new List<JsonElement> { root };
            Assert.NotEmpty(elements);
            foreach (var element in elements)
            {
                var (recipe, errors) = RecipeImportValidator.Validate(element);
                Assert.Empty(errors);
                Assert.NotEmpty(recipe!.Ingredients);
            }
        }

        [Fact]
        public async Task Controller_RejectsOversizedBatchesAndWrongShapes()
        {
            using var ctx = _factory.CreateContext();
            var controller = new RecipeImportController(new RecipeImportService(ctx));

            var tooMany = "[" + string.Join(",", Enumerable.Repeat(Recipe(), 51)) + "]";
            var r1 = await controller.Import(new RecipeImportRequest { Recipes = Json(tooMany) });
            Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(r1.Result);

            var r2 = await controller.Import(new RecipeImportRequest { Recipes = Json("\"text\"") });
            Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(r2.Result);

            var r3 = await controller.Import(new RecipeImportRequest { Recipes = Json(Recipe()) });
            Assert.Single(r3.Value!.Recipes);
        }

        [Theory]
        [InlineData("Suger", "Sugar")]
        [InlineData("Flur", "Flour")]
        [InlineData("Oi", null)]
        [InlineData("Sugar", null)]
        public void NameMatcher_FindsNearMatchesOnly(string input, string? expected)
        {
            var names = new[] { "Sugar", "Flour", "Oat" };
            Assert.Equal(expected, NameMatcher.FindClosest(input, names, n => n));
        }
    }
}
