using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.Common;

public class Branch : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string OperatingHours { get; set; } = "24/7";
    public bool IsActive { get; set; } = true;
}
