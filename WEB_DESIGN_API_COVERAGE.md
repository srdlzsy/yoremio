# Yoremio Web Design API Coverage

Bu dosya web tasariminda API'nin karsilayamayacagi alanlar kalmasin diye tutulur. Ekran mockup'i hazirlanirken buradaki sinirlar esas alinmalidir.

## Public Ana Sayfa

Desteklenen alanlar:

- Kategori listesi: `GET /api/App/bootstrap` veya `GET /api/Kategori` (`urunSayisi` dahil)
- Akilli lokasyon secicisi (aktif urunlu sehir/ilceler): `GET /api/App/bootstrap` (`cities`, `locations`)
- Urun arama/liste/grid: `GET /api/Urun` (standart) ve akilli konum + alternatif kesif: `GET /api/Urun/kesfet`
- Arama kutusu: `q`
- Kategori filtresi: `kategoriId`
- Lokasyon & teslimat kapsami filtreleri: `sehir`, `ilce`, `uretimSehir`, `teslimatSehir`, `sadeceEldenTeslim`, `sadeceKargolu`, `sadeceYeniHasat`
- Fiyat filtresi: `minFiyat`, `maxFiyat`
- Stok filtresi: `sadeceStoktaOlanlar`
- Puan filtresi: `minOrtalamaPuan`
- Dogrulanmis satici filtresi: `sadeceDogrulanmisSatici`
- Siralama: `sort` (`newest`, `relevance`, `price_asc`, `price_desc`, `top_rated`, `most_viewed`, `most_favorited`)
- Sonuc bulunamazsa akilli alternatifler (`GET /api/Urun/kesfet` -> `alternatifler`): `ayniYoredenUrunler`, `sehreGonderimYapanlar`, `stoguTukenenUrunler`, `aktifOrtakAlimlar`, `benimIcinBulOnerisi`
- Public satici vitrin ozeti: `GET /api/Profil/satici/{saticiId}` (`aciklama`, `kapakResimUrl`, `paketTuru` dahil)
- One cikan saticilar: `GET /api/Profil/saticilar/one-cikan`
- Yoremio Ortak Alim vitrini: `GET /api/OrtakAlim`, `GET /api/OrtakAlim/{id}`
- Sehrime Gelen Urunler (planli sevkiyatlar): `GET /api/PlanliGonderim?hedefSehir=`
- Urun karti zengin alanlari: `urun.resimler[0].url`, `birimMiktar`, `teslimatSecenekleri`, `yeniHasatMi`, `goruntulenmeSayisi`, `slug`, `uretimSehir`, `uretimIlce`, `eldenTeslimVarMi`, `kargoVarMi`, `gonderimSehirleri`
- Satici rozetleri: `saticiDogrulanmis`, `saticiMagazaAdi`, `saticiSehir`, `saticiIlce`

Tasarim siniri:

- Ana sayfada canli kampanya, kupon, kargo takibi, odeme veya siparis durumu gosterilmemelidir; bu API'de yoktur.

## Urun Kesif Ve Detay

Desteklenen alanlar:

- Galeri: `resimler`, `videolar`
- Fiyat/stok/kategori/satici bilgisi: `UrunDto`
- Sosyal kanit: `ortalamaPuan`, `toplamPuan`, `toplamYorum`, `toplamFavori`
- Favoriye ekle/cikar: `POST /api/Urun/{urunId}/favori`, `DELETE /api/Urun/{urunId}/favori`
- Fiyat ve stok alarmlari: `POST/DELETE /api/Urun/{urunId}/fiyat-alarmi`, `POST/DELETE /api/Urun/{urunId}/stok-alarmi`
- Satici takip (yeni urun/hasat bildirimi): `POST/DELETE /api/Profil/satici/{saticiId}/takip`
- Favoride mi bilgisi: alici login ise `GET /api/Urun/favorilerim` sonucu urun id ile eslestirilir.
- Talep olustur (urun bazli veya acik pazar / Benim Icin Bul): `POST /api/Talep`, `POST /api/Talep/pazar`
- Ortak alim katilimi veya onerisi: `POST /api/OrtakAlim`, `POST /api/OrtakAlim/{id}/katil`
- Saticiya yaz: `POST /api/Chat/messages/{receiverId}` veya SignalR `SendMessage`
- Satici guven skoru: `GET /api/Profil/satici/{saticiId}/guven-skoru`

Tasarim siniri:

- Sepete ekle, hemen ode, kargo sec veya stok rezervasyonu ana aksiyon yapilmamalidir. Bu API su an talep/teklif pazarligi modelindedir.

## Alici Paneli

Desteklenen alanlar:

