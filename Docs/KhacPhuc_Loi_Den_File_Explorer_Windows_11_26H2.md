# HƯỚNG DẪN XỬ LÝ LỖI ĐEN CỬA SỔ FILE EXPLORER TRÊN WINDOWS 11 26H2 / 24H2

## 1. Hiện tượng gặp phải
- Khi chạy script tối ưu (Extreme Gaming Optimization) trên các bản **Windows 11 24H2** hoặc **Windows 11 26H2** (Build 26100 trở lên):
  - Thanh tiêu đề và các tab phía trên (Tabs, Command Bar) vẫn hiển thị màu sáng (Light mode).
  - Khung nội dung chính (Danh sách ổ đĩa, thư mục) và thanh điều hướng bên trái (Navigation Pane) bị chuyển hoàn toàn sang màu **đen kịt (`#000000`)**.
  - Tên ổ đĩa, nhãn chữ bị chìm/khó đọc do chữ màu tối hiển thị trên nền đen.

---

## 2. Nguyên nhân cốt lõi (Root Cause)

### a. Sự thay đổi kiến trúc của File Explorer trên Windows 11 24H2/26H2:
- Ở các phiên bản Windows cũ (Windows 10, Windows 11 21H2/22H2), File Explorer được xây dựng trên nền tảng **Win32 DirectUI**.
- Từ phiên bản **Windows 11 24H2 / 26H2**, Microsoft đã tái cấu trúc và hiện đại hóa File Explorer bằng công nghệ **WinUI 3 / XAML Islands**.

### b. Xung đột với thiết lập Visual Effects:
- Trong các script tối ưu truyền thống, để ép máy đạt hiệu năng tối đa cho gaming thường áp dụng Registry tweak:
  ```reg
  [HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects]
  "VisualFXSetting"=dword:00000003
  ```
- Giá trị `VisualFXSetting = 3` tương ứng với chế độ **"Adjust for best performance"** (Tối ưu hiệu suất) trong `sysdm.cpl`.
- **Hậu quả trên Windows 11 24H2/26H2**: Khi bật chế độ này, hệ thống sẽ tắt các thành phần Desktop Composition & Visual Styles. Lớp khung XAML Islands bên trong `explorer.exe` không thể nạp được cọ vẽ nền (Background Brush / Mica / Acrylic) và sẽ chuyển thành màu đen kịt (`#000000`). Ngoài ra việc tắt `ListviewAlphaSelect = 0` và `DragFullWindows = 0` cũng gây xung đột trực tiếp với cơ chế dựng hình WinUI.

---

## 3. Cách khắc phục ngay lập tức (Immediate Fix)

Nếu máy tính hoặc máy ảo của bạn đang bị đen màn hình Explorer, bạn chỉ cần mở **CMD (Command Prompt)** hoặc **PowerShell** (quyền Admin hoặc User đều được) và chạy lệnh sau:

### Lệnh chạy trên CMD:
```cmd
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v "VisualFXSetting" /t REG_DWORD /d 0 /f
reg add "HKCU\Control Panel\Desktop" /v "DragFullWindows" /t REG_SZ /d 1 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v "ListviewAlphaSelect" /t REG_DWORD /d 1 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v "ListviewShadow" /t REG_DWORD /d 1 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" /v "EnableTransparency" /t REG_DWORD /d 1 /f
taskkill /F /IM explorer.exe & start explorer.exe
```

### Lệnh chạy trên PowerShell:
```powershell
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects' -Name 'VisualFXSetting' -Value 0
Set-ItemProperty -Path 'HKCU:\Control Panel\Desktop' -Name 'DragFullWindows' -Value '1'
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'ListviewAlphaSelect' -Value 1
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'ListviewShadow' -Value 1
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize' -Name 'EnableTransparency' -Value 1
Stop-Process -Name explorer -Force; Start-Process explorer
```

Sau khi Explorer khởi động lại, nền cửa sổ sẽ lập tức trở lại màu sáng đồng bộ và hiển thị bình thường.

---

## 4. Giải pháp đã cập nhật trong mã nguồn HyperLis

Trong mã nguồn của HyperLis (`HyperLisOptimization.cs` và `OptimizationHelper.cs`), hệ thống đã được cập nhật cơ chế nhận diện phiên bản hệ điều hành tự động:

1. **Trên Windows 11 (`Utilities.IsWindows11`)**:
   - **Không ép `VisualFXSetting = 3`**: Giữ giá trị mặc định `VisualFXSetting = 0` (để Windows tự quản lý lớp XAML WinUI 3).
   - **Giữ `DragFullWindows = 1` và `ListviewAlphaSelect = 1`**: Đảm bảo các thuộc tính đồ họa nền tảng của File Explorer không bị triệt tiêu.
   - Vẫn tối ưu đầy đủ các thành phần phục vụ gaming: Giảm trễ menu (`MenuShowDelay = 200`), tắt hoạt ảnh thu phóng nặng (`MinAnimate = 0`), tắt animation taskbar, ưu tiên GPU/CPU lập lịch game, tối ưu mạng và dịch vụ.

2. **Trên Windows 10**:
   - Vẫn áp dụng tối ưu `VisualFXSetting = 3` bình thường vì File Explorer Win32 trên Windows 10 không bị lỗi mất cọ vẽ nền.

---
*Tài liệu được tạo tự động bởi HyperLis Development Team.*
