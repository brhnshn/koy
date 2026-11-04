# DESIGN — Koy arayüz tasarımı (M6)

Burhan onayladı (2026-11-04): "çok iyi olmuş". Kodlarken bu belge esas alınır.

**Tasarımları görmek için:** [`docs/design/index.html`](design/index.html) dosyasını herhangi bir tarayıcıda aç. Tek dosyadır, internet gerekmez; bütün ekranlar sayfa sayfa görünür, düğmeler tıklanabilir. Ekranların kaynak kodu (renkler, ölçüler) `docs/design/source/*.dc.html` dosyalarında.

**Asıl tuval (claude.ai, özel):** https://claude.ai/artifact/N38DJr48ogJn8sZYS3HsoE. Tuval değişirse dosyaları `docs/design/source/` altına yeniden kopyala ve `python docs/design/build.py` çalıştır.

Tuvalde beş sayfa var:

| Sayfa | İçerik |
|---|---|
| Kararlar ve notlar | Kararlar, ilkeler, M6 yapılacakları, açık sorular |
| Koy uygulaması (ayarlar) | Sekiz ayar sayfası, ilk açılış, sağ alt menü |
| Dock görünümü | Yüzen dock, tam genişlik biçimi, etkileşimler |
| Pencereli görünüm | Aşağı yapış, çekil, yer ayır, iki ekran |
| Çentik görünümü | Durumlar, göstergeler, açık paneller, kısa bildirimler |

Kararların gerekçeleri vault'ta, `projects/notch-dock/Kurallar.md` dosyasında.

## İlkeler
1. Koy, Windows görev çubuğunun kopyası değildir. Saat, pil ve ağ çentikte durur, dock'ta değil.
2. Dock hiçbir pencerenin içeriğini örtmez. Pencere yaklaşınca yer ayırır, aşağı yapışır ya da çekilir.
3. Boştayken işlemci kullanımı sıfırdır. Zamanlayıcılar ve animasyonlar yalnız görünen panelde çalışır.
4. Göz yoran animasyon kullanılmaz. Yay animasyonları kısa ve sakindir.
5. Her davranış ayardan değiştirilebilir. Varsayılanlar akıllı seçilir.
6. Her ekranın kendi çentiği ve dock'u vardır. Tam ekran yalnız kendi ekranındaki Koy'u gizler.

## Görsel dil
- **Zemin:** koyu. Çentiğin dolgusu `#0A0A0B`. Pencere ve dock zemini `#18181B`; dock'ta bu renk %88–96 opaklıkla kullanılır. Kenarlık `rgba(255,255,255,0.10)`.
- **Vurgu:** `#3B82F6`. Ayarlardan başka renk seçilebilir.
- **Yazı:** metin `#F4F4F5`, ikincil metin `#A1A1AA`. Yazı tipi Segoe UI Variable.
- **Köşeler:** yüzen dock 18 px, Koy penceresi 12 px, çentiğin alt köşeleri 18 px.

## Dock
- **Yüzen (varsayılan):**
  - 60 px yüksekliğinde, ekranın altından 10 px yukarıda durur.
  - Düzen soldan sağa: Başlat, uygulamalar, ayırıcı, klasörler ve küçültülmüş pencereler, ayırıcı, Koy simgesi.
  - Çalışan uygulamanın altında nokta, aktif uygulamanın altında 16 px'lik çizgi görünür.
- **Tam genişlik:**
  - 44 px yüksekliğinde. Saat ve tepsi yoktur.
  - Solda çalan müzik, ortada uygulamalar, sağda yalnız Koy simgesi durur.
  - Aktif uygulama, adıyla birlikte hap biçiminde gösterilir.
- **Pencere yaklaşınca** (Koy › Dock):
  - **Yer ayır:** dock alt kenara oturur ve koyu bir şerit olur. Çalışma alanından 46 px ayrılır. Büyütülmüş pencere tam dock'un üstünde biter, arada boşluk kalmaz.
  - **Aşağı yapış (varsayılan):** normal boy bir pencere dock'a yaklaşınca dock alt kenara iner, 60 px'ten 46 px'e incelir, yalnız üst köşeleri yuvarlak kalır. Pencere uzaklaşınca dock yeniden yüzer.
  - **Çekil:** büyütülmüş pencere varken dock gizlenir ve altta 120×4 px'lik bir tutamak kalır. Fare alt kenara inince dock 150 ms sonra geri gelir.
  - **Hep gizli.**
  - Tam ekranda dock her modda gizlidir.
- **Etkileşimler:**
  - Pencere önizlemesi 400 ms bekledikten sonra açılır. Aynı uygulamanın pencereleri gruplanmışsa önizlemeler yan yana gösterilir.
  - Sağ tık menüsü: son kullanılan dosyalar, Yeni pencere, Dock'tan kaldır, Bütün pencereleri kapat, Görev Yöneticisi'nde sonlandır.
  - Okunmamış sayısı kırmızı rozetle gösterilir. Dikkat isteyen pencerenin göstergesi turuncudur.
  - Klasöre tıklanınca son dosyalar ızgara halinde açılır.
  - Sürükleme: sürüklenen simge için yer açılır. Bir dosya uygulamanın üstüne bırakılınca o uygulamayla açılır.
  - Büyütme efekti isteğe bağlıdır, varsayılan olarak kapalıdır.

