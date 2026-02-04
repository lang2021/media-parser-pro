using System.Configuration;
using System.IO;
using System.Windows;

namespace MediaParser.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 全局异常处理
        Current.DispatcherUnhandledException += (sender, args) =>
        {
            MessageBox.Show($"未处理的异常: {args.Exception.Message}\n\n详细信息:\n{args.Exception}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"未处理的异常: {ex.Message}\n\n详细信息:\n{ex}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        try
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动失败: {ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
