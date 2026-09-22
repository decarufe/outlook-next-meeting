using Xunit;
using OutlookNextEvent.Infrastructure.Settings;

namespace OutlookNextEvent.App.Tests;

public sealed class AppScaffoldTests
{
    [Fact]
    public void SettingsPlaceholder_UsesMvpDefaults()
    {
        var settings = new OutlookNextEventSettings("client-id");

        Assert.Equal(7, settings.LookAheadDays);
        Assert.Equal(5, settings.MaxEvents);
    }
}
