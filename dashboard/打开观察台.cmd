@echo off
rem 双击打开作者观察台：live.py 没在跑就在后台启动，采集一次数据，再打开 index.html。
rem 用法：双击，无参数
set "DASH=%~dp0"
powershell -NoProfile -Command "if (-not (Get-CimInstance Win32_Process | Where-Object { $_.Name -like 'python*' -and $_.CommandLine -like '*dashboard*live.py*' })) { Start-Process python -ArgumentList '-I','-X','utf8','%DASH%tools\live.py','--loop' -WindowStyle Hidden }"
python -I -X utf8 "%DASH%tools\collect.py"
start "" "%DASH%index.html"
