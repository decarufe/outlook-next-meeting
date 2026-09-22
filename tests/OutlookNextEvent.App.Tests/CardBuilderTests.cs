using System.Text.Json;
using OutlookNextEvent.Core.Cards;
using OutlookNextEvent.Core.Models;
using Xunit;

namespace OutlookNextEvent.App.Tests;

public sealed class CardBuilderTests
{
    [Fact]
    public void BuildNextEvents_RendersShapedEventsDeterministically()
    {
        var builder = new CardBuilder();
        var generatedAt = new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.FromHours(-4));
        var viewModel = new NextEventsViewModel(
            generatedAt,
            "Eastern Standard Time",
            [
                new NextEventViewModel(
                    "standup",
                    "Daily standup",
                    generatedAt.AddMinutes(30),
                    generatedAt.AddMinutes(60),
                    new DateOnly(2026, 9, 22),
                    new DateOnly(2026, 9, 22),
                    new TimeOnly(12, 30),
                    new TimeOnly(13, 0),
                    false,
                    "12:30–13:00",
                    "Starts in 30 min",
                    TimeSpan.FromMinutes(30),
                    "Salle Ada",
                    null)
            ]);

        var first = builder.BuildNextEvents(viewModel);
        var second = builder.BuildNextEvents(viewModel);

        Assert.Equal(first.TemplateJson, second.TemplateJson);
        Assert.Equal(CardBuilder.EmptyDataJson, first.DataJson);
        Assert.False(first.IsPlaceholderContent);
        Assert.Contains("Daily standup", first.TemplateJson);
        Assert.Contains("12:30–13:00", first.TemplateJson);
        Assert.Contains("Salle Ada", first.TemplateJson);
        Assert.Contains("Dans 30 min", first.TemplateJson);

        using var document = JsonDocument.Parse(first.TemplateJson);
        Assert.Equal("AdaptiveCard", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("1.5", document.RootElement.GetProperty("version").GetString());
        Assert.Equal(["refresh"], ReadVerbs(document));
    }

    [Fact]
    public void BuildNextEvents_RendersEmptyState()
    {
        var builder = new CardBuilder();
        var generatedAt = new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);
        var viewModel = new NextEventsViewModel(
            generatedAt,
            "UTC",
            [],
            generatedAt.AddDays(7));

        var card = builder.BuildNextEvents(viewModel);

        Assert.Contains("Aucun événement à venir", card.TemplateJson);
        Assert.Contains("29/09", card.TemplateJson);

        using var document = JsonDocument.Parse(card.TemplateJson);
        Assert.Equal(["refresh"], ReadVerbs(document));
    }

    [Fact]
    public void AuthAndErrorStates_RouteToWidgetActions()
    {
        var builder = new CardBuilder();

        using var signedOut = JsonDocument.Parse(builder.BuildSignedOut().TemplateJson);
        using var error = JsonDocument.Parse(builder.BuildError("Consentement requis", retainedLastSuccessfulState: false).TemplateJson);

        Assert.Equal(["connect", "refresh"], ReadVerbs(signedOut));
        Assert.Equal(["refresh", "reconnect"], ReadVerbs(error));
        Assert.Contains("Consentement requis", error.RootElement.GetRawText());
    }

    private static string[] ReadVerbs(JsonDocument document)
        => document.RootElement
            .GetProperty("actions")
            .EnumerateArray()
            .Select(action => action.GetProperty("verb").GetString() ?? string.Empty)
            .ToArray();
}
