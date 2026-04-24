using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Entities.Careers;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Shared.Constants;

namespace AdmissionPlex.Api.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedInterestCategoriesAsync(context);
        await SeedAptitudeCategoriesAsync(context);
        await SeedCareerStreamsAsync(context);
        await context.SaveChangesAsync();

        // Auto-seed questions, tests, and careers on first startup
        if (!await context.Questions.AnyAsync())
            await SeedQuestionsAndTestsAsync(context);
    }

    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        string[] roles = { AppRoles.Admin, AppRoles.Student, AppRoles.Counsellor, AppRoles.Coordinator };
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole(role));

        const string adminEmail = "admin@admissionplex.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new AppUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, IsActive = true };
            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded) await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
        }
    }

    private static async Task SeedQuestionsAndTestsAsync(AppDbContext context)
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "sample_questions.json");
        if (!File.Exists(jsonPath)) jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Seed", "sample_questions.json");
        if (!File.Exists(jsonPath)) return;

        var json = await File.ReadAllTextAsync(jsonPath);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<SeedDataModel>(json, opts);
        if (data == null) return;

        var iCats = await context.InterestCategories.ToDictionaryAsync(c => c.Code, c => c.Id);
        var aCats = await context.AptitudeCategories.ToDictionaryAsync(c => c.Code, c => c.Id);

        // Stream Selector Questions
        if (data.StreamSelectorQuestions != null)
            foreach (var sq in data.StreamSelectorQuestions)
            {
                var q = new Question { QuestionText = sq.Text, QuestionType = QuestionType.Likert, SectionType = SectionType.StreamSelector, Difficulty = DifficultyLevel.Easy, MaxScore = 3, IsActive = true, CreatedBy = 1 };
                int o = 1;
                foreach (var op in sq.Options)
                {
                    StreamType? st = null;
                    if (!string.IsNullOrEmpty(op.Stream) && Enum.TryParse<StreamType>(op.Stream, true, out var s)) st = s;
                    q.Options.Add(new QuestionOption { OptionText = op.Text, OptionOrder = o++, ScoreValue = op.Score, StreamTag = st });
                }
                context.Questions.Add(q);
            }

        // Interest Questions
        if (data.InterestQuestions != null)
            foreach (var (code, qs) in data.InterestQuestions)
            {
                if (!iCats.TryGetValue(code, out var cId)) continue;
                foreach (var iq in qs)
                {
                    var q = new Question { QuestionText = iq.Text, QuestionType = QuestionType.Likert, SectionType = SectionType.Interest, InterestCategoryId = cId, Difficulty = DifficultyLevel.Easy, MaxScore = 5, IsActive = true, CreatedBy = 1 };
                    q.Options.Add(new QuestionOption { OptionText = "Strongly Agree", OptionOrder = 1, ScoreValue = 5 });
                    q.Options.Add(new QuestionOption { OptionText = "Agree", OptionOrder = 2, ScoreValue = 4 });
                    q.Options.Add(new QuestionOption { OptionText = "Neutral", OptionOrder = 3, ScoreValue = 3 });
                    q.Options.Add(new QuestionOption { OptionText = "Disagree", OptionOrder = 4, ScoreValue = 2 });
                    q.Options.Add(new QuestionOption { OptionText = "Strongly Disagree", OptionOrder = 5, ScoreValue = 1 });
                    context.Questions.Add(q);
                }
            }

        // Aptitude Questions
        if (data.AptitudeQuestions != null)
            foreach (var (code, qs) in data.AptitudeQuestions)
            {
                if (!aCats.TryGetValue(code, out var cId)) continue;
                foreach (var aq in qs)
                {
                    var q = new Question { QuestionText = aq.Text, QuestionType = QuestionType.Mcq, SectionType = SectionType.Aptitude, AptitudeCategoryId = cId, Difficulty = DifficultyLevel.Medium, MaxScore = 1, IsActive = true, CreatedBy = 1 };
                    int o = 1;
                    foreach (var op in aq.Options)
                        q.Options.Add(new QuestionOption { OptionText = op.Text, OptionOrder = o++, IsCorrect = op.Correct, ScoreValue = op.Score });
                    context.Questions.Add(q);
                }
            }

        await context.SaveChangesAsync();

        // Build 9 sequential sub-tests (matching reference project's TestDefinitionSeedCatalog)
        var interestCatsOrdered = await context.InterestCategories.OrderBy(c => c.DisplayOrder).ToListAsync();
        var aptitudeCatsOrdered = await context.AptitudeCategories.OrderBy(c => c.DisplayOrder).ToListAsync();

        var subTests = new List<(string code, string title, string desc, string icon, string category, TestType type, int duration, long[] interestCatIds, long[] aptitudeCatIds)>
        {
            ("interest_1", "Interest Survey — Part 1", "Discover your areas of interest across Fine Arts, Performing Arts, Machines, Methodical, and People Interaction.", "❤", "Interest", TestType.InterestSurvey, 10,
                interestCatsOrdered.Take(5).Select(c => c.Id).ToArray(), Array.Empty<long>()),

            ("speed_accuracy", "Speed & Accuracy Test", "Test your ability for quick and accurate decision making.", "⚡", "Aptitude", TestType.AptitudeTest, 5,
                Array.Empty<long>(), new[] { aptitudeCatsOrdered.First(c => c.Code == "SA").Id }),

            ("number_calc", "Number Calculation Test", "Test your basic mathematical calculation skills.", "🔢", "Aptitude", TestType.AptitudeTest, 5,
                Array.Empty<long>(), new[] { aptitudeCatsOrdered.First(c => c.Code == "NC").Id }),

            ("mechanical", "Mechanical Ability Test", "Test your understanding of mechanical and scientific principles.", "⚙", "Aptitude", TestType.AptitudeTest, 10,
                Array.Empty<long>(), new[] { aptitudeCatsOrdered.First(c => c.Code == "MA").Id }),

            ("numerical_app", "Numerical Application Test", "Apply mathematical principles to solve real problems.", "√x", "Aptitude", TestType.AptitudeTest, 15,
                Array.Empty<long>(), new[] { aptitudeCatsOrdered.First(c => c.Code == "NA").Id }),

            ("verbal", "Verbal Ability Test", "Test your language proficiency and vocabulary.", "Ab", "Aptitude", TestType.AptitudeTest, 5,
                Array.Empty<long>(), new[] { aptitudeCatsOrdered.First(c => c.Code == "VA").Id }),

            ("logical", "Logical Reasoning Test", "Test your logical and analytical thinking abilities.", "◐", "IQ", TestType.IQTest, 10,
                Array.Empty<long>(), new[] { aptitudeCatsOrdered.First(c => c.Code == "LA").Id }),

            ("spatial", "Spatial Ability Test", "Test your ability to visualize shapes and objects in multiple dimensions.", "◆", "IQ", TestType.IQTest, 10,
                Array.Empty<long>(), new[] { aptitudeCatsOrdered.First(c => c.Code == "SP").Id }),

            ("interest_2", "Interest Survey — Part 2", "Complete your interest profile across Social, Numbers, Research, Language, and Outdoor.", "♥", "Interest", TestType.InterestSurvey, 10,
                interestCatsOrdered.Skip(5).Take(5).Select(c => c.Id).ToArray(), Array.Empty<long>()),
        };

        int displayOrder = 1;
        foreach (var (code, title, desc, icon, category, testType, duration, iCatIds, aCatIds) in subTests)
        {
            if (await context.Tests.AnyAsync(t => t.Code == code)) { displayOrder++; continue; }

            var test = new Test
            {
                Code = code, Title = title, Slug = code.Replace("_", "-"),
                TestType = testType, Category = category, Description = desc,
                Icon = icon, DisplayOrder = displayOrder++,
                DurationMinutes = duration, Price = 0, IsActive = true
            };

            int secOrder = 1;
            // Interest sections
            foreach (var catId in iCatIds)
            {
                var cat = interestCatsOrdered.First(c => c.Id == catId);
                var sec = new TestSection { Title = cat.Name, SectionType = SectionType.Interest, SectionOrder = secOrder++, InterestCategoryId = catId };
                int qo = 1;
                foreach (var q in await context.Questions.Where(q => q.SectionType == SectionType.Interest && q.InterestCategoryId == catId && q.IsActive).OrderBy(q => q.Id).ToListAsync())
                    sec.Questions.Add(new TestSectionQuestion { QuestionId = q.Id, QuestionOrder = qo++ });
                test.Sections.Add(sec);
            }
            // Aptitude sections
            foreach (var catId in aCatIds)
            {
                var cat = aptitudeCatsOrdered.First(c => c.Id == catId);
                var sec = new TestSection { Title = cat.Name, SectionType = SectionType.Aptitude, SectionOrder = secOrder++, AptitudeCategoryId = catId };
                int qo = 1;
                foreach (var q in await context.Questions.Where(q => q.SectionType == SectionType.Aptitude && q.AptitudeCategoryId == catId && q.IsActive).OrderBy(q => q.Id).ToListAsync())
                    sec.Questions.Add(new TestSectionQuestion { QuestionId = q.Id, QuestionOrder = qo++ });
                test.Sections.Add(sec);
            }

            test.TotalQuestions = test.Sections.Sum(s => s.Questions.Count);
            context.Tests.Add(test);
        }
        await context.SaveChangesAsync();

        // Seed Careers
        if (data.Careers != null)
        {
            var streams = await context.CareerStreams.ToDictionaryAsync(s => s.Name, s => s.Id);
            foreach (var cd in data.Careers)
            {
                if (!streams.TryGetValue(cd.Stream, out var sId)) continue;
                Enum.TryParse<GrowthOutlook>(cd.Growth, true, out var gr);
                var career = new Career
                {
                    Title = cd.Title, Slug = cd.Title.ToLower().Replace(" ", "-").Replace("(", "").Replace(")", "").Replace("/", "-"),
                    StreamId = sId, Summary = cd.Summary, Description = cd.Summary,
                    EducationPath = cd.EducationPath, EducationCostRange = cd.EducationCost,
                    AvgSalaryMin = cd.SalaryMin, AvgSalaryMax = cd.SalaryMax,
                    GrowthOutlook = gr, SuitabilityCutoffPct = 65, IsPublished = true
                };
                context.Careers.Add(career);
                await context.SaveChangesAsync();

                if (cd.InterestWeights != null)
                    foreach (var (c, w) in cd.InterestWeights)
                        if (iCats.TryGetValue(c, out var cId))
                            context.CareerInterestWeights.Add(new CareerInterestWeight { CareerId = career.Id, InterestCategoryId = cId, Weight = (decimal)w, MinPercentile = 20 });
                if (cd.AptitudeWeights != null)
                    foreach (var (c, w) in cd.AptitudeWeights)
                        if (aCats.TryGetValue(c, out var cId))
                            context.CareerAptitudeWeights.Add(new CareerAptitudeWeight { CareerId = career.Id, AptitudeCategoryId = cId, Weight = (decimal)w, MinPercentile = 15 });
            }
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedInterestCategoriesAsync(AppDbContext context)
    {
        if (await context.InterestCategories.AnyAsync()) return;
        context.InterestCategories.AddRange(
            new InterestCategory { Code = "FA", Name = "Fine Arts", Description = "Drawing, painting, etc.", DisplayOrder = 1 },
            new InterestCategory { Code = "PA", Name = "Performing Arts", Description = "Singing, dancing, acting.", DisplayOrder = 2 },
            new InterestCategory { Code = "MT", Name = "Machines & Tools", Description = "Working with machines.", DisplayOrder = 3 },
            new InterestCategory { Code = "ME", Name = "Methodical", Description = "Meticulous activities.", DisplayOrder = 4 },
            new InterestCategory { Code = "PI", Name = "People Interaction", Description = "Convincing people.", DisplayOrder = 5 },
            new InterestCategory { Code = "SO", Name = "Social", Description = "Social causes.", DisplayOrder = 6 },
            new InterestCategory { Code = "WN", Name = "Working With Numbers", Description = "Number activities.", DisplayOrder = 7 },
            new InterestCategory { Code = "RA", Name = "Research & Analysis", Description = "Scientific activities.", DisplayOrder = 8 },
            new InterestCategory { Code = "LU", Name = "Language Usage", Description = "Language activities.", DisplayOrder = 9 },
            new InterestCategory { Code = "OS", Name = "Outdoor & Sports", Description = "Outdoor activities.", DisplayOrder = 10 }
        );
    }

    private static async Task SeedAptitudeCategoriesAsync(AppDbContext context)
    {
        if (await context.AptitudeCategories.AnyAsync()) return;
        context.AptitudeCategories.AddRange(
            new AptitudeCategory { Code = "SA", Name = "Speed & Accuracy", Description = "Quick decision making.", DisplayOrder = 1 },
            new AptitudeCategory { Code = "NC", Name = "Number Calculations", Description = "Math calculations.", DisplayOrder = 2 },
            new AptitudeCategory { Code = "MA", Name = "Mechanical Ability", Description = "Scientific principles.", DisplayOrder = 3 },
            new AptitudeCategory { Code = "NA", Name = "Number Application", Description = "Practical math problems.", DisplayOrder = 4 },
            new AptitudeCategory { Code = "VA", Name = "Verbal Ability", Description = "Language proficiency.", DisplayOrder = 5 },
            new AptitudeCategory { Code = "LA", Name = "Logical Ability", Description = "Logical data interpretation.", DisplayOrder = 6 },
            new AptitudeCategory { Code = "SP", Name = "Spatial Ability", Description = "Shape visualization.", DisplayOrder = 7 }
        );
    }

    private static async Task SeedCareerStreamsAsync(AppDbContext context)
    {
        if (await context.CareerStreams.AnyAsync()) return;
        context.CareerStreams.AddRange(
            new CareerStream { Name = "Science", Description = "Science, technology, engineering, mathematics." },
            new CareerStream { Name = "Commerce", Description = "Business, finance, accounting, economics." },
            new CareerStream { Name = "Arts", Description = "Humanities, social sciences, languages, creative." }
        );
    }
}

