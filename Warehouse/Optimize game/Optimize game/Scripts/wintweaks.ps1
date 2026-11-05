# Start Transcript to capture script output
  $logFile = "C:\Windows\Setup\Scripts\wintweaks.log"
  Start-Transcript -Path $logFile
          
        
  # Create a shortcut object
  $shortcut = $shell.CreateShortcut($shortcutPath)
           
  # Set properties of the shortcut
  $shortcut.TargetPath = "powershell.exe"
  $shortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command `"$command`""
  # Save the shortcut
  $shortcut.Save()
        
  # Make the shortcut have 'Run as administrator' property on
  $bytes = [System.IO.File]::ReadAllBytes($shortcutPath)
  # Set byte value at position 0x15 in hex, or 21 in decimal, from the value 0x00 to 0x20 in hex
  $bytes[0x15] = $bytes[0x15] -bor 0x20
  [System.IO.File]::WriteAllBytes($shortcutPath, $bytes)

  # Configure Maximum Password Age in Windows
  net.exe accounts /maxpwage:UNLIMITED
  
  # Allow Execution of PowerShell Script Files
  Set-ExecutionPolicy -Scope 'LocalMachine' -ExecutionPolicy 'RemoteSigned' -Force
  
  # Groups or splits svchost.exe processes based on the amount of physical memory in the system to optimize performance
  $ram = (Get-CimInstance -ClassName Win32_PhysicalMemory | Measure-Object -Property Capacity -Sum).Sum / 1kb
  Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control" -Name "SvcHostSplitThresholdInKB" -Type DWord -Value $ram -Force
  
  $autoLoggerDir = "$env:PROGRAMDATA\Microsoft\Diagnosis\ETLLogs\AutoLogger"
  If (Test-Path "$autoLoggerDir\AutoLogger-Diagtrack-Listener.etl") {
      Remove-Item "$autoLoggerDir\AutoLogger-Diagtrack-Listener.etl"
  }
  $icaclsCommand = "icacls `"$autoLoggerDir`" /deny SYSTEM:`"(OI)(CI)F`""
  Invoke-Expression $icaclsCommand | Out-Null
  

  
  # Removes Microsoft Edge
  foreach ($line in $lines) {
      if ($line -like '*Architecture : *') {
          $architecture = $line -replace 'Architecture : ',''
          # If the architecture is x64, replace it with amd64
          if ($architecture -eq 'x64') {
              $architecture = 'amd64'
          }
          Write-Host "Architecture: $architecture"
          break
      }
  }
  
  if (-not $architecture) {
      Write-Host "Architecture information not found."
  }
  Remove-Item -Path "C:\Program Files (x86)\Microsoft\Edge" -Recurse -Force -ErrorAction Continue
  Remove-Item -Path "C:\Program Files (x86)\Microsoft\EdgeWebView" -Recurse -Force -ErrorAction Continue
  Remove-Item -Path "C:\Program Files (x86)\Microsoft\EdgeUpdate" -Recurse -Force -ErrorAction Continue
  Remove-Item -Path "C:\Program Files (x86)\Microsoft\EdgeCore" -Recurse -Force -ErrorAction Continue
  Remove-Item -Path "C:\Users\Public\Desktop\Microsoft Edge.lnk" -Recurse -Force -ErrorAction Continue
  Remove-Item -Path "C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Microsoft Edge.lnk" -Recurse -Force -ErrorAction Continue
  Remove-Item -Path "C:\Windows\SystemApps\Microsoft.MicrosoftEdge_8wekyb3d8bbwe" -Recurse -Force -ErrorAction Continue
  Remove-Item -Path "C:\Windows\SystemApps\Microsoft.MicrosoftEdgeDevToolsClient_8wekyb3d8bbwe" -Recurse -Force -ErrorAction Continue
  if ($architecture -eq 'amd64') {
      $folderPath = Get-ChildItem -Path "C:\Windows\WinSxS" -Filter "amd64_microsoft-edge-webview_31bf3856ad364e35*" -Directory | Select-Object -ExpandProperty FullName
  
      if ($folderPath) {
          & 'takeown' '/f' $folderPath '/r' 
          & icacls $folderPath  "/grant" "$($adminGroup.Value):(F)" '/T' '/C' 
          Remove-Item -Path $folderPath -Recurse -Force 
      } else {
          Write-Host "Folder not found."
      }
  } elseif ($architecture -eq 'arm64') {
      $folderPath = Get-ChildItem -Path "C:\Windows\WinSxS" -Filter "arm64_microsoft-edge-webview_31bf3856ad364e35*" -Directory | Select-Object -ExpandProperty FullName
  
      if ($folderPath) {
          & 'takeown' '/f' $folderPath '/r'
          & icacls $folderPath  "/grant" "$($adminGroup.Value):(F)" '/T' '/C' 
          Remove-Item -Path $folderPath -Recurse -Force 
      } else {
          Write-Host "Folder not found."
      }
  } else {
      Write-Host "Unknown architecture: $architecture"
  }
  & 'takeown' '/f' "C:\Windows\System32\Microsoft-Edge-Webview" '/r' 
  & 'icacls' "C:\Windows\System32\Microsoft-Edge-Webview" '/grant' "$($adminGroup.Value):(F)" '/T' '/C' 
  Remove-Item -Path "C:\Windows\System32\Microsoft-Edge-Webview" -Recurse -Force
  
  # Removes OneDrive
  Remove-Item "C:\Users\Default\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\OneDrive.lnk" -ErrorAction Continue
  Remove-Item "C:\Users\Default\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\OneDrive.exe" -ErrorAction Continue
  Remove-Item "C:\Windows\System32\OneDriveSetup.exe" -ErrorAction Continue
  Remove-Item "C:\Windows\SysWOW64\OneDriveSetup.exe" -ErrorAction Continue
   
  # Removes Microsoft Teams
  $TeamsPath = [System.IO.Path]::Combine($env:LOCALAPPDATA, 'Microsoft', 'Teams')
  $TeamsUpdateExePath = [System.IO.Path]::Combine($TeamsPath, 'Update.exe')
  
  Stop-Process -Name "*teams*" -Force -ErrorAction Continue
  
  if ([System.IO.File]::Exists($TeamsUpdateExePath)) {
      # Uninstall app
      $proc = Start-Process $TeamsUpdateExePath "-uninstall -s" -PassThru
      $proc.WaitForExit()
  }
  
  Get-AppxPackage "*Teams*" | Remove-AppxPackage -ErrorAction Continue
  Get-AppxPackage "*Teams*" -AllUsers | Remove-AppxPackage -AllUsers -ErrorAction Continue
  
  if ([System.IO.Directory]::Exists($TeamsPath)) {
      Remove-Item $TeamsPath -Force -Recurse -ErrorAction Continue
  }
  
  # Uninstall from Uninstall registry key UninstallString
  $us = (Get-ChildItem -Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall, HKLM:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall | Get-ItemProperty | Where-Object { $_.DisplayName -like '*Teams*'}).UninstallString
  if ($us.Length -gt 0) {
      $us = ($us.Replace('/I', '/uninstall ') + ' /quiet').Replace('  ', ' ')
      $FilePath = ($us.Substring(0, $us.IndexOf('.exe') + 4).Trim())
      $ProcessArgs = ($us.Substring($us.IndexOf('.exe') + 5).Trim().replace('  ', ' '))
      $proc = Start-Process -FilePath $FilePath -Args $ProcessArgs -PassThru
      $proc.WaitForExit()
  }
  
  # Disables Teredo
  $registryKeysTeredo = @(
      @{Path = "HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters"; Name = "DisabledComponents"; Type = "DWord"; Value = 1}
  )
  foreach ($key in $registryKeysTeredo) {
      New-ItemProperty -Path $key.Path -Name $key.Name -PropertyType $key.Type -Value $key.Value -Force
  }
  netsh interface teredo set state disabled
  
  # Disable Scheduled Tasks
  $scheduledTasks = @(
      "Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
      "Microsoft\Windows\Application Experience\ProgramDataUpdater",
      "Microsoft\Windows\Autochk\Proxy",
      "Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
      "Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
      "Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
      "Microsoft\Windows\Feedback\Siuf\DmClient",
      "Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
      "Microsoft\Windows\Windows Error Reporting\QueueReporting",
      "Microsoft\Windows\Application Experience\MareBackup",
      "Microsoft\Windows\Application Experience\StartupAppTask",
      "Microsoft\Windows\Application Experience\PcaPatchDbTask",
      "Microsoft\Windows\Maps\MapsUpdateTask"
  )
  foreach ($task in $scheduledTasks) {
      schtasks /Change /TN $task /Disable
  }
  
  # Enable the Ultimate Performance power plan
  powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61
  
  # Set the Ultimate Performance power plan as active
  powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61
  
  Stop-Transcript