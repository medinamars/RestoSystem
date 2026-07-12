using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.HR;

public class MealEntitlement : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime ShiftDate { get; set; }
    public bool IsClaimed { get; set; }
    public decimal Amount { get; set; }
    public string? MealDescription { get; set; }
    public DateTime? ClaimedAt { get; set; }
}
