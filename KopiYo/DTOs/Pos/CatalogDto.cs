using KopiYo.Common;

namespace KopiYo.DTOs.Pos;

/// <summary>
/// DTO — dikirim sebagai JSON oleh GET /api/pos/catalog dan disimpan di variabel
/// JavaScript selama sesi kasir.
///
/// Bandingkan dengan ProductFormViewModel: yang itu punya SelectList dan pesan
/// validasi berbahasa Indonesia (urusan Razor); yang ini cuma data mentah.
/// Aturannya satu baris: DTO menyeberangi kabel, ViewModel di-bind oleh .cshtml.
/// Semua DTO ditulis sebagai `sealed record` supaya bedanya kelihatan sekilas.
/// </summary>
public sealed record CatalogDto(
    IReadOnlyList<CatalogCategoryDto> Categories,
    IReadOnlyList<CatalogProductDto> Products,
    IReadOnlyList<CatalogVariantGroupDto> VariantGroups,
    decimal TaxPercent,
    decimal ServiceChargePercent);

public sealed record CatalogCategoryDto(int Id, string Name, int DisplayOrder);

/// <summary>
/// Produk hanya menyimpan ID grup variannya, bukan grup lengkapnya. Untuk 40 produk
/// x 3 grup, ini beda antara payload 4 KB dan 60 KB — dan membuat modal varian di
/// JavaScript jadi lookup, bukan pencarian.
/// </summary>
public sealed record CatalogProductDto(
    int Id,
    string Name,
    decimal BasePrice,
    int CategoryId,
    string? ImageUrl,
    IReadOnlyList<int> VariantGroupIds);

public sealed record CatalogVariantGroupDto(
    int Id,
    string Name,
    VariantSelectionMode SelectionMode,
    bool IsRequired,
    int DisplayOrder,
    IReadOnlyList<CatalogVariantOptionDto> Options);

public sealed record CatalogVariantOptionDto(int Id, string Name, decimal PriceDelta);
