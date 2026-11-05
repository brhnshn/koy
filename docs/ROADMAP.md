# ROADMAP — dosya dosya yapılacaklar

Her aşama kendi dalında yapılır, sonunda bellek ölçülür (`docs/MEMORY.md`).
Kutuları iş bittikçe işaretle. Dosya yolları önerilir; daha iyi bir ad varsa kullan ama yapıyı (Core / Platform / App) bozma.

---

## M0 — İskelet ✅ (2026-11-03)
- [x] Çözüm `Koy.slnx`, 4 proje, `global.json` (.NET 10), `Directory.Build.props`, `.editorconfig`, CI (`.github/workflows/ci.yml`)
- [x] `Core/Animation/Spring.cs` + testler
- [x] `Core/Settings/AppSettings.cs`, `SettingsStore.cs`, `SettingsJsonContext.cs` + testler
- [x] `Platform/Shell/TaskbarService.cs` (gizle / geri getir)
- [x] `Platform/Media/IMediaService.cs` (yalnız arayüz)
- [x] `App/Features/Notch/NotchWindow` — üst ortada hap, fareyle yay animasyonlu büyüme, sağ tık → Çıkış
- [x] Bellek ölçümü: 34 MB private (yazılımla çizim)
- [x] Mimari düzeltme (2026-11-04): `IMediaService` → `Core/Media/`; App `Features/` yapısına geçti (`IFeature`, `Features/Notch/NotchFeature`); `AppSettings.NotchEnabled / DockEnabled`; notch sabit tuval 500×260. Kurallar AGENTS.md #11–13.

> **Yol kuralı:** Aşağıda sözleşme (arayüz + model) her zaman `src/Core/<Alan>/`, Windows uygulaması `src/Platform/<Alan>/`, görsel + ViewModel `src/App/Features/<Özellik>/` içindedir.

---

## M1 — Notch kabuğu + Müzik paneli ✅ (2026-11-04)
**Hedef:** Spotify / tarayıcıda çalan müzik notch'ta görünür ve kontrol edilir. < 60 MB.

> **M1 Tasarım Mutabakatı (2026-11-04):**
> - **Geometri:** Ekrana sıfır yapışık (`Top = 0`), üst sağ/sol köşeleri içbükey (*concave*) kulaklı özel `NotchShape` (`Path`).
> - **Durum Geçişleri:** Hover → `Peek`, Tık → `Expanded`. Şarkı değişince 3 sn otomatik `Peek` → `Collapsed`. `WS_EX_NOACTIVATE` yüzünden `Deactivated` olayı çalışmayacağından; `MouseLeave` anında `Peek` derhal, `Expanded` ise ~400 ms tolerans gecikmesiyle `Collapsed`'a döner (veya üst şeride tekrar tıklanınca kapanır).
> - **Boşta %0 CPU Kuralı:** `Collapsed` durumda müzik çalarken sol yanda küçük kapak, sağ yanda **hareketsiz (statik)** ses dalgası vektör ikonu durur (sürekli animasyon çizilmez). Hareketli çubuklar yalnız `Peek` ve `Expanded` durumlarında çalışır.
> - **Sözleşme (`IMediaService`):** Çoklu oturum (`Sessions`, `SelectSessionAsync`) ve interaktif seek (`MediaTimeline`, `SeekAsync`) ile genişletilir. İlerleme çubuğu zamanlayıcısı (1 sn) yalnız `Expanded` + `Music` açıkken çalışır.

