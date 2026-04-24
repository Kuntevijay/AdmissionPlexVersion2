namespace AdmissionPlex.Shared.DTOs.Tests;

public class AptitudeScoreDto
{
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal RawScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public decimal PercentileScore { get; set; }
    public int RankOrder { get; set; }
}
