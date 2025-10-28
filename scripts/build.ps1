Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptsDir = $PSScriptRoot
$RepoRoot   = Split-Path -Parent $ScriptsDir

# Llama al instalador dentro de scripts/, NO en la raíz
& (Join-Path $ScriptsDir 'install-dotnet.ps1')

# Asegura variables en esta sesión
$env:DOTNET_ROOT = Join-Path $RepoRoot '.dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"

dotnet --info
dotnet restore (Join-Path $RepoRoot 'HerramientasV2.sln')
dotnet build   (Join-Path $RepoRoot 'HerramientasV2.sln') -c Release
