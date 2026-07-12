using RestoSystem.Server.Models.Base;
using RestoSystem.Server.Models.Common;

namespace RestoSystem.Server.Models.Inventory;

public class InventoryBatch : BaseEntity
{
    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;
    public int StorageLocationId { get; set; }
    public StorageLocation StorageLocation { get; set; } = null!;
    public string? BatchNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow;
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}
