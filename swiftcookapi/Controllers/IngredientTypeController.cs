using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientTypeController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;
        public IngredientTypeController(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IngredientTypeDto>>> GetAll() =>
            await _context.IngredientTypes
                .Select(t => new IngredientTypeDto { Id = t.Id, Name = t.Name })
                .ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<IngredientTypeDto>> GetById(int id)
        {
            var type = await _context.IngredientTypes
                .Select(t => new IngredientTypeDto { Id = t.Id, Name = t.Name })
                .FirstOrDefaultAsync(t => t.Id == id);

            return type == null ? NotFound() : type;
        }

        [HttpGet("{id}/ingredients")]
        public async Task<ActionResult<IEnumerable<IngredientDto>>> GetIngredients(int id)
        {
            var type = await _context.IngredientTypes.FirstOrDefaultAsync(t => t.Id == id);
            if (type == null) return NotFound();

            return await _context.Ingredients
                .Where(i => i.TypeId == id)
                .Select(i => new IngredientDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    PluralName = i.PluralName,
                    TypeId = i.TypeId,
                    TypeName = type.Name
                })
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<IngredientTypeDto>> Create(IngredientTypeCreateDto dto)
        {
            var type = _mapper.Map<IngredientType>(dto);
            _context.IngredientTypes.Add(type);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = type.Id }, _mapper.Map<IngredientTypeDto>(type));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, IngredientTypeCreateDto dto)
        {
            var type = await _context.IngredientTypes.FindAsync(id);
            if (type == null) return NotFound();
            _mapper.Map(dto, type);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var type = await _context.IngredientTypes.FindAsync(id);
            if (type == null) return NotFound();
            _context.IngredientTypes.Remove(type);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}