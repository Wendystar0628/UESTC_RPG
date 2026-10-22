@echo off
rem Start when the Debug assembly, published business manifest and native import cache exist; build/import only if missing.
rem Usage: double-click this file, or pass game arguments on the command line.
rem 产出：output/start_game/；output/.locks/build.lock（已有产物直接启动，工作区半成品不触发编译）。
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\build\start-game.ps1" %*
if errorlevel 1 pause
