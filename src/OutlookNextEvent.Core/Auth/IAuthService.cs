namespace OutlookNextEvent.Core.Auth;

public interface IAuthService
{
    Task<string> AcquireTokenSilentAsync(CancellationToken cancellationToken = default);

    Task<string> SignInInteractiveAsync(nint parentWindowHandle, CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}
