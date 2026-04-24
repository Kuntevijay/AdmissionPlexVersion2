using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AdmissionPlex.Core.Entities.Tests;

namespace AdmissionPlex.Api.Data.Configurations;

public class InterestCategoryConfiguration : IEntityTypeConfiguration<InterestCategory>
{
    public void Configure(EntityTypeBuilder<InterestCategory> builder)
    {
        builder.ToTable("interest_categories");
        builder.HasIndex(i => i.Code).IsUnique();
        builder.Property(i => i.Code).HasMaxLength(10);
        builder.Property(i => i.Name).HasMaxLength(100);
    }
}

public class AptitudeCategoryConfiguration : IEntityTypeConfiguration<AptitudeCategory>
{
    public void Configure(EntityTypeBuilder<AptitudeCategory> builder)
    {
        builder.ToTable("aptitude_categories");
        builder.HasIndex(a => a.Code).IsUnique();
        builder.Property(a => a.Code).HasMaxLength(10);
        builder.Property(a => a.Name).HasMaxLength(100);
    }
}

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");
        builder.HasIndex(q => q.Uuid).IsUnique();
        builder.HasOne(q => q.InterestCategory).WithMany().HasForeignKey(q => q.InterestCategoryId);
        builder.HasOne(q => q.AptitudeCategory).WithMany().HasForeignKey(q => q.AptitudeCategoryId);
        builder.Property(q => q.QuestionType).HasConversion<string>().HasMaxLength(15);
        builder.Property(q => q.SectionType).HasConversion<string>().HasMaxLength(20);
        builder.Property(q => q.Difficulty).HasConversion<string>().HasMaxLength(10);
        builder.Property(q => q.Weightage).HasPrecision(5, 2);
        builder.Property(q => q.MaxScore).HasPrecision(5, 2);
    }
}

public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.ToTable("question_options");
        builder.HasOne(o => o.Question).WithMany(q => q.Options).HasForeignKey(o => o.QuestionId);
        builder.Property(o => o.OptionText).HasMaxLength(500);
        builder.Property(o => o.ScoreValue).HasPrecision(5, 2);
        builder.Property(o => o.StreamTag).HasConversion<string>().HasMaxLength(15);
    }
}

public class TestConfiguration : IEntityTypeConfiguration<Test>
{
    public void Configure(EntityTypeBuilder<Test> builder)
    {
        builder.ToTable("tests");
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.Title).HasMaxLength(255);
        builder.Property(t => t.Slug).HasMaxLength(255);
        builder.Property(t => t.TestType).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Price).HasPrecision(10, 2);
    }
}

public class TestSectionConfiguration : IEntityTypeConfiguration<TestSection>
{
    public void Configure(EntityTypeBuilder<TestSection> builder)
    {
        builder.ToTable("test_sections");
        builder.HasOne(ts => ts.Test).WithMany(t => t.Sections).HasForeignKey(ts => ts.TestId);
        builder.HasOne(ts => ts.InterestCategory).WithMany().HasForeignKey(ts => ts.InterestCategoryId);
        builder.HasOne(ts => ts.AptitudeCategory).WithMany().HasForeignKey(ts => ts.AptitudeCategoryId);
        builder.Property(ts => ts.SectionType).HasConversion<string>().HasMaxLength(20);
    }
}

public class TestSectionQuestionConfiguration : IEntityTypeConfiguration<TestSectionQuestion>
{
    public void Configure(EntityTypeBuilder<TestSectionQuestion> builder)
    {
        builder.ToTable("test_section_questions");
        builder.HasOne(tsq => tsq.Section).WithMany(ts => ts.Questions).HasForeignKey(tsq => tsq.SectionId);
        builder.HasOne(tsq => tsq.Question).WithMany().HasForeignKey(tsq => tsq.QuestionId);
    }
}

