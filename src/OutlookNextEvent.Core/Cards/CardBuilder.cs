using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using OutlookNextEvent.Core.Models;

namespace OutlookNextEvent.Core.Cards;

public sealed record CardRenderResult(
    string TemplateJson,
    string DataJson = CardBuilder.EmptyDataJson,
    bool IsPlaceholderContent = false);

public sealed class CardBuilder
{
    public const string EmptyDataJson = "{}";

    private const int MaxRenderedEvents = 5;
    private static readonly Uri AdaptiveCardSchema = new("http://adaptivecards.io/schemas/adaptive-card.json");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public CardRenderResult BuildLoading()
        => Render(
            [
                Header(),
                Text("Chargement des événements…", weight: "Bolder"),
                Text("Mise à jour du calendrier Outlook.", isSubtle: true)
            ],
            [RefreshAction()],
            isPlaceholderContent: true);

    public CardRenderResult BuildSignedOut()
        => Render(
            [
                Header(),
                Text("Connexion Outlook requise", weight: "Bolder"),
                Text("Connectez Outlook dans la fenêtre compagnon pour afficher vos prochains événements.", wrap: true)
            ],
            [ConnectAction(), RefreshAction()]);

    public CardRenderResult BuildSignInRequested(bool companionWindowAvailable)
        => Render(
            [
                Header(),
                Text(
                    companionWindowAvailable ? "Fenêtre de connexion ouverte" : "Connexion Outlook à finaliser",
                    weight: "Bolder"),
                Text(
                    companionWindowAvailable
                        ? "Terminez la connexion dans la fenêtre compagnon, puis actualisez le widget."
                        : "La fenêtre compagnon portera le flux interactif dès que l'intégration WinUI/HWND sera branchée.",
                    wrap: true)
            ],
            [ReconnectAction(), RefreshAction()]);

    public CardRenderResult BuildError(string message, bool retainedLastSuccessfulState)
        => Render(
            [
                Header(),
                Text("Impossible d'actualiser", weight: "Bolder", color: "Attention"),
                Text(
                    retainedLastSuccessfulState
                        ? "Le dernier état valide est conservé en mémoire."
                        : "Les événements ne peuvent pas être chargés pour le moment.",
                    wrap: true),
                Text(TrimForCard(message), isSubtle: true, wrap: true, maxLines: 2)
            ],
            [RefreshAction(), ReconnectAction()]);

    public CardRenderResult BuildNextEvents(NextEventsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        return viewModel.IsEmpty
            ? BuildEmpty(viewModel)
            : BuildEventList(viewModel);
    }

    public string BuildPlaceholderCardJson()
        => BuildLoading().TemplateJson;

    private static CardRenderResult BuildEmpty(NextEventsViewModel viewModel)
    {
        var details = viewModel.WindowEnd is null
            ? "Aucun événement à venir dans la fenêtre configurée."
            : $"Aucun événement à venir jusqu'au {FormatDate(viewModel.WindowEnd.Value)}.";

        return Render(
            [
                Header(),
                Text("Aucun événement à venir", weight: "Bolder"),
                Text(details, wrap: true),
                Text(FormatGeneratedAt(viewModel.GeneratedAt), isSubtle: true)
            ],
            [RefreshAction()]);
    }

    private static CardRenderResult BuildEventList(NextEventsViewModel viewModel)
    {
        var body = new List<object>
        {
            Header(),
            Text("Prochains événements", weight: "Bolder"),
            Text(FormatGeneratedAt(viewModel.GeneratedAt), isSubtle: true, spacing: "None")
        };

        foreach (var nextEvent in viewModel.Events.Take(MaxRenderedEvents))
        {
            body.Add(EventRow(nextEvent, viewModel.GeneratedAt));
        }

        return Render(body, [RefreshAction()]);
    }

    private static CardRenderResult Render(
        IReadOnlyList<object> body,
        IReadOnlyList<ActionExecute> actions,
        bool isPlaceholderContent = false)
    {
        var card = new AdaptiveCard(body, actions);
        return new CardRenderResult(JsonSerializer.Serialize(card, JsonOptions), EmptyDataJson, isPlaceholderContent);
    }

    private static TextBlock Header()
        => Text("Outlook Next Event", size: "Medium", weight: "Bolder", spacing: "None", style: "heading");

