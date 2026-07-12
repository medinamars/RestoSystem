using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.Inventory;

public class RawMaterialPriceHistory : BaseEntity
{
    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;
    public decimal Price { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime PriceDate { get; set; }
    public string? Notes { get; set; }
}
