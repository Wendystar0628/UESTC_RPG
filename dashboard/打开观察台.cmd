@echo off
rem 双击启动观察台：隐藏启动实时采集与本地服务，采集当前项目，再打开本机网页。
rem 产出：dashboard/data/auto/、本机127.0.0.1:8765观察台
rem 用法：双击，无参数
set "DASH=%~dp0"
powershell -NoProfile -Command "if (-not (Get-CimInstance Win32_Process | Where-Object { $_.Name -like 'python*' -and $_.CommandLine -like '*dashboard*live.py*' })) { Start-Process python -ArgumentList '-I','-X','utf8','%DASH%tools\live.py','--loop' -WindowStyle Hidden }; if (-not (Get-CimInstance Win32_Process | Where-Object { $_.Name -like 'python*' -and $_.CommandLine -like '*dashboard*serve.py*' })) { Start-Process python -ArgumentList '-I','-X','utf8','%DASH%tools\serve.py' -WindowStyle Hidden }"
python -I -X utf8 "%DASH%tools\collect.py"
start "" "http://127.0.0.1:8765/"
