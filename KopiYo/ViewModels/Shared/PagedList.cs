using KopiYo.Common;

namespace KopiYo.ViewModels.Shared;

/// <summary>
/// Bagian non-generic dari PagedList&lt;T&gt;. Ada supaya partial _Pagination.cshtml
/// bisa menerima paging apa pun tanpa peduli tipe itemnya.
/// </summary>
public interface IPagedListMetadata
{
    int TotalItems { get; }
    int Page { get; }
    int PageSize { get; }
    int TotalPages { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
}

/// <summary>
/// Hasil query yang sudah dipotong per halaman, plus metadata untuk render pagination.
/// ViewModel, bukan DTO: dipakai oleh .cshtml, tidak pernah dikirim sebagai JSON.
/// </summary>
public class PagedList<T> : IPagedListMetadata
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalItems { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = AppConstants.DefaultPageSize;

    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static PagedList<T> Empty(int pageSize = AppConstants.DefaultPageSize)
        => new() { PageSize = pageSize };

    /// <summary>
    /// Menjalankan CountAsync + Skip/Take dalam satu tempat, supaya tidak ada
    /// service yang lupa Skip-nya (yang gejalanya: halaman 2 isinya sama dengan halaman 1).
    /// </summary>
    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = AppConstants.DefaultPageSize;

        var total = await source.CountAsync(ct);
        var items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedList<T> { Items = items, TotalItems = total, Page = page, PageSize = pageSize };
    }
}
