using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AdmissionPlex.Core.Entities.Settings;

namespace AdmissionPlex.Api.Data.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("app_settings");
        builder.HasIndex(s => new { s.Category, s.Key }).IsUnique();
        builder.Property(s => s.Category).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Key).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(2000);
        builder.Property(s => s.Description).HasMaxLength(500);
    }
}

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasIndex(t => new { t.Code, t.Channel }).IsUnique();
        builder.Property(t => t.Code).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Channel).HasMaxLength(15).IsRequired();
        builder.Property(t => t.Subject).HasMaxLength(500);
        builder.Property(t => t.WhatsAppTemplateName).HasMaxLength(100);
        builder.Property(t => t.PushTitle).HasMaxLength(255);
        builder.Property(t => t.ActionUrl).HasMaxLength(500);
        builder.Property(t => t.PushImageUrl).HasMaxLength(500);
    }
}

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_logs");
        builder.HasIndex(l => l.UserId);
        builder.HasIndex(l => l.Channel);
        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.CreatedAt);
        builder.Property(l => l.Channel).HasMaxLength(15).IsRequired();
        builder.Property(l => l.Recipient).HasMaxLength(255).IsRequired();
        builder.Property(l => l.TemplateCode).HasMaxLength(50);
        builder.Property(l => l.Subject).HasMaxLength(500);
        builder.Property(l => l.Status).HasMaxLength(15).IsRequired();
        builder.Property(l => l.ProviderResponse).HasColumnType("jsonb");
    }
}
