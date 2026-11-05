## Cải thiện “Cleaning progress” + tránh Not Responding

### Summary
Làm cho Cleaning chạy mượt khi số file lớn bằng cách (1) throttle cập nhật UI theo thời gian/thống kê, (2) chuyển các thao tác có thể block (Recycle Bin) sang background, và (3) hiển thị tiến trình dạng determinate `processed/total` theo từng phase (Delete items / Deep Temp / Update Cache / System Restore / Recycle Bin / RAM).

### Key Changes
- **Throttle progress callback khi xoá item**
  - Sửa `E:\GGAI\Design\Helpers\CleaningHelper.cs`: mở rộng `CleanAsync(...)` để nhận thêm `CancellationToken` và báo tiến trình dạng “đếm được” (ít nhất: `processed`, `total`, `currentName`).
  - Trong loop xoá, chỉ phát progress khi:
    - đủ `N` item (ví dụ mỗi 200 item) **hoặc**
    - quá `T` ms (ví dụ 200ms) kể từ lần update gần nhất.
- **UI progress determinate + ít dispatch**
  - Sửa `E:\GGAI\Design\Views\CleaningView.xaml.cs`:
    - Tính `total = previewItems.Count` (hoặc `nonBrowserItems.Count`) trước khi gọi `CleanAsync`.
    - Update progress bar theo `processed/total` (map vào dải % 0–50 hoặc 0–60 tuỳ bạn đang chia phase).
    - Thay `Dispatcher.BeginInvoke` “mỗi item” bằng update theo nhịp 200ms (hoặc khi nhận progress đã throttle sẵn).
    - Nội dung status: `Deleting (12345/90000): <file>` + tốc độ ước tính (items/s) nếu muốn.
- **Recycle Bin chạy background**
  - Sửa `E:\GGAI\Design\Views\CleaningView.xaml.cs`: thay gọi trực tiếp `EmptyRecycleBin()` bằng `await Task.Run(() => EmptyRecycleBin())` để UI không bị block.
  - Status trong lúc chạy: “Emptying Recycle Bin (may take a few minutes)…”.
- **Update Cache: tăng “cảm giác đang chạy”**
  - Giữ hành vi xoá cả folder như hiện tại (`Directory.Delete + recreate`).
  - Trước khi xoá, update status rõ ràng: “Stopping services… / Deleting SoftwareDistribution\\Download… / Starting services…”.
  - Đảm bảo mọi bước đều chạy trong background (đang có), và UI chỉ update theo phase (không cần per-file).
- **(Tuỳ chọn nhưng khuyến nghị) Nút Cancel**
  - Bổ sung `CancellationTokenSource` cho phiên cleaning và nút Cancel trên loading overlay.
  - `CleanAsync`, `ResetTempFolders`, Update Cache, System Restore tôn trọng cancel ở các điểm kiểm tra hợp lý.

### Test Plan (manual)
- Case A: Chỉ tick `Update Cache` với `SoftwareDistribution\\Download` rất lớn → UI không Not Responding, status chuyển phase hợp lý.
- Case B: Tick `Temporary Files + Error Reports + Minidumps` với hàng chục nghìn file → progress tăng đều, không lag do spam UI.
- Case C: Tick `Empty Recycle Bin` với Recycle Bin lớn → UI vẫn thao tác được (window không “đóng băng”).
- Case D: Tick combo bạn nêu (Temp + Minidumps + Error Reports + Recycle + Update Cache) → không còn đoạn đứng vài phút “không thấy gì”; status/percent vẫn cập nhật.
- Case E (nếu có Cancel): bấm Cancel khi đang xoá nhiều file → dừng trong thời gian hợp lý, không crash.

### Assumptions
- Loading overlay (`MainWindow.ShowLoading/UpdateLoadingProgress/HideLoading`) hỗ trợ cập nhật text + percent theo nhịp (không yêu cầu list realtime).
- “Update Cache” tiếp tục dùng cơ chế stop/start service + xoá `SoftwareDistribution\\Download` như hiện tại; chỉ cải thiện UX/progress và tránh block UI.
