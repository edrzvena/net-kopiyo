# KopiYo — Peta Lengkap Struktur & Alur

Dokumen ini menjelaskan **setiap file di proyek ini: apa isinya, siapa yang memanggilnya, dan ke mana perginya**.
Kalau kamu bertanya "kelas ini dipakai di mana?" atau "kalau mau nambah fitur X, sentuh file apa?", jawabannya ada di sini.

Semua path relatif ke `MyKopiYo\KopiYo\` kecuali disebut lain.

---

## Daftar Isi

0. [**Untuk yang baru belajar — apa fungsi tiap folder?**](#0-untuk-yang-baru-belajar--apa-fungsi-tiap-folder)
1. [Peta 30 detik](#1-peta-30-detik)
2. [Lima aturan arsitektur yang tidak boleh dilanggar](#2-lima-aturan-arsitektur-yang-tidak-boleh-dilanggar)
3. [Lapisan dan tanggung jawabnya](#3-lapisan-dan-tanggung-jawabnya)
4. [Program.cs — titik rakit semuanya](#4-programcs--titik-rakit-semuanya)
5. [Struktur direktori lengkap](#5-struktur-direktori-lengkap)
6. [Model data dan relasinya](#6-model-data-dan-relasinya)
7. [Alur request end-to-end](#7-alur-request-end-to-end)
8. [Matriks role dan rute](#8-matriks-role-dan-rute)
9. [Mau nambah fitur? Sentuh file ini](#9-mau-nambah-fitur-sentuh-file-ini)
10. [Jebakan yang sudah ditemui dan diperbaiki](#10-jebakan-yang-sudah-ditemui-dan-diperbaiki)

---

## 0. Untuk yang baru belajar — apa fungsi tiap folder?

> Bab ini untuk kamu yang masih belajar C#/.NET. Bab-bab setelahnya menganggap kamu sudah paham
> istilah-istilah di sini. **Baca ini dulu.**

### Hal pertama yang perlu tahu: folder di .NET itu cuma kesepakatan

Compiler C# **tidak peduli** kamu menaruh file di folder mana. Secara teknis, seluruh 130+ file
proyek ini bisa ditumpuk di satu folder dan tetap jalan.

Folder ada untuk **manusia**, bukan untuk compiler. Gunanya: kalau ada bug soal harga,
kamu langsung tahu harus buka `Services/OrderService.cs`, bukan menyisir 130 file.

Yang mengikuti folder adalah **namespace** — nama panjang sebuah class:

```csharp
// File: Services/OrderService.cs
namespace KopiYo.Services;        // ← mengikuti nama folder (konvensi, bukan paksaan)
public sealed class OrderService { ... }
```

Kalau file lain mau memakai class itu, dia menulis `using KopiYo.Services;` di atas.
Itulah kenapa nama folder dan namespace selalu dibikin sama — biar tidak bingung.

---

### Analogi: proyek ini seperti cafe beneran

| Folder | Analoginya di cafe | Tugasnya |
|---|---|---|
| `Models/` | **Buku catatan & formulir kosong** | Menentukan *bentuk* data: nota punya kolom apa saja |
| `Data/` | **Lemari arsip + petugas arsip** | Menyimpan & mengambil data dari database |
| `Services/` | **Koki dan manajer** | Yang benar-benar **berpikir**: hitung harga, potong stok, cek aturan |
| `Controllers/` | **Pelayan** | Terima permintaan dari pelanggan, teruskan ke dapur, bawa hasilnya kembali |
| `Views/` | **Tampilan di meja pelanggan** | Halaman HTML yang dilihat orang |
| `ViewModels/` | **Nampan saji** | Wadah khusus untuk membawa data ke meja, ditata rapi |
| `DTOs/` | **Paket kiriman keluar** | Wadah untuk data yang dikirim sebagai JSON ke JavaScript |
| `Mappings/` | **Tukang pindah isi wadah** | Memindahkan isi dari buku catatan ke nampan saji |
| `Common/` | **Kotak perkakas bersama** | Barang kecil yang dipakai semua orang |
| `wwwroot/` | **Barang yang diberikan apa adanya** | CSS, JavaScript, gambar — dikirim mentah ke browser |

Aturan mainnya seperti di cafe sungguhan:
**pelayan tidak memasak**, dan **koki tidak melayani meja**.
Controller tidak boleh menghitung harga; Service tidak boleh tahu soal HTTP.

---

### Penjelasan per folder

#### `Models/` — bentuk data di database

Isinya class C# biasa, tapi punya julukan khusus: **entity**.
Satu class = satu tabel di database. Satu property = satu kolom.

```csharp
public class Category          //  → tabel  Categories
{
    public int Id { get; set; }          // → kolom Id       (int)
    public string Name { get; set; }     // → kolom Name     (nvarchar)
    public bool IsActive { get; set; }   // → kolom IsActive (bit)
}
```

`{ get; set; }` itu namanya **property** — versi rapi dari "variabel yang bisa dibaca dan ditulis".

**Kalau folder ini tidak ada?** Kamu harus menulis SQL mentah (`SELECT Name FROM Categories...`)
di mana-mana, dan salah ketik nama kolom baru ketahuan saat aplikasi jalan, bukan saat compile.

📁 Di proyek ini: 13 entity — `Product`, `Order`, `OrderItem`, `Ingredient`, dst.

---

#### `Data/` — jembatan ke database

Berisi **`AppDbContext`**, yaitu pintu masuk ke database. Ini bagian dari **EF Core**
(Entity Framework Core) — sebuah **ORM**, alat yang menerjemahkan kode C# menjadi SQL otomatis.

```csharp
// Kamu menulis C# begini:
var kopi = await db.Products.Where(p => p.IsActive).ToListAsync();

// EF Core menerjemahkannya jadi SQL begini:
// SELECT * FROM Products WHERE IsActive = 1
```

Isi folder ini:

| Bagian | Fungsinya |
|---|---|
| `AppDbContext.cs` | Daftar tabel (`DbSet`) + aturan global |
| `Configurations/` | Detail tiap tabel: kolom wajib, panjang maksimal, index, relasi antar tabel |
| `DbInitializer.cs` | Mengisi data awal saat aplikasi pertama jalan (user admin, bahan, produk contoh) |
| `Migrations/` (sejajar `Data/`) | **Riwayat perubahan struktur database**, di-generate otomatis |

**Soal Migration** — ini konsep yang wajib dipahami. Kamu **tidak pernah** membuat tabel manual
di SQL Server. Alurnya:

```
1. Kamu ubah/tambah class di Models/
2. Jalankan:  dotnet ef migrations add NamaPerubahan
      → EF membandingkan kode dengan struktur terakhir, lalu membuat file resep perubahan
3. Jalankan:  dotnet ef database update
      → resep itu dijalankan ke database sungguhan
```

Jadi struktur database selalu **mengikuti kode**, bukan sebaliknya.

---

#### `Services/` — tempat semua "otak" aplikasi

Ini folder **terpenting**. Semua aturan bisnis ada di sini:
cara menghitung total belanja, kapan stok dipotong, siapa boleh membatalkan transaksi.

Isinya berpasangan — **interface** dan **implementasi**:

```csharp
// Services/Interfaces/ICategoryService.cs  — JANJI: "aku bisa melakukan ini"
public interface ICategoryService
{
    Task<ServiceResult<int>> CreateAsync(CategoryFormViewModel vm, CancellationToken ct);
}

// Services/CategoryService.cs  — PELAKSANAAN: "begini caranya"
public sealed class CategoryService : ICategoryService
{
    public async Task<ServiceResult<int>> CreateAsync(...) { /* kode sungguhan */ }
}
```

**"Kenapa repot bikin interface? Kan bisa langsung pakai class-nya?"**

Alasan praktisnya: interface itu **daftar isi**. Buka `ICategoryService.cs` dan dalam 10 detik
kamu tahu service ini bisa apa saja, tanpa membaca 150 baris implementasinya.
Bonus: nanti kalau mau bikin unit test, kamu bisa mengganti implementasinya dengan versi palsu.

**Dependency Injection (DI)** — konsep yang kelihatannya ajaib di awal:

```csharp
public class CategoriesController(ICategoryService categories) : Controller
//                                ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//     Controller ini bilang: "aku BUTUH sesuatu yang bisa mengurus kategori"
{
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await categories.GetAllAsync(activeOnly: false, ct));
    //          ^^^^^^^^^^ langsung dipakai, tanpa pernah ditulis  new CategoryService(...)
}
```

Kamu **tidak pernah** menulis `new CategoryService(...)`. .NET yang membuatkannya dan
menyuntikkannya lewat constructor. Supaya .NET tahu class mana yang harus dibuat,
pasangannya didaftarkan sekali di `Services/ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<ICategoryService, CategoryService>();
//                 ↑ kalau ada yang minta ini,  ↑ buatkan yang ini
```

> `AddScoped` = dibuat satu kali per request HTTP, lalu dibuang.
> `AddSingleton` = dibuat sekali saja selama aplikasi hidup (untuk yang tidak menyimpan state).

**Kalau folder ini tidak ada?** Semua logic akan menumpuk di Controller. Controller jadi
500 baris, tidak bisa dipakai ulang, dan tidak bisa dites.

---

#### `Controllers/` — penerima permintaan HTTP

Controller adalah yang **pertama menerima** ketika seseorang membuka URL.
Namanya menentukan URL-nya — ini konvensi MVC, bukan konfigurasi:

```
URL  /Categories/Edit/5
      └────────┘ └──┘ │
      Controller  │   └── parameter id
                  └────── nama method (disebut "action")

→ CategoriesController.Edit(id: 5)
```

Yang mengatur pola ini ada di `Program.cs`:
`"{controller=Home}/{action=Index}/{id?}"` — artinya kalau tidak disebut, defaultnya
`HomeController.Index`, dan `id` opsional (tanda `?`).

Controller di proyek ini sengaja dibuat **tipis** — tugasnya cuma tiga:

```csharp
[HttpPost]
public async Task<IActionResult> Create(CategoryFormViewModel vm, CancellationToken ct)
{
    if (!ModelState.IsValid) return View(vm);              // 1. cek input valid?
    var result = await categories.CreateAsync(vm, ct);      // 2. suruh Service kerja
    if (!result.Succeeded) { ... return View(vm); }         // 3. terjemahkan hasilnya
    return RedirectToAction(nameof(Index));
}
```

Tidak ada perhitungan, tidak ada query database. Itu semua tugas Service.

**Atribut** — tulisan dalam kurung siku di atas class/method:

```csharp
[Authorize(Roles = "Admin")]   // hanya Admin yang boleh masuk
[HttpPost]                     // method ini hanya menerima POST, bukan GET
```

Atribut itu seperti label yang ditempel; .NET membacanya dan bertindak sesuai isinya.

---

#### `Views/` — halaman HTML (Razor)

File `.cshtml` = HTML **campur** C#. Namanya **Razor**. Tanda `@` berarti "mulai kode C#":

```cshtml
@model IReadOnlyList<CategoryListItemViewModel>   ← tipe data yang dikirim Controller

