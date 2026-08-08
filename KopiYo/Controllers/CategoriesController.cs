using KopiYo.Common;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KopiYo.Controllers;

/// <summary>
/// POLA DASAR CRUD. Controller lain di aplikasi ini menyalin bentuk yang sama:
///  - role di level class,
///  - action tipis (validasi ModelState, panggil service, terjemahkan hasil),
///  - POST selalu diakhiri redirect (POST-Redirect-Get) supaya refresh tidak
///    mengirim ulang form,
///  - pesan hasil lewat TempData,
///  - tidak ada Delete, hanya SetActive.
///
/// Pengulangan pola ini di 5 controller itu DISENGAJA. Base class generic
/// CrudController&lt;T&gt; terlihat pintar, tapi begitu satu entity butuh perilaku
/// beda sedikit, semuanya berantakan.
/// </summary>
[Authorize(Roles = AppConstants.Roles.Admin)]
public class CategoriesController(ICategoryService categories) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // activeOnly: false — layar admin harus melihat yang nonaktif juga.
        var items = await categories.GetAllAsync(activeOnly: false, ct);
        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View(new CategoryFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CategoryFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await categories.CreateAsync(vm, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["StatusSuccess"] = $"Kategori '{vm.Name}' berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var vm = await categories.GetForEditAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CategoryFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await categories.UpdateAsync(vm, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["StatusSuccess"] = $"Kategori '{vm.Name}' berhasil disimpan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken ct)
    {
        var result = await categories.SetActiveAsync(id, isActive, ct);

        if (result.Succeeded)
            TempData["StatusSuccess"] = isActive ? "Kategori diaktifkan." : "Kategori dinonaktifkan.";
        else
            TempData["StatusError"] = result.Error;

        return RedirectToAction(nameof(Index));
    }
}
