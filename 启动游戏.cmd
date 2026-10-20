@echo off
rem Check existing build/import, update when needed, then open the game.
rem Usage: double-click this file, or pass game arguments on the command line.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\build\start-game.ps1" %*
if errorlevel 1 pause
