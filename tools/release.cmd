@echo off
rem Wrapper: tools\release.cmd <version> "notes"  (the work is in release.ps1)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0release.ps1" %*
