using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(200).IsRequired();

        b.Property(x => x.Quantity).HasPrecision(18, 3);
        b.Property(x => x.StockBefore).HasPrecision(18, 3);
        b.Property(x => x.StockAfter).HasPrecision(18, 3);

        b.HasIndex(x => new { x.IngredientId, x.CreatedAt });
        b.HasIndex(x => x.OrderId).HasFilter("[OrderId] IS NOT NULL");

        // Semua Restrict: buku besar ini append-only dan tidak boleh ikut terhapus
        // gara-gara induknya dihapus.
        b.HasOne(x => x.Ingredient)
            .WithMany(i => i.StockMovements)
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Order)
            .WithMany(o => o.StockMovements)
            .HasForeignKey(x => x.OrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.User)
            .WithMany(u => u.StockMovements)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
