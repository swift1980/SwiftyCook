using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using swiftcookapi.Controllers;
using SwiftCookDb;
using SwiftCookDb.Models;
using Xunit;

namespace swiftcookapi.tests
{
    // Seed: recipes 10 Cake and 11 Cookie are cocktails (category 1); 12 Omelette is not.
    public class MealPlanTests : IDisposable
    {
        private static readonly DateOnly Monday = new(2026, 10, 5);
        private readonly RecipeIngredientSearchTestContextFactory _factory = new();

        public MealPlanTests()
        {
            using var ctx = _factory.CreateContext();
            ctx.Recipes.Find(12)!.Yield = 4;
            ctx.SaveChanges();
        }

        public void Dispose() => _factory.Dispose();

        private static MealPlanController Controller(SwiftCookDbContext ctx)
        {
            var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>()).CreateMapper();
            return new MealPlanController(ctx, mapper);
        }

        private static MealPlanEntryCreateDto Dto(int recipeId, MealType type, DateOnly? date = null, int? servings = null) =>
            new() { RecipeId = recipeId, MealType = type, Date = date ?? Monday, Servings = servings };

        [Fact]
        public async Task Create_DefaultsServingsToYieldAndAppendsSortOrder()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);

            var first = (await c.Create(Dto(12, MealType.Dinner))).Result as CreatedAtActionResult;
            var second = (await c.Create(Dto(12, MealType.Dinner))).Result as CreatedAtActionResult;

            var a = Assert.IsType<MealPlanEntryDto>(first!.Value);
            var b = Assert.IsType<MealPlanEntryDto>(second!.Value);
            Assert.Equal(4, a.Servings);
            Assert.Equal("Omelette", a.RecipeName);
            Assert.Equal("Dinner", a.MealType);
            Assert.Equal(0, a.SortOrder);
            Assert.Equal(1, b.SortOrder);
        }

        [Fact]
        public async Task Create_UsesServingsOne_WhenRecipeHasNoYield()
        {
            using var ctx = _factory.CreateContext();
            var result = await Controller(ctx).Create(Dto(11, MealType.Cocktail));

            var dto = Assert.IsType<MealPlanEntryDto>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
            Assert.Equal(1, dto.Servings);
        }

        [Fact]
        public async Task Create_RejectsCocktailOutsideCocktailRow()
        {
            using var ctx = _factory.CreateContext();
            var result = await Controller(ctx).Create(Dto(10, MealType.Dinner));

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(ctx.MealPlanEntries);
        }

        [Fact]
        public async Task Create_RejectsNonCocktailInCocktailRow()
        {
            using var ctx = _factory.CreateContext();
            var result = await Controller(ctx).Create(Dto(12, MealType.Cocktail));

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Create_RejectsUnknownRecipe()
        {
            using var ctx = _factory.CreateContext();
            var result = await Controller(ctx).Create(Dto(999, MealType.Dinner));

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetRange_ReturnsInclusiveRangeInOrder()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);
            await c.Create(Dto(12, MealType.Dinner, Monday.AddDays(1)));
            await c.Create(Dto(12, MealType.Breakfast, Monday));
            await c.Create(Dto(11, MealType.Cocktail, Monday));
            await c.Create(Dto(12, MealType.Lunch, Monday.AddDays(7)));

            var result = (await c.GetRange(Monday, Monday.AddDays(6))).Value!.ToList();

            Assert.Equal(
                new[] { "Breakfast", "Cocktail", "Dinner" },
                result.Select(e => e.MealType));
            Assert.Equal(new[] { Monday, Monday, Monday.AddDays(1) }, result.Select(e => e.Date));
        }

        [Fact]
        public async Task GetRange_RejectsBadRanges()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);

            Assert.IsType<BadRequestObjectResult>((await c.GetRange(Monday, Monday.AddDays(-1))).Result);
            Assert.IsType<BadRequestObjectResult>((await c.GetRange(Monday, Monday.AddDays(100))).Result);
            Assert.IsType<BadRequestObjectResult>((await c.GetRange(default, Monday)).Result);
        }

        [Fact]
        public async Task Update_MovesEntryAndRevalidatesMealType()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);
            var created = (MealPlanEntryDto)((CreatedAtActionResult)(await c.Create(Dto(12, MealType.Dinner))).Result!).Value!;

            var moved = await c.Update(created.Id, new MealPlanEntryUpdateDto
            {
                Date = Monday.AddDays(2), MealType = MealType.Lunch, RecipeId = 12, Servings = 2, SortOrder = 3
            });
            var dto = Assert.IsType<MealPlanEntryDto>(moved.Value);
            Assert.Equal(("Lunch", Monday.AddDays(2), 2, 3), (dto.MealType, dto.Date, dto.Servings, dto.SortOrder));

            var invalid = await c.Update(created.Id, new MealPlanEntryUpdateDto
            {
                Date = Monday, MealType = MealType.Cocktail, RecipeId = 12, Servings = 1
            });
            Assert.IsType<BadRequestObjectResult>(invalid.Result);
        }

        [Fact]
        public async Task Update_ReturnsNotFoundForMissingEntry()
        {
            using var ctx = _factory.CreateContext();
            var result = await Controller(ctx).Update(42, new MealPlanEntryUpdateDto
            {
                Date = Monday, MealType = MealType.Dinner, RecipeId = 12
            });

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Delete_RemovesEntry()
        {
            using var ctx = _factory.CreateContext();
            var c = Controller(ctx);
            var created = (MealPlanEntryDto)((CreatedAtActionResult)(await c.Create(Dto(12, MealType.Dinner))).Result!).Value!;

            Assert.IsType<NoContentResult>(await c.Delete(created.Id));
            Assert.Empty(ctx.MealPlanEntries);
            Assert.IsType<NotFoundResult>(await c.Delete(created.Id));
        }

        [Fact]
        public async Task DeleteRecipe_IsBlockedWhilePlanned()
        {
            using var ctx = _factory.CreateContext();
            await Controller(ctx).Create(Dto(12, MealType.Dinner));
            var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>()).CreateMapper();
            var recipes = new RecipeController(ctx, mapper, null!);

            var result = await recipes.Delete(12);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(await ctx.Recipes.FindAsync(12));
        }

        [Fact]
        public void Dtos_RejectUndefinedMealType()
        {
            var create = new MealPlanEntryCreateDto { Date = Monday, MealType = (MealType)99, RecipeId = 12 };
            var update = new MealPlanEntryUpdateDto { Date = Monday, MealType = (MealType)99, RecipeId = 12 };

            Assert.False(System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                create, new(create), new List<System.ComponentModel.DataAnnotations.ValidationResult>(), true));
            Assert.False(System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                update, new(update), new List<System.ComponentModel.DataAnnotations.ValidationResult>(), true));
        }

        [Fact]
        public async Task DeleteRecipe_SucceedsWhenNotPlanned()
        {
            using var ctx = _factory.CreateContext();
            var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>()).CreateMapper();

            var result = await new RecipeController(ctx, mapper, null!).Delete(12);

            Assert.IsType<NoContentResult>(result);
        }
    }
}
