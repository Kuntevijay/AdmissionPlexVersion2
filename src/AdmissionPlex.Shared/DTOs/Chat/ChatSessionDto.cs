namespace AdmissionPlex.Shared.DTOs.Chat;

public class ChatSessionDto
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }
    public string? Title { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}

public class ChatMessageDto
{
    public long Id { get; set; }
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class SendMessageRequest
{
    public string Message { get; set; } = "";
}

public class StartChatRequest
{
    public string InitialMessage { get; set; } = "";
}
