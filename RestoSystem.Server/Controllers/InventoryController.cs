using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoSystem.Server.Data;
using RestoSystem.Server.Models.Inventory;

namespace RestoSystem.Server.Controllers;

public class InventoryController : BaseApiController
{
    public InventoryController(AppDbContext db) : base(db) { }

    // === Inventory Items ===
    [HttpGet("items")]
    public async Task<IActionResult> GetItems([FromQuery] int? branchId, [FromQuery] string? category)
    {
        var query = _db.InventoryItems
            .Include(i => i.Batches)
            .AsQueryable();

        if (branchId.HasValue) query = query.Where(i => i.BranchId == branchId);
        if (!string.IsNullOrEmpty(category)) query = query.Where(i => i.Category == category);

        return Ok(await query.OrderBy(i => i.Name).ToListAsync());
    }

    [HttpGet("items/{id}")]
    public async Task<IActionResult> GetItem(int id)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Batches).ThenInclude(b => b.StorageLocation)
            .Include(i => i.PriceHistory.OrderByDescending(p => p.PriceDate).Take(10))
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] InventoryItem item)
    {
        item.CurrentStock = 0;
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
    }

    [HttpPut("items/{id}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] InventoryItem item)
    {
        var existing = await _db.InventoryItems.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = item.Name;
        existing.Category = item.Category;
        existing.UnitOfMeasure = item.UnitOfMeasure;
        existing.MinStockLevel = item.MinStockLevel;
        existing.ReorderPoint = item.ReorderPoint;
        existing.IsPerishable = item.IsPerishable;
        existing.IsActive = item.IsActive;
        existing.Description = item.Description;
        existing.Barcode = item.Barcode;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    // === Batches ===
    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches([FromQuery] int? itemId, [FromQuery] bool? expiringSoon)
    {
        var query = _db.InventoryBatches
            .Include(b => b.InventoryItem)
            .Include(b => b.StorageLocation)
            .Include(b => b.Supplier)
            .AsQueryable();

        if (itemId.HasValue) query = query.Where(b => b.InventoryItemId == itemId);
        if (expiringSoon == true)
        {
            var warningDate = DateTime.UtcNow.AddDays(7);
            query = query.Where(b => b.ExpiryDate != null && b.ExpiryDate <= warningDate && b.Quantity > 0);
        }

        return Ok(await query.OrderBy(b => b.ExpiryDate).ToListAsync());
    }

    [HttpPost("batches")]
    public async Task<IActionResult> CreateBatch([FromBody] InventoryBatch batch)
    {
        batch.ReceivedDate = DateTime.UtcNow;
        _db.InventoryBatches.Add(batch);

        // Update item stock
        var item = await _db.InventoryItems.FindAsync(batch.InventoryItemId);
        if (item != null) item.CurrentStock += batch.Quantity;

        // Record movement
        _db.StockMovements.Add(new StockMovement
        {
            InventoryBatchId = batch.Id,
            MovementType = MovementType.Inbound,
            Quantity = batch.Quantity,
            UnitCostAtMovement = batch.UnitCost,
            MovementDate = DateTime.UtcNow,
            Notes = $"Initial batch receipt"
        });

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBatches), new { id = batch.Id }, batch);
    }

    // === Stock Movements ===
    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements([FromQuery] int? batchId, [FromQuery] int? itemId, [FromQuery] int limit = 50)
    {
        var query = _db.StockMovements
            .Include(m => m.InventoryBatch).ThenInclude(b => b.InventoryItem)
            .AsQueryable();

        if (batchId.HasValue) query = query.Where(m => m.InventoryBatchId == batchId);
        if (itemId.HasValue) query = query.Where(m => m.InventoryBatch.InventoryItemId == itemId);

        return Ok(await query.OrderByDescending(m => m.MovementDate).Take(limit).ToListAsync());
    }

    // === Waste Records ===
    [HttpGet("waste")]
    public async Task<IActionResult> GetWaste([FromQuery] int? branchId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.WasteRecords
            .Include(w => w.InventoryItem)
            .AsQueryable();

        if (from.HasValue) query = query.Where(w => w.WasteDate >= from);
        if (to.HasValue) query = query.Where(w => w.WasteDate <= to);

        return Ok(await query.OrderByDescending(w => w.WasteDate).ToListAsync());
    }

    [HttpPost("waste")]
    public async Task<IActionResult> RecordWaste([FromBody] WasteRecord waste)
    {
        waste.WasteDate = DateTime.UtcNow;
        _db.WasteRecords.Add(waste);

        // Reduce batch quantity
        if (waste.InventoryBatchId.HasValue)
        {
            var batch = await _db.InventoryBatches.FindAsync(waste.InventoryBatchId.Value);
            if (batch != null)
            {
                batch.Quantity -= waste.Quantity;
                // Update item stock
                var item = await _db.InventoryItems.FindAsync(batch.InventoryItemId);
                if (item != null) item.CurrentStock -= waste.Quantity;
            }
        }

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetWaste), new { id = waste.Id }, waste);
    }

    // === Suppliers ===
    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers() =>
        Ok(await _db.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync());

    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] Supplier supplier)
    {
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSuppliers), new { id = supplier.Id }, supplier);
    }

    // === Purchase Orders ===
    [HttpGet("purchase-orders")]
    public async Task<IActionResult> GetPurchaseOrders([FromQuery] int? branchId, [FromQuery] string? status)
    {
        var query = _db.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items).ThenInclude(i => i.InventoryItem)
            .AsQueryable();

        if (branchId.HasValue) query = query.Where(po => po.BranchId == branchId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(po => po.Status.ToString() == status);

        return Ok(await query.OrderByDescending(po => po.OrderDate).ToListAsync());
    }

    [HttpPost("purchase-orders")]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] PurchaseOrder po)
    {
        po.PoNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        po.Status = PurchaseOrderStatus.Draft;
        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPurchaseOrders), new { id = po.Id }, po);
    }

    // === Recipes & Food Cost ===
    [HttpGet("recipes")]
    public async Task<IActionResult> GetRecipes()
    {
        var recipes = await _db.Recipes
            .Include(r => r.Ingredients).ThenInclude(ri => ri.InventoryItem)
            .Where(r => r.IsActive)
            .ToListAsync();

        // Auto-calculate food cost per recipe
        var result = recipes.Select(r => new
        {
            r.Id,
            r.MenuItemName,
            r.Category,
            r.SellingPrice,
            r.Description,
            Ingredients = r.Ingredients.Select(ri =>
            {
                var latestPrice = ri.InventoryItem.PriceHistory
                    .OrderByDescending(ph => ph.PriceDate)
                    .FirstOrDefault();
                var unitCost = latestPrice?.Price ?? 0;
                var cost = (ri.Quantity * unitCost) * (1 + ri.WastePercent);
                return new
                {
                    ri.InventoryItem.Name,
                    ri.Quantity,
                    UnitCost = unitCost,
                    WastePercent = ri.WastePercent,
                    LineCost = cost
                };
            }),
            TotalFoodCost = r.Ingredients.Sum(ri =>
            {
                var latestPrice = ri.InventoryItem.PriceHistory
                    .OrderByDescending(ph => ph.PriceDate)
                    .FirstOrDefault();
                var unitCost = latestPrice?.Price ?? 0;
                return (ri.Quantity * unitCost) * (1 + ri.WastePercent);
            }),
            FoodCostPercent = r.SellingPrice > 0
                ? Math.Round(r.Ingredients.Sum(ri =>
                {
                    var latestPrice = ri.InventoryItem.PriceHistory
                        .OrderByDescending(ph => ph.PriceDate)
                        .FirstOrDefault();
                    var unitCost = latestPrice?.Price ?? 0;
                    return (ri.Quantity * unitCost) * (1 + ri.WastePercent);
                }) / r.SellingPrice * 100, 2)
                : 0
        });

        return Ok(result);
    }

    [HttpPost("recipes")]
    public async Task<IActionResult> CreateRecipe([FromBody] Recipe recipe)
    {
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRecipes), new { id = recipe.Id }, recipe);
    }

    // === Price History ===
    [HttpPost("price-history")]
    public async Task<IActionResult> AddPriceRecord([FromBody] RawMaterialPriceHistory record)
    {
        record.PriceDate = DateTime.UtcNow;
        _db.RawMaterialPriceHistories.Add(record);
        await _db.SaveChangesAsync();
        return Ok(record);
    }

    // === Equipment ===
    [HttpGet("equipment")]
    public async Task<IActionResult> GetEquipment([FromQuery] int? branchId)
    {
        var query = _db.Equipment
            .Include(e => e.MaintenanceRecords)
            .AsQueryable();
        if (branchId.HasValue) query = query.Where(e => e.BranchId == branchId);
        return Ok(await query.OrderBy(e => e.Name).ToListAsync());
    }

    [HttpPost("equipment")]
    public async Task<IActionResult> CreateEquipment([FromBody] Equipment equipment)
    {
        _db.Equipment.Add(equipment);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEquipment), new { id = equipment.Id }, equipment);
    }

    [HttpPost("equipment/{id}/maintenance")]
    public async Task<IActionResult> AddMaintenanceRecord(int id, [FromBody] MaintenanceRecord record)
    {
        record.EquipmentId = id;
        _db.MaintenanceRecords.Add(record);
        await _db.SaveChangesAsync();
        return Ok(record);
    }

    // === Dashboard summaries ===
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int? branchId)
    {
        var itemsQuery = _db.InventoryItems.AsQueryable();
        var batchesQuery = _db.InventoryBatches.AsQueryable();
        var wasteQuery = _db.WasteRecords.AsQueryable();

        if (branchId.HasValue)
        {
            itemsQuery = itemsQuery.Where(i => i.BranchId == branchId);
            batchesQuery = batchesQuery.Where(b => b.InventoryItem.BranchId == branchId);
            wasteQuery = wasteQuery.Where(w => w.InventoryItem.BranchId == branchId);
        }

        var lowStockItems = await itemsQuery.Where(i => i.CurrentStock <= i.MinStockLevel).CountAsync();
        var totalItems = await itemsQuery.CountAsync();
        var nearExpiry = await batchesQuery
            .Where(b => b.ExpiryDate != null && b.ExpiryDate <= DateTime.UtcNow.AddDays(7) && b.Quantity > 0)
            .CountAsync();
        var totalWaste = await wasteQuery
            .Where(w => w.WasteDate >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(w => w.EstimatedCost);

        return Ok(new
        {
            totalItems,
            lowStockItems,
            nearExpiryBatches = nearExpiry,
            totalWasteLast30Days = totalWaste
        });
    }
}
