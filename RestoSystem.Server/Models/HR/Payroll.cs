using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.HR;

public class Payroll : BaseEntity
{
    public int PayrollPeriodId { get; set; }
    public PayrollPeriod PayrollPeriod { get; set; } = null!;
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public decimal GrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public decimal BaseSalaryAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal MealAllowanceAmount { get; set; }
    public decimal OvertimeAmount { get; set; }
    public decimal OtherDeductions { get; set; }
    public int TotalHoursWorked { get; set; }
    public int TotalShifts { get; set; }
    public string? Notes { get; set; }
    public ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
}
