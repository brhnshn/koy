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
- [ ] `Platform/Weather/WeatherService.cs`: Open-Meteo (`https://api.open-meteo.com/v1/forecast?latitude=..&longitude=..&current=temperature_2m,weather_code`). Konum: ayarlardan şehir (Open-Meteo geocoding API) — konum izni isteme. 30 dk önbellek, `HttpClient` tek örnek. **Kısmi:** servis, önbellek ve gösterim var; konum sabit (İstanbul), şehir seçimi ve geocoding yok.
- [x] `App/Features/ControlCenter/ControlCenterPanel.xaml` + ViewModel: döşemeler (Wi-Fi, BT, Rahatsız Etme), kaydırıcılar (ses, parlaklık).
- [ ] Döşeme düzenini sürükleyerek değiştirme: düzen `AppSettings.ControlCenterLayout` (liste) olarak saklanır. **Kısmi:** ayar alanı var, ama hiçbir yerde okunmuyor ve sürükleme yok.
- [x] `App/Features/Status/StatusPanel.xaml` + ViewModel: pil, hava durumu, saat.
- [x] Kabul: her kontrol gerçek donanımda denendi; bellek ölçüldü (40.9 MB < 60 MB bütçe).

---

## M3 — Takvim, Pomodoro, Raf, Ayarlar, Dil (2026-11-04, kısmen tamamlandı)
- [x] `Core/Calendar/MonthGrid.cs`: ay görünümü için gün ızgarası (Pazartesi başlangıçlı, kültüre göre). Testli.
- [ ] `Core/Timers/Pomodoro.cs`: durum makinesi (çalışma / kısa mola / uzun mola), süreler ayardan. Testli; zaman `TimeProvider` ile verilir ki test edilebilsin. **Kısmi:** makine ve testler var; süreler ayardan gelmiyor, 25/5/15 dk sabit.
- [ ] `App/Features/Calendar/CalendarPanel.xaml`: gün / hafta / ay, zamanlayıcı, kronometre, Pomodoro. Yalnız yerel; Google/Outlook sonraki sürüm. **Kısmi:** yalnız ay görünümü ve Pomodoro var; gün/hafta görünümü, serbest geri sayım zamanlayıcısı ve kronometre yok.
- [x] Bildirim: zamanlayıcı bitince notch kısa süre Peek durumuna geçer ve titrer (yay animasyonu).
- [x] `App/Features/Shelf/ShelfPanel.xaml`: notch'a dosya / metin sürükle-bırak (`AllowDrop`), listeden dışarı sürükleme (`DragDrop.DoDragDrop`). Dosyalar kopyalanmaz, yol tutulur; liste `%AppData%\Koy\shelf.json`.
- [ ] `App/Features/Settings/SettingsWindow.xaml`: genel (dil, başlangıçta çalış), notch (görünürlük modu, hangi paneller), dock (M4), görünüm (vurgu rengi, katı / cam), hakkında (sürüm, tanılama kopyala). Yalnız açılınca oluşturulur. **Kısmi:** pencere M6.5'te `KoyWindow` ile değişti ve eski `SettingsWindow` silindi; dil, başlangıçta çalış, sürüm ve tanılama çalışıyor, vurgu rengi ve katı/cam görünüme etki etmiyor.
- [x] Başlangıçta çalış: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- [ ] Cam görünüm: `DwmSetWindowAttribute(DWMWA_SYSTEMBACKDROP_TYPE)` yalnız Ayarlar penceresinde; notch katı renk kalır (yazılımla çizimde DWM arka planı saydam pencerede çalışmaz, deneyip ölç). **Kısmi:** `BackdropService` yazıldı ama hiçbir pencerede kullanılmıyor; `Material` ayarı okunmuyor.
- [x] Dil: `Resources/Strings.resx` (en) + `Strings.tr.resx`; XAML'de `{x:Static res:Strings.Key}`. Bütün sabit Türkçe metinler buraya taşınır.
- [x] Tepsi simgesi yerine: notch sağ tık menüsü (Ayarlar, Çıkış). Ayrı `NotifyIcon` eklenmez (WinForms gerektirir).

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
- [x] Başlat düğmesi: `Win` tuşu gönder (`SendInput`), sistem tepsisi düğmesi: `Win+B` ya da `Shell_TrayWnd` taşma penceresi. Tepsi simgelerini taşımak v1 dışında.
- [x] Kabul: süreci öldür → yeniden aç → görev çubuğu geri. Kaldır → görev çubuğu geri. Tam ekranda dock gizli. Bellek ölçüldü.

---

## M5 — Çoklu monitör, kurulum, ilk sürüm (2026-11-04, kısmen tamamlandı)
- [x] `Platform/Display/MonitorService.cs`: `EnumDisplayMonitors`, `GetMonitorInfo`, `GetDpiForMonitor`; ekran değişince (`WM_DISPLAYCHANGE`) pencereleri yeniden yerleştir. Pencere konumları fiziksel pikselden DIP'e çevrilir.
- [ ] Ayar: notch / dock için `Primary`, `All`, `Specific(monitorId)`. **Kısmi:** çözümleyici ve testler var; ayar varsayılanı `Primary` ve Koy penceresinde seçim arayüzü yok.
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
- [ ] Her monitörde ayrı `NotchWindow` ve `DockWindow`, her biri kendi DPI'ında. Ekran takılıp çıkarılınca (`WM_DISPLAYCHANGE`) pencereler eklenir, kaldırılır ve yeniden yerleşir. **Kısmi:** kod var, ama hedef varsayılan olarak yalnız birincil ekran ve seçim arayüzü yok; kullanıcı çoklu ekranı açamıyor.
- [x] Tam ekran algılama monitör başına: yan ekrandaki tam ekran, ana ekrandaki Koy'u gizlemez. `SHQueryUserNotificationState` tüm sistemi kapsıyor, bu yüzden tek başına karar vermemeli. Testli.
- [x] Görev çubuğu gizleme bütün ekranlarda geçerli olmalı (`Shell_SecondaryTrayWnd` da). Geri yükleme de hepsinde çalışmalı.

