# Değişiklik Günlüğü (Changelog)

Bu projedeki tüm önemli değişiklikler bu dosyada belgelenecektir.
Biçim [Keep a Changelog](https://keepachangelog.com/tr/1.1.0/) standardına dayanır ve bu proje [Semantik Sürümleme (SemVer)](https://semver.org/lang/tr/) kurallarına uyar.

---

## [Unreleased]

<!-- release-please sürüm girdisini kendisi üretir. Bu bölüm elle yazılmış ayrıntılı özettir: sürüm PR'ında bu bölümün içeriği yeni sürüm başlığının altına taşınır ve [Unreleased] başlığı silinir (bkz. CONTRIBUTING.md, "Sürüm Nasıl Çıkar"). -->

0.1.0'dan sonraki revizyon: kod incelemesinden çıkan çökmelerin giderilmesi, etkisiz ayarların gerçekten bağlanması, çoklu ekran, kısayollar, Koyu/Açık tema ve kaldırma ekranı.

### Düzeltmeler (Fixed)
- **0.1.0'da çalışmayan, artık çalışanlar:**
  - Kısayollar sayfası yalnızca görüntüydü; artık `RegisterHotKey` ile gerçekten kayıtlı ve değiştirilebilir.
  - Ses ve parlaklık göstergesi (OSD HUD) çentiğe hiç bağlı değildi; artık sistem ses ve parlaklık değişimlerinde açılıyor.
  - Çoklu ekran kullanıcı tarafından açılamıyordu (yalnız birincil ekran); artık Ekranlar sayfasından ekran başına seçiliyor.
  - "1 saat duraklat" yalnızca bir bayrağı değiştiriyordu; artık çentik ve dock'u durduruyor, görev çubuğunu geri getiriyor ve süre bitince devam ediyor.
  - Dock'un varsayılanı "Aşağı yapış" olarak belgelenmişti ama her açılışta "Çekil"e çevriliyordu; artık Aşağı yapış varsayılan ve seçilebilir.
  - "Güncellemeleri otomatik denetle", Pomodoro süreleri, hava durumu yenileme sıklığı, dock orta tık eylemi ve çalışan göstergesi (nokta/çizgi), pil tasarrufunda animasyon azaltma, çentikte tam ekranda gizleme, boşta gösterim, açılma davranışı ve tekerlek eylemi ayarları davranışa bağlandı.
  - "Windows görev çubuğunu gizle" anahtarı kapalıyken dock görev çubuğuna dokunmuyor.
- **Çökmeler:** sağ alt menü ilk tıklamada kapanıyordu (`FailFast`); dock ve çentik kenar zamanlayıcılarındaki başka iş parçacığı erişimi çökmesi giderildi; `crash.log` artık gerçekten yazılıyor ve gözlenmeyen görev hataları süreci düşürmüyor.
- **Görev çubuğu güvenliği:** dock görev çubuğunu iki kez gizleyince "otomatik gizle" kalıntısı kalıyordu; Explorer yeniden başlayınca tepsi simgesi kayboluyor ve görev çubuğu yeniden görünüyordu; "Yer ayır" modunda AppBar kaydı yenilenmiyordu; ilk açılış sihirbazı X ile kapatılınca dock açılıp görev çubuğu gizleniyordu; güncelleme ve kaldırma kancalarında görev çubuğu hata toleranslı biçimde geri getiriliyor.
- UWP pencerelerinde "Sonlandır" `ApplicationFrameHost`'u değil gerçek uygulamayı sonlandırıyor.
- Tam ekran algısı odak başka ekrandayken de her monitörün kendi en üst penceresine bakıyor.
- Ayarlar sıfırlanınca kurulum sihirbazı tekrar açılmıyor; çalışan özellikler yeni varsayılanla yeniden kuruluyor.
- Sabitli uygulama kısayolları çözülürken sızan COM nesnesi serbest bırakılıyor.
- Topluluk katkıları (teşekkürler **xdaxer**):
  - Kontrol Merkezi'ndeki Wi-Fi ve Bluetooth anahtarları artık radyoyu gerçekten açıp kapatıyor; radyo yoksa, kapalıysa ya da Windows izin vermiyorsa döşeme bunu gösteriyor ve Windows ayarlarına yönlendiriyor (#12).
  - Pili olmayan bilgisayarlarda çentikteki pil göstergesi gizleniyor (#8).
  - Dock pencere önizlemesi üst düzey pencereye kaydediliyor; önizleme güncellemesi başarısız olursa sonraki düzende yeniden deneniyor (#10).
  - Açılışta kapalı çentiğin zemini çiziliyor (#13).
  - Açık çentiğin üstünde Müzik, Kontrol, Durum, Takvim ve Raf sekmeleri; kapalı çentikte saatin yanında Wi-Fi ve Bluetooth durum simgeleri (tıklayınca Kontrol sekmesi açılır). Simgeler yalnız Kontrol paneli açıkken ve boşta görünüm "Saat, pil ve bağlantılar" iken çıkar (#14).

### Eklenenler (Added)
- **Genel kısayollar:** çentik, raf, dock, Koy menüsü ve oynat/duraklat için `Ctrl+Alt+N/R/D/K/Space`; tuş yakalama kutusuyla değiştirilir, Windows'un ayırdığı ya da başka uygulamanın tuttuğu kombinasyonlar satırda bildirilir.
- **Ekranlar sayfası:** monitör haritası, ekran başına çentik ve dock, yeni bağlanan ekran için seçenek, "Tanımla" düğmesiyle ekran numarası. Ayarlar kararlı monitör kimliğine bağlıdır; ekran çıkarılıp takılınca geri gelir.
- **Koyu / Açık tema** (ve "Sistem"), yarı saydam malzeme seçeneği, köşe yuvarlaklığı, animasyon hızı; Windows "animasyonları azalt" ve pil tasarrufuna uyum. Emojiler yerine tutarlı çizgi ikonlar, erişilebilirlik adları ve klavye odağı.
- **Koy'u kaldır:** Hakkında sayfasında, onay kartıyla ("Ayarlarımı sakla" varsayılan işaretli). Windows'un "Yüklü uygulamalar" sayfasından kaldırınca da görev çubuğu geri gelir, Windows ile başlatma kaydı silinir ve ayarlarınız korunur. Kurulu olmayan sürümde düğme kapalıdır.
- Hakkında sayfasında gerçek sürüm ve son güncelleme denetimi durumu.
- **Dock sistem tepsisi** (topluluk katkısı, teşekkürler **xdaxer**, #11): Windows görev çubuğu gizliyken Discord, OneDrive gibi arka plan uygulamalarının simgelerine dock'taki düğmeden açılan pencereyle ulaşılır; sol ve sağ tık uygulamaya iletilir. Koy yalnız görev çubuğu gerçekten gizliyken tepsiyi devralır ve her kaydı Windows'a da iletir; Koy kapanınca, duraklayınca ya da çökünce tepsinin eksiksiz geri gelmesi hedeflenir. Dock sayfasından "Sistem tepsisini dock'ta göster" ile kapatılabilir (varsayılan açık). Elle denenmedi.
- Geliştirici aracı: `tools/Koy.UiSnapshots` (arayüzü ekrana çıkarmadan PNG'ye çizer).
- **Takvim:** ay görünümüne ek olarak hafta ve gün görünümü (yerel etkinlikler, `%AppData%\Koy\events.json`; Google/Outlook yok). Gün görünümünde etkinlik eklenip silinir, ay ızgarasındaki ya da hafta satırındaki güne tıklayınca o günün görünümü açılır. Pomodoro'nun yanında serbest geri sayım zamanlayıcısı (süre seç, başlat, duraklat, sıfırla; bitince çentik kısa süre açılıp titrer) ve kronometre (başlat, duraklat, tur, sıfırla). Zamanlayıcı bitiş anını bir kez hesaplar, kronometre saatten okur; ikisi de arka planda saniyelik tik atmaz, ekran yalnız takvim sekmesi görünürken tazelenir.
- **Hava durumu ve konum:** kurulum sihirbazına "Hava durumu için konumunu kullanalım mı?" adımı (Evet / Şimdi değil). Evet denirse konum açılışta, günde en fazla bir kez ve tek seferlik düşük doğrulukla okunur; koordinat ayarlara yazılır, hava 30 dakika önbellekle Open-Meteo'dan gelir. Ayarlar › Çentik'te "Hava durumu için konumu kullan" anahtarı ve yedek olarak şehir arama (Open-Meteo geocoding). Konum yoksa ve şehir seçilmemişse çentikte hava durumu gösterilmez. İzin reddi, zaman aşımı ve ağ hatası uygulamayı etkilemez. Hakkında sayfasında Open-Meteo atfı (CC BY 4.0).
- **Kontrol merkezi:** Wi-Fi, Bluetooth ve Rahatsız Etme döşemeleri sürüklenerek yeniden sıralanır (yay animasyonlu); sıra ayara yazılır ve açılışta uygulanır.
- **Ağ durumu servisi** çentik durum verisine bağlandı (Wi-Fi adı, Ethernet, bağlı değil; olay tabanlı, yoklama yok). Ağ adı konum izni gerektirmeyen Windows bağlantı profilinden alınır.
- Çentikte yazı yazılacak alanlara (takvimde etkinlik adı) tıklanınca pencere geçici olarak etkinleşir ki klavye çalışsın; odak gidince yine odak çalmaz.

### Değişenler (Changed)
- Dock ve çentik tek bir pencere izleyici ve tek bir medya servisini paylaşıyor; pencere olaylarında dock yenilemesi birleştiriliyor (daha az iş).
- Koy penceresi, sihirbaz ve sağ alt menü ortak tema ve ölçü sözlüklerinden besleniyor; sabit renk ve emoji kalmadı.
- Derleme tekrarlanabilir: yerel yollar PDB'ye gömülmüyor.
- Sürüm ve CI: release-please ile otomatik sürüm PR'ı, GitHub Actions adımları commit SHA'sına sabit, `vpk` sürümü Velopack paket sürümüne bağlı.

### Kaldırılanlar (Removed)
- Çentik ve dock sağ tık menülerindeki "Çıkış". Çıkış yalnız Koy penceresindeki "Koy'dan çık" düğmesi ve sağ alt menüden yapılır.
- Hiçbir davranışa bağlı olmayan ayarlar: ses görselleştirici, "müzik dalgasını durdur", "Windows'un kendi göstergesini gizle", çentik boyutu, dock'ta pencere gruplama ve "son kullanılanlar".
- Eski Ayarlar penceresi ve kullanılmayan kod, marka konseptleri ve sunum dosyaları.

### Güvenlik (Security)
- Yayımlanan ikili dosyalara geliştirici makinesinin yerel yolları artık gömülmüyor.
- GitHub Actions adımları commit SHA'sına sabitlendi (tedarik zinciri sertleştirmesi).

### Bilinen eksikler
Dock tepsisi (#11) gerçek donanımda elle denenmedi ve bellek etkisi ölçülmedi; ana görünüm (iki sütunlu çentik) arayüzü tasarım bekliyor; gerçek çok monitörlü ve karışık DPI donanımda doğrulama; bellek ölçümü (son ölçüm 82,7 MB, hedef 60 MB). Ayrıntı: [`docs/ROADMAP.md`](docs/ROADMAP.md).

## [0.1.0] - 2026-11-04

> **Not (sonradan eklendi):** Aşağıdaki listede bu sürümde bile eksik ya da bağlı olmayan şeyler vardı: ekran seçimi arayüzü olmadığı için çoklu ekran kullanıcıya açık değildi, OSD HUD çentiğe bağlı değildi, kısayollar ve ayarların çoğu davranışa bağlı değildi, "Aşağı yapış" seçilemiyordu, "1 saat duraklat" duraklatmıyordu, takvimde kronometre ve serbest zamanlayıcı hiç yoktu (hâlâ yok). Düzeltmeler için [Unreleased](#unreleased) bölümüne bakın.

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

[Unreleased]: https://github.com/brhnshn/koy/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/brhnshn/koy/releases/tag/v0.1.0
