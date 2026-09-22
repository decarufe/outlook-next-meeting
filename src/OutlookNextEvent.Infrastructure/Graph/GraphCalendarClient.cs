using OutlookNextEvent.Core.Calendar;
using OutlookNextEvent.Core.Models;

namespace OutlookNextEvent.Infrastructure.Graph;

public sealed class GraphCalendarClient : ICalendarService
{
    public Task<IReadOnlyList<NextEvent>> GetNextEventsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Graph calendar integration will be implemented after the scaffold is in place.");
    }
}
