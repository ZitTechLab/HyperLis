# ================== ELEVATE ADMIN ==================
# Neu chua chay quyen admin thi tu mo lai voi quyen admin
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    $p = Start-Process powershell.exe -Verb RunAs -ArgumentList $arguments -PassThru -Wait
    exit $p.ExitCode
}

Write-Host "Script dang chay voi quyen Administrator." -ForegroundColor Green

# Ghi log qua trinh tao va cai WinPE
try {
    $scriptDir = $null
    try { if ($PSCommandPath) { $scriptDir = Split-Path -Parent $PSCommandPath } } catch {}
    if (-not $scriptDir -and $PSScriptRoot) { $scriptDir = $PSScriptRoot }
    if (-not $scriptDir) { $scriptDir = (Get-Location).Path }
    $logFile = Join-Path $scriptDir "winpe_install.log"
    Start-Transcript -Path $logFile -Append -Force -ErrorAction SilentlyContinue | Out-Null
} catch {}

# ================== HAM TRO GIUP ==================
function Get-AvailableDrives {
    # Lay danh sach cac o dia co the thu nho (o Fixed, khong phai Removable)
    $AvailableDrives = Get-Partition | Where-Object {
        $_.DriveLetter -and
        $_.DriveLetter -ne '' -and
        ($vol = Get-Volume -DriveLetter $_.DriveLetter -ErrorAction SilentlyContinue) -and
        $vol.DriveType -eq 'Fixed'
    } | Select-Object DriveLetter, @{
        Name='SizeGB'
        Expression={[math]::Round(($_.Size / 1GB), 2)}
    }, @{
        Name='FreeSpaceGB'
        Expression={
            $v = Get-Volume -DriveLetter $_.DriveLetter -ErrorAction SilentlyContinue
            if ($v) { [math]::Round(($v.SizeRemaining / 1GB), 2) } else { 0 }
        }
    }
    return $AvailableDrives
}

