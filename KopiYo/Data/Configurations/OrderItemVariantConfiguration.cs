using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class OrderItemVariantConfiguration : IEntityTypeConfiguration<OrderItemVariant>
{
    public void Configure(EntityTypeBuilder<OrderItemVariant> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.GroupNameSnapshot).HasMaxLength(50).IsRequired();
        b.Property(x => x.OptionNameSnapshot).HasMaxLength(50).IsRequired();
        b.Property(x => x.PriceDelta).HasPrecision(18, 2);

        b.HasOne(x => x.OrderItem)
            .WithMany(i => i.Variants)
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.VariantOption)
            .WithMany(o => o.OrderItemVariants)
            .HasForeignKey(x => x.VariantOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
