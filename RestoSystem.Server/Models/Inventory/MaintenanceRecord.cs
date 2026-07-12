using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.Inventory;

public class MaintenanceRecord : BaseEntity
{
    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public DateTime MaintenanceDate { get; set; }
    public string Type { get; set; } = string.Empty; // Preventive, Repair, Inspection
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? NextScheduledDate { get; set; }
}
