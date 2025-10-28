Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptsDir = $PSScriptRoot
$RepoRoot   = Split-Path -Parent $ScriptsDir

& (Join-Path $ScriptsDir 'install-dotnet.ps1')

$env:DOTNET_ROOT = Join-Path $RepoRoot '.dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"

dotnet test (Join-Path $RepoRoot 'HerramientasV2.sln') -c Release --no-build
