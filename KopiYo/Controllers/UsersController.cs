using KopiYo.Common;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KopiYo.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
public class UsersController(IUserService users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    {
        ViewData["Search"] = search;
        return View(await users.GetPagedAsync(search, page, AppConstants.DefaultPageSize, ct));
    }

    [HttpGet]
    public IActionResult Create() => View(new UserFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(UserFormViewModel vm, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vm.Password))
            ModelState.AddModelError(nameof(vm.Password), "Password wajib diisi untuk user baru.");

        if (!ModelState.IsValid) return View(vm);

        var result = await users.CreateAsync(vm, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["StatusSuccess"] = $"User '{vm.Username}' ({vm.Role}) berhasil dibuat.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var vm = await users.GetForEditAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UserFormViewModel vm, CancellationToken ct)
    {
        // Password tidak diubah lewat form ini, jadi validasinya tidak relevan.
        ModelState.Remove(nameof(vm.Password));
        ModelState.Remove(nameof(vm.ConfirmPassword));

        if (!ModelState.IsValid) return View(vm);

        var result = await users.UpdateAsync(vm, User.GetUserId(), ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["StatusSuccess"] = $"User '{vm.Username}' berhasil disimpan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken ct)
    {
        var vm = await users.GetForResetPasswordAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await users.ResetPasswordAsync(vm.Id, vm.NewPassword, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["StatusSuccess"] = $"Password '{vm.Username}' berhasil diganti.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken ct)
    {
        var result = await users.SetActiveAsync(id, isActive, User.GetUserId(), ct);
        if (result.Succeeded)
            TempData["StatusSuccess"] = isActive ? "User diaktifkan." : "User dinonaktifkan.";
        else
            TempData["StatusError"] = result.Error;

        return RedirectToAction(nameof(Index));
    }
}
