using KopiYo.Common;
using KopiYo.Services.Interfaces;
using KopiYo.ViewModels.Ingredients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KopiYo.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
public class IngredientsController(
    IIngredientService ingredients,
    IInventoryService inventory) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, bool lowStockOnly = false, int page = 1, CancellationToken ct = default)
    {
        ViewData["Search"] = search;
        ViewData["LowStockOnly"] = lowStockOnly;
        return View(await ingredients.GetPagedAsync(
            search, lowStockOnly, page, AppConstants.DefaultPageSize, ct));
    }

    [HttpGet]
    public IActionResult Create() => View(new IngredientFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(IngredientFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await ingredients.CreateAsync(vm, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["StatusSuccess"] =
            $"Bahan '{vm.Name}' dibuat dengan stok 0. Isi stok awalnya lewat Sesuaikan Stok.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var vm = await ingredients.GetForEditAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(IngredientFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await ingredients.UpdateAsync(vm, ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["StatusSuccess"] = $"Bahan '{vm.Name}' disimpan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken ct)
    {
        var result = await ingredients.SetActiveAsync(id, isActive, ct);
        if (result.Succeeded)
            TempData["StatusSuccess"] = isActive ? "Bahan diaktifkan." : "Bahan dinonaktifkan.";
        else
            TempData["StatusError"] = result.Error;

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Adjust(int id, CancellationToken ct)
    {
        var vm = await ingredients.GetForAdjustAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Adjust(StockAdjustmentViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await inventory.AdjustAsync(
            vm.IngredientId, vm.NewQty, vm.Reason, User.GetUserId(), ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }

        TempData["StatusSuccess"] = $"Stok '{vm.IngredientName}' disesuaikan dan tercatat di buku besar.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Movements(
        int? ingredientId, DateTime? from, DateTime? to, int page = 1, CancellationToken ct = default)
    {
        ViewData["IngredientId"] = ingredientId;
        ViewData["From"] = from;
        ViewData["To"] = to;
        return View(await inventory.GetMovementsAsync(
            ingredientId, from, to, page, AppConstants.DefaultPageSize, ct));
    }
}
