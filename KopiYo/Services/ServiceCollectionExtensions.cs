using KopiYo.Services.Interfaces;

namespace KopiYo.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Semua service KopiYo didaftarkan di satu tempat, supaya Program.cs tidak
    /// berubah jadi daftar AddScoped sepanjang 30 baris.
    /// Scoped, karena semuanya memakai AppDbContext yang juga scoped.
    /// </summary>
    public static IServiceCollection AddKopiYoServices(this IServiceCollection services)
    {
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IVariantService, VariantService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IIngredientService, IngredientService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddSingleton<ICsvExporter, CsvExporter>();   // stateless

        return services;
    }
}