public class TestAttemptConfiguration : IEntityTypeConfiguration<TestAttempt>
{
    public void Configure(EntityTypeBuilder<TestAttempt> builder)
    {
        builder.ToTable("test_attempts");
        builder.HasIndex(ta => ta.Uuid).IsUnique();
        builder.HasOne(ta => ta.Student).WithMany().HasForeignKey(ta => ta.StudentId);
        builder.HasOne(ta => ta.Test).WithMany().HasForeignKey(ta => ta.TestId);
        builder.Property(ta => ta.Status).HasConversion<string>().HasMaxLength(15);
        builder.Property(ta => ta.RecommendedStream).HasConversion<string>().HasMaxLength(15);
        builder.Property(ta => ta.IqCategory).HasConversion<string>().HasMaxLength(20);
    }
}

public class TestResponseConfiguration : IEntityTypeConfiguration<TestResponse>
{
    public void Configure(EntityTypeBuilder<TestResponse> builder)
    {
        builder.ToTable("test_responses");
        builder.HasIndex(tr => new { tr.AttemptId, tr.QuestionId }).IsUnique();
        builder.HasOne(tr => tr.Attempt).WithMany(ta => ta.Responses).HasForeignKey(tr => tr.AttemptId);
        builder.HasOne(tr => tr.Question).WithMany().HasForeignKey(tr => tr.QuestionId);
        builder.HasOne(tr => tr.SelectedOption).WithMany().HasForeignKey(tr => tr.SelectedOptionId);
        builder.Property(tr => tr.ScoreObtained).HasPrecision(5, 2);
    }
}

public class InterestScoreConfiguration : IEntityTypeConfiguration<InterestScore>
{
    public void Configure(EntityTypeBuilder<InterestScore> builder)
    {
        builder.ToTable("interest_scores");
        builder.HasIndex(s => new { s.AttemptId, s.InterestCategoryId }).IsUnique();
        builder.HasOne(s => s.Attempt).WithMany(a => a.InterestScores).HasForeignKey(s => s.AttemptId);
        builder.HasOne(s => s.InterestCategory).WithMany().HasForeignKey(s => s.InterestCategoryId);
        builder.Property(s => s.RawScore).HasPrecision(8, 2);
        builder.Property(s => s.MaxPossibleScore).HasPrecision(8, 2);
        builder.Property(s => s.PercentileScore).HasPrecision(5, 2);
    }
}

public class AptitudeScoreConfiguration : IEntityTypeConfiguration<AptitudeScore>
{
    public void Configure(EntityTypeBuilder<AptitudeScore> builder)
    {
        builder.ToTable("aptitude_scores");
        builder.HasIndex(s => new { s.AttemptId, s.AptitudeCategoryId }).IsUnique();
        builder.HasOne(s => s.Attempt).WithMany(a => a.AptitudeScores).HasForeignKey(s => s.AttemptId);
        builder.HasOne(s => s.AptitudeCategory).WithMany().HasForeignKey(s => s.AptitudeCategoryId);
        builder.Property(s => s.RawScore).HasPrecision(8, 2);
        builder.Property(s => s.MaxPossibleScore).HasPrecision(8, 2);
        builder.Property(s => s.PercentileScore).HasPrecision(5, 2);
    }
}

public class CareerSuitabilityScoreConfiguration : IEntityTypeConfiguration<CareerSuitabilityScore>
{
    public void Configure(EntityTypeBuilder<CareerSuitabilityScore> builder)
    {
        builder.ToTable("career_suitability_scores");
        builder.HasIndex(s => new { s.AttemptId, s.CareerId }).IsUnique();
        builder.HasOne(s => s.Attempt).WithMany(a => a.CareerSuitabilityScores).HasForeignKey(s => s.AttemptId);
        builder.HasOne(s => s.Career).WithMany().HasForeignKey(s => s.CareerId);
        builder.Property(s => s.SuitabilityPct).HasPrecision(5, 2);
    }
}