### 6.3 Dock yeniden (Seelen modeli)
- [x] Dock düzeni ayardan seçilir: yüzen kapsül ya da tam genişlik.
- [ ] "Pencere yaklaşınca" davranışı ayardan seçilir: **Yer ayır** (dock alt kenara oturur, çalışma alanı ayrılır), **Aşağı yapış** (normal boy pencere yaklaşınca dock alt kenara iner, incelir, pencereyi örtmez; varsayılan), **Çekil** (büyütülmüş pencerede gizlenir, fare alt kenara inince gelir), **Hep gizli**. Tam ekranda her modda gizli. Büyütülmüş pencere ile dock arasında boşluk kalmamalı. **Kısmi:** dört mod kodda var; varsayılan `Dodge` (Aşağı yapış değil) ve kayıtlı `DockDown` her yüklemede `Dodge`'a çevriliyor, yani Aşağı yapış fiilen seçilemiyor.
- [x] Simgenin üzerine gelince pencere önizlemesi (DWM thumbnail, `DwmRegisterThumbnail`). Yalnız üzerine gelindiğinde kurulur, bellek ölçülür.
- [ ] İsteğe bağlı ayırıcı: solda uygulamalar, sağda klasörler, son kullanılanlar ve küçültülmüş pencereler. **Kısmi:** ayırıcı, tek klasör (İndirilenler) ve küçültülmüş pencereler var; son kullanılanlar yok, ayar arayüzü yok.
- [ ] Saat, pil, ses ve ağ dock'tan çentiğe taşınır (tasarıma göre). **Kısmi:** dock'ta durum alanı yok; saat ve pil çentikte, ses kontrol merkezinde. Ağ durumu hiçbir yerde gösterilmiyor (`INetworkService` / `NetworkService` bağlı değil).

### 6.4 Çentik genişler
- [ ] Ses ve parlaklık değişince çentikte gösterge (Windows'un kendi göstergesinin yerine). **Yok (bağlı değil):** `OsdView` ve `OsdIndicatorViewModel` yazıldı ama ses/parlaklık olaylarına hiç bağlanmadı; gösterge hiç açılmıyor.
- [x] Dosya rafı ve takvim çentikte zaten var (M3). Kapalı çentikten erişimi gözden geçirilir.

### 6.5 Koy'un kendi arayüzü
- [ ] Sağ altta Koy simgesi: dock açıkken dock'un sağ ucunda, dock kapalıyken bildirim alanında (`Shell_NotifyIcon`, CsWin32 ile, WinForms yok). Tıklayınca Koy penceresi açılır. **Kısmi:** simge ve menü çalışıyor, ama tepsi simgesi dock durumundan bağımsız hep gösteriliyor; dock açıkken iki simge olabilir. "1 saat duraklat" yalnız bayrağı değiştiriyor, Çentik ve Dock'u duraklatmıyor.
- [ ] Çıkış yalnız buradan yapılır. Çentik ve dock sağ tık menülerindeki "Çıkış" kaldırılır. **Yapılmadı:** `NotchWindow` ve `DockWindow` bağlam menülerinde "Çıkış" duruyor.
- [x] Ayarlar penceresi yeni tasarıma göre "Koy penceresi" olur: orta boy (880 px), sekiz sayfa (Genel, Çentik, Dock, Ekranlar, Görünüm, Kısayollar, Performans, Hakkında). Sayfaların ayarları tasarım galerisinde. "Hakkında"da "Windows görev çubuğunu geri getir" acil durum düğmesi bulunur. **Kısmi:** pencere, sekiz sayfa ve acil durum düğmesi var; ayarların yaklaşık yarısı kaydedilmiyor ya da hiçbir davranışa bağlı değil (tema, vurgu, malzeme, animasyon, boşta görünüm, açılma, kısayollar, tekerlek/orta tık, pil tasarrufu, Ekranlar sayfası vb.).
- [x] İlk açılış: üç adımlık kurulum (ne gösterilsin: çentik ve dock / yalnız çentik / yalnız dock).

### 6.6 Kabul
- [ ] İki ekranla dene: yan ekranda tam ekran video oynarken ana ekranda çentik ve dock duruyor. **Kısmi:** mantık monitör başına ve testli, ama varsayılan yapılandırmada çoklu ekran kapalı; bu kabul testi kullanıcı tarafından yapılamıyor.
- [ ] Ekranı çıkar ve tak: pencereler doğru yere geliyor, görev çubuğu bütün ekranlarda gizli. **Kısmi:** kod var, çoklu ekran açıkken denenmeli (yukarıdaki kısıt).
- [x] Bellek ölçüldü: 82.7 MB private. Bütçe kararı revizyon sonrasına bırakıldı: AGENTS.md'deki < 60 MB hedefi geçerli, 85 MB eşiği resmi karar değil (bkz. `docs/MEMORY.md`).

---

## Sonraya kalanlar
- Google / Outlook takvim (OAuth).
- Sistem tepsisi simgelerini dock'a taşıma.
- Sabitli uygulama sırasını kayıt defterinden (`Taskband\Favorites`) okuma.
- Kod imzalama.
