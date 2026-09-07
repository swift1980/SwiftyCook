using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToolController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;

        public ToolController(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ToolDto>>> GetAll()
        {
            var tools = await _context.Tools.ToListAsync();
            return _mapper.Map<List<ToolDto>>(tools);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ToolDto>> GetById(int id)
        {
            var tool = await _context.Tools.FindAsync(id);
            return tool == null ? NotFound() : _mapper.Map<ToolDto>(tool);
        }

        [HttpPost]
        public async Task<ActionResult<ToolDto>> Create(ToolCreateDto dto)
        {
            var tool = _mapper.Map<Tool>(dto);
            _context.Tools.Add(tool);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = tool.Id }, _mapper.Map<ToolDto>(tool));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ToolCreateDto dto)
        {
            var tool = await _context.Tools.FindAsync(id);
            if (tool == null) return NotFound();
            _mapper.Map(dto, tool);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tool = await _context.Tools.FindAsync(id);
            if (tool == null) return NotFound();
            _context.Tools.Remove(tool);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
