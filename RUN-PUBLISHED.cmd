@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -File "%~dp0RUN-LOCAL.ps1" -Published
pause
