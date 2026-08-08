namespace KopiYo.Common;

/// <summary>
/// Membedakan kegagalan "salah input" dari kegagalan "kondisi data bentrok",
/// supaya controller bisa memetakannya ke status HTTP yang benar tanpa menebak
/// dari isi pesan error.
/// </summary>
public enum ErrorKind
{
    Validation = 0, // -> 400
    NotFound = 1,   // -> 404
    Conflict = 2    // -> 409  (contoh: stok bahan tidak mencukupi)
}

/// <summary>
/// Hasil operasi service tanpa nilai kembalian.
/// Dipakai supaya service tidak perlu melempar exception untuk kegagalan yang
/// sebenarnya "wajar" (nama duplikat, stok kurang, uang bayar kurang). Exception
/// disimpan untuk hal yang benar-benar tidak terduga.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; protected init; }
    public IReadOnlyList<string> Errors { get; protected init; } = [];
    public ErrorKind Kind { get; protected init; } = ErrorKind.Validation;

    /// <summary>Pesan error pertama, untuk ditempel ke ModelState.</summary>
    public string? Error => Errors.Count > 0 ? Errors[0] : null;

    public static ServiceResult Ok() => new() { Succeeded = true };

    public static ServiceResult Fail(string error, ErrorKind kind = ErrorKind.Validation)
        => new() { Succeeded = false, Errors = [error], Kind = kind };

    public static ServiceResult Fail(IEnumerable<string> errors, ErrorKind kind = ErrorKind.Validation)
        => new() { Succeeded = false, Errors = errors.ToList(), Kind = kind };
}

public sealed class ServiceResult<T> : ServiceResult
{
    public T? Value { get; private init; }

    public static ServiceResult<T> Ok(T value) => new() { Succeeded = true, Value = value };

    public new static ServiceResult<T> Fail(string error, ErrorKind kind = ErrorKind.Validation)
        => new() { Succeeded = false, Errors = [error], Kind = kind };

    public new static ServiceResult<T> Fail(IEnumerable<string> errors, ErrorKind kind = ErrorKind.Validation)
        => new() { Succeeded = false, Errors = errors.ToList(), Kind = kind };

    /// <summary>Meneruskan kegagalan dari ServiceResult non-generic tanpa kehilangan Kind.</summary>
    public static ServiceResult<T> From(ServiceResult failed)
        => new() { Succeeded = false, Errors = failed.Errors, Kind = failed.Kind };
}
