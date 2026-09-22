using Microsoft.Windows.Widgets.Providers;

namespace OutlookNextEvent.App.Widgets;

public interface IWidgetHost
{
    void UpdateWidget(string widgetId, WidgetUpdatePayload payload);

    IReadOnlyList<WidgetSnapshot> GetWidgetSnapshots();
}

public sealed record WidgetSnapshot(
    string Id,
    string DefinitionId,
    bool IsActive,
    string CustomState);

public sealed class WidgetManagerHost : IWidgetHost
{
    public void UpdateWidget(string widgetId, WidgetUpdatePayload payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(widgetId);
        ArgumentNullException.ThrowIfNull(payload);

        var options = new WidgetUpdateRequestOptions(widgetId)
        {
            Template = payload.Template,
            Data = payload.Data,
            CustomState = payload.CustomState,
            IsPlaceholderContent = payload.IsPlaceholderContent
        };

        WidgetManager.GetDefault().UpdateWidget(options);
    }

    public IReadOnlyList<WidgetSnapshot> GetWidgetSnapshots()
        => WidgetManager.GetDefault()
            .GetWidgetInfos()
            .Select(widgetInfo =>
            {
                var context = widgetInfo.WidgetContext;
                return new WidgetSnapshot(
                    context.Id,
                    context.DefinitionId,
                    context.IsActive,
                    widgetInfo.CustomState ?? string.Empty);
            })
            .ToArray();
}
