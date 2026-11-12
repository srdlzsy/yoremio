# Yoremio

Yoremio, organik urun saticilari ile alicilari dogrudan bulusturan bir pazar yeri platformudur.

## GitHub

GitHub repo linkinizi bu alana ekleyin:

- `https://github.com/<kullanici-adi>/<repo-adi>`

README'nin GitHub'da dogru gorunmesi icin dosya adinin `README.md` oldugundan ve depo kok dizininde bulundugundan emin olun.

Projeyi GitHub'a gondermek icin ornek komutlar:

```bash
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/<kullanici-adi>/<repo-adi>.git
git push -u origin main
```

Platform rolleri:

- `SATICI`: urun ve profil yonetimi yapar, taleplere teklif verir.
- `ALICI`: urun kesfeder, favoriler, puan/yorum birakir, talep olusturur.

Not: Platform odeme ve kargo sureci yonetmez; iletisim ve anlasma kaydi tutar.

## Temel Ozellikler

- JWT + Identity tabanli kimlik ve rol yonetimi
- Satici/alici kayit ve giris
- Kategori ve urun yonetimi (birim/miktar, yeni hasat, teslimat kapsami, mensei sehir, goruntulenme ve toplu stok/fiyat guncelleme)
- Yorum, puan, favori, satici takibi, fiyat alarmi ve stok alarmi akislari
- Konum bazli akilli urun kesfi (`/api/Urun/kesfet`) ve sonucsuz aramalarda akilli alternatifler + "Benim Icin Bul" onerisi
- Satici guven skoru ve dogrulanmis satici rozeti (admin manuel onay/ret destekli)
- Sehir/ilce bazli dinamik konum kesfi ve akilli oneriler
- Urun bazli & akilli acik pazar talep/teklif akisi (durum/sayfalama, akilli uretici eslestirme, tek dokunusla talep tekrari, dis anlasma tamamlama onayi)
- Yoremio Ortak Alim (bolgesel toplu siparis olusturma/onerme, satici onayi, katilim/hedef takibi)
- Kalici mesaj gecmisi, okundu bilgisi ve canli bildirimler (`NotificationReceived`) sunan SignalR (`/chathub`)
- Satici Kontrol Merkezi KPI'lari ve Admin dogrulama, moderasyon, tekrar-alici + arz boslugu analitigi (`/api/Admin/talep-bosluklari`)

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
- `GET /api/Urun/kesfet`
- `GET /api/Urun/onerilen`
- `GET /api/Urun/favorilerim`
- `POST /api/Urun/{urunId}/favori`
- `DELETE /api/Urun/{urunId}/favori`
- `PATCH /api/Urun/toplu-stok`
- `POST /api/Urun/{urunId}/fiyat-alarmi` / `DELETE /api/Urun/{urunId}/fiyat-alarmi` / `GET /api/Urun/fiyat-alarmlarim`
- `POST /api/Urun/{urunId}/stok-alarmi` / `DELETE /api/Urun/{urunId}/stok-alarmi` / `GET /api/Urun/stok-alarmlarim`

Talep:

- `POST /api/Talep`
- `POST /api/Talep/pazar`
- `GET /api/Talep/pazar`
- `GET /api/Talep/{talepId}`
- `GET /api/Talep/benim`
- `GET /api/Talep/satici`
- `POST /api/Talep/{talepId}/tekrarla`
- `POST /api/Talep/{talepId}/teklif`
- `POST /api/Talep/teklif/{teklifId}/kabul`
- `POST /api/Talep/teklif/{teklifId}/red`
- `POST /api/Talep/{talepId}/iptal`
- `POST /api/Talep/{talepId}/anlasma-onayi`

Ortak Alim:

- `GET /api/OrtakAlim`
- `GET /api/OrtakAlim/{id}`
- `POST /api/OrtakAlim`
- `POST /api/OrtakAlim/{id}/onayla`
- `POST /api/OrtakAlim/{id}/katil` / `DELETE /api/OrtakAlim/{id}/katil`
- `PATCH /api/OrtakAlim/{id}/durum`

Planli Gonderim ("Sehrime Gelen Urunler"):

- `GET /api/PlanliGonderim?hedefSehir=&menseiSehir=`
- `POST /api/PlanliGonderim`
- `DELETE /api/PlanliGonderim/{id}`

Chat:

- `GET /api/Chat/conversations`
- `GET /api/Chat/messages/{otherUserId}?urunId=&talepId=`
- `POST /api/Chat/messages/{receiverId}` (`urunId`, `talepId` destekli)
- `POST /api/Chat/messages/{otherUserId}/read`
- SignalR: `/chathub`

Bildirim:

- `GET /api/Bildirim`
- `GET /api/Bildirim/okunmamis-sayisi`
- `POST /api/Bildirim/{bildirimId}/okundu`
- `POST /api/Bildirim/tumunu-okundu`

Profil:

- `GET /api/Profil/alici`
- `PUT /api/Profil/alici`
- `GET /api/Profil/satici`
- `PUT /api/Profil/satici`
- `GET /api/Profil/satici/{saticiId}`
- `GET /api/Profil/satici/{saticiId}/guven-skoru`
- `GET /api/Profil/saticilar/one-cikan`
- `POST /api/Profil/satici/{saticiId}/takip`
- `DELETE /api/Profil/satici/{saticiId}/takip`
- `GET /api/Profil/takip-edilenler`

