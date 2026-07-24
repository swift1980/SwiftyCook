using swiftcookapi.Services;
using Xunit;

namespace swiftcookapi.tests
{
    public class RecipeIngredientSearchServiceTests
    {
        private static RecipeIngredientSearchService CreateService(
            RecipeIngredientSearchTestContextFactory factory) =>
            new(factory.CreateContext());

        // ---------- The highest-risk case: UnitId-driven duplicate ingredient ----------

        [Fact]
        public async Task DuplicateIngredientAcrossUnits_IsCountedOnce()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var service = CreateService(factory);

            // Cake lists Flour twice (2 units). It has 4 DISTINCT ingredients: 1,2,3,4.
            var request = new RecipeIngredientSearchRequestDto
            {
                MandatoryIngredientIds = new() { 1 }, // Flour
                OptionalIngredientIds = new(),
                OptionalThreshold = 0
            };

            var outcome = await service.SearchAsync(request);

            Assert.True(outcome.IsValid);
            var cake = Assert.Single(outcome.Result!.Items, r => r.RecipeId == 10);
            Assert.Equal(4, cake.TotalIngredientCount);       // NOT 5 (Flour once)
            Assert.Equal(3, cake.ExtraIngredientCount);        // 4 total - 1 mandatory - 0 optional
        }

        // ---------- Matching rule (Option C) ----------

        [Fact]
        public async Task MandatoryMustAllBePresent()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var service = CreateService(factory);

            // Mandatory Flour(1) + Egg(4): only Cake has both. Cookie lacks Egg, Omelette lacks Flour.
            var request = new RecipeIngredientSearchRequestDto
            {
                MandatoryIngredientIds = new() { 1, 4 },
                OptionalIngredientIds = new(),
                OptionalThreshold = 0
            };

            var outcome = await service.SearchAsync(request);

