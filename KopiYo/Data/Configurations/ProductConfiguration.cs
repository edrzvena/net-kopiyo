using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Sku).HasMaxLength(30);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.ImageUrl).HasMaxLength(300);
        b.Property(x => x.BasePrice).HasPrecision(18, 2);
        b.Property(x => x.IsActive).HasDefaultValue(true);

        // Filtered unique index. Tanpa HasFilter, SQL Server menganggap banyak NULL
        // sebagai duplikat, jadi hanya boleh ada SATU produk tanpa SKU di seluruh tabel.
        b.HasIndex(x => x.Sku).IsUnique().HasFilter("[Sku] IS NOT NULL");

        // Index untuk query katalog POS (produk aktif per kategori).
        b.HasIndex(x => new { x.CategoryId, x.IsActive });

        b.HasOne(x => x.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
