$path = 'E:\GGAI\Design\Warehouse\Optimize game\Optimize game\Scripts\Ultimate_Optimize.ps1'
$content = Get-Content -Path $path -Raw

# 1. Comment out TaskCache deletions which throw access denied (handled by schtasks disable anyway)
$content = $content -replace '(?m)^(reg\.exe delete "HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\TaskCache\\Tasks.*)', '# $1'

# 2. Add Stop-Process before Edge/OneDrive removal to prevent File in Use errors
$edgeKill = "Stop-Process -Name msedge, msedgewebview2, MicrosoftEdgeUpdate, OneDriveSetup -Force -ErrorAction SilentlyContinue`r`n# Removes Microsoft Edge"
$content = $content -replace '(?m)^# Removes Microsoft Edge', $edgeKill

# 3. Comment out the broken Shortcut creation code from wintweaks
$content = $content -replace '(?s)(# Create a shortcut object.*?# Make the shortcut have.*?\WriteAllBytes\(\$shortcutPath, \$bytes\))', '# [Shortcut creation removed]'

Set-Content -Path $path -Value $content -Encoding UTF8
Write-Host "Fixed Script!"
