namespace AdmissionPlex.Shared.DTOs.Tests;

public class SubmitAnswerDto
{
    public long QuestionId { get; set; }
    public long? SelectedOptionId { get; set; }
    public string? OpenAnswer { get; set; }
    public int? TimeTakenSeconds { get; set; }
}
