using OutlookNextEvent.Core.Auth;

namespace OutlookNextEvent.Infrastructure.Auth;

public sealed class MsalAuthService : IAuthService
{
    public Task<string> AcquireTokenSilentAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Silent MSAL/WAM authentication will be implemented in the auth integration work.");
    }

    public Task<string> SignInInteractiveAsync(nint parentWindowHandle, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Interactive sign-in must be launched from the companion window HWND.");
    }
}
