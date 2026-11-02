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
$repo = 'soheilhosseini/WeatherWall-Pack'

# GitHub cannot release from an empty repository: give it a README first.
# (cmd /c keeps gh's stderr away from PowerShell, which would turn it into a terminating error)
cmd /c "gh api repos/$repo/commits?per_page=1 >nul 2>&1"
if ($LASTEXITCODE) {
    $readme = "# WeatherWall-Pack`n`nImage pack for [WeatherWall](https://github.com/soheilhosseini/WeatherWall). " +
        "Installed copies download the latest release automatically.`n"
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($readme))
    Run 'readme' { gh api -X PUT "repos/$repo/contents/README.md" -f message="Add README" -f content=$b64 | Out-Null }
}

# A failed earlier attempt can leave a draft with the same name behind.
cmd /c "gh release delete pack-v$PackVersion --repo $repo --yes >nul 2>&1"

Run 'release' { gh release create "pack-v$PackVersion" $zip "$zip.sig" --repo $repo --title "Image pack $PackVersion" --notes "Image pack $PackVersion" }
Write-Host "Released pack-v$PackVersion"