<h1>Kategori</h1>
@foreach (var item in Model)         ← Model = data dari Controller
{
    <tr>
        <td>@item.Name</td>
        <td>@item.DisplayOrder</td>
    </tr>
}
```

**Aturan pencarian file otomatis:** `CategoriesController.Index()` yang memanggil `View()`
akan mencari `Views/Categories/Index.cshtml`. Nama folder = nama controller, nama file = nama action.
Tidak perlu didaftarkan di mana pun.

Beberapa jenis file khusus di sini:

| Pola nama | Artinya |
|---|---|
| `_Layout.cshtml` | Bingkai halaman: navbar, footer. Semua halaman "dibungkus" ini |
| `_ViewStart.cshtml` | Berjalan sebelum semua view — di sini ditetapkan layout defaultnya |
| `_ViewImports.cshtml` | Daftar `@using` bersama, supaya tidak ditulis ulang di tiap file |
| `_NamaApa.cshtml` | Awalan garis bawah = **partial**, potongan yang dipakai berulang |

`<form asp-action="Create">` itu **Tag Helper** — atribut `asp-*` diproses server lalu diubah
jadi HTML biasa. Enaknya: kalau kamu rename action-nya, compiler/IDE bisa bantu.

---

#### `ViewModels/` vs `DTOs/` — kenapa ada DUA folder untuk "wadah data"?

Ini yang paling sering bikin bingung pemula. Jawabannya: **tujuan pengirimannya beda.**

```
ViewModel  →  dikirim ke file .cshtml  →  jadi HTML
DTO        →  dikirim ke JavaScript    →  jadi JSON
Entity     →  dikirim ke database      →  jadi baris tabel
```

Produk yang sama, tiga bentuk berbeda:

```csharp
// Models/Product.cs — ENTITY (buat database)
public class Product {
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }               // form tidak butuh ini
    public ICollection<OrderItem> OrderItems { get; set; } // JSON tidak butuh ini
}

// ViewModels/Products/... — VIEWMODEL (buat halaman HTML)
public class ProductFormViewModel {
    [Required(ErrorMessage = "Nama produk wajib diisi.")]   // pesan error bahasa Indonesia
    public string Name { get; set; } = "";
    public SelectList? Categories { get; set; }             // isi dropdown — cuma buat HTML
}

// DTOs/Pos/CatalogDto.cs — DTO (buat JavaScript)
public sealed record CatalogProductDto(
    int Id, string Name, decimal BasePrice, int CategoryId,
    string? ImageUrl, IReadOnlyList<int> VariantGroupIds);   // data polos, tanpa hiasan
```

**Kenapa tidak pakai `Product` saja untuk semuanya?** Dua alasan nyata:

1. **Kebocoran data.** `User` punya property `PasswordHash`. Kalau entity langsung diubah jadi
   JSON, hash password ikut terkirim ke browser.
2. **Over-posting.** Kalau form langsung terhubung ke entity, penyerang bisa mengirim field
   `Id=1&IsActive=true` yang tidak ada di form dan tetap tersimpan.

**Cara cepat membedakan di proyek ini:**
- `sealed record` + constructor di kurung → **DTO** (buat JSON)
- `class` + `{ get; set; }` → **ViewModel** (buat halaman)

---

#### `Mappings/` — memindahkan isi antar wadah

Karena bentuknya beda-beda, ada yang harus memindahkan isinya. Itu tugas folder ini:

```csharp
public static ProductFormViewModel ToFormViewModel(this Product p) => new() {
    Id = p.Id,
    Name = p.Name,
    BasePrice = p.BasePrice
    // CreatedAt sengaja TIDAK disalin — form tidak membutuhkannya
};
```

Kata `this` di parameter pertama membuatnya jadi **extension method** — bisa dipanggil
seolah-olah method milik class itu sendiri:

```csharp
var vm = product.ToFormViewModel();   // enak dibaca
```

Ada juga arah sebaliknya, `ApplyTo()`, yang menyalin **hanya field yang boleh diubah** dari
form ke entity. Itulah pertahanan terhadap over-posting yang disebut di atas.

---

#### `Common/` — perkakas yang dipakai semua lapisan

Barang-barang kecil yang tidak cocok masuk folder lain karena dipakai di mana-mana:

| File | Isinya | Contoh |
|---|---|---|
| `Enums.cs` | **Enum** — tipe dengan pilihan terbatas | `OrderStatus.Paid`, `PaymentMethod.Cash` |
| `AppConstants.cs` | Nilai tetap, ditulis sekali | `Roles.Admin = "Admin"` |
| `Result.cs` | Bentuk standar hasil operasi | `ServiceResult.Fail("Stok kurang")` |
| `MoneyExtensions.cs` | Format uang | `25000m.ToRupiah()` → `"Rp 25.000"` |

**Enum** itu tipe data yang isinya cuma daftar pilihan tertentu:

```csharp
public enum OrderStatus { Paid = 1, Voided = 2, Refunded = 3 }
```

Enaknya: `order.Status = OrderStatus.Paid` tidak mungkin salah ketik, sedangkan
`order.Status = "Paid"` bisa saja tertulis `"paid"` atau `"Pid"` dan baru ketahuan saat runtime.

---

#### `wwwroot/` — file yang dikirim mentah ke browser

Isinya tidak diproses server sama sekali. Yang ada di sini bisa diakses langsung lewat URL:

```
wwwroot/js/pos.js   →  https://localhost:7057/js/pos.js
wwwroot/css/pos.css →  https://localhost:7057/css/pos.css
```

Perhatikan: `wwwroot` **tidak ikut** dalam URL-nya. Folder `lib/` berisi Bootstrap dan jQuery
yang di-download sekali dan ikut disimpan di repo.

---

### Ringkasan: perjalanan satu klik

Ketika kasir menekan tombol **BAYAR**, ini yang terjadi berurutan:

```
1.  wwwroot/js/pos.js          kumpulkan isi keranjang, kirim POST ke /api/pos/checkout
                                        ↓
2.  Controllers/Api/PosApiController.cs terima permintaan, ambil id user yang login
                                        ↓
3.  DTOs/Pos/CreateOrderDto.cs          JSON diubah jadi object C# (tanpa field harga!)
                                        ↓
4.  Services/OrderService.cs            ★ OTAKNYA: baca harga dari DB, hitung pajak,
                                          buat nomor order, potong stok, simpan
                                        ↓
5.  Data/AppDbContext.cs                terjemahkan jadi SQL, kirim ke SQL Server
                                        ↓
6.  Models/Order.cs + OrderItem.cs      tersimpan jadi baris di tabel
                                        ↓
7.  Mappings/OrderMappings.cs           ubah entity jadi ReceiptDto
                                        ↓
8.  DTOs/Pos/ReceiptDto.cs              dikirim balik sebagai JSON
                                        ↓
9.  wwwroot/js/pos.js                   tampilkan struk di modal
```

Perhatikan **tidak ada lompatan mundur**. Setiap lapisan hanya bicara dengan tetangganya.
Itulah inti dari "arsitektur berlapis".

---

### Urutan membaca kode kalau mau paham proyek ini

Jangan mulai dari `OrderService.cs` — itu yang paling rumit. Mulai dari yang paling sederhana:

| # | File | Yang akan kamu pahami |
|---|---|---|
| 1 | `Models/Category.cs` | Bentuk entity paling sederhana |
| 2 | `Data/Configurations/CategoryConfiguration.cs` | Cara mengatur tabel |
| 3 | `ViewModels/Categories/CategoryViewModels.cs` | Beda entity dan ViewModel |
| 4 | `Services/Interfaces/ICategoryService.cs` | Membaca sebuah interface |
| 5 | `Services/CategoryService.cs` | Isi sebuah service |
| 6 | `Controllers/CategoriesController.cs` | Pola CRUD lengkap (dipakai 5 controller lain) |
| 7 | `Views/Categories/Index.cshtml` + `_Form.cshtml` | Razor & Tag Helper |
| 8 | `Program.cs` | Bagaimana semuanya dirakit |
| 9 | `Services/OrderService.cs` | Yang tersulit — transaksi, snapshot, stok |

Setelah nomor 6, kamu sudah bisa menambah entity baru sendiri dengan menyalin polanya.
Langkah-langkahnya ada di [§9](#9-mau-nambah-fitur-sentuh-file-ini).

### Istilah yang sering muncul di dokumen ini

| Istilah | Artinya |
|---|---|
| **Entity** | Class yang mewakili satu tabel database |
| **ORM** | Alat penerjemah C# ↔ SQL. Di sini: EF Core |
| **Migration** | File resep perubahan struktur database |
| **DI / Dependency Injection** | .NET yang membuatkan object dan menyuntikkannya lewat constructor |
| **Interface** | Daftar kemampuan tanpa isi; "kontrak" |
| **Action** | Satu method di dalam Controller yang menangani satu URL |
| **Razor** | Bahasa template `.cshtml`: HTML campur C# |
| **Partial** | Potongan view yang dipakai berulang, namanya diawali `_` |
| **Tag Helper** | Atribut `asp-*` di HTML yang diproses server |
| **DTO** | Wadah data untuk dikirim sebagai JSON |
| **ViewModel** | Wadah data untuk dikirim ke halaman HTML |
| **Snapshot** | Salinan nilai yang dibekukan saat transaksi terjadi |
| **Transaksi (DB)** | Sekumpulan perubahan yang berhasil semua, atau gagal semua |
| **CRUD** | Create, Read, Update, Delete |
| **async / await** | Cara menunggu operasi lambat (database, jaringan) tanpa membekukan server |
| **CancellationToken** | Tanda "pengguna sudah menutup halaman, hentikan saja" |

---

## 1. Peta 30 detik

```
Browser
  │
  ├── Halaman admin (Razor, form POST biasa)
  │     Controllers/*Controller.cs
  │         └─► Services/I*Service  ──► Data/AppDbContext ──► SQL Server Express
  │         └─◄ ViewModels/          ──► Views/**/*.cshtml
  │
  └── Layar kasir (1 halaman + JavaScript)
        Views/Pos/Index.cshtml + wwwroot/js/pos.js
            └─► fetch() ──► Controllers/Api/PosApiController
                                └─► Services/IOrderService ──► AppDbContext ──► SQL
                                └─◄ DTOs/Pos/ (JSON)
```

Satu project, dipisah **per folder**, bukan per assembly. Tidak ada Repository generic, tidak ada AutoMapper.

**Aliran data selalu satu arah:**

```
Controller  →  Service  →  DbContext  →  Database
   ↑              ↓
