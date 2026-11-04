# Değişiklik Günlüğü (Changelog)

Bu projedeki tüm önemli değişiklikler bu dosyada belgelenecektir.
Biçim [Keep a Changelog](https://keepachangelog.com/tr/1.1.0/) standardına dayanır ve bu proje [Semantik Sürümleme (SemVer)](https://semver.org/lang/tr/) kurallarına uyar.

---

## [0.1.0] - 2026-11-04

**Koy**'un ilk halka açık sürümü! Windows 11 için sıfır tarayıcı (WebView2/Electron içermeyen), tek süreçte çalışan, yay fiziği animasyonlu **Notch (Çentik)** ve Seelen UI modelinde **Dock**.

### Eklenenler (Added)
- **Yay Animasyonlu Çentik (Notch):**
  - Ekranın üst ortasında 236×34 px boşta durum (saat ve pil göstergesi; müzik çalarken albüm kapağı ve %0 CPU tüketen 4 çubuklu dalga ikonu).
  - İçbükey (*concave*) üst kulaklara sahip özel `NotchShape` vektör geometrisi ve `Spring` yay fiziğiyle 640 px genişliğe açılan sekme yapısı.
  - **Müzik Paneli:** `GlobalSystemMediaTransportControlsSessionManager` üzerinden Spotify, tarayıcılar ve medya oynatıcılar için kapak resmi, şarkı/sanatçı bilgisi, zaman çizelgesi (seek) ve çoklu oturum geçişi.
  - **Takvim & Pomodoro Paneli:** Pazartesi başlangıçlı ay görünümü, yerel zamanlayıcı, kronometre ve çalışma/kısa mola/uzun mola durum makineli Pomodoro sayacı.
  - **Raf (Shelf) Paneli:** Dosya veya metinleri çentiğe sürükleyip bırakma (dosyayı çentiğin üzerine sürükleyince Raf sekmesi otomatik açılır) ve dışarı sürükleme.
  - **Kontrol Merkezi & Durum Panelleri:** CoreAudio ses seviyesi, WMI ve DDC/CI donanım parlaklığı, Wi-Fi/Bluetooth radyo anahtarları, Rahatsız Etme durumu, pil yüzdesi ve Open-Meteo hava durumu (tembel/lazy başlatmalı).
  - **OSD HUD Göstergeleri:** Ses veya ekran parlaklığı değiştiğinde çentikte 1.5 saniye beliren 260×40 px yerleşik gösterge (ses için beyaz `#F4F4F5`, parlaklık için sarı `#FACC15`).
- **Seelen Modeli Dock:**
  - İki ayrı yerleşim biçimi: **Yüzen Kapsül** (60 px yükseklik, 18 px köşe yuvarlaklığı) ve **Tam Genişlik Cam (Full-Width Glass)** (44 px yükseklik, solda canlı medya hapı, ortada uygulamalar, sağda Koy menüsü).
  - **Pencere Yaklaşma Davranışları (`DockProximityMode`):** *Çekil / Akıllı Gizlenme (Dodge - Intellihide)*, *Yer Ayır (ReserveSpace - AppBar)* ve *Hep Gizli (AlwaysHidden)*.
  - **Canlı DWM Pencere Önizlemesi:** Dock simgelerinin üzerinde 400 ms durulduğunda `DwmRegisterThumbnail` ile gerçek zamanlı pencere önizlemesi.
  - **Z-Order Kararlılığı & Etkileşim:** Alt+Tab geçişlerinde sabitlenmemiş açık pencerelerin dock sırasını koruma, sol tıkla odaklama/küçültme, orta tıkla yeni pencere açma veya kapatma, özel koyu cam sağ tık menüsü.
  - **Görev Çubuğu Güvenliği:** Dock açıkken birincil (`Shell_TrayWnd`) ve ikincil (`Shell_SecondaryTrayWnd`) görev çubuklarını gizleme; kapanışta, çökmede, kaldırmada veya acil durum düğmesiyle eksiksiz geri yükleme (`TaskbarService.Restore()`).
- **Çoklu Ekran (Multi-Monitor) Mimarisi:**
  - `DisplayLayoutResolver` ve `MonitorService` ile her monitörde kendi DPI ölçeğinde bağımsız Çentik ve Dock pencereleri.
  - **Ekran Bazlı Tam Ekran İzolasyonu:** Yan ekranda tam ekran oyun veya video açıldığında yalnız o ekrandaki Koy gizlenir; ana ekrandaki Çentik ve Dock çalışmaya devam eder (`Win+Shift+S` ekran alıntısı aracı filtreli).
  - `WM_DISPLAYCHANGE` ile ekran takılıp çıkarıldığında pencereleri dinamik oluşturma ve yeniden konumlandırma.
- **Koy Penceresi, Sağ Alt Sistem Menüsü ve İlk Kurulum Sihirbazı:**
  - 880 px genişliğinde, 8 sayfalı (**Genel, Çentik, Dock, Ekranlar, Görünüm, Kısayollar, Performans, Hakkında**) modern ayarlar penceresi (`KoyWindow`).
  - Sağ altta 320 px hızlı popover menü (`SagAltMenuWindow`) ve CsWin32 `Shell_NotifyIcon` tabanlı sistem tepsisi simgesi (`TrayIconService`).
  - İlk açılışta 3 adımlı kurulum sihirbazı (`SetupWizardWindow`).
  - Tam Türkçe (`tr-TR`) ve İngilizce (`en-US`) anlık dil desteği (`Loc.Instance`).
  - **Marka Kimliği:** Konsept C "Koy Rozeti" (Windows 11 Fluent Squircle + Oyulmuş Çentik + K Monogramı) vektör logosu ve çok katmanlı `.ico` varlık kiti.
  - **Dağıtım & CI/CD:** Velopack kurulum paketi (`Koy-win-Setup.exe`), taşınabilir paket (`Koy-win-Portable.zip`), `SHA256SUMS.txt` doğrulaması ve GitHub Actions otomatik derleme/yayın iş akışları.

[0.1.0]: https://github.com/brhnshn/koy/releases/tag/v0.1.0
