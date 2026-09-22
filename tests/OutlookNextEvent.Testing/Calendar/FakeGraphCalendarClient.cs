using OutlookNextEvent.Core.Calendar;
using OutlookNextEvent.Core.Models;

namespace OutlookNextEvent.Testing.Calendar;

public sealed class FakeGraphCalendarClient : IGraphCalendarClient
{
    private readonly Exception? _exception;

    private FakeGraphCalendarClient(IReadOnlyList<NextEvent> events, Exception? exception = null)
    {
        Events = events;
        _exception = exception;
    }

    public IReadOnlyList<NextEvent> Events { get; }

    public IReadOnlyList<CalendarRequest> Requests => _requests;

    private readonly List<CalendarRequest> _requests = [];

    public static FakeGraphCalendarClient Empty()
        => new(NextEventFixtures.Empty);

    public static FakeGraphCalendarClient Single()
        => new([NextEventFixtures.Single()]);

    public static FakeGraphCalendarClient Many()
        => new(NextEventFixtures.Many());

    public static FakeGraphCalendarClient AllDay()
        => new([NextEventFixtures.AllDay()]);

    public static FakeGraphCalendarClient Cancelled()
        => new([NextEventFixtures.Cancelled()]);

    public static FakeGraphCalendarClient CrossTimeZone()
        => new([NextEventFixtures.CrossTimeZone()]);

    public static FakeGraphCalendarClient WithEvents(params NextEvent[] events)
        => new(events);

    public static FakeGraphCalendarClient Throwing(Exception exception)
        => new([], exception);

    public Task<IReadOnlyList<NextEvent>> GetNextEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _requests.Add(new CalendarRequest(from, to, maxResults));

        if (_exception is not null)
        {
            throw _exception;
        }

        return Task.FromResult(Events);
    }
}

public sealed record CalendarRequest(DateTimeOffset From, DateTimeOffset To, int MaxResults);