## Çentik
- **Boşta:** 236×34 px. Solda saat, sağda pil görünür.
- **Müzik çalarken:** solda albüm kapağı, sağda hareketsiz bir dalga simgesi.
- **Göstergeler:** ses ya da parlaklık değişince çentik 260×40 px olur ve gösterge 1,5 sn görünür. Ses göstergesi beyaz, parlaklık göstergesi sarı (`#FACC15`).
- **Açık çentik sekmeleri:** Müzik, Takvim (Pomodoro ile), Raf, Kontrol ve Durum. Raf, çentiğe dosya bırakılınca kendiliğinden açılır.
- **Kısa bildirimler (Peek):** "Pomodoro bitti", "Şarj tamamlandı", "Kulaklık bağlandı" gibi.

## Koy penceresi
- 880 px genişliğinde, orta boy bir pencere.
- Pencereyi kapatmak Koy'u kapatmaz. Çıkış yalnız sağ alttaki Koy menüsünden ya da pencerenin sol altındaki "Koy'dan çık" düğmesinden yapılır.
- Sol menüde sekiz sayfa vardır:

| Sayfa | Ayarlar |
|---|---|
| Genel | Windows açılınca başlat; Windows görev çubuğunu gizle; arayüz dili; güncellemeleri otomatik denetle; kanal (Kararlı / Önizleme); Şimdi denetle |
| Çentik | Çentiği göster; ne zaman görünsün (Her zaman / Akıllı / Fareyle); tam ekranda gizle; boştayken ne gösterilsin (Saat ve pil / Yalnız saat / Hiçbiri); müzik çalarken kapak ve dalga; ses göstergesi; parlaklık göstergesi; Windows'un kendi göstergesini gizle (deneysel); paneller (sürükleyerek sıralanır, tek tek kapatılır); açılma (Üzerine gelince / Tıklayınca); boyut |
| Dock | Dock'u göster; biçim (Yüzen / Tam genişlik); pencere yaklaşınca (Yer ayır / Aşağı yapış / Çekil / Hep gizli); tam ekranda gizle; geri çıkma gecikmesi; simge boyutu; büyütme efekti; çalışan göstergesi (Nokta / Çizgi); pencereleri grupla; bildirim rozetleri; Başlat düğmesi; klasörler; küçültülmüş pencereler; son kullanılanlar; pencere önizlemesi ve gecikmesi |
| Ekranlar | Monitör haritası (seçilen ekranın ayarları açılır); ekran başına çentik ve dock; dock'ta yalnız o ekrandaki pencereler; yeni bağlanan ekranda ne gösterilsin (Çentik ve dock / Yalnız çentik / Hiçbiri) |
| Görünüm | Tema (Sistem / Koyu / Açık); vurgu rengi; Windows vurgu rengini kullan; malzeme (Katı / Yarı saydam); köşe yuvarlaklığı; animasyon (Sakin / Normal / Hızlı / Kapalı); Windows'un "animasyonları azalt" ayarına uy |
| Kısayollar | Çentik, Raf, Dock, Koy menüsü ve müzik için klavye kısayolları (çakışan kısayol kırmızıyla gösterilir); çentikte tekerlek (Panel değiştir / Ses); dock'ta orta tık (Yeni pencere / Kapat) |
| Performans | Bellek göstergesi (60 MB bütçesine karşı); pil tasarrufunda animasyonları azalt; pil tasarrufunda müzik dalgasını durdur; hava durumunu yenileme sıklığı; ses görselleştirici (deneysel) |
| Hakkında | Sürüm ve lisans (GPLv3); güncellemeleri denetle; tanılama bilgisini kopyala; **Windows görev çubuğunu geri getir** (acil durum düğmesi); ayarları sıfırla; kaynak kodu |

- **Sağ alt menü:** Koy simgesi dock açıkken dock'un sağ ucunda, dock kapalıyken Windows bildirim alanında durur. Menüde şunlar var:
  - Çentik ve Dock için aç/kapa düğmeleri
  - Koy penceresini aç
  - 1 saat duraklat
  - Koy'dan çık
- **İlk açılış:** üç adımlık bir kurulum. İkinci adımda kullanıcı seçer: Çentik ve dock / Yalnız çentik / Yalnız dock.

## Açık sorular
- Dock'taki ayırıcı ve klasörler kesin değil: Burhan "olabilir" dedi. Ayardan kapatılabilir olacak.
- Varsayılan "pencere yaklaşınca" davranışı Aşağı yapış seçildi, gerekirse değiştirilecek.
- Kısayollar örnek. Windows kısayollarıyla çakışma kontrolü yapılacak.
