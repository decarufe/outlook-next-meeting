namespace OutlookNextEvent.Testing.Time;

public static class TestClock
{
    public static readonly DateTimeOffset FixedUtcNow = new(2026, 9, 22, 16, 0, 0, TimeSpan.Zero);

    public static FixedTimeProvider Fixed()
        => new(FixedUtcNow);
}
