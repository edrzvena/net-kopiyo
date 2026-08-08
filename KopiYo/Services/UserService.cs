using KopiYo.Common;
using KopiYo.Data;
using KopiYo.Models;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Shared;
using KopiYo.ViewModels.Users;
using Microsoft.AspNetCore.Identity;

namespace KopiYo.Services;

public sealed class UserService(
    AppDbContext db,
    IPasswordHasher<User> hasher,
    IDateTimeProvider clock) : IUserService
{
    public async Task<PagedList<UserListItemViewModel>> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.Username.Contains(term) || u.FullName.Contains(term));
        }

        var projected = query
            .OrderBy(u => u.Role).ThenBy(u => u.Username)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                OrderCount = u.OrdersAsCashier.Count
            });

        return await PagedList<UserListItemViewModel>.CreateAsync(projected, page, pageSize, ct);
    }

    public async Task<UserFormViewModel?> GetForEditAsync(int id, CancellationToken ct)
        => await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserFormViewModel
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                IsActive = u.IsActive
            })
            .FirstOrDefaultAsync(ct);

    public async Task<ServiceResult<int>> CreateAsync(UserFormViewModel vm, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vm.Password))
            return ServiceResult<int>.Fail("Password wajib diisi untuk user baru.");

        var username = vm.Username.Trim();
        if (await db.Users.AnyAsync(u => u.Username == username, ct))
            return ServiceResult<int>.Fail($"Username '{username}' sudah dipakai.");

        var user = new User
        {
            Username = username,
            FullName = vm.FullName.Trim(),
            Role = vm.Role,
            IsActive = vm.IsActive,
            CreatedAt = clock.NowWib
        };

        // Hash dihitung SETELAH property lain di-set: PasswordHasher<User> menerima
        // instance user-nya, dan ini menjaga polanya konsisten dengan DbInitializer.
        user.PasswordHash = hasher.HashPassword(user, vm.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return ServiceResult<int>.Ok(user.Id);
    }

    public async Task<ServiceResult> UpdateAsync(UserFormViewModel vm, int actingUserId, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == vm.Id, ct);
        if (user is null) return ServiceResult.Fail("User tidak ditemukan.", ErrorKind.NotFound);

        var username = vm.Username.Trim();
        if (await db.Users.AnyAsync(u => u.Username == username && u.Id != vm.Id, ct))
            return ServiceResult.Fail($"Username '{username}' sudah dipakai user lain.");

        // Menurunkan role diri sendiri dari Admin ke Kasir akan langsung mengunci
        // Admin itu keluar dari seluruh menu admin di request berikutnya. Ditolak.
        if (user.Id == actingUserId && user.Role == UserRole.Admin && vm.Role != UserRole.Admin)
            return ServiceResult.Fail("Tidak bisa menurunkan role akun Anda sendiri.");

        if (user.Role == UserRole.Admin && vm.Role != UserRole.Admin)
        {
            var otherActiveAdmins = await db.Users
                .CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != user.Id, ct);
            if (otherActiveAdmins == 0)
                return ServiceResult.Fail("Ini satu-satunya Admin aktif. Buat Admin lain dulu.");
        }

        user.Username = username;
        user.FullName = vm.FullName.Trim();
        user.Role = vm.Role;
        user.IsActive = vm.IsActive;

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ResetPasswordViewModel?> GetForResetPasswordAsync(int id, CancellationToken ct)
        => await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new ResetPasswordViewModel { Id = u.Id, Username = u.Username })
            .FirstOrDefaultAsync(ct);

    public async Task<ServiceResult> ResetPasswordAsync(int userId, string newPassword, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return ServiceResult.Fail("User tidak ditemukan.", ErrorKind.NotFound);

        user.PasswordHash = hasher.HashPassword(user, newPassword);
        await db.SaveChangesAsync(ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetActiveAsync(
        int userId, bool isActive, int actingUserId, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return ServiceResult.Fail("User tidak ditemukan.", ErrorKind.NotFound);

        if (!isActive)
        {
            if (user.Id == actingUserId)
                return ServiceResult.Fail("Tidak bisa menonaktifkan akun Anda sendiri.");

            if (user.Role == UserRole.Admin)
            {
                var otherActiveAdmins = await db.Users
                    .CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != user.Id, ct);
                if (otherActiveAdmins == 0)
                    return ServiceResult.Fail(
                        "Ini Admin aktif terakhir. Kalau dinonaktifkan, tidak ada yang bisa mengelola sistem.");
            }
        }

        user.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
