using KopiYo.Common;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Variants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KopiYo.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
public class VariantGroupsController(IVariantService variants) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await variants.GetGroupsAsync(activeOnly: false, ct));

    [HttpGet]
    public IActionResult Create() => View(new VariantGroupFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(VariantGroupFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await variants.CreateGroupAsync(vm, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["StatusSuccess"] = $"Grup varian '{vm.Name}' ditambahkan. Sekarang isi opsinya.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var vm = await variants.GetGroupForEditAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(VariantGroupFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await variants.UpdateGroupAsync(vm, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["StatusSuccess"] = $"Grup varian '{vm.Name}' disimpan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken ct)
    {
        var result = await variants.SetGroupActiveAsync(id, isActive, ct);
        if (result.Succeeded)
            TempData["StatusSuccess"] = isActive ? "Grup varian diaktifkan." : "Grup varian dinonaktifkan.";
        else
            TempData["StatusError"] = result.Error;

        return RedirectToAction(nameof(Index));
    }

    // ---- Opsi varian ------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> CreateOption(int groupId, CancellationToken ct)
    {
        var vm = await variants.GetOptionForCreateAsync(groupId, ct);
        return vm is null ? NotFound() : View("OptionForm", vm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOption(VariantOptionFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("OptionForm", vm);

        var result = await variants.CreateOptionAsync(vm, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("OptionForm", vm);
        }

        TempData["StatusSuccess"] = $"Opsi '{vm.Name}' ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditOption(int id, CancellationToken ct)
    {
        var vm = await variants.GetOptionForEditAsync(id, ct);
        return vm is null ? NotFound() : View("OptionForm", vm);
    }

    [HttpPost]
    public async Task<IActionResult> EditOption(VariantOptionFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("OptionForm", vm);

        var result = await variants.UpdateOptionAsync(vm, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("OptionForm", vm);
        }

        TempData["StatusSuccess"] = $"Opsi '{vm.Name}' disimpan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetOptionActive(int id, bool isActive, CancellationToken ct)
    {
        var result = await variants.SetOptionActiveAsync(id, isActive, ct);
        if (result.Succeeded)
            TempData["StatusSuccess"] = isActive ? "Opsi diaktifkan." : "Opsi dinonaktifkan.";
        else
            TempData["StatusError"] = result.Error;

        return RedirectToAction(nameof(Index));
    }
}
