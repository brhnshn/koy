# Koy paketleme ve kurulum betiği (Velopack)
# Kullanım: powershell -ExecutionPolicy Bypass -File scripts\pack.ps1 [-Version 0.1.0]

param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

# Native komutlar $ErrorActionPreference'a takilmaz; cikis kodunu elle denetle.
function Assert-LastExit([string]$what) {
    if ($LASTEXITCODE -ne 0) {
        throw "$what basarisiz oldu (cikis kodu $LASTEXITCODE)."
    }
}

Write-Host "==> Koy v$Version derleniyor ve yayin klasorune cikariliyor..." -ForegroundColor Cyan

$publishDir = Join-Path $root 'publish\win-x64'
$distDir = Join-Path $root 'dist'

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

dotnet publish src/App/Koy.App.csproj -c Release -r win-x64 --self-contained false -o $publishDir
Assert-LastExit 'dotnet publish'

Write-Host "==> Velopack paketi (vpk pack) olusturuluyor..." -ForegroundColor Cyan

# vpk aracini kontrol et, yoksa yukle
if (-not (Get-Command 'vpk' -ErrorAction SilentlyContinue)) {
    Write-Host "vpk yukleniyor..." -ForegroundColor Yellow
    dotnet tool install -g vpk | Out-Null
    Assert-LastExit 'vpk kurulumu'
}

vpk pack --packId Koy `
         --packVersion $Version `
         --packDir $publishDir `
         --mainExe Koy.exe `
         --packTitle "Koy" `
         --packAuthors "Burhan Şahin" `
         --icon (Join-Path $root "src\App\Resources\app.ico") `
         --runtime win-x64 `
         --outputDir $distDir
Assert-LastExit 'vpk pack'

Write-Host "==> Portable zip paketi olusturuluyor..." -ForegroundColor Cyan
Compress-Archive -Path "$publishDir\*" -DestinationPath (Join-Path $distDir "Koy-win-Portable.zip") -Force

Write-Host "==> SHA256 karmalari hesaplaniyor..." -ForegroundColor Cyan
$sumsFile = Join-Path $distDir "SHA256SUMS.txt"
if (Test-Path $sumsFile) { Remove-Item $sumsFile -Force }

Get-ChildItem -Path $distDir -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" } | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($_.Name)" | Out-File -FilePath $sumsFile -Append -Encoding utf8
}

Write-Host "==> Paketleme tamamlandi. Ciktilar: $distDir" -ForegroundColor Green
Get-Content $sumsFile
