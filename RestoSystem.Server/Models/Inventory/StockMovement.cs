using RestoSystem.Server.Models.Base;

namespace RestoSystem.Server.Models.Inventory;

public enum MovementType
{
    Inbound,
    OutboundSale,
    Waste,
    Transfer,
    Adjustment,
    Return
}

public class StockMovement : BaseEntity
{
    public int InventoryBatchId { get; set; }
    public InventoryBatch InventoryBatch { get; set; } = null!;
    public MovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCostAtMovement { get; set; }
    public string? ReferenceNumber { get; set; } // PO#, Sales#, etc.
    public string? Notes { get; set; }
    public DateTime MovementDate { get; set; }
}
