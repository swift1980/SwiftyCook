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
    public class CupboardController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;

        private readonly CupboardStockService _stock;

        public CupboardController(SwiftCookDbContext context, IMapper mapper, CupboardStockService stock)
        {
            _context = context;
            _mapper = mapper;
            _stock = stock;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CupboardDto>>> GetAll()
        {
            var items = await _context.Cupboards
                .Include(c => c.Ingredient)
                .Include(c => c.Unit)
                .ToListAsync();

            return _mapper.Map<List<CupboardDto>>(items);
        }

        /// <summary>
        /// Adds stock. Same unit: amounts are summed (unknown + anything stays unknown). A different unit
        /// in the same dimension (e.g. g and kg) is converted and merged into the existing row, shown
        /// in the larger sensible unit. Incomparable units, or a missing amount, get their own row.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CupboardDto>> Create(CupboardCreateDto dto)
        {
            if (dto.Amount is < 0) return BadRequest(new { message = "Amount cannot be negative." });

            var ingredient = await _context.Ingredients.FindAsync(dto.IngredientId);
            var unit = await _context.Units.FindAsync(dto.UnitId);
            if (ingredient == null || unit == null)
                return BadRequest(new { message = "Unknown ingredient or unit." });

            var result = await _stock.AddStockAsync(dto.IngredientId, unit, dto.Amount);

            var created = await _context.Cupboards
                .Include(c => c.Ingredient)
                .Include(c => c.Unit)
                .FirstAsync(c => c.IngredientId == result.IngredientId && c.UnitId == result.UnitId);

            return CreatedAtAction(nameof(GetAll), new { id = result.IngredientId }, _mapper.Map<CupboardDto>(created));
        }

        [HttpDelete("{ingredientId}/{unitId}")]
        public async Task<IActionResult> Delete(int ingredientId, int unitId)
        {
            var cupboard = await _context.Cupboards.FindAsync(ingredientId, unitId);
            if (cupboard == null) return NotFound();
            _context.Cupboards.Remove(cupboard);
            await _context.SaveChangesAsync();
            return NoContent();
        }    }

}
