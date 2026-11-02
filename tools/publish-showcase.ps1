# Puts the README images (GIF, scene grid, social preview) into the pack repository, so the app
# repository itself holds no character art:  tools\publish-showcase.cmd <folder with the images>
param([Parameter(Mandatory = $true)][string]$Folder)
$ErrorActionPreference = 'Stop'
$repo = 'soheilhosseini/WeatherWall-Pack'
$work = Join-Path $env:TEMP ('ww-showcase-' + [Guid]::NewGuid().ToString('N'))

function Run([string]$what, [scriptblock]$cmd) {
    & $cmd
    if ($LASTEXITCODE) { throw "$what failed (exit $LASTEXITCODE)" }
}

Run 'clone' { gh repo clone $repo $work -- --quiet }
New-Item -ItemType Directory -Force (Join-Path $work 'showcase') | Out-Null
Copy-Item (Join-Path $Folder '*') (Join-Path $work 'showcase') -Force
Push-Location $work
try {
    git add -A
    git diff --cached --quiet
    if ($LASTEXITCODE) {
        Run 'commit' { git commit -m "Update showcase images" }
        Run 'push' { git push }
    }
}
finally { Pop-Location }
Remove-Item $work -Recurse -Force
Write-Host "Showcase images published to $repo/showcase"
