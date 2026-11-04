## Ne Değişti? (Summary)
<!-- Bu PR'ın çözdüğü sorunu veya eklediği özelliği kısaca açıklayın. -->

## İlgili Kayıt (Related Issue)
Closes #

## Mimari ve Performans Kontrol Listesi (Checklist)
Lütfen PR göndermeden önce aşağıdaki kuralları (`AGENTS.md`) doğrulayın:

- [ ] **Katman Sınırları:** `Core` katmanına hiçbir WPF, Win32 veya WinRT tipi eklenmedi (`App -> Platform -> Core`).
- [ ] **CsWin32:** Yeni Win32 API çağrıları elle `[DllImport]` yazılmadan `src/Platform/NativeMethods.txt` üzerinden üretildi.
- [ ] **Sıfır Boşta CPU (Zero Idle CPU):** Animasyonlar `Core/Animation/Spring` kullanıyor; hareket bitince `CompositionTarget.Rendering` aboneliği çözülüyor. Görünmeyen panellerde zamanlayıcı çalışmıyor.
- [ ] **Görev Çubuğu Güvenliği:** Dock değişikliği varsa uygulama zorla sonlandırılıp yeniden açıldığında Windows görev çubuğu sorunsuz geri geliyor.
- [ ] **Yerelleştirme & Vektör İkonlar:** Kullanıcıya görünen metinler hem `Strings.resx` (EN) hem `Strings.tr.resx` (TR) dosyalarına eklendi. Düz Unicode emoji kullanılmadı (yalnızca SVG `Path`).
- [ ] **Derleme ve Testler:** `dotnet build Koy.slnx -c Release -warnaserror` (0 uyarı, 0 hata) ve `dotnet test Koy.slnx` tamamen yeşil.
- [ ] **Bellek Ölçümü:** `scripts/measure-memory.ps1` ile bellek etkisi ölçüldü (`Private Bytes` not edildi).
