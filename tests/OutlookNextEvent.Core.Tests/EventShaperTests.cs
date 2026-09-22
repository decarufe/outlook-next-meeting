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
            shaped,
            nextEvent => Assert.Equal("next", nextEvent.Id),
            nextEvent => Assert.Equal("later", nextEvent.Id));
    }
}
