using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace ImageDedup.UI;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        _serviceProvider = new ServiceCollection()
            .AddSingleton<MainWindow>()
            .AddLogging(configure => configure
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole())
            .BuildServiceProvider();
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
