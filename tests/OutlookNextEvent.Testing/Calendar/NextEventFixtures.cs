using OutlookNextEvent.Core.Models;
using OutlookNextEvent.Testing.Time;

namespace OutlookNextEvent.Testing.Calendar;

public static class NextEventFixtures
{
    public static IReadOnlyList<NextEvent> Empty { get; } = Array.Empty<NextEvent>();

    public static NextEvent Single()
        => NextEventBuilder.Create()
            .WithId("single")
            .WithSubject("One upcoming meeting")
            .StartingIn(TimeSpan.FromMinutes(30))
            .WithDuration(TimeSpan.FromMinutes(30))
            .Build();

    public static IReadOnlyList<NextEvent> Many()
        =>
        [
            NextEventBuilder.Create()
                .WithId("later")
                .WithSubject("Later meeting")
                .StartingIn(TimeSpan.FromHours(3))
                .Build(),
            NextEventBuilder.Create()
                .WithId("next")
                .WithSubject("Next meeting")
                .StartingIn(TimeSpan.FromMinutes(15))
                .Build(),
            AllDay(),
            CrossTimeZone()
        ];

    public static NextEvent AllDay()
        => NextEventBuilder.Create()
            .WithId("all-day")
            .WithSubject("Conference")
            .AsAllDay(new DateOnly(2026, 9, 23))
            .Build();

    public static NextEvent Cancelled()
        => NextEventBuilder.Create()
            .WithId("cancelled")
            .WithSubject("Cancelled meeting")
            .StartingIn(TimeSpan.FromMinutes(45))
            .Cancelled()
            .Build();

    public static NextEvent CrossTimeZone()
    {
        var pacificStart = new DateTimeOffset(2026, 9, 22, 9, 30, 0, TimeSpan.FromHours(-7));
        return NextEventBuilder.Create()
            .WithId("cross-timezone")
            .WithSubject("Pacific partner sync")
            .StartingAt(pacificStart)
            .WithDuration(TimeSpan.FromMinutes(45))
            .AtLocation("Remote")
            .Build();
    }

    public static NextEvent InProgress()
        => NextEventBuilder.Create()
            .WithId("in-progress")
            .WithSubject("Already started")
            .StartingAt(TestClock.FixedUtcNow.AddMinutes(-10))
            .EndingAt(TestClock.FixedUtcNow.AddMinutes(20))
            .Build();
}
