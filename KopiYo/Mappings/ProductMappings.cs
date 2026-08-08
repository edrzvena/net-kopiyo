using KopiYo.Models;
using KopiYo.ViewModels.Products;

namespace KopiYo.Mappings;

public static class ProductMappings
{
    public static ProductFormViewModel ToFormViewModel(this Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        CategoryId = p.CategoryId,
        Sku = p.Sku,
        Description = p.Description,
        BasePrice = p.BasePrice,
        ImageUrl = p.ImageUrl,
        IsActive = p.IsActive,
        SelectedVariantGroupIds = p.ProductVariantGroups.Select(g => g.VariantGroupId).ToList()
    };

    /// <summary>
    /// Pertahanan over-posting: hanya field yang memang boleh diubah lewat form
    /// yang di-assign. Id, CreatedAt, dan relasi tidak pernah disentuh di sini,
    /// jadi tidak ada gunanya penyerang menambahkan field itu ke request.
    /// </summary>
    public static void ApplyTo(this ProductFormViewModel vm, Product p, DateTime now)
    {
        p.Name = vm.Name.Trim();
        p.CategoryId = vm.CategoryId;
        p.Sku = string.IsNullOrWhiteSpace(vm.Sku) ? null : vm.Sku.Trim();
        p.Description = vm.Description?.Trim();
        p.BasePrice = vm.BasePrice;
        p.ImageUrl = string.IsNullOrWhiteSpace(vm.ImageUrl) ? null : vm.ImageUrl.Trim();
        p.IsActive = vm.IsActive;
        p.UpdatedAt = now;
    }
}
