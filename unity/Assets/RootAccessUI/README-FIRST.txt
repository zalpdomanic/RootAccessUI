ROOT ACCESS UI — URP ASSET TESLİMİ
Sürüm: 0.3.0-preview.1

Bu .unitypackage, Unity'nin Assets > Import Package > Custom Package menüsüyle içe aktarılır.
Dosyalar Assets/RootAccessUI altına gelir. UPM / package.json kurulumu gerekmez.

İÇİNDEKİLER
- Düzenlenebilir native Unity UI (uGUI / TMP) renderer ve 39 ekran tasarım kaydı.
- Oyun verisiyle çoğalan job / upgrade kartları ve yatay kaydırılan job halkaları.
- REP görünümü: level + ilerleme barı; sayısal ilerleme/hedef metni gizli, COMFORT korunur.
- Prefabs/RootAccessUIHost: kendi oyun adaptörünüz için boş UI host.
- Prefabs/RootAccessUIWithSampleData: kendi URP sahneniz üzerinde örnek verilerle hızlı deneme.
- Samples/Dynamic Collections/RootAccessDynamicExample: referans odayla tam örnek sahne.
- Samples/Integration Example: küçük asenkron bağlantı örneği.

KURULUM
1. Oyunun mevcut URP projesinde Package Manager üzerinden Unity UI (uGUI) ve Input System
   paketlerinin kurulu olduğunu doğrulayın. TMP, kullanılan Unity sürümünde ayrıca kuruluyorsa
   TextMesh Pro'yu da kurun. Test ortamı: Unity 6000.6.6f1, URP 17.6.0, uGUI 2.6.0,
   Input System 1.20.0. Daha eski Unity/URP sürümleri için uyumluluk testi gerekir.
2. Bu unitypackage dosyasını içe aktarın. Aynı projede eski Assets/RootAccessUI kopyası veya
   com.rootaccess.ui UPM paketi varsa ikisini bir arada kullanmayın; aynı GUID ve kaynak yolları vardır.
   Mevcut değişikliklerinizi yedekleyip hangi dağıtımı kullanacağınızı seçin.
3. TextMesh Pro Essential Resources yoksa Tools > Root Access UI > Import TMP Essential Resources
   if missing menüsünü kullanın. Import penceresini onaylayın. Bu adım hazır fontların referans
   verdiği standart TMP shader'ını sağlar. Mevcut TMP ayarlarını otomatik olarak değiştirmiyoruz.
4. Örneği çalıştırmak için Active Input Handling: Input System Package (New) veya Both olmalıdır.
   Örnek bootstrap EventSystem yoksa InputSystemUIInputModule ekler. Gerçek oyun entegrasyonunda
   kendi EventSystem / input modülünüzü kullanın; iki EventSystem oluşturmayın.
5. Samples/Dynamic Collections/RootAccessDynamicExample.unity sahnesini açıp Play'e basın.
   Varsayılan sekiz job vardır. Halkaları yatay sürükleyin, kartlarda RUN / AUTO / LEVEL UP deneyin.
   Jobs panelindeki UPGRADES düğmesi dinamik upgrade listesini açar.

KENDİ OYUN SAHNENİZE EKLEME
Hızlı görünüm denemesi: RootAccessUIWithSampleData prefab'ını mevcut URP sahnenize sürükleyin.
Bu prefab referans odayı kapalı tutar; oyunun kendi kamerası altta görünür. Örnek ekonomi RAM'dedir.

Gerçek bağlantı: RootAccessUIHost prefab'ını sahneye koyun ve kendi controller'ınızda:

  using RootAccess.UI.Integration;
  public RootAccessUiHost ui; // Inspector'dan prefab üzerindeki component'i bağlayın.
  void Start() { ui.Mount(yourGameAdapter); }

yourGameAdapter, IRootAccessUiAdapter arayüzünü uygular:
  UiFrame Capture();
  event Action Changed;
  Task<UiCommandResult> ExecuteAsync(UiCommand command, CancellationToken cancellation);

