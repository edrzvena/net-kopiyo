using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class VariantOptionConfiguration : IEntityTypeConfiguration<VariantOption>
{
    public void Configure(EntityTypeBuilder<VariantOption> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(50).IsRequired();
        b.Property(x => x.PriceDelta).HasPrecision(18, 2);
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => new { x.VariantGroupId, x.Name }).IsUnique();

        // Restrict: opsi varian dirujuk oleh OrderItemVariant (riwayat), jadi
        // menghapus grup tidak boleh ikut menghapus opsinya.
        b.HasOne(x => x.VariantGroup)
            .WithMany(g => g.Options)
            .HasForeignKey(x => x.VariantGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasData(
            new VariantOption { Id = 1, VariantGroupId = 1, Name = "S", PriceDelta = 0m, DisplayOrder = 1, IsActive = true },
            new VariantOption { Id = 2, VariantGroupId = 1, Name = "M", PriceDelta = 3000m, DisplayOrder = 2, IsActive = true },
            new VariantOption { Id = 3, VariantGroupId = 1, Name = "L", PriceDelta = 6000m, DisplayOrder = 3, IsActive = true },
            new VariantOption { Id = 4, VariantGroupId = 2, Name = "Hot", PriceDelta = 0m, DisplayOrder = 1, IsActive = true },
            new VariantOption { Id = 5, VariantGroupId = 2, Name = "Ice", PriceDelta = 2000m, DisplayOrder = 2, IsActive = true },
            new VariantOption { Id = 6, VariantGroupId = 3, Name = "Extra Shot", PriceDelta = 8000m, DisplayOrder = 1, IsActive = true },
            new VariantOption { Id = 7, VariantGroupId = 3, Name = "Less Sugar", PriceDelta = 0m, DisplayOrder = 2, IsActive = true },
            new VariantOption { Id = 8, VariantGroupId = 3, Name = "Extra Cheese", PriceDelta = 5000m, DisplayOrder = 3, IsActive = true }
        );
    }
}
