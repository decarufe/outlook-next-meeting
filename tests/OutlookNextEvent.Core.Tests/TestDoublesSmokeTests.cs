using OutlookNextEvent.Core.Calendar;
using OutlookNextEvent.Testing.Auth;
using OutlookNextEvent.Testing.Calendar;
using OutlookNextEvent.Testing.Time;
using Xunit;

namespace OutlookNextEvent.Core.Tests;

public sealed class TestDoublesSmokeTests
{
    [Fact]
    public async Task FakeAuthService_WithToken_ReturnsTokenAndRecordsCalls()
    {
        var auth = FakeAuthService.WithToken("token");

        var token = await auth.AcquireTokenSilentAsync();
        await auth.SignOutAsync();

        Assert.Equal("token", token);
        Assert.Equal(1, auth.SilentCalls);
        Assert.Equal(1, auth.SignOutCalls);
        Assert.False(auth.IsSignedIn);
    }

    [Fact]
    public async Task FakeAuthService_SignedOut_ThrowsUntilInteractiveSignIn()
    {
        var auth = FakeAuthService.SignedOut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => auth.AcquireTokenSilentAsync());
        var token = await auth.SignInInteractiveAsync(123);

        Assert.Equal(FakeAuthService.DefaultToken, token);
        Assert.Equal(1, auth.SilentCalls);
        Assert.Equal(1, auth.InteractiveCalls);
        Assert.Equal(123, Assert.Single(auth.InteractiveParentWindowHandles));
        Assert.True(auth.IsSignedIn);
    }

    [Fact]
    public async Task FakeGraphCalendarClient_Many_ReturnsDeterministicFixturesAndRecordsRequest()
    {
        var calendar = FakeGraphCalendarClient.Many();
        var from = TestClock.FixedUtcNow;
        var to = from.AddDays(7);

        var events = await calendar.GetNextEventsAsync(from, to, 5);

        Assert.Contains(events, nextEvent => nextEvent.Id == "all-day");
        Assert.Contains(events, nextEvent => nextEvent.Id == "cross-timezone");
        var request = Assert.Single(calendar.Requests);
        Assert.Equal(from, request.From);
        Assert.Equal(to, request.To);
        Assert.Equal(5, request.MaxResults);
    }

    [Fact]
    public async Task FakeGraphCalendarClient_Throwing_SimulatesGraphFailure()
    {
        var calendar = FakeGraphCalendarClient.Throwing(new InvalidOperationException("Graph unavailable."));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => calendar.GetNextEventsAsync(TestClock.FixedUtcNow, TestClock.FixedUtcNow.AddDays(1), 5));

        Assert.Equal("Graph unavailable.", exception.Message);
        Assert.Single(calendar.Requests);
    }

    [Fact]
    public void FixedTimeProvider_CanDriveEventShaperDeterministically()
    {
        var clock = TestClock.Fixed();
        var shaper = new EventShaper(clock);

        var shaped = shaper.ShapeNextEvents(FakeGraphCalendarClient.Single().Events, 5, TimeZoneInfo.Utc);

        Assert.Equal(TestClock.FixedUtcNow, shaped.GeneratedAt);
        Assert.Equal("single", Assert.Single(shaped.Events).Id);
    }
}
