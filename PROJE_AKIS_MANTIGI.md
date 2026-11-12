# Yoremio Proje Akis Mantigi

Bu dokuman Yoremio backend projesinin genel calisma mantigini anlatir. Endpoint detaylari icin `API_DOCUMENTATION.md`, kurulum ve komutlar icin `README.md` okunmalidir. Buradaki amac, projeye yeni bakan birinin "hangi parca ne ise yariyor, is akisi nasil ilerliyor?" sorusunu hizlica cevaplamaktir.

## 1. Projenin Amaci

Yoremio, alici ile yerel uretici/saticiyi bulusturan bir pazar yeri API'sidir. Temel fikir klasik sepet-odeme akisi degil; urun kesfi, talep acma, saticinin teklif vermesi ve alici-satici arasinda anlasma olusmasidir.

Ana islevler:

- Alici urunleri/kategorileri gezer, favoriye ekler, talep acar, teklifi kabul eder, chat yapar.
- Satici email dogrulama sonrasi profilini ve urunlerini yonetir, gelen taleplere teklif verir, chat yapar.
- Admin kategori CRUD ve dashboard gibi yonetim islerini yapar.
- Sistem yorum, puan, favori, bildirim, guven skoru ve destek talepleriyle kullanici deneyimini tamamlar.

## 2. Cozum Katmanlari

Proje katmanli mimariyle ayrilmistir:

| Katman | Gorev |
| --- | --- |
| `API` | HTTP controller'lar, middleware, auth, CORS, startup, health check, SignalR endpointleri. |
| `Application` | DTO'lar, servis interface'leri, validation attribute'lari ve response modelleri. |
| `Domain` | Entity'ler, domain sabitleri, repository interface'leri ve ortak modeller. |
| `Infrastructure` | EF Core DbContext, repository implementasyonlari, servisler, mail, dosya kayit, Cloudinary, SignalR hub. |
| `Tests` | Mevcut test/runner dosyalari. |

Genel is akisi:

```txt
HTTP Request
  -> API Middleware
  -> JWT Auth / Role Policy
  -> Controller
  -> Application Interface
  -> Infrastructure Service
  -> Repository / DbContext
  -> Database
  -> ApiResponse<T>
```

## 3. Standart Response Mantigi

API cevaplari standart envelope ile doner:

```json
{
  "success": true,
  "message": "Islem basarili.",
  "data": {},
  "errors": null,
  "traceId": "..."
}
```

Frontend tarafinda genel kural:

- `success=true`: UI akisa devam eder.
- `success=false`: `message` kullaniciya gosterilir, teknik detay gerekiyorsa `errors` incelenir.
- `traceId`: destek/log takibi icin saklanabilir.

## 4. Roller ve Yetkiler

| Rol | Yapabilir | Yapamaz |
| --- | --- | --- |
| Anonim | Urun listeleme, kategori listeleme, satici guven skoru, one cikan saticilar, urun yorumlari. | Talep, favori, chat, yorum/puan, profil islemleri. |
| `ALICI` | Profil/konum guncelleme, favori, talep acma, teklif kabul, chat, bildirim, anlasma sonrasi yorum/puan. | Urun ekleme, satici profili yonetme, kategori yazma. |
| `SATICI` | Profil/kapak resmi yonetimi, urun CRUD, medya silme, urun aktif/pasif, gelen taleplere teklif, chat. | Kategori CRUD, baska saticinin urununu yonetme. |
| `ADMIN` | Kategori CRUD, admin dashboard, satici dogrulama onayi, kullanici listeleme, urun ve yorum moderasyonu. | Satici yerine urun sahipligi islemleri icin ayri endpoint yoktur. |

Not: Satici kaydinda kullanici ayni anda `SATICI` ve `ALICI` rollerini alir. Boylece satici paneli olan kullanici platformda alici davranislari da gosterebilir.

## 5. Kimlik ve Satici Dogrulama Akisi

Satici kaydi:

