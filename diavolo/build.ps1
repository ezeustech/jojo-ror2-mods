param(
    [Parameter(Mandatory = $true)][string]$GamePath,
    [Parameter(Mandatory = $true)][string]$ProfilePath,
    [switch]$CompileOnly
)
$ErrorActionPreference = 'Stop'
$managed = Join-Path $GamePath 'Risk of Rain 2_Data\Managed'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $PSScriptRoot 'dist\Diavolo'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$names = @('RoR2','UnityEngine','UnityEngine.CoreModule','UnityEngine.TerrainModule','UnityEngine.TextRenderingModule','UnityEngine.PhysicsModule','UnityEngine.AnimationModule','UnityEngine.Networking','UnityEngine.InputLegacyModule','UnityEngine.IMGUIModule','UnityEngine.UI','UnityEngine.UIModule','Unity.Postprocessing.Runtime','UnityEngine.AssetBundleModule','UnityEngine.ParticleSystemModule','UnityEngine.ImageConversionModule','com.unity.multiplayer-hlapi.Runtime','UnityEngine.UNETModule','HGUnityUtils','HGCSharpUtils','LegacyResourcesAPI','KinematicCharacterController','Unity.Addressables','Unity.ResourceManager','netstandard')
$refs = @()
foreach ($name in $names) {
    $path = Join-Path $managed ($name + '.dll')
    if (Test-Path -LiteralPath $path) { $refs += '/reference:' + $path }
}
$refs += '/reference:' + (Join-Path $ProfilePath 'BepInEx\core\BepInEx.dll')
$refs += '/reference:' + (Join-Path $ProfilePath 'BepInEx\core\0Harmony.dll')
foreach ($name in @('R2API.Core.dll','R2API.ContentManagement.dll','R2API.Prefab.dll','R2API.Language.dll','R2API.Networking.dll','RiskOfOptions.dll')) {
    $dll = Get-ChildItem (Join-Path $ProfilePath 'BepInEx\plugins') -Recurse -Filter $name | Select-Object -First 1
    if (!$dll) { throw "Missing dependency $name in $ProfilePath" }
    $refs += '/reference:' + $dll.FullName
}
$sources = Get-ChildItem (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName
& $compiler /nologo /target:library /optimize+ ('/out:' + (Join-Path $output 'Diavolo.dll')) $refs $sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
if ($CompileOnly) { Write-Host "Compiled $output\Diavolo.dll"; return }
Copy-Item (Join-Path $PSScriptRoot 'README.md') $output -Force
$bundle = Join-Path $PSScriptRoot 'dist\Assets\diavoloassets'
if (!(Test-Path -LiteralPath $bundle)) { throw 'Missing asset bundle. Run BuildDiavolo.Build in Unity first.' }
Copy-Item -LiteralPath $bundle -Destination (Join-Path $output 'diavoloassets') -Force
$audioOutput = Join-Path $output 'Audio'
New-Item -ItemType Directory -Force -Path $audioOutput | Out-Null
$audioSource = Join-Path $PSScriptRoot 'assets\audio-polished'
if (!(Test-Path -LiteralPath $audioSource)) { throw 'Run ../dio/tools/polish-audio.py before building the polished release.' }
Copy-Item (Join-Path $audioSource '*.wav') $audioOutput -Force
Copy-Item (Join-Path $PSScriptRoot 'docs\VALIDATION.md') $output -Force
$docsOutput = Join-Path $output 'docs'
New-Item -ItemType Directory -Force -Path $docsOutput | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'docs\CHANGELOG-2.5*.md') $docsOutput -Force
Copy-Item (Join-Path $PSScriptRoot 'docs\CONTROLLER-2.5.3.md') $docsOutput -Force
Write-Host "Built $output\Diavolo.dll"


$overlayOutput = Join-Path $output 'TimeOverlay'
New-Item -ItemType Directory -Force -Path $overlayOutput | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'assets\time-overlay\*') $overlayOutput -Force
