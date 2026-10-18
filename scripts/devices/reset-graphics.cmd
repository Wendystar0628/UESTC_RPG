@echo off
setlocal EnableExtensions DisableDelayedExpansion
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -Command "$ErrorActionPreference = 'Stop'; $settingsPath = Join-Path $env:APPDATA 'UESTC\settings.json'; if (Test-Path -LiteralPath $settingsPath) { $settings = Get-Content -LiteralPath $settingsPath -Raw -Encoding UTF8 | ConvertFrom-Json; if ($null -ne $settings.PSObject.Properties['graphics']) { $settings.PSObject.Properties.Remove('graphics'); $json = ConvertTo-Json -InputObject $settings -Depth 100; [System.IO.File]::WriteAllText($settingsPath, $json, [System.Text.UTF8Encoding]::new($false)) } }"
exit /b %errorlevel%