public class SeedDataModel
{
    public List<SeedStreamQ>? StreamSelectorQuestions { get; set; }
    public Dictionary<string, List<SeedInterestQ>>? InterestQuestions { get; set; }
    public Dictionary<string, List<SeedAptitudeQ>>? AptitudeQuestions { get; set; }
    public List<SeedCareer>? Careers { get; set; }
}
public class SeedStreamQ { public string Text { get; set; } = ""; public List<SeedStreamOpt> Options { get; set; } = new(); }
public class SeedStreamOpt { public string Text { get; set; } = ""; public decimal Score { get; set; } public string? Stream { get; set; } }
public class SeedInterestQ { public string Text { get; set; } = ""; public string Type { get; set; } = "Likert"; }
public class SeedAptitudeQ { public string Text { get; set; } = ""; public List<SeedAptOpt> Options { get; set; } = new(); }
public class SeedAptOpt { public string Text { get; set; } = ""; public bool Correct { get; set; } public decimal Score { get; set; } }
public class SeedCareer
{
    public string Title { get; set; } = ""; public string Stream { get; set; } = ""; public string Summary { get; set; } = "";
    public string? EducationPath { get; set; } public string? EducationCost { get; set; }
    public decimal SalaryMin { get; set; } public decimal SalaryMax { get; set; } public string Growth { get; set; } = "Medium";
    public Dictionary<string, double>? InterestWeights { get; set; } public Dictionary<string, double>? AptitudeWeights { get; set; }
}
