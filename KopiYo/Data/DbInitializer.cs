using KopiYo.Common;
using KopiYo.Models;
using Microsoft.AspNetCore.Identity;

namespace KopiYo.Data;

/// <summary>
/// Seeding saat runtime, idempotent (aman dipanggil tiap kali aplikasi start).
///
/// Kenapa tidak semuanya lewat HasData: PasswordHasher membuat salt acak setiap
/// dipanggil, dan DateTime.Now jelas berubah terus. Kalau nilai seperti itu masuk
/// HasData, setiap `dotnet ef migrations add` akan menghasilkan migration UpdateData
/// yang sia-sia. Jadi: HasData untuk lookup statis, DbInitializer untuk sisanya.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher<User> hasher,
        ILogger logger,
        bool isDevelopment,
        CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        await SeedUsersAsync(db, hasher, logger, ct);
        await SeedIngredientsAsync(db, ct);

        if (isDevelopment)
            await SeedDemoProductsAsync(db, ct);
    }

    private static async Task SeedUsersAsync(
        AppDbContext db, IPasswordHasher<User> hasher, ILogger logger, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct)) return;

        var now = DateTime.Now;

        var admin = new User
        {
            Username = "admin",
            FullName = "Administrator",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = now
        };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");

        var kasir = new User
        {
            Username = "kasir",
            FullName = "Kasir Satu",
            Role = UserRole.Kasir,
            IsActive = true,
            CreatedAt = now
        };
        kasir.PasswordHash = hasher.HashPassword(kasir, "Kasir123!");

        db.Users.AddRange(admin, kasir);
        await db.SaveChangesAsync(ct);

        logger.LogWarning(
            "User default dibuat: admin/Admin123! dan kasir/Kasir123!. GANTI PASSWORD-NYA sebelum dipakai sungguhan.");
    }

    private static async Task SeedIngredientsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Ingredients.AnyAsync(ct)) return;

        var now = DateTime.Now;
        db.Ingredients.AddRange(
            New("Biji Kopi Arabika", UnitOfMeasure.Gram, 5000m, 1000m, 250m, now),
            New("Susu UHT", UnitOfMeasure.Ml, 10000m, 2000m, 20m, now),
            New("Sirup Vanilla", UnitOfMeasure.Ml, 2000m, 500m, 90m, now),
            New("Gula Cair", UnitOfMeasure.Ml, 3000m, 500m, 25m, now),
            New("Bubuk Matcha", UnitOfMeasure.Gram, 1000m, 200m, 400m, now),
            New("Es Batu", UnitOfMeasure.Gram, 20000m, 5000m, 2m, now),
            New("Cup 12oz", UnitOfMeasure.Pcs, 500m, 100m, 1200m, now),
            New("Croissant Beku", UnitOfMeasure.Pcs, 60m, 15m, 9000m, now)
        );
        await db.SaveChangesAsync(ct);

        static Ingredient New(string name, UnitOfMeasure unit, decimal stock, decimal min, decimal cost, DateTime now)
            => new()
            {
                Name = name, Unit = unit, StockQty = stock, MinStockQty = min,
                CostPerUnit = cost, IsActive = true, CreatedAt = now
            };
    }

    private static async Task SeedDemoProductsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(ct)) return;

        var now = DateTime.Now;
        var ingredients = await db.Ingredients.ToDictionaryAsync(i => i.Name, ct);

        // (nama, kategoriId, harga, grup varian yang dipasang, resep)
        var seed = new List<(string Name, int CategoryId, decimal Price, int[] Groups, (string Ingredient, decimal Qty)[] Recipe)>
        {
            ("Espresso",      1, 18000m, [1, 2],    [("Biji Kopi Arabika", 18m), ("Cup 12oz", 1m)]),
            ("Americano",     1, 20000m, [1, 2, 3], [("Biji Kopi Arabika", 18m), ("Cup 12oz", 1m), ("Es Batu", 80m)]),
            ("Caffe Latte",   1, 25000m, [1, 2, 3], [("Biji Kopi Arabika", 18m), ("Susu UHT", 150m), ("Cup 12oz", 1m)]),
            ("Cappuccino",    1, 25000m, [1, 2, 3], [("Biji Kopi Arabika", 18m), ("Susu UHT", 120m), ("Cup 12oz", 1m)]),
            ("Kopi Susu Gula Aren", 1, 22000m, [1, 2, 3], [("Biji Kopi Arabika", 18m), ("Susu UHT", 120m), ("Gula Cair", 30m), ("Cup 12oz", 1m)]),
            ("Matcha Latte",  2, 28000m, [1, 2],    [("Bubuk Matcha", 12m), ("Susu UHT", 180m), ("Cup 12oz", 1m)]),
            ("Vanilla Milk",  2, 21000m, [1, 2],    [("Susu UHT", 200m), ("Sirup Vanilla", 20m), ("Cup 12oz", 1m)]),
            ("Croissant",     3, 22000m, [],        [("Croissant Beku", 1m)]),
            ("Butter Toast",  3, 18000m, [],        []),
            ("Tiramisu Slice",4, 32000m, [],        [])
        };

        foreach (var (name, categoryId, price, groups, recipe) in seed)
        {
            var product = new Product
            {
                Name = name,
                CategoryId = categoryId,
                BasePrice = price,
                IsActive = true,
                CreatedAt = now
            };

            foreach (var groupId in groups)
                product.ProductVariantGroups.Add(new ProductVariantGroup { VariantGroupId = groupId });

            foreach (var (ingredientName, qty) in recipe)
                if (ingredients.TryGetValue(ingredientName, out var ing))
                    product.RecipeItems.Add(new RecipeItem { IngredientId = ing.Id, QtyPerServing = qty });

            db.Products.Add(product);
        }

        await db.SaveChangesAsync(ct);
    }
}
