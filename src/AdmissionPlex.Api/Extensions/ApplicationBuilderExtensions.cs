using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Api.Data.Seed;
using AdmissionPlex.Api.Middleware;

namespace AdmissionPlex.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        return app;
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");

            // Ensure new columns exist (idempotent)
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE tests ADD COLUMN IF NOT EXISTS ""IsPublic"" boolean NOT NULL DEFAULT false;
                    ALTER TABLE tests ADD COLUMN IF NOT EXISTS ""RequiresPayment"" boolean NOT NULL DEFAULT false;
                    ALTER TABLE tests ADD COLUMN IF NOT EXISTS ""IsContinuityFlow"" boolean NOT NULL DEFAULT false;
                    ALTER TABLE tests ADD COLUMN IF NOT EXISTS ""ParentTestCode"" text;
                ");
            }
            catch { /* columns may already exist */ }

            await DbSeeder.SeedAsync(context);
            await DbSeeder.SeedRolesAndAdminAsync(services);
            logger.LogInformation("Database seeded successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database initialization: {Message}", ex.Message);
            throw;
        }
    }
}
