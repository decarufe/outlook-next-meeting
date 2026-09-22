namespace OutlookNextEvent.Infrastructure.Settings;

public sealed class AuthSettings
{
    public const string DefaultTenantId = "common";
    public const string DefaultScope = "Calendars.Read";

    public AuthSettings(
        string clientId,
        string tenantId = DefaultTenantId,
        IReadOnlyList<string>? scopes = null,
        string? redirectUri = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        ClientId = clientId;
        TenantId = tenantId;
        Scopes = scopes is { Count: > 0 } ? scopes : [DefaultScope];
        RedirectUri = string.IsNullOrWhiteSpace(redirectUri)
            ? CreateBrokerRedirectUri(clientId)
            : redirectUri;
    }

    public string ClientId { get; }

    public string TenantId { get; }

    public IReadOnlyList<string> Scopes { get; }

    public string RedirectUri { get; }

    public static string CreateBrokerRedirectUri(string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        return $"ms-appx-web://Microsoft.AAD.BrokerPlugin/{clientId}";
    }
}

public sealed class CalendarSettings
{
    public CalendarSettings(
        int lookaheadDays = 7,
        int maxEvents = 5,
        string? timeZoneId = null)
    {
        if (lookaheadDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lookaheadDays), "Lookahead must be at least one day.");
        }

        if (maxEvents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEvents), "Max events must be at least one.");
        }

        LookaheadDays = lookaheadDays;
        MaxEvents = maxEvents;
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId)
            ? TimeZoneInfo.Local.Id
            : timeZoneId;
    }

    public int LookaheadDays { get; }

    public int MaxEvents { get; }

    public string TimeZoneId { get; }
}

public sealed class OutlookNextEventSettings
{
    public OutlookNextEventSettings(
        string clientId,
        string tenantId = AuthSettings.DefaultTenantId,
        IReadOnlyList<string>? scopes = null,
        int lookAheadDays = 7,
        int maxEvents = 5,
        string? redirectUri = null,
        string? timeZoneId = null)
    {
        Auth = new AuthSettings(clientId, tenantId, scopes, redirectUri);
        Calendar = new CalendarSettings(lookAheadDays, maxEvents, timeZoneId);
    }

    public AuthSettings Auth { get; }

    public CalendarSettings Calendar { get; }

    public string ClientId => Auth.ClientId;

    public string TenantId => Auth.TenantId;

    public IReadOnlyList<string> Scopes => Auth.Scopes;

    public string RedirectUri => Auth.RedirectUri;

    public int LookAheadDays => Calendar.LookaheadDays;

    public int MaxEvents => Calendar.MaxEvents;
}
