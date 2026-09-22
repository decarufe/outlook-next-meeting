using Microsoft.UI.Xaml;
using OutlookNextEvent.App.Widgets;

namespace OutlookNextEvent.App;

public partial class App : Application
{
    private readonly WidgetProviderComServer _widgetProviderComServer = new();
    private Window? _window;

    public App()
    {
        InitializeComponent();
        _widgetProviderComServer.Register();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