ViewModel      Entity → (Mappings) → ViewModel / DTO
```

Controller **tidak pernah** menyentuh `AppDbContext`. Service **tidak pernah** menyentuh `HttpContext`.

---

## 2. Lima aturan arsitektur yang tidak boleh dilanggar

Kalau kamu cuma baca satu bagian dari dokumen ini, baca yang ini.

### Aturan 1 — Order adalah dokumen keuangan yang immutable

Order **tidak pernah** join ke tabel master untuk menampilkan dirinya sendiri.
Tabel master (`Products`, `Users`, `Categories`) = kebenaran **sekarang**.
Tabel order (`Orders`, `OrderItems`, `OrderItemVariants`) = kebenaran **saat itu**.

Karena itu `OrderItem` menyimpan `ProductNameSnapshot`, `CategoryNameSnapshot`, `UnitBasePrice`, `UnitPrice`, `LineTotal`, `VariantDescription` — dan `Order` menyimpan `CashierNameSnapshot`, `TaxPercent`, `ServiceChargePercent`.

> Terbukti dalam pengujian: harga Caffe Latte diubah 25.000 → 39.000, struk lama tetap mencetak 33.000.

**Konsekuensi praktis:** jangan pernah menulis laporan atau struk yang `Include(o => o.Items).ThenInclude(i => i.Product)` untuk mengambil nama/harga. Pakai kolom snapshot.

### Aturan 2 — Semua uang dihitung ulang di server

`CreateOrderDto` **tidak punya field harga sama sekali**. Client hanya mengirim `productId`, `quantity`, `variantOptionIds`.
`OrderService.CreateOrderAsync` membaca `Product.BasePrice` dan `VariantOption.PriceDelta` dari database.

> Terbukti: payload checkout disisipi `"unitPrice":1` — total order tetap dihitung dari harga DB.

**Konsekuensi praktis:** menambahkan field harga ke DTO checkout = membuka celah manipulasi harga dari DevTools.

### Aturan 3 — Stok hanya bergerak lewat `IInventoryService`

`IngredientService.UpdateAsync` **sengaja tidak bisa** mengubah `StockQty`, dan `IngredientFormViewModel` **sengaja tidak punya** field itu.
Setiap perubahan stok menghasilkan baris `StockMovement` dengan `StockBefore` dan `StockAfter`.

**Konsekuensi praktis:** "satu pintu masuk" inilah yang membuat buku besar stok bisa dipercaya. Kalau enam bulan lagi angka stok terlihat salah, ledger-nya bisa diputar ulang.

### Aturan 4 — Tidak ada hard delete di mana pun riwayat bisa menjangkau

Product, Category, VariantGroup, VariantOption, Ingredient, User → hanya `IsActive`.
UI-nya menulis "Nonaktifkan", bukan "Hapus".

Yang **boleh** dihapus beneran cuma dua, karena baris relasi murni yang tidak pernah dirujuk riwayat:
- `RecipeItem`
- `ProductVariantGroup`

Sengaja **tanpa** `HasQueryFilter`. Filter global itu tidak terlihat dan akan ikut menyembunyikan data dari layar admin, lalu kamu berperang dengan `IgnoreQueryFilters()` di mana-mana. Gantinya: service punya parameter `activeOnly` eksplisit — POS kirim `true`, admin kirim `false`.

### Aturan 5 — Role dijaga di controller, bukan di navbar

`[Authorize(Roles = AppConstants.Roles.Admin)]` di **level class**. Menyembunyikan link di `_Layout.cshtml` murni kosmetik.

- **Admin** — semua CRUD master data, laporan, stok, void/refund.
- **Kasir** — hanya `/Pos` dan `POST /api/pos/checkout`. Tidak bisa mengubah data apa pun, tidak bisa void.

Void sengaja Admin-only: "ring penjualan, terima uang tunai, void order, selisihnya masuk kantong" adalah jalur kebocoran kas paling umum di F&B.
Matriksnya gampang diingat: **Kasir membuat, Admin mengoreksi.**

---

## 3. Lapisan dan tanggung jawabnya

| Lapisan | Folder | Boleh | Tidak boleh |
|---|---|---|---|
| **Entity** | `Models/` | Dipetakan EF ke tabel | Di-bind ke view (over-posting), di-serialize ke JSON (bocor `PasswordHash`, cycle) |
| **Konfigurasi** | `Data/Configurations/` | `IEntityTypeConfiguration<T>`, index, delete behavior, `HasData` statis | `HasData` dengan nilai acak/`DateTime.Now` |
| **DbContext** | `Data/AppDbContext.cs` | `DbSet`, convention presisi | `modelBuilder.Entity<X>()` langsung di `OnModelCreating` |
| **Service** | `Services/` | Semua logic bisnis, transaksi, validasi | Menyentuh `HttpContext`, `User`, `TempData`, `ModelState` |
| **DTO** | `DTOs/` | Di-serialize jadi JSON | `SelectList`, `IFormFile`, tipe MVC |
| **ViewModel** | `ViewModels/` | Di-bind `.cshtml`, `SelectList`, `[Display]`, `[Required]` | Keluar sebagai JSON |
| **Mapping** | `Mappings/` | Extension static entity ↔ VM/DTO | Dipanggil di dalam `.Select()` EF (tidak bisa di-translate) |
| **Controller** | `Controllers/` | Validasi `ModelState`, panggil service, terjemahkan hasil | Query EF, logic bisnis, perhitungan uang |

### DTO vs ViewModel — aturan satu baris

- **ViewModel** = *"ada file `.cshtml` yang bind ke dia."* Ditulis sebagai **`class` dengan setter** (model binding butuh constructor tanpa parameter).
- **DTO** = *"dia di-serialize jadi JSON dan menyeberangi kabel."* Ditulis sebagai **`sealed record`**.

Beda sintaks itu saja sudah membuat ketahuan sekilas mana yang mana.

Konsep yang sama, tiga bentuk berbeda:

```csharp
// Models/Product.cs — ENTITY
public class Product {
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal BasePrice { get; set; }
    public DateTime CreatedAt { get; set; }              // tidak relevan buat form
    public ICollection<OrderItem> OrderItems { get; set; } // tidak relevan buat JSON
}

// ViewModels/Products/ProductViewModels.cs — VIEWMODEL
public class ProductFormViewModel {
    [Required(ErrorMessage = "Nama produk wajib diisi.")]
    [Display(Name = "Nama Produk")] public string Name { get; set; } = "";
    public decimal BasePrice { get; set; }
    public SelectList? Categories { get; set; }           // ← ini yang bikin dia BUKAN DTO
    public List<CheckboxItem> VariantGroups { get; set; } = [];
}

// DTOs/Pos/CatalogDto.cs — DTO
public sealed record CatalogProductDto(
    int Id, string Name, decimal BasePrice, int CategoryId,
    string? ImageUrl, IReadOnlyList<int> VariantGroupIds);
```

**Tanda kamu ketuker:**
- `SelectList` muncul di respons JSON → ViewModel bocor ke API.
- `passwordHash` muncul di respons → entity ke-serialize langsung.

---

## 4. `Program.cs` — titik rakit semuanya

Urutannya penting. Dibaca dari atas ke bawah:

### Bagian 1 — Registrasi service

```csharp
AddDbContext<AppDbContext>(UseSqlServer(..., sql => sql.EnableRetryOnFailure()))
Configure<KopiYoSettings>(Configuration.GetSection("KopiYo"))
AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>()   // stateless
AddSingleton(TimeProvider.System)
AddKopiYoServices()                                            // → Services/ServiceCollectionExtensions.cs
```

`EnableRetryOnFailure()` inilah alasan `OrderService` dan `InventoryService` **wajib** membungkus transaksi manualnya dalam `Database.CreateExecutionStrategy().ExecuteAsync(...)`. Tanpa itu: *"The configured execution strategy does not support user-initiated transactions."*

### Bagian 2 — Cookie authentication

```csharp
AddAuthentication(Cookie).AddCookie(options => {
    Cookie.Name       = "KopiYo.Auth"
    LoginPath         = "/Account/Login"
    AccessDeniedPath  = "/Account/AccessDenied"
    ExpireTimeSpan    = 10 jam       // kira-kira satu shift kasir
    SlidingExpiration = true

    // KRITIS untuk layar POS:
    OnRedirectToLogin        → kalau path diawali "/api" balas 401, bukan redirect HTML
    OnRedirectToAccessDenied → kalau path diawali "/api" balas 403, bukan redirect HTML
})
```

Tanpa dua handler itu, cookie yang kedaluwarsa membuat `fetch()` menerima **HTTP 200 berisi HTML halaman login**, lalu `JSON.parse` meledak dengan `Unexpected token <` — error yang sangat menyesatkan.

### Bagian 3 — Antiforgery dan filter global

```csharp
AddAntiforgery(o => o.HeaderName = "RequestVerificationToken")   // supaya fetch() bisa kirim lewat header

Configure<ApiBehaviorOptions>(o =>
    o.InvalidModelStateResponseFactory = PosApiController.MapModelStateErrors)

AddControllersWithViews(options => {
    options.Filters.Add(new AuthorizeFilter());                        // default: semua butuh login
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());  // CSRF di semua non-GET
}).AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
```

- `AuthorizeFilter` global → halaman publik harus opt-out pakai `[AllowAnonymous]`. Lebih sulit lupa daripada menempel `[Authorize]` di setiap controller baru.
- **JANGAN** ganti dengan `AuthorizationOptions.FallbackPolicy` atau `.RequireAuthorization()`. Di .NET 9/10 `MapStaticAssets()` mendaftarkan file statis sebagai *endpoint*, sehingga fallback policy ikut me-redirect `bootstrap.min.css` ke halaman login → login screen tanpa CSS.
- `MapModelStateErrors` menyeragamkan error `[ApiController]` (yang defaultnya `ValidationProblemDetails`) menjadi `ApiErrorDto` — supaya `pos.js` cukup menangani satu bentuk: `{ errors: [...] }`.
- `JsonStringEnumConverter` membuat payload API kebaca manusia: `"Cash"`, bukan `1`.

### Bagian 4 — Pipeline middleware

```csharp
UseExceptionHandler("/Home/Error") + UseHsts()   // produksi saja
UseHttpsRedirection()
UseRouting()
UseAuthentication()     ← template bawaan TIDAK punya ini
UseAuthorization()
MapStaticAssets()
MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets()
```

Urutan `UseRouting → UseAuthentication → UseAuthorization` tidak bisa ditawar. Lupa `UseAuthentication()` menghasilkan gejala yang membingungkan: `User.Identity.IsAuthenticated` selalu `false`, dan login yang berhasil pun tetap dilempar balik ke halaman login terus-menerus.

`.WithStaticAssets()` harus dipertahankan — itu yang membuat `asp-append-version` dan manifest fingerprint aset bekerja.

### Bagian 5 — Seeding

```csharp
using (var scope = app.Services.CreateScope())
    await DbInitializer.SeedAsync(db, hasher, logger, app.Environment.IsDevelopment());
