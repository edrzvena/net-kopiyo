using KopiYo.Common;
using KopiYo.ViewModels.Shared;
using KopiYo.ViewModels.Users;

namespace KopiYo.Services.Interfaces;

public interface IUserService
{
    Task<PagedList<UserListItemViewModel>> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct);

    Task<UserFormViewModel?> GetForEditAsync(int id, CancellationToken ct);
    Task<ServiceResult<int>> CreateAsync(UserFormViewModel vm, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(UserFormViewModel vm, int actingUserId, CancellationToken ct);

    Task<ResetPasswordViewModel?> GetForResetPasswordAsync(int id, CancellationToken ct);
    Task<ServiceResult> ResetPasswordAsync(int userId, string newPassword, CancellationToken ct);

    /// <summary>
    /// Punya dua pengaman: tidak bisa menonaktifkan diri sendiri (biar tidak
    /// mengunci diri keluar), dan tidak bisa menonaktifkan Admin aktif terakhir
    /// (biar tidak ada database yang kehilangan semua adminnya).
    /// </summary>
    Task<ServiceResult> SetActiveAsync(int userId, bool isActive, int actingUserId, CancellationToken ct);
}
