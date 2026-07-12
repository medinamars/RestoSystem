using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.Inventory;

public class Recipe : BaseEntity
{
    public int BranchId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal SellingPrice { get; set; }
    public string Category { get; set; } = string.Empty; // Main, Side, Drink, Dessert
    public bool IsActive { get; set; } = true;
    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}
