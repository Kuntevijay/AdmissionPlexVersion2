using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Cutoffs;

public class CutoffData : BaseEntity
{
    public long CollegeId { get; set; }
    public long BranchId { get; set; }
    public ExamType Exam { get; set; }
    public int Year { get; set; }
    public int Round { get; set; } = 1;
    public string Category { get; set; } = "OPEN";
    public string Gender { get; set; } = "all";
    public int? CutoffRank { get; set; }
    public decimal? CutoffPercentile { get; set; }
    public decimal? CutoffScore { get; set; }
    public int? SeatsAvailable { get; set; }

    public College College { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