    private static ColumnSet EventRow(NextEventViewModel nextEvent, DateTimeOffset generatedAt)
    {
        var detailItems = new List<object>
        {
            Text(nextEvent.Title, weight: "Bolder", wrap: true, maxLines: 2, spacing: "None"),
            Text(FormatEventStatus(nextEvent, generatedAt), isSubtle: true, wrap: true, spacing: "None", maxLines: 1)
        };

        if (!string.IsNullOrWhiteSpace(nextEvent.Location))
        {
            detailItems.Add(Text(nextEvent.Location.Trim(), isSubtle: true, wrap: true, spacing: "None", maxLines: 1));
        }

        return new ColumnSet(
            [
                new Column(
                    "auto",
                    [
                        Text(FormatEventTime(nextEvent), weight: "Bolder", wrap: true, maxLines: 2, spacing: "None")
                    ]),
                new Column("stretch", detailItems)
            ],
            Separator: true,
            Spacing: "Small");
    }

    private static TextBlock Text(
        string text,
        string? size = null,
        string? weight = null,
        string? color = null,
        bool? wrap = null,
        bool? isSubtle = null,
        string? spacing = null,
        int? maxLines = null,
        string? style = null)
        => new(text, size, weight, color, wrap, isSubtle, spacing, maxLines, style);

    private static ActionExecute RefreshAction()
        => new("Actualiser", "refresh");

    private static ActionExecute ConnectAction()
        => new("Connecter Outlook", "connect");

    private static ActionExecute ReconnectAction()
        => new("Reconnecter Outlook", "reconnect");

    private static string FormatEventTime(NextEventViewModel nextEvent)
    {
        if (nextEvent.IsAllDay)
        {
            return "Toute la journée";
        }

        return nextEvent.TimeText;
    }

    private static string FormatEventStatus(NextEventViewModel nextEvent, DateTimeOffset generatedAt)
    {
        if (nextEvent.IsAllDay)
        {
            return nextEvent.StartDate == DateOnly.FromDateTime(generatedAt.Date)
                ? "Aujourd'hui"
                : FormatDate(nextEvent.LocalStart);
        }

        if (generatedAt >= nextEvent.LocalStart && generatedAt < nextEvent.LocalEnd)
        {
            return "En cours";
        }

        var untilStart = nextEvent.LocalStart - generatedAt;
        if (untilStart < TimeSpan.FromMinutes(1))
        {
            return "Commence maintenant";
        }

        if (untilStart < TimeSpan.FromHours(1))
        {
            return $"Dans {(int)Math.Ceiling(untilStart.TotalMinutes)} min";
        }

        if (nextEvent.LocalStart.Date == generatedAt.Date)
        {
            return $"Aujourd'hui à {nextEvent.LocalStart:HH:mm}";
        }

        if (nextEvent.LocalStart.Date == generatedAt.Date.AddDays(1))
        {
            return $"Demain à {nextEvent.LocalStart:HH:mm}";
        }

        return FormatDate(nextEvent.LocalStart);
    }

    private static string FormatGeneratedAt(DateTimeOffset generatedAt)
        => $"Mis à jour à {generatedAt:HH:mm}";

    private static string FormatDate(DateTimeOffset value)
        => value.ToString("dd/MM", CultureInfo.InvariantCulture);

    private static string TrimForCard(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Erreur inconnue.";
        }

        var normalized = message.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= 140 ? normalized : string.Concat(normalized.AsSpan(0, 137), "...");
    }

    private sealed record AdaptiveCard(
        IReadOnlyList<object> Body,
        IReadOnlyList<ActionExecute> Actions)
    {
        [JsonPropertyName("$schema")]
        public Uri Schema { get; init; } = AdaptiveCardSchema;

        public string Type { get; init; } = "AdaptiveCard";

        public string Version { get; init; } = "1.5";
    }

    private sealed record TextBlock(
        string Text,
        string? Size = null,
        string? Weight = null,
        string? Color = null,
        bool? Wrap = null,
        bool? IsSubtle = null,
        string? Spacing = null,
        int? MaxLines = null,
        string? Style = null)
    {
        public string Type { get; init; } = "TextBlock";
    }

    private sealed record ColumnSet(
        IReadOnlyList<Column> Columns,
        bool? Separator = null,
        string? Spacing = null)
    {
        public string Type { get; init; } = "ColumnSet";
    }

    private sealed record Column(
        string Width,
        IReadOnlyList<object> Items)
    {
        public string Type { get; init; } = "Column";
    }

    private sealed record ActionExecute(string Title, string Verb)
    {
        public string Type { get; init; } = "Action.Execute";
    }
}
