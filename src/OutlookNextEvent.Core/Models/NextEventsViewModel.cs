namespace OutlookNextEvent.Core.Models;

public sealed record NextEventsViewModel(
    DateTimeOffset GeneratedAt,
    string TimeZoneId,
    IReadOnlyList<NextEventViewModel> Events,
    DateTimeOffset? WindowEnd = null)
{
    public int Count => Events.Count;

    public bool IsEmpty => Events.Count == 0;

    public static NextEventsViewModel Empty(DateTimeOffset generatedAt, TimeZoneInfo timeZone)
        => new(generatedAt, timeZone.Id, Array.Empty<NextEventViewModel>());
}

public sealed record NextEventViewModel(
    string Id,
    string Title,
    DateTimeOffset LocalStart,
    DateTimeOffset LocalEnd,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    bool IsAllDay,
    string TimeText,
    string RelativeStatus,
    TimeSpan Duration,
    string? Location,
    Uri? JoinUrl)
{
    public bool SpansMultipleDays => EndDate > StartDate;
}
