using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using swiftcookapi.Controllers;
using swiftcookapi.Services;
using SwiftCookDb;
using SwiftCookDb.Models;
using Xunit;

namespace swiftcookapi.tests
{
    public class ShoppingListPurchaseTests : IDisposable
    {
        private const int Gram = 10, Kilo = 11, Piece = 13;
        private readonly RecipeIngredientSearchTestContextFactory _factory = new();

        public ShoppingListPurchaseTests()
        {
            using var ctx = _factory.CreateContext();
            ctx.Units.AddRange(
                new Unit { Id = Gram, Name = "gram", Dimension = UnitDimension.Mass, ToBaseFactor = 1 },
                new Unit { Id = Kilo, Name = "kilogram", Dimension = UnitDimension.Mass, ToBaseFactor = 1000 },
                new Unit { Id = Piece, Name = "piece", Dimension = UnitDimension.Count });
            ctx.SaveChanges();
        }

        public void Dispose() => _factory.Dispose();

        private static ShoppingListController Controller(SwiftCookDbContext ctx, CupboardStockService? stock = null)
        {
            var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>()).CreateMapper();
            return new ShoppingListController(ctx, mapper, stock ?? new CupboardStockService(ctx, new UnitConverter()),
                new MealPlanShoppingService(ctx, new UnitConverter()));
        }

        private int AddItem(int ingredientId, int unitId, decimal? amount)
        {
            using var ctx = _factory.CreateContext();
            var item = new ShoppingList { IngredientId = ingredientId, UnitId = unitId, Amount = amount };
            ctx.ShoppingLists.Add(item);
            ctx.SaveChanges();
            return item.Id;
        }

        private sealed class FailingOnSecondCall : CupboardStockService
        {
            private int _calls;
            public FailingOnSecondCall(SwiftCookDbContext ctx) : base(ctx, new UnitConverter()) { }

            public override async Task<Cupboard> AddStockAsync(int ingredientId, Unit unit, decimal? amount)
            {
                var result = await base.AddStockAsync(ingredientId, unit, amount);
                if (++_calls == 2) throw new InvalidOperationException("boom");
                return result;
            }
        }

        [Fact]
        public async Task Purchase_MovesItemsToCupboardAndClearsThem()
        {
            var a = AddItem(1, Gram, 200);
            var b = AddItem(2, Piece, 3);
            using var ctx = _factory.CreateContext();

            var result = await Controller(ctx).Purchase(new ShoppingListPurchaseDto { ItemIds = { a, b } });

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(ctx.ShoppingLists);
            var rows = await ctx.Cupboards.OrderBy(c => c.IngredientId).ToListAsync();
            Assert.Equal(new[] { (1, Gram, 200m), (2, Piece, 3m) }, rows.Select(r => (r.IngredientId, r.UnitId, r.Amount!.Value)));
        }

        [Fact]
        public async Task Purchase_UsesCupboardMergeRules()
        {
            using (var seed = _factory.CreateContext())
            {
                seed.Cupboards.Add(new Cupboard { IngredientId = 1, UnitId = Gram, Amount = 500 });
                seed.SaveChanges();
            }
            var kilo = AddItem(1, Kilo, 1);
            var sameIngredientAgain = AddItem(1, Gram, 250);
            var other = AddItem(1, Piece, 2);
            using var ctx = _factory.CreateContext();

            await Controller(ctx).Purchase(new ShoppingListPurchaseDto { ItemIds = { kilo, sameIngredientAgain, other } });

            var rows = await ctx.Cupboards.OrderBy(c => c.UnitId).ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.Equal((Kilo, 1.75m), (rows[0].UnitId, rows[0].Amount!.Value));
            Assert.Equal((Piece, 2m), (rows[1].UnitId, rows[1].Amount!.Value));
        }

        [Fact]
        public async Task Purchase_KeepsNoAmountItemsWithUnknownAmount()
        {
            var id = AddItem(5, Piece, null);
            using var ctx = _factory.CreateContext();

            await Controller(ctx).Purchase(new ShoppingListPurchaseDto { ItemIds = { id } });

            var row = Assert.Single(await ctx.Cupboards.ToListAsync());
            Assert.Null(row.Amount);
        }

        [Fact]
        public async Task Purchase_OnlyMovesConfirmedItems()
        {
            var a = AddItem(1, Gram, 100);
            AddItem(2, Gram, 100);
            using var ctx = _factory.CreateContext();

            await Controller(ctx).Purchase(new ShoppingListPurchaseDto { ItemIds = { a } });

            Assert.Equal(2, Assert.Single(ctx.ShoppingLists).IngredientId);
            Assert.Equal(1, Assert.Single(ctx.Cupboards).IngredientId);
        }

        [Fact]
        public async Task Purchase_ConflictsWhenAnItemIsGone_AndChangesNothing()
        {
            var a = AddItem(1, Gram, 100);
            using var ctx = _factory.CreateContext();

            var result = await Controller(ctx).Purchase(new ShoppingListPurchaseDto { ItemIds = { a, 999 } });

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Single(ctx.ShoppingLists);
            Assert.Empty(ctx.Cupboards);
        }

        [Fact]
        public async Task Purchase_RollsBackEverythingOnFailure()
        {
            var a = AddItem(1, Gram, 100);
            var b = AddItem(2, Gram, 100);
            using var ctx = _factory.CreateContext();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Controller(ctx, new FailingOnSecondCall(ctx)).Purchase(new ShoppingListPurchaseDto { ItemIds = { a, b } }));

            using var check = _factory.CreateContext();
            Assert.Equal(2, await check.ShoppingLists.CountAsync());
            Assert.Empty(check.Cupboards);
        }

        [Fact]
        public async Task Purchase_RejectsNegativeAmounts()
        {
            var a = AddItem(1, Gram, -5);
            using var ctx = _factory.CreateContext();

            var result = await Controller(ctx).Purchase(new ShoppingListPurchaseDto { ItemIds = { a } });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Single(ctx.ShoppingLists);
        }
    }
}
