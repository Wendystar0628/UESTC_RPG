@echo off
setlocal EnableExtensions DisableDelayedExpansion
chcp 65001 >nul
set "GRAPHICS_DIR=%APPDATA%\UESTC"
set "STAMP=%DATE%_%TIME%"
set "STAMP=%STAMP:/=-%"
set "STAMP=%STAMP:\=-%"
set "STAMP=%STAMP::=-%"
set "STAMP=%STAMP:.=-%"
set "STAMP=%STAMP: =0%"
set "CHANGED=0"
set "FAILED=0"
call :Archive engine_override.cfg
call :Archive graphics.cfg
if "%FAILED%"=="1" goto Failed
if "%CHANGED%"=="0" goto Default
echo 画面设置已恢复；原设置已改名保留，下次启动将重新推荐画质。
echo Graphics settings reset. Previous files were preserved. Restart the game.
goto Done
:Default
echo 画面设置已是默认值。
echo Graphics settings are already at their defaults.
goto Done
:Failed
echo 部分画面设置未能改名；请关闭游戏后重试，原文件已保留。
echo Some files could not be renamed. Close the game and retry. Originals remain.
:Done
echo.
echo 按任意键关闭。
echo Press any key to close.
pause >nul
exit /b %FAILED%
:Archive
if not exist "%GRAPHICS_DIR%\%~1" exit /b 0
if exist "%GRAPHICS_DIR%\%~1.reset-%STAMP%" (
    set "FAILED=1"
    exit /b 1
)
ren "%GRAPHICS_DIR%\%~1" "%~1.reset-%STAMP%"
if errorlevel 1 (
    set "FAILED=1"
    exit /b 1
)
set "CHANGED=1"
exit /b 0
