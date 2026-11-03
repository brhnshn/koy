# Koy (v0.1.0)

Windows 11 için son derece hafif, yay fiziği animasyonlu bir **Notch** (üst kenar koyu/adası) ve görev çubuğunun yerine geçen bir **Dock**.

Hedef: tek monitörde boşta **< 60 MB private bellek** (Güncel ölçüm: **~45 MB**).

```
   ┌────────────────── Koy Notch ──────────────────┐
   │ [Kapak] Şarkı - Sanatçı   [Dalga]  [Durum: Pil] │
   └───────────────────────────────────────────────┘
                          ...
   ┌─────────────────── Koy Dock ──────────────────┐
   │ [Win] | [Chrome] [Code] [Terminal] [Obsidian] | [^] │
   └───────────────────────────────────────────────┘
```

---

## Özellikler

### 1. Yay Animasyonlu Notch (Üst Kenar Koyu)
- **Müzik Paneli:** Spotify, YouTube (Edge/Chrome) ve Windows Media Player oturumlarını algılar. Albüm kapağı, parça adı, sanatçı, süre çubuğu ve oynat/durdur/ileri/geri kontrolleri.
- **Kontrol Merkezi:** Wi-Fi ve Bluetooth aç/kapa, Rahatsız Etme modu, sistem sesi ve donanım monitör parlaklığı kaydırıcıları.
- **Durum Paneli:** Pil yüzdesi, şarj durumu, Open-Meteo hava durumu (API anahtarsız) ve saat göstergesi.
- **Takvim & Pomodoro:** Ay görünümü, yerel zamanlayıcı ve mola durum makineli Pomodoro modülü.
- **Raf (Shelf):** Sürükle-bırak dosya ve metin geçici depolama alanı.
- **Görünürlük Modları:**
  - `Fixed`: Ekranda sabit durur.
  - `Smart`: Üzerine pencere gelince veya tam ekran uygulama açılınca otomatik gizlenir.
  - `Peek`: Ekranın en üst kenarına fare yaklaştığında çıkar.

### 2. Dock (Görev Çubuğu Alternatifi)
- **Sabitlenmiş Uygulamalar:** Windows görev çubuğuna sabitlenmiş kısayolları otomatik listeler ve 32px simgeleri önbelleğe alır.
- **Açık Pencereler:** Açık uygulamaları gösterge noktalarıyla belirtir, tıklayınca öne getirir veya simge durumuna küçültür.
- **Etkileşim:** Sol tıkla odaklan/küçült, orta tıkla yeni pencere aç, sağ tıkla kapat veya görevi sonlandır.
- **Akıcı Büyüme (Magnification):** macOS tarzı fizik tabanlı yay (`Spring`) büyütme efekti (ayarlardan açılıp kapatılabilir).
- **Başlat & Sistem Tepsisi:** Başlat menüsü tuşu (`Win`) ve taşma menüsü (`Win+B`) erişimi.
- **Görev Çubuğu Güvenliği (Kritik):** Dock kapalıyken görev çubuğuna asla dokunulmaz. Dock etkinken Windows görev çubuğu gizlenir ve sistem çökse dahi açılışta veya kapanışta Windows görev çubuğu otomatik geri yüklenir.

### 3. Çoklu Monitör Desteği
- `EnumDisplayMonitors` ve Per-Monitor DPI ölçekleme desteği.
- Çözünürlük veya monitör değiştiğinde (`WM_DISPLAYCHANGE`) pencereler fiziksel koordinatlara göre otomatik ortalanır.

---

## Kurulum ve Çalıştırma

### Gereksinimler
- Windows 11 (Derleme 22000 veya üzeri)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Kaynak Koddan Derleme
```powershell
# Depoyu klonla
git clone https://github.com/burhan/notch-dock.git
cd notch-dock

# Derle ve testleri calistir
dotnet build Koy.slnx
dotnet test Koy.slnx

# Calistir
dotnet run --project src/App
```

### Kurulum Paketi Oluşturma (Velopack)
```powershell
powershell -ExecutionPolicy Bypass -File scripts\pack.ps1 -Version 0.1.0
```
Oluşan yükleyici `dist\Koy-Setup.exe` dizininde yer alır.

### SHA256 Doğrulama
```powershell
Get-FileHash dist\Koy-Setup.exe -Algorithm SHA256
```
Çıkan karmayı `dist\SHA256SUMS.txt` dosyasıyla karşılaştırabilirsiniz.

---

## Mimari ve Prensipler

```
src/
  Core/       Saf C# (net10.0). WPF ve Windows API YOK. Durum makineleri, yay fiziği, modeller.
  Platform/   Windows 11 API uygulamaları (CsWin32 P/Invoke & WinRT).
  App/        WPF (net10.0-windows10.0.22621.0). İnce ViewModel'ler ve modern arayüz.
tests/
  Core.Tests/ xUnit testleri (100 test).
```

- **Yazılımla Çizim Varsayılan:** GPU sürücülerinin eklediği ~135 MB gereksiz bellek yükü önlenmiştir.
- **Tek Süreç:** Tüm pencereler tek bir hafif süreçte çalışır.
- **Sıfır Unicode Emoji:** Arayüzdeki tüm ikonlar hafif vektör yolları (`Path`) olarak tasarlanmıştır.

---

## Belgeler

- [AGENTS.md](AGENTS.md): Geliştirme standartları ve mimari kurallar
- [docs/PLAN.md](docs/PLAN.md): Mimari kararlar ve teknoloji seçimi
- [docs/ROADMAP.md](docs/ROADMAP.md): Tamamlanan aşamalar yol haritası
- [docs/MEMORY.md](docs/MEMORY.md): Aşamaların bellek ölçüm kayıtları
- [docs/NAMING.md](docs/NAMING.md): İsim geçmişi

---

## Lisans

Bu proje [GPLv3](LICENSE) ile lisanslanmıştır.
