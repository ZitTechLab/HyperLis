@echo off
wpeinit

@echo off
title "HyperLis WSAP Automatic Windows Installation"
setlocal EnableExtensions EnableDelayedExpansion

Echo ------- HyperLis WSAP Automatic Windows Installation -------

:: Mount all drives (Important for WinPE to see all partitions)
if exist "%SystemRoot%\System32\LetterSwap.exe" ("%SystemRoot%\System32\LetterSwap.exe" /MountAll)

:: Search for Config File
Set "FindFile=HyperLisWindowsConfig.txt"
Set "FilePath="

Echo ----- Search %FindFile%
:: 1. Check X: first since our Installer puts it there
If exist "X:\%FindFile%" (
    Set "FilePath=X:\%FindFile%"
    Goto :ConfigFound
)

:: 2. Quick search in root of all drives
For %%D in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    if exist "%%D:\!FindFile!" (
        Set "FilePath=%%D:\!FindFile!"
        goto :ConfigFound
    )
)

:: 3. Recursive search if not found in root
For %%D in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    if exist "%%D:\" (
        For /f "delims=" %%a in ('dir /s /b "%%D:\!FindFile!" 2^>nul') do (
            Set "FilePath=%%a"
            goto :ConfigFound
        )
    )
)

:ConfigFound
If "!FilePath!"=="" (
    Echo ----- Exit to WinPE
    Echo [Error] %FindFile% not found.
    timeout /T 5 /NOBREAK > nul
    Exit /b
) Else (
    Echo %FindFile% was found at: "!FilePath!".
    Set "ConfigDrive=!FilePath:~0,2!"
)
Echo.
Echo ----- Get information from "!FilePath!"
For /f "usebackq tokens=1,* delims==" %%A in ("!FilePath!") do (
    set "%%A=%%B"
    Echo %%A=%%B
)
Echo.

:: Variables setup mapped from Config
Call :WSAP.GetDrive !WinSourceDisk! !WinSourcePartition!
Set "WinSourceDrive=!GetDrive!"

:: Dành cho WSAP: Nếu là dạng WIM/ESD/SWM được copy ra ổ X: (WinPE RAMDisk hoặc phân vùng tạm)
:: WinSourcePathNoDrive đã được lưu trong file Config kèm thư mục gốc (ví dụ \WSAP\sources\install.wim)
If /I "!WinSource!" EQU "WIM" (
    If exist "X:!WinSourcePathNoDrive!" (
        Set "WinSourcePath=X:!WinSourcePathNoDrive!"
        Set "WinSourceDrive=X:"
    ) Else If exist "!WinSourceDrive!!WinSourcePathNoDrive!" (
        Set "WinSourcePath=!WinSourceDrive!!WinSourcePathNoDrive!"
    ) Else (
        Set "WinSourcePath=!WinSourcePathNoDrive!"
    )
) Else If /I "!WinSource!" EQU "ESD" (
    If exist "X:!WinSourcePathNoDrive!" (
        Set "WinSourcePath=X:!WinSourcePathNoDrive!"
        Set "WinSourceDrive=X:"
    ) Else If exist "!WinSourceDrive!!WinSourcePathNoDrive!" (
        Set "WinSourcePath=!WinSourceDrive!!WinSourcePathNoDrive!"
    ) Else (
        Set "WinSourcePath=!WinSourcePathNoDrive!"
    )
) Else If /I "!WinSource!" EQU "SWM" (
    If exist "X:!WinSourcePathNoDrive!" (
        Set "WinSourcePath=X:!WinSourcePathNoDrive!"
        Set "WinSourceDrive=X:"
    ) Else If exist "!WinSourceDrive!!WinSourcePathNoDrive!" (
        Set "WinSourcePath=!WinSourceDrive!!WinSourcePathNoDrive!"
    ) Else (
        Set "WinSourcePath=!WinSourcePathNoDrive!"
    )
) Else (
    Set "WinSourcePath=!WinSourceDrive!!WinSourcePathNoDrive!"
)