### 1.1 Pencereyi sağlamlaştır
- [x] `Platform/Shell/WindowStyles.cs`: `WS_EX_TOOLWINDOW` (Alt+Tab'da görünmesin) ve `WS_EX_NOACTIVATE` (tıklayınca odak çalmasın). `NotchWindow.OnSourceInitialized` içinde uygula. NativeMethods.txt: `GetWindowLong`, `SetWindowLong`, `WINDOW_EX_STYLE`.
- [x] Topmost kaybolmasın: tam ekran olmayan durumda periyodik değil, `WindowTracker` olayıyla (1.4) yeniden `Topmost` ver.
- [x] Çentik şekli: `Border` yerine alt köşeleri yuvarlak, üst kenarları içe kıvrık bir `Path` (`App/Features/Notch/NotchShape.cs`, `Width`/`Height`/`CornerRadius` bağımlılık özellikleri). Animasyon bu kontrolün boyutunu değiştirir; pencere sabit kalır (AGENTS.md #13).
- [x] Panel açılışında fps ve CPU ölç (PerfView ya da Görev Yöneticisi), sonucu `docs/MEMORY.md`'ye not et.

### 1.2 Panel sistemi
- [x] `Core/Notch/NotchPanel.cs`: `enum NotchPanel { Music, ControlCenter, Status, Calendar, Shelf }`
- [x] `Core/Notch/NotchState.cs`: `Collapsed`, `Peek` (küçük bilgi, ör. şarkı adı), `Expanded`. Geçişler saf fonksiyon; testleri `tests/Core.Tests/NotchStateTests.cs`.
- [x] `Core/Notch/NotchController.cs`: panel sırası, durum geçişleri, medya olayında Peek'e geçme kararı (saf mantık, testli).
- [x] `App/Features/Notch/NotchViewModel.cs`: `NotchController`'ı bağlar; `CurrentPanel`, `State`, `NextPanelCommand`, `PreviousPanelCommand`.
- [x] `NotchWindow.xaml`: içerik `ContentControl` + `DataTemplate` ile panel ViewModel'ine göre seçilir. Fare tekerleği / dokunmatik kaydırma → panel değişir.
- [x] Panel geçişi: `Spring` ile yatay kayma + opaklık.

### 1.3 Müzik
- [x] `Platform/Media/MediaService.cs : Core.Media.IMediaService` — `GlobalSystemMediaTransportControlsSessionManager.RequestAsync()`; `CurrentSessionChanged`, `SessionsChanged`, oturum başına `MediaPropertiesChanged`, `PlaybackInfoChanged`. Olaylar arka plan iş parçacığından gelir; ViewModel `Dispatcher` ile UI'a geçirir.
- [x] Kapak: `MediaProperties.Thumbnail.OpenReadAsync()` → byte[]; App tarafında `BitmapImage { DecodePixelWidth = 96 }` + `Freeze()`.
- [x] Birden fazla oturum: `GetSessions()` listesi, panelde küçük uygulama simgeleri arasında geçiş.
- [x] `App/Features/Music/MusicPanel.xaml` + `MusicViewModel.cs`: kapak, başlık, sanatçı, önceki / oynat-duraklat / sonraki, ilerleme çubuğu (`TimelineProperties`; saniyede bir güncelleme yalnız panel açıkken).
- [x] Collapsed durumda müzik çalıyorsa hapın iki yanında küçük kapak + basit dalga simgesi (gerçek ses analizi değil; M1'de animasyonlu çubuk yeter).
- [ ] Ses görselleştirici (isteğe bağlı, bütçe izin verirse): WASAPI loopback, yalnız Expanded + Music panelinde açık. Bellek ölç; 5 MB'tan fazla eklerse ertele.

### 1.4 Görünürlük modları
- [x] `Core/Shell/IWindowTracker.cs` + `IFullscreenDetector.cs` (sözleşmeler, pencere kimliği `nint`).
- [x] `Platform/Shell/WindowTracker.cs`: `SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_OBJECT_LOCATIONCHANGE)`; ön plandaki pencerenin dikdörtgeni değişince olay. Hook delegesini alan olarak tut (GC toplamasın).
- [x] `Platform/Shell/FullscreenDetector.cs`: `SHQueryUserNotificationState` (`QUNS_BUSY`, `QUNS_RUNNING_D3D_FULL_SCREEN`, `QUNS_PRESENTATION_MODE`) + ön plan penceresi monitörü tamamen kaplıyor mu.
- [x] `Core/Visibility/VisibilityController.cs`: girdi (mod, pencere üst üste mi, fare kenarda mı, tam ekran mı, medya olayı) → çıktı (görünür / gizli / tıklama geçirgen). Saf mantık, testli.
- [x] Gizlenme: yukarı kayarak (`Spring`), gizliyken `WS_EX_TRANSPARENT` ile tıklama geçirgen.
- [x] Peek: fare ekranın üst kenarına 2 px yaklaşınca çık. Fare konumu için `GetCursorPos` yalnız gizliyken ve 100 ms aralıkla; görünürken `MouseLeave` kullan.

### 1.5 Kabul
- [x] Spotify, YouTube (Edge/Chrome) ve Windows Media Player'da çalışıyor.
- [x] Tam ekran oyun / video sırasında notch görünmüyor.
- [x] Alt+Tab'da görünmüyor, tıklayınca odak çalmıyor.
- [x] Bellek ölçüldü, < 60 MB. `docs/MEMORY.md` güncellendi.

---

## M2 — Kontrol Merkezi + Durum (2026-11-04, kısmen tamamlandı)
- [x] Sözleşmeler: `Core/Audio/IAudioService.cs`, `Core/Radio/IRadioService.cs`, `Core/Display/IBrightnessService.cs`, `Core/Power/IPowerService.cs`, `Core/Weather/IWeatherService.cs` (+ modeller).
- [x] `Platform/Audio/AudioService.cs`: CoreAudio `IMMDeviceEnumerator` → `IAudioEndpointVolume` (seviye, sessiz, değişim bildirimi `IAudioEndpointVolumeCallback`). CsWin32 ile COM.
- [x] `Platform/Radio/RadioService.cs`: `Windows.Devices.Radios.Radio.GetRadiosAsync()` — Wi-Fi ve Bluetooth aç/kapa. İlk çağrıda `RequestAccessAsync()`.
- [x] `Platform/Display/BrightnessService.cs`: dahili ekran WMI `WmiMonitorBrightness` / `WmiMonitorBrightnessMethods`; harici ekran DDC/CI (`GetPhysicalMonitorsFromHMONITOR`, `SetVCPFeature` kod 0x10). Desteklenmiyorsa kaydırıcı gizlenir.
- [x] `Platform/Shell/FocusAssistService.cs`: Rahatsız Etme durumu (okuma); açma/kapama için resmi API yoksa yalnız Windows ayarını açan kısayol.
- [x] `Platform/Power/PowerService.cs`: `Windows.System.Power.PowerManager` (pil yüzdesi, şarj); pil yoksa panelde gizle.
- [x] `Platform/Weather/WeatherService.cs`: Open-Meteo (`https://api.open-meteo.com/v1/forecast?latitude=..&longitude=..&current=temperature_2m,weather_code`). Konum: ayarlardan şehir (Open-Meteo geocoding API) — konum izni isteme. 30 dk önbellek, `HttpClient` tek örnek. Konum kurulum sihirbazında sorulur (cihaz konumu, açılışta günde en fazla bir kez) ya da Ayarlar › Çentik'ten şehir aranır (`Core/Weather/WeatherLocationPolicy`, `OpenMeteoParser`, `Platform/Weather/LocationService`, `GeocodingService`). Konum yoksa hava durumu gösterilmez; Hakkında'da Open-Meteo atfı.
- [x] `App/Features/ControlCenter/ControlCenterPanel.xaml` + ViewModel: döşemeler (Wi-Fi, BT, Rahatsız Etme), kaydırıcılar (ses, parlaklık).
- [x] Döşeme düzenini sürükleyerek değiştirme: düzen `AppSettings.ControlCenterLayout` (liste) olarak saklanır; sıralama mantığı `Core/Notch/ControlCenterTiles`, sürükleme `ControlCenterPanel` (yay animasyonlu).
- [x] `App/Features/Status/StatusPanel.xaml` + ViewModel: pil, hava durumu, saat.
- [x] Kabul: her kontrol gerçek donanımda denendi; bellek ölçüldü (40.9 MB < 60 MB bütçe).

---

## M3 — Takvim, Pomodoro, Raf, Ayarlar, Dil (2026-11-04, kısmen tamamlandı)
- [x] `Core/Calendar/MonthGrid.cs`: ay görünümü için gün ızgarası (Pazartesi başlangıçlı, kültüre göre). Testli.
- [x] `Core/Timers/Pomodoro.cs`: durum makinesi (çalışma / kısa mola / uzun mola), süreler ayardan (Paket C'de bağlandı). Testli; zaman `TimeProvider` ile verilir ki test edilebilsin.
- [x] `App/Features/Calendar/CalendarPanel.xaml`: gün / hafta / ay, zamanlayıcı, kronometre, Pomodoro. Yalnız yerel (`%AppData%\Koy\events.json`); Google/Outlook sonraki sürüm. Durum makineleri `Core/Timers/CountdownTimer`, `LapStopwatch` (TimeProvider, testli); etkinlik mantığı `Core/Calendar/EventAgenda`, `EventBook`.
- [x] Bildirim: zamanlayıcı bitince notch kısa süre Peek durumuna geçer ve titrer (yay animasyonu).
- [x] `App/Features/Shelf/ShelfPanel.xaml`: notch'a dosya / metin sürükle-bırak (`AllowDrop`), listeden dışarı sürükleme (`DragDrop.DoDragDrop`). Dosyalar kopyalanmaz, yol tutulur; liste `%AppData%\Koy\shelf.json`.
- [x] Ayarlar penceresi: M6.5'te `KoyWindow` oldu, eski `SettingsWindow` silindi. Paket C ve D1 sonrası vurgu rengi, tema ve katı/yarı saydam malzeme görünüme etki ediyor.
- [x] Başlangıçta çalış: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- [x] Cam görünüm: `BackdropService` artık `KoyWindow`'da `Material` ayarına göre kullanılıyor (D1). **Doğrulanmadı:** `AllowsTransparency` penceresinde DWM acrylic'in gerçek cihazda etkisi; etkisizse %88 opak yüzeylerle sınırlı kalır.
- [x] Dil: `Resources/Strings.resx` (en) + `Strings.tr.resx`; XAML'de `{x:Static res:Strings.Key}`. Bütün sabit Türkçe metinler buraya taşınır.
- [x] Tepsi simgesi: `TrayIconService` (CsWin32, WinForms yok); Çıkış yalnız Koy penceresi ve sağ alt menüde (notch sağ tık menüsünden kaldırıldı).

---

## M4 — Dock ✅ (2026-11-04)
**En riskli aşama. Görev çubuğu güvenliği kuralı (AGENTS.md #7) burada kritik.**
- [x] `App/Features/Dock/DockFeature.cs : IFeature`; `App.xaml.cs` yalnız `settings.DockEnabled` ise ekler. Kapalıyken hiçbir dock servisi ya da hook'u kurulmaz (AGENTS.md #12).
- [x] Sözleşmeler: `Core/Dock/IAppBarService.cs`, `IPinnedAppsService.cs`, dock öğe modeli; sıralama ve gruplama mantığı `Core/Dock/` içinde testli.
- [x] `Platform/Shell/AppBarService.cs`: `SHAppBarMessage(ABM_NEW / ABM_QUERYPOS / ABM_SETPOS / ABM_REMOVE)` ile ekranın altında yer ayırma (yalnız Fixed modda). Kapanışta mutlaka `ABM_REMOVE`.
- [x] Görev çubuğunu gizleme: `TaskbarService.Hide()` + `ABM_SETSTATE(ABS_AUTOHIDE)` ile çalışma alanını geri alma; kapanışta eski durumu geri yükle. Eski durumu `%AppData%\Koy\taskbar-state.json` dosyasına yaz ki çökme sonrası açılışta geri yüklenebilsin.
- [x] `Platform/Shell/PinnedAppsService.cs`: `%AppData%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\*.lnk`; simge `SHGetFileInfo` / `IShellItemImageFactory` (32 px, önbellekli). Sıra v1'de klasör sırası.
- [x] `Platform/Shell/WindowTracker.cs` genişlet: açık üst düzey pencereler (`EnumWindows` + görünür, sahipsiz, `WS_EX_TOOLWINDOW` değil), oluşma / kapanma olayları (`EVENT_OBJECT_CREATE / DESTROY / SHOW / HIDE`), yanıp sönme (`HSHELL_FLASH` için `RegisterShellHookWindow`).
- [x] `App/Features/Dock/DockWindow.xaml` + `DockViewModel.cs`: sabitli + açık uygulamalar, çalışan göstergesi, tıklayınca öne getir / küçült (`SetForegroundWindow`, `ShowWindow`), orta tık yeni pencere, sağ tık menüsü (Kapat, Görev Yöneticisi'nde sonlandır = `TerminateProcess` onaylı).
- [x] Fareyle büyüme (macOS tarzı) `Spring` ile; isteğe bağlı, ayardan kapatılabilir.
- [x] Başlat düğmesi: `Win` tuşu gönder (`SendInput`), sistem tepsisi düğmesi: `Win+B` ya da `Shell_TrayWnd` taşma penceresi. Tepsi simgelerini taşımak v1 dışındaydı; PR #11 ile geldi (aşağıda "Dock sistem tepsisi").
- [x] Kabul: süreci öldür → yeniden aç → görev çubuğu geri. Kaldır → görev çubuğu geri. Tam ekranda dock gizli. Bellek ölçüldü.

---

## M5 — Çoklu monitör, kurulum, ilk sürüm (2026-11-04, kısmen tamamlandı)
- [x] `Platform/Display/MonitorService.cs`: `EnumDisplayMonitors`, `GetMonitorInfo`, `GetDpiForMonitor`; ekran değişince (`WM_DISPLAYCHANGE`) pencereleri yeniden yerleştir. Pencere konumları fiziksel pikselden DIP'e çevrilir.
- [x] Ayar: notch / dock ekran başına `MonitorSettings` ile (kararlı monitör kimliği), Koy penceresindeki Ekranlar sayfasından seçilir (D2). Eski `MonitorTarget` göçü testli.
- [x] Velopack: `vpk pack` ile kurulum + `UpdateManager` ile GitHub Releases'tan güncelleme. Kaldırma kancasında `TaskbarService.Restore()`.
- [x] `.github/workflows/release.yml`: etiket (`v*`) itilince derle, `vpk pack`, `SHA256SUMS.txt`, GitHub Release'e yükle.
- [x] README'ye ekran görüntüleri, kurulum, SHA256 doğrulama.
- [x] v0.1.0.

---

## M6 — Koy'u Windows'tan ayır: dock yeniden, çoklu ekran, kendi arayüzü (2026-11-04, açık)
**Neden:** Windows görev çubuğunun aynısı olan tam genişlik çubuk, Koy'u ayırt edici kılmıyor. Çoklu ekranda yalnız birincil ekranda çalışıyor. M5'teki `All` monitör ayarı kodda yok: `MonitorTarget` yalnız tek bir ekran seçiyor. 

### 6.1 Arayüz tasarımı (önce bu, proje sahibi seçer)
- [x] Dock, çentik HUD'u ve Koy penceresi için tasarım taslakları: proje sahibi onayladı (2026-11-04). Özet ve ölçüler `docs/DESIGN.md`'de, görseller `docs/design/index.html`'de.

### 6.2 Çoklu ekran
- [x] `Core`: monitör başına durum modeli. Hangi ekranda ne gösterileceği (`Primary` / `All` / `Specific`) saf mantıkla belirlenir ve test edilir.
- [x] Her monitörde ayrı `NotchWindow` ve `DockWindow`, her biri kendi DPI'ında. Ekran takılıp çıkarılınca (`WM_DISPLAYCHANGE`) pencereler eklenir, kaldırılır ve yeniden yerleşir; kullanıcı Ekranlar sayfasından açar. **Doğrulanmadı:** gerçek çok monitörlü / karışık DPI donanım, ikincil AppBar.
- [x] Tam ekran algılama monitör başına: yan ekrandaki tam ekran, ana ekrandaki Koy'u gizlemez. `SHQueryUserNotificationState` tüm sistemi kapsıyor, bu yüzden tek başına karar vermemeli. Testli.
- [x] Görev çubuğu gizleme bütün ekranlarda geçerli olmalı (`Shell_SecondaryTrayWnd` da). Geri yükleme de hepsinde çalışmalı.

### 6.3 Dock yeniden (Seelen modeli)
- [x] Dock düzeni ayardan seçilir: yüzen kapsül ya da tam genişlik.
- [x] "Pencere yaklaşınca" davranışı ayardan seçilir: **Yer ayır** (dock alt kenara oturur, çalışma alanı ayrılır), **Aşağı yapış** (normal boy pencere yaklaşınca dock alt kenara iner, incelir, pencereyi örtmez; varsayılan), **Çekil** (büyütülmüş pencerede gizlenir, fare alt kenara inince gelir), **Hep gizli**. Tam ekranda her modda gizli. Büyütülmüş pencere ile dock arasında boşluk kalmamalı. Paket C'de varsayılan `DockDown` oldu ve her yüklemede `Dodge`'a çeviren göç kaldırıldı. Not: eski bir kurulumda `settings.json`'da `Dodge` yazılı kalmış olabilir; Dock sayfasından Aşağı yapış seçilir.
- [x] Simgenin üzerine gelince pencere önizlemesi (DWM thumbnail, `DwmRegisterThumbnail`). Yalnız üzerine gelindiğinde kurulur, bellek ölçülür.
- [x] İsteğe bağlı ayırıcı: solda uygulamalar, sağda klasörler, son kullanılanlar ve küçültülmüş pencereler. Ayırıcı, İndirilenler klasörü, küçültülmüş pencereler ve son kullanılanlar yığını var (Windows Son Kullanılanlar klasörü, en çok 10 öğe, yalnız tıklanınca okunur); hepsi Dock sayfasından kapatılır.
- [ ] Saat, pil, ses ve ağ dock'tan çentiğe taşınır (tasarıma göre). **Kısmi:** dock'ta durum alanı yok; saat ve pil çentikte, ses kontrol merkezinde. `INetworkService` / `NetworkService` artık çentik durum verisine bağlı (olay tabanlı, Wi-Fi adı dahil); çentikte görünür gösterimi iki sütunlu ana görünümle gelecek (tasarım bekliyor), bu yüzden kutu açık.

### 6.4 Çentik genişler
- [x] Ses ve parlaklık değişince çentikte gösterge: Paket C'de `AudioService` ve `BrightnessService` olaylarına bağlandı, ayrı ayrı kapatılır. Windows'un kendi göstergesini gizleme hâlâ deneysel.
- [x] Dosya rafı ve takvim çentikte zaten var (M3). Kapalı çentikten erişimi gözden geçirilir.

### 6.5 Koy'un kendi arayüzü
- [x] Sağ altta Koy simgesi: dock açıkken dock'un sağ ucunda, dock kapalıyken bildirim alanında (`Shell_NotifyIcon`, CsWin32 ile, WinForms yok). Tıklayınca Koy penceresi açılır. Dock penceresi varken bildirim alanı simgesi kaldırılır (tek simge), dock kapanınca ya da duraklayınca geri gelir; "1 saat duraklat" gerçekten çentik ve dock'u durdurur; Explorer yeniden başlayınca simge (gizli değilse) yeniden eklenir. **Elle denenmedi** (dock açma/kapama/duraklatma, Explorer yeniden başlatma).
- [x] Çıkış yalnız sağ alt menüden ve Koy penceresindeki "Koy'dan çık" düğmesinden yapılır; çentik ve dock sağ tık menülerindeki "Çıkış" kaldırıldı (Paket C).
- [x] Ayarlar penceresi yeni tasarıma göre "Koy penceresi" olur: orta boy (880 px), sekiz sayfa (Genel, Çentik, Dock, Ekranlar, Görünüm, Kısayollar, Performans, Hakkında). Sayfaların ayarları tasarım galerisinde. "Hakkında"da "Windows görev çubuğunu geri getir" acil durum düğmesi bulunur. Paket C ve D ile ayarlar gerçekten bağlandı; Hakkında sayfasına ayrıca "Koy'u kaldır" eklendi.
- [x] İlk açılış: üç adımlık kurulum (ne gösterilsin: çentik ve dock / yalnız çentik / yalnız dock).

### 6.6 Kabul
- [ ] İki ekranla dene: yan ekranda tam ekran video oynarken ana ekranda çentik ve dock duruyor. **Kısmi:** mantık monitör başına ve testli, kullanıcı Ekranlar sayfasından açabiliyor; gerçek iki ekranla elle kabul testi yapılmadı.
- [ ] Ekranı çıkar ve tak: pencereler doğru yere geliyor, görev çubuğu bütün ekranlarda gizli. **Kısmi:** kod var, gerçek donanımda denenmeli.
- [x] Bellek ölçüldü: 82.7 MB private. Bütçe kararı 2026-11-06'da verildi: boşta 100–150 MB hedef, 200 MB'a kadar kabul (bkz. `docs/MEMORY.md`).

---

## Revizyon paketleri (2026-11-05)
Kod incelemesinden çıkan işler paketlere bölündü.
- [x] **A, stabilite:** SagAltMenu `Measure` çökmesi, kenar zamanlayıcısı thread erişimi, global hata yakalama ve `crash.log`, görev çubuğu çift gizleme ve Explorer yeniden başlama, tek `WindowTracker` ve `MediaService` paylaşımı.
- [x] **B, temizlik:** ölü kod, PDB yol gizleme, belge hijyeni.
- [x] **C, ayarları bağlama:** etkisiz ayarlar bağlandı ya da kaldırıldı; `DockDown` varsayılan; kısayollar (`RegisterHotKey`, özelleştirilebilir, çakışma bildirimi); OSD HUD bağlandı; gerçek "1 saat duraklat"; Çıkış yalnız Koy penceresi ve sağ alt menüde.
- [x] **D1, görünüm:** Koyu/Açık tema ve token sistemi, Lucide tabanlı ikonlar, hareket (`Motion`), malzeme ayarı. Arayüzü PNG'ye çizen `tools/Koy.UiSnapshots`.
- [x] **D2, Ekranlar:** monitör haritası, ekran başına çentik ve dock, kararlı monitör kimliği, gerçek çoklu ekran penceresi yönetimi.
- [x] **F, sürüm ve CI:** release-please, SHA'ya sabitlenmiş action'lar, pinli `vpk`.
- [x] **Kaldırma deneyimi:** Hakkında'da "Koy'u kaldır" + onay görünümü; Windows'tan kaldırmada kanca (görev çubuğu, başlatma kaydı, durum dosyaları; ayarlar korunur).
- [x] **Belge son geçişi:** AGENTS, README, DESIGN, CHANGELOG gerçek duruma getirildi.

## Sıradaki
**E paketi adayları** (bütçe kararı verildi: 100–150 MB hedef, 200 MB'a kadar kabul; her biri yeni servis demek, önce ölç):
- [ ] Pano geçmişi (yalnız metin, sınırlı ve silinebilir).
- [ ] Mikrofon / kamera kullanım göstergesi (çentikte nokta).
- [ ] Bluetooth cihaz HUD'u (kulaklık bağlandı / pil seviyesi).
- [ ] Pil HUD'u tamamlama (düşük pil, şarj tamamlandı uyarıları).
- [ ] **RAM optimizasyonu:** #14 radyo servisini açılışta kuruyor (önceden Kontrol paneli ilk açılınca); boşta bellek farkını ölç, gerekirse radyo izlemesini ilk ihtiyaca ertele ya da simge işi için yükü azalt.
- [x] **Bellek bütçesi kararı (2026-11-06):** boşta 100–150 MB private hedef, çok ekran / açık panellerle 200 MB'a kadar kabul. Son ölçüm 63,9 MB (`docs/MEMORY.md`); dock/tepsi paketi sonrası yeniden ölçülecek.

**Katkıcı PR'ları (xdaxer):**
- [x] #8 pilsiz makinede pil gizleme, #10 dock pencere önizlemesi, #12 Kontrol Merkezi Wi-Fi/Bluetooth yönetimi, #13 açılış zemini: `main`'e alındı (bizim düzeltmelerimizle).
- [x] #14 çentik sekmeleri ve Wi-Fi/Bluetooth durum simgeleri: `main`'e alındı (bizim düzeltmelerimizle: "Hiçbiri" modu, token/ikonlar, hareket politikası, paylaşımlı radyo servisi). Bellek ölçümü yapılmadı, aşağıdaki RAM optimizasyonu işine bırakıldı.
- [ ] #11: kapsam kararı bekliyor (bu ROADMAP v1 dışı demişti).
- #9: `main`'de farklı bir yapıyla (Ekranlar sayfası, D2) çözüldü; kapatılacak.

**Hâlâ açık olan eksikler:** iki sütunlu ana görünüm ve kapalı/Peek çentikte tarih ve ağ simgesi (tasarım bekliyor; veri tarafı hazır: `HomeViewModel`, `NotchLayout.ShowPeekDetails`); UWP uygulama sabitlemeleri (AUMID); ses görselleştirici; gerçek çoklu monitör ve karışık DPI doğrulaması; bellek ölçümü.

---

## Dock sistem tepsisi (PR #11, xdaxer; entegre edildi, elle denenmedi)
- [x] `Core/Tray/`: SHELLTRAYDATA çözümleyici (boyut sınırlı), simge deposu (üst sınır 256), tıklama geri çağrısı, `TrayHostPolicy` (ne zaman barındırılır, Koy-süreç filtresi). Testli.
- [x] `Platform/Shell/TrayHostService.cs`: kendi iş parçacığında gizli `Shell_TrayWnd` penceresi, her kaydı Windows'a da iletir. `ShellTrayLocator.FindExplorerTray` görev çubuğu gizleme/geri getirme kodunda Koy'un kendi penceresini atlar.
- [x] `SharedServices.AcquireTrayHost`: bütün monitörlerdeki dock'lar aynı örneği paylaşır; yalnız Windows görev çubuğu gizliyken (ayar açık ve en az bir dock penceresi var) kurulur. Ayar: Dock sayfası "Sistem tepsisini dock'ta göster" (`DockShowTray`, varsayılan açık).
- [x] Koy kapanır / duraklatılır / dock kapanır / ayar kapanır → pencere kapanır ve Windows tepsisi geri gelir; çökme işleyicileri görev çubuğunu ve tepsi penceresini birlikte kapatır.
- [x] Sağ tıkta açılır pencere kapanmaz: uygulamanın bağlam menüsü açıkken açık kalır; menüden seçim yapılınca ya da pencere ve menünün dışına tıklanınca yay animasyonuyla kapanır (`TrayPopupDismissPolicy`, odak kaybı kapatma sebebi değil).
- [ ] **Elle sınama** (proje sahibi): Discord/Steam/Tailscale tıklama ve menü; Görev Yöneticisi'nden süreci öldür → Windows tepsisi ve görev çubuğu geri; Explorer'ı yeniden başlat → simgeler geri; çok monitör.
- [ ] Bellek ölçümü (AGENTS #1): main ile main + tepsi karşılaştırması, `docs/MEMORY.md`.

## Dock ve tepsi paketi (2026-11-06)
- [x] **Gözcü süreç:** `src/Watchdog/` (`Koy.Watchdog.exe`, Native AOT, pencere yok). Dock görev çubuğunu gizlediği sürece tek gözcü çalışır, Koy'un PID'ini bekler; Koy öldürülürse görev çubuğunu geri getirir (`taskbar-state.json`), temiz çıkışta dokunmaz. Görev çubuğu mantığı `TaskbarService.cs` / `ShellTrayLocator.cs` dosyalarını bağlayarak paylaşılır; karar mantığı `Core/Shell/WatchdogPolicy.cs`. `scripts/pack.ps1` ve `release.yml` exe'yi Koy'un klasörüne koyar. **AOT yayın bu makinede (C++ araçları yok) üretilemedi; elle sınama bekliyor.**
- [x] **Sabitli uygulama sırası:** sıra `AppSettings.DockPinnedOrder`'da; ilk açılışta Taskband `Favorites` tohum (okunamazsa klasör sırası); dock'ta sürükleyerek sıralama (yay ile kayma). Mantık `Core/Dock/PinnedOrder.cs`, testli.
- [x] **Alt kenardan açılma ve tam ekran:** büyütülmüş pencerede dock gizliyse fare alt kenarda ~300 ms durunca (tuş basılı değilken) çıkar, "Alt kenardan aç" ayarıyla kapatılır; gerçek tam ekranda asla çıkmaz. Tam ekran üç işaretin birleşimi, monitör başına (`FullscreenRule`, `EdgeRevealTracker`, `DockPositionResolver`), testli.
- [x] **Tepsi açılır penceresi** sağ tıkta kapanmıyor (yukarıda).
- [x] **Tek tepsi simgesi** (6.5).
- [x] **Dock'ta son kullanılanlar** (6.3).
- [x] **Çoklu ekran varsayılanı:** yeni ekran varsayılanı çentik ve dock açık (`MonitorPolicy.DefaultNewScreenMode`), testli.
- [x] **Belgeler:** bellek bütçesi kararı, ölçüm kaydı, CHANGELOG.
- [ ] **Elle sınama** (proje sahibi): Görev Yöneticisi'nden Koy'u öldür → görev çubuğu birkaç sn içinde kendiliğinden gelmeli; Koy'dan çık / dock'u kapat → gözcü kalmamalı (Görev Yöneticisi'nde `Koy.Watchdog.exe`); simge sürükleyerek sırala, yeniden başlatınca sıra korunmalı; büyütülmüş pencerede alt kenara git (tuş basılı değilken ~300 ms sonra çıkar), oyun / F11 / tam ekran videoda çıkmasın; tepsi simgesine sağ tık → pencere açık kalsın, menüden seçince / dışarı tıklayınca kapansın; dock açıkken bildirim alanında Koy simgesi olmasın, dock kapanınca gelsin; son kullanılanlar yığını; ikinci ekran takınca çentik ve dock açılsın.
- [ ] Gözcü AOT paketinin gerçek CI/`pack.ps1` çıktısından boyut ve bellek ölçümü; paket sonrası Koy belleğinin yeniden ölçümü (`docs/MEMORY.md`).

## Sonraya kalanlar
- Google / Outlook takvim (OAuth).
- Tepsi: gizli (taşma) simgeleri ayrı gösterme, balon bildirimleri (`NIF_INFO`), animasyonlu simgelerin ölçümü.
- UWP (Store) uygulamalarının dock'ta sabitlenmesi: kimlik AUMID, simge `shell:AppsFolder`, pencere eşleme `SHGetPropertyStoreForWindow`. Windows'un sabitleri arasında Spotify, WhatsApp gibi UWP uygulamalar var; Koy bugün yalnız `.lnk` sabitlerini gösteriyor.
- Kod imzalama.
