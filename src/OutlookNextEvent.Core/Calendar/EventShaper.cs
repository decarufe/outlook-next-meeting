using OutlookNextEvent.Core.Models;

namespace OutlookNextEvent.Core.Calendar;

public sealed class EventShaper
{
    public IReadOnlyList<NextEvent> ShapeNextEvents(
        IEnumerable<NextEvent> events,
        DateTimeOffset now,
        int maxResults)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (maxResults <= 0)
        {
            return Array.Empty<NextEvent>();
        }

        return events
            .Where(nextEvent => nextEvent.EndsAt >= now)
            .OrderBy(nextEvent => nextEvent.StartsAt)
            .ThenBy(nextEvent => nextEvent.Subject, StringComparer.CurrentCulture)
            .Take(maxResults)
            .ToArray();
    }
}
