using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using swiftcookapi.Controllers;
using SwiftCookDb.Models;
using swiftcookapi.Services;
using Xunit;

namespace swiftcookapi.tests
{
    public class RecipeIngredientNoteTests : IDisposable
    {
        private readonly RecipeIngredientSearchTestContextFactory _factory = new();

        public void Dispose() => _factory.Dispose();

        [Fact]
        public async Task Create_StoresTrimmedNote_BlankBecomesNull_AndReadDtoReturnsIt()
        {
            using var ctx = _factory.CreateContext();
            var mapper = new MapperConfiguration(c => c.AddProfile<MappingProfile>()).CreateMapper();
            var controller = new RecipeController(ctx, mapper, new RecipeIngredientSearchService(ctx));

            var result = await controller.CreateRecipe(new RecipeCreateDto
            {
                Name = "Salad",
                Ingredients =
                {
                    new RecipeIngredientCreateDto { IngredientId = 5, UnitId = 1, Amount = null, Note = "  to taste " },
                    new RecipeIngredientCreateDto { IngredientId = 1, UnitId = 1, Amount = 2, Note = "   " },
                }
            });

            var read = Assert.IsType<RecipeReadDto>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
            var salt = Assert.Single(read.Ingredients, i => i.IngredientId == 5);
            Assert.Equal("to taste", salt.Note);
            Assert.Null(salt.Amount);
            Assert.Null(Assert.Single(read.Ingredients, i => i.IngredientId == 1).Note);
        }
    }
}
