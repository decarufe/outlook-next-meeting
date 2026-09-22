using OutlookNextEvent.Core.Calendar;
using OutlookNextEvent.Core.Models;
using OutlookNextEvent.Core.Auth;
using OutlookNextEvent.Infrastructure.Settings;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Globalization;

namespace OutlookNextEvent.Infrastructure.Graph;

public sealed class GraphCalendarClient : ICalendarService
{
    private static readonly Uri GraphBaseUri = new("https://graph.microsoft.com/v1.0");

    private readonly GraphServiceClient _graphClient;
    private readonly CalendarSettings _settings;

    public GraphCalendarClient(IAuthService authService, OutlookNextEventSettings settings)
        : this(authService, settings.Calendar)
    {
    }

    public GraphCalendarClient(IAuthService authService, CalendarSettings? settings = null)
        : this(CreateGraphClient(authService), settings)
    {
    }

    internal GraphCalendarClient(GraphServiceClient graphClient, CalendarSettings? settings = null)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        _settings = settings ?? new CalendarSettings();
    }

    public Task<IReadOnlyList<NextEvent>> GetNextEventsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return GetNextEventsAsync(
            now,
            now.AddDays(_settings.LookaheadDays),
            _settings.MaxEvents,
            cancellationToken);
    }

    public async Task<IReadOnlyList<NextEvent>> GetNextEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        if (maxResults <= 0)
        {
            return Array.Empty<NextEvent>();
        }

        var response = await _graphClient.Me.CalendarView.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.StartDateTime = FormatGraphDateTime(from);
                requestConfiguration.QueryParameters.EndDateTime = FormatGraphDateTime(to);
                requestConfiguration.QueryParameters.Top = maxResults;
                requestConfiguration.Headers.Add("Prefer", $"outlook.timezone=\"{_settings.TimeZoneId}\"");
            },
            cancellationToken).ConfigureAwait(false);

        return response?.Value?
            .Select(MapEvent)
            .Where(nextEvent => nextEvent is not null)
            .Cast<NextEvent>()
            .ToArray()
            ?? Array.Empty<NextEvent>();
    }

    private static GraphServiceClient CreateGraphClient(IAuthService authService)
    {
        ArgumentNullException.ThrowIfNull(authService);
        return new GraphServiceClient(new BaseBearerTokenAuthenticationProvider(new AuthServiceAccessTokenProvider(authService)));
    }

    private static string FormatGraphDateTime(DateTimeOffset value)
        => value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);

    private static NextEvent? MapEvent(Event graphEvent)
    {
        if (graphEvent.Id is null || graphEvent.Start is null || graphEvent.End is null)
        {
            return null;
        }

        return new NextEvent(
            graphEvent.Id,
            graphEvent.Subject ?? string.Empty,
            ToDateTimeOffset(graphEvent.Start),
            ToDateTimeOffset(graphEvent.End),
            MapLocation(graphEvent),
            graphEvent.IsAllDay ?? false,
            TryCreateUri(graphEvent.OnlineMeeting?.JoinUrl ?? graphEvent.OnlineMeetingUrl),
            graphEvent.IsCancelled ?? false);
    }

    private static string? MapLocation(Event graphEvent)
    {
        if (!string.IsNullOrWhiteSpace(graphEvent.Location?.DisplayName))
        {
            return graphEvent.Location.DisplayName;
        }

        return graphEvent.Locations?.FirstOrDefault(location => !string.IsNullOrWhiteSpace(location.DisplayName))?.DisplayName;
    }

    private static DateTimeOffset ToDateTimeOffset(DateTimeTimeZone dateTimeTimeZone)
    {
        if (HasExplicitOffset(dateTimeTimeZone.DateTime)
            && DateTimeOffset.TryParse(
            dateTimeTimeZone.DateTime,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var dateTimeOffset))
        {
            return dateTimeOffset;
        }

        if (!DateTime.TryParse(
            dateTimeTimeZone.DateTime,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dateTime))
        {
            return DateTimeOffset.MinValue;
        }

        var timeZone = TryFindTimeZone(dateTimeTimeZone.TimeZone) ?? TimeZoneInfo.Local;
        var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, timeZone.GetUtcOffset(unspecified));
    }

    private static bool HasExplicitOffset(string? dateTime)
    {
        if (string.IsNullOrWhiteSpace(dateTime))
        {
            return false;
        }

        return dateTime.EndsWith('Z')
            || dateTime.LastIndexOf('+') > "yyyy-MM-dd".Length
            || dateTime.LastIndexOf('-') > "yyyy-MM-dd".Length;
    }

    private static TimeZoneInfo? TryFindTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    private static Uri? TryCreateUri(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;

    public sealed class AuthServiceAccessTokenProvider : IAccessTokenProvider
    {
        private readonly IAuthService _authService;

        public AuthServiceAccessTokenProvider(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public AllowedHostsValidator AllowedHostsValidator { get; } = new(["graph.microsoft.com"]);

        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            if (uri is not null && !GraphBaseUri.IsBaseOf(uri))
            {
                throw new InvalidOperationException($"Refusing to attach a Graph token to '{uri.Host}'.");
            }

            return _authService.AcquireTokenSilentAsync(cancellationToken);
        }
    }
}
