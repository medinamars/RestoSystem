using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.Inventory;

public class Equipment : BaseEntity
{
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string Category { get; set; } = string.Empty; // Kitchen, Refrigeration, POS, Furniture
    public DateTime? PurchaseDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public string? WarrantyExpiry { get; set; }
    public string? Location { get; set; }
    public string Status { get; set; } = "Operational"; // Operational, Under Repair, Retired
    public string? Notes { get; set; }
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}
