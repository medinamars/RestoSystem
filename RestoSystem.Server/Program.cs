using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RestoSystem.Server.Data;
using RestoSystem.Server.Models.Common;
using RestoSystem.Server.Models.Inventory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database (InMemory for dev/testing)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("RestoSystem"));

// CORS for SPA proxy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpa", policy =>
    {
        policy.WithOrigins("https://localhost:49358", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Auto-migrate in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Seed data if empty
    if (!db.Branches.Any())
    {
        var now = DateTime.UtcNow;
        db.Branches.AddRange(
            new Branch { Code = "HQ", Name = "Main Branch", Address = "Manila, Philippines", ContactNumber = "02-1234567", OperatingHours = "24/7", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Branch { Code = "QC", Name = "Quezon City Branch", Address = "Quezon City, Philippines", ContactNumber = "02-7654321", OperatingHours = "24/7", IsActive = true, CreatedAt = now, UpdatedAt = now }
        );
        db.Suppliers.AddRange(
            new Supplier { Name = "Fresh Produce Supply Co.", ContactPerson = "Juan Dela Cruz", ContactNumber = "09171234567", Email = "juan@freshproduce.com", Address = "Manila", TinNumber = "123-456-789-000", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Supplier { Name = "Meat Distributors Inc.", ContactPerson = "Maria Santos", ContactNumber = "09189876543", Email = "maria@meatdist.com", Address = "Quezon City", TinNumber = "987-654-321-000", IsActive = true, CreatedAt = now, UpdatedAt = now }
        );
        db.StorageLocations.AddRange(
            new StorageLocation { BranchId = 1, Area = "Freezer-A", StorageArea = StorageArea.Freezer, Description = "Main freezer - meats and frozen goods", CreatedAt = now, UpdatedAt = now },
            new StorageLocation { BranchId = 1, Area = "Chiller-1", StorageArea = StorageArea.Chiller, Description = "Vegetable chiller", CreatedAt = now, UpdatedAt = now },
            new StorageLocation { BranchId = 1, Area = "Dry-Shelf-1", StorageArea = StorageArea.Dry, Description = "Dry goods - rice, canned goods", CreatedAt = now, UpdatedAt = now },
            new StorageLocation { BranchId = 2, Area = "Freezer-B", StorageArea = StorageArea.Freezer, Description = "QC freezer", CreatedAt = now, UpdatedAt = now }
        );
        await db.SaveChangesAsync();
    }
}

app.UseCors("AllowSpa");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// SPA fallback - serve index.html for client-side routes
app.MapFallbackToFile("/index.html");

app.Run();
