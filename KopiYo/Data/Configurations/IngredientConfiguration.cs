using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();

        b.Property(x => x.Unit).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Kuantitas (18,3), bukan (18,2) — resep bisa 7,5 g. Uang tetap (18,2).
        b.Property(x => x.StockQty).HasPrecision(18, 3);
        b.Property(x => x.MinStockQty).HasPrecision(18, 3);
        b.Property(x => x.CostPerUnit).HasPrecision(18, 2);

        b.Property(x => x.IsActive).HasDefaultValue(true);
    }
}
