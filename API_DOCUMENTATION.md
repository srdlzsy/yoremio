# Yoremio API Documentation

Bu doküman UI tasarımı ve frontend geliştirme için ana sözleşmedir. Amaç sadece endpoint listesi vermek değil; hangi ekranın hangi veriye ihtiyaç duyduğunu, akışların arkasındaki temel mantığı, hata/validasyon davranışlarını ve gerçek zamanlı chat kullanımını da netleştirmektir.

## 1. Genel Bakış

Yoremio, yerel üretici/satıcı ile alıcıyı buluşturan bir pazar yeri API'sidir.

Temel roller:

- `ALICI`: Ürünleri keşfeder, favoriler, puan/yorum bırakır, talep oluşturur, satıcıyla chat yapar.
- `SATICI`: Profilini yönetir, ürün ekler/günceller, kendi ürünlerine gelen taleplere teklif verir, alıcıyla chat yapar.
- Anonim kullanıcı: Kategori, ürün, ürün detayı, yorum/puan listesi ve satıcı güven skoru gibi public verileri görebilir.

Ana backend parçaları:

- REST API: CRUD, listeleme, auth, ürün, talep, yorum, puan, profil işlemleri.
- SignalR Hub: Canlı chat ve typing/read eventleri.
- Static files: Ürün resim/video URL'leri API tarafından static file olarak servis edilir.
- JWT auth: Korumalı endpointlerde `Authorization: Bearer <token>` gerekir.

## 2. Runtime Bilgileri

Development base URL:

- HTTP: `http://localhost:5089`
- HTTPS: `https://localhost:7194`

Önemli yollar:

- Health check: `GET /health`
- SignalR hub: `/chathub`
- Swagger/OpenAPI: sadece Development ortamında map edilir.

Rate limit:

- Varsayılan: `300 request / 60 saniye`
- Config: `RateLimiting:PermitLimit`, `RateLimiting:WindowSeconds`
- Limit aşılırsa `429 Too Many Requests` döner.

CORS:

- Config: `Cors:AllowedOrigins`
- Development varsayılanları: `http://localhost:4200`, `https://localhost:4200`, `http://localhost:5173`, `https://localhost:5173`
- SignalR için `AllowCredentials` açıktır.

Startup:

- `Startup:ApplyMigrations`: `null` ise sadece Development ortamında migration çalışır.
- `Startup:SeedSampleData`: `null` ise sadece Development ortamında seed çalışır.

Production notları:

- `Jwt:Key` production'da appsettings içindeki varsayılan secret olamaz.
- `ConnectionStrings:DefaultConnection`, JWT, SMTP ve SMS bilgileri production'da environment/secret manager üzerinden verilmelidir.
- Development seed ve mock ayarları production için açılmamalıdır.

## 3. Frontend İçin Temel Kurallar

### 3.0 App Bootstrap

UI acilisinda once bu endpoint cagrilabilir. Amaci frontend'in kategori listesini, desteklenen rolleri, urun siralama seceneklerini, feature flag'leri ve upload limitlerini tek sozlesmeden almasidir.

`GET /api/App/bootstrap`

Auth: Yok

Response `data`:

```json
{
  "environment": "Development",
  "roles": ["ADMIN", "ALICI", "SATICI"],
  "categories": [
    {
      "id": 1,
      "adi": "Bal",
      "aciklama": "Yerel bal urunleri"
    }
  ],
  "productSorts": [
    "newest",
    "oldest",
    "price_asc",
    "price_desc",
    "name_asc",
    "name_desc",
    "top_rated",
    "most_reviewed",
    "most_favorited"
  ],
  "features": {
    "chatEnabled": true,
    "demandFlowEnabled": true,
    "favoritesEnabled": true,
    "ratingsEnabled": true,
    "reviewsEnabled": true,
    "devVerificationInboxEnabled": true,
    "cloudinaryEnabled": false
  },
  "verification": {
    "requireConfirmedEmailForSellerLogin": true,
    "requireConfirmedPhoneForSellerLogin": true,
    "devVerificationInboxUrl": "/dev/verification"
  },
  "uploads": {
    "maxImageBytes": 5242880,
    "maxVideoBytes": 52428800,
    "maxMultipartBodyBytes": 100000000,
    "imageContentTypePrefixes": ["image/"],
    "videoContentTypePrefixes": ["video/"]
  }
}
```

UI kullanimi:

- Kategori filtrelerini `categories` ile doldurun.
- Urun siralama select'ini `productSorts` ile doldurun.
- Dosya yukleme validasyonunda `uploads` limitlerini kullanin.
- Development ortaminda `features.devVerificationInboxEnabled=true` ise satici dogrulama ekraninda `verification.devVerificationInboxUrl` linkini gosterin.
- Feature flag kapaliysa ilgili UI aksiyonunu gizleyin veya pasif gosterin.

### 3.1 Dashboard / Header Summary

Login sonrasi UI header badge'leri, bos durum kararlarini ve panel kartlarini tek tek liste endpointlerinden hesaplamamalidir. Bunun yerine asagidaki endpointler kullanilmalidir:

- `GET /api/Dashboard/summary`: Login kullanici icin kisa rozet ve sayac ozeti.
- `GET /api/Dashboard/satici`: Satici panelinin ilk ekran kartlari.
- `GET /api/Dashboard/admin`: Admin panelinin ilk ekran kartlari.

UI kullanimi:

- App acilinca `GET /api/App/bootstrap`, token varsa `GET /api/Auth/me`, ardindan `GET /api/Dashboard/summary` cagrilabilir.
- Header mesaj badge'i icin `unreadMessages` kullanin.
- Favoriler/talepler/teklifler bos ekran kararinda summary sayaclarini kullanin.
- Satici panelinde kartlari doldurmak icin `GET /api/Dashboard/satici` cagrin; urun tablosu icin yine `GET /api/Urun/urunlerim` kullanin.
- Admin panelinde ilk KPI kartlari icin `GET /api/Dashboard/admin` cagrin; detay listeleri ayrica ilgili endpointlerden yuklenmelidir.

### 3.2 Auth Header

Korumalı endpointlerde:

```http
Authorization: Bearer <jwt-token>
```

Login response içinden UI'ın saklaması gereken minimum state:

```json
{
  "token": "jwt-token",
  "userId": "identity-user-id",
  "email": "user@example.com",
  "role": "ALICI",
  "roles": ["ALICI"]
}
```

Öneri:

- Token'ı API client interceptor seviyesinde ekleyin.
- Uygulama açılışında token varsa `GET /api/Auth/me` ile oturumu doğrulayın.
- `401` gelirse token'ı temizleyip login ekranına yönlendirin.
- `403` gelirse kullanıcının rolü ekran için yetkisizdir; login'e atmak yerine yetkisiz ekranı göstermek daha doğru olur.

Rol notu:

- `role` geriye uyumluluk icin tekil ilk roldur; yeni UI yetki kontrolunde `roles` listesini esas almalidir.
- Satici kullanicilar hem `SATICI` hem `ALICI` rolu tasiyabilir.

### 3.2 Tarih/Saat

API `DateTime` değerlerini UTC mantığıyla döner.

UI:

- Liste kartlarında lokal kısa tarih/saat gösterin.
- Chat mesajlarında `SentAt`, okunma bilgisinde `ReadAt`, talep/tekliflerde `OlusturmaTarihi` kullanılır.

### 3.3 Para ve Sayılar

`Fiyat`, `BirimFiyat` gibi alanlar `decimal` döner.

UI:

- Para birimi backend'de ayrı alan olarak dönmüyor; mevcut tasarımda TL varsayılabilir.
- Gösterim örneği: `420,00 TL`
- Filtrelerde `minFiyat`, `maxFiyat` decimal gönderilebilir.

### 3.4 Media URL Kullanımı

Ürün medya alanları iki formatta dönebilir:

- Local/demo/static storage: relative URL, örnek `/demo-media/yayla-bali/resimler/1.jpg`
- Cloudinary storage: absolute URL, örnek `https://res.cloudinary.com/.../image/upload/...jpg`

```json
{
  "url": "/demo-media/yayla-bali/resimler/1.jpg"
}
```

UI tek helper ile mutlak URL üretmeli:

```ts
function resolveMediaUrl(url?: string | null) {
  if (!url) return "/placeholder-product.jpg";
  return /^https?:\/\//i.test(url) ? url : `${API_BASE_URL}${url}`;
}
```

Production önerisi:

- Render gibi ephemeral filesystem kullanan ortamlarda upload dosyaları container içinde kalıcı değildir.
- `Cloudinary:Enabled=true` olduğunda yeni upload edilen ürün resimleri/videoları Cloudinary'ye yüklenir ve DB'ye Cloudinary CDN URL'i yazılır.
- Eski relative demo medya URL'leri çalışmaya devam eder; frontend bu yüzden iki URL formatını da desteklemelidir.

### 3.5 Standart Response Envelope

Başarılı veya hatalı çoğu response `ApiResponse<T>` formatındadır:

```json
{
  "success": true,
  "message": "İşlem başarılı.",
  "data": {},
  "errors": null,
  "traceId": "0HN..."
}
```

Validation hatası:

```json
{
  "success": false,
  "message": "Dogrulama hatasi olustu.",
  "data": null,
  "errors": {
    "Email": [
      "Email boş olamaz."
    ],
    "Password": [
      "Şifre en az 8 karakter olmalı."
    ]
  },
  "traceId": "0HN..."
}
```

UI davranışı:

- Toast/snackbar için `message` kullanılabilir.
- Form alanı hataları için `errors` nesnesindeki field adlarını kullanın.
- Destek/log ekranlarında `traceId` gösterilebilir.
- Backend bazı hata mesajlarını iş kuralı olarak `400`, `403`, `404` ile döner; UI bunları kullanıcıya anlaşılır metinle göstermelidir.

## 4. Rol Bazlı UI Haritası

### 4.1 Anonim Kullanıcı

Gösterebileceği ekranlar:

- Ana sayfa / ürün listesi
- Kategori filtresi
- Ürün detay
- Ürün yorumları ve puan ortalaması
- Satıcı güven skoru
- Login/register ekranları

Kullanamayacağı aksiyonlar:

- Favori ekleme
- Puan/yorum yazma
- Talep oluşturma
- Chat
- Ürün yönetimi

### 4.2 Alıcı Paneli

Ana ekranlar:

- Ürün keşif/listesi
- Ürün detay + favori + puan + yorum
- Önerilen ürünler
- Favorilerim
- Taleplerim
- Chat

Önemli endpointler:

- `GET /api/Dashboard/summary`
- `GET /api/Urun`
- `GET /api/Urun/onerilen`
- `GET /api/Urun/favorilerim`
- `POST /api/Urun/{urunId}/favori`
- `DELETE /api/Urun/{urunId}/favori`
- `POST /api/Puan/puan-ekle`
- `POST /api/Yorum`
- `POST /api/Talep`
- `GET /api/Talep/benim`
- `GET /api/Chat/conversations`

### 4.3 Satıcı Paneli

Ana ekranlar:

- Satıcı dashboard
- Satıcı profil
- Ürünlerim
- Ürün ekle/güncelle
- Ürün medya yönetimi
- Gelen talepler
- Teklif verme
- Chat

