# Değişiklik Günlüğü (Changelog)

Bu projedeki tüm önemli değişiklikler bu dosyada belgelenecektir.
Biçim [Keep a Changelog](https://keepachangelog.com/tr/1.1.0/) standardına dayanır ve bu proje [Semantik Sürümleme (SemVer)](https://semver.org/lang/tr/) kurallarına uyar.

---

## [0.1.1](https://github.com/brhnshn/koy/compare/v0.1.0...v0.1.1) (2026-11-06)


### Eklenenler

* çentiğe sekmeler ve Wi-Fi / Bluetooth durum simgeleri ekle ([eb50182](https://github.com/brhnshn/koy/commit/eb50182cbee95ba5985a6c19de73435116e6958b))
* dock'a sistem tepsisi ekle ([e763ee5](https://github.com/brhnshn/koy/commit/e763ee594f9454fce7fab7f7f0d19da6de1ec732))
* tepsi penceresi yalnız görev çubuğu gizliyken kurulur, bütün dock'lar sayaçlı tek örneği paylaşır ve kapanışta Windows tepsisi geri gelir ([a55ee88](https://github.com/brhnshn/koy/commit/a55ee88abc0da4dde1ac4749346441c392ea0d2a))


### Düzeltmeler

* açık paneller değişince (ör. Raf'a sürükle-bırak) çentik sekmelerini yeniden kur ([59f86f2](https://github.com/brhnshn/koy/commit/59f86f2cd1633ca3a56fd0700e2fd9297a23d58e))
* açılışta kapalı çentiğin zeminini çiz ([2f21718](https://github.com/brhnshn/koy/commit/2f2171886d81aa605d45daa8ae39b3104037c010))
* ağ durumu servisini paylaşımlı kur, Wi-Fi adını ve özetini Core'a taşı ([69edde3](https://github.com/brhnshn/koy/commit/69edde3805b5f1c517c16dbbd3b37bce12c38477))
* Ana kart düğmelerinin boşlukları da tıklanır; hızlı başlat + ortasına tıklayınca çentik kapanmıyor ([3b95e22](https://github.com/brhnshn/koy/commit/3b95e2250d7058541b962e8422bdbc478c4dd215))
* Ana müzik kartında çalan sesten 24 çubuk (WASAPI loopback, yalnız kart görünür ve müzik çalarken) ([90ca584](https://github.com/brhnshn/koy/commit/90ca5849893a6ffc900f99c649bf3121f11bd03a))
* Ayarları sıfırlayınca kurulum sihirbazı tekrar açılmasın, çalışan özellikler yeni varsayılanla yeniden kurulsun ([3217785](https://github.com/brhnshn/koy/commit/3217785dd075d1d3e59f546dbd7a10d306f36e37))
* betiklerde native komutlarin cikis kodunu denetle ([90a43a9](https://github.com/brhnshn/koy/commit/90a43a90c41b8f5d30bf1ae42a9bd8203b3d0a84))
* bir ekrandaki büyütülmüş pencere öbür ekranın dock'unu yapışık moda geçirmesin ([679d882](https://github.com/brhnshn/koy/commit/679d88254e3a78b8d33d501a2fd3890da2a1a3c7))
* boşta görünüm Yalnız saat ve Hiçbiri iken Wi-Fi / Bluetooth simgelerini gizle ([b4db5b6](https://github.com/brhnshn/koy/commit/b4db5b6dab17bb42ac9f247f54fd022784be59a8))
* boşta pil göstergesini HasBattery'ye bağla, HasBattery değişince ShowIdleBattery bildir ([5b0bafd](https://github.com/brhnshn/koy/commit/5b0bafd771259ad8db9317eb82d92c58d601614b))
* büyütülmüş pencerede dock'u alt kenardan açılır yap, tam ekranı üç işaretle ayır ve monitör başına değerlendir ([6444cb4](https://github.com/brhnshn/koy/commit/6444cb4546f57445b105b3558a79fe13291b6f97))
* çentik ayarı açık her monitörde ayrı pencere olarak kurulsun ve ekranın kendi DPI'ına göre yerleşsin ([c283f78](https://github.com/brhnshn/koy/commit/c283f783e401fad3615072cb4a09f413d01df2ca))
* Çentik ayarlarını davranışa bağla: tam ekranda gizle, boşta gösterim, müzik dalgası, açılma davranışı, tekerlek eylemi ([061ba4a](https://github.com/brhnshn/koy/commit/061ba4a9d107bb9942bc3e9e9f7e658af9225770))
* Çentik ve dock sağ tık menülerinden Çıkış'ı kaldır; çıkış yalnız Koy penceresi ve sağ alt menüden ([b6ccc52](https://github.com/brhnshn/koy/commit/b6ccc5227edd6e2efc12b06e88b01d9812252fc8))
* cihaz konumunda çentikte şehir adı (OpenStreetMap ters konumlama, ~1 km yuvarlanmış koordinat); hava isteği de yuvarlanır ([35dcf66](https://github.com/brhnshn/koy/commit/35dcf6616947984388b082209d636f1ff2b4a08b))
* çökme işleyicilerinde Koy'un tepsi penceresini de kapat ([b0d2f1d](https://github.com/brhnshn/koy/commit/b0d2f1dbdae65b968cc918c620f50ffaf0e5c3c9))
* dock açıkken Koy'un bildirim alanı simgesini kaldır, dock kapanınca ya da duraklayınca geri getir ([5fd6655](https://github.com/brhnshn/koy/commit/5fd6655224da859e4b05ae78772af94cf0b1435a))
* dock ayarı açık her monitörde kurulsun, AppBar payı ve görev çubuğu gizleme her ekranda doğru çalışsın ([0af2701](https://github.com/brhnshn/koy/commit/0af2701d6de97e2b07cc14c173a361e4fb8edc5d))
* Dock orta tık eylemini ve çalışan göstergesi stilini (nokta/çizgi) ayara bağla ([44a85fb](https://github.com/brhnshn/koy/commit/44a85fbe08588a3d2aa441204e153d86a03e90df))
* dock pencere önizlemesini üst düzey pencereye kaydet ([894be8b](https://github.com/brhnshn/koy/commit/894be8bd2903ca3180f9007421c8f21eef2ea9c9))
* dock Stop'ta görev çubuğu geri yüklemesi try/finally ile garanti, ölü tepsi tutamacı yeniden kurulur, elle geri getirmede tepsi bırakılır ([6a42bfa](https://github.com/brhnshn/koy/commit/6a42bfae01b51e11b181824552053e1790aaa7fe))
* dock tepsi düğmesi kaybolmasın, tepsi penceresi ölünce ya da kurulum başarısız olunca yeniden kurulsun ([f98afcc](https://github.com/brhnshn/koy/commit/f98afccf4aacaf383b5d2e0b1cc0498fc89f43f6))
* dock varsayılanı Yer ayır, Pencere dock'a yaklaşınca seçeneklerine seçime göre açıklama satırı ([54c2db7](https://github.com/brhnshn/koy/commit/54c2db7334e1be7a90f1cfdd1e6a819191a25ebf))
* Dock varsayılanını Aşağı yapış yap, DockDown'u Çekil'e çeviren zorla göçü kaldır ([0fd1121](https://github.com/brhnshn/koy/commit/0fd1121988db8aaf79090489ac29b024b42a34c8))
* dock'a son kullanılan dosyalar yığın düğmesini ekle (Windows Son Kullanılanlar klasörü, yalnız açılınca okunur) ([bc7cb83](https://github.com/brhnshn/koy/commit/bc7cb8394b1e7b232aed8f4cb84b8fe4e0f49cd8))
* DWM önizleme güncellemesi başarısız olursa kutuyu hatırlama, sonraki düzen geçişinde yeniden dene ([455884c](https://github.com/brhnshn/koy/commit/455884c8cf6d3893455f5bed05286b8c95e23d16))
* Explorer yeniden başlayınca tepsi simgesini ekle ve görev çubuğu gizlemesini yeniden uygula ([dad923b](https://github.com/brhnshn/koy/commit/dad923b7bba813cda7970c76b4014ec917ce71da))
* Explorer yeniden başlayınca Yer ayır modunda AppBar kaydını yeniden yap ([13e879d](https://github.com/brhnshn/koy/commit/13e879de65500d3bdb097004b278978598549b01))
* global hata yakalamayı sağlamlaştır (CrashLog, UnobservedTaskException, WndProc ve WinEvent korumaları) ([4291852](https://github.com/brhnshn/koy/commit/4291852e00d356d2b684c0af94c9c586beb23a05))
* görev çubuğunu Koy öldürülünce geri getiren gözcü süreç ekle ([4bb6101](https://github.com/brhnshn/koy/commit/4bb6101ad0f08328a8138fc834ff5dc6b4b747d9))
* güncelleme ve kaldırma kancalarında görev çubuğunu hata toleranslı biçimde geri getir ([275bce5](https://github.com/brhnshn/koy/commit/275bce59a86214ca07755b5df139ecef784856ab))
* Güncellemeleri otomatik denetle ayarını bağla (açılıştan sonra arka planda indir, kapanırken uygula) ([0211323](https://github.com/brhnshn/koy/commit/021132365a5b51e47a1af50770fda00e569a4719))
* H çentiğine Pano, Sistem, Takvim sayfalarını, Odak görevlerini ve Ana sağ sütununu bağla (servisler yalnız görünürken alınır) ([7ce18e5](https://github.com/brhnshn/koy/commit/7ce18e57bb8c67aa95ef66ee9591f2f7e667ef61))
* H öncesi ayar dosyalarında Pano ve Sistem sekmeleri açık gelsin; CrashLog yolunu değiştiren testler sırayla koşsun ([1c45c51](https://github.com/brhnshn/koy/commit/1c45c51e37e5c22a4c6a199d55e319b611986d21))
* H servisleri main'e taşınırken kalan tepsi sayacı alanını kaldır (TrayHostLease kullanılıyor) ([f98d18c](https://github.com/brhnshn/koy/commit/f98d18c122891ab718a6f29e4cab106eb8529dcf))
* Hakkında sayfasına Koy'u kaldır düğmesi ve onay görünümü ekle ([30e1b19](https://github.com/brhnshn/koy/commit/30e1b197632665d3c1d22d14c70c3a7969bafe99))
* Hava durumu yenileme sıklığını ve Pomodoro sürelerini ayara bağla ([f2b1cfd](https://github.com/brhnshn/koy/commit/f2b1cfdfd50f67dd3659496b3096546a9ffba678))
* hava durumunu konuma bağla (sihirbaz adımı, ayarlarda konum anahtarı ve şehir arama, Open-Meteo atfı) ([1b5db81](https://github.com/brhnshn/koy/commit/1b5db81fe3ce187cd11762e65084edce5d300b4c))
* hızlı başlat dosya seçicisi açıkken çentik açık kalır, seçici çentik penceresine bağlı açılır; ROADMAP ve snapshot sahneleri ([ed07281](https://github.com/brhnshn/koy/commit/ed0728134449347ec3cbabbe7bae8af0edbf38dd))
* HomePanel yardımcı metodu Parent özelliğini gizlemesin (CS0108) ([38c3614](https://github.com/brhnshn/koy/commit/38c361466aa2d9e2547abd5165bfeb5dbf5c6005))
* kenar gözetleme zamanlayıcılarını ve Core olaylarını UI thread'ine taşı (VerifyAccess çökmesi) ([f3cc88f](https://github.com/brhnshn/koy/commit/f3cc88f3159cc95d4e30ca2f839aea72a48caa57))
* kontrol merkezi döşemelerini sürükleyerek sırala, sırayı ayara yaz ve açılışta uygula ([28c72c0](https://github.com/brhnshn/koy/commit/28c72c06020c790be6dd30dcb4d06165bbccd7c1))
* Kontrol Merkezi'nde Wi-Fi ve Bluetooth'u gerçekten yönet ([0c2f878](https://github.com/brhnshn/koy/commit/0c2f8785de9f487168ce01096e7530c5ad4191c3))
* Kontrol'de bölünmüş Wi-Fi/Bluetooth döşemeleri, çentik içi liste sayfaları, Bluetooth rune simgesi, Ana kulaklık kartı ve Bağlı cihazlar ([4742f88](https://github.com/brhnshn/koy/commit/4742f886ef49e05117b9e5e9e6d03e2387e38c03))
* Kontrol'de Rahatsız Etme başlığı daralan döşemeye sığsın ([d5df8e7](https://github.com/brhnshn/koy/commit/d5df8e733e62937899c24042314760593bcda522))
* köşe varsayılanı değişikliğine göre ayar testlerini güncelle ([b25018a](https://github.com/brhnshn/koy/commit/b25018a827b2c9d333ef6d62073482f9ab1d4e3b))
* kurulum sihirbazı tamamlanmadan kapatılırsa dock açılıp görev çubuğu gizlenmesin ([24acf78](https://github.com/brhnshn/koy/commit/24acf78e49fe93c1acdd4eb7022c82a8a84b38ae))
* müzik kapağı net (320 px'e kadar çözülür, yüksek kaliteli ölçekleme, yuvarlak köşe); çubuklarda tiz bantlar eğimle dengelenir ([c47e1b7](https://github.com/brhnshn/koy/commit/c47e1b77f9236e6317b99a69035bab966cc5cefd))
* müzik kartına tıklayınca Ana gövdesini kaplayan büyük düzen, tekrar tıklayınca eski boyut ([a9c585b](https://github.com/brhnshn/koy/commit/a9c585beb1cd41aa790b5891e9b1a3943afde6a5))
* otomatik güncelleme yeni sürüm bulunca 'güncel' yazmasın; indirildi ve Koy kapanınca kurulacak durumu ([9127872](https://github.com/brhnshn/koy/commit/9127872ddccda2f8fff380f10f1e36c2e575b8c3))
* panel geçiş animasyonu animasyon hızına, azaltılmış harekete ve pil tasarrufuna uysun ([51ec7d8](https://github.com/brhnshn/koy/commit/51ec7d85df8d44019a2807d1958d6e745b4c4827))
* parça yokken fareyle çentiğe girince Wi-Fi / Bluetooth simgeleri kaybolmasın (tıklama ve ipucu erişilebilir olsun) ([19c4f62](https://github.com/brhnshn/koy/commit/19c4f6213c0d35de7656f395e0ae483f2f9e7dca))
* parlaklık kaynağı ekran düzeniyle yeniden belirlenir, ayar çağrıları arka planda birleşir, yol yoksa kaydırıcı devre dışı ([b10fc8d](https://github.com/brhnshn/koy/commit/b10fc8d16210f40a85ce164de9347ff84adf2d3d))
* pil olmayan bilgisayarda çentikteki pil göstergesini gizle ([f03dc07](https://github.com/brhnshn/koy/commit/f03dc07f993ce9607809aa060ddc8451a31a3d32))
* Pil tasarrufunda animasyonları azalt ayarını çentik ve dock'a bağla; Dock seçenekleri değişince dock yeniden kurulsun ([6240ef4](https://github.com/brhnshn/koy/commit/6240ef4d18340d75d959a175cc219dab2f62da17))
* PinnedAppsService kısayol çözerken COM nesnesini serbest bırak ([584bb77](https://github.com/brhnshn/koy/commit/584bb775b534fef5e272ba45705a7361d48c3479))
* radyo taramasında bayat erişim durumunu sıfırla, görev ve abone hatalarını günlüğe yaz, birleşimde kaybolan parlaklık başlatmasını geri getir ([b2d58a3](https://github.com/brhnshn/koy/commit/b2d58a3443798bcb37c5e10389b0559a884e243d))
* README'de imza, SmartScreen, SHA256 ve otomatik güncellemenin neyi kanıtladığı açıkça yazıldı ([4d71d7b](https://github.com/brhnshn/koy/commit/4d71d7be80587d3d1869dd8a145f5120006022fd))
* ROADMAP Wi-Fi ve Bluetooth listeleri bölümü, DESIGN Kontrol ve AGENTS Yapı güncellendi ([68d8296](https://github.com/brhnshn/koy/commit/68d829690bc543ead45318ddc9061c336482db51))
* ROADMAP'e atlama listesi işini doğrulanmış bulgularıyla açık iş olarak ekle ([3c3d22f](https://github.com/brhnshn/koy/commit/3c3d22f8a96ca494ef8805c5f7c156214bc4c9d4))
* sabitli uygulama sırasını Koy ayarlarında tut, dock'ta sürükleyerek sıralamayı ekle ([606f410](https://github.com/brhnshn/koy/commit/606f410a54e07dd3023e3f5332f1bc77e3086462))
* Sağ alt menüdeki 1 saat duraklat gerçekten çalışsın (çentik ve dock durur, görev çubuğu döner, süre bitince devam) ([0680b1c](https://github.com/brhnshn/koy/commit/0680b1c2cefa2eb2ae55cec6f3a0a99ab73fa7f5))
* sağ alt menüdeki ekran sayısı metnini yerelleştir ([0feda6b](https://github.com/brhnshn/koy/commit/0feda6ba5f1dc405e5b6ea8b7e0504ee60333fec))
* sağ alt menüyü HWND oluşmadan ölçerken olan FailFast çökmesini gider ([18dd966](https://github.com/brhnshn/koy/commit/18dd96680a027b7d6db474d4fd1f48cfb83bef19))
* Ses ve parlaklık göstergesini (OSD) çentiğe bağla ve ayarlara uydur ([c3055fd](https://github.com/brhnshn/koy/commit/c3055fda0c4993f3a6c9e2cab70441076c0f904e))
* sihirbaz metinlerini resx'e taşı, çıkışa onay ekle, görünürlük açıklaması ve düğme/boşluk cilası ([2986590](https://github.com/brhnshn/koy/commit/29865906b3204c04ce8ae6c449a6fcacf5ad0171))
* Sistem sayfasına dönünce son bilinen değerler hemen gösterilir, anlık değerler servis başlarken okunur ([455e7e7](https://github.com/brhnshn/koy/commit/455e7e7568375f9c952fbc2f5169068e781859cf))
* takvime gün ve hafta görünümü, serbest zamanlayıcı ve kronometre ekle ([760c798](https://github.com/brhnshn/koy/commit/760c79817d25c06a9aeb2164f19302200815fcc4))
* tam ekran algısı odak başka ekrandayken de her monitörün kendi en üst penceresine baksın ([b6272d6](https://github.com/brhnshn/koy/commit/b6272d6d48052724881043fa19a0c001d48df9d6))
* TaskbarService.Hide idempotent olsun, önceki görev çubuğu durumu AUTOHIDE ile ezilmesin ([8c5996f](https://github.com/brhnshn/koy/commit/8c5996f47717462954a06e950affed2e56c96a55))
* tepsi açılır penceresi sağ tıkta kapanmasın, uygulama menüsü açıkken açık kalsın ve seçim ya da dış tıklamayla yay animasyonuyla kapansın ([536b13c](https://github.com/brhnshn/koy/commit/536b13cb02374aa6e6549151539740cc09ce54dc))
* tepsi ayarının metnini görev çubuğu gizliyken geçerli olduğunu söyleyecek şekilde güncelle ([a879bfd](https://github.com/brhnshn/koy/commit/a879bfda60586fcdce8a8f89a4db9ca72f934068))
* tepsi düğmesinin kullandığı SystemTray metin anahtarını İngilizce ve Türkçe kaynaklara ekle ([e9147a9](https://github.com/brhnshn/koy/commit/e9147a9ecc4c1829a7f932597483e6370ff089e1))
* tepsi kapanışta arayüz iş parçacığını 3 sn bekletmesin, uygulama adı çözümü (disk G/Ç) arka plana alındı ([b44ceaf](https://github.com/brhnshn/koy/commit/b44ceaf56b5f59d560778fdbab1c5cecd28aaab4))
* tepsi kayıtlarını kaçırma ve çift pencereyi gider ([640ffb9](https://github.com/brhnshn/koy/commit/640ffb914bd4e6b7bc40206abbac613b5fcb4dd4))
* tepsi penceresinin gölge rengini Freezable içinde StaticResource ile bağla ([ccb3a96](https://github.com/brhnshn/koy/commit/ccb3a96006778e02be4d2f5d537dbff5993d9bbc))
* tepsi servisinde GDI işini kilit dışına al, pencere tanıtıcısını iş parçacığı güvenli yap, hataları CrashLog'a yaz ([8a706ac](https://github.com/brhnshn/koy/commit/8a706ac0e58651759a9f40dc5d5f067f339edf91))
* tepsi uygulamasının sağ tık menüsü dock'un üstünde açılsın, menü varken dock kendini öne almasın ([f95d6fe](https://github.com/brhnshn/koy/commit/f95d6feeacfba655026be3b1bb17ab2f79eb86cf))
* tepsi WM_COPYDATA boyut denetimleri, simge sayısına üst sınır ve Koy-süreç filtresi Core'a taşındı ([739c9b0](https://github.com/brhnshn/koy/commit/739c9b01e6152e9d4897e257411171e04da93e93))
* test 3 bulguları (dock, tepsi simgeleri, sayfa geçişi, bellek göstergesi, tek örnek) ([d7630d5](https://github.com/brhnshn/koy/commit/d7630d5997f2f93aa735083d736a5894b1d1f5a0))
* UWP penceresinde Sonlandır ApplicationFrameHost'u değil gerçek uygulama sürecini hedeflesin ([e626516](https://github.com/brhnshn/koy/commit/e6265161e5633565a1727bd2c6873ce76a01fc23))
* Wi-Fi (WiFiAdapter + kayıtlı profil adları) ve Bluetooth (DeviceWatcher + KS bağlan/kes) platform servisleri ([1716786](https://github.com/brhnshn/koy/commit/1716786d0c836991054163a6875ac8feebfd32d0))
* Wi-Fi ve Bluetooth listeleri için Core mantığı (sinyal çubukları, birleştirme, cihaz türü, pil) ve testleri ([c8560e1](https://github.com/brhnshn/koy/commit/c8560e1c90afd570b434bd3ab16acb554c110179))
* Windows görev çubuğunu gizle anahtarı kapalıyken dock görev çubuğuna dokunmasın ([fc7eeb3](https://github.com/brhnshn/koy/commit/fc7eeb3e8e52bc3608c26bab4f30b963740168b2))
* Windows'tan kaldırmada başlatma kaydını ve durum dosyalarını temizle, ayarları koru ([f728f1c](https://github.com/brhnshn/koy/commit/f728f1ce4f0544834576a6ba8587cbfc82b1eecf))
* yatay video kapakları (Chrome ~150x83) kırpılıp büyütülmez; tamamı gösterilir, arkada bulanık hâli ([cdd31c4](https://github.com/brhnshn/koy/commit/cdd31c4f8fae9021aa392286c9aa3a2124f135ae))
* yeni takılan ekranda çentik ve dock varsayılanını açık olarak sabitle, ekran başına ayarı ayrı kuruluma bağlama ([81ef48d](https://github.com/brhnshn/koy/commit/81ef48ddc27bf53301b16b455bc957502871e157))


### Performans

* pencere olaylarında DockController yenilemesini ertele ve birleştir ([08af2fd](https://github.com/brhnshn/koy/commit/08af2fdb802776f962384175a58ff0bcbab923bf))

## [Unreleased]

<!-- release-please sürüm girdisini kendisi üretir. Bu bölüm elle yazılmış ayrıntılı özettir: sürüm PR'ında bu bölümün içeriği yeni sürüm başlığının altına taşınır ve [Unreleased] başlığı silinir (bkz. CONTRIBUTING.md, "Sürüm Nasıl Çıkar"). -->

0.1.0'dan sonraki ilk büyük sürüm. Öne çıkanlar:
- Yeni çentik tasarımı: 7 sekme (Ana, Odak, Takvim, Raf, Pano, Sistem, Kontrol).
- Windows'taki gibi Wi-Fi ve Bluetooth listeleri.
- Çalan müziğe göre hareket eden çubuklar ve büyütülebilen müzik kartı.
- Görev çubuğunu her koşulda geri getiren gözcü süreç ve tek örnek koruması.
- Çoklu ekran, kısayollar, Koyu/Açık tema, kaldırma ekranı.
- 0.1.0'da yalnız görüntü olan ayarların gerçekten çalışması.

> **v0.1.0'dan geçenler için:** v0.1.0'da otomatik güncelleme denetimi yoktu. Bu sürümü Ayarlar › Hakkında › "Şimdi denetle" ile ya da yeni kurulum dosyasıyla kurun; sonraki sürümler otomatik gelir. v0.1.0 ile başka bir Koy kopyasını aynı anda çalıştırmayın: eski sürüm ikinci ekranda çökebiliyor ve görev çubuğu geri gelmiyordu (bu sürümde düzeltildi).

### Düzeltmeler (Fixed)
- **Aynı anda iki Koy çalışmıyor:** ikinci açılış yeni bir kopya başlatmak yerine çalışan Koy'un penceresini öne getirir. İki kopya birlikte çalışınca eski sürüm ikinci ekranda çöküyor ve görev çubuğu geri gelmiyordu.
- **İki ekranda dock'lar birbirini etkilemiyor:** bir ekranda büyütülen pencere, kenardan taşan birkaç pikseliyle yan ekrandaki dock'u "yapışık" moda geçiriyordu. Ön plandaki pencere artık yalnız kendi ekranının dock'unu etkiliyor.
- **Başlat ve Arama açılınca dock görünüyor:** Windows tuşuyla açılan Başlat menüsü, Arama ve hızlı ayarlar dock'u gizlemiyor; dock o ekranda görev çubuğu gibi görünür kalıyor. Koy menüsünü açmak da dock'u gizlemiyor.
- **Parlaklık donması:** ikinci ekran kapatılınca parlaklık kaydırıcısı artık olmayan ekrana yazmaya çalışıyor ve çentik donuyordu. Parlaklık yolu ekran düzeni değişince yeniden seçiliyor (dizüstü ekranında Windows, harici ekranda DDC/CI), yazma arka planda yapılıyor; hiçbir yol yoksa kaydırıcı devre dışı ve nedeni yazıyor.
- **Sistem sayfası:** sekmeye geri dönünce değerler 1–2 saniye "–" görünüyordu; artık son bilinen değerler hemen görünüyor.
- **Hızlı başlat "+" düğmesi:** düğmenin ortasına tıklanınca çentik kapanıyordu; dosya seçici açıkken çentik açık kalıyor ve seçici çentiğin önünde açılıyor.
- **Tepsi simgeleri net:** simgeler kendi boyutunda (16 px) çiziliyor, saydamlık doğru okunuyor. Başlat düğmesinde gerçek Windows logosu.
- **Koy penceresinde sayfa geçişi** kesik kesik değil, tek bir yumuşak solma.
- **Bellek göstergesi** Görev Yöneticisi ile aynı ölçüyü (özel çalışma kümesi) gösteriyor.
- **Otomatik güncelleme durumu:** yeni sürüm bulunup indirildiğinde "güncel" yerine "yeni sürüm hazır; Koy kapanınca kurulacak" yazıyor.
- **Görev çubuğu Koy öldürülünce geri geliyor:** Görev Yöneticisi'nden sonlandırılan Koy'un gizlediği görev çubuğu otomatik gizlemede kalıyordu. Dock görev çubuğunu gizlediği sürece küçük bir gözcü süreç (`Koy.Watchdog.exe`, Native AOT, pencere yok) Koy'u izliyor; Koy ölürse görev çubuğunu önceki haline döndürüyor, düzgün kapanışta dokunmuyor.
- **Dock alt kenardan açılıyor:** büyütülmüş pencere dock'u gizlediğinde fare alt kenarda ~300 ms (tuş basılı değilken) durunca dock çıkıyor; Dock sayfasındaki "Alt kenardan aç" ile kapatılır. Oyunda, F11 tarayıcıda ve tam ekran videoda dock hiçbir koşulda çıkmıyor. Tam ekran artık Windows'un tam ekran bildirimi, başlık çubuğunun olmayışı ve monitörü kaplama işaretlerinin birleşimiyle ve monitör başına belirleniyor (yan ekrandaki oyun bu ekrandaki dock'u gizlemiyor).
- **Tepsi açılır penceresi sağ tıkta kapanmıyor:** uygulamanın bağlam menüsü açıkken pencere açık kalıyor; menüden seçim yapılınca ya da pencere ve menünün dışına tıklanınca kısa bir yay animasyonuyla kapanıyor.
- **Dock'taki tepsi düğmesi kaybolmuyor:** tepsi penceresi beklenmedik kapanırsa ya da kurulumu bir kez başarısız olursa düğme ekran değişimine kadar geri gelmiyordu; artık pencere kapanınca dock haberdar oluyor, düğme yeniden kuruluyor, başarısız kurulum zamanla yeniden deneniyor ve sebep `crash.log`'a yazılıyor.
- **Tepsi uygulamasının sağ tık menüsü dock'un altında kalmıyor:** dock ön plan değişiminde kendini yeniden en üste alıyor ve açılan menünün üstüne çıkıyordu. Menü açıkken dock kendini öne almıyor, tepsi penceresi açıkken görülen menüler en üste alınıyor ve menü çıpası dock'un üzerine düşerse dock'un üst kenarının üstüne çekiliyor.
- **Dock'ta tek Koy simgesi:** dock açıkken bildirim alanındaki Koy simgesi kaldırılıyor, dock kapanınca ya da duraklayınca geri geliyor.
- **0.1.0'da çalışmayan, artık çalışanlar:**
  - Kısayollar sayfası yalnızca görüntüydü; artık `RegisterHotKey` ile gerçekten kayıtlı ve değiştirilebilir.
  - Ses ve parlaklık göstergesi (OSD HUD) çentiğe hiç bağlı değildi; artık sistem ses ve parlaklık değişimlerinde açılıyor.
  - Çoklu ekran kullanıcı tarafından açılamıyordu (yalnız birincil ekran); artık Ekranlar sayfasından ekran başına seçiliyor.
  - "1 saat duraklat" yalnızca bir bayrağı değiştiriyordu; artık çentik ve dock'u durduruyor, görev çubuğunu geri getiriyor ve süre bitince devam ediyor.
  - "Pencere yaklaşınca" ayarı her açılışta "Çekil"e çevriliyordu; artık seçilen değer kalıyor.
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
  - Açık çentikte sekme çubuğu ve kapalı çentikte saatin yanında Wi-Fi ve Bluetooth durum simgeleri (#14). Sekmeler bu sürümde yeni çentik tasarımıyla yeniden düzenlendi.

### Eklenenler (Added)
- **Yeni çentik (tasarım H):** üstte saat, tarih, hava durumu, pil, bildirim ve iğne düğmesiyle durum satırı; altta ikonlu 7 sekme. Hangi sekmelerin görüneceği Ayarlar › Çentik'ten seçilir.
  - **Ana:** müzik kartı ya da selamlama ve büyük saat. Sağda bağlı kulaklığın pili, CPU/RAM, ağ hızı ve hızlı başlat şeridi (en çok 8 uygulama).
  - **Odak:** Pomodoro, serbest zamanlayıcı ve kronometre; günlük ve haftalık görev listesi. Çalışan zamanlayıcı kapalı çentikte de görünür.
  - **Takvim:** ay, hafta ve gün görünümü, yerel etkinlikler.
  - **Raf:** dosya ve metin sürükle-bırak.
  - **Pano:** kopyalanan metinlerin geçmişi (bağlantı, kod, metin süzgeci; sabitleme). Geçmiş yalnız bellekte tutulur, diske yalnız sabitlenen öğeler yazılır. Parola yöneticilerinin "geçmişe alma" işaretli içerikleri alınmaz. Ayarlardan kapatılabilir.
  - **Sistem:** CPU, bellek, en çok kaynak kullanan süreç, ağ hızı ve Koy'un kendi belleği.
  - **Kontrol:** Wi-Fi, Bluetooth, Rahatsız Etme, ses, parlaklık ve bağlı cihazlar.
  - Veriler yalnız ilgili sekme görünürken toplanır; çentik kapalıyken işlemci kullanımı sıfırdır.
- **Wi-Fi ve Bluetooth listeleri:** Windows hızlı ayarlarındaki gibi. Döşemenin solu aç/kapa, sağdaki ok listeyi çentiğin içinde açar.
  - **Wi-Fi:** sinyal gücü, kilit ve "Kayıtlı" işaretleriyle ağlar; bağlan ve bağlantıyı kes; şifre kutusu satırın içinde açılır. Ağ adları için Windows konum izni istiyorsa bunu söyler ve ayara götürür.
  - **Bluetooth:** eşleşmiş cihazlar türüne göre simge ve pil yüzdesiyle. Kulaklık ve hoparlörde bağlan/kes; diğer cihazlarda Windows'un Bluetooth ayarları açılır.
  - Listenin altında küçük bir "Diğer … ayarları" bağlantısı var. Bluetooth simgesi yeniden çizildi.
- **Müzik çubukları:** Ana'daki müzik kartında çalan sesten hesaplanan 24 çubuk. Yalnız kart görünür ve müzik çalarken çalışır; "kapak ve dalga" ayarına bağlıdır, pil tasarrufunda kapanır. Ses kaydedilmez, hiçbir yere gönderilmez.
- **Müzik kartını büyütme:** karta tıklayınca Ana'nın tamamını kaplar: büyük kapak, tam şarkı adı, geniş çubuklar. Tekrar tıklayınca eski boyutuna döner.
- **Cihaz konumunda şehir adı:** konum kullanılıyorsa çentikte "Konumum" yerine şehir adı yazar (OpenStreetMap). Koordinat yaklaşık 1 km'ye yuvarlanarak gönderilir; hava durumu isteği de artık yuvarlanmış koordinatla gider. Hakkında sayfasında OpenStreetMap atfı.
- **Sabitli uygulama sırası:** sıra Koy'un ayarlarında tutuluyor (`DockPinnedOrder`); ilk açılışta Windows'un sırası (Taskband `Favorites`) tohum olarak okunuyor, okunamazsa klasör sırası kullanılıyor. Simgeler dock'ta sürüklenerek sıralanıyor (yay animasyonuyla kayma), yeni sabitlenen sona ekleniyor, kaldırılan listeden düşüyor.
- **Dock'ta son kullanılanlar:** ayırıcının sağında yığın simgesi; Windows'un Son Kullanılanlar klasöründen en çok 10 dosya, yalnız tıklanınca okunur (izleme yok). Dock sayfasından kapatılır.
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
- **Dock varsayılanı "Yer ayır":** yeni kurulumlarda dock Windows görev çubuğu gibi hep görünür, pencereler onun üstünde biter. Mevcut ayar korunur. "Pencere dock'a yaklaşınca" seçicisinin altında seçili seçeneğin ne yaptığı yazıyor.
- **Müzik kapakları net:** kapak kendi çözünürlüğünde (en çok 320 px) çözülüyor ve köşeleri yuvarlak. Video kapakları (Chrome, Windows'a yalnız ~150×83 px veriyor) kırpılıp büyütülmüyor; resmin tamamı görünüyor, arkasını bulanık hâli dolduruyor.
- **Bellek bütçesi:** boşta 100–150 MB private hedef, çok ekran ya da açık panellerle 200 MB'a kadar kabul (eski hedef 60 MB idi). Performans sayfasındaki gösterge yeni hedefe göre. Bu sürümün geliştirme ölçümü: açılışta ~66 MB private.
- Yeni takılan ekranda çentik ve dock varsayılan açık; ayarlar ekran başına baştan yapılmıyor, genel ayarlar her ekrana uygulanıyor (Ekranlar sayfasından ekran seçilip kapatılır).
- Dock kenar izleme zamanlayıcısı 50 ms yerine 100 ms ve yalnız dock gizliyken çalışıyor.
- Dock ve çentik tek bir pencere izleyici ve tek bir medya servisini paylaşıyor; pencere olaylarında dock yenilemesi birleştiriliyor (daha az iş).
- Koy penceresi, sihirbaz ve sağ alt menü ortak tema ve ölçü sözlüklerinden besleniyor; sabit renk ve emoji kalmadı.
- Derleme tekrarlanabilir: yerel yollar PDB'ye gömülmüyor.
- Sürüm ve CI: release-please ile otomatik sürüm PR'ı, GitHub Actions adımları commit SHA'sına sabit, `vpk` sürümü Velopack paket sürümüne bağlı.

### Kaldırılanlar (Removed)
- Dock'taki küçültülmüş pencereler düğmesi ve ayarı (bir işe yaramıyordu).
- Çentik ve dock sağ tık menülerindeki "Çıkış". Çıkış yalnız Koy penceresindeki "Koy'dan çık" düğmesi ve sağ alt menüden yapılır.
- Hiçbir davranışa bağlı olmayan ayarlar: eski ses görselleştirici anahtarı (çubuklar bu sürümde "kapak ve dalga" ayarına bağlı olarak geldi), "müzik dalgasını durdur", "Windows'un kendi göstergesini gizle", çentik boyutu ve dock'ta pencere gruplama.
- Eski Ayarlar penceresi ve kullanılmayan kod, marka konseptleri ve sunum dosyaları.

### Güvenlik (Security)
- Depo kuralları: `main` dalına zorla push ve dal silme, yayınlanmış `v*` etiketlerinin taşınması ve silinmesi engellendi.
- README'de kod imzası olmadığı, SmartScreen uyarısında ne yapılacağı ve `SHA256SUMS.txt` ile otomatik güncellemenin neyi doğruladığı açıkça yazıldı.
- Yayımlanan ikili dosyalara geliştirici makinesinin yerel yolları artık gömülmüyor.
- GitHub Actions adımları commit SHA'sına sabitlendi (tedarik zinciri sertleştirmesi).

### Bilinen eksikler
- Kurulum dosyası ve güncellemeler henüz kod imzalı değil (SmartScreen uyarısı).
- Dock tepsisi (#11), Wi-Fi/Bluetooth bağlanma, kulaklık bağla/kes, parlaklık ve gözcü süreç her donanım çeşidinde denenmedi.
- Karışık DPI'lı çok monitör düzenlerinde doğrulama.
- Windows'un UWP sabitlemeleri (Spotify, WhatsApp gibi) dock'ta henüz gösterilmiyor.
- Hızlı başlata klasör eklenemiyor.
- Dock'ta uygulamaya özel sağ tık menüsü (atlama listesi) araştırıldı, yapılmadı.

Ayrıntı: [`docs/ROADMAP.md`](docs/ROADMAP.md).

## [0.1.0] - 2026-11-04

> **Not (sonradan eklendi):** Aşağıdaki listede bu sürümde bile eksik ya da bağlı olmayan şeyler vardı: ekran seçimi arayüzü olmadığı için çoklu ekran kullanıcıya açık değildi, OSD HUD çentiğe bağlı değildi, kısayollar ve ayarların çoğu davranışa bağlı değildi, "Aşağı yapış" seçilemiyordu, "1 saat duraklat" duraklatmıyordu, takvimde kronometre ve serbest zamanlayıcı hiç yoktu (sonradan eklendi). Düzeltmeler için [Unreleased](#unreleased) bölümüne bakın.

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
