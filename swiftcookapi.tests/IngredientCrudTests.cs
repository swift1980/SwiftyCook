using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SwiftCookDb;
using SwiftCookDb.Models;
using swiftcookapi.Controllers;
using Xunit;

namespace swiftcookapi.tests
{
    public class IngredientCrudTests : IDisposable
    {
        private readonly RecipeIngredientSearchTestContextFactory _factory = new();
        private readonly IMapper _mapper =
            new MapperConfiguration(c => c.AddProfile<MappingProfile>()).CreateMapper();

        public void Dispose() => _factory.Dispose();

        private IngredientController Ingredients(SwiftCookDbContext ctx) => new(ctx, _mapper);
        private IngredientTypeController Types(SwiftCookDbContext ctx) => new(ctx, _mapper);

        [Fact]
        public async Task Create_RejectsCaseInsensitiveDuplicateName()
        {
            using var ctx = _factory.CreateContext();
            var result = await Ingredients(ctx).Create(new IngredientCreateDto { Name = "  flour " });
            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task Create_PersistsIsStaple()
        {
            using var ctx = _factory.CreateContext();
            var result = await Ingredients(ctx).Create(new IngredientCreateDto { Name = "Water", IsStaple = true });
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.True(Assert.IsType<IngredientDto>(created.Value).IsStaple);
        }

        [Fact]
        public async Task Update_RejectsDuplicateOfAnotherIngredientButAllowsOwnName()
        {
            using var ctx = _factory.CreateContext();
            var controller = Ingredients(ctx);
            Assert.IsType<ConflictObjectResult>(
                await controller.Update(2, new IngredientCreateDto { Name = "FLOUR" }));
            Assert.IsType<NoContentResult>(
                await controller.Update(2, new IngredientCreateDto { Name = "sugar", IsStaple = true }));
            Assert.True(ctx.Ingredients.Find(2)!.IsStaple);
        }

        [Fact]
        public async Task Delete_BlockedWhenUsedInRecipes()
        {
            using var ctx = _factory.CreateContext();
            var result = await Ingredients(ctx).Delete(1);
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Contains("recipe", conflict.Value!.ToString());
        }

        [Fact]
        public async Task Delete_BlockedWhenInCupboardOrOnShoppingList()
        {
            using var ctx = _factory.CreateContext();
            ctx.Cupboards.Add(new Cupboard { IngredientId = 6, UnitId = 1, Amount = 1 });
            ctx.ShoppingLists.Add(new ShoppingList { IngredientId = 6, UnitId = 1, Amount = 1 });
            await ctx.SaveChangesAsync();

            var conflict = Assert.IsType<ConflictObjectResult>(await Ingredients(ctx).Delete(6));
            Assert.Contains("cupboard", conflict.Value!.ToString());
            Assert.Contains("shopping list", conflict.Value!.ToString());
        }

        [Fact]
        public async Task Delete_SucceedsWhenUnused()
        {
            using var ctx = _factory.CreateContext();
            Assert.IsType<NoContentResult>(await Ingredients(ctx).Delete(6));
            Assert.Null(ctx.Ingredients.Find(6));
        }

        [Fact]
        public async Task DeleteType_BlockedWhenIngredientsUseIt()
        {
            using var ctx = _factory.CreateContext();
            ctx.IngredientCategories.Add(new IngredientCategory { Id = 1, Name = "Pantry" });
            ctx.IngredientTypes.Add(new IngredientType { Id = 1, Name = "Flour", CategoryId = 1 });
            ctx.Ingredients.Find(1)!.TypeId = 1;
            await ctx.SaveChangesAsync();

            Assert.IsType<ConflictObjectResult>(await Types(ctx).Delete(1));
        }

        [Fact]
        public async Task DeleteType_BlockedWhenCategoryFallback()
        {
            using var ctx = _factory.CreateContext();
            ctx.IngredientCategories.Add(new IngredientCategory { Id = 1, Name = "Pantry" });
            ctx.IngredientTypes.Add(new IngredientType { Id = 1, Name = "Other", CategoryId = 1 });
            await ctx.SaveChangesAsync();
            ctx.IngredientCategories.Find(1)!.FallbackTypeId = 1;
            await ctx.SaveChangesAsync();

            Assert.IsType<ConflictObjectResult>(await Types(ctx).Delete(1));
        }

        [Fact]
        public async Task DeleteType_SucceedsWhenUnused()
        {
            using var ctx = _factory.CreateContext();
            ctx.IngredientCategories.Add(new IngredientCategory { Id = 1, Name = "Pantry" });
            ctx.IngredientTypes.Add(new IngredientType { Id = 1, Name = "Spare", CategoryId = 1 });
            await ctx.SaveChangesAsync();

            Assert.IsType<NoContentResult>(await Types(ctx).Delete(1));
        }
    }
}
