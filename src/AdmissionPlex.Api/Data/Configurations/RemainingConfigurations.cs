using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AdmissionPlex.Core.Entities.Chat;
using AdmissionPlex.Core.Entities.Counselling;
using AdmissionPlex.Core.Entities.Referrals;
using AdmissionPlex.Core.Entities.Payments;
using AdmissionPlex.Core.Entities.Content;
using AdmissionPlex.Core.Entities.Notifications;

namespace AdmissionPlex.Api.Data.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<CareerChatSession>
{
    public void Configure(EntityTypeBuilder<CareerChatSession> builder)
    {
        builder.ToTable("career_chat_sessions");
        builder.HasIndex(s => s.Uuid).IsUnique();
        builder.HasOne(s => s.Student).WithMany().HasForeignKey(s => s.StudentId);
        builder.Property(s => s.ContextJson).HasColumnType("jsonb");
    }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<CareerChatMessage>
{
    public void Configure(EntityTypeBuilder<CareerChatMessage> builder)
    {
        builder.ToTable("career_chat_messages");
        builder.HasOne(m => m.Session).WithMany(s => s.Messages).HasForeignKey(m => m.SessionId);
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(15);
        builder.Property(m => m.MetadataJson).HasColumnType("jsonb");
    }
}

public class CounsellorSessionConfiguration : IEntityTypeConfiguration<CounsellorSession>
{
    public void Configure(EntityTypeBuilder<CounsellorSession> builder)
    {
        builder.ToTable("counsellor_sessions");
        builder.HasIndex(s => s.Uuid).IsUnique();
        builder.HasOne(s => s.Student).WithMany().HasForeignKey(s => s.StudentId);
        builder.HasOne(s => s.Counsellor).WithMany().HasForeignKey(s => s.CounsellorId);
        builder.Property(s => s.SessionType).HasConversion<string>().HasMaxLength(15);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(15);
    }
}

public class CounsellorAvailabilityConfiguration : IEntityTypeConfiguration<CounsellorAvailability>
{
    public void Configure(EntityTypeBuilder<CounsellorAvailability> builder)
    {
        builder.ToTable("counsellor_availabilities");
        builder.HasOne(a => a.Counsellor).WithMany().HasForeignKey(a => a.CounsellorId);
    }
}

public class ReferralCodeConfiguration : IEntityTypeConfiguration<ReferralCode>
{
    public void Configure(EntityTypeBuilder<ReferralCode> builder)
    {
        builder.ToTable("referral_codes");
        builder.HasIndex(r => r.Code).IsUnique();
        builder.Property(r => r.Code).HasMaxLength(20);
        builder.HasIndex(r => r.UserId);
    }
}

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.ToTable("referrals");
        builder.HasOne(r => r.ReferralCode).WithMany().HasForeignKey(r => r.ReferralCodeId);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(15);
    }
}

public class ReferralRewardConfiguration : IEntityTypeConfiguration<ReferralReward>
{
    public void Configure(EntityTypeBuilder<ReferralReward> builder)
    {
        builder.ToTable("referral_rewards");
        builder.HasOne(rr => rr.Referral).WithMany().HasForeignKey(rr => rr.ReferralId);
        
        builder.Property(rr => rr.RewardType).HasConversion<string>().HasMaxLength(15);
        builder.Property(rr => rr.Status).HasConversion<string>().HasMaxLength(15);
        builder.Property(rr => rr.Amount).HasPrecision(10, 2);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasIndex(p => p.Uuid).IsUnique();
        builder.HasIndex(p => p.OrderId).IsUnique();
        builder.HasIndex(p => p.UserId);
        builder.Property(p => p.OrderId).HasMaxLength(50);
        builder.Property(p => p.Amount).HasPrecision(10, 2);
        builder.Property(p => p.Currency).HasMaxLength(3);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(15);
        builder.Property(p => p.PaymentFor).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CcavenueResponseJson).HasColumnType("jsonb");
        builder.Property(p => p.DiscountAmount).HasPrecision(10, 2);
    }
}

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("pages");
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Slug).HasMaxLength(255);
        builder.Property(p => p.Title).HasMaxLength(255);
        builder.Property(p => p.PageType).HasConversion<string>().HasMaxLength(15);
    }
}

public class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.ToTable("faqs");
        builder.Property(f => f.Category).HasMaxLength(100);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasIndex(n => new { n.UserId, n.IsRead });
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(25);
        builder.Property(n => n.Title).HasMaxLength(255);
    }
}
