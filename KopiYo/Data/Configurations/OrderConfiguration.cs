using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.OrderNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.OrderNumber).IsUnique();   // jaring pengaman terakhir untuk race nomor order

        b.Property(x => x.CashierNameSnapshot).HasMaxLength(100).IsRequired();
        b.Property(x => x.Note).HasMaxLength(250);
        b.Property(x => x.ReversalReason).HasMaxLength(250);

        b.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Persen pakai (5,2): cukup untuk 0,00 - 100,00.
        b.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        b.Property(x => x.ServiceChargePercent).HasPrecision(5, 2);
        b.Property(x => x.TaxPercent).HasPrecision(5, 2);
        // Kolom uang lain memakai default konvensi (18,2) dari ConfigureConventions.

        // Index yang dipakai hampir semua laporan.
        b.HasIndex(x => new { x.OrderDate, x.Status });
        b.HasIndex(x => new { x.CashierId, x.OrderDate });

        // PENTING: ada DUA FK dari Order ke Users (CashierId dan ReversedByUserId).
        // EF tidak bisa menebak navigasi baliknya, jadi keduanya WAJIB dikonfigurasi
        // eksplisit — kalau tidak, model gagal dibangun dengan pesan
        // "Unable to determine the relationship represented by navigation...".
        b.HasOne(x => x.Cashier)
            .WithMany(u => u.OrdersAsCashier)
            .HasForeignKey(x => x.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ReversedByUser)
            .WithMany(u => u.OrdersReversed)
            .HasForeignKey(x => x.ReversedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
