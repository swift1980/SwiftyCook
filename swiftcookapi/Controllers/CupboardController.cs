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

        private readonly UnitConverter _converter;

        public CupboardController(SwiftCookDbContext context, IMapper mapper, UnitConverter converter)
        {
            _context = context;
            _mapper = mapper;
            _converter = converter;
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

            var rows = await _context.Cupboards
                .Include(c => c.Unit)
                .Where(c => c.IngredientId == dto.IngredientId)
                .ToListAsync();

            var sameUnit = rows.FirstOrDefault(r => r.UnitId == dto.UnitId);
            Cupboard result;

            if (sameUnit != null)
            {
                sameUnit.Amount = sameUnit.Amount.HasValue && dto.Amount.HasValue
                    ? sameUnit.Amount + dto.Amount
                    : null;
                result = sameUnit;
            }
            else
            {
                var convertible = dto.Amount.HasValue
                    ? rows.FirstOrDefault(r => r.Amount.HasValue && _converter.CanConvert(unit, r.Unit))
                    : null;

                if (convertible != null
                    && _converter.TryAdd(convertible.Amount!.Value, convertible.Unit, dto.Amount!.Value, unit, out var sum))
                {
                    var candidates = new[] { convertible.Unit, unit };
                    var (amount, displayUnit) = _converter.ToDisplay(sum, convertible.Unit, candidates);

                    // The unit is part of the key, so a unit change means replacing the row.
                    _context.Cupboards.Remove(convertible);
                    result = new Cupboard { IngredientId = dto.IngredientId, UnitId = displayUnit.Id, Amount = amount };
                    _context.Cupboards.Add(result);
                }
                else
                {
                    result = new Cupboard { IngredientId = dto.IngredientId, UnitId = dto.UnitId, Amount = dto.Amount };
                    _context.Cupboards.Add(result);
                }
            }

            await _context.SaveChangesAsync();

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