Önemli endpointler:

- `GET /api/Dashboard/summary`
- `GET /api/Dashboard/satici`
- `GET /api/Profil/satici`
- `PUT /api/Profil/satici`
- `GET /api/Urun/urunlerim`
- `POST /api/Urun/urun-ekle`
- `PUT /api/Urun/{urunId}`
- `PATCH /api/Urun/{urunId}/status`
- `DELETE /api/Urun/{urunId}`
- `DELETE /api/Urun/{urunId}/resimler/{resimId}`
- `DELETE /api/Urun/{urunId}/videolar/{videoId}`
- `GET /api/Talep/satici`
- `POST /api/Talep/{talepId}/teklif`
- `GET /api/Chat/conversations`

Not: Kategori oluşturma/güncelleme/silme endpointleri `ADMIN` rolü ister. Satıcı panelinde kategori CRUD gösterilmemeli, sadece kategori seçimi yapılmalıdır.

## 5. Ekran Akışları

### 5.1 App Açılışı / Session Bootstrap

1. Local/session storage içinde token var mı bakılır.
2. Token varsa `GET /api/Auth/me` çağrılır.
3. Başarılıysa kullanıcı state'i kurulur: `userId`, `email`, `role`, doğrulama durumları.
4. Başarısızsa token temizlenir ve anonim state'e düşülür.

`GET /api/Auth/me` response:

```json
{
  "success": true,
  "message": "Kullanıcı bilgisi getirildi.",
  "data": {
    "userId": "user-id",
    "email": "buyer@example.com",
    "userName": "buyer@example.com",
    "phoneNumber": "+905321000001",
    "role": "ALICI",
    "roles": ["ALICI"],
    "emailConfirmed": true,
    "phoneNumberConfirmed": true
  },
  "traceId": "..."
}
```

### 5.2 Ana Sayfa / Ürün Listeleme

Önerilen çağrı sırası:

1. `GET /api/Kategori`
2. `GET /api/Urun?page=1&pageSize=12&sort=newest`
3. Kullanıcı alıcıysa opsiyonel: `GET /api/Urun/favorilerim`

UI bileşenleri:

- Arama kutusu: `q`
- Kategori filtresi: `kategoriId`
- Fiyat aralığı: `minFiyat`, `maxFiyat`
- Lokasyon filtresi: `sehir`, `ilce`
- Stok toggle: `sadeceStoktaOlanlar=true`
- Puan filtresi: `minOrtalamaPuan`
- Sıralama select: `sort`
- Pagination: `page`, `pageSize`, `totalPages`

