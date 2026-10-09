using Microsoft.EntityFrameworkCore;
using swiftcookapi.Services;
using SwiftCookDb;
using SwiftCookDb.Models;
using Xunit;

namespace swiftcookapi.tests
{
    public class MealPlanShoppingTests : IDisposable
    {
        private const int Gram = 10, Kilo = 11, Sgl = 14, Millilitre = 15;
        private static readonly DateOnly Today = new(2026, 10, 7);
        private static readonly DateOnly Tomorrow = new(2026, 10, 8);
        private static readonly DateOnly Yesterday = new(2026, 10, 6);
        private readonly RecipeIngredientSearchTestContextFactory _factory = new();

        public MealPlanShoppingTests()
        {
            using var ctx = _factory.CreateContext();
            ctx.Units.AddRange(
                new Unit { Id = Gram, Name = "gram", Dimension = UnitDimension.Mass, ToBaseFactor = 1 },
                new Unit { Id = Kilo, Name = "kilogram", Dimension = UnitDimension.Mass, ToBaseFactor = 1000 },
                new Unit { Id = Sgl, Name = "sgl", Dimension = UnitDimension.Count },
                new Unit { Id = Millilitre, Name = "millilitre", Dimension = UnitDimension.Volume, ToBaseFactor = 1 });
            // Recipe 20 yields 4; recipe 21 has no yield.
            ctx.Recipes.AddRange(new Recipe { Id = 20, Name = "Pancakes", Yield = 4 }, new Recipe { Id = 21, Name = "Toast" });
            ctx.SaveChanges();
        }

        public void Dispose() => _factory.Dispose();

        private void Line(int recipeId, int ingredientId, int unitId, decimal? amount)
        {
            using var ctx = _factory.CreateContext();
            ctx.RecipeIngredients.Add(new RecipeIngredient { RecipeId = recipeId, IngredientId = ingredientId, UnitId = unitId, Amount = amount });
            ctx.SaveChanges();
        }

        private void Plan(int recipeId, DateOnly date, int servings, MealType type = MealType.Dinner)
        {
            using var ctx = _factory.CreateContext();
            ctx.MealPlanEntries.Add(new MealPlanEntry { RecipeId = recipeId, Date = date, Servings = servings, MealType = type });
            ctx.SaveChanges();
        }

        private void Stock(int ingredientId, int unitId, decimal? amount)
        {
            using var ctx = _factory.CreateContext();
            ctx.Cupboards.Add(new Cupboard { IngredientId = ingredientId, UnitId = unitId, Amount = amount });
            ctx.SaveChanges();
        }

        private void Listed(int ingredientId, int unitId, decimal? amount)
        {
            using var ctx = _factory.CreateContext();
            ctx.ShoppingLists.Add(new ShoppingList { IngredientId = ingredientId, UnitId = unitId, Amount = amount });
            ctx.SaveChanges();
        }

        private async Task<List<ShortageLine>> Calc(DateOnly? to = null)
        {
            using var ctx = _factory.CreateContext();
            return await new MealPlanShoppingService(ctx, new UnitConverter()).CalculateAsync(Yesterday, to ?? Tomorrow.AddDays(5), Today);
        }

        [Fact]
        public async Task ScalesByServingsOverYield()
        {
            Line(20, 1, Gram, 200); // for 4 servings
            Plan(20, Tomorrow, 2);

            var lines = await Calc();

            var line = Assert.Single(lines);
            Assert.Equal(100m, line.Amount);
            Assert.Equal(Gram, line.UnitId);
        }

        [Fact]
        public async Task MissingYieldCountsAsOne()
        {
            Line(21, 1, Gram, 50);
            Plan(21, Tomorrow, 3);

            Assert.Equal(150m, Assert.Single(await Calc()).Amount);
        }

        [Fact]
        public async Task SumsAcrossRecipesAndConvertsWithinDimension()
        {
            Line(20, 1, Gram, 400);
            Line(21, 1, Kilo, 1);
            Plan(20, Tomorrow, 4);
            Plan(21, Tomorrow.AddDays(1), 1);

            var line = Assert.Single(await Calc());

            Assert.Equal(1400m, line.Amount);
            Assert.Equal(Gram, line.UnitId);
        }

