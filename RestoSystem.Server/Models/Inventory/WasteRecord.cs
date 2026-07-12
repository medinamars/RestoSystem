using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.Inventory;

public class WasteRecord : BaseEntity
{
    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;
    public int? InventoryBatchId { get; set; }
    public InventoryBatch? InventoryBatch { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty; // Expired, Spoiled, Damaged, Overproduction
    public decimal EstimatedCost { get; set; }
    public DateTime WasteDate { get; set; }
    public string? Notes { get; set; }
}
