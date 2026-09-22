using OutlookNextEvent.Core.Calendar;
using OutlookNextEvent.Core.Cards;
using OutlookNextEvent.Infrastructure.Settings;

namespace OutlookNextEvent.App.Widgets;

public sealed record WidgetUpdatePayload(
    string Template,
    string Data,
    string CustomState,
    bool IsPlaceholderContent = false);

public interface INextEventsWidgetContentProvider
{
    WidgetUpdatePayload BuildLoading();

    WidgetUpdatePayload BuildConnectionRequired();

    WidgetUpdatePayload BuildSignInRequested(bool companionWindowAvailable);

    WidgetUpdatePayload BuildError(Exception exception, bool retainedLastSuccessfulState);

    Task<WidgetUpdatePayload> BuildContentAsync(CancellationToken cancellationToken = default);
}

public sealed class NextEventsWidgetContentProvider : INextEventsWidgetContentProvider
{
    private readonly ICalendarService? _calendarService;
    private readonly CalendarSettings _calendarSettings;
    private readonly IEventShaper _eventShaper;
    private readonly TimeProvider _timeProvider;
    private readonly CardBuilder _cardBuilder;

    public NextEventsWidgetContentProvider(
        CardBuilder cardBuilder,
        ICalendarService? calendarService = null,
        CalendarSettings? calendarSettings = null,
        IEventShaper? eventShaper = null,
        TimeProvider? timeProvider = null)
    {
        _cardBuilder = cardBuilder ?? throw new ArgumentNullException(nameof(cardBuilder));
        _calendarService = calendarService;
        _calendarSettings = calendarSettings ?? new CalendarSettings();
        _eventShaper = eventShaper ?? new EventShaper(timeProvider);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public WidgetUpdatePayload BuildLoading()
        => ToPayload("loading", _cardBuilder.BuildLoading());

    public WidgetUpdatePayload BuildConnectionRequired()
        => ToPayload("connection-required", _cardBuilder.BuildSignedOut());

    public WidgetUpdatePayload BuildSignInRequested(bool companionWindowAvailable)
        => ToPayload("sign-in-requested", _cardBuilder.BuildSignInRequested(companionWindowAvailable));

    public WidgetUpdatePayload BuildError(Exception exception, bool retainedLastSuccessfulState)
        => ToPayload("error", _cardBuilder.BuildError(exception.Message, retainedLastSuccessfulState));

    public async Task<WidgetUpdatePayload> BuildContentAsync(CancellationToken cancellationToken = default)
    {
        if (_calendarService is null)
        {
            return BuildConnectionRequired();
        }

        var now = _timeProvider.GetUtcNow();
        var windowEnd = now.AddDays(_calendarSettings.LookaheadDays);
        var events = await _calendarService
            .GetNextEventsAsync(now, windowEnd, _calendarSettings.MaxEvents, cancellationToken)
            .ConfigureAwait(false);
        var shaped = _eventShaper.ShapeNextEvents(
            events,
            now,
            _calendarSettings.MaxEvents,
            ResolveTimeZone(_calendarSettings.TimeZoneId),
            windowEnd);

        return ToPayload("ready", _cardBuilder.BuildNextEvents(shaped));
    }

    private static WidgetUpdatePayload ToPayload(string customState, CardRenderResult card)
        => new(card.TemplateJson, card.DataJson, customState, card.IsPlaceholderContent);

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }
}
