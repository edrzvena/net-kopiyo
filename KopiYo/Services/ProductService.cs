using KopiYo.Common;
using KopiYo.Data;
using KopiYo.DTOs.Pos;
using KopiYo.Mappings;
using KopiYo.Models;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Products;
using KopiYo.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace KopiYo.Services;

public sealed class ProductService(
    AppDbContext db,
    IDateTimeProvider clock,
    IOptions<KopiYoSettings> settings) : IProductService
{
    public async Task<PagedList<ProductListItemViewModel>> GetPagedAsync(
        string? search, int? categoryId, bool? isActive, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) || (p.Sku != null && p.Sku.Contains(term)));
        }

        if (categoryId is > 0)
            query = query.Where(p => p.CategoryId == categoryId);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var projected = query
            .OrderBy(p => p.Category.DisplayOrder).ThenBy(p => p.Name)
            .Select(p => new ProductListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                CategoryName = p.Category.Name,
                BasePrice = p.BasePrice,
                IsActive = p.IsActive,
                VariantGroupCount = p.ProductVariantGroups.Count,
                RecipeItemCount = p.RecipeItems.Count
            });

        return await PagedList<ProductListItemViewModel>.CreateAsync(projected, page, pageSize, ct);
    }

    public async Task<ProductFormViewModel> BuildCreateFormAsync(CancellationToken ct)
    {
        var vm = new ProductFormViewModel();
        await RepopulateFormAsync(vm, ct);
        return vm;
    }

    public async Task<ProductFormViewModel?> GetForEditAsync(int id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .Include(p => p.ProductVariantGroups)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null) return null;

        // Di sini extension method AMAN dipakai, karena datanya sudah ter-materialisasi
        // oleh FirstOrDefaultAsync. Yang tidak boleh adalah memanggilnya di dalam .Select().
        var vm = product.ToFormViewModel();
        await RepopulateFormAsync(vm, ct);
        return vm;
    }

    public async Task RepopulateFormAsync(ProductFormViewModel vm, CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(ct);

        vm.Categories = new SelectList(categories, "Id", "Name", vm.CategoryId);

        var groups = await db.VariantGroups.AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.IsRequired,
                OptionCount = g.Options.Count(o => o.IsActive)
            })
            .ToListAsync(ct);

        var selected = vm.SelectedVariantGroupIds.ToHashSet();
        vm.VariantGroups = groups.Select(g => new CheckboxItem
        {
            Id = g.Id,
            Label = g.Name,
            Hint = $"{g.OptionCount} opsi" + (g.IsRequired ? " · wajib dipilih" : ""),
            Selected = selected.Contains(g.Id)
        }).ToList();
    }

    public async Task<ServiceResult<int>> CreateAsync(ProductFormViewModel vm, CancellationToken ct)
    {
        var validation = await ValidateAsync(vm, null, ct);
        if (!validation.Succeeded) return ServiceResult<int>.From(validation);

        var product = new Product { CreatedAt = clock.NowWib };
        vm.ApplyTo(product, clock.NowWib);
        product.UpdatedAt = null;   // baru dibuat, belum pernah diubah

        foreach (var groupId in vm.SelectedVariantGroupIds.Distinct())
            product.ProductVariantGroups.Add(new ProductVariantGroup { VariantGroupId = groupId });

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return ServiceResult<int>.Ok(product.Id);
    }

    public async Task<ServiceResult> UpdateAsync(ProductFormViewModel vm, CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.ProductVariantGroups)
            .FirstOrDefaultAsync(p => p.Id == vm.Id, ct);

        if (product is null)
            return ServiceResult.Fail("Produk tidak ditemukan.", ErrorKind.NotFound);

        var validation = await ValidateAsync(vm, vm.Id, ct);
        if (!validation.Succeeded) return validation;

        vm.ApplyTo(product, clock.NowWib);

        // Sinkronkan grup varian: buang yang tidak dicentang, tambah yang baru.
        // ProductVariantGroup adalah baris link murni, jadi hard delete di sini benar —
        // riwayat order tidak pernah merujuknya (order menyimpan snapshot-nya sendiri).
        var desired = vm.SelectedVariantGroupIds.Distinct().ToHashSet();
        var current = product.ProductVariantGroups.Select(g => g.VariantGroupId).ToHashSet();

        foreach (var link in product.ProductVariantGroups.Where(g => !desired.Contains(g.VariantGroupId)).ToList())
            product.ProductVariantGroups.Remove(link);

        foreach (var groupId in desired.Except(current))
            product.ProductVariantGroups.Add(new ProductVariantGroup { VariantGroupId = groupId });

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null)
            return ServiceResult.Fail("Produk tidak ditemukan.", ErrorKind.NotFound);

        product.IsActive = isActive;
        product.UpdatedAt = clock.NowWib;
        await db.SaveChangesAsync(ct);

        return ServiceResult.Ok();
    }

    public async Task<CatalogDto> GetPosCatalogAsync(CancellationToken ct)
    {
        // Projeksi ditulis INLINE, bukan lewat extension method di Mappings/.
        // Extension method pada entity tidak bisa diterjemahkan ke SQL — memakainya
        // di dalam .Select() akan melempar exception saat runtime. Ini jebakan
        // nomor satu ketika memakai mapper manual, jadi sengaja dikomentari di sini.
        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new CatalogCategoryDto(c.Id, c.Name, c.DisplayOrder))
            .ToListAsync(ct);

        var products = await db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Category.IsActive)
            .OrderBy(p => p.Category.DisplayOrder).ThenBy(p => p.Name)
            .Select(p => new CatalogProductDto(
                p.Id,
                p.Name,
                p.BasePrice,
                p.CategoryId,
                p.ImageUrl,
                // Hanya grup yang aktif DAN punya minimal satu opsi aktif yang
                // dikirim. Grup kosong hanya akan memunculkan modal varian hampa.
                p.ProductVariantGroups
                    .Where(g => g.VariantGroup.IsActive && g.VariantGroup.Options.Any(o => o.IsActive))
                    .OrderBy(g => g.VariantGroup.DisplayOrder)
                    .Select(g => g.VariantGroupId)
                    .ToList()))
            .ToListAsync(ct);

        // Grup varian dikirim SEKALI dan datar; produk hanya merujuknya lewat id.
        var groups = await db.VariantGroups.AsNoTracking()
            .Where(g => g.IsActive && g.Options.Any(o => o.IsActive))
            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name)
            .Select(g => new CatalogVariantGroupDto(
                g.Id,
                g.Name,
                g.SelectionMode,
                g.IsRequired,
                g.DisplayOrder,
                g.Options
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.DisplayOrder).ThenBy(o => o.Name)
                    .Select(o => new CatalogVariantOptionDto(o.Id, o.Name, o.PriceDelta))
                    .ToList()))
            .ToListAsync(ct);

        var cfg = settings.Value;
        return new CatalogDto(categories, products, groups, cfg.TaxPercent, cfg.ServiceChargePercent);
    }

    private async Task<ServiceResult> ValidateAsync(ProductFormViewModel vm, int? excludeId, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == vm.CategoryId, ct))
            return ServiceResult.Fail("Kategori yang dipilih tidak ditemukan.");

        var sku = string.IsNullOrWhiteSpace(vm.Sku) ? null : vm.Sku.Trim();
        if (sku is not null &&
            await db.Products.AnyAsync(p => p.Sku == sku && (excludeId == null || p.Id != excludeId), ct))
            return ServiceResult.Fail($"SKU '{sku}' sudah dipakai produk lain.");

        var groupIds = vm.SelectedVariantGroupIds.Distinct().ToList();
        if (groupIds.Count > 0)
        {
            var found = await db.VariantGroups.CountAsync(g => groupIds.Contains(g.Id), ct);
            if (found != groupIds.Count)
                return ServiceResult.Fail("Ada grup varian yang dipilih tapi tidak ditemukan.");
        }

        return ServiceResult.Ok();
    }
}
