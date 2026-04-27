using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Core.Interfaces.Services;
using AdmissionPlex.Shared.Common;
using AdmissionPlex.Shared.DTOs.Tests;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ITestScoringService _scoringService;
    private readonly AppDbContext _context;

    public TestsController(IUnitOfWork uow, ITestScoringService scoringService, AppDbContext context)
    {
        _uow = uow;
        _scoringService = scoringService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tests = await _uow.Tests.GetAllAsync();
        var dtos = tests.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<TestDto>>.Ok(dtos));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var test = await _uow.Tests.GetWithSectionsAsync(id);
        if (test == null)
            return NotFound(ApiResponse<object>.Fail("Test not found."));
        return Ok(ApiResponse<TestDto>.Ok(MapToDto(test)));
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var test = await _uow.Tests.GetBySlugAsync(slug);
        if (test == null)
            return NotFound(ApiResponse<object>.Fail("Test not found."));
        return Ok(ApiResponse<TestDto>.Ok(MapToDto(test)));
    }

    /// <summary>
    /// Start a test — creates an attempt and returns all questions (without answers)
    /// </summary>
    [Authorize(Roles = "Student")]
    [HttpPost("{id:long}/start")]
    public async Task<IActionResult> StartTest(long id)
    {
        var test = await _uow.Tests.GetWithFullDetailsAsync(id);
        if (test == null || !test.IsActive)
            return NotFound(ApiResponse<object>.Fail("Test not found or inactive."));

        var studentId = await GetStudentIdAsync();
        if (studentId == 0)
            return BadRequest(ApiResponse<object>.Fail("Student profile not found."));

        // Check for existing in-progress attempt
        var existingAttempts = await _uow.TestAttempts.GetByStudentIdAsync(studentId);
        var activeAttempt = existingAttempts.FirstOrDefault(a =>
            a.TestId == id && (a.Status == AttemptStatus.Started || a.Status == AttemptStatus.InProgress));
        if (activeAttempt != null)
            return BadRequest(ApiResponse<object>.Fail("You already have an active attempt for this test."));

        // Create attempt
        var attempt = new TestAttempt
        {
            StudentId = studentId,
            TestId = id,
            Status = AttemptStatus.InProgress
        };
        await _uow.TestAttempts.AddAsync(attempt);
        await _uow.SaveChangesAsync();

        // Build questions for student (no correct answers exposed)
        var questionNumber = 0;
        var questions = test.Sections
            .OrderBy(s => s.SectionOrder)
            .SelectMany(section => section.Questions
                .OrderBy(sq => sq.QuestionOrder)
                .Select(sq =>
                {
                    questionNumber++;
                    return new TestQuestionForStudentDto
                    {
                        QuestionId = sq.Question.Id,
                        QuestionText = sq.Question.QuestionText,
                        QuestionType = sq.Question.QuestionType.ToString(),
                        ImageUrl = sq.Question.ImageUrl,
                        QuestionNumber = questionNumber,
                        SectionTitle = section.Title,
                        Options = sq.Question.Options
                            .OrderBy(o => o.OptionOrder)
                            .Select(o => new OptionForStudentDto
                            {
                                Id = o.Id,
                                OptionText = o.OptionText,
                                OptionOrder = o.OptionOrder
                            }).ToList()
                    };
                })).ToList();

        return Ok(ApiResponse<StartTestResponseDto>.Ok(new StartTestResponseDto
        {
            AttemptUuid = attempt.Uuid,
            AttemptId = attempt.Id,
            TestTitle = test.Title,
            DurationMinutes = test.DurationMinutes,
            TotalQuestions = questionNumber,
            Instructions = test.Instructions,
            Questions = questions
        }));
    }

    /// <summary>
    /// Submit a single answer during the test
    /// </summary>
    [Authorize(Roles = "Student")]
    [HttpPost("attempts/{attemptId:long}/submit")]
    public async Task<IActionResult> SubmitAnswer(long attemptId, [FromBody] SubmitAnswerDto dto)
    {
        var attempt = await _uow.TestAttempts.GetByIdAsync(attemptId);
        if (attempt == null)
            return NotFound(ApiResponse<object>.Fail("Attempt not found."));
        if (attempt.Status == AttemptStatus.Completed)
            return BadRequest(ApiResponse<object>.Fail("Test already completed."));

        // Calculate score for this answer
        decimal score = 0;
        if (dto.SelectedOptionId.HasValue)
        {
            var option = await _uow.Questions.GetByIdAsync(dto.QuestionId);
            // We need the option entity
            var question = await _uow.Questions.GetWithOptionsAsync(dto.QuestionId);
            var selectedOption = question?.Options.FirstOrDefault(o => o.Id == dto.SelectedOptionId.Value);
            if (selectedOption != null)
                score = selectedOption.ScoreValue;
        }

        // Check if response already exists (update) or create new
        var existingResponses = await _uow.TestAttempts.GetWithResponsesAsync(attemptId);
        var existing = existingResponses?.Responses.FirstOrDefault(r => r.QuestionId == dto.QuestionId);

        if (existing != null)
        {
            existing.SelectedOptionId = dto.SelectedOptionId;
            existing.OpenAnswer = dto.OpenAnswer;
            existing.ScoreObtained = score;
            existing.TimeTakenSeconds = dto.TimeTakenSeconds;
            existing.AnsweredAt = DateTime.UtcNow;
        }
        else
        {
            var response = new TestResponse
            {
                AttemptId = attemptId,
                QuestionId = dto.QuestionId,
                SelectedOptionId = dto.SelectedOptionId,
                OpenAnswer = dto.OpenAnswer,
                ScoreObtained = score,
                TimeTakenSeconds = dto.TimeTakenSeconds
            };
            await _uow.TestAttempts.AddAsync(attempt); // ensure tracked
            existingResponses!.Responses.Add(response);
        }

        await _uow.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { Saved = true }));
    }

    /// <summary>
    /// Finish the test — submit all remaining answers, compute scores, generate report
    /// </summary>
    [Authorize(Roles = "Student")]
    [HttpPost("attempts/{attemptId:long}/finish")]
    public async Task<IActionResult> FinishTest(long attemptId, [FromBody] FinishTestDto dto)
    {
        var attempt = await _uow.TestAttempts.GetWithResponsesAsync(attemptId);
        if (attempt == null)
            return NotFound(ApiResponse<object>.Fail("Attempt not found."));
        if (attempt.Status == AttemptStatus.Completed)
            return BadRequest(ApiResponse<object>.Fail("Test already completed."));

        // Save any remaining answers from the batch
        foreach (var answer in dto.Answers)
        {
            var existing = attempt.Responses.FirstOrDefault(r => r.QuestionId == answer.QuestionId);
            decimal score = 0;

            if (answer.SelectedOptionId.HasValue)
            {
                var question = await _uow.Questions.GetWithOptionsAsync(answer.QuestionId);
                var opt = question?.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId.Value);
                if (opt != null) score = opt.ScoreValue;
            }

            if (existing != null)
            {
                existing.SelectedOptionId = answer.SelectedOptionId;
                existing.OpenAnswer = answer.OpenAnswer;
                existing.ScoreObtained = score;
                existing.TimeTakenSeconds = answer.TimeTakenSeconds;
            }
            else
            {
                attempt.Responses.Add(new TestResponse
                {
                    AttemptId = attemptId,
                    QuestionId = answer.QuestionId,
                    SelectedOptionId = answer.SelectedOptionId,
                    OpenAnswer = answer.OpenAnswer,
                    ScoreObtained = score,
                    TimeTakenSeconds = answer.TimeTakenSeconds
                });
            }
        }

        await _uow.SaveChangesAsync();

        // Score the test
        TestAttempt scoredAttempt;
        if (attempt.Test.TestType == TestType.StreamSelector)
            scoredAttempt = await _scoringService.ScoreStreamSelectorAsync(attemptId);
        else
            scoredAttempt = await _scoringService.ScorePsychometricTestAsync(attemptId);

        return Ok(ApiResponse<TestAttemptDto>.Ok(MapAttemptToDto(scoredAttempt)));
    }

    /// <summary>
    /// Get attempt details with results
    /// </summary>
    [Authorize]
    [HttpGet("attempts/{attemptId:long}")]
    public async Task<IActionResult> GetAttempt(long attemptId)
    {
        var attempt = await _uow.TestAttempts.GetWithFullResultsAsync(attemptId);
        if (attempt == null)
            return NotFound(ApiResponse<object>.Fail("Attempt not found."));

        return Ok(ApiResponse<TestAttemptDto>.Ok(MapAttemptToDto(attempt)));
    }

    /// <summary>
    /// Get full psychometric report data (JSON)
    /// </summary>
    [Authorize]
    [HttpGet("attempts/{attemptId:long}/report")]
    public async Task<IActionResult> GetReport(long attemptId)
    {
        var attempt = await _uow.TestAttempts.GetWithFullResultsAsync(attemptId);
        if (attempt == null)
            return NotFound(ApiResponse<object>.Fail("Attempt not found."));
        if (attempt.Status != AttemptStatus.Completed)
            return BadRequest(ApiResponse<object>.Fail("Test not yet completed."));

        var report = new PsychometricReportDto
        {
            StudentName = $"{attempt.Student.FirstName} {attempt.Student.LastName}",
            StudentClass = attempt.Student.CurrentClass,
            SchoolName = attempt.Student.SchoolName,
            TestDate = attempt.CompletedAt ?? attempt.StartedAt,
            OverallIqScore = attempt.OverallIqScore ?? 0,
            IqCategory = attempt.IqCategory?.ToString() ?? "",
            PdfReportUrl = attempt.PdfReportUrl,

            Top3Interests = attempt.InterestScores
                .OrderBy(s => s.RankOrder).Take(3)
                .Select(MapInterestScore).ToList(),

            Top3Aptitudes = attempt.AptitudeScores
                .OrderBy(s => s.RankOrder).Take(3)
                .Select(MapAptitudeScore).ToList(),

            AllInterestScores = attempt.InterestScores
                .OrderBy(s => s.InterestCategory.DisplayOrder)
                .Select(MapInterestScore).ToList(),

            AllAptitudeScores = attempt.AptitudeScores
                .OrderBy(s => s.AptitudeCategory.DisplayOrder)
                .Select(MapAptitudeScore).ToList(),

            AllCareerSuitability = attempt.CareerSuitabilityScores
                .OrderBy(s => s.RankOrder)
                .Select(MapCareerSuitability).ToList(),

            Top5Careers = attempt.CareerSuitabilityScores
                .OrderBy(s => s.RankOrder).Take(5)
                .Select(MapCareerSuitability).ToList(),

            RecommendedCareers = attempt.CareerSuitabilityScores
                .Where(s => s.IsRecommended)
                .OrderBy(s => s.RankOrder)
                .Select(MapCareerSuitability).ToList(),

            CareersToConsider = attempt.CareerSuitabilityScores
                .Where(s => s.IsCanBeConsidered)
                .OrderBy(s => s.RankOrder)
                .Select(MapCareerSuitability).ToList()
        };

        return Ok(ApiResponse<PsychometricReportDto>.Ok(report));
    }

    /// <summary>
    /// Get student's test history
    /// </summary>
    [Authorize(Roles = "Student")]
    [HttpGet("my-attempts")]
    public async Task<IActionResult> GetMyAttempts()
    {
        var studentId = await GetStudentIdAsync();
        var attempts = await _uow.TestAttempts.GetByStudentIdAsync(studentId);
        var dtos = attempts.Select(MapAttemptToDto).ToList();
        return Ok(ApiResponse<List<TestAttemptDto>>.Ok(dtos));
    }

    // === Helpers ===

    private async Task<long> GetStudentIdAsync()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var students = await _uow.Students.FindAsync(s => s.UserId == userId);
        var student = students.FirstOrDefault();
        return student?.Id ?? 0;
    }

    // ── CRUD ──
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTest([FromBody] CreateTestDto dto)
    {
        var test = new Test
        {
            Code = dto.Code, Title = dto.Title, Slug = dto.Code.Replace("_", "-"),
            TestType = Enum.TryParse<TestType>(dto.TestType, true, out var tt) ? tt : TestType.PsychometricFull,
            Category = dto.Category, Description = dto.Description, Icon = dto.Icon ?? "📝",
            DisplayOrder = dto.DisplayOrder, DurationMinutes = dto.DurationMinutes,
            Price = dto.Price, IsActive = dto.IsActive, IsPublic = dto.IsPublic,
            RequiresPayment = dto.RequiresPayment, IsContinuityFlow = dto.IsContinuityFlow,
            ParentTestCode = dto.ParentTestCode, Instructions = dto.Instructions,
            IncludesCounsellorSession = dto.IncludesCounsellorSession
        };
        _context.Tests.Add(test);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<TestDto>.Ok(MapToDto(test)));
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTest(long id, [FromBody] CreateTestDto dto)
    {
        var test = await _context.Tests.FindAsync(id);
        if (test == null) return NotFound();
        test.Code = dto.Code; test.Title = dto.Title; test.Slug = dto.Code.Replace("_", "-");
        test.Category = dto.Category; test.Description = dto.Description; test.Icon = dto.Icon;
        test.DisplayOrder = dto.DisplayOrder; test.DurationMinutes = dto.DurationMinutes;
        test.Price = dto.Price; test.IsActive = dto.IsActive; test.IsPublic = dto.IsPublic;
        test.RequiresPayment = dto.RequiresPayment; test.IsContinuityFlow = dto.IsContinuityFlow;
        test.ParentTestCode = dto.ParentTestCode; test.Instructions = dto.Instructions;
        test.IncludesCounsellorSession = dto.IncludesCounsellorSession;
        if (Enum.TryParse<TestType>(dto.TestType, true, out var tt2)) test.TestType = tt2;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<TestDto>.Ok(MapToDto(test)));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTest(long id)
    {
        var test = await _context.Tests.FindAsync(id);
        if (test == null) return NotFound();
        _context.Tests.Remove(test);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted."));
    }

    private static TestDto MapToDto(Test t) => new()
    {
        Id = t.Id, Code = t.Code, Title = t.Title, Slug = t.Slug,
        TestType = t.TestType.ToString(), Category = t.Category,
        Description = t.Description, Icon = t.Icon, DisplayOrder = t.DisplayOrder,
        DurationMinutes = t.DurationMinutes, TotalQuestions = t.TotalQuestions,
        Price = t.Price, IsActive = t.IsActive,
        IncludesCounsellorSession = t.IncludesCounsellorSession,
        Instructions = t.Instructions,
        IsPublic = t.IsPublic, RequiresPayment = t.RequiresPayment,
        IsContinuityFlow = t.IsContinuityFlow, ParentTestCode = t.ParentTestCode,
        Sections = t.Sections?.OrderBy(s => s.SectionOrder).Select(s => new TestSectionDto
        {
            Id = s.Id,
            Title = s.Title,
            SectionType = s.SectionType.ToString(),
            SectionOrder = s.SectionOrder,
            TimeLimitMinutes = s.TimeLimitMinutes,
            InterestCategoryName = s.InterestCategory?.Name,
            AptitudeCategoryName = s.AptitudeCategory?.Name,
            QuestionCount = s.Questions?.Count ?? 0
        }).ToList() ?? new()
    };

    private static TestAttemptDto MapAttemptToDto(TestAttempt a) => new()
    {
        Id = a.Id,
        Uuid = a.Uuid,
        TestId = a.TestId,
        TestTitle = a.Test?.Title ?? "",
        TestType = a.Test?.TestType.ToString() ?? "",
        Status = a.Status.ToString(),
        StartedAt = a.StartedAt,
        CompletedAt = a.CompletedAt,
        RecommendedStream = a.RecommendedStream?.ToString(),
        OverallIqScore = a.OverallIqScore,
        IqCategory = a.IqCategory?.ToString(),
        PdfReportUrl = a.PdfReportUrl,
        TotalQuestions = a.Test?.TotalQuestions ?? 0,
        AnsweredQuestions = a.Responses?.Count ?? 0
    };

    private static InterestScoreDto MapInterestScore(InterestScore s) => new()
    {
        CategoryCode = s.InterestCategory.Code,
        CategoryName = s.InterestCategory.Name,
        Description = s.InterestCategory.Description,
        RawScore = s.RawScore,
        MaxPossibleScore = s.MaxPossibleScore,
        PercentileScore = s.PercentileScore,
        RankOrder = s.RankOrder
    };

    private static AptitudeScoreDto MapAptitudeScore(AptitudeScore s) => new()
    {
        CategoryCode = s.AptitudeCategory.Code,
        CategoryName = s.AptitudeCategory.Name,
        Description = s.AptitudeCategory.Description,
        RawScore = s.RawScore,
        MaxPossibleScore = s.MaxPossibleScore,
        PercentileScore = s.PercentileScore,
        RankOrder = s.RankOrder
    };

    private static CareerSuitabilityDto MapCareerSuitability(CareerSuitabilityScore s) => new()
    {
        CareerId = s.CareerId,
        CareerTitle = s.Career.Title,
        StreamName = s.Career.Stream?.Name ?? "",
        SuitabilityPct = s.SuitabilityPct,
        IsRecommended = s.IsRecommended,
        IsCanBeConsidered = s.IsCanBeConsidered,
        RankOrder = s.RankOrder,
        EducationPath = s.Career.EducationPath,
        EducationCostRange = s.Career.EducationCostRange,
        AdmissionInfo = s.Career.AdmissionInfo
    };
}

public class CreateTestDto
{
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string? TestType { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPublic { get; set; }
    public bool RequiresPayment { get; set; }
    public bool IsContinuityFlow { get; set; }
    public string? ParentTestCode { get; set; }
    public string? Instructions { get; set; }
    public bool IncludesCounsellorSession { get; set; }
}
