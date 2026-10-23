# ROOT ACCESS

Tek ekranlı, mobil odaklı interaktif idle oyun demosu. HTML, CSS ve JavaScript ile çalışır; kurulum veya build adımı gerektirmez.

## Tarayıcıda oyna

**[ROOT ACCESS demosunu aç](https://zalpdomanic.github.io/RootAccessUI/)**

Bu bağlantı doğrudan oyunu açar. Arkadaşlarınızın dosya indirmesi veya GitHub hesabına giriş yapması gerekmez. İlerleme her oyuncunun kendi tarayıcısında saklanır.

## Çalıştırma

Depoyu indirin veya klonlayın, ardından `index.html` dosyasını tarayıcıda açın. Yerel sunucu kullanmak isterseniz depo klasöründe `python -m http.server 8080` çalıştırıp `http://localhost:8080` adresini açabilirsiniz.

## Demo

- HACK ile para kazanın, barı doldurup geçici iş hızı bonusu alın.
- REP 1'den başlayın; yeni işler REP 2, 3 ve 4'te açılır. REP seviyeleri devam eder.
- JOBS menüsünden işleri çalıştırın, fiyatı görünen AUTO + düğmesiyle otomasyon satın alın.
- İşleri 1×, 10×, 100× veya BUY MAX ile yükseltin.
- Server rack'e dokunup ekipman kurun. Rack başına 5 raf bulunur; menüde aynı anda 3 raf gösterilir, diğerlerine kaydırarak erişilir.
- Yerdeki + alanından ikinci bir 5 raflı rack satın alın.

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