Call :WSAP.GetDrive !RecommendedDisk! !RecommendedEFIPart!
Set "EFIDrive=!GetDrive!"

Call :WSAP.GetDrive !RecommendedDisk! !RecommendedOSPart!
Set "OSDrive=!GetDrive!"

Call :WSAP.GetDrive !BootWimDisk! !BootWimPartition!
Set "BootWimDrive=!GetDrive!"
Set "BootWimPath=!BootWimDrive!!BootWimPathNoDrive!"
Set "BootSdiPath=!BootWimDrive!!BootSdiPathNoDrive!"

If "!DriversDisk!" NEQ "" (
    Call :WSAP.GetDrive !DriversDisk! !DriversPartition!
    Set "DriversDrive=!GetDrive!"
    Set "DriversPath=!DriversDrive!!DriversPathNoDrive!"
) else (
    Set "DriversPath="
)

Echo ----- Manage !WinSource!
If /I "!WinSourceDrive!" equ "!OSDrive!" (
    Echo !WinSource! is in !OSDrive!:
    Echo Continuing installation from local drive...
    REM Call :WSAP.Finish
)

If /I "!WinSource!" equ "ISO" (Call :WSAP.ManageISO)

If not exist "!WinSourcePath!" (
    Echo !WinSourcePath! not found.
    Call :WSAP.Finish
)
Echo WinSourcePath="!WinSourcePath!"
Echo.

Echo ----- Manage Driver for Windows
If /I "!DriversDrive!" equ "!OSDrive!" Call :WSAP.MoveDrivers
If EXIST "!DriversPath!" (Echo DriverPath="!DriversPath!")
Echo.

Echo ----- Manage Partition
Set "ans1="
If /I "!AutoFormat!" NEQ "true" (Call :WSAP.AskToManagePartition)

(echo list disk ^& echo list volume) ^| diskpart

If /I "%FirmwareType%" neq "UEFI" (
    Call :WSAP.ManagePartition.MBR
) Else (
    Call :WSAP.ManagePartition.UEFI
)
Echo.

Echo ----- Extract !WinSource! to !OSDrive!
DISM /apply-image /imagefile:"%WinSourcePath%" /index:%WimIndex% /applydir:%OSDrive%\ 
IF %ERRORLEVEL% EQU 0 (
    ECHO Windows image applied successfully.
    Ren "!FilePath!" "HyperLisWindowsConfig_%random%.txt"
) ELSE (
    ECHO FAIL: DISM failed to extract the Windows image.
)
Echo.

If /I "!FirmwareType!" NEQ "UEFI" (
    Echo ----- Install Bootloader %OSDrive% Legacy
    BCDBOOT %OSDrive%\Windows /s %OSDrive% /f BIOS
    IF %ERRORLEVEL% EQU 0 (Echo Install Bootloader successfully) Else (Echo Fail to install Bootloader)
) Else ( 
    Echo ----- Install Bootloader %EFIDrive% UEFI
    BCDBOOT %OSDrive%\Windows /s %EFIDrive% /f UEFI
    IF %ERRORLEVEL% EQU 0 (Echo Install Bootloader successfully) Else (Echo Fail to install Bootloader)
)
Echo.

Echo ----- Add Driver to !OSDrive!
If "!DriversDisk!" NEQ "" (
    DISM /Image:%OSDrive%\ /Add-Driver /Driver:"%DriversPath%" /Recurse /ForceUnsigned 
    IF %ERRORLEVEL% EQU 0 (Echo Add driver successfully) Else (Echo Fail to add driver)
)
Echo.

