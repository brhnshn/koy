# Koy paketleme ve kurulum betiği (Velopack)
# Kullanım: powershell -ExecutionPolicy Bypass -File scripts\pack.ps1 [-Version 0.1.0]

param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

Write-Host "==> Koy v$Version derleniyor ve yayin klasorune cikariliyor..." -ForegroundColor Cyan

$publishDir = Join-Path $root 'publish\win-x64'
$distDir = Join-Path $root 'dist'

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir -Force | Out-Null }

dotnet publish src/App/Koy.App.csproj -c Release -r win-x64 --self-contained false -o $publishDir

Write-Host "==> Velopack paketi (vpk pack) olusturuluyor..." -ForegroundColor Cyan

# vpk aracini kontrol et, yoksa yukle
if (-not (Get-Command 'vpk' -ErrorAction SilentlyContinue)) {
    Write-Host "vpk yukleniyor..." -ForegroundColor Yellow
    dotnet tool install -g vpk | Out-Null
}

vpk pack --packId Koy `
         --packVersion $Version `
         --packDir $publishDir `
         --mainExe Koy.exe `
         --packTitle "Koy" `
         --packAuthors "Koy" `
         --outputDir $distDir

Write-Host "==> SHA256 karmalari hesaplaniyor..." -ForegroundColor Cyan
$sumsFile = Join-Path $distDir "SHA256SUMS.txt"
if (Test-Path $sumsFile) { Remove-Item $sumsFile -Force }

Get-ChildItem -Path $distDir -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" } | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($_.Name)" | Out-File -FilePath $sumsFile -Append -Encoding utf8
}

Write-Host "==> Paketleme tamamlandi. Ciktilar: $distDir" -ForegroundColor Green
Get-Content $sumsFile
