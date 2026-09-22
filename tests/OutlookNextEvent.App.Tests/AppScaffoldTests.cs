using System.Xml.Linq;
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

    [Fact]
    public void WidgetTemplate_ExposesRefreshAndConnectActions()
    {
        var template = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "OutlookNextEvent.App",
            "Cards",
            "next-events.template.json"));

        Assert.Contains("\"verb\": \"refresh\"", template);
        Assert.Contains("\"verb\": \"connect\"", template);
        Assert.Contains("${status}", template);
        Assert.Contains("${detail}", template);
    }

    [Fact]
    public void WidgetProvider_DoesNotLaunchInteractiveMsalFromWidgetCallback()
    {
        var providerSource = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "OutlookNextEvent.App",
            "Widgets",
            "NextEventsWidgetProvider.cs"));

        Assert.Contains("ICompanionSignInLauncher", providerSource);
        Assert.DoesNotContain("AcquireTokenInteractive", providerSource);
        Assert.DoesNotContain("SignInInteractiveAsync", providerSource);
    }

    [Fact]
    public void PackageManifest_RegistersNextEventsWidgetProviderForMsixSideload()
    {
        var repoRoot = FindRepoRoot();
        var manifest = XDocument.Load(Path.Combine(repoRoot, "src", "OutlookNextEvent.App", "Package.appxmanifest"));
        var providerSource = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "OutlookNextEvent.App",
            "Widgets",
            "NextEventsWidgetProvider.cs"));

        XNamespace manifestNs = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        XNamespace uap3 = "http://schemas.microsoft.com/appx/manifest/uap/windows10/3";
        XNamespace com = "http://schemas.microsoft.com/appx/manifest/com/windows10";
        XNamespace rescap = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";

        var identity = manifest.Root!.Element(manifestNs + "Identity");
        Assert.Equal("Decarufe.OutlookNextEvent", identity?.Attribute("Name")?.Value);
        Assert.Equal("CN=OutlookNextEventDev", identity?.Attribute("Publisher")?.Value);

        var classId = manifest.Descendants(com + "Class").Single().Attribute("Id")?.Value;
        Assert.False(string.IsNullOrWhiteSpace(classId));
        Assert.Contains($"[Guid(\"{classId}\")]", providerSource);

        var appExtension = manifest.Descendants(uap3 + "AppExtension").Single();
        Assert.Equal("com.microsoft.windows.widgets", appExtension.Attribute("Name")?.Value);

        var createInstance = manifest.Descendants(manifestNs + "CreateInstance").Single();
        Assert.Equal(classId, createInstance.Attribute("ClassId")?.Value);

        var widgetDefinition = manifest.Descendants(manifestNs + "Definition").Single();
        Assert.Equal("NextEventsWidget", widgetDefinition.Attribute("Id")?.Value);
        Assert.Equal("false", widgetDefinition.Attribute("AllowMultiple")?.Value);

        var sizes = manifest.Descendants(manifestNs + "Size")
            .Select(size => size.Attribute("Name")?.Value ?? string.Empty)
            .OrderBy(size => size)
            .ToArray();
        Assert.Equal(["large", "medium", "small"], sizes);

        var capabilities = manifest.Root.Element(manifestNs + "Capabilities");
        Assert.NotNull(capabilities);
        Assert.Contains(
            capabilities.Elements(manifestNs + "Capability"),
            capability => capability.Attribute("Name")?.Value == "internetClient");
        Assert.Contains(
            capabilities.Elements(rescap + "Capability"),
            capability => capability.Attribute("Name")?.Value == "runFullTrust");
    }

    [Fact]
    public void PackagingDocumentation_ExplainsDevCertificateAndSideload()
    {
        var documentation = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "packaging.md"));

        Assert.Contains("CN=OutlookNextEventDev", documentation);
        Assert.Contains("New-SelfSignedCertificate", documentation);
        Assert.Contains("Add-AppxPackage", documentation);
        Assert.Contains("com.microsoft.windows.widgets", documentation);
        Assert.Contains("8F3D7C7B-8D0D-41BF-8D9E-608A70D03E95", documentation);
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

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OutlookNextEvent.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root.");
    }
}
