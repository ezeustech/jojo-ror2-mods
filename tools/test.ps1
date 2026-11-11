param([string]$DotnetPath = 'dotnet')
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
foreach ($test in @('TestDioSpecial', 'TestDioNetwork', 'TestModAudio', 'TestStandPrimary')) {
    $project = Join-Path $repositoryRoot "dio\tools\$test\$test.csproj"
    & $DotnetPath run --project $project --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "$test failed." }
}
foreach ($test in @('CombatRules', 'ComboSequence', 'GamepadChord')) {
    $project = Join-Path $repositoryRoot "diavolo\tests\$test\$test.csproj"
    & $DotnetPath run --project $project --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "$test failed." }
}
