namespace OutlookNextEvent.Core.Models;

public sealed record NextEvent(
    string Id,
    string Subject,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? Location,
    bool IsAllDay,
    Uri? JoinUrl);
