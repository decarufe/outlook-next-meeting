using System.Text.Json;
using OutlookNextEvent.Core.Calendar;
using OutlookNextEvent.Core.Cards;
using OutlookNextEvent.Core.Models;
using OutlookNextEvent.Testing.Calendar;
using OutlookNextEvent.Testing.Time;
using Xunit;

namespace OutlookNextEvent.App.Tests;

public sealed class CardBuilderTests
{
    [Fact]
    public void BuildLoading_RendersValidPlaceholderWithRefreshAction()
    {
        var builder = new CardBuilder();

        var card = builder.BuildLoading();

        Assert.Equal(CardBuilder.EmptyDataJson, card.DataJson);
        Assert.True(card.IsPlaceholderContent);
        using var document = JsonDocument.Parse(card.TemplateJson);
        AssertAdaptiveCard(document);
        Assert.Equal(["refresh"], ReadVerbs(document));
        Assert.Equal(["Actualiser"], ReadActionTitles(document));
        AssertTextContains(document, "Outlook Next Event");
        AssertTextContains(document, "Chargement des événements…");
        AssertTextContains(document, "Mise à jour du calendrier Outlook.");
    }

    [Fact]
    public void BuildSignedOut_RendersConnectActionForCompanionSignInPath()
    {
        var builder = new CardBuilder();

        var card = builder.BuildSignedOut();

        Assert.Equal(CardBuilder.EmptyDataJson, card.DataJson);
        Assert.False(card.IsPlaceholderContent);
        using var document = JsonDocument.Parse(card.TemplateJson);
        AssertAdaptiveCard(document);
        Assert.Equal(["connect", "refresh"], ReadVerbs(document));
        Assert.Equal(["Connecter Outlook", "Actualiser"], ReadActionTitles(document));
        AssertTextContains(document, "Connexion Outlook requise");
        AssertTextContains(document, "Connectez Outlook dans la fenêtre compagnon pour afficher vos prochains événements.");
    }

    [Theory]
    [InlineData(true, "Fenêtre de connexion ouverte", "Terminez la connexion dans la fenêtre compagnon, puis actualisez le widget.")]
    [InlineData(false, "Connexion Outlook à finaliser", "La fenêtre compagnon portera le flux interactif dès que l'intégration WinUI/HWND sera branchée.")]
    public void BuildSignInRequested_RendersCompanionWindowState(
        bool companionWindowAvailable,
        string expectedTitle,
        string expectedDetail)
    {
        var builder = new CardBuilder();

        var card = builder.BuildSignInRequested(companionWindowAvailable);

        using var document = JsonDocument.Parse(card.TemplateJson);
        AssertAdaptiveCard(document);
        Assert.Equal(["reconnect", "refresh"], ReadVerbs(document));
        Assert.Equal(["Reconnecter Outlook", "Actualiser"], ReadActionTitles(document));
        AssertTextContains(document, expectedTitle);
        AssertTextContains(document, expectedDetail);
    }

    [Fact]
    public void BuildError_RendersRetryReconnectAndTrimmedMessage()
    {
        var builder = new CardBuilder();
        var message = string.Concat("Consentement requis", Environment.NewLine, new string('x', 200));

        var card = builder.BuildError(message, retainedLastSuccessfulState: false);

        using var document = JsonDocument.Parse(card.TemplateJson);
        AssertAdaptiveCard(document);
        Assert.Equal(["refresh", "reconnect"], ReadVerbs(document));
        Assert.Equal(["Actualiser", "Reconnecter Outlook"], ReadActionTitles(document));
        AssertTextContains(document, "Impossible d'actualiser");
        AssertTextContains(document, "Les événements ne peuvent pas être chargés pour le moment.");
        var errorText = ReadAllText(document).Single(text => text.StartsWith("Consentement requis", StringComparison.Ordinal));
        Assert.DoesNotContain(Environment.NewLine, errorText);
        Assert.EndsWith("...", errorText);
        Assert.True(errorText.Length <= 140);
    }

    [Fact]
    public void BuildError_WhenRetainingLastState_RendersRetainedStateCopy()
    {
        var builder = new CardBuilder();

        var card = builder.BuildError("Graph indisponible", retainedLastSuccessfulState: true);

        using var document = JsonDocument.Parse(card.TemplateJson);
        AssertAdaptiveCard(document);
        Assert.Equal(["refresh", "reconnect"], ReadVerbs(document));
        AssertTextContains(document, "Le dernier état valide est conservé en mémoire.");
        AssertTextContains(document, "Graph indisponible");
    }

    [Fact]
    public void BuildNextEvents_EmptyState_RendersNoUpcomingWindowAndRefreshOnly()
    {
        var builder = new CardBuilder();
        var viewModel = new NextEventsViewModel(
            TestClock.FixedUtcNow,
            TimeZoneInfo.Utc.Id,
            [],
            TestClock.FixedUtcNow.AddDays(7));

        var card = builder.BuildNextEvents(viewModel);

        using var document = JsonDocument.Parse(card.TemplateJson);
        AssertAdaptiveCard(document);
        Assert.Equal(["refresh"], ReadVerbs(document));
        AssertTextContains(document, "Aucun événement à venir");
        AssertTextContains(document, "Aucun événement à venir jusqu'au 29/09.");
        AssertTextContains(document, "Mis à jour à 16:00");
    }

