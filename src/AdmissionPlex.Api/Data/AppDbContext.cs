using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Users;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Entities.Careers;
using AdmissionPlex.Core.Entities.Cutoffs;
using AdmissionPlex.Core.Entities.Chat;
using AdmissionPlex.Core.Entities.Counselling;
using AdmissionPlex.Core.Entities.Referrals;
using AdmissionPlex.Core.Entities.Payments;
using AdmissionPlex.Core.Entities.Content;
using AdmissionPlex.Core.Entities.Notifications;

namespace AdmissionPlex.Api.Data;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, long>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // User Profiles (linked to AppUser via UserId)
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<CounsellorProfile> CounsellorProfiles => Set<CounsellorProfile>();
    public DbSet<CoordinatorProfile> CoordinatorProfiles => Set<CoordinatorProfile>();
    public DbSet<CoordinatorSchool> CoordinatorSchools => Set<CoordinatorSchool>();

    // Tests & Psychometric
    public DbSet<InterestCategory> InterestCategories => Set<InterestCategory>();
    public DbSet<AptitudeCategory> AptitudeCategories => Set<AptitudeCategory>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<TestSection> TestSections => Set<TestSection>();
    public DbSet<TestSectionQuestion> TestSectionQuestions => Set<TestSectionQuestion>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<TestResponse> TestResponses => Set<TestResponse>();
    public DbSet<InterestScore> InterestScores => Set<InterestScore>();
    public DbSet<AptitudeScore> AptitudeScores => Set<AptitudeScore>();
    public DbSet<CareerSuitabilityScore> CareerSuitabilityScores => Set<CareerSuitabilityScore>();

    // Careers
    public DbSet<CareerStream> CareerStreams => Set<CareerStream>();
    public DbSet<Career> Careers => Set<Career>();
    public DbSet<CareerSubject> CareerSubjects => Set<CareerSubject>();
    public DbSet<CareerInterestWeight> CareerInterestWeights => Set<CareerInterestWeight>();
    public DbSet<CareerAptitudeWeight> CareerAptitudeWeights => Set<CareerAptitudeWeight>();

    // Cutoffs
    public DbSet<College> Colleges => Set<College>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<CutoffData> CutoffData => Set<CutoffData>();

    // Chat
    public DbSet<CareerChatSession> CareerChatSessions => Set<CareerChatSession>();
    public DbSet<CareerChatMessage> CareerChatMessages => Set<CareerChatMessage>();

    // Counselling
    public DbSet<CounsellorSession> CounsellorSessions => Set<CounsellorSession>();
    public DbSet<CounsellorAvailability> CounsellorAvailabilities => Set<CounsellorAvailability>();

    // Referrals
    public DbSet<ReferralCode> ReferralCodes => Set<ReferralCode>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<ReferralReward> ReferralRewards => Set<ReferralReward>();

    // Payments
    public DbSet<Payment> Payments => Set<Payment>();

    // Content
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Faq> Faqs => Set<Faq>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Rename Identity tables to snake_case for PostgreSQL
        modelBuilder.Entity<AppUser>(b => b.ToTable("users"));
        modelBuilder.Entity<AppRole>(b => b.ToTable("roles"));
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<long>>(b => b.ToTable("user_roles"));
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<long>>(b => b.ToTable("user_claims"));
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<long>>(b => b.ToTable("user_logins"));
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<long>>(b => b.ToTable("role_claims"));
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<long>>(b => b.ToTable("user_tokens"));

        // AppUser → profile navigations
        modelBuilder.Entity<AppUser>(b =>
        {
            b.HasIndex(u => u.Uuid).IsUnique();
            b.HasOne(u => u.StudentProfile).WithOne().HasForeignKey<StudentProfile>(s => s.UserId);
            b.HasOne(u => u.CounsellorProfile).WithOne().HasForeignKey<CounsellorProfile>(c => c.UserId);
            b.HasOne(u => u.CoordinatorProfile).WithOne().HasForeignKey<CoordinatorProfile>(c => c.UserId);
        });

        // Apply all domain entity configurations from the Configurations folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Core.Common.AuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
