using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Entities.Careers;
using AdmissionPlex.Shared.Constants;

namespace AdmissionPlex.Api.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedInterestCategoriesAsync(context);
        await SeedAptitudeCategoriesAsync(context);
        await SeedCareerStreamsAsync(context);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds roles and default admin user. Must be called separately with service provider.
    /// </summary>
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        // Seed roles
        string[] roles = { AppRoles.Admin, AppRoles.Student, AppRoles.Counsellor, AppRoles.Coordinator };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole(role));
            }
        }

        // Seed default admin
        const string adminEmail = "admin@admissionplex.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            }
        }
    }

    private static async Task SeedInterestCategoriesAsync(AppDbContext context)
    {
        if (await context.InterestCategories.AnyAsync()) return;

        var categories = new List<InterestCategory>
        {
            new() { Code = "FA", Name = "Fine Arts", Description = "Measures interest in activities such as drawing, painting, etc.", DisplayOrder = 1 },
            new() { Code = "PA", Name = "Performing Arts", Description = "Measures interest in activities such as singing, dancing, acting, etc.", DisplayOrder = 2 },
            new() { Code = "MT", Name = "Machines & Tools", Description = "Measures interest for working with machines and mechanisms.", DisplayOrder = 3 },
            new() { Code = "ME", Name = "Methodical", Description = "Measures interest in activities that require high level of meticulousness.", DisplayOrder = 4 },
            new() { Code = "PI", Name = "People Interaction", Description = "Measures interest in activities involving convincing people.", DisplayOrder = 5 },
            new() { Code = "SO", Name = "Social", Description = "Measures interest in activities involving contribution towards social causes.", DisplayOrder = 6 },
            new() { Code = "WN", Name = "Working With Numbers", Description = "Measures interest in activities involving numbers.", DisplayOrder = 7 },
            new() { Code = "RA", Name = "Research & Analysis", Description = "Measures interest in activities that are scientific in nature.", DisplayOrder = 8 },
            new() { Code = "LU", Name = "Language Usage", Description = "Measures interest in activities involving languages.", DisplayOrder = 9 },
            new() { Code = "OS", Name = "Outdoor & Sports", Description = "Measures interest in activities that keeps one outdoors.", DisplayOrder = 10 },
        };

        await context.InterestCategories.AddRangeAsync(categories);
    }

    private static async Task SeedAptitudeCategoriesAsync(AppDbContext context)
    {
        if (await context.AptitudeCategories.AnyAsync()) return;

        var categories = new List<AptitudeCategory>
        {
            new() { Code = "SA", Name = "Speed & Accuracy", Description = "Measures aptitude for quick and accurate decision making.", DisplayOrder = 1 },
            new() { Code = "NC", Name = "Number Calculations", Description = "Measures aptitude for basic mathematical calculations.", DisplayOrder = 2 },
            new() { Code = "MA", Name = "Mechanical Ability", Description = "Measures aptitude of understanding basic scientific principles.", DisplayOrder = 3 },
            new() { Code = "NA", Name = "Number Application", Description = "Measures aptitude for applying mathematical principles to practical problems.", DisplayOrder = 4 },
            new() { Code = "VA", Name = "Verbal Ability", Description = "Measures aptitude for proficiency in language.", DisplayOrder = 5 },
            new() { Code = "LA", Name = "Logical Ability", Description = "Measures aptitude for interpretation of data in a logical manner.", DisplayOrder = 6 },
            new() { Code = "SP", Name = "Spatial Ability", Description = "Measures aptitude for visualization of shapes and figures in multiple dimensions.", DisplayOrder = 7 },
        };

        await context.AptitudeCategories.AddRangeAsync(categories);
    }

    private static async Task SeedCareerStreamsAsync(AppDbContext context)
    {
        if (await context.CareerStreams.AnyAsync()) return;

        var streams = new List<CareerStream>
        {
            new() { Name = "Science", Description = "Careers in science, technology, engineering, and mathematics." },
            new() { Name = "Commerce", Description = "Careers in business, finance, accounting, and economics." },
            new() { Name = "Arts", Description = "Careers in humanities, social sciences, languages, and creative fields." },
        };

        await context.CareerStreams.AddRangeAsync(streams);
    }
}
