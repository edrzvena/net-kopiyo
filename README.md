# KopiYo

Aplikasi kasir (POS) untuk cafe: satu layar transaksi untuk kasir, master data
dan laporan untuk admin, dengan stok bahan baku yang berkurang otomatis
mengikuti resep tiap menu.

ASP.NET Core MVC · .NET 10 · EF Core 10 · SQL Server Express · Bootstrap 5.3

---

## Daftar isi

- [Apa yang bisa dilakukan](#apa-yang-bisa-dilakukan)
- [Menjalankan](#menjalankan)
- [Konfigurasi](#konfigurasi)
- [Struktur proyek](#struktur-proyek)
- [Lima aturan arsitektur](#lima-aturan-arsitektur)
- [Siapa boleh akses apa](#siapa-boleh-akses-apa)
- [Perintah yang sering dipakai](#perintah-yang-sering-dipakai)
- [Dokumentasi lain](#dokumentasi-lain)

---

## Apa yang bisa dilakukan

### Kasir — `/Pos`

Satu layar tanpa pindah halaman: katalog di tengah, kategori di kiri, keranjang
menempel di kanan.

- Pilih menu, tentukan varian (ukuran, panas/dingin, extra shot), tambah catatan
  seperti *less ice*
- Diskon persen, service charge, pajak — semuanya dihitung ulang **di server**
- Tunai / QRIS / Debit, tombol uang pas dan pecahan cepat, kembalian otomatis
- Struk langsung muncul setelah bayar dan bisa dicetak (`@media print` sudah diatur
  supaya navbar dan tombol tidak ikut tercetak)

### Master data — khusus admin

- **Kategori** menu beserta urutan tampilnya
- **Produk** dengan harga dasar dan grup varian yang menempel padanya
- **Grup varian** dan opsinya, lengkap dengan selisih harga per opsi
- **Resep** — bahan apa saja dan berapa banyak yang dipakai tiap satu porsi produk
- **Pengguna** dan perannya, termasuk reset password

### Stok bahan baku

- Berkurang otomatis setiap penjualan, sesuai resep produk yang terjual
- Setiap perubahan menghasilkan baris di buku besar stok dengan
  `StockBefore` / `StockAfter` — tidak ada perubahan stok yang tidak tercatat
- Penyesuaian manual (stok opname, barang rusak, barang masuk) lewat menu
  *Sesuaikan Stok*, bukan dengan mengetik langsung di kolom stok
- Peringatan stok menipis: badge di navbar dan filter di halaman bahan

### Laporan — khusus admin

- **Dashboard** harian: omzet, jumlah transaksi, item terjual, rata-rata per
  transaksi, grafik 7 hari terakhir, rincian metode bayar, menu terlaris
- **Penjualan** per rentang tanggal, **per kasir**, dan **menu terlaris**
- Ekspor **CSV** untuk penjualan dan menu terlaris — delimiter `;` dan
  ber-BOM UTF-8, jadi langsung rapi saat dibuka Excel berlokal Indonesia
- **Riwayat transaksi** dengan void dan refund

### Void & refund

- **Void** — membatalkan transaksi salah input; omzet berkurang dan bahan
  dikembalikan ke stok
- **Refund** — mencatat pengembalian uang; omzet berkurang, tapi stok **tidak**
  otomatis kembali karena minuman yang telanjur dibuat memang sudah terpakai
- Keduanya hanya menandai, tidak pernah menghapus baris. Jejak audit tetap utuh
  dan laporan omzet hanya menghitung order berstatus `Paid`.

---

## Menjalankan

### Prasyarat

| Kebutuhan | Keterangan |
|---|---|
| [.NET SDK 10](https://dotnet.microsoft.com/download) | `dotnet --version` harus ≥ `10.0` |
| SQL Server Express | Instance bernama `.\SQLEXPRESS`, autentikasi Windows |
| `dotnet-ef` | Opsional — hanya perlu kalau mau **membuat** migrasi baru. `dotnet tool install --global dotnet-ef` |

Tidak ada Node.js, npm, atau build step frontend. Bootstrap dan jQuery
di-*commit* apa adanya di `wwwroot/lib/`.

### Langkah

```bash
cd KopiYo
dotnet build
dotnet run
```

Database dan seluruh migrasinya **dibuat otomatis** saat pertama kali dijalankan —
`DbInitializer.SeedAsync` memanggil `Database.MigrateAsync()`, jadi tidak perlu
`dotnet ef database update` manual untuk memulai.

Buka salah satu:

- <http://localhost:5242>
- <https://localhost:7057>

### Akun bawaan

Dibuat saat tabel `Users` masih kosong:

| Username | Password | Peran |
|---|---|---|
| `admin` | `Admin123!` | Admin |
| `kasir` | `Kasir123!` | Kasir |

> ⚠️ **Kedua akun ini dibuat di environment apa pun, bukan hanya Development.**
> `DbInitializer.SeedAsync` hanya membatasi *data demo* (kategori, produk, varian,
> resep) ke Development — user dan daftar bahan baku selalu di-seed.
> Ganti kedua password sebelum aplikasi ini dipakai sungguhan. Aplikasi menulis
> peringatan `LogWarning` setiap kali user default dibuat.

---

## Konfigurasi

Semua di `KopiYo/appsettings.json` bagian `"KopiYo"`. Mengubah nilainya
**tidak** butuh perubahan kode:

```jsonc
{
  "CafeName": "KopiYo",                  // dicetak di kepala struk
  "CafeAddress": "Jl. Kopi Nikmat No. 1",
  "CafePhone": "0812-0000-0000",
  "TaxPercent": 10.0,                    // di-snapshot ke tiap order —
                                         // struk lama tidak ikut berubah
  "ServiceChargePercent": 0.0,
  "BlockSaleOnInsufficientStock": true,  // false = boleh jual sampai stok minus,
                                         // kasir cuma dapat peringatan
  "LowStockWarningEnabled": true
}
```

Connection string:

```
Server=.\SQLEXPRESS;Database=db_kopiyo;Trusted_Connection=True;TrustServerCertificate=True;
```

`TrustServerCertificate=True` wajib ada — SqlClient 4.0+ memakai `Encrypt=true`
sebagai default, sedangkan SQL Express lokal memakai sertifikat *self-signed*.

---

## Struktur proyek

Satu project, dipisah **per folder**, bukan per assembly.

```
KopiYo/
├── Common/        enum, konstanta, ServiceResult, extension uang & waktu
├── Models/        13 entity EF
├── Data/          AppDbContext, 13 IEntityTypeConfiguration, DbInitializer
├── Migrations/    migrasi EF Core
├── DTOs/          sealed record — kontrak JSON untuk /api
├── ViewModels/    class + setter — model untuk .cshtml
├── Mappings/      extension static entity ↔ VM/DTO (manual, bukan AutoMapper)
├── Services/      Interfaces/ + implementasi — SELURUH logic bisnis di sini
├── Controllers/   MVC tipis + Api/ (2 ApiController)
├── Views/         Razor
└── wwwroot/       css/, js/, lib/ (Bootstrap 5.3.3, jQuery)
```

**Aliran satu arah:** `Controller → Service → DbContext → SQL`

Controller tidak pernah menyentuh `AppDbContext`. Service tidak pernah menyentuh
`HttpContext`, `ModelState`, atau `TempData`.

**Sengaja tidak dipakai:** Repository/UnitOfWork generic, AutoMapper, CsvHelper,
ASP.NET Core Identity, framework frontend. Paket NuGet hanya dua:
`Microsoft.EntityFrameworkCore.SqlServer` dan `.Design`.

---

## Lima aturan arsitektur

Ringkasnya di sini; alasan lengkap dan contoh kodenya ada di
[`flow.md` §2](./flow.md#2-lima-aturan-arsitektur-yang-tidak-boleh-dilanggar).

1. **Order adalah dokumen keuangan yang immutable.**
   Nama produk, harga satuan, nama kasir, dan persen pajak di-*snapshot* ke baris
   order saat penjualan terjadi. Struk lama tidak ikut berubah ketika harga menu
   dinaikkan. Karena itu struk dan laporan tidak pernah `Include` tabel master.

2. **Semua uang dihitung ulang di server.**
   `CreateOrderDto` tidak punya field harga sama sekali — client hanya mengirim
   `productId`, `quantity`, dan `variantOptionIds`.

3. **Stok hanya bergerak lewat `IInventoryService`.**
   Kolom stok tidak bisa diedit lewat form biasa, dan setiap perubahan
   menghasilkan baris `StockMovement`.

4. **Tidak ada hard delete di mana pun riwayat bisa menjangkau.**
   Hanya `IsActive`; tombolnya bertuliskan "Nonaktifkan", bukan "Hapus".

5. **Role dijaga di controller, bukan di navbar.**
   `[Authorize(Roles = ...)]` di level class. Menyembunyikan link di layout murni
   kosmetik — kasir yang mengetik `/Products` langsung tetap ditolak.

---

## Siapa boleh akses apa

| Rute | Anonim | Kasir | Admin |
|---|:---:|:---:|:---:|
| `/Account/Login`, `/AccessDenied` | ✅ | ✅ | ✅ |
| `/` | → login | → `/Pos` | → `/Reports/Dashboard` |
| `/Pos`, `/Pos/Receipt/{id}` | → login | ✅ order sendiri | ✅ semua |
| `GET api/pos/catalog`, `POST api/pos/checkout` | 401 | ✅ | ✅ |
| `GET api/pos/orders/{id}/receipt` | 401 | ✅ sendiri, **403** milik orang lain | ✅ |
| `/Categories`, `/Products`, `/VariantGroups`, `/Ingredients`, `/Users` | → login | **AccessDenied** | ✅ |
| `/Orders`, `/Orders/Reverse` | → login | **AccessDenied** | ✅ |
| `/Reports/*` termasuk ekspor CSV | → login | **AccessDenied** | ✅ |
| `api/admin/*` | 401 | **403** | ✅ |
| File statis (`/css/**`, `/js/**`, `/lib/**`) | ✅ | ✅ | ✅ |

Permintaan ke `/api` dijawab **401/403**, bukan redirect ke halaman login —
kalau tidak, `fetch()` akan menerima HTML dan `JSON.parse` gagal dengan pesan
menyesatkan `Unexpected token <`.

File statis harus tetap **200 untuk anonim**. Kalau berubah jadi 302, artinya
`AuthorizeFilter` diganti dengan `FallbackPolicy` dan halaman login akan tampil
tanpa CSS.

---

## Perintah yang sering dipakai

Semua dijalankan dari `KopiYo/`, bukan root repo.

```bash
dotnet build
dotnet run

# Migrasi
dotnet ef migrations add NamaMigration
dotnet build                      # WAJIB sebelum update — lihat catatan di bawah
dotnet ef database update

# Reset data development
dotnet ef database drop --force

# Periksa isi database
sqlcmd -S ".\SQLEXPRESS" -E -C -I -d db_kopiyo -W -Q "SELECT ..."
```

Beberapa hal yang pernah menjebak di proyek ini:

- `dotnet ef database update --no-build` setelah `migrations add` akan menjawab
  *"already up to date"* padahal tabelnya belum ada — `--no-build` memuat
  assembly lama. Jalankan `dotnet build` dulu.
- `sqlcmd` wajib memakai `-C` (sertifikat self-signed) dan `-I` (database ini
  punya *filtered index* yang mensyaratkan `QUOTED_IDENTIFIER ON`).

Daftar jebakan yang lebih lengkap ada di
[`flow.md` §10](./flow.md#10-jebakan-yang-sudah-ditemui-dan-diperbaiki).

---

## Pengujian

Belum ada test project. Verifikasi dilakukan dengan menjalankan aplikasi lalu
memeriksanya lewat HTTP dan `sqlcmd`.

---

## Dokumentasi lain

| Berkas | Isi |
|---|---|
| [`flow.md`](./flow.md) | Peta lengkap: fungsi tiap folder, isi tiap file, alur request end-to-end, model data. **Mulai dari sini kalau mau mengubah kode.** |
| [`flow.md` §0](./flow.md#0-untuk-yang-baru-belajar--apa-fungsi-tiap-folder) | Penjelasan dari nol untuk yang baru belajar C#/.NET: entity, DI, interface, Razor, migration, beda DTO dan ViewModel |
| [`CLAUDE.md`](./CLAUDE.md) | Konvensi kode dan aturan yang harus diikuti asisten AI saat menyentuh repo ini |
