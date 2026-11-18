# Frontend entegrasyonu

Geliştirme adresi `http://localhost:5196`, API öneki `/api/v1`. Her geliştiricinin localhost adresi kendi bilgisayarını gösterir. Swagger arayüzü `/swagger`; sözleşme `/swagger/v1/swagger.json` veya depodaki [OpenAPI dosyası](openapi.json). İsteklerde JSON sayıları noktalı ondalık gösterimle gönderilir. Ekranda Türkçe gösterim için Intl.NumberFormat("tr-TR") kullanabilirsiniz.

## Hesap ekranı

```js
const yanit = await fetch(apiAdresi + "/api/v1/hesaplamalar/onizleme", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ buyukluk: 6, derinlikKm: 10, bolge: "TR" })
});
const veri = await yanit.json();
if (!yanit.ok) throw new Error(veri.title);
```

Önizleme veritabanına yazmaz. Sonuç kartında `durum`, `tahmin`, `sinif.ad`, `surum` gösterin. Detayda `logDerinlik`, `pay`, `katsayi`, `bolen` kullanılabilir. Sınıfı API'den okuyun. [Örnek yanıtın tamamı](sentetik-sonuc.json).

## Ekranlar ve istekler

| Ekran | Yol (API öneki altında) | Erişim |
|---|---|---|
| Bölge ve model seçimi | GET /bolgeler ve GET /bolgeler/{kod}/siniflar | Açık |
| Olay kataloğu | GET /depremler?sayfa=1&adet=25&artan=false | Açık; saglayici filtresi |
| Olay detayı | GET /depremler/{id} ve GET /depremler/{id}/hesaplamalar | Açık |
| Hesap detayı | GET /hesaplamalar/{id} | Açık |
| Olay kaydet / düzelt | POST /depremler veya PUT /depremler/{id} | Yönetim |
| Proje uyarıları | GET /uyarilar?onaylandi=false ve GET /uyarilar/{id} | Açık |
| İncelendi işaretle | POST /uyarilar/{id}/onay | Yönetim |
| Bina envanteri | POST /binalar ve GET /binalar/{id} | Yönetim |
| Belge türleri | GET /binalar/belge-turleri | Yönetim |
| Belge incelemesi | POST /binalar/{id}/degerlendirmeler | Yönetim |
| İnceleme detayı | GET /binalar/{id}/degerlendirmeler/{incelemeId} | Yönetim |

Yönetim çağrıları için kullanıcının oturumunu ve yetkisini denetleyen güvenilir bir sunucu üzerinden `X-Yonetim-Anahtari` başlığı ekleyin. Anahtarı tarayıcı paketine koymayın. Salt tarayıcı uygulaması açık uçları doğrudan kullanabilir. Bina envanterinde kişi bilgisi toplamaktan kaçının.

## Sentetik olay isteği

```json
{"model":{"buyukluk":6,"derinlikKm":10,"bolge":"TR"},"buyuklukTuru":"Mw","enlem":39,"boylam":32,"yer":"Sentetik saha","olusZamani":"2025-01-01T12:00:00Z","saglayici":"Sentetik","kaynakKimligi":"ornek-001"}
```

POST 201 ve Location başlığıyla depremId, hesapId, uyariId (eşik altında null) ve sonuc döndürür. Liste öğeleri id, zaman, veri alanlarından oluşur. Zamanlar ISO 8601 biçimindedir. Katalog olay zamanına göre sıralanır; zaman alanı kaydın alındığı andır. Son sayfada adet değerinden az sonuç gelebilir. Geçersiz istek 400, yönetim yetkisi eksikse 401, bulunamayan kayıt 404, yinelenen olay 409, istek sınırı 429 döner. Hata gövdesinden title ve status okuyun.

## Sentetik bina isteği

```json
{"ad":"Sentetik bina","konum":"Kurgusal saha","betonSinifi":"C30","yapimYili":2000,"katSayisi":4,"tasiyiciSistem":"Betonarme çerçeve","belgeler":[],"yonetmelik":null}
```

Değerlendirme POST isteğinin gövdesi yoktur. Yanıttaki veri.durum, veri.bildirilmeyenBelgeler ve veri.dayanak kullanıcıya birlikte gösterilir. Altı belge türü bildirilmiş olsa bile sonuç uzman incelemesine yönlendirir.

