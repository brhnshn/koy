# Koy

Windows 11 için hafif bir notch (üst kenar koyu/adası) ve görev çubuğunun yerine geçen bir dock.
Hedef: tek monitörde boşta 60 MB'ın altında bellek.

> Geliştirme aşamasında. Henüz kullanılabilir bir sürüm yok.

## Derleme

Gerekenler: Windows 11, [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build Koy.slnx
dotnet test Koy.slnx
dotnet run --project src/App
```

Notch'a sağ tıklayıp **Çıkış** ile kapatılır.

## Belgeler
- [AGENTS.md](AGENTS.md): Geliştirme kuralları ve komutlar
- [docs/PLAN.md](docs/PLAN.md): Hedefler, mimari, teknoloji kararları
- [docs/ROADMAP.md](docs/ROADMAP.md): Aşama aşama yapılacaklar
- [docs/MEMORY.md](docs/MEMORY.md): Bellek ölçümleri
- [docs/NAMING.md](docs/NAMING.md): İsim önerileri

## Lisans

[GPLv3](LICENSE)
