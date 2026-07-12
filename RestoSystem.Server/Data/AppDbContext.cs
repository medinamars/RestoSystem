using Microsoft.EntityFrameworkCore;
using RestoSystem.Server.Models.Base;
using RestoSystem.Server.Models.Common;
using RestoSystem.Server.Models.Inventory;
using RestoSystem.Server.Models.HR;
namespace RestoSystem.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Common
    public DbSet<Branch> Branches => Set<Branch>();

    // Inventory
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RawMaterialPriceHistory> RawMaterialPriceHistories => Set<RawMaterialPriceHistory>();
    public DbSet<WasteRecord> WasteRecords => Set<WasteRecord>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    // HR
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<Memo> Memos => Set<Memo>();
    public DbSet<IncidentReport> IncidentReports => Set<IncidentReport>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<MealEntitlement> MealEntitlements => Set<MealEntitlement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, new object[] { modelBuilder });
            }
        }

        // === Common ===
        modelBuilder.Entity<Branch>(e =>
        {
            e.HasIndex(b => b.Code).IsUnique();
            e.Property(b => b.Name).IsRequired().HasMaxLength(200);
            e.Property(b => b.Code).IsRequired().HasMaxLength(20);
        });

        // === Inventory ===
        modelBuilder.Entity<Supplier>(e =>
        {
            e.Property(s => s.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(s => s.Name);
        });

        modelBuilder.Entity<StorageLocation>(e =>
        {
            e.HasOne(sl => sl.Branch)
                .WithMany()
                .HasForeignKey(sl => sl.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(sl => sl.Area).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.HasOne(ii => ii.Branch)
                .WithMany()
                .HasForeignKey(ii => ii.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(ii => ii.Name).IsRequired().HasMaxLength(200);
            e.Property(ii => ii.Category).HasMaxLength(100);
            e.Property(ii => ii.UnitOfMeasure).HasMaxLength(50);
            e.HasIndex(ii => ii.Name);
        });

        modelBuilder.Entity<InventoryBatch>(e =>
        {
            e.HasOne(ib => ib.InventoryItem)
                .WithMany(ii => ii.Batches)
                .HasForeignKey(ib => ib.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ib => ib.StorageLocation)
                .WithMany(sl => sl.Batches)
                .HasForeignKey(ib => ib.StorageLocationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ib => ib.Supplier)
                .WithMany()
                .HasForeignKey(ib => ib.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(ib => ib.BatchNumber).HasMaxLength(100);
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.HasOne(sm => sm.InventoryBatch)
                .WithMany(ib => ib.Movements)
                .HasForeignKey(sm => sm.InventoryBatchId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(sm => sm.ReferenceNumber).HasMaxLength(100);
        });

        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            e.HasOne(po => po.Branch)
                .WithMany()
                .HasForeignKey(po => po.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(po => po.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(po => po.PoNumber).IsRequired().HasMaxLength(50);
            e.HasIndex(po => po.PoNumber).IsUnique();
        });

        modelBuilder.Entity<PurchaseOrderItem>(e =>
        {
            e.HasOne(poi => poi.PurchaseOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(poi => poi.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(poi => poi.InventoryItem)
                .WithMany()
                .HasForeignKey(poi => poi.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Recipe>(e =>
        {
            e.Property(r => r.MenuItemName).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<RecipeIngredient>(e =>
        {
            e.HasOne(ri => ri.Recipe)
                .WithMany(r => r.Ingredients)
                .HasForeignKey(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ri => ri.InventoryItem)
                .WithMany(ii => ii.RecipeIngredients)
                .HasForeignKey(ri => ri.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RawMaterialPriceHistory>(e =>
        {
            e.HasOne(r => r.InventoryItem)
                .WithMany(ii => ii.PriceHistory)
                .HasForeignKey(r => r.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Supplier)
                .WithMany()
                .HasForeignKey(r => r.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WasteRecord>(e =>
        {
            e.HasOne(w => w.InventoryItem)
                .WithMany()
                .HasForeignKey(w => w.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(w => w.InventoryBatch)
                .WithMany()
                .HasForeignKey(w => w.InventoryBatchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Equipment>(e =>
        {
            e.Property(eq => eq.Name).IsRequired().HasMaxLength(200);
            e.Property(eq => eq.SerialNumber).HasMaxLength(100);
        });

        modelBuilder.Entity<MaintenanceRecord>(e =>
        {
            e.HasOne(mr => mr.Equipment)
                .WithMany(eq => eq.MaintenanceRecords)
                .HasForeignKey(mr => mr.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // === HR ===
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasIndex(emp => emp.EmployeeCode).IsUnique();
            e.Property(emp => emp.EmployeeCode).IsRequired().HasMaxLength(20);
            e.Property(emp => emp.FirstName).IsRequired().HasMaxLength(100);
            e.Property(emp => emp.LastName).IsRequired().HasMaxLength(100);
            e.Property(emp => emp.Position).HasMaxLength(100);
            e.Property(emp => emp.ContactNumber).HasMaxLength(50);
            e.HasOne(emp => emp.Branch)
                .WithMany()
                .HasForeignKey(emp => emp.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttendanceLog>(e =>
        {
            e.HasOne(al => al.Employee)
                .WithMany(emp => emp.AttendanceLogs)
                .HasForeignKey(al => al.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(al => new { al.EmployeeId, al.ClockIn });
        });

        modelBuilder.Entity<PayrollPeriod>(e =>
        {
            e.HasIndex(pp => new { pp.WeekStart, pp.WeekEnd }).IsUnique();
        });

        modelBuilder.Entity<Payroll>(e =>
        {
            e.HasOne(p => p.PayrollPeriod)
                .WithMany(pp => pp.Payrolls)
                .HasForeignKey(p => p.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Employee)
                .WithMany(emp => emp.Payrolls)
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => new { p.PayrollPeriodId, p.EmployeeId }).IsUnique();
        });

        modelBuilder.Entity<Payslip>(e =>
        {
            e.HasOne(ps => ps.Payroll)
                .WithMany(p => p.Payslips)
                .HasForeignKey(ps => ps.PayrollId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ps => ps.Employee)
                .WithMany()
                .HasForeignKey(ps => ps.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeDocument>(e =>
        {
            e.HasOne(ed => ed.Employee)
                .WithMany(emp => emp.Documents)
                .HasForeignKey(ed => ed.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Memo>(e =>
        {
            e.HasOne(m => m.Employee)
                .WithMany(emp => emp.Memos)
                .HasForeignKey(m => m.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(m => m.Subject).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<IncidentReport>(e =>
        {
            e.HasOne(ir => ir.Employee)
                .WithMany(emp => emp.IncidentReports)
                .HasForeignKey(ir => ir.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(ir => ir.Title).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<PerformanceReview>(e =>
        {
            e.HasOne(pr => pr.Employee)
                .WithMany(emp => emp.PerformanceReviews)
                .HasForeignKey(pr => pr.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MealEntitlement>(e =>
        {
            e.HasOne(me => me.Employee)
                .WithMany(emp => emp.MealEntitlements)
                .HasForeignKey(me => me.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SetSoftDeleteFilter<T>(ModelBuilder builder) where T : BaseEntity
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Branch>().HasData(
            new Branch { Id = 1, Code = "HQ", Name = "Main Branch", Address = "Manila, Philippines", ContactNumber = "02-1234567", OperatingHours = "24/7", IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
            new Branch { Id = 2, Code = "QC", Name = "Quezon City Branch", Address = "Quezon City, Philippines", ContactNumber = "02-7654321", OperatingHours = "24/7", IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now }
        );

        modelBuilder.Entity<Supplier>().HasData(
            new Supplier { Id = 1, Name = "Fresh Produce Supply Co.", ContactPerson = "Juan Dela Cruz", ContactNumber = "09171234567", Email = "juan@freshproduce.com", Address = "Manila", TinNumber = "123-456-789-000", IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now },
            new Supplier { Id = 2, Name = "Meat Distributors Inc.", ContactPerson = "Maria Santos", ContactNumber = "09189876543", Email = "maria@meatdist.com", Address = "Quezon City", TinNumber = "987-654-321-000", IsActive = true, IsDeleted = false, CreatedAt = now, UpdatedAt = now }
        );

        modelBuilder.Entity<StorageLocation>().HasData(
            new StorageLocation { Id = 1, BranchId = 1, Area = "Freezer-A", StorageArea = StorageArea.Freezer, Description = "Main freezer - meats and frozen goods", IsDeleted = false, CreatedAt = now, UpdatedAt = now },
            new StorageLocation { Id = 2, BranchId = 1, Area = "Chiller-1", StorageArea = StorageArea.Chiller, Description = "Vegetable chiller", IsDeleted = false, CreatedAt = now, UpdatedAt = now },
            new StorageLocation { Id = 3, BranchId = 1, Area = "Dry-Shelf-1", StorageArea = StorageArea.Dry, Description = "Dry goods - rice, canned goods", IsDeleted = false, CreatedAt = now, UpdatedAt = now },
            new StorageLocation { Id = 4, BranchId = 2, Area = "Freezer-B", StorageArea = StorageArea.Freezer, Description = "QC freezer", IsDeleted = false, CreatedAt = now, UpdatedAt = now }
        );
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
