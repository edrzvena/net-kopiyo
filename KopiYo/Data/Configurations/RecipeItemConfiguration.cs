using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class RecipeItemConfiguration : IEntityTypeConfiguration<RecipeItem>
{
    public void Configure(EntityTypeBuilder<RecipeItem> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.QtyPerServing).HasPrecision(18, 3);

        // Satu bahan hanya boleh muncul sekali dalam satu resep.
        b.HasIndex(x => new { x.ProductId, x.IngredientId }).IsUnique();

        // Cascade: resep dimiliki produknya.
        b.HasOne(x => x.Product)
            .WithMany(p => p.RecipeItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: bahan yang masih dipakai resep tidak boleh dihapus.
        b.HasOne(x => x.Ingredient)
            .WithMany(i => i.RecipeItems)
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