```txt
POST /api/Auth/register/satici
  -> ApplicationUser olusur
  -> SATICI + ALICI rolleri atanir
  -> SaticiProfili olusur
  -> Email dogrulamasi gerekiyorsa SaticiProfili.AktifMi=false baslar
  -> Dogrulama maili gonderilir
```

Email dogrulama:

```txt
Maildeki kod
  -> POST /api/Auth/confirm-email
  -> EmailConfirmed=true
  -> SaticiProfili.AktifMi=true
```

Login:

- `POST /api/Auth/login` JWT token doner.
- Email dogrulamasi zorunluysa, dogrulanmamis satici login olamaz.
- SMS/Twilio dogrulamasi kaldirildi; satici dogrulamasinda ana kriter emaildir.

Mail gonderimi:

- Local gelistirmede mock outbox kullanilabilir: `/dev/verification`
- Gercek gonderimde SMTP veya Brevo API desteklenir.
- Render ortaminda SMTP timeout riski oldugu icin `Email__Smtp__Provider=BrevoApi` tercih edilir.

## 6. Uygulama Acilis Akisi

Frontend uygulama acilirken onerilen sira:

```txt
GET /api/App/bootstrap
  -> kategori/siralama/feature flag gibi baslangic bilgileri

Token varsa:
GET /api/Auth/me
  -> kullanici ve rol bilgisi

Login kullanici varsa:
GET /api/Dashboard/summary
  -> header/panel ozetleri
```

Ana sayfa public veri icin:

```txt
GET /api/Kategori
GET /api/Urun
GET /api/Profil/saticilar/one-cikan?take=6
```

## 7. Kategori Mantigi

Kategori urun ekleme formunun temel bagimliligidir. `GET /api/Kategori` bos kalirsa frontend urun ekleme formunda kategori sectiremez.

Kurallar:

- Kategori listesi publictir.
- Kategori yazma islemleri `ADMIN` rolundedir.
- Backend kategori listesi bos ise varsayilan kategorileri garanti etmeye calisir.
- Satici panelinde kategori CRUD degil, sadece kategori secimi gosterilmelidir.

## 8. Urun ve Medya Akisi

Urun ekleme:

```txt
SATICI
  -> GET /api/Kategori
  -> POST /api/Urun/urun-ekle multipart/form-data
  -> Urun kaydi olusur
  -> Resim/video dosyalari kaydedilir
  -> UrunResim/UrunVideo URL kayitlari yazilir
```

Urun guncelleme:

- `PUT /api/Urun/{urunId}` sadece urun sahibi satici tarafindan yapilir.
- Yeni medya gonderilirse mevcut medyaya eklenir.
- Mevcut medyayi kaldirmak icin ayri silme endpointleri kullanilir.

Medya silme:

```txt
DELETE /api/Urun/{urunId}/resimler/{resimId}
DELETE /api/Urun/{urunId}/videolar/{videoId}
```

Urun aktif/pasif:

```txt
PUT /api/Urun/{urunId}/status
Body: { "aktifMi": true }
```

Production notu:

- Render gibi kalici disk vermeyen ortamlarda local `wwwroot` uploadlari kalici kabul edilmemelidir.
- Production icin `Cloudinary__Enabled=true` kullanilmasi daha dogrudur.

## 9. Urun Kesif, Konum Ayrimi ve Akilli Alternatifler

Public urun kesfi:

```txt
GET /api/Urun?q=&kategoriId=&minFiyat=&maxFiyat=&uretimSehir=&teslimatSehir=&sadeceEldenTeslim=&sadeceKargolu=&sadeceYeniHasat=&sadeceDogrulanmisSatici=&sort=&page=&pageSize=
GET /api/Urun/kesfet?q=&teslimatSehir=&uretimSehir=&sort=&page=&pageSize=
```

Konum ve teslimat mantigi:

