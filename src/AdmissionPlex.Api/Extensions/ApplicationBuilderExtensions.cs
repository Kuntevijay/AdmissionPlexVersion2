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
            if (app.Environment.IsDevelopment())
            {
                // EnsureCreated creates all tables from the DbContext model
                // without needing migration files. For production, switch to MigrateAsync.
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Database ensured created.");
            }

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
