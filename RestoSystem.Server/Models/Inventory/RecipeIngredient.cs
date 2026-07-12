using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.Inventory;

public class RecipeIngredient : BaseEntity
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;
    public decimal Quantity { get; set; } // quantity of this ingredient per serving
    public decimal WastePercent { get; set; } // e.g. 0.05 = 5% waste/yield loss
}
