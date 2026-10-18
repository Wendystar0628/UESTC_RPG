@echo off
rem 双击编译并导入工程，再打开开始页；具体步骤在 scripts\build\start_game.ps1。
rem 用法：双击；命令行写 启动游戏.cmd [-NoBuild] [游戏参数]，-NoBuild 跳过编译与导入，游戏参数原样传给游戏
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\build\start_game.ps1" %*
if errorlevel 1 pause
