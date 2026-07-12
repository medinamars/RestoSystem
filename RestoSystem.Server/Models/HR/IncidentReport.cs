using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.HR;

public class IncidentReport : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public int BranchId { get; set; }
    public DateTime IncidentDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // "Low", "Medium", "High", "Critical"
    public string Status { get; set; } = "Open"; // "Open", "Investigating", "Resolved", "Closed"
    public string? Resolution { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public string? ReportedBy { get; set; }
}
