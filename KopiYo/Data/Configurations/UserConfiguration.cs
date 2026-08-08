using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.Username).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Username).IsUnique();

        b.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();

        // Enum disimpan sebagai string supaya kolomnya terbaca "Admin"/"Kasir" di SSMS.
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

        b.Property(x => x.IsActive).HasDefaultValue(true);
    }
}
