ROOT ACCESS — LOADING UI
Sürüm: 0.3.0-preview.1

İKİ TESLİM
RootAccessUI-URP-Assets-0.3.0-preview.1.unitypackage: mevcut UI + loading.
RootAccessUI-Loading-Standalone-0.3.0-preview.1.unitypackage: yalnız loading,
gereken ortak renderer, shader, font ve oda görselleri, prefab ve örnek sahne.
İki paket aynı dosya yolları ve GUID'leri kullanır. İkisini birden import etmek
zorunlu değildir. Sadece loading eklemek için küçük paketi seçin. Önceden UI
paketini import ettiyseniz dosyaları yeniden adlandırmadan aynı yola import edin.

GEREKSİNİMLER / KURULUM
Unity 6000.6.6f1 + URP 17.6.0 + uGUI 2.6.0 üzerinde doğrulandı.
TextMesh Pro Essential Resources kurulmuş olmalı (Window > TextMeshPro).
Örnek sahne Input System kullanır; mevcut oyunda var olan EventSystem kullanılmalı.
Package kendi URP ayarını, kamera ayarını veya global ProjectSettings'i getirmez.
Başka Unity sürümleri için önce ayrı branch/projede import testi yapın.

1. Assets > Import Package > Custom Package ile .unitypackage dosyasını seçin.
2. Assets/RootAccessUI/Prefabs/RootAccessLoading.prefab'ı sahneye ekleyin.
3. Referansı RootAccessLoadingHost olarak oyun yöneticinizde tutun.
4. Assets/RootAccessUI/Samples/Loading/RootAccessLoadingDemo.unity örneğini inceleyin.
5. LoadingDemo yalnız test verisidir. Gerçek reklam / sahne yüklemesini kendi
   oyununuz bağlar. RootAccessLoadingDemo veya demo bootstrap'ı üretime koymayın.

API — Unity main thread üzerinden çağırın
using RootAccess.UI.Loading;
RootAccessLoadingHost loading;

int ticket = loading.BeginStartup(retryTicket => StartRealLoading(retryTicket));
CancellationToken cancellation = loading.Cancellation;
loading.ReportProgress(ticket, null);  // İlerleme bilinmiyor: dönen simit.
loading.ReportProgress(ticket, 0.37f); // Gerçek 0..1 ilerleme. Yapay sayaç yok.
loading.Complete(ticket);             // Yükleme gerçekten bittiğinde.
loading.Fail(ticket);                 // Hata: yeniden deneme görünümü.

int adTicket = loading.BeginAd(
    retryTicket => RequestYourAdSdk(retryTicket),
    () => ReturnToYourGame(),
    timeoutSeconds: 15);
RequestYourAdSdk(adTicket);

Reklam hazır olduğunda loading.Complete(adTicket) çağırıp SDK'nın kendi reklam
gösterimini başlatın. Reklam yüklenemezse loading.Fail(adTicket) çağırın.
Ödülü SADECE SDK'nın doğrulanmış ödül/tamamlanma olayında oyun/backend verir.
Loading paneli hiçbir zaman para/ödül eklemez; Close işlemi de ödül vermez.

SDK olayınız worker thread'den geliyorsa Unity main thread'e aktarın.
async/await çağrısını Unity main thread'de başlatıp Unity bağlamına döndüğünüzden
emin olun; ConfigureAwait(false) sonrasında UI API'sini doğrudan çağırmayın.
Ticket her yeni açılış/yeniden denemede değişir; eski cevabın yeni paneli kapatması
engellenir. BeginAd/BeginStartup dönüşünü o işlemin callback'ine bağlayın; sonradan
loading.Ticket okuyarak eski işlemi yeni işlem gibi göndermeyin.
Cancellation token timeout, tekrar açılış, Hide, disable/destroy ile iptal olur.
İptal, SDK'nın desteklemediği bir ağ isteğini sihirli şekilde durdurmaz; geç gelen
cevapları ticket ile kontrol etmek yine gereklidir.

DAVRANIŞ / SAHNE SAHİPLİĞİ
Loading kendi ScreenSpaceOverlay Canvas'ını açar (varsayılan SortingOrder=200).
Kamera ve oyun sahnesi değişmez. Reklam panelinin altında mevcut oyun görüntüsü
kalır; loading paketinde HUD/job/oyun mekaniği oluşturulmaz.
Ekranın tamamında pointer click-through engellenir. Klavye/gamepad veya özel
InputAction oyun mekaniklerini ayrıca loading.IsVisible ile siz durdurun.
EventSystem'ı oyun sahibi oluşturur. Üretim prefab'ı ikinci EventSystem oluşturmaz.
Sahne değişirken loading'i korumak isterseniz üst oyun yöneticisinde
DontDestroyOnLoad kullanın; modül bunu kendiliğinden dayatmaz.
Mevcut Canvas sıralamanız 200'den yüksekse SortingOrder'ı ona göre yükseltin.
Kapatma callback'i verilmezse Back to game pasiftir; retry callback'i verilmezse
Try again pasiftir. Ana yükleme başarısızlığında aynı panelde Loading failed yazar.
Varsayılan zaman aşımı: başlangıç 60 saniye, reklam 15 saniye. Süreyi gerçek
işleminize göre Begin... parametresinden ayarlayın.

TEKRAR KULLANIM / PERFORMANS
Bir startup ağacı, bir reklam ağacı ilk kullanımda hazırlanır; sonraki açılışlar
bunları kullanır. Oda/arka plan tek startup ağacındadır. Yüzde ve simit dalları
duruma göre gösterilir. Her yüzde değişiminde prefab/ekran yeniden üretilmez.
Native şekiller mevcut immutable ortak material cache'ini kullanır. Simit hareketi
transform'da; doluluk RectTransform genişliğindedir. Paylaşılan material'ı değiştirmeyin.
Canvas üzerindeki TexCoord1/2/3 kanalları renderer tarafından yönetilir.
Tüm dosyaları iki farklı klasöre kopyalayarak ikinci bir namespace/assembly
oluşturmayın; GUID'leri ve mevcut Assets/RootAccessUI yollarını koruyun.
Hide önbelleği saklar; host destroy edildiğinde Canvas ve kaynak referansları temizlenir.
ReduceMotion simit hareketini kapatır; Animate=false statik görsel test içindir.
Genel tuş feedback azaltma ayarı mevcut RootAccessPressFeedback.ReduceMotion'dır.

GÖRSEL / SINIRLAR
Figma 1080x1920 referansı: Root Access UI — Demo v2 / 04 Loading — Review.
Ekran boyu/safe area için başlık, alt durum ve ortalanan modal ayrı yerleşir.
Mevcut projede sRGB/Gamma görsel karşılaştırması yapıldı. Linear URP projesinde
renderer renk dönüşümünü uygular; kendi sahnenizde transparan karışımı doğrulayın.
Gerçek reklam sağlayıcısı ve backend entegrasyonu bu pakete dahil değildir.
Demo kodundaki süreler yalnız gösterim içindir; üretimde gerçek ilerleme kullanın.
Telefon GPU/FPS/ısınma ölçümü ekip cihazlarında yapılmalıdır.