- Profilim ve konum/gorunen ad duzenleme: `GET /api/Profil/alici`, `PUT /api/Profil/alici` (`userName`, `sehir`, `ilce`, `adres`, `favoriKategori`)
- Onerilen urunler (konum ve kategori bazli): `GET /api/Urun/onerilen`
- Favoriler: `GET /api/Urun/favorilerim`
- Taleplerim (durum ve sayfalama destekli) + akilli acik pazar talebi: `GET /api/Talep/benim?durum=&page=&pageSize=`, `POST /api/Talep/pazar`, `POST /api/Talep/{talepId}/tekrarla`
- Talep detay: `GET /api/Talep/{talepId}`
- Talep karti urun/satici gorseli: `urunResimUrl`, `urunAdi`, `urunFiyat`, `saticiMagazaAdi`, `saticiSehir`, `saticiIlce`, `talepTuru`, `arananUrunAdi`, `teslimatSehir`
- Teklif kabul/red, talep iptal ve dis anlasma tamamlama onayi: `POST /api/Talep/teklif/{teklifId}/kabul`, `POST /api/Talep/teklif/{teklifId}/red`, `POST /api/Talep/{talepId}/iptal`, `POST /api/Talep/{talepId}/anlasma-onayi`
- Alarmlar & takip edilen ureticiler & ortak alimlar: `GET /api/Urun/fiyat-alarmlarim`, `GET /api/Urun/stok-alarmlarim`, `GET /api/Profil/takip-edilenler`, `POST/DELETE /api/OrtakAlim/{id}/katil`
- Mesajlar: `GET /api/Chat/conversations`, `GET /api/Chat/messages/{otherUserId}`
- Puan/yorum yetki kontrolu: `GET /api/Urun/{urunId}/yorum-yetkisi` (`yorumYapabilir`, `puanlayabilir`, `mevcutPuan`)
- Puan/yorum: sadece anlasilmis talep sonrasi `POST /api/Puan/puan-ekle`, `POST /api/Yorum`

Tasarim siniri:

- Siparis/fatura/odeme gecmisi tasarlanmamalidir; talep kabul edilince sadece `ANLASILDI` olur.

## Satici Paneli

Desteklenen alanlar:

- KPI ve kontrol merkezi kartlari (yanitsiz talep, acik pazar firsatlari, dusuk stok, takipci, yanit suresi, tamamlanan anlasmalar, aksiyon onerileri): `GET /api/Dashboard/satici`
- Profil (magaza hikayesi + kapak resmi yukleme destekli): `GET /api/Profil/satici`, `PUT /api/Profil/satici` (JSON veya multipart `kapakResim`)
- Urunlerim ve toplu stok/fiyat/yeni hasat guncelleme: `GET /api/Urun/urunlerim`, `PATCH /api/Urun/toplu-stok`
- Urun ekle/guncelle: `POST /api/Urun/urun-ekle`, `PUT /api/Urun/{urunId}`
- Medya yukle: multipart `Resimler`, `Videolar`
- Medya sil: `DELETE /api/Urun/{urunId}/resimler/{resimId}`, `DELETE /api/Urun/{urunId}/videolar/{videoId}`
- Aktif/pasif: `PATCH /api/Urun/{urunId}/status`
- Gelen direkt talepler + akilli eslesen acik pazar talepleri: `GET /api/Talep/satici?durum=&page=&pageSize=`, `GET /api/Talep/pazar`
- Teklif ver/guncelle + dis anlasma tamamlama onayi: `POST /api/Talep/{talepId}/teklif`, `POST /api/Talep/{talepId}/anlasma-onayi`
- Ortak alim baslatma/onay/durum guncelleme: `POST /api/OrtakAlim`, `POST /api/OrtakAlim/{id}/onayla`, `PATCH /api/OrtakAlim/{id}/durum`
- Chat: Chat REST + SignalR endpointleri

Tasarim siniri:

- Satici panelinde kategori CRUD gosterilmemelidir; kategori yazma `ADMIN` rolundedir.
- Kargo, odeme veya kupon yonetimi gosterilmemelidir.

## Admin Paneli

Desteklenen alanlar:

- KPI ozeti (donusum, anlasma ve 60 gunluk tekrar alici orani dahil): `GET /api/Dashboard/admin`
- Sehir + urun bazli arz boslugu analitigi (yeni uretici kazanimi): `GET /api/Admin/talep-bosluklari`
- Kullanici listesi ve rol filtreleme: `GET /api/Admin/kullanicilar`
- Satici dogrulama rozeti onay/ret: `PATCH /api/Admin/saticilar/{saticiId}/dogrulama`
- Satici aktif/pasif yapma: `PATCH /api/Admin/saticilar/{saticiId}/durum`
- Urun moderasyonu (listele + aktif/pasif): `GET /api/Admin/urunler`, `PATCH /api/Admin/urunler/{urunId}/durum`
- Yorum moderasyonu (listele + soft-hide/aktif): `GET /api/Admin/yorumlar`, `PATCH /api/Admin/yorumlar/{yorumId}/durum`

## Chat ve Canli Bildirimler

Desteklenen alanlar:

- Konusma listesi: `GET /api/Chat/conversations`
- Mesaj gecmisi: `GET /api/Chat/messages/{otherUserId}`
- Mesaj gonderme: `POST /api/Chat/messages/{receiverId}` veya SignalR
- Okundu: `POST /api/Chat/messages/{otherUserId}/read`
- Unread badge: `unreadCount`
- Canli bildirim zili: `/chathub` uzerinden `NotificationReceived` (`BildirimDto`) ve `UnreadNotificationCountUpdated` (`number`)

Tasarim siniri:

- Chat kaydi su an dogrudan urun veya talep entity'sine bagli degildir. Chat yaninda urun/talep context karti gosterilecekse bu kart acilan urun veya talep ekranindaki mevcut veriden beslenmelidir.
