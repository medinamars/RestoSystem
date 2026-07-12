using RestoSystem.Server.Models.Base;
using RestoSystem.Server.Models.Common;

namespace RestoSystem.Server.Models.Inventory;

public class InventoryItem : BaseEntity
{
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty; // Produce, Meat, Dairy, Dry Goods, Packaging, etc.
    public string UnitOfMeasure { get; set; } = string.Empty; // kg, pcs, liters, box
    public decimal MinStockLevel { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public string? ImageUrl { get; set; }
    public string? Barcode { get; set; }
    public bool IsPerishable { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<InventoryBatch> Batches { get; set; } = new List<InventoryBatch>();
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<RawMaterialPriceHistory> PriceHistory { get; set; } = new List<RawMaterialPriceHistory>();
}
