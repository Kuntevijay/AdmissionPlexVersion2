using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Shared.Common;
using AdmissionPlex.Shared.DTOs.Tests;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public QuestionsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? sectionType = null,
        [FromQuery] long? interestCategoryId = null,
        [FromQuery] long? aptitudeCategoryId = null,
        [FromQuery] bool activeOnly = true)
    {
        IEnumerable<Question> questions;

        if (!string.IsNullOrEmpty(sectionType) && Enum.TryParse<SectionType>(sectionType, true, out var st))
            questions = await _uow.Questions.GetBySectionTypeAsync(st);
        else if (interestCategoryId.HasValue)
            questions = await _uow.Questions.GetByInterestCategoryAsync(interestCategoryId.Value);
        else if (aptitudeCategoryId.HasValue)
            questions = await _uow.Questions.GetByAptitudeCategoryAsync(aptitudeCategoryId.Value);
        else
            questions = await _uow.Questions.GetAllAsync();

        if (activeOnly)
            questions = questions.Where(q => q.IsActive);

        var dtos = questions.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<QuestionDto>>.Ok(dtos));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var question = await _uow.Questions.GetWithOptionsAsync(id);
        if (question == null)
            return NotFound(ApiResponse<object>.Fail("Question not found."));

        return Ok(ApiResponse<QuestionDto>.Ok(MapToDto(question)));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QuestionCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.QuestionText))
            return BadRequest(ApiResponse<object>.Fail("Question text is required."));

        if (!Enum.TryParse<QuestionType>(dto.QuestionType, true, out var questionType))
            return BadRequest(ApiResponse<object>.Fail("Invalid question type."));

        if (!Enum.TryParse<SectionType>(dto.SectionType, true, out var sectionType))
            return BadRequest(ApiResponse<object>.Fail("Invalid section type."));

        if (!Enum.TryParse<DifficultyLevel>(dto.Difficulty, true, out var difficulty))
            difficulty = DifficultyLevel.Medium;

        var question = new Question
        {
            QuestionText = dto.QuestionText,
            QuestionType = questionType,
            SectionType = sectionType,
            InterestCategoryId = dto.InterestCategoryId,
            AptitudeCategoryId = dto.AptitudeCategoryId,
            Difficulty = difficulty,
            Weightage = dto.Weightage,
            MaxScore = dto.MaxScore,
            Explanation = dto.Explanation,
            ImageUrl = dto.ImageUrl,
            IsActive = true,
            CreatedBy = GetUserId()
        };

        foreach (var opt in dto.Options)
        {
            StreamType? streamTag = null;
            if (!string.IsNullOrEmpty(opt.StreamTag) && Enum.TryParse<StreamType>(opt.StreamTag, true, out var st))
                streamTag = st;

            question.Options.Add(new QuestionOption
            {
                OptionText = opt.OptionText,
                OptionOrder = opt.OptionOrder,
                IsCorrect = opt.IsCorrect,
                ScoreValue = opt.ScoreValue,
                StreamTag = streamTag,
                ImageUrl = opt.ImageUrl
            });
        }

        await _uow.Questions.AddAsync(question);
        await _uow.SaveChangesAsync();

        var created = await _uow.Questions.GetWithOptionsAsync(question.Id);
        return CreatedAtAction(nameof(GetById), new { id = question.Id },
            ApiResponse<QuestionDto>.Ok(MapToDto(created!)));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] QuestionCreateDto dto)
    {
        var question = await _uow.Questions.GetWithOptionsAsync(id);
        if (question == null)
            return NotFound(ApiResponse<object>.Fail("Question not found."));

        if (!Enum.TryParse<QuestionType>(dto.QuestionType, true, out var questionType))
            return BadRequest(ApiResponse<object>.Fail("Invalid question type."));

        if (!Enum.TryParse<SectionType>(dto.SectionType, true, out var sectionType))
            return BadRequest(ApiResponse<object>.Fail("Invalid section type."));

        Enum.TryParse<DifficultyLevel>(dto.Difficulty, true, out var difficulty);

        question.QuestionText = dto.QuestionText;
        question.QuestionType = questionType;
        question.SectionType = sectionType;
        question.InterestCategoryId = dto.InterestCategoryId;
        question.AptitudeCategoryId = dto.AptitudeCategoryId;
        question.Difficulty = difficulty;
        question.Weightage = dto.Weightage;
        question.MaxScore = dto.MaxScore;
        question.Explanation = dto.Explanation;
        question.ImageUrl = dto.ImageUrl;

        // Replace options
        question.Options.Clear();
        foreach (var opt in dto.Options)
        {
            StreamType? streamTag = null;
            if (!string.IsNullOrEmpty(opt.StreamTag) && Enum.TryParse<StreamType>(opt.StreamTag, true, out var st))
                streamTag = st;

            question.Options.Add(new QuestionOption
            {
                OptionText = opt.OptionText,
                OptionOrder = opt.OptionOrder,
                IsCorrect = opt.IsCorrect,
                ScoreValue = opt.ScoreValue,
                StreamTag = streamTag,
                ImageUrl = opt.ImageUrl
            });
        }

        _uow.Questions.Update(question);
        await _uow.SaveChangesAsync();

        return Ok(ApiResponse<QuestionDto>.Ok(MapToDto(question)));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var question = await _uow.Questions.GetByIdAsync(id);
        if (question == null)
            return NotFound(ApiResponse<object>.Fail("Question not found."));

        question.IsActive = false; // soft delete
        _uow.Questions.Update(question);
        await _uow.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { }, "Question deactivated."));
    }

    private long GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null ? long.Parse(claim.Value) : 0;
    }

    private static QuestionDto MapToDto(Question q) => new()
    {
        Id = q.Id,
        Uuid = q.Uuid,
        QuestionText = q.QuestionText,
        QuestionType = q.QuestionType.ToString(),
        SectionType = q.SectionType.ToString(),
        InterestCategoryId = q.InterestCategoryId,
        InterestCategoryName = q.InterestCategory?.Name,
        AptitudeCategoryId = q.AptitudeCategoryId,
        AptitudeCategoryName = q.AptitudeCategory?.Name,
        Difficulty = q.Difficulty.ToString(),
        Weightage = q.Weightage,
        MaxScore = q.MaxScore,
        Explanation = q.Explanation,
        ImageUrl = q.ImageUrl,
        IsActive = q.IsActive,
        Options = q.Options.Select(o => new QuestionOptionDto
        {
            Id = o.Id,
            OptionText = o.OptionText,
            OptionOrder = o.OptionOrder,
            IsCorrect = o.IsCorrect,
            ScoreValue = o.ScoreValue,
            StreamTag = o.StreamTag?.ToString(),
            ImageUrl = o.ImageUrl
        }).OrderBy(o => o.OptionOrder).ToList()
    };
}
