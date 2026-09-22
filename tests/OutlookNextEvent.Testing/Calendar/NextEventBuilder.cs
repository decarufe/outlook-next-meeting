using OutlookNextEvent.Core.Models;
using OutlookNextEvent.Testing.Time;

namespace OutlookNextEvent.Testing.Calendar;

public sealed class NextEventBuilder
{
    private string _id = "meeting";
    private string _subject = "Planning meeting";
    private DateTimeOffset _startsAt = TestClock.FixedUtcNow.AddMinutes(30);
    private DateTimeOffset _endsAt = TestClock.FixedUtcNow.AddHours(1);
    private string? _location = "Salle Ada";
    private bool _isAllDay;
    private Uri? _joinUrl = new("https://teams.example.test/meet");
    private bool _isCancelled;

    public static NextEventBuilder Create()
        => new();

    public NextEventBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public NextEventBuilder WithSubject(string subject)
    {
        _subject = subject;
        return this;
    }

    public NextEventBuilder StartingAt(DateTimeOffset startsAt)
    {
        _startsAt = startsAt;
        if (_endsAt <= _startsAt)
        {
            _endsAt = _startsAt.AddMinutes(30);
        }

        return this;
    }

    public NextEventBuilder StartingIn(TimeSpan offset)
        => StartingAt(TestClock.FixedUtcNow.Add(offset));

    public NextEventBuilder WithDuration(TimeSpan duration)
    {
        _endsAt = _startsAt.Add(duration);
        return this;
    }

    public NextEventBuilder EndingAt(DateTimeOffset endsAt)
    {
        _endsAt = endsAt;
        return this;
    }

    public NextEventBuilder AtLocation(string? location)
    {
        _location = location;
        return this;
    }

    public NextEventBuilder WithJoinUrl(Uri? joinUrl)
    {
        _joinUrl = joinUrl;
        return this;
    }

    public NextEventBuilder AsAllDay(DateOnly date, int days = 1, TimeSpan? offset = null)
    {
        var zoneOffset = offset ?? TimeSpan.Zero;
        _startsAt = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), zoneOffset);
        _endsAt = _startsAt.AddDays(days);
        _isAllDay = true;
        _joinUrl = null;
        return this;
    }

    public NextEventBuilder Cancelled()
    {
        _isCancelled = true;
        return this;
    }

    public NextEvent Build()
        => new(
            _id,
            _subject,
            _startsAt,
            _endsAt,
            _location,
            _isAllDay,
            _joinUrl,
            _isCancelled);
}
