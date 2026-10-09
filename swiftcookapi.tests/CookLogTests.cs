using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using swiftcookapi.Controllers;
using SwiftCookDb;
using SwiftCookDb.Models;
using Xunit;

namespace swiftcookapi.tests
{
    public class CookLogTests : IDisposable
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
        private readonly RecipeIngredientSearchTestContextFactory _factory = new();

        public void Dispose() => _factory.Dispose();

        private static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<MappingProfile>()).CreateMapper();
        private static CookLogController Logs(SwiftCookDbContext ctx) => new(ctx, Mapper());
        private static MealPlanController Plan(SwiftCookDbContext ctx) => new(ctx, Mapper());

        private async Task<int> PlannedEntry(DateOnly date, int servings = 3)
        {
            using var ctx = _factory.CreateContext();
            var created = await Plan(ctx).Create(new MealPlanEntryCreateDto { RecipeId = 12, MealType = MealType.Dinner, Date = date, Servings = servings });
            return Assert.IsType<MealPlanEntryDto>(Assert.IsType<CreatedAtActionResult>(created.Result).Value).Id;
        }

        [Fact]
        public async Task LogAsMade_CreatesRow_AndListIsNewestFirst()
        {
            using var ctx = _factory.CreateContext();
            var c = Logs(ctx);
            await c.Create(new CookLogCreateDto { RecipeId = 12, CookedOn = Today.AddDays(-10), Servings = 2, Notes = "  nice " });
            await c.Create(new CookLogCreateDto { RecipeId = 12, CookedOn = Today.AddDays(-2), Servings = 4, Notes = "  " });
            await c.Create(new CookLogCreateDto { RecipeId = 10, CookedOn = Today, Servings = 1 });

            var list = (await c.GetForRecipe(12)).Value!.ToList();
            Assert.Equal(new[] { Today.AddDays(-2), Today.AddDays(-10) }, list.Select(l => l.CookedOn));
            Assert.Null(list[0].Notes);
            Assert.Equal("nice", list[1].Notes);
        }

        [Fact]
        public async Task Create_RejectsUnknownRecipe_AndFutureDates()
        {
            using var ctx = _factory.CreateContext();
            var c = Logs(ctx);
            Assert.IsType<BadRequestObjectResult>((await c.Create(new CookLogCreateDto { RecipeId = 999, CookedOn = Today })).Result);
            Assert.IsType<BadRequestObjectResult>((await c.Create(new CookLogCreateDto { RecipeId = 12, CookedOn = Today.AddDays(5) })).Result);
            Assert.IsType<BadRequestObjectResult>((await c.GetForRecipe(0)).Result);
            Assert.Empty(ctx.CookLogs);
        }

        [Fact]
        public async Task Update_ChangesFields_AndDeleteRemovesRow()
        {
            using var ctx = _factory.CreateContext();
            var c = Logs(ctx);
            var id = Assert.IsType<CookLogDto>(Assert.IsType<CreatedAtActionResult>((await c.Create(new CookLogCreateDto { RecipeId = 12, CookedOn = Today, Servings = 2 })).Result).Value).Id;

            var updated = (await c.Update(id, new CookLogUpdateDto { CookedOn = Today.AddDays(-1), Servings = 6, Notes = "x" })).Value!;
            Assert.Equal(6, updated.Servings);
            Assert.Equal(Today.AddDays(-1), updated.CookedOn);
            Assert.IsType<NotFoundResult>((await c.Update(999, new CookLogUpdateDto { CookedOn = Today })).Result);

            Assert.IsType<NoContentResult>(await c.Delete(id));
            Assert.IsType<NotFoundResult>(await c.Delete(id));
        }

        [Fact]
        public async Task MarkMade_CreatesLinkedLogWithPlannedDateAndServings()
        {
            var entryId = await PlannedEntry(Today.AddDays(-1), 3);

            using var ctx = _factory.CreateContext();
            var result = (await Plan(ctx).MarkMade(entryId)).Value!;
            Assert.NotNull(result.CookLogId);

            using var check = _factory.CreateContext();
            var log = await check.CookLogs.SingleAsync();
            Assert.Equal(result.CookLogId, log.Id);
            Assert.Equal(12, log.RecipeId);
            Assert.Equal(Today.AddDays(-1), log.CookedOn);
            Assert.Equal(3, log.Servings);
        }

        [Fact]
        public async Task MarkMade_Twice_ConflictsAndDoesNotDuplicate()
        {
            var entryId = await PlannedEntry(Today);
            using (var ctx = _factory.CreateContext()) await Plan(ctx).MarkMade(entryId);

            using var ctx2 = _factory.CreateContext();
            Assert.IsType<ConflictObjectResult>((await Plan(ctx2).MarkMade(entryId)).Result);
            Assert.Equal(1, await ctx2.CookLogs.CountAsync());
        }

        [Fact]
        public async Task MarkMade_FutureEntryOrMissing_IsRejected()
        {
            var entryId = await PlannedEntry(Today.AddDays(7));
            using var ctx = _factory.CreateContext();
            Assert.IsType<BadRequestObjectResult>((await Plan(ctx).MarkMade(entryId)).Result);
            Assert.IsType<NotFoundResult>((await Plan(ctx).MarkMade(999)).Result);
            Assert.Empty(ctx.CookLogs);
        }

        [Fact]
        public async Task Undo_RemovesLinkAndRow_AndRequiresMade()
        {
            var entryId = await PlannedEntry(Today);
            using (var ctx = _factory.CreateContext()) await Plan(ctx).MarkMade(entryId);

            using var ctx2 = _factory.CreateContext();
            var undone = (await Plan(ctx2).UndoMade(entryId)).Value!;
            Assert.Null(undone.CookLogId);

            using var check = _factory.CreateContext();
            Assert.Empty(check.CookLogs);
            Assert.Null((await check.MealPlanEntries.FindAsync(entryId))!.CookLogId);
            Assert.IsType<ConflictObjectResult>((await Plan(check).UndoMade(entryId)).Result);
        }

        [Fact]
        public async Task DeletingLog_UnmarksLinkedEntry()
        {
            var entryId = await PlannedEntry(Today);
            int logId;
            using (var ctx = _factory.CreateContext()) logId = (await Plan(ctx).MarkMade(entryId)).Value!.CookLogId!.Value;

            using var ctx2 = _factory.CreateContext();
            await Logs(ctx2).Delete(logId);

            using var check = _factory.CreateContext();
            Assert.Null((await check.MealPlanEntries.FindAsync(entryId))!.CookLogId);
        }

        [Fact]
        public async Task RemovingMadeEntry_KeepsTheLog()
        {
            var entryId = await PlannedEntry(Today);
            using (var ctx = _factory.CreateContext()) await Plan(ctx).MarkMade(entryId);

            using var ctx2 = _factory.CreateContext();
            await Plan(ctx2).Delete(entryId);

            using var check = _factory.CreateContext();
            Assert.Equal(1, await check.CookLogs.CountAsync());
        }

        [Fact]
        public async Task ChangingRecipeOfMadeEntry_IsRejected_ButServingsCanChange()
        {
            var entryId = await PlannedEntry(Today);
            using (var ctx = _factory.CreateContext()) await Plan(ctx).MarkMade(entryId);

            using var ctx2 = _factory.CreateContext();
            var c = Plan(ctx2);
            var bad = await c.Update(entryId, new MealPlanEntryUpdateDto { Date = Today, MealType = MealType.Dinner, RecipeId = 10, Servings = 2, SortOrder = 0 });
            Assert.IsType<BadRequestObjectResult>(bad.Result);

            var ok = await c.Update(entryId, new MealPlanEntryUpdateDto { Date = Today, MealType = MealType.Dinner, RecipeId = 12, Servings = 5, SortOrder = 0 });
            Assert.Equal(5, ok.Value!.Servings);
            Assert.NotNull(ok.Value.CookLogId);
        }
    }
}
