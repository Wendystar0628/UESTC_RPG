@echo off
rem 删除当前用户设置文件 UESTC\settings.json 里的 graphics 一节，让画面设置恢复默认。
rem 用法：双击或在命令行运行 scripts\devices\reset-graphics.cmd，无参数
setlocal EnableExtensions DisableDelayedExpansion
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -Command "$ErrorActionPreference = 'Stop'; $settingsPath = Join-Path $env:APPDATA 'UESTC\settings.json'; if (Test-Path -LiteralPath $settingsPath) { $settings = Get-Content -LiteralPath $settingsPath -Raw -Encoding UTF8 | ConvertFrom-Json; if ($null -ne $settings.PSObject.Properties['graphics']) { $settings.PSObject.Properties.Remove('graphics'); $json = ConvertTo-Json -InputObject $settings -Depth 100; [System.IO.File]::WriteAllText($settingsPath, $json, [System.Text.UTF8Encoding]::new($false)) } }"
exit /b %errorlevel%
