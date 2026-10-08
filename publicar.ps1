# Arma el ZIP portable de Better Look Windows, listo para publicar.
# Uso: clic derecho > "Ejecutar con PowerShell"   (o:  .\publicar.ps1  desde una terminal en esta carpeta)
# La ventana se queda abierta al terminar, para que puedas leer el resultado o el error.
Set-Location $PSScriptRoot

try {
    $ErrorActionPreference = 'Stop'

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'No se encontró "dotnet". Instala el SDK de .NET 10 y vuelve a intentarlo.'
    }

    $csproj = Join-Path $PSScriptRoot 'BetterLookWin\BetterLookWin.csproj'
    if (-not (Test-Path $csproj)) { throw "No encontré $csproj. Ejecuta el script desde la carpeta que tiene BetterLookWin.sln." }

    $match = Select-String -Path $csproj -Pattern '<Version>\s*([^<\s]+)\s*</Version>' | Select-Object -First 1
    if (-not $match) { throw 'No encontré <Version> en BetterLookWin.csproj' }
    $version = $match.Matches[0].Groups[1].Value

    $name    = "BetterLookWin-$version-win-x64"
    $publish = Join-Path $PSScriptRoot "publish\$name"
    $release = Join-Path $PSScriptRoot 'release'
    $zip     = Join-Path $release "$name.zip"

    Write-Host "Publicando versión $version ..." -ForegroundColor Cyan
    if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }

    # Con .NET incluido (self-contained): quien la use no necesita instalar nada.
    dotnet publish $csproj -c Release -r win-x64 --self-contained true -o $publish
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló (código $LASTEXITCODE). Mira los mensajes de arriba." }

    $readme = Join-Path $PSScriptRoot 'LEEME-portable.txt'
    if (Test-Path $readme) { Copy-Item $readme (Join-Path $publish 'LEEME.txt') }

    New-Item $release -ItemType Directory -Force | Out-Null
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $publish -DestinationPath $zip -CompressionLevel Optimal

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
    "$hash  $name.zip" | Set-Content (Join-Path $release "$name.sha256.txt") -Encoding ascii

    $mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
    Write-Host ""
    Write-Host "Listo: $zip  ($mb MB)" -ForegroundColor Green
    Write-Host "SHA256: $hash"
    Write-Host "Pega esa huella en la descripción de la descarga (GitHub u otro sitio)."
}
catch {
    Write-Host ""
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Pulsa Enter para cerrar"
