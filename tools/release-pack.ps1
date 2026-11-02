# Publishes a new image pack:  tools\release-pack.cmd <image folder> <pack version, e.g. 3>
# Zips the folder, signs it, and creates a release in the separate pack repository.
param(
    [Parameter(Mandatory = $true)][string]$Folder,
    [Parameter(Mandatory = $true)][string]$PackVersion
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot

function Run([string]$what, [scriptblock]$cmd) {
    & $cmd
    if ($LASTEXITCODE) { throw "$what failed (exit $LASTEXITCODE)" }
}

if (-not (Test-Path "$root\bin\SignTool.exe")) { Run 'build' { cmd /c "`"$root\build.cmd`"" } }
$zip = "$root\bin\pack.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $Folder '*') -DestinationPath $zip -CompressionLevel Optimal
Run 'sign' { & "$root\bin\SignTool.exe" sign $zip }
Run 'verify' { & "$root\bin\SignTool.exe" verify $zip }
Run 'release' { gh release create "pack-v$PackVersion" $zip "$zip.sig" --repo soheilhosseini/WeatherWall-Pack --title "Image pack $PackVersion" --notes "Image pack $PackVersion" }
Write-Host "Released pack-v$PackVersion"
