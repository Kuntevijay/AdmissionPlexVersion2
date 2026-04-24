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

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student")]
public class AssessmentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _uow;
    private readonly ITestScoringService _scoring;

    public AssessmentController(AppDbContext context, IUnitOfWork uow, ITestScoringService scoring)
    {
        _context = context;
        _uow = uow;
        _scoring = scoring;
    }

    /// <summary>
    /// Get assessment overview — all 9 sub-tests with student's progress
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var studentId = await GetStudentIdAsync();
        if (studentId == 0) return BadRequest(ApiResponse<object>.Fail("Student profile not found."));

        // Get or create assessment session
        var session = await _context.AssessmentSessions
            .Include(s => s.Attempts).ThenInclude(a => a.Test)
            .Include(s => s.Attempts).ThenInclude(a => a.Responses)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (session == null)
        {
            session = new AssessmentSession { StudentId = studentId };
            _context.AssessmentSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        // Get all 9 sub-tests
        var tests = await _context.Tests
            .Where(t => t.TestType != TestType.StreamSelector && t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new
            {
                t.Id, t.Code, t.Title, t.Description, t.Icon, t.Category,
                t.DurationMinutes, t.TotalQuestions, t.DisplayOrder
            })
            .ToListAsync();

        // Map student's progress
        var completedCodes = session.Attempts
            .Where(a => a.Status == AttemptStatus.Completed)
            .Select(a => a.Test.Code).ToHashSet();

        var inProgressCodes = session.Attempts
            .Where(a => a.Status == AttemptStatus.InProgress || a.Status == AttemptStatus.Started)
            .Select(a => a.Test.Code).ToHashSet();

        var testCards = tests.Select((t, idx) =>
        {
            string status;
            int? answeredCount = null;

            if (completedCodes.Contains(t.Code))
            {
                status = "Completed";
                answeredCount = session.Attempts
                    .FirstOrDefault(a => a.Test.Code == t.Code && a.Status == AttemptStatus.Completed)?
                    .Responses?.Count;
            }
            else if (inProgressCodes.Contains(t.Code))
                status = "InProgress";
            else if (idx == 0 || completedCodes.Count >= idx)
                status = "Available";
            else
                status = "Locked";

            return new
            {
                t.Id, t.Code, t.Title, t.Description, t.Icon, t.Category,
                t.DurationMinutes, t.TotalQuestions, Status = status, AnsweredCount = answeredCount
            };
        }).ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            SessionId = session.Id,
            session.Uuid,
            CompletedCount = completedCodes.Count,
            TotalCount = tests.Count,
            AllCompleted = completedCodes.Count >= tests.Count,
            SavedReportId = session.SavedReportId,
            Tests = testCards
        }));
    }

    /// <summary>
    /// Start a specific sub-test within the assessment
    /// </summary>
    [HttpPost("test/{code}/start")]
    public async Task<IActionResult> StartSubTest(string code)
    {
        var studentId = await GetStudentIdAsync();
        if (studentId == 0) return BadRequest(ApiResponse<object>.Fail("Student profile not found."));

        var test = await _context.Tests
            .Include(t => t.Sections.OrderBy(s => s.SectionOrder))
                .ThenInclude(s => s.Questions.OrderBy(q => q.QuestionOrder))
                    .ThenInclude(sq => sq.Question)
                        .ThenInclude(q => q.Options.OrderBy(o => o.OptionOrder))
            .FirstOrDefaultAsync(t => t.Code == code && t.IsActive);

        if (test == null) return NotFound(ApiResponse<object>.Fail("Test not found."));

        // Get or create session
        var session = await _context.AssessmentSessions
            .Include(s => s.Attempts)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (session == null)
        {
            session = new AssessmentSession { StudentId = studentId };
            _context.AssessmentSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        // Check for existing attempt
        var existing = session.Attempts.FirstOrDefault(a => a.TestId == test.Id);
        if (existing != null && existing.Status == AttemptStatus.Completed)
            return BadRequest(ApiResponse<object>.Fail("This sub-test is already completed."));

        if (existing != null && (existing.Status == AttemptStatus.InProgress || existing.Status == AttemptStatus.Started))
        {
            // Return existing attempt
            return Ok(ApiResponse<object>.Ok(BuildTestRunResponse(test, existing.Id)));
        }

        // Create new attempt
        var attempt = new TestAttempt
        {
            StudentId = studentId,
            TestId = test.Id,
            AssessmentSessionId = session.Id,
            Status = AttemptStatus.InProgress
        };
        _context.TestAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(BuildTestRunResponse(test, attempt.Id)));
    }

    /// <summary>
    /// Submit answers for a sub-test
    /// </summary>
    [HttpPost("test/{code}/submit")]
    public async Task<IActionResult> SubmitSubTest(string code, [FromBody] SubmitTestDto dto)
    {
        var studentId = await GetStudentIdAsync();
        if (studentId == 0) return BadRequest(ApiResponse<object>.Fail("Student profile not found."));

        var test = await _context.Tests.FirstOrDefaultAsync(t => t.Code == code);
        if (test == null) return NotFound(ApiResponse<object>.Fail("Test not found."));

        var session = await _context.AssessmentSessions
            .Include(s => s.Attempts).ThenInclude(a => a.Responses)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);
        if (session == null) return BadRequest(ApiResponse<object>.Fail("No assessment session found."));

        var attempt = session.Attempts.FirstOrDefault(a => a.TestId == test.Id && a.Status != AttemptStatus.Completed);
        if (attempt == null) return BadRequest(ApiResponse<object>.Fail("No active attempt for this test."));

        // Save answers
        foreach (var answer in dto.Answers)
        {
            var existing = attempt.Responses.FirstOrDefault(r => r.QuestionId == answer.QuestionId);
            decimal score = 0;
            if (answer.SelectedOptionId.HasValue)
            {
                var option = await _context.QuestionOptions.FindAsync(answer.SelectedOptionId.Value);
                if (option != null) score = option.ScoreValue;
            }

            if (existing != null)
            {
                existing.SelectedOptionId = answer.SelectedOptionId;
                existing.ScoreObtained = score;
            }
            else
            {
                attempt.Responses.Add(new TestResponse
                {
                    AttemptId = attempt.Id,
                    QuestionId = answer.QuestionId,
                    SelectedOptionId = answer.SelectedOptionId,
                    ScoreObtained = score
                });
            }
        }

        attempt.Status = AttemptStatus.Completed;
        attempt.CompletedAt = DateTime.UtcNow;

        // SAVE RESPONSES FIRST - scoring queries need them in DB
        await _context.SaveChangesAsync();

        // Re-count completed after save
        session.CompletedCount = session.Attempts.Count(a => a.Status == AttemptStatus.Completed);

        // Check if all sub-tests are completed
        var totalSubTests = await _context.Tests.CountAsync(t => t.TestType != TestType.StreamSelector && t.IsActive);
        session.AllCompleted = session.CompletedCount >= totalSubTests;

        Guid? reportId = null;
        string? nextTestCode = null;

        if (session.AllCompleted)
        {
            // Score the full assessment
            session.CompletedAt = DateTime.UtcNow;
            reportId = Guid.NewGuid();
            session.SavedReportId = reportId;

            // Compute scores across ALL attempts (responses are now in DB)
            await ComputeFullAssessmentScoresAsync(session);
        }
        else
        {
            // Find next available test
            var completedTestIds = session.Attempts
                .Where(a => a.Status == AttemptStatus.Completed)
                .Select(a => a.TestId).ToList();

            var nextTest = await _context.Tests
                .Where(t => t.TestType != TestType.StreamSelector && t.IsActive
                         && !completedTestIds.Contains(t.Id))
                .OrderBy(t => t.DisplayOrder)
                .FirstOrDefaultAsync();
            nextTestCode = nextTest?.Code;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            TotalQuestions = dto.Answers.Count,
            AllCompleted = session.AllCompleted,
            SavedReportId = reportId,
            NextTestCode = nextTestCode,
            CompletedCount = session.CompletedCount,
            TotalCount = totalSubTests
        }));
    }

    /// <summary>
    /// Get the assessment report (only available after all 9 sub-tests are completed)
    /// </summary>
    [HttpGet("report")]
    public async Task<IActionResult> GetReport()
    {
        var studentId = await GetStudentIdAsync();
        if (studentId == 0) return BadRequest(ApiResponse<object>.Fail("Student profile not found."));

        var session = await _context.AssessmentSessions
            .Include(s => s.Attempts)
            .Include(s => s.Student)
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.AllCompleted);

        if (session == null)
            return BadRequest(ApiResponse<object>.Fail("Assessment not yet completed. Complete all 9 sub-tests first."));

        // Get scores from first completed attempt (where scoring was stored)
        var attemptIds = session.Attempts.Select(a => a.Id).ToList();

        var interestScores = await _context.InterestScores
            .Include(s => s.InterestCategory)
            .Where(s => attemptIds.Contains(s.AttemptId))
            .ToListAsync();

        var aptitudeScores = await _context.AptitudeScores
            .Include(s => s.AptitudeCategory)
            .Where(s => attemptIds.Contains(s.AttemptId))
            .ToListAsync();

        var careerScores = await _context.CareerSuitabilityScores
            .Include(s => s.Career).ThenInclude(c => c.Stream)
            .Where(s => attemptIds.Contains(s.AttemptId))
            .ToListAsync();

        var report = new
        {
            StudentName = $"{session.Student.FirstName} {session.Student.LastName}",
            StudentClass = session.Student.CurrentClass,
            SchoolName = session.Student.SchoolName,
            TestDate = session.CompletedAt ?? session.CreatedAt,
            session.OverallIqScore,
            IqCategory = session.IqCategory ?? "",

            Top3Interests = interestScores.OrderBy(s => s.RankOrder).Take(3)
                .Select(s => new { s.InterestCategory.Code, s.InterestCategory.Name, s.PercentileScore, s.RankOrder }).ToList(),

            Top3Aptitudes = aptitudeScores.OrderBy(s => s.RankOrder).Take(3)
                .Select(s => new { s.AptitudeCategory.Code, s.AptitudeCategory.Name, s.PercentileScore, s.RankOrder }).ToList(),

            AllInterestScores = interestScores.OrderBy(s => s.InterestCategory.DisplayOrder)
                .Select(s => new { s.InterestCategory.Code, s.InterestCategory.Name, s.InterestCategory.Description, s.RawScore, s.MaxPossibleScore, s.PercentileScore, s.RankOrder }).ToList(),

            AllAptitudeScores = aptitudeScores.OrderBy(s => s.AptitudeCategory.DisplayOrder)
                .Select(s => new { s.AptitudeCategory.Code, s.AptitudeCategory.Name, s.AptitudeCategory.Description, s.RawScore, s.MaxPossibleScore, s.PercentileScore, s.RankOrder }).ToList(),

            AllCareerSuitability = careerScores.OrderBy(s => s.RankOrder)
                .Select(s => new
                {
                    s.CareerId, CareerTitle = s.Career.Title, StreamName = s.Career.Stream?.Name ?? "",
                    s.SuitabilityPct, s.IsRecommended, s.IsCanBeConsidered, s.RankOrder,
                    s.Career.EducationPath, s.Career.EducationCostRange,
                    Tier = s.IsRecommended ? "Recommended" : s.IsCanBeConsidered ? "Considered" : "Not a fit"
                }).ToList(),

            RecommendedCareers = careerScores
                .Where(s => s.IsRecommended).OrderBy(s => s.RankOrder)
                .Select(s => new { s.Career.Title, StreamName = s.Career.Stream?.Name ?? "", s.SuitabilityPct, s.Career.EducationPath, s.Career.EducationCostRange }).ToList(),

            CareersToConsider = careerScores
                .Where(s => s.IsCanBeConsidered).OrderBy(s => s.RankOrder)
                .Select(s => new { s.Career.Title, StreamName = s.Career.Stream?.Name ?? "", s.SuitabilityPct, s.Career.EducationPath }).ToList()
        };

        return Ok(ApiResponse<object>.Ok(report));
    }

    /// <summary>
    /// Reset all sub-tests (start over)
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetAssessment()
    {
        var studentId = await GetStudentIdAsync();
        var session = await _context.AssessmentSessions
            .Include(s => s.Attempts).ThenInclude(a => a.Responses)
            .Include(s => s.InterestScores)
            .Include(s => s.AptitudeScores)
            .Include(s => s.CareerSuitabilityScores)
            .FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (session != null)
        {
            _context.CareerSuitabilityScores.RemoveRange(session.CareerSuitabilityScores);
            _context.AptitudeScores.RemoveRange(session.AptitudeScores);
            _context.InterestScores.RemoveRange(session.InterestScores);
            foreach (var attempt in session.Attempts)
                _context.TestResponses.RemoveRange(attempt.Responses);
            _context.TestAttempts.RemoveRange(session.Attempts);
            _context.AssessmentSessions.Remove(session);
            await _context.SaveChangesAsync();
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Assessment reset."));
    }

    private object BuildTestRunResponse(Test test, long attemptId)
    {
        int qNum = 0;
        var questions = test.Sections
            .OrderBy(s => s.SectionOrder)
            .SelectMany(section => section.Questions
                .OrderBy(sq => sq.QuestionOrder)
                .Select(sq =>
                {
                    qNum++;
                    return new
                    {
                        Id = sq.Question.Id,
                        Text = sq.Question.QuestionText,
                        Type = sq.Question.QuestionType == QuestionType.Likert ? "Likert" : "MCQ",
                        QuestionNumber = qNum,
                        Options = sq.Question.Options
                            .OrderBy(o => o.OptionOrder)
                            .Select(o => new { o.Id, Text = o.OptionText })
                            .ToList()
                    };
                })).ToList();

        return new
        {
            AttemptId = attemptId,
            test.Title,
            test.Description,
            TotalQuestions = questions.Count,
            Questions = questions
        };
    }

    private async Task ComputeFullAssessmentScoresAsync(AssessmentSession session)
    {
        // Gather all responses from all sub-test attempts
        var allAttemptIds = session.Attempts
            .Where(a => a.Status == AttemptStatus.Completed)
            .Select(a => a.Id).ToList();

        var allResponses = await _context.TestResponses
            .Include(r => r.Question).ThenInclude(q => q.Options)
            .Where(r => allAttemptIds.Contains(r.AttemptId))
            .ToListAsync();

        // Interest scores
        var interestCats = await _context.InterestCategories.ToListAsync();
        var interestScores = new List<InterestScore>();
        foreach (var cat in interestCats)
        {
            var catResponses = allResponses.Where(r => r.Question.SectionType == SectionType.Interest && r.Question.InterestCategoryId == cat.Id).ToList();
            var rawScore = catResponses.Sum(r => r.ScoreObtained);
            var maxScore = catResponses.Sum(r => r.Question.MaxScore);
            interestScores.Add(new InterestScore
            {
                AttemptId = allAttemptIds.First(), // Link to first attempt for compatibility
                InterestCategoryId = cat.Id,
                RawScore = rawScore,
                MaxPossibleScore = maxScore,
                PercentileScore = maxScore > 0 ? Math.Round(rawScore / maxScore * 100, 2) : 0
            });
        }
        var rankedI = interestScores.OrderByDescending(s => s.PercentileScore).ToList();
        for (int i = 0; i < rankedI.Count; i++) rankedI[i].RankOrder = i + 1;

        // Aptitude scores
        var aptitudeCats = await _context.AptitudeCategories.ToListAsync();
        var aptitudeScores = new List<AptitudeScore>();
        foreach (var cat in aptitudeCats)
        {
            var catResponses = allResponses.Where(r => r.Question.SectionType == SectionType.Aptitude && r.Question.AptitudeCategoryId == cat.Id).ToList();
            var correct = catResponses.Count(r => r.SelectedOptionId.HasValue && r.Question.Options.Any(o => o.Id == r.SelectedOptionId && o.IsCorrect));
            var total = catResponses.Count;
            aptitudeScores.Add(new AptitudeScore
            {
                AttemptId = allAttemptIds.First(),
                AptitudeCategoryId = cat.Id,
                RawScore = correct,
                MaxPossibleScore = total,
                PercentileScore = total > 0 ? Math.Round((decimal)correct / total * 100, 2) : 0
            });
        }
        var rankedA = aptitudeScores.OrderByDescending(s => s.PercentileScore).ToList();
        for (int i = 0; i < rankedA.Count; i++) rankedA[i].RankOrder = i + 1;

        // IQ
        var avgApt = aptitudeScores.Any() ? (double)aptitudeScores.Average(s => s.PercentileScore) : 0;
        var iqScore = (int)Math.Round(60 + avgApt * 0.8);
        iqScore = Math.Clamp(iqScore, 60, 140);
        var iqCat = iqScore switch { >= 130 => "Superior", >= 120 => "Superior", >= 110 => "AboveAverage", >= 90 => "Average", >= 80 => "BelowAverage", _ => "NeedsImprovement" };

        // Career suitability
        var careers = await _context.Careers.Include(c => c.InterestWeights).Include(c => c.AptitudeWeights).Include(c => c.Stream).Where(c => c.IsPublished).ToListAsync();
        var careerScores = new List<CareerSuitabilityScore>();
        foreach (var career in careers)
        {
            var iAvg = career.InterestWeights.Any() ? career.InterestWeights.Average(w => {
                var s = interestScores.FirstOrDefault(x => x.InterestCategoryId == w.InterestCategoryId);
                return s != null ? (double)(w.Weight * s.PercentileScore) : 0;
            }) / career.InterestWeights.Average(w => (double)w.Weight) : 0;

            var aAvg = career.AptitudeWeights.Any() ? career.AptitudeWeights.Average(w => {
                var s = aptitudeScores.FirstOrDefault(x => x.AptitudeCategoryId == w.AptitudeCategoryId);
                return s != null ? (double)(w.Weight * s.PercentileScore) : 0;
            }) / career.AptitudeWeights.Average(w => (double)w.Weight) : 0;

            var iqFit = iqScore >= 120 ? 100 : iqScore >= 100 ? 90 : iqScore >= 80 ? 70 : 40;
            var suitability = Math.Round(iAvg * 0.50 + aAvg * 0.40 + iqFit * 0.10, 2);
            var pct = Math.Clamp((decimal)suitability, 0, 100);

            careerScores.Add(new CareerSuitabilityScore
            {
                AttemptId = allAttemptIds.First(),
                CareerId = career.Id,
                SuitabilityPct = pct,
                IsRecommended = pct >= 80,
                IsCanBeConsidered = pct >= 65 && pct < 80
            });
        }
        var rankedC = careerScores.OrderByDescending(s => s.SuitabilityPct).ToList();
        for (int i = 0; i < rankedC.Count; i++) rankedC[i].RankOrder = i + 1;

        // Save to session
        session.OverallIqScore = iqScore;
        session.IqCategory = iqCat;
        session.InterestScores = rankedI;
        session.AptitudeScores = rankedA;
        session.CareerSuitabilityScores = rankedC;
    }

    private async Task<long> GetStudentIdAsync()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var student = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == userId);
        return student?.Id ?? 0;
    }
}

public class SubmitTestDto
{
    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    public long QuestionId { get; set; }
    public long? SelectedOptionId { get; set; }
}
