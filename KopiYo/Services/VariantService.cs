using KopiYo.Common;
using KopiYo.Data;
using KopiYo.Models;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Variants;

namespace KopiYo.Services;

public sealed class VariantService(AppDbContext db) : IVariantService
{
    public async Task<IReadOnlyList<VariantGroupListItemViewModel>> GetGroupsAsync(
        bool activeOnly, CancellationToken ct)
    {
        var query = db.VariantGroups.AsNoTracking();
        if (activeOnly) query = query.Where(g => g.IsActive);

        return await query
            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name)
            .Select(g => new VariantGroupListItemViewModel
            {
                Id = g.Id,
                Name = g.Name,
                SelectionMode = g.SelectionMode,
                IsRequired = g.IsRequired,
                DisplayOrder = g.DisplayOrder,
                IsActive = g.IsActive,
                ProductCount = g.ProductVariantGroups.Count,
                Options = g.Options
                    .OrderBy(o => o.DisplayOrder).ThenBy(o => o.Name)
                    .Select(o => new VariantOptionListItemViewModel
                    {
                        Id = o.Id,
                        Name = o.Name,
                        PriceDelta = o.PriceDelta,
                        DisplayOrder = o.DisplayOrder,
                        IsActive = o.IsActive
                    }).ToList()
            })
            .ToListAsync(ct);
    }

    public async Task<VariantGroupFormViewModel?> GetGroupForEditAsync(int id, CancellationToken ct)
        => await db.VariantGroups.AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new VariantGroupFormViewModel
            {
                Id = g.Id,
                Name = g.Name,
                SelectionMode = g.SelectionMode,
                IsRequired = g.IsRequired,
                DisplayOrder = g.DisplayOrder,
                IsActive = g.IsActive
            })
            .FirstOrDefaultAsync(ct);

    public async Task<ServiceResult<int>> CreateGroupAsync(VariantGroupFormViewModel vm, CancellationToken ct)
    {
        var name = vm.Name.Trim();
        if (await db.VariantGroups.AnyAsync(g => g.Name == name, ct))
            return ServiceResult<int>.Fail($"Grup varian '{name}' sudah ada.");

        var group = new VariantGroup
        {
            Name = name,
            SelectionMode = vm.SelectionMode,
            IsRequired = vm.IsRequired,
            DisplayOrder = vm.DisplayOrder,
            IsActive = vm.IsActive
        };

        db.VariantGroups.Add(group);
        await db.SaveChangesAsync(ct);
        return ServiceResult<int>.Ok(group.Id);
    }

    public async Task<ServiceResult> UpdateGroupAsync(VariantGroupFormViewModel vm, CancellationToken ct)
    {
        var group = await db.VariantGroups.FirstOrDefaultAsync(g => g.Id == vm.Id, ct);
        if (group is null) return ServiceResult.Fail("Grup varian tidak ditemukan.", ErrorKind.NotFound);

        var name = vm.Name.Trim();
        if (await db.VariantGroups.AnyAsync(g => g.Name == name && g.Id != vm.Id, ct))
            return ServiceResult.Fail($"Grup varian '{name}' sudah dipakai.");

        // Mengubah Multiple -> Single saat grup ini sudah terpasang di produk itu aman:
        // validasi "maksimal satu pilihan" dijalankan ulang di setiap checkout, dan
        // order lama tidak terpengaruh karena varian-nya sudah di-snapshot.
        group.Name = name;
        group.SelectionMode = vm.SelectionMode;
        group.IsRequired = vm.IsRequired;
        group.DisplayOrder = vm.DisplayOrder;
        group.IsActive = vm.IsActive;

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetGroupActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var group = await db.VariantGroups.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (group is null) return ServiceResult.Fail("Grup varian tidak ditemukan.", ErrorKind.NotFound);

        group.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<VariantOptionFormViewModel?> GetOptionForCreateAsync(int groupId, CancellationToken ct)
    {
        var group = await db.VariantGroups.AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => new { g.Id, g.Name })
            .FirstOrDefaultAsync(ct);

        if (group is null) return null;

        return new VariantOptionFormViewModel { VariantGroupId = group.Id, GroupName = group.Name };
    }

    public async Task<VariantOptionFormViewModel?> GetOptionForEditAsync(int optionId, CancellationToken ct)
        => await db.VariantOptions.AsNoTracking()
            .Where(o => o.Id == optionId)
            .Select(o => new VariantOptionFormViewModel
            {
                Id = o.Id,
                VariantGroupId = o.VariantGroupId,
                GroupName = o.VariantGroup.Name,
                Name = o.Name,
                PriceDelta = o.PriceDelta,
                DisplayOrder = o.DisplayOrder,
                IsActive = o.IsActive
            })
            .FirstOrDefaultAsync(ct);

    public async Task<ServiceResult<int>> CreateOptionAsync(VariantOptionFormViewModel vm, CancellationToken ct)
    {
        if (!await db.VariantGroups.AnyAsync(g => g.Id == vm.VariantGroupId, ct))
            return ServiceResult<int>.Fail("Grup varian tidak ditemukan.", ErrorKind.NotFound);

        var name = vm.Name.Trim();
        if (await db.VariantOptions.AnyAsync(o => o.VariantGroupId == vm.VariantGroupId && o.Name == name, ct))
            return ServiceResult<int>.Fail($"Opsi '{name}' sudah ada di grup ini.");

        var option = new VariantOption
        {
            VariantGroupId = vm.VariantGroupId,
            Name = name,
            PriceDelta = vm.PriceDelta,
            DisplayOrder = vm.DisplayOrder,
            IsActive = vm.IsActive
        };

        db.VariantOptions.Add(option);
        await db.SaveChangesAsync(ct);
        return ServiceResult<int>.Ok(option.Id);
    }

    public async Task<ServiceResult> UpdateOptionAsync(VariantOptionFormViewModel vm, CancellationToken ct)
    {
        var option = await db.VariantOptions.FirstOrDefaultAsync(o => o.Id == vm.Id, ct);
        if (option is null) return ServiceResult.Fail("Opsi varian tidak ditemukan.", ErrorKind.NotFound);

        var name = vm.Name.Trim();
        if (await db.VariantOptions.AnyAsync(
                o => o.VariantGroupId == option.VariantGroupId && o.Name == name && o.Id != vm.Id, ct))
            return ServiceResult.Fail($"Opsi '{name}' sudah ada di grup ini.");

        // Mengubah PriceDelta di sini HANYA memengaruhi penjualan berikutnya.
        // Order lama menyimpan PriceDelta-nya sendiri di OrderItemVariant.
        option.Name = name;
        option.PriceDelta = vm.PriceDelta;
        option.DisplayOrder = vm.DisplayOrder;
        option.IsActive = vm.IsActive;

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetOptionActiveAsync(int optionId, bool isActive, CancellationToken ct)
    {
        var option = await db.VariantOptions.FirstOrDefaultAsync(o => o.Id == optionId, ct);
        if (option is null) return ServiceResult.Fail("Opsi varian tidak ditemukan.", ErrorKind.NotFound);

        option.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
