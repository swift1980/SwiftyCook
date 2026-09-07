using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagController : ControllerBase
    {
        private readonly SwiftCookDbContext _context;
        private readonly IMapper _mapper;

        public TagController(SwiftCookDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetAll()
        {
            var tags = await _context.Tags.ToListAsync();
            return _mapper.Map<List<TagDto>>(tags);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TagDto>> GetById(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            return tag == null ? NotFound() : _mapper.Map<TagDto>(tag);
        }

        [HttpPost]
        public async Task<ActionResult<TagDto>> Create(TagCreateDto dto)
        {
            var tag = _mapper.Map<Tag>(dto);
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = tag.Id }, _mapper.Map<TagDto>(tag));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TagCreateDto dto)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null) return NotFound();
            _mapper.Map(dto, tag);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null) return NotFound();
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
