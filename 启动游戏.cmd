@echo off
rem Check existing build/import, update when needed, then open the game.
rem Usage: double-click this file, or pass game arguments on the command line.
rem 产出：output/start_game/；output/.locks/build.lock（委托启动脚本按需构建与导入）。
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\build\start-game.ps1" %*
if errorlevel 1 pause
