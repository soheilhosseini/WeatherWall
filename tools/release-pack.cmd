@echo off
rem Wrapper: tools\release-pack.cmd <image folder> <pack version>  (the work is in release-pack.ps1)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0release-pack.ps1" %*