Echo ----- Manage unattend.xml File
Set "UnattendedFile="
:: 1. Search on the same drive as Config file
If "!UnattendedFilePathNoDrive!" NEQ "" (
    If "!ConfigDrive!" NEQ "" (
        If Exist "!ConfigDrive!!UnattendedFilePathNoDrive!" (
            Set "UnattendedFile=!ConfigDrive!!UnattendedFilePathNoDrive!"
        )
    )
    :: 2. Search across all drives for the specified relative path
    If "!UnattendedFile!"=="" (
        For %%D in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
            If "!UnattendedFile!"=="" (
                If Exist "%%D:!UnattendedFilePathNoDrive!" Set "UnattendedFile=%%D:!UnattendedFilePathNoDrive!"
            )
        )
    )
    :: 3. Fallback to :WSAP.GetDrive if disk and partition are set
    If "!UnattendedFile!"=="" If "!UnattendedFileDisk!" NEQ "" (
        Call :WSAP.GetDrive !UnattendedFileDisk! !UnattendedFilePartition!
        If Exist "!GetDrive!!UnattendedFilePathNoDrive!" Set "UnattendedFile=!GetDrive!!UnattendedFilePathNoDrive!"
    )
)

:: 4. Fallback search for autounattend.xml or unattend.xml on any drive
If "!UnattendedFile!"=="" (
    For %%D in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
        If "!UnattendedFile!"=="" (
            If Exist "%%D:\autounattend.xml" Set "UnattendedFile=%%D:\autounattend.xml"
            If Exist "%%D:\unattend.xml" Set "UnattendedFile=%%D:\unattend.xml"
        )
    )
)

If "!UnattendedFile!" NEQ "" (
    Echo Found unattend file at: "!UnattendedFile!"
    If Not Exist "!OSDrive!\Windows\Panther" md "!OSDrive!\Windows\Panther" >nul 2>&1
    If Not Exist "!OSDrive!\Windows\Panther\Unattend" md "!OSDrive!\Windows\Panther\Unattend" >nul 2>&1
    If Not Exist "!OSDrive!\Windows\System32\Sysprep" md "!OSDrive!\Windows\System32\Sysprep" >nul 2>&1
    copy /y "!UnattendedFile!" "!OSDrive!\Windows\Panther\unattend.xml" >nul 2>&1
    copy /y "!UnattendedFile!" "!OSDrive!\Windows\Panther\autounattend.xml" >nul 2>&1
    copy /y "!UnattendedFile!" "!OSDrive!\Windows\Panther\Unattend\unattend.xml" >nul 2>&1
    copy /y "!UnattendedFile!" "!OSDrive!\Windows\System32\Sysprep\unattend.xml" >nul 2>&1
    copy /y "!UnattendedFile!" "!OSDrive!\unattend.xml" >nul 2>&1
    copy /y "!UnattendedFile!" "!OSDrive!\autounattend.xml" >nul 2>&1

    :: Register UnattendFile into Offline Registry
    reg load "HKLM\OfflineSystem" "!OSDrive!\Windows\System32\config\SYSTEM" >nul 2>&1
    reg add "HKLM\OfflineSYSTEM\Setup" /v "UnattendFile" /t REG_SZ /d "C:\Windows\Panther\unattend.xml" /f >nul 2>&1
    reg unload "HKLM\OfflineSystem" >nul 2>&1

    Echo Applying unattend file with DISM...
    DISM /Image:!OSDrive!\ /Apply-Unattend:"!UnattendedFile!"
    IF !ERRORLEVEL! EQU 0 (Echo Apply unattend successfully) Else (Echo DISM apply-unattend finished with code !ERRORLEVEL!)
) Else (
    Echo Warning: Unattend file was not found on any drive.
)
Echo.

