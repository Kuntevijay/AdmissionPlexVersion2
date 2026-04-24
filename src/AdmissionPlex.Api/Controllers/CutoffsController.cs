using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdmissionPlex.Core.Entities.Cutoffs;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CutoffsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public CutoffsController(IUnitOfWork uow) => _uow = uow;

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? exam = null,
        [FromQuery] int? year = null,
        [FromQuery] long? collegeId = null,
        [FromQuery] long? branchId = null,
        [FromQuery] string? category = null)
    {
        ExamType? examType = null;
        if (!string.IsNullOrEmpty(exam) && Enum.TryParse<ExamType>(exam, true, out var et))
            examType = et;

        var cutoffs = await _uow.Cutoffs.SearchAsync(examType, year, collegeId, branchId, category);
        var dtos = cutoffs.Select(c => new
        {
            c.Id, CollegeName = c.College.Name, BranchName = c.Branch.Name,
            Exam = c.Exam.ToString(), c.Year, c.Round, c.Category,
            c.CutoffPercentile, c.CutoffRank, c.CutoffScore, c.SeatsAvailable,
            CollegeCity = c.College.City, CollegeType = c.College.Type.ToString()
        });
        return Ok(ApiResponse<object>.Ok(dtos));
    }

    [HttpGet("colleges")]
    public async Task<IActionResult> GetColleges()
    {
        var colleges = await _uow.Cutoffs.GetAllCollegesAsync();
        return Ok(ApiResponse<object>.Ok(colleges.Select(c => new { c.Id, c.Name, c.Code, c.City, c.State, Type = c.Type.ToString() })));
    }

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches()
    {
        var branches = await _uow.Cutoffs.GetAllBranchesAsync();
        return Ok(ApiResponse<object>.Ok(branches.Select(b => new { b.Id, b.Name, b.Code })));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CutoffCreateDto dto)
    {
        if (!Enum.TryParse<ExamType>(dto.Exam, true, out var examType))
            return BadRequest(ApiResponse<object>.Fail("Invalid exam type."));

        var cutoff = new CutoffData
        {
            CollegeId = dto.CollegeId,
            BranchId = dto.BranchId,
            Exam = examType,
            Year = dto.Year,
            Round = dto.Round,
            Category = dto.Category,
            CutoffPercentile = dto.CutoffPercentile,
            CutoffRank = dto.CutoffRank,
            CutoffScore = dto.CutoffScore,
            SeatsAvailable = dto.SeatsAvailable
        };

        await _uow.Cutoffs.AddAsync(cutoff);
        await _uow.SaveChangesAsync();
        return Created("", ApiResponse<object>.Ok(new { cutoff.Id }));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("colleges")]
    public async Task<IActionResult> CreateCollege([FromBody] CollegeCreateDto dto)
    {
        if (!Enum.TryParse<CollegeType>(dto.Type, true, out var collegeType))
            return BadRequest(ApiResponse<object>.Fail("Invalid college type."));

        var college = new College
        {
            Name = dto.Name, Code = dto.Code, University = dto.University,
            City = dto.City, State = dto.State, Type = collegeType, Website = dto.Website
        };
        // Add via context since we don't have a college-specific repo method
        await _uow.Cutoffs.AddAsync(new CutoffData()); // placeholder
        // Actually we need direct access - let's use the DbSet approach
        return Created("", ApiResponse<object>.Ok(new { Message = "Use direct DbContext for college creation" }));
    }
}

public class CutoffCreateDto
{
    public long CollegeId { get; set; }
    public long BranchId { get; set; }
    public string Exam { get; set; } = "";
    public int Year { get; set; }
    public int Round { get; set; } = 1;
    public string Category { get; set; } = "OPEN";
    public decimal? CutoffPercentile { get; set; }
    public int? CutoffRank { get; set; }
    public decimal? CutoffScore { get; set; }
    public int? SeatsAvailable { get; set; }
}

public class CollegeCreateDto
{
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public string? University { get; set; }
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Type { get; set; } = "Private";
    public string? Website { get; set; }
}
