using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.HR;

public class PerformanceReview : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime ReviewDate { get; set; }
    public string ReviewedBy { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5
    public string? Strengths { get; set; }
    public string? AreasForImprovement { get; set; }
    public string? Comments { get; set; }
    public string? ActionItems { get; set; }
    public DateTime? NextReviewDate { get; set; }
}