Capture, oyun state'ini UiFrame.Jobs / UiFrame.Upgrades / Text / Fills / Actions alanlarına
çevirir. Liste değişince Changed tetiklenir. Butonlar sabit dizi indeksine değil benzersiz Id'ye
komut gönderir. Kaynak fiyat, kilit, level, otomasyon ve satın alma kararı oyun/backend'e aittir.
DynamicExampleAdapter.cs çalışan örnektir; içindeki sahte ekonomi formüllerini üretime taşımayın.
Host tek başına, Mount çağrılmadan görünüm üretmez. UI hiyerarşisi Mount sırasında oluşturulur;
tasarım şablonları Runtime/Resources/RootAccessUI/Screens içindedir.

URP VE SAHNE SINIRI
- UI Screen Space Overlay Canvas kullanır. URP renderer'a feature, kamera stack veya shader
  değişikliği eklemek gerekmez. Game camera, URP asset, Quality ve Player Settings pakete dahil değildir.
- RootAccessUiHost.ShowReferenceEnvironment=false gerçek oyun sahnesi içindir ve prefab'larda
  varsayılandır. Kamera/layer oluşturmaz; ortam ve karakter animasyonları oyuna ait kalır.
- ShowReferenceEnvironment=true yalnız referans oda / demo modu içindir; layer 31 kullanır.
- Gerçek sahne modunda modal arka planı karartılır. Canlı 3D sahne blur'u bu pakette uygulanmaz.
- UI dünyadaki input'u veya Time.timeScale'i yönetmez. Modal açılınca oyun dünyasının input'unu
  oyun tarafında yönetin. Host kapatılıp yeniden açılırsa yeniden Mount çağırın.
- Tasarım görsel referansı Gamma renk uzayında doğrulandı. Kendi URP projenizin Linear ayarını
  sırf UI için değiştirmeyin; son renk/alpha görünümünü hedef oyunda kontrol edin.

VERİ / İÇERİK SINIRI
Yeni job ve upgrade aynı kart türündeyse yeni Figma çizimi gerekmez. Yeni veri kaydı yeterlidir.
Tamamen yeni mekanik, farklı kart yerleşimi veya yeni ikon için ilgili şablon/asset eklenir.
Dinamik örneğin UPGRADES düğmesi BUY MAX yuvasını kullanır; son oyun navigasyonunu ekip belirler.
0, 4, 12 ve 50 job örneği sınandı; 16 üzerindeki listelerde sanallaştırma/pooling uygulanır. Mobil cihaz performansını ölçün.
39 ekranın görsel kaynakları vardır; tüm ekranlar gerçek backend'e bağlanmış değildir.
Mevcut asıl demo simülasyonu, kullanıcı save dosyaları, gizli bilgiler ve editor test araçları dahil değildir.

ASENKRON BAĞLANTI
Host komut beklerken tekrar basmayı engeller. Sonucu CommandFinished, bekleme durumunu BusyChanged
olaylarından alın. 15 saniyelik timeout sunucunun işlemi yapmadığı anlamına gelmez; adapter sunucuyla
durumu uzlaştırmalıdır. RequestId için sunucu tarafında idempotency kullanın. Otomatik retry yoktur.
Unmount, oturumu iptal eder; sunucu işlemini geri alma garantisi vermez.

DAHA FAZLA
Documentation/DINAMIK-LISTELER.txt: DTO ve veri örnekleri.
Documentation/screen-binding-catalog.json: 39 ekran için node kimlikleri ve adları.
Tools > Root Access UI > Check URP and font prerequisites: ayar değiştirmeden önkoşul kontrolü.