            Assert.True(outcome.IsValid);
            var item = Assert.Single(outcome.Result!.Items);
            Assert.Equal(10, item.RecipeId);
        }

        [Fact]
        public async Task OptionalThreshold_FiltersByMatchCount()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var service = CreateService(factory);

            // Mandatory Flour(1); optional Sugar(2), Butter(3), Egg(4); threshold 3.
            // Cake matches all 3 optional; Cookie matches only 2 (Sugar, Butter) -> excluded.
            var request = new RecipeIngredientSearchRequestDto
            {
                MandatoryIngredientIds = new() { 1 },
                OptionalIngredientIds = new() { 2, 3, 4 },
                OptionalThreshold = 3
            };

            var outcome = await service.SearchAsync(request);

            Assert.True(outcome.IsValid);
            var item = Assert.Single(outcome.Result!.Items);
            Assert.Equal(10, item.RecipeId);
            Assert.Equal(3, item.OptionalMatchCount);
            Assert.Equal(3, item.OptionalTotal);
            Assert.Equal(new[] { 2, 3, 4 }, item.MatchedOptionalIds.OrderBy(x => x));
        }

        // ---------- Ranking: optionalMatchCount DESC, then extras ASC ----------

        [Fact]
        public async Task Results_AreRankedByOptionalMatchThenExtras()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var service = CreateService(factory);

            // Mandatory Flour(1); optional Sugar(2), Butter(3), Egg(4); threshold 2.
            // Cake:   optional matches = 3 (2,3,4), extras = 0
            // Cookie: optional matches = 2 (2,3),   extras = 0
            // Cake should rank first (higher optional match count).
            var request = new RecipeIngredientSearchRequestDto
            {
                MandatoryIngredientIds = new() { 1 },
                OptionalIngredientIds = new() { 2, 3, 4 },
                OptionalThreshold = 2
            };

            var outcome = await service.SearchAsync(request);

            Assert.True(outcome.IsValid);
            var ids = outcome.Result!.Items.Select(i => i.RecipeId).ToList();
            Assert.Equal(new[] { 10, 11 }, ids);
        }

        // ---------- Normalize quietly ----------

        [Fact]
        public async Task MandatoryWins_WhenIdInBothLists()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var service = CreateService(factory);

            // Flour(1) in both lists -> treated as mandatory only; optional pool becomes empty.
            var request = new RecipeIngredientSearchRequestDto
            {
                MandatoryIngredientIds = new() { 1 },
                OptionalIngredientIds = new() { 1 },
                OptionalThreshold = 1
            };

            var outcome = await service.SearchAsync(request);

            Assert.True(outcome.IsValid);
            // Optional pool empty -> threshold treated as 0; all recipes with Flour qualify.
            Assert.All(outcome.Result!.Items, r => Assert.Equal(0, r.OptionalTotal));
            Assert.Contains(outcome.Result.Items, r => r.RecipeId == 10);
            Assert.Contains(outcome.Result.Items, r => r.RecipeId == 11);
        }

        [Fact]
        public async Task DuplicateIdsInList_AreDeduplicated()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var service = CreateService(factory);

            var request = new RecipeIngredientSearchRequestDto
            {
                MandatoryIngredientIds = new() { 1, 1, 1 },
                OptionalIngredientIds = new() { 2, 2 },
                OptionalThreshold = 1
            };

            var outcome = await service.SearchAsync(request);

            Assert.True(outcome.IsValid);
            Assert.Equal(1, outcome.Result!.Items.First().OptionalTotal); // Sugar once
        }

        [Fact]
        public async Task EmptyOptionalPool_TreatsThresholdAsZero()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var service = CreateService(factory);

            var request = new RecipeIngredientSearchRequestDto
            {
                MandatoryIngredientIds = new() { 4 }, // Egg -> Cake + Omelette
                OptionalIngredientIds = new(),
                OptionalThreshold = 5 // ignored because optional pool is empty
            };

            var outcome = await service.SearchAsync(request);

            Assert.True(outcome.IsValid);
            Assert.Equal(2, outcome.Result!.TotalCount);
        }

        // ---------- Reject loudly ----------

        [Fact]
        public async Task EmptySearch_ReturnsInvalid()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var service = CreateService(factory);

            var outcome = await service.SearchAsync(new RecipeIngredientSearchRequestDto());

            Assert.False(outcome.IsValid);
            Assert.NotNull(outcome.Error);
        }

        [Fact]
        public async Task UnknownIngredientId_ReturnsInvalidWithOffendingId()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var service = CreateService(factory);

            var request = new RecipeIngredientSearchRequestDto
            {
                MandatoryIngredientIds = new() { 999 }, // not in master list
                OptionalIngredientIds = new(),
                OptionalThreshold = 0
            };

            var outcome = await service.SearchAsync(request);

            Assert.False(outcome.IsValid);
            Assert.Contains("999", outcome.Error);
        }

        [Fact]
        public async Task ThresholdOutOfRange_ReturnsInvalid()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var service = CreateService(factory);

            var request = new RecipeIngredientSearchRequestDto
            {
                MandatoryIngredientIds = new() { 1 },
                OptionalIngredientIds = new() { 2, 3 },
                OptionalThreshold = 5 // > optional pool size (2)
            };

            var outcome = await service.SearchAsync(request);

            Assert.False(outcome.IsValid);
            Assert.NotNull(outcome.Error);
        }

        // ---------- Pagination ----------

        [Fact]
        public async Task Pagination_LimitsAndReportsTotals()
        {
            using var factory = new RecipeIngredientSearchTestContextFactory();
            var service = CreateService(factory);

            // Mandatory Egg(4) -> Cake + Omelette (2 results); page size 1.
            var request = new RecipeIngredientSearchRequestDto
            {
                MandatoryIngredientIds = new() { 4 },
                OptionalIngredientIds = new(),
                OptionalThreshold = 0,
                Page = 1,
                PageSize = 1
            };

            var outcome = await service.SearchAsync(request);

            Assert.True(outcome.IsValid);
            Assert.Single(outcome.Result!.Items);
            Assert.Equal(2, outcome.Result.TotalCount);
            Assert.Equal(2, outcome.Result.TotalPages);
        }
    }
}