# Publishes a new app version:  tools\release.cmd 1.0.1 "What changed"
# Builds, tests, signs, commits the version bump, pushes, and creates the GitHub release.
# Every installed copy picks it up within a day (or at once from "Check for updates").
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Notes = "",
    [switch]$DryRun   # build, test and sign only; no commit, push or release
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
Set-Location $root
if (-not $Notes) { $Notes = "WeatherWall $Version" }

function Run([string]$what, [scriptblock]$cmd) {
    & $cmd
    if ($LASTEXITCODE) { throw "$what failed (exit $LASTEXITCODE)" }
}

$info = Join-Path $root 'src\AssemblyInfo.cs'
$text = [IO.File]::ReadAllText($info) -replace 'Version\("[0-9.]+"\)', "Version(""$Version"")"
[IO.File]::WriteAllText($info, $text)

Run 'build' { cmd /c "`"$root\build.cmd`"" }
Run 'tests' { cmd /c "`"$root\tests\run.cmd`"" }
Run 'sign' { & "$root\bin\SignTool.exe" sign "$root\bin\WeatherWall.exe" }
Run 'verify' { & "$root\bin\SignTool.exe" verify "$root\bin\WeatherWall.exe" }
if ($DryRun) { Write-Host "Dry run: v$Version built, tested and signed; nothing published."; return }

git add -A
git diff --cached --quiet
if ($LASTEXITCODE) { Run 'commit' { git commit -m "Release v$Version" } }
Run 'push' { git push -u origin HEAD }
Run 'release' { gh release create "v$Version" "$root\bin\WeatherWall.exe" "$root\bin\WeatherWall.exe.sig" --title "WeatherWall $Version" --notes $Notes }
Write-Host "Released v$Version"
