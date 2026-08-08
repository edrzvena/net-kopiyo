using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class ProductVariantGroupConfiguration : IEntityTypeConfiguration<ProductVariantGroup>
{
    public void Configure(EntityTypeBuilder<ProductVariantGroup> b)
    {
        b.HasKey(x => new { x.ProductId, x.VariantGroupId });

        // Cascade: baris link ini milik produknya, tidak punya arti sendiri.
        b.HasOne(x => x.Product)
            .WithMany(p => p.ProductVariantGroups)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.VariantGroup)
            .WithMany(g => g.ProductVariantGroups)
            .HasForeignKey(x => x.VariantGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
