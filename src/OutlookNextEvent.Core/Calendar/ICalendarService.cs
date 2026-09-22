using OutlookNextEvent.Core.Models;

namespace OutlookNextEvent.Core.Calendar;

public interface ICalendarService
{
    Task<IReadOnlyList<NextEvent>> GetNextEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int maxResults,
        CancellationToken cancellationToken = default);
}
