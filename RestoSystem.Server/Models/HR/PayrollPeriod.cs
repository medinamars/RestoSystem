using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.HR;

public class PayrollPeriod : BaseEntity
{
    public DateTime WeekStart { get; set; } // Sunday
    public DateTime WeekEnd { get; set; } // Saturday
    public DateTime PayDate { get; set; } // Tuesday
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
}
