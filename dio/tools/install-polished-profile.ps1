param([Parameter(Mandatory = $true)][string]$ProfilePath)
$ErrorActionPreference = 'Stop'
$profileRoot = (Resolve-Path -LiteralPath $ProfilePath).Path
if (Get-Process -Name 'Risk of Rain 2' -ErrorAction SilentlyContinue) { throw 'Close Risk of Rain 2 before replacing its loaded plugins.' }
$workspaceRoot = Split-Path $PSScriptRoot -Parent
$backupRoot = Join-Path $workspaceRoot ('backups\stand-refinement-installed-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$mods = @(
    @{Name='DIO';Version='3.6.5';Source=(Join-Path $workspaceRoot 'dist\DioPrototype');Target='BepInEx\plugins\ezeus-DIO_EyesOfHeaven\DIO_EyesOfHeaven';Metadata=(Join-Path $workspaceRoot 'packaging\thunderstore');Dll='EyesOfHeavenDio.dll'},
    @{Name='Diavolo';Version='2.5.6';Source=(Join-Path $workspaceRoot '..\diavolo\dist\Diavolo');Target='BepInEx\plugins\ezeus-Diavolo\Diavolo';Metadata=(Join-Path $workspaceRoot '..\diavolo\thunderstore');Dll='Diavolo.dll'}
)
$operations = @()
foreach ($mod in $mods) {
    $targetRoot = (Resolve-Path -LiteralPath (Join-Path $profileRoot $mod.Target)).Path
    if (!$targetRoot.StartsWith($profileRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected destination outside the selected profile.' }
    $relativeFiles = @($mod.Dll) + @(Get-ChildItem -LiteralPath (Join-Path $mod.Source 'Audio') -File -Filter '*.wav' | ForEach-Object { 'Audio\' + $_.Name })
    foreach ($relative in $relativeFiles) {
        $operations += [pscustomobject]@{Mod=$mod.Name;Version=$mod.Version;Source=(Join-Path $mod.Source $relative);Target=(Join-Path $targetRoot $relative);Backup=(Join-Path (Join-Path $backupRoot $mod.Name) $relative);Before='';After=''}
    }
    foreach ($doc in @('README.md','CHANGELOG.md')) {
        $operations += [pscustomobject]@{Mod=$mod.Name;Version=$mod.Version;Source=(Join-Path $mod.Metadata $doc);Target=(Join-Path (Split-Path $targetRoot -Parent) $doc);Backup=(Join-Path (Join-Path $backupRoot $mod.Name) ('metadata-'+$doc));Before='';After=''}
    }
    $operations += [pscustomobject]@{Mod=$mod.Name;Version=$mod.Version;Source=(Join-Path $mod.Metadata 'README.md');Target=(Join-Path $targetRoot 'README.md');Backup=(Join-Path (Join-Path $backupRoot $mod.Name) 'README.md');Before='';After=''}
}
# Validate every source and back up every destination before changing the profile.
foreach ($operation in $operations) {
    if (!(Test-Path -LiteralPath $operation.Source -PathType Leaf)) { throw ('Missing source: '+$operation.Source) }
    $operation.After = (Get-FileHash -LiteralPath $operation.Source -Algorithm SHA256).Hash
    if (Test-Path -LiteralPath $operation.Target -PathType Leaf) {
        $operation.Before = (Get-FileHash -LiteralPath $operation.Target -Algorithm SHA256).Hash
        New-Item -ItemType Directory -Path (Split-Path $operation.Backup -Parent) -Force | Out-Null
        if (!(Test-Path -LiteralPath $operation.Backup)) { Copy-Item -LiteralPath $operation.Target -Destination $operation.Backup }
    }
}
foreach ($operation in $operations) {
    New-Item -ItemType Directory -Path (Split-Path $operation.Target -Parent) -Force | Out-Null
    if ($operation.Before -ne $operation.After) { Copy-Item -LiteralPath $operation.Source -Destination $operation.Target -Force }
    if ((Get-FileHash -LiteralPath $operation.Target -Algorithm SHA256).Hash -ne $operation.After) { throw ('Installed hash mismatch: '+$operation.Target) }
}
$report = [pscustomobject]@{Profile=$profileRoot;Backup=$backupRoot;Files=$operations.Count;Installed=$operations;LiveGameplayVerified=$false}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $workspaceRoot 'docs\stand-refinement-install.json') -Encoding utf8
Write-Host ('Installed and SHA-256 verified '+$operations.Count+' files: DIO 3.6.5, Diavolo 2.5.6. Backup: '+$backupRoot)
