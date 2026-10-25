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

Depoyu indirin veya klonlayın, ardından `index.html` dosyasını tarayıcıda açın. Yerel sunucu kullanmak isterseniz depo klasöründe `python -m http.server 8080` çalıştırıp `http://localhost:8080` adresini açabilirsiniz.

## Demo

- HACK ile para kazanın. Kırık beyaz buton 10 dokunuşta kırmızıyla dolar ve geçici iş hızı bonusu verir; hız çarpanı butonun üzerinde görünür.
- REP 1'den başlayın; yeni işler REP 2, 3 ve 4'te açılır. REP seviyeleri devam eder.
- JOBS menüsünden işleri çalıştırın, fiyatı görünen AUTO + düğmesiyle otomasyon satın alın.
- JOBS başlığındaki küçük halkalar işlerin gerçek ilerlemesini gösterir; her tamamlanmada yeni tura başlar. Hazır işler ve kilitli işler sönük kalır.
- Para panelindeki kum saatli AFK butonu kazanç penceresini açar. Otomatik rigler yalnızca oyun kapalı/arka plandayken AFK kasasına kazandırır; açıkken gelir cüzdana geçer. AFK süresi saat/dakika/saniye gösterilir ve oyun açıkken durur. Kasa tamamlanan her AFK dakikasında güncellenir, toplam 8 saatte dolar.
- Bir AFK dakikasından sonra Collect ile çekim yapılabilir. Watch ad · 2×, açıkça etiketlenen 5 saniyelik demo reklam önizlemesinden sonra iki kat öder; gerçek reklam ağı bağlı değildir. Önizleme arka planda duraklar, iptal edilirse ödeme yapılmaz. Alınmayan kazanç ve yarım kalan dakika kayıtta korunur.
- JOBS okuna dokununca liste doğrudan tam açılır. Aşağı ok veya başlığı aşağı kaydırma menüyü kapatır. Üstteki para paneli ve alttaki HACK kontrolleri sabit kalır.
- İşleri 1×, 10×, 100× veya BUY MAX ile yükseltin.
- Server rack'e dokunup ekipman kurun. Rack, ekipman kurulum ve yerdeki + ile açılan rack satın alma menüleri JOBS gibi HUD ile HACK arasındaki alanı tamamen kaplar. Rack başına 5 raf bulunur; uzun içerikler menü içinde kaydırılır.
- Yerdeki +, Expand Setup menüsünü açar: ikinci bir 5 raflı rack satın alın, bir otomasyon işi seçin veya boş rafa fiber modem kurun. Fiyatlar kartlarda, satın alma düğmelerinden ayrı gösterilir.
- JOBS, SERVER RACK ve EXPAND SETUP aynı koyu kartlar, ince çerçeveli düğmeler ve sıcak sarı fiyatları kullanır. İş ve ekipman fiyatları aksiyonlardan ayrı gösterilir; uzun listeler menü içinde kaydırılır.

İlerleme tarayıcının localStorage alanında saklanır. Settings → Reset Demo ile sıfırlanır. Demodaki bütün tutarlar oyun içi sanal paradır.

## Kaynaklar ve doğrulama

- `index.html`: ana ekran ve menüler
- `style.css`: mobil yerleşim ve tipografi
- `app.js`: arayüz etkileşimleri ve kayıt
- `engine.js`: oyun ekonomisi, REP, otomasyon ve rack kuralları
- `assets/`: oda, rack ve yerel font dosyaları
- `test-engine.cjs`: oyun kuralı kontrolleri

Node.js kuruluysa `node test-engine.cjs` ile kontroller çalıştırılabilir. Oyun Node.js gerektirmez.

GitHub Pages, `main` dalının kök klasöründen yayınlanır. Bu dala gönderilen güncellemeler otomatik olarak oyun bağlantısına yansır.

Arayüzde başlık/aksiyonlar Pixel Operator Bold, para ve sayaçlar Share Tech Mono kullanır. Comfort ve HACK çarpanı Pixel Operator Mono Bold olarak vurgulanır. HACK yazısı SVG olarak korunur. Font dosyaları ve CC0 lisansı `assets/fonts/pixel-operator/` klasöründedir.
