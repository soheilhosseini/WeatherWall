@echo off
setlocal
cd /d "%~dp0.."
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist bin mkdir bin
"%CSC%" /nologo /codepage:65001 /target:exe /out:bin\SceneTests.exe /r:System.Web.Extensions.dll /r:System.Core.dll ^
  /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ^
  src\Scenes.cs src\Occasions.cs src\WeatherClient.cs src\Updater.cs src\AssemblyInfo.cs src\AutoCity.cs src\TimeZones.cs ^
  tests\SceneTests.cs tests\UpdaterTests.cs tests\LocationTests.cs
if errorlevel 1 exit /b 1
bin\SceneTests.exe
