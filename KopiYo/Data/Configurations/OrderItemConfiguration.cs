using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.ProductNameSnapshot).HasMaxLength(150).IsRequired();
        b.Property(x => x.CategoryNameSnapshot).HasMaxLength(100).IsRequired();
        b.Property(x => x.VariantDescription).HasMaxLength(250);
        b.Property(x => x.Note).HasMaxLength(200);

        // Index untuk pengelompokan best seller.
        b.HasIndex(x => new { x.ProductId, x.OrderId });

        // Cascade: item tidak punya kehidupan di luar order-nya.
        b.HasOne(x => x.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: produk tidak pernah dihapus (hanya dinonaktifkan), jadi FK ini
        // tidak akan pernah menggantung — dan Restrict memastikan itu tetap benar
        // walaupun nanti ada yang iseng mencoba DELETE lewat SQL.
        b.HasOne(x => x.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
