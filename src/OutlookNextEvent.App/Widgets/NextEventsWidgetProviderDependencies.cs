using OutlookNextEvent.App.Cards;
using OutlookNextEvent.Core.Calendar;

namespace OutlookNextEvent.App.Widgets;

public sealed class NextEventsWidgetProviderDependencies
{
    public NextEventsWidgetProviderDependencies(
        IWidgetHost widgetHost,
        INextEventsWidgetContentProvider contentProvider,
        ICompanionSignInLauncher signInLauncher)
    {
        WidgetHost = widgetHost ?? throw new ArgumentNullException(nameof(widgetHost));
        ContentProvider = contentProvider ?? throw new ArgumentNullException(nameof(contentProvider));
        SignInLauncher = signInLauncher ?? throw new ArgumentNullException(nameof(signInLauncher));
    }

    public IWidgetHost WidgetHost { get; }

    public INextEventsWidgetContentProvider ContentProvider { get; }

    public ICompanionSignInLauncher SignInLauncher { get; }

    public static NextEventsWidgetProviderDependencies CreateDefault()
        => new(
            new WidgetManagerHost(),
            new NextEventsWidgetContentProvider(new CardBuilder(), eventShaper: new EventShaper()),
            new CompanionSignInLauncher());
}
