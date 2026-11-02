@echo off
rem Wrapper: tools\publish-showcase.cmd <folder>  (the work is in publish-showcase.ps1)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-showcase.ps1" %*
