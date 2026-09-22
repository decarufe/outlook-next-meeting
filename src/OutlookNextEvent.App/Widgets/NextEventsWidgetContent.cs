using System.Text.Json;
using OutlookNextEvent.App.Cards;
using OutlookNextEvent.Core.Calendar;
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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICalendarService? _calendarService;
    private readonly CalendarSettings _calendarSettings;
    private readonly CardBuilder _cardBuilder;

    public NextEventsWidgetContentProvider(
        CardBuilder cardBuilder,
        ICalendarService? calendarService = null,
        CalendarSettings? calendarSettings = null)
    {
        _cardBuilder = cardBuilder ?? throw new ArgumentNullException(nameof(cardBuilder));
        _calendarService = calendarService;
        _calendarSettings = calendarSettings ?? new CalendarSettings();
    }

    public WidgetUpdatePayload BuildLoading()
        => Build("Chargement des événements…", "Le provider prépare la mise à jour du widget.", "loading", isPlaceholder: true);

    public WidgetUpdatePayload BuildConnectionRequired()
        => Build(
            "Connexion Outlook requise.",
            "Utilisez le bouton Connecter Outlook. Le flux interactif s'ouvrira dans la fenêtre compagnon, pas dans le callback du widget.",
            "connection-required",
            isPlaceholder: true);

    public WidgetUpdatePayload BuildSignInRequested(bool companionWindowAvailable)
        => Build(
            companionWindowAvailable ? "Fenêtre de connexion ouverte." : "Connexion Outlook à finaliser.",
            companionWindowAvailable
                ? "Terminez la connexion dans la fenêtre compagnon, puis actualisez le widget."
                : "Le déclencheur de fenêtre compagnon est prêt; l'intégration WinUI/HWND finale reste à brancher.",
            "sign-in-requested",
            isPlaceholder: true);

    public WidgetUpdatePayload BuildError(Exception exception, bool retainedLastSuccessfulState)
        => Build(
            retainedLastSuccessfulState
                ? "Impossible d'actualiser; dernier état conservé."
                : "Impossible de charger les événements.",
            exception.Message,
            "error",
            isPlaceholder: true);

    public async Task<WidgetUpdatePayload> BuildContentAsync(CancellationToken cancellationToken = default)
    {
        if (_calendarService is null)
        {
            return BuildConnectionRequired();
        }

        var now = DateTimeOffset.UtcNow;
        var events = await _calendarService
            .GetNextEventsAsync(now, now.AddDays(_calendarSettings.LookaheadDays), _calendarSettings.MaxEvents, cancellationToken)
            .ConfigureAwait(false);

        // #9 owns sorting/time-zone shaping and #10 owns final Adaptive Card rendering.
        return Build(
            events.Count == 0 ? "Aucun événement à venir." : $"{events.Count} événement(s) chargé(s).",
            "Rendu temporaire du provider; EventShaper et CardBuilder final se brancheront dans les issues #9/#10.",
            "ready",
            eventCount: events.Count,
            isPlaceholder: true);
    }

    private WidgetUpdatePayload Build(
        string status,
        string detail,
        string customState,
        int? eventCount = null,
        bool isPlaceholder = false)
    {
        var data = JsonSerializer.Serialize(new
        {
            status,
            detail,
            eventCount
        }, JsonOptions);

        return new WidgetUpdatePayload(_cardBuilder.BuildPlaceholderCardJson(), data, customState, isPlaceholder);
    }
}
