# Yoremio

Yoremio, organik urun saticilari ile alicilari dogrudan bulusturan bir pazar yeri platformudur.

Platform rolleri:

- `SATICI`: urun ve profil yonetimi yapar, taleplere teklif verir.
- `ALICI`: urun kesfeder, favoriler, puan/yorum birakir, talep olusturur.

Not: Platform odeme ve kargo sureci yonetmez; iletisim ve anlasma kaydi tutar.

## Temel Ozellikler

- JWT + Identity tabanli kimlik ve rol yonetimi
- Satici/alici kayit ve giris
- Kategori ve urun yonetimi
- Yorum, puan ve favori akislari
- Onerilen urunler
- Satici guven skoru ve dogrulanmis satici rozeti
- Sehir/ilce bazli filtreleme
- Talep/teklif akisi
- SignalR tabanli mesajlasma (`/chathub`)

## Teknoloji Yigini

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core + PostgreSQL (`Npgsql`)
- ASP.NET Core Identity
- JWT Bearer Authentication
- SignalR

## Cozum Yapisi

- `API/`: endpointler, middleware, startup
- `Application/`: DTO'lar, servis arayuzleri, validation
- `Domain/`: entity'ler, modeller, sabitler, repository arayuzleri
- `Infrastructure/`: EF Core context, repository ve servis implementasyonlari
- `Tests/`: test runner ve chat e2e
- `scripts/`: yardimci scriptler

## Hizli Baslangic

### Gereksinimler

- .NET SDK 9.x
- PostgreSQL

### 1) Paketleri geri yukle

```bash
dotnet restore API/API.csproj
```

### 2) Cozumu derle

```bash
dotnet build yoremio.sln
```

### 3) Migration uygula

```bash
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project API/API.csproj --context YoremioContext
```

### 4) API'yi calistir

```bash
dotnet run --project API/API.csproj
```

HTTPS launch profile ile calistirmak icin:

```bash
dotnet run --project API/API.csproj --launch-profile https
```

## Development Seed

Development ortaminda uygulama acilisinda ornek veri otomatik seed edilir:

- roller
- test kullanicilari
- kategoriler
- urunler
- yorumlar
- puanlar
- favoriler
- talep ve teklifler

Seed urun medyalari `API/wwwroot/demo-media/` altinda tutulur.

Seed medyalarini yeniden uretmek icin:

```bash
powershell -ExecutionPolicy Bypass -File scripts/Download-SeedMedia.ps1
```

API yeniden baslatildiginda seed urunleri klasordeki medya dosyalari ile senkronize edilir.

### Test Kullanici Bilgileri

Tum kullanicilarin sifresi:

- `Test123!`

Saticilar:

- `ayse@demo.yoremio.local`
- `mehmet@demo.yoremio.local`
- `zeynep@demo.yoremio.local`

Alicilar:

- `elif@demo.yoremio.local`
- `can@demo.yoremio.local`
- `selin@demo.yoremio.local`

## Test

Genel test:

```bash
dotnet run --project Tests/Tests.csproj
```

Chat e2e testi:

```bash
dotnet run --project Tests/Tests.csproj -- --chat-e2e
```

## Ornek Endpointler

Auth:

- `POST /api/Auth/register/satici`
- `POST /api/Auth/register/alici`
- `POST /api/Auth/login`
- `GET /api/Auth/me`

Urun:

- `GET /api/Urun`
- `GET /api/Urun/onerilen`
- `GET /api/Urun/favorilerim`
- `POST /api/Urun/{urunId}/favori`
- `DELETE /api/Urun/{urunId}/favori`

Talep:

- `POST /api/Talep`
- `GET /api/Talep/benim`
- `GET /api/Talep/satici`
- `POST /api/Talep/{talepId}/teklif`
- `POST /api/Talep/teklif/{teklifId}/kabul`

Profil:

- `GET /api/Profil/satici`
- `PUT /api/Profil/satici`
- `GET /api/Profil/satici/{saticiId}/guven-skoru`

## Konfigurasyon Notlari

- JWT ayarlari `API/appsettings.json` icindeki `Jwt` bolumunden gelir.
- CORS allowlist `Cors:AllowedOrigins` uzerinden yonetilir.
- `Verification:PublicBaseUrl`, `Email:Smtp` ve `Sms:Twilio` ayarlari satici dogrulama akisinda kullanilir.

## Dokumantasyon

Detayli endpoint dokumani: `API_DOCUMENTATION.md`

## Lisans

Lisans bilgisi belirtilmediyse proje sahibi ile teyit edilmelidir.
