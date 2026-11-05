using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Threading.Tasks;
using HyperLisUI.Helpers;

namespace HyperLisUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        // 2. Bắt các ngoại lệ chưa được xử lý ở cấp độ AppDomain (Lỗi Thread, lỗi nạp DLL...)
        AppDomain.CurrentDomain.UnhandledException += (s, args) => 
        {
            Logger.LogCrash(args.ExceptionObject as Exception, "AppDomain.UnhandledException");
        };
        
        // 3. Bắt các ngoại lệ trên luồng UI chính (Dispatcher)
        this.DispatcherUnhandledException += (s, args) => 
        {
            Logger.LogCrash(args.Exception, "DispatcherUnhandledException");
            args.Handled = true; // Cố gắng ngăn crash ứng dụng ngay lập tức
        };

        // 4. Bắt lỗi từ các Task bất đồng bộ (Background Tasks) bị bỏ sót
        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Logger.LogCrash(args.Exception, "TaskScheduler.UnobservedTaskException");
            args.SetObserved();
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Vô hiệu hóa Hardware Acceleration (GPU Render) của WPF
        // Cứu cánh cho thiết bị dùng CPU/VGA AMD lỗi driver
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        
        base.OnStartup(e);

        // Khởi động trình tự động kiểm tra bản cập nhật
        CheckForUpdatesAsync();
    }

    private async void CheckForUpdatesAsync()
    {
        // Đợi 3s để UI chính kịp load, tránh làm chậm ứng dụng khi mở
        await Task.Delay(3000);

        try
        {
            var updateService = new HyperLisUI.Services.UpdateService();
            var updateInfo = await updateService.CheckForUpdateAsync();

            if (updateInfo != null)
            {
                // Kiểm tra xem người dùng đã chọn Bỏ qua phiên bản này chưa
                if (AppConfigManager.Config.SkippedVersion == updateInfo.Version)
                {
                    return; // Đã bỏ qua bản này
                }

                // Kiểm tra xem người dùng có chọn Nhắc lại sau không
                if (AppConfigManager.Config.RemindDate.HasValue && AppConfigManager.Config.RemindDate.Value > DateTime.Now)
                {
                    return; // Chưa đến ngày nhắc lại
                }

                Dispatcher.Invoke(() =>
                {
                    var updateWindow = new HyperLisUI.Views.Components.UpdateWindow(updateInfo, updateService);
                    updateWindow.ShowDialog();
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogCrash(ex, "App.CheckForUpdatesAsync");
        }
    }
}
