using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Entities.Careers;
using AdmissionPlex.Core.Entities.Settings;
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
        await SeedDefaultSettingsAsync(context);
        await SeedNotificationTemplatesAsync(context);
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

    private static async Task SeedDefaultSettingsAsync(AppDbContext context)
    {
        if (await context.AppSettings.AnyAsync()) return;

        var defaults = new List<AppSetting>
        {
            // SMTP
            new() { Category = "smtp", Key = "Host", Value = "", IsEnabled = false, Description = "SMTP server hostname (e.g. smtp.gmail.com)" },
            new() { Category = "smtp", Key = "Port", Value = "587", IsEnabled = false, Description = "SMTP port (587 for TLS, 465 for SSL)" },
            new() { Category = "smtp", Key = "Username", Value = "", IsEnabled = false, Description = "SMTP username / email" },
            new() { Category = "smtp", Key = "Password", Value = "", IsSensitive = true, IsEnabled = false, Description = "SMTP password or app password" },
            new() { Category = "smtp", Key = "FromEmail", Value = "", IsEnabled = false, Description = "Sender email address" },
            new() { Category = "smtp", Key = "FromName", Value = "AdmissionPlex", IsEnabled = false, Description = "Sender display name" },
            new() { Category = "smtp", Key = "EnableSsl", Value = "true", IsEnabled = false, Description = "Enable SSL/TLS" },

            // SMS
            new() { Category = "sms", Key = "Provider", Value = "msg91", IsEnabled = false, Description = "SMS provider: msg91, twilio, textlocal" },
            new() { Category = "sms", Key = "ApiKey", Value = "", IsSensitive = true, IsEnabled = false, Description = "API key / Auth key" },
            new() { Category = "sms", Key = "SenderId", Value = "ADMPLX", IsEnabled = false, Description = "SMS sender ID (6 chars)" },
            new() { Category = "sms", Key = "TemplateId", Value = "", IsEnabled = false, Description = "MSG91 template ID (DLT registered)" },
            new() { Category = "sms", Key = "AccountSid", Value = "", IsEnabled = false, Description = "Twilio Account SID (if using Twilio)" },
            new() { Category = "sms", Key = "FromNumber", Value = "", IsEnabled = false, Description = "Twilio From number (if using Twilio)" },

            // WhatsApp
            new() { Category = "whatsapp", Key = "Provider", Value = "meta", IsEnabled = false, Description = "WhatsApp provider: meta, interakt, wati" },
            new() { Category = "whatsapp", Key = "ApiKey", Value = "", IsSensitive = true, IsEnabled = false, Description = "API token / access token" },
            new() { Category = "whatsapp", Key = "PhoneNumberId", Value = "", IsEnabled = false, Description = "Meta WhatsApp phone number ID" },
            new() { Category = "whatsapp", Key = "BaseUrl", Value = "", IsEnabled = false, Description = "Wati base URL (if using Wati)" },

            // Push (Firebase)
            new() { Category = "push", Key = "ServerKey", Value = "", IsSensitive = true, IsEnabled = false, Description = "Firebase Cloud Messaging server key" },
            new() { Category = "push", Key = "ProjectId", Value = "", IsEnabled = false, Description = "Firebase project ID" },

            // Google Auth
            new() { Category = "google_auth", Key = "ClientId", Value = "", IsEnabled = false, Description = "Google OAuth 2.0 Client ID" },
            new() { Category = "google_auth", Key = "ClientSecret", Value = "", IsSensitive = true, IsEnabled = false, Description = "Google OAuth 2.0 Client Secret (not used for ID token flow, kept for reference)" },
            new() { Category = "google_auth", Key = "AutoRegister", Value = "true", IsEnabled = false, Description = "Auto-create account on first Google login" },
        };

        context.AppSettings.AddRange(defaults);
    }

    private static async Task SeedNotificationTemplatesAsync(AppDbContext context)
    {
        if (await context.NotificationTemplates.AnyAsync()) return;

        var templates = new List<NotificationTemplate>
        {
            // Welcome email
            new()
            {
                Code = "welcome", Name = "Welcome Email", Channel = "email",
                Subject = "Welcome to AdmissionPlex, {{StudentName}}!",
                BodyHtml = """
                    <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
                        <h2 style="color:#4f46e5;">Welcome to AdmissionPlex!</h2>
                        <p>Hi {{StudentName}},</p>
                        <p>Thank you for registering. You can now take the psychometric career assessment
                           to discover your ideal career path.</p>
                        <p><a href="{{SiteUrl}}/student/assessment" style="background:#4f46e5;color:#fff;padding:12px 24px;
                           border-radius:8px;text-decoration:none;display:inline-block;">Start Assessment →</a></p>
                        <p style="color:#666;font-size:13px;">— Team AdmissionPlex</p>
                    </div>
                    """,
                BodyText = "Welcome to AdmissionPlex, {{StudentName}}! Start your career assessment at {{SiteUrl}}/student/assessment",
                IsActive = true
            },
            // Welcome SMS
            new()
            {
                Code = "welcome", Name = "Welcome SMS", Channel = "sms",
                BodyText = "Welcome to AdmissionPlex, {{StudentName}}! Start your career assessment now. {{SiteUrl}}",
                IsActive = true
            },
            // Test completion
            new()
            {
                Code = "test_complete", Name = "Test Completed Email", Channel = "email",
                Subject = "You completed {{TestName}} — AdmissionPlex",
                BodyHtml = """
                    <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
                        <h2 style="color:#4f46e5;">Section Completed!</h2>
                        <p>Hi {{StudentName}},</p>
                        <p>You have successfully completed <strong>{{TestName}}</strong>.</p>
                        <p>Progress: {{CompletedCount}} / {{TotalCount}} sections done.</p>
                        <p><a href="{{SiteUrl}}/student/assessment" style="background:#4f46e5;color:#fff;padding:12px 24px;
                           border-radius:8px;text-decoration:none;display:inline-block;">Continue Assessment →</a></p>
                    </div>
                    """,
                IsActive = true
            },
            // Report ready
            new()
            {
                Code = "report_ready", Name = "Report Ready Email", Channel = "email",
                Subject = "Your Career Report is Ready — AdmissionPlex",
                BodyHtml = """
                    <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
                        <h2 style="color:#22c55e;">Your Career Report is Ready!</h2>
                        <p>Hi {{StudentName}},</p>
                        <p>Congratulations! You have completed all sections of the psychometric assessment.
                           Your detailed career report is now available.</p>
                        <p><a href="{{SiteUrl}}/student/assessment/report" style="background:#22c55e;color:#fff;padding:12px 24px;
                           border-radius:8px;text-decoration:none;display:inline-block;">View Report →</a></p>
                    </div>
                    """,
                BodyText = "Hi {{StudentName}}, your career report is ready! View it at {{SiteUrl}}/student/assessment/report",
                IsActive = true
            },
            // Report ready push
            new()
            {
                Code = "report_ready", Name = "Report Ready Push", Channel = "push",
                PushTitle = "Career Report Ready!",
                BodyText = "Your psychometric career report is ready to view.",
                ActionUrl = "/student/assessment/report",
                IsActive = true
            },
            // Payment success
            new()
            {
                Code = "payment_success", Name = "Payment Success Email", Channel = "email",
                Subject = "Payment Received — Order {{OrderId}}",
                BodyHtml = """
                    <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
                        <h2 style="color:#22c55e;">Payment Successful</h2>
                        <p>Hi {{StudentName}},</p>
                        <p>We have received your payment of <strong>₹{{Amount}}</strong> (Order: {{OrderId}}).</p>
                        <p>You now have full access to your detailed career report.</p>
                    </div>
                    """,
                BodyText = "Payment of ₹{{Amount}} received. Order: {{OrderId}}. — AdmissionPlex",
                IsActive = true
            },
            // Payment success SMS
            new()
            {
                Code = "payment_success", Name = "Payment Success SMS", Channel = "sms",
                BodyText = "AdmissionPlex: Payment of Rs.{{Amount}} received. Order: {{OrderId}}. Access your full report now.",
                IsActive = true
            },
        };

        context.NotificationTemplates.AddRange(templates);
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
