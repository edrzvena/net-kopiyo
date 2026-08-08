using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();

        b.Property(x => x.Description).HasMaxLength(250);
        b.Property(x => x.IsActive).HasDefaultValue(true);

        // Data lookup statis boleh lewat HasData: tidak ada nilai acak/volatile di sini,
        // sehingga `dotnet ef migrations add` tidak menghasilkan UpdateData berulang.
        b.HasData(
            new Category { Id = 1, Name = "Kopi", Description = "Signature KopiYo", DisplayOrder = 1, IsActive = true, CreatedAt = SeedConstants.SeedDate },
            new Category { Id = 2, Name = "Non-Kopi", Description = "Teh, cokelat, matcha", DisplayOrder = 2, IsActive = true, CreatedAt = SeedConstants.SeedDate },
            new Category { Id = 3, Name = "Makanan", Description = "Pastry & snack", DisplayOrder = 3, IsActive = true, CreatedAt = SeedConstants.SeedDate },
            new Category { Id = 4, Name = "Dessert", Description = "Cake & manis-manis", DisplayOrder = 4, IsActive = true, CreatedAt = SeedConstants.SeedDate }
        );
    }
}
