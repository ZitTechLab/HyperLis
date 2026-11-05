# Kế hoạch Review Source Code: Tab General (Optimizer)

Dưới đây là sơ đồ cấu trúc mã nguồn cho tab "General" (được đặt tên là `universalTab` trong code) của dự án Optimizer.

## 1. Giao diện (UI Layout)
**File:** `Optimizer/Forms/MainForm.Designer.cs`
- **Container:** `universalTab` (Tab Page chính).
- **Control chính:** Các control dạng `ToggleCard` (ví dụ: `reportingSw`, `telemetryTasksSw`, `defenderSw`...) được thêm vào `universalTab`.

## 2. Logic & Sự kiện (Events)
**File:** `Optimizer/Forms/MainForm.cs`
- **Khởi tạo:** Phương thức `EnableToggleEvents` (gọi trong Constructor/Load) đăng ký sự kiện click.
  - Ví dụ: `reportingSw.ToggleClicked += new EventHandler(toggleSwitch8_Click);`
- **Xử lý sự kiện:** Các hàm `toggleSwitchX_Click` gọi tới lớp logic.
  - Ví dụ: `toggleSwitch8_Click` gọi `OptimizeHelper.DisableErrorReporting()`.

## 3. Thư viện Xử lý (Core Logic)
**File:** `Optimizer/OptimizeHelper.cs`
- Chứa các phương thức thực thi việc chỉnh sửa Registry, chạy lệnh CMD, hoặc Stop/Start Service.
- Các hàm liên quan General Tab:
  - `DisableErrorReporting`, `EnableErrorReporting`
  - `DisableTelemetryServices`, `EnableTelemetryServices`
  - `DisableDefender`, `EnableDefender`
  - ... và nhiều hàm khác tương ứng với từng toggle.

**File:** `Optimizer/Utilities.cs`
- Các hàm hỗ trợ cấp thấp:
  - `RunCommand(string command)`
  - `StopService(string serviceName)`
  - `StartService(string serviceName)`
  - `SetRegistryValue`...

---
> [!NOTE]
> Để review toàn bộ tính năng của tab General, bạn nên bắt đầu từ `MainForm.cs` để xem mapping giữa UI và hàm xử lý, sau đó đi sâu vào `OptimizeHelper.cs` để xem chi tiết lệnh thực thi.
