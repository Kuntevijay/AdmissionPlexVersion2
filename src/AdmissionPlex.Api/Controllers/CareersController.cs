using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdmissionPlex.Core.Entities.Careers;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CareersController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public CareersController(IUnitOfWork uow) => _uow = uow;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var careers = await _uow.Careers.GetPublishedAsync();
        var dtos = careers.Select(c => new
        {
            c.Id, c.Slug, c.Title, c.Summary,
            StreamName = c.Stream?.Name ?? "",
            GrowthOutlook = c.GrowthOutlook.ToString(),
            c.EducationPath, c.EducationCostRange,
            SalaryMin = c.AvgSalaryMin, SalaryMax = c.AvgSalaryMax,
            c.AvgSalaryMin, c.AvgSalaryMax, c.ImageUrl
        });
        return Ok(ApiResponse<object>.Ok(dtos));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var career = await _uow.Careers.GetBySlugAsync(slug);
        if (career == null)
            return NotFound(ApiResponse<object>.Fail("Career not found."));

        return Ok(ApiResponse<object>.Ok(new
        {
            career.Id, career.Slug, career.Title, career.Summary, career.Description,
            StreamName = career.Stream?.Name ?? "",
            GrowthOutlook = career.GrowthOutlook.ToString(),
            career.EducationPath, career.EducationCostRange, career.AdmissionInfo,
            career.AvgSalaryMin, career.AvgSalaryMax, career.JobMarketSize,
            career.SkillsRequired, career.TopColleges, career.EntranceExams,
            career.ImageUrl,
            Subjects = career.Subjects.Select(s => new { s.SubjectName, Importance = s.Importance.ToString() })
        }));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CareerCreateDto dto)
    {
        if (!Enum.TryParse<GrowthOutlook>(dto.GrowthOutlook, true, out var growth))
            growth = GrowthOutlook.Medium;

        var career = new Career
        {
            Title = dto.Title,
            Slug = dto.Title.ToLower().Replace(" ", "-").Replace("&", "and"),
            StreamId = dto.StreamId,
            Summary = dto.Summary,
            Description = dto.Description ?? "",
            EducationPath = dto.EducationPath,
            EducationCostRange = dto.EducationCostRange,
            AdmissionInfo = dto.AdmissionInfo,
            AvgSalaryMin = dto.AvgSalaryMin,
            AvgSalaryMax = dto.AvgSalaryMax,
            GrowthOutlook = growth,
            JobMarketSize = dto.JobMarketSize,
            SkillsRequired = dto.SkillsRequired,
            TopColleges = dto.TopColleges,
            EntranceExams = dto.EntranceExams,
            SuitabilityCutoffPct = dto.SuitabilityCutoffPct ?? 80m
        };

        await _uow.Careers.AddAsync(career);
        await _uow.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBySlug), new { slug = career.Slug },
            ApiResponse<object>.Ok(new { career.Id, career.Slug }));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] CareerCreateDto dto)
    {
        var career = await _uow.Careers.GetByIdAsync(id);
        if (career == null)
            return NotFound(ApiResponse<object>.Fail("Career not found."));

        Enum.TryParse<GrowthOutlook>(dto.GrowthOutlook, true, out var growth);

        career.Title = dto.Title;
        career.Summary = dto.Summary;
        career.Description = dto.Description ?? career.Description;
        career.StreamId = dto.StreamId;
        career.EducationPath = dto.EducationPath;
        career.EducationCostRange = dto.EducationCostRange;
        career.AdmissionInfo = dto.AdmissionInfo;
        career.AvgSalaryMin = dto.AvgSalaryMin;
        career.AvgSalaryMax = dto.AvgSalaryMax;
        career.GrowthOutlook = growth;
        career.JobMarketSize = dto.JobMarketSize;
        career.SuitabilityCutoffPct = dto.SuitabilityCutoffPct ?? career.SuitabilityCutoffPct;

        _uow.Careers.Update(career);
        await _uow.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { career.Id }));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var career = await _uow.Careers.GetByIdAsync(id);
        if (career == null)
            return NotFound(ApiResponse<object>.Fail("Career not found."));

        career.IsPublished = false;
        _uow.Careers.Update(career);
        await _uow.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Career unpublished."));
    }
}

public class CareerCreateDto
{
    public string Title { get; set; } = "";
    public long StreamId { get; set; }
    public string Summary { get; set; } = "";
    public string? Description { get; set; }
    public string? EducationPath { get; set; }
    public string? EducationCostRange { get; set; }
    public string? AdmissionInfo { get; set; }
    public decimal? AvgSalaryMin { get; set; }
    public decimal? AvgSalaryMax { get; set; }
    public string GrowthOutlook { get; set; } = "Medium";
    public string? JobMarketSize { get; set; }
    public string? SkillsRequired { get; set; }
    public string? TopColleges { get; set; }
    public string? EntranceExams { get; set; }
    public decimal? SuitabilityCutoffPct { get; set; }
}
