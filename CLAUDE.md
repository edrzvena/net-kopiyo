# CLAUDE.md — KopiYo

Aplikasi kasir (POS) cafe KopiYo. ASP.NET Core MVC, **net10.0**, SQL Server Express, EF Core 10.

📖 **Peta lengkap struktur, isi tiap file, dan alur request end-to-end ada di [`flow.md`](./flow.md).**
Baca itu sebelum mengubah apa pun yang menyentuh order, stok, atau role.

🎓 Baru belajar C#/.NET? Mulai dari **[`flow.md` §0 — apa fungsi tiap folder](./flow.md#0-untuk-yang-baru-belajar--apa-fungsi-tiap-folder)**.
Di situ dijelaskan dari nol: entity, DI, interface, Razor, migration, beda DTO vs ViewModel,
plus urutan file yang sebaiknya dibaca lebih dulu.

---

## Perintah

Semua dijalankan dari `MyKopiYo\KopiYo\` (bukan root repo).

```bash
dotnet build
dotnet run                       # https://localhost:7057 · http://localhost:5242

dotnet ef migrations add NamaMigration
dotnet build                     # WAJIB sebelum update — lihat "Jebakan" di bawah
dotnet ef database update
dotnet ef database drop --force  # reset data development

sqlcmd -S ".\SQLEXPRESS" -E -C -I -d db_kopiyo -W -Q "SELECT ..."
```

`sqlcmd` **wajib** pakai `-C` (self-signed cert SQL Express) dan `-I` (database ini punya filtered index yang mensyaratkan `QUOTED_IDENTIFIER ON`).

Akun bawaan: `admin`/`Admin123!` (Admin), `kasir`/`Kasir123!` (Kasir).

Tidak ada test project. Verifikasi dilakukan dengan menjalankan aplikasi lalu memeriksa lewat HTTP + `sqlcmd`.

---

## Bentuk proyek

Satu project, dipisah **per folder**, bukan per assembly.

```
Common/       enum, konstanta, ServiceResult, extension
Models/       13 entity EF
Data/         AppDbContext, 13 IEntityTypeConfiguration, DbInitializer, Migrations
DTOs/         sealed record — kontrak JSON
ViewModels/   class + setter — model untuk .cshtml
Mappings/     extension static entity ↔ VM/DTO (manual, bukan AutoMapper)
Services/     Interfaces/ + implementasi — SEMUA logic bisnis
Controllers/  MVC tipis + Api/ (2 ApiController)
Views/        Razor
```

**Aliran satu arah:** `Controller → Service → DbContext → SQL`.
Controller tidak pernah menyentuh `AppDbContext`. Service tidak pernah menyentuh `HttpContext`/`ModelState`/`TempData`.

**Sengaja TIDAK dipakai** (jangan ditambahkan tanpa alasan kuat): Repository/UnitOfWork generic, AutoMapper, CsvHelper, ASP.NET Core Identity, framework frontend/npm. Paket hanya dua: `Microsoft.EntityFrameworkCore.SqlServer` dan `.Design`, keduanya 10.0.10.

---

## Lima aturan yang tidak boleh dilanggar

### 1. Order adalah dokumen keuangan yang immutable
Order tidak pernah join ke tabel master untuk menampilkan dirinya. Nama produk, harga satuan, nama kasir, persen pajak — semuanya **di-snapshot** ke baris order saat penjualan terjadi.
Jangan menulis struk/laporan yang mengambil nama atau harga lewat `.ThenInclude(i => i.Product)`.

### 2. Semua uang dihitung ulang di server
`CreateOrderDto` **tidak punya field harga**. Client hanya mengirim `productId`, `quantity`, `variantOptionIds`.
Menambahkan field harga ke DTO checkout = membuka celah manipulasi dari DevTools.

### 3. Stok hanya bergerak lewat `IInventoryService`
`IngredientService.UpdateAsync` sengaja tidak bisa mengubah `StockQty`, dan `IngredientFormViewModel` sengaja tidak punya field itu. Setiap perubahan menghasilkan baris `StockMovement` dengan `StockBefore`/`StockAfter`.

### 4. Tidak ada hard delete di mana pun riwayat bisa menjangkau
Hanya `IsActive`. UI menulis "Nonaktifkan", bukan "Hapus".
Pengecualian: `RecipeItem` dan `ProductVariantGroup` (baris relasi murni).
Jangan menambahkan `HasQueryFilter` — pakai parameter `activeOnly` eksplisit di service.

### 5. Role dijaga di controller, bukan di navbar
`[Authorize(Roles = AppConstants.Roles.Admin)]` di **level class**.
- **Admin** — semua CRUD, laporan, stok, void/refund.
- **Kasir** — hanya `/Pos` dan `POST /api/pos/checkout`. Tidak bisa mengubah data apa pun, **tidak bisa void**.

Menyembunyikan link di `_Layout.cshtml` murni kosmetik.
Otorisasi tingkat objek (IDOR) tidak bisa ditangani atribut — lihat `PosApiController.GetReceipt`.

---

## Konvensi kode

| Hal | Aturan |
|---|---|
| **DTO** | `sealed record`, positional. Di-serialize jadi JSON. Tidak boleh ada `SelectList`/tipe MVC |
| **ViewModel** | `class` dengan setter (model binding butuh itu). Boleh `SelectList`, `[Display]`, `[Required]`. Tidak pernah keluar sebagai JSON |
| **Entity** | Jangan di-bind ke view (over-posting), jangan di-serialize ke JSON |
| **Enum** | Disimpan ke DB sebagai **string** (`HasConversion<string>()`) |
| **Uang** | `decimal(18,2)`. Kuantitas bahan `decimal(18,3)`. Persen `decimal(5,2)` |
| **Pembulatan** | `RoundRupiah()` di setiap langkah, simpan nilai bulatnya — agar `Subtotal − Diskon + Service + Pajak == GrandTotal` persis |
| **Waktu** | `IDateTimeProvider.NowWib`/`TodayWib`. **Tidak ada `DateTime.Now` static di service** |
| **Nilai balik service** | `ServiceResult` / `ServiceResult<T>` dengan `ErrorKind`, bukan exception, untuk kegagalan yang wajar |
| **Nullable** | Aktif. Nav reference `= null!;`, koleksi `= [];` |
| **Bahasa** | Semua teks yang dilihat pengguna (validasi, error, label, tombol) **Bahasa Indonesia** |
| **Komentar** | Bahasa Indonesia, dan hanya untuk menjelaskan **kenapa**, bukan apa |

### Pola CRUD (ditetapkan `CategoriesController`)

```
GET  Index    → service.GetPagedAsync/GetAllAsync(activeOnly: false) → View
GET  Create   → View(new FormViewModel())
POST Create   → !ModelState.IsValid → (repopulate SelectList) → View(vm)
                !result.Succeeded   → ModelState.AddModelError + View(vm)
                sukses → TempData["StatusSuccess"] + RedirectToAction(Index)
