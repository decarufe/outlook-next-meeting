using OutlookNextEvent.Core.Auth;

namespace OutlookNextEvent.Testing.Auth;

public sealed class FakeAuthService : IAuthService
{
    public const string DefaultToken = "fake-access-token";

    private readonly string _token;
    private readonly Exception? _silentException;
    private readonly Exception? _interactiveException;

    private FakeAuthService(
        string token,
        Exception? silentException = null,
        Exception? interactiveException = null,
        bool isSignedIn = true)
    {
        _token = token;
        _silentException = silentException;
        _interactiveException = interactiveException;
        IsSignedIn = isSignedIn;
    }

    public bool IsSignedIn { get; private set; }

    public int SilentCalls { get; private set; }

    public int InteractiveCalls { get; private set; }

    public int SignOutCalls { get; private set; }

    public IReadOnlyList<nint> InteractiveParentWindowHandles => _interactiveParentWindowHandles;

    private readonly List<nint> _interactiveParentWindowHandles = [];

    public static FakeAuthService WithToken(string token = DefaultToken)
        => new(token);

    public static FakeAuthService SignedOut(Exception? silentException = null)
        => new(
            DefaultToken,
            silentException ?? new InvalidOperationException("Interactive sign-in is required."),
            isSignedIn: false);

    public static FakeAuthService TokenExpired(Exception? silentException = null)
        => new(
            DefaultToken,
            silentException ?? new InvalidOperationException("The cached token is expired."),
            isSignedIn: false);

    public static FakeAuthService InteractiveFailure(Exception? interactiveException = null)
        => new(
            DefaultToken,
            new InvalidOperationException("Interactive sign-in is required."),
            interactiveException: interactiveException ?? new InvalidOperationException("Interactive sign-in failed."),
            isSignedIn: false);

    public Task<string> AcquireTokenSilentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SilentCalls++;

        if (_silentException is not null)
        {
            throw _silentException;
        }

        IsSignedIn = true;
        return Task.FromResult(_token);
    }

    public Task<string> SignInInteractiveAsync(nint parentWindowHandle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        InteractiveCalls++;
        _interactiveParentWindowHandles.Add(parentWindowHandle);

        if (_interactiveException is not null)
        {
            throw _interactiveException;
        }

        IsSignedIn = true;
        return Task.FromResult(_token);
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SignOutCalls++;
        IsSignedIn = false;
        return Task.CompletedTask;
    }
}
