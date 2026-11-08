# Despliega la API de BussinesMS al VPS de produccion (Ubuntu 22.04).
#
# Uso (desde la carpeta BackEnd/):   .\deploy.ps1 -Servidor administrator@IP -Puerto N
#   -Servidor y -Puerto son obligatorios (no hay valores por defecto: el VPS todavia no esta fijado en el codigo).
#
# Que hace:
#   1. dotnet publish (Release) en una carpeta temporal de esta PC.
#   2. Lo empaqueta en un .tgz y lo sube por scp.
#   3. En el servidor: lo descomprime en /opt/bussinesms-api/releases/<fecha-hora>, apunta el
#      enlace /opt/bussinesms-api/current a esa version, reinicia el servicio systemd
#      bussinesms-api y borra las versiones viejas (se guardan las ultimas $VersionesAGuardar).
#
# NO aplica migraciones: si el cambio trae migraciones nuevas, generar el script idempotente de cada contexto
# (dotnet ef migrations script --idempotent --context AuthDbContext|SistemaDbContext|NavidadDbContext
#  -p src/BussinesMS.Infraestructura -s src/BussinesMS.API) y correrlo en el servidor con sqlcmd ANTES de este deploy.
#
# Volver a la version anterior (en el servidor):
#   ls -1t /opt/bussinesms-api/releases
#   ln -sfn /opt/bussinesms-api/releases/<version> /opt/bussinesms-api/current
#   sudo systemctl restart bussinesms-api
#
# Requiere: llave SSH ya autorizada en el servidor, y tar.exe/ssh.exe/scp.exe (vienen con Windows).

param(
    [Parameter(Mandatory = $true)][string]$Servidor,
    [Parameter(Mandatory = $true)][int]$Puerto,
    [int]$VersionesAGuardar = 5
)

$ErrorActionPreference = "Stop"

$raiz = $PSScriptRoot
$version = Get-Date -Format "yyyyMMdd-HHmmss"
$base = "/opt/bussinesms-api"
$destino = "$base/releases/$version"
$salida = Join-Path $env:TEMP "bussinesms-api-publish"
$paquete = Join-Path $env:TEMP "bussinesms-api-$version.tgz"
$paqueteRemoto = "/tmp/bussinesms-api-$version.tgz"

# Avisar si hay cambios sin commitear: lo que se sube es lo que hay en disco, no lo del ultimo commit.
$cambios = git -C $raiz status --porcelain
if ($cambios) {
    Write-Warning "Hay cambios sin commitear en BackEnd/. Se va a subir lo que hay en disco."
    $respuesta = Read-Host "Continuar igual? (s/n)"
    if ($respuesta -ne "s") { exit 1 }
}

Write-Host "==> 1/3 Compilando (Release)..." -ForegroundColor Cyan
if (Test-Path $salida) { Remove-Item -Recurse -Force $salida }
dotnet publish (Join-Path $raiz "src/BussinesMS.API") -c Release -o $salida
if ($LASTEXITCODE -ne 0) { throw "Fallo dotnet publish." }

# appsettings.Development.json tiene claves de desarrollo: no se sube al servidor.
$appsettingsDev = Join-Path $salida "appsettings.Development.json"
if (Test-Path $appsettingsDev) { Remove-Item -Force $appsettingsDev }

Write-Host "==> 2/3 Subiendo version $version..." -ForegroundColor Cyan
tar -czf $paquete -C $salida .
if ($LASTEXITCODE -ne 0) { throw "Fallo al empaquetar con tar." }
scp -P $Puerto $paquete "${Servidor}:$paqueteRemoto"
if ($LASTEXITCODE -ne 0) { throw "Fallo la subida por scp." }
Remove-Item -Force $paquete

Write-Host "==> 3/3 Activando la version y reiniciando el servicio (puede pedir la contrasena de sudo)..." -ForegroundColor Cyan
$remoto = "set -e; " +
    "mkdir -p $destino; " +
    "tar -xzf $paqueteRemoto -C $destino; " +
    "rm -f $paqueteRemoto; " +
    "ln -sfn $destino $base/current; " +
    "sudo systemctl restart bussinesms-api; " +
    "sleep 3; " +
    "systemctl is-active bussinesms-api; " +
    "ls -1dt $base/releases/* | tail -n +$($VersionesAGuardar + 1) | xargs -r rm -rf"
ssh -t -p $Puerto $Servidor $remoto
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "El servicio no quedo activo. Ver el error en el servidor con:" -ForegroundColor Red
    Write-Host "  sudo journalctl -u bussinesms-api -n 50 --no-pager"
    Write-Host "y volver a la version anterior (ver el encabezado de este script)."
    exit 1
}

Write-Host ""
Write-Host "Listo: version $version activa en $base/current" -ForegroundColor Green
