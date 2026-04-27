using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Api.Repositories;
using AdmissionPlex.Api.Services;
using AdmissionPlex.Shared.Constants;
using AdmissionPlex.Core.Interfaces.Services;

namespace AdmissionPlex.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                })
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<ITestRepository, TestRepository>();
        services.AddScoped<ITestAttemptRepository, TestAttemptRepository>();
        services.AddScoped<ICareerRepository, CareerRepository>();
        services.AddScoped<ICutoffRepository, CutoffRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<ICounsellorRepository, CounsellorRepository>();
        services.AddScoped<IReferralRepository, ReferralRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }

    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // ASP.NET Identity
        services.AddIdentity<AppUser, AppRole>(options =>
        {
            // Password policy
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;

            // Lockout
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;

            // User
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
            options.AddPolicy("CounsellorOrAdmin", policy => policy.RequireRole(AppRoles.Counsellor, AppRoles.Admin));
            options.AddPolicy("CoordinatorOrAdmin", policy => policy.RequireRole(AppRoles.Coordinator, AppRoles.Admin));
            options.AddPolicy("StudentOnly", policy => policy.RequireRole(AppRoles.Student));
        });

        // Auth services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<AuthService>();

        return services;
    }

}

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITestScoringService, TestScoringService>();
        services.AddScoped<ICCavenueService, CCavenueService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReferralService, ReferralService>();

        // Settings & Notifications
        services.AddScoped<IAppSettingService, AppSettingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<GoogleAuthService>();

        // HttpClientFactory for external API calls (SMS, WhatsApp, Push, Google)
        services.AddHttpClient();

        return services;
    }
}
