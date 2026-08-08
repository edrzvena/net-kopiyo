using KopiYo.Models;

namespace KopiYo.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<VariantGroup> VariantGroups => Set<VariantGroup>();
    public DbSet<VariantOption> VariantOptions => Set<VariantOption>();
    public DbSet<ProductVariantGroup> ProductVariantGroups => Set<ProductVariantGroup>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemVariant> OrderItemVariants => Set<OrderItemVariant>();
    public DbSet<OrderCounter> OrderCounters => Set<OrderCounter>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SATU baris ini memungut semua kelas IEntityTypeConfiguration<T> di assembly.
        // Jangan pernah menulis modelBuilder.Entity<X>(...) langsung di sini — begitulah
        // caranya file ini tumbuh jadi 600 baris yang tidak ada yang mau menyentuh.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Default global, jauh lebih aman daripada menabur HasColumnType("decimal(18,2)")
        // satu per satu dan lupa di satu tempat (yang diam-diam jadi decimal(18,0)
        // alias semua rupiah dibulatkan hilang).
        // Kolom kuantitas bahan meng-override ini dengan HasPrecision(18, 3) di config-nya.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);

        // Cegah string tanpa MaxLength diam-diam jadi nvarchar(max), yang tidak bisa di-index.
        configurationBuilder.Properties<string>().HaveMaxLength(200);
    }
}
