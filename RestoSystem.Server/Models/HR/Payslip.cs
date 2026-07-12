using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.HR;

public class Payslip : BaseEntity
{
    public int PayrollId { get; set; }
    public Payroll Payroll { get; set; } = null!;
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime GeneratedDate { get; set; }
    public DateTime? SentDate { get; set; }
    public string? DeliveryMethod { get; set; } // "Email", "FacebookMessenger", "Printed"
    public bool IsDelivered { get; set; }
    public string? PdfPath { get; set; }
}