Liste response `data` formatı:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 12,
  "totalCount": 80,
  "totalPages": 7
}
```

### 5.3 Ürün Detay

Önerilen çağrı sırası:

1. `GET /api/Urun/{id}`
2. `GET /api/Yorum/{urunId}` veya ürün DTO içindeki `yorumlar`
3. `GET /api/Puan/ortalama/{urunId}` veya ürün DTO içindeki `ortalamaPuan`
4. `GET /api/Profil/satici/{saticiId}/guven-skoru`
5. Alıcı login ise favori durumunu anlamak için `GET /api/Urun/favorilerim`

Ürün detay UI alanları:

- Görsel/video galeri: `resimler`, `videolar`
- Ürün adı: `adi`
- Açıklama: `aciklama`
- Fiyat: `fiyat`
- Stok: `stokMiktari`
- Satıcı adı: `saticiMagazaAdi`
- Satıcı konum: `saticiSehir`, `saticiIlce`
- Doğrulanmış satıcı rozeti: `saticiDogrulanmis`
- Ortalama puan: `ortalamaPuan`
- Sosyal kanıt: `toplamPuan`, `toplamYorum`, `toplamFavori`

Alıcı aksiyonları:

- Favoriye ekle/çıkar
- Puan ver
- Yorum ekle/güncelle/sil
- Talep oluştur
- Satıcıya mesaj gönder

### 5.4 Satıcı Ürün Yönetimi

Ürünlerim ekranı:

1. `GET /api/Urun/urunlerim`
2. Kart/listede ürün medya, fiyat, stok, kategori, puan sayaçları gösterilir.

Ürün ekleme:

1. `GET /api/Kategori`
2. `POST /api/Urun/urun-ekle` multipart/form-data

Ürün güncelleme:

1. `GET /api/Urun/{urunId}`
2. `PUT /api/Urun/{urunId}` multipart/form-data

Aktif/pasif durumu:

- `PATCH /api/Urun/{urunId}/status`
- Body: `{ "aktifMi": false }`
- UI'da toggle veya menu aksiyonu olarak kullanin. Pasife alinan urun public listelemede gosterilmez, saticinin `urunlerim` ekraninda yonetilmeye devam eder.

Medya silme:

- Resim: `DELETE /api/Urun/{urunId}/resimler/{resimId}`
- Video: `DELETE /api/Urun/{urunId}/videolar/{videoId}`

Önemli mantık:

- `PUT /api/Urun/{urunId}` ile gönderilen yeni medya mevcut medyaya eklenir.
- Mevcut medyayı kaldırmak için özel delete endpointleri kullanılmalıdır.
- Ürün silme şu an gerçek delete davranışıdır; UI'da onay modalı şart.

### 5.5 Talep ve Teklif Akışı

Mantık:

- Alıcı ürün için talep açar.
- Satıcı sadece kendi ürünlerine gelen talepleri görür.
- Satıcı açık taleplere teklif verir.
- Alıcı bir teklifi kabul ederse talep `ANLASILDI` olur, kabul edilen teklif `KABUL`, diğer teklifler `RED` olur.
- Bu akış ödeme/kargo/sipariş tamamlamaz; sadece alıcı-satıcı anlaşma durumunu yönetir.

Alıcı ekranı:

1. Ürün detaydan `POST /api/Talep`
2. Taleplerim için `GET /api/Talep/benim`
3. Teklif kabul için `POST /api/Talep/teklif/{teklifId}/kabul`

Satıcı ekranı:

1. Gelen talepler için `GET /api/Talep/satici`
2. Teklif için `POST /api/Talep/{talepId}/teklif`

Durum değerleri:

- Talep: `ACIK`, `ANLASILDI`, `IPTAL`
- Teklif: `BEKLEMEDE`, `KABUL`, `RED`

UI önerisi:

- `ACIK`: teklif verilebilir / kabul edilebilir.
- `ANLASILDI`: son durum, aksiyon kapatılır.
- `IPTAL`: şu an API'da iptal endpointi yok; ileride UI için pasif durum olarak desteklenebilir.

### 5.6 Chat Akışı

Chat iki kanaldan çalışır:

- REST: konuşma listesi, mesaj geçmişi, REST fallback mesaj gönderme, okundu işaretleme.
- SignalR: canlı mesaj, typing, read eventleri.

Önerilen chat açılış sırası:

1. Kullanıcı login olunca SignalR bağlantısı kurulur.
2. `GET /api/Chat/conversations` ile konuşma listesi yüklenir.
3. Kullanıcı bir konuşmayı açınca `GET /api/Chat/messages/{otherUserId}?page=1&pageSize=50` çağrılır.
4. Konuşma açıldığında `MarkConversationRead(otherUserId)` veya `POST /api/Chat/messages/{otherUserId}/read` çağrılır.
5. Mesaj gönderirken tercihen SignalR `SendMessage(toUserId, message)` kullanılır.
6. SignalR bağlantısı yoksa `POST /api/Chat/messages/{receiverId}` fallback olarak kullanılabilir.

UI state önerisi:

- `MessageSentV2` gelene kadar optimistic mesaj `pending` gösterilebilir.
- `MessageSentV2` ile gerçek `id`, `sentAt`, `readAt` state'e yazılır.
- `ReceiveMessageV2` ile açık konuşmaya mesaj eklenir; konuşma açık değilse unread count artırılır.
- `MessagesRead(readerUserId, readAtUtc)` gelirse ilgili konuşmadaki kendi mesajlarınız okundu işaretlenebilir.
- `Typing(fromUserId)` kısa süreli typing indicator için kullanılabilir.

## 6. Auth API

### 6.1 Satıcı Kaydı

`POST /api/Auth/register/satici`

Auth: Yok

Body:

```json
{
  "email": "seller@example.com",
  "phoneNumber": "+905551112233",
  "password": "Secret123!",
  "magazaAdi": "Demo Mağaza",
  "vergiNo": "1234567890",
  "adres": "Kadıköy Mah. Örnek Sok. No:1",
  "sehir": "İstanbul",
  "ilce": "Kadıköy"
}
```

Validasyon:

- `email`: zorunlu, geçerli email
- `phoneNumber`: zorunlu
- `password`: zorunlu, minimum 8 karakter; Identity policy gereği büyük harf, küçük harf, rakam ve özel karakter beklenir.
- `magazaAdi`: zorunlu, minimum 3 karakter
- `vergiNo`: zorunlu
- `adres`, `sehir`, `ilce`: opsiyonel

Response:

```json
{
  "success": true,
  "message": "Satıcı kaydı başarılı.",
  "data": null,
  "traceId": "..."
}
```

Mantık:

- Satıcı kaydı sonrası email/telefon doğrulama akışı config'e bağlıdır.
- `Verification:RequireConfirmedEmailForSellerLogin=true` veya `Verification:RequireConfirmedPhoneForSellerLogin=true` ise satıcı profili `AktifMi=false` başlar.
- Zorunlu doğrulamalar tamamlanınca satıcı aktif hale gelir.
- Gerçek email/SMS servisi yoksa bu flag'ler `false` yapılır; satıcı kayıt sonrası direkt aktif/login olabilir.
- Config `Verification:RequireConfirmedEmailForSellerLogin=true` ise satıcı login için email doğrulaması gerekir.
- Config `Verification:RequireConfirmedPhoneForSellerLogin=true` ise satıcı login için telefon doğrulaması da gerekir.

### 6.2 Alıcı Kaydı

`POST /api/Auth/register/alici`

Auth: Yok

Body:

```json
{
  "email": "buyer@example.com",
  "password": "Secret123!"
}
```

Validasyon:

- `email`: zorunlu, geçerli email
- `password`: zorunlu, minimum 8 karakter; Identity policy büyük/küçük harf, rakam, özel karakter ister.

Response:

```json
{
  "success": true,
  "message": "Alıcı kaydı başarılı.",
  "data": null,
  "traceId": "..."
}
```

Not:

- Alıcı profil yönetimi için ayrı public controller yoktur; UI şu an alıcı bilgisi için `GET /api/Auth/me`, favoriler, talepler ve chat verilerini kullanır.

### 6.3 Login

`POST /api/Auth/login`

Auth: Yok

Body:

```json
{
  "email": "seller@example.com",
  "password": "Secret123!"
}
```

Response `data`:

```json
{
  "token": "jwt-token",
  "userId": "user-id",
  "email": "seller@example.com",
  "role": "SATICI",
  "roles": ["SATICI", "ALICI"]
}
```

Hata:

- Yanlış bilgi veya doğrulama eksikliği durumunda `401 Unauthorized` döner.
- UI `message` alanını gösterip kullanıcıyı formda tutmalıdır.

### 6.4 Me

`GET /api/Auth/me`

Auth: Var

Response `data`:

```json
{
  "userId": "user-id",
  "email": "seller@example.com",
  "userName": "seller@example.com",
  "phoneNumber": "+905551112233",
  "role": "SATICI",
  "roles": ["SATICI", "ALICI"],
  "emailConfirmed": true,
  "phoneNumberConfirmed": true
}
```

UI kullanımı:

- Navbar kullanıcı adı
- Rol bazlı menü
- Doğrulama uyarıları
- SignalR user id eşlemesi

### 6.5 Email Doğrulama

UI kod formu için ana endpoint:

`POST /api/Auth/confirm-email`

Auth: Yok

Body:

```json
{
  "email": "seller@example.com",
  "code": "123456"
}
```

Response:

```json
{
  "success": true,
  "message": "Email basariyla dogrulandi.",
  "data": null,
  "traceId": "..."
}
```

UI notu:

- Kullanıcıya `userId` kesinlikle sorulmaz.
- Form alanları sadece `email` ve `code` olmalıdır.
- Email mesajında hem kod hem de direkt doğrulama linki bulunur.

Direkt link fallback endpointi:

`GET /api/Auth/confirm-email?userId={userId}&token={token}`

Auth: Yok

Response:

```json
{
  "success": true,
  "message": "Email başarıyla doğrulandı.",
  "data": null,
  "traceId": "..."
}
```

UI notu:

- Link doğrudan API'ye gidiyorsa JSON döner.
- Daha iyi UX için frontend bir doğrulama sayfası açıp query parametrelerini backend'e iletebilir.
- Bu GET endpointi kullanıcı formu için değil, email linkinden tıklama senaryosu içindir.

### 6.6 Telefon Doğrulama

UI kod formu için ana endpoint:

`POST /api/Auth/confirm-phone`

Auth: Yok

Body:

```json
{
  "email": "seller@example.com",
  "code": "123456"
}
```

Response:

```json
{
  "success": true,
  "message": "Telefon basariyla dogrulandi.",
  "data": null,
  "traceId": "..."
}
```

UI notu:

- Kullanıcıya `userId` sorulmaz.
- SMS kodu girilirken email bilgisi kayıt formundan veya doğrulama ekran state'inden taşınmalıdır.

Direkt link fallback endpointi:

`GET /api/Auth/confirm-phone?userId={userId}&token={token}`

Auth: Yok

Response:

```json
{
  "success": true,
  "message": "Telefon başarıyla doğrulandı.",
  "data": null,
  "traceId": "..."
}
```

### 6.7 Dogrulama Mesajini Yeniden Gonder

`POST /api/Auth/resend-verification`

Auth: Yok

Kullanim:

- Satici kayit sonrasi kullanici email/SMS mesajini bulamazsa.
- Login denemesinde "email dogrulanmamis" veya "telefon dogrulanmamis" hatasi alirsa.
- Development ortaminda mock kutuya yeni email/SMS mesajlari dusurmek icin.

Body:

```json
{
  "email": "seller@example.com"
}
```

Response:

```json
{
  "success": true,
  "message": "Dogrulama mesaji varsa yeniden gonderildi.",
  "data": null,
  "traceId": "..."
}
```

Guvenlik davranisi:

- Email sistemde yoksa veya kullanici satici degilse yine basarili gibi cevap doner.
- Bu davranis hesap/email varligini disariya sizdirmamak icindir.
- Email ve telefon zaten dogrulanmissa yeni mesaj gonderilmez, yine basarili cevap doner.
- Eksik olan dogrulamalar icin yeniden mesaj uretilir. Ornek: email dogrulanmis ama telefon dogrulanmamis ise sadece SMS uretilir.

UI onerisi:

- Satici kayit basarili olduktan sonra "Dogrulama mesajlarini tekrar gonder" butonu koyun.
- Login `401` donup mesaj dogrulama eksigine isaret ediyorsa ayni butonu login ekraninda da gosterin.
- Development modunda buton sonrasi `/dev/verification` ekranina link verilebilir.

### 6.8 Development Mock Dogrulama Kutusu

Bu proje simdilik ucretli SMS/email servislerine bagimli kalmadan gelistirilebilsin diye Development ortaminda mock dogrulama kutusu sunar.

Ne zaman kullanilir:

- `Email:Smtp:UseMockSender=true`
- `Sms:Twilio:UseMockSender=true`
- `ASPNETCORE_ENVIRONMENT=Development`

UI sayfasi:

`GET /dev/verification`

Auth: Yok

Ortam: Sadece Development

Davranis:

- Tarayicida mock email ve SMS mesajlarini listeler.
- Satici kaydi sonrasi uretilen email dogrulama linki ve telefon dogrulama kodu burada gorunur.
- Sayfa 5 saniyede bir otomatik yenilenir.
- Mesajlar uygulama belleğinde tutulur; API yeniden baslatilirsa temizlenir.
- Production ortaminda `404 Not Found` doner.

Frontend veya test araclari icin JSON mesaj listesi:

`GET /dev/verification/messages`

Response `data`:

```json
[
  {
    "id": "9d3457e5-7cd6-4d72-b160-1d7d1ac6e6f8",
    "channel": "email",
    "to": "seller@example.com",
    "subject": "Yoremio email dogrulama",
    "body": "Yoremio email dogrulama kodunuz: 123456 ... <a href='http://localhost:5089/api/auth/confirm-email?...'>Email adresimi dogrula</a>",
    "createdAtUtc": "2026-06-29T12:30:00Z"
  },
  {
    "id": "3811d71a-29c4-44b8-98e7-1698de1696d4",
    "channel": "sms",
    "to": "+905551112233",
    "subject": null,
    "body": "Yoremio telefon dogrulama kodunuz: 123456. Dogrulama baglantisi: http://localhost:5089/api/auth/confirm-phone?...",
    "createdAtUtc": "2026-06-29T12:30:01Z"
  }
]
```

Mesaj kutusunu temizleme:

`DELETE /dev/verification/messages`

Response:

```json
{
  "success": true,
  "message": "Mock dogrulama kutusu temizlendi.",
  "data": null,
  "traceId": "..."
}
```

UI akisi:

1. Satici kayit formu `POST /api/Auth/register/satici` cagirir.
2. Basarili response sonrasi UI kullaniciya "Email ve telefon dogrulama gerekiyor" mesaji gosterir.
3. Development modunda gelistirici veya test kullanicisi `/dev/verification` ekranini acar.
4. Email mesajindaki kod `POST /api/Auth/confirm-email` ile `email + code` olarak gonderilir.
5. SMS mesajindaki kod `POST /api/Auth/confirm-phone` ile `email + code` olarak gonderilir.
6. Linke tiklama senaryosunda eski GET endpointleri de calisir, ama UI formunda `userId` istenmez.
7. Zorunlu dogrulamalar tamamlaninca satici profili aktif olur.

Gercek servis notu:

- Smtp ve Twilio ayarlari doldurulursa mock kapatilip gercek gonderim yapilabilir.
- Ucretsiz/deneme servislerin limitleri zamanla degisebildigi icin production karari verilmeden once guncel kota ve KVKK/veri isleme kosullari ayrica kontrol edilmelidir.
- Simdilik en maliyetsiz ve stabil gelistirme yolu bu mock dogrulama kutusudur.

## 7. Profil API

### 7.1 Satıcı Profilim

`GET /api/Profil/satici`

Auth: `SATICI`

Response `data`:

```json
{
  "kullaniciId": "seller-user-id",
  "magazaAdi": "Posof Organik",
  "vergiNo": "10000000002",
  "adres": "Posof Yayla Yolu 14",
  "sehir": "Ardahan",
  "ilce": "Posof",
  "kayitTarihi": "2025-09-03T09:00:00Z",
  "aktifMi": true,
  "dogrulanmisSatici": true,
  "email": "mehmet@demo.yoremio.local",
  "userName": "mehmet@demo.yoremio.local",
  "phoneNumber": "+905301000002"
}
```

UI alanları:

- Mağaza adı, vergi no, adres, şehir, ilçe
- Aktif/doğrulanmış rozetleri
- İletişim bilgileri

### 7.2 Satıcı Profil Güncelle

`PUT /api/Profil/satici`

Auth: `SATICI`

Body:

```json
{
  "magazaAdi": "Yeni Mağaza Adı",
  "adres": "Yeni adres",
  "sehir": "Ardahan",
  "ilce": "Merkez",
  "phoneNumber": "+905301000099"
}
```

Validasyon:

- `magazaAdi`: opsiyonel, gönderilirse minimum 3 karakter
- `phoneNumber`: opsiyonel, phone format validasyonu

Response:

- `SaticiProfilDto`

### 7.3 Satıcı Güven Skoru

`GET /api/Profil/satici/{saticiId}/guven-skoru`

Auth: Yok

Response `data`:

```json
{
  "kullaniciId": "seller-user-id",
  "magazaAdi": "Posof Organik",
  "dogrulanmisSatici": true,
  "urunSayisi": 4,
  "ortalamaPuan": 4.8,
  "toplamPuan": 21,
  "toplamYorum": 12,
  "toplamFavori": 30,
  "guvenSkoru": 87.5
}
```

UI kullanımı:

- Ürün detay satıcı kartı
- Satıcı profil public görünümü
- Doğrulanmış satıcı rozeti
- Güven skoru progress/ring göstergesi

### 7.4 One Cikan Saticilar

`GET /api/Profil/saticilar/one-cikan?take=6`

Auth: Yok

Response `data`:

```json
[
  {
    "kullaniciId": "seller-user-id",
    "magazaAdi": "Posof Organik",
    "sehir": "Ardahan",
    "ilce": "Posof",
    "dogrulanmisSatici": true,
    "urunSayisi": 4,
    "ortalamaPuan": 4.8,
    "toplamYorum": 12,
    "toplamFavori": 30,
    "guvenSkoru": 87.5,
    "kapakResimUrl": "/demo-media/yayla-bali/resimler/1.jpg"
  }
]
```

UI kullanimi:

- Ana sayfadaki one cikan veya dogrulanmis saticilar bandi.
- Satici kartinda magazanin kapak gorseli icin `kapakResimUrl`.
- Rozet icin `dogrulanmisSatici`, sosyal kanit icin `ortalamaPuan`, `toplamYorum`, `toplamFavori`.
- `kapakResimUrl` de urun medyasi gibi relative veya absolute olabilir; `resolveMediaUrl` helper'i kullanilmalidir.

## 8. Kategori API

Kategori modeli:

```json
{
  "id": 1,
  "adi": "Kahvaltılık",
  "aciklama": "Bal, reçel, yumurta ve kahvaltılık ürünler"
}
```

### 8.1 Kategori Listesi

`GET /api/Kategori`

Auth: Yok

Response:

- `data`: `KategoriDto[]`

UI kullanımı:

- Ana sayfa kategori chipleri
- Ürün formunda kategori select
- Filtre paneli

### 8.2 Kategori Detay

`GET /api/Kategori/{id}`

Auth: Yok

### 8.3 Kategori Oluştur

`POST /api/Kategori`

Auth: `ADMIN`

Body:

```json
{
  "adi": "Yeni Kategori",
  "aciklama": "Kategori açıklaması"
}
```

Validasyon:

- `adi`: zorunlu, minimum 2 karakter
- `aciklama`: opsiyonel

Response:

- `201 Created`
- `data`: `KategoriDto`

### 8.4 Kategori Güncelle

`PUT /api/Kategori/{id}`

Auth: `ADMIN`

Body:

```json
{
  "adi": "Güncel Kategori",
  "aciklama": "Güncel açıklama"
}
```

### 8.5 Kategori Sil

`DELETE /api/Kategori/{id}`

Auth: `ADMIN`

UI notu:

- Silme aksiyonu için onay modalı kullanılmalıdır.
- Kategoriye bağlı ürün varsa veritabanı ilişkileri nedeniyle ileride hata alınabilir; UI genel hata mesajını gösterebilmelidir.

## 9. Ürün API

### 9.1 Ürün DTO

Ürün listesi ve detay response modeli:

```json
{
  "id": 10,
  "adi": "Yayla Balı",
  "aciklama": "Posof yaylalarından toplanan çiçek balı.",
  "fiyat": 420,
  "stokMiktari": 12,
  "kategoriId": 5,
  "kategoriAdi": "Bal",
  "saticiId": "seller-user-id",
  "saticiMagazaAdi": "Posof Organik",
  "saticiSehir": "Ardahan",
  "saticiIlce": "Posof",
  "saticiDogrulanmis": true,
  "ortalamaPuan": 5,
  "toplamPuan": 1,
  "toplamYorum": 1,
  "toplamFavori": 1,
  "yorumlar": [],
  "puanlar": [],
  "resimler": [
    {
      "id": 1,
      "url": "/demo-media/yayla-bali/resimler/1.jpg"
    }
  ],
  "videolar": [
    {
      "id": 1,
      "url": "/demo-media/yayla-bali/videolar/1.mp4"
    }
  ]
}
```

### 9.2 Ürün Listele / Ara / Filtrele

`GET /api/Urun`

Auth: Yok

Query params:

| Parametre | Tip | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `q` | string | null | Ürün adı/açıklama içinde arar. |
| `kategoriId` | int | null | Kategori filtresi. |
| `minFiyat` | decimal | null | Minimum fiyat. |
| `maxFiyat` | decimal | null | Maksimum fiyat. Min/max ters gelirse backend düzeltir. |
| `saticiId` | string | null | Satıcıya göre filtre. |
| `sehir` | string | null | Satıcı şehir filtresi, contains mantığı. |
| `ilce` | string | null | Satıcı ilçe filtresi, contains mantığı. |
| `sadeceAktif` | bool | true | Aktif ürünleri getirir. |
| `sadeceStoktaOlanlar` | bool | false | Stok `> 0` ürünleri getirir. |
| `minOrtalamaPuan` | double | null | Ortalama puan alt limiti. |
| `sort` | string | newest | Sıralama. |
| `page` | int | 1 | 1'den küçükse 1 yapılır. |
| `pageSize` | int | 12 | 1'den küçükse 12, 100'den büyükse 100 yapılır. |

`sort` değerleri:

- `price_asc`
- `price_desc`
- `name_asc`
- `name_desc`
- `top_rated`
- `most_reviewed`
- `most_favorited`
- `newest`
- `oldest`

Örnek:

```http
GET /api/Urun?q=bal&kategoriId=5&minFiyat=100&maxFiyat=500&sort=top_rated&page=1&pageSize=12
```

Response `data`:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 12,
  "totalCount": 24,
  "totalPages": 2
}
```