Echo ----- Bypass Windows 11 requirements
If /I "!AutoBypassWindows11!" equ "true" (
    reg load "HKLM\OfflineSystem" "!OSDrive!\Windows\System32\config\SYSTEM" >nul
    If %ErrorLevel% NEQ 0 (Echo Failed to load registry hive)
    reg add "HKLM\OfflineSYSTEM\Setup\LabConfig" /v "BypassTPMCheck" /t REG_DWORD /d 1 /f
    reg add "HKLM\OfflineSYSTEM\Setup\LabConfig" /v "BypassSecureBootCheck" /t REG_DWORD /d 1 /f
    reg add "HKLM\OfflineSYSTEM\Setup\LabConfig" /v "BypassRAMCheck" /t REG_DWORD /d 1 /f
    reg add "HKLM\OfflineSYSTEM\Setup\LabConfig" /v "BypassStorageCheck" /t REG_DWORD /d 1 /f
    reg add "HKLM\OfflineSYSTEM\Setup\MoSetup" /v "AllowUpgradesWithUnsupportedTPMOrCPU" /t REG_DWORD /d 1 /f
    reg unload "HKLM\OfflineSystem" >nul
    If %ErrorLevel% EQU 0 (Echo Bypass added successfully) Else (Echo Failed to unload registry hive)    
)
Echo.

Echo ----- Bypass login Microsoft account
If /I "!AutoBypassNRO!" equ "true" (
    reg load "HKLM\OfflineSoftware" "!OSDrive!\Windows\System32\config\SOFTWARE" >nul
    If %ErrorLevel% NEQ 0 (Echo Failed to load registry hive)
    reg add "HKLM\OfflineSoftware\Microsoft\Windows\CurrentVersion\OOBE" /v "BypassNRO" /t REG_DWORD /d 1 /f >nul
    reg unload "HKLM\OfflineSoftware" >nul
    If %ErrorLevel% EQU 0 (Echo Apply OOBE network bypass successfully) Else (Echo Failed to unload registry hive)    
)
Echo.

Echo ----- Create Boot Menu for Other Windows OS
If /I "!AutoOtherWindowsBootMenu!" equ "true" (Call :WSAP.AutoOtherWindowsBootMenu)
Echo.

Echo ----- Create Boot Menu for WinPE
If /I "!AutoWinpeBootMenu!" equ "true" (Call :WSAP.AutoWinpeBootMenu)
Echo.

Echo ----- Restart computer
If /I "!AutoRestart!" equ "true" (
    Echo Automatic Windows installation has finished. Restart computer in 5 seconds.
    timeout /T 5 /NOBREAK > nul
    shutdown /r /t 0
    Exit
) 
Echo Automatic Windows installation has finished successfully. 
Pause
Exit

::::::::::::WSAP::::::::::::::WSAP::::::::::::::WSAP::::::::::::::
:WSAP.AutoOtherWindowsBootMenu
Set "BcdPath=!EFIDrive!\EFI\Microsoft\Boot\BCD"
If not exist "!BcdPath!" (
    Echo BCD not found. Failed to create boot menu for WinPE.
    Goto :EOF
)
If not exist "!BootSdiPath!" (
    Echo BootSdiPath not found. Failed to create boot menu for WinPE.
    Goto :EOF
)
If not exist "!BootWimPath!" (
    Echo BootWimPath not found. Failed to create boot menu for WinPE.
    Goto :EOF
)
Echo BCD is available: "!BcdPath!"

For /f "tokens=*" %%d in ('fsutil fsinfo drives') Do (Set "DriveList=%%d")
Set "DriveList=!DriveList:Drives: =!"
Set "DriveList=!DriveList:\=!"
Set "DriveList=!DriveList:%SystemDrive% =!"
Set "DriveList=!DriveList:%OSDrive% =!"
For %%d in (%DriveList%) Do (
        If Exist "%%d\Windows\System32" (Call :WSAP.AutoOtherWindowsBootMenu.1 "%%d")
)
Goto :EOF

