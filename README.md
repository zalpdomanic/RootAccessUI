# ROOT ACCESS

Tek ekranlı, mobil odaklı interaktif idle oyun demosu. HTML, CSS ve JavaScript ile çalışır; kurulum veya build adımı gerektirmez.

## Unity UI paketi

Unity için düzenlenebilir UI kaynakları ve **0.3.0-preview.1** kurulum paketleri artık bu depoda:

- [Tam UI + Loading paketini indir](https://github.com/zalpdomanic/RootAccessUI/raw/refs/heads/main/unity/packages/RootAccessUI-URP-Assets-0.3.0-preview.1.unitypackage)
- [Yalnız Loading paketini indir](https://github.com/zalpdomanic/RootAccessUI/raw/refs/heads/main/unity/packages/RootAccessUI-Loading-Standalone-0.3.0-preview.1.unitypackage)
- [Unity kurulumu ve ekip entegrasyonu](unity/README.md)

Unity sürümü; dinamik job/upgrade listelerini, sade REP görünümünü, job halkalarından RUN ve basma geri bildirimini, başlangıç/reklam loading ekranlarını içerir. Bu bölüm Unity teslimidir; aşağıdaki tarayıcı demosu ayrı çalışır.

## Tarayıcıda oyna

**[ROOT ACCESS demosunu aç](https://zalpdomanic.github.io/RootAccessUI/)**

Bu bağlantı doğrudan oyunu açar. Arkadaşlarınızın dosya indirmesi veya GitHub hesabına giriş yapması gerekmez. İlerleme her oyuncunun kendi tarayıcısında saklanır.

## Çalıştırma

Depoyu indirin veya klonlayın, depo klasöründe `python -m http.server 8080` çalıştırıp `http://localhost:8080` adresini açın. Reklam hazırlığı yerel bir JSON kaynağı yüklediği için yerel HTTP sunucusu kullanın.

## Demo

- HACK ile para kazanın. Kırık beyaz buton 10 dokunuşta kırmızıyla dolar ve geçici iş hızı bonusu verir; hız çarpanı butonun üzerinde görünür.
- REP 1'den başlayın; yeni işler REP 2, 3 ve 4'te açılır. REP seviyeleri devam eder.
- JOBS menüsünden işleri çalıştırın, fiyatı görünen AUTO + düğmesiyle otomasyon satın alın.
- JOBS başlığındaki küçük halkalar işlerin gerçek ilerlemesini gösterir. Hazır manuel işte parlamayan koyu yüzey ve aralıklı küçük bir hareketle dokunmaya davet eden sarı **▶**, otomatik işte **AUTO**, çalışan işte kalan saniye görünür. Play hareketi halkalarda farklı zamanlarda çalışır; iş başlayınca veya hareket azaltma tercihi açıkken durur. Hazır halkaya dokunarak işi başlatabilirsiniz; basarken küçük bir hareket olur. Otomatik, çalışan veya kilitli işler yeniden tetiklenmez. Halka alanı yeni işler eklendiğinde yatay kaydırılabilir.
- Ana ekrandaki **SHOP** üç kategori açar: **PC** dokunuş/tam bar kazancını, **EQUIPMENT** iş ödemelerini ve hızını, **FURNITURE** Comfort üzerinden iş ve AFK kazançlarını geliştirir. Ürünlerin fiyatı, seviyesi, REP kilidi ve sahiplik durumu gerçek oyun verisine bağlıdır. Ortak kartlar bir kez oluşturulur; canlı değerler aynı öğelerde güncellenir.
- Yeni mobilyadan gelen her Comfort puanı iş çarpanına +%2 puan ve AFK gelirine +%3 ek çarpan sağlar; dokunuş kazancını değiştirmez. Mevcut başlangıç Comfort etkisi korunur. Mobilyalar bu demoda kalıcı bonus olarak çalışır; oda görseline yeni model yerleştirmez. Fiyat ve bonuslar demo denge değerleridir.
- REP yalnız level ve ilerleme barıyla gösterilir; uzun sayılar COMFORT alanına taşmaz.
- Oyun açılırken ortak oda görselini kullanan loading ekranı, gerçekten yüklenen görsel/font kaynaklarını takip eder. Süre bilinmiyorsa dönen simit gösterilir. Settings içindeki LOADING PREVIEW ile ekran tekrar görülebilir.
- Para panelindeki kum saatli AFK butonu kazanç penceresini açar. Otomatik rigler yalnızca oyun kapalı/arka plandayken AFK kasasına kazandırır; açıkken gelir cüzdana geçer. AFK süresi saat/dakika/saniye gösterilir ve oyun açıkken durur. Kasa tamamlanan her AFK dakikasında güncellenir, toplam 8 saatte dolar.
- Bir AFK dakikasından sonra Collect ile çekim yapılabilir. Watch ad · 2× önce Preparing ad panelini açar. Demo reklam kaynağı yüklenemezse veya zaman aşımı olursa Ad unavailable panelinde Try again / Back to game görünür. Ardından açıkça etiketlenen 5 saniyelik demo reklam önizlemesi tamamlandığında iki kat öder; gerçek reklam ağı bağlı değildir. Hata, yeniden deneme ve iptal ödeme yapmaz. Önizleme arka planda duraklar. Alınmayan kazanç ve yarım kalan dakika kayıtta korunur. Settings → AD ERROR PREVIEW yalnız hata/yeniden deneme görünümünü gösterir; ödül vermez.
- JOBS okuna dokununca liste doğrudan tam açılır. Aşağı ok veya başlığı aşağı kaydırma menüyü kapatır. Üstteki para paneli ve alttaki HACK kontrolleri sabit kalır.
- İşleri 1×, 10×, 100× veya BUY MAX ile yükseltin.
- **SHOP → EQUIPMENT → MY EQUIPMENT** kurulu rack ve rafları açar. Rack başına 5 raf bulunur; uzun içerikler menü içinde kaydırılır.
- **SHOP → EQUIPMENT → RACKS & AUTO +**, Expand Setup menüsünü açar: ikinci bir 5 raflı rack satın alın, bir otomasyon işi seçin veya boş rafa fiber modem kurun. Fiyatlar kartlarda, satın alma düğmelerinden ayrı gösterilir. Odadaki tıklama hedeflerinin yerine Shop kullanılır; bu menülerdeki geri düğmesi Equipment kategorisine döner.
- JOBS, SERVER RACK ve EXPAND SETUP aynı koyu kartlar, ince çerçeveli düğmeler ve sıcak sarı fiyatları kullanır. İş ve ekipman fiyatları aksiyonlardan ayrı gösterilir; uzun listeler menü içinde kaydırılır.

İlerleme tarayıcının localStorage alanında saklanır. Settings → Reset Demo ile sıfırlanır. Demodaki bütün tutarlar oyun içi sanal paradır.

## Kaynaklar ve doğrulama

- `index.html`: ana ekran ve menüler
- `style.css`: mobil yerleşim ve tipografi
- `app.js`: arayüz etkileşimleri ve kayıt
- `engine.js`: oyun ekonomisi, REP, otomasyon ve rack kuralları
- `shop.js` / `shop.css`: üç kategorili Shop, ortak ürün kartları, hafif yuvarlak köşeler ve job halkalarının durumları
- `loading.js` / `loading.css`: tekrar kullanılan startup/reklam panelleri, iptal, zaman aşımı ve eski callback koruması
- `assets/demo-ad.json`: gerçek reklam servisinden bağımsız yerel demo kaynağı
- `assets/`: oda, rack ve yerel font dosyaları
- `test-engine.cjs`: oyun kuralı kontrolleri

Node.js kuruluysa `node test-engine.cjs` (40 oyun kuralı), `node test-shop.cjs` (16 satın alma, bonus ve kayıt kontrolü) ve `node test-loading.cjs` (7 yükleme yaşam döngüsü kontrolü) çalıştırılabilir. Oyun Node.js gerektirmez. Shop kaydı eski ilerlemeyi koruyarak eklenir. Bu web güncellemesi Unity paketlerini değiştirmez.

GitHub Pages, `main` dalının kök klasöründen yayınlanır. Bu dala gönderilen güncellemeler otomatik olarak oyun bağlantısına yansır.

Arayüzde başlık/aksiyonlar Pixel Operator Bold, para ve sayaçlar Share Tech Mono kullanır. Comfort ve HACK çarpanı Pixel Operator Mono Bold olarak vurgulanır. HACK yazısı SVG olarak korunur. Font dosyaları ve CC0 lisansı `assets/fonts/pixel-operator/` klasöründedir.
