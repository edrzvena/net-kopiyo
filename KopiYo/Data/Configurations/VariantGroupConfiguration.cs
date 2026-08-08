using KopiYo.Common;
using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class VariantGroupConfiguration : IEntityTypeConfiguration<VariantGroup>
{
    public void Configure(EntityTypeBuilder<VariantGroup> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();

        b.Property(x => x.SelectionMode).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasData(
            new VariantGroup { Id = 1, Name = "Ukuran", SelectionMode = VariantSelectionMode.Single, IsRequired = true, DisplayOrder = 1, IsActive = true },
            new VariantGroup { Id = 2, Name = "Suhu", SelectionMode = VariantSelectionMode.Single, IsRequired = true, DisplayOrder = 2, IsActive = true },
            new VariantGroup { Id = 3, Name = "Extra", SelectionMode = VariantSelectionMode.Multiple, IsRequired = false, DisplayOrder = 3, IsActive = true }
        );
    }
}
