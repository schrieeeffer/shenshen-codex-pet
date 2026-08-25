using System.IO;
using System.Windows;
using ShenshenPet.Core;
using MessageBox = System.Windows.MessageBox;

namespace ShenshenPet.Windows;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Any(argument => string.Equals(argument, "--install-codex", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var target = CodexPetInstaller.Install(Path.Combine(AppContext.BaseDirectory, "codex"));
                MessageBox.Show(
                    $"已安装深深 Codex 桌宠：\n{target}\n\n请在设置 > Pets 中刷新并启用，然后输入 /pet 唤醒。",
                    "深深桌宠",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "安装失败", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            Shutdown();
            return;
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }
}