### 9.3 Ürün Detay

`GET /api/Urun/{id}`

Auth: Yok

Response:

- `data`: `UrunDto`

Hata:

- Ürün yoksa `404 Not Found`

### 9.4 Önerilen Ürünler

`GET /api/Urun/onerilen?take=12`

Auth: `ALICI`

Mantık:

- Kullanıcının favori kategorilerine bakar.
- Favorilenmiş ürünleri hariç tutar.
- Puan, yorum, favori ve güncellik ile sıralar.
- Yeterli sonuç yoksa genel iyi ürünlerle tamamlar.
- `take` en fazla 50 olur.

Response:

- `data`: `UrunDto[]`

### 9.5 Satıcının Ürünleri

`GET /api/Urun/urunlerim`

Auth: `SATICI`

Response:

- `data`: `UrunDto[]`

### 9.6 Favorilerim

`GET /api/Urun/favorilerim`

Auth: `ALICI`

Response:

- `data`: `UrunDto[]`

UI kullanımı:

- Favori sayfası
- Ürün listesinde kalp durumunu anlamak için id set'i oluşturma

### 9.7 Favoriye Ekle

`POST /api/Urun/{urunId}/favori`

Auth: `ALICI`

Response:

```json
{
  "success": true,
  "message": "Ürün favorilere eklendi.",
  "data": null,
  "traceId": "..."
}
```

Not:

- Ürün zaten favorideyse yine `200 OK` döner, message "Ürün zaten favorilerde." olur.

### 9.8 Favoriden Çıkar

`DELETE /api/Urun/{urunId}/favori`

Auth: `ALICI`

Hata:

- Favori kaydı yoksa `404 Not Found`

### 9.9 Ürün Ekle

`POST /api/Urun/urun-ekle`

Auth: `SATICI`

Content-Type: `multipart/form-data`

Form alanları:

| Alan | Tip | Zorunlu | Açıklama |
| --- | --- | --- | --- |
| `Adi` | string | Evet | Minimum 5 karakter. |
| `Aciklama` | string | Hayır | Boşsa empty string kaydedilir. |
| `Fiyat` | decimal | Evet | 0'dan büyük olmalı. |
| `StokMiktari` | int | Evet | 0 veya daha büyük. |
| `KategoriId` | int | Evet | 1 veya daha büyük, kategori var olmalı. |
| `Resimler` | file[] | Hayır | `image/*`, her dosya max 5 MB. |
| `Videolar` | file[] | Hayır | `video/*`, her dosya max 50 MB. |

Multipart örnek mantığı:

```ts
const form = new FormData();
form.append("Adi", values.adi);
form.append("Aciklama", values.aciklama ?? "");
form.append("Fiyat", String(values.fiyat));
form.append("StokMiktari", String(values.stokMiktari));
form.append("KategoriId", String(values.kategoriId));

for (const file of imageFiles) form.append("Resimler", file);
for (const file of videoFiles) form.append("Videolar", file);
```

Response:

- `201 Created`
- `data`: `UrunDto`

### 9.10 Ürün Güncelle

`PUT /api/Urun/{urunId}`

Auth: `SATICI`

Content-Type: `multipart/form-data`

Form alanları:

| Alan | Tip | Zorunlu | Açıklama |
| --- | --- | --- | --- |
| `Adi` | string | Hayır | Gönderilirse minimum 3 karakter. |
| `Aciklama` | string | Hayır | Gönderilirse günceller. |
| `Fiyat` | decimal | Hayır | Gönderilirse 0'dan büyük olmalı. |
| `StokMiktari` | int | Hayır | Gönderilirse 0 veya daha büyük. |
| `KategoriId` | int | Hayır | Gönderilirse 1 veya daha büyük. |
| `Resimler` | file[] | Hayır | Yeni resim ekler. |
| `Videolar` | file[] | Hayır | Yeni video ekler. |

Önemli:

- `PUT` mevcut medyayı replace etmez; yeni medya ekler.
- Mevcut resim/video silmek için ilgili delete endpointleri kullanılır.

### 9.11 Urun Durumu Guncelle

`PATCH /api/Urun/{urunId}/status`

Auth: `SATICI`

Body:

```json
{
  "aktifMi": false
}
```

Response:

- `data`: `UrunDto`

UI kullanimi:

- Satici urun yonetiminde aktif/pasif toggle icin kullanilir.
- `aktifMi=false` urunu silmez; urun satici panelinde kalir, public listelemede varsayilan olarak gizlenir.
- Delete yerine once pasife alma aksiyonu one cikarilabilir; kalici silme icin ayrica onay modal'i kullanilmalidir.

### 9.12 Ürün Sil

`DELETE /api/Urun/{urunId}`

Auth: `SATICI`

Not:

- Sadece ürün sahibi satıcı silebilir.
- UI onay modalı kullanmalıdır.

### 9.13 Ürün Resmi Sil

`DELETE /api/Urun/{urunId}/resimler/{resimId}`

Auth: `SATICI`

### 9.14 Ürün Videosu Sil

`DELETE /api/Urun/{urunId}/videolar/{videoId}`

Auth: `SATICI`

## 10. Puan API

### 10.1 Puan Ver / Güncelle

`POST /api/Puan/puan-ekle`

Auth: `ALICI`

Body:

```json
{
  "urunId": 1,
  "puanDegeri": 5
}
```

Validasyon:

- `urunId`: geçerli ürün id
- `puanDegeri`: 1-5 arasında

Mantık:

- Aynı alıcı aynı ürüne tekrar puan verirse mevcut puan güncellenir.
- Ürün yoksa veya aktif değilse hata döner.

Response:

```json
{
  "success": true,
  "message": "Puan kaydedildi.",
  "data": {
    "ortalama": 5
  },
  "traceId": "..."
}
```

UI notu:

- Bu endpoint response'undaki `data.ortalama` mevcut implementasyonda kaydedilen/güncellenen puan değerini temsil eder.
- Ürünün gerçek ortalamasını göstermek için `GET /api/Puan/ortalama/{urunId}` veya `UrunDto.ortalamaPuan` kullanılmalıdır.

### 10.2 Ürün Puanları

`GET /api/Puan/urun/{urunId}`

Auth: Yok

Response `data`:

```json
[
  {
    "id": 1,
    "urunId": 10,
    "kullaniciId": "buyer-user-id",
    "puanDegeri": 5,
    "puanTarihi": "2026-03-05T08:00:00Z"
  }
]
```

### 10.3 Ortalama Puan

`GET /api/Puan/ortalama/{urunId}`

Auth: Yok

Response:

```json
{
  "success": true,
  "message": "Ortalama puan getirildi.",
  "data": {
    "ortalama": 4.7
  },
  "traceId": "..."
}
```

## 11. Yorum API

Yorum modeli:

```json
{
  "id": 1,
  "urunId": 10,
  "icerik": "Ürün çok taze geldi.",
  "tarih": "2026-03-05T08:30:00Z",
  "kullaniciId": "buyer-user-id",
  "kullaniciAdi": "buyer@example.com"
}
```

### 11.1 Yorum Ekle

`POST /api/Yorum`

Auth: `ALICI`

Body:

```json
{
  "urunId": 10,
  "icerik": "Ürün çok taze geldi."
}
```

Validasyon:

- `urunId`: 1 veya daha büyük
- `icerik`: zorunlu, minimum 3 karakter

Response:

- `data`: `YorumDto`

### 11.2 Yorum Güncelle

`PUT /api/Yorum/{yorumId}`

Auth: `ALICI`

Body:

```json
{
  "urunId": 10,
  "icerik": "Güncel yorum içeriği."
}
```

Not:

- Sadece yorumu yazan alıcı güncelleyebilir.

