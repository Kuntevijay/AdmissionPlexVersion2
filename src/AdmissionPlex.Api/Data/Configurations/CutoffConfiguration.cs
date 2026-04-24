using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AdmissionPlex.Core.Entities.Cutoffs;

namespace AdmissionPlex.Api.Data.Configurations;

public class CollegeConfiguration : IEntityTypeConfiguration<College>
{
    public void Configure(EntityTypeBuilder<College> builder)
    {
        builder.ToTable("colleges");
        builder.HasIndex(c => c.Code).IsUnique().HasFilter("\"Code\" IS NOT NULL");
        builder.Property(c => c.Name).HasMaxLength(255);
        builder.Property(c => c.Code).HasMaxLength(20);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.State).HasMaxLength(100);
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(15);
    }
}

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");
        builder.Property(b => b.Name).HasMaxLength(255);
        builder.Property(b => b.Code).HasMaxLength(20);
    }
}

public class CutoffDataConfiguration : IEntityTypeConfiguration<CutoffData>
{
    public void Configure(EntityTypeBuilder<CutoffData> builder)
    {
        builder.ToTable("cutoff_data");
        builder.HasIndex(c => new { c.CollegeId, c.BranchId, c.Exam, c.Year, c.Round, c.Category }).IsUnique();
        builder.HasOne(c => c.College).WithMany().HasForeignKey(c => c.CollegeId);
        builder.HasOne(c => c.Branch).WithMany().HasForeignKey(c => c.BranchId);
        builder.Property(c => c.Exam).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Category).HasMaxLength(30);
        builder.Property(c => c.CutoffPercentile).HasPrecision(6, 3);
        builder.Property(c => c.CutoffScore).HasPrecision(8, 2);
    }
}
