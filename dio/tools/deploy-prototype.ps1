$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$source = Join-Path $projectRoot 'dist\DioPrototype'
$target = Join-Path $projectRoot 'test-profile\BepInEx\plugins\DioPrototype'
$required = @('EyesOfHeavenDio.dll', 'eohdio', 'diofeedback', 'diospecial', 'Audio\time-stop.wav', 'Audio\time-resume.wav', 'Audio\impact.wav', 'Audio\impact-heavy.wav', 'Audio\swing.wav', 'Audio\stand-swing.wav', 'Audio\blood-drain.wav', 'Audio\knife-impact.wav', 'Audio\road-fall.wav', 'Audio\road-crack.wav', 'Audio\road-explosion.wav', 'Audio\barrage.wav', 'Audio\time-stop-main.wav')
$required += @('Audio\voice-muda-01.wav', 'Audio\voice-muda-02.wav', 'Audio\voice-wryy.wav', 'Audio\death-voice.wav', 'Audio\ex-stand-power.wav', 'Audio\time-stop-r.wav', 'Audio\shift-punch-voice.wav', 'Audio\blood-drain-voice.wav', 'Audio\road-roller-voice.wav', 'Audio\stand-summon.wav', 'Audio\stand-block.wav')
$required += @(Get-ChildItem -LiteralPath (Join-Path $source 'Audio') -Filter 'za-warudo-voice-*.wav' | ForEach-Object { 'Audio\' + $_.Name })
foreach ($relative in $required) {
    if (!(Test-Path -LiteralPath (Join-Path $source $relative))) { throw "Build is incomplete: $relative" }
}
foreach ($relative in $required) {
    $inputFile = Join-Path $source $relative
    $outputFile = Join-Path $target $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $outputFile -Parent) | Out-Null
    Copy-Item -LiteralPath $inputFile -Destination $outputFile -Force
    if ((Get-FileHash -LiteralPath $inputFile).Hash -ne (Get-FileHash -LiteralPath $outputFile).Hash) { throw "Deployment hash mismatch: $relative" }
}
Write-Host 'Complete DIO build deployed; all required files verified.'
