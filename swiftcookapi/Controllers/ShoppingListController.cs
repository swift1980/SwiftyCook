using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingListController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;

        public ShoppingListController(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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
        public async Task<ActionResult<ShoppingList>> Create(ShoppingList item)
        {
            _context.ShoppingLists.Add(item);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = item.Id }, item);
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
