using OutlookNextEvent.Core.Models;

namespace OutlookNextEvent.Core.Calendar;

public interface IEventShaper
{
    NextEventsViewModel ShapeNextEvents(
        IEnumerable<NextEvent> events,
        DateTimeOffset now,
        int maxResults,
        TimeZoneInfo? targetTimeZone = null,
        DateTimeOffset? windowEnd = null);
}

public sealed class EventShaper : IEventShaper
{
    private static readonly TimeSpan ZeroDuration = TimeSpan.Zero;

    private readonly TimeProvider _clock;

    public EventShaper(TimeProvider? clock = null)
    {
        _clock = clock ?? TimeProvider.System;
    }

    public NextEventsViewModel ShapeNextEvents(
        IEnumerable<NextEvent> events,
        int maxResults,
        TimeZoneInfo? targetTimeZone = null,
        TimeSpan? lookahead = null)
    {
        var now = _clock.GetUtcNow();
        DateTimeOffset? windowEnd = lookahead is null ? null : now.Add(lookahead.Value);

        return ShapeNextEvents(events, now, maxResults, targetTimeZone, windowEnd);
    }

    public NextEventsViewModel ShapeNextEvents(
        IEnumerable<NextEvent> events,
        DateTimeOffset now,
        int maxResults,
        TimeZoneInfo? targetTimeZone = null,
        DateTimeOffset? windowEnd = null)
    {
        ArgumentNullException.ThrowIfNull(events);

        var timeZone = targetTimeZone ?? TimeZoneInfo.Local;
        if (maxResults <= 0)
        {
            return NextEventsViewModel.Empty(TimeZoneInfo.ConvertTime(now, timeZone), timeZone);
        }

        var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
        DateTimeOffset? localWindowEnd = windowEnd is null ? null : TimeZoneInfo.ConvertTime(windowEnd.Value, timeZone);
        var shapedEvents = events
            .Where(nextEvent => IsUpcoming(nextEvent, now, windowEnd))
            .Select(nextEvent => ShapeEvent(nextEvent, localNow, timeZone))
            .OrderBy(nextEvent => nextEvent.LocalStart)
            .ThenBy(nextEvent => nextEvent.IsAllDay ? 0 : 1)
            .ThenBy(nextEvent => nextEvent.Title, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToArray();

        return new NextEventsViewModel(localNow, timeZone.Id, shapedEvents, localWindowEnd);
    }

    private static bool IsUpcoming(NextEvent nextEvent, DateTimeOffset now, DateTimeOffset? windowEnd)
    {
        if (nextEvent.IsCancelled || nextEvent.EndsAt <= now)
        {
            return false;
        }

        return windowEnd is null || nextEvent.StartsAt <= windowEnd.Value;
    }

    private static NextEventViewModel ShapeEvent(
        NextEvent nextEvent,
        DateTimeOffset localNow,
        TimeZoneInfo timeZone)
    {
        var localStart = TimeZoneInfo.ConvertTime(nextEvent.StartsAt, timeZone);
        var localEnd = TimeZoneInfo.ConvertTime(nextEvent.EndsAt, timeZone);
        var startDate = DateOnly.FromDateTime(localStart.DateTime);
        var endDate = GetDisplayEndDate(nextEvent.IsAllDay, localStart, localEnd);
        TimeOnly? startTime = nextEvent.IsAllDay ? null : TimeOnly.FromDateTime(localStart.DateTime);
        TimeOnly? endTime = nextEvent.IsAllDay ? null : TimeOnly.FromDateTime(localEnd.DateTime);

        return new NextEventViewModel(
            nextEvent.Id,
            NormalizeTitle(nextEvent.Subject),
            localStart,
            localEnd,
            startDate,
            endDate,
            startTime,
            endTime,
            nextEvent.IsAllDay,
            FormatTimeText(nextEvent.IsAllDay, localStart, localEnd),
            FormatRelativeStatus(nextEvent.IsAllDay, localStart, localEnd, localNow),
            DurationBetween(localStart, localEnd),
            nextEvent.Location,
            nextEvent.JoinUrl);
    }

    private static DateOnly GetDisplayEndDate(bool isAllDay, DateTimeOffset localStart, DateTimeOffset localEnd)
    {
        var endDate = DateOnly.FromDateTime(localEnd.DateTime);
        if (isAllDay && localEnd.TimeOfDay == TimeSpan.Zero && localEnd > localStart)
        {
            return endDate.AddDays(-1);
        }

        return endDate;
    }

    private static string NormalizeTitle(string subject)
        => string.IsNullOrWhiteSpace(subject) ? "(Sans titre)" : subject.Trim();

    private static string FormatTimeText(bool isAllDay, DateTimeOffset localStart, DateTimeOffset localEnd)
    {
        if (isAllDay)
        {
            return "All day";
        }

        if (localStart.Date == localEnd.Date)
        {
            return $"{localStart:HH:mm}–{localEnd:HH:mm}";
        }

        return $"{localStart:yyyy-MM-dd HH:mm}–{localEnd:yyyy-MM-dd HH:mm}";
    }

    private static string FormatRelativeStatus(
        bool isAllDay,
        DateTimeOffset localStart,
        DateTimeOffset localEnd,
        DateTimeOffset localNow)
    {
        if (localNow >= localStart && localNow < localEnd)
        {
            return isAllDay ? "All day today" : "In progress";
        }

        var untilStart = localStart - localNow;
        if (untilStart <= TimeSpan.Zero)
        {
            return "Upcoming";
        }

        if (untilStart < TimeSpan.FromMinutes(1))
        {
            return "Starts now";
        }

        if (untilStart < TimeSpan.FromHours(1))
        {
            return $"Starts in {(int)Math.Ceiling(untilStart.TotalMinutes)} min";
        }

        if (localStart.Date == localNow.Date)
        {
            return $"Starts in {(int)Math.Ceiling(untilStart.TotalHours)} h";
        }

        return localStart.Date == localNow.Date.AddDays(1)
            ? "Tomorrow"
            : "Upcoming";
    }

    private static TimeSpan DurationBetween(DateTimeOffset localStart, DateTimeOffset localEnd)
    {
        var duration = localEnd - localStart;
        return duration < ZeroDuration ? ZeroDuration : duration;
    }
}
