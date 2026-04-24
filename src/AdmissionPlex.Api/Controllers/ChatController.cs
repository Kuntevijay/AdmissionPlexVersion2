using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AdmissionPlex.Core.Entities.Chat;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Shared.Common;
using AdmissionPlex.Shared.DTOs.Chat;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student")]
public class ChatController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public ChatController(IUnitOfWork uow) => _uow = uow;

    /// <summary>
    /// Get all chat sessions for the current student
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        var studentId = await GetStudentIdAsync();
        if (studentId == 0) return BadRequest(ApiResponse<object>.Fail("Student profile not found."));

        var sessions = await _uow.Chat.GetByStudentIdAsync(studentId);
        var dtos = sessions.Select(s => new ChatSessionDto
        {
            Id = s.Id,
            Uuid = s.Uuid,
            Title = s.Title,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            MessageCount = s.Messages?.Count ?? 0
        });
        return Ok(ApiResponse<object>.Ok(dtos));
    }

    /// <summary>
    /// Start a new chat session
    /// </summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> StartSession([FromBody] StartChatRequest request)
    {
        var studentId = await GetStudentIdAsync();
        if (studentId == 0) return BadRequest(ApiResponse<object>.Fail("Student profile not found."));

        var session = new CareerChatSession
        {
            StudentId = studentId,
            Title = request.InitialMessage.Length > 50
                ? request.InitialMessage[..50] + "..."
                : request.InitialMessage
        };

        await _uow.Chat.AddAsync(session);
        await _uow.SaveChangesAsync();

        // Add system context message
        var systemMsg = new CareerChatMessage
        {
            SessionId = session.Id,
            Role = ChatRole.System,
            Content = "You are a career guidance assistant for Indian students. Help them explore career options based on their interests, aptitude scores, and educational background. Be encouraging and informative."
        };

        // Add assistant greeting
        var greeting = new CareerChatMessage
        {
            SessionId = session.Id,
            Role = ChatRole.Assistant,
            Content = "Hi! I'm your career guidance assistant. I can help you explore career paths, understand entrance exams, compare salary prospects, and find the right fit based on your interests and abilities. What would you like to know?"
        };

        // Add user's initial message
        var userMsg = new CareerChatMessage
        {
            SessionId = session.Id,
            Role = ChatRole.User,
            Content = request.InitialMessage
        };

        // Generate a contextual response
        var responseContent = GenerateCareerResponse(request.InitialMessage);
        var assistantResponse = new CareerChatMessage
        {
            SessionId = session.Id,
            Role = ChatRole.Assistant,
            Content = responseContent
        };

        var sessionWithMsgs = await _uow.Chat.GetWithMessagesAsync(session.Id);
        sessionWithMsgs!.Messages.Add(systemMsg);
        sessionWithMsgs.Messages.Add(greeting);
        sessionWithMsgs.Messages.Add(userMsg);
        sessionWithMsgs.Messages.Add(assistantResponse);
        await _uow.SaveChangesAsync();

        return Created("", ApiResponse<object>.Ok(new
        {
            SessionId = session.Id,
            session.Uuid,
            Messages = new[]
            {
                new ChatMessageDto { Role = "Assistant", Content = greeting.Content, CreatedAt = greeting.CreatedAt },
                new ChatMessageDto { Role = "User", Content = userMsg.Content, CreatedAt = userMsg.CreatedAt },
                new ChatMessageDto { Role = "Assistant", Content = assistantResponse.Content, CreatedAt = assistantResponse.CreatedAt }
            }
        }));
    }

    /// <summary>
    /// Get all messages in a chat session
    /// </summary>
    [HttpGet("sessions/{sessionId:long}/messages")]
    public async Task<IActionResult> GetMessages(long sessionId)
    {
        var session = await _uow.Chat.GetWithMessagesAsync(sessionId);
        if (session == null) return NotFound(ApiResponse<object>.Fail("Session not found."));

        var dtos = session.Messages
            .Where(m => m.Role != ChatRole.System)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role.ToString(),
                Content = m.Content,
                CreatedAt = m.CreatedAt
            });
        return Ok(ApiResponse<object>.Ok(dtos));
    }

    /// <summary>
    /// Send a message in an existing chat session
    /// </summary>
    [HttpPost("sessions/{sessionId:long}/messages")]
    public async Task<IActionResult> SendMessage(long sessionId, [FromBody] SendMessageRequest request)
    {
        var session = await _uow.Chat.GetWithMessagesAsync(sessionId);
        if (session == null) return NotFound(ApiResponse<object>.Fail("Session not found."));
        if (!session.IsActive) return BadRequest(ApiResponse<object>.Fail("This chat session is closed."));

        // Add user message
        session.Messages.Add(new CareerChatMessage
        {
            SessionId = sessionId,
            Role = ChatRole.User,
            Content = request.Message
        });

        // Generate AI response (placeholder — replace with actual AI API call)
        var responseContent = GenerateCareerResponse(request.Message);
        var assistantMsg = new CareerChatMessage
        {
            SessionId = sessionId,
            Role = ChatRole.Assistant,
            Content = responseContent
        };
        session.Messages.Add(assistantMsg);
        await _uow.SaveChangesAsync();

        return Ok(ApiResponse<ChatMessageDto>.Ok(new ChatMessageDto
        {
            Id = assistantMsg.Id,
            Role = "Assistant",
            Content = assistantMsg.Content,
            CreatedAt = assistantMsg.CreatedAt
        }));
    }

    /// <summary>
    /// Placeholder AI response generator — replace with actual LLM API integration
    /// </summary>
    private static string GenerateCareerResponse(string userMessage)
    {
        var msg = userMessage.ToLower();

        if (msg.Contains("salary") || msg.Contains("pay") || msg.Contains("earn"))
            return "Salary varies significantly by career, experience, and location. In India, entry-level salaries for engineers typically range from ₹3-8 LPA, while medical professionals start around ₹5-12 LPA. Would you like me to compare specific careers?";

        if (msg.Contains("engineer") || msg.Contains("computer") || msg.Contains("software") || msg.Contains("it"))
            return "Software Engineering is one of the highest-demand careers in India! You'll need strong logical ability and mathematical aptitude. Key paths include B.Tech (CSE/IT) via JEE/MHT-CET, or BCA → MCA. Top colleges include IITs, NITs, COEP, and VJTI. Want to know about entrance exam preparation?";

        if (msg.Contains("doctor") || msg.Contains("medical") || msg.Contains("mbbs"))
            return "Medicine is a rewarding career requiring strong aptitude in Biology and Chemistry. The path is NEET → MBBS (5.5 years) → Specialization (MD/MS). The journey is long but fulfilling. Shall I explain the NEET preparation strategy or compare medical specializations?";

        if (msg.Contains("architect") || msg.Contains("design"))
            return "Architecture combines creativity with technical skills! You'll need strong spatial ability and interest in fine arts. The path is NATA/JEE Paper 2 → B.Arch (5 years). It's a great fit if you enjoy both drawing and problem-solving. Want to explore related careers like Interior Design or Urban Planning?";

        if (msg.Contains("commerce") || msg.Contains("ca") || msg.Contains("business") || msg.Contains("finance"))
            return "Commerce opens doors to CA, MBA, Banking, Finance, and Entrepreneurship. If you're strong in numbers and methodical thinking, CA or Financial Analysis could be great fits. For people skills, consider Marketing or HR management. Which area interests you more?";

        if (msg.Contains("arts") || msg.Contains("humanities") || msg.Contains("writing") || msg.Contains("teach"))
            return "Arts and Humanities offer diverse careers — from Teaching and Journalism to Psychology and Public Policy. If you have strong language skills and social interest, careers like Content Writing, Counselling, or Civil Services could be excellent matches. What are your strongest interests?";

        if (msg.Contains("compare") || msg.Contains("vs") || msg.Contains("difference"))
            return "I'd be happy to compare career paths for you! Please tell me which two careers you'd like me to compare, and I'll break down the education requirements, salary prospects, work-life balance, and growth outlook for each.";

        if (msg.Contains("entrance") || msg.Contains("exam") || msg.Contains("preparation"))
            return "Key entrance exams in India include: JEE (Engineering), NEET (Medical), CLAT (Law), NID/NIFT (Design), CA Foundation, and CUET (Central Universities). Each has different preparation strategies. Which exam are you interested in?";

        return "That's a great question! Based on your interests, I can help you explore specific career paths, compare options, understand entrance exams, or discuss salary prospects. Could you tell me more about what subjects you enjoy and what kind of work environment appeals to you?";
    }

    private async Task<long> GetStudentIdAsync()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var students = await _uow.Students.FindAsync(s => s.UserId == userId);
        return students.FirstOrDefault()?.Id ?? 0;
    }
}