app.Run();
```

---

## 5. Struktur direktori lengkap

Setiap file yang ada di repo, dengan keterangan satu baris. Angka dalam kurung = jumlah file di folder itu.

```
MyKopiYo\                                    ← ROOT REPO (di sini .git berada)
│
├── .git\
├── .idea\                                   konfigurasi JetBrains Rider
├── .gitignore                               ignore .NET lengkap (bin, obj, .vs, .idea, secrets, db lokal)
├── .mcp.json                                MCP server Rider
├── MyKopiYo.sln                             solution, berisi 1 project
├── CLAUDE.md                                aturan operasional untuk sesi coding
├── flow.md                                  ← dokumen ini
│
└── KopiYo\                                  ← PROJECT (semua path di dokumen ini relatif ke sini)
    │
    ├── KopiYo.csproj                        net10.0 · 2 PackageReference · <Using> EF Core global
    ├── Program.cs                           titik rakit: DI, cookie auth, filter global, pipeline, seeding
    ├── appsettings.json                     ConnectionStrings + section "KopiYo" (pajak, service, stok)
    ├── appsettings.Development.json         logging saja
    │
    ├── Properties\
    │   └── launchSettings.json              profil http (5242) & https (7057)
    │
    ├── Common\                              (6) — dipakai SEMUA lapisan, tidak bergantung apa pun
    │   ├── AppConstants.cs                  Roles.Admin/Kasir (const string), OrderPrefix "KY",
    │   │                                    FullNameClaim, DefaultPageSize=20, WibTimeZoneId
    │   ├── ClaimsPrincipalExtensions.cs     GetUserId(), GetUsername(), GetFullName(), IsAdmin()
    │   ├── Enums.cs                         UserRole, PaymentMethod, OrderStatus, StockMovementType,
    │   │                                    UnitOfMeasure, VariantSelectionMode — disimpan sbg string
    │   ├── KopiYoSettings.cs                di-bind dari section "KopiYo"; TaxPercent, ServiceChargePercent,
    │   │                                    BlockSaleOnInsufficientStock, identitas cafe
    │   ├── MoneyExtensions.cs               ToRupiah(), ToQty(), RoundRupiah()
    │   └── Result.cs                        ServiceResult, ServiceResult<T>, ErrorKind{Validation,NotFound,Conflict}
    │
    ├── Models\                              (14) — entity EF, 13 + 1 sisa scaffold
    │   ├── Category.cs                      kategori menu · DisplayOrder menyetir urutan tombol POS
    │   ├── ErrorViewModel.cs                ← sisa scaffold, dipakai Views/Shared/Error.cshtml
    │   ├── Ingredient.cs                    bahan baku · StockQty (18,3) · hanya diubah IInventoryService
    │   ├── Order.cs                         nota · snapshot CashierName/TaxPercent/ServiceChargePercent
    │   │                                    + ReversedAt/ReversedByUserId/ReversalReason (void & refund)
    │   ├── OrderCounter.cs                  PK DateOnly · nomor urut harian anti-race
    │   ├── OrderItem.cs                     baris nota · SEMUA kolom *Snapshot + UnitPrice/LineTotal tersimpan
    │   ├── OrderItemVariant.cs              varian terpilih ter-snapshot (normalized utk analitik)
    │   ├── Product.cs                       menu · BasePrice = harga SEKARANG, bukan yg dipakai struk lama
    │   ├── ProductVariantGroup.cs           join produk↔grup · composite PK · BOLEH hard delete
    │   ├── RecipeItem.cs                    produk X pakai bahan Y sebanyak Z · BOLEH hard delete
    │   ├── StockMovement.cs                 buku besar stok APPEND-ONLY · StockBefore/StockAfter
    │   ├── User.cs                          akun login · 2 koleksi order: OrdersAsCashier & OrdersReversed
    │   ├── VariantGroup.cs                  "Ukuran"/"Suhu"/"Extra" · SelectionMode menyetir UI POS
    │   └── VariantOption.cs                 "S"/"L"/"Ice"/"Extra Shot" · PriceDelta global sekali definisi
    │
    ├── Data\                                (2 + 14 di Configurations\)
    │   ├── AppDbContext.cs                  13 DbSet · OnModelCreating 1 baris · ConfigureConventions presisi
    │   ├── DbInitializer.cs                 MigrateAsync + seed runtime idempotent (user, bahan, produk demo)
    │   │
    │   └── Configurations\                  (14) — satu per entity + konstanta seed
    │       ├── SeedConstants.cs             SeedDate konstan — JANGAN DateTime.Now di HasData
    │       ├── CategoryConfiguration.cs     unique Name · HasData 4 kategori
    │       ├── IngredientConfiguration.cs   unique Name · StockQty/MinStockQty presisi (18,3)
    │       ├── OrderConfiguration.cs        unique OrderNumber · idx (OrderDate,Status) & (CashierId,OrderDate)
    │       │                                ⚠ DUA FK ke Users dikonfigurasi eksplisit di sini
    │       ├── OrderCounterConfiguration.cs PK DateOnly · RowVersion
    │       ├── OrderItemConfiguration.cs    Cascade dari Order · Restrict ke Product · idx (ProductId,OrderId)
    │       ├── OrderItemVariantConfiguration.cs  Cascade dari OrderItem · Restrict ke VariantOption
    │       ├── ProductConfiguration.cs      unique Sku FILTERED · idx (CategoryId,IsActive)
    │       ├── ProductVariantGroupConfiguration.cs  composite PK · Cascade dari Product
    │       ├── RecipeItemConfiguration.cs   unique (ProductId,IngredientId) · Cascade dari Product
    │       ├── StockMovementConfiguration.cs  semua FK Restrict · idx filtered OrderId
    │       ├── UserConfiguration.cs         unique Username · Role sbg string
    │       ├── VariantGroupConfiguration.cs unique Name · HasData 3 grup
    │       └── VariantOptionConfiguration.cs unique (GroupId,Name) · HasData 8 opsi
    │
    ├── Migrations\                          (3) — hasil generate dotnet-ef, SEJAJAR Data\ bukan di dalamnya
    │   ├── 20260808150629_InitialCreate.cs
    │   ├── 20260808150629_InitialCreate.Designer.cs
    │   └── AppDbContextModelSnapshot.cs
    │
    ├── DTOs\                                (4) — sealed record · kontrak JSON · TIDAK boleh ada tipe MVC
    │   ├── Pos\
    │   │   ├── CatalogDto.cs                CatalogDto, CatalogCategoryDto, CatalogProductDto,
    │   │   │                                CatalogVariantGroupDto, CatalogVariantOptionDto
    │   │   │                                → respons GET /api/pos/catalog
    │   │   ├── CreateOrderDto.cs            CreateOrderDto, CreateOrderItemDto
    │   │   │                                → request POST /api/pos/checkout — TANPA FIELD HARGA
    │   │   └── ReceiptDto.cs                ReceiptDto, ReceiptLineDto, OrderResultDto, ApiErrorDto
    │   │                                    → respons checkout + model _ReceiptPartial.cshtml
    │   └── Reports\
    │       └── ReportDtos.cs                SalesSummaryDto, PaymentBreakdownDto, BestSellerDto,
    │                                        CashierSalesDto, DailySalesPointDto, OrderCsvRowDto
    │
    ├── ViewModels\                          (9 file di 9 subfolder) — class + setter · di-bind .cshtml
    │   ├── Account\LoginViewModel.cs
    │   ├── Categories\CategoryViewModels.cs      CategoryListItemViewModel, CategoryFormViewModel
    │   ├── Ingredients\IngredientViewModels.cs   IngredientListItem/Form, StockAdjustment,
    │   │                                         StockMovementListItem — Form TANPA field stok
    │   ├── Orders\OrderViewModels.cs             OrderListItem, OrderDetails, OrderDetailLine,
    │   │                                         OrderStockMovement, ReverseOrder
    │   ├── Products\ProductViewModels.cs         ProductListItem, CheckboxItem, ProductForm,
    │   │                                         ProductRecipe, RecipeLine
    │   ├── Reports\ReportViewModels.cs           Dashboard, DateRange, SalesReport, CashierReport, BestSellers
    │   ├── Shared\PagedList.cs                   IPagedListMetadata + PagedList<T>.CreateAsync()
    │   ├── Users\UserViewModels.cs               UserListItem, UserForm, ResetPassword
    │   └── Variants\VariantViewModels.cs         VariantGroupListItem/Form, VariantOptionListItem/Form
    │
    ├── Mappings\                             (3) — extension static, manual, bukan AutoMapper
    │   ├── CategoryMappings.cs               ToFormViewModel(), ApplyTo()
    │   ├── OrderMappings.cs                  ToReceiptDto(order, settings)
    │   └── ProductMappings.cs                ToFormViewModel(), ApplyTo(vm, entity, now)
    │                                         ⚠ TIDAK BOLEH dipanggil di dalam .Select() EF
    │
    ├── Services\                             (14 + 11) — SEMUA logic bisnis ada di sini
    │   ├── ServiceCollectionExtensions.cs    AddKopiYoServices() — 12 AddScoped + 1 AddSingleton
    │   ├── AuthService.cs                    verifikasi password, susun ClaimsPrincipal, rehash-on-login
    │   ├── CategoryService.cs                CRUD kategori + GetSelectListAsync
    │   ├── CsvExporter.cs                    delimiter ';' + BOM UTF-8 ditempel manual
    │   ├── DateTimeProvider.cs               NowWib / TodayWib — pembungkus TimeProvider
    │   ├── IngredientService.cs              CRUD bahan — SENGAJA tidak bisa ubah StockQty
    │   ├── InventoryService.cs               BuildConsumption, ConsumeForOrder, RestoreForOrder,
    │   │                                     Adjust, GetMovements, GetLowStock · pakai WITH (UPDLOCK)
    │   ├── OrderNumberGenerator.cs           KY-yyyyMMdd-0001 · UPDLOCK + HOLDLOCK
    │   ├── OrderService.cs                   ★ JANTUNG: checkout 12 langkah, struk, riwayat, void/refund
    │   ├── ProductService.cs                 CRUD produk + GetPosCatalogAsync (projeksi INLINE)
    │   ├── RecipeService.cs                  baca & simpan resep (ganti total)
    │   ├── ReportService.cs                  dashboard, ringkasan, deret harian, terlaris, per kasir, export
    │   ├── UserService.cs                    CRUD user + guard "admin aktif terakhir"
    │   ├── VariantService.cs                 CRUD grup varian + opsinya
    │   │
    │   └── Interfaces\                       (11)
    │       ├── IAuthService.cs
    │       ├── ICategoryService.cs
    │       ├── IDateTimeProvider.cs
    │       ├── IIngredientService.cs         berisi IIngredientService + IRecipeService
    │       ├── IInventoryService.cs
    │       ├── IOrderNumberGenerator.cs
    │       ├── IOrderService.cs
    │       ├── IProductService.cs
    │       ├── IReportService.cs             berisi IReportService + ICsvExporter + record CsvColumn<T>
    │       ├── IUserService.cs
    │       └── IVariantService.cs
    │
    ├── Controllers\                          (10 + 2) — TIPIS: validasi, panggil service, terjemahkan hasil
    │   ├── AccountController.cs              Login GET/POST, Logout (POST form), AccessDenied
    │   ├── CategoriesController.cs           ★ POLA DASAR CRUD yang disalin controller lain  [Admin]
    │   ├── HomeController.cs                 Index → redirect per role · Privacy · Error
    │   ├── IngredientsController.cs          CRUD bahan + Adjust + Movements                  [Admin]
    │   ├── OrdersController.cs               Index, Details, Receipt, Reverse (void/refund)   [Admin]
    │   ├── PosController.cs                  Index (layar kasir), Receipt (cek IDOR)          [dua role]
    │   ├── ProductsController.cs             CRUD produk + Recipe                             [Admin]
    │   ├── ReportsController.cs              Dashboard, Sales, ByCashier, BestSellers, Export [Admin]
    │   ├── UsersController.cs                CRUD user + ResetPassword                        [Admin]
    │   ├── VariantGroupsController.cs        CRUD grup + opsi varian                          [Admin]
    │   └── Api\
    │       ├── AdminApiController.cs         api/admin/ingredients/low-stock                  [Admin di class]
    │       └── PosApiController.cs           api/pos/catalog · checkout · orders/{id}/receipt [dua role]
    │                                         + MapModelStateErrors() dipakai Program.cs
    │
    ├── Views\
    │   ├── _ViewImports.cshtml               @using Common/Models/DTOs/ViewModels + TagHelpers
    │   ├── _ViewStart.cshtml                 Layout = "_Layout"
    │   │
    │   ├── Shared\
    │   │   ├── _Layout.cshtml                navbar per role · user chip · logout POST · badge low-stock
    │   │   ├── _Layout.cshtml.css            ← sisa scaffold (CSS isolation)
    │   │   ├── _LoginLayout.cshtml           tanpa navbar — belum ada yang login
    │   │   ├── _Pagination.cshtml            bind IPagedListMetadata · pertahankan query string aktif
    │   │   ├── _ReceiptPartial.cshtml        bind ReceiptDto · SATU partial, DUA pintu masuk
    │   │   ├── _StatusMessage.cshtml         TempData StatusSuccess/StatusError · dipanggil di _Layout
    │   │   ├── _ValidationScriptsPartial.cshtml   ← sisa scaffold
    │   │   └── Error.cshtml                       ← sisa scaffold
    │   │
    │   ├── Account\      Login.cshtml · AccessDenied.cshtml
    │   ├── Categories\   Index · Create · Edit · _Form            (_Form dipakai Create & Edit)
    │   ├── Home\         Index.cshtml ← TIDAK PERNAH DIRENDER · Privacy.cshtml   ← sisa scaffold
    │   ├── Ingredients\  Index · Create · Edit · _Form · Adjust · Movements
    │   ├── Orders\       Index · Details · Reverse   (Receipt me-render ulang Views/Pos/Receipt.cshtml)
    │   ├── Pos\          Index.cshtml (3 kolom + 2 modal + AntiForgeryToken) · Receipt.cshtml
    │   ├── Products\     Index · Create · Edit · _Form · Recipe   (Recipe punya JS tambah/hapus baris)
    │   ├── Reports\      Dashboard · Sales · ByCashier · BestSellers · _RangeFilter
    │   ├── Users\        Index · Create · Edit · ResetPassword
    │   └── VariantGroups\ Index · Create · Edit · _GroupForm · OptionForm
    │                                          (OptionForm melayani CreateOption & EditOption)
    │
    ├── wwwroot\
    │   ├── favicon.ico
    │   ├── css\
    │   │   ├── pos.css                       grid produk, keranjang sticky
    │   │   ├── receipt.css                   @media print — sembunyikan semua kecuali #receipt
    │   │   └── site.css                      ← sisa scaffold
    │   ├── js\
    │   │   ├── pos.js                        ★ SELURUH layar kasir: katalog, cart, modal varian, checkout
    │   │   └── site.js                       ← sisa scaffold (kosong)
    │   └── lib\                              LibMan, tanpa libman.json · tidak di-gitignore
    │       ├── bootstrap\
    │       ├── jquery\
    │       ├── jquery-validation\
    │       └── jquery-validation-unobtrusive\
    │
    ├── bin\                                  ← gitignored
    └── obj\                                  ← gitignored