### 11.3 Yorum Sil

`DELETE /api/Yorum/{yorumId}`

Auth: `ALICI`

Not:

- Sadece yorumu yazan alıcı silebilir.

### 11.4 Ürün Yorumları

`GET /api/Yorum/{urunId}`

Auth: Yok

Response:

- `data`: `YorumDto[]`

## 12. Talep API

### 12.1 Talep DTO

```json
{
  "id": 1,
  "aliciId": "buyer-user-id",
  "urunId": 10,
  "urunAdi": "Yayla Balı",
  "urunResimUrl": "/demo-media/yayla-bali/resimler/1.jpg",
  "urunFiyat": 420,
  "saticiId": "seller-user-id",
  "saticiMagazaAdi": "Posof Organik",
  "saticiSehir": "Ardahan",
  "saticiIlce": "Posof",
  "miktar": 3,
  "not": "Toplu alım için fiyat rica ederim.",
  "durum": "ACIK",
  "olusturmaTarihi": "2026-03-10T08:00:00Z",
  "teklifler": [
    {
      "id": 5,
      "talepId": 1,
      "saticiId": "seller-user-id",
      "saticiMagazaAdi": "Posof Organik",
      "birimFiyat": 395,
      "mesaj": "3 kavanoz ve üzeri için indirim uygulayabilirim.",
      "durum": "BEKLEMEDE",
      "olusturmaTarihi": "2026-03-10T14:00:00Z"
    }
  ]
}
```

### 12.2 Talep Oluştur

`POST /api/Talep`

Auth: `ALICI`

Body:

```json
{
  "urunId": 10,
  "miktar": 3,
  "not": "Hafta sonu teslim alınabilir."
}
```

Validasyon:

- `urunId`: 1 veya daha büyük, ürün var ve aktif olmalı
- `miktar`: 1-100000
- `not`: opsiyonel, max 1000 karakter

Response:

- `data`: `TalepDto`

### 12.3 Alıcı Taleplerim

`GET /api/Talep/benim`

Auth: `ALICI`

Response:

- `data`: `TalepDto[]`

UI kullanımı:

- Taleplerim listesi
- Talep kartinda urun gorseli, fiyat, satici magazasi ve lokasyon bilgisi
- Talep detayında teklifler
- Teklif kabul aksiyonu

### 12.4 Satıcıya Gelen Talepler

`GET /api/Talep/satici`

Auth: `SATICI`

Mantık:

- Sadece satıcının kendi ürünlerine açılan talepler döner.

Response:

- `data`: `TalepDto[]`

### 12.5 Teklif Ver / Teklifi Güncelle

`POST /api/Talep/{talepId}/teklif`

Auth: `SATICI`

Body:

```json
{
  "birimFiyat": 395,
  "mesaj": "3 kavanoz ve üzeri için indirim uygulayabilirim."
}
```

Validasyon:

- `birimFiyat`: opsiyonel, gönderilirse 0.01 - 100000000 arası
- `mesaj`: zorunlu, max 1000 karakter

Mantık:

- Sadece `ACIK` taleplere teklif verilebilir.
- Satıcı sadece kendi ürününe ait talebe teklif verebilir.
- Aynı satıcı aynı talebe tekrar teklif verirse mevcut teklif güncellenir ve durum `BEKLEMEDE` olur.

Response:

- `data`: `TalepTeklifDto`

### 12.6 Teklif Kabul Et

`POST /api/Talep/teklif/{teklifId}/kabul`

Auth: `ALICI`

Mantık:

- Sadece talebi oluşturan alıcı kabul edebilir.
- Talep `ACIK` olmalıdır.
- Kabul sonrası talep `ANLASILDI` olur.
- Kabul edilen teklif `KABUL`, diğer teklifler `RED` olur.

Response:

- `data`: güncel `TalepDto`

## 13. Chat API

### 13.1 Chat DTO'ları

`ChatConversationDto`:

```json
{
  "userId": "other-user-id",
  "userName": "other@example.com",
  "email": "other@example.com",
  "lastMessage": "Merhaba, ürün hala stokta mı?",
  "lastSenderId": "other-user-id",
  "lastMessageAt": "2026-06-15T10:20:30Z",
  "unreadCount": 2
}
```

`ChatMessageDto`:

```json
{
  "id": 123,
  "senderId": "sender-user-id",
  "receiverId": "receiver-user-id",
  "message": "Merhaba, ürün hala stokta mı?",
  "sentAt": "2026-06-15T10:20:30Z",
  "readAt": null,
  "isMine": true
}
```

`ChatConversationMessagesDto`:

```json
{
  "otherUserId": "other-user-id",
  "items": [],
  "page": 1,
  "pageSize": 50,
  "totalCount": 120,
  "totalPages": 3
}
```

### 13.2 Konuşma Listesi

`GET /api/Chat/conversations`

Auth: Var

Mantık:

- Kullanıcının son mesajlarını tarar.
- Her karşı kullanıcı için son mesajı seçer.
- `unreadCount` sadece karşıdan gelip okunmamış mesajları sayar.

Response:

- `data`: `ChatConversationDto[]`

### 13.3 Konuşma Mesajları

`GET /api/Chat/messages/{otherUserId}?page=1&pageSize=50`

Auth: Var

Query:

- `page`: default 1
- `pageSize`: default 50, max 100

Mantık:

- İki kullanıcı arasındaki mesajları getirir.
- Repository son mesajlardan sayfa alır, response içinde kronolojik sıralı döner.
- Eski mesajları yüklemek için `page` artırılabilir.

Response:

- `data`: `ChatConversationMessagesDto`

### 13.4 REST ile Mesaj Gönder

`POST /api/Chat/messages/{receiverId}`

Auth: Var

Body:

```json
{
  "message": "Merhaba, ürün hala stokta mı?"
}
```

Validasyon:

- `message`: zorunlu, max 1000 karakter
- Kullanıcı kendisine mesaj gönderemez.
- Receiver user var olmalıdır.

Mantık:

- Mesaj DB'ye kaydedilir.
- Alıcı online ise SignalR `ReceiveMessage` ve `ReceiveMessageV2` eventleri gönderilir.
- Gönderen taraf response ile kaydedilmiş mesajı alır.

Response:

- `data`: `ChatMessageDto`

### 13.5 Konuşmayı Okundu İşaretle

`POST /api/Chat/messages/{otherUserId}/read`

Auth: Var

Response:

```json
{
  "success": true,
  "message": "Konuşma okundu olarak işaretlendi.",
  "data": {
    "markedCount": 3,
    "readAt": "2026-06-15T10:25:00Z"
  },
  "traceId": "..."
}
```

Mantık:

- Sadece karşı kullanıcıdan gelen ve `readAt=null` olan mesajları günceller.
- Karşı kullanıcı online ise `MessagesRead(readerUserId, readAtUtc)` eventini alır.

### 13.6 Bildirim API

Bildirimler login kullanicinin header badge, panel uyarilari ve aksiyon listesi icin kullanilir. Su olaylarda otomatik bildirim olusur:

- Yeni chat mesaji alan kullaniciya `MESAJ`.
- Urune talep acildiginda saticiya `TALEP`.
- Talebe teklif verildiginde aliciya `TEKLIF`.
- Teklif kabul edildiginde saticiya `TEKLIF`.

`GET /api/Bildirim?sadeceOkunmamis=false&page=1&pageSize=20`

Auth: Var

Response:

```json
{
  "success": true,
  "message": "Bildirimler getirildi.",
  "data": {
    "items": [
      {
        "id": 12,
        "tur": "TEKLIF",
        "baslik": "Yeni teklif",
        "mesaj": "Yayla Bali talebiniz icin yeni teklif var.",
        "ilgiliVarlikTuru": "TalepTeklif",
        "ilgiliVarlikId": "5",
        "aksiyonUrl": "/talepler/1",
        "olusturmaTarihi": "2026-07-08T13:20:00Z",
        "okunmaTarihi": null,
        "okunduMu": false
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1,
    "okunmamisSayisi": 1
  }
}
```

Ek endpointler:

- `GET /api/Bildirim/okunmamis-sayisi`
- `POST /api/Bildirim/{bildirimId}/okundu`
- `POST /api/Bildirim/tumunu-okundu`

## 14. Dashboard API

Dashboard endpointleri UI'nin loading/empty state ve panel kartlarini daha az istekle doldurmasi icin vardir. Liste verisi yerine sayac ve KPI doner; detay tablolar icin ilgili liste endpointleri kullanilmaya devam eder.

### 14.1 Kullanici Ozeti

`GET /api/Dashboard/summary`

Auth: Login kullanici

Response `data`:

```json
{
  "roles": ["ALICI"],
  "unreadMessages": 3,
  "favoriteProducts": 5,
  "myProducts": 0,
  "openDemands": 2,
  "buyerOpenDemands": 2,
  "sellerOpenDemands": 0,
  "pendingOffers": 1,
  "buyerPendingOffers": 1,
  "sellerPendingOffers": 0
}
```

UI gorevleri:

- Header mesaj badge'i: `unreadMessages`
- Favoriler bos state: `favoriteProducts === 0`
- Taleplerim bos state: `buyerOpenDemands === 0`
- Satici gelen talepler bos state: `sellerOpenDemands === 0`
- Alici teklif bekleyen durum badge'i: `buyerPendingOffers`
- Satici teklif bekleyen durum badge'i: `sellerPendingOffers`
- Rol bazli nav: `roles`

### 14.2 Satici Dashboard

`GET /api/Dashboard/satici`

Auth: `SATICI`

Response `data`:

```json
{
  "totalProducts": 12,
  "activeProducts": 10,
  "inactiveProducts": 2,
  "outOfStockProducts": 1,
  "totalFavorites": 34,
  "totalReviews": 8,
  "totalRatings": 10,
  "averageRating": 4.6,
  "trustScore": 86.5,
  "openDemands": 4,
  "agreedDemands": 3,
  "pendingOffers": 2,
  "acceptedOffers": 3,
  "rejectedOffers": 1,
  "unreadMessages": 5
}
```

UI gorevleri:

- Satici ana panel KPI kartlari.
- Urun yonetimi tab'lerinde aktif/pasif/stokta yok sayaclari.
- Talep/teklif badge'leri.
- Guven skoru progress/rozet gosterimi.
- Mesaj merkezi okunmamis sayaci.

### 14.3 Admin Dashboard

`GET /api/Dashboard/admin`

Auth: `ADMIN`

Response `data`:

```json
{
  "totalUsers": 120,
  "totalSellers": 34,
  "activeSellers": 28,
  "totalBuyers": 86,
  "totalProducts": 210,
  "activeProducts": 190,
  "inactiveProducts": 20,
  "totalDemands": 45,
  "openDemands": 18,
  "agreedDemands": 20,
  "totalReviews": 72,
  "totalMessages": 410,
  "unreadMessages": 12
}
```

UI gorevleri:

- Admin ana panel KPI kartlari.
- Kullanici/satici/urun/moderasyon ekranlarina hizli link kartlari.
- Operasyonel bos durum ve uyari kararlarinda sayac kaynagi.

## 15. SignalR Chat Hub

Hub URL:

```text
/chathub
```

