param(
  [string] $GjallarRoot = "E:\Projects\Gjallar",
  [string] $SpecimenText = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SpecimenText)) {
  $SpecimenText = "VOID " +
    [string]([char]0x30E1) + [string]([char]0x30BF) + [string]([char]0x3081) + " " +
    [string]([char]0x3042) + [string]([char]0x30A2) + " " +
    [string]([char]0x304B) + [string]([char]0x306A) + " " +
    [string]([char]0x30AB) + [string]([char]0x30CA)
}

$projectPath = Join-Path $GjallarRoot "src\Gjallar\Gjallar.csproj"
$buildDir = Join-Path $GjallarRoot "src\Gjallar\bin\Debug\net10.0"
$exePath = Join-Path $buildDir "Gjallar.exe"
$scratchDir = Join-Path $GjallarRoot "scratch"
$framebufferPath = Join-Path $scratchDir "verify-bitmap-family.fb"
$statusPath = Join-Path $scratchDir "verify-bitmap-family.status.json"
$frameDumpPath = Join-Path $scratchDir "verify-bitmap-family.ppm"
$specimenPath = Join-Path $scratchDir "verify-bitmap-family-specimen.txt"

dotnet build $projectPath | Out-Host

if (!(Test-Path $exePath)) {
  throw "Gjallar executable not found at $exePath"
}

New-Item -ItemType Directory -Force -Path $scratchDir | Out-Null
[System.IO.File]::WriteAllText($specimenPath, $SpecimenText, [System.Text.Encoding]::UTF8)

$bufferBytes = 1920 * 1080 * 4
$file = [System.IO.File]::Open($framebufferPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::ReadWrite)
try {
  $file.SetLength($bufferBytes)
}
finally {
  $file.Dispose()
}

& $exePath `
  --fb $framebufferPath `
  --width 1920 `
  --height 1080 `
  --frames 1 `
  --mouse "" `
  --stats-path $statusPath `
  --frame-dump-path $frameDumpPath `
  --specimen-text-file $specimenPath | Out-Host

if (!(Test-Path $statusPath)) {
  throw "Gjallar did not write status to $statusPath"
}

$status = Get-Content $statusPath -Raw | ConvertFrom-Json

if ($status.status -ne "specimen-rendered") {
  throw "Expected specimen-rendered status, got '$($status.status)'"
}

$expectedFamily = @(
  @{ width = 12; height = 12 }
  @{ width = 14; height = 14 }
  @{ width = 16; height = 16 }
)

$actualFamily = @($status.fonts.loaded | ForEach-Object {
  [pscustomobject]@{
    width = [int]$_.width
    height = [int]$_.height
    kana = [bool]$_.kana
  }
})

if ($actualFamily.Count -ne $expectedFamily.Count) {
  throw "Expected $($expectedFamily.Count) loaded packaged fonts, got $($actualFamily.Count)"
}

for ($i = 0; $i -lt $expectedFamily.Count; $i++) {
  $expected = $expectedFamily[$i]
  $actual = $actualFamily[$i]
  if ($actual.width -ne $expected.width -or $actual.height -ne $expected.height) {
    throw ("Loaded font mismatch at index {0}: expected {1}x{2}, got {3}x{4}" -f $i, $expected.width, $expected.height, $actual.width, $actual.height)
  }
  if (-not $actual.kana) {
    throw "Loaded font $($actual.width)x$($actual.height) is not kana-capable"
  }
}

foreach ($entry in @($status.fonts.specimenSupport)) {
  if ($entry.missing.Count -ne 0) {
    throw "Font $($entry.width)x$($entry.height) is missing specimen glyphs: $($entry.missing -join ', ')"
  }
}

Write-Host "Gjallar bitmap family verification passed."
Write-Host "  specimenText=$SpecimenText"
Write-Host "  loaded=$((@($actualFamily) | ForEach-Object { ""$($_.width)x$($_.height)"" }) -join ', ')"
Write-Host "  statusPath=$statusPath"
Write-Host "  frameDumpPath=$frameDumpPath"
