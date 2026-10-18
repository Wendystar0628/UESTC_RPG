@echo off
rem Double-click to build, import and open the title page; the steps live in scripts\build\start_game.ps1.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\build\start_game.ps1" %*
if errorlevel 1 pause
