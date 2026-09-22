namespace OutlookNextEvent.App.Widgets;

public interface ICompanionSignInLauncher
{
    Task<bool> RequestSignInAsync(string widgetId, CancellationToken cancellationToken = default);
}

public sealed class CompanionSignInLauncher : ICompanionSignInLauncher
{
    public Task<bool> RequestSignInAsync(string widgetId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // The interactive MSAL call must be made later by the WinUI companion window,
        // which can provide a real HWND to MsalAuthService.SignInInteractiveAsync.
        return Task.FromResult(false);
    }
}
