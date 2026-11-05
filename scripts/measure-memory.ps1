# Uygulamayı Release olarak derler, açar, boşta bekler ve belleği ölçer.
# Kullanım: powershell -ExecutionPolicy Bypass -File scripts\measure-memory.ps1 [-Seconds 10] [-Gpu]
param(
    [int]$Seconds = 10,
    [switch]$Gpu
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

# Derleme hata verirse eski bir exe'yi olcmemek icin dur ve ciktiyi goster.
$buildOutput = dotnet build src/App/Koy.App.csproj -c Release
if ($LASTEXITCODE -ne 0) {
    $buildOutput | Write-Host
    throw "dotnet build basarisiz oldu (cikis kodu $LASTEXITCODE)."
}
$exe = Join-Path $root 'src\App\bin\Release\net10.0-windows10.0.22621.0\Koy.exe'

$env:KOY_GPU = if ($Gpu) { '1' } else { '0' }
$p = Start-Process $exe -PassThru
try {
    Start-Sleep -Seconds $Seconds
    $p.Refresh()
    if ($p.HasExited) {
        throw "Uygulama olcumden once kapandi (cikis kodu $($p.ExitCode))."
    }
    '{0:yyyy-MM-dd HH:mm}  private: {1:N1} MB  working set: {2:N1} MB  gpu: {3}' -f `
        (Get-Date), ($p.PrivateMemorySize64 / 1MB), ($p.WorkingSet64 / 1MB), [bool]$Gpu
}
finally {
    if (-not $p.HasExited) { Stop-Process $p -Force }
}