        [Fact]
        public async Task ExcludesPastDatesAndDatesBeyondRange()
        {
            Line(20, 1, Gram, 400);
            Plan(20, Yesterday, 4);
            Plan(20, Tomorrow.AddDays(30), 4);

            Assert.Empty(await Calc());
        }

        [Fact]
        public async Task IncludesToday()
        {
            Line(20, 1, Gram, 400);
            Plan(20, Today, 4);

            Assert.Single(await Calc());
        }

        [Fact]
        public async Task ExcludesStaples()
        {
            using (var ctx = _factory.CreateContext())
            {
                ctx.Ingredients.Single(i => i.Id == 5).IsStaple = true;
                ctx.SaveChanges();
            }
            Line(20, 5, Gram, 5);
            Plan(20, Tomorrow, 4);

            Assert.Empty(await Calc());
        }

        [Fact]
        public async Task SubtractsCupboardAndExistingShoppingList()
        {
            Line(20, 1, Gram, 1000);
            Plan(20, Tomorrow, 4);
            Stock(1, Kilo, 0.4m); // 400 g
            Listed(1, Gram, 100);

            Assert.Equal(500m, Assert.Single(await Calc()).Amount);
        }

        [Fact]
        public async Task NothingWhenCovered()
        {
            Line(20, 1, Gram, 500);
            Plan(20, Tomorrow, 4);
            Stock(1, Gram, 500);

            Assert.Empty(await Calc());
        }

        [Fact]
        public async Task IncomparableCupboardUnitAddsFullAmountFlagged()
        {
            Line(20, 1, Gram, 500);
            Plan(20, Tomorrow, 4);
            Stock(1, Millilitre, 900);

            var line = Assert.Single(await Calc());

            Assert.Equal(500m, line.Amount);
            Assert.True(line.UnitMismatch);
        }

        [Fact]
        public async Task UnknownCupboardAmountDoesNotCoverAnAmount()
        {
            Line(20, 1, Gram, 500);
            Plan(20, Tomorrow, 4);
            Stock(1, Gram, null);

            var line = Assert.Single(await Calc());

            Assert.Equal(500m, line.Amount);
            Assert.False(line.UnitMismatch);
        }

        [Fact]
        public async Task NoAmountLineOnlyListedWhenIngredientAbsentEverywhere()
        {
            Line(20, 5, Sgl, null);
            Line(20, 6, Sgl, null);
            Plan(20, Tomorrow, 4);
            Stock(6, Sgl, null);

            var line = Assert.Single(await Calc());

            Assert.Equal(5, line.IngredientId);
            Assert.Null(line.Amount);
            Assert.Equal(Sgl, line.UnitId);
        }

        [Fact]
        public async Task CountUnitAmountsAreSummed()
        {
            Line(20, 4, Sgl, 4); // 4 eggs for 4 servings
            Plan(20, Tomorrow, 8);

            var line = Assert.Single(await Calc());

            Assert.Equal(8m, line.Amount);
            Assert.Equal(Sgl, line.UnitId);
        }

        [Fact]
        public async Task AddingTwiceIsIdempotent()
        {
            Line(20, 1, Gram, 500);
            Line(20, 5, Sgl, null);
            Plan(20, Tomorrow, 4);

            for (var i = 0; i < 2; i++)
            {
                using var ctx = _factory.CreateContext();
                await new MealPlanShoppingService(ctx, new UnitConverter()).AddToShoppingListAsync(Yesterday, Tomorrow.AddDays(5), Today);
            }

            using var check = _factory.CreateContext();
            var rows = await check.ShoppingLists.ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.All(rows, r => Assert.Equal(MealPlanShoppingService.SourceLabel, r.Source));
        }

        [Fact]
        public async Task MismatchRowsAreMarkedInSource()
        {
            Line(20, 1, Gram, 500);
            Plan(20, Tomorrow, 4);
            Stock(1, Millilitre, 100);

            using var ctx = _factory.CreateContext();
            await new MealPlanShoppingService(ctx, new UnitConverter()).AddToShoppingListAsync(Yesterday, Tomorrow.AddDays(5), Today);

            using var check = _factory.CreateContext();
            Assert.Equal(MealPlanShoppingService.MismatchSourceLabel, Assert.Single(await check.ShoppingLists.ToListAsync()).Source);
        }
    }
}