Auth:

- JWT gerekir.
- Browser SignalR client için token `accessTokenFactory` ile verilebilir.
- API ayrıca `/chathub?access_token=<jwt>` query kullanımını destekler.

TypeScript bağlantı örneği:

```ts
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${API_BASE_URL}/chathub`, {
    accessTokenFactory: () => authToken
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

Client -> Server methodları:

| Method | Parametreler | Açıklama |
| --- | --- | --- |
| `SendMessage` | `toUserId`, `message` | Ana canlı mesaj gönderme metodu. |
| `SendMessageLegacy` | `fromUserId`, `toUserId`, `message` | Eski istemciler için uyumluluk. `fromUserId` token kullanıcısıyla aynı olmalı. |
| `MarkConversationRead` | `otherUserId` | Konuşmayı okundu işaretler. |
| `Typing` | `toUserId` | Karşı tarafa typing event gönderir. |

Server -> Client eventleri:

| Event | Payload | Açıklama |
| --- | --- | --- |
| `Connected` | `userId` | Bağlantı kurulduğunda caller'a döner. |
| `ReceiveMessage` | `fromUserId`, `message` | Legacy receive event. |
| `MessageSent` | `toUserId`, `message`, `sentAtUtc` | Legacy sent confirmation. |
| `ReceiveMessageV2` | `ChatMessageDto` | Yeni istemciler için tam mesaj DTO'su. |
| `MessageSentV2` | `ChatMessageDto` | Gönderen tarafa tam mesaj DTO'su. |
| `ConversationRead` | `otherUserId`, `markedCount`, `readAtUtc` | Caller'ın okundu işaretleme sonucu. |
| `MessagesRead` | `readerUserId`, `readAtUtc` | Karşı taraf mesajlarınızı okudu. |
| `Typing` | `fromUserId` | Karşı kullanıcı yazıyor. |

Önerilen frontend event setup:

```ts
connection.on("ReceiveMessageV2", (message) => {
  // Açık konuşmadaysa append et, değilse conversation unread artır.
});

connection.on("MessageSentV2", (message) => {
  // Optimistic/pending mesajı gerçek id ve sentAt ile değiştir.
});

connection.on("MessagesRead", (readerUserId, readAtUtc) => {
  // readerUserId ile olan konuşmada kendi mesajlarını readAt ile işaretle.
});

connection.on("Typing", (fromUserId) => {
  // Kısa süreli typing indicator göster.
});
```

Hub hata davranışı:

- İş kuralı hataları `HubException` olarak döner.
- UI send sırasında try/catch ile mesajı failed state'e almalıdır.

## 16. Development Seed

Development ortamında migration ve seed otomatik çalışır.

Seed içerikleri:

- Roller: `SATICI`, `ALICI`
- Test kullanıcıları
- Kategoriler
- Demo ürünler
- Ürün resimleri/videoları
- Yorumlar
- Puanlar
- Favoriler
- Talep ve teklif kayıtları

Tüm seed kullanıcıları için şifre:

```text
Test123!
```

Satıcılar:

- `ayse@demo.yoremio.local`
- `mehmet@demo.yoremio.local`
- `zeynep@demo.yoremio.local`

Alıcılar:

- `elif@demo.yoremio.local`
- `can@demo.yoremio.local`
- `selin@demo.yoremio.local`

Seed kategoriler:

- `Sebze`
- `Meyve`
- `Sut Urunleri`
- `Bakliyat`
- `Kahvaltilik`

Seed ürün medya klasörü:

```text
API/wwwroot/demo-media/
```

Medya dosyalarını yeniden indirmek için:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Download-SeedMedia.ps1
```

Not:

- API yeniden başlatıldığında seed ürünleri bu klasördeki medya URL'leriyle senkronize edilir.
- Seed ürünlerde eski demo medya kayıtları temizlenir ve güncel `demo-media` yolları tutulur.

## 17. Status Code ve Hata Mantığı

Yaygın status code'lar:

| Status | Anlam | UI davranışı |
| --- | --- | --- |
| `200 OK` | İşlem başarılı | `data` kullanılır, gerekirse `message` gösterilir. |
| `201 Created` | Kayıt oluşturuldu | Yeni kayda yönlendirme yapılabilir. |
| `400 Bad Request` | Validation veya iş kuralı hatası | Form hatası/toast gösterilir. |
| `401 Unauthorized` | Token yok/geçersiz/login başarısız | Token temizlenir, login istenir. |
| `403 Forbidden` | Rol/yetki yok | Yetkisiz ekranı gösterilir. |
| `404 Not Found` | Kayıt bulunamadı | Boş durum veya detay bulunamadı sayfası. |
| `429 Too Many Requests` | Rate limit | Kısa bekleme uyarısı gösterilir. |
| `500 Internal Server Error` | Beklenmeyen sunucu hatası | Genel hata mesajı + traceId gösterilir. |

Global exception mapping:

- `ArgumentException` -> `400`
- `UnauthorizedAccessException` -> `403`
- `KeyNotFoundException` -> `404`
- Diğer hatalar -> `500`

Production ortamında beklenmeyen `500` detayları gizlenir; UI `message` ve `traceId` göstermelidir.

## 18. Endpoint Özet Tablosu

### App

| Method | Endpoint | Auth | Rol | Aciklama |
| --- | --- | --- | --- | --- |
| `GET` | `/api/App/bootstrap` | Hayir | - | UI baslangic sozlesmesi, kategori, feature flag, upload limitleri |

### Dashboard

| Method | Endpoint | Auth | Rol | Aciklama |
| --- | --- | --- | --- | --- |
| `GET` | `/api/Dashboard/summary` | Evet | Any | Header badge, bos durum ve kisa kullanici ozeti |
| `GET` | `/api/Dashboard/satici` | Evet | `SATICI` | Satici panel KPI kartlari |
| `GET` | `/api/Dashboard/admin` | Evet | `ADMIN` | Admin panel KPI kartlari |

## Complete Project Logic Guide

Bu bolum projenin nasil dusunuldugunu anlatir. Frontend, mobil, test ve deployment ekipleri bu bolumu "urun mantigi" sozlesmesi olarak okuyabilir.

### Product Scope

Yoremio bir organik/yerel urun pazar yeri API'sidir. Platformun temel amaci alici ile yerel saticiyi bulusturmak, urun kesfi yapmak, talep/teklif uzerinden anlasma kaydi tutmak ve canli mesajlasma saglamaktir.

Kapsam icinde olanlar:

- Kullanici kaydi, login ve JWT tabanli oturum.
- Alici, satici ve admin rolleri.
- Satici profil dogrulama ve guven skoru.
- Kategori ve urun yonetimi.
- Urun medya yukleme/silme.
- Urun listeleme, arama, filtreleme, siralama ve oneriler.
- Favori, talep, teklif, yorum, puan.
- REST + SignalR tabanli chat.
- Development icin mock email/SMS dogrulama kutusu.

Kapsam disinda olanlar:

- Odeme alma.
- Kargo/siparis takibi.
- Fatura/e-arsiv.
- Stok rezervasyonu.
- Iade/iptal sureci.
- Tam moderasyon workflow'u. `ADMIN` rolu su an dashboard ozeti ve kategori yonetimi icin kullanilir.

### Architecture

Solution katmanlari:

| Katman | Klasor | Sorumluluk |
| --- | --- | --- |
| API | `API/` | Controller, middleware, auth pipeline, CORS, rate limit, health, SignalR map. |
| Application | `Application/` | DTO, servis arayuzleri, validation attribute'lari. |
| Domain | `Domain/` | Entity, role/durum sabitleri, repository arayuzleri, query modelleri. |
| Infrastructure | `Infrastructure/` | EF Core context, repository implementasyonlari, servis implementasyonlari, email/SMS/dosya/chat altyapisi. |
| Tests | `Tests/` | Console test runner ve chat e2e runner. |

Backend request akisi:

1. Request API controller veya SignalR hub'a gelir.
2. Auth gerekiyorsa JWT middleware token'i dogrular.
3. Controller kullanici id/rol bilgisini claim'lerden alir.
4. Application interface uzerinden Infrastructure service cagrilir.
5. Service is kuralini uygular.
6. Repository EF Core ile PostgreSQL verisine erisir.
7. Controller `ApiResponse<T>` envelope ile cevap doner.

### Core Domain Model

| Entity | Mantik |
| --- | --- |
| `ApplicationUser` | Identity kullanicisi. Email, phone, confirmed flag'leri ve roller burada yonetilir. |
| `SaticiProfili` | Saticinin magazasi, vergi no, adres, sehir/ilce, aktiflik ve kayit tarihi. |
| `AliciProfili` | Alicinin adres/favori kategori gibi temel profil bilgileri. |
| `Kategori` | Urun taksonomisi. Yazma yetkisi `ADMIN`. |
| `Urun` | Saticiya ait urun. Kategori, fiyat, stok, aktiflik, medya, puan, yorum ve favorilerle iliskilidir. |
| `UrunResim` / `UrunVideo` | Urun medya URL kayitlari. Local static veya Cloudinary absolute URL olabilir. |
| `UrunFavori` | Alici kullanicinin favori urun iliskisi. Kullanici-urun unique tutulur. |
| `Talep` | Alicinin bir urun icin actigi talep. `ACIK`, `ANLASILDI`, `IPTAL`. |
| `TalepTeklif` | Saticinin talebe verdigi teklif. `BEKLEMEDE`, `KABUL`, `RED`. |
| `Yorum` | Alicinin urune yorumu. Production kuralinda sadece anlasilmis talep sonrasi eklenebilir. |
| `Puan` | Alicinin urune 1-5 puani. Kullanici-urun unique; production kuralinda sadece anlasilmis talep sonrasi eklenebilir. |
| `ChatMessage` | Iki kullanici arasindaki kalici mesaj. `ReadAt` okundu bilgisini tasir. |

Onemli iliskiler:

- Bir `ApplicationUser` en fazla bir `SaticiProfili` ve bir `AliciProfili` tasir.
- Satici kaydinda kullanici hem `SATICI` hem `ALICI` rolu alabilir.
- Bir `Urun`, bir `SaticiProfili` ve bir `Kategori` ile iliskilidir.
- Bir `Talep`, alici ve urun uzerinden saticiya baglanir.
- Bir `TalepTeklif`, talep ve satici arasinda unique kabul edilir.
- Bir `Puan`, ayni kullanici ve urun icin tek kayittir; tekrar puanlama guncelleme mantigiyla calisir.

### Roles And Permissions

| Rol | Yapabilir | Yapamaz / Not |
| --- | --- | --- |
| Anonymous | Public kategori, urun, yorum/puan listesi, guven skoru gorebilir. | Favori, talep, chat, yorum, puan, urun/profil yonetimi yapamaz. |
| `ALICI` | Favori, onerilenler, talep, teklif kabul, chat, anlasilmis alim sonrasi yorum/puan. | Urun ekleyemez, satici profili yonetemez, kategori yazamaz. |
| `SATICI` | Profil, urun, medya, gelen talep, teklif, chat. | Kategori yazamaz. |
| `ADMIN` | Admin dashboard ozeti ve kategori olusturma/guncelleme/silme. | Urun sahipligi veya satici yerine islem yapma endpointi yoktur. |

Frontend kurali:

- Yetki kontrolunde `roles` listesini esas alin.
- `role` alani sadece geriye uyumluluk icindir.
- Bir kullanicida birden fazla rol olabilir.

### Authentication And Verification Lifecycle

Satici kaydi:

1. UI `POST /api/Auth/register/satici` cagirir.
2. Identity kullanicisi olusturulur.
3. `SaticiProfili` olusturulur ve `AktifMi=false` baslar.
4. Kullaniciya `SATICI` ve `ALICI` rolleri verilir.
5. Email dogrulama kodu/linki ve telefon dogrulama kodu/linki uretilir.
6. Mock sender aciksa mesajlar `/dev/verification` kutusuna duser.
7. Email ve telefon dogrulaninca satici profili aktif olur.

Alici kaydi:

1. UI `POST /api/Auth/register/alici` cagirir.
2. Identity kullanicisi olusturulur.
3. `ALICI` rolu atanir.
4. Alici hemen login olabilir.

Login:

1. UI `POST /api/Auth/login` cagirir.
2. Email/password kontrol edilir.
3. `Verification:RequireConfirmedEmailForSellerLogin=true` ise satici email dogrulamasi zorunludur.
4. `Verification:RequireConfirmedPhoneForSellerLogin=true` ise satici telefon dogrulamasi da zorunludur.
5. JWT icine user id, email, username ve tum roller yazilir.

Yeniden dogrulama:

- `POST /api/Auth/resend-verification` email alir.
- Kullanici yoksa veya satici degilse yine basarili cevap doner.
- Eksik dogrulamalar icin yeniden email/SMS uretilir.
- Bu davranis hesap varligini disari sizdirmemek icindir.

### App Bootstrap Lifecycle

UI acilisinda onerilen sira:

1. `GET /api/App/bootstrap`
2. Token varsa `GET /api/Auth/me`
3. Token gecerliyse `GET /api/Dashboard/summary`
4. Public ana sayfa icin `GET /api/Urun`
5. Login kullanici aliciysa favori/onerilen/talep/chat verileri.
6. Login kullanici saticiysa `GET /api/Dashboard/satici`, profil/urunlerim/gelen talepler/chat verileri.

Bootstrap UI'a sunlari verir:

- Rollerin listesi.
- Kategoriler.
- Urun siralama secenekleri.
- Feature flag'ler.
- Dogrulama ayarlari.
- Upload limitleri.

### Product And Media Lifecycle

Urun ekleme:

1. `SATICI` `POST /api/Urun/urun-ekle` multipart/form-data gonderir.
2. Kategori varligi kontrol edilir.
3. Urun kaydi olusturulur.
4. Resim/video dosyalari kaydedilir.
5. Medya URL'leri `UrunResim` / `UrunVideo` olarak eklenir.

Urun guncelleme:

- Sadece urun sahibi satici guncelleyebilir.
- `PUT /api/Urun/{urunId}` ile gonderilen yeni medya mevcut medyaya eklenir.
- Eski medyayi kaldirmak icin delete medya endpointleri kullanilir.

Urun aktif/pasif:

- `PATCH /api/Urun/{urunId}/status` sadece urun sahibi satici tarafindan kullanilir.
- `aktifMi=false` urunu silmez; public kesif listesinde gizler.
- UI delete aksiyonundan once pasife alma secenegini one cikarmalidir.

Medya storage:

- Local storage aktifse dosya `wwwroot` altina guvenli path kontroluyle yazilir.
- Cloudinary aktifse yeni uploadlar Cloudinary URL'i olarak kaydedilir.
- UI relative URL'leri API base URL ile birlestirmelidir.

Upload guvenligi:

- Resim: content type `image/`, izinli uzanti ve dosya imzasi gerekir.
- Video: content type `video/`, izinli uzanti ve dosya imzasi gerekir.
- Maksimum multipart boyutu `100 MB`.

### Discovery, Search And Recommendation Logic

`GET /api/Urun` public urun kesif endpointidir.

Filtreler:

- Metin: `q`
- Kategori: `kategoriId`
- Fiyat: `minFiyat`, `maxFiyat`
- Satici: `saticiId`
- Lokasyon: `sehir`, `ilce`
- Stok: `sadeceStoktaOlanlar`
- Puan: `minOrtalamaPuan`
- Aktiflik: `sadeceAktif`

Siralama:

- `newest`, `oldest`
- `price_asc`, `price_desc`
- `name_asc`, `name_desc`
- `top_rated`
- `most_reviewed`
- `most_favorited`

Onerilen urunler:

- `GET /api/Urun/onerilen`
- Alicinin favori kategorileri dikkate alinir.
- Zaten favorilenen urunler oneriden dislanir.
- Eksik kalirsa genel populer/puanli urunlerle tamamlanir.

### Demand And Offer Lifecycle

Talep akisi platformun siparis yerine kullandigi anlasma kaydidir.

1. Alici urun detayindan `POST /api/Talep` ile talep acar.
2. Talep `ACIK` baslar.
3. Satici sadece kendi urunlerine gelen talepleri `GET /api/Talep/satici` ile gorur.
4. Satici `POST /api/Talep/{talepId}/teklif` ile teklif verir veya mevcut teklifini gunceller.
5. Alici `POST /api/Talep/teklif/{teklifId}/kabul` ile teklifi kabul eder.
6. Talep `ANLASILDI` olur.
7. Kabul edilen teklif `KABUL`, diger teklifler `RED` olur.

Onemli:

- Bu akis odeme veya kargo baslatmaz.
- UI metinlerinde "siparis tamamlandi" yerine "anlasildi" kullanin.
- Yorum/puan icin bu anlasma kaydi gerekir.

### Reviews, Ratings And Trust Logic

Yorum/puan kurali:

- Kullanici `ALICI` rolunde olmalidir.
- Ilgili urun aktif/var olmalidir.
- Ayni urun icin kullanicinin `ANLASILDI` talebi ve kabul edilmis teklifi olmalidir.

Puan:

- Deger `1-5` araligindadir.
- Ayni kullanici-urun icin tek puan vardir.
- Tekrar puanlama mevcut kaydi gunceller.

Guven skoru:

- Public endpoint: `GET /api/Profil/satici/{saticiId}/guven-skoru`
- Ortalama puan, yorum sayisi, favori sayisi, kayit suresi ve dogrulanmis satici bilgisiyle hesaplanir.
- UI bunu satici kartinda rozet/progress olarak gosterebilir.

### Chat Logic

Chat iki katmanlidir:

- REST: konusma listesi, mesaj gecmisi, fallback mesaj gonderme, okundu isaretleme.
- SignalR: canli mesaj, typing, okundu eventleri.

SignalR connection:

- Hub yolu: `/chathub`
- JWT query string ile `access_token` olarak verilebilir.
- User id `ClaimTypes.NameIdentifier` uzerinden SignalR user id olur.

Hub methodlari:

- `SendMessage(toUserId, message)`
- `SendMessageLegacy(fromUserId, toUserId, message)`
- `MarkConversationRead(otherUserId)`
- `Typing(toUserId)`

Hub eventleri:

- `Connected(userId)`
- `ReceiveMessage(fromUserId, message)`
- `MessageSent(toUserId, message, sentAtUtc)`
- `ReceiveMessageV2(ChatMessageDto)`
- `MessageSentV2(ChatMessageDto)`
- `ConversationRead(otherUserId, markedCount, readAtUtc)`
- `MessagesRead(readerUserId, readAtUtc)`
- `Typing(fromUserId)`

Mesaj kurallari:

- Kullanici kendisine mesaj atamaz.
- Mesaj bos olamaz.
- Maksimum mesaj uzunlugu `1000`.
- Alici kullanici var olmalidir.

### Error, Rate Limit And Observability

Global exception mapping:

- `ArgumentException` -> `400`
- `UnauthorizedAccessException` -> `403`
- `KeyNotFoundException` -> `404`
- Beklenmeyen hata -> `500`

HTTP behavior:

- `401`: token yok, gecersiz veya login basarisiz.
- `403`: token var ama rol/is kuralina yetki yok.
- `404`: kaynak yok.
- `429`: global rate limit asildi.

Logging:

- Console logging single-line format kullanir.
- `X-Correlation-Id` request/response header olarak desteklenir.
- `traceId` UI destek akisi icin saklanabilir.

### Development Seed And Demo Accounts

Development ortaminda seed aciksa uygulama baslangicinda su veriler olusturulur:

- Roller: `ADMIN`, `SATICI`, `ALICI`
- Admin: `admin@demo.yoremio.local`
- Saticilar: `ayse@demo.yoremio.local`, `mehmet@demo.yoremio.local`, `zeynep@demo.yoremio.local`
- Alicilar: `elif@demo.yoremio.local`, `can@demo.yoremio.local`, `selin@demo.yoremio.local`
- Kategoriler, urunler, medya, favoriler, puanlar, yorumlar, talepler ve teklifler.

Tum demo kullanicilarin sifresi:

- `Test123!`

### Production Deployment Rules

Production icin zorunlu kurallar:

- `Jwt:Key` environment/secret manager ile verilmeli.
- `ConnectionStrings:DefaultConnection` gercek PostgreSQL bilgisi olmali.
- `CHANGE_ME` placeholder kalirsa uygulama acilmaz.
- `Startup:SeedSampleData=false` olmali.
- Migration uygulamasi pipeline veya kontrollu startup ayariyla yonetilmeli.
- Local `wwwroot` uploadlari kalici disk olmayan ortamlarda kullanilmamali; Cloudinary acilmali.
- SMTP/SMS gercek servis ayarlari secret olarak verilmeli.
- Gercek SMTP/SMS baglanana kadar production demo ortaminda `Verification:RequireConfirmedEmailForSellerLogin=false` ve `Verification:RequireConfirmedPhoneForSellerLogin=false` kullanilabilir.
- Gercek SMTP/SMS baglandiginda `Email:Smtp:UseMockSender=false`, `Sms:Twilio:UseMockSender=false` ve iki dogrulama flag'i `true` yapilmalidir.
- CORS sadece gercek frontend originlerini icermeli.

### Frontend Screen Contract

Minimum ekranlar:

- Public home: kategori, urun liste, filtre, pagination.
- Urun detay: medya galeri, satici karti, guven skoru, puan/yorum, favori, talep, chat.
- Auth: login, alici kayit, satici kayit, dogrulama bekleme, dogrulama sonuc.
- Alici paneli: favoriler, onerilenler, taleplerim, chat.
- Satici paneli: dashboard, profil, urunlerim, urun formu, aktif/pasif durum, medya yonetimi, gelen talepler, teklif formu, chat.
- Admin paneli: dashboard, kategori liste, kategori ekle/guncelle/sil.
- Development ekran: mock dogrulama kutusu.

UI state kurallari:

- Token varsa acilista `GET /api/Auth/me` ile dogrula.
- Yetki icin `roles` kullan.
- Upload limitleri icin bootstrap `uploads` alanini kullan.
- Feature flag kapaliysa aksiyonu gizle.
- `traceId` hata destek ekraninda gosterilebilir.

## Production Hardening Notes

Bu bolum UI ve deployment tarafinin yeni production kurallarini tek yerden gormesi icindir.

### Roles

- `ADMIN`: dashboard ozeti ve kategori olusturma/guncelleme/silme gibi sistemsel yonetim isleri.
- `SATICI`: profil, urun, talep/teklif, chat.
- `ALICI`: kesif, favori, talep, chat, dogrulanmis alim sonrasi yorum/puan.
- Kategori yazma endpointleri artik `ADMIN` ister; satici panelinde kategori CRUD gizlenmelidir.

### Verified Reviews And Ratings

- `POST /api/Yorum` ve `POST /api/Puan/puan-ekle` icin kullanicinin ilgili urunde `ANLASILDI` durumunda talebi ve kabul edilmis teklifi olmalidir.
- Bu kural sahte yorum/puan riskini azaltir.
- UI, henuz anlasilmis talebi olmayan kullanicida yorum/puan formunu pasif gostermelidir.

### Upload Security

- Resim yuklemeleri sadece `image/` content type, izinli uzanti ve dosya imzasi eslesirse kabul edilir.
- Izinli resim uzantilari: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`.
- Video yuklemeleri sadece `video/` content type, izinli uzanti ve dosya imzasi eslesirse kabul edilir.
- Izinli video uzantilari: `.mp4`, `.webm`, `.mov`.
- UI, `GET /api/App/bootstrap` icindeki `uploads` limitlerini form validasyonunda kullanmalidir.

### Secrets And Config

- `API/appsettings.json` artik gercek secret tasimaz; `CHANGE_ME` placeholder kullanir.
- Production ortaminda `Jwt:Key` veya `ConnectionStrings:DefaultConnection` placeholder kalirsa uygulama acilista hata verir.
- Gercek secretlar environment variable, user-secrets veya deployment secret manager ile verilmelidir.

### CI

- `.github/workflows/ci.yml` restore, release build ve test runner adimlarini calistirir.
- Pull request kapisinda build/test kirmadan merge edilmemelidir.

### Auth

| Method | Endpoint | Auth | Rol | Açıklama |
| --- | --- | --- | --- | --- |
| `POST` | `/api/Auth/register/satici` | Hayır | - | Satıcı kaydı |
| `POST` | `/api/Auth/register/alici` | Hayır | - | Alıcı kaydı |
| `POST` | `/api/Auth/login` | Hayır | - | JWT login |
| `GET` | `/api/Auth/me` | Evet | Any | Aktif kullanıcı |
| `POST` | `/api/Auth/confirm-email` | Hayır | - | Email kod doğrulama (`email + code`) |
| `POST` | `/api/Auth/confirm-phone` | Hayır | - | Telefon kod doğrulama (`email + code`) |
| `GET` | `/api/Auth/confirm-email` | Hayır | - | Email link fallback doğrulama |
| `GET` | `/api/Auth/confirm-phone` | Hayır | - | Telefon link fallback doğrulama |

Auth ek endpoint:

| Method | Endpoint | Auth | Rol | Aciklama |
| --- | --- | --- | --- | --- |
| `POST` | `/api/Auth/resend-verification` | Hayir | - | Satici email/SMS dogrulama mesajini yeniden gonder |

### Development Verification

| Method | Endpoint | Auth | Rol | Aciklama |
| --- | --- | --- | --- | --- |
| `GET` | `/dev/verification` | Hayir | - | Development mock email/SMS kutusu HTML arayuzu |
| `GET` | `/dev/verification/messages` | Hayir | - | Mock dogrulama mesajlari JSON listesi |
| `DELETE` | `/dev/verification/messages` | Hayir | - | Mock dogrulama kutusunu temizle |

### Dashboard

| Method | Endpoint | Auth | Rol | Aciklama |
| --- | --- | --- | --- | --- |
| `GET` | `/api/Dashboard/summary` | Evet | Any | Header badge, bos durum ve kisa kullanici ozeti |
| `GET` | `/api/Dashboard/satici` | Evet | `SATICI` | Satici panel KPI kartlari |
| `GET` | `/api/Dashboard/admin` | Evet | `ADMIN` | Admin panel KPI kartlari |

### Profil

| Method | Endpoint | Auth | Rol | Açıklama |
| --- | --- | --- | --- | --- |
| `GET` | `/api/Profil/satici` | Evet | `SATICI` | Satıcı profilim |
| `PUT` | `/api/Profil/satici` | Evet | `SATICI` | Satıcı profil güncelle |
| `GET` | `/api/Profil/saticilar/one-cikan` | Hayir | - | Ana sayfa icin one cikan saticilar |
| `GET` | `/api/Profil/satici/{saticiId}/guven-skoru` | Hayır | - | Public satıcı güven skoru |

### Kategori

| Method | Endpoint | Auth | Rol | Açıklama |
| --- | --- | --- | --- | --- |
| `GET` | `/api/Kategori` | Hayır | - | Kategori listesi |
| `GET` | `/api/Kategori/{id}` | Hayır | - | Kategori detay |
| `POST` | `/api/Kategori` | Evet | `ADMIN` | Kategori oluştur |
| `PUT` | `/api/Kategori/{id}` | Evet | `ADMIN` | Kategori güncelle |
| `DELETE` | `/api/Kategori/{id}` | Evet | `ADMIN` | Kategori sil |

### Ürün

| Method | Endpoint | Auth | Rol | Açıklama |
| --- | --- | --- | --- | --- |
| `GET` | `/api/Urun` | Hayır | - | Ürün liste/arama |
| `GET` | `/api/Urun/{id}` | Hayır | - | Ürün detay |
| `GET` | `/api/Urun/onerilen` | Evet | `ALICI` | Önerilen ürünler |
| `GET` | `/api/Urun/urunlerim` | Evet | `SATICI` | Satıcının ürünleri |
| `GET` | `/api/Urun/favorilerim` | Evet | `ALICI` | Favori ürünler |
| `POST` | `/api/Urun/{urunId}/favori` | Evet | `ALICI` | Favoriye ekle |
| `DELETE` | `/api/Urun/{urunId}/favori` | Evet | `ALICI` | Favoriden çıkar |
| `POST` | `/api/Urun/urun-ekle` | Evet | `SATICI` | Ürün ekle |
| `PUT` | `/api/Urun/{urunId}` | Evet | `SATICI` | Ürün güncelle |
| `PATCH` | `/api/Urun/{urunId}/status` | Evet | `SATICI` | Ürün aktif/pasif durumu |
| `DELETE` | `/api/Urun/{urunId}` | Evet | `SATICI` | Ürün sil |
| `DELETE` | `/api/Urun/{urunId}/resimler/{resimId}` | Evet | `SATICI` | Ürün resmi sil |
| `DELETE` | `/api/Urun/{urunId}/videolar/{videoId}` | Evet | `SATICI` | Ürün videosu sil |

### Puan

| Method | Endpoint | Auth | Rol | Açıklama |
| --- | --- | --- | --- | --- |
| `POST` | `/api/Puan/puan-ekle` | Evet | `ALICI` | Puan ver/güncelle |
| `GET` | `/api/Puan/urun/{urunId}` | Hayır | - | Ürün puanları |
| `GET` | `/api/Puan/ortalama/{urunId}` | Hayır | - | Ortalama puan |

### Yorum

| Method | Endpoint | Auth | Rol | Açıklama |
| --- | --- | --- | --- | --- |
| `POST` | `/api/Yorum` | Evet | `ALICI` | Yorum ekle |
| `PUT` | `/api/Yorum/{yorumId}` | Evet | `ALICI` | Yorum güncelle |
| `DELETE` | `/api/Yorum/{yorumId}` | Evet | `ALICI` | Yorum sil |
| `GET` | `/api/Yorum/{urunId}` | Hayır | - | Ürün yorumları |

### Talep

| Method | Endpoint | Auth | Rol | Açıklama |
| --- | --- | --- | --- | --- |
| `POST` | `/api/Talep` | Evet | `ALICI` | Talep oluştur |
| `GET` | `/api/Talep/benim` | Evet | `ALICI` | Alıcı talepleri |
| `GET` | `/api/Talep/satici` | Evet | `SATICI` | Satıcıya gelen talepler |
| `POST` | `/api/Talep/{talepId}/teklif` | Evet | `SATICI` | Teklif ver/güncelle |
| `POST` | `/api/Talep/teklif/{teklifId}/kabul` | Evet | `ALICI` | Teklif kabul |

### Chat

| Method | Endpoint | Auth | Rol | Açıklama |
| --- | --- | --- | --- | --- |
| `GET` | `/api/Chat/conversations` | Evet | Any | Konuşma listesi |
| `GET` | `/api/Chat/messages/{otherUserId}` | Evet | Any | Mesaj geçmişi |
| `POST` | `/api/Chat/messages/{receiverId}` | Evet | Any | REST mesaj gönder |
| `POST` | `/api/Chat/messages/{otherUserId}/read` | Evet | Any | Konuşmayı okundu işaretle |
| SignalR | `/chathub` | Evet | Any | Canlı chat |

## 19. UI Tasarım Checklist'i

Bu dokümana göre UI çıkarırken minimum ekran seti:

- Public ana sayfa: kategori + ürün liste + filtre + pagination
- Ürün detay: galeri + satıcı kartı + puan/yorum + talep/chat/favori aksiyonları
- Auth: login, alıcı kayıt, satıcı kayıt, doğrulama sonuç ekranı
- Alıcı paneli: önerilenler, favorilerim, taleplerim, chat
- Satıcı paneli: dashboard, profil, ürünlerim, ürün formu, aktif/pasif durum, medya yönetimi, gelen talepler, teklif formu, chat
- Admin paneli: dashboard, kategori liste, kategori ekle/güncelle/sil
- Ortak: global loading, empty state, error state, 401/403/429/500 davranışları

Frontend tarafında özellikle unutulmaması gerekenler:

- Tüm response'larda `success/message/data/errors/traceId` envelope bekleyin.
- Login sonrasi header badge ve bos state kararlarini `GET /api/Dashboard/summary` ile besleyin.
- Satici paneli KPI kartlarini `GET /api/Dashboard/satici` ile, admin paneli KPI kartlarini `GET /api/Dashboard/admin` ile doldurun.
- Satici urun listesinde kalici silme yerine once `PATCH /api/Urun/{urunId}/status` aktif/pasif toggle'ini one cikarin.
- Upload endpointlerinde JSON değil `multipart/form-data` kullanın.
- Upload alan adlarını backend DTO adlarıyla gönderin: `Adi`, `Fiyat`, `Resimler`, `Videolar`.
- Media URL'leri local/demo için relative, Cloudinary için absolute gelebilir; `http` ile başlıyorsa direkt kullanın, değilse API base URL ile birleştirin.
- Satıcı login için email + telefon doğrulama gereksinimini UI metninde açıklayın.
- Chat'te REST geçmiş + SignalR canlı eventleri birlikte kullanılmalıdır.
- Puan sonrası gerçek ortalama için ürün detayını veya ortalama endpointini refresh edin.
- Talep kabulü ödeme/sipariş değildir; UI metinlerinde "anlaşıldı" olarak konumlandırın.
