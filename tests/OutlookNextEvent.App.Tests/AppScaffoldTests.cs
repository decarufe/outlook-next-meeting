using Xunit;
using OutlookNextEvent.Core.Auth;
using OutlookNextEvent.Infrastructure.Graph;
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
        Assert.Equal(["Calendars.Read"], settings.Scopes);
        Assert.Equal("ms-appx-web://Microsoft.AAD.BrokerPlugin/client-id", settings.RedirectUri);
    }

    [Fact]
    public async Task GraphAccessTokenProvider_DelegatesToAuthService()
    {
        var authService = new FakeAuthService("access-token");
        var provider = new GraphCalendarClient.AuthServiceAccessTokenProvider(authService);

        var token = await provider.GetAuthorizationTokenAsync(new Uri("https://graph.microsoft.com/v1.0/me/calendarView"));

        Assert.Equal("access-token", token);
        Assert.Equal(1, authService.SilentCalls);
    }

    private sealed class FakeAuthService(string token) : IAuthService
    {
        public int SilentCalls { get; private set; }

        public Task<string> AcquireTokenSilentAsync(CancellationToken cancellationToken = default)
        {
            SilentCalls++;
            return Task.FromResult(token);
        }

        public Task<string> SignInInteractiveAsync(nint parentWindowHandle, CancellationToken cancellationToken = default)
            => Task.FromResult(token);

        public Task SignOutAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
