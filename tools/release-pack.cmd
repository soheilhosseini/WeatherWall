@echo off
rem Publishes a new image pack:  tools\release-pack.cmd <image folder> <pack version, e.g. 3>
rem Zips the folder, signs it, and creates a release in the separate pack repository.
setlocal
cd /d "%~dp0.."
if "%~2"=="" (echo usage: tools\release-pack.cmd ^<image folder^> ^<pack version^> & exit /b 2)
set SRC=%~1
set PV=%~2
if not exist bin\SignTool.exe call build.cmd || exit /b 1
if exist bin\pack.zip del bin\pack.zip
powershell -NoProfile -Command "Compress-Archive -Path '%SRC%\*' -DestinationPath bin\pack.zip -CompressionLevel Optimal" || exit /b 1
bin\SignTool.exe sign bin\pack.zip || exit /b 1
bin\SignTool.exe verify bin\pack.zip || exit /b 1
gh release create pack-v%PV% bin\pack.zip bin\pack.zip.sig --repo soheilhosseini/WeatherWall-Pack --title "Image pack %PV%" --notes "Image pack %PV%" || exit /b 1
echo Released pack-v%PV%
