# Requires PowerShell 5+ / 7+
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptsDir = $PSScriptRoot                      # ...\HerramientasV2\scripts
$RepoRoot   = Split-Path -Parent $ScriptsDir     # ...\HerramientasV2
$DotnetDir  = Join-Path $RepoRoot '.dotnet'
New-Item -ItemType Directory -Path $DotnetDir -Force | Out-Null

# Descarga el instalador oficial si no existe
$Installer = Join-Path $DotnetDir 'dotnet-install.ps1'
if (-not (Test-Path $Installer)) {
  Invoke-WebRequest -UseBasicParsing -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $Installer
}

# Ajusta el canal a lo que uses (7.0 según tu requerimiento)
& $Installer -InstallDir $DotnetDir -Channel '7.0'

# Exporta para esta sesión
$env:DOTNET_ROOT = $DotnetDir
$env:PATH = "$DotnetDir;$env:PATH"

# (Opcional) Verificación rápida
& (Join-Path $DotnetDir 'dotnet.exe') --info
