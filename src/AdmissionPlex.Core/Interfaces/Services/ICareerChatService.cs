namespace AdmissionPlex.Core.Interfaces.Services;

public interface ICareerChatService
{
    Task<(long SessionId, string Response)> StartSessionAsync(long studentId, string initialMessage);
    Task<string> SendMessageAsync(long sessionId, string message);
}
