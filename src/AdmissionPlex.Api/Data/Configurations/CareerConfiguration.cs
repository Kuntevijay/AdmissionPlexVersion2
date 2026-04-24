using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AdmissionPlex.Core.Entities.Careers;

namespace AdmissionPlex.Api.Data.Configurations;

public class CareerStreamConfiguration : IEntityTypeConfiguration<CareerStream>
{
    public void Configure(EntityTypeBuilder<CareerStream> builder)
    {
        builder.ToTable("career_streams");
        builder.HasIndex(cs => cs.Name).IsUnique();
        builder.Property(cs => cs.Name).HasMaxLength(100);
    }
}

public class CareerConfiguration : IEntityTypeConfiguration<Career>
{
    public void Configure(EntityTypeBuilder<Career> builder)
    {
        builder.ToTable("careers");
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasOne(c => c.Stream).WithMany(s => s.Careers).HasForeignKey(c => c.StreamId);
        builder.Property(c => c.Slug).HasMaxLength(255);
        builder.Property(c => c.Title).HasMaxLength(255);
        builder.Property(c => c.GrowthOutlook).HasConversion<string>().HasMaxLength(15);
        builder.Property(c => c.AvgSalaryMin).HasPrecision(12, 2);
        builder.Property(c => c.AvgSalaryMax).HasPrecision(12, 2);
        builder.Property(c => c.SuitabilityCutoffPct).HasPrecision(5, 2);
        builder.Property(c => c.SkillsRequired).HasColumnType("jsonb");
        builder.Property(c => c.TopColleges).HasColumnType("jsonb");
        builder.Property(c => c.EntranceExams).HasColumnType("jsonb");
    }
}

public class CareerInterestWeightConfiguration : IEntityTypeConfiguration<CareerInterestWeight>
{
    public void Configure(EntityTypeBuilder<CareerInterestWeight> builder)
    {
        builder.ToTable("career_interest_weights");
        builder.HasIndex(w => new { w.CareerId, w.InterestCategoryId }).IsUnique();
        builder.HasOne(w => w.Career).WithMany(c => c.InterestWeights).HasForeignKey(w => w.CareerId);
        builder.HasOne(w => w.InterestCategory).WithMany().HasForeignKey(w => w.InterestCategoryId);
        builder.Property(w => w.Weight).HasPrecision(5, 2);
        builder.Property(w => w.MinPercentile).HasPrecision(5, 2);
    }
}

public class CareerAptitudeWeightConfiguration : IEntityTypeConfiguration<CareerAptitudeWeight>
{
    public void Configure(EntityTypeBuilder<CareerAptitudeWeight> builder)
    {
        builder.ToTable("career_aptitude_weights");
        builder.HasIndex(w => new { w.CareerId, w.AptitudeCategoryId }).IsUnique();
        builder.HasOne(w => w.Career).WithMany(c => c.AptitudeWeights).HasForeignKey(w => w.CareerId);
        builder.HasOne(w => w.AptitudeCategory).WithMany().HasForeignKey(w => w.AptitudeCategoryId);
        builder.Property(w => w.Weight).HasPrecision(5, 2);
        builder.Property(w => w.MinPercentile).HasPrecision(5, 2);
    }
}