Admin:

- `GET /api/Admin/kullanicilar`
- `PATCH /api/Admin/saticilar/{saticiId}/dogrulama`
- `PATCH /api/Admin/saticilar/{saticiId}/durum`
- `GET /api/Admin/urunler`
- `PATCH /api/Admin/urunler/{urunId}/durum`
- `GET /api/Admin/yorumlar`
- `PATCH /api/Admin/yorumlar/{yorumId}/durum`
- `GET /api/Admin/talep-bosluklari`

## Konfigurasyon Notlari

- JWT ayarlari `API/appsettings.json` icindeki `Jwt` bolumunden gelir.
- CORS allowlist `Cors:AllowedOrigins` uzerinden yonetilir.
- `Verification:PublicBaseUrl` ve `Email:Smtp` ayarlari satici email dogrulama akisinda kullanilir.
- `Startup:ApplyMigrations` ve `Startup:SeedSampleData` bos/null birakilirsa sadece Development ortaminda otomatik calisir.
- `RateLimiting:PermitLimit` ve `RateLimiting:WindowSeconds` global API rate limit davranisini belirler.
- `RateLimiting:AuthPermitLimit` (varsayilan `15`) ve `RateLimiting:AuthWindowSeconds` (varsayilan `60`) auth endpointleri (`AuthStrict` politikasi) icin sıkı rate limit ayarlar.
- Health check uclari: `/health` (uygulama liveness) ve `/health/ready` (PostgreSQL baglanti readiness kontrolu).
- `Cloudinary:Enabled=true` verilirse yeni urun medya uploadlari Cloudinary CDN'e yuklenir. Gerekli ayarlar: `Cloudinary:CloudName`, `Cloudinary:ApiKey`, `Cloudinary:ApiSecret`, `Cloudinary:UploadFolder`.

### Local Production Ayarlari

API baslarken repo kokundeki `.env.local` dosyasini otomatik okur. Bu dosya `.gitignore` icindedir; canli veritabani, Cloudinary, SMTP ve JWT secret degerleri burada tutulabilir.

Localde ayni production ayarlariyla calistirmak icin:

```powershell
dotnet run --project API
```

Docker ile localde ayni ayarlari kullanmak icin:

```powershell
docker compose -f docker-compose.local.yml up --build
```

`.env.local` icinde `ASPNETCORE_ENVIRONMENT=Production` varsa uygulama production kurallariyla acilir. Satici dogrulamasinda sadece email kullanilir.

### Gercek Email Saglayicilari

Email dogrulama icin SMTP uyumlu veya HTTPS API destekli ucretsiz kotali servisler kullanilabilir:

- Brevo API: `Email:Smtp:Provider=BrevoApi`, `ApiKey=<Brevo API key>`, `ApiBaseUrl=https://api.brevo.com`
- Brevo: `Email:Smtp:Provider=Brevo`, `Host=smtp-relay.brevo.com`, `UserName=<Brevo SMTP login>`, `Password=<Brevo SMTP key>`
- Resend: `Email:Smtp:Provider=Resend`, `Host=smtp.resend.com`, `UserName=resend`, `Password=<Resend API key>`

Gercek email gondermek icin `Email:Smtp:UseMockSender=false` yapilmalidir.

Render deploy varsayilani SMTP port timeout riskini azaltmak icin Brevo API ile gercek email dogrulama gonderecek sekildedir:

- `Verification:RequireConfirmedEmailForSellerLogin=true`
- `Email:Smtp:UseMockSender=false`
- `Email:Smtp:Provider=BrevoApi`

Deploy oncesi Render dashboard'da `Email__Smtp__ApiKey`, `Email__Smtp__FromAddress` ve `Verification__PublicBaseUrl` secret/config degerleri girilmelidir. SMTP provider kullanilacaksa `Email__Smtp__UserName` ve `Email__Smtp__Password` da girilmelidir. Bu degerler eksikse production uygulama bilincli olarak acilmaz.

## Production Notlari

- JWT key, SMTP bilgileri ve veritabani connection string production ortaminda environment variable, user secret veya secret manager ile verilmelidir.
- Production ortaminda varsayilan `Jwt:Key` kullanilirsa uygulama acilista hata verir.
- Render gibi kalici disk vermeyen ortamlarda local `wwwroot` uploadlari production icin uygun degildir; Cloudinary veya benzeri kalici obje/CDN servisi kullanilmalidir.
- Migration uygulamasi production deployment pipeline'inda kontrollu calistirilmelidir; uygulama icinde acmak icin `Startup:ApplyMigrations=true` verilebilir.
- Seed verisi production'da kapali tutulmalidir; `Startup:SeedSampleData=true` sadece test/demo ortamlarinda kullanilmalidir.
- Satici girisi varsayilan olarak email dogrulamasi ister.

## Dokumantasyon

Detayli endpoint dokumani: `API_DOCUMENTATION.md`

Genel proje akis ve is mantigi: `PROJE_AKIS_MANTIGI.md`

## Lisans

Lisans bilgisi belirtilmediyse proje sahibi ile teyit edilmelidir.
