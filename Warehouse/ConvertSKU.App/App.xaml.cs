using System;
using System.IO;
using System.Windows;

namespace ConvertSKU.App;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            var window = new MainWindow();
            window.Show();
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "startup-error.log");
            File.WriteAllText(logPath, ex.ToString());

            MessageBox.Show(
                $"App failed to start.\n\n{ex.Message}\n\nLog: {logPath}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }
}