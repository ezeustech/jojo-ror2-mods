param([Parameter(Mandatory = $true)][string]$GamePath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$game = (Resolve-Path -LiteralPath $GamePath).Path
$preloader = Join-Path $projectRoot 'test-profile\BepInEx\core\BepInEx.Preloader.dll'
if (!(Test-Path -LiteralPath $preloader)) { throw 'Workspace test profile is missing.' }
if (Get-Process -Name 'Risk of Rain 2' -ErrorAction SilentlyContinue) { throw 'Close Risk of Rain 2 before launching the prototype profile.' }
& (Join-Path $PSScriptRoot 'deploy-prototype.ps1')
$arguments = '--doorstop-enable true --doorstop-target-assembly "' + $preloader + '"'
# User-run launcher intentionally opens the game for interactive testing.
Start-Process -FilePath (Join-Path $game 'Risk of Rain 2.exe') -WorkingDirectory $game -ArgumentList $arguments
