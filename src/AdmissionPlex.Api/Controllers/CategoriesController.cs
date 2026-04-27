using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    public CategoriesController(AppDbContext context) => _context = context;

    [HttpGet("interest-categories")]
    public async Task<IActionResult> GetInterest()
        => Ok(ApiResponse<object>.Ok(await _context.InterestCategories.OrderBy(c => c.DisplayOrder).ToListAsync()));

    [HttpPost("interest-categories")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateInterest([FromBody] CategoryDto dto)
    {
        if (await _context.InterestCategories.AnyAsync(c => c.Code == dto.Code))
            return BadRequest(ApiResponse<object>.Fail($"Code '{dto.Code}' already exists."));
        var cat = new InterestCategory { Code = dto.Code.ToUpper(), Name = dto.Name, Description = dto.Description ?? "", DisplayOrder = dto.DisplayOrder > 0 ? dto.DisplayOrder : await _context.InterestCategories.CountAsync() + 1, IsActive = true };
        _context.InterestCategories.Add(cat);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(cat));
    }

    [HttpPut("interest-categories/{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateInterest(long id, [FromBody] CategoryDto dto)
    {
        var cat = await _context.InterestCategories.FindAsync(id);
        if (cat == null) return NotFound();
        cat.Code = dto.Code.ToUpper(); cat.Name = dto.Name; cat.Description = dto.Description ?? cat.Description;
        if (dto.DisplayOrder > 0) cat.DisplayOrder = dto.DisplayOrder;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(cat));
    }

    [HttpDelete("interest-categories/{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteInterest(long id)
    {
        var cat = await _context.InterestCategories.FindAsync(id);
        if (cat == null) return NotFound();
        if (await _context.Questions.AnyAsync(q => q.InterestCategoryId == id))
            return BadRequest(ApiResponse<object>.Fail("Cannot delete — questions linked."));
        _context.InterestCategories.Remove(cat);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted."));
    }

    [HttpGet("aptitude-categories")]
    public async Task<IActionResult> GetAptitude()
        => Ok(ApiResponse<object>.Ok(await _context.AptitudeCategories.OrderBy(c => c.DisplayOrder).ToListAsync()));

    [HttpPost("aptitude-categories")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAptitude([FromBody] CategoryDto dto)
    {
        if (await _context.AptitudeCategories.AnyAsync(c => c.Code == dto.Code))
            return BadRequest(ApiResponse<object>.Fail($"Code '{dto.Code}' already exists."));
        var cat = new AptitudeCategory { Code = dto.Code.ToUpper(), Name = dto.Name, Description = dto.Description ?? "", DisplayOrder = dto.DisplayOrder > 0 ? dto.DisplayOrder : await _context.AptitudeCategories.CountAsync() + 1, IsActive = true };
        _context.AptitudeCategories.Add(cat);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(cat));
    }

    [HttpPut("aptitude-categories/{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAptitude(long id, [FromBody] CategoryDto dto)
    {
        var cat = await _context.AptitudeCategories.FindAsync(id);
        if (cat == null) return NotFound();
        cat.Code = dto.Code.ToUpper(); cat.Name = dto.Name; cat.Description = dto.Description ?? cat.Description;
        if (dto.DisplayOrder > 0) cat.DisplayOrder = dto.DisplayOrder;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(cat));
    }

    [HttpDelete("aptitude-categories/{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAptitude(long id)
    {
        var cat = await _context.AptitudeCategories.FindAsync(id);
        if (cat == null) return NotFound();
        if (await _context.Questions.AnyAsync(q => q.AptitudeCategoryId == id))
            return BadRequest(ApiResponse<object>.Fail("Cannot delete — questions linked."));
        _context.AptitudeCategories.Remove(cat);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted."));
    }
}

public class CategoryDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}
