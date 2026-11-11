# Yoremio Web Design API Coverage

Bu dosya web tasariminda API'nin karsilayamayacagi alanlar kalmasin diye tutulur. Ekran mockup'i hazirlanirken buradaki sinirlar esas alinmalidir.

## Public Ana Sayfa

Desteklenen alanlar:

- Kategori listesi: `GET /api/App/bootstrap` veya `GET /api/Kategori` (`urunSayisi` dahil)
- Akilli lokasyon secicisi (aktif urunlu sehir/ilceler): `GET /api/App/bootstrap` (`cities`, `locations`)
- Urun arama/liste/grid: `GET /api/Urun`
- Arama kutusu: `q`
- Kategori filtresi: `kategoriId`
- Lokasyon filtresi: `sehir`, `ilce`
- Fiyat filtresi: `minFiyat`, `maxFiyat`
- Stok filtresi: `sadeceStoktaOlanlar`
- Puan filtresi: `minOrtalamaPuan`
- Dogrulanmis satici filtresi: `sadeceDogrulanmisSatici`
- Siralama: `sort`
- Public satici vitrin ozeti: `GET /api/Profil/satici/{saticiId}` (`aciklama`, `kapakResimUrl` dahil)
- One cikan saticilar: `GET /api/Profil/saticilar/one-cikan`
- Urun karti gorseli: `urun.resimler[0].url`
- Satici rozetleri: `saticiDogrulanmis`, `saticiMagazaAdi`, `saticiSehir`, `saticiIlce`

Tasarim siniri:

- Ana sayfada canli kampanya, kupon, kargo takibi, odeme veya siparis durumu gosterilmemelidir; bu API'de yoktur.

## Urun Kesif Ve Detay

Desteklenen alanlar:

- Galeri: `resimler`, `videolar`
- Fiyat/stok/kategori/satici bilgisi: `UrunDto`
- Sosyal kanit: `ortalamaPuan`, `toplamPuan`, `toplamYorum`, `toplamFavori`
- Favoriye ekle/cikar: `POST /api/Urun/{urunId}/favori`, `DELETE /api/Urun/{urunId}/favori`
- Favoride mi bilgisi: alici login ise `GET /api/Urun/favorilerim` sonucu urun id ile eslestirilir.
- Talep olustur: `POST /api/Talep`
- Saticiya yaz: `POST /api/Chat/messages/{receiverId}` veya SignalR `SendMessage`
- Satici guven skoru: `GET /api/Profil/satici/{saticiId}/guven-skoru`

Tasarim siniri:

- Sepete ekle, hemen ode, kargo sec veya stok rezervasyonu ana aksiyon yapilmamalidir. Bu API su an talep/teklif pazarligi modelindedir.

## Alici Paneli

Desteklenen alanlar:

- Profilim ve konum/gorunen ad duzenleme: `GET /api/Profil/alici`, `PUT /api/Profil/alici` (`userName`, `sehir`, `ilce`, `adres`, `favoriKategori`)
- Onerilen urunler (konum ve kategori bazli): `GET /api/Urun/onerilen`
- Favoriler: `GET /api/Urun/favorilerim`
- Taleplerim (durum ve sayfalama destekli): `GET /api/Talep/benim?durum=&page=&pageSize=`
- Talep detay: `GET /api/Talep/{talepId}`
- Talep karti urun/satici gorseli: `urunResimUrl`, `urunAdi`, `urunFiyat`, `saticiMagazaAdi`, `saticiSehir`, `saticiIlce`
- Teklif kabul/red ve talep iptal: `POST /api/Talep/teklif/{teklifId}/kabul`, `POST /api/Talep/teklif/{teklifId}/red`, `POST /api/Talep/{talepId}/iptal`
- Mesajlar: `GET /api/Chat/conversations`, `GET /api/Chat/messages/{otherUserId}`
- Puan/yorum yetki kontrolu: `GET /api/Urun/{urunId}/yorum-yetkisi` (`yorumYapabilir`, `puanlayabilir`, `mevcutPuan`)
- Puan/yorum: sadece anlasilmis talep sonrasi `POST /api/Puan/puan-ekle`, `POST /api/Yorum`

Tasarim siniri:

- Siparis/fatura/odeme gecmisi tasarlanmamalidir; talep kabul edilince sadece `ANLASILDI` olur.

## Satici Paneli

Desteklenen alanlar:

- KPI kartlari: `GET /api/Dashboard/satici`
- Profil (magaza hikayesi + kapak resmi yukleme destekli): `GET /api/Profil/satici`, `PUT /api/Profil/satici` (JSON veya multipart `kapakResim`)
- Urunlerim: `GET /api/Urun/urunlerim`
- Urun ekle/guncelle: `POST /api/Urun/urun-ekle`, `PUT /api/Urun/{urunId}`
- Medya yukle: multipart `Resimler`, `Videolar`
- Medya sil: `DELETE /api/Urun/{urunId}/resimler/{resimId}`, `DELETE /api/Urun/{urunId}/videolar/{videoId}`
- Aktif/pasif: `PATCH /api/Urun/{urunId}/status`
- Gelen talepler (durum ve sayfalama destekli): `GET /api/Talep/satici?durum=&page=&pageSize=`
- Teklif ver/guncelle: `POST /api/Talep/{talepId}/teklif`
- Chat: Chat REST + SignalR endpointleri

Tasarim siniri:

- Satici panelinde kategori CRUD gosterilmemelidir; kategori yazma `ADMIN` rolundedir.
- Kargo, odeme veya kupon yonetimi gosterilmemelidir.

## Admin Paneli

Desteklenen alanlar:

- KPI ozeti: `GET /api/Dashboard/admin`
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
