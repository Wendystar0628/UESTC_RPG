@echo off
rem Double-click to open the author dashboard: start live.py if not running, collect once, open index.html.
set "DASH=%~dp0"
powershell -NoProfile -Command "if (-not (Get-CimInstance Win32_Process | Where-Object { $_.Name -like 'python*' -and $_.CommandLine -like '*dashboard*live.py*' })) { Start-Process python -ArgumentList '-I','-X','utf8','%DASH%tools\live.py','--loop' -WindowStyle Hidden }"
python -I -X utf8 "%DASH%tools\collect.py"
start "" "%DASH%index.html"
