using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnitController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;

        public UnitController(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UnitDto>>> GetAll()
        {
            var units = await _context.Units.ToListAsync();
            return _mapper.Map<List<UnitDto>>(units);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UnitDto>> GetById(int id)
        {
            var unit = await _context.Units.FindAsync(id);
            return unit == null ? NotFound() : _mapper.Map<UnitDto>(unit);
        }

        [HttpPost]
        public async Task<ActionResult<UnitDto>> Create(UnitCreateDto dto)
        {
            var unit = _mapper.Map<Unit>(dto);
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = unit.Id }, _mapper.Map<UnitDto>(unit));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UnitCreateDto dto)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit == null) return NotFound();
            _mapper.Map(dto, unit);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit == null) return NotFound();
            _context.Units.Remove(unit);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
