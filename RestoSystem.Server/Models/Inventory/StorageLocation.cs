using RestoSystem.Server.Models.Base;
using RestoSystem.Server.Models.Common;

namespace RestoSystem.Server.Models.Inventory;

public enum StorageArea
{
    Dry,
    Chiller,
    Freezer,
    PrepArea,
    StorageRoom
}

public class StorageLocation : BaseEntity
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public string Area { get; set; } = string.Empty; // e.g. "Freezer-A", "Dry Shelf 2"
    public StorageArea StorageArea { get; set; }
    public string? Description { get; set; }
    public ICollection<InventoryBatch> Batches { get; set; } = new List<InventoryBatch>();
}
