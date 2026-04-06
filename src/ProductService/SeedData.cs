namespace ProductService;

using ProductService.Models;
using ProductService.Services;

public static class SeedData
{
    public static void Initialize(ProductDbContext db)
    {
        if (db.Products.Any()) return;

        db.Products.AddRange(
            new Product { Id = 1, Name = "Wireless Mouse", Category = "Electronics", Price = 29.99m, Stock = 150 },
            new Product { Id = 2, Name = "Mechanical Keyboard", Category = "Electronics", Price = 89.99m, Stock = 75 },
            new Product { Id = 3, Name = "USB-C Hub", Category = "Accessories", Price = 49.99m, Stock = 200 },
            new Product { Id = 4, Name = "Monitor Stand", Category = "Furniture", Price = 39.99m, Stock = 50 },
            new Product { Id = 5, Name = "Webcam HD", Category = "Electronics", Price = 59.99m, Stock = 100 }
        );
        db.SaveChanges();
        Console.WriteLine("🌱 Seeded 5 demo products");
    }
}
