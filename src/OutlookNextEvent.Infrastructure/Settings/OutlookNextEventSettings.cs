namespace OutlookNextEvent.Infrastructure.Settings;

public sealed record OutlookNextEventSettings(
    string ClientId,
    int LookAheadDays = 7,
    int MaxEvents = 5);