    [Fact]
    public void BuildNextEvents_ConsumesShapedEventsAndRendersTimedLocationAndAllDayRows()
    {
        var shaper = new EventShaper(TestClock.Fixed());
        var builder = new CardBuilder();
        var shaped = shaper.ShapeNextEvents(
            [
                NextEventFixtures.Single(),
                NextEventFixtures.AllDay()
            ],
            5,
            TimeZoneInfo.Utc);

        var first = builder.BuildNextEvents(shaped);
        var second = builder.BuildNextEvents(shaped);

        Assert.Equal(first.TemplateJson, second.TemplateJson);
        Assert.Equal(CardBuilder.EmptyDataJson, first.DataJson);
        Assert.False(first.IsPlaceholderContent);

        using var document = JsonDocument.Parse(first.TemplateJson);
        AssertAdaptiveCard(document);
        Assert.Equal(["refresh"], ReadVerbs(document));
        Assert.Equal(2, CountElementsOfType(document.RootElement, "ColumnSet"));
        AssertTextContains(document, "Prochains événements");
        AssertTextContains(document, "Mis à jour à 16:00");
        AssertTextContains(document, "One upcoming meeting");
        AssertTextContains(document, "16:30–17:00");
        AssertTextContains(document, "Dans 30 min");
        AssertTextContains(document, "Salle Ada");
        AssertTextContains(document, "Conference");
        AssertTextContains(document, "Toute la journée");
        AssertTextContains(document, "23/09");
    }

    [Fact]
    public void BuildNextEvents_RendersAtMostFiveEventRows()
    {
        var builder = new CardBuilder();
        var events = Enumerable.Range(1, 6)
            .Select(index => CreateTimedViewModel(index))
            .ToArray();
        var viewModel = new NextEventsViewModel(TestClock.FixedUtcNow, TimeZoneInfo.Utc.Id, events);

        var card = builder.BuildNextEvents(viewModel);

        using var document = JsonDocument.Parse(card.TemplateJson);
        AssertAdaptiveCard(document);
        Assert.Equal(5, CountElementsOfType(document.RootElement, "ColumnSet"));
        AssertTextContains(document, "Event 1");
        AssertTextContains(document, "Event 5");
        Assert.DoesNotContain("Event 6", ReadAllText(document));
    }

    [Fact]
    public void BuildPlaceholderCardJson_MatchesLoadingTemplate()
    {
        var builder = new CardBuilder();

        var placeholder = builder.BuildPlaceholderCardJson();

        using var placeholderDocument = JsonDocument.Parse(placeholder);
        using var loadingDocument = JsonDocument.Parse(builder.BuildLoading().TemplateJson);
        AssertAdaptiveCard(placeholderDocument);
        Assert.Equal(loadingDocument.RootElement.GetRawText(), placeholderDocument.RootElement.GetRawText());
    }

    private static NextEventViewModel CreateTimedViewModel(int index)
    {
        var localStart = TestClock.FixedUtcNow.AddMinutes(index * 10);
        var localEnd = localStart.AddMinutes(30);

        return new NextEventViewModel(
            $"event-{index}",
            $"Event {index}",
            localStart,
            localEnd,
            DateOnly.FromDateTime(localStart.DateTime),
            DateOnly.FromDateTime(localEnd.DateTime),
            TimeOnly.FromDateTime(localStart.DateTime),
            TimeOnly.FromDateTime(localEnd.DateTime),
            false,
            $"{localStart:HH:mm}–{localEnd:HH:mm}",
            $"Starts in {index * 10} min",
            TimeSpan.FromMinutes(30),
            null,
            null);
    }

    private static void AssertAdaptiveCard(JsonDocument document)
    {
        Assert.Equal("http://adaptivecards.io/schemas/adaptive-card.json", document.RootElement.GetProperty("$schema").GetString());
        Assert.Equal("AdaptiveCard", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("1.5", document.RootElement.GetProperty("version").GetString());
        Assert.True(document.RootElement.TryGetProperty("body", out var body));
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.NotEmpty(body.EnumerateArray());
        Assert.True(document.RootElement.TryGetProperty("actions", out var actions));
        Assert.Equal(JsonValueKind.Array, actions.ValueKind);
    }

    private static void AssertTextContains(JsonDocument document, string expectedText)
        => Assert.Contains(expectedText, ReadAllText(document));

    private static string[] ReadVerbs(JsonDocument document)
        => ReadActionStrings(document, "verb");

    private static string[] ReadActionTitles(JsonDocument document)
        => ReadActionStrings(document, "title");

    private static string[] ReadActionStrings(JsonDocument document, string propertyName)
        => document.RootElement
            .GetProperty("actions")
            .EnumerateArray()
            .Select(action => action.GetProperty(propertyName).GetString() ?? string.Empty)
            .ToArray();

    private static string[] ReadAllText(JsonDocument document)
    {
        var texts = new List<string>();
        AddTextValues(document.RootElement, texts);
        return texts.ToArray();
    }

    private static void AddTextValues(JsonElement element, List<string> texts)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("text", out var textElement)
                    && textElement.ValueKind == JsonValueKind.String)
                {
                    texts.Add(textElement.GetString() ?? string.Empty);
                }

                foreach (var property in element.EnumerateObject())
                {
                    AddTextValues(property.Value, texts);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    AddTextValues(item, texts);
                }

                break;
        }
    }

    private static int CountElementsOfType(JsonElement element, string type)
    {
        var count = 0;
        CountElementsOfType(element, type, ref count);
        return count;
    }

    private static void CountElementsOfType(JsonElement element, string type, ref int count)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("type", out var typeElement)
                    && typeElement.GetString() == type)
                {
                    count++;
                }

                foreach (var property in element.EnumerateObject())
                {
                    CountElementsOfType(property.Value, type, ref count);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CountElementsOfType(item, type, ref count);
                }

                break;
        }
    }
}