function Check-XDriveAvailable {
    # Kiem tra xem o X: da duoc su dung chua
    $XDrive = Get-Partition -DriveLetter X -ErrorAction SilentlyContinue
    if ($XDrive) {
        Write-Host "O X: dang duoc su dung. Ban co muon format de dung lai o X: khong?" -ForegroundColor Yellow
        $choice = Read-Host "Chon Y de tiep tuc (du lieu se mat) hoac N de huy (Y/N)"
        if ($choice -notmatch '^[Yy]') {
            Write-Host "Huy thao tac." -ForegroundColor Red
            return $false
        }
        try {
            Write-Host "Dang format o X:..." -ForegroundColor Yellow
            Format-Volume -DriveLetter X -FileSystem NTFS -NewFileSystemLabel "WINPE" -Confirm:$false -Force
            Write-Host "Da format lai o X: thanh cong." -ForegroundColor Green
            return $true
        } catch {
            Write-Host "Loi khi format o X:: $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }
    return $true
}

function Create-XDrive {
    param(
        [string]$SourceDrive,
        [int]$SizeGB = 10   # co dinh 10GB
    )

    $SizeMB = $SizeGB * 1024

    # Kiem tra o nguon ton tai
    $SourcePartition = Get-Partition -DriveLetter $SourceDrive -ErrorAction SilentlyContinue
    if (-not $SourcePartition) {
        Write-Host "Khong tim thay o $SourceDrive." -ForegroundColor Red
        return $false
    }

    # Kiem tra dung luong kha dung
    $SourceVolume = Get-Volume -DriveLetter $SourceDrive -ErrorAction SilentlyContinue
    if (-not $SourceVolume) {
        Write-Host "Khong the lay thong tin o $SourceDrive." -ForegroundColor Red
        return $false
    }

    $FreeSpaceGB = [math]::Round(($SourceVolume.SizeRemaining / 1GB), 2)
    if ($SizeGB -gt $FreeSpaceGB) {
        Write-Host "O $SourceDrive khong du dung luong. Kha dung: $FreeSpaceGB GB." -ForegroundColor Red
        return $false
    }

    try {
        # Thu nho o nguon
        Write-Host "        Dang thu nho o $SourceDrive voi $SizeMB MB..." -ForegroundColor Yellow
        $NewSize = (Get-Partition -DriveLetter $SourceDrive).Size - ($SizeMB * 1MB)
        Resize-Partition -DriveLetter $SourceDrive -Size $NewSize -ErrorAction Stop
        Write-Host "    Da thu nho o $SourceDrive thanh cong." -ForegroundColor Green

        # Tao phan vung moi tu khong gian vua thu nho
        $DiskNumber = $SourcePartition.DiskNumber
        Start-Sleep -Seconds 2

        Write-Host "        Dang tao phan vung moi va gan chu cai o X:..." -ForegroundColor Yellow
        $XPartition = $null

        # 1. Thu tao bang PowerShell New-Partition truoc
        try {
            $newPart = New-Partition -DiskNumber $DiskNumber -Size ($SizeMB * 1MB) -DriveLetter X -ErrorAction Stop
            Start-Sleep -Seconds 2
            Format-Volume -DriveLetter X -FileSystem NTFS -NewFileSystemLabel "WINPE" -Confirm:$false -Force -ErrorAction Stop
            $XPartition = Get-Partition -DriveLetter X -ErrorAction SilentlyContinue
        } catch {
            Write-Host "        New-Partition PowerShell: $($_.Exception.Message) -> Chuyen sang DiskPart..." -ForegroundColor DarkYellow
        }

        # 2. Neu New-Partition chua tao duoc, dung DiskPart
        if (-not $XPartition) {
$DiskPartScript = @"
select disk $DiskNumber
create partition primary size=$SizeMB
format fs=ntfs quick label="WINPE"
assign letter=X
exit
"@
            $dpOutput = $DiskPartScript | diskpart 2>&1
            Write-Host ($dpOutput | Out-String)
            Start-Sleep -Seconds 3
            $XPartition = Get-Partition -DriveLetter X -ErrorAction SilentlyContinue
        }

        if ($XPartition) {
            Write-Host "    Da tao o X: dung luong $SizeGB GB tu o $SourceDrive." -ForegroundColor Green
            return $true
        } else {
            Write-Host "    Khong the tao o X: sau khi thu nho o dia." -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "Loi khi tao o X:: $($_.Exception.Message)" -ForegroundColor Red

        # Co gang khoi phuc dung luong neu co phan vung co gian duoc
        try {
            $Partitions = Get-Partition -DiskNumber $SourcePartition.DiskNumber | Sort-Object PartitionNumber
            foreach ($Partition in $Partitions) {
                $SupportedSize = Get-PartitionSupportedSize -InputObject $Partition -ErrorAction SilentlyContinue
                if ($SupportedSize -and $Partition.Size -lt $SupportedSize.SizeMax) {
                    try {
                        Write-Host "Dang khoi phuc dung luong cho partition $($Partition.PartitionNumber)..." -ForegroundColor Yellow
                        Resize-Partition -InputObject $Partition -Size $SupportedSize.SizeMax -ErrorAction Stop
                        Write-Host "Da khoi phuc dung luong." -ForegroundColor Green
                        break
                    } catch {
                        Write-Host "Khong the khoi phuc dung luong: $($_.Exception.Message)" -ForegroundColor Red
                    }
                }
            }
        } catch {
            Write-Host "Loi khi khoi phuc dung luong: $($_.Exception.Message)" -ForegroundColor Red
        }
        return $false
    }
}

function Mount-WindowsISO {
    param([string]$ISOPath)

    if (-not (Test-Path $ISOPath)) {
        Write-Host "File ISO khong ton tai: $ISOPath" -ForegroundColor Red
        return $null
    }

    try {
        Write-Host "        Dang mount file ISO..." -ForegroundColor Yellow
        $MountResult = Mount-DiskImage -ImagePath $ISOPath -PassThru
        Start-Sleep -Seconds 3
        $DriveLetter = (Get-DiskImage -ImagePath $ISOPath | Get-Volume).DriveLetter
        Write-Host "    Da mount ISO vao o $DriveLetter." -ForegroundColor Green
        return $DriveLetter
    } catch {
        Write-Host "Loi khi mount ISO: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Apply-WinPE {
    param(
        [string]$ISODrive,
        [string]$TargetDrive
    )

    $BootWimPath = "${ISODrive}:\sources\boot.wim"
    if (-not (Test-Path $BootWimPath)) {
        Write-Host "Khong tim thay file boot.wim trong ISO." -ForegroundColor Red
        return $false
    }

    try {
        Write-Host "Dang ap dung WinPE image..." -ForegroundColor Yellow

        if (-not (Test-Path "${TargetDrive}:\")) {
            Write-Host "O $TargetDrive khong ton tai." -ForegroundColor Red
            return $false
        }

        $TargetPath = "${TargetDrive}:\"
        Write-Host "Ap dung boot.wim vao $TargetPath"
        Dism /Apply-Image /ImageFile:"$BootWimPath" /Index:1 /ApplyDir:"$TargetPath"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Da ap dung WinPE thanh cong." -ForegroundColor Green
            return $true
        } else {
            Write-Host "Loi khi ap dung WinPE. Ma loi: $LASTEXITCODE" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "Loi khi ap dung WinPE: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Setup-BootEnvironment {
    param([string]$TargetDrive)

    try {
        Write-Host "Dang thiet lap moi truong khoi dong..." -ForegroundColor Yellow

        if (-not (Test-Path "${TargetDrive}:\Windows")) {
            Write-Host "Thu muc Windows khong ton tai tren o $TargetDrive." -ForegroundColor Red
            return $false
        }

        $TargetPath = "${TargetDrive}:\Windows"
        & "$env:SystemRoot\System32\bcdboot.exe" $TargetPath
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Loi khi tao boot files. Ma loi: $LASTEXITCODE" -ForegroundColor Red
            return $false
        }

        & "$env:SystemRoot\System32\bcdedit.exe" /set "{current}" bootmenupolicy legacy | Out-Null
        Write-Host "Da thiet lap moi truong khoi dong thanh cong." -ForegroundColor Green
        return $true
    } catch {
        Write-Host "Loi khi thiet lap boot: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Pause-And-Exit {
    param([string]$Message)
    if ($Message) { Write-Host $Message -ForegroundColor Red }
    Read-Host "Nhan Enter de thoat..."
    exit
}

# ================== MAIN ==================
try {
    Write-Host "=== TAO O WINPE 10GB VA CAI BOOT ===`n" -ForegroundColor Cyan

# 1) Kiem tra / chuan bi o X:
if (-not (Check-XDriveAvailable)) { Pause-And-Exit "Loi: Khong the kiem tra hoac tao o X:" }

$XDriveExists = Get-Partition -DriveLetter X -ErrorAction SilentlyContinue
if ($XDriveExists) {
    Write-Host "O X: da ton tai, bo qua buoc tao o." -ForegroundColor Green
} else {
    Write-Host "`nDanh sach o co the thu nho:" -ForegroundColor Yellow
    $AvailableDrives = Get-AvailableDrives
    $AvailableDrives | Format-Table -AutoSize

    $SourceDrive = "C"
    $SelectedDrive = $AvailableDrives | Where-Object { $_.DriveLetter -eq $SourceDrive }
    
    if (-not $SelectedDrive) {
        Write-Host "Loi: Khong tim thay o C hoac o C khong ho tro (khong phai Fixed Disk)." -ForegroundColor Red
        Write-Host "Vui long kiem tra lai cau hinh o dia."
        Pause-And-Exit
    } else {
        Write-Host "    Da chon o mac dinh $SourceDrive - Dung luong trong: $($SelectedDrive.FreeSpaceGB) GB." -ForegroundColor Green
    }

    $SizeGB = 10
    Write-Host "        Dung luong o X: mac dinh = $SizeGB GB (10240 MB)." -ForegroundColor Cyan

    if ($SizeGB -gt $SelectedDrive.FreeSpaceGB) {
        Write-Host "Yeu cau $SizeGB GB vuot qua kha dung cua o $SourceDrive ($($SelectedDrive.FreeSpaceGB) GB)." -ForegroundColor Red
        Pause-And-Exit
    }

    if (-not (Create-XDrive -SourceDrive $SourceDrive -SizeGB $SizeGB)) {
        Write-Host "Khong the tiep tuc do loi tao o dia." -ForegroundColor Red
        Pause-And-Exit
    }
}

# 2) Dam bao ISO: WinPEFolder\winpe.iso
$isoDir  = Join-Path $scriptDir 'WinPEFolder'
$isoPath = Join-Path $isoDir  'winpe.iso'
$downloadUrl = 'https://github.com/ZitTechLab/WSAP/releases/download/ver1.0.7/winpe.iso'

$candidateIsoPaths = @(
    $isoPath,
    (Join-Path (Split-Path $scriptDir -Parent) 'Download\winpe.iso'),
    (Join-Path $scriptDir '..\Download\winpe.iso'),
    (Join-Path $scriptDir '..\..\..\Download\winpe.iso'),
    (Join-Path $scriptDir 'Download\winpe.iso')
)
foreach ($cand in $candidateIsoPaths) {
    if (Test-Path $cand) {
        $candSize = [math]::Round((Get-Item $cand).Length / 1MB, 2)
        if ($candSize -ge 10) {
            $isoPath = (Get-Item $cand).FullName
            Write-Host "    Da tim thay winpe.iso tai: $isoPath ($candSize MB)" -ForegroundColor Green
            break
        }
    }
}

Write-Host "        Duong dan ISO su dung: $isoPath" -ForegroundColor Cyan

if (-not (Test-Path (Split-Path $isoPath -Parent))) {
    New-Item -ItemType Directory -Path (Split-Path $isoPath -Parent) -Force | Out-Null
}

if (-not (Test-Path $isoPath)) {
    Write-Host "Khong tim thay winpe.iso -> bat dau tai ve..." -ForegroundColor Yellow
    try {
        try {
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
        } catch {
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        }

        $downloaded = $false

        if (Get-Command curl.exe -ErrorAction SilentlyContinue) {
            Write-Host "Dang tai ISO bang curl.exe..." -ForegroundColor Cyan
            & curl.exe -fL -o "$isoPath" "$downloadUrl"
            if ((Test-Path $isoPath) -and (Get-Item $isoPath).Length -ge 10MB) {
                $downloaded = $true
            } else {
                if (Test-Path $isoPath) { Remove-Item -Path $isoPath -Force -ErrorAction SilentlyContinue }
            }
        }

        if (-not $downloaded) {
            try {
                Write-Host "Dang tai ISO bang Start-BitsTransfer..." -ForegroundColor Cyan
                Start-BitsTransfer -Source $downloadUrl -Destination $isoPath -ErrorAction Stop
                if ((Test-Path $isoPath) -and (Get-Item $isoPath).Length -ge 10MB) {
                    $downloaded = $true
                } else {
                    if (Test-Path $isoPath) { Remove-Item -Path $isoPath -Force -ErrorAction SilentlyContinue }
                }
            } catch {
                Write-Host "BITS loi, chuyen sang Invoke-WebRequest..." -ForegroundColor DarkYellow
            }
        }

        if (-not $downloaded) {
            Write-Host "Dang tai ISO bang Invoke-WebRequest..." -ForegroundColor Cyan
            Invoke-WebRequest -Uri $downloadUrl -OutFile $isoPath -UseBasicParsing -ErrorAction Stop
            if ((Test-Path $isoPath) -and (Get-Item $isoPath).Length -ge 10MB) {
                $downloaded = $true
            } else {
                if (Test-Path $isoPath) { Remove-Item -Path $isoPath -Force -ErrorAction SilentlyContinue }
            }
        }

        if (-not (Test-Path $isoPath)) { throw "File ISO van khong ton tai sau khi tai." }
        $sizeMB = [math]::Round((Get-Item $isoPath).Length / 1MB, 2)
        if ($sizeMB -lt 10) { throw "File ISO qua nho ($sizeMB MB) - co the tai chua hoan chinh." }

        Write-Host "Da tai ISO thanh cong: $sizeMB MB." -ForegroundColor Green
    } catch {
        Pause-And-Exit "Loi tai ISO: $($_.Exception.Message)"
    }
}

# 3) Mount ISO, ap WinPE vao X:, tao boot
try {
    $isoDriveLetter = Mount-WindowsISO -ISOPath $isoPath
    if (-not $isoDriveLetter) {
        Write-Host "Khong mount duoc ISO." -ForegroundColor Red
        Pause-And-Exit
    }

    $bootWimPath = "$($isoDriveLetter):\sources\boot.wim"
    if (-not (Test-Path $bootWimPath)) {
        Write-Host "Khong tim thay \sources\boot.wim trong ISO." -ForegroundColor Red
        Dismount-DiskImage -ImagePath $isoPath -ErrorAction SilentlyContinue
        Pause-And-Exit
    }

    $volX = Get-Volume -DriveLetter X -ErrorAction SilentlyContinue
    if ($volX) {
        try {
            Format-Volume -DriveLetter X -FileSystem NTFS -NewFileSystemLabel 'ZitPE' -Confirm:$false -ErrorAction Stop

            Dism /Apply-Image /ImageFile:"$bootWimPath" /Index:1 /ApplyDir:"X:\"
            if ($LASTEXITCODE -ne 0) { throw "DISM apply-image failed voi ma $LASTEXITCODE" }

            $startnetSource = $null
            $startnetCandidates = @(
                (Join-Path $scriptDir 'startnet.cmd'),
                (Join-Path (Split-Path $scriptDir -Parent) 'Config\startnet.cmd'),
                (Join-Path $scriptDir '..\Warehouse\startnet.cmd'),
                (Join-Path $scriptDir 'Warehouse\startnet.cmd')
            )
            foreach ($sc in $startnetCandidates) {
                if (Test-Path $sc) { $startnetSource = (Get-Item $sc).FullName; break }
            }
            if ($startnetSource) {
                Copy-Item -Path $startnetSource -Destination "X:\Windows\System32\startnet.cmd" -Force
                Write-Host "    Da cap nhat X:\Windows\System32\startnet.cmd thanh cong." -ForegroundColor Green
            }

            Write-Host "    Dang tao boot entry voi bcdboot..." -ForegroundColor Cyan
            & "$env:SystemRoot\System32\bcdboot.exe" "X:\Windows" /f ALL

            if ($LASTEXITCODE -ne 0) { 
                Write-Host "    bcdboot gap loi ($LASTEXITCODE), chuyen sang phuong an tao boot thu cong bang bcdedit..." -ForegroundColor Yellow
                
                $desc = "ZitPE"
                $output = & "$env:SystemRoot\System32\bcdedit.exe" /copy "{current}" /d "$desc" 2>&1
                if ($output -match '{([a-f0-9-]{36})}') {
                    $guid = "{"+$matches[1]+"}"
                    Write-Host "    Da tao entry thu cong: $guid" -ForegroundColor Cyan
                    
                    & "$env:SystemRoot\System32\bcdedit.exe" /set "$guid" device partition=X:
                    & "$env:SystemRoot\System32\bcdedit.exe" /set "$guid" osdevice partition=X:
                    & "$env:SystemRoot\System32\bcdedit.exe" /set "$guid" systemroot \Windows
                    & "$env:SystemRoot\System32\bcdedit.exe" /set "$guid" detecthal on
                    & "$env:SystemRoot\System32\bcdedit.exe" /set "$guid" winpe yes
                    
                    & "$env:SystemRoot\System32\bcdedit.exe" /displayorder "$guid" /addlast
                    Write-Host "    Da them ZitPE vao menu boot thanh cong." -ForegroundColor Green
                } else {
                    throw "Khong the tao boot entry thu cong. Loi bcdedit copy: $output"
                }
            } else {
                & "$env:SystemRoot\System32\bcdedit.exe" /set "{current}" bootmenupolicy legacy | Out-Null
            }

            Write-Host "    Dang cau hinh boot mac dinh..." -ForegroundColor Cyan
            $bcdData = & "$env:SystemRoot\System32\bcdedit.exe" /enum
            $currentGuid = $null
            $foundX = $false
            
            foreach ($line in $bcdData) {
                if ($line -match '^identifier\s+({.+})') {
                    $currentGuid = $matches[1]
                }
                if ($line -match 'partition=X:') {
                    if ($currentGuid) {
                        $foundX = $true
                        break
                    }
                }
            }

            if ($foundX -and $currentGuid) {
                Write-Host "    Tim thay boot entry: $currentGuid" -ForegroundColor Cyan
                & "$env:SystemRoot\System32\bcdedit.exe" /set "$currentGuid" description "ZitPE"
                
                & "$env:SystemRoot\System32\bcdedit.exe" /default "{current}"
                & "$env:SystemRoot\System32\bcdedit.exe" /bootsequence "$currentGuid"
                & "$env:SystemRoot\System32\bcdedit.exe" /timeout 10
                Write-Host "    Da cau hinh: Boot vao WinPE (ZitPE) cho lan khoi dong ke tiep." -ForegroundColor Green
                Write-Host "    Mac dinh cac lan sau: Windows (Timeout 10s)." -ForegroundColor Green
            } else {
                Write-Host "    Canh bao: Khong tim thay entry cua o X: de set default." -ForegroundColor Yellow
            }

            Write-Host "Hoan tat cai WinPE vao o X:." -ForegroundColor Green
        } catch {
            Write-Host "Loi khi cai WinPE vao X:: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "Khong tim thay o X: de cai WinPE. Can tao/assign X: truoc." -ForegroundColor Yellow
    }

    Dismount-DiskImage -ImagePath $isoPath -ErrorAction SilentlyContinue
    } catch {
        Write-Host "Da xay ra loi: $($_.Exception.Message)" -ForegroundColor Red
        Dismount-DiskImage -ImagePath $isoPath -ErrorAction SilentlyContinue
        Pause-And-Exit
    }

} catch {
    Pause-And-Exit "Loi khong xac dinh (Main Crash): $($_.Exception.Message)"
}

for ($i = 5; $i -ge 1; $i--) {
    Write-Host ("Tu dong dong sau... {0}s" -f $i)
    Start-Sleep -Seconds 1
}
exit
