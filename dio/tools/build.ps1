param(
    [Parameter(Mandatory = $true)][string]$GamePath,
    [Parameter(Mandatory = $true)][string]$BepInExCoreDir,
    [Parameter(Mandatory = $true)][string]$R2ApiNetworkingPath,
    [string]$DotnetPath = 'dotnet',
    [switch]$CompileOnly
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$GamePath = (Resolve-Path -LiteralPath $GamePath).Path
$BepInExCoreDir = (Resolve-Path -LiteralPath $BepInExCoreDir).Path
$R2ApiNetworkingPath = (Resolve-Path -LiteralPath $R2ApiNetworkingPath).Path
& $DotnetPath build (Join-Path $projectRoot 'src\DioMod') -c Release --nologo `
    "-p:GameDir=$GamePath" "-p:BepInExCoreDir=$BepInExCoreDir" "-p:R2ApiNetworkingPath=$R2ApiNetworkingPath"
if ($LASTEXITCODE -ne 0) { throw 'Mod build failed.' }
$destination = Join-Path $projectRoot 'dist\DioPrototype'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\DioMod\bin\Release\netstandard2.1\EyesOfHeavenDio.dll') -Destination $destination -Force
if ($CompileOnly) { Write-Host "Compiled $destination\EyesOfHeavenDio.dll"; return }
foreach ($name in @('eohdio', 'diofeedback', 'diospecial')) {
    $bundle = Join-Path $projectRoot "dist\Assets\$name"
    if (!(Test-Path -LiteralPath $bundle -PathType Leaf)) {
        throw "Missing $name asset bundle. See docs/BUILDING.md; commercial and supplied assets are not included in this source checkout."
    }
    Copy-Item -LiteralPath $bundle -Destination $destination -Force
}
$audioSource = Join-Path $projectRoot 'assets\audio-polished'
if (!(Test-Path -LiteralPath $audioSource)) { throw 'Processed local audio is missing. See docs/BUILDING.md.' }
$audioDestination = Join-Path $destination 'Audio'
New-Item -ItemType Directory -Force -Path $audioDestination | Out-Null
Copy-Item -Path (Join-Path $audioSource '*.wav') -Destination $audioDestination -Force
