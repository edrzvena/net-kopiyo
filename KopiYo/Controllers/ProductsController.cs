using KopiYo.Common;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KopiYo.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
public class ProductsController(
    IProductService products,
    ICategoryService categories,
    IRecipeService recipes) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, int? categoryId, bool? isActive, int page = 1, CancellationToken ct = default)
    {
        ViewData["Search"] = search;
        ViewData["CategoryId"] = categoryId;
        ViewData["IsActive"] = isActive;
        ViewData["Categories"] = await categories.GetSelectListAsync(categoryId, ct);

        var result = await products.GetPagedAsync(
            search, categoryId, isActive, page, AppConstants.DefaultPageSize, ct);

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
        => View(await products.BuildCreateFormAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(ProductFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // SelectList dan daftar checkbox tidak ikut ter-post balik, jadi harus
            // diisi ulang sebelum me-render View — kalau lupa, dropdown-nya kosong.
            await products.RepopulateFormAsync(vm, ct);
            return View(vm);
        }

        var result = await products.CreateAsync(vm, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await products.RepopulateFormAsync(vm, ct);
            return View(vm);
        }

        TempData["StatusSuccess"] = $"Produk '{vm.Name}' berhasil ditambahkan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var vm = await products.GetForEditAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProductFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await products.RepopulateFormAsync(vm, ct);
            return View(vm);
        }

        var result = await products.UpdateAsync(vm, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await products.RepopulateFormAsync(vm, ct);
            return View(vm);
        }

        TempData["StatusSuccess"] = $"Produk '{vm.Name}' berhasil disimpan.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Editor resep: menentukan bahan apa dan berapa banyak yang terpakai
    /// setiap kali produk ini terjual.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Recipe(int id, CancellationToken ct)
    {
        var vm = await recipes.GetRecipeAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Recipe(ProductRecipeViewModel vm, CancellationToken ct)
    {
        var result = await recipes.SaveRecipeAsync(vm.ProductId, vm.Lines, ct);
        if (!result.Succeeded)
        {
            TempData["StatusError"] = result.Error;
            return RedirectToAction(nameof(Recipe), new { id = vm.ProductId });
        }

        TempData["StatusSuccess"] = $"Resep '{vm.ProductName}' disimpan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken ct)
    {
        var result = await products.SetActiveAsync(id, isActive, ct);
        if (result.Succeeded)
            TempData["StatusSuccess"] = isActive ? "Produk diaktifkan." : "Produk dinonaktifkan.";
        else
            TempData["StatusError"] = result.Error;

        return RedirectToAction(nameof(Index));
    }
}
