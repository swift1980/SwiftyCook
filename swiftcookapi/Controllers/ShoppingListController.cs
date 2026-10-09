using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;
using swiftcookapi.Services;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingListController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;
        private readonly CupboardStockService _stock;
        private readonly MealPlanShoppingService _mealPlanShopping;

        public ShoppingListController(SwiftCookDbContext context, IMapper mapper, CupboardStockService stock, MealPlanShoppingService mealPlanShopping)
        {
            _context = context;
            _mapper = mapper;
            _stock = stock;
            _mealPlanShopping = mealPlanShopping;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShoppingListDto>>> GetAll()
        {
            var items = await _context.ShoppingLists
                .Include(s => s.Ingredient)
                .Include(s => s.Unit)
                .ToListAsync();

            return _mapper.Map<List<ShoppingListDto>>(items);
        }

        [HttpPost]
        public async Task<ActionResult<ShoppingListDto>> Create(ShoppingListCreateDto dto)
        {
            var item = _mapper.Map<ShoppingList>(dto);
            _context.ShoppingLists.Add(item);
            await _context.SaveChangesAsync();

            var created = await _context.ShoppingLists
                .Include(s => s.Ingredient)
                .Include(s => s.Unit)
                .FirstOrDefaultAsync(s => s.Id == item.Id);

            return CreatedAtAction(nameof(GetAll), new { id = item.Id }, _mapper.Map<ShoppingListDto>(created));
        }

        /// <summary>
        /// Moves the confirmed shopping list rows to the Cupboard (using the Cupboard merge rules) and
        /// removes them from the list. All or nothing: any failure leaves both unchanged.
        /// </summary>
        [HttpPost("purchase")]
        public async Task<IActionResult> Purchase(ShoppingListPurchaseDto dto)
        {
            var ids = dto.ItemIds.Distinct().ToList();
            var items = await _context.ShoppingLists
                .Include(s => s.Unit)
                .Where(s => ids.Contains(s.Id))
                .OrderBy(s => s.Id)
                .ToListAsync();

            if (items.Count != ids.Count)
                return Conflict(new { message = "The shopping list has changed. Reload it and try again." });
            if (items.Any(i => i.Amount < 0))
                return BadRequest(new { message = "Cannot purchase an item with a negative amount." });

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in items)
                    await _stock.AddStockAsync(item.IngredientId, item.Unit, item.Amount);

                _context.ShoppingLists.RemoveRange(items);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // A row was removed by another request mid-purchase; the transaction rolls back.
                return Conflict(new { message = "The shopping list has changed. Reload it and try again." });
            }

            return NoContent();
        }

        /// <summary>
        /// Adds what is missing for the planned meals in the date range (see <see cref="MealPlanShoppingService"/>).
        /// With dryRun it only previews. The shortage is always recalculated here, never taken from the client.
        /// </summary>
        [HttpPost("from-mealplan")]
        public async Task<ActionResult<IEnumerable<ShoppingListGenerationLineDto>>> FromMealPlan(ShoppingListFromMealPlanDto dto)
        {
            var from = dto.From!.Value;
            var to = dto.To!.Value;
            if (to < from)
                return BadRequest(new { message = "'to' cannot be before 'from'." });
            if (to.DayNumber - from.DayNumber >= MealPlanController.MaxRangeDays)
                return BadRequest(new { message = $"The range cannot exceed {MealPlanController.MaxRangeDays} days." });

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var lines = dto.DryRun
                ? await _mealPlanShopping.CalculateAsync(from, to, today)
                : await _mealPlanShopping.AddToShoppingListAsync(from, to, today);

            return lines.Select(l => new ShoppingListGenerationLineDto
            {
                IngredientId = l.IngredientId,
                IngredientName = l.IngredientName,
                UnitId = l.UnitId,
                UnitName = l.UnitName,
                Amount = l.Amount,
                UnitMismatch = l.UnitMismatch,
            }).ToList();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.ShoppingLists.FindAsync(id);
            if (item == null) return NotFound();
            _context.ShoppingLists.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