::::::::::::WSAP::::::::::::::WSAP::::::::::::::WSAP::::::::::::::
:WSAP.AutoOtherWindowsBootMenu.1
Set "GUID1="
For /f "tokens=2 delims={}" %%a in ('BCDedit /store "!BcdPath!" /create /d "Windows %Random%" /application osloader') do set "GUID1={%%a}"
if not defined GUID1 (
    echo Failed to create boot entry.
    Goto :EOF
)
BCDedit /store "!BcdPath!" /set {%GUID1%} device partition=%~1 >nul
BCDedit /store "!BcdPath!" /set {%GUID1%} osdevice partition=%~1 >nul
BCDedit /store "!BcdPath!" /set {%GUID1%} path \Windows\System32\winload.efi >nul
BCDedit /store "!BcdPath!" /set {%GUID1%} systemroot \Windows >nul
BCDedit /store "!BcdPath!" /displayorder {%GUID1%} /addlast && (Echo Create boot entry for windows successfully.) || (echo Failed to create boot entry.)
BCDedit /store "!BcdPath!" /timeout 10 >nul
BCDedit /store "!BcdPath!" /set {default} bootmenupolicy standard >nul
Goto :EOF

::::::::::::WSAP::::::::::::::WSAP::::::::::::::WSAP::::::::::::::
:WSAP.AutoWinpeBootMenu
Set "BcdPath=!EFIDrive!\EFI\Microsoft\Boot\BCD"
If not exist "!BcdPath!" (
    Echo BCD not found. Failed to create boot menu for WinPE.
    Goto :EOF
)
If not exist "!BootSdiPath!" (
    Echo BootSdiPath not found. Failed to create boot menu for WinPE.
    Goto :EOF
)
If not exist "!BootWimPath!" (
    Echo BootWimPath not found. Failed to create boot menu for WinPE.
    Goto :EOF
)
Echo BCD is available: "!BcdPath!"
bcdedit /store "!BcdPath!" /create {ramdiskoptions} /d "Ramdisk Options" >nul
bcdedit /store "!BcdPath!" /set {ramdiskoptions} ramdisksdidevice partition=!BootWimPath:~0,2! >nul
bcdedit /store "!BcdPath!" /set {ramdiskoptions} ramdisksdipath "!BootSdiPath:~2!" >nul
for /f "tokens=2 delims={}" %%a in ('bcdedit /store "!BcdPath!" /create /d "WinPE %Random%" /application osloader') do (
    set "GUID={%%a}"
)
bcdedit /store "!BcdPath!" /set %GUID% device "ramdisk=[!BootWimPath:~0,2!]!BootWimPath:~2!,{ramdiskoptions}"
If /I "!FirmwareType!" NEQ "UEFI" (
   bcdedit /store "!BcdPath!" /set %GUID% path "\windows\system32\winload.exe" >nul
) Else (
    bcdedit /store "!BcdPath!" /set %GUID% path "\windows\system32\boot\winload.efi" >nul
)
bcdedit /store "!BcdPath!" /set %GUID% osdevice "ramdisk=[!BootWimPath:~0,2!]!BootWimPath:~2!,{ramdiskoptions}" >nul
bcdedit /store "!BcdPath!" /set %GUID% systemroot \windows >nul
bcdedit /store "!BcdPath!" /set %GUID% winpe yes >nul
bcdedit /store "!BcdPath!" /set %GUID% detecthal yes >nul
bcdedit /store "!BcdPath!" /displayorder %GUID% /addlast && (Echo Create boot entry for WinPE successfully.) || (echo Failed to create boot entry.)
bcdedit /store "!BcdPath!" /timeout 10 >nul
bcdedit /store "!BcdPath!" /set {default} bootmenupolicy standard >nul
Goto :EOF

::::::::::::WSAP::::::::::::::WSAP::::::::::::::WSAP::::::::::::::
:WSAP.ManageISO
"%SystemDrive%\Windows\System32\imdisk.exe" -a -f "!WinSourcePath!" -m X: >NUL
If exist "X:\WSAP\sources\install.wim" (Set "WinSourcePath=X:\WSAP\sources\install.wim")
If exist "X:\WSAP\sources\install.esd" (Set "WinSourcePath=X:\WSAP\sources\install.esd")
If exist "X:\WSAP\sources\install.swm" (Set "WinSourcePath=X:\WSAP\sources\install.swm")
Goto :EOF

