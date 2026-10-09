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
    public class CupboardTests : IDisposable
    {
        private const int Gram = 10, Kilo = 11, Millilitre = 12, Piece = 13;
        private readonly RecipeIngredientSearchTestContextFactory _factory = new();

        public CupboardTests()
        {
            using var ctx = _factory.CreateContext();
            ctx.Units.AddRange(
                new Unit { Id = Gram, Name = "gram", Dimension = UnitDimension.Mass, ToBaseFactor = 1 },
                new Unit { Id = Kilo, Name = "kilogram", Dimension = UnitDimension.Mass, ToBaseFactor = 1000 },
                new Unit { Id = Millilitre, Name = "millilitre", Dimension = UnitDimension.Volume, ToBaseFactor = 1 },
                new Unit { Id = Piece, Name = "piece", Dimension = UnitDimension.Count });
            ctx.SaveChanges();
        }

        public void Dispose() => _factory.Dispose();

        private static CupboardController Controller(SwiftCookDbContext ctx)
        {
            var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>()).CreateMapper();
            return new CupboardController(ctx, mapper, new UnitConverter());
        }

        private static CupboardCreateDto Dto(int unitId, decimal? amount, int ingredientId = 1) =>
            new() { IngredientId = ingredientId, UnitId = unitId, Amount = amount };

        [Fact]
        public async Task SameUnit_SumsAmounts()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);
            await c.Create(Dto(Gram, 200));
            await c.Create(Dto(Gram, 300));

            var row = Assert.Single(await ctx.Cupboards.ToListAsync());
            Assert.Equal(500m, row.Amount);
        }

        [Fact]
        public async Task SameDimension_MergesIntoLargerUnit()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);
            await c.Create(Dto(Gram, 500));
            await c.Create(Dto(Kilo, 1));

            var row = Assert.Single(await ctx.Cupboards.ToListAsync());
            Assert.Equal(Kilo, row.UnitId);
            Assert.Equal(1.5m, row.Amount);
        }

        [Fact]
        public async Task SmallAdditionToLargeUnit_StaysInLargeUnit()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);
            await c.Create(Dto(Kilo, 2));
            await c.Create(Dto(Gram, 500));

            var row = Assert.Single(await ctx.Cupboards.ToListAsync());
            Assert.Equal(Kilo, row.UnitId);
            Assert.Equal(2.5m, row.Amount);
        }

        [Fact]
        public async Task IncomparableUnits_GetSeparateRows()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);
            await c.Create(Dto(Gram, 200));
            await c.Create(Dto(Millilitre, 100));
            await c.Create(Dto(Piece, 3));

            Assert.Equal(3, await ctx.Cupboards.CountAsync());
        }

        [Fact]
        public async Task SameUnitWithUnknownAmount_StaysUnknown()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);
            await c.Create(Dto(Gram, 200));
            await c.Create(Dto(Gram, null));

            var row = Assert.Single(await ctx.Cupboards.ToListAsync());
            Assert.Null(row.Amount);
        }

        [Fact]
        public async Task UnknownAmountInOtherUnit_GetsOwnRow()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);
            await c.Create(Dto(Gram, 200));
            await c.Create(Dto(Kilo, null));

            Assert.Equal(2, await ctx.Cupboards.CountAsync());
        }

        [Fact]
        public async Task Create_RejectsNegativeAmountAndUnknownReferences()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);
            Assert.IsType<BadRequestObjectResult>((await c.Create(Dto(Gram, -1))).Result);
            Assert.IsType<BadRequestObjectResult>((await c.Create(Dto(999, 1))).Result);
            Assert.IsType<BadRequestObjectResult>((await c.Create(Dto(Gram, 1, 999))).Result);
        }

        [Fact]
        public async Task Delete_RemovesOnlyTheGivenUnitRow()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);
            await c.Create(Dto(Gram, 200));
            await c.Create(Dto(Millilitre, 100));

            Assert.IsType<NoContentResult>(await c.Delete(1, Gram));
            Assert.IsType<NotFoundResult>(await c.Delete(1, Gram));
            Assert.Equal(Millilitre, Assert.Single(await ctx.Cupboards.ToListAsync()).UnitId);
        }
    }
}
