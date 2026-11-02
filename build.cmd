@echo off
rem Builds WeatherWall.exe with the C# compiler that ships with Windows (.NET Framework 4.x).
rem No SDK or Visual Studio needed.
setlocal
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist bin mkdir bin
"%CSC%" /nologo /codepage:65001 /target:winexe /optimize+ /out:bin\WeatherWall.exe /win32icon:assets\app.ico ^
  /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /r:System.Core.dll ^
  /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ^
  src\*.cs
if errorlevel 1 exit /b 1
"%CSC%" /nologo /codepage:65001 /target:exe /out:bin\SignTool.exe tools\SignTool.cs
if errorlevel 1 exit /b 1
echo Built bin\WeatherWall.exe and bin\SignTool.exe
