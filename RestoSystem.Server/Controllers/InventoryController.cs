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
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest req)
    {
        var item = new InventoryItem
        {
            BranchId = req.BranchId,
            Name = req.Name,
            Category = req.Category,
            UnitOfMeasure = req.UnitOfMeasure,
            MinStockLevel = req.MinStockLevel,
            ReorderPoint = req.ReorderPoint,
            CurrentStock = 0,
            IsPerishable = req.IsPerishable,
            IsActive = true,
            Description = req.Description,
            Barcode = req.Barcode
        };
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
    }

    [HttpPut("items/{id}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateItemRequest req)
    {
        var existing = await _db.InventoryItems.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = req.Name;
        existing.Category = req.Category;
        existing.UnitOfMeasure = req.UnitOfMeasure;
        existing.MinStockLevel = req.MinStockLevel;
        existing.ReorderPoint = req.ReorderPoint;
        existing.IsPerishable = req.IsPerishable;
        existing.IsActive = req.IsActive;
        existing.Description = req.Description;
        existing.Barcode = req.Barcode;

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
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchRequest req)
    {
        var batch = new InventoryBatch
        {
            InventoryItemId = req.InventoryItemId,
            StorageLocationId = req.StorageLocationId,
            BatchNumber = req.BatchNumber,
            Quantity = req.Quantity,
            UnitCost = req.UnitCost,
            ExpiryDate = req.ExpiryDate,
            SupplierId = req.SupplierId,
            ReceivedDate = DateTime.UtcNow
        };
        _db.InventoryBatches.Add(batch);

        // Update item stock
        var item = await _db.InventoryItems.FindAsync(batch.InventoryItemId);
        if (item != null) item.CurrentStock += batch.Quantity;

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
    public async Task<IActionResult> RecordWaste([FromBody] CreateWasteRequest req)
    {
        var waste = new WasteRecord
        {
            InventoryItemId = req.InventoryItemId,
            InventoryBatchId = req.InventoryBatchId,
            Quantity = req.Quantity,
            Reason = req.Reason,
            EstimatedCost = req.EstimatedCost,
            WasteDate = DateTime.UtcNow
        };
        _db.WasteRecords.Add(waste);

        // Reduce batch quantity
        if (waste.InventoryBatchId.HasValue)
        {
            var batch = await _db.InventoryBatches.FindAsync(waste.InventoryBatchId.Value);
            if (batch != null)
            {
                batch.Quantity -= waste.Quantity;
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
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest req)
    {
        var supplier = new Supplier
        {
            Name = req.Name,
            ContactPerson = req.ContactPerson,
            ContactNumber = req.ContactNumber,
            Email = req.Email,
            Address = req.Address,
            TinNumber = req.TinNumber,
            IsActive = true
        };
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
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest req)
    {
        var po = new PurchaseOrder
        {
            BranchId = req.BranchId,
            SupplierId = req.SupplierId,
            PoNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            Status = PurchaseOrderStatus.Draft,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = req.ExpectedDeliveryDate,
            TotalAmount = req.TotalAmount,
            Notes = req.Notes
        };

        if (req.Items != null)
        {
            foreach (var itemReq in req.Items)
            {
                po.Items.Add(new PurchaseOrderItem
                {
                    InventoryItemId = itemReq.InventoryItemId,
                    QuantityOrdered = itemReq.QuantityOrdered,
                    UnitPrice = itemReq.UnitPrice
                });
            }
        }

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
    public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeRequest req)
    {
        var recipe = new Recipe
        {
            BranchId = req.BranchId,
            MenuItemName = req.MenuItemName,
            Category = req.Category,
            SellingPrice = req.SellingPrice,
            Description = req.Description,
            IsActive = true
        };

        if (req.Ingredients != null)
        {
            foreach (var ingReq in req.Ingredients)
            {
                recipe.Ingredients.Add(new RecipeIngredient
                {
                    InventoryItemId = ingReq.InventoryItemId,
                    Quantity = ingReq.Quantity,
                    WastePercent = ingReq.WastePercent
                });
            }
        }

        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRecipes), new { id = recipe.Id }, recipe);
    }

    // === Price History ===
    [HttpPost("price-history")]
    public async Task<IActionResult> AddPriceRecord([FromBody] AddPriceRecordRequest req)
    {
        var record = new RawMaterialPriceHistory
        {
            InventoryItemId = req.InventoryItemId,
            Price = req.Price,
            SupplierId = req.SupplierId,
            PriceDate = DateTime.UtcNow,
            Notes = req.Notes
        };
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
    public async Task<IActionResult> AddMaintenanceRecord(int id, [FromBody] CreateMaintenanceRequest req)
    {
        var record = new MaintenanceRecord
        {
            EquipmentId = id,
            Type = req.Type,
            Description = req.Description,
            Cost = req.Cost,
            PerformedBy = req.PerformedBy,
            MaintenanceDate = req.MaintenanceDate,
            Notes = req.Notes,
            NextScheduledDate = req.NextScheduledDate
        };
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
        var wasteRecords = await wasteQuery
            .Where(w => w.WasteDate >= DateTime.UtcNow.AddDays(-30))
            .ToListAsync();
        var totalWaste = wasteRecords.Sum(w => (double)w.EstimatedCost);

        return Ok(new
        {
            totalItems,
            lowStockItems,
            nearExpiryBatches = nearExpiry,
            totalWasteLast30Days = totalWaste
        });
    }
}

// Request DTOs
public record CreateItemRequest(
    int BranchId,
    string Name,
    string Category,
    string UnitOfMeasure,
    decimal MinStockLevel,
    decimal ReorderPoint,
    bool IsPerishable,
    string? Description = null,
    string? Barcode = null
);

public record CreateBatchRequest(
    int InventoryItemId,
    int StorageLocationId,
    decimal Quantity,
    decimal UnitCost,
    DateTime? ExpiryDate = null,
    string? BatchNumber = null,
    int? SupplierId = null
);

public record CreateSupplierRequest(
    string Name,
    string ContactPerson,
    string ContactNumber,
    string Email,
    string Address,
    string TinNumber
);

public record UpdateItemRequest(
    string Name,
    string Category,
    string UnitOfMeasure,
    decimal MinStockLevel,
    decimal ReorderPoint,
    bool IsPerishable,
    bool IsActive,
    string? Description = null,
    string? Barcode = null
);

public record CreateWasteRequest(
    int InventoryItemId,
    decimal Quantity,
    string Reason,
    decimal EstimatedCost,
    int? InventoryBatchId = null
);

public record CreatePurchaseOrderItemRequest(
    int InventoryItemId,
    decimal QuantityOrdered,
    decimal UnitPrice
);

public record CreatePurchaseOrderRequest(
    int BranchId,
    int SupplierId,
    decimal TotalAmount,
    List<CreatePurchaseOrderItemRequest>? Items = null,
    DateTime? ExpectedDeliveryDate = null,
    string? Notes = null
);

public record CreateRecipeIngredientRequest(
    int InventoryItemId,
    decimal Quantity,
    decimal WastePercent = 0
);

public record CreateRecipeRequest(
    int BranchId,
    string MenuItemName,
    string Category,
    decimal SellingPrice,
    List<CreateRecipeIngredientRequest>? Ingredients = null,
    string? Description = null
);

public record AddPriceRecordRequest(
    int InventoryItemId,
    decimal Price,
    int? SupplierId = null,
    string? Notes = null
);

public record CreateMaintenanceRequest(
    string Type,
    string Description,
    decimal Cost,
    string PerformedBy,
    DateTime MaintenanceDate,
    string? Notes = null,
    DateTime? NextScheduledDate = null
);
