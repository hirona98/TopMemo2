using System.Windows;
using Application = System.Windows.Application;

namespace TopMemo2;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainWindow = new MainWindow();
        MainWindow = _mainWindow;
        _mainWindow.StartHidden();
    }
}