```

### Ringkasan jumlah

| Folder | File | Isi |
|---|---:|---|
| `Common/` | 6 | enum, konstanta, ServiceResult, extension, settings |
| `Models/` | 14 | 13 entity + 1 sisa scaffold |
| `Data/` | 2 | AppDbContext, DbInitializer |
| `Data/Configurations/` | 14 | 13 config + SeedConstants |
| `Migrations/` | 3 | InitialCreate + snapshot |
| `DTOs/` | 4 | 3 Pos + 1 Reports |
| `ViewModels/` | 9 | satu file per area |
| `Mappings/` | 3 | Category, Order, Product |
| `Services/` | 14 | 13 implementasi + ServiceCollectionExtensions |
| `Services/Interfaces/` | 11 | 12 interface (2 di antaranya menumpang satu file) |
| `Controllers/` | 12 | 10 MVC + 2 API |
| `Views/` | 47 | termasuk 8 partial/layout bersama |
| `wwwroot/` | 6 | di luar `lib/` |

### Catatan struktur

- **Nomor migration `20260808150629`** — timestamp saat `InitialCreate` dibuat. Migration berikutnya akan bertambah di folder yang sama.
- **`Services/Interfaces/IIngredientService.cs` berisi dua interface** (`IIngredientService` + `IRecipeService`), begitu juga `IReportService.cs` (`IReportService` + `ICsvExporter` + `record CsvColumn<T>`). Sengaja disatukan karena selalu dipakai bersama.
- **`ViewModels/` satu file per area**, bukan satu file per kelas. `CategoryViewModels.cs` memuat ListItem + Form sekaligus — memisahkannya hanya menambah file tanpa menambah kejelasan.
- **`wwwroot/lib/` ikut di-commit** dan tidak ada `libman.json`. Kalau perlu update Bootstrap/jQuery, ganti isinya manual atau buat manifest LibMan baru.
- **`Views/Home/Index.cshtml` tidak pernah dirender** — `HomeController.Index` selalu `RedirectToAction` sesuai role. Dibiarkan karena tidak mengganggu.

### Katalog per folder

Bagian berikut menjelaskan tiap file lebih dalam: apa isinya dan **siapa yang memanggilnya**.

### `Common/` — dipakai semua lapisan

| File | Isi | Dipakai di |
|---|---|---|
| `Enums.cs` | `UserRole`, `PaymentMethod`, `OrderStatus`, `StockMovementType`, `UnitOfMeasure`, `VariantSelectionMode` | Semua entity, DTO, ViewModel. Disimpan ke DB **sebagai string** lewat `HasConversion<string>()` supaya kolomnya terbaca `Paid`, bukan `1` |
| `AppConstants.cs` | `Roles.Admin`/`Roles.Kasir` (`const string`), `OrderPrefix = "KY"`, `FullNameClaim`, `DefaultPageSize = 20`, `WibTimeZoneId` | `[Authorize(Roles = ...)]` (atribut hanya menerima konstanta compile-time, jadi tidak bisa pakai enum), `OrderNumberGenerator`, semua controller yang paging |
| `Result.cs` | `ServiceResult`, `ServiceResult<T>`, `ErrorKind {Validation, NotFound, Conflict}` | Nilai balik **semua** method service yang bisa gagal. `ErrorKind` yang dipetakan controller ke status HTTP |
| `MoneyExtensions.cs` | `ToRupiah()`, `ToQty()`, `RoundRupiah()` | View `.cshtml` dan perhitungan `OrderService`. **Jangan panggil di dalam `.Select()` EF** |
| `ClaimsPrincipalExtensions.cs` | `GetUserId()`, `GetUsername()`, `GetFullName()`, `IsAdmin()` | Controller (`User.GetUserId()`) dan `_Layout.cshtml` |
| `KopiYoSettings.cs` | `CafeName/Address/Phone`, `TaxPercent`, `ServiceChargePercent`, `BlockSaleOnInsufficientStock`, `LowStockWarningEnabled` | Di-bind dari section `"KopiYo"` di `appsettings.json`. Di-inject ke `OrderService`, `InventoryService`, `ProductService`, `OrderMappings` |

> `ServiceResult.Kind` adalah alasan controller bisa membedakan 400 dari 409 **tanpa menebak dari isi pesan error**.

### `Models/` — 13 entity + 1 sisa scaffold

| File | Peran | Catatan penting |
|---|---|---|
| `User.cs` | Akun login | `PasswordHash` diisi `PasswordHasher<User>`. Punya **dua** koleksi order: `OrdersAsCashier` dan `OrdersReversed` |
| `Category.cs` | Kategori menu | `DisplayOrder` menentukan urutan tombol di layar kasir |
| `Product.cs` | Menu | `BasePrice` = harga **sekarang**, bukan yang dipakai struk lama |
| `VariantGroup.cs` | "Ukuran", "Suhu", "Extra" | `SelectionMode` menyetir UI POS: `Single` → radio, `Multiple` → checkbox. `IsRequired` → wajib dipilih |
| `VariantOption.cs` | "S"/"M"/"L", "Hot"/"Ice", "Extra Shot" | `PriceDelta` didefinisikan **sekali secara global**, bukan diduplikasi di 40 produk |
| `ProductVariantGroup.cs` | Join produk ↔ grup varian | Composite PK. Salah satu dari dua entity yang boleh hard delete |
| `Order.cs` | Nota penjualan | Kolom snapshot: `CashierNameSnapshot`, `TaxPercent`, `ServiceChargePercent`. Field pembalikan: `ReversedAt`, `ReversedByUserId`, `ReversalReason` (dipakai bersama void & refund) |
| `OrderItem.cs` | Baris nota | Semua kolom `*Snapshot` + `UnitPrice`/`LineTotal` yang **disimpan**, bukan dihitung ulang saat baca |
| `OrderItemVariant.cs` | Varian yang dipilih, ter-snapshot | Ada **selain** `OrderItem.VariantDescription`. Polanya: normalized untuk analitik, denormalized untuk tampilan |
| `OrderCounter.cs` | Nomor urut harian | PK `DateOnly` → kolom SQL `date`. Ada supaya nomor order tidak balapan |
| `Ingredient.cs` | Bahan baku | `StockQty` presisi **(18,3)**, uang tetap (18,2). Hanya boleh diubah lewat `IInventoryService` |
| `RecipeItem.cs` | Produk X pakai bahan Y sebanyak Z | Boleh hard delete |
| `StockMovement.cs` | Buku besar stok, **append-only** | `Quantity` selalu positif; arah dari `MovementType`. `StockBefore`/`StockAfter` = fitur utamanya |
| `ErrorViewModel.cs` | Sisa scaffold, dipakai `Views/Shared/Error.cshtml` | Satu-satunya file di `Models/` yang bukan entity |

### `Data/` — persistence

| File | Peran |
|---|---|
| `AppDbContext.cs` | 13 `DbSet`. `OnModelCreating` isinya **satu baris** `ApplyConfigurationsFromAssembly`. `ConfigureConventions` menetapkan default `decimal(18,2)` dan `string` max 200 |
| `Configurations/SeedConstants.cs` | `SeedDate` konstan untuk semua `HasData` |
| `Configurations/*Configuration.cs` (13 file) | Satu per entity: key, max length, presisi, index, delete behavior, seed statis |
| `DbInitializer.cs` | `MigrateAsync()` + seed runtime idempotent: user `admin`/`kasir`, 8 bahan baku, 10 produk demo (khusus Development) |
| `Migrations/` | `InitialCreate` + snapshot model |

**Kenapa seeding dipecah dua tempat:**

| Cara | Untuk apa | Kenapa |
|---|---|---|
| `HasData` di configuration | 4 Category, 3 VariantGroup, 8 VariantOption | Nilai statis dengan Id tetap, aman masuk migration |
| `DbInitializer` runtime | User, bahan, produk demo | `PasswordHasher` menghasilkan salt **acak setiap dipanggil**, dan `DateTime.Now` jelas berubah. Kalau masuk `HasData`, setiap `dotnet ef migrations add` menghasilkan migration `UpdateData` sia-sia |

**Delete behavior:**

| Relasi | Behavior | Alasan |
|---|---|---|
| Order → OrderItem | Cascade | Item tidak punya kehidupan di luar order |
| OrderItem → OrderItemVariant | Cascade | Sama |
| Product → RecipeItem | Cascade | Resep dimiliki produknya |
| Product → ProductVariantGroup | Cascade | Baris link |
| **Semua sisanya** | **Restrict** | Apa pun yang menyentuh riwayat |

⚠️ `Order` punya **dua FK ke `Users`** (`CashierId`, `ReversedByUserId`). EF tidak bisa menebak navigasi baliknya — keduanya **wajib** dikonfigurasi eksplisit di `OrderConfiguration.cs`, kalau tidak model gagal dibangun dengan pesan yang tidak membantu.

### `DTOs/` — kontrak JSON

| File | Isi | Dikirim/diterima oleh |
|---|---|---|
| `Pos/CatalogDto.cs` | `CatalogDto`, `CatalogCategoryDto`, `CatalogProductDto`, `CatalogVariantGroupDto`, `CatalogVariantOptionDto` | Respons `GET /api/pos/catalog` |
| `Pos/CreateOrderDto.cs` | `CreateOrderDto`, `CreateOrderItemDto` | Request `POST /api/pos/checkout` — **tanpa field harga** |
| `Pos/ReceiptDto.cs` | `ReceiptDto`, `ReceiptLineDto`, `OrderResultDto`, `ApiErrorDto` | Respons checkout & struk. Juga dipakai `_ReceiptPartial.cshtml` |
| `Reports/ReportDtos.cs` | `SalesSummaryDto`, `PaymentBreakdownDto`, `BestSellerDto`, `CashierSalesDto`, `DailySalesPointDto`, `OrderCsvRowDto` | `IReportService`, dipakai `ReportViewModels` dan export CSV |

`ReceiptDto` dipakai di **dua tempat sekaligus**: sebagai JSON (respons checkout) dan sebagai model Razor (`_ReceiptPartial.cshtml`). Itu pengecualian yang disengaja — isinya murni data, tidak ada tipe MVC di dalamnya.

`GET /api/pos/catalog` mengirim `variantGroups` **sekali secara datar**, produk hanya merujuk lewat `variantGroupIds`. Untuk 40 produk × 3 grup, itu beda antara payload 4 KB dan 60 KB — dan membuat modal varian di JS jadi lookup, bukan pencarian.

### `ViewModels/` — model untuk Razor

| File | Kelas | Dipakai view |
|---|---|---|
| `Shared/PagedList.cs` | `IPagedListMetadata`, `PagedList<T>` | `_Pagination.cshtml` (lewat interface non-generic-nya) |
| `Account/LoginViewModel.cs` | `LoginViewModel` | `Account/Login.cshtml` |
| `Categories/CategoryViewModels.cs` | `CategoryListItemViewModel`, `CategoryFormViewModel` | `Categories/Index|Create|Edit|_Form` |
| `Variants/VariantViewModels.cs` | `VariantGroupListItemViewModel`, `VariantOptionListItemViewModel`, `VariantGroupFormViewModel`, `VariantOptionFormViewModel` | `VariantGroups/*` |
| `Products/ProductViewModels.cs` | `ProductListItemViewModel`, `CheckboxItem`, `ProductFormViewModel`, `ProductRecipeViewModel`, `RecipeLineViewModel` | `Products/*` |
| `Users/UserViewModels.cs` | `UserListItemViewModel`, `UserFormViewModel`, `ResetPasswordViewModel` | `Users/*` |
| `Ingredients/IngredientViewModels.cs` | `IngredientListItemViewModel`, `IngredientFormViewModel`, `StockAdjustmentViewModel`, `StockMovementListItemViewModel` | `Ingredients/*` |
| `Orders/OrderViewModels.cs` | `OrderListItemViewModel`, `OrderDetailsViewModel`, `OrderDetailLineViewModel`, `OrderStockMovementViewModel`, `ReverseOrderViewModel` | `Orders/*` |
| `Reports/ReportViewModels.cs` | `DashboardViewModel`, `DateRangeViewModel`, `SalesReportViewModel`, `CashierReportViewModel`, `BestSellersViewModel` | `Reports/*` |

`PagedList<T>.CreateAsync(query, page, pageSize, ct)` menjalankan `CountAsync` + `Skip`/`Take` di satu tempat — supaya tidak ada service yang lupa `Skip`-nya (gejalanya: halaman 2 isinya sama dengan halaman 1).

`_Pagination.cshtml` bind ke `IPagedListMetadata`, bukan ke `PagedList<T>`, supaya satu partial melayani semua tipe.

### `Mappings/` — mapper manual

| File | Method | Dipanggil dari |
|---|---|---|
| `CategoryMappings.cs` | `ToFormViewModel()`, `ApplyTo()` | `CategoryService` |
| `ProductMappings.cs` | `ToFormViewModel()`, `ApplyTo(vm, entity, now)` | `ProductService` |
| `OrderMappings.cs` | `ToReceiptDto(order, settings)` | `OrderService.CreateOrderAsync` dan `GetReceiptAsync` |

**Method `ApplyTo` adalah pertahanan over-posting.** Hanya field yang boleh diubah lewat form yang di-assign — `Id`, `CreatedAt`, dan relasi tidak pernah disentuh, jadi tidak ada gunanya penyerang menambahkan field itu ke request.

⚠️ **Extension method pada entity tidak bisa dipakai di dalam `.Select()` EF** — tidak bisa diterjemahkan ke SQL. Pakai setelah data ter-materialisasi (`FirstOrDefaultAsync`, `ToListAsync`), atau tulis projeksinya inline di query. `ProductService.GetPosCatalogAsync` sengaja menulis projeksi inline karena itu jalur baca terpanas.

**Kenapa manual, bukan AutoMapper:** `Ctrl+Click` di `ToReceiptDto()` langsung memperlihatkan pemetaannya. Di AutoMapper, `Ctrl+Click` tidak menunjukkan apa-apa, property yang di-rename diam-diam ter-map jadi `null`, dan kegagalannya muncul saat runtime di produksi, bukan saat compile.

### `Services/` — semua logic bisnis

Semua `AddScoped` lewat `ServiceCollectionExtensions.AddKopiYoServices()`, kecuali `ICsvExporter` yang singleton (stateless).

| Interface | Implementasi | Tanggung jawab | Dipanggil dari |
|---|---|---|---|
| `IDateTimeProvider` | `DateTimeProvider` | `NowWib`, `TodayWib`. Membungkus `TimeProvider` | Semua service yang butuh waktu — **tidak ada `DateTime.Now` static di service mana pun** |
| `IAuthService` | `AuthService` | Verifikasi password, susun `ClaimsPrincipal`, rehash-on-login | `AccountController` |
| `ICategoryService` | `CategoryService` | CRUD kategori + `GetSelectListAsync` | `CategoriesController`, `ProductsController` |
| `IVariantService` | `VariantService` | CRUD grup varian + opsinya | `VariantGroupsController` |
| `IProductService` | `ProductService` | CRUD produk, pasang grup varian, **`GetPosCatalogAsync`** | `ProductsController`, `PosApiController` |
| `IUserService` | `UserService` | CRUD user, reset password, guard admin terakhir | `UsersController` |
| `IIngredientService` | `IngredientService` | CRUD bahan — **tanpa** kemampuan ubah stok | `IngredientsController` |
| `IRecipeService` | `RecipeService` | Baca & simpan resep (ganti total) | `ProductsController` |
| `IInventoryService` | `InventoryService` | `BuildConsumptionAsync`, `ConsumeForOrderAsync`, `RestoreForOrderAsync`, `AdjustAsync`, `GetMovementsAsync`, `GetLowStockAsync` | `OrderService`, `IngredientsController`, `AdminApiController` |
| `IOrderNumberGenerator` | `OrderNumberGenerator` | `KY-yyyyMMdd-0001` dengan row lock | `OrderService` saja |
| `IOrderService` | `OrderService` | Checkout, struk, riwayat, void/refund | `PosApiController`, `PosController`, `OrdersController` |
| `IReportService` | `ReportService` | Dashboard, ringkasan, deret harian, terlaris, per kasir, data export | `ReportsController` |
| `ICsvExporter` | `CsvExporter` | Serialisasi CSV generik | `ReportsController` |

Interface `ICsvExporter` dan record `CsvColumn<T>` didefinisikan di `Services/Interfaces/IReportService.cs` — sengaja disatukan karena hanya dipakai bersama laporan.

#### `OrderService.CreateOrderAsync` — jantung aplikasi

Urutan lengkapnya, semuanya dalam **satu transaksi**:

```
 0. Validasi bentuk (keranjang kosong, qty 1-999, diskon 0-100) — sebelum sentuh DB
 1. CreateExecutionStrategy().ExecuteAsync → BeginTransactionAsync(ReadCommitted)
 2. Load Product (+Category +ProductVariantGroups) dan VariantOption (+VariantGroup) — 2 query, tracked
 3. Validasi keberadaan + IsActive
 4. Per baris: ValidateVariants()
      • grup yang dipilih harus terpasang di produk itu
      • grup Single maksimal satu pilihan
      • grup IsRequired wajib terisi
 5. HARGA dari DB: unitPrice = BasePrice + Σ PriceDelta; isi semua kolom snapshot
 6. Total order, RoundRupiah() di setiap langkah, nilai bulat yang disimpan
 7. Pembayaran: Cash → tolak kalau kurang, hitung kembalian; QRIS/Debit → pas, kembalian 0
 8. Snapshot kasir + timestamp WIB + Status = Paid
 9. OrderNumber ← IOrderNumberGenerator.NextAsync()      (di dalam transaksi, row-locked)
10. IInventoryService.BuildConsumptionAsync → ConsumeForOrderAsync
       gagal → ServiceResult Conflict → controller balas 409, SELURUHNYA rollback
11. SATU SaveChangesAsync untuk order + item + varian + stok + movement
12. CommitAsync → return OrderResultDto (struk + warnings)
```

Return lebih awal dari dalam delegate membuat `await using var tx` ter-dispose tanpa commit → **rollback otomatis**. Tidak perlu `RollbackAsync` eksplisit di setiap cabang gagal.

Pembulatan memakai `Math.Round(v, 0, MidpointRounding.AwayFromZero)` dan **nilai bulatnya yang disimpan**, sehingga `Subtotal − Diskon + Service + Pajak == GrandTotal` persis. Kalau tidak, angka di struk bisa meleset 1 rupiah dari total tersimpan.

#### `OrderNumberGenerator` — kenapa serumit itu

```csharp
db.OrderCounters.FromSql($"""
    SELECT * FROM OrderCounters WITH (UPDLOCK, HOLDLOCK)
    WHERE BusinessDate = {businessDate}
    """)
```

- **`UPDLOCK`** — ambil update-lock saat **membaca**. Tanpa ini dua transaksi sama-sama membaca `LastSequence = 7` lalu sama-sama menulis 8.
- **`HOLDLOCK`** — mengunci **range**-nya, bukan cuma baris yang ada. Ini yang membuat cabang "insert kalau baris tanggal ini belum ada" juga aman.

Lock bertahan sampai transaksi milik pemanggil di-commit, jadi checkout bersamaan **menunggu**, bukan balapan. Unique index di `Orders.OrderNumber` adalah jaring pengaman terakhir.

`BusinessDate` diambil dari **jam WIB**, bukan UTC — penjualan jam 00:30 WIB masuk buku hari ini.

#### `InventoryService` — stok kurang

`ConsumeForOrderAsync` juga memakai `WITH (UPDLOCK)` saat membaca `Ingredients`. Tanpa itu dua checkout bersamaan sama-sama membaca stok 5, sama-sama mengurangi 5, dan stok berakhir di −5 padahal penjualan kedua seharusnya ditolak.

Perilaku saat stok kurang dikendalikan `KopiYoSettings.BlockSaleOnInsufficientStock`:

| Nilai | Perilaku |
|---|---|
| `true` (default) | `ServiceResult` `Conflict` → **HTTP 409**, seluruh transaksi rollback: tidak ada order, stok tidak berubah, nomor order tidak terbuang |
| `false` | Penjualan lanjut ke stok minus, pesan masuk `OrderResultDto.Warnings[]` dan tampil sebagai toast kuning di POS |

Default-nya `true` karena stok minus diam-diam merusak semua angka turunan (nilai persediaan, reorder point, COGS) dan baru ketahuan saat tutup buku — tipe kegagalan paling jelek.

#### `ReportService` — dua aturan wajib

**1. Rentang tanggal setengah terbuka.**

```csharp
private static (DateTime From, DateTime To) Range(DateOnly from, DateOnly to)
    => (from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue));
// selalu:  o.OrderDate >= From && o.OrderDate < To
```

Memakai `<= tanggalAkhir` diam-diam membuang **semua** penjualan setelah jam 00:00:00.000 di hari terakhir — yaitu hampir seluruh penjualan hari itu.

**2. `SumAsync` harus di-cast ke nullable.**

```csharp
await query.SumAsync(o => (decimal?)o.GrandTotal, ct) ?? 0m
```

`SUM()` atas nol baris mengembalikan SQL `NULL`, dan memetakan `NULL` ke `decimal` non-nullable melempar `InvalidOperationException`. Tanpa cast, dashboard error 500 di pagi pertama yang belum ada penjualan.

**Pengelompokan selalu ke kolom snapshot**, bukan join ke tabel master:

```csharp
.GroupBy(oi => new { oi.ProductId, oi.ProductNameSnapshot })   // best seller
.GroupBy(o  => new { o.CashierId, o.CashierNameSnapshot })     // per kasir
```

Konsekuensinya: produk yang pernah diganti nama muncul sebagai dua baris terpisah. Itu **benar secara historis** — nama itulah yang tercetak di struk saat itu.

### `Controllers/` — tipis, tanpa logic

| Controller | Rute | Authorize | Service yang dipakai |
|---|---|---|---|
| `AccountController` | `/Account/Login|Logout|AccessDenied` | `[AllowAnonymous]`, Logout `[Authorize]` | `IAuthService` |
| `HomeController` | `/`, `/Home/Privacy|Error` | `[Authorize]` di Index, sisanya `[AllowAnonymous]` | — |
| `PosController` | `/Pos`, `/Pos/Receipt/{id}` | `[Authorize]` (dua role) | `IOrderService` |
| `CategoriesController` | `/Categories/...` | **`[Authorize(Roles = Admin)]`** | `ICategoryService` |
| `ProductsController` | `/Products/...` + `/Products/Recipe/{id}` | **Admin** | `IProductService`, `ICategoryService`, `IRecipeService` |
| `VariantGroupsController` | `/VariantGroups/...` | **Admin** | `IVariantService` |
| `UsersController` | `/Users/...` | **Admin** | `IUserService` |
| `IngredientsController` | `/Ingredients/...` + `/Adjust` + `/Movements` | **Admin** | `IIngredientService`, `IInventoryService` |
| `OrdersController` | `/Orders/...` + `/Reverse` | **Admin** | `IOrderService` |
| `ReportsController` | `/Reports/...` + `/Export*Csv` | **Admin** | `IReportService`, `ICsvExporter`, `IDateTimeProvider` |
| `Api/PosApiController` | `api/pos/catalog|checkout|orders/{id}/receipt` | `[Authorize]` (dua role) | `IProductService`, `IOrderService` |
| `Api/AdminApiController` | `api/admin/ingredients/low-stock` | **Admin di level class** | `IInventoryService` |

**Pola CRUD yang diulang di 5 controller** (ditetapkan oleh `CategoriesController`, yang paling sederhana):

```csharp
GET  Index   → service.GetPagedAsync/GetAllAsync(activeOnly: false)  → View
GET  Create  → View(new FormViewModel())
POST Create  → !ModelState.IsValid → View(vm)
               !result.Succeeded   → ModelState.AddModelError + View(vm)
               sukses → TempData["StatusSuccess"] + RedirectToAction(Index)   ← PRG
GET  Edit    → service.GetForEditAsync → null ? NotFound : View
POST Edit    → sama seperti Create
POST SetActive → TempData + RedirectToAction(Index)   ← tidak ada Delete
```

Pengulangan ini **disengaja**. Base class generic `CrudController<T>` terlihat pintar, tapi begitu satu entity butuh perilaku sedikit berbeda, semuanya berantakan.

**Otorisasi tingkat objek (IDOR)** tidak bisa ditangani atribut. Ada di dua tempat:

```csharp
// PosApiController.GetReceipt dan PosController.Receipt
var ownerId = await orders.GetCashierIdAsync(id, ct);
if (ownerId is null) return NotFound();
if (!User.IsAdmin() && ownerId != User.GetUserId()) return Forbid();
```

Tanpa ini, kasir Budi bisa membaca struk kasir Ani cukup dengan menebak id-nya.

### `Views/` — Razor

| Folder | File | Catatan |
|---|---|---|
| root | `_ViewImports.cshtml` | `@using KopiYo.Common/Models/DTOs/ViewModels` + TagHelpers |
| | `_ViewStart.cshtml` | `Layout = "_Layout"` |
| `Shared/` | `_Layout.cshtml` | Navbar per role, user chip, logout **POST form**, badge low-stock (khusus Admin) |
| | `_LoginLayout.cshtml` | Tanpa navbar — belum ada yang login |
| | `_StatusMessage.cshtml` | Baca `TempData["StatusSuccess"]`/`["StatusError"]`. Dipanggil sekali di `_Layout`, jadi semua halaman dapat gratis |
| | `_Pagination.cshtml` | Bind ke `IPagedListMetadata`. Mempertahankan semua query string aktif saat pindah halaman |
| | `_ReceiptPartial.cshtml` | Bind ke `ReceiptDto`. **Satu partial, dua pintu masuk** |
| | `Error.cshtml`, `_ValidationScriptsPartial.cshtml`, `_Layout.cshtml.css` | Sisa scaffold |
| `Account/` | `Login.cshtml`, `AccessDenied.cshtml` | Login pakai `_LoginLayout` |
| `Pos/` | `Index.cshtml` | 3 kolom + 2 modal + `@Html.AntiForgeryToken()` |
| | `Receipt.cshtml` | Cetak ulang, render `_ReceiptPartial` |
| `Categories/` | `Index`, `Create`, `Edit`, `_Form` | `_Form` dipakai bersama Create & Edit |
| `Products/` | `Index`, `Create`, `Edit`, `_Form`, `Recipe` | `Recipe` punya JS untuk tambah/hapus baris |
| `VariantGroups/` | `Index`, `Create`, `Edit`, `_GroupForm`, `OptionForm` | `OptionForm` melayani CreateOption & EditOption |
| `Users/` | `Index`, `Create`, `Edit`, `ResetPassword` | Password diubah di layar terpisah |
| `Ingredients/` | `Index`, `Create`, `Edit`, `_Form`, `Adjust`, `Movements` | `_Form` tidak punya input stok |
| `Orders/` | `Index`, `Details`, `Reverse` | `Receipt` me-render ulang `~/Views/Pos/Receipt.cshtml` |
| `Reports/` | `Dashboard`, `Sales`, `ByCashier`, `BestSellers`, `_RangeFilter` | Bar chart pakai `div`, tanpa library chart |
| `Home/` | `Index.cshtml`, `Privacy.cshtml` | **`Index.cshtml` tidak pernah dirender** — `HomeController.Index` selalu redirect per role. Sisa scaffold |

### `wwwroot/`

| File | Peran |
|---|---|
| `js/pos.js` | Seluruh layar kasir. Baris pertamanya komentar: *"semua angka di file ini HANYA untuk tampilan"* |
| `css/pos.css` | Grid produk, keranjang sticky |
| `css/receipt.css` | `@media print` — sembunyikan semua kecuali `#receipt` |
| `js/site.js`, `css/site.css` | Sisa scaffold |
| `lib/` | Bootstrap 5, jQuery, jquery-validation (dari LibMan, tanpa `libman.json`) |

---

## 6. Model data dan relasinya

```
Category ──1:N──► Product ──1:N──► ProductVariantGroup ──N:1──► VariantGroup
                    │                                                │
                    ├──1:N──► RecipeItem ──N:1──► Ingredient        1:N
                    │                                 │              ▼
                    └──1:N──► OrderItem              1:N       VariantOption
                                 │                    │              │
                                 │                    ▼             1:N
                                 │              StockMovement         │
                                 │                    ▲               │
                                 └──1:N──► OrderItemVariant ──────────┘
                                              │
Order ──1:N──► OrderItem                      │
  │                                           │
  ├──1:N──► StockMovement                     │
  ├──N:1──► User (Cashier)                    │
  └──N:1──► User (ReversedBy, nullable)       │

OrderCounter — berdiri sendiri, PK = DateOnly
```

### Index

| Tabel | Index | Jenis |
|---|---|---|
| Users | `Username` | UNIQUE |
| Categories | `Name` | UNIQUE |
| Products | `Sku` | UNIQUE **filtered** `WHERE [Sku] IS NOT NULL` |
| Products | `CategoryId, IsActive` | biasa — query katalog POS |
| VariantGroups | `Name` | UNIQUE |
| VariantOptions | `VariantGroupId, Name` | UNIQUE |
| Orders | `OrderNumber` | UNIQUE |
| Orders | `OrderDate, Status` | biasa — dipakai semua laporan |
| Orders | `CashierId, OrderDate` | biasa — laporan per kasir |
| OrderItems | `ProductId, OrderId` | biasa — pengelompokan best seller |
| Ingredients | `Name` | UNIQUE |
| RecipeItems | `ProductId, IngredientId` | UNIQUE |
| StockMovements | `IngredientId, CreatedAt` | biasa |
| StockMovements | `OrderId` | filtered `WHERE [OrderId] IS NOT NULL` |

> Filter di index `Sku` itu wajib: tanpa `HasFilter`, SQL Server menganggap banyak `NULL` sebagai duplikat, sehingga hanya boleh ada **satu** produk tanpa SKU di seluruh tabel.

### Presisi decimal

Ditetapkan sekali di `AppDbContext.ConfigureConventions`:

```csharp
configurationBuilder.Properties<decimal>().HavePrecision(18, 2);   // default uang
configurationBuilder.Properties<string>().HaveMaxLength(200);      // cegah nvarchar(max)
```

Yang menimpa default:
- Kuantitas bahan → `HasPrecision(18, 3)` (resep bisa 7,5 g)
- Persentase → `HasPrecision(5, 2)` (cukup untuk 0,00–100,00)

---

## 7. Alur request end-to-end

### 7.1 Login

```
GET /Pos  (belum login)
  → AuthorizeFilter global menolak
  → cookie middleware redirect 302 → /Account/Login?ReturnUrl=%2FPos

POST /Account/Login  {Username, Password, __RequestVerificationToken}
  → AccountController.Login
      → IAuthService.ValidateCredentialsAsync
          → db.Users.FirstOrDefault(Username)
          → hasher.VerifyHashedPassword
              Failed            → Fail("Username atau password salah.")   ← pesan SAMA untuk
              SuccessRehashNeeded → hash ulang + SaveChanges                 user tidak ada
              !IsActive         → Fail("Akun ini sudah dinonaktifkan.")      dan password salah
          → BuildPrincipal: NameIdentifier, Name, FullName, ClaimTypes.Role
      → HttpContext.SignInAsync(...)
      → LocalRedirect(returnUrl) atau RedirectToAction(Home.Index)
                                    ↑ LocalRedirect = pertahanan open-redirect

GET /  → HomeController.Index
           User.IsAdmin() ? → /Reports/Dashboard
                           : → /Pos
```

`ClaimTypes.Role` adalah yang dibaca `[Authorize(Roles = ...)]` dan `User.IsInRole()`. Salah tipe claim = role tidak pernah cocok.

### 7.2 CRUD admin (contoh: tambah produk)

```
GET /Products/Create
  → ProductsController.Create
      → IProductService.BuildCreateFormAsync
          → RepopulateFormAsync: isi SelectList kategori + daftar CheckboxItem grup varian
  → View(vm)  →  Views/Products/Create.cshtml  →  partial _Form.cshtml

POST /Products/Create
  → AutoValidateAntiforgeryToken memeriksa token   ← global filter
  → ModelState tidak valid?
       → RepopulateFormAsync(vm)   ← WAJIB. SelectList & checkbox tidak ikut ter-post balik;
       → View(vm)                     lupa ini = dropdown kosong saat validasi gagal
  → IProductService.CreateAsync
      → ValidateAsync: kategori ada? SKU unik? grup varian ada?
      → new Product + vm.ApplyTo(product, now)      ← hanya field yang boleh diubah
      → tambah ProductVariantGroup per grup terpilih
      → SaveChangesAsync
  → TempData["StatusSuccess"] + RedirectToAction(Index)     ← PRG
  → _StatusMessage.cshtml menampilkannya di halaman berikutnya
```

### 7.3 Checkout kasir — alur terpenting

```
GET /Pos
  → PosController.Index → Views/Pos/Index.cshtml
  → @Html.AntiForgeryToken() menaruh hidden input di halaman

pos.js  init()
  → GET /api/pos/catalog
      → PosApiController.GetCatalog
          → IProductService.GetPosCatalogAsync
              → 3 query (categories, products, variantGroups) — projeksi INLINE, AsNoTracking
              → CatalogDto (+ TaxPercent & ServiceChargePercent dari KopiYoSettings)
  → simpan di variabel JS, render kategori + grid produk

Klik produk
  → punya grup varian?  ya → modal: radio untuk Single, checkbox untuk Multiple
                              tombol Tambah disabled sampai semua grup wajib terisi
                        tidak → langsung addLine()
  → addLine() menggabungkan baris kalau lineKey() sama
       lineKey = productId | variantOptionIds tersortir | note

Pilih metode bayar
  → Cash  → input Bayar aktif + tombol cepat (Uang Pas / 50rb / 100rb), kembalian live
  → QRIS/Debit → amountPaid = total, kembalian 0

Klik BAYAR
  → btn.disabled = true  SEBELUM await          ← membunuh double-submit
  → POST /api/pos/checkout
       headers: RequestVerificationToken
       body:    { items:[{productId, quantity, variantOptionIds, note}], discountPercent,
                  discountAmount, paymentMethod, amountPaid, note }
                                     ↑ TIDAK ADA field harga
  → PosApiController.Checkout
      → IOrderService.CreateOrderAsync(dto, User.GetUserId())   ← 12 langkah di §5
      → hasil.Kind: Conflict → 409 | NotFound → 404 | lainnya → 400
  → JS:
       401  → location.href = '/Account/Login'
       !ok  → tampilkan errors, aktifkan lagi tombolnya
       200  → renderReceiptModal(result) + clearCart()
              modal menampilkan KEMBALIAN dengan huruf besar,
              tombol Cetak (window.print() + receipt.css) dan Transaksi Baru
```

Cetak ulang belakangan:

```
/Pos/Receipt/{id}     kasir — hanya order miliknya (cek IDOR)
/Orders/Receipt/{id}  admin — semua order
        keduanya me-render Views/Pos/Receipt.cshtml → _ReceiptPartial.cshtml
```

### 7.4 Void / refund

```
GET /Orders/Details/{id}
  → IOrderService.GetDetailsAsync
      CanVoid   = Status == Paid && tanggal order == hari ini WIB
      CanRefund = Status == Paid

GET /Orders/Reverse?id={id}&isVoid=true|false
  → GetForReverseAsync → RestoreStock default: true untuk void, false untuk refund
      (void: minumannya belum dibuat. refund: sudah dibuat dan dibuang, bahannya memang hilang)

POST /Orders/Reverse
  → IOrderService.ReverseOrderAsync
      → CreateExecutionStrategy → BeginTransaction
      → Status != Paid?  → Fail("Order ini sudah dibatalkan.")   ← penjaga idempotensi:
                                                                    tanpa ini, refresh halaman
                                                                    mengembalikan stok DUA KALI
      → isVoid && tanggal != hari ini → Fail("Void hanya untuk transaksi hari ini.")
      → restoreStock → IInventoryService.RestoreForOrderAsync
            membalik movement Out milik order INI (bukan resep produk sekarang —
            resepnya bisa saja sudah diubah sejak penjualan terjadi)
      → Status = Voided|Refunded, isi ReversedAt/ReversedByUserId/ReversalReason
      → SaveChanges + Commit
```

Baris order **tidak pernah dihapus** — menghapusnya menghancurkan jejak audit dan memungkinkan orang menyembunyikan pencurian. Laporan hanya menghitung `Status == Paid`, jadi omzet otomatis turun.

### 7.5 Laporan dan CSV

```
GET /Reports/Dashboard?date=yyyy-MM-dd
  → IReportService.GetDashboardAsync
      → GetSalesSummaryAsync(day, day)       revenue, count, itemCount, average
      → GetPaymentBreakdownAsync             GroupBy PaymentMethod
      → GetBestSellersAsync(day, day, 5)     GroupBy snapshot
      → GetDailySeriesAsync(day-6, day)      GroupBy tanggal, hari kosong diisi DI MEMORI
      → hitung LowStockCount

GET /Reports/ExportSalesCsv?from=...&to=...
  → IReportService.GetOrdersForExportAsync   ← SEMUA status, termasuk void/refund,
                                                karena file ini untuk rekonsiliasi
  → ICsvExporter.Export(rows, columns)
      delimiter ';'                  ← Windows Indonesia pakai ',' sebagai pemisah desimal
      UTF-8 + preamble BOM manual    ← lihat §10
      angka CultureInfo.InvariantCulture tanpa pemisah ribuan
  → File(bytes, "text/csv", "penjualan-....csv")
```

Link export cukup `<a href>` GET biasa — cookie autentikasi ikut otomatis, tidak perlu JS/blob/fetch.

---

## 8. Matriks role dan rute

| Rute | Anonim | Kasir | Admin |
|---|:---:|:---:|:---:|
| `/Account/Login`, `/AccessDenied` | ✅ | ✅ | ✅ |
| `/Home/Privacy`, `/Home/Error` | ✅ | ✅ | ✅ |
| `/` | → login | → `/Pos` | → `/Reports/Dashboard` |
| `/Pos`, `/Pos/Receipt/{id}` | → login | ✅ (order sendiri) | ✅ (semua) |
| `GET api/pos/catalog` | 401 | ✅ | ✅ |
| `POST api/pos/checkout` | 401 | ✅ | ✅ |
| `GET api/pos/orders/{id}/receipt` | 401 | ✅ order sendiri, **403** milik orang lain | ✅ |
| `/Categories`, `/Products`, `/VariantGroups`, `/Users`, `/Ingredients` | → login | **AccessDenied** | ✅ |
| `/Orders`, `/Orders/Details`, `/Orders/Reverse` | → login | **AccessDenied** | ✅ |
| `/Reports/*`, `/Reports/Export*Csv` | → login | **AccessDenied** | ✅ |
| `api/admin/*` | 401 | **403** | ✅ |
| File statis (`/lib/**`, `/css/**`, `/js/**`) | ✅ | ✅ | ✅ |

File statis harus tetap **200 untuk anonim** — kalau berubah jadi 302, artinya seseorang mengganti `AuthorizeFilter` dengan `FallbackPolicy` dan halaman login jadi tanpa CSS.

---

## 9. Mau nambah fitur? Sentuh file ini

### Menambah entity baru + CRUD-nya

1. `Models/Xxx.cs` — entity
2. `Data/Configurations/XxxConfiguration.cs` — key, max length, index, delete behavior
3. `Data/AppDbContext.cs` — tambah `DbSet<Xxx>`
4. `dotnet ef migrations add AddXxx` → **baca SQL-nya** → `dotnet ef database update`
5. `ViewModels/Xxx/XxxViewModels.cs` — `XxxListItemViewModel` + `XxxFormViewModel`
6. `Mappings/XxxMappings.cs` — `ToFormViewModel()` + `ApplyTo()` *(opsional kalau projeksinya inline)*
7. `Services/Interfaces/IXxxService.cs` + `Services/XxxService.cs`
8. `Services/ServiceCollectionExtensions.cs` — `AddScoped<IXxxService, XxxService>()`
9. `Controllers/XxxController.cs` — salin pola `CategoriesController`
10. `Views/Xxx/Index|Create|Edit|_Form.cshtml`
11. `Views/Shared/_Layout.cshtml` — tambah link di dropdown yang sesuai

### Menambah endpoint API baru

- Untuk kasir → `Controllers/Api/PosApiController.cs`
- Khusus admin → `Controllers/Api/AdminApiController.cs` (role sudah di level class)
- DTO-nya → `DTOs/Pos/` atau `DTOs/Reports/`, tulis sebagai `sealed record`
- POST otomatis butuh header `RequestVerificationToken`

### Menambah laporan baru

1. `DTOs/Reports/ReportDtos.cs` — record hasilnya
2. `Services/Interfaces/IReportService.cs` — tanda tangan method
3. `Services/ReportService.cs` — query. **Wajib**: `AsNoTracking()`, `Range()` setengah terbuka, `PaidOrders()`, `SumAsync` di-cast nullable, projeksi ke tipe anonim dulu baru map ke record
4. `ViewModels/Reports/ReportViewModels.cs`
5. `Controllers/ReportsController.cs` + `Views/Reports/Xxx.cshtml`
6. `Views/Shared/_Layout.cshtml` — link di dropdown Laporan

### Mengubah pajak / service charge

`appsettings.json` section `"KopiYo"`. **Tidak ada** perubahan kode.
Order lama tidak terpengaruh — persentasenya sudah di-snapshot di barisnya masing-masing.

### Menambah grup varian baru (mis. "Level Gula")

Murni lewat UI: `/VariantGroups/Create`, lalu tambahkan opsinya, lalu centang grup itu di form produk yang relevan. **Tanpa** perubahan kode dan **tanpa** migration.

### Mengizinkan penjualan saat stok kurang

`appsettings.json` → `"BlockSaleOnInsufficientStock": false`.
Penjualan lanjut, peringatannya muncul di `OrderResultDto.Warnings[]` dan tampil sebagai toast kuning di POS.

---

## 10. Jebakan yang sudah ditemui dan diperbaiki

Semua di bawah ini **benar-benar terjadi** saat membangun proyek ini, bukan daftar teoretis.

| # | Jebakan | Gejalanya | Perbaikannya |
|---|---|---|---|
| 1 | `GroupBy` diproyeksikan langsung ke constructor record positional | Dashboard **HTTP 500**: *"could not be translated"* | Projeksi ke tipe anonim dulu, `.ToListAsync()`, baru map ke record di memori — `ReportService.cs` |
| 2 | `new UTF8Encoding(true).GetBytes()` **tidak** menulis BOM | Excel membuka CSV sebagai ANSI, teks Indonesia jadi mojibake | Tempel `encoding.GetPreamble()` manual di depan — `CsvExporter.cs`. Flag itu hanya memengaruhi `GetPreamble()`, yang dipakai `StreamWriter` |
| 3 | `dotnet ef database update --no-build` setelah `migrations add` | *"No migrations were applied. The database is already up to date"* padahal tabel belum ada | `dotnet build` dulu — `--no-build` memuat assembly lama yang belum berisi migration baru |
| 4 | `[ValidateAntiForgeryToken]` di **level class** API controller | `GET /api/pos/catalog` balas **400** | Cukup andalkan `AutoValidateAntiforgeryToken` global (yang melewati verb aman). Atribut eksplisit memvalidasi **semua** verb termasuk GET |
| 5 | `sqlcmd` tanpa flag `-I` | *"UPDATE failed... 'QUOTED_IDENTIFIER'"* | Pakai `sqlcmd -I`. Database ini punya filtered index, yang mensyaratkan `QUOTED_IDENTIFIER ON` |
| 6 | `sqlcmd` tanpa flag `-C` | *"The certificate chain was issued by an authority that is not trusted"* | Pakai `sqlcmd -C` (setara `TrustServerCertificate=True`) |

### Jebakan yang dicegah sejak awal (jangan dibongkar)

| Hal | Kenapa begitu |
|---|---|
| `AuthorizeFilter`, bukan `FallbackPolicy` | `MapStaticAssets()` mendaftarkan CSS/JS sebagai endpoint → fallback policy me-redirect `bootstrap.min.css` ke login |
| `CreateExecutionStrategy().ExecuteAsync` membungkus transaksi | `EnableRetryOnFailure()` + `BeginTransaction` manual = exception |
| `HasData` tanpa `PasswordHasher`/`DateTime.Now` | Salt acak setiap panggilan → setiap `migrations add` menghasilkan `UpdateData` sia-sia |
| Dua FK `Order` → `Users` dikonfigurasi eksplisit | EF tidak bisa menebak navigasi baliknya; model gagal build |
| Cookie event `OnRedirectToLogin` untuk `/api` | Kalau tidak, `fetch()` menerima HTML login → `JSON.parse` gagal `Unexpected token <` |
| `SumAsync` di-cast `(decimal?)` | `SUM()` atas nol baris = SQL `NULL` → `InvalidOperationException` di pagi yang sepi |
| Rentang tanggal setengah terbuka | `<= tanggalAkhir` membuang hampir seluruh penjualan hari terakhir |
| `.WithStaticAssets()` dipertahankan | Itu yang membuat `asp-append-version` bekerja |
| `<Using Include="Microsoft.EntityFrameworkCore" />` di csproj | `ImplicitUsings` tidak mencakup EF Core |
| Nullable enabled → nav reference `= null!;`, koleksi `= [];` | Kalau tidak, tenggelam dalam peringatan CS8618 |

---

## Lampiran — perintah yang sering dipakai

```bash
cd "C:\Pedro\Shigoto\learningShigoto\fundamental\fundamentalDotnet\custom\MyKopiYo\KopiYo"

dotnet build
dotnet run                     # https://localhost:7057  ·  http://localhost:5242

dotnet ef migrations add NamaMigration
dotnet ef migrations script -o "..\_review.sql"    # BACA sebelum apply
dotnet build && dotnet ef database update          # build dulu, lihat jebakan #3
dotnet ef database drop --force                    # reset saat development

sqlcmd -S ".\SQLEXPRESS" -E -C -I -d db_kopiyo -W -Q "SELECT name FROM sys.tables ORDER BY name"
```

Akun bawaan (dibuat `DbInitializer`, **ganti sebelum dipakai sungguhan**):

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin123!` | Admin |
| `kasir` | `Kasir123!` | Kasir |
