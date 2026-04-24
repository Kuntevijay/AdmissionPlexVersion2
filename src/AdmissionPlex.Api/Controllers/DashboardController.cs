using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    public DashboardController(AppDbContext context) => _context = context;

    [HttpGet("admin")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var totalStudents = await _context.StudentProfiles.CountAsync();
        var totalTests = await _context.TestAttempts.CountAsync();
        var completedTests = await _context.TestAttempts.CountAsync(a => a.Status == AttemptStatus.Completed);
        var totalCounsellors = await _context.CounsellorProfiles.CountAsync();
        var totalPayments = await _context.Payments.Where(p => p.Status == PaymentStatus.Success).SumAsync(p => (decimal?)p.Amount) ?? 0;
        var totalQuestions = await _context.Questions.CountAsync(q => q.IsActive);
        var totalCareers = await _context.Careers.CountAsync(c => c.IsPublished);

        var recentAttempts = await _context.TestAttempts
            .Include(a => a.Student).Include(a => a.Test)
            .OrderByDescending(a => a.StartedAt).Take(10)
            .Select(a => new {
                StudentName = a.Student.FirstName + " " + a.Student.LastName,
                TestName = a.Test.Title, Status = a.Status.ToString(),
                a.StartedAt, a.OverallIqScore
            }).ToListAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            TotalStudents = totalStudents, TotalTestsTaken = totalTests,
            CompletedTests = completedTests, ActiveCounsellors = totalCounsellors,
            Revenue = totalPayments, TotalQuestions = totalQuestions,
            TotalCareers = totalCareers, RecentAttempts = recentAttempts
        }));
    }
}
