using Xunit;
using OutlookNextEvent.Core.Calendar;
using OutlookNextEvent.Core.Models;

namespace OutlookNextEvent.Core.Tests;

public sealed class EventShaperTests
{
    [Fact]
    public void ShapeNextEvents_ReturnsUpcomingEventsInStartOrder()
    {
        var now = new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);
        var shaper = new EventShaper();
        var events = new[]
        {
            new NextEvent("later", "Later", now.AddHours(2), now.AddHours(3), null, false, null),
            new NextEvent("past", "Past", now.AddHours(-2), now.AddHours(-1), null, false, null),
            new NextEvent("next", "Next", now.AddMinutes(30), now.AddHours(1), null, false, null)
        };

        var shaped = shaper.ShapeNextEvents(events, now, 2);

        Assert.Collection(
            shaped.Events,
            nextEvent => Assert.Equal("next", nextEvent.Id),
            nextEvent => Assert.Equal("later", nextEvent.Id));
    }

    [Fact]
    public void ShapeNextEvents_ExcludesCancelledEvents()
    {
        var now = new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);
        var shaper = new EventShaper();
        var events = new[]
        {
            new NextEvent("cancelled", "Cancelled", now.AddMinutes(15), now.AddMinutes(45), null, false, null, true),
            new NextEvent("active", "Active", now.AddMinutes(30), now.AddHours(1), null, false, null)
        };

        var shaped = shaper.ShapeNextEvents(events, now, 5);

        var nextEvent = Assert.Single(shaped.Events);
        Assert.Equal("active", nextEvent.Id);
    }

    [Fact]
    public void ShapeNextEvents_ConvertsTimesToTargetTimeZone()
    {
        var now = new DateTimeOffset(2026, 9, 22, 16, 0, 0, TimeSpan.Zero);
        var shaper = new EventShaper();
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var events = new[]
        {
            new NextEvent(
                "tz",
                "Time zone",
                new DateTimeOffset(2026, 9, 22, 18, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 22, 19, 0, 0, TimeSpan.Zero),
                null,
                false,
                null)
        };

        var shaped = shaper.ShapeNextEvents(events, now, 5, eastern);

        var nextEvent = Assert.Single(shaped.Events);
        Assert.Equal(eastern.Id, shaped.TimeZoneId);
        Assert.Equal(new TimeOnly(14, 0), nextEvent.StartTime);
        Assert.Equal("14:00–15:00", nextEvent.TimeText);
    }

    [Fact]
    public void ShapeNextEvents_AllDayEventsExposeDatesWithoutTimes()
    {
        var now = new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);
        var shaper = new EventShaper();
        var events = new[]
        {
            new NextEvent(
                "all-day",
                "Conference",
                new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 24, 0, 0, 0, TimeSpan.Zero),
                null,
                true,
                null)
        };

        var shaped = shaper.ShapeNextEvents(events, now, 5, TimeZoneInfo.Utc);

        var nextEvent = Assert.Single(shaped.Events);
        Assert.True(nextEvent.IsAllDay);
        Assert.Null(nextEvent.StartTime);
        Assert.Null(nextEvent.EndTime);
        Assert.Equal(new DateOnly(2026, 9, 23), nextEvent.StartDate);
        Assert.Equal(new DateOnly(2026, 9, 23), nextEvent.EndDate);
        Assert.Equal("All day", nextEvent.TimeText);
    }
}
