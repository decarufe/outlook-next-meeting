namespace OutlookNextEvent.Testing.Time;

public sealed class FixedTimeProvider : TimeProvider
{
    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        UtcNow = utcNow.ToUniversalTime();
    }

    public DateTimeOffset UtcNow { get; private set; }

    public override DateTimeOffset GetUtcNow()
        => UtcNow;

    public void SetUtcNow(DateTimeOffset utcNow)
    {
        UtcNow = utcNow.ToUniversalTime();
    }

    public void Advance(TimeSpan offset)
    {
        UtcNow = UtcNow.Add(offset);
    }
}
