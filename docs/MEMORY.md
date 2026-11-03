# Bellek ölçüm kaydı

Her aşamanın sonunda `scripts/measure-memory.ps1` çalıştır ve sonucu bu tabloya ekle.
Hedef: tek monitörde, boşta **< 60 MB private**.

Makine: Windows 11 Home 26200, RTX 4050 Laptop + Intel UHD.

| Tarih | Aşama | Private | Working set | Not |
| --- | --- | --- | --- | --- |
| 2026-11-03 | M0, GPU ile çizim | 169 MB | 194 MB | NVIDIA sürücüsü ~135 MB ekliyor |
| 2026-11-03 | M0, yazılımla çizim | 34 MB | 93 MB | Varsayılan yapıldı (`App.xaml.cs`) |
| 2026-11-04 | M0, Features yapısı + 500×260 tuval | 35 MB | 94 MB | Büyük tuval belleği değiştirmedi |
| 2026-11-04 | M1, NotchShape + Müzik paneli | 47.7 MB | 127.0 MB | Bütçe (< 60 MB) korundu |
| 2026-11-04 | M1 Tam (Müzik + Görünürlük Modları) | 45.1 MB | 125.5 MB | Bütçe (< 60 MB) korundu |
| 2026-11-04 | M2, Kontrol Merkezi + Durum Panelleri | 40.9 MB | 258.9 MB | Bütçe (< 60 MB) korundu |
| 2026-11-04 | M3, Takvim, Pomodoro, Raf, Ayarlar | 39.2 MB | 256.6 MB | Bütçe (< 60 MB) korundu |

## Öğrenilenler
- **GPU ile çizim pahalı.** WPF donanım hızlandırması açıkken ekran kartı sürücüsü sürece ~135 MB private bellek ekliyor. Notch ve dock küçük yüzeyler; yazılımla çizim (`RenderMode.SoftwareOnly`) yeterli. Animasyon akıcılığı M1'de ölçülecek; takılırsa önce çizilen alanı küçült, GPU'ya dönmeden önce ölç.
- Working set paylaşılan DLL'leri de sayar; karşılaştırmada **private** değeri esas alınır.
