$localMachine = Get-Content -Path "E:\GGAI\Design\Warehouse\Optimize game\Optimize game\Scripts\localmachine.cmd" -Raw
$defaultUser = Get-Content -Path "E:\GGAI\Design\Warehouse\Optimize game\Optimize game\Scripts\defaultuser.cmd" -Raw
$wintweaks = Get-Content -Path "E:\GGAI\Design\Warehouse\Optimize game\Optimize game\Scripts\wintweaks.ps1" -Raw

# Replace HKU\DefaultUser with HKCU to apply to the current user
$currentUser = $defaultUser -replace 'HKU\\DefaultUser', 'HKCU'

$outPath = "E:\GGAI\Design\Warehouse\Optimize game\Optimize game\Scripts\Ultimate_Optimize.ps1"

$script = @"
<#
.SYNOPSIS
Ultimate Post-Install Optimization Script.
Combined from localmachine.cmd, defaultuser.cmd, wintweaks.ps1 and extra tweaks.
#>

# Require Admin
if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Warning "Vui long chay script nay duoi quyen Administrator (Run as Administrator)!"
    Start-Sleep -Seconds 3
    Exit
}

Write-Host "=== 1. AP DUNG LOCAL MACHINE TWEAKS ===" -ForegroundColor Cyan
$localMachine

Write-Host "=== 2. AP DUNG CURRENT USER TWEAKS ===" -ForegroundColor Cyan
$currentUser

Write-Host "=== 3. AP DUNG PUBG SPRAY & MOUSE KEYS TWEAKS ===" -ForegroundColor Cyan
reg add "HKEY_CURRENT_USER\Control Panel\Accessibility\MouseKeys" /v MaximumSpeed /t REG_SZ /d 10 /f
reg add "HKEY_CURRENT_USER\Control Panel\Accessibility\MouseKeys" /v TimeToMaximumSpeed /t REG_SZ /d 5000 /f
reg add "HKEY_CURRENT_USER\Control Panel\Accessibility\MouseKeys" /v Flags /t REG_SZ /d 190 /f
reg add "HKEY_CURRENT_USER\Control Panel\Desktop" /v CursorBlinkRate /t REG_SZ /d 200 /f

Write-Host "=== 4. AP DUNG POWERSHELL WINTWEAKS ===" -ForegroundColor Cyan
$wintweaks

Write-Host "Hoan tat toi uu hoa! Vui long khoi dong lai may de cac thay doi co hieu luc." -ForegroundColor Green
Start-Sleep -Seconds 5
"@

Set-Content -Path $outPath -Value $script -Encoding UTF8
Write-Host "Successfully generated Ultimate_Optimize.ps1"