GET/POST Edit → sama
POST SetActive → TempData + Redirect        ← tidak ada action Delete
```

Pengulangan pola ini di 5 controller **disengaja**. Jangan buat base class `CrudController<T>` generic.

Kalau form punya `SelectList` atau daftar checkbox, **wajib** memanggil `RepopulateFormAsync(vm)` sebelum `return View(vm)` di jalur gagal — data itu tidak ikut ter-post balik.

### Aturan query EF

- `AsNoTracking()` untuk semua pembacaan.
- Projeksi **inline** di dalam `.Select()`. Extension method dari `Mappings/` **tidak bisa** diterjemahkan ke SQL — pakai hanya setelah materialisasi.
- `GroupBy` → proyeksikan ke **tipe anonim** dulu, `ToListAsync()`, baru map ke record. Constructor record positional di dalam `GroupBy.Select()` tidak bisa diterjemahkan EF Core 10.
- `SumAsync(x => (decimal?)x.Field) ?? 0m` — `SUM()` atas nol baris = SQL `NULL`.
- Rentang tanggal **setengah terbuka**: `>= from && < to.AddDays(1)`. Jangan pernah `<= to`.
- `.ToString("N0")` / `ToRupiah()` **tidak boleh** di dalam `.Select()` yang belum materialisasi.
- Transaksi manual **wajib** dibungkus `db.Database.CreateExecutionStrategy().ExecuteAsync(...)` karena `EnableRetryOnFailure()` aktif.

---

## Jebakan (semuanya pernah terjadi di proyek ini)

| Jebakan | Perbaikan |
|---|---|
| `dotnet ef database update --no-build` setelah `migrations add` → *"already up to date"* padahal tabel belum ada | `dotnet build` dulu; `--no-build` memuat assembly lama |
| `new UTF8Encoding(true).GetBytes()` tidak menulis BOM | Tempel `encoding.GetPreamble()` manual — flag itu hanya memengaruhi `GetPreamble()` |
| `GroupBy` → constructor record positional | Tipe anonim dulu, map setelah `ToListAsync()` |
| `[ValidateAntiForgeryToken]` di level class API controller | Memvalidasi semua verb termasuk GET. Andalkan `AutoValidateAntiforgeryToken` global |
| `AuthorizationOptions.FallbackPolicy` / `.RequireAuthorization()` | `MapStaticAssets()` mendaftarkan CSS sebagai endpoint → login screen tanpa CSS. Pakai MVC `AuthorizeFilter` |
| `HasData` berisi `PasswordHasher.HashPassword()` atau `DateTime.Now` | Salt acak → setiap `migrations add` menghasilkan `UpdateData` sia-sia. Seed runtime di `DbInitializer` |
| Lupa `UseAuthentication()` | `IsAuthenticated` selalu false, redirect loop tak berujung |
| Cookie tanpa event `OnRedirectToLogin` untuk `/api` | `fetch()` menerima HTML login → `JSON.parse` gagal `Unexpected token <` |
| Dua FK `Order` → `Users` tanpa konfigurasi eksplisit | Model gagal build |

**Catatan pengujian:** PowerShell `Invoke-WebRequest` menyimpan `-Headers` ke dalam `WebRequestSession` dan mengirimnya ulang di request berikutnya. Saat menguji antiforgery, **wajib pakai session baru** — kalau tidak, hasilnya false negative.

---

## Konfigurasi

`appsettings.json` section `"KopiYo"` — mengubah nilai di sini **tidak** butuh perubahan kode:

```jsonc
{
  "TaxPercent": 10.0,                    // di-snapshot ke tiap order; struk lama tidak berubah
  "ServiceChargePercent": 0.0,
  "BlockSaleOnInsufficientStock": true,  // false = boleh jual sampai stok minus + warning
  "LowStockWarningEnabled": true
}
```

Connection string: `Server=.\SQLEXPRESS;Database=db_kopiyo;Trusted_Connection=True;TrustServerCertificate=True;`
`TrustServerCertificate=True` wajib — SqlClient 4.0+ default `Encrypt=true` dan SQL Express lokal pakai self-signed cert.

---

## Sisa scaffold (aman diabaikan)

`Views/Home/Index.cshtml` tidak pernah dirender — `HomeController.Index` selalu redirect per role.
`Views/Home/Privacy.cshtml`, `Views/Shared/Error.cshtml`, `Models/ErrorViewModel.cs`, `wwwroot/js/site.js`, `wwwroot/css/site.css` juga sisa template `dotnet new mvc`.
