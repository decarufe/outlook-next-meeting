using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using Microsoft.Windows.Widgets.Providers;

namespace OutlookNextEvent.App.Widgets;

[ComVisible(true)]
[Guid("8F3D7C7B-8D0D-41BF-8D9E-608A70D03E95")]
public sealed class NextEventsWidgetProvider : IWidgetProvider
{
    public const string WidgetDefinitionId = "NextEventsWidget";

    private static readonly ConcurrentDictionary<string, RunningWidget> RunningWidgets = new();
    private static readonly ManualResetEvent EmptyWidgetListEvent = new(false);

    private readonly IWidgetHost _widgetHost;
    private readonly INextEventsWidgetContentProvider _contentProvider;
    private readonly ICompanionSignInLauncher _signInLauncher;

    public NextEventsWidgetProvider()
        : this(NextEventsWidgetProviderDependencies.CreateDefault())
    {
    }

    public NextEventsWidgetProvider(NextEventsWidgetProviderDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);

        _widgetHost = dependencies.WidgetHost;
        _contentProvider = dependencies.ContentProvider;
        _signInLauncher = dependencies.SignInLauncher;

        RestoreRunningWidgets();
    }

    public static WaitHandle EmptyWidgets => EmptyWidgetListEvent;

    public void Activate(WidgetContext widgetContext)
    {
        var widget = Upsert(widgetContext);
        widget.IsActive = true;

        SendUpdate(widget, _contentProvider.BuildLoading());
        _ = RefreshWidgetAsync(widget.Id);
    }

    public void CreateWidget(WidgetContext widgetContext)
    {
        var widget = Upsert(widgetContext);
        widget.IsActive = widgetContext.IsActive;

        SendUpdate(widget, _contentProvider.BuildLoading());
        _ = RefreshWidgetAsync(widget.Id);
    }

    public void Deactivate(string widgetId)
    {
        if (RunningWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.IsActive = false;
        }
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        RunningWidgets.TryRemove(widgetId, out _);

        if (RunningWidgets.IsEmpty)
        {
            EmptyWidgetListEvent.Set();
        }
    }

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        var widget = Upsert(actionInvokedArgs.WidgetContext);

        switch (actionInvokedArgs.Verb?.Trim().ToLowerInvariant())
        {
            case "refresh":
                SendUpdate(widget, _contentProvider.BuildLoading());
                _ = RefreshWidgetAsync(widget.Id);
                break;

            case "connect":
            case "reconnect":
                _ = RequestCompanionSignInAsync(widget.Id);
                break;

            default:
                SendUpdate(widget, widget.LastSuccessfulPayload ?? _contentProvider.BuildConnectionRequired());
                break;
        }
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        var widget = Upsert(contextChangedArgs.WidgetContext);
        widget.IsActive = contextChangedArgs.WidgetContext.IsActive;

        SendUpdate(widget, widget.LastSuccessfulPayload ?? _contentProvider.BuildLoading());
        if (widget.LastSuccessfulPayload is null)
        {
            _ = RefreshWidgetAsync(widget.Id);
        }
    }

    private void RestoreRunningWidgets()
    {
        foreach (var snapshot in _widgetHost.GetWidgetSnapshots())
        {
            RunningWidgets.TryAdd(
                snapshot.Id,
                new RunningWidget(snapshot.Id, snapshot.DefinitionId)
                {
                    IsActive = snapshot.IsActive,
                    CustomState = snapshot.CustomState
                });
        }

        if (!RunningWidgets.IsEmpty)
        {
            EmptyWidgetListEvent.Reset();
        }
    }

    private static RunningWidget Upsert(WidgetContext widgetContext)
    {
        var widget = RunningWidgets.GetOrAdd(
            widgetContext.Id,
            id => new RunningWidget(id, widgetContext.DefinitionId));

        widget.DefinitionId = widgetContext.DefinitionId;
        EmptyWidgetListEvent.Reset();
        return widget;
    }

    private async Task RefreshWidgetAsync(string widgetId)
    {
        if (!RunningWidgets.TryGetValue(widgetId, out var widget))
        {
            return;
        }

        try
        {
            var payload = await _contentProvider.BuildContentAsync().ConfigureAwait(false);
            widget.LastSuccessfulPayload = payload.CustomState == "ready" ? payload : widget.LastSuccessfulPayload;
            SendUpdate(widget, payload);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SendUpdate(widget, _contentProvider.BuildError(exception, widget.LastSuccessfulPayload is not null));
        }
    }

    private async Task RequestCompanionSignInAsync(string widgetId)
    {
        if (!RunningWidgets.TryGetValue(widgetId, out var widget))
        {
            return;
        }

        try
        {
            var launched = await _signInLauncher.RequestSignInAsync(widgetId).ConfigureAwait(false);
            SendUpdate(widget, _contentProvider.BuildSignInRequested(launched));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SendUpdate(widget, _contentProvider.BuildError(exception, widget.LastSuccessfulPayload is not null));
        }
    }

    private void SendUpdate(RunningWidget widget, WidgetUpdatePayload payload)
    {
        widget.CustomState = payload.CustomState;
        _widgetHost.UpdateWidget(widget.Id, payload);
    }

    private sealed class RunningWidget(string id, string definitionId)
    {
        public string Id { get; } = id;

        public string DefinitionId { get; set; } = definitionId;

        public bool IsActive { get; set; }

        public string CustomState { get; set; } = string.Empty;

        public WidgetUpdatePayload? LastSuccessfulPayload { get; set; }
    }
}
