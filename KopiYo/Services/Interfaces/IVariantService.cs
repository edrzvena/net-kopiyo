using KopiYo.Common;
using KopiYo.ViewModels.Variants;

namespace KopiYo.Services.Interfaces;

public interface IVariantService
{
    Task<IReadOnlyList<VariantGroupListItemViewModel>> GetGroupsAsync(bool activeOnly, CancellationToken ct);

    Task<VariantGroupFormViewModel?> GetGroupForEditAsync(int id, CancellationToken ct);
    Task<ServiceResult<int>> CreateGroupAsync(VariantGroupFormViewModel vm, CancellationToken ct);
    Task<ServiceResult> UpdateGroupAsync(VariantGroupFormViewModel vm, CancellationToken ct);
    Task<ServiceResult> SetGroupActiveAsync(int id, bool isActive, CancellationToken ct);

    Task<VariantOptionFormViewModel?> GetOptionForCreateAsync(int groupId, CancellationToken ct);
    Task<VariantOptionFormViewModel?> GetOptionForEditAsync(int optionId, CancellationToken ct);
    Task<ServiceResult<int>> CreateOptionAsync(VariantOptionFormViewModel vm, CancellationToken ct);
    Task<ServiceResult> UpdateOptionAsync(VariantOptionFormViewModel vm, CancellationToken ct);
    Task<ServiceResult> SetOptionActiveAsync(int optionId, bool isActive, CancellationToken ct);
}