- `UretimSehir` / `UretimIlce` (mensei) ile satici konumu ve `GonderimSehirleri` / `EldenTeslimVarMi` / `KargoVarMi` ayri tutulur.
- `TeslimatSehir` filtresi hem o sehirdeki yerel saticilari hem de kargo ile o sehre gonderim yapan uzak ureticileri getirir; `sort=relevance` yerel + o sehre gonderen urunleri ustte siralar.
- `GET /api/Urun/kesfet` sonucunda urun bulunamazsa bos ekran yerine `alternatifler` doner: `ayniYoredenUrunler`, `sehreGonderimYapanlar`, `stoguTukenenUrunler` (stok alarmi kurulabilir), `aktifOrtakAlimlar` ve `benimIcinBulOnerisi` (`POST /api/Talep/pazar`).
- Stogu `0` olan urunlerde `POST /api/Urun/{urunId}/stok-alarmi`, fiyat dususu icin `POST /api/Urun/{urunId}/fiyat-alarmi` ve satici yenilikleri icin `POST /api/Profil/satici/{saticiId}/takip` desteklenir.

One cikan saticilar ve public magaza ozeti:

```txt
GET /api/Profil/saticilar/one-cikan?take=6
GET /api/Profil/satici/{saticiId}
```

Satici kartlari icin onemli alanlar:

- `saticiId`
- `magazaAdi`
- `ortalamaPuan`
- `toplamYorum`
- `toplamFavori`
- `dogrulanmisSatici`
- `vitrinUrunId`

Frontend satici kartina tiklayinca belirsizlik olmamasi icin `vitrinUrunId` varsa urun detayina gidebilir.

## 10. Talep, Teklif, Akilli Acik Pazar ve Ortak Alim Akisi

Yoremio'da satin alma yerine "talep ve anlasma" modeli vardir:

```txt
1) Direkt Urun Talebi:
   ALICI urun detayindan talep acar -> POST /api/Talep (TalepTuru = URUN)

2) Akilli Acik Pazar Talebi ("Benim Icin Bul"):
   ALICI belirli bir urunu secmeksizin aradigi urunu ve teslimat sehrini ilan eder
     -> POST /api/Talep/pazar (TalepTuru = ACIK_PAZAR)
     -> Sistem kategori/yore/teslimat kapsamina gore aktif saticilari skorlayarak TalepEslesmeleri kaydeder
     -> Sadece eslesen ureticilere hedefli bildirim gider
     -> Saticilar GET /api/Talep/pazar uzerinden gorur ve POST /api/Talep/{talepId}/teklif ile teklif yaristirir

3) Ortak Aksiyonlar & Elde Tutma:
   - Tek dokunusla gecmis talebi tekrarlama -> POST /api/Talep/{talepId}/tekrarla
   - Teklif kabul / red / iptal -> POST /api/Talep/teklif/{teklifId}/kabul|red, POST /api/Talep/{talepId}/iptal
   - Platform disi anlasma tamamlama teyidi -> POST /api/Talep/{talepId}/anlasma-onayi

4) Yoremio Ortak Alim (/api/OrtakAlim):
   - Satici dogrudan ACIK grup siparisi acar veya alici bir ureticiye ONAY_BEKLIYOR ortak alim onerir
   - Satici onaylar (POST /api/OrtakAlim/{id}/onayla) -> alicilar katilir (POST /api/OrtakAlim/{id}/katil)
   - Katilimci sayisi MinimumKatilimci degerine ulasinca durum HEDEF_TAMAMLANDI olur; teslimat ve soguk zincir/iptal kosullari seffaf tasinir.
```

Bu modelin etkileri:

- Bildirim olusur.
- Alici ve satici arasinda guvenli is akisi takip edilir.
- Yorum/puan uygunlugu icin kabul edilmis teklif/anlasilmis talep kaydi kullanilir.

## 11. Yorum, Puan ve Guven Skoru

Yorum/puan kuralinin amaci sahte yorumlari azaltmaktir.

Genel kural:

