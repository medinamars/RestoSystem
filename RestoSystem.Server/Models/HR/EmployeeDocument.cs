using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.HR;

public class EmployeeDocument : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string DocumentType { get; set; } = string.Empty; // Contract, GovtID, Certificate, etc.
    public string DocumentName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}
