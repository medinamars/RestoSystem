using RestoSystem.Server.Models.Base;
using RestoSystem.Server.Models.Common;

namespace RestoSystem.Server.Models.HR;

public enum EmploymentStatus
{
    Active,
    Probationary,
    Regularized,
    Resigned,
    Terminated,
    OnLeave
}

public class Employee : BaseEntity
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? Nickname { get; set; }
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? GovernmentIds { get; set; } // JSON: SSS, PhilHealth, Pag-IBIG, TIN
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public string Position { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Probationary;
    public DateTime HireDate { get; set; }
    public DateTime? RegularizationDate { get; set; }
    public DateTime? ResignationDate { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal CommissionRate { get; set; } // percentage
    public bool ReceivesMealAllowance { get; set; } = true;
    public decimal MealAllowanceAmount { get; set; }
    public string? PhotoUrl { get; set; }
    public byte[]? FaceEncoding { get; set; } // stored face embedding for recognition
    public string? FacebookMessengerId { get; set; } // for payslip delivery
    public string? Notes { get; set; }
    public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
    public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
    public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
    public ICollection<Memo> Memos { get; set; } = new List<Memo>();
    public ICollection<IncidentReport> IncidentReports { get; set; } = new List<IncidentReport>();
    public ICollection<PerformanceReview> PerformanceReviews { get; set; } = new List<PerformanceReview>();
    public ICollection<MealEntitlement> MealEntitlements { get; set; } = new List<MealEntitlement>();
}