- Alici yorum/puan verebilmek icin ilgili urunde anlasilmis bir talebe sahip olmalidir.
- UI, formu acmadan once `GET /api/Urun/{urunId}/yorum-yetkisi` ile uygunlugu kontrol etmelidir.

Yorum yetkisi response mantigi:

```json
{
  "yorumYapabilir": false,
  "puanlayabilir": false,
  "mevcutPuan": null,
  "sebep": "Yorum yapabilmek icin bu urunle ilgili kabul edilmis bir talebiniz olmalidir."
}
```

Guven skoru:

- Saticinin puan, yorum, favori ve dogrulama durumuna gore sosyal kanit saglar.
- Public endpoint: `GET /api/Profil/satici/{saticiId}/guven-skoru`

## 12. Chat ve Bildirim Akisi

Chat iki sekilde calisir:

1. REST endpointleriyle gecmis, gonderim ve okundu bilgisi.
2. SignalR ile canli mesajlasma ve anlik bildirim akisi.

REST endpointleri:

```txt
GET /api/Chat/conversations
GET /api/Chat/messages/{otherUserId}
POST /api/Chat/messages/{receiverId}
POST /api/Chat/messages/{otherUserId}/read
```

SignalR:

```txt
/chathub
  -> ReceiveMessageV2 / MessageSentV2 (canli chat)
  -> NotificationReceived (yeni bildirim + okunmamisSayisi)
  -> UnreadNotificationCountUpdated (guncel okunmamis sayaci)
```

Bildirimler su olaylarda kullanilir:

- Talep acilinca saticiya bildirim (`TALEP`).
- Teklif verilince aliciya bildirim (`TEKLIF`).
- Teklif kabul edilince saticiya bildirim (`TEKLIF`).
- Admin urun/yorum/satici moderasyon karari alinca ilgili kullaniciya bildirim (`SISTEM`).
- Chat ve panel badge'leri icin okunmamis sayilar.

## 13. Destek Talebi Akisi

Frontend yardim/destek butonu backend'e basit bir talep gonderir:

```txt
POST /api/Destek/talep
```

Contract:

```json
{
  "konu": "Urun formunda kategori secemiyorum",
  "mesaj": "Kategori listesi bos geliyor.",
  "email": "kullanici@example.com",
  "telefon": "+905...",
  "ekran": "urun-ekleme",
  "ilgiliVarlikTuru": "Urun",
  "ilgiliVarlikId": "123"
}
```

Backend talebi loglar ve takip icin `talepId` doner. Bu akisin amaci ilk etapta destek kaydini kaybetmemektir; ileride DB tablosu veya harici destek servisi eklenebilir.

## 14. Dosya ve Medya Saklama

Iki mod vardir:

| Mod | Davranis |
| --- | --- |
| Local static | Dosyalar API `wwwroot` altina yazilir, URL relative olabilir. |
| Cloudinary | Dosyalar Cloudinary'ye yuklenir, DB'ye absolute CDN URL yazilir. |

Frontend medya gosterirken:

- URL `http` ile basliyorsa direkt kullan.
- Relative ise API base URL ile birlestir.

Production icin Cloudinary onerilir.

## 15. Ortam ve Deploy Mantigi

Local:

- `.env.local` repo kokunden otomatik okunur.
- `.env.local` git'e girmez.
- Localde gercek SMTP/API veya development mock outbox kullanilabilir.

Render:

- `render.yaml` service ve genel env anahtarlarini tarif eder.
- Secret degerler `sync: false` ise Render Dashboard'a manuel girilir.
- Aynı env key Render'da iki kez eklenmemelidir; duplicate key hatasi verir.
- Env degisince deploy/restart gerekir.

Email production:

```txt
Email__Smtp__Provider=BrevoApi
Email__Smtp__ApiKey=<Brevo API key>
Email__Smtp__ApiBaseUrl=https://api.brevo.com
Email__Smtp__FromAddress=<Brevo'da dogrulanmis sender>
Email__Smtp__UseMockSender=false
Verification__PublicBaseUrl=https://yoremio.onrender.com
```

