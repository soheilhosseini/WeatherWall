@echo off
rem Publishes a new app version:  tools\release.cmd 1.0.1 "What changed"
rem Builds, tests, signs, commits the version bump, pushes, and creates the GitHub release.
rem Every installed copy picks it up within a day (or at once from "Check for updates").
setlocal
cd /d "%~dp0.."
if "%~1"=="" (echo usage: tools\release.cmd ^<version^> "notes" & exit /b 2)
set VER=%~1
set NOTES=%~2
if "%NOTES%"=="" set NOTES=WeatherWall %VER%

powershell -NoProfile -Command "(Get-Content src\AssemblyInfo.cs) -replace 'Version\(\"[0-9.]+\"\)', 'Version(\"%VER%\")' | Set-Content -Encoding UTF8 src\AssemblyInfo.cs"
call build.cmd || exit /b 1
call tests\run.cmd || exit /b 1
bin\SignTool.exe sign bin\WeatherWall.exe || exit /b 1
bin\SignTool.exe verify bin\WeatherWall.exe || exit /b 1

git add -A && git commit -m "Release v%VER%" && git push || exit /b 1
gh release create v%VER% bin\WeatherWall.exe bin\WeatherWall.exe.sig --title "WeatherWall %VER%" --notes "%NOTES%" || exit /b 1
echo Released v%VER%
