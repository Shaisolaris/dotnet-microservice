namespace ProductService.Services;

using Microsoft.EntityFrameworkCore;
using ProductService.Models;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Sku).IsUnique();
            e.Property(p => p.Price).HasPrecision(10, 2);
        });

        // Seed data
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Wireless Mouse", Sku = "WM-001", Description = "Ergonomic wireless mouse", Price = 29.99m, Stock = 150, Category = "Electronics" },
            new Product { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Mechanical Keyboard", Sku = "MK-001", Description = "RGB mechanical keyboard", Price = 89.99m, Stock = 75, Category = "Electronics" },
            new Product { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "USB-C Hub", Sku = "HB-001", Description = "7-in-1 USB-C hub", Price = 49.99m, Stock = 200, Category = "Accessories" }
        );
    }
}

public class ProductService
{
    private readonly ProductDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ProductDbContext db, ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Product>> GetAllAsync(string? category = null, bool? active = null)
    {
        var query = _db.Products.AsQueryable();
        if (category != null) query = query.Where(p => p.Category == category);
        if (active.HasValue) query = query.Where(p => p.IsActive == active.Value);
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id) => await _db.Products.FindAsync(id);
    public async Task<Product?> GetBySkuAsync(string sku) => await _db.Products.FirstOrDefaultAsync(p => p.Sku == sku);

    public async Task<Product> CreateAsync(CreateProductRequest req)
    {
        var product = new Product
        {
            Name = req.Name, Sku = req.Sku, Description = req.Description,
            Price = req.Price, Stock = req.Stock, Category = req.Category,
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Product created: {Sku}", product.Sku);
        return product;
    }

    public async Task<Product?> UpdateAsync(Guid id, UpdateProductRequest req)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return null;

        if (req.Name != null) product.Name = req.Name;
        if (req.Description != null) product.Description = req.Description;
        if (req.Price.HasValue) product.Price = req.Price.Value;
        if (req.Category != null) product.Category = req.Category;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product?> UpdateStockAsync(Guid id, int quantity)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return null;
        if (product.Stock + quantity < 0) throw new InvalidOperationException("Insufficient stock");

        product.Stock += quantity;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return false;
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
