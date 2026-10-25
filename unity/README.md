# Root Access — Unity UI

Sürüm **0.3.0-preview.1** · 25 Ekim 2026

Mevcut URP oyununa eklenebilen, native uGUI / TextMesh Pro bileşenleri ve oyun verisi için adaptör arayüzü. Bu klasör tek başına açılacak bir Unity projesi değildir; kaynaklar ve import paketleri içerir.

## İndir

| Paket | İçerik | Boyut |
|---|---|---|
| [Tam UI + Loading](https://github.com/zalpdomanic/RootAccessUI/raw/refs/heads/main/unity/packages/RootAccessUI-URP-Assets-0.3.0-preview.1.unitypackage) | Dinamik UI, 39 ekran kaydı, 4 loading durumu, prefab ve örnekler | 6,7 MB |
| [Yalnız Loading](https://github.com/zalpdomanic/RootAccessUI/raw/refs/heads/main/unity/packages/RootAccessUI-Loading-Standalone-0.3.0-preview.1.unitypackage) | Başlangıç, reklam hazırlanması, hata/tekrar deneme ve gereken ortak kaynaklar | 2,3 MB |

İhtiyacınıza göre **bir paket** seçin. İkisinde de README, prefab ve örnek sahne vardır. Aynı yollar/GUID'ler korunur; standalone ardından tam paketi import etmek ortak dosyaları günceller. Dosyaları yeniden adlandırıp ikinci bir kopya oluşturmayın. Projenizde bu kaynakları değiştirdiyseniz güncellemeyi sürüm kontrolü üzerinden karşılaştırın.

## Kurulum

Doğrulanan ortam: **Unity 6000.6.6f1, URP 17.6.0, uGUI 2.6.0, Input System 1.20.0**. TextMesh Pro Essential Resources gerekir. Başka Unity sürümleri için uyumluluk testi yapın.

1. Unity'de `Assets > Import Package > Custom Package` ile seçtiğiniz dosyayı import edin.
2. Kaynaklar `Assets/RootAccessUI` altına gelir. Kamera, URP asset'i ve global ProjectSettings pakete dahil değildir.
3. Tam UI için [README-FIRST.txt](Assets/RootAccessUI/README-FIRST.txt), loading için [LOADING-README.txt](Assets/RootAccessUI/LOADING-README.txt) dosyasını izleyin.

Kaynak üzerinden entegrasyon tercih ediliyorsa bu klasördeki `Assets/RootAccessUI` ve `Assets/RootAccessUI.meta` dosyasını projenizin Assets klasörüne taşıyın; tüm `.meta` dosyalarını koruyun. Paket importu ile kaynak kopyalamayı iki farklı yola uygulamayın.

## Örnekler ve bağlantı noktaları

- Tam UI: `Prefabs/RootAccessUIHost.prefab`; `IRootAccessUiAdapter` ile oyun verisini bağlayın.
- Veri örneği: `Samples/Dynamic Collections/RootAccessDynamicExample.unity`.
- Loading: `Prefabs/RootAccessLoading.prefab`; `RootAccessLoadingHost.BeginStartup`, `BeginAd`, `ReportProgress`, `Complete`, `Fail` çağrılarını kullanın.
- Loading örneği: `Samples/Loading/RootAccessLoadingDemo.unity`.

Yollar `Assets/RootAccessUI/` altındadır. Üretim prefab'ı ikinci bir EventSystem oluşturmaz; oyunun mevcut EventSystem'ını kullanın. Örnek ekonomi ve sahte yükleme süreleri yalnız demo içindir. Gerçek reklam SDK'sı, backend, ödül doğrulaması ve oyun kuralları ekip tarafından bağlanır. UI API'leri Unity main thread üzerinde çağrılır.

## Son değişiklikler

- Veriden üretilen job/upgrade kartları ve yatay kaydırılan job halkaları.
- Manuel job halkasına dokunarak RUN ve küçük basma geri bildirimi.
- REP level + ilerleme barı; uzun sayılar COMFORT alanına taşmaz.
- Loading için ortak startup ve reklam görünümü; durum değişiminde aynı ağaçlar kullanılır.
- İlerleme, belirsiz süre simidi, zaman aşımı, iptal, yeniden deneme ve eski callback'leri reddeden ticket sistemi.
- AFK paneliyle uyumlu reklam kutuları ve teal/gold butonlar.

## Doğrulama

17 loading davranış testi, dört Figma/Unity görsel karşılaştırması ve uzun ekran/tablet yerleşim kontrolü geçti. 20 tekrar açılışta nesne/material sayısı artmadı. Standalone paket temiz URP doğrulama projesine, ardından tam paket üzerine import edildi: eksik script yok, ortak GUID'ler korundu. Önceki paketin kaynakları korundu; loading ve belgeleri eklendi.

[Doğrulama özeti ve SHA-256](docs/validation.json). Bu sonuçlar fiziksel telefon GPU/FPS ölçümü veya gerçek reklam SDK testi değildir. Görsel referans Gamma ortamında kontrol edildi; hedef oyunun Linear renk uzayında transparan karışımı ayrıca kontrol edin. Figma kimliği için projede onaylanan live-readback + SHA-256 yöntemi kullanıldı; canonical v1.1 snapshot geçişi iddia edilmez.

Font ve üçüncü taraf lisansları [THIRD-PARTY-NOTICES.txt](Assets/RootAccessUI/THIRD-PARTY-NOTICES.txt) ve ilgili kaynak klasörlerindedir.
