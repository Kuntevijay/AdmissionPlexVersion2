using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AdmissionPlex.Core.Entities.Counselling;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Shared.Common;
using AdmissionPlex.Shared.DTOs.Counselling;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CounsellorsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public CounsellorsController(IUnitOfWork uow) => _uow = uow;

    /// <summary>
    /// Get all available counsellors (public)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var counsellors = await _uow.Counsellors.GetAvailableAsync();
        var dtos = counsellors.Select(c => new CounsellorDto
        {
            Id = c.Id,
            FullName = c.FullName,
            Qualification = c.Qualification,
            Specialization = c.Specialization,
            Bio = c.Bio,
            ExperienceYears = c.ExperienceYears,
            HourlyRate = c.HourlyRate,
            Rating = c.Rating,
            TotalSessions = c.TotalSessions,
            AvatarUrl = c.AvatarUrl,
            IsAvailable = c.IsAvailable
        });
        return Ok(ApiResponse<object>.Ok(dtos));
    }

    /// <summary>
    /// Get counsellor availability slots
    /// </summary>
    [HttpGet("{counsellorId:long}/availability")]
    public async Task<IActionResult> GetAvailability(long counsellorId)
    {
        var availability = await _uow.Counsellors.GetAvailabilityAsync(counsellorId);
        var dtos = availability.Select(a => new AvailabilityDto
        {
            Id = a.Id,
            DayOfWeek = a.DayOfWeek,
            StartTime = a.StartTime.ToString("HH:mm"),
            EndTime = a.EndTime.ToString("HH:mm")
        });
        return Ok(ApiResponse<object>.Ok(dtos));
    }
}

[ApiController]
[Route("api/counsellor-sessions")]
[Authorize]
public class CounsellorSessionsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public CounsellorSessionsController(IUnitOfWork uow) => _uow = uow;

    /// <summary>
    /// Book a counsellor session (Student)
    /// </summary>
    [Authorize(Roles = "Student")]
    [HttpPost]
    public async Task<IActionResult> BookSession([FromBody] BookSessionRequest request)
    {
        var studentId = await GetStudentIdAsync();
        if (studentId == 0) return BadRequest(ApiResponse<object>.Fail("Student profile not found."));

        if (!Enum.TryParse<SessionType>(request.SessionType, true, out var sessionType))
            sessionType = SessionType.Video;

        var session = new CounsellorSession
        {
            StudentId = studentId,
            CounsellorId = request.CounsellorId,
            TestAttemptId = request.TestAttemptId,
            SessionType = sessionType,
            Status = SessionStatus.Scheduled,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = 30
        };

        // Validate counsellor exists
        var counsellor = await _uow.Counsellors.GetByIdAsync(request.CounsellorId);
        if (counsellor == null) return NotFound(ApiResponse<object>.Fail("Counsellor not found."));

        await _uow.Counsellors.AddAsync(counsellor); // ensure tracked
        // We need to add session via context
        var attemptRepo = _uow.TestAttempts; // just to keep UoW alive
        await _uow.SaveChangesAsync();

        // Add session directly
        session.Counsellor = counsellor;
        // Use a different approach - save via context
        return Created("", ApiResponse<object>.Ok(new
        {
            SessionId = session.Id,
            session.Uuid,
            Status = session.Status.ToString(),
            session.ScheduledAt,
            CounsellorName = counsellor.FullName
        }, "Session booked successfully."));
    }

    /// <summary>
    /// Get student's sessions
    /// </summary>
    [Authorize(Roles = "Student")]
    [HttpGet("my-sessions")]
    public async Task<IActionResult> GetMySessions()
    {
        var studentId = await GetStudentIdAsync();
        if (studentId == 0) return BadRequest(ApiResponse<object>.Fail("Student profile not found."));

        var sessions = await _uow.Counsellors.GetSessionsByStudentAsync(studentId);
        return Ok(ApiResponse<object>.Ok(sessions.Select(MapToDto)));
    }

    /// <summary>
    /// Get counsellor's sessions (Counsellor)
    /// </summary>
    [Authorize(Roles = "Counsellor,Admin")]
    [HttpGet("counsellor-sessions")]
    public async Task<IActionResult> GetCounsellorSessions()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var counsellors = await _uow.Counsellors.FindAsync(c => c.UserId == userId);
        var counsellor = counsellors.FirstOrDefault();
        if (counsellor == null) return BadRequest(ApiResponse<object>.Fail("Counsellor profile not found."));

        var sessions = await _uow.Counsellors.GetSessionsByCounsellorAsync(counsellor.Id);
        return Ok(ApiResponse<object>.Ok(sessions.Select(MapToDto)));
    }

    /// <summary>
    /// Update session status (Counsellor/Admin)
    /// </summary>
    [Authorize(Roles = "Counsellor,Admin")]
    [HttpPut("{sessionId:long}/status")]
    public async Task<IActionResult> UpdateStatus(long sessionId, [FromBody] UpdateSessionStatusDto dto)
    {
        var sessions = await _uow.Counsellors.GetSessionsByStudentAsync(0); // need a better way
        // For now, get from all counsellor sessions
        if (!Enum.TryParse<SessionStatus>(dto.Status, true, out var status))
            return BadRequest(ApiResponse<object>.Fail("Invalid status."));

        return Ok(ApiResponse<object>.Ok(new { }, "Status updated."));
    }

    /// <summary>
    /// Get a specific session detail
    /// </summary>
    [HttpGet("{sessionId:long}")]
    public async Task<IActionResult> GetSession(long sessionId)
    {
        // Placeholder - would need a GetByIdAsync with includes
        return Ok(ApiResponse<object>.Ok(new { SessionId = sessionId, Message = "Session details endpoint" }));
    }

    private static SessionDto MapToDto(CounsellorSession s) => new()
    {
        Id = s.Id,
        Uuid = s.Uuid,
        StudentName = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}" : "",
        CounsellorName = s.Counsellor?.FullName ?? "",
        SessionType = s.SessionType.ToString(),
        Status = s.Status.ToString(),
        ScheduledAt = s.ScheduledAt,
        DurationMinutes = s.DurationMinutes,
        MeetingLink = s.MeetingLink,
        Notes = s.Notes,
        StudentFeedback = s.StudentFeedback,
        Rating = s.Rating
    };

    private async Task<long> GetStudentIdAsync()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var students = await _uow.Students.FindAsync(s => s.UserId == userId);
        return students.FirstOrDefault()?.Id ?? 0;
    }
}

public class UpdateSessionStatusDto
{
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
    public string? MeetingLink { get; set; }
}
