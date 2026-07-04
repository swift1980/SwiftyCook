using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CupboardController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;

        public CupboardController(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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

        [HttpPost]
        public async Task<ActionResult<Cupboard>> Create(Cupboard cupboard)
        {
            _context.Cupboards.Add(cupboard);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = cupboard.IngredientId }, cupboard);
        }

        [HttpDelete("{ingredientId}")]
        public async Task<IActionResult> Delete(int ingredientId)
        {
            var cupboard = await _context.Cupboards.FindAsync(ingredientId);
            if (cupboard == null) return NotFound();
            _context.Cupboards.Remove(cupboard);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
