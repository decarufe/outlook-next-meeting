using OutlookNextEvent.Core.Calendar;
using OutlookNextEvent.Core.Models;
using OutlookNextEvent.Testing.Calendar;
using OutlookNextEvent.Testing.Time;
using Xunit;

namespace OutlookNextEvent.Core.Tests;

public sealed class EventShaperTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

    [Fact]
    public void ShapeNextEvents_EmptyInput_ReturnsEmptyViewModel()
    {
        var shaper = new EventShaper(TestClock.Fixed());

        var shaped = shaper.ShapeNextEvents(NextEventFixtures.Empty, 5, Utc, TimeSpan.FromDays(7));

        Assert.True(shaped.IsEmpty);
        Assert.Equal(0, shaped.Count);
        Assert.Empty(shaped.Events);
        Assert.Equal(TestClock.FixedUtcNow, shaped.GeneratedAt);
        Assert.Equal(Utc.Id, shaped.TimeZoneId);
        Assert.Equal(TestClock.FixedUtcNow.AddDays(7), shaped.WindowEnd);
    }

    [Fact]
    public void ShapeNextEvents_OrdersByLocalStartWithDeterministicTieBreakers()
    {
        var shaper = new EventShaper(TestClock.Fixed());
        var tieStart = new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);
        var events = new[]
        {
            NextEventBuilder.Create()
                .WithId("later")
                .WithSubject("Later")
                .StartingAt(tieStart.AddHours(3))
                .Build(),
            NextEventBuilder.Create()
                .WithId("timed-beta")
                .WithSubject("Beta")
                .StartingAt(tieStart)
                .WithDuration(TimeSpan.FromHours(2))
                .Build(),
            NextEventBuilder.Create()
                .WithId("stable-first")
                .WithSubject("Same title")
                .StartingAt(tieStart.AddHours(1))
                .Build(),
            NextEventBuilder.Create()
                .WithId("all-day")
                .WithSubject("Zulu")
                .AsAllDay(new DateOnly(2026, 9, 23))
                .Build(),
            NextEventBuilder.Create()
                .WithId("timed-alpha")
                .WithSubject("Alpha")
                .StartingAt(tieStart)
                .WithDuration(TimeSpan.FromHours(2))
                .Build(),
            NextEventBuilder.Create()
                .WithId("stable-second")
                .WithSubject("Same title")
                .StartingAt(tieStart.AddHours(1))
                .Build()
        };

        var shaped = shaper.ShapeNextEvents(events, 10, Utc);

        Assert.Equal(
            ["all-day", "timed-alpha", "timed-beta", "stable-first", "stable-second", "later"],
            shaped.Events.Select(nextEvent => nextEvent.Id).ToArray());
    }

    [Fact]
    public void ShapeNextEvents_NormalizesCrossTimeZoneEventsForComparisonAndDisplay()
    {
        var shaper = new EventShaper(TestClock.Fixed());
        var events = new[]
        {
            NextEventBuilder.Create()
                .WithId("utc-later")
                .WithSubject("UTC later")
                .StartingAt(new DateTimeOffset(2026, 9, 22, 17, 0, 0, TimeSpan.Zero))
                .WithDuration(TimeSpan.FromMinutes(30))
                .Build(),
            NextEventFixtures.CrossTimeZone(),
            NextEventBuilder.Create()
                .WithId("central-earlier")
                .WithSubject("Central earlier")
                .StartingAt(new DateTimeOffset(2026, 9, 22, 11, 15, 0, TimeSpan.FromHours(-5)))
                .WithDuration(TimeSpan.FromMinutes(30))
                .Build()
        };

        var shaped = shaper.ShapeNextEvents(events, 5, Eastern);

        Assert.Equal(
            ["central-earlier", "cross-timezone", "utc-later"],
            shaped.Events.Select(nextEvent => nextEvent.Id).ToArray());

        var crossTimeZone = shaped.Events.Single(nextEvent => nextEvent.Id == "cross-timezone");
        Assert.Equal(Eastern.Id, shaped.TimeZoneId);
        Assert.Equal(new DateOnly(2026, 9, 22), crossTimeZone.StartDate);
        Assert.Equal(new TimeOnly(12, 30), crossTimeZone.StartTime);
        Assert.Equal(new TimeOnly(13, 15), crossTimeZone.EndTime);
        Assert.Equal("12:30–13:15", crossTimeZone.TimeText);
        Assert.Equal(TimeSpan.FromMinutes(45), crossTimeZone.Duration);
        Assert.Equal(TimeSpan.FromHours(-4), crossTimeZone.LocalStart.Offset);
        Assert.Equal(TimeSpan.FromHours(-4), crossTimeZone.LocalEnd.Offset);
    }

    [Fact]
    public void ShapeNextEvents_DaylightSavingFallBack_PreservesInstantDurationAndLocalOffsets()
    {
        var now = new DateTimeOffset(2026, 11, 1, 4, 0, 0, TimeSpan.Zero);
        var shaper = new EventShaper(new FixedTimeProvider(now));
        var events = new[]
        {
            NextEventBuilder.Create()
                .WithId("dst-fallback")
                .WithSubject("DST fallback")
                .StartingAt(new DateTimeOffset(2026, 11, 1, 1, 30, 0, TimeSpan.FromHours(-4)))
                .EndingAt(new DateTimeOffset(2026, 11, 1, 1, 30, 0, TimeSpan.FromHours(-5)))
                .Build()
        };

        var shaped = shaper.ShapeNextEvents(events, 5, Eastern);

        var nextEvent = Assert.Single(shaped.Events);
        Assert.Equal(new TimeOnly(1, 30), nextEvent.StartTime);
        Assert.Equal(new TimeOnly(1, 30), nextEvent.EndTime);
        Assert.Equal("01:30–01:30", nextEvent.TimeText);
        Assert.Equal(TimeSpan.FromHours(1), nextEvent.Duration);
        Assert.Equal(TimeSpan.FromHours(-4), nextEvent.LocalStart.Offset);
        Assert.Equal(TimeSpan.FromHours(-5), nextEvent.LocalEnd.Offset);
    }

    [Fact]
    public void ShapeNextEvents_AllDayEventsUseDatesOnlyAndSortBeforeTimedEventsAtSameStart()
    {
        var shaper = new EventShaper(TestClock.Fixed());
        var sameStart = new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);
        var events = new[]
        {
            NextEventBuilder.Create()
                .WithId("timed")
                .WithSubject("Timed midnight maintenance")
                .StartingAt(sameStart)
                .WithDuration(TimeSpan.FromHours(2))
                .Build(),
            NextEventFixtures.AllDay()
        };

        var shaped = shaper.ShapeNextEvents(events, 5, Utc);

        Assert.Equal(["all-day", "timed"], shaped.Events.Select(nextEvent => nextEvent.Id).ToArray());
        var allDay = shaped.Events[0];
        Assert.True(allDay.IsAllDay);
        Assert.Null(allDay.StartTime);
        Assert.Null(allDay.EndTime);
        Assert.Equal(new DateOnly(2026, 9, 23), allDay.StartDate);
        Assert.Equal(new DateOnly(2026, 9, 23), allDay.EndDate);
        Assert.Equal("All day", allDay.TimeText);
        Assert.Equal("Tomorrow", allDay.RelativeStatus);
    }

    [Fact]
    public void ShapeNextEvents_UpcomingFilter_IncludesStartingNowAndInProgressButExcludesEndedEvents()
    {
        var shaper = new EventShaper(TestClock.Fixed());
        var events = new[]
        {
            NextEventBuilder.Create()
                .WithId("ended-exactly-now")
                .WithSubject("Ended now")
                .StartingAt(TestClock.FixedUtcNow.AddMinutes(-30))
                .EndingAt(TestClock.FixedUtcNow)
                .Build(),
            NextEventFixtures.InProgress(),
            NextEventBuilder.Create()
                .WithId("starting-now")
                .WithSubject("Starting now")
                .StartingAt(TestClock.FixedUtcNow)
                .WithDuration(TimeSpan.FromMinutes(30))
                .Build(),
            NextEventBuilder.Create()
                .WithId("past")
                .WithSubject("Past")
                .StartingAt(TestClock.FixedUtcNow.AddHours(-2))
                .EndingAt(TestClock.FixedUtcNow.AddHours(-1))
                .Build()
        };

        var shaped = shaper.ShapeNextEvents(events, 5, Utc);

        Assert.Equal(["in-progress", "starting-now"], shaped.Events.Select(nextEvent => nextEvent.Id).ToArray());
        Assert.All(shaped.Events, nextEvent => Assert.Equal("In progress", nextEvent.RelativeStatus));
    }

    [Fact]
    public void ShapeNextEvents_UpcomingFilter_IncludesEventsStartingAtWindowEndOnly()
    {
        var shaper = new EventShaper(TestClock.Fixed());
        var windowEnd = TestClock.FixedUtcNow.AddDays(1);
        var events = new[]
        {
            NextEventBuilder.Create()
                .WithId("at-window-end")
                .WithSubject("At window end")
                .StartingAt(windowEnd)
                .WithDuration(TimeSpan.FromMinutes(30))
                .Build(),
            NextEventBuilder.Create()
                .WithId("after-window-end")
                .WithSubject("After window end")
                .StartingAt(windowEnd.AddTicks(1))
                .WithDuration(TimeSpan.FromMinutes(30))
                .Build()
        };

        var shaped = shaper.ShapeNextEvents(events, TestClock.FixedUtcNow, 5, Utc, windowEnd);

        var nextEvent = Assert.Single(shaped.Events);
        Assert.Equal("at-window-end", nextEvent.Id);
        Assert.Equal(windowEnd, shaped.WindowEnd);
    }

    [Fact]
    public void ShapeNextEvents_CancelledEvents_AreExcluded()
    {
        var shaper = new EventShaper(TestClock.Fixed());
        var events = new[]
        {
            NextEventFixtures.Cancelled(),
            NextEventFixtures.Single()
        };

        var shaped = shaper.ShapeNextEvents(events, 5, Utc);

        var nextEvent = Assert.Single(shaped.Events);
        Assert.Equal("single", nextEvent.Id);
    }

    [Fact]
    public void ShapeNextEvents_MaxCountCapsAfterFilteringAndSorting()
    {
        var shaper = new EventShaper(TestClock.Fixed());
        var events = new[]
        {
            NextEventBuilder.Create()
                .WithId("third")
                .StartingIn(TimeSpan.FromHours(3))
                .Build(),
            NextEventFixtures.Cancelled(),
            NextEventBuilder.Create()
                .WithId("first")
                .StartingIn(TimeSpan.FromMinutes(5))
                .Build(),
            NextEventBuilder.Create()
                .WithId("second")
                .StartingIn(TimeSpan.FromHours(1))
                .Build(),
            NextEventBuilder.Create()
                .WithId("fourth")
                .StartingIn(TimeSpan.FromHours(4))
                .Build()
        };

        var shaped = shaper.ShapeNextEvents(events, 2, Utc);

        Assert.Equal(2, shaped.Count);
        Assert.Equal(["first", "second"], shaped.Events.Select(nextEvent => nextEvent.Id).ToArray());
    }

    [Fact]
    public void ShapeNextEvents_MaxCountLargerThanAvailable_ReturnsAllAvailableEvents()
    {
        var shaper = new EventShaper(TestClock.Fixed());
        var events = new[]
        {
            NextEventBuilder.Create()
                .WithId("first")
                .StartingIn(TimeSpan.FromMinutes(10))
                .Build(),
            NextEventBuilder.Create()
                .WithId("second")
                .StartingIn(TimeSpan.FromMinutes(20))
                .Build()
        };

        var shaped = shaper.ShapeNextEvents(events, 5, Utc);

        Assert.Equal(2, shaped.Count);
        Assert.Equal(["first", "second"], shaped.Events.Select(nextEvent => nextEvent.Id).ToArray());
    }

    [Fact]
    public void ShapeNextEvents_MaxCountZero_ReturnsEmptyViewModel()
    {
        var shaper = new EventShaper(TestClock.Fixed());

        var shaped = shaper.ShapeNextEvents([NextEventFixtures.Single()], 0, Utc);

        Assert.True(shaped.IsEmpty);
        Assert.Empty(shaped.Events);
        Assert.Equal(TestClock.FixedUtcNow, shaped.GeneratedAt);
    }
}
