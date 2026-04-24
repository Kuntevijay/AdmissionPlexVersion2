using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AdmissionPlex.Core.Entities.Users;

namespace AdmissionPlex.Api.Data.Configurations;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("student_profiles");
        builder.HasIndex(s => s.UserId).IsUnique();
        // FK to AppUser is configured in AppDbContext.OnModelCreating
        builder.HasOne(s => s.Coordinator).WithMany().HasForeignKey(s => s.CoordinatorId);
        builder.Property(s => s.FirstName).HasMaxLength(100);
        builder.Property(s => s.LastName).HasMaxLength(100);
        builder.Property(s => s.CurrentClass).HasMaxLength(20);
        builder.Property(s => s.SchoolName).HasMaxLength(255);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.State).HasMaxLength(100);
        builder.Property(s => s.Board).HasConversion<string>().HasMaxLength(10);
        builder.Property(s => s.Stream).HasConversion<string>().HasMaxLength(15);
        builder.Property(s => s.Gender).HasConversion<string>().HasMaxLength(10);
        builder.Property(s => s.ReferredByCode).HasMaxLength(20);
    }
}

public class CounsellorProfileConfiguration : IEntityTypeConfiguration<CounsellorProfile>
{
    public void Configure(EntityTypeBuilder<CounsellorProfile> builder)
    {
        builder.ToTable("counsellor_profiles");
        builder.HasIndex(c => c.UserId).IsUnique();
        builder.Property(c => c.FullName).HasMaxLength(200);
        builder.Property(c => c.Qualification).HasMaxLength(255);
        builder.Property(c => c.HourlyRate).HasPrecision(10, 2);
        builder.Property(c => c.Rating).HasPrecision(3, 2);
    }
}

public class CoordinatorProfileConfiguration : IEntityTypeConfiguration<CoordinatorProfile>
{
    public void Configure(EntityTypeBuilder<CoordinatorProfile> builder)
    {
        builder.ToTable("coordinator_profiles");
        builder.HasIndex(c => c.UserId).IsUnique();
        builder.Property(c => c.FullName).HasMaxLength(200);
        builder.Property(c => c.CommissionPct).HasPrecision(5, 2);
    }
}

public class CoordinatorSchoolConfiguration : IEntityTypeConfiguration<CoordinatorSchool>
{
    public void Configure(EntityTypeBuilder<CoordinatorSchool> builder)
    {
        builder.ToTable("coordinator_schools");
        builder.HasOne(cs => cs.Coordinator).WithMany(c => c.Schools).HasForeignKey(cs => cs.CoordinatorId);
        builder.Property(cs => cs.SchoolName).HasMaxLength(255);
        builder.Property(cs => cs.SchoolCity).HasMaxLength(100);
    }
}