0.2.0-preview.4 — HALKADAN RUN
Manuel job halkası kartın UiJob.Run Id, Quantity ve Enabled bilgisini kullanır; aynı komutu job Id ile gönderir.
Hazır manuel halkaya dokunmak job'ı başlatır ve mevcut ekranda kalır. Çalışan veya kilitli manuel halka komut göndermez.
Oyun işlem tamamlanınca Run.Enabled=true döndürmeli ve Changed yayınlamalıdır. Halka yeniden çalıştırılabilir olur.
Otomatik job halkası mevcut select-job navigasyonunu korur. Manuel job detayları Jobs panelinden açılır.
Yatay sürükleme RUN göndermez. Bekleyen işlem sırasında mevcut busy/tekrar basma koruması geçerlidir.

0.2.0-preview.5 — HALKA BASMA EFEKTİ
Job halkaları basılıyken merkezinden yüzde 4 küçülür ve mevcut yüzde 13 parlama efektini kullanır.
Tıklama alanı ve liste yerleşimi sabit kalır. Kısa RUN dokunuşu, istek beklenirken kontrol
pasifleşse bile kısa efektini tamamlar. Sürükleme ve iptal durumunda görünüm eski haline döner.
RootAccessPressFeedback.ReduceMotion=true olduğunda küçülme uygulanmaz; parlama devam eder.
Data adapter veya backend bağlantısında değişiklik gerekmez.

0.2.0-preview.6 — UI PERFORMANS DÜZENLEMESİ
- Aynı görünümlü native şekiller referans sayımıyla ortak immutable material kullanır.
  Boyut, köşe yarıçapları, simit doluluğu ve basılma efekti elementin vertex verisindedir.
  Canvas TexCoord1/2/3 kanalları otomatik etkinleşir. Material değerlerini doğrudan
  değiştirmeyin; bu diğer elementleri etkiler. SetArc/SetRadii/SetPressVisual kullanın.
  Stil düzenlerken paint/effect dizilerini yerinde değiştirmek yerine yeni veri atayın
  ve Configure çağırın. Örnek veri sözleşmesi değişmemiştir.
- Buton listener'ları ve stil uygulamaları aynı veri için yeniden kurulmaz.
  Sabit hedef ve element aramaları önbelleğe alınır. Her snapshot'taki zorunlu
  Canvas.ForceUpdateCanvases kaldırıldı; kaydırma, adapter.Capture çağırmadan işlenir.
- 16'dan fazla kayıt bulunan job, simit ve upgrade listeleri görünür bölge + tampon
  alanla sınırlandırılır. Ekran dışındaki satırlar varyantlarına göre havuzda tutulur
  ve yeniden bağlanır. Havuz başına en fazla 8 bekleyen satır vardır.
  JobCount/RingCount/UpgradeCount veri sayısı; Live*Count aktif satır sayısıdır.
  FindJob/FindRing/FindUpgrade ekran dışında null dönebilir; dönen GameObject'i
  kalıcı oyun kimliği olarak saklamayın. İşlemler her zaman sabit veri Id'siyle yapılır.
  ScrollToJob(id)/ScrollToRing(id)/ScrollToUpgrade(id) ile hedefi görünür yapın;
  görünümü okumadan önce sonraki frame'i bekleyin. FocusJobId de desteklenir.
- Değişen game state için Changed tetikleyin. Örnekteki 10 Hz güncelleme zorunlu
  backend frekansı değildir. Snapshot döndükten sonra içindeki kayıtları değiştirmeyin.
- Bu düzenleme hedef telefon FPS/ısınma/pil ölçümünün yerine geçmez. Unity Profiler
  ve Frame Debugger ile kendi oyun sahnenizde ölçün. Material sayısı draw call sayısı değildir.


0.3.0-preview.1 — LOADING
Başlangıç, reklam hazırlanması ve hata/tekrar deneme modülü eklendi.
Assets/RootAccessUI/LOADING-README.txt dosyasını okuyun.
Mevcut dinamik veri, REP görünümü ve simit/tuş feedback iyileştirmeleri korunur.
