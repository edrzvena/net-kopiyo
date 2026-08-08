using KopiYo.Common;
using KopiYo.Data;
using KopiYo.Mappings;
using KopiYo.Models;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Categories;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KopiYo.Services;

public sealed class CategoryService(AppDbContext db, IDateTimeProvider clock) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryListItemViewModel>> GetAllAsync(
        bool activeOnly, CancellationToken ct)
    {
        var query = db.Categories.AsNoTracking();
        if (activeOnly) query = query.Where(c => c.IsActive);

        // Projeksi ditulis INLINE, bukan lewat extension method, supaya bisa
        // diterjemahkan ke SQL dan hanya kolom yang dipakai yang ditarik.
        return await query
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryListItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync(ct);
    }

    public async Task<CategoryFormViewModel?> GetForEditAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return category?.ToFormViewModel();
    }

    public async Task<ServiceResult<int>> CreateAsync(CategoryFormViewModel vm, CancellationToken ct)
    {
        var name = vm.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == name, ct))
            return ServiceResult<int>.Fail($"Kategori '{name}' sudah ada.");

        var category = new Category { CreatedAt = clock.NowWib };
        vm.ApplyTo(category);

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return ServiceResult<int>.Ok(category.Id);
    }

    public async Task<ServiceResult> UpdateAsync(CategoryFormViewModel vm, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == vm.Id, ct);
        if (category is null)
            return ServiceResult.Fail("Kategori tidak ditemukan.", ErrorKind.NotFound);

        var name = vm.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == name && c.Id != vm.Id, ct))
            return ServiceResult.Fail($"Kategori '{name}' sudah dipakai kategori lain.");

        vm.ApplyTo(category);
        await db.SaveChangesAsync(ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
            return ServiceResult.Fail("Kategori tidak ditemukan.", ErrorKind.NotFound);

        // Menonaktifkan kategori tidak otomatis menonaktifkan produknya; produk yang
        // kategorinya mati tetap bisa dijual sampai produknya sendiri dinonaktifkan.
        // Karena itu Admin diberi tahu dulu, bukan dibiarkan menebak.
        if (!isActive)
        {
            var activeProducts = await db.Products.CountAsync(p => p.CategoryId == id && p.IsActive, ct);
            if (activeProducts > 0)
                return ServiceResult.Fail(
                    $"Kategori '{category.Name}' masih punya {activeProducts} produk aktif. " +
                    "Nonaktifkan produknya dulu.");
        }

        category.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<SelectList> GetSelectListAsync(int? selectedId, CancellationToken ct)
    {
        var items = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(ct);

        return new SelectList(items, "Id", "Name", selectedId);
    }
}
