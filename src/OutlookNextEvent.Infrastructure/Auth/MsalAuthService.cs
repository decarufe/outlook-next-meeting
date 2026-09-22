using OutlookNextEvent.Core.Auth;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using OutlookNextEvent.Infrastructure.Settings;

namespace OutlookNextEvent.Infrastructure.Auth;

public sealed class MsalAuthService : IAuthService
{
    private readonly IPublicClientApplication _application;
    private readonly string[] _scopes;
    private IntPtr _currentParentWindowHandle;

    public MsalAuthService(OutlookNextEventSettings settings, Func<IntPtr>? parentWindowHandleProvider = null)
        : this(settings.Auth, parentWindowHandleProvider)
    {
    }

    public MsalAuthService(AuthSettings settings, Func<IntPtr>? parentWindowHandleProvider = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _scopes = settings.Scopes.ToArray();
        var hwndProvider = parentWindowHandleProvider ?? (() => _currentParentWindowHandle);

        _application = PublicClientApplicationBuilder
            .Create(settings.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, settings.TenantId)
            .WithRedirectUri(settings.RedirectUri)
            .WithParentActivityOrWindow(hwndProvider)
            .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
            {
                Title = "OutlookNextEvent"
            })
            .Build();
    }

    internal MsalAuthService(IPublicClientApplication application, AuthSettings settings)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(settings);

        _application = application;
        _scopes = settings.Scopes.ToArray();
    }

    public async Task<string> AcquireTokenSilentAsync(CancellationToken cancellationToken = default)
    {
        var account = await GetCachedAccountAsync(cancellationToken).ConfigureAwait(false)
            ?? PublicClientApplication.OperatingSystemAccount;

        var result = await _application
            .AcquireTokenSilent(_scopes, account)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return result.AccessToken;
    }

    public async Task<string> SignInInteractiveAsync(nint parentWindowHandle, CancellationToken cancellationToken = default)
    {
        if (parentWindowHandle == 0)
        {
            throw new ArgumentException("Interactive MSAL sign-in requires a companion window HWND.", nameof(parentWindowHandle));
        }

        _currentParentWindowHandle = (IntPtr)parentWindowHandle;

        try
        {
            return await AcquireTokenSilentAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (MsalUiRequiredException)
        {
            var result = await _application
                .AcquireTokenInteractive(_scopes)
                .WithParentActivityOrWindow((IntPtr)parentWindowHandle)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.AccessToken;
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _application.GetAccountsAsync().ConfigureAwait(false);

        foreach (var account in accounts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _application.RemoveAsync(account).ConfigureAwait(false);
        }
    }

    private async Task<IAccount?> GetCachedAccountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var accounts = await _application.GetAccountsAsync().ConfigureAwait(false);
        return accounts.FirstOrDefault();
    }
}
