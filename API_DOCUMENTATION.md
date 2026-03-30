# Yoremio API Documentation

## Runtime

- Development HTTP: `http://localhost:5089`
- Development HTTPS: `https://localhost:7194`
- Health: `/health`
- SignalR hub: `/chathub`

Swagger/OpenAPI sadece development ortaminda map edilir.

## Development Seed

Development ortaminda API acilisinda migration ve seed otomatik calisir.

Seed icerigi:

- roller: `SATICI`, `ALICI`
- test kullanicilari
- kategoriler
- demo urunler
- yorumlar
- puanlar
- favoriler
- talep ve teklif kayitlari

Seed urun medya klasoru:

- `API/wwwroot/demo-media/`

Medya dosyalarini yeniden indirmek icin:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Download-SeedMedia.ps1
```

API yeniden baslatildiginda seed urunleri bu klasordeki medya URL'leriyle senkronize edilir. Seed urunlerde eski medya kayitlari temizlenir, sadece `demo-media` yolları tutulur.

### Test Kullanicilari

Tum kullanicilar icin sifre:

- `Test123!`

Saticilar:

- `ayse@demo.yoremio.local`
- `mehmet@demo.yoremio.local`
- `zeynep@demo.yoremio.local`

Alicilar:

- `elif@demo.yoremio.local`
- `can@demo.yoremio.local`
- `selin@demo.yoremio.local`

## Auth

- Korumali endpointler `Authorization: Bearer <token>` ister
- SignalR baglantisinda `/chathub` icin `access_token` query string desteklenir
- CORS allowlist `Cors:AllowedOrigins` altindan okunur

### `POST /api/Auth/register/satici`

Body:

```json
{
  "email": "seller@example.com",
  "phoneNumber": "5551112233",
  "password": "Secret123!",
  "magazaAdi": "Demo Magaza",
  "vergiNo": "1234567890",
  "adres": "Istanbul",
  "sehir": "Istanbul",
  "ilce": "Kadikoy"
}
```

Notlar:

- email ve telefon dogrulama akisi vardir
- email veya SMS gonderimi basarisiz olursa kayit geri alinir

### `POST /api/Auth/register/alici`

Body:

```json
{
  "email": "buyer@example.com",
  "password": "Secret123!"
}
```

### `POST /api/Auth/login`

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

### `GET /api/Auth/me`

Auth: `Yes`

### `GET /api/Auth/confirm-email?userId={userId}&token={token}`

Auth: `No`

Query params:

- `userId`
- `token`

Notlar:

- email dogrulama linki icin kullanilir
- `userId` veya `token` eksikse `400 Bad Request` doner

### `GET /api/Auth/confirm-phone?userId={userId}&token={token}`

Auth: `No`

Query params:

- `userId`
- `token`

Notlar:

- telefon dogrulama linki icin kullanilir
- `userId` veya `token` eksikse `400 Bad Request` doner

## Standart Response

Tum endpointler `ApiResponse<T>` ile doner:

```json
{
  "success": true,
  "message": "Islem basarili.",
  "data": {},
  "errors": null,
  "traceId": "00-xxx"
}
```

Validation hata ornegi:

```json
{
  "success": false,
  "message": "Dogrulama hatasi olustu.",
  "data": null,
  "errors": {
    "Email": [
      "Email bos olamaz."
    ]
  },
  "traceId": "00-xxx"
}
```

## Profil

### `GET /api/Profil/satici`

Auth: `Yes` (`SATICI`)

### `PUT /api/Profil/satici`

Auth: `Yes` (`SATICI`)

### `GET /api/Profil/satici/{saticiId}/guven-skoru`

Auth: `No`

## Kategori

### `GET /api/Kategori`

### `GET /api/Kategori/{id}`

### `POST /api/Kategori`

Auth: `Yes` (`SATICI`)

### `PUT /api/Kategori/{id}`

Auth: `Yes` (`SATICI`)

### `DELETE /api/Kategori/{id}`

Auth: `Yes` (`SATICI`)

## Urun

### `GET /api/Urun`

Query params:

- `q`
- `kategoriId`
- `minFiyat`
- `maxFiyat`
- `saticiId`
- `sehir`
- `ilce`
- `sadeceAktif`
- `sadeceStoktaOlanlar`
- `minOrtalamaPuan`
- `sort`
- `page`
- `pageSize`

`sort` degerleri:

- `price_asc`
- `price_desc`
- `name_asc`
- `name_desc`
- `top_rated`
- `most_reviewed`
- `most_favorited`
- `newest`
- `oldest`

Response `data.items[]` alani:

```json
{
  "id": 10,
  "adi": "Yayla Bali",
  "aciklama": "Posof yaylalarindan toplanan cicek bali",
  "fiyat": 420,
  "stokMiktari": 12,
  "kategoriId": 6,
  "saticiId": "user-id",
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

### `GET /api/Urun/{id}`

### `GET /api/Urun/urunlerim`

Auth: `Yes` (`SATICI`)

### `GET /api/Urun/onerilen?take=12`

Auth: `Yes` (`ALICI`)

### `GET /api/Urun/favorilerim`

Auth: `Yes` (`ALICI`)

### `POST /api/Urun/{urunId}/favori`

Auth: `Yes` (`ALICI`)

### `DELETE /api/Urun/{urunId}/favori`

Auth: `Yes` (`ALICI`)

### `POST /api/Urun/urun-ekle`

Auth: `Yes` (`SATICI`)

Content type: `multipart/form-data`

Alanlar:

- `Adi`
- `Aciklama`
- `Fiyat`
- `StokMiktari`
- `KategoriId`
- `Resimler`
- `Videolar`

### `PUT /api/Urun/{urunId}`

Auth: `Yes` (`SATICI`)

### `DELETE /api/Urun/{urunId}`

Auth: `Yes` (`SATICI`)

### `DELETE /api/Urun/{urunId}/resimler/{resimId}`

Auth: `Yes` (`SATICI`)

### `DELETE /api/Urun/{urunId}/videolar/{videoId}`

Auth: `Yes` (`SATICI`)

## Puan

### `POST /api/Puan/puan-ekle`

Auth: `Yes` (`ALICI`)

Body:

```json
{
  "urunId": 1,
  "puanDegeri": 5
}
```

### `GET /api/Puan/urun/{urunId}`

### `GET /api/Puan/ortalama/{urunId}`

## Yorum

### `POST /api/Yorum`

Auth: `Yes` (`ALICI`)

### `PUT /api/Yorum/{yorumId}`

Auth: `Yes` (`ALICI`)

### `DELETE /api/Yorum/{yorumId}`

Auth: `Yes` (`ALICI`)

### `GET /api/Yorum/{urunId}`

## Talep

### `POST /api/Talep`

Auth: `Yes` (`ALICI`)

Body:

```json
{
  "urunId": 1,
  "miktar": 3,
  "not": "Hafta sonu teslim alinabilir."
}
```

### `GET /api/Talep/benim`

Auth: `Yes` (`ALICI`)

### `GET /api/Talep/satici`

Auth: `Yes` (`SATICI`)

### `POST /api/Talep/{talepId}/teklif`

Auth: `Yes` (`SATICI`)

Body:

```json
{
  "birimFiyat": 220,
  "mesaj": "Yarin teslimata hazirlayabilirim."
}
```

### `POST /api/Talep/teklif/{teklifId}/kabul`

Auth: `Yes` (`ALICI`)

Not:

- odeme alinmaz
- teklif kabul edilince talep `ANLASILDI`, diger teklifler `RED` olur

## SignalR

Hub endpoint:

- `/chathub`

Client -> Server:

- `SendMessage(toUserId, message)`
- `SendMessageLegacy(fromUserId, toUserId, message)`
- `Typing(toUserId)`

Server -> Client:

- `ReceiveMessage(fromUserId, message)`
- `MessageSent(toUserId, message, sentAtUtc)`
- `Typing(fromUserId)`
- `Connected(userId)`

## Common Status Codes

- `200 OK`
- `201 Created`
- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `404 Not Found`
- `500 Internal Server Error`
