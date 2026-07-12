using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.HR;

public class Memo : BaseEntity
{
    public int? EmployeeId { get; set; } // null = broadcast to branch
    public Employee? Employee { get; set; }
    public int BranchId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Warning", "Commendation", "General", "Policy"
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedDate { get; set; }
    public DateTime IssueDate { get; set; }
}
