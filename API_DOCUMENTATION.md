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

### 3.1 Auth Header

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
  "role": "ALICI"
}
```

Öneri:

- Token'ı API client interceptor seviyesinde ekleyin.
- Uygulama açılışında token varsa `GET /api/Auth/me` ile oturumu doğrulayın.
- `401` gelirse token'ı temizleyip login ekranına yönlendirin.
- `403` gelirse kullanıcının rolü ekran için yetkisizdir; login'e atmak yerine yetkisiz ekranı göstermek daha doğru olur.

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

- Satıcı profil
- Ürünlerim
- Ürün ekle/güncelle
- Ürün medya yönetimi
- Gelen talepler
- Teklif verme
- Chat
- Kategori yönetimi

Önemli endpointler:

- `GET /api/Profil/satici`
- `PUT /api/Profil/satici`
- `GET /api/Urun/urunlerim`
- `POST /api/Urun/urun-ekle`
- `PUT /api/Urun/{urunId}`
- `DELETE /api/Urun/{urunId}`
- `DELETE /api/Urun/{urunId}/resimler/{resimId}`
- `DELETE /api/Urun/{urunId}/videolar/{videoId}`
- `GET /api/Talep/satici`
- `POST /api/Talep/{talepId}/teklif`
- `GET /api/Chat/conversations`

Not: Kategori oluşturma/güncelleme/silme endpointleri şu an `SATICI` rolüne açıktır. UI'da bu alanı isterseniz sadece yönetici benzeri satıcılar için görünür yapacak ayrı frontend kuralı ekleyebilirsiniz; backend tarafında ayrı `ADMIN` rolü yoktur.

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

- Satıcı kaydı sonrası email ve telefon doğrulama akışı vardır.
- Satıcı profili `AktifMi=false` başlar.
- Email ve telefon doğrulanınca satıcı aktif hale gelir.
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
  "role": "SATICI"
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

### 6.6 Telefon Doğrulama

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

Auth: `SATICI`

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

Auth: `SATICI`

Body:

```json
{
  "adi": "Güncel Kategori",
  "aciklama": "Güncel açıklama"
}
```

### 8.5 Kategori Sil

`DELETE /api/Kategori/{id}`

Auth: `SATICI`

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

### 9.11 Ürün Sil

`DELETE /api/Urun/{urunId}`

Auth: `SATICI`

Not:

- Sadece ürün sahibi satıcı silebilir.
- UI onay modalı kullanmalıdır.

### 9.12 Ürün Resmi Sil

`DELETE /api/Urun/{urunId}/resimler/{resimId}`

Auth: `SATICI`

### 9.13 Ürün Videosu Sil

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

## 14. SignalR Chat Hub

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

## 15. Development Seed

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

## 16. Status Code ve Hata Mantığı

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

## 17. Endpoint Özet Tablosu

### Auth

| Method | Endpoint | Auth | Rol | Açıklama |
| --- | --- | --- | --- | --- |
| `POST` | `/api/Auth/register/satici` | Hayır | - | Satıcı kaydı |
| `POST` | `/api/Auth/register/alici` | Hayır | - | Alıcı kaydı |
| `POST` | `/api/Auth/login` | Hayır | - | JWT login |
| `GET` | `/api/Auth/me` | Evet | Any | Aktif kullanıcı |
| `GET` | `/api/Auth/confirm-email` | Hayır | - | Email doğrulama |
| `GET` | `/api/Auth/confirm-phone` | Hayır | - | Telefon doğrulama |

### Profil

| Method | Endpoint | Auth | Rol | Açıklama |
| --- | --- | --- | --- | --- |
| `GET` | `/api/Profil/satici` | Evet | `SATICI` | Satıcı profilim |
| `PUT` | `/api/Profil/satici` | Evet | `SATICI` | Satıcı profil güncelle |
| `GET` | `/api/Profil/satici/{saticiId}/guven-skoru` | Hayır | - | Public satıcı güven skoru |

### Kategori

| Method | Endpoint | Auth | Rol | Açıklama |
| --- | --- | --- | --- | --- |
| `GET` | `/api/Kategori` | Hayır | - | Kategori listesi |
| `GET` | `/api/Kategori/{id}` | Hayır | - | Kategori detay |
| `POST` | `/api/Kategori` | Evet | `SATICI` | Kategori oluştur |
| `PUT` | `/api/Kategori/{id}` | Evet | `SATICI` | Kategori güncelle |
| `DELETE` | `/api/Kategori/{id}` | Evet | `SATICI` | Kategori sil |

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

## 18. UI Tasarım Checklist'i

Bu dokümana göre UI çıkarırken minimum ekran seti:

- Public ana sayfa: kategori + ürün liste + filtre + pagination
- Ürün detay: galeri + satıcı kartı + puan/yorum + talep/chat/favori aksiyonları
- Auth: login, alıcı kayıt, satıcı kayıt, doğrulama sonuç ekranı
- Alıcı paneli: önerilenler, favorilerim, taleplerim, chat
- Satıcı paneli: profil, ürünlerim, ürün formu, medya yönetimi, gelen talepler, teklif formu, chat
- Ortak: global loading, empty state, error state, 401/403/429/500 davranışları

Frontend tarafında özellikle unutulmaması gerekenler:

- Tüm response'larda `success/message/data/errors/traceId` envelope bekleyin.
- Upload endpointlerinde JSON değil `multipart/form-data` kullanın.
- Upload alan adlarını backend DTO adlarıyla gönderin: `Adi`, `Fiyat`, `Resimler`, `Videolar`.
- Media URL'leri local/demo için relative, Cloudinary için absolute gelebilir; `http` ile başlıyorsa direkt kullanın, değilse API base URL ile birleştirin.
- Satıcı login için email + telefon doğrulama gereksinimini UI metninde açıklayın.
- Chat'te REST geçmiş + SignalR canlı eventleri birlikte kullanılmalıdır.
- Puan sonrası gerçek ortalama için ürün detayını veya ortalama endpointini refresh edin.
- Talep kabulü ödeme/sipariş değildir; UI metinlerinde "anlaşıldı" olarak konumlandırın.
