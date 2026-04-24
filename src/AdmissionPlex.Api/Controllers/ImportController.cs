using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ImportController : ControllerBase
{
    private readonly AppDbContext _context;

    public ImportController(AppDbContext context) => _context = context;

    // ─────────────── PRIMARY IMPORT: per-section JSON ───────────────
    // Accepts the exact format:
    // {
    //   "section": "number_calculation",
    //   "aptitude_code": "NC",            ← for aptitude/IQ questions
    //   "interest_code": "FA",            ← for interest questions
    //   "section_type": "quiz",           ← for career quiz questions
    //   "questions": [
    //     {
    //       "question_en": "ADD (29 + 21)",
    //       "question_mr": "बेरीज करा",
    //       "options": { "a": "50", "b": "51", "c": "49", "d": "52" },
    //       "correct_answer": "a",
    //       "stream": "Science"            ← only for quiz questions
    //     }
    //   ]
    // }

    [HttpPost("section")]
    public async Task<IActionResult> ImportSection([FromBody] JsonElement json)
    {
        try
        {
            var raw = json.GetRawText();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<SectionImport>(raw, opts);
            if (data?.Questions == null || !data.Questions.Any())
                return BadRequest(ApiResponse<object>.Fail("No questions found."));

            // Detect section type
            var sectionKind = DetectSectionKind(data);

            long? interestCatId = null;
            long? aptitudeCatId = null;

            if (sectionKind == "aptitude" && !string.IsNullOrEmpty(data.AptitudeCode))
            {
                var cat = await _context.AptitudeCategories.FirstOrDefaultAsync(c => c.Code == data.AptitudeCode);
                if (cat == null) return BadRequest(ApiResponse<object>.Fail($"Aptitude code '{data.AptitudeCode}' not found. Valid: SA, NC, MA, NA, VA, LA, SP"));
                aptitudeCatId = cat.Id;
            }

            if (sectionKind == "interest" && !string.IsNullOrEmpty(data.InterestCode))
            {
                var cat = await _context.InterestCategories.FirstOrDefaultAsync(c => c.Code == data.InterestCode);
                if (cat != null) interestCatId = cat.Id;
            }

            int created = 0, skipped = 0;

            foreach (var q in data.Questions)
            {
                var text = q.QuestionEn ?? q.Text ?? "";
                if (string.IsNullOrWhiteSpace(text)) { skipped++; continue; }
                if (await _context.Questions.AnyAsync(x => x.QuestionText == text)) { skipped++; continue; }

                // Per-question interest code override
                long? qInterestId = interestCatId;
                if (!string.IsNullOrEmpty(q.InterestCode))
                {
                    var c = await _context.InterestCategories.FirstOrDefaultAsync(x => x.Code == q.InterestCode);
                    if (c != null) qInterestId = c.Id;
                }

                var question = new Question
                {
                    QuestionText = text,
                    QuestionType = sectionKind == "interest" ? QuestionType.Likert : QuestionType.Mcq,
                    SectionType = sectionKind == "quiz" ? SectionType.StreamSelector : sectionKind == "interest" ? SectionType.Interest : SectionType.Aptitude,
                    InterestCategoryId = sectionKind == "interest" ? qInterestId : null,
                    AptitudeCategoryId = sectionKind == "aptitude" ? aptitudeCatId : null,
                    Difficulty = DifficultyLevel.Medium,
                    MaxScore = sectionKind == "interest" ? 5 : sectionKind == "quiz" ? 3 : 1,
                    IsActive = true,
                    CreatedBy = 1
                };

                if (sectionKind == "interest")
                {
                    question.Options.Add(new QuestionOption { OptionText = "Strongly Agree", OptionOrder = 1, ScoreValue = 5 });
                    question.Options.Add(new QuestionOption { OptionText = "Agree", OptionOrder = 2, ScoreValue = 4 });
                    question.Options.Add(new QuestionOption { OptionText = "Neutral", OptionOrder = 3, ScoreValue = 3 });
                    question.Options.Add(new QuestionOption { OptionText = "Disagree", OptionOrder = 2, ScoreValue = 2 });
                    question.Options.Add(new QuestionOption { OptionText = "Strongly Disagree", OptionOrder = 5, ScoreValue = 1 });
                }
                else if (sectionKind == "quiz" && q.Options.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                {
                    // Quiz: each option tagged to a stream
                    int ord = 1;
                    foreach (var (letter, optText) in GetOptionPairs(q.Options))
                    {
                        StreamType? stream = null;
                        // If per-option stream tag provided
                        if (q.OptionStreams != null && q.OptionStreams.TryGetValue(letter, out var streamStr))
                            stream = ParseStream(streamStr);
                        else if (!string.IsNullOrEmpty(q.Stream))
                            stream = ParseStream(q.Stream);

                        question.Options.Add(new QuestionOption
                        {
                            OptionText = optText, OptionOrder = ord++,
                            StreamTag = stream, ScoreValue = 1
                        });
                    }
                }
                else if (q.Options.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                {
                    // Aptitude MCQ: options a/b/c/d, one correct
                    var correctLetter = (q.CorrectAnswer ?? "a").ToLower().Trim();
                    int ord = 1;
                    foreach (var (letter, optText) in GetOptionPairs(q.Options))
                    {
                        question.Options.Add(new QuestionOption
                        {
                            OptionText = optText, OptionOrder = ord++,
                            IsCorrect = letter == correctLetter,
                            ScoreValue = letter == correctLetter ? 1 : 0
                        });
                    }
                }

                _context.Questions.Add(question);
                created++;
            }

            await _context.SaveChangesAsync();
            await RebuildTestSectionsAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                Section = data.Section,
                SectionKind = sectionKind,
                QuestionsCreated = created,
                QuestionsSkipped = skipped,
                TotalInBank = await _context.Questions.CountAsync(q => q.IsActive)
            }));
        }
        catch (Exception ex) { return BadRequest(ApiResponse<object>.Fail($"Import failed: {ex.Message}")); }
    }

    // ─────────────── AUTO-DETECT: accepts any format ───────────────
    [HttpPost("questions-json")]
    public async Task<IActionResult> ImportAuto([FromBody] JsonElement json)
    {
        var raw = json.GetRawText();
        // If it has "section" + "questions" → new per-section format
        if (raw.Contains("\"section\"") && raw.Contains("\"questions\""))
            return await ImportSection(json);

        // Legacy format — just redirect to section with best guess
        return await ImportSection(json);
    }

    // ─────────────── SEED SAMPLE DATA ───────────────
    [HttpPost("seed-sample-data")]
    public async Task<IActionResult> SeedSampleData()
    {
        if (await _context.Questions.AnyAsync())
            return Ok(ApiResponse<object>.Ok(new { QuestionsCreated = 0, TestsCreated = 0, CareersCreated = 0, Message = "Data exists." }));

        await Data.Seed.DbSeeder.SeedAsync(_context);
        return Ok(ApiResponse<object>.Ok(new
        {
            QuestionsCreated = await _context.Questions.CountAsync(),
            TestsCreated = await _context.Tests.CountAsync(),
            CareersCreated = await _context.Careers.CountAsync()
        }));
    }

    // ─────────────── TEMPLATE ───────────────
    [HttpGet("template")]
    [AllowAnonymous]
    public IActionResult GetTemplate() => Ok(ApiResponse<object>.Ok(new
    {
        AptitudeExample = new { section = "number_calculation", aptitude_code = "NC", questions = new[] { new { question_en = "ADD (29 + 21)", question_mr = "बेरीज करा", options = new { a = "50", b = "51", c = "49", d = "52" }, correct_answer = "a" } } },
        InterestExample = new { section = "interest_fine_arts", section_type = "interest", interest_code = "FA", questions = new[] { new { question_en = "I enjoy drawing in my free time." } } },
        QuizExample = new { section = "career_quiz", section_type = "quiz", questions = new[] { new { question_en = "Which do you prefer?", options = new { a = "Lab experiments", b = "Business planning", c = "Creative writing" }, option_streams = new { a = "Science", b = "Commerce", c = "Arts" } } } }
    }));

    // ─────────────── QUIZ QUESTIONS API (public, for quiz page) ───────────────
    [HttpGet("quiz-questions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetQuizQuestions()
    {
        var questions = await _context.Questions
            .Include(q => q.Options.OrderBy(o => o.OptionOrder))
            .Where(q => q.SectionType == SectionType.StreamSelector && q.IsActive)
            .OrderBy(q => q.Id)
            .Select(q => new
            {
                q.Id,
                Text = q.QuestionText,
                Options = q.Options.OrderBy(o => o.OptionOrder).Select(o => new
                {
                    o.Id,
                    Text = o.OptionText,
                    Stream = o.StreamTag.HasValue ? o.StreamTag.Value.ToString() : null
                }).ToList()
            }).ToListAsync();

        return Ok(ApiResponse<object>.Ok(questions));
    }

    // ─────────────── HELPERS ───────────────
    private string DetectSectionKind(SectionImport data)
    {
        if (!string.IsNullOrEmpty(data.SectionType))
        {
            var t = data.SectionType.ToLower();
            if (t == "quiz" || t == "streamselector" || t == "stream_selector") return "quiz";
            if (t == "interest") return "interest";
            return "aptitude";
        }
        if (!string.IsNullOrEmpty(data.AptitudeCode)) return "aptitude";
        if (!string.IsNullOrEmpty(data.InterestCode)) return "interest";
        var sec = (data.Section ?? "").ToLower();
        if (sec.Contains("quiz") || sec.Contains("stream")) return "quiz";
        if (sec.Contains("interest")) return "interest";
        return "aptitude";
    }

    private static List<(string letter, string text)> GetOptionPairs(JsonElement options)
    {
        var pairs = new List<(string, string)>();
        foreach (var letter in new[] { "a", "b", "c", "d", "e" })
            if (options.TryGetProperty(letter, out var val) && val.ValueKind == JsonValueKind.String)
                pairs.Add((letter, val.GetString()!));
        return pairs;
    }

    private static StreamType? ParseStream(string? s) => s?.ToLower() switch
    {
        "science" => StreamType.Science,
        "commerce" => StreamType.Commerce,
        "arts" => StreamType.Arts,
        _ => null
    };

    private async Task RebuildTestSectionsAsync()
    {
        var tests = await _context.Tests.Include(t => t.Sections).ThenInclude(s => s.Questions).ToListAsync();
        foreach (var test in tests)
            foreach (var sec in test.Sections)
            {
                IQueryable<Question> q = _context.Questions.Where(x => x.IsActive);
                if (sec.InterestCategoryId.HasValue)
                    q = q.Where(x => x.SectionType == SectionType.Interest && x.InterestCategoryId == sec.InterestCategoryId);
                else if (sec.AptitudeCategoryId.HasValue)
                    q = q.Where(x => x.SectionType == SectionType.Aptitude && x.AptitudeCategoryId == sec.AptitudeCategoryId);
                else continue;

                var existing = sec.Questions.Select(x => x.QuestionId).ToHashSet();
                int maxOrd = sec.Questions.Any() ? sec.Questions.Max(x => x.QuestionOrder) : 0;
                foreach (var qn in await q.OrderBy(x => x.Id).ToListAsync())
                    if (!existing.Contains(qn.Id))
                        sec.Questions.Add(new TestSectionQuestion { QuestionId = qn.Id, QuestionOrder = ++maxOrd });

                test.TotalQuestions = test.Sections.Sum(s => s.Questions.Count);
            }
        await _context.SaveChangesAsync();
    }
}

// ─────── Import Models ───────
public class SectionImport
{
    [JsonPropertyName("section")] public string? Section { get; set; }
    [JsonPropertyName("section_type")] public string? SectionType { get; set; }
    [JsonPropertyName("aptitude_code")] public string? AptitudeCode { get; set; }
    [JsonPropertyName("interest_code")] public string? InterestCode { get; set; }
    [JsonPropertyName("questions")] public List<SectionQ>? Questions { get; set; }
}

public class SectionQ
{
    [JsonPropertyName("question_en")] public string? QuestionEn { get; set; }
    [JsonPropertyName("question_mr")] public string? QuestionMr { get; set; }
    [JsonPropertyName("text")] public string? Text { get; set; }
    [JsonPropertyName("interest_code")] public string? InterestCode { get; set; }
    [JsonPropertyName("stream")] public string? Stream { get; set; }
    [JsonPropertyName("options")] public JsonElement Options { get; set; }
    [JsonPropertyName("option_streams")] public Dictionary<string, string>? OptionStreams { get; set; }
    [JsonPropertyName("correct_answer")] public string? CorrectAnswer { get; set; }
    [JsonPropertyName("id")] public string? Id { get; set; }
}

public class LegacyImport
{
    public Dictionary<string, List<LegacyIQ>>? InterestQuestions { get; set; }
    public Dictionary<string, List<LegacyAQ>>? AptitudeQuestions { get; set; }
}
public class LegacyIQ { public string Text { get; set; } = ""; }
public class LegacyAQ { public string Text { get; set; } = ""; public List<LegacyOpt>? Options { get; set; } }
public class LegacyOpt { public string? Text { get; set; } public bool Correct { get; set; } public decimal Score { get; set; } }

// In ImportController class - but we need to add inside the class
