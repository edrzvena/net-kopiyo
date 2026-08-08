using KopiYo.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KopiYo.Data.Configurations;

public class OrderCounterConfiguration : IEntityTypeConfiguration<OrderCounter>
{
    public void Configure(EntityTypeBuilder<OrderCounter> b)
    {
        // DateOnly dipetakan native ke kolom SQL `date` sejak EF 8 — tidak perlu converter.
        b.HasKey(x => x.BusinessDate);

        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
