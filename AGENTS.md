# AGENTS.md — Koy

Bu dosya bu depoda çalışan bütün ajanlar (Antigravity/Gemini, Claude, Codex) için tek talimat kaynağıdır.
İletişim Türkçe. Kullanıcı Burhan.

## Proje
Windows 11 için hafif bir **notch** (ekranın üst ortasında, yay animasyonlu koy/ada) ve görev çubuğunun yerine geçen bir **dock**.
İlham: [kua8/Nectar](https://github.com/kua8/Nectar) (Tauri/WebView2, bellek ağır). **Nectar'dan kod, görsel ya da metin kopyalanmaz**; Nectar GPLv3 olsa da biz kendi kodumuzu yazarız.

- Hedefler, mimari, kararlar: `docs/PLAN.md`
- Sıradaki iş ve dosya dosya yapılacaklar: `docs/ROADMAP.md`
- Bellek ölçümleri: `docs/MEMORY.md`
- Arayüz tasarımı (dock, çentik, Koy penceresi, her sayfanın ayarları): `docs/DESIGN.md`. Arayüz işinden önce oku. Ekranların görseli: `docs/design/index.html` (tarayıcıda aç).

## Yığın
C# + WPF, .NET 10 (`global.json`), yalnız Windows 11. MVVM: CommunityToolkit.Mvvm. Win32: Microsoft.Windows.CsWin32. Test: xUnit. Lisans GPLv3.

## Komutlar
```powershell
dotnet build Koy.slnx                       # derle
dotnet test Koy.slnx                        # testler
dotnet run --project src/App                # çalıştır (notch'a sağ tık → Çıkış)
powershell -ExecutionPolicy Bypass -File scripts\measure-memory.ps1   # bellek ölç
```
SDK bulunamazsa yeni terminal aç (PATH yenilenmemiş olabilir).

## Yapı
```
src/Core/       Saf C# (net10.0). WPF ve Windows API YOK.
                Servis sözleşmeleri (IMediaService…) ve veri modelleri, durum makineleri, orkestrasyon,
                yay fiziği, ayarlar. Klasörler özelliğe göre: Media/, Audio/, Notch/, Visibility/, Settings/…
src/Platform/   Core'daki sözleşmelerin Windows 11 uygulamaları (WinRT / CsWin32). Aynı klasör adları:
                Media/MediaService.cs : Core.Media.IMediaService. Win32 çağrısı için adını NativeMethods.txt'ye
                ekle, CsWin32 üretir; elle DllImport yazma.
src/App/        WPF. Yalnız görseller ve ince ViewModel'ler, özellik klasörleriyle:
                Features/IFeature.cs           özellik giriş noktası (Start / Stop)
                Features/Notch/                NotchFeature, NotchWindow, NotchShape, NotchViewModel
                Features/Music/, ControlCenter/, Status/, Calendar/, Shelf/   notch panelleri
                Features/Dock/                 DockFeature, DockWindow… (kapalıyken hiç yüklenmez)
                Features/Settings/             SettingsWindow
                Resources/                     paylaşılan stiller, Strings.resx
tests/Core.Tests/  xUnit, yalnız Core. Ayrı App.Tests yok: mantık Core'da, ViewModel ince kalır.
docs/           PLAN, ROADMAP, MEMORY, NAMING.
scripts/        Yardımcı betikler.
```
Bağımlılık yönü: `App → Platform → Core` ve `App → Core`. Core hiçbir şeye bağımlı değildir. Platform WPF'e bağımlı olmaz.
Özellikler ayrı projelere (assembly) bölünmez; klasör yeterli.

## Değişmez kurallar
1. **Bellek bütçesi: tek monitör, boşta < 60 MB private.** Her aşama sonunda `scripts/measure-memory.ps1` çalıştır, sonucu `docs/MEMORY.md` tablosuna yaz. Bütçeyi aşan değişiklik birleştirilmez; önce sebebi bul.
2. **Tarayıcı yok.** WebView2, CefSharp, Electron, Blazor Hybrid kullanılmaz.
3. **Yazılımla çizim varsayılan.** GPU ile çizim ~135 MB ekliyor (bkz. `docs/MEMORY.md`). `App.xaml.cs` içindeki `RenderMode.SoftwareOnly` satırını ölçmeden kaldırma.
4. **Tek süreç.** Bütün pencereler (her monitör için notch + dock, ayarlar) aynı süreçte. Ayarlar penceresi yalnız açılınca oluşturulur, kapanınca bırakılır.
5. **Servisler yalnız gerektiğinde çalışır.** Notch gizliyken görselleştirici, fare takibi, küçük resim üretimi durur. Zamanlayıcılar (`DispatcherTimer`) görünür değilken durdurulur.
6. **Animasyon yalnız hareket varken kare çizer.** `CompositionTarget.Rendering` animasyon başlarken bağlanır, `Spring` durunca çözülür (örnek: `NotchWindow.xaml.cs`). WPF `Storyboard` yerine `Core/Animation/Spring` kullanılır.
7. **Görev çubuğu güvenliği.** Uygulama nasıl kapanırsa kapansın (normal çıkış, istisna, süreç öldürülmesi) Windows görev çubuğu geri gelmeli. Açılışta `TaskbarService.Restore()` çağrısı kalır. Dock'la ilgili her değişiklikte elle test: Görev Yöneticisi'nden süreci öldür → uygulamayı yeniden aç → görev çubuğu görünüyor mu?
8. **Ağır kütüphane yok.** Serilog, Autofac, ReactiveUI, MahApps, büyük UI kütleri eklenmez. Yeni NuGet paketi eklemeden önce Burhan'a sor ve bellek etkisini ölç.
9. **Görseller:** Albüm kapağı gibi resimler gösterilecek boyutta çözülür (`BitmapImage.DecodePixelWidth`), sonra `Freeze()` edilir.
10. **Ayarlar** `%AppData%\Koy\settings.json`. Yeni alan → `AppSettings` içine varsayılan değerle eklenir; kaynak üreticili `SettingsJsonContext` kullanılır, yansımalı `JsonSerializer` çağrısı yazılmaz.
11. **Sözleşmeler Core'da.** Her platform servisinin arayüzü ve veri modeli `src/Core/<Alan>/` içinde, uygulaması `src/Platform/<Alan>/` içinde. Core arayüzlerinde **WinRT / Win32 tipi görünmez** (`IRandomAccessStreamReference`, `HWND` yok): kapak `byte[]`, pencere kimliği `nint`. Durum ve karar mantığı (ne zaman gizlen, hangi panel, Pomodoro) Core'da yazılır ve test edilir; ViewModel yalnız bağlar.
12. **Özellik yükleme.** Her özelliğin tek giriş noktası `IFeature` (`Start` / `Stop`). `App.xaml.cs` ayarlara bakıp yalnız açık özellikleri başlatır. Kapalı özelliğin penceresi oluşturulmaz, servisi ve hook'u kurulmaz. **Dock kapalıyken (`DockEnabled = false`) Windows görev çubuğuna hiç dokunulmaz** (açılıştaki güvenlik `Restore()` çağrısı hariç).
13. **Sabit tuval.** Notch penceresi en büyük panel boyutunda (şu an 500×260 DIP) sabit durur; animasyon yalnız içindeki şeklin boyutunu değiştirir. `Window.Width/Height` animasyon sırasında değiştirilmez (saydam pencere titrer). Yazılımla çizimde maliyet pencere boyutuyla orantılıdır (her karede bütün pencere gönderilir), bu yüzden pencereyi gereğinden büyük yapma. M1'de panel açılışında fps ve CPU ölçülür; takılırsa çözüm, pencereyi yalnız durum değişiminde (açılmadan önce / kapandıktan sonra) bir kez boyutlandırmaktır.

## Kod biçimi
- Dosya kapsamlı namespace, `Nullable` açık, `var` serbest.
- Private alanlar `_camelCase`. ViewModel'lerde `[ObservableProperty]` ve `[RelayCommand]`.
- Yorumlar Türkçe ve kısa; *neden*i anlatır, *ne*yi değil.
- Kullanıcıya görünen metin XAML'e ya da koda sabit yazılmaz: `src/App/Resources/Strings.resx` (İngilizce) ve `Strings.tr.resx` (Türkçe), ikisine birden eklenir (M3'te kurulur; o zamana kadar Türkçe sabit metin kabul).
- Platform servisleri Core'daki arayüzlerin arkasında olur (kural #11) ki mantık sahte servisle test edilebilsin.

## Bitti tanımı (her görev için)
- `dotnet build` uyarısız ve hatasız, `dotnet test` yeşil.
- Davranış uygulama çalıştırılarak denendi (ekran görüntüsü ya da kısa açıklama).
- Aşama bittiyse bellek ölçüldü ve `docs/MEMORY.md` güncellendi.
- `docs/ROADMAP.md` içindeki ilgili kutu işaretlendi.

## Git
- Ana dal `main`. Her iş kendi dalında: `feat/m1-music-panel`, `fix/...`.
- Commit mesajı Türkçe, kısa, emir kipinde: "Müzik panelini ekle".
- Push ve PR yalnız Burhan isteyince.
