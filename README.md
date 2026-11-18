# Deprem Analiz API

Türkiye için önerilmiş bir deneysel dış merkez şiddet modelini inceleyen üniversite projesi. Backend, verilen parametrelerle hesabı yeniden üretmeyi, sınıfın nasıl seçildiğini görmeyi ve manuel girilen olayları izlemeyi sağlar. Bina modülü belge envanteri ve veri yeterliliğini ayrı değerlendirir.

**Frontend geliştiricisi:** [Türkçe entegrasyon rehberi](docs/FRONTEND.md) · [OpenAPI sözleşmesi](docs/openapi.json) · [Gerçek API'den alınmış sentetik örnek yanıt](docs/sentetik-sonuc.json).

## Bilimsel kapsam ve parametreler

I₀ = (M − log₁₀(h) − C) / d. M girilen büyüklük, h kilometre cinsinden odak derinliği, C bölgesel katsayı ve d bölendir. Başlangıç yapılandırması TR, C=0,57, d=0,5 kullanır. Girdi sınırları yazılımsal çalışma sınırlarıdır: −2 ≤ M ≤ 10 ve 0 < h ≤ 800. Sonuç üzerinde kırpma veya sınıflandırma öncesi yuvarlama yapılmaz. Sonlu kayan noktalı değerler kullanılır. Örneğin M=6 ve h=10 için log₁₀(h)=1, pay=4,43 ve deneysel tahmin 8,86 olur.

| Ham tahmin | Proje sınıfı | Varsayılan işlem |
|---|---|---|
| <4 | Proje eşiği altında | Kaydet |
| [4,6) | Hafif–Orta | Göster |
| [6,8) | Güçlü–Çok Güçlü | Proje uyarısı |
| ≥8 | Yıkıcı | Öncelikli proje uyarısı |

Bu sınıfların anlamı proje içindeki inceleme önceliğidir. Henüz gözlemsel kalibrasyon veya hata aralığı yoktur. Sonuç bina hasarını, resmi şiddeti veya acil durum talimatını temsil etmez. [Bilimsel araştırma, kaynaklar ve TBDY kapsamı](docs/BILIMSEL-DAYANAK.md).

## Mimari ve veri modeli

| Dizin | Sorumluluk |
|---|---|
| src/Domain | Saf C# hesaplama, aralık kontrolü ve uyarı kuralı |
| src/Application | İstek/yanıt sözleşmeleri, sağlayıcı arayüzü, bağımsız bina belge incelemesi |
| src/Infrastructure | EF Core, SQL Server, ilişkiler, migration ve saklanan kayıtlardan DTO üretimi |
| src/Api | ASP.NET Core 10 denetleyicileri, işlem koordinasyonu, FluentValidation, Serilog, Swagger ve erişim denetimi |
| tests/Deprem.Tests | xUnit model testleri ve gerçek SQL Server üzerinde HTTP entegrasyon testleri |

Bir olayın birden fazla hesabı olabilir. Sağlayıcı + kaynak olay kimliği benzersizdir. Her hesap, girdileri ve sınıf tanımını o andaki sürümüyle JSON olarak saklar. Bir uyarının bileşik yabancı anahtarı onu doğru olay ve hesaba bağlar. Her olay/kural çifti benzersizdir. Bina başına çok sayıda belge değerlendirmesi saklanabilir. Olaylar, hesaplar, uyarılar, binalar, incelemeler ve işlem izi ayrı tablolardadır. Bina envanteri sürümün tek belgesi olarak JSON şeklinde saklanır. Saatler UTC olarak yazılır. Dosyadaki bölge ve sınıf tanımları dağıtımla sürümlenir.

Kayıt isteği doğrulanır, hesap ve sınıf belirlenir, uyarı kuralı çalışır. Olay, sonuç, gerekiyorsa uyarı ve işlem izi tek SQL işlemi içinde yazılır. Başarılı her olayın hesaplaması vardır. PUT yeni hesap geçmişi üretir. Aynı politikada aynı seviyeye dönüş mevcut uyarıyı korur. Uyarı onayı tekrarlanabilir. Sağlayıcı adı manuel veri kökeni beyanıdır. AFAD ve USGS bağlantısı etkin değildir.

## Windows üzerinde kurulum

.NET 10 SDK, Git ve SQL Server LocalDB gerekir. Başka bir SQL Server için bağlantıyı ortam değişkeni olarak verin. Development ayarları geçerli Windows hesabıyla LocalDB kullanır.

```powershell
sqllocaldb start MSSQLLocalDB
dotnet restore
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project src/Infrastructure --startup-project src/Api
dotnet run --project src/Api --launch-profile http
```

Swagger: http://localhost:5196/swagger . Hazırlık kontrolü: http://localhost:5196/saglik . Yazma işlemlerini kullanacak geliştirici kendi bilgisayarında aşağıdaki komutla güçlü bir yerel anahtar oluşturabilir. Aynı proje için tekrar çalıştırmak mevcut anahtarı değiştirir.

```powershell
$anahtar = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
dotnet user-secrets set YonetimAnahtari $anahtar --project src/Api
```

## Yapılandırma ve işletim

- `ConnectionStrings__Deprem`: SQL Server bağlantısı. Üretimde ayrı migration hesabı ve veri okuma/yazma yetkili uygulama hesabı kullanın.
- `YonetimAnahtari`: güvenilir yönetim sunucusunun sırrı. Tanımsızsa yönetim işlemleri kapalıdır. Tek yönetici yetkisi vardır. İşlem izi servis düzeyindedir.
- `IzinliKokenler__0`: tarayıcı frontend'inin tam origin değeri. Development varsayılanları localhost:5173 ve localhost:3000.
- `ModelDosyasi`: JSON yapılandırma dosyasının yolu. Varsayılan src/Api/model.json. Bölge için katsayı, bölen, sıralı yarı açık sınıflar ve uyarı eşiği buradan yönetilir. Kural değişikliğinde sürümü artırıp yeniden başlatın. Eşik null ise uyarı üretimi kapanır.
- `ASPNETCORE_HTTP_PORTS`: konteyner HTTP portu. İnternete açarken HTTPS sonlandıran ters vekil veya yönetilen platform kullanın. IP başına dakikada 180 istek sınırı vardır. Vekil arkasında limit aynı vekil adresini paylaşabilir.

## Docker ve dağıtım

Docker Engine ve Compose v2 ile geliştirme amaçlı SQL Server Developer ve API birlikte başlar. x86-64 SQL Server imajını çalıştırabilen bir makine gerekir. Git'e eklenmeyen yerel ortamda güçlü bir SQL parolası (bağlantı dizgesi ayıracı içermeyen) ve ayrı yönetim anahtarı tanımlayın.

```powershell
$env:SQL_PAROLA = 'Aa!' + [Guid]::NewGuid().ToString('N')
$env:YONETIM_ANAHTARI = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
docker compose up --build -d
```

Mevcut SQL veri birimi için ilk kurulumdaki parolayı koruyun. Compose önce SQL hazır olana kadar bekler, sonra migration işini ve API'yi çalıştırır. SQL portu dışarı açılmaz. API yerel 5196 portuna bağlanır. Üretim hizmeti için ayrı SQL veritabanı ve uygun lisans kullanın. Dockerfile ile API imajını yayınlayın, aynı imajdan `dotnet Api.dll --goc` çalıştırarak şemayı uygulayın (ENTRYPOINT kullanılan ortamda komut argümanı yalnızca `--goc`). Ardından normal hizmeti başlatıp /saglik ve sentetik önizleme isteğini doğrulayın.

## Testler

```powershell
dotnet build -c Release
dotnet test -c Release --no-build
```

Testler LocalDB üzerinde benzersiz isimli geçici veritabanı açıp kaldırır. Başka sunucuda `TEST_SQL` ile test veritabanı oluşturma ve silme yetkisi olan bir test bağlantısı verilebilir. GitHub Actions Windows üzerinde aynı SQL Server testlerini çalıştırır.

## Sınırlamalar ve sonraki çalışmalar

Denetimli sağlayıcı entegrasyonu, olay bazında bağımsız kalibrasyon, belirsizlik aralıkları, kurumsal kimlik sağlayıcısı ve mühendis onaylı yapısal analiz ileriki çalışma alanlarıdır. Bu sürüm otomatik veri çekmez, dışarıya bildirim göndermez ve bina performans hesabı yapmaz. Canlı dağıtım için barındırma sağlayıcısının ve SQL hizmetinin ayrıca yetkilendirilmesi gerekir.

