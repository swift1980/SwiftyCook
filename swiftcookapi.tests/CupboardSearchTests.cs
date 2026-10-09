using swiftcookapi.Services;
using Xunit;

namespace swiftcookapi.tests
{
    // Seed: Cake {1,2,3,4}, Cookie {1,2,3} (both Category 1), Omelette {4,5} (Category 2).
    public class CupboardSearchTests
    {
        private static async Task<RecipeIngredientSearchOutcome> Run(
            RecipeIngredientSearchTestContextFactory factory,
            int[] ids, bool includeMissing = false, int? categoryId = null, int[]? staples = null)
        {
            using var ctx = factory.CreateContext();
            if (staples != null)
            {
                foreach (var i in ctx.Ingredients.Where(i => staples.Contains(i.Id))) i.IsStaple = true;
                ctx.SaveChanges();
            }
            var service = new RecipeIngredientSearchService(ctx);
            return await service.SearchCupboardAsync(new CupboardSearchRequestDto
            {
                IngredientIds = ids.ToList(),
                IncludeMissing = includeMissing,
                CategoryId = categoryId
            });
        }

        [Fact]
        public async Task Strict_ReturnsOnlyFullyCoveredRecipes()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var outcome = await Run(factory, new[] { 1, 2, 3 });

            var item = Assert.Single(outcome.Result!.Items);
            Assert.Equal(11, item.RecipeId);
            Assert.Equal(0, item.MissingIngredientCount);
        }

        [Fact]
        public async Task Strict_SelectionCanBeASubsetOfTheCupboard()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            // Only Flour and Sugar selected: Cookie also needs Butter, so nothing is makeable.
            var outcome = await Run(factory, new[] { 1, 2 });
            Assert.Empty(outcome.Result!.Items);
        }

        [Fact]
        public async Task Staples_CountAsCovered()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            // Egg selected, Salt is a staple: Omelette {4,5} is makeable.
            var outcome = await Run(factory, new[] { 4 }, staples: new[] { 5 });

            var item = Assert.Single(outcome.Result!.Items);
            Assert.Equal(12, item.RecipeId);
        }

        [Fact]
        public async Task StaplesAlone_DoNotMakeARecipeMatch()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            // Vanilla selected but in no recipe; Salt staple must not pull Omelette in.
            var outcome = await Run(factory, new[] { 6 }, staples: new[] { 5 });
            Assert.Empty(outcome.Result!.Items);
        }

        [Fact]
        public async Task IncludeMissing_RanksByFewestMissing()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var outcome = await Run(factory, new[] { 1, 2, 3 }, includeMissing: true);

            var items = outcome.Result!.Items;
            Assert.Equal(new[] { 11, 10 }, items.Select(i => i.RecipeId).ToArray());
            Assert.Equal(new int?[] { 0, 1 }, items.Select(i => i.MissingIngredientCount).ToArray());
        }

        [Fact]
        public async Task IncludeMissing_StaplesAreNeverMissing()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var outcome = await Run(factory, new[] { 1 }, includeMissing: true, staples: new[] { 2, 3 });

            var cookie = Assert.Single(outcome.Result!.Items, i => i.RecipeId == 11);
            Assert.Equal(0, cookie.MissingIngredientCount);
            var cake = Assert.Single(outcome.Result!.Items, i => i.RecipeId == 10);
            Assert.Equal(1, cake.MissingIngredientCount);
        }

        [Fact]
        public async Task CategoryFilter_ScopesTheRecipePool()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var outcome = await Run(factory, new[] { 4 }, includeMissing: true, categoryId: 2);

            Assert.Equal(12, Assert.Single(outcome.Result!.Items).RecipeId);
        }

        [Fact]
        public async Task EmptyOrUnknownSelection_IsInvalid()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            Assert.False((await Run(factory, Array.Empty<int>())).IsValid);
            Assert.False((await Run(factory, new[] { 999 })).IsValid);
        }
    }
}