::::::::::::WSAP::::::::::::::WSAP::::::::::::::WSAP::::::::::::::
:WSAP.Finish
Set "ans="
Echo -----------------------------------------------
Echo  [1]. Only press Enter to Restart PC.
Echo  [2]. Type anything and press Enter to Exit CMD.
Echo -----------------------------------------------
Set /p "ans=> Your wishes: "
IF "!ans!" equ "1" (shutdown /r /t 0)
IF "!ans!" equ "" (shutdown /r /t 0)
Exit
Goto :EOF

::::::::::::WSAP::::::::::::::WSAP::::::::::::::WSAP::::::::::::::
:WSAP.MoveDrivers
XCOPY "%DriversPath%" "%SystemDrive%%DriversPathNoDrive%" /E /H /Q /Y /I
Set "DriversDrive=%SystemDrive%"
Set "DriversPath=%SystemDrive%%DriversPathNoDrive%"
Goto :EOF

::::::::::::WSAP::::::::::::::WSAP::::::::::::::WSAP::::::::::::::
:WSAP.ManagePartition.UEFI
(
    Echo select disk %RecommendedDisk%
    Echo select partition %RecommendedEFIPart%
    Echo format fs=fat32 quick label="EFI" override
    Echo set id=c12a7328-f81f-11d2-ba4b-00a0c93ec93b
    Echo select partition %RecommendedOSPart%
    Echo format fs=NTFS quick label="Windows" override
    Echo exit
) | diskpart
Goto :EOF

::::::::::::WSAP::::::::::::::WSAP::::::::::::::WSAP::::::::::::::
:WSAP.ManagePartition.MBR
(
    Echo select disk %RecommendedDisk%
    Echo select partition %RecommendedOSPart%
    Echo format fs=NTFS quick label="Windows" override
    Echo active
    Echo exit
) | diskpart
Goto :EOF

::::::::::::WSAP::::::::::::::WSAP::::::::::::::WSAP::::::::::::::
:WSAP.GetDrive
Set "GetDrive="
Set "temp_dp=%temp%\dp_!random!.txt"
Set "temp_out=%temp%\dp_out_!random!.txt"
(
    echo select disk %1
    echo select partition %2
    echo assign
    echo detail partition
) > "!temp_dp!"
diskpart /s "!temp_dp!" > "!temp_out!"
For /f "delims=" %%L in ('type "!temp_out!"') do (
    set "line=%%L"
    :: Check if the line starts with "* " (selected volume)
    if "!line:~0,2!"=="* " (
        set "DriveLetter=!line:~14,3!"
        set "DriveLetter=!DriveLetter: =!"
        :: Make sure it is exactly one letter A-Z
        echo !DriveLetter!| findstr /R /I "^[A-Z]$" >nul
        if !errorlevel!==0 set "GetDrive=!DriveLetter!:"
    )
)
:: Fallback if not selected
if "!GetDrive!"=="" (
    For /f "delims=" %%L in ('type "!temp_out!"') do (
        set "line=%%L"
        if "!line:~0,2!"=="  " (
            set "DriveLetter=!line:~14,3!"
            set "DriveLetter=!DriveLetter: =!"
            echo !DriveLetter!| findstr /R /I "^[A-Z]$" >nul
            if !errorlevel!==0 set "GetDrive=!DriveLetter!:"
        )
    )
)
del /q "!temp_dp!" "!temp_out!" >nul 2>&1
Goto :EOF

::::::::::::WSAP::::::::::::::WSAP::::::::::::::WSAP::::::::::::::
:WSAP.AskToManagePartition
Echo Warning: All data on OS partition will be Permanently Erased.
Set /p "ans1=> To auto-format partitions, press Enter for Yes: " 
If "!ans1!" equ "" Set "ans1=Y"
If /I "!ans1!" equ "y" Set "ans1=Y"
If /I "!ans1!" NEQ "Y" (
    Echo Returning to WinPE in 5 seconds.
    timeout /T 5 /NOBREAK > nul
    Exit
)
Goto :EOF