Brevo tarafinda IP whitelist aciksa Render IP'si Brevo authorized IP listesine eklenmelidir.

## 16. Hata Takibi

Backend hatalarinda:

- Response icindeki `traceId` UI tarafinda gosterilebilir veya loglanabilir.
- Render logs icinde ayni `traceId` aranarak asil exception bulunabilir.
- Mail tarafinda response bazen sade hata doner; asil detay loglarda olur.

Ornek:

```txt
traceId=response.traceId
Render Logs -> traceId ara -> stack trace / inner exception oku
```

## 17. Frontend Icin Pratik Akis Haritasi

Ana sayfa:

```txt
GET /api/App/bootstrap
GET /api/Kategori
GET /api/Urun/kesfet
GET /api/OrtakAlim
GET /api/Profil/saticilar/one-cikan
```

Satici kayit:

```txt
POST /api/Auth/register/satici
POST /api/Auth/confirm-email
POST /api/Auth/login
```

Satici paneli:

```txt
GET /api/Dashboard/satici (kontrol merkezi KPI ve aksiyon onerileri)
GET /api/Profil/satici
PUT /api/Profil/satici (aciklama + kapakResim multipart)
GET /api/Urun/urunlerim
PATCH /api/Urun/toplu-stok
GET /api/Talep/satici?durum=&page=&pageSize=
GET /api/Talep/pazar
POST /api/OrtakAlim
GET /api/Chat/conversations
```

Admin paneli:

```txt
GET /api/Dashboard/admin
GET /api/Admin/talep-bosluklari
GET /api/Admin/kullanicilar
PATCH /api/Admin/saticilar/{saticiId}/dogrulama
GET /api/Admin/urunler
PATCH /api/Admin/urunler/{urunId}/durum
GET /api/Admin/yorumlar
PATCH /api/Admin/yorumlar/{yorumId}/durum
```

Alici urun detayi ve kesif/alarmlar:

```txt
GET /api/Urun/{id}
GET /api/Profil/satici/{saticiId}/guven-skoru
GET /api/Urun/{urunId}/yorum-yetkisi
POST /api/Talep
POST /api/Talep/pazar
POST /api/Talep/{talepId}/tekrarla
POST /api/Urun/{urunId}/favori
POST /api/Urun/{urunId}/fiyat-alarmi
POST /api/Urun/{urunId}/stok-alarmi
POST /api/Profil/satici/{saticiId}/takip
POST /api/OrtakAlim/{id}/katil
```

Chat:

```txt
GET /api/Chat/conversations
GET /api/Chat/messages/{otherUserId}
POST /api/Chat/messages/{receiverId}
POST /api/Chat/messages/{otherUserId}/read
SignalR /chathub
```

## 18. Gelistirici Notlari

- Controller'lar ince tutulmali; is kurali servis katmaninda kalmali.
- DTO kontratlari frontend ile uyumlu tutulmali.
- Yeni endpoint eklenirse `API_DOCUMENTATION.md` ve gerekiyorsa bu dokuman guncellenmeli.
- Production secret degerleri asla repo dosyalarina yazilmamali.
- Local `.env.local` ile Render Dashboard env'leri birbirinden bagimsizdir.
- Satici urun islemlerinde sahiplik kontrolu atlanmamalidir.
- Yorum/puan gibi guven etkileyen islemlerde talep/anlasma kontrolu korunmalidir.

## 19. Kisa Ozet

Yoremio'nun cekirdegi su dongudur:

```txt
Satici email ile dogrulanir
  -> urun ekler
  -> alici urunu kesfeder
  -> talep acar
  -> satici teklif verir
  -> alici kabul eder
  -> taraflar chat/bildirim ile sureci takip eder
  -> alici yorum/puan verir
  -> saticinin guven skoru guclenir
```

Bu dongu kategori, medya, favori, dashboard, destek ve bildirim servisleriyle tamamlanir.
