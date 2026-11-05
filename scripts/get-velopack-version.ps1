# Velopack NuGet surumunu src/App/Koy.App.csproj'dan okuyup yazdirir.
# vpk (dotnet tool) surumu bununla ayni olmali; surum tek yerden (csproj) yonetilir.

$ErrorActionPreference = 'Stop'
$csproj = Join-Path (Split-Path $PSScriptRoot -Parent) 'src\App\Koy.App.csproj'
$match = [regex]::Match((Get-Content $csproj -Raw), '<PackageReference\s+Include="Velopack"\s+Version="([^"]+)"')
if (-not $match.Success) {
    throw "Velopack surumu $csproj icinde bulunamadi."
}
$match.Groups[1].Value
